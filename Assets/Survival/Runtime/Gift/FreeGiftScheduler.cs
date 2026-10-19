#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Clock;

namespace Survival.Domain.Gift
{
    public sealed record GiftRewardDef(string Id, string Resource, int Min, int Max, int Weight);

    public sealed record GiftConfig(
        string Id,
        string StoreId,
        string OfferId,
        int MinSeconds,
        int MaxSeconds,
        int DailyCap,
        IReadOnlyList<GiftRewardDef> Pool);

    public sealed record PendingGift(string RewardId, string Resource, int Amount, double OfferedAt, bool FromDebug);

    /// <summary>
    /// Free gift at random intervals (MinSeconds..MaxSeconds after the last claim), at most DailyCap per UTC day,
    /// never more than one unclaimed. Offline time never stacks gifts.
    /// </summary>
    public sealed class FreeGiftScheduler
    {
        private const double DaySeconds = 86400.0;
        private readonly Random _rng;

        public FreeGiftScheduler(GiftConfig config, IGameClock clock, int seed)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            if (config.Pool.Count == 0)
            {
                throw new ArgumentException("Gift pool is empty.");
            }

            _rng = new Random(seed);
            Day = DayOf(clock.Now);
            NextAt = clock.Now + RollDelay();
        }

        public GiftConfig Config { get; }
        public IGameClock Clock { get; }
        public double NextAt { get; private set; }
        public PendingGift? Pending { get; private set; }
        public int OfferedToday { get; private set; }
        public long Day { get; private set; }
        public bool HasPending => Pending != null;
        public bool CapReached => OfferedToday >= Config.DailyCap;

        public double SecondsUntilNext => Math.Max(0, NextAt - Clock.Now);

        public event Action<PendingGift>? Offered;

        /// <summary>Call every frame (or on resume). Returns true when a gift was just offered.</summary>
        public bool Tick()
        {
            Rollover();
            if (Pending != null || CapReached || Clock.Now < NextAt)
            {
                return false;
            }

            Offer(fromDebug: false);
            return true;
        }

        /// <summary>Debug: offer a gift right now (ignores timer and cap, still one at a time).</summary>
        public bool DebugOfferNow()
        {
            Rollover();
            if (Pending != null)
            {
                return false;
            }

            Offer(fromDebug: true);
            return true;
        }

        /// <summary>Hand over the pending gift and schedule the next. Caller grants it.</summary>
        public PendingGift? Claim()
        {
            var gift = Pending;
            if (gift == null)
            {
                return null;
            }

            Pending = null;
            NextAt = Clock.Now + RollDelay();
            return gift;
        }

        private void Offer(bool fromDebug)
        {
            var def = RollReward();
            var amount = def.Max <= def.Min ? def.Min : _rng.Next(def.Min, def.Max + 1);
            Pending = new PendingGift(def.Id, def.Resource, amount, Clock.Now, fromDebug);
            if (!fromDebug)
            {
                OfferedToday++;
            }

            Offered?.Invoke(Pending);
        }

        private void Rollover()
        {
            var day = DayOf(Clock.Now);
            if (day != Day)
            {
                Day = day;
                OfferedToday = 0;
            }
        }

        private double RollDelay()
        {
            var lo = Math.Max(0, Config.MinSeconds);
            var hi = Math.Max(lo, Config.MaxSeconds);
            return lo + _rng.NextDouble() * (hi - lo);
        }

        private GiftRewardDef RollReward()
        {
            var total = 0;
            foreach (var r in Config.Pool)
            {
                total += Math.Max(0, r.Weight);
            }

            if (total <= 0)
            {
                return Config.Pool[0];
            }

            var pick = _rng.Next(total);
            foreach (var r in Config.Pool)
            {
                var w = Math.Max(0, r.Weight);
                if (pick < w)
                {
                    return r;
                }

                pick -= w;
            }

            return Config.Pool[Config.Pool.Count - 1];
        }

        private static long DayOf(double seconds) => (long)Math.Floor(seconds / DaySeconds);
    }
}
