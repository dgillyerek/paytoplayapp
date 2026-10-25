#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Survival.Domain.Campaign;
using Survival.Domain.Ids;
using Survival.Domain.Runner;
using Survival.Domain.Serialization;

namespace Survival.Domain.Store
{
    /// <summary>One rung of the power-up ladder: its store offer, the reference USD price (value maths only), one-time or not.</summary>
    public sealed record PowerPackDef(string OfferId, double Usd, bool OneTime);

    /// <summary>A rung with its store offer and its value: gem-equivalents, per dollar, and the bonus over the baseline rung.</summary>
    public sealed record PowerPackRow(PowerPackDef Pack, OfferDef Offer, double Value, double ValuePerUsd, double BonusPct)
    {
        /// <summary>"+95%" style: the bonus rounded down to 5 (the baseline rung has none).</summary>
        public string BonusLabel => BonusPct < 5 ? "" : "+" + ((int)(Math.Floor(BonusPct / 5.0) * 5)).ToString(CultureInfo.InvariantCulture) + "%";

        public IReadOnlyDictionary<string, int> Grants => Offer.Grants;

        public int Grant(string resourceId) => Offer.Grants.TryGetValue(resourceId, out var v) ? v : 0;
    }

    /// <summary>
    /// power_packs.json: the power-up ladder over store.power_packs (stores.json has each pack's sku and grants; the theme
    /// pack's products.json its product id and price label). Grants are wallet resources only: gold, food, gems, speed-up
    /// minutes, Hero Tomes (res.hero_xp: one instant level each) and War Banner charges (res.runner_boost).
    /// </summary>
    public sealed class PowerPackCatalog
    {
        public const string FileName = "power_packs.json";

        public string StoreId { get; private init; } = SurvIds.StorePowerPacks;
        public string BaselineOfferId { get; private init; } = SurvIds.OfferPowerT1;
        public IReadOnlyDictionary<string, double> ValueRates { get; private init; } = new Dictionary<string, double>();
        public double BoostDamagePct { get; private init; }
        public double BoostHpPct { get; private init; }
        public IReadOnlyList<PowerPackDef> Packs { get; private init; } = Array.Empty<PowerPackDef>();

        public double BoostDamageMul => 1 + (BoostDamagePct / 100.0);
        public double BoostHpMul => 1 + (BoostHpPct / 100.0);

        public PowerPackDef? Pack(string offerId) => Packs.FirstOrDefault(p => p.OfferId == offerId);

        public static PowerPackCatalog Parse(string json)
        {
            var root = LayoutJson.Root(json, FileName);
            var rates = new Dictionary<string, double>(StringComparer.Ordinal);
            if (root.TryGet("value_rates", out var rn) && rn is JsonNode.Obj r)
            {
                foreach (var f in r.Fields)
                {
                    if (f.Value is JsonNode.Num n)
                    {
                        rates[f.Key] = n.Value;
                    }
                }
            }

            var boost = root.TryGet("runner_boost", out var bn) && bn is JsonNode.Obj b ? b : new JsonNode.Obj();
            var packs = LayoutJson.Objs(root, "packs")
                .Select(p => new PowerPackDef(LayoutJson.Str(p, "offer"), Usd(p), LayoutJson.OptBool(p, "one_time", false)))
                .ToList();
            if (packs.Any(p => p.Usd <= 0))
            {
                throw new FormatException(FileName + ": every pack needs a usd price above 0.");
            }

            return new PowerPackCatalog
            {
                StoreId = LayoutJson.OptStr(root, "store", SurvIds.StorePowerPacks),
                BaselineOfferId = LayoutJson.OptStr(root, "baseline", SurvIds.OfferPowerT1),
                ValueRates = rates,
                BoostDamagePct = Math.Max(0, LayoutJson.OptNum(boost, "damage_pct", 0f)),
                BoostHpPct = Math.Max(0, LayoutJson.OptNum(boost, "hp_pct", 0f)),
                Packs = packs
            };
        }

        private static double Usd(JsonNode.Obj p) =>
            p.TryGet("usd", out var n) && n is JsonNode.Num num ? num.Value : throw new FormatException(FileName + ": every pack needs a number 'usd'.");

