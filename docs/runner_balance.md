# Runner balance: 50 levels

**Goal (Derek):** a free player with normal upgrades gets through levels 1–30 fairly easily. Levels 31–50 are close to impossible without buying upgrades. Any change to a baseline (units, power-ups, upgrades, starting damage …) that moves the curve noticeably should fail a test.

**Where it stands:** the free player hits the **wall at level 31**. Levels 1–12 play the same as before. Since the Gold Mine (below), a free player is no longer stuck for good: L40 takes about 3 months and L50 about 18 months.

## How the math works

1. **What a level asks for (measured, not invented).** For each level, the test plays the real `RunnerRun` sim with a dodging player (0.3 s reaction, 1/30 s steps). It scales every squad unit's damage and HP by one number *s* and bisects for the smallest *s* that still wins (range 1/8–64, 12 halvings).
   - **Required power = s²** (damage × HP, a Lanchester-style "power").
   - Required power runs ≈0.5 on L1, 0.96 on L12, 2.0 on L20, 4.6 on L25 and 9.6 on L30. It jumps to **15.0 on L31**, then reaches 20 on L40 and 30.5 on L50.
2. **What a player has.** Power = (DPS-weighted damage multiplier) × (HP-weighted HP multiplier) over the level's full squad (lead plus follower chests). A hero's multiplier is `HeroStats(level, stars) / HeroStats(1, 1)` from campaign.json.
3. **Ratio = player power / required power** at the nominal pace (level *n* is reached in session *n*, i.e. day (n−1)/3). Ratio ≥ 1 means the level clears. The **wall** is the first level where the free player's ratio drops below 1.
4. **The economy (`RunnerBalance` / `RunnerEconomy`).** It is a deterministic session-by-session timeline from a fresh save. Each session it:
   - adds farm, Gold Mine and gift income, the one-time campaign, bounty and quest rewards, and the profile's power-pack buys (contents from stores.json);
   - ticks trainings on 2 slots, using the real level-up gold, food and timers (30·1.6^L s);
   - applies star-ups and unlocks from shards;
   - buys greedy level-ups (best Δpower per gold + 2·food);
   - uses speed-ups and gems for rushes (1 gem/min) and Hero Tomes on the best hero;
   - attempts the next level once (the stuck player buys on a fail and keeps going). There are **no replays**: a beaten level can never be played again, so each level pays its win gold and shards once.

   - spends the building share of gold (25%) on the village: raise the Castle when the next mine level needs it, build the Gold Mine, then upgrade it, one job at a time, with the real costs and timers from base_economy.json (jobs under 5 min finish free).

   Runner win gold uses the real reward formula.
5. **The level ramp (runner_levels.json `ramp`).**
   - Past L12, levels stop adding length past 330 m and stop adding enemies or boxes past 20 rows (`count_cap`).
   - Each level multiplies enemy HP by 1.115 and enemy damage by 1.072 (boss HP 1.115).
   - From L31 on there is a one-time **step of +30% HP / +12% damage**, followed by a gentler 1.021 HP / 1.0125 damage / 1.016 boss HP per level (HP and damage were 1.016 / 1.0105 before the Oct 26, 2026 obstacle pass; see below).
   - The result is a ramp, not a cliff: no level needs more than 1.6× the level before (tested).

## Tougher obstacles and a frailer hero (Oct 27, 2026)

**Why (Derek):** "the destructible obstacles are too weak. regular obstacles don't do enough damage", and "the hero need to start with less health. it's too easy on the first few levels without any upgrades. don't make it significantly harder".

**What changed:**

