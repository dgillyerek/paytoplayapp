#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using Survival.Domain.Serialization;

namespace Survival.Domain.Runner
{
    /// <summary>
    /// runner_levels.json "progression": how many levels the runner has, the seed that lays out levels 2+, and how
    /// fast each step gets harder. Steps are per level after the first (Level n gets 1 + step * (n - 1)).
    /// </summary>
    public sealed record RunnerProgressionDef(
        int Count,
        int Seed,
        float LengthStep,
        float SpeedStep,
        float FoeHpStep,
        float BossHpStep,
        float BoxHpStep,
        float RewardStep,
        IReadOnlyList<string> Names)
    {
        internal static RunnerProgressionDef Read(JsonNode.Obj o)
        {
            var names = new List<string>();
            if (o.TryGet("names", out var nn) && nn is JsonNode.Arr arr)
            {
                foreach (var n in arr.Items)
                {
                    if (n is JsonNode.Str s)
                    {
                        names.Add(s.Value);
                    }
                }
            }

            return new RunnerProgressionDef(
                Math.Max(1, LayoutJson.OptInt(o, "count", 1)),
                LayoutJson.OptInt(o, "seed", 1),
                Math.Max(0f, LayoutJson.OptNum(o, "length_step", 12f)),
                Math.Max(0f, LayoutJson.OptNum(o, "speed_step", 0.02f)),
                Math.Max(0f, LayoutJson.OptNum(o, "foe_hp_step", 0.12f)),
                Math.Max(0f, LayoutJson.OptNum(o, "boss_hp_step", 0.2f)),
                Math.Max(0f, LayoutJson.OptNum(o, "box_hp_step", 0.1f)),
                Math.Max(0f, LayoutJson.OptNum(o, "reward_step", 0.3f)),
                names)
            {
                Ramp = o.TryGet("ramp", out var rn) && rn is JsonNode.Obj ro ? RunnerRampDef.Read(ro) : null,
                BossEvery = Math.Max(1, LayoutJson.OptInt(o, "boss_every", 1)),
                Tweaks = RunnerLevelTweak.ReadAll(o)
            };
        }

        /// <summary>Per-level hand adjustments ("level_tweaks"), by level number; levels not listed use <see cref="RunnerLevelTweak.None"/>.</summary>
        public IReadOnlyDictionary<int, RunnerLevelTweak> Tweaks { get; init; } = new Dictionary<int, RunnerLevelTweak>();

        /// <summary>The hand adjustment for Level <paramref name="number"/> (all 1.0 when none is set).</summary>
        public RunnerLevelTweak Tweak(int number) => Tweaks.TryGetValue(number, out var tw) ? tw : RunnerLevelTweak.None;

        /// <summary>The late-game ramp (levels past <see cref="RunnerRampDef.From"/>), or null for the plain linear steps.</summary>
        public RunnerRampDef? Ramp { get; init; }

        /// <summary>
        /// A big boss stands at the end of every <see cref="BossEvery"/>th level only (Derek 2026-10-26: "there should only
        /// be a big boss every 5 levels or so"): Levels 5, 10, 15, ... The other levels end on a last wave and the finish line.
        /// </summary>
        public int BossEvery { get; init; } = 1;

        /// <summary>Whether Level <paramref name="number"/> ends on the boss.</summary>
        public bool IsBossLevel(int number) => BossEvery <= 1 || (number >= 1 && number % BossEvery == 0);

        /// <summary>The t (level - 1) the enemy and box counts use: it stops growing at the ramp's count cap.</summary>
        public int CountT(int number) => Math.Max(0, Math.Min(number, Ramp?.CountCap ?? int.MaxValue) - 1);
    }

    /// <summary>
    /// runner_levels.json "progression" "level_tweaks": a small hand adjustment to one generated level, keyed by its number
    /// ("19": { "wave": 0.85, "foe_hp": 0.9, "barrier_hp": 0.7 }). Everything defaults to 1.0. The layout (where every row
    /// stands, which units, traps and barriers) stays the seed's; "wave" scales how many enemies each wave group has
    /// (never below one), "foe_hp" the level's enemy HP and "barrier_hp" its barriers' HP. Used only to smooth a level the
    /// seed made much harder than the one before it (RunnerBalanceTests.No_level_needs_much_more_than_the_one_before).
    /// </summary>
    public sealed record RunnerLevelTweak(float Wave = 1f, float FoeHp = 1f, float BarrierHp = 1f)
    {
        public static readonly RunnerLevelTweak None = new();

