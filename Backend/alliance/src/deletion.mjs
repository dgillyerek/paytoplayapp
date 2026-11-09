// POST /v1/account/delete: remove a player's personal data in one transaction (schema 006).
// Deleted: chat messages, help requests / helps given, gifts, alliance membership (leadership handed on, empty alliance
// dropped), marches and battle reports, reports filed and blocks (reports about them lose the id and the text snapshot), entitlements. Other players' rows that point at this
// player are unlinked (opponent / target / reported id set NULL). The players row is scrubbed (random device + token
// hash so the old device id and token stop working, name "Deleted", no stock, off the map, age band unknown) and
// kept only as the owner of store receipt rows (iap_purchases / iap_grants), which hold no personal info.
import crypto from "node:crypto";

export const DELETE_STEPS = [
  ["UPDATE world_nodes SET occupier_march_id=NULL WHERE occupier_march_id IN (SELECT id FROM marches WHERE player_id=$1)"],
  ["UPDATE battle_reports SET march_id=NULL WHERE march_id IN (SELECT id FROM marches WHERE player_id=$1)"],
  ["DELETE FROM battle_reports WHERE player_id=$1"],
  ["UPDATE battle_reports SET opponent_id=NULL WHERE opponent_id=$1"],
  ["UPDATE marches SET target_player_id=NULL WHERE target_player_id=$1"],
  ["DELETE FROM marches WHERE player_id=$1"],
  ["DELETE FROM alliance_chat WHERE player_id=$1"],
  ["DELETE FROM help_given WHERE helper_id=$1"],
  ["DELETE FROM help_requests WHERE player_id=$1"],
  ["DELETE FROM alliance_gifts WHERE recipient_id=$1"],
  ["DELETE FROM chat_reports WHERE reporter_id=$1"],
  ["UPDATE chat_reports SET reported_player_id=NULL, body_snapshot='[deleted]' WHERE reported_player_id=$1"],
  ["DELETE FROM chat_blocks WHERE blocker_id=$1 OR blocked_id=$1"],
  ["DELETE FROM player_entitlements WHERE player_id=$1"],
  ["DELETE FROM rate_hits WHERE player_id=$1"],
];

const SCRUB = `UPDATE players SET device_hash=$2, token_hash=$3, display_name='Deleted', power=0, hero_power=0, placed=false,
  stock='{}'::jsonb, age_band='unknown', chat_mode=NULL, deleted_at=now() WHERE id=$1`;

/** Leave the alliance like POST /v1/alliances/leave: hand leadership on, or drop the alliance when it empties. */
async function leave(c, pid) {
  const m = (await c.query("SELECT alliance_id, role FROM alliance_members WHERE player_id=$1", [pid])).rows[0];
  if (!m) return;
  await c.query("DELETE FROM alliance_members WHERE player_id=$1", [pid]);
  const next = (await c.query("SELECT player_id FROM alliance_members WHERE alliance_id=$1 ORDER BY role DESC, joined_at LIMIT 1", [m.alliance_id])).rows[0];
  if (!next) await c.query("DELETE FROM alliances WHERE id=$1", [m.alliance_id]);
  else if (m.role === 5) { await c.query("UPDATE alliance_members SET role=5 WHERE player_id=$1", [next.player_id]); await c.query("UPDATE alliances SET leader_id=$2 WHERE id=$1", [m.alliance_id, next.player_id]); }
}

export async function deletePlayer(pool, pid, rand = () => crypto.randomBytes(24).toString("hex")) {
  const c = await pool.connect();
  try {
    await c.query("BEGIN");
    await leave(c, pid);
    for (const [sql] of DELETE_STEPS) await c.query(sql, [pid]);
    await c.query(SCRUB, [pid, "deleted:" + rand(), "deleted:" + rand()]);
    await c.query("COMMIT");
    return true;
  } catch (e) { await c.query("ROLLBACK"); throw e; } finally { c.release(); }
}

export function deletionRoutes({ pool, err, json, limited }) {
  return {
    "POST /v1/account/delete": async (_r, b, p) => {
      if (b.confirm !== "delete") return err("confirm_required");
      if (await limited(p.id, "write")) return err("rate_limited", 429);
      await deletePlayer(pool, p.id);
      return json({ deleted: true });
    },
  };
}