        /// <summary>Gem-equivalent value of <paramref name="grants"/> (resources without a rate count 0).</summary>
        public double Value(IReadOnlyDictionary<string, int> grants) =>
            grants.Sum(g => ValueRates.TryGetValue(g.Key, out var rate) ? rate * g.Value : 0);

        /// <summary>Every pack with its offer in <paramref name="store"/>, in file order (packs without an offer are skipped).</summary>
        public IReadOnlyList<PowerPackRow> Rows(StoreDef store)
        {
            var offers = store.Offers.ToDictionary(o => o.Id, StringComparer.Ordinal);
            var basePack = Pack(BaselineOfferId);
            var basePerUsd = basePack != null && offers.TryGetValue(basePack.OfferId, out var bo) ? Value(bo.Grants) / basePack.Usd : 0;
            var rows = new List<PowerPackRow>();
            foreach (var p in Packs)
            {
                if (!offers.TryGetValue(p.OfferId, out var offer))
                {
                    continue;
                }

                var value = Value(offer.Grants);
                var perUsd = value / p.Usd;
                rows.Add(new PowerPackRow(p, offer, value, perUsd, basePerUsd > 0 ? ((perUsd / basePerUsd) - 1) * 100 : 0));
            }

            return rows;
        }
    }

    /// <summary>
    /// The power-pack shop: buys a rung through the store's purchase service (sandbox in this build; no real billing) into
    /// the campaign wallet, and keeps one-time offers to one purchase.
    /// </summary>
    public sealed class PowerPackShop
    {
        public PowerPackShop(PowerPackCatalog catalog, StoreSystem store, CampaignSession campaign)
        {
            Catalog = catalog;
            Store = store;
            Campaign = campaign;
        }

        public PowerPackCatalog Catalog { get; }
        public StoreSystem Store { get; }
        public CampaignSession Campaign { get; }

        /// <summary>The rungs on sale now (a bought one-time offer is gone).</summary>
        public IReadOnlyList<PowerPackRow> Available()
        {
            var store = Store.Find(Catalog.StoreId);
            return store == null
                ? Array.Empty<PowerPackRow>()
                : Catalog.Rows(store).Where(r => !(r.Pack.OneTime && Campaign.HasBought(r.Pack.OfferId))).ToList();
        }

        public string PriceLabel(PowerPackRow row) =>
            Store.ProductFor(row.Offer.Sku)?.PriceLabel is { Length: > 0 } label ? label : "$" + row.Pack.Usd.ToString("0.00", CultureInfo.InvariantCulture);

        public void Buy(string offerId, Action<PurchaseOutcome> done)
        {
            var pack = Catalog.Pack(offerId);
            if (pack == null)
            {
                done(new PurchaseOutcome(false, "", "", "store.unknown_offer"));
                return;
            }

            if (pack.OneTime && Campaign.HasBought(offerId))
            {
                done(new PurchaseOutcome(false, Store.ProductFor(Store.FindOffer(offerId)?.Sku)?.ProductId ?? "", "", "store.already_bought"));
                return;
            }

            if (!ReferenceEquals(Store.Wallet, Campaign.Wallet))
            {
                throw new InvalidOperationException("Power packs grant into the store wallet; it must be the campaign's wallet.");
            }

            Store.BuySku(offerId, outcome =>
            {
                if (outcome.Ok)
                {
                    if (pack.OneTime)
                    {
                        Campaign.MarkBought(offerId);
                    }

                    Campaign.Touch();
                }

                done(outcome);
            });
        }
    }

    /// <summary>
    /// War Banners (res.runner_boost): while the wallet holds a charge, every hero unit in a runner level hits harder and
    /// has more HP (power_packs.json runner_boost); RunnerRewards.Grant spends one charge per new runner level cleared.
    /// </summary>
    public static class RunnerBanner
    {
        public static bool Active(CampaignSession campaign) => campaign.Wallet.Get(SurvIds.ResRunnerBoost) > 0;

        /// <summary>The saved heroes' loadout, boosted when a banner charge is held.</summary>
        public static RunnerLoadout Loadout(RunnerCatalog runner, CampaignSession campaign, PowerPackCatalog? packs)
        {
            var loadout = RunnerLoadout.From(runner, campaign.Roster);
            return packs != null && Active(campaign) ? loadout.Boosted(runner, packs.BoostDamageMul, packs.BoostHpMul) : loadout;
        }
    }
}
