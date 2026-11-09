// Integration test of iap_repo.mjs against a real Postgres with 001-004 applied. Skipped unless IAP_TEST_DATABASE_URL
// points at a throwaway Neon branch (never main). Every row it writes is removed at the end.
import test from "node:test";
import assert from "node:assert/strict";
import crypto from "node:crypto";
import { pgIapRepo } from "../src/iap_repo.mjs";

const url = process.env.IAP_TEST_DATABASE_URL;
const opts = { skip: url ? false : "set IAP_TEST_DATABASE_URL to a Neon test branch" };

test("pg repo: idempotent grant, other-account refusal, restore, refund", opts, async () => {
  const pg = (await import("pg")).default;
  const pool = new pg.Pool({ connectionString: url, max: 4 });
  const run = crypto.randomBytes(4).toString("hex");
  const players = [];
  try {
    for (const n of ["a", "b"]) {
      const r = await pool.query("INSERT INTO players(device_hash, token_hash, display_name) VALUES($1,$2,$3) RETURNING id", ["iaptest-" + run + n, "iaptest-" + run + n, "IapTest"]);
      players.push(Number(r.rows[0].id));
    }
    const [a, b] = players;
    const repo = pgIapRepo(pool);
    const row = (playerId, token, consumable, extra = {}) => ({ playerId, store: "google_play", productId: "com.paytoplay.fantasykingdoma.hard_m", sku: "sku.hard_m", token,
      orderId: "GPA.t", consumable, purchasedAt: new Date().toISOString(), payload: { purchaseState: 0 }, grants: { "res.hard": 500 }, entitlements: [], ...extra });

    const tok = "iaptest-" + run + "-c";
    const results = await Promise.all([1, 2, 3, 4, 5].map(() => repo.recordGrant(row(a, tok, true))));
    assert.equal(results.filter((r) => r.outcome === "new").length, 1);
    assert.ok(results.every((r) => r.grants["res.hard"] === 500));
    assert.equal((await pool.query("SELECT count(*)::int c FROM iap_grants g JOIN iap_purchases p ON p.id=g.purchase_id WHERE p.purchase_token=$1", [tok])).rows[0].c, 1);
    assert.equal((await repo.recordGrant(row(b, tok, true))).outcome, "other_player");

    const nc = "iaptest-" + run + "-nc";
    const ent = { productId: "com.paytoplay.fantasykingdoma.track_royal", sku: "sku.track_royal", grants: {}, entitlements: ["ent.track_royal"] };
    assert.equal((await repo.recordGrant(row(a, nc, false, ent))).outcome, "new");
    const restored = await repo.recordGrant(row(b, nc, false, ent));
    assert.equal(restored.outcome, "restored");
    assert.deepEqual(restored.grants, {});
    assert.deepEqual(await repo.listEntitlements(b), ["ent.track_royal"]);

    const pend = "iaptest-" + run + "-p";
    await repo.recordPending(row(a, pend, true));
    assert.equal((await repo.findPurchase("google_play", pend)).state, "pending");
    assert.equal((await repo.recordGrant(row(a, pend, true))).outcome, "new");

    assert.equal((await repo.revoke("google_play", nc, "refunded")).found, true);
    assert.deepEqual(await repo.listEntitlements(a), []);
    assert.equal((await repo.recordGrant(row(a, nc, false, ent))).outcome, "revoked");

    const mid = "iaptest-" + run;
    assert.equal(await repo.recordEvent(mid, "voided", nc, {}), true);
    assert.equal(await repo.recordEvent(mid, "voided", nc, {}), false);
    await pool.query("DELETE FROM iap_rtdn_events WHERE message_id=$1", [mid]);
  } finally {
    if (players.length) {
      await pool.query("DELETE FROM player_entitlements WHERE player_id = ANY($1)", [players]);
      await pool.query("DELETE FROM iap_grants WHERE purchase_id IN (SELECT id FROM iap_purchases WHERE player_id = ANY($1)) OR player_id = ANY($1)", [players]);
      await pool.query("DELETE FROM iap_purchases WHERE player_id = ANY($1)", [players]);
      await pool.query("DELETE FROM players WHERE id = ANY($1)", [players]);
    }
    await pool.end();
  }
});
