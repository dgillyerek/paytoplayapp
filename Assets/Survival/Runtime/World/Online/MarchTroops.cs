#nullable enable
using System;

namespace Survival.Domain.World.Online
{
    /// <summary>
    /// Troop slider math for the send card. The slider can only pick between 1 and the troops the server says are at
    /// home; the server checks the number again, so this is for the player's eyes, not for trust.
    /// </summary>
    public static class MarchTroops
    {
        /// <summary>Where the slider starts: half of what is home (at least 1). 0 when nobody is home.</summary>
        public static int Start(int home) => home <= 0 ? 0 : Math.Max(1, home / 2);

        public static int Clamp(int troops, int home) => home <= 0 ? 0 : Math.Min(home, Math.Max(1, troops));

        /// <summary>Slider position 0..1 to a troop count (1..home).</summary>
        public static int FromSlider(float t, int home) =>
            home <= 0 ? 0 : Clamp((int)Math.Round(1 + (home - 1) * Math.Min(1f, Math.Max(0f, t)), MidpointRounding.AwayFromZero), home);

        public static float ToSlider(int troops, int home) => home <= 1 ? 1f : (Clamp(troops, home) - 1f) / (home - 1f);

        public static bool CanSend(int troops, int home) => troops >= 1 && troops <= home;
    }
}
