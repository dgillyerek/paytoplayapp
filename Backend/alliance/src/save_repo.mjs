// Postgres side of cloud saves (schema: Backend/schema/007_cloud_saves.sql).

async function tx(pool, fn) {
  const c = await pool.connect();
  try {
    await c.query("BEGIN");
    const out = await fn((sql, args = []) => c.query(sql, args));
    await c.query("COMMIT");
    return out;
  } catch (e) {
    await c.query("ROLLBACK").catch(() => {});
    throw e;
  } finally {
    c.release();
  }
}

export function pgSaveRepo(pool) {
  const q = (sql, args = []) => pool.query(sql, args);
  return {
    async get(playerId) {
      const r = await q("SELECT version, blob, updated_at FROM player_saves WHERE player_id=$1", [playerId]);
      return r.rows[0] || null;
    },
    async getConsent(playerId) {
      const r = await q("SELECT cloud_save_consent_at FROM players WHERE id=$1", [playerId]);
      const v = r.rows[0]?.cloud_save_consent_at;
      return v ? new Date(v).toISOString() : null;
    },
    async setConsent(playerId) {
      const r = await q(
        "UPDATE players SET cloud_save_consent_at=coalesce(cloud_save_consent_at, now()) WHERE id=$1 RETURNING cloud_save_consent_at",
        [playerId],
      );
      return new Date(r.rows[0].cloud_save_consent_at).toISOString();
    },
    /**
     * Optimistic put. If baseVersion matches, replace blob and bump version.
     * On mismatch, merge(server, incoming) and still bump (last-write with sensible merge).
     * → { version, blob, merged }
     */
    async put(playerId, baseVersion, blob, mergeFn) {
      return tx(pool, async (q) => {
        const cur = (await q("SELECT version, blob FROM player_saves WHERE player_id=$1 FOR UPDATE", [playerId])).rows[0];
        if (!cur) {
          if (baseVersion !== 0) {
            // First save but client thought there was a server row: still accept as create with merge against empty.
            const merged = mergeFn({}, blob);
            const ins = await q(
              "INSERT INTO player_saves(player_id, version, blob) VALUES($1,1,$2) RETURNING version, blob",
              [playerId, JSON.stringify(merged)],
            );
            return { version: Number(ins.rows[0].version), blob: ins.rows[0].blob, merged: true };
          }
          const ins = await q(
            "INSERT INTO player_saves(player_id, version, blob) VALUES($1,1,$2) RETURNING version, blob",
            [playerId, JSON.stringify(blob)],
          );
          return { version: Number(ins.rows[0].version), blob: ins.rows[0].blob, merged: false };
        }
        const serverVersion = Number(cur.version);
        if (baseVersion === serverVersion) {
          const next = serverVersion + 1;
          const upd = await q(
            "UPDATE player_saves SET version=$2, blob=$3, updated_at=now() WHERE player_id=$1 RETURNING version, blob",
            [playerId, next, JSON.stringify(blob)],
          );
          return { version: Number(upd.rows[0].version), blob: upd.rows[0].blob, merged: false };
        }
        const mergedBlob = mergeFn(cur.blob, blob);
        const next = serverVersion + 1;
        const upd = await q(
          "UPDATE player_saves SET version=$2, blob=$3, updated_at=now() WHERE player_id=$1 RETURNING version, blob",
          [playerId, next, JSON.stringify(mergedBlob)],
        );
        return { version: Number(upd.rows[0].version), blob: upd.rows[0].blob, merged: true };
      });
    },
    /** Apply clawback once per purchase_id. */
    async clawback(playerId, purchaseId, grants, clawFn) {
      return tx(pool, async (q) => {
        const ins = await q(
          "INSERT INTO save_clawbacks(player_id, purchase_id, grants) VALUES($1,$2,$3) ON CONFLICT (purchase_id) DO NOTHING RETURNING id",
          [playerId, purchaseId, JSON.stringify(grants)],
        );
        if (!ins.rows[0]) return { applied: false, duplicate: true };
        const cur = (await q("SELECT version, blob FROM player_saves WHERE player_id=$1 FOR UPDATE", [playerId])).rows[0];
        if (!cur) return { applied: true, noSave: true };
        const nextBlob = clawFn(cur.blob, grants);
        await q("UPDATE player_saves SET version=version+1, blob=$2, updated_at=now() WHERE player_id=$1", [
          playerId,
          JSON.stringify(nextBlob),
        ]);
        return { applied: true, version: Number(cur.version) + 1 };
      });
    },
  };
}
