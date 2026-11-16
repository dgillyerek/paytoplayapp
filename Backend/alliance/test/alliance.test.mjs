import test from "node:test";
import assert from "node:assert/strict";
import { ROLE, canSetRole, joinRefusal, rewardedHelps, helpSeconds, TASK_KEY, inTransaction, allianceRoutes, RULES } from "../src/alliance.mjs";

test("only officers and up change roles, and only below their own", () => {
  assert.ok(canSetRole(ROLE.LEADER, ROLE.MEMBER, ROLE.OFFICER));
  assert.ok(canSetRole(ROLE.OFFICER, ROLE.MEMBER, 3));
  assert.ok(!canSetRole(ROLE.OFFICER, ROLE.MEMBER, ROLE.OFFICER), "an officer cannot make another officer");
  assert.ok(!canSetRole(ROLE.OFFICER, ROLE.OFFICER, ROLE.MEMBER), "an officer cannot demote an equal");
  assert.ok(!canSetRole(3, ROLE.MEMBER, 2), "rank 3 is not an officer");
  assert.ok(!canSetRole(ROLE.LEADER, ROLE.MEMBER, ROLE.LEADER), "leadership only moves when the leader leaves");
  assert.ok(!canSetRole(ROLE.LEADER, ROLE.MEMBER, 0));
  assert.ok(!canSetRole(ROLE.LEADER, ROLE.MEMBER, 2.5));
});

test("join is refused for a missing, closed or full alliance", () => {
  assert.deepEqual(joinRefusal(undefined), ["not_found", 404]);
  assert.deepEqual(joinRefusal({ is_open: false, n: 1, max_members: 50 }), ["closed", 403]);
  assert.deepEqual(joinRefusal({ is_open: true, n: 50, max_members: 50 }), ["full", 409]);
  assert.equal(joinRefusal({ is_open: true, n: 49, max_members: 50 }), null);
});

test("help rewards stop at the daily cap", () => {
  assert.equal(rewardedHelps(3, 3), 3);
  assert.equal(rewardedHelps(3, RULES.helpRewardsPerDay + 1), 2);
  assert.equal(rewardedHelps(3, RULES.helpRewardsPerDay + 10), 0);
  assert.equal(rewardedHelps(0, 5), 0);
});

test("help seconds are clamped and task keys are a safe charset", () => {
  assert.equal(helpSeconds(1), RULES.minHelpSeconds);
  assert.equal(helpSeconds(1e9), RULES.maxHelpSeconds);
  assert.equal(helpSeconds("nope"), RULES.minHelpSeconds);
  assert.equal(helpSeconds(120.9), 120);
  assert.ok(TASK_KEY.test("build.castle:2"));
  assert.ok(!TASK_KEY.test("Build Castle"));
  assert.ok(!TASK_KEY.test(""));
});

// A fake pool that records statements, so the transaction shape can be checked without a database.
function fakePool(answer = () => ({ rows: [] })) {
  const log = [];
  const client = { query: async (sql, args) => { log.push(sql.trim().split(/\s+/)[0]); const r = answer(sql, args); if (r instanceof Error) throw r; return r; }, release: () => log.push("release") };
  return { log, pool: { connect: async () => client } };
}

test("transactions commit on success and roll back on error", async () => {
  const ok = fakePool();
  assert.equal(await inTransaction(ok.pool, async (c) => { await c.query("SELECT 1"); return 7; }), 7);
  assert.deepEqual(ok.log, ["BEGIN", "SELECT", "COMMIT", "release"]);
  const bad = fakePool();
  await assert.rejects(inTransaction(bad.pool, async () => { throw new Error("boom"); }), /boom/);
  assert.deepEqual(bad.log, ["BEGIN", "ROLLBACK", "release"]);
});

const helpers = () => {
  const json = (body, status = 200) => ({ status, body });
  return { json, err: (code, status = 400) => json({ error: code }, status), limited: async () => false, clean: (s, max) => (typeof s === "string" ? s.trim().slice(0, max) : "") };
};

test("join locks the alliance row and inserts inside one transaction", async () => {
  const { log, pool } = fakePool((sql) => {
    if (sql.includes("FOR UPDATE")) return { rows: [{ id: 9, is_open: true, max_members: 50 }] };
    if (sql.includes("count(*)")) return { rows: [{ n: 10 }] };
    return { rows: [] };
  });
  const q = async (sql) => (sql.includes("FROM alliances WHERE id=$1") ? { rows: [{ id: 9 }] } : { rows: [] });
  const routes = allianceRoutes({ q, pool, ...helpers() });
  const res = await routes["POST /v1/alliances/join"](null, { allianceId: 9 }, { id: 1 });
  assert.equal(res.status, 200);
  assert.deepEqual(log, ["BEGIN", "SELECT", "SELECT", "INSERT", "COMMIT", "release"]);
});

test("join into a full alliance inserts nothing", async () => {
  const { log, pool } = fakePool((sql) => {
    if (sql.includes("FOR UPDATE")) return { rows: [{ id: 9, is_open: true, max_members: 2 }] };
    if (sql.includes("count(*)")) return { rows: [{ n: 2 }] };
    return { rows: [] };
  });
  const routes = allianceRoutes({ q: async () => ({ rows: [] }), pool, ...helpers() });
  const res = await routes["POST /v1/alliances/join"](null, { allianceId: 9 }, { id: 1 });
  assert.deepEqual(res, { status: 409, body: { error: "full" } });
  assert.ok(!log.includes("INSERT"));
});

test("a leaving leader hands the alliance to the next member in the same transaction", async () => {
  const { log, pool } = fakePool((sql) => (sql.includes("ORDER BY role DESC") ? { rows: [{ player_id: 2 }] } : { rows: [] }));
  const q = async () => ({ rows: [{ alliance_id: 9, role: ROLE.LEADER }] });
  const routes = allianceRoutes({ q, pool, ...helpers() });
  const res = await routes["POST /v1/alliances/leave"](null, {}, { id: 1 });
  assert.equal(res.status, 200);
  assert.deepEqual(log, ["BEGIN", "DELETE", "SELECT", "UPDATE", "UPDATE", "COMMIT", "release"]);
});

test("filling a help request thanks its helpers once", async () => {
  const sent = [];
  const q = async (sql, args) => {
    sent.push(sql.trim().split(/\s+/)[0]);
    if (sql.includes("FROM alliance_members")) return { rows: [{ alliance_id: 9, role: 1 }] };
    if (sql.startsWith("WITH ok")) return { rows: [{ id: args[0], open: args[0] !== 5 }] };
    if (sql.includes("count(*)::int c")) return { rows: [{ c: 2 }] };
    return { rows: [] };
  };
  const routes = allianceRoutes({ q, pool: null, ...helpers() });
  const res = await routes["POST /v1/help/give"](null, { requestIds: [4, 5] }, { id: 1 });
  assert.deepEqual(res.body, { helped: 2, gold: 2 * RULES.goldPerHelp });
  assert.equal(sent.filter((s) => s === "INSERT").length, 1, "one gift insert, for request 5 only");
});
