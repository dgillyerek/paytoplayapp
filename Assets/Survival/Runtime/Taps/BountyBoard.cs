#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Survival.Domain.Economy;
using Survival.Domain.Ids;

namespace Survival.Domain.Taps
{
    /// <summary>A bounty template: activity kind, target range, time limit once accepted, reward pool and rolls.</summary>
    public sealed record BountyDef(string Kind, int Min, int Max, double LimitSeconds, int Weight);

    public sealed record BountyConfig(
        int Slots,
        int MaxActive,
        double RefreshEverySeconds,
        int RefreshGems,
        IReadOnlyList<string> Givers,
        IReadOnlyList<BountyDef> Bounties,
        IReadOnlyList<TapRewardDef> Rewards,
        int RareChancePercent)
    {
        /// <summary>4 posters, 2 active at once, new posters every 4 hours (25 gems to post new ones now).</summary>
        public static BountyConfig Default { get; } = new BountyConfig(
            4,
            2,
            4 * 3600,
            25,
            new[] { "smith", "farmer", "captain", "priestess", "ranger" },
            new[]
            {
                new BountyDef(ActivityKinds.RunnerKills, 40, 120, 2 * 3600, 30),
                new BountyDef(ActivityKinds.RunnerWin, 1, 3, 3 * 3600, 20),
                new BountyDef(ActivityKinds.Harvest, 4, 10, 3 * 3600, 25),
                new BountyDef(ActivityKinds.Upgrade, 1, 2, 6 * 3600, 15),
                new BountyDef(ActivityKinds.HeroLevel, 1, 2, 4 * 3600, 10),
            },
            new[]
            {
                new TapRewardDef(SurvIds.ResSoft, 1500, 5000, 30),
                new TapRewardDef(SurvIds.ResWood, 800, 2500, 20),
                new TapRewardDef(SurvIds.ResStone, 600, 2000, 20),
                new TapRewardDef(SurvIds.ResFood, 800, 2500, 20),
                new TapRewardDef(SurvIds.ResSpeedupM, 10, 45, 15),
            },
            20);
    }

    public enum BountyState
    {
        Offered,
        Active,
        Done,
        Expired,
        Claimed,
    }

    public sealed class Bounty
    {
        public Bounty(string giver, string kind, int target, double limitSeconds, bool rare, Dictionary<string, int> reward)
        {
            Giver = giver;
            Kind = kind;
            Target = Math.Max(1, target);
            LimitSeconds = limitSeconds;
            Rare = rare;
            Reward = reward;
        }

        public string Giver { get; }
        public string Kind { get; }
        public int Target { get; }
        public double LimitSeconds { get; }
        public bool Rare { get; }
        public Dictionary<string, int> Reward { get; }
        public int Progress { get; internal set; }
        public double EndsAt { get; internal set; }
        public bool Accepted { get; internal set; }
        public bool Claimed { get; internal set; }

        public float Fraction => Math.Min(1f, Progress / (float)Target);

        public BountyState StateAt(double now)
        {
            if (Claimed)
            {
                return BountyState.Claimed;
            }

            if (!Accepted)
            {
                return BountyState.Offered;
            }

            if (Progress >= Target)
            {
                return BountyState.Done;
            }

            return now >= EndsAt ? BountyState.Expired : BountyState.Active;
        }
    }

    public enum BountyResult
    {
        Ok,
        NoSuchBounty,
        WrongState,
        TooManyActive,
        NotEnoughGems,
    }

    /// <summary>
    /// The Tavern Bounty Board: villagers pin posters with a task, a time limit and a reward. Accept up to
    /// <see cref="BountyConfig.MaxActive"/>; progress comes from <see cref="ActivityFeed"/>. Finished bounties are
    /// claimed; expired ones are lost. Every few real-time hours the board re-posts every poster that is not in
    /// progress (gems post new ones now). Rare posters (gold wax seal) pay double. Rewards are town economy only.
    /// </summary>
    public sealed class BountyBoard
    {
        private readonly Func<double> _now;
        private readonly Random _rng;
        private readonly List<Bounty> _slots = new List<Bounty>();

        public BountyBoard(BountyConfig config, Func<double> now, int seed)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _now = now ?? throw new ArgumentNullException(nameof(now));
            _rng = new Random(seed);
            Repost();
        }

        public BountyConfig Config { get; }
        public IReadOnlyList<Bounty> Slots => _slots;
        public double NextRefreshAt { get; private set; }
        public double Now => _now();
        public double SecondsUntilRefresh => Math.Max(0, NextRefreshAt - Now);

