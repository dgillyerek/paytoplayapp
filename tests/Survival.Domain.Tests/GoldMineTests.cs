using Survival.Domain.Base;
using Survival.Domain.Clock;
using Survival.Domain.Ids;
using Survival.Domain.Session;
using Survival.Domain.Theme;

namespace Survival.Domain.Tests;

/// <summary>The Gold Mine: a Build-menu village building that digs gold the way a farm grows food.</summary>
public sealed class GoldMineTests
{
    private static (HookSession Hook, ManualClock Clock) Fresh(int castle)
    {
        var clock = new ManualClock(1_000_000);
        var hook = Hook.Session(clock).Hook!;
        hook.Build.UseSpots(Layouts.Village().BuildSpots.Select(s => s.Id).ToList());
        foreach (var res in new[] { SurvIds.ResSoft, SurvIds.ResStone, SurvIds.ResWood, SurvIds.ResFood })
        {
            hook.Wallet.Add(res, 50_000_000);
        }

        while (hook.Base.HqLevel < castle)
        {
            Assert.True(hook.Base.TryStartUpgrade(SurvIds.BasePlotHq01));
            clock.Advance(400_000);
            hook.Tick();
        }

        hook.Base.ClaimCompletion(SurvIds.BasePlotHq01);
        return (hook, clock);
    }

    /// <summary>Build a mine and let construction finish on its own; collect everything so it starts empty.</summary>
    private static BasePlot Mine(HookSession hook, ManualClock clock)
    {
        var plot = hook.Build.TryBuild("mine")!;
        Assert.NotNull(plot);
        clock.Advance(hook.Timers.Remaining(BaseEconomy.UpgradeTimerId(plot.Id)) + 1);
        hook.Tick();
        Assert.False(plot.UnderConstruction);
        hook.Base.ClaimCompletion(plot.Id);
        return plot;
    }

