using System.Text.Json;
using Survival.Domain.Base;
using Survival.Domain.Catalog;
using Survival.Domain.Clock;
using Survival.Domain.Economy;
using Survival.Domain.Flavor;
using Survival.Domain.Gift;
using Survival.Domain.Ids;
using Survival.Domain.Session;
using Survival.Domain.Store;
using Survival.Domain.World;

namespace Survival.Domain.Tests;

internal static class Hook
{
    public static HookCatalog Catalog() => HookCatalogLoader.LoadFromDirectory(TestPaths.CatalogDir);

    public static IReadOnlyDictionary<string, ProductDef> Products() =>
        HookCatalogLoader.ParseProducts(File.ReadAllText(Path.Combine(TestPaths.PackDir, "store", "products.json")));

    public static SurvivalSession Session(ManualClock clock, SandboxPurchaseService? store = null, int seed = 7) =>
        new(
            AppFlavorConfig.FantasyKingdomA,
            TestPaths.LoadPack(),
            CatalogLoader.LoadFromDirectory(TestPaths.CatalogDir),
            Catalog(),
            Products(),
            clock,
            store ?? new SandboxPurchaseService(),
            seed);
}

public sealed class HookIdTests
{
    private static List<string> JsonList(string key)
    {
        var json = File.ReadAllText(Path.Combine(TestPaths.CatalogDir, CatalogLoader.DictionaryFileName));
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty(key).EnumerateArray().Select(e => e.GetString()!).ToList();
    }

    [Fact]
    public void Wood_food_stone_are_real_resource_ids_in_dictionary_and_catalog()
    {
        Assert.Contains(SurvIds.ResStone, SurvIds.Resources);
        Assert.Contains(SurvIds.ResWood, SurvIds.Resources);
        Assert.Contains(SurvIds.ResFood, SurvIds.Resources);
        Assert.Equal(SurvIds.Resources, JsonList("resources"));
        var catalog = CatalogLoader.LoadFromDirectory(TestPaths.CatalogDir);
        Assert.Contains(SurvIds.ResWood, catalog.Resources);
    }

    [Fact]
    public void Dictionary_lists_quest_store_offer_gift_and_plot_families()
    {
        Assert.Equal(SurvIds.BasePlots, JsonList("base_plots_slice"));
        Assert.Equal(SurvIds.Stores, JsonList("stores"));
        Assert.Equal(SurvIds.Offers.Concat(SurvIds.PowerOffers).Concat(SurvIds.TrackOffers), JsonList("offers"));
        var gifts = JsonList("gifts");
        Assert.Contains(SurvIds.GiftSchedulerFree, gifts);
        foreach (var g in SurvIds.Gifts)
        {
            Assert.Contains(g, gifts);
        }

        var quest = JsonList("quest");
        Assert.Contains(SurvIds.QuestChainStarter, quest);
        foreach (var q in SurvIds.QuestStepsStarter)
        {
            Assert.Contains(q, quest);
        }

        foreach (var id in quest.Concat(gifts).Concat(JsonList("offers")).Concat(JsonList("stores")))
        {
            Assert.False(SurvIds.IsForbidden(id));
        }
    }

    [Fact]
    public void Hook_data_uses_only_dictionary_ids()
    {
        var c = Hook.Catalog();
        Assert.Equal(SurvIds.BasePlots, c.Plots.Select(p => p.Id).ToList());
        Assert.Equal(SurvIds.BuildingHq, c.Plots[0].BuildingId);
        Assert.Equal(SurvIds.BuildingEconomy, c.Plots[1].BuildingId);
        Assert.Equal(SurvIds.BuildingEconomy, c.Plots[2].BuildingId);
        Assert.Equal(SurvIds.ResFood, c.Plots[1].Produces);
        Assert.Equal(SurvIds.ResWood, c.Plots[2].Produces);
        Assert.Equal(SurvIds.QuestStepsStarter, c.StarterChain.Steps.Select(s => s.Id).ToList());
        Assert.Equal(SurvIds.Stores, c.Stores.Select(s => s.Id).ToList());
        Assert.Equal(SurvIds.Offers.OrderBy(x => x), c.Stores.Single(s => s.Id == SurvIds.StoreGemShop).Offers.Select(o => o.Id).OrderBy(x => x));
        Assert.Equal(SurvIds.PowerOffers, c.Stores.Single(s => s.Id == SurvIds.StorePowerPacks).Offers.Select(o => o.Id).ToList());
        Assert.Equal(SurvIds.TrackOffers, c.Stores.Single(s => s.Id == SurvIds.StoreRewardTrack).Offers.Select(o => o.Id).ToList());
        Assert.Equal(SurvIds.Gifts, c.Gifts.Pool.Select(g => g.Id).ToList());
        foreach (var plot in c.Plots)
        {
            foreach (var level in plot.Levels)
            {
                foreach (var res in level.UpgradeCost.Keys)
                {
                    Assert.Contains(res, SurvIds.Resources);
                }
            }
        }
    }

