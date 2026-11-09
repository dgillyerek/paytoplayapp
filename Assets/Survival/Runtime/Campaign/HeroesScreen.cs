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

        /// <summary>
        /// How much of the screen to redraw (Derek 2026-11-02: tapping a hero blinked the whole screen). The shell
        /// (panel, banner, balances, buttons) is built only when the screen is not already open; a tab switch redraws
        /// the tabs, cards and detail; anything else (a hero tap, an unlock) only swaps the detail pane.
        /// </summary>
        public static HeroesRebuild Plan(bool screenOpen, bool shownFoes, bool foesTab)
        {
            if (!screenOpen)
            {
                return HeroesRebuild.Shell;
            }

            return shownFoes != foesTab ? HeroesRebuild.Body : HeroesRebuild.DetailOnly;
        }

        /// <summary>
        /// The star row is redrawn only when the shown count changes (Derek 2026-11-03: unearned stars flashed while
        /// the screen's 0.25 s refresh kept touching them). <paramref name="shown"/> is -1 before the first draw.
        /// </summary>
        public static bool StarsNeedRedraw(int shown, int count) => shown != count;
    }

    public enum HeroesRebuild
    {
        /// <summary>Open the screen from scratch.</summary>
        Shell,
        /// <summary>Keep the shell; redraw tabs, cards and detail.</summary>
        Body,
        /// <summary>Keep everything; move the glow and swap the detail pane.</summary>
        DetailOnly,
    }
}
