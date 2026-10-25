using Survival.Domain.Campaign;
using Survival.Domain.Catalog;
using Survival.Domain.Clock;
using Survival.Domain.Economy;
using Survival.Domain.Ids;
using Survival.Domain.Runner;
using Survival.Domain.Store;
using Xunit.Abstractions;

namespace Survival.Domain.Tests;

/// <summary>
/// The power-up ladder (store.power_packs in stores.json + power_packs.json + the theme's products.json): every rung
/// grants what it lists into the campaign wallet through the sandbox purchase service, value per dollar climbs with the
/// price (the $39.99 King's Treasury about 2x the $0.99 rung), the one-time starter stays bought across a save, Hero Tomes
/// and War Banners do what the store says, and in the balance model one $39.99 pack moves a player stuck at the free wall
/// several levels on (docs/runner_balance.md, /workspace/runner/iap_ladder.md).
/// </summary>
public sealed class PowerPackTests
{
    private readonly ITestOutputHelper _out;

    public PowerPackTests(ITestOutputHelper output)
    {
        _out = output;
    }

    /// <summary>The stuck $39.99 buyer must clear at least this many new levels in the session he buys it.</summary>
    public const int StuckBuyMinLevels = 6;

    private static PowerPackCatalog Packs() => PowerPackCatalog.Parse(File.ReadAllText(Path.Combine(TestPaths.CatalogDir, PowerPackCatalog.FileName)));

    private static StoreDef PackStore() => Hook.Catalog().Stores.Single(s => s.Id == SurvIds.StorePowerPacks);

    private static IReadOnlyList<PowerPackRow> Rows() => Packs().Rows(PackStore());

    private static (PowerPackShop Shop, CampaignSession Campaign, SandboxPurchaseService Sandbox) Shop()
    {
        var catalog = CampaignCatalog.Parse(File.ReadAllText(Path.Combine(TestPaths.CatalogDir, CampaignCatalog.FileName)));
        var wallet = new Wallet();
        var campaign = new CampaignSession(catalog, wallet, new ManualClock(1000));
        var sandbox = new SandboxPurchaseService();
        var store = new StoreSystem(Hook.Catalog().Stores, Hook.Products(), sandbox, wallet);
        return (new PowerPackShop(Packs(), store, campaign), campaign, sandbox);
    }

    [Fact]
    public void The_ladder_is_the_six_price_tiers_plus_a_one_time_starter_with_white_label_products()
    {
        var rows = Rows();
        foreach (var r in rows)
        {
            _out.WriteLine($"{r.Offer.Id,-20} ${r.Pack.Usd,6:0.00} value {r.Value,6:0} ({r.ValuePerUsd,5:0.0}/$) {r.BonusLabel,-5} {(r.Pack.OneTime ? "one-time" : "")} {string.Join(", ", r.Grants.Select(g => g.Key + " " + g.Value))}");
        }

        Assert.Equal(new[] { 0.99, 1.99, 4.99, 9.99, 19.99, 39.99 }, rows.Where(r => !r.Pack.OneTime).Select(r => r.Pack.Usd).ToArray());
        var starter = Assert.Single(rows, r => r.Pack.OneTime);
        Assert.Equal(SurvIds.OfferPowerStarter, starter.Offer.Id);
        Assert.Equal(new[] { SurvIds.OfferPowerStarter, SurvIds.OfferPowerT1, SurvIds.OfferPowerT2, SurvIds.OfferPowerT3, SurvIds.OfferPowerT4, SurvIds.OfferPowerT5, SurvIds.OfferPowerT6 }, rows.Select(r => r.Offer.Id).ToArray());

        var products = Hook.Products();
        foreach (var r in rows)
        {
            Assert.Equal(OfferKinds.Sku, r.Offer.Kind);
            Assert.True(products.TryGetValue(r.Offer.Sku!, out var p), r.Offer.Sku);
            Assert.StartsWith("com.paytoplay.fantasykingdoma.", p!.ProductId);
            Assert.Equal("$" + r.Pack.Usd.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), p.PriceLabel);
        }

