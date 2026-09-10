using System;
using System.Collections.Generic;
using System.IO;
using Grove.Domain.Serialization;

namespace Grove.Domain.Art
{
    /// <summary>One DES-001/002 PNG listed in <c>Assets/Grove/Art/Area1/manifest.json</c>.</summary>
    public sealed record ArtAsset(
        string Stub,
        string Filename,
        string Category,
        float PixelsPerUnit,
        float PivotX,
        float PivotY);

    /// <summary>
    /// Parses the Area 1 art pack manifest and maps catalog item ids → stub names.
    /// Unity loads the PNGs; headless tests only need the JSON + stub map.
    /// </summary>
    public sealed class ArtManifest
    {
        public const string RelativeRepoPath = "Assets/Grove/Art/Area1";
        public const string ManifestFileName = "manifest.json";

        private readonly Dictionary<string, ArtAsset> _byStub;

        public ArtManifest(string pack, string title, bool des001Ready, bool des002Ready, IReadOnlyList<ArtAsset> assets)
        {
            Pack = pack ?? "";
            Title = title ?? "";
            Des001Ready = des001Ready;
            Des002Ready = des002Ready;
            Assets = assets ?? throw new ArgumentNullException(nameof(assets));
            _byStub = new Dictionary<string, ArtAsset>(assets.Count, StringComparer.Ordinal);
            for (var i = 0; i < assets.Count; i++)
            {
                _byStub[assets[i].Stub] = assets[i];
            }
        }

        public string Pack { get; }

        public string Title { get; }

        public bool Des001Ready { get; }

        public bool Des002Ready { get; }

        public IReadOnlyList<ArtAsset> Assets { get; }

        public bool TryGet(string stub, out ArtAsset asset) => _byStub.TryGetValue(stub, out asset!);

        public static ArtManifest LoadDefault() => LoadFromDirectory(ResolveDirectory());

        public static ArtManifest LoadFromDirectory(string directory)
        {
            var path = Path.Combine(directory, ManifestFileName);
            return Parse(File.ReadAllText(path));
        }

        public static ArtManifest Parse(string json)
        {
            var root = JsonRead.Parse(json) as JsonNode.Obj
                       ?? throw new FormatException("Expected JSON object for art manifest.");
            var pack = OptString(root, "pack", "");
            var title = OptString(root, "title", "");
            var des001 = OptBool(root, "des001_ready", false);
            var des002 = OptBool(root, "des002_ready", false);
            if (!root.TryGet("assets", out var assetsNode) || assetsNode is not JsonNode.Arr list)
            {
                throw new FormatException("Expected JSON array 'assets'.");
            }

            var assets = new List<ArtAsset>(list.Items.Count);
            foreach (var node in list.Items)
            {
                var obj = node as JsonNode.Obj ?? throw new FormatException("Expected JSON object for art asset.");
                var stub = ReqString(obj, "stub");
                var filename = ReqString(obj, "filename");
                var category = OptString(obj, "category", "misc");
                var ppu = OptNumber(obj, "suggestedPPU", 100f);
                ReadPivot(obj, out var px, out var py);
                assets.Add(new ArtAsset(stub, filename, category, ppu, px, py));
            }

            return new ArtManifest(pack, title, des001, des002, assets);
        }

        /// <summary>Catalog item id → DES-001 stub. Unknown ids return false (no programmer-letter fallback).</summary>
        public static bool TryStubForItem(string itemId, out string stub)
        {
            stub = itemId switch
            {
                "wildflower_t1" => "WF_T01_Seed",
                "wildflower_t2" => "WF_T02_Sprout",
                "wildflower_t3" => "WF_T03_Bud",
                "wildflower_t4" => "WF_T04_Wildflower",
                "wildflower_t5" => "WF_T05_Bouquet",
                "wildflower_t6" => "WF_T06_FlowerBox",
                "herb_t1" => "HB_T01_HerbSprig",
                "herb_t2" => "HB_T02_HerbPot",
                "herb_t3" => "HB_T03_HerbBasket",
                "tool_t1" => "TL_T01_Twig",
                "tool_t2" => "TL_T02_Stick",
                "tool_t3" => "TL_T03_HandRake",
                "coin" => "FX_CoinPouch_Tiny",
                _ => ""
            };
            return stub.Length > 0;
        }

        public static string ResolveDirectory()
        {
            foreach (var candidate in Candidates())
            {
                if (File.Exists(Path.Combine(candidate, ManifestFileName)))
                {
                    return candidate;
                }
            }

            throw new FileNotFoundException(
                "Area 1 art manifest not found. Expected Assets/Grove/Art/Area1/manifest.json.");
        }

        private static IEnumerable<string> Candidates()
        {
            var baseDir = AppContext.BaseDirectory;
            yield return Path.Combine(baseDir, "Art");
            yield return Path.Combine(baseDir, "Area1");

            for (var dir = new DirectoryInfo(baseDir); dir != null; dir = dir.Parent)
            {
                yield return Path.Combine(dir.FullName, "Assets", "Grove", "Art", "Area1");
            }

            for (var dir = new DirectoryInfo(Environment.CurrentDirectory); dir != null; dir = dir.Parent)
            {
                yield return Path.Combine(dir.FullName, "Assets", "Grove", "Art", "Area1");
            }
        }

        private static void ReadPivot(JsonNode.Obj obj, out float x, out float y)
        {
            x = 0.5f;
            y = 0.5f;
            if (!obj.TryGet("pivot", out var node) || node is not JsonNode.Arr arr || arr.Items.Count < 2)
            {
                return;
            }

            if (arr.Items[0] is JsonNode.Num nx)
            {
                x = (float)nx.Value;
            }

            if (arr.Items[1] is JsonNode.Num ny)
            {
                y = (float)ny.Value;
            }
        }

        private static string ReqString(JsonNode.Obj obj, string key)
        {
            if (!obj.TryGet(key, out var node) || node is not JsonNode.Str str || string.IsNullOrWhiteSpace(str.Value))
            {
                throw new FormatException($"Expected non-empty string '{key}'.");
            }

            return str.Value;
        }

        private static string OptString(JsonNode.Obj obj, string key, string defaultValue)
        {
            if (!obj.TryGet(key, out var node) || node is JsonNode.Null)
            {
                return defaultValue;
            }

            return node is JsonNode.Str str ? str.Value : defaultValue;
        }

        private static bool OptBool(JsonNode.Obj obj, string key, bool defaultValue)
        {
            if (!obj.TryGet(key, out var node) || node is JsonNode.Null)
            {
                return defaultValue;
            }

            return node is JsonNode.Bool b ? b.Value : defaultValue;
        }

        private static float OptNumber(JsonNode.Obj obj, string key, float defaultValue)
        {
            if (!obj.TryGet(key, out var node) || node is JsonNode.Null)
            {
                return defaultValue;
            }

            return node is JsonNode.Num num ? (float)num.Value : defaultValue;
        }
    }
}