| | Before | After |
|---|---|---|
| Barricade / palisade / stone wall HP (share of the beat's box HP) | 0.15 / 0.22 / 0.30 | 0.41 / 0.60 / 0.83 |
| Full-width gate HP share | 0.10 | 0.28 |
| Least barrier HP (side / full width) | 40 / 60 | 110 / 165 |
| Palisade damage (L1, + per level) | 50 + 4 | 92 + 7 |
| Stone wall damage | 60 + 5 | 110 + 9 |
| Barricade damage | 70 + 5 | 130 + 9 |
| Spikes damage | 70 + 5 | 130 + 9 |
| Log damage | 96 + 7 | 176 + 13 |
| Fire damage | 52 + 5 | 96 + 9 |
| L1 (hand-made): spikes / fire / palisade | 70 / 52 / 120 HP, 50 dmg | 130 / 96 / 330 HP, 92 dmg |
| Lyra (lead hero) base HP | 420 | 320 |

- Barrier HP is about 2.75x, so a barrier takes sustained fire to break. Trap and barrier contact damage is about 1.85x.
- A side hit still does 2x (`SideHitMul`). A follower still takes 40% of the hero's damage, capped one short of its 25 HP, so one hit never kills a fresh follower.
- Lyra's HP is cut on the base unit, so trained HP boosts keep their value.
- Up to L30, walking into a full-width barrier with the weakest squad that clears the level now costs up to 65% of the hero's HP; before, it was under half. The test now allows up to two thirds.

**Early levels with no upgrades (320 HP).** These come from a test player who dodges everything, and from careless players who walk the hero into the first 1, 2, 3 or all of a level's traps and barriers and dodge the rest.
- Clean play wins L1–L8 and never drops below 310 HP.
- One mistake still wins L1–L8, with the lowest point between 127 and 310 HP.
- Three mistakes still win L1–L6, but the hero drops to 8–118 HP. L7 is lost at 3 mistakes and L8 at 2.
- Walking into everything clears only L1 (118 HP at the lowest).

**Re-tune.** The tougher obstacles made the layout re-rolls below stale, so I swept salts 0–15 per level again. The new picks (36 levels; a pick must also keep an escape lane beside every trap and keep the boxes' and enemies' total HP within 2%) bring the curve back to the pre-variety one: free wall L31, every step at most 1.50x, L1–12 under power 1. That also brings the easier L34–35, L41, L44 and L49 back in line.

| Level | Layout | Level | Layout | Level | Layout | Level | Layout |
|---|---|---|---|---|---|---|---|
| L2 | 2 | L3 | 8 | L4 | 12 | L5 | 1 |
| L6 | 15 | L10 | 2 | L11 | 2 | L12 | 2 |
| L14 | 15 | L15 | 5 | L16 | 13 | L18 | 4 |
| L20 | 15 | L21 | 2 | L22 | 12 | L24 | 7 |
| L25 | 12 | L26 | 11 | L27 | 5 | L28 | 5 |
| L29 | 2 | L30 | 8 | L31 | 15 | L33 | 3 |
| L34 | 10 | L37 | 12 | L38 | 13 | L41 | 4 |
| L42 | 4 | L43 | 8 | L44 | 11 | L45 | 1 |
| L46 | 5 | L47 | 7 | L48 | 9 | L50 | 14 |

**Required power, #90 first pass → now:** L34 16.0 → 19.8, L35 16.1 → 18.4, L41 17.9 → 21.1, L44 22.3 → 30.5, L49 24.0 → 30.3, L50 33.5 → 35.1 (before #90: 19.5 / 17.5 / 22.3 / 29.3 / 29.6 / 35.7).

| Profile | Wall | L50 first clear (day): before #90 → #90 first pass → now |
|---|---|---|
| free | L31 | 830.0 → 564.3 → **829.0** |
| light | L34 | 63.0 → 63.0 → **56.0** |
| mid | L36 | 28.0 → 28.0 → **23.0** |
| whale | none | 16.3 → 16.3 → **16.3** |
| stuck_39 | L31 | 745.0 → 485.7 → **602.0** |

## Layout variety (Oct 27, 2026)

**Why (Derek):** "it seems like each item is in the same place". Every generated level put its beats every 13 m and built each beat the same way, so boxes, traps and barriers stood in the same spots level after level (98–100% of item positions matched the level before).

**What changed (`RunnerProgression`, L2 onwards; L1 stays hand-made):**
- **Uneven spacing.** After the three opening beats, the road runs in stretches of 1–3 beats that are tight (9–11.5 m), even (12–14 m) or breathing (15–18 m). The draw stays within `MaxDrift` (6 m) of the even 13 m beat. Fitting back to the level's length can push a beat a little further (10.5 m at most). The last beat stands where it always did, so the finale and the boss keep their places.
- **Set pieces.** Each beat is one of 8 set pieces (`RunnerSetPiece`):
  - **March:** the old layout.
  - **WaveFirst:** half the back line ahead of the box.
  - **Ambush:** the box in the middle with the wave split to both sides.
  - **TrapCorridor:** traps down both edges.
  - **Barricade:** a barrier on one edge with the wave across the open side.
  - **ArcherNest:** archers behind a wall on one edge.
  - **CrateRun:** the beat's boxes back to back down the middle.
  - **FodderRush:** the front wave in three small packs.
- **How the pieces are picked.** Beat 2 cycles Ambush / CrateRun / ArcherNest / FodderRush by level number, so two levels in a row never open the same way. Later beats are drawn from the seed among the pieces that beat's contents allow, never the same piece twice running.
- **What stays the same.** What each beat holds (boxes, enemies, traps, barriers, HP) still follows the beat index. So counts, HP, the box-to-enemy trend and the 1.6x limit are unchanged in kind; only placement moves.
- **Safety.** Every trap and side barrier still leaves an escape lane of at least 2 m (`KeepEscapeLanes`). Enemies that would spawn pressed against a full-width barrier stand `WallStandOff` (1.8 m) back, so they walk from their first frame (`StandOffWalls`).
- **Result.** Two levels in a row now share 3–39% of their item positions (same kind, same lane, within 1 m), against 98–100% before.

**Layout re-rolls (`"layout": n` in `level_tweaks`).** A re-roll changes only the RNG for where the beats start and which set piece each is. Everything on the road stays the same. The first roll made some levels much harder or easier than the curve, which mostly came down to whether boxes got opened before the wave arrived. (Superseded by the re-tune in "Tougher obstacles and a frailer hero" above.) A sweep of salts 0–9 per level (required power, a shipped-squad win and a free at-pace win) picked the re-rolls that keep the curve close to before, keep the walls, keep L1–12 under power 1, and keep every step under 1.6x:

| Level | Layout | Required power before → after |
|---|---|---|
| L2 | 7 | 0.67 → 0.67 |
| L5 | 2 | 0.70 → 0.67 |
| L6 | 3 | 0.87 → 0.84 |
| L9 | 1 | 0.78 → 0.81 |
| L10 | 2 | 0.80 → 0.80 |
| L11 | 2 | 0.70 → 0.78 |
| L12 | 2 | 0.83 → 0.81 |
| L15 | 5 | 1.27 → 1.21 |
| L29 | 3 | 10.46 → 10.68 |
| L31 | 4 | 14.94 → 14.71 |
| L32 | 4 | 14.62 → 14.53 |
| L33 | 2 | 14.31 → 14.31 |
| L50 | 2 | 35.70 → 33.48 |

**Balance before → after:**
- **Walls:** unchanged (free L31, light L34, mid L36, whale none, stuck_39 L31).
- **Largest step:** 1.56x → 1.49x.
- **Free player:** L40 day 152 → 98 and L50 day 830 → 564. Some late levels (L34–35, L41, L44, L49) roll easier than before.
- **Paying players:** light, mid and whale clear days are unchanged.

`RunnerLayoutVarietyTests` checks:
- position similarity under 50% for every pair of neighbouring levels, and L10 / L11 / L12;
- no repeated set-piece sequence;
- 9–18 m gaps;
- escape lanes;
- the wall stand-off.

`RunnerLevelTweakTests` checks that a re-roll keeps the same boxes, enemies, traps, barriers and HP (within 2%, since HP climbs along the road) and moves rows.

## Side hits, enemies avoid obstacles, per-level tweaks (Oct 27, 2026)

**What changed in play (Derek):**
- **Side hits hurt more.** A hero or follower that runs into the side of a trap or barrier takes double damage (`SideHitMul` 2). A follower's side hit is capped at 24 of its 25 HP, so one hit never kills it outright.
- **Enemies go around traps and barriers** instead of walking over them. They look 5 m ahead, sidestep at 3 m/s and keep 0.15 m clear. A barrier that spans the whole road can't be walked around, so enemies stop and batter it.
- **Full-width barriers** have 0.1 of the beat-box HP, so the squad can still shoot through them.
- **Ramp:** foe HP grows 1.112 a level and foe damage 1.067. Past the wall, wall HP grows 1.026 a level and the wall step is 1.35.

**Per-level tweaks (`progression.level_tweaks` in `runner_levels.json`, `RunnerLevelTweak`).** Each level can set `wave` (enemies per wave group), `foe_hp` (enemy HP), `barrier_hp` (barrier HP) and `layout` (re-roll the layout; see "Layout variety" below). The L19 / L22 / L26 wave and HP trims of this pass were replaced by layout re-rolls in the layout-variety pass. The "gets harder level by level" test skips the enemy-count and HP checks only for a level trimmed with `wave` or `foe_hp`.

**King's Treasury ($39.99):** War Banners 5 → **6**. With 5, a player stuck at the free wall cleared only 5 new levels (L31–L35), and the test asks for at least 6. With 6, they clear **11 (L31–L41)**. Value is 8,800 → 8,860 gem-eq (1.98× → 1.99× the $0.99 pack's value per $), and the label stays "+95%".

