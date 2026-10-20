using Survival.Domain.Base;
using Survival.Domain.Clock;
using Survival.Domain.Ids;
using Survival.Domain.Session;
using Survival.Domain.Theme;

namespace Survival.Domain.Tests;

public sealed class VillageBuildTests
{
    private static (HookSession Hook, ManualClock Clock) Fresh(bool rich = true)
    {
        var clock = new ManualClock(1_000_000);
        var hook = Hook.Session(clock).Hook!;
        hook.Build.UseSpots(Layouts.Village().BuildSpots.Select(s => s.Id).ToList());
        if (rich)
        {
            foreach (var res in new[] { SurvIds.ResSoft, SurvIds.ResStone, SurvIds.ResWood, SurvIds.ResFood })
            {
                hook.Wallet.Add(res, 50_000_000);
            }
        }

        return (hook, clock);
    }

    private static void CastleTo(HookSession hook, ManualClock clock, int level)
    {
        while (hook.Base.HqLevel < level)
        {
            Assert.True(hook.Base.TryStartUpgrade(SurvIds.BasePlotHq01));
            clock.Advance(400_000);
            Assert.True(hook.FinishFree(SurvIds.BasePlotHq01) > 0);
        }
    }

    [Fact]
    public void Menu_lists_farm_lumber_quarry_forge_with_cost_timer_and_castle_gated_counts()
    {
        var types = Hook.Catalog().Buildables;
        Assert.Equal(new[] { "farm", "lumber", "quarry", "forge" }, types.Select(t => t.Type));
        foreach (var t in types)
        {
            Assert.NotEmpty(t.BuildCost);
            Assert.True(t.BuildSeconds > 0);
            Assert.True(t.Levels.Count >= VillageBuild.MaxLevel, t.Type);
            Assert.Equal(0, t.MaxCount(1)); // nothing extra at Castle Lv 1
            Assert.Equal(t.UnlockAtCastle.OrderBy(x => x), t.UnlockAtCastle);
        }

        var farm = types[0];
        Assert.Equal(new[] { 2, 4, 7, 11 }, farm.UnlockAtCastle);
        Assert.Equal(1, farm.MaxCount(2));
        Assert.Equal(2, farm.MaxCount(4));
        Assert.Equal(4, farm.MaxCount(20));
        Assert.Equal(7, farm.NextUnlockCastle(4));
        Assert.Null(farm.NextUnlockCastle(11));
        Assert.Equal(600, farm.CostFor(0)[SurvIds.ResSoft]);
        Assert.Equal(960, farm.CostFor(1)[SurvIds.ResSoft]); // each further copy costs 1.6x
        Assert.Equal(135, farm.SecondsFor(1)); // and takes 1.5x longer
        var pack = TestPaths.LoadPack();
        foreach (var t in types)
        {
            Assert.True(pack.Strings.ContainsKey("build.type." + t.Type), t.Type);
            Assert.True(pack.TryArt(HookArtKeys.ForBuildType(t.Type), out _), t.Type);
        }
    }

    [Fact]
    public void Castle_level_one_locks_build_then_castle_two_allows_one_farm()
    {
        var (hook, clock) = Fresh();
        Assert.Equal(BuildBlock.Locked, hook.Build.Check("farm"));
        Assert.Null(hook.Build.TryBuild("farm"));
        CastleTo(hook, clock, 2);
        Assert.Equal(BuildBlock.None, hook.Build.Check("farm"));
        Assert.Equal(BuildBlock.Locked, hook.Build.Check("quarry")); // quarry waits for Castle Lv 3
        Assert.NotNull(hook.Build.TryBuild("farm"));
        clock.Advance(10_000);
        Assert.True(hook.FinishFree(hook.Build.Placed.Keys.First()) > 0);
        Assert.Equal(BuildBlock.MaxCount, hook.Build.Check("farm"));
        Assert.Equal(BuildBlock.Unknown, hook.Build.Check("dragon_lair"));
    }

    [Fact]
    public void Build_spends_the_cost_runs_a_timer_and_opens_at_level_one()
    {
        var (hook, clock) = Fresh();
        CastleTo(hook, clock, 2);
        var gold = hook.Wallet.Get(SurvIds.ResSoft);
        var wood = hook.Wallet.Get(SurvIds.ResWood);
        var plot = hook.Build.TryBuild("farm", "village.spot_05")!;
        Assert.Equal("base.plot.farm_b01", plot.Id);
        Assert.Equal("farm", plot.Def.BuildType);
        Assert.Equal("village.spot_05", hook.Build.SpotOf(plot.Id));
        Assert.Equal(gold - 600, hook.Wallet.Get(SurvIds.ResSoft));
        Assert.Equal(wood - 400, hook.Wallet.Get(SurvIds.ResWood));
        Assert.True(plot.UnderConstruction);
        Assert.False(plot.IsProducer);
        Assert.Equal(UpgradeBlock.AlreadyUpgrading, hook.Base.CheckUpgrade(plot.Id));
        Assert.True(hook.Base.IsUpgrading(plot.Id));
        Assert.Equal(90, hook.Timers.Remaining(BaseEconomy.UpgradeTimerId(plot.Id)), 3);
        Assert.Equal(BuildBlock.QueueBusy, hook.Build.Check("lumber"));

        var built = new List<string>();
        hook.Base.Constructed += p => built.Add(p.Id);
        Assert.Equal(1, hook.FinishFree(plot.Id)); // 90 s is inside the free-finish window
        Assert.Equal(new[] { plot.Id }, built);
        Assert.False(plot.UnderConstruction);
        Assert.Equal(1, plot.Level);
        Assert.True(plot.IsProducer);
        clock.Advance(600);
        hook.Tick();
        Assert.True(plot.CanCollect);
        Assert.True(hook.Base.Collect(plot.Id) > 0);
        Assert.True(LevelBadge.Shows(plot.Def));
        Assert.Equal(HookArtKeys.ForBuildType("farm"), BuildingVisual.For(plot).ArtKey);
    }

