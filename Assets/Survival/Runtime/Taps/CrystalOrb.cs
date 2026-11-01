#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Survival.Domain.Economy;
using Survival.Domain.Ids;

namespace Survival.Domain.Taps
{
    /// <summary>One kind of orb task: <c>Kind</c> from <see cref="ActivityKinds"/>, target Min..Max.</summary>
    public sealed record OrbTaskDef(string Kind, int Min, int Max, int Weight);

    public sealed record OrbConfig(
        double TaskEverySeconds,
        int FreeRerollsPerDay,
        int RerollGems,
        int HastenGems,
        int ChestRolls,
        IReadOnlyList<OrbTaskDef> Tasks,
        IReadOnlyList<TapRewardDef> Chest)
    {
        /// <summary>
        /// A new vision every 3 hours; 2 free re-rolls a day, then 15 gems; 25 gems to call the next vision now.
        /// The mystery chest pays town resources, speed-ups and a few gems only (nothing that feeds runner power).
        /// </summary>
        public static OrbConfig Default { get; } = new OrbConfig(
            3 * 3600,
            2,
            15,
            25,
            3,
            new[]
            {
                new OrbTaskDef(ActivityKinds.RunnerKills, 30, 80, 30),
                new OrbTaskDef(ActivityKinds.RunnerWin, 1, 2, 20),
                new OrbTaskDef(ActivityKinds.Harvest, 3, 6, 25),
                new OrbTaskDef(ActivityKinds.Upgrade, 1, 1, 15),
                new OrbTaskDef(ActivityKinds.HeroLevel, 1, 3, 10),
            },
            new[]
            {
                new TapRewardDef(SurvIds.ResSoft, 2000, 6000, 30),
                new TapRewardDef(SurvIds.ResWood, 1000, 3000, 20),
                new TapRewardDef(SurvIds.ResStone, 800, 2500, 20),
                new TapRewardDef(SurvIds.ResFood, 1000, 3000, 20),
                new TapRewardDef(SurvIds.ResSpeedupM, 15, 60, 15),
                new TapRewardDef(SurvIds.ResHard, 10, 40, 8),
            });
    }

    public sealed class OrbTask
    {
        public OrbTask(string kind, int target, int progress = 0)
        {
            Kind = kind;
            Target = Math.Max(1, target);
            Progress = Math.Max(0, Math.Min(Target, progress));
        }

        public string Kind { get; }
        public int Target { get; }
        public int Progress { get; internal set; }
        public bool Done => Progress >= Target;
        public float Fraction => Target <= 0 ? 1f : Math.Min(1f, Progress / (float)Target);
    }

    public enum OrbState
    {
        /// <summary>Waiting for the next vision (countdown).</summary>
        Resting,

        /// <summary>A vision is ready: tap the orb to reveal it.</summary>
        Glowing,

        /// <summary>Task revealed, in progress.</summary>
        Active,

        /// <summary>Task done: tap to open the mystery chest.</summary>
        Complete,
    }

    public enum OrbSpend
    {
        Ok,
        NoTask,
        NotEnoughGems,
        NotNeeded,
    }

    /// <summary>
    /// Oracle's Crystal Orb. Every few hours the orb glows; tapping it reveals a random task (slay foes in the
    /// runner, win runner levels, collect harvests, upgrade a building, level a hero). Progress comes from
    /// <see cref="ActivityFeed"/> through <see cref="Report"/>. A finished task opens a mystery chest. Re-rolls are
    /// free a few times a day, then cost gems; gems can also call the next vision early. Real-time, offline-safe.
    /// </summary>
    public sealed class CrystalOrb
    {
        private readonly Func<double> _now;
        private readonly Random _rng;

        public CrystalOrb(OrbConfig config, Func<double> now, int seed)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _now = now ?? throw new ArgumentNullException(nameof(now));
            _rng = new Random(seed);
            NextAt = _now();
            Day = TapKit.DayOf(NextAt);
        }

        public OrbConfig Config { get; }
        public OrbTask? Task { get; private set; }
        public double NextAt { get; private set; }
        public long Day { get; private set; }
        public int RerollsToday { get; private set; }

        public double Now => _now();

        public OrbState State
        {
            get
            {
                if (Task != null)
                {
                    return Task.Done ? OrbState.Complete : OrbState.Active;
                }

                return Now >= NextAt ? OrbState.Glowing : OrbState.Resting;
            }
        }

        public double SecondsUntilNext => Math.Max(0, NextAt - Now);