    [Fact]
    public void Theme_a_products_map_every_sku_offer_to_bundle_product_ids()
    {
        var products = Hook.Products();
        foreach (var offer in Hook.Catalog().Stores.SelectMany(s => s.Offers).Where(o => o.Kind == OfferKinds.Sku))
        {
            Assert.True(products.TryGetValue(offer.Sku!, out var p));
            Assert.StartsWith("com.paytoplay.fantasykingdoma.", p!.ProductId);
            Assert.NotEmpty(p.PriceLabel);
        }
    }
}

public sealed class TimerAndWalletTests
{
    [Fact]
    public void Rush_cost_is_one_gem_per_minute_rounded_up_and_free_under_five_minutes()
    {
        var rules = new RushRules(300, 1, 1);
        Assert.Equal(10, rules.GemCost(600));
        Assert.Equal(6, rules.GemCost(301));
        Assert.Equal(0, rules.GemCost(300));
        Assert.Equal(0, rules.GemCost(0));
        Assert.True(rules.IsFree(299));
        Assert.False(rules.IsFree(301));
    }

    [Fact]
    public void Timer_counts_down_then_finishes_free_inside_window()
    {
        var clock = new ManualClock(1000);
        var q = new TimerQueue(clock, new RushRules(), 1);
        Assert.True(q.TryStart("t", "x", 600, out _));
        Assert.False(q.TryStart("t2", "x", 10, out _));
        clock.Advance(290);
        Assert.Equal(310, q.Remaining("t"), 3);
        Assert.Equal(6, q.RushCost("t"));
        Assert.False(q.CanFinishFree("t"));
        Assert.False(q.TryFinishFree("t"));
        clock.Advance(15);
        Assert.True(q.CanFinishFree("t"));
        Assert.Equal(0, q.RushCost("t"));
        Assert.True(q.TryFinishFree("t"));
        Assert.True(q.IsDone("t"));
        Assert.True(q.TryComplete("t"));
        Assert.True(q.HasFreeSlot);
    }

    [Fact]
    public void Gem_rush_spends_gems_and_fails_without_enough()
    {
        var clock = new ManualClock();
        var q = new TimerQueue(clock, new RushRules(), 1);
        q.TryStart("t", "x", 600, out _);
        var poor = new Wallet(new Dictionary<string, int> { [SurvIds.ResHard] = 9 });
        Assert.False(q.TryRush("t", poor, out _));
        Assert.Equal(9, poor.Get(SurvIds.ResHard));
        var rich = new Wallet(new Dictionary<string, int> { [SurvIds.ResHard] = 50 });
        Assert.True(q.TryRush("t", rich, out var spent));
        Assert.Equal(10, spent);
        Assert.Equal(40, rich.Get(SurvIds.ResHard));
        Assert.True(q.IsDone("t"));
    }

    [Fact]
    public void Speedup_items_shave_minutes_without_waste()
    {
        var clock = new ManualClock();
        var q = new TimerQueue(clock, new RushRules(), 1);
        q.TryStart("t", "x", 600, out _);
        var w = new Wallet(new Dictionary<string, int> { [SurvIds.ResSpeedupM] = 5 });
        Assert.Equal(5, q.ApplySpeedups("t", w));
        Assert.Equal(300, q.Remaining("t"), 3);
        Assert.True(q.CanFinishFree("t"));
        w.Add(SurvIds.ResSpeedupM, 60);
        Assert.Equal(5, q.ApplySpeedups("t", w));
        Assert.Equal(55, w.Get(SurvIds.ResSpeedupM));
        Assert.True(q.IsDone("t"));
    }

