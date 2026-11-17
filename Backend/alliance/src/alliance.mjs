// Alliance, chat, help and gift routes. Pure rules are exported for tests; routes take the shared helpers from index.mjs.

export const RULES = { maxHelps: 10, minHelpSeconds: 60, maxHelpSeconds: 3600, helpRewardsPerDay: 30, goldPerHelp: 100,
  chatMax: 280, maxMembers: 50, giftGemsPerHelpRequestClosed: 0 };

/** alliance_members.role values. 2 and 3 are member ranks between MEMBER and OFFICER. */
export const ROLE = { MEMBER: 1, OFFICER: 4, LEADER: 5 };

/** Gems each helper gets when a help request they helped on fills up. */
export const HELP_THANKS_GEMS = 2;

/** May a member with actorRole set a member with targetRole to newRole? Officers and up promote below themselves only. */
export function canSetRole(actorRole, targetRole, newRole) {
  return Number.isInteger(newRole) && newRole >= ROLE.MEMBER && newRole < ROLE.LEADER
    && actorRole >= ROLE.OFFICER && targetRole < actorRole && newRole < actorRole;
}

/** Error code when an alliance row (with its member count n) cannot take one more member, else null. */
export function joinRefusal(a) {
  if (!a) return ["not_found", 404];
  if (!a.is_open) return ["closed", 403];
  if (a.n >= a.max_members) return ["full", 409];
  return null;
}

/** Rewarded helps from this call: `helped` new helps, `givenToday` (including them) against the daily cap. */
export function rewardedHelps(helped, givenToday, perDay = RULES.helpRewardsPerDay) {
  return Math.max(0, Math.min(helped, perDay - (givenToday - helped)));
}

/** Clamps a requested seconds-per-help into the allowed range. */
export const helpSeconds = (v) => Math.min(RULES.maxHelpSeconds, Math.max(RULES.minHelpSeconds, Math.floor(Number(v) || 0)));

export const TASK_KEY = /^[a-z0-9_.:-]{1,40}$/;

/** Runs fn(client) in one transaction on its own pooled connection. */
export async function inTransaction(pool, fn) {
  const c = await pool.connect();
  try {
    await c.query("BEGIN");
    const out = await fn(c);
    await c.query("COMMIT");
    return out;
  } catch (e) {
    await c.query("ROLLBACK").catch(() => {});
    throw e;
  } finally {
    c.release();
  }
}

