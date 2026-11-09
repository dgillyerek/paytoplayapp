import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { createIap, buildCatalog, googleVerifier } from "../src/iap.mjs";
import { GooglePlayError } from "../src/googleplay.mjs";
import { appStoreClient } from "../src/appstore.mjs";
import { appStoreVerifier } from "../src/iap.mjs";
import { memoryIapRepo } from "./memory_repo.mjs";

const PKG = "com.paytoplay.fantasykingdoma";
const GEMS_M = PKG + ".hard_m", TRACK = PKG + ".track_royal", POWER6 = PKG + ".power_t6";
const pack = (f) => JSON.parse(readFileSync(new URL("../../../Assets/ThemePack/fantasy_kingdom_a/store/" + f, import.meta.url), "utf8"));
const stores = JSON.parse(readFileSync(new URL("../../../Assets/Survival/Resources/Survival/stores.json", import.meta.url), "utf8"));

/** Fake Play API: token → ProductPurchase. Records calls. Unknown token → 404 (invalid_token). */
function fakePlay(purchases = {}) {
  const calls = [];
  return {
    calls,
    async getProductPurchase(pkg, productId, token) {
      calls.push(["get", pkg, productId, token]);
      const p = purchases[token];
      if (p instanceof Error) throw p;
      if (!p) throw new GooglePlayError("invalid_token", "404", 404);
      return typeof p === "function" ? p() : p;
    },
    async acknowledge(pkg, productId, token) { calls.push(["ack", pkg, productId, token]); return {}; },
  };
}
const bought = (extra = {}) => ({ purchaseState: 0, consumptionState: 0, acknowledgementState: 0, orderId: "GPA.1", purchaseTimeMillis: "1762700000000", ...extra });
function make(play, opts = {}) {
  const repo = memoryIapRepo();
  const iap = createIap({ repo, verifiers: { google_play: googleVerifier(play, PKG), app_store: appStoreVerifier(appStoreClient()) }, packageName: PKG, rtdnToken: "s3cret", log: { error() {}, warn() {}, info() {} }, ...opts });
  return { repo, iap };
}
const body = (productId, purchaseToken) => ({ store: "google_play", productId, purchaseToken });
const alice = { id: 1 }, bob = { id: 2 };

// ---- grant mapping ----
test("catalog covers every theme-pack product, and grants equal the stores.json sku offers", () => {
  const cat = buildCatalog(pack("products.json"), pack("iap_rewards.json"));
  assert.equal(cat.size, pack("products.json").products.length);
  for (const s of stores.stores) for (const o of s.offers) if (o.kind === "sku") {
    const item = [...cat.values()].find((c) => c.sku === o.sku);
    assert.ok(item, o.sku + " missing");
    assert.deepEqual({ ...item.grants }, o.grants || {}, o.sku);
  }
});
test("catalog refuses a product with no reward row and a reward row with no product", () => {
  assert.throws(() => buildCatalog({ products: [{ sku: "sku.x", product_id: "p.x" }] }, { rewards: [] }), /no reward row/);
  assert.throws(() => buildCatalog({ products: [] }, { rewards: [{ sku: "sku.x", type: "consumable", grants: {} }] }), /no product/);
  assert.throws(() => buildCatalog({ products: [{ sku: "sku.x", product_id: "p.x" }] }, { rewards: [{ sku: "sku.x", type: "consumable", grants: { "res.hard": -1 } }] }), /non-negative/);
});
test("a valid purchase returns the server's grants for that product, not anything the client sent", async () => {
  const { iap } = make(fakePlay({ t1: bought() }));
  const r = await iap.validate(alice, { ...body(GEMS_M, "t1"), grants: { "res.hard": 999999 } });
  assert.equal(r.status, 200);
  assert.equal(r.body.status, "granted");
  assert.deepEqual(r.body.grants, { "res.hard": 500 });
  assert.equal(r.body.consumable, true);
  assert.equal(r.body.fresh, true);
});
test("power pack t6 maps to its full reward bundle", async () => {
  const { iap } = make(fakePlay({ t6: bought() }));
  const r = await iap.validate(alice, body(POWER6, "t6"));
  assert.deepEqual(r.body.grants, { "res.hero_xp": 10, "res.runner_boost": 6, "res.speedup_m": 2000, "res.soft": 1000000, "res.food": 500000 });
});
test("non-consumable returns entitlements, no wallet grants, and is acknowledged server-side", async () => {
  const play = fakePlay({ tr: bought() });
  const { iap, repo } = make(play);
  const r = await iap.validate(alice, body(TRACK, "tr"));
  assert.deepEqual(r.body.grants, {});
  assert.deepEqual([...r.body.entitlements], ["ent.track_royal"]);
  assert.equal(r.body.consumable, false);
  await new Promise((s) => setImmediate(s));
  assert.ok(play.calls.some((c) => c[0] === "ack"));
  assert.deepEqual(await repo.listEntitlements(1), ["ent.track_royal"]);
});

