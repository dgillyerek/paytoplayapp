#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Survival.Domain.Campaign;
using Survival.Domain.Ids;
using Survival.Domain.Serialization;

namespace Survival.Domain.Store
{
    /// <summary>The three reward columns: Free for everyone, Premium with the $2.99 pass, Royal with the $3.99 pass (which also opens Premium).</summary>
    public enum TrackColumn
    {
        Free = 0,
        Premium = 1,
        Royal = 2
    }

    /// <summary>What the player owns: nothing, the $2.99 Premium pass, or the $3.99 Royal pass (Premium + Royal columns).</summary>
    public enum TrackTier
    {
        None = 0,
        Premium = 1,
        Royal = 2
    }

    /// <summary>A cell's state for the UI.</summary>
    public enum TrackCellState
    {
        /// <summary>Milestone not reached yet (and the column is open, or will be).</summary>
        Upcoming,

        /// <summary>Reached and owned: claim it now.</summary>
        Claimable,

        /// <summary>Already taken.</summary>
        Claimed,

        /// <summary>The column needs a pass the player has not bought (reached or not).</summary>
        Locked
    }

    /// <summary>One milestone row: the runner level that opens it and the rewards in each column.</summary>
    public sealed record TrackRow(int Level, IReadOnlyDictionary<string, int> Free, IReadOnlyDictionary<string, int> Premium, IReadOnlyDictionary<string, int> Royal)
    {
        public IReadOnlyDictionary<string, int> Rewards(TrackColumn c) => c switch
        {
            TrackColumn.Free => Free,
            TrackColumn.Premium => Premium,
            _ => Royal
        };
    }

    /// <summary>
    /// reward_track.json: the Kingdom Path, a three-column reward track over runner levels cleared. Rewards are wallet
    /// resources (gold, food, gems, speed-up minutes, Hero Tomes res.hero_xp, War Banners res.runner_boost) or hero shards
    /// keyed "shard:&lt;hero id&gt;". Offers live in stores.json (store.reward_track), product ids in the theme's products.json.
    /// </summary>
    public sealed class RewardTrackCatalog
    {
        public const string FileName = "reward_track.json";
        public const string ShardPrefix = "shard:";

        public string Title { get; private init; } = "Kingdom Path";
        public string PremiumOffer { get; private init; } = SurvIds.OfferTrackPremium;
        public string RoyalOffer { get; private init; } = SurvIds.OfferTrackRoyal;
        public string UpgradeOffer { get; private init; } = SurvIds.OfferTrackRoyalUpgrade;
        public IReadOnlyList<TrackRow> Rows { get; private init; } = Array.Empty<TrackRow>();

