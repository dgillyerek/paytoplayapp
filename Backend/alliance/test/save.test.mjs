import test from "node:test";
import assert from "node:assert/strict";
import { createSave, mergeBlobs, clawbackBlob, assertNoPii, SAVE_SCHEMA } from "../src/save.mjs";
import { memorySaveRepo } from "./memory_save_repo.mjs";

test("merge: quests by chain id take max index/progress; gather takes min available", () => {
  const a = {
    schema: 1,
    quests: { "quest.chain.starter": { index: 2, progress: 1 } },
    world: { gather: { "theme_a.node.gather_quarry": { available: 5000, updatedAt: 1 } } },
    campaign: { wallet: { "res.hard": 10 }, wins: 1, cleared: ["a"] },
  };
  const b = {
    schema: 1,
    quests: { "quest.chain.starter": { index: 5, progress: 0 } },
    world: { gather: { "theme_a.node.gather_quarry": { available: 4200, updatedAt: 2 } } },
    campaign: { wallet: { "res.hard": 50 }, wins: 3, cleared: ["b"] },
  };
  const m = mergeBlobs(a, b);
  assert.equal(m.quests["quest.chain.starter"].index, 5);
  assert.equal(m.quests["quest.chain.starter"].progress, 0);
  assert.equal(m.world.gather["theme_a.node.gather_quarry"].available, 4200);
  assert.equal(m.campaign.wallet["res.hard"], 50);
  assert.equal(m.campaign.wins, 3);
  assert.deepEqual(m.campaign.cleared, ["a", "b"]);
});

test("merge: legacy singular quest shape", () => {
  const m = mergeBlobs(
    { quest: { chainId: "quest.chain.starter", index: 1, progress: 2 } },
    { quests: { "quest.chain.starter": { index: 3, progress: 0 } } },
  );
  assert.equal(m.quests["quest.chain.starter"].index, 3);
});

test("assertNoPii rejects email/name", () => {
  assert.equal(assertNoPii({ email: "a@b.c" }), "pii_forbidden");
  assert.equal(assertNoPii({ schema: 1, quests: {} }), null);
});

test("clawback subtracts wallet grants", () => {
  const out = clawbackBlob({ campaign: { wallet: { "res.hard": 100, "res.soft": 5 } } }, { "res.hard": 40 });
  assert.equal(out.campaign.wallet["res.hard"], 60);
  assert.equal(out.campaign.wallet["res.soft"], 5);
});

test("put: under13 without consent → 403; with consentGranted records and accepts", async () => {
  const repo = memorySaveRepo();
  const saves = createSave({ repo, ageBandOf: async () => "under13" });
  const player = { id: 1 };
  const blob = { schema: SAVE_SCHEMA, quests: { "quest.chain.starter": { index: 1, progress: 0 } }, world: { gather: { n1: { available: 8400, updatedAt: 1 } } } };
  let r = await saves.put(player, { baseVersion: 0, blob });
  assert.equal(r.status, 403);
  assert.equal(r.body.error, "consent_required");
  r = await saves.put(player, { baseVersion: 0, blob, consentGranted: true });
  assert.equal(r.status, 200);
  assert.equal(r.body.version, 1);
  assert.ok(r.body.consentAt);
  r = await saves.put(player, { baseVersion: 1, blob: { ...blob, quests: { "quest.chain.starter": { index: 4, progress: 1 } } } });
  assert.equal(r.status, 200);
  assert.equal(r.body.version, 2);
  assert.equal(r.body.merged, false);
});

test("put: conflict merges", async () => {
  const repo = memorySaveRepo();
  const saves = createSave({ repo, ageBandOf: async () => "13plus" });
  const player = { id: 2 };
  const blob1 = { schema: 1, quests: { "quest.chain.starter": { index: 1, progress: 0 } }, world: { gather: { q: { available: 8400, updatedAt: 1 } } } };
  await saves.put(player, { baseVersion: 0, blob: blob1 });
  // Client still thinks version 0
  const blob2 = { schema: 1, quests: { "quest.chain.starter": { index: 8, progress: 2 } }, world: { gather: { q: { available: 6720, updatedAt: 2 } } } };
  const r = await saves.put(player, { baseVersion: 0, blob: blob2 });
  assert.equal(r.status, 200);
  assert.equal(r.body.merged, true);
  assert.equal(r.body.blob.quests["quest.chain.starter"].index, 8);
  assert.equal(r.body.blob.world.gather.q.available, 6720);
});

test("clawback via onPurchaseRevoked is idempotent", async () => {
  const repo = memorySaveRepo();
  const saves = createSave({ repo, ageBandOf: async () => "13plus" });
  const player = { id: 3 };
  await saves.put(player, { baseVersion: 0, blob: { schema: 1, campaign: { wallet: { "res.hard": 500 } } } });
  const a = await saves.onPurchaseRevoked({ playerId: 3, purchaseId: 99, grants: { "res.hard": 200 } });
  assert.equal(a.applied, true);
  const g = await saves.get(player);
  assert.equal(g.body.blob.campaign.wallet["res.hard"], 300);
  const b = await saves.onPurchaseRevoked({ playerId: 3, purchaseId: 99, grants: { "res.hard": 200 } });
  assert.equal(b.duplicate, true);
  const g2 = await saves.get(player);
  assert.equal(g2.body.blob.campaign.wallet["res.hard"], 300);
});
