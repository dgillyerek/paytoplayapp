using System;

namespace Grove.Domain.Orders
{
    /// <summary>One DEV-018 splash beat: copy plus the DES-002 art stub Unity binds.</summary>
    public sealed record SplashBeat(string Kind, string Caption, string ArtStub)
    {
        public static SplashBeat Boot(PresentationCopy copy) =>
            new SplashBeat("boot", copy.BootTitle, "Splash_Boot_ProjectGrove");

        public static SplashBeat AreaStart(PresentationCopy copy) =>
            new SplashBeat("area", copy.AreaName, "Splash_AreaStart_FrontGarden");

        public static SplashBeat Milestone(PresentationCopy copy) =>
            new SplashBeat("milestone", copy.MilestoneSplash, "Splash_Milestone_FrontGardenRestored");

        public static SplashBeat OrderComplete() =>
            new SplashBeat("order", "Order complete", "Splash_OrderComplete");
    }

    /// <summary>
    /// DEV-018 sequence: boot “Project Grove” → area start “Front Garden” →
    /// milestone “Front Garden Restored” when Order 6 completes.
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

        public bool BootShown { get; private set; }

        public bool AreaStartShown { get; private set; }

        public bool MilestoneShown { get; private set; }

        public SplashBeat? TryConsumeBoot()
        {
            if (BootShown)
            {
                return null;
            }

            BootShown = true;
            return SplashBeat.Boot(Copy);
        }

        public SplashBeat? TryConsumeAreaStart()
        {
            if (AreaStartShown)
            {
                return null;
            }

            AreaStartShown = true;
            return SplashBeat.AreaStart(Copy);
        }

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

        public SplashBeat? TryConsumeMilestoneBeat(bool milestoneReached)
        {
            var caption = TryConsumeMilestone(milestoneReached);
            if (caption == null)
            {
                return null;
            }

            return SplashBeat.Milestone(Copy);
        }
    }
}