        public static RewardTrackCatalog Parse(string json)
        {
            var root = LayoutJson.Root(json, FileName);
            var rows = LayoutJson.Objs(root, "rows")
                .Select(r => new TrackRow(LayoutJson.OptInt(r, "level", 0), Grants(r, "free"), Grants(r, "premium"), Grants(r, "royal")))
                .ToList();
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].Level <= 0 || (i > 0 && rows[i].Level <= rows[i - 1].Level))
                {
                    throw new FormatException(FileName + ": row levels must be positive and strictly increasing.");
                }
            }

            return new RewardTrackCatalog
            {
                Title = LayoutJson.OptStr(root, "title", "Kingdom Path"),
                PremiumOffer = LayoutJson.OptStr(root, "premium_offer", SurvIds.OfferTrackPremium),
                RoyalOffer = LayoutJson.OptStr(root, "royal_offer", SurvIds.OfferTrackRoyal),
                UpgradeOffer = LayoutJson.OptStr(root, "upgrade_offer", SurvIds.OfferTrackRoyalUpgrade),
                Rows = rows
            };
        }

        private static IReadOnlyDictionary<string, int> Grants(JsonNode.Obj row, string key)
        {
            var d = new Dictionary<string, int>(StringComparer.Ordinal);
            if (row.TryGet(key, out var n) && n is JsonNode.Obj o)
            {
                foreach (var f in o.Fields)
                {
                    if (f.Value is JsonNode.Num v && v.Value > 0)
                    {
                        d[f.Key] = (int)v.Value;
                    }
                }
            }

            return d;
        }
    }

    /// <summary>
    /// The player's Kingdom Path: which passes they own (one-time store offers in the campaign save), which cells they
    /// claimed (saved as "track_claimed"), and progress (runner levels cleared). Buying a pass later opens every row
    /// already reached for claiming. Premium then Royal is an upgrade: the Royal button sells the $0.99 upgrade SKU.
    /// The free (no-spend) pace model (RunnerBalance) does not read this track; see RewardTrackTests.
    /// </summary>
    public sealed class RewardTrack
    {
        public RewardTrack(RewardTrackCatalog catalog, StoreSystem store, CampaignSession campaign)
        {
            Catalog = catalog;
            Store = store;
            Campaign = campaign;
        }

        public RewardTrackCatalog Catalog { get; }
        public StoreSystem Store { get; }
        public CampaignSession Campaign { get; }

        /// <summary>Runner levels beaten so far (the track's progress).</summary>
        public int Progress => Math.Max(0, Campaign.RunnerUnlocked - 1);

        public TrackTier Tier =>
            Campaign.HasBought(Catalog.RoyalOffer) || Campaign.HasBought(Catalog.UpgradeOffer) ? TrackTier.Royal
            : Campaign.HasBought(Catalog.PremiumOffer) ? TrackTier.Premium
            : TrackTier.None;

        public bool Owns(TrackColumn c) => (int)Tier >= (int)c;

        public static string CellKey(int level, TrackColumn c) => level + ":" + c.ToString().ToLowerInvariant();

        public bool IsClaimed(TrackRow row, TrackColumn c) => Campaign.IsTrackClaimed(CellKey(row.Level, c));

        public TrackCellState State(TrackRow row, TrackColumn c)
        {
            if (row.Rewards(c).Count == 0)
            {
                return TrackCellState.Claimed;
            }

            if (IsClaimed(row, c))
            {
                return TrackCellState.Claimed;
            }

            if (!Owns(c))
            {
                return TrackCellState.Locked;
            }

            return Progress >= row.Level ? TrackCellState.Claimable : TrackCellState.Upcoming;
        }

        /// <summary>Reached cells the player could claim now in <paramref name="c"/>.</summary>
        public int ClaimableCount(TrackColumn c) => Catalog.Rows.Count(r => State(r, c) == TrackCellState.Claimable);

        public int ClaimableCount() => ClaimableCount(TrackColumn.Free) + ClaimableCount(TrackColumn.Premium) + ClaimableCount(TrackColumn.Royal);

        /// <summary>Reached cells a pass would open right now (for the "buy and claim N rewards" pitch).</summary>
        public int LockedReached(TrackColumn c) => Catalog.Rows.Count(r => Progress >= r.Level && State(r, c) == TrackCellState.Locked);

        /// <summary>Takes one cell's rewards. Null when it is not claimable (not reached, not owned, already claimed).</summary>
        public IReadOnlyDictionary<string, int>? Claim(int level, TrackColumn c)
        {
            var row = Catalog.Rows.FirstOrDefault(r => r.Level == level);
            if (row == null || State(row, c) != TrackCellState.Claimable)
            {
                return null;
            }

            var rewards = row.Rewards(c);
            Grant(rewards);
            Campaign.MarkTrackClaimed(CellKey(row.Level, c));
            return rewards;
        }

        /// <summary>Claims every claimable cell; the summed rewards (empty when nothing was ready).</summary>
        public IReadOnlyDictionary<string, int> ClaimAll()
        {
            var sum = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var row in Catalog.Rows)
            {
                foreach (TrackColumn c in new[] { TrackColumn.Free, TrackColumn.Premium, TrackColumn.Royal })
                {
                    if (Claim(row.Level, c) is { } got)
                    {
                        foreach (var g in got)
                        {
                            sum[g.Key] = (sum.TryGetValue(g.Key, out var v) ? v : 0) + g.Value;
                        }
                    }
                }
            }

            return sum;
        }

        private void Grant(IReadOnlyDictionary<string, int> rewards)
        {
            var wallet = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var r in rewards)
            {
                if (r.Key.StartsWith(RewardTrackCatalog.ShardPrefix, StringComparison.Ordinal))
                {
                    Campaign.Roster.AddShards(r.Key.Substring(RewardTrackCatalog.ShardPrefix.Length), r.Value);
                }
                else
                {
                    wallet[r.Key] = r.Value;
                }
            }

            Campaign.Wallet.Grant(wallet);
        }

        // ---------- passes ----------

        /// <summary>The offer the Royal button sells now: the full $3.99 pass, or the $0.99 upgrade for Premium owners; null when owned.</summary>
        public string? RoyalOfferNow => Tier switch
        {
            TrackTier.None => Catalog.RoyalOffer,
            TrackTier.Premium => Catalog.UpgradeOffer,
            _ => null
        };

        public string? PremiumOfferNow => Tier == TrackTier.None ? Catalog.PremiumOffer : null;

        public string PriceLabel(string offerId) => Store.ProductFor(Store.FindOffer(offerId)?.Sku)?.PriceLabel ?? "";

        public void BuyPremium(Action<PurchaseOutcome> done) => Buy(PremiumOfferNow, done);

        /// <summary>Royal pass (both paid columns); for a Premium owner this buys the upgrade SKU (the difference).</summary>
        public void BuyRoyal(Action<PurchaseOutcome> done) => Buy(RoyalOfferNow, done);

        private void Buy(string? offerId, Action<PurchaseOutcome> done)
        {
            if (offerId == null)
            {
                done(new PurchaseOutcome(false, "", "", "store.already_bought"));
                return;
            }

            if (!ReferenceEquals(Store.Wallet, Campaign.Wallet))
            {
                throw new InvalidOperationException("The reward track grants into the store wallet; it must be the campaign's wallet.");
            }

            Store.BuySku(offerId, outcome =>
            {
                if (outcome.Ok)
                {
                    Campaign.MarkBought(offerId);
                }

                done(outcome);
            });
        }
    }
}
