#nullable enable
namespace Survival.Domain.Base
{
    /// <summary>What one tap on a town building does. The building art, its name plate, timer and every sign floating
    /// on it (FREE / "!" badge, upgrade arrow, level shield, collect bubble, congrats pill) all route here, so tapping
    /// a sign never does less than tapping the building (Derek 2026-11-03).</summary>
    public enum PlotTapAction
    {
        /// <summary>A finished build / upgrade reward is waiting: claim it with the celebration.</summary>
        ClaimReward,

        /// <summary>A build / upgrade timer is running: finish it free (FREE / "!") or open speed-up.</summary>
        TimerAction,

        /// <summary>Resources are waiting: collect them.</summary>
        Collect,

        /// <summary>Nothing pending: open the building (its upgrade screen).</summary>
        Open,
    }

    public static class PlotTap
    {
        public static PlotTapAction Decide(bool rewardPending, bool upgrading, bool canCollect) =>
            rewardPending ? PlotTapAction.ClaimReward
            : upgrading ? PlotTapAction.TimerAction
            : canCollect ? PlotTapAction.Collect
            : PlotTapAction.Open;

        public static PlotTapAction Decide(BaseEconomy system, string plotId)
        {
            var plot = system.Find(plotId);
            return Decide(system.PendingFor(plotId) != null, system.IsUpgrading(plotId), plot?.CanCollect == true);
        }
    }
}
