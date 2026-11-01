#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Survival.Domain.Serialization;

namespace Survival.Domain.Taps
{
    /// <summary>Things the player does that tap features (Crystal Orb, Bounty Board) count toward.</summary>
    public static class ActivityKinds
    {
        public const string RunnerKills = "runner_kills";
        public const string RunnerWin = "runner_win";
        public const string Harvest = "harvest";
        public const string Upgrade = "upgrade";
        public const string HeroLevel = "hero_level";

        public static readonly IReadOnlyList<string> All = new[] { RunnerKills, RunnerWin, Harvest, Upgrade, HeroLevel };
    }

    /// <summary>
    /// One place the game reports player activity (runner kills and wins, harvest taps, building upgrades,
    /// hero level-ups). Features listen to <see cref="Reported"/>; nothing here changes the economy.
    /// </summary>
    public static class ActivityFeed
    {
        public static event Action<string, int>? Reported;

        public static void Report(string kind, int amount)
        {
            if (amount > 0 && !string.IsNullOrEmpty(kind))
            {
                Reported?.Invoke(kind, amount);
            }
        }

        /// <summary>Tests: drop every listener.</summary>
        public static void ResetForTests() => Reported = null;
    }

    /// <summary>One weighted reward line: Resource amount Min..Max.</summary>
    public sealed record TapRewardDef(string Resource, int Min, int Max, int Weight);

    /// <summary>Small shared helpers for the tap features: weighted reward rolls, UTC day, save JSON.</summary>
    public static class TapKit
    {
        public const double DaySeconds = 86400.0;

        public static long DayOf(double now) => (long)Math.Floor(now / DaySeconds);

        /// <summary>Roll <paramref name="count"/> distinct-resource rewards from the pool (merged into one bag).</summary>
        public static Dictionary<string, int> Roll(IReadOnlyList<TapRewardDef> pool, int count, Random rng)
        {
            var bag = new Dictionary<string, int>(StringComparer.Ordinal);
            var left = new List<TapRewardDef>(pool);
            for (var i = 0; i < count && left.Count > 0; i++)
            {
                var total = 0;
                foreach (var d in left)
                {
                    total += Math.Max(0, d.Weight);
                }

                if (total <= 0)
                {
                    break;
                }

                var pick = rng.Next(total);
                var idx = 0;
                for (; idx < left.Count; idx++)
                {
                    pick -= Math.Max(0, left[idx].Weight);
                    if (pick < 0)
                    {
                        break;
                    }
                }

                idx = Math.Min(idx, left.Count - 1);
                var def = left[idx];
                left.RemoveAt(idx);
                var amount = def.Max <= def.Min ? def.Min : rng.Next(def.Min, def.Max + 1);
                bag[def.Resource] = (bag.TryGetValue(def.Resource, out var have) ? have : 0) + amount;
            }

            return bag;
        }

        public static int Pick(IReadOnlyList<int> weights, Random rng)
        {
            var total = 0;
            foreach (var w in weights)
            {
                total += Math.Max(0, w);
            }

            if (total <= 0)
            {
                return 0;
            }

            var pick = rng.Next(total);
            for (var i = 0; i < weights.Count; i++)
            {
                pick -= Math.Max(0, weights[i]);
                if (pick < 0)
                {
                    return i;
                }
            }

            return weights.Count - 1;
        }

        public static StringBuilder Str(StringBuilder sb, string? s)
        {
            if (s == null)
            {
                return sb.Append("null");
            }

            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    default:
                        if (c < 0x20)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            return sb.Append('"');
        }

        public static StringBuilder Num(StringBuilder sb, double v) => sb.Append(v.ToString("R", CultureInfo.InvariantCulture));

        public static StringBuilder Bag(StringBuilder sb, IReadOnlyDictionary<string, int>? bag)
        {
            sb.Append('{');
            var first = true;
            if (bag != null)
            {
                foreach (var kv in bag)
                {
                    if (!first)
                    {
                        sb.Append(',');
                    }

                    first = false;
                    Str(sb, kv.Key).Append(':').Append(kv.Value.ToString(CultureInfo.InvariantCulture));
                }
            }

            return sb.Append('}');
        }

        internal static JsonNode.Obj? ParseObj(string? json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                return JsonRead.Parse(json!) as JsonNode.Obj;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        internal static double D(JsonNode.Obj o, string key, double fallback = 0) =>
            o.TryGet(key, out var n) && n is JsonNode.Num num ? num.Value : fallback;

        internal static int I(JsonNode.Obj o, string key, int fallback = 0) =>
            o.TryGet(key, out var n) && n is JsonNode.Num num ? (int)Math.Round(num.Value) : fallback;

        internal static string? S(JsonNode.Obj o, string key) =>
            o.TryGet(key, out var n) && n is JsonNode.Str s ? s.Value : null;

        internal static bool B(JsonNode.Obj o, string key) =>
            o.TryGet(key, out var n) && n is JsonNode.Bool b && b.Value;

        internal static Dictionary<string, int> BagOf(JsonNode.Obj o, string key)
        {
            var bag = new Dictionary<string, int>(StringComparer.Ordinal);
            if (o.TryGet(key, out var n) && n is JsonNode.Obj obj)
            {
                foreach (var kv in obj.Fields)
                {
                    if (kv.Value is JsonNode.Num num)
                    {
                        bag[kv.Key] = (int)Math.Round(num.Value);
                    }
                }
            }

            return bag;
        }

        internal static IEnumerable<JsonNode.Obj> Items(JsonNode.Obj o, string key)
        {
            if (o.TryGet(key, out var n) && n is JsonNode.Arr arr)
            {
                foreach (var item in arr.Items)
                {
                    if (item is JsonNode.Obj obj)
                    {
                        yield return obj;
                    }
                }
            }
        }
    }
}