    [Fact]
    public void Wallet_spend_is_all_or_nothing_and_raises_changes()
    {
        var w = new Wallet(new Dictionary<string, int> { [SurvIds.ResSoft] = 100, [SurvIds.ResWood] = 5 });
        var changes = new List<(string, int, int)>();
        w.Changed += (id, now, delta) => changes.Add((id, now, delta));
        Assert.False(w.TrySpend(new Dictionary<string, int> { [SurvIds.ResSoft] = 50, [SurvIds.ResWood] = 10 }));
        Assert.Equal(100, w.Get(SurvIds.ResSoft));
        Assert.Empty(changes);
        Assert.True(w.TrySpend(new Dictionary<string, int> { [SurvIds.ResSoft] = 50, [SurvIds.ResWood] = 5 }));
        Assert.Equal(50, w.Get(SurvIds.ResSoft));
        Assert.Equal(0, w.Get(SurvIds.ResWood));
        Assert.Contains((SurvIds.ResSoft, 50, -50), changes);
        Assert.Equal(0, w.Add(SurvIds.ResFood, -10));
    }

    [Fact]
    public void Hud_chips_read_the_same_wallet_as_the_hook()
    {
        var session = Hook.Session(new ManualClock());
        Assert.NotNull(session.Hook);
        Assert.Equal(96700, session.Chips.Get(ChipWallet.StoneChip));
        Assert.Equal(96700, session.Hook!.Wallet.Get(SurvIds.ResStone));
        Assert.Equal(245600, session.Hook.Wallet.Get(SurvIds.ResSoft));
        Assert.Equal(178300, session.Hook.Wallet.Get(SurvIds.ResWood));
        Assert.Equal(64200, session.Hook.Wallet.Get(SurvIds.ResFood));
        Assert.Equal(50, session.Chips.Get(ChipWallet.HardChip));
        session.Chips.Add(ChipWallet.StoneChip, 800);
        Assert.Equal(97500, session.Hook.Wallet.Get(SurvIds.ResStone));
        Assert.Equal(ChipWallet.StoneChip, ChipWallet.ChipFor(SurvIds.ResStone));
        Assert.Null(ChipWallet.ChipFor(SurvIds.ResSpeedupM));
    }

    [Fact]
    public void Session_without_hook_data_keeps_hook_null()
    {
        var session = new SurvivalSession(
            AppFlavorConfig.FantasyKingdomA,
            TestPaths.LoadPack(),
            CatalogLoader.LoadFromDirectory(TestPaths.CatalogDir));
        Assert.Null(session.Hook);
    }
}

public sealed class BaseEconomyTests
{
    [Fact]
    public void Farm_and_lumber_start_full_collect_then_refill_at_rate_up_to_cap()
    {
        var clock = new ManualClock(5000);
        var hook = Hook.Session(clock).Hook!;
        var food = hook.Wallet.Get(SurvIds.ResFood);
        Assert.True(hook.Base.Find(SurvIds.BasePlotFarm01)!.CanCollect);
        Assert.Equal(600, hook.Base.Collect(SurvIds.BasePlotFarm01));
        Assert.Equal(food + 600, hook.Wallet.Get(SurvIds.ResFood));
        Assert.Equal(0, hook.Base.Collect(SurvIds.BasePlotFarm01));
        clock.Advance(10);
        Assert.Equal(0, hook.Base.Collect(SurvIds.BasePlotFarm01)); // 10 < min 20
        clock.Advance(10);
        Assert.Equal(20, hook.Base.Collect(SurvIds.BasePlotFarm01)); // 3600/h = 1/s
        clock.Advance(7200);
        hook.Base.Tick();
        Assert.Equal(600, hook.Base.Find(SurvIds.BasePlotFarm01)!.Collectable);
        Assert.Equal(500, hook.Base.Collect(SurvIds.BasePlotLumber01));
        Assert.Equal(0, hook.Base.Collect(SurvIds.BasePlotHq01));
    }

