using Survival.Domain.Base;
using Survival.Domain.Clock;
using Survival.Domain.Ids;
using Survival.Domain.Session;

namespace Survival.Domain.Tests;

public sealed class BuildReadyTests
{
    private static readonly string[] Res = { SurvIds.ResSoft, SurvIds.ResStone, SurvIds.ResWood, SurvIds.ResFood };

    private static (HookSession Hook, ManualClock Clock) Fresh()
    {
        var clock = new ManualClock(1_000_000);
        var hook = Hook.Session(clock).Hook!;
        hook.Build.UseSpots(Layouts.Village().BuildSpots.Select(s => s.Id).ToList());
        return (hook, clock);
    }

    private static void Rich(HookSession hook)
    {
        foreach (var r in Res)
        {
            hook.Wallet.Add(r, 50_000_000);
        }
    }

    private static void Broke(HookSession hook)
    {
        foreach (var r in Res)
        {
            hook.Wallet.TrySpend(r, hook.Wallet.Get(r));
        }
    }

    private static void FillBuilders(HookSession hook)
    {
        foreach (var p in hook.Base.Plots.ToList())
        {
            if (!hook.Timers.HasFreeSlot)
            {
                break;
            }

            hook.Base.TryStartUpgrade(p.Id);
        }

        Assert.False(hook.Timers.HasFreeSlot);
    }

    [Fact]
    public void Affordable_upgrade_shows_the_badge()
    {
        var (hook, _) = Fresh();
        Rich(hook);
        Assert.True(BuildReady.Any(hook.Base, hook.Build));
    }

    private static void CastleTo(HookSession hook, ManualClock clock, int level)
    {
        while (hook.Base.HqLevel < level)
        {
            Assert.True(hook.Base.TryStartUpgrade(SurvIds.BasePlotHq01));
            clock.Advance(400_000);
            var t = hook.Timers.Find(BaseEconomy.UpgradeTimerId(SurvIds.BasePlotHq01));
            if (t != null)
            {
                var wait = t.Remaining(clock.Now) - hook.Timers.Rules.FreeWindow(t.TotalSeconds);
                if (wait > 0)
                {
                    clock.Advance(wait);
                }
            }

            Assert.True(hook.FinishFree(SurvIds.BasePlotHq01) > 0);
        }
    }

    [Fact]
    public void Badge_stays_up_while_every_builder_is_busy()
    {
        var (hook, clock) = Fresh();
        Rich(hook);
        CastleTo(hook, clock, 3);
        FillBuilders(hook);
        Assert.Equal(BuildBlock.QueueBusy, hook.Build.Check("farm"));
        Assert.True(BuildReady.Any(hook.Base, hook.Build));
    }

    [Fact]
    public void Nothing_affordable_hides_the_badge_even_with_free_builders()
    {
        var (hook, _) = Fresh();
        Broke(hook);
        Assert.True(hook.Timers.HasFreeSlot);
        Assert.False(BuildReady.Any(hook.Base, hook.Build));
    }

    [Fact]
    public void Busy_and_broke_is_false()
    {
        var (hook, _) = Fresh();
        Rich(hook);
        FillBuilders(hook);
        Broke(hook);
        Assert.False(BuildReady.Any(hook.Base, hook.Build));
    }

    [Fact]
    public void Block_classification()
    {
        Assert.True(BuildReady.Counts(BuildBlock.None));
        Assert.True(BuildReady.Counts(BuildBlock.QueueBusy));
        Assert.False(BuildReady.Counts(BuildBlock.Locked));
        Assert.False(BuildReady.Counts(BuildBlock.Cost));
        Assert.True(BuildReady.Counts(UpgradeBlock.QueueBusy));
        Assert.False(BuildReady.Counts(UpgradeBlock.NeedsCastle));
        Assert.False(BuildReady.Counts(UpgradeBlock.AlreadyUpgrading));
    }

    [Fact]
    public void Builder_offer_shows_until_hired_and_again_after_it_expires()
    {
        var (hook, clock) = Fresh();
        Assert.NotNull(hook.TempBuilderProduct);
        Assert.True(BuildReady.ShowBuilderOffer(hook.Timers, true));
        Assert.False(BuildReady.ShowBuilderOffer(hook.Timers, false));
        hook.Timers.ExtraSlotUntil = clock.Now + HookSession.TempBuilderSeconds;
        Assert.False(BuildReady.ShowBuilderOffer(hook.Timers, true));
        clock.Advance(HookSession.TempBuilderSeconds + 1);
        Assert.True(BuildReady.ShowBuilderOffer(hook.Timers, true));
    }
}