// ---- validation ----
test("unknown product, missing token and bad store are refused before calling Google", async () => {
  const play = fakePlay();
  const { iap } = make(play);
  assert.equal((await iap.validate(alice, body(PKG + ".nope", "t"))).body.error, "unknown_product");
  assert.equal((await iap.validate(alice, body(GEMS_M, ""))).body.error, "bad_request");
  assert.equal((await iap.validate(alice, { store: "steam", productId: GEMS_M, purchaseToken: "t" })).body.error, "bad_request");
  assert.equal(play.calls.length, 0);
});
test("Google calls purchases.products.get with the package, product and token", async () => {
  const play = fakePlay({ tok: bought() });
  await make(play).iap.validate(alice, body(GEMS_M, "tok"));
  assert.deepEqual(play.calls[0], ["get", PKG, GEMS_M, "tok"]);
});
test("a token Google does not know is rejected and grants nothing", async () => {
  const { iap, repo } = make(fakePlay());
  const r = await iap.validate(alice, body(GEMS_M, "forged"));
  assert.equal(r.body.status, "rejected");
  assert.equal(r.body.reason, "invalid_token");
  assert.equal(repo.grants.length, 0);
});
test("cancelled purchase is rejected; pending is recorded but not granted", async () => {
  const { iap, repo } = make(fakePlay({ c: bought({ purchaseState: 1 }), p: bought({ purchaseState: 2 }) }));
  assert.equal((await iap.validate(alice, body(GEMS_M, "c"))).body.reason, "cancelled");
  assert.equal((await iap.validate(alice, body(GEMS_M, "p"))).body.status, "pending");
  assert.equal(repo.grants.length, 0);
  assert.equal((await repo.findPurchase("google_play", "p")).state, "pending");
});
test("pending that completes later is granted once", async () => {
  let state = 2;
  const { iap, repo } = make(fakePlay({ p: () => bought({ purchaseState: state }) }));
  assert.equal((await iap.validate(alice, body(GEMS_M, "p"))).body.status, "pending");
  state = 0;
  const r = await iap.validate(alice, body(GEMS_M, "p"));
  assert.equal(r.body.status, "granted");
  assert.deepEqual(r.body.grants, { "res.hard": 500 });
  assert.equal(repo.grants.length, 1);
});
test("Google reporting a different product for the token is rejected", async () => {
  const { iap } = make(fakePlay({ t: bought({ productId: PKG + ".hard_s" }) }));
  assert.equal((await iap.validate(alice, body(GEMS_M, "t"))).body.reason, "product_mismatch");
});
test("absent purchaseState (proto3 default 0) counts as purchased", async () => {
  const { iap } = make(fakePlay({ t: { orderId: "GPA.2" } }));
  assert.equal((await iap.validate(alice, body(GEMS_M, "t"))).body.status, "granted");
});
test("a consumed token we never recorded is never granted", async () => {
  const { iap, repo } = make(fakePlay({ t: bought({ consumptionState: 1 }) }));
  assert.equal((await iap.validate(alice, body(GEMS_M, "t"))).body.reason, "already_consumed");
  assert.equal(repo.grants.length, 0);
});
test("Google outage or bad credentials → 503 so the device keeps the order open and retries", async () => {
  const { iap, repo } = make(fakePlay({ a: new GooglePlayError("unavailable", "500", 500), b: new GooglePlayError("auth", "403", 403), c: new GooglePlayError("not_configured") }));
  for (const t of ["a", "b", "c"]) assert.equal((await iap.validate(alice, body(GEMS_M, t))).status, 503);
  assert.equal(repo.grants.length, 0);
});
test("App Store path answers 501 (stub) and grants nothing", async () => {
  const { iap, repo } = make(fakePlay());
  const r = await iap.validate(alice, { store: "app_store", productId: GEMS_M, transactionId: "2000000001", signedTransaction: "eyJ..." });
  assert.equal(r.status, 501);
  assert.equal(r.body.error, "app_store_not_enabled");
  assert.equal(repo.grants.length, 0);
});

