// PayToPlay Survival alliance API (Neon Function). DATABASE_URL is injected by Neon; nothing secret lives in git.
import pg from "pg";
import crypto from "node:crypto";
import { attachDatabasePool } from "@neon/functions";

const pool = new pg.Pool({ connectionString: process.env.DATABASE_URL, max: 5 });
attachDatabasePool(pool);
const q = (sql, args = []) => pool.query(sql, args);

export const RULES = { maxHelps: 10, minHelpSeconds: 60, maxHelpSeconds: 3600, helpRewardsPerDay: 30, goldPerHelp: 100,
  chatMax: 280, maxMembers: 50, giftGemsPerHelpRequestClosed: 0 };
const LIMITS = { signin: [10, 3600], chat: [20, 60], write: [60, 60], read: [240, 60] };

const sha = (s) => crypto.createHash("sha256").update(s).digest("hex");
const json = (body, status = 200) => new Response(JSON.stringify(body), { status, headers: { "content-type": "application/json", "access-control-allow-origin": "*", "access-control-allow-headers": "authorization,content-type" } });
const err = (code, status = 400) => json({ error: code }, status);
const clean = (s, max) => (typeof s === "string" ? s.replace(/[\u0000-\u001f]/g, "").trim().slice(0, max) : "");

async function limited(playerKey, bucket) {
  const [n, win] = LIMITS[bucket];
  await q("DELETE FROM rate_hits WHERE at < now() - interval '1 day'").catch(() => {});
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
const membership = async (pid) => (await q("SELECT alliance_id, role FROM alliance_members WHERE player_id=$1", [pid])).rows[0] || null;
async function allianceView(aid) {
  const a = (await q("SELECT id,name,tag,banner,leader_id,is_open,max_members FROM alliances WHERE id=$1", [aid])).rows[0];
  if (!a) return null;
  a.members = (await q("SELECT p.id, p.display_name name, p.power, m.role, (p.last_seen > now() - interval '10 minutes') online FROM alliance_members m JOIN players p ON p.id=m.player_id WHERE m.alliance_id=$1 ORDER BY m.role DESC, p.power DESC", [aid])).rows;
  return a;
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
      [dh, sha(token), name, crypto.randomInt(0, 2000), crypto.randomInt(0, 2000)]);
    return json({ token, player: rows[0] });
  },
  "POST /v1/profile": async (_r, b, p) => {
    const name = clean(b.name, 20); const power = Number.isInteger(b.power) && b.power >= 0 && b.power < 1e9 ? b.power : null;
    await q("UPDATE players SET display_name=coalesce(nullif($2,''),display_name), power=coalesce($3,power) WHERE id=$1", [p.id, name, power]);
    return json({ ok: true });
  },
  "GET /v1/me": async (_r, _b, p) => { const m = await membership(p.id); return json({ player: p, alliance: m ? await allianceView(m.alliance_id) : null, role: m?.role ?? 0 }); },
  "GET /v1/alliances": async (r) => {
    const s = clean(new URL(r.url).searchParams.get("q") || "", 30);
    const { rows } = await q(`SELECT a.id,a.name,a.tag,a.banner,a.is_open,a.max_members,count(m.*)::int members, coalesce(sum(p.power),0)::bigint power
      FROM alliances a LEFT JOIN alliance_members m ON m.alliance_id=a.id LEFT JOIN players p ON p.id=m.player_id
      WHERE ($1='' OR a.name ILIKE '%'||$1||'%' OR a.tag ILIKE $1) GROUP BY a.id ORDER BY members DESC, a.id LIMIT 30`, [s]);
    return json({ alliances: rows });
  },
  "POST /v1/alliances": async (_r, b, p) => {
    const name = clean(b.name, 24), tag = clean(b.tag, 4).toUpperCase();
    if (name.length < 3 || !/^[A-Z0-9]{2,4}$/.test(tag)) return err("bad_name_or_tag");
    if (await membership(p.id)) return err("already_in_alliance", 409);
    const c = await pool.connect();
    try {
      await c.query("BEGIN");
      const a = await c.query("INSERT INTO alliances(name,tag,banner,leader_id,is_open) VALUES($1,$2,$3,$4,$5) RETURNING id", [name, tag, clean(b.banner, 32) || "banner_01", p.id, b.open !== false]);
      await c.query("INSERT INTO alliance_members(player_id,alliance_id,role) VALUES($1,$2,5)", [p.id, a.rows[0].id]);
      await c.query("COMMIT");
      return json({ alliance: await allianceView(a.rows[0].id) }, 201);
    } catch (e) { await c.query("ROLLBACK"); if (e.code === "23505") return err("name_or_tag_taken", 409); throw e; } finally { c.release(); }
  },
  "POST /v1/alliances/join": async (_r, b, p) => {
    if (await membership(p.id)) return err("already_in_alliance", 409);
    const a = (await q("SELECT a.id,a.is_open,a.max_members,(SELECT count(*) FROM alliance_members WHERE alliance_id=a.id)::int n FROM alliances a WHERE id=$1", [b.allianceId])).rows[0];
    if (!a) return err("not_found", 404);
    if (!a.is_open) return err("closed", 403);
    if (a.n >= a.max_members) return err("full", 409);
    await q("INSERT INTO alliance_members(player_id,alliance_id,role) VALUES($1,$2,1)", [p.id, a.id]);
    return json({ alliance: await allianceView(a.id) });
  },
  "POST /v1/alliances/leave": async (_r, _b, p) => {
    const m = await membership(p.id); if (!m) return err("not_in_alliance", 409);
    await q("DELETE FROM alliance_members WHERE player_id=$1", [p.id]);
    const next = (await q("SELECT player_id FROM alliance_members WHERE alliance_id=$1 ORDER BY role DESC, joined_at LIMIT 1", [m.alliance_id])).rows[0];
    if (!next) await q("DELETE FROM alliances WHERE id=$1", [m.alliance_id]);
    else if (m.role === 5) { await q("UPDATE alliance_members SET role=5 WHERE player_id=$1", [next.player_id]); await q("UPDATE alliances SET leader_id=$2 WHERE id=$1", [m.alliance_id, next.player_id]); }
    return json({ ok: true });
  },
  "POST /v1/alliances/role": async (_r, b, p) => {
    const m = await membership(p.id); const t = await membership(b.playerId);
    const role = b.role;
    if (!m || !t || t.alliance_id !== m.alliance_id || b.playerId === p.id) return err("not_allowed", 403);
    if (!Number.isInteger(role) || role < 1 || role > 4 || m.role < 4 || t.role >= m.role || role >= m.role) return err("not_allowed", 403);
    await q("UPDATE alliance_members SET role=$2 WHERE player_id=$1", [b.playerId, role]);
    return json({ ok: true });
  },
  "POST /v1/alliances/settings": async (_r, b, p) => {
    const m = await membership(p.id); if (!m || m.role < 4) return err("not_allowed", 403);
    await q("UPDATE alliances SET is_open=coalesce($2,is_open), banner=coalesce(nullif($3,''),banner) WHERE id=$1", [m.alliance_id, typeof b.open === "boolean" ? b.open : null, clean(b.banner, 32)]);
    return json({ ok: true });
  },
  "GET /v1/chat": async (r, _b, p) => {
    const m = await membership(p.id); if (!m) return err("not_in_alliance", 409);
    const after = Number(new URL(r.url).searchParams.get("after") || 0) || 0;
    const { rows } = await q("SELECT c.id, c.player_id, p.display_name name, c.body, c.created_at FROM alliance_chat c JOIN players p ON p.id=c.player_id WHERE c.alliance_id=$1 AND c.id>$2 ORDER BY c.id DESC LIMIT 50", [m.alliance_id, after]);
    return json({ messages: rows.reverse() });
  },
  "POST /v1/chat": async (_r, b, p) => {
    const m = await membership(p.id); if (!m) return err("not_in_alliance", 409);
    if (await limited(p.id, "chat")) return err("rate_limited", 429);
    const body = clean(b.body, RULES.chatMax); if (!body) return err("empty");
    const { rows } = await q("INSERT INTO alliance_chat(alliance_id,player_id,body) VALUES($1,$2,$3) RETURNING id, created_at", [m.alliance_id, p.id, body]);
    return json({ message: rows[0] }, 201);
  },
  "GET /v1/help": async (_r, _b, p) => {
    const m = await membership(p.id); if (!m) return err("not_in_alliance", 409);
    const { rows } = await q(`SELECT h.id, h.player_id, pl.display_name name, h.task_key, h.helps, h.max_helps, h.seconds_per_help,
      EXISTS(SELECT 1 FROM help_given g WHERE g.request_id=h.id AND g.helper_id=$2) helped_by_me
      FROM help_requests h JOIN players pl ON pl.id=h.player_id WHERE h.alliance_id=$1 AND h.open AND h.created_at > now() - interval '24 hours' ORDER BY h.id DESC LIMIT 50`, [m.alliance_id, p.id]);
    return json({ requests: rows });
  },
  "POST /v1/help/request": async (_r, b, p) => {
    const m = await membership(p.id); if (!m) return err("not_in_alliance", 409);
    if (await limited(p.id, "write")) return err("rate_limited", 429);
    const task = clean(b.taskKey, 40); if (!/^[a-z0-9_.:-]{1,40}$/.test(task)) return err("bad_task");
    const sec = Math.min(RULES.maxHelpSeconds, Math.max(RULES.minHelpSeconds, Math.floor(Number(b.secondsPerHelp) || 0)));
    const dup = (await q("SELECT id FROM help_requests WHERE player_id=$1 AND task_key=$2 AND open", [p.id, task])).rows[0];
    if (dup) return json({ requestId: dup.id });
    const { rows } = await q("INSERT INTO help_requests(alliance_id,player_id,task_key,max_helps,seconds_per_help) VALUES($1,$2,$3,$4,$5) RETURNING id", [m.alliance_id, p.id, task, RULES.maxHelps, sec]);
    return json({ requestId: rows[0].id }, 201);
  },
  "POST /v1/help/give": async (_r, b, p) => {
    const m = await membership(p.id); if (!m) return err("not_in_alliance", 409);
    if (await limited(p.id, "write")) return err("rate_limited", 429);
    const ids = (Array.isArray(b.requestIds) ? b.requestIds : [b.requestId]).filter(Number.isInteger).slice(0, 50);
    let helped = 0;
    for (const id of ids) {
      const r = await q(`WITH ok AS (SELECT id FROM help_requests WHERE id=$1 AND alliance_id=$2 AND open AND player_id<>$3 AND helps<max_helps),
        ins AS (INSERT INTO help_given(request_id,helper_id) SELECT id,$3 FROM ok ON CONFLICT DO NOTHING RETURNING request_id)
        UPDATE help_requests h SET helps=helps+1, seconds_saved=seconds_saved+seconds_per_help, open=(helps+1<max_helps) FROM ins WHERE h.id=ins.request_id RETURNING h.id`, [id, m.alliance_id, p.id]);
      helped += r.rowCount;
    }
    const today = (await q("SELECT count(*)::int c FROM help_given WHERE helper_id=$1 AND created_at > date_trunc('day', now())", [p.id])).rows[0].c;
    const rewarded = Math.max(0, Math.min(helped, RULES.helpRewardsPerDay - (today - helped)));
    return json({ helped, gold: rewarded * RULES.goldPerHelp });
  },
  "GET /v1/help/mine": async (_r, _b, p) => {
    const { rows } = await q("SELECT id, task_key, helps, max_helps, seconds_saved, open FROM help_requests WHERE player_id=$1 AND created_at > now() - interval '24 hours' ORDER BY id DESC", [p.id]);
    return json({ requests: rows });
  },
  "POST /v1/help/close": async (_r, b, p) => {
    await q("UPDATE help_requests SET open=false WHERE player_id=$1 AND task_key=$2", [p.id, clean(b.taskKey, 40)]);
    return json({ ok: true });
  },
  "GET /v1/gifts": async (_r, _b, p) => json({ gifts: (await q("SELECT id, source, gems, created_at FROM alliance_gifts WHERE recipient_id=$1 AND NOT claimed ORDER BY id LIMIT 100", [p.id])).rows }),
  "POST /v1/gifts/claim": async (_r, _b, p) => {
    const { rows } = await q("UPDATE alliance_gifts SET claimed=true WHERE recipient_id=$1 AND NOT claimed RETURNING gems", [p.id]);
    return json({ claimed: rows.length, gems: rows.reduce((s, r) => s + r.gems, 0) });
  },
};
// Server-side gift sources: when a member's help request fills up, every helper gets a small thank-you gift.
const giftOnFill = `INSERT INTO alliance_gifts(alliance_id,recipient_id,source,gems)
  SELECT h.alliance_id, g.helper_id, 'help_thanks', 2 FROM help_requests h JOIN help_given g ON g.request_id=h.id WHERE h.id=$1`;