    [Fact]
    public void Builds_finish_free_inside_five_minutes_and_longer_ones_take_a_gem_rush()
    {
        var (hook, clock) = Fresh();
        CastleTo(hook, clock, 12);
        var first = hook.Build.TryBuild("forge")!; // 300 s: free-finish window from the start
        Assert.True(hook.Timers.CanFinishFree(BaseEconomy.UpgradeTimerId(first.Id)));
        Assert.Equal(1, hook.FinishFree(first.Id));
        var second = hook.Build.TryBuild("forge")!; // 2nd copy: 450 s
        Assert.Equal(450, hook.Timers.Remaining(BaseEconomy.UpgradeTimerId(second.Id)), 3);
        Assert.False(hook.Timers.CanFinishFree(BaseEconomy.UpgradeTimerId(second.Id)));
        Assert.Equal(0, hook.FinishFree(second.Id));
        hook.Wallet.Add(SurvIds.ResHard, 1000);
        var gems = hook.Wallet.Get(SurvIds.ResHard);
        Assert.Equal(1, hook.Rush(second.Id, out var spent));
        Assert.True(spent > 0);
        Assert.Equal(gems - spent, hook.Wallet.Get(SurvIds.ResHard));
        Assert.False(second.UnderConstruction);
        Assert.Equal(BuildBlock.MaxCount, hook.Build.Check("forge"));
    }

    [Fact]
    public void Tapped_spot_is_used_otherwise_auto_place_takes_the_next_free_spot()
    {
        var (hook, clock) = Fresh();
        CastleTo(hook, clock, 4);
        var spots = hook.Build.Spots;
        var a = hook.Build.TryBuild("farm")!; // auto
        Assert.Equal(spots[0], hook.Build.SpotOf(a.Id));
        hook.FinishFree(a.Id);
        Assert.Equal(BuildBlock.SpotTaken, hook.Build.Check("lumber", spots[0]));
        Assert.Equal(BuildBlock.NoSpot, hook.Build.Check("lumber", "village.nowhere"));
        var b = hook.Build.TryBuild("lumber", spots[3])!;
        Assert.Equal(spots[3], hook.Build.SpotOf(b.Id));
        hook.FinishFree(b.Id);
        Assert.Equal(spots[1], hook.Build.NextFreeSpot);
        Assert.Equal(spots.Count - 2, hook.Build.FreeSpots.Count());
    }

    [Fact]
    public void No_free_spot_blocks_building()
    {
        var (hook, clock) = Fresh();
        CastleTo(hook, clock, 2);
        hook.Build.UseSpots(new[] { "village.spot_01" });
        var a = hook.Build.TryBuild("farm")!;
        hook.FinishFree(a.Id);
        Assert.Equal(BuildBlock.NoSpot, hook.Build.Check("lumber"));
    }

    [Fact]
    public void Not_enough_resources_blocks_and_refunds_nothing()
    {
        var (hook, clock) = Fresh(rich: false);
        hook.Wallet.Add(SurvIds.ResSoft, 50_000);
        hook.Wallet.Add(SurvIds.ResStone, 50_000);
        hook.Wallet.Add(SurvIds.ResWood, 50_000);
        CastleTo(hook, clock, 2);
        var wood = hook.Wallet.Get(SurvIds.ResWood);
        hook.Wallet.TrySpend(SurvIds.ResWood, wood);
        Assert.Equal(BuildBlock.Cost, hook.Build.Check("farm"));
        Assert.Null(hook.Build.TryBuild("farm"));
        Assert.Empty(hook.Build.Placed);
    }

    [Fact]
    public void Built_buildings_upgrade_to_level_six_but_not_past_the_castle()
    {
        var (hook, clock) = Fresh();
        CastleTo(hook, clock, 2);
        var farm = hook.Build.TryBuild("farm")!;
        hook.FinishFree(farm.Id);
        Assert.True(hook.Base.TryStartUpgrade(farm.Id));
        clock.Advance(100_000);
        Assert.Equal(2, hook.FinishFree(farm.Id));
        Assert.Equal(UpgradeBlock.NeedsCastle, hook.Base.CheckUpgrade(farm.Id));
        CastleTo(hook, clock, 10);
        while (hook.Base.CheckUpgrade(farm.Id) == UpgradeBlock.None)
        {
            Assert.True(hook.Base.TryStartUpgrade(farm.Id));
            clock.Advance(100_000);
            Assert.True(hook.FinishFree(farm.Id) > 0);
        }

        Assert.Equal(VillageBuild.MaxLevel, farm.Level);
        Assert.Equal(UpgradeBlock.MaxLevel, hook.Base.CheckUpgrade(farm.Id));
        Assert.Equal("6", LevelBadge.Text(farm.Level));
    }