export function allianceRoutes({ q, pool, err, json, limited, clean }) {
  const membership = async (pid) => (await q("SELECT alliance_id, role FROM alliance_members WHERE player_id=$1", [pid])).rows[0] || null;
  async function allianceView(aid) {
    const a = (await q("SELECT id,name,tag,banner,leader_id,is_open,max_members FROM alliances WHERE id=$1", [aid])).rows[0];
    if (!a) return null;
    a.members = (await q(`SELECT p.id, p.display_name name, p.power, m.role, (p.last_seen > now() - interval '10 minutes') online
      FROM alliance_members m JOIN players p ON p.id=m.player_id WHERE m.alliance_id=$1 ORDER BY m.role DESC, p.power DESC`, [aid])).rows;
    return a;
  }

  return {
    "GET /v1/me": async (_r, _b, p) => {
      const m = await membership(p.id);
      return json({ player: p, alliance: m ? await allianceView(m.alliance_id) : null, role: m?.role ?? 0 });
    },
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
      try {
        const id = await inTransaction(pool, async (c) => {
          const a = await c.query("INSERT INTO alliances(name,tag,banner,leader_id,is_open) VALUES($1,$2,$3,$4,$5) RETURNING id",
            [name, tag, clean(b.banner, 32) || "banner_01", p.id, b.open !== false]);
          await c.query("INSERT INTO alliance_members(player_id,alliance_id,role) VALUES($1,$2,$3)", [p.id, a.rows[0].id, ROLE.LEADER]);
          return a.rows[0].id;
        });
        return json({ alliance: await allianceView(id) }, 201);
      } catch (e) {
        if (e.code === "23505") return err("name_or_tag_taken", 409);
        throw e;
      }
    },
    "POST /v1/alliances/join": async (_r, b, p) => {
      if (await membership(p.id)) return err("already_in_alliance", 409);
      // Lock the alliance row so two joins at once cannot both see the last free seat.
      const refusal = await inTransaction(pool, async (c) => {
        const a = (await c.query("SELECT id,is_open,max_members FROM alliances WHERE id=$1 FOR UPDATE", [b.allianceId])).rows[0];
        if (a) a.n = (await c.query("SELECT count(*)::int n FROM alliance_members WHERE alliance_id=$1", [a.id])).rows[0].n;
        const no = joinRefusal(a);
        if (no) return no;
        await c.query("INSERT INTO alliance_members(player_id,alliance_id,role) VALUES($1,$2,$3)", [p.id, a.id, ROLE.MEMBER]);
        return null;
      }).catch((e) => { if (e.code === "23505") return ["already_in_alliance", 409]; throw e; });
      if (refusal) return err(...refusal);
      return json({ alliance: await allianceView(b.allianceId) });
    },
    "POST /v1/alliances/leave": async (_r, _b, p) => {
      const m = await membership(p.id); if (!m) return err("not_in_alliance", 409);
      await inTransaction(pool, async (c) => {
        await c.query("DELETE FROM alliance_members WHERE player_id=$1", [p.id]);
        const next = (await c.query("SELECT player_id FROM alliance_members WHERE alliance_id=$1 ORDER BY role DESC, joined_at LIMIT 1 FOR UPDATE",
          [m.alliance_id])).rows[0];
        if (!next) {
          await c.query("DELETE FROM alliances WHERE id=$1", [m.alliance_id]);
        } else if (m.role === ROLE.LEADER) {
          await c.query("UPDATE alliance_members SET role=$2 WHERE player_id=$1", [next.player_id, ROLE.LEADER]);
          await c.query("UPDATE alliances SET leader_id=$2 WHERE id=$1", [m.alliance_id, next.player_id]);
        }
      });
      return json({ ok: true });
    },
    "POST /v1/alliances/role": async (_r, b, p) => {
      const m = await membership(p.id); const t = await membership(b.playerId);
      if (!m || !t || t.alliance_id !== m.alliance_id || b.playerId === p.id) return err("not_allowed", 403);
      if (!canSetRole(m.role, t.role, b.role)) return err("not_allowed", 403);
      await q("UPDATE alliance_members SET role=$2 WHERE player_id=$1", [b.playerId, b.role]);
      return json({ ok: true });
    },
    "POST /v1/alliances/settings": async (_r, b, p) => {
      const m = await membership(p.id); if (!m || m.role < ROLE.OFFICER) return err("not_allowed", 403);
      await q("UPDATE alliances SET is_open=coalesce($2,is_open), banner=coalesce(nullif($3,''),banner) WHERE id=$1",
        [m.alliance_id, typeof b.open === "boolean" ? b.open : null, clean(b.banner, 32)]);
      return json({ ok: true });
    },
    "GET /v1/chat": async (r, _b, p) => {
      const m = await membership(p.id); if (!m) return err("not_in_alliance", 409);
      const after = Number(new URL(r.url).searchParams.get("after") || 0) || 0;
      const { rows } = await q(`SELECT c.id, c.player_id, p.display_name name, c.body, c.created_at FROM alliance_chat c JOIN players p ON p.id=c.player_id
        WHERE c.alliance_id=$1 AND c.id>$2 ORDER BY c.id DESC LIMIT 50`, [m.alliance_id, after]);
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
        FROM help_requests h JOIN players pl ON pl.id=h.player_id WHERE h.alliance_id=$1 AND h.open AND h.created_at > now() - interval '24 hours'
        ORDER BY h.id DESC LIMIT 50`, [m.alliance_id, p.id]);
      return json({ requests: rows });
    },
    "POST /v1/help/request": async (_r, b, p) => {
      const m = await membership(p.id); if (!m) return err("not_in_alliance", 409);
      if (await limited(p.id, "write")) return err("rate_limited", 429);
      const task = clean(b.taskKey, 40); if (!TASK_KEY.test(task)) return err("bad_task");
      const dup = (await q("SELECT id FROM help_requests WHERE player_id=$1 AND task_key=$2 AND open", [p.id, task])).rows[0];
      if (dup) return json({ requestId: dup.id });
      const { rows } = await q("INSERT INTO help_requests(alliance_id,player_id,task_key,max_helps,seconds_per_help) VALUES($1,$2,$3,$4,$5) RETURNING id",
        [m.alliance_id, p.id, task, RULES.maxHelps, helpSeconds(b.secondsPerHelp)]);
      return json({ requestId: rows[0].id }, 201);
    },
    "POST /v1/help/give": async (_r, b, p) => {
      const m = await membership(p.id); if (!m) return err("not_in_alliance", 409);
      if (await limited(p.id, "write")) return err("rate_limited", 429);
      const ids = (Array.isArray(b.requestIds) ? b.requestIds : [b.requestId]).filter(Number.isInteger).slice(0, 50);
      const filled = [];
      for (const id of ids) {
        const r = await q(`WITH ok AS (SELECT id FROM help_requests WHERE id=$1 AND alliance_id=$2 AND open AND player_id<>$3 AND helps<max_helps),
          ins AS (INSERT INTO help_given(request_id,helper_id) SELECT id,$3 FROM ok ON CONFLICT DO NOTHING RETURNING request_id)
          UPDATE help_requests h SET helps=helps+1, seconds_saved=seconds_saved+seconds_per_help, open=(helps+1<max_helps)
          FROM ins WHERE h.id=ins.request_id RETURNING h.id, h.open`, [id, m.alliance_id, p.id]);
        for (const row of r.rows) filled.push(row);
      }
      // A request this call filled thanks every helper once (the source key makes it idempotent).
      for (const f of filled.filter((x) => !x.open)) {
        await q(`INSERT INTO alliance_gifts(alliance_id,recipient_id,source,gems)
          SELECT h.alliance_id, g.helper_id, 'help_thanks:'||h.id, $2 FROM help_requests h JOIN help_given g ON g.request_id=h.id
          WHERE h.id=$1 AND NOT EXISTS (SELECT 1 FROM alliance_gifts x WHERE x.source='help_thanks:'||h.id)`, [f.id, HELP_THANKS_GEMS]);
      }
      const today = (await q("SELECT count(*)::int c FROM help_given WHERE helper_id=$1 AND created_at > date_trunc('day', now())", [p.id])).rows[0].c;
      return json({ helped: filled.length, gold: rewardedHelps(filled.length, today) * RULES.goldPerHelp });
    },
    "GET /v1/help/mine": async (_r, _b, p) => {
      const { rows } = await q(`SELECT id, task_key, helps, max_helps, seconds_saved, open FROM help_requests
        WHERE player_id=$1 AND created_at > now() - interval '24 hours' ORDER BY id DESC`, [p.id]);
      return json({ requests: rows });
    },
    "POST /v1/help/close": async (_r, b, p) => {
      await q("UPDATE help_requests SET open=false WHERE player_id=$1 AND task_key=$2", [p.id, clean(b.taskKey, 40)]);
      return json({ ok: true });
    },
    "GET /v1/gifts": async (_r, _b, p) =>
      json({ gifts: (await q("SELECT id, source, gems, created_at FROM alliance_gifts WHERE recipient_id=$1 AND NOT claimed ORDER BY id LIMIT 100", [p.id])).rows }),
    "POST /v1/gifts/claim": async (_r, _b, p) => {
      const { rows } = await q("UPDATE alliance_gifts SET claimed=true WHERE recipient_id=$1 AND NOT claimed RETURNING gems", [p.id]);
      return json({ claimed: rows.length, gems: rows.reduce((s, r) => s + r.gems, 0) });
    },
  };
}
