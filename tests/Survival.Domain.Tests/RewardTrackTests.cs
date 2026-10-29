using Survival.Domain.Campaign;
using Survival.Domain.Clock;
using Survival.Domain.Economy;
using Survival.Domain.Ids;
using Survival.Domain.Store;

namespace Survival.Domain.Tests;

/// <summary>
/// The Kingdom Path reward track (reward_track.json + store.reward_track): Free column for everyone, Premium ($2.99)
/// opens column 2, Royal ($3.99) opens columns 2 and 3; Premium then Royal sells the $0.99 upgrade SKU; buying late opens
/// every reached cell; a cell pays once and stays claimed across a save. The free-pace model never reads the track.
/// </summary>
public sealed class RewardTrackTests
{
    private static RewardTrackCatalog Track() => RewardTrackCatalog.Parse(File.ReadAllText(Path.Combine(TestPaths.CatalogDir, RewardTrackCatalog.FileName)));

    private static (RewardTrack Track, CampaignSession Campaign, SandboxPurchaseService Sandbox) Make()
    {
        var catalog = CampaignCatalog.Parse(File.ReadAllText(Path.Combine(TestPaths.CatalogDir, CampaignCatalog.FileName)));
        var wallet = new Wallet();
        var campaign = new CampaignSession(catalog, wallet, new ManualClock(1000));
        var sandbox = new SandboxPurchaseService();
        var store = new StoreSystem(Hook.Catalog().Stores, Hook.Products(), sandbox, wallet);
        return (new RewardTrack(Track(), store, campaign), campaign, sandbox);
    }

    private static PurchaseOutcome Buy(Action<Action<PurchaseOutcome>> buy)
    {
        PurchaseOutcome? got = null;
        buy(o => got = o);
        return got!;
    }

    [Fact]
    public void The_track_has_three_filled_columns_and_white_label_sandbox_products()
    {
        var t = Track();
        Assert.True(t.Rows.Count >= 12);
        Assert.All(t.Rows, r => Assert.True(r.Free.Count > 0 && r.Premium.Count > 0 && r.Royal.Count > 0));
        var (track, _, _) = Make();
        Assert.Equal("$2.99", track.PriceLabel(SurvIds.OfferTrackPremium));
        Assert.Equal("$3.99", track.PriceLabel(SurvIds.OfferTrackRoyal));
        Assert.Equal("$0.99", track.PriceLabel(SurvIds.OfferTrackRoyalUpgrade));
        Assert.Equal("com.paytoplay.fantasykingdoma.track_royal", track.Store.ProductFor(SurvIds.SkuTrackRoyal)!.ProductId);
        Assert.True(track.Store.Purchases.IsSandbox);
    }

    [Fact]
    public void Free_players_claim_only_the_free_column_of_reached_rows()
    {
        var (track, campaign, _) = Make();
        var first = track.Catalog.Rows[0];
        Assert.Equal(TrackCellState.Upcoming, track.State(first, TrackColumn.Free));
        Assert.Null(track.Claim(first.Level, TrackColumn.Free));
        campaign.UnlockRunner(first.Level + 1);
        Assert.Equal(TrackCellState.Claimable, track.State(first, TrackColumn.Free));
        Assert.Equal(TrackCellState.Locked, track.State(first, TrackColumn.Premium));
        Assert.Equal(TrackCellState.Locked, track.State(first, TrackColumn.Royal));
        Assert.Null(track.Claim(first.Level, TrackColumn.Premium));
        var got = track.Claim(first.Level, TrackColumn.Free);
        Assert.NotNull(got);
        foreach (var g in got!)
        {
            Assert.Equal(g.Value, campaign.Wallet.Get(g.Key));
        }
    }

    [Fact]
    public void A_cell_pays_once()
    {
        var (track, campaign, _) = Make();
        var row = track.Catalog.Rows[0];
        campaign.UnlockRunner(row.Level + 1);
        Assert.NotNull(track.Claim(row.Level, TrackColumn.Free));
        Assert.Null(track.Claim(row.Level, TrackColumn.Free));
        Assert.Equal(TrackCellState.Claimed, track.State(row, TrackColumn.Free));
        Assert.Empty(track.ClaimAll());
    }

    [Fact]
    public void Premium_opens_column_two_only_and_late_buyers_claim_every_reached_row()
    {
        var (track, campaign, sandbox) = Make();
        campaign.UnlockRunner(track.Catalog.Rows[4].Level + 1);
        Assert.Equal(5, track.LockedReached(TrackColumn.Premium));
        Assert.True(Buy(track.BuyPremium).Ok);
        Assert.Equal(TrackTier.Premium, track.Tier);
        Assert.Equal(new[] { "com.paytoplay.fantasykingdoma.track_premium" }, sandbox.History);
        Assert.Equal(5, track.ClaimableCount(TrackColumn.Premium));
        Assert.Equal(0, track.ClaimableCount(TrackColumn.Royal));
        Assert.Equal(TrackCellState.Upcoming, track.State(track.Catalog.Rows[5], TrackColumn.Premium));
        Assert.False(Buy(track.BuyPremium).Ok);
    }