    [Fact]
    public void Castle_upgrade_spends_cost_runs_600s_and_gates_economy_upgrades()
    {
        var clock = new ManualClock();
        var hook = Hook.Session(clock).Hook!;
        Assert.Equal(UpgradeBlock.NeedsCastle, hook.Base.CheckUpgrade(SurvIds.BasePlotFarm01));
        var gold = hook.Wallet.Get(SurvIds.ResSoft);
        Assert.True(hook.Base.TryStartUpgrade(SurvIds.BasePlotHq01));
        Assert.Equal(gold - 2000, hook.Wallet.Get(SurvIds.ResSoft));
        Assert.Equal(UpgradeBlock.AlreadyUpgrading, hook.Base.CheckUpgrade(SurvIds.BasePlotHq01));
        var timerId = BaseEconomy.UpgradeTimerId(SurvIds.BasePlotHq01);
        Assert.Equal(600, hook.Timers.Remaining(timerId), 3);
        Assert.Equal(0, hook.FinishFree(SurvIds.BasePlotHq01));
        clock.Advance(300);
        Assert.Equal(2, hook.FinishFree(SurvIds.BasePlotHq01));
        Assert.Equal(2, hook.Base.HqLevel);
        Assert.Equal(UpgradeBlock.None, hook.Base.CheckUpgrade(SurvIds.BasePlotFarm01));
        Assert.True(hook.Base.TryStartUpgrade(SurvIds.BasePlotFarm01));
        Assert.Equal(UpgradeBlock.QueueBusy, hook.Base.CheckUpgrade(SurvIds.BasePlotLumber01));
    }

    [Fact]
    public void Castle_gem_rush_costs_ten_gems_from_the_fifty_start()
    {
        var hook = Hook.Session(new ManualClock()).Hook!;
        Assert.True(hook.Base.TryStartUpgrade(SurvIds.BasePlotHq01));
        Assert.Equal(2, hook.Rush(SurvIds.BasePlotHq01, out var gems));
        Assert.Equal(10, gems);
        Assert.Equal(40, hook.Wallet.Get(SurvIds.ResHard));
    }

    [Fact]
    public void Upgrade_without_resources_is_blocked_and_charges_nothing()
    {
        var clock = new ManualClock();
        var catalog = Hook.Catalog();
        var wallet = new Wallet();
        var hook = new HookSession(catalog, wallet, clock, Hook.Products(), new SandboxPurchaseService());
        Assert.Equal(UpgradeBlock.Cost, hook.Base.CheckUpgrade(SurvIds.BasePlotHq01));
        Assert.False(hook.Base.TryStartUpgrade(SurvIds.BasePlotHq01));
        Assert.Equal(50, wallet.Get(SurvIds.ResHard));
        Assert.Empty(hook.Timers.Active);
    }
}

public sealed class StarterQuestTests
{
    [Fact]
    public void Starter_chain_collect_gather_upgrade_then_done()
    {
        var clock = new ManualClock();
        var session = Hook.Session(clock);
        var hook = session.Hook!;
        var q = hook.Quests;
        Assert.Equal(SurvIds.QuestStarterCollect, q.Current!.Id);

        hook.ReportMarch(SurvIds.WorldNodeGather01, SurvIds.WorldNodeTypeGather); // not the active step
        Assert.Equal(0, q.Progress);
        hook.Base.Collect(SurvIds.BasePlotFarm01);
        hook.Base.Collect(SurvIds.BasePlotLumber01);
        Assert.Equal(2, q.Progress);
        Assert.Null(q.Claim(hook.Wallet));
        clock.Advance(30);
        hook.Base.Collect(SurvIds.BasePlotFarm01);
        Assert.True(q.IsComplete);
        var food = hook.Wallet.Get(SurvIds.ResFood);
        var r1 = q.Claim(hook.Wallet)!;
        Assert.Equal(300, r1[SurvIds.ResFood]);
        Assert.Equal(food + 300, hook.Wallet.Get(SurvIds.ResFood));

        Assert.Equal(SurvIds.QuestStarterGather, q.Current!.Id);
        hook.ReportMarch(SurvIds.WorldNodeFight01, SurvIds.WorldNodeTypeFight);
        Assert.False(q.IsComplete);
        hook.ReportMarch(SurvIds.WorldNodeGather01, SurvIds.WorldNodeTypeGather);
        Assert.True(q.IsComplete);
        q.Claim(hook.Wallet);
        Assert.Equal(5, hook.Wallet.Get(SurvIds.ResSpeedupM));

        Assert.Equal(SurvIds.QuestStarterUpgradeHq, q.Current!.Id);
        Assert.Equal(1, q.Progress);
        hook.Base.TryStartUpgrade(SurvIds.BasePlotHq01);
        Assert.Equal(5, hook.Timers.ApplySpeedups(BaseEconomy.UpgradeTimerId(SurvIds.BasePlotHq01), hook.Wallet));
        Assert.Equal(2, hook.FinishFree(SurvIds.BasePlotHq01));
        Assert.True(q.IsComplete);
        var gems = hook.Wallet.Get(SurvIds.ResHard);
        q.Claim(hook.Wallet);
        Assert.Equal(gems + 20, hook.Wallet.Get(SurvIds.ResHard));
        Assert.True(q.ChainDone);
        Assert.Null(q.Current);
    }

