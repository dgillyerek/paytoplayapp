import test from "node:test";
import assert from "node:assert/strict";
import { PRESET, SAFE_NAME, effectiveChatMode, chatRefusal, visibleChat, ageBandChangeAllowed, isRestricted, safetyRoutes } from "../src/safety.mjs";

test("under13 and unknown are restricted; only 13plus is not", () => {
  assert.ok(isRestricted("under13")); assert.ok(isRestricted("unknown")); assert.ok(isRestricted(undefined)); assert.ok(!isRestricted("13plus"));
});
test("restricted players never get free chat and default to off", () => {
  assert.equal(effectiveChatMode("under13", null), "off");
  assert.equal(effectiveChatMode("under13", "free"), "off");
  assert.equal(effectiveChatMode("unknown", "presets"), "presets");
  assert.equal(effectiveChatMode("13plus", null), "free");
  assert.equal(effectiveChatMode("13plus", "off"), "off");
});
test("presets mode accepts only preset ids", () => {
  assert.equal(chatRefusal("presets", "[[p:hello]]"), null);
  for (const bad of ["hello", "[[p:hello]] call me 555-1234", "[[p:HELLO]]", "[[p:https://x.y]]", "[[p:]]"]) assert.equal(chatRefusal("presets", bad), "presets_only");
  assert.equal(chatRefusal("off", "[[p:hello]]"), "chat_off");
  assert.equal(chatRefusal("free", "anything"), null);
});
test("visible chat drops blocked senders and non-presets", () => {
  const rows = [{ player_id: 1, body: "[[p:hi]]" }, { player_id: 2, body: "my address is" }, { player_id: 3, body: "[[p:gg]]" }];
  assert.deepEqual(visibleChat("presets", rows, new Set(["3"])).map((r) => r.player_id), [1]);
  assert.deepEqual(visibleChat("free", rows, new Set(["3"])).map((r) => r.player_id), [1, 2]);
  assert.deepEqual(visibleChat("off", rows, new Set()), []);
});
test("under13 age band is sticky", () => {
  assert.ok(!ageBandChangeAllowed("under13", "13plus")); assert.ok(!ageBandChangeAllowed("under13", "unknown"));
  assert.ok(ageBandChangeAllowed("unknown", "13plus")); assert.ok(ageBandChangeAllowed("unknown", "under13"));
});
test("generated names match the safe shape; typed ones do not", () => {
  assert.ok(SAFE_NAME.test("Swift Ranger 417")); assert.ok(!SAFE_NAME.test("Sam Smith 2014")); assert.ok(!SAFE_NAME.test("sam@x.com"));
  assert.ok(PRESET.test("[[p:help_please]]"));
});

// Route wiring against a fake db: a restricted player's free-text chat is refused before the original handler runs.
const fake = (players) => {
  const calls = [];
  const q = async (sql, args) => {
    calls.push(sql);
    if (sql.startsWith("SELECT age_band")) return { rows: [players[args[0]]] };
    if (sql.startsWith("SELECT alliance_id")) return { rows: [{ alliance_id: 9 }] };
    return { rows: [] };
  };
  return { q, calls };
};
const env = (players, routes) => {
  const { q, calls } = fake(players);
  const json = (b, s = 200) => ({ status: s, body: b });
  const err = (c, s = 400) => json({ error: c }, s);
  return { r: safetyRoutes({ q, err, json, clean: (s) => (typeof s === "string" ? s : ""), limited: async () => false, routes }), calls };
};
test("restricted free text is refused server-side; 13plus passes through", async () => {
  let reached = 0;
  const routes = { "POST /v1/chat": async () => { reached++; return { status: 201 }; }, "POST /v1/alliances": async () => ({ status: 201 }), "POST /v1/profile": async (_r, b) => ({ status: 200, body: b }) };
  const { r } = env({ 1: { age_band: "under13", chat_mode: "presets" }, 2: { age_band: "13plus", chat_mode: null } }, routes);
  assert.equal((await r["POST /v1/chat"]({}, { body: "hi I live at" }, { id: 1 })).status, 403);
  assert.equal((await r["POST /v1/chat"]({}, { body: "[[p:hello]]" }, { id: 1 })).status, 201);
  assert.equal((await r["POST /v1/chat"]({}, { body: "hi" }, { id: 2 })).status, 201);
  assert.equal(reached, 2);
  assert.equal((await r["POST /v1/alliances"]({}, { name: "Sam's Fort" }, { id: 1 })).status, 403);
  assert.equal((await r["POST /v1/profile"]({}, { name: "Sam Smith" }, { id: 1 })).body.name, undefined);
  assert.equal((await r["POST /v1/profile"]({}, { name: "Grown Up" }, { id: 2 })).body.name, "Grown Up");
});
test("age band cannot be lifted from under13", async () => {
  const routes = { "POST /v1/account/age_band": async () => ({ status: 200 }) };
  const { r } = env({ 1: { age_band: "under13", chat_mode: null } }, routes);
  assert.equal((await r["POST /v1/account/age_band"]({}, { ageBand: "13plus" }, { id: 1 })).status, 403);
});
