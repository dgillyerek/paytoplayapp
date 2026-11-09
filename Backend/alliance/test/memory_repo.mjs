// In-memory twin of iap_repo.mjs with the same outcomes, for unit tests (the pg repo has its own integration test).
export function memoryIapRepo() {
  const purchases = new Map(); // `${store}|${token}` → row
  const grants = []; // { purchaseId, playerId, sku, grants, entitlements, kind, revoked }
  const ents = new Map(); // `${playerId}|${ent}` → { purchaseId, revoked }
  const events = new Set();
  let nextId = 1;
  const k = (s, t) => s + "|" + t;
  function grantTo(p, r, kind) {
    let g = grants.find((x) => x.purchaseId === p.id && x.playerId === r.playerId);
    if (!g) { g = { purchaseId: p.id, playerId: r.playerId, sku: r.sku, grants: kind === "restore" ? {} : { ...r.grants }, entitlements: [...r.entitlements], kind, revoked: false }; grants.push(g); }
    for (const e of r.entitlements) ents.set(r.playerId + "|" + e, { purchaseId: p.id, revoked: false });
    return g;
  }
  return {
    purchases, grants, ents, events,
    async findPurchase(store, token) { const p = purchases.get(k(store, token)); return p ? { ...p } : null; },
    async setState(store, token, state) { const p = purchases.get(k(store, token)); if (p) p.state = state; },
    async recordPending(r) { if (!purchases.has(k(r.store, r.token))) purchases.set(k(r.store, r.token), { id: nextId++, player_id: r.playerId, product_id: r.productId, state: "pending", consumable: r.consumable }); },
    async recordGrant(r) {
      let p = purchases.get(k(r.store, r.token));
      if (!p) {
        p = { id: nextId++, player_id: r.playerId, product_id: r.productId, state: "granted", consumable: r.consumable };
        purchases.set(k(r.store, r.token), p);
        const g = grantTo(p, r, "purchase");
        return { outcome: "new", purchaseId: p.id, grants: g.grants, entitlements: g.entitlements };
      }
      if (["refunded", "voided", "cancelled"].includes(p.state)) return { outcome: "revoked", state: p.state, purchaseId: p.id };
      if (p.state === "pending") { p.state = "granted"; p.player_id = r.playerId; const g = grantTo(p, r, "purchase"); return { outcome: "new", purchaseId: p.id, grants: g.grants, entitlements: g.entitlements }; }
      if (p.player_id === r.playerId) { const g = grantTo(p, r, "purchase"); return { outcome: "same_player", purchaseId: p.id, grants: g.grants, entitlements: g.entitlements }; }
      if (r.consumable) return { outcome: "other_player", purchaseId: p.id };
      const g = grantTo(p, r, "restore");
      return { outcome: "restored", purchaseId: p.id, grants: g.grants, entitlements: g.entitlements };
    },
    async revoke(store, token, state) {
      const p = purchases.get(k(store, token)); if (!p) return { found: false };
      p.state = state;
      for (const g of grants) if (g.purchaseId === p.id) g.revoked = true;
      for (const v of ents.values()) if (v.purchaseId === p.id) v.revoked = true;
      return { found: true, purchaseId: p.id };
    },
    async cancelPending(store, token) { const p = purchases.get(k(store, token)); if (p && p.state === "pending") p.state = "cancelled"; },
    async listEntitlements(playerId) { return [...ents.entries()].filter(([key, v]) => key.startsWith(playerId + "|") && !v.revoked).map(([key]) => key.slice(String(playerId).length + 1)).sort(); },
    async recordEvent(id) { if (events.has(id)) return false; events.add(id); return true; },
  };
}
