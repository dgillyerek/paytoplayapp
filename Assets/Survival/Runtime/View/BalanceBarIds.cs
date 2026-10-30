#nullable enable
using System.Collections.Generic;
using Survival.Domain.Ids;

namespace Survival.Domain.View
{
    /// <summary>Which balances each spend screen shows in its top balance bar, in pill order.</summary>
    public static class BalanceBarIds
    {
        /// <summary>Heroes / hero upgrade: Level Up spends gold + food, Rush spends gems or speed-ups, Tome spends tomes.</summary>
        public static readonly IReadOnlyList<string> Heroes = new[] { SurvIds.ResSoft, SurvIds.ResFood, SurvIds.ResHard, SurvIds.ResSpeedupM, SurvIds.ResHeroXp };

        /// <summary>Power Packs: what the packs grant (gems, tomes, war banners, speed-ups).</summary>
        public static readonly IReadOnlyList<string> PowerPacks = new[] { SurvIds.ResHard, SurvIds.ResHeroXp, SurvIds.ResRunnerBoost, SurvIds.ResSpeedupM };

        /// <summary>Recruit: gems (shards show on their own row).</summary>
        public static readonly IReadOnlyList<string> Recruit = new[] { SurvIds.ResHard };

        /// <summary>Building upgrade and Build sheets: the building costs plus gems for rushing.</summary>
        public static readonly IReadOnlyList<string> Building = new[] { SurvIds.ResSoft, SurvIds.ResWood, SurvIds.ResStone, SurvIds.ResFood, SurvIds.ResHard };

        /// <summary>Speed Up sheet: speed-ups and gems.</summary>
        public static readonly IReadOnlyList<string> SpeedUp = new[] { SurvIds.ResSpeedupM, SurvIds.ResHard };

        /// <summary>Amount text for one pill: speed-ups as time, everything else abbreviated (12.5K, 1.2M).</summary>
        public static string Format(string resourceId, int amount) =>
            resourceId == SurvIds.ResSpeedupM ? World.ChipWallet.FormatMinutesAbbrev(amount) : World.ChipWallet.FormatAbbrev(amount);
    }
}
