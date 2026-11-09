import test from "node:test";
import assert from "node:assert/strict";
import { deletePlayer, deletionRoutes, DELETE_STEPS } from "../src/deletion.mjs";

const fakePool = (member) => {
  const log = [];
  const client = {
    query: async (sql, args) => {
      log.push([sql.replace(/\s+/g, " ").trim(), args]);
      if (sql.startsWith("SELECT alliance_id, role")) return { rows: member ? [member] : [] };
      if (sql.startsWith("SELECT player_id FROM alliance_members")) return { rows: [] };
      return { rows: [] };
    },
    release: () => log.push(["release"]),
  };
  return { pool: { connect: async () => client }, log };
};

test("deletion runs in one transaction and scrubs the player row", async () => {
  const { pool, log } = fakePool(null);
  assert.ok(await deletePlayer(pool, 42, () => "r"));
  const sql = log.map((l) => l[0]);
  assert.equal(sql[0], "BEGIN");
  assert.equal(sql.at(-2), "COMMIT");
  assert.equal(sql.at(-1), "release");
  const scrub = log.find((l) => l[0].startsWith("UPDATE players SET device_hash"));
  assert.deepEqual(scrub[1], [42, "deleted:r", "deleted:r"]);
  assert.match(scrub[0], /display_name='Deleted'/);
  for (const [s] of DELETE_STEPS) assert.ok(sql.includes(s), s);
});
test("the last member's alliance is dropped", async () => {
  const { pool, log } = fakePool({ alliance_id: 7, role: 5 });
  await deletePlayer(pool, 1);
  assert.ok(log.some((l) => l[0] === "DELETE FROM alliances WHERE id=$1" && l[1][0] === 7));
});
test("the route wants an explicit confirm", async () => {
  const json = (b, s = 200) => ({ status: s, body: b });
  const err = (c, s = 400) => json({ error: c }, s);
  const { pool } = fakePool(null);
  const r = deletionRoutes({ pool, err, json, limited: async () => false })["POST /v1/account/delete"];
  assert.equal((await r({}, {}, { id: 1 })).status, 400);
  assert.deepEqual((await r({}, { confirm: "delete" }, { id: 1 })).body, { deleted: true });
});
