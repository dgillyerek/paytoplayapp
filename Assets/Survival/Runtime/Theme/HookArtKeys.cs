using System;
using System.Collections.Generic;

namespace Survival.Domain.Theme
{
    /// <summary>
    /// Theme content keys for the hook slice UI kit. Systems ask for a key; the ThemePack (pack.json "art")
    /// decides which PNG it is, so the kit stays white-label. 9-slice borders come from pack.json "art_slices".
    /// </summary>
    public static class HookArtKeys
    {
        public const string Prefix = "theme_a.hook.";

        public const string Panel = "theme_a.hook.ui.panel";
        public const string Plate = "theme_a.hook.ui.plate";
        public const string ButtonGold = "theme_a.hook.ui.button_gold";
        public const string ButtonGreen = "theme_a.hook.ui.button_green";
        public const string ButtonBlue = "theme_a.hook.ui.button_blue";
        public const string ButtonRed = "theme_a.hook.ui.button_red";
        public const string Chip = "theme_a.hook.ui.chip";
        public const string BarFrame = "theme_a.hook.ui.bar_frame";
        public const string BarFillGreen = "theme_a.hook.ui.bar_fill_green";
        public const string BarFillGold = "theme_a.hook.ui.bar_fill_gold";
        public const string BarFillBlue = "theme_a.hook.ui.bar_fill_blue";
        public const string Scroll = "theme_a.hook.ui.scroll";
        public const string RibbonRed = "theme_a.hook.ui.ribbon_red";
        public const string RibbonBlue = "theme_a.hook.ui.ribbon_blue";
        public const string BadgeFree = "theme_a.hook.ui.badge_free";
        public const string Header = "theme_a.hook.ui.header";

        public const string ChestClosed = "theme_a.hook.icon.chest_closed";
        public const string ChestOpen = "theme_a.hook.icon.chest_open";
        public const string Gift = "theme_a.hook.icon.gift";
        public const string GemsTier1 = "theme_a.hook.icon.gems_tier_1";
        public const string GemsTier2 = "theme_a.hook.icon.gems_tier_2";
        public const string GemsTier3 = "theme_a.hook.icon.gems_tier_3";

        public const string FxRays = "theme_a.hook.fx.rays";
        public const string FxGlow = "theme_a.hook.fx.glow";
        public const string FxSparkle = "theme_a.hook.fx.sparkle";
        public const string FxBurstRing = "theme_a.hook.fx.burst_ring";
        public const string FxConfetti = "theme_a.hook.fx.confetti";

        /// <summary>Resource icon key, e.g. res.soft -> theme_a.hook.icon.res.soft.</summary>
        public static string ForResource(string resourceId) => Prefix + "icon." + resourceId;

        /// <summary>Building art for a base plot, e.g. base.plot.farm_01 -> theme_a.hook.plot.farm_01.</summary>
        public static string ForPlot(string plotId)
        {
            const string plotPrefix = "base.plot.";
            var tail = plotId.StartsWith(plotPrefix, StringComparison.Ordinal) ? plotId.Substring(plotPrefix.Length) : plotId;
            return Prefix + "plot." + tail;
        }

        /// <summary>Gem Shop tier art by offer position (0 = smallest).</summary>
        public static string ForGemTier(int index) => index <= 0 ? GemsTier1 : index == 1 ? GemsTier2 : GemsTier3;

        public static IReadOnlyList<string> Required { get; } = new[]
        {
            Panel, Plate, ButtonGold, ButtonGreen, ButtonBlue, ButtonRed, Chip, BarFrame, BarFillGreen, BarFillGold,
            BarFillBlue, Scroll, RibbonRed, RibbonBlue, BadgeFree, Header, ChestClosed, ChestOpen, Gift, GemsTier1,
            GemsTier2, GemsTier3, FxRays, FxGlow, FxSparkle, FxBurstRing, FxConfetti
        };
    }
}