    [Fact]
    public void Save_round_trips_levels_built_buildings_and_running_timers()
    {
        var (hook, clock) = Fresh();
        CastleTo(hook, clock, 3);
        var farm = hook.Build.TryBuild("farm", "village.spot_02")!;
        hook.FinishFree(farm.Id);
        Assert.True(hook.Base.TryStartUpgrade(farm.Id));
        clock.Advance(100_000);
        hook.FinishFree(farm.Id);
        var quarry = hook.Build.TryBuild("quarry", "village.spot_07")!; // still building at save time
        clock.Advance(40);
        var json = VillageSave.Capture(hook.Base, hook.Build);

        var clock2 = new ManualClock(clock.Now + 30); // 30 s later, game closed in between
        var fresh = Hook.Session(clock2).Hook!;
        fresh.Build.UseSpots(Layouts.Village().BuildSpots.Select(s => s.Id).ToList());
        Assert.True(VillageSave.Apply(json, fresh.Base, fresh.Build));
        Assert.Equal(3, fresh.Base.HqLevel);
        var f = fresh.Base.Find(farm.Id)!;
        Assert.Equal(2, f.Level);
        Assert.Equal("farm", f.Def.BuildType);
        Assert.Equal("village.spot_02", fresh.Build.SpotOf(farm.Id));
        var q = fresh.Base.Find(quarry.Id)!;
        Assert.True(q.UnderConstruction);
        Assert.Equal(150 - 70, fresh.Timers.Remaining(BaseEconomy.UpgradeTimerId(quarry.Id)), 3);
        clock2.Advance(80);
        Assert.Equal(1, fresh.FinishFree(quarry.Id));
        Assert.False(q.UnderConstruction);
        // New ids continue after restored ones.
        CastleTo(fresh, clock2, 4);
        Assert.Equal("base.plot.farm_b03", fresh.Build.TryBuild("farm")!.Id);
    }

    [Fact]
    public void Bad_or_empty_save_is_ignored()
    {
        var (hook, _) = Fresh();
        Assert.False(VillageSave.Apply("", hook.Base, hook.Build));
        Assert.False(VillageSave.Apply("{not json", hook.Base, hook.Build));
        Assert.True(VillageSave.Apply("{\"v\":1,\"built\":[{\"id\":\"base.plot.x_b01\",\"type\":\"nope\",\"spot\":\"village.spot_01\"}]}", hook.Base, hook.Build));
        Assert.Empty(hook.Build.Placed);
        Assert.Equal(1, hook.Base.HqLevel);
    }

    [Fact]
    public void Bigger_village_keeps_the_castle_central_and_spots_clear_of_buildings()
    {
        var v = Layouts.Village();
        Assert.Equal(3600f, v.Width);
        Assert.Equal(4800f, v.Height);
        Assert.True(v.Width * v.Height >= 2.5f * 2160f * 2880f, "about 2-3x the old area");
        Assert.True(v.MinZoom < 0.67f && v.MinZoom >= 0.4f);
        var castle = v.SpotFor(SurvIds.BasePlotHq01)!;
        Assert.InRange(castle.X, 0.45f, 0.55f);
        Assert.InRange(castle.YFromTop, 0.3f, 0.5f);
        var total = Hook.Catalog().Buildables.Sum(t => t.UnlockAtCastle.Count);
        Assert.True(v.BuildSpots.Count >= total, $"{v.BuildSpots.Count} spots for {total} buildables");
        Assert.Equal(v.BuildSpots.Count, v.BuildSpots.Select(s => s.Id).Distinct().Count());
        foreach (var s in v.BuildSpots)
        {
            var (sx, sy) = v.ToMap(s.X, s.YFromTop);
            foreach (var p in v.Plots)
            {
                var (px, py) = v.ToMap(p.X, p.YFromTop);
                var min = (s.Size + p.Size) * v.Width * 0.5f;
                Assert.True(MathF.Sqrt((sx - px) * (sx - px) + (sy - py) * (sy - py)) >= min * 0.9f, s.Id + " vs " + p.PlotId);
            }

            foreach (var o in v.BuildSpots.Where(o => o != s))
            {
                var (ox, oy) = v.ToMap(o.X, o.YFromTop);
                Assert.True(MathF.Sqrt((sx - ox) * (sx - ox) + (sy - oy) * (sy - oy)) >= s.Size * v.Width, s.Id + " vs " + o.Id);
            }
        }

        foreach (var t in new[] { "tree", "rocks", "field", "lake", "forest" })
        {
            Assert.Contains(v.Props, p => p.Type == t);
        }
    }
}
