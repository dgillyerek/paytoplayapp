// Integration test against Neon cloud-saves-dev branch. Skipped unless SAVE_TEST_DATABASE_URL (or IAP_TEST_DATABASE_URL) is set.
import test from "node:test";
import assert from "node:assert/strict";
import crypto from "node:crypto";
import { pgSaveRepo } from "../src/save_repo.mjs";
import { mergeBlobs, clawbackBlob } from "../src/save.mjs";

const url = process.env.SAVE_TEST_DATABASE_URL || process.env.IAP_TEST_DATABASE_URL;
const opts = { skip: url ? false : "set SAVE_TEST_DATABASE_URL to the cloud-saves Neon dev branch" };

test("pg save repo: put, conflict merge, consent, clawback", opts, async () => {
  const pg = (await import("pg")).default;
  const pool = new pg.Pool({ connectionString: url, max: 4 });
  const run = crypto.randomBytes(4).toString("hex");
  try {
    const p = await pool.query(
      "INSERT INTO players(device_hash, token_hash, display_name, age_band) VALUES($1,$2,$3,'under13') RETURNING id",
      ["savetest-" + run, "savetest-" + run, "SaveTest"],
    );
    const playerId = Number(p.rows[0].id);
    const repo = pgSaveRepo(pool);
    assert.equal(await repo.getConsent(playerId), null);
    const consentAt = await repo.setConsent(playerId);
    assert.ok(consentAt);
    const blob = { schema: 1, quests: { "quest.chain.starter": { index: 2, progress: 1 } }, world: { gather: { q: { available: 8400, updatedAt: 1 } } }, campaign: { wallet: { "res.hard": 100 } } };
    const a = await repo.put(playerId, 0, blob, mergeBlobs);
    assert.equal(a.version, 1);
    assert.equal(a.merged, false);
    const b = await repo.put(playerId, 0, { ...blob, quests: { "quest.chain.starter": { index: 10, progress: 0 } }, world: { gather: { q: { available: 5000, updatedAt: 2 } } } }, mergeBlobs);
    assert.equal(b.merged, true);
    assert.equal(b.blob.quests["quest.chain.starter"].index, 10);
    assert.equal(b.blob.world.gather.q.available, 5000);
    // Need a purchase row for FK on clawback — insert minimal if 004 tables exist
    const purchase = await pool.query(
      `INSERT INTO iap_purchases(player_id, store, product_id, sku, purchase_token, consumable, state)
       VALUES($1,'google_play','com.paytoplay.fantasykingdoma.hard_m','sku.hard_m',$2,true,'granted') RETURNING id`,
      [playerId, "savetest-tok-" + run],
    );
    const purchaseId = Number(purchase.rows[0].id);
    const c = await repo.clawback(playerId, purchaseId, { "res.hard": 40 }, clawbackBlob);
    assert.equal(c.applied, true);
    const got = await repo.get(playerId);
    assert.equal(got.blob.campaign.wallet["res.hard"], 60);
    const d = await repo.clawback(playerId, purchaseId, { "res.hard": 40 }, clawbackBlob);
    assert.equal(d.duplicate, true);
    await pool.query("DELETE FROM save_clawbacks WHERE player_id=$1", [playerId]);
    await pool.query("DELETE FROM player_saves WHERE player_id=$1", [playerId]);
    await pool.query("DELETE FROM iap_purchases WHERE id=$1", [purchaseId]);
    await pool.query("DELETE FROM players WHERE id=$1", [playerId]);
  } finally {
    await pool.end();
  }
});
