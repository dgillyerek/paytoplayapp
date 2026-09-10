namespace Grove.Domain.Orders
{
    /// <summary>
    /// Boot / area / goal / milestone strings plus NPC + order-slot policy.
    /// Unity plays DEV-018 splashes from this copy plus DES-002 PNGs.
    /// </summary>
    public sealed record PresentationCopy(
        string BootTitle,
        string AreaName,
        string Goal,
        string MilestoneSplash,
        string NpcId,
        string NpcDisplayName,
        string NpcPortrait,
        int OrderSlotsMax,
        string OrderQueue)
    {
        public const string DefaultCoachCrate = "Tap the crate to grow supplies.";
        public const string DefaultCoachMerge = "Drag two matches together.";
        public const string DefaultCoachDeliver = "Deliver to Maya.";

        public string CoachCrate { get; init; } = DefaultCoachCrate;
        public string CoachMerge { get; init; } = DefaultCoachMerge;
        public string CoachDeliver { get; init; } = DefaultCoachDeliver;

        public static PresentationCopy Default { get; } = new PresentationCopy(
            "Project Grove",
            "Front Garden",
            "Restore the Front Garden",
            "Front Garden Restored",
            "maya",
            "Maya",
            "2d-ui-only",
            3,
            "scripted");
    }
}