    [Fact]
    public void Mine_is_a_build_menu_gold_producer_unlocked_with_the_first_farm()
    {
        var mine = Hook.Catalog().Buildables.Single(t => t.Type == "mine");
        var farm = Hook.Catalog().Buildables.Single(t => t.Type == "farm");
        Assert.Equal(SurvIds.ResSoft, mine.Produces);
        Assert.Equal(0, mine.MaxCount(1));
        Assert.Equal(1, mine.MaxCount(2));
        Assert.Equal(farm.UnlockAtCastle[0], mine.UnlockAtCastle[0]);
        Assert.Equal(1, mine.MaxCount(30)); // one mine
        Assert.Equal(10, mine.Levels.Count);
        for (var i = 1; i < mine.Levels.Count; i++)
        {
            Assert.True(mine.Levels[i].PerHour > mine.Levels[i - 1].PerHour, $"L{i + 1} rate");
            Assert.True(mine.Levels[i].Capacity > mine.Levels[i - 1].Capacity, $"L{i + 1} cap");
            Assert.Equal(mine.Levels[i].PerHour * 6, mine.Levels[i].Capacity); // fills in 6 h: 4 collects a day get it all
        }

        for (var i = 1; i < mine.Levels.Count - 1; i++)
        {
            Assert.True(mine.Levels[i].UpgradeCost[SurvIds.ResSoft] > mine.Levels[i - 1].UpgradeCost[SurvIds.ResSoft], $"L{i + 1} cost");
            Assert.True(mine.Levels[i].UpgradeSeconds > mine.Levels[i - 1].UpgradeSeconds, $"L{i + 1} timer");
        }

        // White-label: name, description and art come from the ThemePack.
        var pack = TestPaths.LoadPack();
        Assert.Equal("Gold Mine", pack.StringOr("build.type.mine", ""));
        Assert.False(string.IsNullOrEmpty(pack.StringOr("build.desc.mine", "")));
        Assert.True(pack.TryArt(HookArtKeys.ForBuildType("mine"), out var rel));
        Assert.True(File.Exists(Path.Combine(Layouts.RepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a", rel)), rel);
    }

    [Fact]
    public void Production_accrues_at_the_rate_up_to_the_cap_and_collect_grants_gold()
    {
        var (hook, clock) = Fresh(2);
        var plot = Mine(hook, clock);
        Assert.True(plot.IsProducer);
        Assert.Equal(SurvIds.ResSoft, plot.Def.Produces);
        Assert.Equal(0, plot.Collectable);
        var rate = plot.Current.PerHour;
        var cap = plot.Current.Capacity;

        clock.Advance(3600);
        hook.Tick();
        Assert.Equal(rate, plot.Collectable);
        Assert.False(plot.IsFull);

        clock.Advance(100 * 3600); // far past the cap
        hook.Tick();
        Assert.Equal(cap, plot.Collectable);
        Assert.True(plot.IsFull);
        Assert.True(plot.CanCollect);

        var gold = hook.Wallet.Get(SurvIds.ResSoft);
        Assert.Equal(cap, hook.Base.Collect(plot.Id));
        Assert.Equal(gold + cap, hook.Wallet.Get(SurvIds.ResSoft));
        Assert.Equal(0, plot.Collectable);
        Assert.False(plot.CanCollect);
        Assert.Equal(HookArtKeys.ForBuildType("mine"), BuildingVisual.For(plot).ArtKey);
    }

    [Fact]
    public void Upgrade_raises_rate_and_cap_and_completes_on_its_own_but_not_past_the_castle()
    {
        var (hook, clock) = Fresh(3);
        var plot = Mine(hook, clock);
        var rate = plot.Current.PerHour;
        var cap = plot.Current.Capacity;
        var gold = hook.Wallet.Get(SurvIds.ResSoft);
        var cost = plot.Current.UpgradeCost[SurvIds.ResSoft];
        var autos = new List<(string, int)>();
        hook.Base.AutoCompleted += (p, l, _) => autos.Add((p.Id, l));

        Assert.True(hook.Base.TryStartUpgrade(plot.Id));
        Assert.Equal(gold - cost, hook.Wallet.Get(SurvIds.ResSoft));
        clock.Advance(hook.Timers.Remaining(BaseEconomy.UpgradeTimerId(plot.Id)) + 1);
        hook.Tick();
        Assert.Equal(2, plot.Level);
        Assert.Equal(new[] { (plot.Id, 2) }, autos);
        Assert.True(plot.Current.PerHour > rate);
        Assert.True(plot.Current.Capacity > cap);
        Assert.Equal(plot.Current.PerHour, hook.Base.PendingFor(plot.Id)!.Rewards[SurvIds.ResSoft]); // an hour of gold

        // Level 3 = Castle 3: the next one waits for the Castle.
        Assert.True(hook.Base.TryStartUpgrade(plot.Id));
        clock.Advance(hook.Timers.Remaining(BaseEconomy.UpgradeTimerId(plot.Id)) + 1);
        hook.Tick();
        Assert.Equal(3, plot.Level);
        Assert.Equal(UpgradeBlock.NeedsCastle, hook.Base.CheckUpgrade(plot.Id));
    }

    [Fact]
    public void Save_keeps_the_mine_its_level_and_its_uncollected_gold_and_it_fills_while_closed()
    {
        var (hook, clock) = Fresh(2);
        var plot = Mine(hook, clock);
        Assert.True(hook.Base.TryStartUpgrade(plot.Id));
        clock.Advance(hook.Timers.Remaining(BaseEconomy.UpgradeTimerId(plot.Id)) + 1);
        hook.Tick();
        hook.Base.Collect(plot.Id);
        clock.Advance(3600);
        hook.Tick();
        var stored = plot.Stored;
        Assert.True(stored > 0);
        var json = VillageSave.Capture(hook.Base, hook.Build);

        var clock2 = new ManualClock(clock.Now + 1800); // half an hour closed
        var fresh = Hook.Session(clock2).Hook!;
        fresh.Build.UseSpots(Layouts.Village().BuildSpots.Select(s => s.Id).ToList());
        Assert.True(VillageSave.Apply(json, fresh.Base, fresh.Build));
        var m = fresh.Base.Find(plot.Id)!;
        Assert.Equal("mine", m.Def.BuildType);
        Assert.Equal(2, m.Level);
        Assert.Equal(stored, m.Stored, 6);
        fresh.Tick();
        Assert.Equal(stored + m.Current.PerHour / 2.0, m.Stored, 3);

        // A long absence still stops at the cap; an old save without stock starts empty.
        clock2.Advance(100 * 3600);
        fresh.Tick();
        Assert.True(m.IsFull);
        var old = Hook.Session(new ManualClock(clock.Now)).Hook!;
        old.Build.UseSpots(Layouts.Village().BuildSpots.Select(s => s.Id).ToList());
        Assert.True(VillageSave.Apply(json.Substring(0, json.IndexOf(",\"stock\"")) + "}", old.Base, old.Build));
        Assert.Equal(0, old.Base.Find(plot.Id)!.Stored);
    }

    [Fact]
    public void Balance_the_mine_makes_free_L31_to_50_very_slow_instead_of_impossible_and_leaves_payers_alone()
    {
        var r = BalanceReport.Get();
        double Day(string p, int n) => r.Row(p, n).ClearedDay ?? double.PositiveInfinity;
        Assert.Equal(1, r.Def.GoldMines);

        // Free: L1-30 on the schedule (about day 10), the wall still at ~L31 ...
        Assert.InRange(r.Wall("free"), 30, 33);
        Assert.True(Day("free", 30) <= 11, $"free L30 day {Day("free", 30)}");

        // ... but no longer stuck for ever: L40 in months, L50 in well over a year (inside the 3-year model horizon).
        Assert.InRange(Day("free", 40), 90, 240);
        Assert.InRange(Day("free", 50), 500, 1000);
        Assert.Equal(r.Levels, r.Furthest("free"));

        // The free model builds the mine early and maxes it; it's the bulk of his late gold.
        var free = r.Economies["free"];
        Assert.True(free.Daily[9].Mine >= 5, $"mine L{free.Daily[9].Mine} on day 10");
        Assert.Equal(10, free.Daily[^1].Mine);
        var late = free.Daily[365];
        var earlier = free.Daily[180];
        Assert.True(late.MineGoldEarned - earlier.MineGoldEarned > 0.5 * (late.GoldEarned - earlier.GoldEarned), "mine share of free gold, days 180-365");

        // Payers unchanged or faster than before the mine (L50 days before: light 63.3, mid 28.3, whale 16.3).
        Assert.True(Day("light", 50) <= 63.4, $"light L50 day {Day("light", 50)}");
        Assert.True(Day("mid", 50) <= 28.4, $"mid L50 day {Day("mid", 50)}");
        Assert.True(Day("whale", 50) <= 16.4, $"whale L50 day {Day("whale", 50)}");
    }
}
