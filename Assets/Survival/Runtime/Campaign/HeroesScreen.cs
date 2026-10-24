#nullable enable

namespace Survival.Domain.Campaign
{
    /// <summary>
    /// The Heroes upgrade screen's banner and tabs (Derek 2026-10-23: the banner still said "HEROES" on the Foes tab).
    /// The banner title follows the selected tab on open and on every switch, with the same banner art and font; the
    /// selected tab is the gold one, the other blue, whichever it is.
    /// </summary>
    public static class HeroesScreen
    {
        public const string HeroesTitleKey = "campaign.heroes.title";
        public const string FoesTitleKey = "campaign.foes.title";

        /// <summary>String key of the banner title for the tab shown.</summary>
        public static string TitleKey(bool foesTab) => foesTab ? FoesTitleKey : HeroesTitleKey;

        /// <summary>Banner title when the theme pack has no string for <see cref="TitleKey"/>.</summary>
        public static string TitleFallback(bool foesTab) => foesTab ? "FOES" : "HEROES";

        /// <summary>True when the tab (<paramref name="tabIsFoes"/>) is the selected one and so drawn highlighted.</summary>
        public static bool TabHighlighted(bool tabIsFoes, bool foesTab) => tabIsFoes == foesTab;
    }
}
