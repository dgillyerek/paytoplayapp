// Shared world map: castles, resource nodes, PvE camps, marches, PvP and battle reports.
// No tick server: settle() advances every due march whenever anyone reads or writes the world.
import crypto from "node:crypto";

export const WORLD = {
  size: 1000, sight: 15, marchSight: 6, territory: 6, maxView: 25, maxRange: 60,
  secPerTile: 4, minMarchSec: 20, maxMarches: 3, newShieldHours: 24, defeatShieldHours: 4,
  gatherPerSecPer20Troops: 1, loadPerTroop: 10, nodeAmountPerLevel: 10000, nodeRespawnMin: 30, campRespawnMin: 15,
  campStrengthPerLevel: 600, defenderLossCap: 0.10, attackerLossCap: 0.25, gatherLossCap: 0.20,
  // PvP loot: winners take this share of what sits above the protected amount, limited by what the survivors can carry.
  lootShare: 0.20, protectedStock: 2000, lootKinds: ["food", "wood", "stone", "gold"],
  // Server-owned troops: the cap comes from power, wounded troops heal back toward it over time.
  troopCapMin: 100, troopsPerPower: 0.1, healPerHour: 0.10, seedMax: 100000,
};

// ---- pure rules (unit-tested in test/world.test.mjs) ----
export const dist = (ax, ay, bx, by) => Math.hypot(ax - bx, ay - by);
export const travelSec = (d) => Math.max(WORLD.minMarchSec, Math.ceil(d * WORLD.secPerTile));
export function roll(seed) { // deterministic 0.9..1.1 from a seed string
  const h = crypto.createHash("sha256").update(String(seed)).digest();
  return 0.9 + 0.2 * (h.readUInt32BE(0) / 0xffffffff);
}
export const strength = (troops, hero) => troops * (1 + Math.max(0, hero) / 1000);
/** Resolve one fight. Same inputs, same result. lossCap caps what the loser can lose (protects defenders/free players). */
export function battle(a, d, seed, lossCap) {
  const sa = strength(a.troops, a.hero) * roll(seed + ":a"), sd = strength(d.troops, d.hero) * roll(seed + ":d");
  const attackerWon = sa > sd;
  const ratio = Math.min(sa, sd) / Math.max(sa, sd, 1);
  const loserFrac = Math.min(lossCap, 0.3 * ratio + 0.05), winnerFrac = Math.min(0.15, 0.15 * ratio);
  const aLoss = Math.round(a.troops * (attackerWon ? winnerFrac : Math.min(loserFrac, WORLD.attackerLossCap)));
  const dLoss = Math.round(d.troops * (attackerWon ? Math.min(loserFrac, lossCap) : winnerFrac));
  return { attackerWon, aLoss, dLoss };
}
export function positionAt(m, t) { // where a march is at time t (ms)
  const lerp = (fx, fy, tx, ty, t0, t1) => { const k = t1 <= t0 ? 1 : Math.min(1, Math.max(0, (t - t0) / (t1 - t0))); return [fx + (tx - fx) * k, fy + (ty - fy) * k]; };
  if (m.state === "marching") return lerp(m.from_x, m.from_y, m.to_x, m.to_y, +m.start_at, +m.arrive_at);
  if (m.state === "gathering") return [m.to_x, m.to_y];
  if (m.state === "returning") { const [sx, sy] = m.carried?.from ?? [m.to_x, m.to_y]; return lerp(sx, sy, m.from_x, m.from_y, +m.return_start, +m.return_at); }
  return [m.from_x, m.from_y];
}
export const allianceColor = (aid) => Number(BigInt(aid) % 8n);
export const gatherRate = (troops, level) => (troops / 20) * WORLD.gatherPerSecPer20Troops * (1 + 0.25 * (level - 1));

