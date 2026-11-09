// Child safety routes and guards (COPPA / Google Play Families, mixed audience). Schema: Backend/schema/005_child_safety.sql.
// Server-side floors, whatever the client says:
//  - players whose age_band is not '13plus' (under13 or unknown) are "restricted": chat is 'off' or 'presets' only,
//    no typed display names (a generated one is used), no founding alliances with a typed name.
//  - an age_band of 'under13' cannot be changed back to '13plus' by the client (account deletion is the reset).
//  - presets travel as [[p:<id>]] only, so no free text, links or personal info can be sent by a restricted player.
export const PRESET = /^\[\[p:[a-z0-9_]{1,32}\]\]$/;
export const SAFE_NAME = /^[A-Z][a-z]+ [A-Z][a-z]+ [0-9]{3}$/;
export const CHAT_MODES = ["off", "presets", "free"];
export const REPORT_REASONS = ["mean", "personal_info", "spam", "other"];

export const isRestricted = (ageBand) => ageBand !== "13plus";

/** The chat mode in force: restricted players never get 'free', and default to 'off'. */
export function effectiveChatMode(ageBand, stored) {
  if (!isRestricted(ageBand)) return CHAT_MODES.includes(stored) ? stored : "free";
  return stored === "presets" ? "presets" : "off";
}

/** Error code when this body may not be sent in this mode, else null. */
export function chatRefusal(mode, body) {
  if (mode === "off") return "chat_off";
  if (mode === "presets" && !PRESET.test(body)) return "presets_only";
  return null;
}

/** Rows a player may see: blocked senders dropped; in presets mode only preset bodies. */
export function visibleChat(mode, rows, blocked) {
  if (mode === "off") return [];
  return rows.filter((r) => !blocked.has(String(r.player_id)) && (mode === "free" || PRESET.test(r.body)));
}

/** age_band change rule: under13 is sticky. */
export const ageBandChangeAllowed = (from, to) => !(from === "under13" && to !== "under13");

export function safetyRoutes({ q, err, json, clean, limited, routes }) {
  const who = async (pid) => (await q("SELECT age_band, chat_mode FROM players WHERE id=$1", [pid])).rows[0] || { age_band: "unknown", chat_mode: null };
  const mode = async (pid) => { const w = await who(pid); return effectiveChatMode(w.age_band, w.chat_mode); };
  const blockedBy = async (pid) => new Set((await q("SELECT blocked_id FROM chat_blocks WHERE blocker_id=$1", [pid])).rows.map((r) => String(r.blocked_id)));
  const membership = async (pid) => (await q("SELECT alliance_id FROM alliance_members WHERE player_id=$1", [pid])).rows[0] || null;

  const postChat = routes["POST /v1/chat"];
  const postAlliance = routes["POST /v1/alliances"];
  const postProfile = routes["POST /v1/profile"];
  const postAgeBand = routes["POST /v1/account/age_band"];

  const wrapped = {
    "GET /v1/chat": async (r, _b, p) => {
      const m = await membership(p.id); if (!m) return err("not_in_alliance", 409);
      const cm = await mode(p.id);
      if (cm === "off") return json({ messages: [], chatMode: cm });
      const after = Number(new URL(r.url).searchParams.get("after") || 0) || 0;
      const { rows } = await q("SELECT c.id, c.player_id, p.display_name name, c.body, c.created_at FROM alliance_chat c JOIN players p ON p.id=c.player_id WHERE c.alliance_id=$1 AND c.id>$2 ORDER BY c.id DESC LIMIT 50", [m.alliance_id, after]);
      const shown = visibleChat(cm, rows.reverse(), await blockedBy(p.id));
      // Advance the client's cursor past hidden rows too, so it does not re-ask for them forever.
      const last = rows.length ? rows[rows.length - 1].id : null;
      return json({ messages: shown, chatMode: cm, lastId: last });
    },
    "POST /v1/chat": async (r, b, p) => {
      const refusal = chatRefusal(await mode(p.id), clean(b.body, 280));
      if (refusal) return err(refusal, 403);
      return postChat(r, b, p);
    },
    "POST /v1/alliances": async (r, b, p) => (isRestricted((await who(p.id)).age_band) ? err("restricted_account", 403) : postAlliance(r, b, p)),
    "POST /v1/profile": async (r, b, p) => (isRestricted((await who(p.id)).age_band) ? postProfile(r, { ...b, name: undefined }, p) : postProfile(r, b, p)),
    "POST /v1/account/child_policy": async (_r, b, p) => {
      const w = await who(p.id);
      const want = CHAT_MODES.includes(b.chat) ? b.chat : null;
      const restricted = isRestricted(w.age_band);
      const stored = restricted && want === "free" ? "presets" : want;
      await q("UPDATE players SET chat_mode=$2 WHERE id=$1", [p.id, stored]);
      if (restricted) {
        const name = typeof b.safeName === "string" && SAFE_NAME.test(b.safeName) ? b.safeName : null;
        await q("UPDATE players SET display_name=coalesce($2, 'Knight ' || lpad((id % 1000)::text, 3, '0')) WHERE id=$1", [p.id, name]);
      }
      return json({ ok: true, chatMode: effectiveChatMode(w.age_band, stored), restricted });
    },
    "POST /v1/chat/report": async (_r, b, p) => {
      if (await limited(p.id, "write")) return err("rate_limited", 429);
      const reason = REPORT_REASONS.includes(b.reason) ? b.reason : "other";
      const msg = (await q("SELECT id, player_id, body FROM alliance_chat WHERE id=$1", [Number(b.messageId) || 0])).rows[0];
      if (!msg) return err("not_found", 404);
      await q("INSERT INTO chat_reports(reporter_id, message_id, reported_player_id, body_snapshot, reason) VALUES($1,$2,$3,$4,$5)", [p.id, msg.id, msg.player_id, msg.body, reason]);
      return json({ ok: true }, 201);
    },
    "POST /v1/chat/block": async (_r, b, p) => {
      const target = Number(b.playerId) || 0;
      if (!target || target === Number(p.id)) return err("bad_player");
      await q("INSERT INTO chat_blocks(blocker_id, blocked_id) VALUES($1,$2) ON CONFLICT DO NOTHING", [p.id, target]);
      return json({ ok: true });
    },
    "POST /v1/chat/unblock": async (_r, b, p) => {
      await q("DELETE FROM chat_blocks WHERE blocker_id=$1 AND blocked_id=$2", [p.id, Number(b.playerId) || 0]);
      return json({ ok: true });
    },
    "GET /v1/chat/blocks": async (_r, _b, p) => json({ blocked: (await q("SELECT blocked_id player_id FROM chat_blocks WHERE blocker_id=$1", [p.id])).rows }),
  };
  if (postAgeBand) {
    wrapped["POST /v1/account/age_band"] = async (r, b, p) => (ageBandChangeAllowed((await who(p.id)).age_band, b.ageBand) ? postAgeBand(r, b, p) : err("age_band_locked", 403));
  }
  return wrapped;
}
