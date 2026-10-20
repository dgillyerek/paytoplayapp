#nullable enable
using System;
using System.Collections.Generic;

namespace Survival.Domain.Serialization
{
    /// <summary>Lenient readers for ThemePack layout files (map/world_layout.json, map/village_layout.json).</summary>
    internal static class LayoutJson
    {
        public static JsonNode.Obj Root(string json, string what) =>
            JsonRead.Parse(json) as JsonNode.Obj ?? throw new FormatException(what + ": expected a JSON object.");

        public static JsonNode.Obj Obj(JsonNode.Obj o, string key) =>
            o.TryGet(key, out var n) && n is JsonNode.Obj obj ? obj : throw new FormatException($"Expected object '{key}'.");

        public static IEnumerable<JsonNode.Obj> Objs(JsonNode.Obj o, string key)
        {
            if (!o.TryGet(key, out var n) || n is not JsonNode.Arr arr)
            {
                yield break;
            }

            foreach (var item in arr.Items)
            {
                if (item is JsonNode.Obj obj)
                {
                    yield return obj;
                }
            }
        }

        public static string Str(JsonNode.Obj o, string key) =>
            o.TryGet(key, out var n) && n is JsonNode.Str s && !string.IsNullOrWhiteSpace(s.Value)
                ? s.Value
                : throw new FormatException($"Expected string '{key}'.");

        public static string OptStr(JsonNode.Obj o, string key, string fallback) =>
            o.TryGet(key, out var n) && n is JsonNode.Str s && !string.IsNullOrWhiteSpace(s.Value) ? s.Value : fallback;

        public static float Num(JsonNode.Obj o, string key) =>
            o.TryGet(key, out var n) && n is JsonNode.Num num ? (float)num.Value : throw new FormatException($"Expected number '{key}'.");

        public static float OptNum(JsonNode.Obj o, string key, float fallback) =>
            o.TryGet(key, out var n) && n is JsonNode.Num num ? (float)num.Value : fallback;

        public static int OptInt(JsonNode.Obj o, string key, int fallback) =>
            o.TryGet(key, out var n) && n is JsonNode.Num num ? (int)Math.Round(num.Value) : fallback;

        public static bool OptBool(JsonNode.Obj o, string key, bool fallback) =>
            o.TryGet(key, out var n) && n is JsonNode.Bool b ? b.Value : fallback;

        /// <summary>Normalized 0..1 coordinate check (layout files author positions as fractions of the map).</summary>
        public static float Frac(JsonNode.Obj o, string key)
        {
            var v = Num(o, key);
            if (v < 0f || v > 1f)
            {
                throw new FormatException($"'{key}' must be in 0..1, got {v}.");
            }

            return v;
        }
    }
}