/** Real PvP loot from the defender's stored resources: a capped share of the unprotected stock, limited by carry room. */
export function lootFrom(stock, carry) {
  const want = {}; let total = 0;
  for (const k of WORLD.lootKinds) {
    const have = Math.max(0, Math.floor(Number(stock?.[k]) || 0));
    const v = Math.floor(Math.max(0, have - WORLD.protectedStock) * WORLD.lootShare);
    if (v > 0) { want[k] = v; total += v; }
  }
  const room = Math.max(0, Math.floor(carry));
  if (total <= room) return want;
  const out = {}; const k2 = room / total;
  for (const [k, v] of Object.entries(want)) { const t = Math.floor(v * k2); if (t > 0) out[k] = t; }
  return out;
}
/** Stock minus what was taken (never below zero). */
export function minusStock(stock, take) {
  const out = { ...(stock || {}) };
  for (const [k, v] of Object.entries(take || {})) out[k] = Math.max(0, (Math.floor(Number(out[k]) || 0)) - v);
  return out;
}
export const troopCap = (power) => Math.max(WORLD.troopCapMin, Math.floor(Math.max(0, power) * WORLD.troopsPerPower));
/** Troops at home after healing for ms milliseconds. Home + out on marches never heals past the cap. */
export function healTroops(home, out, cap, ms) {
  const room = cap - out - home;
  if (room <= 0) return home;
  return home + Math.min(room, Math.floor(cap * WORLD.healPerHour * Math.max(0, ms) / 3600000));
}
/** Is a send valid against the server's own count (the client's number is never trusted)? */
export function canSend(troops, home) { return Number.isInteger(troops) && troops >= 1 && troops <= home; }

