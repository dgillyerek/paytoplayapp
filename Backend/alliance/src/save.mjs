// Cloud saves: versioned JSON blob per players.id. Local-first on the client; this module is GET/PUT + merge + clawback.
// COPPA: blob must not contain personal info (enforced by shape checks). Under-13 writes need cloud_save_consent_at.

export const SAVE_SCHEMA = 1;
export const MAX_BLOB_BYTES = 200_000;

/** Monotonic int fields under campaign: take max. */
const CAMPAIGN_MAX_INT = ["wins", "battles", "bounty", "runner_unlocked"];
/** Array-of-string fields: union. */
const CAMPAIGN_UNION = ["cleared", "bought_once"];

function isPlain(o) {
  return o != null && typeof o === "object" && !Array.isArray(o);
}

/** Reject blobs that look like they carry PII (defense in depth; client also must not send these). */
export function assertNoPii(blob) {
  if (!isPlain(blob)) return "bad_blob";
  for (const k of ["email", "name", "display_name", "displayName", "birthDate", "birth_date", "adId", "advertisingId", "deviceId", "phone"]) {
    if (k in blob) return "pii_forbidden";
  }
  return null;
}

export function blobBytes(blob) {
  return Buffer.byteLength(JSON.stringify(blob ?? {}), "utf8");
}

/** Max of two numbers; non-numbers ignored. */
function maxNum(a, b) {
  const x = typeof a === "number" && Number.isFinite(a) ? a : null;
  const y = typeof b === "number" && Number.isFinite(b) ? b : null;
  if (x == null) return y;
  if (y == null) return x;
  return Math.max(x, y);
}

function unionStr(a, b) {
  const s = new Set();
  for (const arr of [a, b]) if (Array.isArray(arr)) for (const v of arr) if (typeof v === "string") s.add(v);
  return [...s].sort();
}

/** Merge wallet / resource maps: max per key (monotonic progress). */
export function mergeWallet(a, b) {
  const out = {};
  for (const src of [a, b]) {
    if (!isPlain(src)) continue;
    for (const [k, v] of Object.entries(src)) {
      if (typeof v === "number" && Number.isFinite(v) && v >= 0) out[k] = maxNum(out[k], Math.floor(v)) ?? Math.floor(v);
    }
  }
  return out;
}

function mergeCampaign(a, b) {
  if (!isPlain(a)) return isPlain(b) ? structuredClone(b) : {};
  if (!isPlain(b)) return structuredClone(a);
  const out = { ...structuredClone(a), ...structuredClone(b) };
  if (isPlain(a.wallet) || isPlain(b.wallet)) out.wallet = mergeWallet(a.wallet, b.wallet);
  for (const k of CAMPAIGN_MAX_INT) out[k] = maxNum(a[k], b[k]) ?? out[k];
  for (const k of CAMPAIGN_UNION) out[k] = unionStr(a[k], b[k]);
  // heroes: per-hero max level/stars/shards, owned = either
  if (isPlain(a.heroes) || isPlain(b.heroes)) {
    const heroes = {};
    const keys = new Set([...Object.keys(a.heroes || {}), ...Object.keys(b.heroes || {})]);
    for (const id of keys) {
      const x = a.heroes?.[id] || {}, y = b.heroes?.[id] || {};
      heroes[id] = {
        owned: !!(x.owned || y.owned),
        level: maxNum(x.level, y.level) ?? 1,
        stars: maxNum(x.stars, y.stars) ?? 0,
        shards: maxNum(x.shards, y.shards) ?? 0,
      };
    }
    out.heroes = heroes;
  }
  return out;
}

