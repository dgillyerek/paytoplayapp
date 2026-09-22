using System;
using System.Collections.Generic;
using System.Globalization;

namespace Survival.Domain.World
{
    /// <summary>
    /// HUD chip amounts. Keys are layout slots (e.g. hud_layout res_stone), not new SURV-P0 resource IDs.
    /// Theme A labels those chips in the pack (Gold / Wood / Stone / Food).
    /// </summary>
    public sealed class ChipWallet
    {
        public const string StoneChip = "res_stone";
        public const string SoftChip = "res_soft";

        private readonly Dictionary<string, int> _amounts;

        public ChipWallet(IReadOnlyDictionary<string, int> starting)
        {
            _amounts = new Dictionary<string, int>(starting, StringComparer.Ordinal);
        }

        public int Get(string chipKey) => _amounts.TryGetValue(chipKey, out var v) ? v : 0;

        public int Add(string chipKey, int delta)
        {
            var next = Get(chipKey) + delta;
            if (next < 0)
            {
                next = 0;
            }

            _amounts[chipKey] = next;
            return next;
        }

        public static string FormatCompact(int amount)
        {
            if (amount >= 10000)
            {
                var k = amount / 1000.0;
                return k.ToString("0.0", CultureInfo.InvariantCulture) + "K";
            }

            return FormatGrouped(amount);
        }

        public static string FormatGrouped(int amount) =>
            amount.ToString("N0", CultureInfo.InvariantCulture);
    }
}
