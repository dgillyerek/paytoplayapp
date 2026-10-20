using Survival.Domain.Base;
using Survival.Domain.Clock;
using Survival.Domain.Ids;
using Survival.Domain.Session;

namespace Survival.Domain.Tests;

/// <summary>Build / upgrade timers finish on their own; the reward waits in the congratulations bubble until tapped.</summary>
public sealed class AutoCompleteTests
{
    private static (HookSession Hook, ManualClock Clock) Fresh()
    {
        var clock = new ManualClock(1_000_000);
        var hook = Hook.Session(clock).Hook!;
        hook.Build.UseSpots(Layouts.Village().BuildSpots.Select(s => s.Id).ToList());
        foreach (var res in new[] { SurvIds.ResSoft, SurvIds.ResStone, SurvIds.ResWood, SurvIds.ResFood })
        {
            hook.Wallet.Add(res, 50_000_000);
        }

        return (hook, clock);
    }

    /// <summary>Castle up first so the Farm may level (buildings can't outgrow the Castle); clear its bubbles.</summary>
    private static (HookSession Hook, ManualClock Clock) WithCastle(int level)
    {
        var (hook, clock) = Fresh();
        while (hook.Base.HqLevel < level)
        {
            Assert.True(hook.Base.TryStartUpgrade(SurvIds.BasePlotHq01));
            clock.Advance(400_000);
            hook.Tick();
        }

        Assert.NotNull(hook.Base.ClaimCompletion(SurvIds.BasePlotHq01));
        return (hook, clock);
    }

    private static double Seconds(HookSession hook, string plotId) => hook.Timers.Remaining(BaseEconomy.UpgradeTimerId(plotId));

    [Fact]
    public void Upgrade_completes_by_itself_when_the_timer_runs_out_and_the_reward_waits_for_the_tap()
    {
        var (hook, clock) = WithCastle(4);
        var farm = SurvIds.BasePlotFarm01;
        var before = hook.Base.Find(farm)!.Level;
        var autos = new List<(string, int, bool)>();
        hook.Base.AutoCompleted += (p, l, b) => autos.Add((p.Id, l, b));
        Assert.True(hook.Base.TryStartUpgrade(farm));

        clock.Advance(Seconds(hook, farm) - 1);
        hook.Tick();
        Assert.Equal(before, hook.Base.Find(farm)!.Level); // a second to go: nothing yet
        Assert.Empty(autos);

        clock.Advance(2);
        hook.Tick();
        Assert.Equal(before + 1, hook.Base.Find(farm)!.Level);
        Assert.False(hook.Base.IsUpgrading(farm));
        Assert.Equal(new[] { (farm, before + 1, false) }, autos);

        var pending = hook.Base.PendingFor(farm);
        Assert.NotNull(pending);
        var food = hook.Wallet.Get(SurvIds.ResFood);
        hook.Tick();
        Assert.Equal(food, hook.Wallet.Get(SurvIds.ResFood)); // not auto-granted
        var reward = pending!.Rewards[SurvIds.ResFood];
        Assert.Equal(hook.Base.Find(farm)!.Current.PerHour, reward); // one hour of the new level's output

        Assert.NotNull(hook.Base.ClaimCompletion(farm));
        Assert.Equal(food + reward, hook.Wallet.Get(SurvIds.ResFood));
        Assert.Null(hook.Base.PendingFor(farm));
        Assert.Null(hook.Base.ClaimCompletion(farm)); // once only
    }

    [Fact]
    public void Construction_opens_at_level_one_by_itself_and_starts_producing()
    {
        var (hook, clock) = Fresh();
        while (hook.Base.HqLevel < 2)
        {
            Assert.True(hook.Base.TryStartUpgrade(SurvIds.BasePlotHq01));
            clock.Advance(400_000);
            hook.Tick();
        }

        Assert.NotNull(hook.Base.ClaimCompletion(SurvIds.BasePlotHq01)); // the Castle's own bubble (gold)
        var plot = hook.Build.TryBuild("farm")!;
        Assert.True(plot.UnderConstruction);
        Assert.False(plot.IsProducer);
        clock.Advance(Seconds(hook, plot.Id) + 1);
        hook.Tick();
        Assert.False(plot.UnderConstruction);
        Assert.True(plot.IsProducer);
        Assert.Equal(1, plot.Level);
        Assert.True(hook.Base.PendingFor(plot.Id)!.WasBuild);

        clock.Advance(3600);
        hook.Tick();
        Assert.True(plot.Stored > 0); // production resumed
    }