// ---- idempotency ----
test("the same token sent twice grants once; the replay returns the same grants with fresh=false", async () => {
  const { iap, repo } = make(fakePlay({ t: bought() }));
  const a = await iap.validate(alice, body(GEMS_M, "t"));
  const b = await iap.validate(alice, body(GEMS_M, "t"));
  assert.equal(a.body.fresh, true);
  assert.equal(b.body.fresh, false);
  assert.deepEqual(b.body.grants, a.body.grants);
  assert.equal(a.body.purchaseId, b.body.purchaseId);
  assert.equal(repo.grants.length, 1);
  assert.equal(repo.purchases.size, 1);
});
test("replay after the device consumed it still answers granted (crash between grant and confirm)", async () => {
  let consumed = 0;
  const { iap, repo } = make(fakePlay({ t: () => bought({ consumptionState: consumed }) }));
  await iap.validate(alice, body(GEMS_M, "t"));
  consumed = 1;
  const r = await iap.validate(alice, body(GEMS_M, "t"));
  assert.equal(r.body.status, "granted");
  assert.equal(repo.grants.length, 1);
});
test("concurrent duplicates grant once", async () => {
  const { iap, repo } = make(fakePlay({ t: bought() }));
  const rs = await Promise.all([1, 2, 3, 4].map(() => iap.validate(alice, body(GEMS_M, "t"))));
  assert.equal(rs.filter((r) => r.body.fresh).length, 1);
  assert.equal(repo.grants.length, 1);
});
test("another account cannot redeem a consumable token already redeemed", async () => {
  const { iap, repo } = make(fakePlay({ t: bought() }));
  await iap.validate(alice, body(GEMS_M, "t"));
  const r = await iap.validate(bob, body(GEMS_M, "t"));
  assert.equal(r.status, 409);
  assert.equal(r.body.error, "owned_by_other_account");
  assert.equal(repo.grants.length, 1);
});
test("a non-consumable restores to a new account (reinstall) as entitlements only", async () => {
  const { iap, repo } = make(fakePlay({ t: bought({ acknowledgementState: 1 }) }));
  await iap.validate(alice, body(TRACK, "t"));
  const r = await iap.validate(bob, body(TRACK, "t"));
  assert.equal(r.body.status, "granted");
  assert.equal(r.body.restored, true);
  assert.deepEqual(r.body.grants, {});
  assert.deepEqual(await repo.listEntitlements(2), ["ent.track_royal"]);
});
test("a token sent under a different product id is refused", async () => {
  const { iap } = make(fakePlay({ t: bought() }));
  await iap.validate(alice, body(GEMS_M, "t"));
  assert.equal((await iap.validate(alice, body(POWER6, "t"))).body.reason, "product_mismatch");
});

