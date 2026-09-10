using System;

namespace Grove.Domain.Orders
{
    /// <summary>
    /// DEV-018 data hook. Unity can toast or later play a splash using
    /// <see cref="PresentationCopy.MilestoneSplash"/> without a full splash scene.
    /// </summary>
    public sealed class SplashStub
    {
        public SplashStub(PresentationCopy copy)
        {
            Copy = copy ?? throw new ArgumentNullException(nameof(copy));
        }

        public PresentationCopy Copy { get; }

        public string BootTitle => Copy.BootTitle;

        public string AreaName => Copy.AreaName;

        public string Goal => Copy.Goal;

        public string MilestoneSplash => Copy.MilestoneSplash;

        public bool MilestoneShown { get; private set; }

        /// <summary>Returns splash copy once when the Front Garden milestone is first reached.</summary>
        public string? TryConsumeMilestone(bool milestoneReached)
        {
            if (!milestoneReached || MilestoneShown)
            {
                return null;
            }

            MilestoneShown = true;
            return Copy.MilestoneSplash;
        }
    }
}
