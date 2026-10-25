# Runner balance: 50 levels

**Goal (Derek):** a free player with normal upgrades gets through levels 1–30 fairly easily. Levels 31–50 are close to impossible without buying upgrades. Any change to a baseline (units, power-ups, upgrades, starting damage …) that moves the curve noticeably should fail a test.

**Where it stands:** the free player hits the **wall at level 31**. Levels 1–12 play the same as before. Since the Gold Mine (below), a free player is no longer stuck for good: L40 takes about 5 months and L50 about 2 years.

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
   - From L31 on there is a one-time **step of +30% HP / +12% damage**, followed by a gentler 1.016 / 1.008 per level.
   - The result is a ramp, not a cliff: no level needs more than 1.6× the level before (tested).

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
| King's Treasury (best value) | $39.99 | 10 Tomes, 5 Banners, 33 h 20 min, 1M gold, 500k food | 8,800 | 220 | +95% (1.98×) |

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

**What $39.99 buys a stuck player:** the free player fails L31 in session 31 (day 10). Buying one King's Treasury there clears **L31–L38 in that same session (8 new levels)**. In that run, the tomes take lyra 12→18 and rowan 12→19, and the banners carry L31–35. Before the no-replay change, L40 then came on day 26; with no replays and the Gold Mine it comes on day 32.

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

**Last intentional regen (Oct 24, 2026, power packs):** the free curve is unchanged (no `free` line in the diff). Light/whale moved because the gem crate was replaced by the pack ladder (whale wall L39 → none, light L50 day 358 → 64), and the `mid` and `stuck_39` profiles are new. If a band test (wall at L30–33 etc.) fails, the change broke the design itself, not just the snapshot: retune `ramp` in runner_levels.json instead.

To see the table: `dotnet test tests/Survival.Domain.Tests --filter Writes_the_balance_table --logger "console;verbosity=detailed"`.
