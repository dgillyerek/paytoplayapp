#nullable enable
using Survival.Domain.Clock;

namespace Survival.Domain.Base
{
    /// <summary>
    /// The Build button's upgrade badge and the Build menu's extra-builder offer (Derek 2026-11-01).
    /// Badge: something can be built or upgraded right now with what the player has (unlocked, room left, affordable),
    /// counting jobs that only wait on a busy builder, so the badge stays up while builders work.
    /// </summary>
    public static class BuildReady
    {
        /// <summary>A build block that still counts as "ready": free to start, or only waiting on a builder.</summary>
        public static bool Counts(BuildBlock block) => block == BuildBlock.None || block == BuildBlock.QueueBusy;

        public static bool Counts(UpgradeBlock block) => block == UpgradeBlock.None || block == UpgradeBlock.QueueBusy;

        /// <summary>True when at least one new building or building upgrade is unlocked and affordable (builder busy or not).</summary>
        public static bool Any(BaseEconomy economy, VillageBuild build)
        {
            foreach (var t in build.Types)
            {
                if (Counts(build.Check(t.Type)) && economy.Wallet.CanAfford(build.CostOf(t.Type)))
                {
                    return true;
                }
            }

            foreach (var p in economy.Plots)
            {
                if (Counts(economy.CheckUpgrade(p.Def.Id)) && economy.Wallet.CanAfford(p.UpgradeCost))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Show the "hire an extra builder" card in the Build menu: the product exists and none is hired now.</summary>
        public static bool ShowBuilderOffer(TimerQueue timers, bool productAvailable) => productAvailable && !timers.ExtraSlotActive;

        /// <summary>
        /// What a Build-menu row shows (Derek 2026-11-02): a job that only waits on a builder looks exactly like a free one.
        /// Busy + unaffordable shows as Cost, since the builder check runs before the cost check.
        /// </summary>
        public static BuildBlock Shown(BuildBlock block, bool canAfford) =>
            block == BuildBlock.QueueBusy ? (canAfford ? BuildBlock.None : BuildBlock.Cost) : block;

        /// <summary>
        /// Where a tap on a Build-menu row goes (Derek 2026-11-02): free builder starts placement; no free builder
        /// opens the buy-a-builder screen (when it can be bought); anything else is refused with its reason.
        /// </summary>
        public static BuildTap Route(BuildBlock block, bool canAfford, bool builderOffered)
        {
            if (block == BuildBlock.None)
            {
                return BuildTap.Place;
            }

            if (block == BuildBlock.QueueBusy && canAfford && builderOffered)
            {
                return BuildTap.BuyBuilder;
            }

            return BuildTap.Refuse;
        }
    }

    /// <summary>Result of tapping a Build-menu row.</summary>
    public enum BuildTap
    {
        Place,
        BuyBuilder,
        Refuse,
    }
}