        internal static Dictionary<int, RunnerLevelTweak> ReadAll(JsonNode.Obj progression)
        {
            var all = new Dictionary<int, RunnerLevelTweak>();
            if (!progression.TryGet("level_tweaks", out var tn) || tn is not JsonNode.Obj t)
            {
                return all;
            }

            foreach (var key in t.Fields.Keys)
            {
                if (key.StartsWith("_", StringComparison.Ordinal) || !int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                {
                    continue;
                }

                if (t.TryGet(key, out var v) && v is JsonNode.Obj o)
                {
                    all[n] = new RunnerLevelTweak(
                        Math.Max(0.1f, LayoutJson.OptNum(o, "wave", 1f)),
                        Math.Max(0.1f, LayoutJson.OptNum(o, "foe_hp", 1f)),
                        Math.Max(0.1f, LayoutJson.OptNum(o, "barrier_hp", 1f)));
                }
            }

            return all;
        }
    }

    /// <summary>
    /// runner_levels.json "progression" "ramp": how levels past <see cref="From"/> keep getting harder once the road stops
    /// getting longer. Each level past From multiplies enemy HP, enemy damage and boss HP by the growth; each level past
    /// <see cref="WallFrom"/> by the (steeper) wall growth instead. That wall is what a free player's trained heroes stop
    /// keeping up with (RunnerBalance; tests/Survival.Domain.Tests/RunnerBalanceTests.cs).
    /// </summary>
    public sealed record RunnerRampDef(
        int From,
        int WallFrom,
        int CountCap,
        float LengthCap,
        float SpeedCap,
        float FoeHpGrowth,
        float FoeDmgGrowth,
        float BossHpGrowth,
        float WallHpGrowth,
        float WallDmgGrowth,
        float WallBossGrowth,
        float WallStepHp = 1f,
        float WallStepDmg = 1f)
    {
        internal static RunnerRampDef Read(JsonNode.Obj o) => new(
            Math.Max(1, LayoutJson.OptInt(o, "from", 12)),
            Math.Max(1, LayoutJson.OptInt(o, "wall_from", 30)),
            Math.Max(1, LayoutJson.OptInt(o, "count_cap", 20)),
            Math.Max(10f, LayoutJson.OptNum(o, "length_cap", 330f)),
            Math.Max(0.5f, LayoutJson.OptNum(o, "speed_cap", 2.3f)),
            Math.Max(1f, LayoutJson.OptNum(o, "foe_hp_growth", 1f)),
            Math.Max(1f, LayoutJson.OptNum(o, "foe_dmg_growth", 1f)),
            Math.Max(1f, LayoutJson.OptNum(o, "boss_hp_growth", 1f)),
            Math.Max(1f, LayoutJson.OptNum(o, "wall_hp_growth", 1f)),
            Math.Max(1f, LayoutJson.OptNum(o, "wall_dmg_growth", 1f)),
            Math.Max(1f, LayoutJson.OptNum(o, "wall_boss_growth", 1f)),
            Math.Max(1f, LayoutJson.OptNum(o, "wall_step_hp", 1f)),
            Math.Max(1f, LayoutJson.OptNum(o, "wall_step_dmg", 1f)));

        /// <summary>
        /// Product of the per-level growths from From + 1 to <paramref name="number"/> (1 up to From), times the one-time
        /// <paramref name="step"/> from the first wall level on.
        /// </summary>
        public float Mul(int number, float growth, float wallGrowth, float step = 1f)
        {
            var m = 1.0;
            for (var n = From + 1; n <= number; n++)
            {
                m *= n > WallFrom ? wallGrowth : growth;
                if (n == WallFrom + 1)
                {
                    m *= step;
                }
            }

            return (float)m;
        }

        public float FoeHpMul(int number) => Mul(number, FoeHpGrowth, WallHpGrowth, WallStepHp);

        public float FoeDmgMul(int number) => Mul(number, FoeDmgGrowth, WallDmgGrowth, WallStepDmg);

        public float BossHpMul(int number) => Mul(number, BossHpGrowth, WallBossGrowth, WallStepHp);
    }

