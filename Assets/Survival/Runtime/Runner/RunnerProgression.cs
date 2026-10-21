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
    /// Lays out runner levels 2+ from Level 1 and a seed: a longer road, more and bigger enemy waves (Vespera's
    /// casters from Level 4, Ashwyrm from Level 7), more and tougher boxes paying more followers and bigger upgrades,
    /// a tougher boss, and bigger rewards. Deterministic: the same seed and number always give the same level.
    /// </summary>
    public static class RunnerProgression
    {
        /// <summary>Metres from one beat (boxes, then a wave) to the next.</summary>
        public const float BeatMetres = 13f;

        private static readonly string[] FollowerUnits = { "rowan", "lyra", "rowan", "sir_aldric", "lyra", "oakenshield" };
        private static readonly string[] ShardHeroes = { "hero.lyra", "hero.rowan", "hero.oakenshield", "hero.sir_aldric" };

        public static RunnerLevelDef Generate(RunnerLevelDef first, RunnerProgressionDef p, int number, Func<string, bool> hasUnit)
        {
            if (number < 2)
            {
                return first;
            }

            var t = number - 1;
            var rnd = new Random(unchecked(p.Seed + (number * 7919)));
            var length = (float)Math.Round(first.Length + (p.LengthStep * t));
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

            var side = rnd.Next(2) == 0 ? -1f : 1f;
            var beat = 0;
            var beats = Math.Max(1, (int)Math.Floor((bossAt - 16f - 12f) / BeatMetres) + 1);
            for (var z = 12f; z < bossAt - 16f; z += BeatMetres, beat++)
            {
                // How far into the level this beat is (0 = the start, 1 = just before the boss): the waves grow along the
                // road as the squad grows from the boxes, so the extra difficulty of a later level lands late in it.
                var along = beats > 1 ? beat / (float)(beats - 1) : 1f;
                // Boxes: a follower chest every beat (bigger with the level), an upgrade crate on the other side every
                // second beat, a hero chest on beats 3 and 6.
                var hpAt = (int)Math.Round(8.6f * z * boxMul);
                var fx = side * (1.6f + ((float)rnd.NextDouble() * 0.6f));
                if (beat == 3 || beat == 6)
                {
                    rows.Add(Box(z, fx, hpAt, RunnerBoxKind.Hero, beat == 3 ? "sir_aldric" : "oakenshield", 1, 0f));
                }
                else
                {
                    var count = 2 + (beat % 3) + (t / 3);
                    rows.Add(Box(z, fx, hpAt, RunnerBoxKind.Followers, FollowerUnits[beat % FollowerUnits.Length], count, 0f));
                }

                if (beat % 2 == 1)
                {
                    var weapon = (beat / 2) % 2 == 0;
                    var amount = weapon ? 0.25f + (0.02f * t) : 0.2f + (0.015f * t);
                    rows.Add(Box(z + 2f, -side * (1.6f + ((float)rnd.NextDouble() * 0.6f)), (int)Math.Round(hpAt * 1.1f), weapon ? RunnerBoxKind.Weapon : RunnerBoxKind.Rate, "", 1, amount));
                }

                // A wave after the boxes: more of them, and the pool widens with the level.
                var unit = beat < 2 ? (beat == 0 ? "bonequill" : "nightfang") : pool[beat % pool.Count];
                if (beat >= beats / 2 && (unit == "vespera" || unit == "ashwyrm") == false && number >= 4 && beat % 4 == 0)
                {
                    // The tougher types, late in the level.
                    unit = number >= 7 && hasUnit("ashwyrm") && beat % 8 == 0 ? "ashwyrm" : hasUnit("vespera") ? "vespera" : unit;
                }

                var heavy = unit == "blightroot" || unit == "ashwyrm";
                var waveCount = heavy
                    ? 1 + (int)Math.Round(t * along / 4f)
                    : 2 + (beat % 2) + (int)Math.Round(t * along * 0.6f);
                var wx = ((float)rnd.NextDouble() * 3f) - 1.5f;
                rows.Add(new RunnerRowDef(z + 8f, RunnerRowKind.Enemy, wx, unit, waveCount, heavy ? 3f : 1.3f, null));
                side = -side;
            }

            // The boss's guard: a line of archers just before it, longer with the level.
            rows.Add(new RunnerRowDef(bossAt - 7f, RunnerRowKind.Enemy, 0f, "bonequill", 2 + (t / 3), 1.6f, null));
            rows.Add(new RunnerRowDef(bossAt, RunnerRowKind.Boss, 0f, BossUnit(first), 1, 0f, null));
            rows.Sort((a, b) => a.At.CompareTo(b.At));
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
                first.MaxFollowers + t,
                rows,
                rewards)
            {
                Number = number,
                DisplayName = number - 1 < p.Names.Count ? p.Names[number - 1] : "Road " + number.ToString(CultureInfo.InvariantCulture),
                FoeHpMul = first.FoeHpMul * (1f + (p.FoeHpStep * t)),
                BossHpMul = first.BossHpMul * (1f + (p.BossHpStep * t))
            };
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
            new RunnerRowDef(z, RunnerRowKind.Box, x, unit, 1, 0f, new RunnerBoxDef(Math.Max(1, hp), kind, unit, kind == RunnerBoxKind.Hero ? 1 : Math.Max(1, count), amount));
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
