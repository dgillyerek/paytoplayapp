// 13+ guild chat: profanity filter + moderation queue. Schema: Backend/schema/008_moderation.sql.
// The word list is the theme pack's kids/chat_filter.json, shared with the client (Survival.Domain.Kids.ChatFilter);
// keep the rules here and in ChatFilter.cs in step. The server is authoritative: it masks words, refuses links and
// contact info, and copies every masked / refused message into chat_reports (source='filter') for review.
// Queue: player reports (POST /v1/chat/report) and filter hits land in chat_reports. A moderator reads and resolves them
// over GET /v1/mod/queue and POST /v1/mod/resolve with "Authorization: Bearer <MODERATION_TOKEN>" (env). No reviewer UI.
import crypto from "node:crypto";
import filterJson from "../../../Assets/ThemePack/fantasy_kingdom_a/kids/chat_filter.json" with { type: "json" };

const WORD = /[\p{L}0-9@$]+/gu;
const LINK = /(https?:\/\/|www\.|\b[a-z0-9-]+\.(com|net|org|io|gg|ph|ca|au|nz|me|ly|co|app|xyz|tv)\b)/i;
const EMAIL = /[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}/i;
const PHONE = /(\+?\d[\d\s().-]{7,}\d)/;
const LEET = { 0: "o", 1: "i", 3: "e", 4: "a", 5: "s", 7: "t", "@": "a", $: "s" };

export const fold = (w) => [...w.toLowerCase()].map((c) => LEET[c] ?? c).join("");
export const collapse = (w) => w.replace(/(.)\1+/gu, "$1");

export function createFilter(cfg = filterJson) {
  const words = new Set();
  for (const w of cfg.words || []) { const f = fold(String(w)); if (f) { words.add(f); words.add(collapse(f)); } }
  const blockLinks = cfg.block_links !== false;
  const blockContact = cfg.block_contact_info !== false;
  /** → { text, masked, refusal } ; refusal: 'links_not_allowed' | 'personal_info' | null. */
  return function apply(body) {
    const s = typeof body === "string" ? body : "";
    if (blockLinks && LINK.test(s)) return { text: s, masked: 0, refusal: "links_not_allowed" };
    if (blockContact && (EMAIL.test(s) || PHONE.test(s))) return { text: s, masked: 0, refusal: "personal_info" };
    let masked = 0;
    const text = s.replace(WORD, (m) => { const f = fold(m); if (words.has(f) || words.has(collapse(f))) { masked++; return "*".repeat([...m].length); } return m; });
    return { text, masked, refusal: null };
  };
}
export const chatFilter = createFilter();

export const MOD_ACTIONS = ["dismiss", "delete_message", "mute_24h", "mute_7d"];
const MUTE = { mute_24h: "24 hours", mute_7d: "7 days" };

/** Constant-time bearer check; with no MODERATION_TOKEN set the moderation endpoints are off. */
export function moderatorAuthorized(req, token) {
  if (!token || token.length < 24) return false;
  const a = req.headers.get("authorization") || "";
  if (!a.startsWith("Bearer ")) return false;
  const x = Buffer.from(a.slice(7)), y = Buffer.from(token);
  return x.length === y.length && crypto.timingSafeEqual(x, y);
}

/** Routes that take the moderator token instead of a player token (wired in index.mjs before player auth). */
export function moderationRoutes({ q, err, json }) {
  return {
    "GET /v1/mod/queue": async (req) => {
      const status = new URL(req.url).searchParams.get("status") || "open";
      const { rows } = await q(`SELECT id, source, reason, message_id, reported_player_id, reporter_id, body_snapshot, created_at, status, action, reviewed_at
        FROM chat_reports WHERE status=$1 ORDER BY created_at LIMIT 100`, [status === "closed" ? "closed" : "open"]);
      return json({ reports: rows });
    },
    "POST /v1/mod/resolve": async (_req, b) => {
      const id = Number(b.id) || 0;
      const action = MOD_ACTIONS.includes(b.action) ? b.action : null;
      if (!id || !action) return err("bad_request");
      const r = (await q("SELECT id, message_id, reported_player_id FROM chat_reports WHERE id=$1", [id])).rows[0];
      if (!r) return err("not_found", 404);
      if (action === "delete_message" && r.message_id) await q("DELETE FROM alliance_chat WHERE id=$1", [r.message_id]);
      if (MUTE[action] && r.reported_player_id) await q(`UPDATE players SET chat_muted_until=now() + interval '${MUTE[action]}' WHERE id=$1`, [r.reported_player_id]);
      await q("UPDATE chat_reports SET status='closed', reviewed=true, action=$2, reviewer_note=$3, reviewed_at=now() WHERE id=$1",
        [id, action, typeof b.note === "string" ? b.note.slice(0, 500) : null]);
      return json({ ok: true, id, action });
    },
  };
}