function mergeVillage(a, b) {
  if (!isPlain(a)) return isPlain(b) ? structuredClone(b) : {};
  if (!isPlain(b)) return structuredClone(a);
  // Prefer the higher schema version's shape; levels/stock take max; built union by id.
  const out = { v: maxNum(a.v, b.v) ?? 1, levels: {}, stock: {}, built: [], timers: [], pending: [] };
  for (const src of [a, b]) {
    if (isPlain(src.levels)) for (const [k, v] of Object.entries(src.levels)) out.levels[k] = maxNum(out.levels[k], v) ?? v;
    if (isPlain(src.stock)) {
      for (const [k, v] of Object.entries(src.stock)) {
        // VillageSave shape: [stored, lastTick]
        if (Array.isArray(v) && v.length >= 2) {
          const prev = Array.isArray(out.stock[k]) ? out.stock[k] : [0, 0];
          out.stock[k] = [maxNum(prev[0], v[0]) ?? v[0] ?? 0, maxNum(prev[1], v[1]) ?? v[1] ?? 0];
        } else if (isPlain(v)) {
          const prev = isPlain(out.stock[k]) ? out.stock[k] : {};
          out.stock[k] = {
            amount: maxNum(prev.amount, v.amount) ?? v.amount ?? 0,
            produced: maxNum(prev.produced, v.produced) ?? v.produced ?? 0,
          };
        }
      }
    }
  }
  const byId = new Map();
  for (const src of [a, b]) {
    if (!Array.isArray(src.built)) continue;
    for (const row of src.built) {
      if (!isPlain(row) || typeof row.id !== "string") continue;
      const prev = byId.get(row.id);
      if (!prev || (typeof row.level === "number" && row.level >= (prev.level || 0))) byId.set(row.id, { ...prev, ...row });
    }
  }
  out.built = [...byId.values()];
  // timers / pending: last-write (b wins) — absolute clocks, not monotonic
  out.timers = Array.isArray(b.timers) ? structuredClone(b.timers) : Array.isArray(a.timers) ? structuredClone(a.timers) : [];
  out.pending = Array.isArray(b.pending) ? structuredClone(b.pending) : Array.isArray(a.pending) ? structuredClone(a.pending) : [];
  out.temp_builder_until = b.temp_builder_until ?? a.temp_builder_until ?? 0;
  return out;
}

function mergeQuestEntry(a, b) {
  if (!isPlain(a)) return isPlain(b) ? structuredClone(b) : null;
  if (!isPlain(b)) return structuredClone(a);
  const index = maxNum(a.index, b.index) ?? 0;
  let progress;
  if ((a.index || 0) === (b.index || 0)) progress = maxNum(a.progress, b.progress) ?? 0;
  else progress = (a.index || 0) > (b.index || 0) ? (a.progress || 0) : (b.progress || 0);
  return { index, progress };
}

/** quests: { [chainId]: { index, progress } }. Also accepts legacy singular { chainId, index, progress }. */
function mergeQuests(a, b) {
  const out = {};
  const ingest = (src) => {
    if (!isPlain(src)) return;
    // legacy singular
    if (typeof src.chainId === "string" && (src.index != null || src.progress != null)) {
      out[src.chainId] = mergeQuestEntry(out[src.chainId], { index: src.index, progress: src.progress }) || out[src.chainId];
      return;
    }
    for (const [chainId, entry] of Object.entries(src)) {
      if (!isPlain(entry)) continue;
      out[chainId] = mergeQuestEntry(out[chainId], entry) || entry;
    }
  };
  ingest(a);
  ingest(b);
  return Object.keys(out).length ? out : null;
}

/** world.gather: { [nodeId]: { available, updatedAt } } — remaining Quarry/gather stock (min on conflict = more depleted). */
function mergeWorld(a, b) {
  if (!isPlain(a)) return isPlain(b) ? structuredClone(b) : null;
  if (!isPlain(b)) return structuredClone(a);
  const out = { gather: {} };
  const ga = isPlain(a.gather) ? a.gather : {};
  const gb = isPlain(b.gather) ? b.gather : {};
  const ids = new Set([...Object.keys(ga), ...Object.keys(gb)]);
  for (const id of ids) {
    const x = ga[id], y = gb[id];
    if (!isPlain(x)) { out.gather[id] = structuredClone(y); continue; }
    if (!isPlain(y)) { out.gather[id] = structuredClone(x); continue; }
    // Remaining stock: take the LOWER available (more progress depleting the node).
    const available = (typeof x.available === "number" && typeof y.available === "number")
      ? Math.min(x.available, y.available)
      : (typeof y.available === "number" ? y.available : x.available);
    out.gather[id] = {
      available: Math.max(0, Math.floor(available ?? 0)),
      updatedAt: maxNum(x.updatedAt, y.updatedAt) ?? y.updatedAt ?? x.updatedAt ?? 0,
      // Optional future regen clock; last-write if only one side has it.
      regenAt: maxNum(x.regenAt, y.regenAt) ?? y.regenAt ?? x.regenAt ?? null,
    };
    if (out.gather[id].regenAt == null) delete out.gather[id].regenAt;
  }
  return out;
}

function mergeFtue(a, b) {
  // Last-write for step id is weak; prefer the one later in a known order when both present — client may re-apply.
  if (!isPlain(a)) return isPlain(b) ? structuredClone(b) : null;
  if (!isPlain(b)) return structuredClone(a);
  // Prefer b (incoming / server last-write) when both set; CloudSaveSync re-checks on apply.
  return { stepId: typeof b.stepId === "string" && b.stepId ? b.stepId : a.stepId };
}

