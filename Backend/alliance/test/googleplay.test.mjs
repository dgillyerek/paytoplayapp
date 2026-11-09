import test from "node:test";
import assert from "node:assert/strict";
import crypto from "node:crypto";
import { googlePlayClient, parseServiceAccount, signAssertion, GooglePlayError, ANDROID_PUBLISHER_SCOPE } from "../src/googleplay.mjs";

const { privateKey, publicKey } = crypto.generateKeyPairSync("rsa", { modulusLength: 2048 });
const SA = { type: "service_account", client_email: "iap@paytoplay.iam.gserviceaccount.com", private_key: privateKey.export({ type: "pkcs8", format: "pem" }), private_key_id: "k1", token_uri: "https://oauth2.googleapis.com/token" };

function fakeFetch(handler) {
  const calls = [];
  const f = async (url, init = {}) => { calls.push({ url: String(url), init }); const [status, body] = handler(String(url), init, calls.length); return new Response(body == null ? null : JSON.stringify(body), { status }); };
  f.calls = calls;
  return f;
}
const ok = (url) => (url.includes("oauth2") ? [200, { access_token: "at-1", expires_in: 3600 }] : [200, { purchaseState: 0, orderId: "GPA.9" }]);

test("service account JSON is read raw or base64, and refused when missing", () => {
  assert.equal(parseServiceAccount(JSON.stringify(SA)).client_email, SA.client_email);
  assert.equal(parseServiceAccount(Buffer.from(JSON.stringify(SA)).toString("base64")).client_email, SA.client_email);
  assert.throws(() => parseServiceAccount(""), (e) => e.code === "not_configured");
  assert.throws(() => parseServiceAccount("{}"), (e) => e.code === "not_configured");
});
test("assertion is an RS256 JWT for the androidpublisher scope, verifiable with the key", () => {
  const jwt = signAssertion(SA, 1000);
  const [h, c, s] = jwt.split(".");
  const claims = JSON.parse(Buffer.from(c, "base64url"));
  assert.equal(JSON.parse(Buffer.from(h, "base64url")).alg, "RS256");
  assert.equal(claims.scope, ANDROID_PUBLISHER_SCOPE);
  assert.equal(claims.iss, SA.client_email);
  assert.equal(claims.exp - claims.iat, 3600);
  assert.ok(crypto.createVerify("RSA-SHA256").update(h + "." + c).verify(publicKey, s, "base64url"));
});
test("products.get hits the right URL with the bearer token, and the token is cached", async () => {
  const f = fakeFetch(ok);
  const g = googlePlayClient({ serviceAccountJson: JSON.stringify(SA), fetchImpl: f, now: () => 1_000_000 });
  const p = await g.getProductPurchase("com.paytoplay.fantasykingdoma", "com.paytoplay.fantasykingdoma.hard_m", "tok/+=");
  await g.getProductPurchase("com.paytoplay.fantasykingdoma", "com.paytoplay.fantasykingdoma.hard_m", "tok2");
  assert.equal(p.orderId, "GPA.9");
  assert.equal(f.calls.filter((c) => c.url.includes("oauth2")).length, 1);
  const get = f.calls[1];
  assert.equal(get.url, "https://androidpublisher.googleapis.com/androidpublisher/v3/applications/com.paytoplay.fantasykingdoma/purchases/products/com.paytoplay.fantasykingdoma.hard_m/tokens/tok%2F%2B%3D");
  assert.equal(get.init.headers.authorization, "Bearer at-1");
});
test("acknowledge POSTs to :acknowledge", async () => {
  const f = fakeFetch((url) => (url.includes("oauth2") ? [200, { access_token: "a" }] : [204, null]));
  await googlePlayClient({ serviceAccountJson: JSON.stringify(SA), fetchImpl: f }).acknowledge("p", "x", "t");
  assert.ok(f.calls[1].url.endsWith("/tokens/t:acknowledge"));
  assert.equal(f.calls[1].init.method, "POST");
});
test("HTTP errors map to stable codes", async () => {
  const codes = {};
  for (const status of [400, 404, 410, 401, 403, 500, 503]) {
    const f = fakeFetch((url) => (url.includes("oauth2") ? [200, { access_token: "a" }] : [status, { error: { code: status } }]));
    await assert.rejects(googlePlayClient({ serviceAccountJson: JSON.stringify(SA), fetchImpl: f }).getProductPurchase("p", "x", "t"), (e) => { codes[status] = e.code; return e instanceof GooglePlayError; });
  }
  assert.deepEqual(codes, { 400: "invalid_token", 404: "invalid_token", 410: "invalid_token", 401: "auth", 403: "auth", 500: "unavailable", 503: "unavailable" });
  const refused = fakeFetch(() => [400, { error: "invalid_grant" }]);
  await assert.rejects(googlePlayClient({ serviceAccountJson: JSON.stringify(SA), fetchImpl: refused }).getProductPurchase("p", "x", "t"), (e) => e.code === "auth");
  const down = async () => { throw new Error("ECONNRESET"); };
  await assert.rejects(googlePlayClient({ serviceAccountJson: JSON.stringify(SA), fetchImpl: down }).getProductPurchase("p", "x", "t"), (e) => e.code === "unavailable");
});
