namespace Survival.Domain.Layout
{
    /// <summary>Normalized HUD rectangle. Y = 0 at the bottom of the 1080×1920 Game view.</summary>
    public readonly struct HudRect
    {
        public HudRect(float xMin, float yMin, float xMax, float yMax)
        {
            XMin = xMin;
            YMin = yMin;
            XMax = xMax;
            YMax = yMax;
        }

        public float XMin { get; }
        public float YMin { get; }
        public float XMax { get; }
        public float YMax { get; }

        public static HudRect FromDesign(float xMin, float yTopMin, float xMax, float yTopMax) =>
            new HudRect(xMin, 1f - yTopMax, xMax, 1f - yTopMin);
    }

    public static class PlayHudLayout
    {
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 1920f;
    }
}