/** Last-write with sensible merge for known sections. `incoming` is the PUT body; `server` is the stored row. */
export function mergeBlobs(server, incoming) {
  const s = isPlain(server) ? server : {};
  const i = isPlain(incoming) ? incoming : {};
  const out = {
    schema: SAVE_SCHEMA,
    savedAt: maxNum(s.savedAt, i.savedAt) ?? Date.now() / 1000,
    campaign: mergeCampaign(s.campaign, i.campaign),
    village: mergeVillage(s.village, i.village),
  };
  const q = mergeQuests(s.quests || s.quest, i.quests || i.quest);
  if (q) out.quests = q;
  const w = mergeWorld(s.world, i.world);
  if (w) out.world = w;
  const f = mergeFtue(s.ftue, i.ftue);
  if (f) out.ftue = f;
  // iapApplied: union of string hashes
  out.iapApplied = unionStr(s.iapApplied, i.iapApplied);
  // grantLedger: merge by purchase id; keep grant maps (identical when same purchase)
  out.grantLedger = {};
  for (const src of [s.grantLedger, i.grantLedger]) {
    if (!isPlain(src)) continue;
    for (const [pid, grants] of Object.entries(src)) {
      if (isPlain(grants)) out.grantLedger[pid] = { ...(out.grantLedger[pid] || {}), ...grants };
    }
  }
  return out;
}

/** Subtract wallet grants from a blob (refund clawback). Mutates a copy; floors at 0. */
export function clawbackBlob(blob, grants) {
  const out = isPlain(blob) ? structuredClone(blob) : { schema: SAVE_SCHEMA };
  if (!isPlain(grants)) return out;
  if (!isPlain(out.campaign)) out.campaign = {};
  if (!isPlain(out.campaign.wallet)) out.campaign.wallet = {};
  for (const [k, v] of Object.entries(grants)) {
    if (typeof v !== "number" || !Number.isFinite(v) || v <= 0) continue;
    const cur = typeof out.campaign.wallet[k] === "number" ? out.campaign.wallet[k] : 0;
    out.campaign.wallet[k] = Math.max(0, Math.floor(cur) - Math.floor(v));
  }
  out.savedAt = Date.now() / 1000;
  return out;
}

/**
 * Save service. repo: { get, put, setConsent, getConsent, clawback }.
 * ageBandOf(player) → 'under13'|'13plus'|'unknown'.
 */
export function createSave({ repo, ageBandOf, log = console }) {
  const reply = (status, body) => ({ status, body });

  async function get(player) {
    const row = await repo.get(player.id);
    const consentAt = await repo.getConsent(player.id);
    return reply(200, {
      version: row?.version ?? 0,
      blob: row?.blob ?? null,
      consentAt: consentAt || null,
      ageBand: await ageBandOf(player),
    });
  }

  async function put(player, body) {
    if (!isPlain(body)) return reply(400, { error: "bad_request" });
    const blob = body.blob;
    const pii = assertNoPii(blob);
    if (pii) return reply(400, { error: pii });
    if (blobBytes(blob) > MAX_BLOB_BYTES) return reply(413, { error: "too_large" });
    if (blob.schema != null && blob.schema !== SAVE_SCHEMA) return reply(400, { error: "bad_schema" });

    const band = await ageBandOf(player);
    let consentAt = await repo.getConsent(player.id);
    if (band === "under13") {
      if (body.consentGranted === true && !consentAt) {
        consentAt = await repo.setConsent(player.id);
      }
      if (!consentAt) return reply(403, { error: "consent_required" });
    }
    // unknown: allow cloud save (age gate is a separate workstream); under13 is the hard COPPA case.

    const baseVersion = Number.isInteger(body.baseVersion) && body.baseVersion >= 0 ? body.baseVersion : null;
    if (baseVersion == null) return reply(400, { error: "bad_request" });

    const normalized = { ...blob, schema: SAVE_SCHEMA };
    const result = await repo.put(player.id, baseVersion, normalized, mergeBlobs);
    return reply(200, {
      version: result.version,
      blob: result.blob,
      merged: result.merged,
      consentAt: consentAt || null,
    });
  }

  /** Called from IAP RTDN after revoke: claw back wallet grants on the cloud save. */
  async function onPurchaseRevoked({ playerId, purchaseId, grants }) {
    if (!playerId || !purchaseId || !isPlain(grants)) return { applied: false };
    try {
      return await repo.clawback(playerId, purchaseId, grants, clawbackBlob);
    } catch (e) {
      log.warn?.("save clawback failed", e?.message || e);
      return { applied: false, error: String(e?.message || e) };
    }
  }

  return { get, put, onPurchaseRevoked, mergeBlobs, clawbackBlob };
}
