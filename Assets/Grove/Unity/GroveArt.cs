using System;
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
        public const string StubTeachRing = "UI_TeachRing";
        public const string StubHudDock = "UI_OrderTray_Dock";
        public const string StubMayaBubble = "UI_Maya_Bubble";

        /// <summary>DES-003/004 drop folder (optional). Bottom dock + teach rings overlay the pack.</summary>
        public const string DesignDropRelative = "design/unity-drop";

        private static Dictionary<string, Sprite>? _byStub;
        private static Sprite? _white;

        public static bool Ready { get; private set; }

        public static int LoadedCount { get; private set; }

        /// <summary>Backdrop + wood tray + cream cells must all load or Derek sees a bare grid.</summary>
        public static bool HasBoardComposite =>
            Get(StubBackdrop) != null && Get(StubBoardSurface) != null && Get(StubCellEmpty) != null;

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

            OverlayDesignDrop();
        }

        public static Sprite? Get(string stub)
        {
            EnsureLoaded();
            return _byStub != null && _byStub.TryGetValue(stub, out var sprite) ? sprite : null;
        }

        public static Sprite Require(string stub) => Get(stub) ?? WhiteSprite();

        public static Sprite? TeachRingSprite =>
            Get(StubTeachRing) ?? Get(StubCellHighlight);

        public static Sprite? HudDockSprite =>
            Get(StubHudDock) ?? Get(StubOrderCard);

        public static Sprite? MayaBubbleSprite =>
            Get(StubMayaBubble) ?? Get(StubOrderCard);

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

        private static void OverlayDesignDrop()
        {
            if (_byStub == null)
            {
                return;
            }

            foreach (var dir in DesignDropDirectories())
            {
                if (!Directory.Exists(dir))
                {
                    continue;
                }

                string[] files;
                try
                {
                    files = Directory.GetFiles(dir, "*.png", SearchOption.AllDirectories);
                }
                catch (System.Exception)
                {
                    continue;
                }

                for (var i = 0; i < files.Length; i++)
                {
                    var stub = StubForDropFile(Path.GetFileNameWithoutExtension(files[i]));
                    if (stub == null)
                    {
                        continue;
                    }

                    var asset = new ArtAsset(stub, Path.GetFileName(files[i]), "hud", 100f, 0.5f, 0.5f);
                    var sprite = LoadPng(files[i], asset);
                    if (sprite != null)
                    {
                        _byStub[stub] = sprite;
                    }
                }
            }
        }

        private static IEnumerable<string> DesignDropDirectories()
        {
            yield return Path.Combine("/workspace", "design", "unity-drop");
            var data = Application.dataPath;
            if (!string.IsNullOrEmpty(data))
            {
                yield return Path.GetFullPath(Path.Combine(data, "..", DesignDropRelative));
            }

            yield return Path.Combine(Environment.CurrentDirectory, DesignDropRelative);
        }

        private static string? StubForDropFile(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            var n = name.Replace('-', '_');
            if (ContainsInsensitive(n, "TeachRing") || ContainsInsensitive(n, "DES_004") ||
                ContainsInsensitive(n, "Ring_Teach") || ContainsInsensitive(n, "Teach_Ring"))
            {
                return StubTeachRing;
            }

            if (ContainsInsensitive(n, "Bubble") || ContainsInsensitive(n, "Maya_Speech"))
            {
                return StubMayaBubble;
            }

            if (ContainsInsensitive(n, "Dock") || ContainsInsensitive(n, "DES_003") ||
                ContainsInsensitive(n, "SideRail") || ContainsInsensitive(n, "Side_Rail") ||
                ContainsInsensitive(n, "OrderTray_Dock"))
            {
                return StubHudDock;
            }

            return null;
        }

        private static bool ContainsInsensitive(string haystack, string needle) =>
            haystack.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