        public int ActiveCount
        {
            get
            {
                var n = 0;
                foreach (var b in _slots)
                {
                    if (b.StateAt(Now) == BountyState.Active)
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        /// <summary>Posters ready to claim (for the HUD dot).</summary>
        public int ReadyCount
        {
            get
            {
                var n = 0;
                foreach (var b in _slots)
                {
                    if (b.StateAt(Now) == BountyState.Done)
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        /// <summary>Re-post on the timer. Call regularly (cheap). True if posters changed.</summary>
        public bool Tick()
        {
            if (Now < NextRefreshAt)
            {
                return false;
            }

            Repost();
            return true;
        }

        public BountyResult Accept(int slot)
        {
            if (slot < 0 || slot >= _slots.Count)
            {
                return BountyResult.NoSuchBounty;
            }

            var b = _slots[slot];
            if (b.StateAt(Now) != BountyState.Offered)
            {
                return BountyResult.WrongState;
            }

            if (ActiveCount >= Config.MaxActive)
            {
                return BountyResult.TooManyActive;
            }

            b.Accepted = true;
            b.EndsAt = Now + b.LimitSeconds;
            return BountyResult.Ok;
        }

        /// <summary>Count activity toward every active bounty of that kind. Returns how many it just finished.</summary>
        public int Report(string kind, int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            var finished = 0;
            foreach (var b in _slots)
            {
                if (b.StateAt(Now) == BountyState.Active && string.Equals(b.Kind, kind, StringComparison.Ordinal))
                {
                    b.Progress = Math.Min(b.Target, b.Progress + amount);
                    if (b.Progress >= b.Target)
                    {
                        finished++;
                    }
                }
            }

            return finished;
        }

        /// <summary>Claim a finished bounty. The caller grants the returned bag (celebration). Null if not done.</summary>
        public Dictionary<string, int>? Claim(int slot)
        {
            if (slot < 0 || slot >= _slots.Count || _slots[slot].StateAt(Now) != BountyState.Done)
            {
                return null;
            }

            _slots[slot].Claimed = true;
            return new Dictionary<string, int>(_slots[slot].Reward);
        }

        /// <summary>Pay gems to re-post every poster that is not in progress or waiting to be claimed.</summary>
        public BountyResult BuyRefresh(Wallet wallet)
        {
            if (!wallet.TrySpend(SurvIds.ResHard, Config.RefreshGems))
            {
                return BountyResult.NotEnoughGems;
            }

            Repost();
            return BountyResult.Ok;
        }

        private void Repost()
        {
            var now = Now;
            for (var i = 0; i < Config.Slots; i++)
            {
                if (i < _slots.Count)
                {
                    var s = _slots[i].StateAt(now);
                    if (s == BountyState.Active || s == BountyState.Done)
                    {
                        continue;
                    }

                    _slots[i] = Roll();
                }
                else
                {
                    _slots.Add(Roll());
                }
            }

            NextRefreshAt = now + Config.RefreshEverySeconds;
        }

        private Bounty Roll()
        {
            var weights = new List<int>();
            foreach (var d in Config.Bounties)
            {
                weights.Add(d.Weight);
            }

            var def = Config.Bounties[TapKit.Pick(weights, _rng)];
            var target = def.Max <= def.Min ? def.Min : _rng.Next(def.Min, def.Max + 1);
            var giver = Config.Givers[_rng.Next(Config.Givers.Count)];
            var rare = _rng.Next(100) < Config.RareChancePercent;
            var reward = TapKit.Roll(Config.Rewards, 2, _rng);
            if (rare)
            {
                var keys = new List<string>(reward.Keys);
                foreach (var k in keys)
                {
                    reward[k] *= 2;
                }
            }

            return new Bounty(giver, def.Kind, target, def.LimitSeconds, rare, reward);
        }

        // ---------- save ----------

        public const string PrefsKey = "survival.taps.bounty.v1";

        public string Capture()
        {
            var sb = new StringBuilder("{\"v\":1,\"next\":");
            TapKit.Num(sb, NextRefreshAt).Append(",\"slots\":[");
            for (var i = 0; i < _slots.Count; i++)
            {
                var b = _slots[i];
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append("{\"giver\":");
                TapKit.Str(sb, b.Giver).Append(",\"kind\":");
                TapKit.Str(sb, b.Kind).Append(",\"target\":").Append(b.Target).Append(",\"progress\":").Append(b.Progress);
                sb.Append(",\"limit\":");
                TapKit.Num(sb, b.LimitSeconds).Append(",\"ends\":");
                TapKit.Num(sb, b.EndsAt).Append(",\"accepted\":").Append(b.Accepted ? "true" : "false");
                sb.Append(",\"claimed\":").Append(b.Claimed ? "true" : "false").Append(",\"rare\":").Append(b.Rare ? "true" : "false");
                sb.Append(",\"reward\":");
                TapKit.Bag(sb, b.Reward).Append('}');
            }

            return sb.Append("]}").ToString();
        }

        /// <summary>Restore a <see cref="Capture"/>; then catches up on any refresh that fell due while away.</summary>
        public bool Apply(string? json)
        {
            var o = TapKit.ParseObj(json);
            if (o == null)
            {
                return false;
            }

            var loaded = new List<Bounty>();
            foreach (var s in TapKit.Items(o, "slots"))
            {
                var kind = TapKit.S(s, "kind");
                if (kind == null)
                {
                    continue;
                }

                var b = new Bounty(TapKit.S(s, "giver") ?? "smith", kind, TapKit.I(s, "target", 1), TapKit.D(s, "limit", 3600), TapKit.B(s, "rare"), TapKit.BagOf(s, "reward"))
                {
                    Progress = TapKit.I(s, "progress"),
                    EndsAt = TapKit.D(s, "ends"),
                    Accepted = TapKit.B(s, "accepted"),
                    Claimed = TapKit.B(s, "claimed"),
                };
                loaded.Add(b);
            }

            if (loaded.Count == 0)
            {
                return false;
            }

            _slots.Clear();
            _slots.AddRange(loaded);
            NextRefreshAt = TapKit.D(o, "next", Now);
            Tick();
            return true;
        }
    }
}
