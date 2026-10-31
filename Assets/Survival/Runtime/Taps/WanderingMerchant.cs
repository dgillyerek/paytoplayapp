#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Survival.Domain.Economy;
using Survival.Domain.Ids;

namespace Survival.Domain.Taps
{
    public static class MerchantDealKinds
    {
        /// <summary>Pay town resources (gold, wood, food...).</summary>
        public const string Trade = "trade";

        /// <summary>Pay gems.</summary>
        public const string Gems = "gems";

        /// <summary>Real-money bundle through the store's purchase service (sandbox in dev).</summary>
        public const string Sku = "sku";

        /// <summary>A gift: no price.</summary>
        public const string Free = "free";
    }

    public sealed record MerchantDeal(
        string Id,
        string Kind,
        IReadOnlyDictionary<string, int> Price,
        IReadOnlyDictionary<string, int> Grants,
        int Weight,
        string? Sku = null);

    public sealed record MerchantConfig(
        double StaySeconds,
        double GapMinSeconds,
        double GapMaxSeconds,
        int FreeChancePercent,
        IReadOnlyList<MerchantDeal> Deals)
    {
        public const string BundleSku = "sku.merchant_bundle";

        private static Dictionary<string, int> B(params (string id, int n)[] items)
        {
            var d = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var (id, n) in items)
            {
                d[id] = n;
            }

            return d;
        }