    /// <summary>
    /// Lays out runner levels from Level 1 and a seed (Level 1's own rows in the JSON were laid out by <see cref="Rows"/>
    /// with number 1, then left editable). Every 13 m beat has a follower chest on one side and a power-up crate on the
    /// other (damage, fire rate, range, heal, or a small extra-follower chest), a mixed wave after it (a fodder front
    /// line, a different second line, sometimes a heavy anchor), skirmishers in between, and from the second beat on
    /// lane traps (spikes, then fire, then rolling logs) that get more common with the level. Vespera's casters join
    /// from Level 4, Ashwyrm from Level 7. Deterministic: the same seed and number always give the same level.
    /// </summary>
    public static class RunnerProgression
    {
        /// <summary>A level without a boss ends this many metres past its last row (the finale's captain): the finish gate.</summary>
        public const float FinishPad = 3f;

        /// <summary>The finale (no-boss levels) starts this far before where the boss would stand (the old archer line's spot).</summary>
        public const float FinaleBack = 7f;

        /// <summary>From this level on the finale has two captains.</summary>
        public const int FinaleSecondCaptainFrom = 20;

        /// <summary>Metres from one beat (boxes, then a wave) to the next.</summary>
        public const float BeatMetres = 13f;

        /// <summary>Most enemies standing side by side in one row; bigger groups form ranks behind.</summary>
        public const int MaxPerRank = 5;

        /// <summary>How far past the first follower chest the opening wave stands (later waves stand 8 m past theirs).</summary>
        public const float FirstWaveMetres = 12f;

        /// <summary>A second trap on one beat in four from <see cref="SecondTrapFrom"/>, on every other beat from <see cref="SecondTrapEveryOtherFrom"/>.</summary>
        public const int SecondTrapFrom = 13, SecondTrapEveryOtherFrom = 22;

        /// <summary>
        /// Breakable barriers (Derek 2026-10-26): from Level 2 on every third beat, every other beat from Level 15, every beat
        /// from Level 25. Most stand against the edge of the road away from the beat's box (dodge or shoot), leaving at least
        /// 2 m open; from Level 6 every fourth one is a
        /// full-width palisade gate or stone wall that has to be shot down (or walked through, at its damage). HP is a share
        /// of the beat's box HP (so it scales with the level); damage grows a step a level like the traps'.
        /// </summary>
        public const int BarrierFrom = 2, BarrierEveryOtherFrom = 15, BarrierEveryBeatFrom = 25, FullWidthBarrierFrom = 6, FullWidthEvery = 4;

        public const int PalisadeDamage = 50, PalisadeDamageStep = 4, StoneWallDamage = 60, StoneWallDamageStep = 5, BarricadeDamage = 70, BarricadeDamageStep = 5;

        /// <summary>Barrier HP as a share of the beat's box HP: a barricade breaks fast, a stone wall slowly; a full-width one at <see cref="FullWidthHpShare"/>.</summary>
        public const float BarricadeHpShare = 0.15f, PalisadeHpShare = 0.22f, StoneWallHpShare = 0.3f, FullWidthHpShare = 0.1f;

        /// <summary>Metres past the beat's chest a barrier stands (behind the crate, before the skirmishers).</summary>
        public const float BarrierOffset = 3f;

        /// <summary>Trap damage on Level 1 and per level after (about 1.75x the old 40 + 3, 55 + 4 and 30 + 3: Derek 2026-10-26).</summary>
        public const int SpikeDamage = 70, SpikeDamageStep = 5, LogDamage = 96, LogDamageStep = 7, FireDamage = 52, FireDamageStep = 5;

        private static readonly string[] FollowerUnits = { "rowan", "lyra", "rowan", "oakenshield", "lyra", "oakenshield" };
        private static readonly string[] ShardHeroes = { "hero.lyra", "hero.rowan", "hero.oakenshield", "hero.sir_aldric" };
        private static readonly RunnerBoxKind[] PowerUps = { RunnerBoxKind.Weapon, RunnerBoxKind.Rate, RunnerBoxKind.Range, RunnerBoxKind.Followers, RunnerBoxKind.Heal, RunnerBoxKind.Weapon, RunnerBoxKind.Rate, RunnerBoxKind.Followers };