        Assert.Equal(rows.Select(r => products[r.Offer.Sku!].ProductId).Distinct().Count(), rows.Count);
    }

    [Fact]
    public void Value_per_dollar_climbs_with_price_and_the_39_99_pack_is_about_twice_the_0_99()
    {
        var tiers = Rows().Where(r => !r.Pack.OneTime).OrderBy(r => r.Pack.Usd).ToList();
        for (var i = 1; i < tiers.Count; i++)
        {
            Assert.True(tiers[i].ValuePerUsd > tiers[i - 1].ValuePerUsd * 1.05, $"{tiers[i].Offer.Id} {tiers[i].ValuePerUsd:0.0}/$ vs {tiers[i - 1].Offer.Id} {tiers[i - 1].ValuePerUsd:0.0}/$");
            Assert.True(tiers[i].BonusPct > tiers[i - 1].BonusPct);
        }

        var top = tiers[^1];
        var x = top.ValuePerUsd / tiers[0].ValuePerUsd;
        Assert.InRange(x, 1.9, 2.1);
        Assert.Equal("", tiers[0].BonusLabel);
        Assert.Equal("+95%", top.BonusLabel);
        Assert.All(tiers.Skip(1), t => Assert.Matches(@"^\+\d+%$", t.BonusLabel));

        // Every rung above the bottom one holds real power: Hero Tomes (one instant level each) from $4.99 up, more with each tier.
        var tomes = tiers.Select(t => t.Grant(SurvIds.ResHeroXp)).ToList();
        Assert.All(tiers.Skip(2), t => Assert.True(t.Grant(SurvIds.ResHeroXp) > 0, t.Offer.Id));
        for (var i = 3; i < tiers.Count; i++)
        {
            Assert.True(tomes[i] > tomes[i - 1], $"{tiers[i].Offer.Id} tomes {tomes[i]}");
        }

        Assert.All(tiers, t => Assert.True(t.Grant(SurvIds.ResRunnerBoost) > 0, t.Offer.Id));
        Assert.True(top.Grant(SurvIds.ResHeroXp) >= 10, "the $39.99 pack is a substantial power-up");
    }

    [Fact]
    public void Each_pack_grants_its_contents_into_the_campaign_wallet()
    {
        foreach (var row in Rows())
        {
            var (shop, campaign, sandbox) = Shop();
            var before = row.Grants.ToDictionary(g => g.Key, g => campaign.Wallet.Get(g.Key));
            PurchaseOutcome? got = null;
            shop.Buy(row.Offer.Id, o => got = o);
            Assert.True(got?.Ok, row.Offer.Id + ": " + got?.Error);
            Assert.Equal(Hook.Products()[row.Offer.Sku!].ProductId, got!.ProductId);
            Assert.Single(sandbox.History);
            foreach (var g in row.Grants)
            {
                Assert.Equal(before[g.Key] + g.Value, campaign.Wallet.Get(g.Key));
            }
        }
    }

    [Fact]
    public void A_failed_purchase_grants_nothing_and_does_not_use_up_the_starter()
    {
        var (shop, campaign, sandbox) = Shop();
        sandbox.FailNext = true;
        PurchaseOutcome? got = null;
        shop.Buy(SurvIds.OfferPowerStarter, o => got = o);
        Assert.False(got?.Ok);
        Assert.Equal(0, campaign.Wallet.Get(SurvIds.ResHeroXp));
        Assert.False(campaign.HasBought(SurvIds.OfferPowerStarter));
        Assert.Contains(shop.Available(), r => r.Offer.Id == SurvIds.OfferPowerStarter);
    }

    [Fact]
    public void The_starter_pack_sells_once_and_stays_bought_across_a_save()
    {
        var (shop, campaign, _) = Shop();
        Assert.Equal(7, shop.Available().Count);
        PurchaseOutcome? got = null;
        shop.Buy(SurvIds.OfferPowerStarter, o => got = o);
        Assert.True(got?.Ok);
        Assert.True(campaign.HasBought(SurvIds.OfferPowerStarter));
        Assert.DoesNotContain(shop.Available(), r => r.Offer.Id == SurvIds.OfferPowerStarter);
        var tomes = campaign.Wallet.Get(SurvIds.ResHeroXp);
        shop.Buy(SurvIds.OfferPowerStarter, o => got = o);
        Assert.False(got!.Ok);
        Assert.Equal("store.already_bought", got.Error);
        Assert.Equal(tomes, campaign.Wallet.Get(SurvIds.ResHeroXp));

        // Repeatable rungs stay on sale.
        shop.Buy(SurvIds.OfferPowerT6, o => got = o);
        shop.Buy(SurvIds.OfferPowerT6, o => got = o);
        Assert.True(got.Ok);
        Assert.False(campaign.HasBought(SurvIds.OfferPowerT6));

        var json = campaign.Save();
        var again = new CampaignSession(campaign.Catalog, new Wallet(), new ManualClock(1000));
        Assert.True(again.Load(json));
        Assert.True(again.HasBought(SurvIds.OfferPowerStarter));
        Assert.Equal(campaign.Wallet.Get(SurvIds.ResHeroXp), again.Wallet.Get(SurvIds.ResHeroXp));
        Assert.Equal(campaign.Wallet.Get(SurvIds.ResRunnerBoost), again.Wallet.Get(SurvIds.ResRunnerBoost));

        // An older save without the field: nothing bought yet.
        var old = new CampaignSession(campaign.Catalog, new Wallet(), new ManualClock(1000));
        Assert.True(old.Load(json.Replace("\"bought_once\"", "\"x_bought\"")));
        Assert.False(old.HasBought(SurvIds.OfferPowerStarter));
    }

    [Fact]
    public void A_hero_tome_is_one_level_now_up_to_the_star_cap_counting_training()
    {
        var (_, campaign, _) = Shop();
        var roster = campaign.Roster;
        var hero = roster.OwnedIds().First();
        Assert.Equal(LevelUpCheck.CantAfford, roster.CanUseTome(hero));
        var locked = campaign.Catalog.Heroes.FirstOrDefault(h => !roster.IsOwned(h.Id));
        if (locked != null)
        {
            Assert.Equal(LevelUpCheck.NotOwned, roster.CanUseTome(locked.Id));
        }

        Assert.Equal(LevelUpCheck.Unknown, roster.CanUseTome("hero.nobody"));

        var cap = roster.LevelCap(hero);
        campaign.Wallet.Add(SurvIds.ResHeroXp, cap + 5);
        var gold = campaign.Wallet.Get(SurvIds.ResSoft);
        var level = roster.Get(hero)!.Level;
        Assert.Equal(LevelUpCheck.Ok, roster.TryUseTome(hero));
        Assert.Equal(level + 1, roster.Get(hero)!.Level);
        Assert.Equal(cap + 4, campaign.Wallet.Get(SurvIds.ResHeroXp));
        Assert.Equal(gold, campaign.Wallet.Get(SurvIds.ResSoft));

        while (roster.TryUseTome(hero) == LevelUpCheck.Ok)
        {
        }

        Assert.Equal(cap, roster.Get(hero)!.Level);
        Assert.Equal(LevelUpCheck.AtStarCap, roster.CanUseTome(hero));

        // A level in training counts: a tome can't push past the cap once that training ends.
        var (_, c2, _) = Shop();
        var h2 = c2.Roster.OwnedIds().First();
        c2.Wallet.Add(SurvIds.ResSoft, 100_000_000);
        c2.Wallet.Add(SurvIds.ResFood, 100_000_000);
        c2.Wallet.Add(SurvIds.ResHeroXp, 100);
        var cap2 = c2.Roster.LevelCap(h2);
        while (c2.Roster.Get(h2)!.Level < cap2 - 1)
        {
            Assert.Equal(LevelUpCheck.Ok, c2.Roster.TryUseTome(h2));
        }

        Assert.Equal(LevelUpCheck.Ok, c2.Roster.TryStartLevelUp(h2));
        Assert.Equal(LevelUpCheck.AtStarCap, c2.Roster.CanUseTome(h2));
    }

    [Fact]
    public void A_war_banner_boosts_the_squad_and_is_spent_only_on_a_new_level_cleared()
    {
        var (_, campaign, _) = Shop();
        var runner = Runner.Catalog();
        var packs = Packs();
        var plain = RunnerBanner.Loadout(runner, campaign, packs);
        Assert.False(RunnerBanner.Active(campaign));
        campaign.Wallet.Add(SurvIds.ResRunnerBoost, 2);
        Assert.True(RunnerBanner.Active(campaign));
        var boosted = RunnerBanner.Loadout(runner, campaign, packs);
        var heroUnits = runner.Units.Values.Where(u => u.HeroId.Length > 0).Select(u => u.Id).ToList();
        Assert.NotEmpty(heroUnits);
        foreach (var u in heroUnits)
        {
            Assert.Equal(plain.For(u).DamageMul * 1.1f, boosted.For(u).DamageMul, 3);
            Assert.Equal(plain.For(u).HpMul * 1.1f, boosted.For(u).HpMul, 3);
        }

        // A loss and a replay of a cleared level are free; a first clear spends one charge.
        var l1 = runner.ByNumber(1);
        RunnerRewards.Grant(RunnerRewards.For(l1, new RunnerResult(l1.Id, false, 1, 0, 0, 1, 0, 10, 30, 1)), campaign);
        Assert.Equal(2, campaign.Wallet.Get(SurvIds.ResRunnerBoost));
        RunnerRewards.Grant(RunnerRewards.For(l1, new RunnerResult(l1.Id, true, 3, 1, 0, 2, 1, l1.Length, 90, 1)), campaign);
        Assert.Equal(1, campaign.Wallet.Get(SurvIds.ResRunnerBoost));
        RunnerRewards.Grant(RunnerRewards.For(l1, new RunnerResult(l1.Id, true, 3, 1, 0, 2, 1, l1.Length, 90, 1)), campaign);
        Assert.Equal(1, campaign.Wallet.Get(SurvIds.ResRunnerBoost));
    }

    [Fact]
    public void One_39_99_pack_moves_a_player_stuck_at_the_free_wall_several_levels_on()
    {
        var r = BalanceReport.Get();
        var stuck = r.Economies["stuck_39"];
        var buy = Assert.Single(stuck.StuckBuys);
        var p = Assert.Single(stuck.Purchases);
        Assert.Equal(SurvIds.OfferPowerT6, p.OfferId);
        _out.WriteLine($"stuck at L{buy.Level} (session {buy.Session}, day {(buy.Session - 1) / (double)r.Def.SessionsPerDay:0.0}): one $39.99 pack clears {buy.Burst} new levels in that session (L{buy.Level}-L{buy.Level + buy.Burst - 1})");

        // He gets stuck where the free player does, and the pack clears that level and at least N more right away.
        Assert.Equal(r.Wall("free"), buy.Level);
        Assert.True(buy.Burst >= StuckBuyMinLevels, $"burst {buy.Burst}");
        var through = buy.Level + buy.Burst - 1;
        var freeDay = r.Row("free", through).ClearedDay ?? double.MaxValue;
        var stuckDay = r.Row("stuck_39", through).ClearedDay ?? double.MaxValue;
        Assert.True(stuckDay + 30 <= freeDay, $"L{through}: stuck_39 day {stuckDay:0.0}, free day {freeDay:0.0}");

        // It is a boost, not the end game: without more spend the wall comes back before L50.
        Assert.Contains(r.Rows["stuck_39"].Skip(through), x => x.Ratio < 1.0);
        Assert.InRange(r.Rows["stuck_39"][^1].UsdSpent, 39.98, 40.0);
    }
}
