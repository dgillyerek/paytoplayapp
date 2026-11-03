using Survival.Domain.Base;
using Survival.Domain.Clock;
using Survival.Domain.Ids;

namespace Survival.Domain.Tests;

/// <summary>Derek 2026-11-03: every sign on a building (FREE, upgrade arrow, bubble...) taps like the building.</summary>
public sealed class PlotTapTests
{
    [Theory]
    [InlineData(true, true, true, PlotTapAction.ClaimReward)]
    [InlineData(false, true, true, PlotTapAction.TimerAction)]
    [InlineData(false, false, true, PlotTapAction.Collect)]
    [InlineData(false, false, false, PlotTapAction.Open)]
    public void PriorityOrder(bool pending, bool upgrading, bool collect, PlotTapAction want) =>
        Assert.Equal(want, PlotTap.Decide(pending, upgrading, collect));

    [Fact]
    public void UpgradingCastleRoutesToTimer()
    {
        var clock = new ManualClock(1_000_000);
        var hook = Hook.Session(clock).Hook!;
        foreach (var r in new[] { SurvIds.ResSoft, SurvIds.ResStone, SurvIds.ResWood, SurvIds.ResFood })
        {
            hook.Wallet.Add(r, 50_000_000);
        }

        var farm = SurvIds.BasePlotHq01;
        Assert.NotEqual(PlotTapAction.TimerAction, PlotTap.Decide(hook.Base, farm));
        Assert.True(hook.Base.TryStartUpgrade(farm));
        Assert.Equal(PlotTapAction.TimerAction, PlotTap.Decide(hook.Base, farm));
    }
}
