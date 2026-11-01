#nullable enable
using System.Collections.Generic;
using Survival.Domain.Economy;
using Survival.Domain.Ids;

namespace Survival.Domain.Alliance
{
    /// <summary>What a bag item's Use button does (None: no button).</summary>
    public enum BagUse
    {
        None,
        SpeedUpTimer,
        OpenHeroes,
        OpenRoad,
    }

    /// <summary>One bag row: the wallet resource, its count and its use.</summary>
    public sealed record BagItem(string ResourceId, int Count, BagUse Use);

    /// <summary>
    /// The Bag screen (Derek 2026-10-31): the player's items from the save (the wallet), with a Use button where a use
    /// exists. Speed-ups go on the running timer that ends soonest; Hero Tomes open the Heroes screen and energy / runner boosts open the Road, where they are spent.
    /// Currencies (gold, gems, food...) live in the balance bar, not here.
    /// </summary>
    public static class BagContents
    {
        public static readonly string[] ItemIds =
        {
            SurvIds.ResSpeedupM, SurvIds.ResHeroXp, SurvIds.ResRunnerBoost, SurvIds.ResAllianceGift, SurvIds.ResEnergy,
        };

        public static BagUse UseOf(string resourceId) => resourceId switch
        {
            SurvIds.ResSpeedupM => BagUse.SpeedUpTimer,
            SurvIds.ResHeroXp => BagUse.OpenHeroes,
            SurvIds.ResEnergy or SurvIds.ResRunnerBoost => BagUse.OpenRoad,
            _ => BagUse.None,
        };

        /// <summary>Every bag item with its count; owned ones first (in <see cref="ItemIds"/> order), then the empty ones.</summary>
        public static IReadOnlyList<BagItem> From(Wallet wallet)
        {
            var owned = new List<BagItem>();
            var empty = new List<BagItem>();
            foreach (var id in ItemIds)
            {
                var n = wallet.Get(id);
                (n > 0 ? owned : empty).Add(new BagItem(id, n, UseOf(id)));
            }

            owned.AddRange(empty);
            return owned;
        }

        /// <summary>A Use button shows when the item has a use and the player owns some (and, for speed-ups, something is running).</summary>
        public static bool CanUse(BagItem item, bool timerRunning) =>
            item.Count > 0 && item.Use switch
            {
                BagUse.SpeedUpTimer => timerRunning,
                BagUse.OpenHeroes or BagUse.OpenRoad => true,
                _ => false,
            };
    }
}