// ---- RTDN ----
const push = (n, id = "m1") => ({ message: { messageId: id, data: Buffer.from(JSON.stringify({ version: "1.0", packageName: PKG, eventTimeMillis: "1", ...n })).toString("base64") }, subscription: "projects/x/subscriptions/y" });
const rtdnUrl = (t = "s3cret") => new URL("https://x/v1/iap/rtdn?token=" + t);
test("RTDN needs the push token", async () => {
  const { iap } = make(fakePlay());
  assert.equal((await iap.rtdn(rtdnUrl("wrong"), push({ testNotification: {} }))).status, 401);
  assert.equal((await iap.rtdn(new URL("https://x/v1/iap/rtdn"), push({ testNotification: {} }))).status, 401);
  assert.equal((await make(fakePlay(), { rtdnToken: "" }).iap.rtdn(rtdnUrl(""), push({ testNotification: {} }))).status, 401);
  assert.equal((await iap.rtdn(rtdnUrl(), push({ testNotification: {} }))).status, 200);
});
test("voided purchase (refund) revokes its grants and entitlements, and the token can never be re-granted", async () => {
  const { iap, repo } = make(fakePlay({ t: bought() }));
  await iap.validate(alice, body(TRACK, "t"));
  const r = await iap.rtdn(rtdnUrl(), push({ voidedPurchaseNotification: { purchaseToken: "t", orderId: "GPA.1", productType: 2, refundType: 1 } }));
  assert.equal(r.body.revoked, true);
  assert.equal((await repo.findPurchase("google_play", "t")).state, "refunded");
  assert.ok(repo.grants.every((g) => g.revoked));
  assert.deepEqual(await repo.listEntitlements(1), []);
  assert.equal((await iap.validate(alice, body(TRACK, "t"))).body.status, "rejected");
});
test("duplicate RTDN delivery is acknowledged and changes nothing more", async () => {
  const { iap } = make(fakePlay({ t: bought() }));
  await iap.validate(alice, body(GEMS_M, "t"));
  const n = { voidedPurchaseNotification: { purchaseToken: "t", productType: 2 } };
  assert.equal((await iap.rtdn(rtdnUrl(), push(n, "dup"))).body.duplicate, undefined);
  assert.equal((await iap.rtdn(rtdnUrl(), push(n, "dup"))).body.duplicate, true);
});
test("one-time product cancelled (type 2) closes a pending purchase; other packages are ignored", async () => {
  const { iap, repo } = make(fakePlay({ p: bought({ purchaseState: 2 }) }));
  await iap.validate(alice, body(GEMS_M, "p"));
  await iap.rtdn(rtdnUrl(), push({ oneTimeProductNotification: { version: "1.0", notificationType: 2, purchaseToken: "p", sku: GEMS_M } }));
  assert.equal((await repo.findPurchase("google_play", "p")).state, "cancelled");
  const other = await iap.rtdn(rtdnUrl(), { message: { messageId: "o", data: Buffer.from(JSON.stringify({ packageName: "com.other" })).toString("base64") } });
  assert.equal(other.body.ignored, "other_package");
});

// ---- COPPA / Play Families ----
test("only purchase-state fields of Google's answer are stored, never buyer data", async () => {
  const raw = bought({ regionCode: "CA", obfuscatedExternalAccountId: "acct", obfuscatedExternalProfileId: "prof", developerPayload: "x", kind: "androidpublisher#productPurchase" });
  const { iap } = make(fakePlay({ t: raw }));
  const repo = memoryIapRepo();
  let stored;
  const spy = { ...repo, recordGrant: async (r) => { stored = r.payload; return repo.recordGrant(r); } };
  const iap2 = createIap({ repo: spy, verifiers: { google_play: googleVerifier(fakePlay({ t: raw }), PKG) }, packageName: PKG, log: {} });
  await iap2.validate(alice, body(GEMS_M, "t"));
  assert.deepEqual(Object.keys(stored).sort(), ["acknowledgementState", "consumptionState", "orderId", "purchaseState", "purchaseTimeMillis"]);
  assert.ok(iap);
});
