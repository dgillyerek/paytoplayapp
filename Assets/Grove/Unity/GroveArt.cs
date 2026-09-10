using System.Collections.Generic;
using System.IO;
using Grove.Domain.Art;
using UnityEngine;

namespace Grove.Unity
{
    /// <summary>
    /// Loads DES-001/002 PNGs from <c>Assets/Grove/Art/Area1/</c> via the manifest.
    /// Runtime <see cref="Texture2D.LoadImage"/> so Play Mode shows production art even
    /// before Unity finishes sprite import. PPU 100, center pivot.
    /// </summary>
    internal static class GroveArt
    {
        public const string StubBackdrop = "ENV_FG_Backdrop";
        public const string StubBoardSurface = "ENV_FG_BoardSurface";
        public const string StubCellEmpty = "ENV_FG_CellEmpty";
        public const string StubCellHighlight = "ENV_FG_CellHighlight";
        public const string StubCrateCharged = "ENV_FG_GardenCrate_Charged";
        public const string StubCrateEmpty = "ENV_FG_GardenCrate_Empty";
        public const string StubCrateIdle = "ENV_FG_GardenCrate_Idle";
        public const string StubMayaNeutral = "MAYA_Portrait_Neutral";
        public const string StubMayaHappy = "MAYA_Portrait_Happy";
        public const string StubEnergyPill = "HUD_EnergyPill";
        public const string StubCoin = "HUD_Wallet_Coin";
        public const string StubGem = "HUD_Wallet_Gem";
        public const string StubOrderCard = "UI_OrderTray_Card";
        public const string StubButton = "UI_Btn_Primary";
        public const string StubEnergyEmpty = "UI_Modal_EnergyEmpty";
        public const string StubMergeSparkle = "VFX_MergeSparkle";

        private static Dictionary<string, Sprite>? _byStub;
        private static Sprite? _white;

        public static bool Ready { get; private set; }

        public static int LoadedCount { get; private set; }

        public static void EnsureLoaded()
        {
            if (_byStub != null)
            {
                return;
            }

            _byStub = new Dictionary<string, Sprite>(64, System.StringComparer.Ordinal);
            try
            {
                var dir = ResolveArtDirectory();
                var manifest = ArtManifest.LoadFromDirectory(dir);
                for (var i = 0; i < manifest.Assets.Count; i++)
                {
                    var asset = manifest.Assets[i];
                    var path = Path.Combine(dir, asset.Filename.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(path))
                    {
                        Debug.LogWarning("Grove art missing: " + path);
                        continue;
                    }

                    var sprite = LoadPng(path, asset);
                    if (sprite != null)
                    {
                        _byStub[asset.Stub] = sprite;
                    }
                }

                LoadedCount = _byStub.Count;
                Ready = LoadedCount > 0;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Grove art pack failed to load: " + ex.Message);
                Ready = false;
            }
        }

        public static Sprite? Get(string stub)
        {
            EnsureLoaded();
            return _byStub != null && _byStub.TryGetValue(stub, out var sprite) ? sprite : null;
        }

        public static Sprite Require(string stub) => Get(stub) ?? WhiteSprite();

        public static Sprite SpriteForItem(string itemId)
        {
            if (ArtManifest.TryStubForItem(itemId, out var stub))
            {
                var sprite = Get(stub);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return Get(StubCellEmpty) ?? WhiteSprite();
        }

        public static Sprite WhiteSprite()
        {
            if (_white != null)
            {
                return _white;
            }

            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var pixels = new Color[64];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.name = "grove_ui_white";
            _white = Sprite.Create(tex, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 4f, 0, SpriteMeshType.FullRect);
            return _white;
        }

        private static string ResolveArtDirectory()
        {
            var data = Application.dataPath;
            if (!string.IsNullOrEmpty(data))
            {
                var fromData = Path.Combine(data, "Grove", "Art", "Area1");
                if (File.Exists(Path.Combine(fromData, ArtManifest.ManifestFileName)))
                {
                    return fromData;
                }
            }

            return ArtManifest.ResolveDirectory();
        }

        private static Sprite? LoadPng(string path, ArtAsset asset)
        {
            var bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes, markNonReadable: false))
            {
                Object.Destroy(tex);
                return null;
            }

            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.name = asset.Stub;
            var ppu = asset.PixelsPerUnit > 0.01f ? asset.PixelsPerUnit : 100f;
            var pivot = new Vector2(asset.PivotX, asset.PivotY);
            return Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                pivot,
                ppu,
                0,
                SpriteMeshType.FullRect);
        }
    }
}