        /// <summary>
        /// Stays 1 hour, comes back 2–5 hours after leaving, 35% of visits include one FREE gift. Each visit shows one
        /// trade (town resources), one gem deal and one real-money bundle. Grants are town economy only.
        /// </summary>
        public static MerchantConfig Default { get; } = new MerchantConfig(
            3600,
            2 * 3600,
            5 * 3600,
            35,
            new[]
            {
                new MerchantDeal("trade_gold_speed", MerchantDealKinds.Trade, B((SurvIds.ResSoft, 3000)), B((SurvIds.ResSpeedupM, 30)), 30),
                new MerchantDeal("trade_wood_stone", MerchantDealKinds.Trade, B((SurvIds.ResWood, 2000)), B((SurvIds.ResStone, 2500)), 25),
                new MerchantDeal("trade_food_gold", MerchantDealKinds.Trade, B((SurvIds.ResFood, 2500)), B((SurvIds.ResSoft, 4000)), 25),
                new MerchantDeal("trade_stone_wood", MerchantDealKinds.Trade, B((SurvIds.ResStone, 2000)), B((SurvIds.ResWood, 2500)), 20),
                new MerchantDeal("gems_gold", MerchantDealKinds.Gems, B((SurvIds.ResHard, 30)), B((SurvIds.ResSoft, 9000)), 30),
                new MerchantDeal("gems_speed", MerchantDealKinds.Gems, B((SurvIds.ResHard, 40)), B((SurvIds.ResSpeedupM, 90)), 30),
                new MerchantDeal("gems_build", MerchantDealKinds.Gems, B((SurvIds.ResHard, 25)), B((SurvIds.ResWood, 3000), (SurvIds.ResStone, 3000)), 25),
                new MerchantDeal("bundle_caravan", MerchantDealKinds.Sku, B(), B((SurvIds.ResHard, 150), (SurvIds.ResSoft, 12000), (SurvIds.ResSpeedupM, 60)), 1, BundleSku),
                new MerchantDeal("free_gold", MerchantDealKinds.Free, B(), B((SurvIds.ResSoft, 1500)), 40),
                new MerchantDeal("free_speed", MerchantDealKinds.Free, B(), B((SurvIds.ResSpeedupM, 15)), 30),
                new MerchantDeal("free_food", MerchantDealKinds.Free, B(), B((SurvIds.ResFood, 1200)), 30),
            });
    }

    public sealed class MerchantOffer
    {
        public MerchantOffer(MerchantDeal deal, bool bought = false)
        {
            Deal = deal;
            Bought = bought;
        }

        public MerchantDeal Deal { get; }
        public bool Bought { get; internal set; }
        public bool IsFree => Deal.Kind == MerchantDealKinds.Free;
    }

    public enum MerchantBuy
    {
        Ok,
        NotHere,
        NoSuchOffer,
        AlreadyBought,
        CannotAfford,
        UseStore,
    }

    /// <summary>
    /// The Wandering Merchant: a caravan that parks in the village for an hour at random real-time intervals with
    /// three one-off offers (a trade, a gem deal, a real-money bundle); some visits swap the trade or gem deal for a
    /// FREE gift. Offline-safe: <see cref="Tick"/> catches up on visits that came and went while the app was closed.
    /// </summary>
    public sealed class WanderingMerchant
    {
        private readonly Func<double> _now;
        private readonly Random _rng;
        private readonly List<MerchantOffer> _offers = new List<MerchantOffer>();

        public WanderingMerchant(MerchantConfig config, Func<double> now, int seed)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _now = now ?? throw new ArgumentNullException(nameof(now));
            _rng = new Random(seed);
            ArrivesAt = _now();
            LeavesAt = ArrivesAt + Config.StaySeconds;
            Roll();
        }

        public MerchantConfig Config { get; }
        public double ArrivesAt { get; private set; }
        public double LeavesAt { get; private set; }
        public double Now => _now();
        public bool IsHere => Now >= ArrivesAt && Now < LeavesAt;
        public double SecondsLeft => Math.Max(0, LeavesAt - Now);
        public double SecondsUntilArrival => Math.Max(0, ArrivesAt - Now);
        public IReadOnlyList<MerchantOffer> Offers => _offers;

        public bool HasFreeWaiting
        {
            get
            {
                if (!IsHere)
                {
                    return false;
                }

                foreach (var o in _offers)
                {
                    if (o.IsFree && !o.Bought)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>Advance visits. True when a new visit was scheduled (offers changed).</summary>
        public bool Tick()
        {
            var changed = false;
            while (Now >= LeavesAt)
            {
                var gap = Config.GapMinSeconds + _rng.NextDouble() * (Config.GapMaxSeconds - Config.GapMinSeconds);
                ArrivesAt = LeavesAt + gap;
                LeavesAt = ArrivesAt + Config.StaySeconds;
                Roll();
                changed = true;
            }

            return changed;
        }

        /// <summary>Buy a trade, gem deal or free gift. Spends from <paramref name="wallet"/>; caller grants the returned bag.</summary>
        public MerchantBuy TryBuy(int index, Wallet wallet, out IReadOnlyDictionary<string, int>? grants)
        {
            grants = null;
            var check = Check(index);
            if (check != MerchantBuy.Ok)
            {
                return check;
            }

            var offer = _offers[index];
            if (offer.Deal.Kind == MerchantDealKinds.Sku)
            {
                return MerchantBuy.UseStore;
            }

            if (offer.Deal.Price.Count > 0 && !wallet.TrySpend(offer.Deal.Price))
            {
                return MerchantBuy.CannotAfford;
            }

            offer.Bought = true;
            grants = offer.Deal.Grants;
            return MerchantBuy.Ok;
        }

        /// <summary>Whether an offer can be bought right now (before starting a store purchase).</summary>
        public MerchantBuy Check(int index)
        {
            if (!IsHere)
            {
                return MerchantBuy.NotHere;
            }

            if (index < 0 || index >= _offers.Count)
            {
                return MerchantBuy.NoSuchOffer;
            }

            return _offers[index].Bought ? MerchantBuy.AlreadyBought : MerchantBuy.Ok;
        }

        /// <summary>Record a successful store purchase of the bundle. Caller grants the deal's bag.</summary>
        public bool MarkBought(int index)
        {
            if (index < 0 || index >= _offers.Count || _offers[index].Bought)
            {
                return false;
            }

            _offers[index].Bought = true;
            return true;
        }

        private void Roll()
        {
            _offers.Clear();
            _offers.Add(new MerchantOffer(PickOf(MerchantDealKinds.Trade)));
            _offers.Add(new MerchantOffer(PickOf(MerchantDealKinds.Gems)));
            _offers.Add(new MerchantOffer(PickOf(MerchantDealKinds.Sku)));
            if (_rng.Next(100) < Config.FreeChancePercent)
            {
                _offers[_rng.Next(2)] = new MerchantOffer(PickOf(MerchantDealKinds.Free));
            }
        }

        private MerchantDeal PickOf(string kind)
        {
            var pool = new List<MerchantDeal>();
            var weights = new List<int>();
            foreach (var d in Config.Deals)
            {
                if (d.Kind == kind)
                {
                    pool.Add(d);
                    weights.Add(d.Weight);
                }
            }

            if (pool.Count == 0)
            {
                throw new InvalidOperationException("Merchant has no deals of kind " + kind);
            }

            return pool[TapKit.Pick(weights, _rng)];
        }

        private MerchantDeal? Find(string id)
        {
            foreach (var d in Config.Deals)
            {
                if (d.Id == id)
                {
                    return d;
                }
            }

            return null;
        }

        // ---------- save ----------

        public const string PrefsKey = "survival.taps.merchant.v1";

        public string Capture()
        {
            var sb = new StringBuilder("{\"v\":1,\"arrives\":");
            TapKit.Num(sb, ArrivesAt).Append(",\"leaves\":");
            TapKit.Num(sb, LeavesAt).Append(",\"offers\":[");
            for (var i = 0; i < _offers.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append("{\"id\":");
                TapKit.Str(sb, _offers[i].Deal.Id).Append(",\"bought\":").Append(_offers[i].Bought ? "true" : "false").Append('}');
            }

            return sb.Append("]}").ToString();
        }

        public bool Apply(string? json)
        {
            var o = TapKit.ParseObj(json);
            if (o == null)
            {
                return false;
            }

            var offers = new List<MerchantOffer>();
            foreach (var item in TapKit.Items(o, "offers"))
            {
                var deal = Find(TapKit.S(item, "id") ?? "");
                if (deal != null)
                {
                    offers.Add(new MerchantOffer(deal, TapKit.B(item, "bought")));
                }
            }

            if (offers.Count == 0)
            {
                return false;
            }

            _offers.Clear();
            _offers.AddRange(offers);
            ArrivesAt = TapKit.D(o, "arrives", Now);
            LeavesAt = TapKit.D(o, "leaves", ArrivesAt + Config.StaySeconds);
            Tick();
            return true;
        }
    }
}
