namespace Grove.Domain.Orders
{
    /// <summary>
    /// Boot / area / goal / milestone strings plus NPC + order-slot policy.
    /// Unity binds this for DEV-018 splash later; domain only stores the copy.
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
