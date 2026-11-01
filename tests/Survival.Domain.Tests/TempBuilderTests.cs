using System.Collections.Generic;
using Survival.Domain.Base;
using Survival.Domain.Clock;
using Survival.Domain.Economy;
using Survival.Domain.Ids;
using Survival.Domain.Session;
using Survival.Domain.Store;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>Derek 2026-10-30: when every builder is busy, offer one more for 12 hours at $1.99; rebuy once it leaves.</summary>
public sealed class TempBuilderTests
{
    private static (HookSession Hook, ManualClock Clock, SandboxPurchaseService Pay) Rich()
    {
        var clock = new ManualClock();
        var pay = new SandboxPurchaseService();
        var wallet = new Wallet();
        var hook = new HookSession(Hook.Catalog(), wallet, clock, Hook.Products(), pay);
        wallet.Grant(new Dictionary<string, int> { [SurvIds.ResSoft] = 1_000_000, [SurvIds.ResFood] = 1_000_000, [SurvIds.ResWood] = 1_000_000, [SurvIds.ResStone] = 1_000_000 });
        return (hook, clock, pay);
    }

    private static void FillBuilders(HookSession hook)
    {
        for (var i = 0; hook.Timers.HasFreeSlot; i++)
        {
            Assert.True(hook.Timers.TryStart("busy." + i, SurvIds.BasePlotFarm01, 3600, out _));
        }
    }

    [Fact]
    public void The_offer_appears_only_when_every_builder_is_busy()
    {
        var (hook, _, _) = Rich();
        Assert.Equal(UpgradeBlock.None, hook.Base.CheckUpgrade(SurvIds.BasePlotHq01));
        Assert.False(hook.TempBuilderOffered(SurvIds.BasePlotHq01));
        FillBuilders(hook);
        Assert.True(hook.TempBuilderOffered(SurvIds.BasePlotHq01));
        Assert.Equal("$1.99", hook.TempBuilderProduct!.PriceLabel);
    }

    [Fact]
    public void Hiring_adds_a_builder_for_12_hours_and_starts_the_upgrade_then_it_leaves_and_can_be_hired_again()
    {
        var (hook, clock, pay) = Rich();
        FillBuilders(hook);
        var slots = hook.Timers.BaseSlots;
        var started = false;
        hook.HireTempBuilderAndUpgrade(SurvIds.BasePlotHq01, ok => started = ok);
        Assert.True(started);
        Assert.True(hook.Base.IsUpgrading(SurvIds.BasePlotHq01));
        Assert.Equal(slots + 1, hook.Timers.Slots);
        Assert.Single(pay.History);
        Assert.Equal(12 * 3600, hook.Timers.ExtraSlotRemaining, 3);

        // No second hire while it's on the job.
        PurchaseOutcome? again = null;
        hook.HireTempBuilder(o => again = o);
        Assert.False(again!.Ok);
        Assert.Single(pay.History);

        clock.Advance(12 * 3600 - 1);
        Assert.True(hook.Timers.ExtraSlotActive);
        clock.Advance(1);
        Assert.False(hook.Timers.ExtraSlotActive);
        Assert.Equal(slots, hook.Timers.Slots);

        hook.HireTempBuilder(o => again = o);
        Assert.True(again!.Ok);
        Assert.True(hook.Timers.ExtraSlotActive);
    }

    [Fact]
    public void A_job_the_temporary_builder_started_finishes_even_after_it_leaves()
    {
        var (hook, clock, _) = Rich();
        FillBuilders(hook);
        hook.HireTempBuilderAndUpgrade(SurvIds.BasePlotHq01, _ => { });
        var t = hook.Timers.Find(BaseEconomy.UpgradeTimerId(SurvIds.BasePlotHq01))!;
        var before = hook.Base.LevelOf(SurvIds.BuildingHq);
        // Past the expiry, the timer still runs; no new job fits while every permanent builder is busy.
        clock.Advance(12 * 3600 + 1);
        Assert.False(hook.Timers.HasFreeSlot);
        clock.Advance(t.Remaining(clock.Now) + 1);
        hook.Tick();
        Assert.True(hook.Base.LevelOf(SurvIds.BuildingHq) > before || hook.Base.Pending.Count > 0);
    }

    [Fact]
    public void The_hire_survives_save_and_load_and_a_failed_purchase_hires_nobody()
    {
        var (hook, clock, pay) = Rich();
        FillBuilders(hook);
        pay.FailNext = true;
        var ok = true;
        hook.HireTempBuilderAndUpgrade(SurvIds.BasePlotHq01, r => ok = r);
        Assert.False(ok);
        Assert.False(hook.Timers.ExtraSlotActive);

        hook.HireTempBuilderAndUpgrade(SurvIds.BasePlotHq01, r => ok = r);
        Assert.True(ok);
        var json = VillageSave.Capture(hook.Base, hook.Build);

        var fresh = new HookSession(Hook.Catalog(), new Wallet(), clock, Hook.Products(), new SandboxPurchaseService());
        Assert.True(VillageSave.Apply(json, fresh.Base, fresh.Build));
        Assert.True(fresh.Timers.ExtraSlotActive);
        Assert.Equal(hook.Timers.ExtraSlotUntil, fresh.Timers.ExtraSlotUntil);
        Assert.True(fresh.Base.IsUpgrading(SurvIds.BasePlotHq01));
        Assert.Equal(hook.Timers.Active.Count, fresh.Timers.Active.Count);
    }
}
