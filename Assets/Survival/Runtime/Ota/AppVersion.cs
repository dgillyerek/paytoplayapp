#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Survival.Domain.Ota
{
    /// <summary>Dotted numeric app version ("0.1.42"), compared segment by segment; a missing segment counts as 0.</summary>
    public sealed class AppVersion : IComparable<AppVersion>
    {
        private readonly int[] _parts;

        private AppVersion(int[] parts, string text)
        {
            _parts = parts;
            Text = text;
        }

        public string Text { get; }

        /// <summary>Parses "1", "1.2", "1.2.3", "1.2.3.4"; a "-suffix" or "+build" tail is ignored. False on anything else.</summary>
        public static bool TryParse(string? text, out AppVersion version)
        {
            version = null!;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var core = text!.Trim();
            var cut = core.IndexOfAny(new[] { '-', '+', ' ' });
            if (cut >= 0)
            {
                core = core.Substring(0, cut);
            }

            var bits = core.Split('.');
            if (bits.Length == 0 || bits.Length > 4)
            {
                return false;
            }

            var parts = new List<int>(bits.Length);
            foreach (var bit in bits)
            {
                if (bit.Length == 0 || bit.Length > 9 || !int.TryParse(bit, NumberStyles.None, CultureInfo.InvariantCulture, out var n))
                {
                    return false;
                }

                parts.Add(n);
            }

            version = new AppVersion(parts.ToArray(), text.Trim());
            return true;
        }

        public static AppVersion Parse(string text) =>
            TryParse(text, out var v) ? v : throw new FormatException("Not a dotted numeric version: '" + text + "'.");

        public int CompareTo(AppVersion? other)
        {
            if (other is null)
            {
                return 1;
            }

            var n = Math.Max(_parts.Length, other._parts.Length);
            for (var i = 0; i < n; i++)
            {
                var a = i < _parts.Length ? _parts[i] : 0;
                var b = i < other._parts.Length ? other._parts[i] : 0;
                if (a != b)
                {
                    return a < b ? -1 : 1;
                }
            }

            return 0;
        }

        public override string ToString() => Text;
    }
}