        public static RunnerLevelDef Generate(RunnerLevelDef first, RunnerProgressionDef p, int number, Func<string, bool> hasUnit)
        {
            if (number < 2)
            {
                return first;
            }

            var t = number - 1;
            var ramp = p.Ramp;
            var length = (float)Math.Round(Math.Min(first.Length + (p.LengthStep * t), ramp?.LengthCap ?? float.MaxValue));
            var rows = Rows(p, number, length, first.HalfWidth, p.IsBossLevel(number) ? BossUnit(first) : null, hasUnit);
            if (!p.IsBossLevel(number))
            {
                // No boss: the finish gate stands just past the finale, not where the boss section would end.
                length = Math.Min(length, (float)Math.Ceiling(rows[rows.Count - 1].At + FinishPad));
            }

            var r = first.Rewards;
            var pay = 1f + (p.RewardStep * t);
            var rewards = new RunnerRewardDef(
                (int)Math.Round(r.WinGold * pay),
                (int)Math.Round(r.GoldPerKill * pay),
                (int)Math.Round(r.GoldPerBox * pay),
                (int)Math.Round(r.Shards * (1f + (0.5f * p.RewardStep * t))),
                ShardHeroes[(number - 1) % ShardHeroes.Length]);
            var slug = "road_" + number.ToString("00", CultureInfo.InvariantCulture);
            return new RunnerLevelDef(
                "runner." + slug,
                "runner.level." + slug,
                first.Hero,
                length,
                Math.Min(first.Speed + (p.SpeedStep * t), Math.Max(first.Speed, ramp?.SpeedCap ?? float.MaxValue)),
                first.HalfWidth,
                first.MaxFollowers,
                rows,
                rewards)
            {
                Number = number,
                DisplayName = number - 1 < p.Names.Count ? p.Names[number - 1] : "Road " + number.ToString(CultureInfo.InvariantCulture),
                FoeHpMul = first.FoeHpMul * (1f + (p.FoeHpStep * t)) * (ramp?.FoeHpMul(number) ?? 1f) * p.Tweak(number).FoeHp,
                BossHpMul = first.BossHpMul * (1f + (p.BossHpStep * t)) * (ramp?.BossHpMul(number) ?? 1f),
                FoeDmgMul = first.FoeDmgMul * (ramp?.FoeDmgMul(number) ?? 1f),
                GateCharge = first.GateCharge
            };
        }

