#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Campaign;
using Survival.Domain.Clock;
using Survival.Domain.Economy;
using Survival.Domain.Ids;
using Survival.Domain.Serialization;
using Survival.Domain.Store;

namespace Survival.Domain.Crusade
{
    /// <summary>Where season XP came from (amounts in <see cref="CrusadeBanner.XpFor"/>).</summary>
    public enum CrusadeXpSource
    {
        RunnerLevel,
        Upgrade,
        Quest,
    }

    public sealed class CrusadeTier
    {
        public CrusadeTier(int number, IReadOnlyDictionary<string, int> free, IReadOnlyDictionary<string, int> premium)
        {
            Number = number;
            Free = free;
            Premium = premium;
        }

        /// <summary>1-based tier number.</summary>
        public int Number { get; }
        public IReadOnlyDictionary<string, int> Free { get; }
        public IReadOnlyDictionary<string, int> Premium { get; }
    }

    /// <summary>
    /// Crusade Banner season pass. Seasons are 28 real days on a fixed UTC calendar (everyone's season ends at the same
    /// moment). Season XP comes from clearing runner levels, finishing upgrades and quests; every 250 XP opens a tier.
    /// Each tier has a free reward and a premium reward; premium needs the Royal Banner (sandbox SKU sku.pass_season,
    /// $9.99), which also unlocks premium rewards of tiers already reached. A new season wipes XP, claims and the
    /// banner. Saved in crusade_banner.json.
    /// </summary>
    public sealed class CrusadeBanner
    {
        public const string FileName = "crusade_banner.json";
        public const string StoreId = "store.crusade_banner";
        public const string OfferId = "offer.crusade_premium";
        public const string Sku = SurvIds.SkuPassSeason;
        public const string FallbackPrice = "$9.99";
        public const string FallbackProductId = "com.paytoplay.fantasykingdoma.pass_season";

        public const int TierCount = 20;
        public const int XpPerTier = 250;
        public const double SeasonSeconds = 28 * 86400.0;

        /// <summary>Season 1 starts Monday 2026-01-05 00:00 UTC.</summary>
        public const double SeasonEpoch = 1767571200;

        public static readonly IReadOnlyList<CrusadeTier> DefaultTiers = BuildTiers();

        private readonly HashSet<int> _freeClaimed = new();
        private readonly HashSet<int> _premiumClaimed = new();

        public CrusadeBanner(IGameClock clock, IReadOnlyList<CrusadeTier>? tiers = null)
        {
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Tiers = tiers ?? DefaultTiers;
            Season = CurrentSeason;
        }

        public IGameClock Clock { get; }
        public IReadOnlyList<CrusadeTier> Tiers { get; }

        /// <summary>Season this progress belongs to (1-based).</summary>
        public int Season { get; private set; }
        public int Xp { get; private set; }
        public bool HasPremium { get; private set; }

        /// <summary>Last runner level count seen, so XP is paid only for levels cleared after the banner was set up.</summary>
        public int RunnerSeen { get; set; } = -1;

        public event Action? Changed;

        public int CurrentSeason => 1 + (int)Math.Floor(Math.Max(0, Clock.Now - SeasonEpoch) / SeasonSeconds);
        public double SeasonEndsAt => SeasonEpoch + (CurrentSeason * SeasonSeconds);
        public double SecondsLeft => Math.Max(0, SeasonEndsAt - Clock.Now);

        /// <summary>Tiers reached (0..TierCount).</summary>
        public int TierReached => Math.Min(Tiers.Count, Xp / XpPerTier);

        /// <summary>XP inside the current tier (for the bar), and whether the track is maxed.</summary>
        public int XpIntoTier => TierReached >= Tiers.Count ? XpPerTier : Xp % XpPerTier;

        public static int XpFor(CrusadeXpSource source) => source switch
        {
            CrusadeXpSource.RunnerLevel => 120,
            CrusadeXpSource.Upgrade => 60,
            CrusadeXpSource.Quest => 80,
            _ => 0,
        };

        /// <summary>Starts a fresh season when the calendar has moved on. True if it rolled.</summary>
        public bool RollSeason()
        {
            var now = CurrentSeason;
            if (now == Season)
            {
                return false;
            }

            Season = now;
            Xp = 0;
            HasPremium = false;
            _freeClaimed.Clear();
            _premiumClaimed.Clear();
            Changed?.Invoke();
            return true;
        }

        public void AddXp(CrusadeXpSource source, int times = 1)
        {
            if (times <= 0)
            {
                return;
            }

            RollSeason();
            Xp = Math.Min(Tiers.Count * XpPerTier, Xp + (XpFor(source) * times));
            Changed?.Invoke();
        }

        public bool IsClaimed(int tier, bool premium) => (premium ? _premiumClaimed : _freeClaimed).Contains(tier);

