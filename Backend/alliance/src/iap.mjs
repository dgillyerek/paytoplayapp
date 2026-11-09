// Real-money purchases: receipt validation, idempotent recording, server-side grants, refunds (RTDN).
// The client never decides what a purchase is worth: grants come from the theme pack's iap_rewards.json, looked up
// here by store product id (products.json). One purchase token is redeemed once (UNIQUE (store, purchase_token)).
import crypto from "node:crypto";
import productsJson from "../../../Assets/ThemePack/fantasy_kingdom_a/store/products.json" with { type: "json" };
import rewardsJson from "../../../Assets/ThemePack/fantasy_kingdom_a/store/iap_rewards.json" with { type: "json" };
import { GooglePlayError } from "./googleplay.mjs";
import { AppStoreNotEnabled } from "./appstore.mjs";

export const DEFAULT_PACKAGE = "com.paytoplay.fantasykingdoma";
export const STORES = ["google_play", "app_store"];

/** productId → { sku, productId, consumable, grants, entitlements }. Throws when a product has no reward row (or vice versa). */
export function buildCatalog(products = productsJson, rewards = rewardsJson) {
  const bySku = new Map();
  for (const r of rewards.rewards || []) {
    if (r.type !== "consumable" && r.type !== "non_consumable") throw new Error(`iap_rewards: ${r.sku} bad type ${r.type}`);
    for (const [k, v] of Object.entries(r.grants || {})) if (!Number.isInteger(v) || v < 0) throw new Error(`iap_rewards: ${r.sku} grant ${k} must be a non-negative integer`);
    bySku.set(r.sku, r);
  }
  const byProduct = new Map();
  for (const p of products.products || []) {
    const r = bySku.get(p.sku);
    if (!r) throw new Error(`iap_rewards: no reward row for ${p.sku}`);
    byProduct.set(p.product_id, Object.freeze({ sku: p.sku, productId: p.product_id, consumable: r.type === "consumable",
      grants: Object.freeze({ ...(r.grants || {}) }), entitlements: Object.freeze([...(r.entitlements || [])]) }));
    bySku.delete(p.sku);
  }
  if (bySku.size) throw new Error(`iap_rewards: rows with no product: ${[...bySku.keys()].join(", ")}`);
  return byProduct;
}

/** Google Play verifier: ProductPurchase → { state: purchased|pending|cancelled|mismatch, ... }. Absent ints mean 0 (proto3 JSON). */
export function googleVerifier(client, packageName = DEFAULT_PACKAGE) {
  return async ({ productId, purchaseToken }) => {
    const p = await client.getProductPurchase(packageName, productId, purchaseToken);
    const ps = p.purchaseState ?? 0;
    const state = p.productId && p.productId !== productId ? "mismatch" : ps === 0 ? "purchased" : ps === 2 ? "pending" : "cancelled";
    return {
      state, raw: p, orderId: p.orderId ?? null,
      purchasedAt: p.purchaseTimeMillis ? new Date(Number(p.purchaseTimeMillis)).toISOString() : null,
      consumed: (p.consumptionState ?? 0) === 1, acknowledged: (p.acknowledgementState ?? 0) === 1,
      acknowledge: () => client.acknowledge(packageName, productId, purchaseToken),
    };
  };
}

/** App Store verifier (stub until the App Store Server API is wired; see appstore.mjs). */
export function appStoreVerifier(client) {
  return async ({ signedTransaction, transactionId }) => client.verifyTransaction(signedTransaction, transactionId);
}

/** Keep only the purchase-state fields of Google's answer: nothing about the buyer (COPPA / Play Families). */
export const PAYLOAD_FIELDS = ["orderId", "purchaseState", "consumptionState", "acknowledgementState", "purchaseTimeMillis", "purchaseType", "quantity"];
export const minimalPayload = (raw) => Object.fromEntries(PAYLOAD_FIELDS.filter((k) => raw && raw[k] !== undefined).map((k) => [k, raw[k]]));
export const AGE_BANDS = ["under13", "13plus", "unknown"];

const str = (v, max) => (typeof v === "string" && v.length > 0 && v.length <= max && !/[\u0000-\u001f]/.test(v) ? v : null);
const reply = (status, body) => ({ status, body });
const granted = (item, rec) => reply(200, { status: "granted", sku: item.sku, productId: item.productId, consumable: item.consumable,
  grants: rec.grants, entitlements: rec.entitlements, fresh: rec.outcome === "new", restored: rec.outcome === "restored", purchaseId: rec.purchaseId });

/**
 * The purchase service, independent of HTTP and Postgres. repo: see iap_repo.mjs (pg) / test/memory_repo.mjs.
 * verifiers: { google_play, app_store } async ({ productId, purchaseToken, signedTransaction, transactionId }) → result.
 */