        /// <summary>
        /// The spawn rows of level <paramref name="number"/> on a <paramref name="length"/> m road: the boss row last when
        /// <paramref name="bossUnit"/> is set, otherwise the road ends on the last wave and the finish line.
        /// </summary>
        public static List<RunnerRowDef> Rows(RunnerProgressionDef p, int number, float length, float halfWidth, string? bossUnit, Func<string, bool> hasUnit)
        {
            var t = Math.Max(0, number - 1);
            // Enemy / box counts stop growing at the ramp's count cap (the road stops growing too); HP and damage carry on.
            var tc = p.CountT(number);
            // A hand tweak for this level (default 1.0): wave sizes and barrier HP only, so the layout stays the seed's.
            var tw = p.Tweak(number);
            int W(int count) => tw.Wave == 1f ? count : Math.Max(1, (int)Math.Round(count * tw.Wave));
            var rnd = new Random(unchecked(p.Seed + (number * 7919)));
            var bossAt = length - 8f;
            var boxMul = 1f + (p.BoxHpStep * t);
            var rows = new List<RunnerRowDef>();
            var pool = new List<string> { "nightfang", "bonequill", "nightfang", "blightroot", "bonequill" };
            if (number >= 4 && hasUnit("vespera"))
            {
                pool.Add("vespera");
            }

            if (number >= 7 && hasUnit("ashwyrm"))
            {
                pool.Add("ashwyrm");
            }

            // Traps: spikes from Level 1, fire from Level 2, rolling logs from Level 4. Every other beat on Levels 1-4,
            // every beat from Level 5, and a second trap on one beat in four from Level 13 and every other beat from Level 22
            // (Derek 2026-10-26: "the obstacles need to be more prevelant and do more damage": about 1.5-2x the old every
            // third / every other beat).
            var traps = new List<RunnerHazardKind> { RunnerHazardKind.Spikes };
            if (number >= 2)
            {
                traps.Add(RunnerHazardKind.Fire);
            }

            if (number >= 4)
            {
                traps.Add(RunnerHazardKind.Log);
            }

            var trapEvery = number >= 5 ? 1 : 2;
            var side = rnd.Next(2) == 0 ? -1f : 1f;
            var beat = 0;
            var beats = Math.Max(1, (int)Math.Floor((bossAt - 16f - 12f) / BeatMetres) + 1);
            var trap = 0;
            // Barriers draw from their own seed so adding them leaves the rest of the road as it was.
            var brnd = new Random(unchecked(p.Seed + (number * 104729) + 17));
            var barrier = 0;
            for (var z = 12f; z < bossAt - 16f; z += BeatMetres, beat++)
            {
                // How far into the level this beat is (0 = the start, 1 = just before the boss): the waves grow along the
                // road as the squad grows from the boxes, so the extra difficulty of a later level lands late in it.
                var along = beats > 1 ? beat / (float)(beats - 1) : 1f;
                var hpAt = (int)Math.Round(8.6f * z * boxMul);
                var fx = side * (1.6f + ((float)rnd.NextDouble() * 0.6f));
                var ux = -side * (1.6f + ((float)rnd.NextDouble() * 0.6f));

                // One box a beat, not two (Derek 2026-10-23: "theres too many upgrades to enimies"): a follower chest on
                // the even beats, a power-up crate on the odd ones, plus a chest on beat 1 and the hero chest on beat 3
                // (and beat 6). The waves keep growing, so later levels have more boxes but fewer per enemy.
                if (beat == 3 || beat == 6)
                {
                    rows.Add(Box(z, fx, hpAt, RunnerBoxKind.Hero, beat == 3 ? "rowan" : "oakenshield", 1, 0f));
                }
                else if (beat % 2 == 0 || beat == 1)
                {
                    var count = 2 + (beat % 3) + (tc / 3);
                    rows.Add(Box(z, fx, hpAt, RunnerBoxKind.Followers, FollowerUnits[beat % FollowerUnits.Length], count, 0f));
                }

                // A power-up crate on the other side on the odd beats, cycling through the kinds; half as many crates, so
                // each damage / rate crate gives a little more than the old 0.2 / 0.15.
                if (beat % 2 == 1)
                {
                    var kind = PowerUps[(beat / 2) % PowerUps.Length];
                    var upHp = (int)Math.Round(hpAt * 0.8f);
                    switch (kind)
                    {
                        case RunnerBoxKind.Weapon:
                            rows.Add(Box(z + 1.5f, ux, upHp, kind, "", 1, 0.3f + (0.02f * t)));
                            break;
                        case RunnerBoxKind.Rate:
                            rows.Add(Box(z + 1.5f, ux, upHp, kind, "", 1, 0.25f + (0.012f * t)));
                            break;
                        case RunnerBoxKind.Range:
                            rows.Add(Box(z + 1.5f, ux, upHp, kind, "", 1, 0.15f));
                            break;
                        case RunnerBoxKind.Heal:
                            rows.Add(Box(z + 1.5f, ux, (int)Math.Round(upHp * 0.7f), kind, "", 1, 0.35f));
                            break;
                        default:
                            rows.Add(Box(z + 1.5f, ux, (int)Math.Round(upHp * 0.6f), RunnerBoxKind.Followers, FollowerUnits[(beat + 2) % FollowerUnits.Length], 1 + (tc / 4), 0f));
                            break;
                    }
                }

                // Skirmishers between the boxes and the wave, on every other beat (a pair of runners or archers).
                if (beat % 2 == 1 && beat >= 3)
                {
                    var skirm = beat % 4 == 1 ? "nightfang" : "bonequill";
                    Group(rows, z + 4f, fx * 0.6f, skirm, W(2 + (int)Math.Round(tc * along * 0.2f)), 1.1f, halfWidth);
                }

                if (HasBarrier(number, beat))
                {
                    AddBarrier(rows, z + BarrierOffset, tw.BarrierHp == 1f ? hpAt : (int)Math.Round(hpAt * tw.BarrierHp), number, t, barrier, brnd, halfWidth, Math.Sign(beat % 2 == 1 ? ux : fx));
                    barrier++;
                }

                // A lane trap between the boxes and the wave.
                if (beat >= 1 && (beat - 1) % trapEvery == 0)
                {
                    // Not in the lane of the chest or crate just before it, so the busier roads still let the squad take its boxes.
                    AddTrap(rows, traps[trap % traps.Count], z + 5.5f, rnd, halfWidth, t, beat % 2 == 1 ? ux : fx);
                    trap++;
                    if (number >= SecondTrapFrom && (beat % 4 == 1 || (number >= SecondTrapEveryOtherFrom && beat % 2 == 1)))
                    {
                        AddTrap(rows, traps[trap % traps.Count], z + 10f, rnd, halfWidth, t);
                        trap++;
                    }
                }

                // The wave: a fodder front line, a different second line behind it, and on some beats a heavy anchor.
                var front = beat < 2 ? (beat == 0 ? "bonequill" : "nightfang") : pool[beat % pool.Count];
                if (front == "blightroot" || front == "ashwyrm")
                {
                    front = beat % 2 == 0 ? "nightfang" : "bonequill";
                }

                var back = beat == 0 ? "nightfang" : pool[((beat * 3) + 1) % pool.Count];
                if (back == front || back == "blightroot" || back == "ashwyrm")
                {
                    back = front == "bonequill" ? "nightfang" : "bonequill";
                }

                if (number >= 4 && beat >= beats / 2 && beat % 3 == 0 && hasUnit("vespera"))
                {
                    back = "vespera";
                }

                var wx = ((float)rnd.NextDouble() * 2.4f) - 1.2f;
                // The first two waves stay small: the squad is still the hero and a chest or two.
                var opening = beat < 2;
                // The very first wave stands 12 m past its chest (8 m later on): the faster walkers otherwise reach the
                // chest as it opens and pick off the new 1 hp followers before they can shoot.
                Group(rows, z + (beat == 0 ? FirstWaveMetres : 8f), wx, front, W(opening ? 2 + beat : 3 + (beat % 2) + (int)Math.Round(tc * along * 0.3f)), 1.1f, halfWidth);
                if (beat > 0)
                {
                    Group(rows, z + 10.5f, -wx * 0.5f, back, W(opening ? 1 : 2 + (int)Math.Round(tc * along * 0.25f)), 1.4f, halfWidth);
                }

                if (beat % 3 == 2 || (t >= 6 && beat % 3 == 0 && beat > 0))
                {
                    var heavy = number >= 7 && hasUnit("ashwyrm") && beat % 2 == 0 ? "ashwyrm" : "blightroot";
                    var hc = 1 + (int)Math.Round(tc * along / 5f);
                    Group(rows, z + 12f, -wx, heavy, W(hc), 3.2f, halfWidth);
                }

                side = -side;
            }

            if (bossUnit != null)
            {
                // The boss's guard: a line of archers, longer with the level, then the boss.
                Group(rows, bossAt - 7f, 0f, "bonequill", 3 + (tc / 2), 1.4f, halfWidth);
                rows.Add(new RunnerRowDef(bossAt, RunnerRowKind.Boss, 0f, bossUnit, 1, 0f, null));
            }
            else
            {
                Finale(rows, number, t, tc, bossAt - FinaleBack, traps, halfWidth, hasUnit, W);
            }

            rows.Sort((a, b) => a.At.CompareTo(b.At));
            return rows;
        }

