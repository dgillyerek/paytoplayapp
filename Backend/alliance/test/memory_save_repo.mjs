export function memorySaveRepo() {
  const saves = new Map(); // playerId → { version, blob }
  const consent = new Map();
  const clawbacks = new Set();
  return {
    async get(playerId) {
      return saves.get(Number(playerId)) || null;
    },
    async getConsent(playerId) {
      return consent.get(Number(playerId)) || null;
    },
    async setConsent(playerId) {
      const iso = new Date().toISOString();
      consent.set(Number(playerId), iso);
      return iso;
    },
    async put(playerId, baseVersion, blob, mergeFn) {
      const id = Number(playerId);
      const cur = saves.get(id);
      if (!cur) {
        const merged = baseVersion !== 0;
        const b = merged ? mergeFn({}, blob) : blob;
        const row = { version: 1, blob: structuredClone(b) };
        saves.set(id, row);
        return { version: 1, blob: structuredClone(b), merged };
      }
      if (baseVersion === cur.version) {
        cur.version += 1;
        cur.blob = structuredClone(blob);
        return { version: cur.version, blob: structuredClone(cur.blob), merged: false };
      }
      cur.blob = mergeFn(cur.blob, blob);
      cur.version += 1;
      return { version: cur.version, blob: structuredClone(cur.blob), merged: true };
    },
    async clawback(playerId, purchaseId, grants, clawFn) {
      const key = Number(purchaseId);
      if (clawbacks.has(key)) return { applied: false, duplicate: true };
      clawbacks.add(key);
      const cur = saves.get(Number(playerId));
      if (!cur) return { applied: true, noSave: true };
      cur.blob = clawFn(cur.blob, grants);
      cur.version += 1;
      return { applied: true, version: cur.version };
    },
    _saves: saves,
  };
}