// ---- settlement ----
const sec = (t, s) => new Date(+t + s * 1000);
async function report(c, r) {
  await c.query(`INSERT INTO battle_reports(player_id,march_id,kind,opponent_id,x,y,my_troops,my_loss,their_troops,their_loss,loot)
    VALUES($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11)`, [r.player, r.march ?? null, r.kind, r.opponent ?? null, r.x, r.y, r.my ?? 0, r.myLoss ?? 0, r.their ?? 0, r.theirLoss ?? 0, JSON.stringify(r.loot ?? {})]);
}
async function goHome(c, m, t, loss = 0, from = null) {
  const [fx, fy] = from ?? [m.to_x, m.to_y];
  const carried = { ...m.carried, from: [fx, fy] };
  await c.query("UPDATE marches SET state='returning', troops=greatest(0,troops-$2), return_start=$3, return_at=$4, carried=$5 WHERE id=$1",
    [m.id, loss, t, sec(t, travelSec(dist(fx, fy, m.from_x, m.from_y))), JSON.stringify(carried)]);
}
async function allianceOf(c, pid) { return (await c.query("SELECT alliance_id FROM alliance_members WHERE player_id=$1", [pid])).rows[0]?.alliance_id ?? null; }
function gatheredSoFar(m, t) {
  const g = m.carried; if (!g?.rate) return 0;
  return Math.min(g.take, Math.floor(g.rate * Math.max(0, (+t - +m.arrive_at) / 1000)));
}
async function stopGathering(c, m, t, loss = 0) {
  const got = gatheredSoFar(m, t);
  const n = (await c.query("UPDATE world_nodes SET amount=greatest(0,amount-$2), occupier_march_id=NULL WHERE id=$1 RETURNING amount", [m.node_id, got])).rows[0];
  if (n && n.amount <= 0) await c.query("UPDATE world_nodes SET respawn_at=$2 WHERE id=$1", [m.node_id, sec(t, WORLD.nodeRespawnMin * 60)]);
  m.carried = { ...m.carried, got };
  await goHome(c, m, t, loss);
}
async function occupy(c, m, node, t) {
  const rate = gatherRate(m.troops, node.level), take = Math.min(node.amount, m.troops * WORLD.loadPerTroop);
  const carried = { kind: node.kind, rate, take };
  await c.query("UPDATE marches SET state='gathering', gather_until=$2, carried=$3 WHERE id=$1", [m.id, sec(t, take / Math.max(rate, 0.001)), JSON.stringify(carried)]);
  await c.query("UPDATE world_nodes SET occupier_march_id=$2 WHERE id=$1", [node.id, m.id]);
}
async function arrive(c, m, t) {
  const at = { x: m.to_x, y: m.to_y };
  if (m.kind === "gather") {
    const node = (await c.query("SELECT * FROM world_nodes WHERE id=$1 FOR UPDATE", [m.node_id])).rows[0];
    if (!node || node.kind === "camp" || (node.respawn_at && node.respawn_at > t) || node.amount <= 0) { await report(c, { player: m.player_id, march: m.id, kind: "bounced", ...at, my: m.troops }); return goHome(c, m, t); }
    const occ = node.occupier_march_id && (await c.query("SELECT * FROM marches WHERE id=$1 AND state='gathering'", [node.occupier_march_id])).rows[0];
    if (!occ) return occupy(c, m, node, t);
    const [a1, a2] = [await allianceOf(c, m.player_id), await allianceOf(c, occ.player_id)];
    if (occ.player_id === m.player_id || (a1 && a1 === a2)) { await report(c, { player: m.player_id, march: m.id, kind: "gather_contested", opponent: occ.player_id, ...at, my: m.troops }); return goHome(c, m, t); }
    const b = battle({ troops: m.troops, hero: m.hero_power }, { troops: occ.troops, hero: occ.hero_power }, `g${m.id}:${occ.id}`, WORLD.gatherLossCap);
    await report(c, { player: m.player_id, march: m.id, kind: b.attackerWon ? "attack_won" : "attack_lost", opponent: occ.player_id, ...at, my: m.troops, myLoss: b.aLoss, their: occ.troops, theirLoss: b.dLoss });
    await report(c, { player: occ.player_id, march: occ.id, kind: b.attackerWon ? "defend_lost" : "defend_won", opponent: m.player_id, ...at, my: occ.troops, myLoss: b.dLoss, their: m.troops, theirLoss: b.aLoss });
    if (!b.attackerWon) { await c.query("UPDATE marches SET troops=greatest(0,troops-$2) WHERE id=$1", [occ.id, b.dLoss]); return goHome(c, m, t, b.aLoss); }
    await stopGathering(c, occ, t, b.dLoss);
    m.troops -= b.aLoss; await c.query("UPDATE marches SET troops=$2 WHERE id=$1", [m.id, m.troops]);
    const fresh = (await c.query("SELECT * FROM world_nodes WHERE id=$1", [node.id])).rows[0];
    return fresh.amount > 0 ? occupy(c, m, fresh, t) : goHome(c, m, t);
  }
  if (m.kind === "camp") {
    const node = (await c.query("SELECT * FROM world_nodes WHERE id=$1 FOR UPDATE", [m.node_id])).rows[0];
    if (!node || (node.respawn_at && node.respawn_at > t)) { await report(c, { player: m.player_id, march: m.id, kind: "bounced", ...at, my: m.troops }); return goHome(c, m, t); }
    const campTroops = WORLD.campStrengthPerLevel * node.level;
    const b = battle({ troops: m.troops, hero: m.hero_power }, { troops: campTroops, hero: 0 }, `c${m.id}`, 1);
    const loot = b.attackerWon ? { gold: 200 * node.level, heroXp: 50 * node.level } : {};
    if (b.attackerWon) await c.query("UPDATE world_nodes SET respawn_at=$2 WHERE id=$1", [node.id, sec(t, WORLD.campRespawnMin * 60)]);
    await report(c, { player: m.player_id, march: m.id, kind: b.attackerWon ? "camp_won" : "camp_lost", ...at, my: m.troops, myLoss: b.aLoss, their: campTroops, theirLoss: b.dLoss, loot });
    return goHome(c, m, t, b.aLoss);
  }
  // attack a castle
  const d = (await c.query("SELECT id, troops, hero_power, power, shield_until, castle_x, castle_y, stock FROM players WHERE id=$1 FOR UPDATE", [m.target_player_id])).rows[0];
  if (!d || d.castle_x !== m.to_x || d.castle_y !== m.to_y) { await report(c, { player: m.player_id, march: m.id, kind: "bounced", ...at, my: m.troops }); return goHome(c, m, t); }
  if (d.shield_until > t) { await report(c, { player: m.player_id, march: m.id, kind: "shielded", opponent: d.id, ...at, my: m.troops }); return goHome(c, m, t); }
  const b = battle({ troops: m.troops, hero: m.hero_power }, { troops: d.troops, hero: d.hero_power }, `a${m.id}`, WORLD.defenderLossCap);
  // Real loot: taken out of the defender's stored stock in this same transaction (the row is locked above).
  const loot = b.attackerWon ? lootFrom(d.stock, Math.max(0, m.troops - b.aLoss) * WORLD.loadPerTroop) : {};
  const lost = Object.fromEntries(Object.entries(loot).map(([k, v]) => [k, -v]));
  if (Object.keys(loot).length) await c.query("UPDATE players SET stock=$2 WHERE id=$1", [d.id, JSON.stringify(minusStock(d.stock, loot))]);
  await report(c, { player: m.player_id, march: m.id, kind: b.attackerWon ? "attack_won" : "attack_lost", opponent: d.id, ...at, my: m.troops, myLoss: b.aLoss, their: d.troops, theirLoss: b.dLoss, loot });
  await report(c, { player: d.id, march: m.id, kind: b.attackerWon ? "defend_lost" : "defend_won", opponent: m.player_id, ...at, my: d.troops, myLoss: b.dLoss, their: m.troops, theirLoss: b.aLoss, loot: lost });
  await c.query("UPDATE players SET troops=greatest(0,troops-$2), shield_until=CASE WHEN $3 THEN greatest(shield_until,$4) ELSE shield_until END WHERE id=$1",
    [d.id, b.dLoss, b.attackerWon, sec(t, WORLD.defeatShieldHours * 3600)]);
  return goHome(c, m, t, b.aLoss);
}
export async function settle(pool, now = new Date()) {
  const c = await pool.connect();
  try {
    await c.query("BEGIN");
    await c.query("SELECT pg_advisory_xact_lock(4207)");
    await c.query("UPDATE world_nodes SET respawn_at=NULL, amount=CASE WHEN kind='camp' THEN 0 ELSE $2::int*level END WHERE respawn_at IS NOT NULL AND respawn_at <= $1", [now, WORLD.nodeAmountPerLevel]);
    for (let i = 0; i < 500; i++) {
      const m = (await c.query(`SELECT *, CASE state WHEN 'marching' THEN arrive_at WHEN 'gathering' THEN gather_until ELSE return_at END due
        FROM marches WHERE state<>'done' AND CASE state WHEN 'marching' THEN arrive_at WHEN 'gathering' THEN gather_until ELSE return_at END <= $1
        ORDER BY due, id LIMIT 1 FOR UPDATE`, [now])).rows[0];
      if (!m) break;
      const t = m.due;
      if (m.state === "marching") await arrive(c, m, t);
      else if (m.state === "gathering") await stopGathering(c, m, t);
      else {
        await c.query("UPDATE marches SET state='done' WHERE id=$1", [m.id]);
        // survivors walk back into the castle (old marches sent before troops left home are skipped)
        if (m.left_home && m.troops > 0) await c.query("UPDATE players SET troops=troops+$2 WHERE id=$1", [m.player_id, m.troops]);
        if (m.kind === "gather" && m.carried?.got > 0)
          await report(c, { player: m.player_id, march: m.id, kind: "gather_done", x: m.to_x, y: m.to_y, my: m.troops, loot: { [m.carried.kind]: m.carried.got } });
      }
    }
    await c.query("COMMIT");
  } catch (e) { await c.query("ROLLBACK"); throw e; } finally { c.release(); }
}