        /// <summary>
        /// The closing set piece of a level without a boss, where the boss section used to stand (Derek 2026-10-27: "too
        /// much empty space at the end of the runner levels that don't have a larger enemy"): a trap gauntlet on both
        /// edges with the middle left open, a fast fodder line down that middle, the old archer line, and a captain (a
        /// heavy: Blightroot, Ashwyrm from Level 7; two of them from <see cref="FinaleSecondCaptainFrom"/>) at the back,
        /// just short of the finish gate (<see cref="FinishPad"/> m past the last rank).
        /// </summary>
        private static void Finale(List<RunnerRowDef> rows, int number, int t, int tc, float z, List<RunnerHazardKind> traps, float halfWidth, Func<string, bool> hasUnit, Func<int, int> w)
        {
            var edge = halfWidth - 1.1f;
            var from = rows.Count;
            AddFinaleTrap(rows, traps[number % traps.Count], z, -edge, halfWidth, t);
            AddFinaleTrap(rows, traps[(number + 1) % traps.Count], z + 1.5f, edge, halfWidth, t);
            Group(rows, z + 3f, 0f, "nightfang", w(3 + (tc / 3)), 1.1f, halfWidth);
            var captain = number >= 7 && hasUnit("ashwyrm") ? "ashwyrm" : "blightroot";
            Group(rows, z + 6f + (FinaleRanks(3 + (tc / 2)) * 1.5f) + 1f, 0f, captain, number >= FinaleSecondCaptainFrom ? 2 : 1, 3.2f, halfWidth);
            for (var i = from; i < rows.Count; i++)
            {
                rows[i] = rows[i] with { Finale = true };
            }

            // The archer line a boss level has as the boss's guard stands between the push and the captain.
            Group(rows, z + 6f, 0f, "bonequill", 3 + (tc / 2), 1.4f, halfWidth);
        }