        public int FreeRerollsLeft
        {
            get
            {
                Rollover();
                return Math.Max(0, Config.FreeRerollsPerDay - RerollsToday);
            }
        }

        public int RerollCost => FreeRerollsLeft > 0 ? 0 : Config.RerollGems;

        /// <summary>Tap a glowing orb: reveal a random task. False unless the orb is glowing.</summary>
        public bool Reveal()
        {
            if (State != OrbState.Glowing)
            {
                return false;
            }

            Task = RollTask(null);
            return true;
        }

        /// <summary>Swap the current (unfinished) task for a different random one: free re-roll or gems.</summary>
        public OrbSpend Reroll(Wallet wallet)
        {
            if (Task == null || Task.Done)
            {
                return OrbSpend.NoTask;
            }

            var cost = RerollCost;
            if (cost > 0 && !wallet.TrySpend(SurvIds.ResHard, cost))
            {
                return OrbSpend.NotEnoughGems;
            }

            RerollsToday++;
            Task = RollTask(Task.Kind);
            return OrbSpend.Ok;
        }

        /// <summary>Pay gems to make the orb glow now instead of waiting.</summary>
        public OrbSpend Hasten(Wallet wallet)
        {
            if (State != OrbState.Resting)
            {
                return OrbSpend.NotNeeded;
            }

            if (!wallet.TrySpend(SurvIds.ResHard, Config.HastenGems))
            {
                return OrbSpend.NotEnoughGems;
            }

            NextAt = Now;
            return OrbSpend.Ok;
        }

        /// <summary>Count player activity toward the revealed task. Returns true when this report finished it.</summary>
        public bool Report(string kind, int amount)
        {
            if (Task == null || Task.Done || amount <= 0 || !string.Equals(Task.Kind, kind, StringComparison.Ordinal))
            {
                return false;
            }

            Task.Progress = Math.Min(Task.Target, Task.Progress + amount);
            return Task.Done;
        }

        /// <summary>Open the mystery chest of a finished task. Caller grants the rewards (celebration). Null if not done.</summary>
        public Dictionary<string, int>? Claim()
        {
            if (Task == null || !Task.Done)
            {
                return null;
            }

            var chest = TapKit.Roll(Config.Chest, Config.ChestRolls, _rng);
            Task = null;
            NextAt = Now + Config.TaskEverySeconds;
            return chest;
        }

        private OrbTask RollTask(string? notKind)
        {
            var pool = new List<OrbTaskDef>();
            foreach (var t in Config.Tasks)
            {
                if (!string.Equals(t.Kind, notKind, StringComparison.Ordinal))
                {
                    pool.Add(t);
                }
            }

            if (pool.Count == 0)
            {
                pool.AddRange(Config.Tasks);
            }

            var weights = new List<int>();
            foreach (var t in pool)
            {
                weights.Add(t.Weight);
            }

            var def = pool[TapKit.Pick(weights, _rng)];
            var target = def.Max <= def.Min ? def.Min : _rng.Next(def.Min, def.Max + 1);
            return new OrbTask(def.Kind, target);
        }

        private void Rollover()
        {
            var day = TapKit.DayOf(Now);
            if (day != Day)
            {
                Day = day;
                RerollsToday = 0;
            }
        }

        // ---------- save ----------

        public const string PrefsKey = "survival.taps.orb.v1";

        public string Capture()
        {
            var sb = new StringBuilder("{\"v\":1,\"next\":");
            TapKit.Num(sb, NextAt).Append(",\"day\":").Append(Day).Append(",\"rerolls\":").Append(RerollsToday);
            if (Task != null)
            {
                sb.Append(",\"task\":");
                TapKit.Str(sb, Task.Kind).Append(",\"target\":").Append(Task.Target).Append(",\"progress\":").Append(Task.Progress);
            }

            return sb.Append('}').ToString();
        }

        /// <summary>Restore a <see cref="Capture"/>. Bad or empty JSON leaves a fresh orb.</summary>
        public bool Apply(string? json)
        {
            var o = TapKit.ParseObj(json);
            if (o == null)
            {
                return false;
            }

            NextAt = TapKit.D(o, "next", Now);
            Day = (long)TapKit.D(o, "day", TapKit.DayOf(Now));
            RerollsToday = TapKit.I(o, "rerolls");
            var kind = TapKit.S(o, "task");
            Task = kind != null ? new OrbTask(kind, TapKit.I(o, "target", 1), TapKit.I(o, "progress")) : null;
            Rollover();
            return true;
        }
    }
}
