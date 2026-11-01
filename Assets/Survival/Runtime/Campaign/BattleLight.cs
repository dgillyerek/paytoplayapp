using System;

namespace Survival.Domain.Campaign
{
    /// <summary>
    /// Lighting for the open-arena battle (Derek 2026-10-28: "add some light to the scene. the characters are very
    /// dark"). One warm sun that turns with the orbiting camera so fighters are always lit from over the viewer's
    /// shoulder (never backlit), a bright warm sky/ground trilight in place of the old grey flat ambient and the second
    /// directional fill, and fog pushed past the fighting area so it no longer greys the far fighters.
    /// </summary>
    public static class BattleLight
    {
        /// <summary>Sun elevation: high enough to light faces under helmets, low enough to model the bodies.</summary>
        public const float SunPitchDegrees = 48f;

        /// <summary>Sun yaw relative to the camera's view direction: from behind and to the right of the viewer.</summary>
        public const float SunSideDegrees = 35f;

        public const float SunIntensity = 1.35f;
        public static readonly (float R, float G, float B) SunColour = (1f, 0.93f, 0.80f);

        /// <summary>Trilight ambient: warm sky, sunlit-grass horizon bounce, warm earth below.</summary>
        public static readonly (float R, float G, float B) SkyAmbient = (0.66f, 0.68f, 0.74f);
        public static readonly (float R, float G, float B) EquatorAmbient = (0.56f, 0.53f, 0.44f);
        public static readonly (float R, float G, float B) GroundAmbient = (0.34f, 0.31f, 0.24f);

        /// <summary>Fog starts beyond the far side of the arena as seen from the orbit, so fighters stay clear.</summary>
        public static float FogStartMetres(float arenaRadius) => (2.4f * arenaRadius) + 12f;

        public static float FogEndMetres(float arenaRadius) => FogStartMetres(arenaRadius) + 24f;

        /// <summary>The sun's yaw for a camera looking along <paramref name="cameraYawDegrees"/> (world forward yaw).</summary>
        public static float SunYawDegrees(float cameraYawDegrees) => Mod360(cameraYawDegrees + SunSideDegrees);

        /// <summary>Angle between the sun's travel and the camera's view, 0 = light straight from behind the viewer.</summary>
        public static float BacklightDegrees(float cameraYawDegrees)
        {
            var d = Math.Abs(Mod360(SunYawDegrees(cameraYawDegrees) - cameraYawDegrees));
            return d > 180f ? 360f - d : d;
        }

        private static float Mod360(float a) => ((a % 360f) + 360f) % 360f;
    }
}