        private static int FinaleRanks(int count) => (count + MaxPerRank - 1) / MaxPerRank;

        private static void AddFinaleTrap(List<RunnerRowDef> rows, RunnerHazardKind kind, float z, float x, float halfWidth, int t)
        {
            var def = kind switch
            {
                RunnerHazardKind.Spikes => new RunnerHazardDef(kind, 2.0f, SpikeDamage + (SpikeDamageStep * t), 0f),
                RunnerHazardKind.Log => new RunnerHazardDef(kind, 2.2f, LogDamage + (LogDamageStep * t), 0f),
                _ => new RunnerHazardDef(kind, 1.8f, FireDamage + (FireDamageStep * t), 0f)
            };
            rows.Add(new RunnerRowDef(z, RunnerRowKind.Hazard, Math.Max(-halfWidth + (def.Width * 0.5f), Math.Min(halfWidth - (def.Width * 0.5f), x)), "", 1, 0f, null) { Hazard = def });
        }

        /// <summary>A group of <paramref name="count"/> enemies around x: ranks of up to <see cref="MaxPerRank"/>, 1.5 m apart, fitted to the lane.</summary>
        private static void Group(List<RunnerRowDef> rows, float z, float x, string unit, int count, float spread, float halfWidth)
        {
            var rank = 0;
            while (count > 0)
            {
                var n = Math.Min(MaxPerRank, count);
                var s = n > 1 ? Math.Min(spread, ((2f * halfWidth) - 1.4f) / (n - 1)) : 0f;
                var span = s * (n - 1) * 0.5f;
                var cx = Math.Max(-halfWidth + 0.7f + span, Math.Min(halfWidth - 0.7f - span, x + (rank % 2 == 1 ? s * 0.5f : 0f)));
                rows.Add(new RunnerRowDef(z + (rank * 1.5f), RunnerRowKind.Enemy, (float)Math.Round(cx, 2), unit, n, (float)Math.Round(s, 2), null));
                count -= n;
                rank++;
            }
        }

        /// <summary>Whether beat <paramref name="beat"/> of Level <paramref name="number"/> has a breakable barrier.</summary>
        public static bool HasBarrier(int number, int beat) =>
            number >= BarrierFrom && beat >= 2 && (number >= BarrierEveryBeatFrom || (number >= BarrierEveryOtherFrom ? beat % 2 == 0 : beat % 3 == 2));

        private static void AddBarrier(List<RunnerRowDef> rows, float z, int hpAt, int number, int t, int index, Random rnd, float halfWidth, int boxSide)
        {
            var full = number >= FullWidthBarrierFrom && index % FullWidthEvery == FullWidthEvery - 1;
            RunnerBarrierDef def;
            var x = 0f;
            if (full)
            {
                var kind = (index / FullWidthEvery) % 2 == 0 ? RunnerBarrierKind.Palisade : RunnerBarrierKind.StoneWall;
                def = new RunnerBarrierDef(kind, 2f * halfWidth, Math.Max(60, (int)Math.Round(hpAt * FullWidthHpShare)), BarrierDamage(kind, t));
            }
            else
            {
                var kind = (RunnerBarrierKind)rnd.Next(3);
                var share = kind switch
                {
                    RunnerBarrierKind.StoneWall => StoneWallHpShare,
                    RunnerBarrierKind.Barricade => BarricadeHpShare,
                    _ => PalisadeHpShare
                };
                // Against the edge away from the beat's box, so it never hides the box (or the wave coming down the middle)
                // from the squad's fire; never the whole road: at least 2 m of open road stays beside it.
                var width = kind == RunnerBarrierKind.StoneWall ? 3.6f : 3.2f;
                x = -boxSide * (halfWidth - (width * 0.5f));
                def = new RunnerBarrierDef(kind, width, Math.Max(40, (int)Math.Round(hpAt * share)), BarrierDamage(kind, t));
            }

            rows.Add(new RunnerRowDef(z, RunnerRowKind.Barrier, x, "", 1, 0f, null) { Barrier = def });
        }

