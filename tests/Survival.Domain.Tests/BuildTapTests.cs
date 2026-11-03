using Survival.Domain.Base;
using Survival.Domain.Clock;
using Survival.Domain.Ids;
using Survival.Domain.Session;

namespace Survival.Domain.Tests;

/// <summary>Derek 2026-11-02: Build rows stay tappable; no free builder opens the buy-a-builder screen.</summary>
public sealed class BuildTapTests
{
    private static HookSession Rich(int castle)
    {
        var clock = new ManualClock(1_000_000);
        var hook = Hook.Session(clock).Hook!;
        hook.Build.UseSpots(Layouts.Village().BuildSpots.Select(s => s.Id).ToList());
        foreach (var r in new[] { SurvIds.ResSoft, SurvIds.ResStone, SurvIds.ResWood, SurvIds.ResFood })
        {
            hook.Wallet.Add(r, 50_000_000);
        }

        while (hook.Base.HqLevel < castle)
        {
            Assert.True(hook.Base.TryStartUpgrade(SurvIds.BasePlotHq01));
            clock.Advance(10_000_000);
            Assert.True(hook.FinishFree(SurvIds.BasePlotHq01) > 0 || hook.Base.TryFinishUpgrade(SurvIds.BasePlotHq01) > 0);
        }

        return hook;
    }

    private static BuildTap Tap(HookSession hook, string type) =>
        BuildReady.Route(hook.Build.Check(type), hook.Wallet.CanAfford(hook.Build.CostOf(type)), hook.TempBuilderOfferedForBuild(type) && hook.TempBuilderProduct != null);

    private static void FillBuilders(HookSession hook)
    {
        for (var i = 0; hook.Timers.HasFreeSlot; i++)
        {
            Assert.True(hook.Timers.TryStart("busy." + i, SurvIds.BasePlotFarm01, 3600, out _));
        }
    }

    [Fact]
    public void Free_builder_starts_the_build()
    {
        var hook = Rich(3);
        Assert.Equal(BuildTap.Place, Tap(hook, "farm"));
    }

    [Fact]
    public void No_free_builder_opens_the_buy_screen_and_row_looks_buildable()
    {
        var hook = Rich(3);
        FillBuilders(hook);
        Assert.Equal(BuildBlock.QueueBusy, hook.Build.Check("farm"));
        Assert.Equal(BuildTap.BuyBuilder, Tap(hook, "farm"));
        Assert.Equal(BuildBlock.None, BuildReady.Shown(hook.Build.Check("farm"), true));
    }

    [Fact]
    public void Extra_builder_already_hired_and_busy_is_refused()
    {
        var hook = Rich(3);
        hook.Timers.ExtraSlotUntil = long.MaxValue / 4;
        FillBuilders(hook);
        Assert.Equal(BuildTap.Refuse, Tap(hook, "farm"));
    }

    [Fact]
    public void Other_blocks_keep_their_behavior()
    {
        Assert.Equal(BuildTap.Refuse, BuildReady.Route(BuildBlock.Locked, true, true));
        Assert.Equal(BuildTap.Refuse, BuildReady.Route(BuildBlock.Cost, false, true));
        Assert.Equal(BuildTap.Refuse, BuildReady.Route(BuildBlock.QueueBusy, false, true));
        Assert.Equal(BuildBlock.Cost, BuildReady.Shown(BuildBlock.QueueBusy, false));
        Assert.Equal(BuildBlock.Locked, BuildReady.Shown(BuildBlock.Locked, true));
    }
}
