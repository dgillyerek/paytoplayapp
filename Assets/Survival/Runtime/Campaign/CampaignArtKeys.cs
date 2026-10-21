#nullable enable
using System.Collections.Generic;

namespace Survival.Domain.Campaign
{
    /// <summary>Theme A pack.json art keys for the Heroes and Battle Setup screens (each has a code-drawn fallback).</summary>
    public static class CampaignArtKeys
    {
        public const string IconBattle = "theme_a.ui.icon_battle";
        public const string IconShard = "theme_a.ui.icon_shard";
        public const string IconHeroes = "theme_a.ui.icon_heroes";

        /// <summary>Hero frame blue (#1b3060, Sir Aldric's blue) and the villain purple.</summary>
        public static readonly float[] HeroFrameRgb = { 0x1b / 255f, 0x30 / 255f, 0x60 / 255f };
        public static readonly float[] VillainFrameRgb = { 0x4a / 255f, 0x1f / 255f, 0x6e / 255f };

        public static IEnumerable<string> All(CampaignCatalog catalog)
        {
            yield return IconBattle;
            yield return IconShard;
            yield return IconHeroes;
            foreach (var h in catalog.Heroes)
            {
                yield return h.PortraitKey;
            }
        }
    }
}