        public static int BarrierDamage(RunnerBarrierKind kind, int t) => kind switch
        {
            RunnerBarrierKind.StoneWall => StoneWallDamage + (StoneWallDamageStep * t),
            RunnerBarrierKind.Barricade => BarricadeDamage + (BarricadeDamageStep * t),
            _ => PalisadeDamage + (PalisadeDamageStep * t)
        };

        private static void AddTrap(List<RunnerRowDef> rows, RunnerHazardKind kind, float z, Random rnd, float halfWidth, int t, float? clearOf = null)
        {
            // Never the full lane: a trap covers one side or the middle, so there is always a way round.
            var lanes = new List<float> { -2.2f, 0f, 2.2f };
            if (clearOf is float keep)
            {
                lanes.RemoveAll(l => Math.Abs(l - keep) < 1.2f);
            }

            var x = lanes[rnd.Next(lanes.Count)];
            var def = kind switch
            {
                RunnerHazardKind.Spikes => new RunnerHazardDef(kind, 2.0f, SpikeDamage + (SpikeDamageStep * t), (float)Math.Round(rnd.NextDouble() * RunnerRules.SpikeCycle, 2)),
                RunnerHazardKind.Log => new RunnerHazardDef(kind, 2.2f, LogDamage + (LogDamageStep * t), 0f),
                _ => new RunnerHazardDef(kind, 1.8f, FireDamage + (FireDamageStep * t), 0f)
            };
            rows.Add(new RunnerRowDef(z, RunnerRowKind.Hazard, Math.Max(-halfWidth + (def.Width * 0.5f), Math.Min(halfWidth - (def.Width * 0.5f), x)), "", 1, 0f, null) { Hazard = def });
        }

        private static string BossUnit(RunnerLevelDef first)
        {
            foreach (var r in first.Rows)
            {
                if (r.Kind == RunnerRowKind.Boss)
                {
                    return r.Unit;
                }
            }

            return "ironhowl";
        }

        private static RunnerRowDef Box(float z, float x, int hp, RunnerBoxKind kind, string unit, int count, float amount) =>
            new RunnerRowDef(z, RunnerRowKind.Box, x, unit, 1, 0f, new RunnerBoxDef(Math.Max(100, hp), kind, unit, kind == RunnerBoxKind.Hero ? 1 : Math.Max(1, count), amount));
    }

    /// <summary>
    /// Which runner levels can be played. Levels are beaten in order and a beaten level can never be played again: only the
    /// next unbeaten level (the frontier) can be started, a loss there can be retried, a win moves the frontier on.
    /// <c>unlocked</c> is the frontier's number, saved as "runner_unlocked" in survival_save.json; levelCount + 1 means
    /// every level is beaten.
    /// </summary>
    public static class RunnerProgress
    {
        /// <summary>The frontier after <paramref name="result"/> (a win on the frontier moves it on; levelCount + 1 = all beaten).</summary>
        public static int AfterRun(int unlocked, RunnerResult result, int levelCount)
        {
            var open = Math.Max(1, unlocked);
            if (result.Won)
            {
                open = Math.Max(open, result.Number + 1);
            }

            return Math.Max(1, Math.Min(open, Math.Max(1, levelCount) + 1));
        }

        /// <summary>Reached: beaten or the frontier.</summary>
        public static bool IsOpen(int unlocked, int number) => number >= 1 && number <= Math.Max(1, unlocked);

        /// <summary>Beaten: below the frontier. It shows as completed and can never be started again.</summary>
        public static bool IsBeaten(int unlocked, int number) => number >= 1 && number < Math.Max(1, unlocked);

        /// <summary>Only the frontier (the next unbeaten level) can be started.</summary>
        public static bool CanStart(int unlocked, int number, int levelCount) =>
            number >= 1 && number <= levelCount && number == Math.Max(1, unlocked);

        /// <summary>Run <paramref name="number"/> again after a run of it ended: only after a loss (a win beat it for good).</summary>
        public static bool CanRetry(int unlocked, int number, bool won, int levelCount) => !won && CanStart(unlocked, number, levelCount);

        /// <summary>The level to open (the frontier), or null when every level is beaten.</summary>
        public static int? Frontier(int unlocked, int levelCount)
        {
            var f = Math.Max(1, unlocked);
            return f <= levelCount ? f : null;
        }

        public static bool AllBeaten(int unlocked, int levelCount) => Math.Max(1, unlocked) > levelCount;
    }
}