    [Fact]
    public void Reach_level_step_starts_complete_when_castle_already_there()
    {
        var clock = new ManualClock();
        var hook = Hook.Session(clock).Hook!;
        hook.Base.TryStartUpgrade(SurvIds.BasePlotHq01);
        hook.Rush(SurvIds.BasePlotHq01, out _);
        var q = hook.Quests;
        hook.Base.Collect(SurvIds.BasePlotFarm01);
        hook.Base.Collect(SurvIds.BasePlotLumber01);
        clock.Advance(30);
        hook.Base.Collect(SurvIds.BasePlotFarm01);
        q.Claim(hook.Wallet);
        hook.ReportMarch(SurvIds.WorldNodeGather01, SurvIds.WorldNodeTypeGather);
        q.Claim(hook.Wallet);
        Assert.Equal(SurvIds.QuestStarterUpgradeHq, q.Current!.Id);
        Assert.True(q.IsComplete);
    }
}

public sealed class FreeGiftTests
{
    private static FreeGiftScheduler Make(ManualClock clock, int seed = 3) =>
        new(Hook.Catalog().Gifts, clock, seed);

    [Fact]
    public void First_gift_lands_between_20_and_45_minutes_and_only_one_waits()
    {
        var clock = new ManualClock(10_000);
        var g = Make(clock);
        Assert.InRange(g.NextAt - clock.Now, 1200, 2700);
        clock.Advance(1199);
        Assert.False(g.Tick());
        clock.Advance(2700 - 1199);
        Assert.True(g.Tick());
        Assert.NotNull(g.Pending);
        clock.Advance(10 * 3600);
        Assert.False(g.Tick());
        Assert.Equal(1, g.OfferedToday);
    }

    [Fact]
    public void Claim_schedules_next_in_window_and_daily_cap_is_six()
    {
        var clock = new ManualClock(0);
        var g = Make(clock, 11);
        var offered = 0;
        for (var i = 0; i < 200 && clock.Now < 86_399; i++)
        {
            clock.Now = Math.Min(86_399, Math.Max(clock.Now, g.NextAt));
            if (g.Tick())
            {
                offered++;
                var before = clock.Now;
                Assert.NotNull(g.Claim());
                Assert.InRange(g.NextAt - before, 1200, 2700);
            }
            else if (g.CapReached)
            {
                break;
            }
        }

        Assert.Equal(6, offered);
        Assert.True(g.CapReached);
        clock.Now = 86_400 + 3000;
        Assert.True(g.Tick());
        Assert.Equal(1, g.OfferedToday);
    }

    [Fact]
    public void Debug_offer_is_instant_ignores_cap_but_still_one_at_a_time()
    {
        var clock = new ManualClock(0);
        var g = Make(clock);
        Assert.True(g.DebugOfferNow());
        Assert.True(g.Pending!.FromDebug);
        Assert.False(g.DebugOfferNow());
        Assert.Equal(0, g.OfferedToday);
        Assert.NotNull(g.Claim());
        Assert.True(g.DebugOfferNow());
    }