| Profile | Wall before → after | L35 | L40 | L45 | L50 first clear (day) before → after |
|---|---|---|---|---|---|
| free | L31 → **L31** | 75.3 → 51.3 | 122.3 → 152.7 | 233.3 → 282.0 | 701.3 → **831.7** |
| light | L33 → **L34** | 21.0 → 14.7 | 35.0 → 28.0 | 49.0 → 49.3 | 63.3 → **63.0** |
| mid | L35 → **L36** | 14.0 → 11.3 | 15.7 → 15.3 | 22.0 → 21.3 | 28.3 → **28.0** |
| whale | none → **none** | 11.3 → 11.3 | 13.0 → 13.0 | 14.7 → 14.7 | 16.3 → **16.3** |
| stuck_39 | L31 → **L31** | 10.0 → 10.0 | 28.7 → 10.0 | 157.0 → 197.3 | 624.7 → **747.0** |

Required power is now 0.83 on L12, 2.04 on L20, 4.21 on L25, 10.3 on L30, 14.9 on L31 (a 1.45 step), 22.5 on L40 and 35.7 on L50. The free player still clears L30 on day 9.7 and fails L31 (ratio 0.92).

## Level finale and finish gate on levels without a boss (Oct 27, 2026)

Derek: *"there's too much empty space at the end of the runner levels that don't have an larger enemy"*. Once bosses moved to every 5th level, the other levels kept the empty stretch where the boss section used to stand. The squad walked 11–20 s of empty road before the finish (L1 20 s, L2 16 s, L17+ about 13 s).

