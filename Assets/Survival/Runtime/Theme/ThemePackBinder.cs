using System;
using System.Collections.Generic;
using Survival.Domain.Ids;
using Survival.Domain.Layout;
using Survival.Domain.Serialization;

namespace Survival.Domain.Theme
{
    public sealed record ThemeArtBinding(string ContentKey, string RelativePath);

    public sealed record WorldPin(
        string NodeId,
        string NodeTypeId,
        string ContentKey,
        float PinX,
        float PinYFromTop,
        float SpriteSize);

    /// <summary>
    /// Binds theme_a.* content keys to strings and art paths. Systems keep using stable IDs.
    /// Packed at build time — not a player-facing theme switcher.
    /// </summary>
    public sealed class ThemePackBinder
    {
        public ThemePackBinder(
            string themeId,
            string packId,
            string systemsApi,
            string displayName,
            IReadOnlyDictionary<string, string> strings,
            IReadOnlyDictionary<string, string> art,
            IReadOnlyList<WorldPin> pins,
            IReadOnlyDictionary<string, HudRect> hud)
        {
            if (!string.Equals(systemsApi, SurvIds.SystemsApi, StringComparison.Ordinal))
            {
                throw new FormatException($"ThemePack {SurvIds.PackSystemsApi} must be {SurvIds.SystemsApi}.");
            }

            ThemeId = themeId ?? throw new ArgumentNullException(nameof(themeId));
            PackId = packId ?? throw new ArgumentNullException(nameof(packId));
            SystemsApi = systemsApi;
            DisplayName = displayName ?? "";
            Strings = strings ?? throw new ArgumentNullException(nameof(strings));
            Art = art ?? throw new ArgumentNullException(nameof(art));
            Pins = pins ?? throw new ArgumentNullException(nameof(pins));
            Hud = hud ?? throw new ArgumentNullException(nameof(hud));
        }

        public string ThemeId { get; }
        public string PackId { get; }
        public string SystemsApi { get; }
        public string DisplayName { get; }
        public IReadOnlyDictionary<string, string> Strings { get; }
        public IReadOnlyDictionary<string, string> Art { get; }
        public IReadOnlyList<WorldPin> Pins { get; }
        public IReadOnlyDictionary<string, HudRect> Hud { get; }

        public string StringOr(string key, string fallback) =>
            Strings.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value) ? value : fallback;

        public bool TryArt(string contentKey, out string relativePath) => Art.TryGetValue(contentKey, out relativePath!);

        public static ThemePackBinder Parse(string packJson, string stringsJson, string pinsJson, string hudJson)
        {
            var pack = RequireObj(JsonRead.Parse(packJson), "pack");
            var themeId = ReqString(pack, "theme_id");
            var packId = pack.TryGet("id", out var idNode) && idNode is JsonNode.Str idStr ? idStr.Value : themeId;
            var systemsApi = ReqString(pack, "systems_api");
            var displayName = pack.TryGet("display_name", out var dn) && dn is JsonNode.Str dns ? dns.Value : "";
            var strings = ReadStringMap(stringsJson);
            var art = ReadArtMap(pack);
            var pins = ReadPins(pinsJson);
            var hud = ReadHud(hudJson);
            return new ThemePackBinder(themeId, packId, systemsApi, displayName, strings, art, pins, hud);
        }

        private static IReadOnlyDictionary<string, string> ReadStringMap(string json)
        {
            var root = RequireObj(JsonRead.Parse(json), "strings");
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in root.Fields)
            {
                if (pair.Value is JsonNode.Str str)
                {
                    map[pair.Key] = str.Value;
                }
            }

            return map;
        }

        private static IReadOnlyDictionary<string, string> ReadArtMap(JsonNode.Obj pack)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!pack.TryGet("art", out var artNode) || artNode is not JsonNode.Obj art)
            {
                return map;
            }

            foreach (var pair in art.Fields)
            {
                if (pair.Value is JsonNode.Str str)
                {
                    map[pair.Key] = str.Value;
                }
            }

            return map;
        }

        private static IReadOnlyList<WorldPin> ReadPins(string json)
        {
            var root = RequireObj(JsonRead.Parse(json), "pins");
            var arr = RequireArr(root, "pins");
            var list = new List<WorldPin>(arr.Items.Count);
            foreach (var item in arr.Items)
            {
                var obj = RequireObj(item, "pin");
                list.Add(new WorldPin(
                    ReqString(obj, "node_id"),
                    ReqString(obj, "node_type"),
                    ReqString(obj, "content_key"),
                    ReqFloat(obj, "pin_x"),
                    ReqFloat(obj, "pin_y_from_top"),
                    obj.TryGet("sprite_size", out var sizeNode) && sizeNode is JsonNode.Num sizeNum
                        ? (float)sizeNum.Value
                        : 0.18f));
            }

            return list;
        }

        private static IReadOnlyDictionary<string, HudRect> ReadHud(string json)
        {
            var root = RequireObj(JsonRead.Parse(json), "hud");
            var map = new Dictionary<string, HudRect>(StringComparer.Ordinal);
            foreach (var pair in root.Fields)
            {
                if (pair.Value is not JsonNode.Obj box)
                {
                    continue;
                }

                map[pair.Key] = HudRect.FromDesign(
                    ReqFloat(box, "x_min"),
                    ReqFloat(box, "y_top_min"),
                    ReqFloat(box, "x_max"),
                    ReqFloat(box, "y_top_max"));
            }

            return map;
        }

        private static JsonNode.Obj RequireObj(JsonNode node, string name) =>
            node as JsonNode.Obj ?? throw new FormatException($"Expected object for {name}.");

        private static JsonNode.Arr RequireArr(JsonNode.Obj root, string key)
        {
            if (!root.TryGet(key, out var node) || node is not JsonNode.Arr arr)
            {
                throw new FormatException($"Expected array '{key}'.");
            }

            return arr;
        }

        private static string ReqString(JsonNode.Obj obj, string key)
        {
            if (!obj.TryGet(key, out var node) || node is not JsonNode.Str str)
            {
                throw new FormatException($"Expected string '{key}'.");
            }

            return str.Value;
        }

        private static float ReqFloat(JsonNode.Obj obj, string key)
        {
            if (!obj.TryGet(key, out var node) || node is not JsonNode.Num num)
            {
                throw new FormatException($"Expected number '{key}'.");
            }

            return (float)num.Value;
        }
    }
}
