// Postgres side of purchases (schema: Backend/schema/004_purchases.sql). Every write that grants runs in one
// transaction and keys on UNIQUE (store, purchase_token), so a token is granted once however often it is sent.

async function tx(pool, fn) {
  const c = await pool.connect();
  try {
    await c.query("BEGIN");
    const out = await fn((sql, args = []) => c.query(sql, args));
    await c.query("COMMIT");
    return out;
  } catch (e) { await c.query("ROLLBACK").catch(() => {}); throw e; } finally { c.release(); }
}

async function grantTo(q, purchaseId, row, kind) {
  const grants = kind === "restore" ? {} : row.grants; // a restore never re-credits wallet resources
  const g = await q(`INSERT INTO iap_grants(purchase_id, player_id, sku, grants, entitlements, kind) VALUES($1,$2,$3,$4,$5,$6)
    ON CONFLICT (purchase_id, player_id) DO NOTHING RETURNING grants, entitlements`, [purchaseId, row.playerId, row.sku, JSON.stringify(grants), row.entitlements, kind]);
  for (const e of row.entitlements) {
    await q(`INSERT INTO player_entitlements(player_id, entitlement, purchase_id) VALUES($1,$2,$3)
      ON CONFLICT (player_id, entitlement) DO UPDATE SET purchase_id=EXCLUDED.purchase_id, revoked_at=NULL, granted_at=now()`, [row.playerId, e, purchaseId]);
  }
  if (g.rows[0]) return g.rows[0];
  return (await q("SELECT grants, entitlements FROM iap_grants WHERE purchase_id=$1 AND player_id=$2", [purchaseId, row.playerId])).rows[0];
}

export function pgIapRepo(pool) {
  const q = (sql, args = []) => pool.query(sql, args);
  return {
    async findPurchase(store, token) {
      return (await q("SELECT id, player_id, product_id, state, consumable FROM iap_purchases WHERE store=$1 AND purchase_token=$2", [store, token])).rows[0] || null;
    },
    async setState(store, token, state) {
      await q("UPDATE iap_purchases SET state=$3, updated_at=now() WHERE store=$1 AND purchase_token=$2", [store, token, state]);
    },
    async recordPending(r) {
      await q(`INSERT INTO iap_purchases(player_id, store, product_id, sku, purchase_token, order_id, consumable, state, purchased_at, store_payload)
        VALUES($1,$2,$3,$4,$5,$6,$7,'pending',$8,$9) ON CONFLICT (store, purchase_token) DO NOTHING`,
        [r.playerId, r.store, r.productId, r.sku, r.token, r.orderId, r.consumable, r.purchasedAt, JSON.stringify(r.payload)]);
    },
    /** → { outcome: new | same_player | restored | other_player | revoked, purchaseId, grants, entitlements, state? } */
    async recordGrant(r) {
      return tx(pool, async (q) => {
        const ins = await q(`INSERT INTO iap_purchases(player_id, store, product_id, sku, purchase_token, order_id, consumable, state, purchased_at, store_payload)
          VALUES($1,$2,$3,$4,$5,$6,$7,'granted',$8,$9) ON CONFLICT (store, purchase_token) DO NOTHING RETURNING id`,
          [r.playerId, r.store, r.productId, r.sku, r.token, r.orderId, r.consumable, r.purchasedAt, JSON.stringify(r.payload)]);
        if (ins.rows[0]) {
          const g = await grantTo(q, ins.rows[0].id, r, "purchase");
          return { outcome: "new", purchaseId: Number(ins.rows[0].id), grants: g.grants, entitlements: g.entitlements };
        }
        const p = (await q("SELECT id, player_id, state FROM iap_purchases WHERE store=$1 AND purchase_token=$2 FOR UPDATE", [r.store, r.token])).rows[0];
        const id = Number(p.id);
        if (["refunded", "voided", "cancelled"].includes(p.state)) return { outcome: "revoked", state: p.state, purchaseId: id };
        if (p.state === "pending") {
          await q("UPDATE iap_purchases SET state='granted', player_id=$2, order_id=coalesce($3, order_id), purchased_at=coalesce($4, purchased_at), store_payload=$5, updated_at=now() WHERE id=$1",
            [id, r.playerId, r.orderId, r.purchasedAt, JSON.stringify(r.payload)]);
          const g = await grantTo(q, id, r, "purchase");
          return { outcome: "new", purchaseId: id, grants: g.grants, entitlements: g.entitlements };
        }
        if (Number(p.player_id) === Number(r.playerId)) {
          const g = (await q("SELECT grants, entitlements FROM iap_grants WHERE purchase_id=$1 AND player_id=$2", [id, r.playerId])).rows[0]
            || await grantTo(q, id, r, "purchase");
          return { outcome: "same_player", purchaseId: id, grants: g.grants, entitlements: g.entitlements };
        }
        if (r.consumable) return { outcome: "other_player", purchaseId: id };
        // Same store user on a new install / device (new player row): restore the unlock, never the wallet grants.
        const g = await grantTo(q, id, r, "restore");
        return { outcome: "restored", purchaseId: id, grants: g.grants, entitlements: g.entitlements };
      });
    },
    async revoke(store, token, state) {
      return tx(pool, async (q) => {
        const p = (await q("UPDATE iap_purchases SET state=$3, updated_at=now() WHERE store=$1 AND purchase_token=$2 RETURNING id, player_id", [store, token, state])).rows[0];
        if (!p) return { found: false };
        // Sum purchase-kind grants for clawback (restore grants are empty). First non-revoked row wins.
        const g = (await q("SELECT grants FROM iap_grants WHERE purchase_id=$1 AND kind='purchase' ORDER BY id LIMIT 1", [p.id])).rows[0];
        await q("UPDATE iap_grants SET revoked_at=now() WHERE purchase_id=$1 AND revoked_at IS NULL", [p.id]);
        await q("UPDATE player_entitlements SET revoked_at=now() WHERE purchase_id=$1 AND revoked_at IS NULL", [p.id]);
        return { found: true, purchaseId: Number(p.id), playerId: Number(p.player_id), grants: g?.grants || {} };
      });
    },
    async cancelPending(store, token) {
      await q("UPDATE iap_purchases SET state='cancelled', updated_at=now() WHERE store=$1 AND purchase_token=$2 AND state='pending'", [store, token]);
    },
    async listEntitlements(playerId) {
      return (await q("SELECT entitlement FROM player_entitlements WHERE player_id=$1 AND revoked_at IS NULL ORDER BY entitlement", [playerId])).rows.map((r) => r.entitlement);
    },
    async recordEvent(messageId, kind, token, payload) {
      const r = await q("INSERT INTO iap_rtdn_events(message_id, kind, purchase_token, payload) VALUES($1,$2,$3,$4) ON CONFLICT DO NOTHING RETURNING message_id", [messageId, kind, token, JSON.stringify(payload)]);
      return r.rowCount > 0;
    },
  };
}
