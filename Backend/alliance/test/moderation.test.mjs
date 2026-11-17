import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { createFilter, chatFilter, moderatorAuthorized, moderationRoutes, MOD_ACTIONS } from "../src/moderation.mjs";

test("shipped filter masks whole words after leet folding and repeat collapsing", () => {
  assert.equal(chatFilter("what the fuck").text, "what the ****");
  assert.equal(chatFilter("you b1tch").text, "you *****");
  assert.equal(chatFilter("fuuuuck").text, "*******");
  assert.equal(chatFilter("sh1t and $hit").text, "**** and ****");
  for (const ok of ["classic assassin from Scunthorpe", "I have 1500000 gold and level 31", "good game, see you at the castle"]) {
    const r = chatFilter(ok); assert.equal(r.text, ok); assert.equal(r.masked, 0); assert.equal(r.refusal, null);
  }
});
test("links and contact info are refused", () => {
  for (const b of ["https://discord.gg/abc", "go to www.example.org", "free gems at gems4u.xyz", "mail kid@example.com"]) assert.equal(chatFilter(b).refusal, "links_not_allowed");
  for (const b of ["text me 0917 555 1234", "call +1 (416) 555-0199"]) assert.equal(chatFilter(b).refusal, "personal_info");
});
test("flags in the json switch link / contact refusal off", () => {
  const f = createFilter({ words: ["darn"], block_links: false, block_contact_info: false });
  assert.equal(f("darn www.x.com 0917 555 1234").text, "**** www.x.com 0917 555 1234");
});
test("the server reads the same list the client ships", () => {
  const j = JSON.parse(readFileSync(new URL("../../../Assets/ThemePack/fantasy_kingdom_a/kids/chat_filter.json", import.meta.url), "utf8"));
  assert.ok(j.words.length > 20);
});

const req = (auth) => ({ headers: { get: (h) => (h === "authorization" ? auth : null) }, url: "https://x/v1/mod/queue" });
test("moderator endpoints need a long MODERATION_TOKEN and the exact bearer", () => {
  const tok = "m".repeat(32);
  assert.ok(moderatorAuthorized(req("Bearer " + tok), tok));
  assert.ok(!moderatorAuthorized(req("Bearer " + tok + "x"), tok));
  assert.ok(!moderatorAuthorized(req(""), tok));
  assert.ok(!moderatorAuthorized(req("Bearer short"), "short"));
  assert.ok(!moderatorAuthorized(req("Bearer "), ""));
});
test("resolve: delete_message removes the message, mute sets the timer, and the row is closed", async () => {
  const sql = [];
  const q = async (s, a) => { sql.push([s, a]); return s.startsWith("SELECT id, message_id") ? { rows: [{ id: 3, message_id: 77, reported_player_id: 5 }] } : { rows: [] }; };
  const json = (b, st = 200) => ({ status: st, body: b });
  const r = moderationRoutes({ q, err: (c, st = 400) => json({ error: c }, st), json });
  assert.equal((await r["POST /v1/mod/resolve"]({}, { id: 3, action: "nope" })).status, 400);
  assert.equal((await r["POST /v1/mod/resolve"]({}, { id: 3, action: "delete_message" })).status, 200);
  assert.ok(sql.some(([s, a]) => s.startsWith("DELETE FROM alliance_chat") && a[0] === 77));
  assert.equal((await r["POST /v1/mod/resolve"]({}, { id: 3, action: "mute_24h", note: "abuse" })).status, 200);
  assert.ok(sql.some(([s]) => s.includes("chat_muted_until=now() + interval '24 hours'")));
  assert.ok(sql.some(([s, a]) => s.startsWith("UPDATE chat_reports SET status='closed'") && a[1] === "mute_24h" && a[2] === "abuse"));
  assert.deepEqual(MOD_ACTIONS, ["dismiss", "delete_message", "mute_24h", "mute_7d"]);
});