- **Finale** (`RunnerProgression.Finale`, rows flagged `Finale`), where the boss section used to be. Every generated level without a boss now ends like this:
  - a trap gauntlet: one trap on each edge, with the middle left open;
  - a fodder push down the middle: Nightfang, `3 + tc/3` (with the per-level wave tweak);
  - the archer line (the same line a boss level has as the boss's guard);
  - a captain heavy at the back: Blightroot, Ashwyrm from L7, and two captains from L20 (`FinaleSecondCaptainFrom`).
- **Finish gate** `FinishPad` = 3 m past the captain. The level is shortened to fit, and the length cap is unchanged:

  | Level | Before | After |
  |---|---|---|
  | L2 | 162 | 159 |
  | L9 | 246 | 244 |
  | L16 | 330 | 328 |
  | L17–49 | 330 | 330 |

  On L17–49 the finale fills the old dead stretch. The last row moved from 318 m to 326.5 m.
- **Gate charge** (`RunnerRules.GateChargeSeconds` = 1.2 s, level setting `gate_charge`):
  - What it does: on a level without a boss, once the last row has spawned and nothing is left ahead (no enemy, box, trap or barrier), the squad runs for the gate. It covers the rest in at most 1.2 s and never goes slower than the walk.
  - Where it is on: the campaign's first level turns it on in `runner_levels.json`, and the generated levels copy it. Test levels leave it off.
  - Effect: it only shortens a walk that has nothing left in it, so it changes no outcome.
- **Result:** at most 1.8 s of empty road before the finish on every level without a boss, even with a squad 4x the required power (L1 included). Boss levels are unchanged.
- **Balance:** the required power is unchanged on every level, and so are the walls: free L31, light L34, mid L36, whale none, stuck_39 L31. The largest level-to-level jump is still 1.56x. Shorter levels shift the first-clear days slightly, for example free L50 on day 831.7 → 830.0. The golden was regenerated.

## Enemy shots, denser traps, bosses every 5 levels, barriers (Oct 26, 2026)

Derek: *"the attacks from the enimies in the runner stage move too slowly once fired. make them about 25% faster. Also they don't start firing from as far away as the heroes can"*, *"the obstacles need to be more prevelant and do more damage. there should only be a a big boss every 5 levels or so"*, and *"there hsould be some obstacles that can get destroyed but do damage if you don't avoid them. maybe some type of gate or stone fence"*.

- **Enemy shots 25% faster:** Bonequill 12 → 15 m/s, Vespera 11 → 13.75 m/s. A shot's flight length now covers the range it was fired from.
- **Range parity:** a ranged enemy (not the boss) opens fire from its own range or the squad's longest current reach, whichever is longer. The squad's reach includes range crates (`RangeMul`), so the enemies' range grows with the heroes'. A ranged foe that a hero can hit can also shoot back.
- **Ranged enemy damage 15 → 5 (Bonequill) and 24 → 7 (Vespera).** With faster shots fired from ~1.5x as far, archers hit far more often, and on the old damage the free wall dropped well below L31. The new numbers keep a ranged foe's damage per fight about where it was.
- **Traps about 1.5–2x as often and about 1.75x the damage.**
  - Placement: every other beat on L1–4 and every beat from L5. A second trap goes on one beat in four from L13 and on every other beat from L22. Before, it was every third beat or every other beat.
  - Damage: spikes 70 + 5/level, rolling log 96 + 7/level, fire 52 + 5/level. Before, it was 40 + 3, 55 + 4 and 30 + 3.
  - A trap never sits in the lane of the box just before it, so every trap can still be dodged. The dodge sim agrees: steering costs 0.36 levels in 50, and not steering costs 10.
  - Per level (traps / mean damage), before → after: L1 3/37 → 5/63, L10 14/70 → 17/122, L20 22/105 → 28/180, L30 22/138 → 33/237, L50 22/204 → 33/350.
- **Big boss (Ironhowl) only on L5, 10, 15 … 50** (`boss_every: 5`): 10 boss levels instead of 50. The other levels end on the last wave and the finish line (since Oct 27 on a closing finale, see above). The boss bar and the quake only appear on boss levels. The boss TTK balance test now runs on boss levels only.
- **Breakable barriers** (`kind: "barrier"` rows):
  - Kinds: palisade gate, stone wall, spiked barricade.
  - Each has an always-visible HP bar. Squad shots chip it down until it breaks with debris.
  - Walking into one damages whoever touches it (the hero takes the full damage; a follower takes a trap share) and smashes it.
  - Placement: L2+ every third beat, every other beat from L15, every beat from L25. At 3 m past the beat's box.
    - Most barriers stand against the road edge away from that box and leave at least 2 m open (dodge or shoot).
    - From L6, every fourth barrier is full width (alternating palisade and stone wall), so it has to be shot down or walked through.
  - HP is a share of the beat's box HP, so it scales with the level: barricade 15%, palisade 22%, stone wall 30%, full width 20%.
  - Contact damage: palisade 50 + 4/level, stone wall 60 + 5/level, barricade 70 + 5/level.
  - Barrier counts: L5 4, L10 6, L15 10, L20 11, L25+ 21 (720 in all, 159 full width).
  - Tests check that at L1–30 the free player's required power shoots down every full-width barrier in time.
  - The art is placeholder primitives (`RunnerLevelView.Barriers.cs`). **Design should make the final art.**
  - Sound hooks (`RunnerLevelView.BarrierSound` event): `runner.barrier.hit`, `runner.barrier.break`, `runner.barrier.smash`.
- **Ramp past the wall:** HP 1.016 → 1.021 and damage 1.0105 → 1.0125 per level; boss HP is unchanged at 1.016. Fewer bosses made late levels shorter fights, and this keeps the free L50 grind near two years.

| Profile | Wall before → after | L35 | L40 | L45 | L50 first clear (day) before → after |
|---|---|---|---|---|---|
| free | L31 → **L31** | 28.7 → 75.3 | 195.3 → 122.3 | 238.0 → 233.3 | 566.3 → **701.3** |
| light | L33 → L33 | 14.7 → 21.0 | 28.0 → 35.0 | 43.0 → 49.0 | 63.3 → 63.3 |
| mid | L40 → L35 | 11.3 → 14.0 | 14.0 → 15.7 | 21.7 → 22.0 | 28.3 → 28.3 |
| whale | none → none | 11.3 → 11.3 | 13.0 → 13.0 | 14.7 → 14.7 | 16.3 → 16.3 |
| stuck_39 | L31 → L31 | 10.0 → 10.0 | 122.7 → 28.7 | 165.3 → 157.0 | 494.0 → 624.7 |

The "before" numbers come from the model on main just before this change. They differ slightly from the older tables above, which predate later tweaks.

**L1–30 stay easy:** the free player still clears L30 on day 9.7, on schedule. The required-power ratio is L12 6.3 → 7.6, L20 3.8 → 4.9, L25 3.0 → 2.6 and L30 1.34 → 1.15, and the free wall stays at L31 (ratio 0.96). The mid player's wall moved from L40 to L35, but mid still clears L50 on day 28.

## Power packs (the IAP ladder, added Oct 24, 2026)

Derek: *"there should be things that allow a substantial power up. like a 39.99 price. and everything in between"*.

The packs are the `store.power_packs` offers in `stores.json`. Their SKUs are in `iap.json`, and the store product IDs are in the theme's `products.json` (`com.paytoplay.fantasykingdoma.*`). Value rates and the War Banner boost are in `power_packs.json`. The code is `Assets/Survival/Runtime/Store/PowerPacks.cs` (`PowerPackCatalog`, `PowerPackShop`, `RunnerBanner`). **All contents and value rates are invented (`_invented`); Derek's call.**

| Offer | Price | Contents | Value* | Value / $ | Label |
|---|---|---|---|---|---|
| Starter (once per save) | $1.99 | 300 gems, 2 Hero Tomes, 30 min speed-up | 830 | 417 | +275% |
| Handful of Banners | $0.99 | 50 gems, 1 War Banner | 110 | 111 | — |
| Squire's Kit | $1.99 | 100 gems, 1 Banner, 15k gold, 60 min speed-up | 250 | 126 | +10% |
| Knight's Satchel | $4.99 | 2 Tomes, 1 Banner, 60 min, 25k gold, 10k food | 710 | 142 | +25% |
| Captain's Chest (popular) | $9.99 | 3 Tomes, 2 Banners, 5 h, 100k gold, 50k food | 1,570 | 157 | +40% |
| Warlord's Hoard | $19.99 | 6 Tomes, 3 Banners, 16 h, 300k gold, 100k food | 3,640 | 182 | +60% |
| King's Treasury (best value) | $39.99 | 10 Tomes, 6 Banners, 33 h 20 min, 1M gold, 500k food | 8,860 | 222 | +95% (1.99×) |

\*Value is in gems: 1 gem = 1 speed-up minute = 500 gold = 250 food, a Hero Tome = 250 and a War Banner = 60. The "+X%" label is value per $ against the $0.99 pack, rounded down. Value per $ rises with every tier (tested).

- **Hero Tome:** one instant hero level, still capped by stars. It also counts a level that is in training.
- **War Banner:** one charge gives ×1.1 damage and ×1.1 HP to the whole squad. A charge is spent on each **first clear** (every win is a first clear now); losses are free.
- **Gold and food** go 100% to heroes.
- **UI:** a **POWER UP** sheet on the Heroes screen lists the ladder. Hero detail has a **Tome (n)** button. Purchases go through the existing store purchase path (`StoreSystem.BuySku`, sandbox IAP), and **no real billing is wired up**.

## Results

| ratio on schedule | L12 | L20 | L25 | L30 | **L31** | L35 | L40 | L50 |
|---|---|---|---|---|---|---|---|---|
| free | 6.3 | 5.0 | 2.8 | 1.43 | **0.91** | 0.76 | 0.75 | 0.52 |
| light | 6.9 | 5.0 | 3.3 | 1.57 | 1.01 | 0.84 | 0.79 | 0.60 |
| mid | 8.9 | 5.9 | 4.5 | 2.15 | 1.38 | 1.09 | 1.03 | 0.87 |
| whale | 27.0 | 11.4 | 10.6 | 4.18 | 2.68 | 3.42 | 2.67 | 2.26 |
| stuck_39 | 6.3 | 5.0 | 2.8 | 1.43 | 0.91 | 1.28 | 1.22 | 0.80 |

**Walls and first clears:**

| Profile (invented) | Buys | Wall | L35 | L40 | L45 | L50 | Spent by the L50 clear |
|---|---|---|---|---|---|---|---|
| free | nothing | **L31** | 65 | 145 | 280 | **703** (≈ 23 months) | $0 |
| light | starter + one $4.99 a week | **L34** | 18 | 29 | 50 | **64** | ≈ $52 |
| mid | starter + one $19.99 a week | **L38** | 12 | 15 | 22 | **29** | ≈ $102 |
| whale | starter + two $39.99 a week | **none** | 12 | 13 | 15 | **17** (on schedule) | ≈ $202 |
| stuck_39 | free until stuck, then ONE $39.99 | L31 (at pace) | 10 | 32 | 182 | **606** | $39.99 |

**What $39.99 buys a stuck player:** the free player fails L31 in session 31 (day 10). Buying one King's Treasury there clears **L31–L38 in that same session (8 new levels)**. (Since Oct 27, with side hits and the 6th Banner: **L31–L41, 11 new levels**; see the Oct 27 section.) In that run, the tomes take lyra 12→18 and rowan 12→19, and the banners carry L31–35. Before the no-replay change, L40 then came on day 26; with no replays and the Gold Mine it comes on day 32.

- **Free power over time:** 13.7 on day 10, 18.3 on day 30, 23.5 on day 90 and 31.5 on day 365. The free curve did not move with the packs.
- **Free roster:** lyra and rowan reach 5★ around day 5–7. Their levels are then food-bound at 12–13 by L30, and the free player creeps up roughly one level per month.

Chart: `balance_curve.png` (kept outside the repo; regenerate it from the CSV) shows all five profiles and marks the $39.99 purchase. Regenerated with the Gold Mine on Oct 24, 2026.

## No replays (Oct 24, 2026)

Derek: "once you beat a runner level there should be no going back". A beaten level shows as completed in the level list and can never be started again; only the next unbeaten level has a play button. A loss can be retried. The model lost its 1 replay per session.

- **Walls did not move:** free L31, light L34, mid L38, whale none, stuck_39 L31.
- **L1–30 stay comfortable:** the free player still clears L30 on day 9.7, on schedule. The margins are lower early on (L12 ratio 8.8 → 6.3, L20 6.1 → 5.0) but L25–30 are unchanged (2.8 / 1.42), so first-clear rewards were not retuned.
- **Payers barely moved:** light, mid and whale reach L50 on the same days (64 / 29 / 17).
- **Free and stuck players now stop for good.** Before, replay gold was the free player's only real gold income after the wall (the farms pay food). Without it the free player stops at **L33** (was L50 on day 583) and stuck_39 at **L38** (was L50 on day 493). They pile up food, but gold sits at about 150k against a 364k level-up. Derek chose more village gold: see the Gold Mine below.

## Gold Mine (Oct 24, 2026)

Derek: "add building to the village which grant gold on a basis like the food. the building can be upgraded to produce more gold over time".

The Gold Mine is a Build-menu village building (`base_economy.json` buildable `"mine"`, `building.economy`, produces `res.soft`). It works exactly like a farm, using the same code:
- it digs gold up to its cap, and a collect bubble shows when it's ready; the player taps it to collect;
- build and upgrade timers finish on their own, and the congratulations bubble pays one hour of output;
- upgrades can't pass the Castle level;
- it uses the same build spots and auto-placement as other buildings;
- you get one mine, unlocked at Castle 2, together with the first farm.

The name ("Gold Mine"), description and art come from the ThemePack (`build.type.mine`, `build.desc.mine`, `theme_a.hook.build.mine`). Uncollected stock (gold, and farm food too) now survives a save and keeps filling while the game is closed, up to the cap.

**Art placeholder (for Design):** `theme_a.hook.build.mine` reuses the world-map Gold Mine node (`art/world/map/node_gold.png`) until a painted village building exists.

| Level | Gold / h | Cap (6 h) | Upgrade to next: gold | wood | timer |
|---|---|---|---|---|---|
| build | — | — | 1,000 | 800 | 2 min |
| 1 | 80 | 480 | 1,000 | 300 | 5 min |
| 2 | 120 | 720 | 2,200 | 660 | 10 min |
| 3 | 180 | 1,080 | 4,840 | 1,450 | 18 min |
| 4 | 270 | 1,620 | 10,600 | 3,200 | 34 min |
| 5 | 400 | 2,400 | 23,400 | 7,000 | 1 h 5 min |
| 6 | 610 | 3,660 | 51,500 | 15,500 | 2 h 4 min |
| 7 | 910 | 5,460 | 113,000 | 34,000 | 3 h 55 min |
| 8 | 1,370 | 8,220 | 249,000 | 75,000 | 7 h 27 min |
| 9 | 2,050 | 12,300 | 549,000 | 165,000 | 14 h 9 min |
| 10 | 3,080 | 18,480 | max | | |

Rate ×1.5 per level, and the cap holds 6 hours, so 4 collects a day take it all (L10 ≈ 74k gold/day). Gold cost ×2.2 and timer ×1.9 per level, close to the farm's curve style. Level *n* needs Castle *n*.

**Balance (model: 1 mine, greedy build/upgrade from the 25% building share, collected 4×/day):**
- The free player has mine L5+ by day 10 and maxes it (L10) later on, gated by the Castle. From day 180 to 365 the mine is more than half of his gold.
- At-pace ratios and walls are unchanged (free L31, light L34, mid L38, whale none, stuck_39 L31). L1–30 are still on schedule (L30 on day 9.7).
- Payers are unchanged (light, mid and whale reach L50 on days 63 / 28 / 16).

| Profile | Wall | L35 | L40 | L45 | L50 (before → after) |
|---|---|---|---|---|---|
| free | L31 | never → 65 | never → 145 | never → 280 | never (stopped at L33) → **703** |
| stuck_39 | L31 | 10 | never → 32 | never → 182 | never (stopped at L38) → **606** |
| light / mid / whale | L34 / L38 / none | unchanged | unchanged | unchanged | 63 / 28 / 16 (unchanged) |

- **Model limits:** the model counts the mine's and the Castle's gold only, not their wood/stone (wood cost is kept low, 30% of the gold). Every 4 h collect is assumed. The rates are invented (`_invented`), so they're Derek's call. A base of 60 gold/h instead of 80 would give free L40 about day 190 and L50 about day 930.
- **Tests:** `GoldMineTests.cs` covers production up to the cap, collect granting gold, an upgrade raising rate and cap and finishing on its own (and blocked past the Castle), save/load of level and stored gold (and offline fill), the ThemePack name/art, and the balance bands: free wall L30–33, free L30 by day 11, free L40 on days 90–240, free L50 on days 500–1000, the mine maxed, and payers no slower.

## Invented numbers (need Derek's call)

These are in `runner_balance.json`, `power_packs.json` and `stores.json`.

- **Pace:** 3 sessions/day; each session tries 1 new level. No replays (beaten levels are locked).
- **Village:** 3 farms at level 4, collected 4×/day; 1 Gold Mine (`gold_mines`) built and upgraded greedily, collected 4×/day; 4 of the 6 daily gifts opened; 75% of farm/run/mine gold goes to heroes, and the other 25% pays for the mine and the Castle.
- **Pack contents, value rates, the War Banner ×1.1/×1.1 and the "+X%" labels:** all invented (see the table above).
- **Profiles:** how often each payer buys (weekly buys are spread across the week; the starter is bought in session 1). The stuck player buys the moment they first fail a level ≥ 31, then keeps playing until they lose.
- **Greedy cost:** food counts as 2 gold.
- **Test margin:** the play tests run with a "trained squad" of 1.3 × required scale from L13 on.

**Known limits:**
- Per-level layout noise is about ±15–30% (L29/L32/L33 bump).
- The model never levels oakenshield for a free player, because the DPS weighting favours lyra and rowan. Tomes go greedily to the hero with the best power gain.
- The analytic kill-time passes overrun-time around L25 and the player still wins; boxes, followers and dodging absorb it.
- The model is not verified in Unity; it is the domain sim only.

## The tests (`tests/Survival.Domain.Tests/RunnerBalanceTests.cs`)

- `The_first_12_levels_still_need_no_upgrades` checks that L1–12 need scale ≤ 1.
- `A_free_player_who_upgrades_clears_levels_1_to_30_on_schedule_with_margin` checks:
  - ratio ≥ 1.25 through L25 and ≥ 1.15 on L26–30;
  - the sim wins with ≥ 50% hero HP;
  - pressure rises (L26–30 average below half of the L1–10 average).
- `The_free_player_hits_the_wall_at_31_and_falls_further_behind_through_50` checks:
  - the wall is in L30–33;
  - every one of L31–50 is below 1;
  - at least 18 of 20 sims are lost;
  - the gap widens;
  - L50 ratio is 0.3–0.7;
  - L40 first clears after day 60 and L50 after a year (with the Gold Mine: days 145 and 703).
- `Paying_moves_the_wall_and_gets_through_the_last_20_much_sooner` checks:
  - free ≥ light ≥ mid ≥ whale first-clear days on every level;
  - weekly spend is light $4.99, mid $19.99 and whale ≥ $79.98;
  - the light wall is 2–5 levels past free, mid is 2+ past light (≤ L45), and whale has no wall;
  - the whale clears L50 within 30 days (20× sooner than free), mid within 60, and light within 120 (4× sooner than free).
- `PowerPackTests.cs` checks:
  - the ladder ($0.99–$39.99 plus the starter) and its product IDs;
  - value per $ is strictly rising, the $39.99 pack is 1.9–2.1× the $0.99, and the labels match;
  - every pack grants exactly its contents, and a failed purchase grants nothing;
  - the starter can be bought once and that survives a save round-trip;
  - the Tome rules (star cap, training) and the Banner boost/spend;
  - a stuck free player who buys one $39.99 clears **at least 6 new levels**.
- `Kill_time_against_overrun_time_tracks_the_pressure` and `No_level_needs_much_more_than_the_one_before` (≤ 1.6×).
- **Golden snapshot** `The_curve_matches_the_golden_snapshot` compares against `tests/Survival.Domain.Tests/Golden/runner_balance.json`. It fails when:
  - any level's ratio or required power moves more than **10%**;
  - any profile's wall moves more than **2 levels**;
  - the L50 first-clear day moves more than 25%.

  The failure message is a readable per-level diff (old → new, % change) that also lists the biggest within-tolerance moves. Sensitivity tests confirm that lyra damage ×1.3 and halving the hero gold share both trip it.
- `Writes_the_balance_table_and_csv` prints the full table and writes `runner_balance.csv` next to the test binaries (`tests/Survival.Domain.Tests/bin/Debug/net8.0/`).

### Regenerating the golden (after an intended change)

```
UPDATE_GOLDEN=1 dotnet test tests/Survival.Domain.Tests --filter RunnerBalance
```

Commit the updated `Golden/runner_balance.json` along with the change, and read the diff in the PR.

**Last intentional regen (Oct 27, 2026, layout variety):** walls unchanged (free L31, light L34, mid L36, whale none, stuck_39 L31). Free L50 day 830 → 564, stuck_39 745 → 486, light / mid / whale unchanged. See "Layout variety" above.

**Previous regen (Oct 27, 2026, side hits, enemies avoid obstacles, L19/L22/L26 tweaks, 6th Banner in the $39.99 pack):** walls free L31, light L33 → L34, mid L35 → L36, whale none, stuck_39 L31. Free L50 day 701 → 832, mid 28 → 28. See the Oct 27 section above.

**Previous regen (Oct 26, 2026, enemy shots, traps, bosses every 5, barriers):** walls free L31, light L33, mid L40 → L35, whale none, stuck_39 L31. Free L50 day 566 → 701. See the Oct 26 section above.

**Previous regen (Oct 24, 2026, power packs):** the free curve is unchanged (no `free` line in the diff). Light/whale moved because the gem crate was replaced by the pack ladder (whale wall L39 → none, light L50 day 358 → 64), and the `mid` and `stuck_39` profiles are new. If a band test (wall at L30–33 etc.) fails, the change broke the design itself, not just the snapshot: retune `ramp` in runner_levels.json instead.

To see the table: `dotnet test tests/Survival.Domain.Tests --filter Writes_the_balance_table --logger "console;verbosity=detailed"`.