        public bool CanClaim(int tier, bool premium) =>
            tier >= 1 && tier <= TierReached && !IsClaimed(tier, premium) && (!premium || HasPremium);

        /// <summary>Number of rewards ready to tap (the rail badge).</summary>
        public int ReadyCount
        {
            get
            {
                var n = 0;
                for (var t = 1; t <= TierReached; t++)
                {
                    if (CanClaim(t, false))
                    {
                        n++;
                    }

                    if (CanClaim(t, true))
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        public IReadOnlyDictionary<string, int>? Claim(int tier, bool premium, Wallet wallet)
        {
            RollSeason();
            if (!CanClaim(tier, premium))
            {
                return null;
            }

            var row = Tiers[tier - 1];
            var reward = premium ? row.Premium : row.Free;
            (premium ? _premiumClaimed : _freeClaimed).Add(tier);
            wallet.Grant(reward);
            Changed?.Invoke();
            return reward;
        }

        /// <summary>Called after the store confirms the SKU purchase.</summary>
        public void UnlockPremium()
        {
            RollSeason();
            if (!HasPremium)
            {
                HasPremium = true;
                Changed?.Invoke();
            }
        }

        /// <summary>The one SKU offer, priced from products.json (sku.pass_season). Grants nothing itself: it unlocks the column.</summary>
        public static OfferDef PremiumOffer() =>
            new OfferDef(OfferId, OfferKinds.Sku, Sku, new Dictionary<string, int>(), new Dictionary<string, int>(), "season");

        /// <summary>A store holding only the banner offer, sharing the game's purchase service and wallet.</summary>
        public static StoreSystem MakeStore(ProductDef? product, ISkuPurchaseService purchases, Wallet wallet) =>
            new StoreSystem(
                new[] { new StoreDef(StoreId, new[] { PremiumOffer() }) },
                new Dictionary<string, ProductDef> { [Sku] = product ?? new ProductDef(Sku, FallbackProductId, FallbackPrice) },
                purchases,
                wallet);

        public string Save()
        {
            var w = new JsonWriter();
            w.Begin();
            w.Field("version", 1);
            w.Field("season", Season);
            w.Field("xp", Xp);
            w.Field("premium", HasPremium);
            w.Field("runner_seen", RunnerSeen);
            Ints(w, "free", _freeClaimed);
            Ints(w, "prem", _premiumClaimed);
            w.End();
            return w.ToString();
        }

        public bool Load(string json)
        {
            try
            {
                if (JsonRead.Parse(json) is not JsonNode.Obj o)
                {
                    return false;
                }

                Season = CampaignCatalog.Int(o, "season", CurrentSeason);
                Xp = Math.Max(0, CampaignCatalog.Int(o, "xp", 0));
                HasPremium = CampaignCatalog.Bool(o, "premium");
                RunnerSeen = CampaignCatalog.Int(o, "runner_seen", -1);
                ReadInts(o, "free", _freeClaimed);
                ReadInts(o, "prem", _premiumClaimed);
                RollSeason();
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static void Ints(JsonWriter w, string key, HashSet<int> set)
        {
            var list = new List<int>(set);
            list.Sort();
            w.Key(key);
            w.BeginArray();
            foreach (var i in list)
            {
                w.Value(i.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            w.EndArray();
        }

        private static void ReadInts(JsonNode.Obj o, string key, HashSet<int> set)
        {
            set.Clear();
            foreach (var n in CampaignCatalog.Items(o, key))
            {
                if (n is JsonNode.Str s && int.TryParse(s.Value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var v))
                {
                    set.Add(v);
                }
            }
        }

        private static IReadOnlyList<CrusadeTier> BuildTiers()
        {
            var list = new List<CrusadeTier>();
            for (var t = 1; t <= TierCount; t++)
            {
                var free = new Dictionary<string, int>();
                var prem = new Dictionary<string, int>();
                switch (t % 4)
                {
                    case 1: free[SurvIds.ResSoft] = 3000 + (t * 500); break;
                    case 2: free[SurvIds.ResFood] = 3000 + (t * 400); break;
                    case 3: free[SurvIds.ResSpeedupM] = 10; break;
                    default: free[SurvIds.ResHard] = 10; break;
                }

                prem[SurvIds.ResHard] = 20 + (t * 2);
                if (t % 4 == 0)
                {
                    prem[SurvIds.ResHeroXp] = 1;
                }
                else
                {
                    prem[SurvIds.ResSpeedupM] = 30;
                }

                if (t == TierCount)
                {
                    free.Clear();
                    free[SurvIds.ResHard] = 50;
                    free[SurvIds.ResSoft] = 20000;
                    prem.Clear();
                    prem[SurvIds.ResHard] = 300;
                    prem[SurvIds.ResHeroXp] = 5;
                    prem[SurvIds.ResSpeedupM] = 240;
                }

                list.Add(new CrusadeTier(t, free, prem));
            }

            return list;
        }
    }
}