// ---- placement ----
export async function ensurePlaced(q, pid) {
  const p = (await q("SELECT placed FROM players WHERE id=$1", [pid])).rows[0];
  if (!p || p.placed) return;
  const anchor = (await q("SELECT castle_x x, castle_y y FROM players WHERE placed AND last_seen > now() - interval '7 days' ORDER BY random() LIMIT 1")).rows[0] ?? { x: 500, y: 500 };
  const S = WORLD.size;
  let spot = null;
  for (let i = 0; i < 40 && !spot; i++) {
    const a = Math.random() * Math.PI * 2, r = 8 + Math.random() * 6;
    const x = Math.min(S - 1, Math.max(0, Math.round(anchor.x + Math.cos(a) * r))), y = Math.min(S - 1, Math.max(0, Math.round(anchor.y + Math.sin(a) * r)));
    const near = (await q("SELECT 1 FROM players WHERE placed AND abs(castle_x-$1)<=3 AND abs(castle_y-$2)<=3 UNION ALL SELECT 1 FROM world_nodes WHERE x=$1 AND y=$2 LIMIT 1", [x, y])).rowCount;
    if (!near) spot = { x, y };
  }
  spot ??= { x: crypto.randomInt(0, S), y: crypto.randomInt(0, S) };
  await q("UPDATE players SET castle_x=$2, castle_y=$3, placed=true, shield_until=now()+make_interval(hours=>$4) WHERE id=$1", [pid, spot.x, spot.y, WORLD.newShieldHours]);
  const kinds = ["food", "wood", "stone", "food", "camp", "camp"];
  for (const kind of kinds) {
    const a = Math.random() * Math.PI * 2, r = 3 + Math.random() * 7, lvl = 1 + crypto.randomInt(0, 2);
    const x = Math.min(S - 1, Math.max(0, Math.round(spot.x + Math.cos(a) * r))), y = Math.min(S - 1, Math.max(0, Math.round(spot.y + Math.sin(a) * r)));
    const castle = (await q("SELECT 1 FROM players WHERE placed AND castle_x=$1 AND castle_y=$2", [x, y])).rowCount;
    if (!castle) await q("INSERT INTO world_nodes(x,y,kind,level,amount) VALUES($1,$2,$3,$4,$5) ON CONFLICT DO NOTHING", [x, y, kind, lvl, kind === "camp" ? 0 : WORLD.nodeAmountPerLevel * lvl]);
  }
}

