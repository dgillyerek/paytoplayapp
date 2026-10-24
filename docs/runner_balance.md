# Runner balance: 50 levels

**Goal (Derek):** a free player with normal upgrades gets through levels 1–30 fairly easily. Levels 31–50 are close to impossible without buying upgrades. Any change to a baseline (units, power-ups, upgrades, starting damage …) that moves the curve noticeably should fail a test.

**Where it stands:** the free player hits the **wall at level 31**. Levels 1–12 play the same as before.

## How the math works

1. **What a level asks for (measured, not invented).** For each level, the test plays the real `RunnerRun` sim with a dodging player (0.3 s reaction, 1/30 s steps). It scales every squad unit's damage and HP by one number *s* and bisects for the smallest *s* that still wins (range 1/8–64, 12 halvings).
   - **Required power = s²** (damage × HP, a Lanchester-style "power").
   - Required power runs ≈0.5 on L1, 0.96 on L12, 2.0 on L20, 4.6 on L25 and 9.6 on L30. It jumps to **15.0 on L31**, then reaches 20 on L40 and 30.5 on L50.
2. **What a player has.** Power = (DPS-weighted damage multiplier) × (HP-weighted HP multiplier) over the level's full squad (lead plus follower chests). A hero's multiplier is `HeroStats(level, stars) / HeroStats(1, 1)` from campaign.json.
3. **Ratio = player power / required power** at the nominal pace (level *n* is reached in session *n*, i.e. day (n−1)/3). Ratio ≥ 1 means the level clears. The **wall** is the first level where the free player's ratio drops below 1.
4. **The economy (`RunnerBalance` / `RunnerEconomy`).** It is a deterministic session-by-session timeline from a fresh save. Each session it:
   - adds farm and gift income, the one-time campaign, bounty and quest rewards, and the payer's weekly gem pack;
   - ticks trainings on 2 slots, using the real level-up gold, food and timers (30·1.6^L s);
   - applies star-ups and unlocks from shards;
   - buys greedy level-ups (best Δpower per gold + 2·food);
   - uses gems for rushes (1 gem/min) and buys crates (payers only);
   - attempts the next level once and replays the best cleared level once.

   Runner win gold uses the real reward formula.
5. **The level ramp (runner_levels.json `ramp`).**
   - Past L12, levels stop adding length past 330 m and stop adding enemies or boxes past 20 rows (`count_cap`).
   - Each level multiplies enemy HP by 1.115 and enemy damage by 1.072 (boss HP 1.115).
   - From L31 on there is a one-time **step of +30% HP / +12% damage**, followed by a gentler 1.016 / 1.008 per level.
   - The result is a ramp, not a cliff: no level needs more than 1.6× the level before (tested).

## Results (free = no money)

| | L12 | L20 | L25 | L30 | **L31** | L35 | L40 | L50 |
|---|---|---|---|---|---|---|---|---|
| free ratio on schedule | 8.8 | 6.1 | 2.8 | 1.43 | **0.91** | 0.76 | 0.75 | 0.52 |
| light ratio | 11.1 | 7.5 | 3.6 | 1.73 | 1.11 | 0.88 | 0.87 | 0.60 |
| whale ratio | 14.9 | 9.5 | 4.7 | 2.35 | 1.51 | 1.23 | 1.17 | 0.83 |

**First clear (day):**

| Profile | L31 | L35 | L40 | L45 | L50 |
|---|---|---|---|---|---|
| free | 13 | 49 | 117 | 237 | **583** |
| light ($4.99/wk) | 10 | 28 | 70 | 143 | **358** |
| whale ($49.99/wk) | 10 | 11 | 16 | 38 | **95** (~$700 spent) |

- **Walls:** free L31, light L34, whale L39.
- **Free power over time:** 13.7 on day 10, 18.3 on day 30, 23.5 on day 90 and 31.5 on day 365.
- **Free roster:** lyra and rowan reach 5★ around day 5–7. Their levels are then food-bound at 12–13 by L30, and the free player creeps up roughly one level per month.

Chart: `balance_curve.png` (kept outside the repo; regenerate it from the CSV, see below).

## Invented numbers (need Derek's call) — all in `Assets/Survival/Resources/Survival/runner_balance.json`

- **Pace:** 3 sessions/day; each session plays 1 new level and 1 replay.
- **Village:** 3 farms at level 4, collected 4×/day; 4 of the 6 daily gifts opened; 75% of gold goes to heroes.
- **Gem resource crate: 100 gems → 30,000 gold + 15,000 food. THIS OFFER DOES NOT EXIST IN THE GAME TODAY.**
  - Gems currently only buy rush time (1 gem/min) and hero unlocks.
  - Without some gold/food offer, money barely helps past the wall, and the wall applies to everyone.
- **Spend tiers:** light = one $4.99 pack (500 gems) a week; whale = ten a week.
- **Greedy cost:** food counts as 2 gold.
- **Test margin:** the play tests run with a "trained squad" of 1.3 × required scale from L13 on.

**Known limits:**
- Even a whale can't clear L45–50 on schedule; whale leverage is capped by training timers, the rush rate and exponential gold costs.
- Per-level layout noise is about ±15–30% (L29/L32/L33 bump).
- The model never levels oakenshield for a free player, because the DPS weighting favours lyra and rowan.
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
  - L40 first clears after day 60 and L50 after a year.
- `Paying_gets_through_the_last_20_much_sooner` checks:
  - light clears every level no later than free, and whale no later than light;
  - the whale wall is more than 3 levels past the free wall;
  - the whale clears L50 within 120 days and 4× sooner than free.
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

Commit the updated `Golden/runner_balance.json` along with the change, and read the diff in the PR. If a band test (wall at L30–33 etc.) fails, the change broke the design itself, not just the snapshot: retune `ramp` in runner_levels.json instead.

To see the table: `dotnet test tests/Survival.Domain.Tests --filter Writes_the_balance_table --logger "console;verbosity=detailed"`.