    [Fact]
    public void Royal_opens_both_paid_columns()
    {
        var (track, campaign, sandbox) = Make();
        campaign.UnlockRunner(track.Catalog.Rows[2].Level + 1);
        Assert.True(Buy(track.BuyRoyal).Ok);
        Assert.Equal(TrackTier.Royal, track.Tier);
        Assert.Equal(new[] { "com.paytoplay.fantasykingdoma.track_royal" }, sandbox.History);
        Assert.Equal(3, track.ClaimableCount(TrackColumn.Premium));
        Assert.Equal(3, track.ClaimableCount(TrackColumn.Royal));
        Assert.Null(track.PremiumOfferNow);
        Assert.Null(track.RoyalOfferNow);
        Assert.False(Buy(track.BuyRoyal).Ok);
    }

    [Fact]
    public void Premium_then_royal_sells_the_upgrade_for_the_difference()
    {
        var (track, _, sandbox) = Make();
        Assert.Equal(SurvIds.OfferTrackRoyal, track.RoyalOfferNow);
        Assert.True(Buy(track.BuyPremium).Ok);
        Assert.Equal(SurvIds.OfferTrackRoyalUpgrade, track.RoyalOfferNow);
        Assert.True(Buy(track.BuyRoyal).Ok);
        Assert.Equal(TrackTier.Royal, track.Tier);
        Assert.Equal(new[] { "com.paytoplay.fantasykingdoma.track_premium", "com.paytoplay.fantasykingdoma.track_royal_upgrade" }, sandbox.History);
    }

    [Fact]
    public void A_cancelled_purchase_opens_nothing()
    {
        var (track, _, sandbox) = Make();
        sandbox.FailNext = true;
        Assert.False(Buy(track.BuyRoyal).Ok);
        Assert.Equal(TrackTier.None, track.Tier);
    }

    [Fact]
    public void Shard_rewards_go_to_the_hero()
    {
        var (track, campaign, _) = Make();
        var row = track.Catalog.Rows.First(r => r.Royal.Keys.Any(k => k.StartsWith(RewardTrackCatalog.ShardPrefix)));
        var key = row.Royal.Keys.First(k => k.StartsWith(RewardTrackCatalog.ShardPrefix));
        var hero = key.Substring(RewardTrackCatalog.ShardPrefix.Length);
        var before = campaign.Roster.Get(hero)!.Shards;
        campaign.UnlockRunner(row.Level + 1);
        Buy(track.BuyRoyal);
        Assert.NotNull(track.Claim(row.Level, TrackColumn.Royal));
        Assert.Equal(before + row.Royal[key], campaign.Roster.Get(hero)!.Shards);
    }

    [Fact]
    public void Passes_and_claims_survive_a_save()
    {
        var (track, campaign, _) = Make();
        campaign.UnlockRunner(track.Catalog.Rows[1].Level + 1);
        Buy(track.BuyPremium);
        track.ClaimAll();
        var json = campaign.Save();
        var again = new CampaignSession(campaign.Catalog, new Wallet(), new ManualClock(1000));
        Assert.True(again.Load(json));
        var t2 = new RewardTrack(track.Catalog, new StoreSystem(Hook.Catalog().Stores, Hook.Products(), new SandboxPurchaseService(), again.Wallet), again);
        Assert.Equal(TrackTier.Premium, t2.Tier);
        Assert.Equal(0, t2.ClaimableCount());
        Assert.Null(t2.Claim(track.Catalog.Rows[0].Level, TrackColumn.Free));
        Assert.Equal(2, t2.LockedReached(TrackColumn.Royal));
    }

    [Fact]
    public void The_free_pace_model_does_not_read_the_track()
    {
        // Track rewards are outside RunnerBalance (the locked free pace, RunnerFreePaceTests): no input of it is a track.
        var types = typeof(Survival.Domain.Runner.RunnerBalanceSources).GetConstructors().SelectMany(c => c.GetParameters()).Select(p => p.ParameterType)
            .Concat(typeof(Survival.Domain.Runner.RunnerBalanceSources).GetProperties().Select(p => p.PropertyType));
        Assert.DoesNotContain(types, t => t == typeof(RewardTrackCatalog) || t == typeof(RewardTrack));
    }
}
