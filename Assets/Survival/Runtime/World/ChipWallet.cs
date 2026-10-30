#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using Survival.Domain.Economy;
using Survival.Domain.Ids;

namespace Survival.Domain.World
{
    /// <summary>
    /// HUD chip view over the <see cref="Wallet"/>. Keys are hud_layout slots (res_soft, res_stone, ...);
    /// each maps to a SURV-P0 res.* id (res.soft, res.stone, res.wood, res.food, res.hard).
    /// Theme A labels those chips in the pack (Gold / Wood / Stone / Food / Gems).
    /// </summary>
    public sealed class ChipWallet
    {
        public const string StoneChip = "res_stone";
        public const string SoftChip = "res_soft";
        public const string WoodChip = "res_wood";
        public const string FoodChip = "res_food";
        public const string HardChip = "res_hard";

        private static readonly Dictionary<string, string> ChipToResource = new(StringComparer.Ordinal)
        {
            [SoftChip] = SurvIds.ResSoft,
            [StoneChip] = SurvIds.ResStone,
            [WoodChip] = SurvIds.ResWood,
            [FoodChip] = SurvIds.ResFood,
            [HardChip] = SurvIds.ResHard
        };

        public ChipWallet(IReadOnlyDictionary<string, int> starting)
        {
            Wallet = new Wallet();
            foreach (var pair in starting)
            {
                Wallet.Add(ResourceFor(pair.Key), pair.Value);
            }
        }

        public ChipWallet(Wallet wallet) => Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));

        public Wallet Wallet { get; }

        /// <summary>hud chip key → res.* id. Unknown keys pass through unchanged.</summary>
        public static string ResourceFor(string chipKey) =>
            ChipToResource.TryGetValue(chipKey, out var id) ? id : chipKey;

        /// <summary>res.* id → hud chip key, or null when the resource has no HUD chip.</summary>
        public static string? ChipFor(string resourceId)
        {
            foreach (var pair in ChipToResource)
            {
                if (string.Equals(pair.Value, resourceId, StringComparison.Ordinal))
                {
                    return pair.Key;
                }
            }

            return null;
        }

        public int Get(string chipKey) => Wallet.Get(ResourceFor(chipKey));

        public int Add(string chipKey, int delta) => Wallet.Add(ResourceFor(chipKey), delta);

        public static string FormatCompact(int amount)
        {
            if (amount >= 10000)
            {
                var k = amount / 1000.0;
                return k.ToString("0.0", CultureInfo.InvariantCulture) + "K";
            }

            return FormatGrouped(amount);
        }

        /// <summary>Balance-bar amount: grouped under 10,000, then 12.5K / 250K / 1.2M (one decimal, trailing .0 dropped).</summary>
        public static string FormatAbbrev(int amount)
        {
            if (amount < 10000)
            {
                return FormatGrouped(amount);
            }

            if (amount < 1000000)
            {
                return Trim(Math.Floor(amount / 100.0) / 10.0) + "K";
            }

            return Trim(Math.Floor(amount / 100000.0) / 10.0) + "M";
        }

        /// <summary>Speed-up minutes for the balance bar: 45m, 2.5h, 3d.</summary>
        public static string FormatMinutesAbbrev(int minutes)
        {
            if (minutes < 60)
            {
                return minutes.ToString(CultureInfo.InvariantCulture) + "m";
            }

            if (minutes < 1440)
            {
                return Trim(Math.Floor(minutes / 6.0) / 10.0) + "h";
            }

            return Trim(Math.Floor(minutes / 144.0) / 10.0) + "d";
        }

        private static string Trim(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);

        public static string FormatGrouped(int amount) =>
            amount.ToString("N0", CultureInfo.InvariantCulture);
    }
}