    [Fact]
    public void Gift_rolls_come_from_the_pool_within_ranges()
    {
        var clock = new ManualClock(0);
        var pool = Hook.Catalog().Gifts.Pool.ToDictionary(p => p.Id);
        var g = Make(clock, 99);
        for (var i = 0; i < 300; i++)
        {
            g.DebugOfferNow();
            var gift = g.Claim()!;
            var def = pool[gift.RewardId];
            Assert.Equal(def.Resource, gift.Resource);
            Assert.InRange(gift.Amount, def.Min, def.Max);
        }
    }

    [Fact]
    public void Claimed_energy_gift_tops_up_the_energy_meter()
    {
        var clock = new ManualClock();
        var session = Hook.Session(clock);
        var hook = session.Hook!;
        for (var seed = 0; seed < 500; seed++)
        {
            hook.Gifts.DebugOfferNow();
            if (hook.Gifts.Pending!.Resource == SurvIds.ResEnergy)
            {
                var before = session.Energy.Current;
                var gift = hook.ClaimFreeGift()!;
                Assert.Equal(before + gift.Amount, session.Energy.Current);
                return;
            }

            hook.ClaimFreeGift();
        }

        Assert.Fail("No energy gift rolled.");
    }

    [Fact]
    public void Free_slot_shows_in_gem_shop_only_while_a_gift_waits()
    {
        var hook = Hook.Session(new ManualClock()).Hook!;
        Assert.False(hook.FreeAvailable(SurvIds.StoreGemShop));
        hook.Gifts.DebugOfferNow();
        Assert.True(hook.FreeAvailable(SurvIds.StoreGemShop));
        hook.ClaimFreeGift();
        Assert.False(hook.FreeAvailable(SurvIds.StoreGemShop));
    }
}

public sealed class SandboxStoreTests
{
    [Fact]
    public void Sandbox_sku_purchase_grants_gems_under_theme_product_id()
    {
        var sandbox = new SandboxPurchaseService();
        var hook = Hook.Session(new ManualClock(), sandbox).Hook!;
        PurchaseOutcome? got = null;
        hook.Store.BuySku(SurvIds.OfferGemsS, o => got = o);
        Assert.True(got!.Ok);
        Assert.Equal("com.paytoplay.fantasykingdoma.hard_s", got.ProductId);
        Assert.Equal(130, hook.Wallet.Get(SurvIds.ResHard));
        hook.Store.BuySku(SurvIds.OfferGemsM, o => got = o);
        Assert.Equal(630, hook.Wallet.Get(SurvIds.ResHard));
        Assert.True(sandbox.IsSandbox);
        Assert.Equal(2, sandbox.History.Count);
    }

    [Fact]
    public void Failed_sandbox_purchase_grants_nothing()
    {
        var sandbox = new SandboxPurchaseService { FailNext = true };
        var hook = Hook.Session(new ManualClock(), sandbox).Hook!;
        PurchaseOutcome? got = null;
        hook.Store.BuySku(SurvIds.OfferGemsS, o => got = o);
        Assert.False(got!.Ok);
        Assert.Equal(50, hook.Wallet.Get(SurvIds.ResHard));
    }

    [Fact]
    public void Speedup_offer_costs_twelve_gems_for_fifteen_minutes()
    {
        var hook = Hook.Session(new ManualClock()).Hook!;
        Assert.True(hook.Store.TryBuyWithCurrency(SurvIds.OfferSpeedup15));
        Assert.Equal(38, hook.Wallet.Get(SurvIds.ResHard));
        Assert.Equal(15, hook.Wallet.Get(SurvIds.ResSpeedupM));
        hook.Wallet.TrySpend(SurvIds.ResHard, 30);
        Assert.False(hook.Store.TryBuyWithCurrency(SurvIds.OfferSpeedup15));
        Assert.Equal(8, hook.Wallet.Get(SurvIds.ResHard));
        Assert.Equal(15, hook.Wallet.Get(SurvIds.ResSpeedupM));
    }

    [Fact]
    public void Free_offer_is_not_buyable_as_sku_or_currency()
    {
        var hook = Hook.Session(new ManualClock()).Hook!;
        Assert.False(hook.Store.TryBuyWithCurrency(SurvIds.OfferFreeGift));
        PurchaseOutcome? got = null;
        hook.Store.BuySku(SurvIds.OfferFreeGift, o => got = o);
        Assert.False(got!.Ok);
    }
}
