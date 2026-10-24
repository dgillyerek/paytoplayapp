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
                names);
        }
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
        /// <summary>Metres from one beat (boxes, then a wave) to the next.</summary>
        public const float BeatMetres = 13f;

        /// <summary>Most enemies standing side by side in one row; bigger groups form ranks behind.</summary>
        public const int MaxPerRank = 5;

        /// <summary>How far past the first follower chest the opening wave stands (later waves stand 8 m past theirs).</summary>
        public const float FirstWaveMetres = 12f;

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
            var length = (float)Math.Round(first.Length + (p.LengthStep * t));
            var rows = Rows(p, number, length, first.HalfWidth, BossUnit(first), hasUnit);
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
                first.Speed + (p.SpeedStep * t),
                first.HalfWidth,
                first.MaxFollowers,
                rows,
                rewards)
            {
                Number = number,
                DisplayName = number - 1 < p.Names.Count ? p.Names[number - 1] : "Road " + number.ToString(CultureInfo.InvariantCulture),
                FoeHpMul = first.FoeHpMul * (1f + (p.FoeHpStep * t)),
                BossHpMul = first.BossHpMul * (1f + (p.BossHpStep * t))
            };
        }

        /// <summary>The spawn rows of level <paramref name="number"/> on a <paramref name="length"/> m road (boss row last).</summary>
        public static List<RunnerRowDef> Rows(RunnerProgressionDef p, int number, float length, float halfWidth, string bossUnit, Func<string, bool> hasUnit)
        {
            var t = Math.Max(0, number - 1);
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

            // Traps: spikes from Level 1, fire from Level 2, rolling logs from Level 4. Every third beat on Level 1,
            // every other beat by Level 5, then a second trap in some beats.
            var traps = new List<RunnerHazardKind> { RunnerHazardKind.Spikes };
            if (number >= 2)
            {
                traps.Add(RunnerHazardKind.Fire);
            }

            if (number >= 4)
            {
                traps.Add(RunnerHazardKind.Log);
            }

            var trapEvery = number >= 5 ? 2 : 3;
            var side = rnd.Next(2) == 0 ? -1f : 1f;
            var beat = 0;
            var beats = Math.Max(1, (int)Math.Floor((bossAt - 16f - 12f) / BeatMetres) + 1);
            var trap = 0;
            for (var z = 12f; z < bossAt - 16f; z += BeatMetres, beat++)
            {
                // How far into the level this beat is (0 = the start, 1 = just before the boss): the waves grow along the
                // road as the squad grows from the boxes, so the extra difficulty of a later level lands late in it.
                var along = beats > 1 ? beat / (float)(beats - 1) : 1f;
                var hpAt = (int)Math.Round(8.6f * z * boxMul);
                var fx = side * (1.6f + ((float)rnd.NextDouble() * 0.6f));
                var ux = -side * (1.6f + ((float)rnd.NextDouble() * 0.6f));

                // The follower chest (a hero chest on beats 3 and 6).
                if (beat == 3 || beat == 6)
                {
                    rows.Add(Box(z, fx, hpAt, RunnerBoxKind.Hero, beat == 3 ? "rowan" : "oakenshield", 1, 0f));
                }
                else
                {
                    var count = 2 + (beat % 3) + (t / 3);
                    rows.Add(Box(z, fx, hpAt, RunnerBoxKind.Followers, FollowerUnits[beat % FollowerUnits.Length], count, 0f));
                }

                // A power-up crate on the other side, every beat, cycling through the kinds.
                var kind = PowerUps[beat % PowerUps.Length];
                var upHp = (int)Math.Round(hpAt * 0.8f);
                switch (kind)
                {
                    case RunnerBoxKind.Weapon:
                        rows.Add(Box(z + 1.5f, ux, upHp, kind, "", 1, 0.2f + (0.015f * t)));
                        break;
                    case RunnerBoxKind.Rate:
                        rows.Add(Box(z + 1.5f, ux, upHp, kind, "", 1, 0.15f + (0.01f * t)));
                        break;
                    case RunnerBoxKind.Range:
                        rows.Add(Box(z + 1.5f, ux, upHp, kind, "", 1, 0.15f));
                        break;
                    case RunnerBoxKind.Heal:
                        rows.Add(Box(z + 1.5f, ux, (int)Math.Round(upHp * 0.7f), kind, "", 1, 0.35f));
                        break;
                    default:
                        rows.Add(Box(z + 1.5f, ux, (int)Math.Round(upHp * 0.6f), RunnerBoxKind.Followers, FollowerUnits[(beat + 2) % FollowerUnits.Length], 1 + (t / 4), 0f));
                        break;
                }

                // Skirmishers between the boxes and the wave, on every other beat (a pair of runners or archers).
                if (beat % 2 == 1 && beat >= 3)
                {
                    var skirm = beat % 4 == 1 ? "nightfang" : "bonequill";
                    Group(rows, z + 4f, fx * 0.6f, skirm, 2 + (int)Math.Round(t * along * 0.2f), 1.1f, halfWidth);
                }

                // A lane trap between the boxes and the wave.
                if (beat >= 1 && (beat - 1) % trapEvery == 0)
                {
                    AddTrap(rows, traps[trap % traps.Count], z + 5.5f, rnd, halfWidth, t);
                    trap++;
                    if ((number >= 9 && beat % 4 == 1) || (number >= 12 && beat % 4 == 3))
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
                Group(rows, z + (beat == 0 ? FirstWaveMetres : 8f), wx, front, opening ? 2 + beat : 3 + (beat % 2) + (int)Math.Round(t * along * 0.3f), 1.1f, halfWidth);
                if (beat > 0)
                {
                    Group(rows, z + 10.5f, -wx * 0.5f, back, opening ? 1 : 2 + (int)Math.Round(t * along * 0.25f), 1.4f, halfWidth);
                }

                if (beat % 3 == 2 || (t >= 6 && beat % 3 == 0 && beat > 0))
                {
                    var heavy = number >= 7 && hasUnit("ashwyrm") && beat % 2 == 0 ? "ashwyrm" : "blightroot";
                    var hc = 1 + (int)Math.Round(t * along / 5f);
                    Group(rows, z + 12f, -wx, heavy, hc, 3.2f, halfWidth);
                }

                side = -side;
            }

            // The boss's guard: a line of archers just before it, longer with the level.
            Group(rows, bossAt - 7f, 0f, "bonequill", 3 + (t / 2), 1.4f, halfWidth);
            rows.Add(new RunnerRowDef(bossAt, RunnerRowKind.Boss, 0f, bossUnit, 1, 0f, null));
            rows.Sort((a, b) => a.At.CompareTo(b.At));
            return rows;
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

        private static void AddTrap(List<RunnerRowDef> rows, RunnerHazardKind kind, float z, Random rnd, float halfWidth, int t)
        {
            // Never the full lane: a trap covers one side or the middle, so there is always a way round.
            var lanes = new[] { -2.2f, 0f, 2.2f };
            var x = lanes[rnd.Next(lanes.Length)];
            var def = kind switch
            {
                RunnerHazardKind.Spikes => new RunnerHazardDef(kind, 2.0f, 40 + (3 * t), (float)Math.Round(rnd.NextDouble() * RunnerRules.SpikeCycle, 2)),
                RunnerHazardKind.Log => new RunnerHazardDef(kind, 2.2f, 55 + (4 * t), 0f),
                _ => new RunnerHazardDef(kind, 1.8f, 30 + (3 * t), 0f)
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

    /// <summary>Which runner levels are open: Level 1 always, each win opens the next. Saved in survival_save.json.</summary>
    public static class RunnerProgress
    {
        /// <summary>Highest level number open after <paramref name="result"/> (a win on the highest open level opens the next).</summary>
        public static int AfterRun(int unlocked, RunnerResult result, int levelCount)
        {
            var open = Math.Max(1, unlocked);
            if (result.Won)
            {
                open = Math.Max(open, result.Number + 1);
            }

            return Math.Max(1, Math.Min(open, Math.Max(1, levelCount)));
        }

        public static bool IsOpen(int unlocked, int number) => number >= 1 && number <= Math.Max(1, unlocked);
    }
}
