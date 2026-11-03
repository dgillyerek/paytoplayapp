import test from "node:test";
import assert from "node:assert/strict";
import { battle, positionAt, travelSec, WORLD, roll, gatherRate } from "../src/world.mjs";

test("battle is deterministic", () => {
  const a = battle({ troops: 1000, hero: 200 }, { troops: 900, hero: 0 }, "x1", 0.1);
  assert.deepEqual(a, battle({ troops: 1000, hero: 200 }, { troops: 900, hero: 0 }, "x1", 0.1));
});
test("roll stays within 0.9..1.1", () => { for (let i = 0; i < 200; i++) { const r = roll("s" + i); assert.ok(r >= 0.9 && r <= 1.1); } });
test("much stronger side always wins", () => assert.ok(battle({ troops: 5000, hero: 500 }, { troops: 500, hero: 0 }, "k", 0.1).attackerWon));
test("castle defender never loses more than the cap", () => {
  for (let i = 0; i < 100; i++) { const b = battle({ troops: 100000, hero: 0 }, { troops: 1000, hero: 0 }, "c" + i, WORLD.defenderLossCap); assert.ok(b.dLoss <= 100); }
});
test("losing attacker loses at most the attacker cap", () => {
  const b = battle({ troops: 100, hero: 0 }, { troops: 10000, hero: 0 }, "z", 1); assert.ok(!b.attackerWon); assert.ok(b.aLoss <= 25);
});
test("travel time has a floor and scales", () => { assert.equal(travelSec(0), WORLD.minMarchSec); assert.equal(travelSec(30), 120); });
test("march position interpolates", () => {
  const m = { state: "marching", from_x: 0, from_y: 0, to_x: 10, to_y: 0, start_at: new Date(0), arrive_at: new Date(10000) };
  assert.deepEqual(positionAt(m, 5000), [5, 0]); assert.deepEqual(positionAt(m, 99999), [10, 0]);
});
test("gather rate grows with level", () => assert.ok(gatherRate(1000, 2) > gatherRate(1000, 1)));
