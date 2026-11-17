// PayToPlay Survival alliance API (Neon Function). DATABASE_URL is injected by Neon; nothing secret lives in git.
import pg from "pg";
import crypto from "node:crypto";
import { attachDatabasePool } from "@neon/functions";
import { worldRoutes, ensurePlaced, WORLD } from "./world.mjs";
import { createIap, googleVerifier, appStoreVerifier, DEFAULT_PACKAGE, AGE_BANDS } from "./iap.mjs";
import { pgIapRepo } from "./iap_repo.mjs";
import { googlePlayClient } from "./googleplay.mjs";
import { appStoreClient } from "./appstore.mjs";
import { createSave } from "./save.mjs";
import { pgSaveRepo } from "./save_repo.mjs";
import { safetyRoutes } from "./safety.mjs";
import { deletionRoutes } from "./deletion.mjs";
import { allianceRoutes, RULES } from "./alliance.mjs";

const pool = new pg.Pool({ connectionString: process.env.DATABASE_URL, max: 5 });
attachDatabasePool(pool);
const q = (sql, args = []) => pool.query(sql, args);

export { RULES };
const LIMITS = { signin: [10, 3600], chat: [20, 60], write: [60, 60], read: [240, 60] };

const sha = (s) => crypto.createHash("sha256").update(s).digest("hex");
const json = (body, status = 200) => new Response(JSON.stringify(body), { status, headers: { "content-type": "application/json", "access-control-allow-origin": "*", "access-control-allow-headers": "authorization,content-type" } });
const err = (code, status = 400) => json({ error: code }, status);
const clean = (s, max) => (typeof s === "string" ? s.replace(/[\u0000-\u001f]/g, "").trim().slice(0, max) : "");

// Old rate_hits rows are swept at most once a minute per instance, in the background, not on every request.
let lastSweep = 0;
function sweepRateHits() {
  const now = Date.now();
  if (now - lastSweep < 60_000) return;
  lastSweep = now;
  q("DELETE FROM rate_hits WHERE at < now() - interval '1 day'").catch(() => {});
}
async function limited(playerKey, bucket) {
  const [n, win] = LIMITS[bucket];
  sweepRateHits();
  const { rows } = await q("SELECT count(*)::int c FROM rate_hits WHERE player_id=$1 AND bucket=$2 AND at > now() - make_interval(secs => $3)", [playerKey, bucket, win]);
  if (rows[0].c >= n) return true;
  await q("INSERT INTO rate_hits(player_id,bucket) VALUES($1,$2)", [playerKey, bucket]);
  return false;
}
async function me(req) {
  const a = req.headers.get("authorization") || "";
  if (!a.startsWith("Bearer ")) return null;
  const { rows } = await q("UPDATE players SET last_seen=now() WHERE token_hash=$1 RETURNING id, display_name", [sha(a.slice(7))]);
  return rows[0] || null;
}
const routes = {
  "POST /v1/signin": async (_r, b) => {
    const device = clean(b.deviceId, 128);
    if (device.length < 8) return err("bad_device");
    const dh = sha("dev:" + device);
    if (await limited(-Number.parseInt(dh.slice(0, 12), 16), "signin")) return err("rate_limited", 429);
    const token = crypto.randomBytes(32).toString("base64url");
    const name = clean(b.name, 20) || "Knight" + dh.slice(0, 4);
    const { rows } = await q(`INSERT INTO players(device_hash, token_hash, display_name, castle_x, castle_y) VALUES($1,$2,$3,$4,$5)
      ON CONFLICT (device_hash) DO UPDATE SET token_hash=EXCLUDED.token_hash, last_seen=now() RETURNING id, display_name, power, castle_x, castle_y`,
      [dh, sha(token), name, 0, 0]);
    await ensurePlaced(q, rows[0].id);
    const placed = (await q("SELECT id, display_name, power, castle_x, castle_y, shield_until FROM players WHERE id=$1", [rows[0].id])).rows[0];
    return json({ token, player: placed });
  },
  "POST /v1/profile": async (_r, b, p) => {
    const name = clean(b.name, 20); const ok = (v) => (Number.isInteger(v) && v >= 0 && v < 1e9 ? v : null);
    // Troops: the client may seed its garrison once (capped); after that the server owns the count and ignores this field.
    const seed = ok(b.troops) == null ? null : Math.min(ok(b.troops), WORLD.seedMax);
    await q(`UPDATE players SET display_name=coalesce(nullif($2,''),display_name), power=coalesce($3,power), hero_power=coalesce($5,hero_power),
      troops=CASE WHEN NOT troops_seeded AND $4::int IS NOT NULL THEN $4::int ELSE troops END, troops_at=CASE WHEN NOT troops_seeded AND $4::int IS NOT NULL THEN now() ELSE troops_at END, troops_seeded=troops_seeded OR $4::int IS NOT NULL WHERE id=$1`,
      [p.id, name, ok(b.power), seed, ok(b.heroPower)]);
    // Stored resources (what PvP raiders can take). Unclaimed raid losses are taken off so a sync can't undo them.
    if (b.stock && typeof b.stock === "object") {
      const pending = (await q("SELECT loot FROM battle_reports WHERE player_id=$1 AND NOT claimed AND kind='defend_lost'", [p.id])).rows;
      const stock = {};
      for (const k of WORLD.lootKinds) { const v = ok(b.stock[k]); if (v != null) stock[k] = v; }
      for (const r of pending) for (const [k, v] of Object.entries(r.loot || {})) if (v < 0 && k in stock) stock[k] = Math.max(0, stock[k] + v);
      await q("UPDATE players SET stock=$2 WHERE id=$1", [p.id, JSON.stringify(stock)]);
    }
    return json({ ok: true });
  },
};
Object.assign(routes, allianceRoutes({ q, pool, err, json, limited, clean }));
Object.assign(routes, worldRoutes({ q, pool, err, json, limited, clean }));