// ---- views ----
const marchView = (m, t) => { const [x, y] = positionAt(m, t); return { id: m.id, playerId: m.player_id, kind: m.kind, state: m.state, x: +x.toFixed(2), y: +y.toFixed(2),
  fromX: m.from_x, fromY: m.from_y, toX: m.to_x, toY: m.to_y, troops: m.troops, startAt: m.start_at, arriveAt: m.arrive_at, gatherUntil: m.gather_until, returnAt: m.return_at,
  carrying: m.state === "gathering" ? gatheredSoFar(m, t) : (m.carried?.got ?? 0), resource: m.carried?.kind ?? null }; };

export function worldRoutes({ q, pool, err, json, limited, clean }) {
  const int = (v) => (Number.isFinite(Number(v)) ? Math.trunc(Number(v)) : NaN);
  const heal = async (pid) => {
    const r = (await q(`SELECT p.troops, p.power, p.troops_at, coalesce((SELECT sum(troops) FROM marches WHERE player_id=p.id AND state<>'done' AND left_home),0)::int out
      FROM players p WHERE p.id=$1`, [pid])).rows[0];
    if (!r) return;
    const next = healTroops(r.troops, r.out, troopCap(r.power), Date.now() - +r.troops_at);
    await q("UPDATE players SET troops=greatest(troops,$2), troops_at=now() WHERE id=$1", [pid, next]);
  };
  const meWorld = async (pid) => (await q(`SELECT p.id, p.display_name name, p.power, p.troops, p.hero_power, p.castle_x x, p.castle_y y, p.shield_until, m.alliance_id
    FROM players p LEFT JOIN alliance_members m ON m.player_id=p.id WHERE p.id=$1`, [pid])).rows[0];
  return {
    "GET /v1/world/me": async (_r, _b, p) => {
      await ensurePlaced(q, p.id); await settle(pool); await heal(p.id);
      const w = await meWorld(p.id);
      const marches = (await q("SELECT * FROM marches WHERE player_id=$1 AND state<>'done' ORDER BY id", [p.id])).rows.map((m) => marchView(m, Date.now()));
      const unread = (await q("SELECT count(*)::int c FROM battle_reports WHERE player_id=$1 AND NOT claimed", [p.id])).rows[0].c;
      return json({ castle: { x: w.x, y: w.y }, shieldUntil: w.shield_until, troops: w.troops, troopCap: troopCap(w.power), heroPower: w.hero_power, marches, unreadReports: unread, rules: WORLD });
    },
    "GET /v1/world/map": async (r, _b, p) => {
      await ensurePlaced(q, p.id); await settle(pool);
      const sp = new URL(r.url).searchParams, now = Date.now();
      const cx = int(sp.get("x")), cy = int(sp.get("y")), rad = Math.min(WORLD.maxView, Math.max(1, int(sp.get("r") ?? 12)));
      if (Number.isNaN(cx) || Number.isNaN(cy) || Number.isNaN(rad)) return err("bad_viewport");
      const w = await meWorld(p.id);
      // Fog: sight comes from my castle, my alliance's castles and my live marches.
      const allies = w.alliance_id ? (await q("SELECT p.id, p.castle_x x, p.castle_y y FROM alliance_members m JOIN players p ON p.id=m.player_id WHERE m.alliance_id=$1 AND p.placed", [w.alliance_id])).rows : [{ id: p.id, x: w.x, y: w.y }];
      const live = (await q("SELECT * FROM marches WHERE state<>'done' AND least(from_x,to_x)<=$1::int+$3::int AND greatest(from_x,to_x)>=$1::int-$3::int AND least(from_y,to_y)<=$2::int+$3::int AND greatest(from_y,to_y)>=$2::int-$3::int LIMIT 300", [cx, cy, rad + WORLD.sight])).rows;
      const allyIds = new Set(allies.map((a) => String(a.id)));
      const sight = [...allies.map((a) => ({ x: a.x, y: a.y, r: WORLD.sight }))];
      for (const m of live) if (allyIds.has(String(m.player_id))) { const [x, y] = positionAt(m, now); sight.push({ x: Math.round(x), y: Math.round(y), r: WORLD.marchSight }); }
      const seen = (x, y) => sight.some((s) => dist(s.x, s.y, x, y) <= s.r);
      const box = [cx - rad, cx + rad, cy - rad, cy + rad];
      const castles = (await q(`SELECT p.id, p.display_name name, p.power, p.castle_x x, p.castle_y y, (p.shield_until > now()) shielded, a.id alliance_id, a.tag
        FROM players p LEFT JOIN alliance_members m ON m.player_id=p.id LEFT JOIN alliances a ON a.id=m.alliance_id
        WHERE p.placed AND p.castle_x BETWEEN $1 AND $2 AND p.castle_y BETWEEN $3 AND $4 LIMIT 400`, box)).rows
        .filter((c) => allyIds.has(String(c.id)) || seen(c.x, c.y))
        .map((c) => ({ id: c.id, name: c.name, power: c.power, x: c.x, y: c.y, shielded: c.shielded, mine: String(c.id) === String(p.id),
          ally: allyIds.has(String(c.id)), tag: c.tag, color: c.alliance_id ? allianceColor(c.alliance_id) : -1 }));
      const nodes = (await q(`SELECT n.id, n.x, n.y, n.kind, n.level, n.amount, mo.player_id occupier FROM world_nodes n LEFT JOIN marches mo ON mo.id=n.occupier_march_id
        WHERE (n.respawn_at IS NULL OR n.respawn_at <= now()) AND n.x BETWEEN $1 AND $2 AND n.y BETWEEN $3 AND $4 LIMIT 400`, box)).rows.filter((n) => seen(n.x, n.y))
        .map((n) => ({ ...n, occupiedByMe: String(n.occupier) === String(p.id), occupiedByAlly: n.occupier != null && allyIds.has(String(n.occupier)) }));
      const marches = live.map((m) => marchView(m, now)).filter((m) => allyIds.has(String(m.playerId)) || seen(m.x, m.y))
        .map((m) => ({ ...m, mine: String(m.playerId) === String(p.id), ally: allyIds.has(String(m.playerId)) }));
      const territory = castles.filter((c) => c.tag).map((c) => ({ x: c.x, y: c.y, r: WORLD.territory, tag: c.tag, color: c.color }));
      return json({ center: { x: cx, y: cy, r: rad }, me: { x: w.x, y: w.y }, sight, castles, nodes, marches, territory });
    },
    "POST /v1/world/march": async (_r, b, p) => {
      if (await limited(p.id, "write")) return err("rate_limited", 429);
      await ensurePlaced(q, p.id); await settle(pool);
      const kind = clean(b.kind, 8), x = int(b.x), y = int(b.y), troops = int(b.troops);
      if (!["gather", "camp", "attack"].includes(kind)) return err("bad_kind");
      if (!(x >= 0 && y >= 0 && x < WORLD.size && y < WORLD.size)) return err("bad_target");
      await heal(p.id);
      const w = await meWorld(p.id);
      if (!canSend(troops, w.troops)) return err("bad_troops"); // checked against the server's own count
      const d = dist(w.x, w.y, x, y); if (d > WORLD.maxRange || d < 1) return err("out_of_range");
      const busy = (await q("SELECT count(*)::int c FROM marches WHERE player_id=$1 AND state<>'done'", [p.id])).rows[0];
      if (busy.c >= WORLD.maxMarches) return err("too_many_marches", 409);
      let node = null, target = null;
      if (kind === "attack") {
        target = (await q("SELECT p.id, m.alliance_id FROM players p LEFT JOIN alliance_members m ON m.player_id=p.id WHERE p.placed AND p.castle_x=$1 AND p.castle_y=$2", [x, y])).rows[0];
        if (!target || String(target.id) === String(p.id)) return err("no_castle_there");
        if (w.alliance_id && String(target.alliance_id) === String(w.alliance_id)) return err("same_alliance", 403);
      } else {
        node = (await q("SELECT id, kind FROM world_nodes WHERE x=$1 AND y=$2 AND (respawn_at IS NULL OR respawn_at<=now())", [x, y])).rows[0];
        if (!node || (kind === "camp") !== (node.kind === "camp")) return err("no_node_there");
      }
      const now = new Date();
      // Troops leave home and the march is created in one transaction; the conditional UPDATE stops double-spends.
      const c = await pool.connect(); let rows;
      try {
        await c.query("BEGIN");
        const took = (await c.query("UPDATE players SET troops=troops-$2 WHERE id=$1 AND troops>=$2 RETURNING troops", [p.id, troops])).rowCount;
        if (!took) { await c.query("ROLLBACK"); return err("bad_troops"); }
        ({ rows } = await c.query(`INSERT INTO marches(player_id,kind,from_x,from_y,to_x,to_y,node_id,target_player_id,troops,hero_power,start_at,arrive_at,left_home)
          VALUES($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12,true) RETURNING *`, [p.id, kind, w.x, w.y, x, y, node?.id ?? null, target?.id ?? null, troops, w.hero_power, now, sec(now, travelSec(d))]));
        await c.query("COMMIT");
      } catch (e) { await c.query("ROLLBACK"); throw e; } finally { c.release(); }
      if (kind === "attack") await q("UPDATE players SET shield_until=least(shield_until, now()) WHERE id=$1", [p.id]); // attacking drops your own peace shield
      return json({ march: marchView(rows[0], Date.now()) }, 201);
    },
    "POST /v1/world/recall": async (_r, b, p) => {
      if (await limited(p.id, "write")) return err("rate_limited", 429);
      await settle(pool);
      const c = await pool.connect();
      try {
        await c.query("BEGIN"); await c.query("SELECT pg_advisory_xact_lock(4207)");
        const m = (await c.query("SELECT * FROM marches WHERE id=$1 AND player_id=$2 AND state IN ('marching','gathering') FOR UPDATE", [int(b.marchId), p.id])).rows[0];
        if (!m) { await c.query("ROLLBACK"); return err("not_recallable", 409); }
        const t = new Date();
        if (m.state === "gathering") await stopGathering(c, m, t);
        else await goHome(c, m, t, 0, positionAt(m, +t));
        await c.query("COMMIT");
      } catch (e) { await c.query("ROLLBACK"); throw e; } finally { c.release(); }
      return json({ ok: true });
    },
    "GET /v1/world/reports": async (r, _b, p) => {
      await settle(pool);
      const after = int(new URL(r.url).searchParams.get("after") || 0) || 0;
      const { rows } = await q(`SELECT b.id, b.kind, b.opponent_id, o.display_name opponent, b.x, b.y, b.my_troops, b.my_loss, b.their_troops, b.their_loss, b.loot, b.claimed, b.created_at
        FROM battle_reports b LEFT JOIN players o ON o.id=b.opponent_id WHERE b.player_id=$1 AND b.id>$2 ORDER BY b.id DESC LIMIT 50`, [p.id, after]);
      return json({ reports: rows });
    },
    "POST /v1/world/reports/claim": async (_r, _b, p) => {
      if (await limited(p.id, "write")) return err("rate_limited", 429);
      const { rows } = await q("UPDATE battle_reports SET claimed=true WHERE player_id=$1 AND NOT claimed RETURNING loot, kind, my_loss", [p.id]);
      const total = {}, lost = {}; let troopsLost = 0;
      for (const r of rows) {
        for (const [k, v] of Object.entries(r.loot || {})) { if (v > 0) total[k] = (total[k] || 0) + v; else if (v < 0) lost[k] = (lost[k] || 0) - v; }
        if (r.kind.startsWith("defend")) troopsLost += r.my_loss;
      }
      // lost = what raiders took from your stores; the client takes it out of the wallet
      return json({ claimed: rows.length, loot: total, lost, troopsLostDefending: troopsLost });
    },
  };
}
