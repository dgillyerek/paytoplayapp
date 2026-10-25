#nullable enable
using System;

namespace Survival.Domain.Runner
{
    /// <summary>
    /// What the runner shows when a box / chest pays out (Derek 2026-10-24: "the powerup notification banner is too in
    /// the way maybe make it just a quick pop-up from the chests where it's granted"). No screen banner: a small
    /// world-space pop-up (icon + short label such as "+25% DMG" or "+1 Lyra") rises from the box where it was granted
    /// and fades in under a second, above the road ahead of the squad, so it never covers the play area or the squad.
    /// "Squad full" pays out the same way at the lead. The boss warnings (boss incoming, quake DODGE!) stay banners.
    /// </summary>
    public static class RunnerPickupPopup
    {
        /// <summary>How long a pop-up shows, in seconds.</summary>
        public const float LifeSeconds = 0.9f;

        /// <summary>How far it rises over its life, in metres.</summary>
        public const float RiseMetres = 1.2f;

        /// <summary>Height above the road it starts at: the top of the opened box.</summary>
        public const float StartHeight = 1.4f;

        /// <summary>Label text size (the old banner was 40 across most of the screen; this is a small tag).</summary>
        public const int TextSize = 30;

        public static RunnerPopupSpec For(RunnerBoxDef box, float x, float z)
        {
            var pct = (int)Math.Round(box.Amount * 100f, MidpointRounding.AwayFromZero);
            return box.Kind switch
            {
                RunnerBoxKind.Weapon => new RunnerPopupSpec("runner.box.weapon", "+{0}% DMG", pct.ToString(), "", WeaponIcon, x, z),
                RunnerBoxKind.Rate => new RunnerPopupSpec("runner.box.rate", "+{0}% SPD", pct.ToString(), "", SpeedIcon, x, z),
                RunnerBoxKind.Range => new RunnerPopupSpec("runner.box.range", "+{0}% RNG", pct.ToString(), "", WeaponIcon, x, z),
                RunnerBoxKind.Heal => new RunnerPopupSpec("runner.box.heal", "+{0}% HP", pct.ToString(), "", HealIcon, x, z),
                RunnerBoxKind.Hero => new RunnerPopupSpec("runner.popup.hero", "+1 {0}", box.Unit, "", HeroIcon, x, z, UnitArg: 0),
                _ => new RunnerPopupSpec("runner.joined", "+{0} {1}", Math.Max(1, box.Count).ToString(), box.Unit, HeroIcon, x, z, UnitArg: 1),
            };
        }

        /// <summary>A follower / hero chest at a full squad: the extra units pay out as damage (×<paramref name="damageMul"/>), at the lead.</summary>
        public static RunnerPopupSpec SquadFull(float damageMul, float x, float z) =>
            new RunnerPopupSpec("runner.popup.squad_full", "Squad full! ×{0} DMG", damageMul.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), "", UpgradeIcon, x, z);

        /// <summary>Height above the road at <paramref name="age"/> seconds.</summary>
        public static float Height(float age) => StartHeight + (RiseMetres * Ease(Clamp01(age / LifeSeconds)));

        /// <summary>Opacity at <paramref name="age"/>: a quick pop in, held, then faded out over the last ~40%.</summary>
        public static float Alpha(float age)
        {
            if (age < 0f || age >= LifeSeconds)
            {
                return 0f;
            }

            var u = age / LifeSeconds;
            return u < 0.6f ? 1f : 1f - ((u - 0.6f) / 0.4f);
        }

        /// <summary>Scale at <paramref name="age"/>: a short pop from 1.35x to 1x over the first 0.12 s.</summary>
        public static float Scale(float age) => 1f + (0.35f * Math.Max(0f, 1f - (age / 0.12f)));

        /// <summary>Whether a runner event still shows the big screen banner: only the boss warnings do.</summary>
        public static bool UsesBanner(RunnerBannerCause cause) =>
            cause == RunnerBannerCause.BossIncoming || cause == RunnerBannerCause.BossQuake;

        public const string WeaponIcon = "theme_a.runner.crate_weapon";
        public const string SpeedIcon = "theme_a.runner.crate_speed";
        public const string HealIcon = "theme_a.ui.icon_upgrade";
        public const string HeroIcon = "theme_a.ui.icon_heroes";
        public const string UpgradeIcon = "theme_a.ui.icon_upgrade";

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        private static float Ease(float u) => 1f - ((1f - u) * (1f - u));
    }

    /// <summary>What may raise a runner notice (<see cref="RunnerPickupPopup.UsesBanner"/>).</summary>
    public enum RunnerBannerCause
    {
        BoxPickup,
        SquadFull,
        BossIncoming,
        BossQuake,
    }

    /// <summary>
    /// One pick-up pop-up: a string key / fallback with up to two arguments (<paramref name="UnitArg"/> says which
    /// argument, if any, is a unit id the scene shows by its hero name), an icon art key, and the world spot (the box).
    /// </summary>
    public sealed record RunnerPopupSpec(string Key, string Fallback, string Arg0, string Arg1, string IconKey, float X, float Z, int UnitArg = -1)
    {
        public float Life => RunnerPickupPopup.LifeSeconds;

        public bool ShowsBanner => false;
    }
}
