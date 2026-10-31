#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Survival.Domain.Economy;
using Survival.Domain.Ids;

namespace Survival.Domain.Taps
{
    public sealed record WellConfig(
        int FreePerDay,
        int GoldTossesPerDay,
        int GoldCost,
        int GemTossesPerDay,
        int GemCost,
        int JackpotChancePermille,
        IReadOnlyList<TapRewardDef> Pool,
        IReadOnlyDictionary<string, int> Jackpot)
    {
        /// <summary>
        /// 1 free toss a day, then 3 tosses at 5,000 gold, then up to 10 at 20 gems. 3% jackpot. Town economy only.
        /// </summary>
        public static WellConfig Default { get; } = new WellConfig(
            1,
            3,
            5000,
            10,
            20,
            30,
            new[]
            {
                new TapRewardDef(SurvIds.ResSoft, 3000, 9000, 30),
                new TapRewardDef(SurvIds.ResWood, 1500, 4000, 20),
                new TapRewardDef(SurvIds.ResStone, 1200, 3500, 20),
                new TapRewardDef(SurvIds.ResFood, 1500, 4000, 20),
                new TapRewardDef(SurvIds.ResSpeedupM, 15, 45, 15),
                new TapRewardDef(SurvIds.ResHard, 5, 25, 8),
            },
            new Dictionary<string, int>
            {
                [SurvIds.ResHard] = 100,
                [SurvIds.ResSoft] = 25000,
                [SurvIds.ResSpeedupM] = 120,
            });
    }

    public enum WellCostKind
    {
        Free,
        Gold,
        Gems,
        SoldOut,
    }

    public readonly struct WellCost
    {
        public WellCost(WellCostKind kind, string? resource, int amount)
        {
            Kind = kind;
            Resource = resource;
            Amount = amount;
        }

        public WellCostKind Kind { get; }
        public string? Resource { get; }
        public int Amount { get; }
    }

    public sealed class WellToss
    {
        public WellToss(WellCost paid, Dictionary<string, int> rewards, bool jackpot)
        {
            Paid = paid;
            Rewards = rewards;
            Jackpot = jackpot;
        }

        public WellCost Paid { get; }
        public Dictionary<string, int> Rewards { get; }
        public bool Jackpot { get; }
    }

    /// <summary>
    /// The Wishing Well: toss a coin, make a wish, get a random handful of town resources (with a small jackpot
    /// chance). The first toss each UTC day is free; then a few gold tosses; then gem tosses up to a daily cap.
    /// </summary>
    public sealed class WishingWell
    {
        private readonly Func<double> _now;
        private readonly Random _rng;
        private long _day;
        private int _tosses;

        public WishingWell(WellConfig config, Func<double> now, int seed)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _now = now ?? throw new ArgumentNullException(nameof(now));
            _rng = new Random(seed);
            _day = TapKit.DayOf(_now());
        }

        public WellConfig Config { get; }

        public int TossesToday
        {
            get
            {
                Rollover();
                return _tosses;
            }
        }

        public bool FreeAvailable => NextCost.Kind == WellCostKind.Free;

        /// <summary>Seconds until the next UTC midnight (free toss comes back).</summary>
        public double SecondsUntilReset => (TapKit.DayOf(_now()) + 1) * 86400.0 - _now();

        public WellCost NextCost
        {
            get
            {
                var n = TossesToday;
                if (n < Config.FreePerDay)
                {
                    return new WellCost(WellCostKind.Free, null, 0);
                }

                n -= Config.FreePerDay;
                if (n < Config.GoldTossesPerDay)
                {
                    return new WellCost(WellCostKind.Gold, SurvIds.ResSoft, Config.GoldCost);
                }

                n -= Config.GoldTossesPerDay;
                return n < Config.GemTossesPerDay
                    ? new WellCost(WellCostKind.Gems, SurvIds.ResHard, Config.GemCost)
                    : new WellCost(WellCostKind.SoldOut, null, 0);
            }
        }

        /// <summary>Toss. Spends the current price from <paramref name="wallet"/>; caller grants the rewards. Null if sold out or broke.</summary>
        public WellToss? Toss(Wallet wallet)
        {
            var cost = NextCost;
            if (cost.Kind == WellCostKind.SoldOut)
            {
                return null;
            }

            if (cost.Resource != null && !wallet.TrySpend(cost.Resource, cost.Amount))
            {
                return null;
            }

            _tosses++;
            if (_rng.Next(1000) < Config.JackpotChancePermille)
            {
                return new WellToss(cost, new Dictionary<string, int>(Config.Jackpot), true);
            }

            return new WellToss(cost, TapKit.Roll(Config.Pool, 2, _rng), false);
        }

        private void Rollover()
        {
            var day = TapKit.DayOf(_now());
            if (day != _day)
            {
                _day = day;
                _tosses = 0;
            }
        }

        // ---------- save ----------

        public const string PrefsKey = "survival.taps.well.v1";

        public string Capture() => new StringBuilder("{\"v\":1,\"day\":").Append(_day).Append(",\"tosses\":").Append(_tosses).Append('}').ToString();

        public bool Apply(string? json)
        {
            var o = TapKit.ParseObj(json);
            if (o == null)
            {
                return false;
            }

            _day = (long)TapKit.D(o, "day", TapKit.DayOf(_now()));
            _tosses = Math.Max(0, TapKit.I(o, "tosses"));
            Rollover();
            return true;
        }
    }
}