export function createIap({ repo, verifiers, catalog = buildCatalog(), packageName = DEFAULT_PACKAGE, rtdnToken = "", onRevoked = null, log = console }) {
  async function validate(player, b) {
    const store = STORES.includes(b?.store) ? b.store : null;
    const productId = str(b?.productId, 200);
    if (!store || !productId) return reply(400, { error: "bad_request" });
    const item = catalog.get(productId);
    if (!item) return reply(400, { error: "unknown_product" });
    const token = store === "google_play" ? str(b.purchaseToken, 2048) : str(b.transactionId, 200);
    if (!token) return reply(400, { error: "bad_request" });

    const existing = await repo.findPurchase(store, token);
    if (existing && ["refunded", "voided", "cancelled"].includes(existing.state)) return reply(200, { status: "rejected", reason: existing.state });
    if (existing && existing.product_id !== productId) return reply(200, { status: "rejected", reason: "product_mismatch" });

    let v;
    try {
      v = await verifiers[store]({ productId, purchaseToken: token, signedTransaction: str(b.signedTransaction, 12000), transactionId: token });
    } catch (e) {
      if (e instanceof AppStoreNotEnabled) return reply(501, { error: e.code });
      if (e instanceof GooglePlayError && e.code === "invalid_token") return reply(200, { status: "rejected", reason: "invalid_token" });
      log.error?.("iap verify failed", e?.code || "", e?.message || e);
      return reply(503, { error: "verify_unavailable" }); // keep the order open on the device; it retries
    }

    if (v.state === "mismatch") return reply(200, { status: "rejected", reason: "product_mismatch" });
    if (v.state === "cancelled") {
      if (existing) await repo.setState(store, token, "cancelled");
      return reply(200, { status: "rejected", reason: "cancelled" });
    }
    const row = { playerId: player.id, store, productId, sku: item.sku, token, orderId: v.orderId, consumable: item.consumable,
      purchasedAt: v.purchasedAt, payload: minimalPayload(v.raw), grants: item.grants, entitlements: item.entitlements };
    if (v.state === "pending") {
      await repo.recordPending(row);
      return reply(200, { status: "pending" });
    }
    // Consumed with no record of ours: redeemed somewhere that skipped this server. Never grant it.
    if (v.consumed && !existing) return reply(200, { status: "rejected", reason: "already_consumed" });

    const rec = await repo.recordGrant(row);
    if (rec.outcome === "revoked") return reply(200, { status: "rejected", reason: rec.state });
    if (rec.outcome === "other_player") return reply(409, { error: "owned_by_other_account" });
    if (!item.consumable && !v.acknowledged && v.acknowledge) v.acknowledge().catch((e) => log.warn?.("iap acknowledge failed", e?.message || e));
    return granted(item, rec);
  }

  async function entitlements(player) {
    return reply(200, { entitlements: await repo.listEntitlements(player.id) });
  }

  /** Pub/Sub push: { message: { data: base64(DeveloperNotification), messageId }, subscription }. Auth: ?token= shared secret. */
  async function rtdn(url, b) {
    const got = url.searchParams.get("token") || "";
    if (!rtdnToken || got.length !== rtdnToken.length || !crypto.timingSafeEqual(Buffer.from(got), Buffer.from(rtdnToken))) return reply(401, { error: "unauthorized" });
    const msg = b?.message;
    if (!msg?.data || !msg?.messageId) return reply(200, { ok: true, ignored: "no_message" });
    let n;
    try { n = JSON.parse(Buffer.from(msg.data, "base64").toString("utf8")); } catch { return reply(200, { ok: true, ignored: "bad_data" }); }
    if (n.packageName && n.packageName !== packageName) return reply(200, { ok: true, ignored: "other_package" });

    const voided = n.voidedPurchaseNotification, one = n.oneTimeProductNotification;
    const kind = voided ? "voided" : one ? "one_time_" + one.notificationType : n.testNotification ? "test" : "other";
    const token = voided?.purchaseToken || one?.purchaseToken || null;
    // Act first, then log the message id: every action is idempotent, so a Pub/Sub redelivery after a crash is safe.
    let result = { ok: true };
    if (voided && token) {
      // refundType 1 = full refund, 2 = quantity-based partial. Either way the token's grants and entitlements are revoked.
      const r = await repo.revoke("google_play", token, voided.refundType ? "refunded" : "voided");
      let clawback = null;
      if (r.found && onRevoked) clawback = await onRevoked({ playerId: r.playerId, purchaseId: r.purchaseId, grants: r.grants || {} });
      result = { ok: true, revoked: r.found, clawback };
    } else if (one && one.notificationType === 2 && token) { // ONE_TIME_PRODUCT_CANCELED: a pending purchase was cancelled
      await repo.cancelPending("google_play", token);
    } // PURCHASED (1): the device re-delivers it and validates; test notifications need nothing.
    const first = await repo.recordEvent(String(msg.messageId), kind, token, n);
    return reply(200, first ? result : { ...result, duplicate: true });
  }

  return { validate, entitlements, rtdn, catalog };
}