    [Fact]
    public void Castle_bubble_pays_a_quarter_of_the_step_gold()
    {
        var (hook, clock) = Fresh();
        var paid = hook.Base.Hq!.UpgradeCost[SurvIds.ResSoft];
        Assert.True(hook.Base.TryStartUpgrade(SurvIds.BasePlotHq01));
        clock.Advance(400_000);
        hook.Tick();
        var gold = hook.Base.PendingFor(SurvIds.BasePlotHq01)!.Rewards[SurvIds.ResSoft];
        Assert.Equal(Math.Max(BaseEconomy.MinCompletionGold, (int)(Math.Round(paid * 0.25 / 10.0, MidpointRounding.AwayFromZero) * 10)), gold);
    }

    [Fact]
    public void Timers_that_ran_out_while_closed_complete_at_startup_and_the_bubble_survives_a_save()
    {
        var (hook, clock) = WithCastle(4);
        var farm = SurvIds.BasePlotFarm01;
        var level = hook.Base.Find(farm)!.Level;
        Assert.True(hook.Base.TryStartUpgrade(farm));
        var save = VillageSave.Capture(hook.Base, hook.Build);

        // Reopen a day later.
        var later = new ManualClock(clock.Now + 86_400);
        var reopened = Hook.Session(later).Hook!;
        reopened.Build.UseSpots(Layouts.Village().BuildSpots.Select(s => s.Id).ToList());
        Assert.True(VillageSave.Apply(save, reopened.Base, reopened.Build));
        Assert.True(reopened.Base.IsUpgrading(farm));
        var done = reopened.Base.CompleteDueUpgrades();
        Assert.Single(done);
        Assert.Equal(level + 1, reopened.Base.Find(farm)!.Level);
        var p = reopened.Base.Find(farm)!;
        Assert.True(p.Stored <= p.Current.Capacity);

        // Closed again before tapping: the bubble comes back.
        var save2 = VillageSave.Capture(reopened.Base, reopened.Build);
        var third = Hook.Session(new ManualClock(later.Now + 10)).Hook!;
        third.Build.UseSpots(Layouts.Village().BuildSpots.Select(s => s.Id).ToList());
        Assert.True(VillageSave.Apply(save2, third.Base, third.Build));
        Assert.Empty(third.Base.CompleteDueUpgrades());
        var pending = third.Base.PendingFor(farm);
        Assert.NotNull(pending);
        Assert.Equal(level + 1, pending!.Level);
        var food = third.Wallet.Get(SurvIds.ResFood);
        Assert.NotNull(third.Base.ClaimCompletion(farm));
        Assert.Equal(food + pending.Rewards[SurvIds.ResFood], third.Wallet.Get(SurvIds.ResFood));
    }

    [Fact]
    public void Gem_rush_and_free_finish_still_work_and_leave_the_reward_to_collect()
    {
        var (hook, clock) = WithCastle(4);
        hook.Wallet.Add(SurvIds.ResHard, 10_000);
        var farm = SurvIds.BasePlotFarm01;
        var level = hook.Base.Find(farm)!.Level;
        Assert.True(hook.Base.TryStartUpgrade(farm));
        var gems = hook.Wallet.Get(SurvIds.ResHard);
        Assert.Equal(level + 1, hook.Rush(farm, out var spent));
        Assert.Equal(gems - spent, hook.Wallet.Get(SurvIds.ResHard));
        Assert.NotNull(hook.Base.ClaimCompletion(farm));

        // Free finish inside the window (under 5 minutes left).
        Assert.True(hook.Base.TryStartUpgrade(farm));
        var left = Seconds(hook, farm);
        clock.Advance(Math.Max(0, left - 60));
        Assert.True(hook.Timers.CanFinishFree(BaseEconomy.UpgradeTimerId(farm)));
        Assert.Equal(level + 2, hook.FinishFree(farm));
        Assert.NotNull(hook.Base.PendingFor(farm));
    }

    [Fact]
    public void Two_steps_finishing_before_a_tap_stack_into_one_bubble()
    {
        var (hook, clock) = WithCastle(4);
        var farm = SurvIds.BasePlotFarm01;
        Assert.True(hook.Base.TryStartUpgrade(farm));
        clock.Advance(400_000);
        hook.Tick();
        var first = hook.Base.PendingFor(farm)!.Rewards[SurvIds.ResFood];
        Assert.True(hook.Base.TryStartUpgrade(farm));
        clock.Advance(400_000);
        hook.Tick();
        var both = hook.Base.PendingFor(farm)!;
        Assert.Single(hook.Base.Pending);
        Assert.Equal(first + hook.Base.Find(farm)!.Current.PerHour, both.Rewards[SurvIds.ResFood]);
    }
}
