import test from "node:test";
import assert from "node:assert/strict";
import { audienceMode, blockedByAudience } from "../src/audience.mjs";
import { effectiveChatMode, safetyRoutes } from "../src/safety.mjs";
import { createSave, SAVE_SCHEMA } from "../src/save.mjs";
import { createIap, googleVerifier, appStoreVerifier } from "../src/iap.mjs";
import { appStoreClient } from "../src/appstore.mjs";
import { memorySaveRepo } from "./memory_save_repo.mjs";
import { memoryIapRepo } from "./memory_repo.mjs";

const PKG = "com.paytoplay.fantasykingdoma";
const quiet = { error() {}, warn() {}, info() {} };

test("AUDIENCE_MODE defaults to teen_plus; families is opt-in; junk falls back to teen_plus", () => {
  assert.equal(audienceMode({}), "teen_plus");
  assert.equal(audienceMode({ AUDIENCE_MODE: "families" }), "families");
  assert.equal(audienceMode({ AUDIENCE_MODE: " Families " }), "families");
  assert.equal(audienceMode({ AUDIENCE_MODE: "kids" }), "teen_plus");
  assert.ok(blockedByAudience("teen_plus", "under13"));
  for (const b of ["13plus", "unknown"]) assert.ok(!blockedByAudience("teen_plus", b));
  for (const b of ["under13", "13plus", "unknown"]) assert.ok(!blockedByAudience("families", b));
});

test("chat mode: teen_plus shuts under13 completely; families keeps presets; 13plus free in both", () => {
  assert.equal(effectiveChatMode("under13", "presets", "teen_plus"), "off");
  assert.equal(effectiveChatMode("under13", "presets", "families"), "presets");
  assert.equal(effectiveChatMode("under13", "presets"), "presets"); // default = previous (families) behavior
  for (const a of ["teen_plus", "families"]) assert.equal(effectiveChatMode("13plus", null, a), "free");
});

function iap(audience, band) {
  const play = { async getProductPurchase() { return { purchaseState: 0, consumptionState: 0, acknowledgementState: 0, orderId: "GPA.1", purchaseTimeMillis: "1762700000000" }; }, async acknowledge() { return {}; } };
  return createIap({ repo: memoryIapRepo(), verifiers: { google_play: googleVerifier(play, PKG), app_store: appStoreVerifier(appStoreClient()) }, packageName: PKG, log: quiet, audience, ageBandOf: async () => band });
}
const buy = { store: "google_play", productId: PKG + ".hard_m", purchaseToken: "tok-1" };

test("iap: teen_plus refuses under13 accounts; 13plus and unknown are validated normally", async () => {
  const r = await iap("teen_plus", "under13").validate({ id: 1 }, buy);
  assert.equal(r.status, 403);
  assert.equal(r.body.error, "audience_13plus_only");
  for (const band of ["13plus", "unknown"]) assert.equal((await iap("teen_plus", band).validate({ id: 1 }, buy)).body.status, "granted");
});

test("iap: families keeps the previous behavior (no server age gate)", async () => {
  for (const band of ["under13", "13plus", "unknown"]) assert.equal((await iap("families", band).validate({ id: 1 }, buy)).body.status, "granted");
});

const blob = { schema: SAVE_SCHEMA, quests: { "quest.chain.starter": { index: 1, progress: 0 } } };
test("cloud save: teen_plus blocks under13 even with consent; families keeps the consent path; 13plus saves in both", async () => {
  let r = await createSave({ repo: memorySaveRepo(), ageBandOf: async () => "under13", audience: "teen_plus" }).put({ id: 1 }, { baseVersion: 0, blob, consentGranted: true });
  assert.equal(r.status, 403);
  assert.equal(r.body.error, "audience_13plus_only");
  r = await createSave({ repo: memorySaveRepo(), ageBandOf: async () => "under13", audience: "families" }).put({ id: 1 }, { baseVersion: 0, blob, consentGranted: true });
  assert.equal(r.status, 200);
  for (const audience of ["teen_plus", "families"]) {
    r = await createSave({ repo: memorySaveRepo(), ageBandOf: async () => "13plus", audience }).put({ id: 1 }, { baseVersion: 0, blob });
    assert.equal(r.status, 200);
  }
});

// Route wiring against a fake db: 13+ free text is filtered, queued, and muted players are refused.
function chatEnv(player, { mutedNow = false } = {}) {
  const sql = [];
  const inserted = [];
  const q = async (s, args) => {
    sql.push(s);
    if (s.startsWith("SELECT age_band")) return { rows: [player] };
    if (s.startsWith("SELECT chat_muted_until")) return { rows: [{ m: mutedNow }] };
    if (s.startsWith("INSERT INTO chat_reports")) { inserted.push(args); return { rows: [] }; }
    return { rows: [] };
  };
  const sent = [];
  const routes = { "POST /v1/chat": async (_r, b) => { sent.push(b.body); return { status: 201, clone: () => ({ json: async () => ({ message: { id: 77 } }) }) }; } };
  const json = (b, st = 200) => ({ status: st, body: b });
  const r = safetyRoutes({ q, err: (c, st = 400) => json({ error: c }, st), json, clean: (s) => (typeof s === "string" ? s : ""), limited: async () => false, routes, audience: "teen_plus", log: quiet });
  return { r, sent, inserted };
}

test("13+ chat: profanity is masked and queued; links and phone numbers are refused and queued; clean text passes", async () => {
  const { r, sent, inserted } = chatEnv({ age_band: "13plus", chat_mode: null });
  assert.equal((await r["POST /v1/chat"]({}, { body: "well shit" }, { id: 5 })).status, 201);
  assert.equal(sent[0], "well ****");
  assert.deepEqual(inserted[0], [77, 5, "well shit", "profanity"]);
  const link = await r["POST /v1/chat"]({}, { body: "join discord.gg/x" }, { id: 5 });
  assert.equal(link.status, 403);
  assert.equal(link.body.error, "links_not_allowed");
  assert.equal((await r["POST /v1/chat"]({}, { body: "text me 0917 555 1234" }, { id: 5 })).body.error, "personal_info");
  assert.equal(inserted.length, 3);
  assert.equal((await r["POST /v1/chat"]({}, { body: "good game all" }, { id: 5 })).status, 201);
  assert.equal(sent.at(-1), "good game all");
  assert.equal(inserted.length, 3);
});

test("13+ chat: a muted player cannot send", async () => {
  const { r, sent } = chatEnv({ age_band: "13plus", chat_mode: null }, { mutedNow: true });
  const res = await r["POST /v1/chat"]({}, { body: "hello" }, { id: 5 });
  assert.equal(res.status, 403);
  assert.equal(res.body.error, "chat_muted");
  assert.equal(sent.length, 0);
});
