using System;
using Survival.Domain.Ids;

namespace Survival.Domain.Flavor
{
    /// <summary>Build-time app identity. One flavor per store binary; pack is baked, not chosen in-game.</summary>
    public sealed record AppFlavorConfig(
        string FlavorId,
        string ThemeId,
        string SystemsApi,
        string PackPath,
        string BundleId,
        string AppName,
        string IconPath,
        string SplashPath)
    {
        public static AppFlavorConfig FantasyKingdomA { get; } = new(
            SurvIds.FlavorIdFantasyKingdomA,
            SurvIds.ThemeIdFantasyKingdomA,
            SurvIds.SystemsApi,
            "ThemePack/fantasy_kingdom_a",
            "com.paytoplay.fantasykingdoma",
            "Kingdom Defense",
            "art/store/flavor_icon.png",
            "art/splash/THEME_A_SPLASH_KEEP_1080x1920.png");

        public void Validate()
        {
            if (!string.Equals(SystemsApi, SurvIds.SystemsApi, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"{SurvIds.PackSystemsApi} must equal {SurvIds.SystemsApi}.");
            }

            if (string.IsNullOrWhiteSpace(PackPath))
            {
                throw new InvalidOperationException($"{SurvIds.PackPath} is required.");
            }
        }
    }
}