export default {
  async fetch(req) {
    if (req.method === "OPTIONS") return json({});
    const url = new URL(req.url);
    const key = req.method + " " + url.pathname.replace(/\/$/, "");
    if (key === "GET /v1/health") return json({ ok: true, rules: RULES });
    const h = routes[key]; if (!h) return err("not_found", 404);
    try {
      let body = {};
      if (req.method === "POST") { const t = await req.text(); if (t.length > 4096) return err("too_large", 413); body = t ? JSON.parse(t) : {}; }
      if (key === "POST /v1/signin") return await h(req, body);
      const p = await me(req); if (!p) return err("unauthorized", 401);
      if (req.method === "GET" && await limited(p.id, "read")) return err("rate_limited", 429);
      const res = await h(req, body, p);
      if (key === "POST /v1/help/give" && res.status === 200) {
        // gifts for any request this call just filled
        const full = await q("SELECT h.id FROM help_requests h WHERE NOT h.open AND h.helps>=h.max_helps AND NOT EXISTS (SELECT 1 FROM alliance_gifts x WHERE x.source='help_thanks:'||h.id) AND h.id = ANY($1::bigint[])", [Array.isArray(body.requestIds) ? body.requestIds : [body.requestId]]);
        for (const f of full.rows) { await q(giftOnFill.replace("'help_thanks'", "'help_thanks:'||h.id"), [f.id]); }
      }
      return res;
    } catch (e) { console.error(e); return err(e instanceof SyntaxError ? "bad_json" : "server_error", e instanceof SyntaxError ? 400 : 500); }
  },
};
