// Google Play Developer API (Android Publisher v3) for one-time products: purchases.products.get / acknowledge.
// Auth: a Google Cloud service account (granted "View financial data" + "Manage orders" in Play Console). Its key JSON
// comes from the GOOGLE_PLAY_SERVICE_ACCOUNT_JSON env var (raw JSON or base64 of it) and is never committed.
import crypto from "node:crypto";

export const ANDROID_PUBLISHER_SCOPE = "https://www.googleapis.com/auth/androidpublisher";
const API = "https://androidpublisher.googleapis.com/androidpublisher/v3/applications";

/** Error with a stable code: invalid_token (the purchase does not exist for this app/product), auth (our
 * credentials are wrong or lack Play Console access), not_configured (no service account), unavailable (retry). */
export class GooglePlayError extends Error {
  constructor(code, message, status = 0) { super(message || code); this.code = code; this.status = status; }
}

export function parseServiceAccount(raw) {
  if (!raw || !String(raw).trim()) throw new GooglePlayError("not_configured", "GOOGLE_PLAY_SERVICE_ACCOUNT_JSON is not set");
  let text = String(raw).trim();
  if (!text.startsWith("{")) text = Buffer.from(text, "base64").toString("utf8");
  let sa;
  try { sa = JSON.parse(text); } catch { throw new GooglePlayError("not_configured", "service account JSON does not parse"); }
  if (!sa.client_email || !sa.private_key) throw new GooglePlayError("not_configured", "service account JSON lacks client_email/private_key");
  return sa;
}

const b64u = (v) => Buffer.from(typeof v === "string" ? v : JSON.stringify(v)).toString("base64url");

/** Signed RS256 JWT assertion for the OAuth jwt-bearer grant. */
export function signAssertion(sa, nowSec) {
  const head = b64u({ alg: "RS256", typ: "JWT", ...(sa.private_key_id ? { kid: sa.private_key_id } : {}) });
  const claims = b64u({ iss: sa.client_email, scope: ANDROID_PUBLISHER_SCOPE, aud: sa.token_uri || "https://oauth2.googleapis.com/token", iat: nowSec, exp: nowSec + 3600 });
  const sig = crypto.createSign("RSA-SHA256").update(head + "." + claims).sign(sa.private_key, "base64url");
  return head + "." + claims + "." + sig;
}

export function googlePlayClient({ serviceAccountJson, fetchImpl = globalThis.fetch, now = () => Date.now() } = {}) {
  let sa = null;
  let cached = null; // { token, exp }

  async function accessToken() {
    sa ??= parseServiceAccount(serviceAccountJson);
    const t = now();
    if (cached && cached.exp - 60_000 > t) return cached.token;
    const assertion = signAssertion(sa, Math.floor(t / 1000));
    let res;
    try {
      res = await fetchImpl(sa.token_uri || "https://oauth2.googleapis.com/token", {
        method: "POST",
        headers: { "content-type": "application/x-www-form-urlencoded" },
        body: new URLSearchParams({ grant_type: "urn:ietf:params:oauth:grant-type:jwt-bearer", assertion }).toString(),
      });
    } catch (e) { throw new GooglePlayError("unavailable", "token endpoint unreachable: " + e.message); }
    if (res.status >= 500) throw new GooglePlayError("unavailable", "token endpoint " + res.status, res.status);
    if (!res.ok) throw new GooglePlayError("auth", "service account token refused (" + res.status + ")", res.status);
    const body = await res.json();
    cached = { token: body.access_token, exp: t + (Number(body.expires_in) || 3600) * 1000 };
    return cached.token;
  }

  async function call(method, url, body) {
    const token = await accessToken();
    let res;
    try {
      res = await fetchImpl(url, { method, headers: { authorization: "Bearer " + token, ...(body ? { "content-type": "application/json" } : {}) }, body: body ? JSON.stringify(body) : undefined });
    } catch (e) { throw new GooglePlayError("unavailable", "Play API unreachable: " + e.message); }
    if (res.status === 401) cached = null;
    if (res.status === 400 || res.status === 404 || res.status === 410) throw new GooglePlayError("invalid_token", "Play API " + res.status, res.status);
    if (res.status === 401 || res.status === 403) throw new GooglePlayError("auth", "Play API " + res.status + " (service account lacks Play Console access?)", res.status);
    if (!res.ok) throw new GooglePlayError("unavailable", "Play API " + res.status, res.status);
    const text = await res.text();
    return text ? JSON.parse(text) : {};
  }

  const path = (pkg, productId, token) => `${API}/${encodeURIComponent(pkg)}/purchases/products/${encodeURIComponent(productId)}/tokens/${encodeURIComponent(token)}`;

  return {
    /** purchases.products.get → ProductPurchase { purchaseState 0 purchased | 1 cancelled | 2 pending, consumptionState, acknowledgementState, orderId, purchaseTimeMillis, productId?, ... } */
    getProductPurchase: (pkg, productId, token) => call("GET", path(pkg, productId, token)),
    /** purchases.products.acknowledge (non-consumables; the client's confirm also acknowledges, this is the safety net). */
    acknowledge: (pkg, productId, token) => call("POST", path(pkg, productId, token) + ":acknowledge", {}),
  };
}