// Purchases (iap.mjs). Secrets come only from env: GOOGLE_PLAY_SERVICE_ACCOUNT_JSON, IAP_RTDN_PUSH_TOKEN.
const PKG = process.env.GOOGLE_PLAY_PACKAGE_NAME || DEFAULT_PACKAGE;
const saveRepo = pgSaveRepo(pool);
const ageBandOf = async (p) => (await q("SELECT age_band FROM players WHERE id=$1", [p.id])).rows[0]?.age_band ?? "unknown";
const saves = createSave({ repo: saveRepo, ageBandOf });
const iap = createIap({
  repo: pgIapRepo(pool),
  verifiers: {
    google_play: googleVerifier(googlePlayClient({ serviceAccountJson: process.env.GOOGLE_PLAY_SERVICE_ACCOUNT_JSON }), PKG),
    app_store: appStoreVerifier(appStoreClient(process.env)),
  },
  packageName: PKG,
  rtdnToken: process.env.IAP_RTDN_PUSH_TOKEN || "",
  onRevoked: (info) => saves.onPurchaseRevoked(info),
});
const asResponse = (r) => json(r.body, r.status);
Object.assign(routes, {
  "POST /v1/iap/validate": async (_r, b, p) => (await limited(p.id, "write")) ? err("rate_limited", 429) : asResponse(await iap.validate(p, b)),
  "GET /v1/iap/entitlements": async (_r, _b, p) => asResponse(await iap.entitlements(p)),
  // Account age band (COPPA / Play Families). The only age data kept; the client's purchase gate reads it.
  "GET /v1/account": async (_r, _b, p) => json({ id: p.id, ageBand: await ageBandOf(p), cloudSaveConsentAt: await saveRepo.getConsent(p.id) }),
  "POST /v1/account/age_band": async (_r, b, p) => {
    if (!AGE_BANDS.includes(b.ageBand)) return err("bad_age_band");
    await q("UPDATE players SET age_band=$2 WHERE id=$1", [p.id, b.ageBand]);
    return json({ ok: true, ageBand: b.ageBand });
  },
  // Cloud saves (local-first on the client). Blob is game progress only — no PII.
  "GET /v1/save": async (_r, _b, p) => asResponse(await saves.get(p)),
  "PUT /v1/save": async (_r, b, p) => (await limited(p.id, "write")) ? err("rate_limited", 429) : asResponse(await saves.put(p, b)),
});
// Child safety (safety.mjs): chat mode / presets / report / block, restricted names, sticky under13. Wraps routes above.
Object.assign(routes, safetyRoutes({ q, err, json, clean, limited, routes }));
// In-app account deletion (deletion.mjs): scrubs the player and removes their data (incl. player_saves / save_clawbacks).
Object.assign(routes, deletionRoutes({ pool, err, json, limited }));
export default {
  async fetch(req) {
    if (req.method === "OPTIONS") return json({});
    const url = new URL(req.url);
    const key = req.method + " " + url.pathname.replace(/\/$/, "");
    if (key === "GET /v1/health") return json({ ok: true, rules: RULES });
    const rtdn = key === "POST /v1/iap/rtdn"; // Pub/Sub push: no player token, authenticated by ?token= instead
    const h = rtdn ? null : routes[key]; if (!h && !rtdn) return err("not_found", 404);
    try {
      let body = {};
      const maxBody = key === "PUT /v1/save" ? 220000 : key.startsWith("POST /v1/iap/") ? 16384 : 4096; // App Store JWS + Pub/Sub envelopes are bigger
      if (req.method === "POST" || req.method === "PUT") { const t = await req.text(); if (t.length > maxBody) return err("too_large", 413); body = t ? JSON.parse(t) : {}; }
      if (rtdn) return asResponse(await iap.rtdn(url, body));
      if (key === "POST /v1/signin") return await h(req, body);
      const p = await me(req); if (!p) return err("unauthorized", 401);
      if (req.method === "GET" && await limited(p.id, "read")) return err("rate_limited", 429);
      return await h(req, body, p);
    } catch (e) { console.error(e); return err(e instanceof SyntaxError ? "bad_json" : "server_error", e instanceof SyntaxError ? 400 : 500); }
  },
};
