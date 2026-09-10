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
        public const string StubTeachRing = "UI_Teach_Ring";
        public const string StubTeachHand = "UI_Teach_Hand";
        public const string StubHudDock = "UI_OrderDock_Panel";
        public const string StubGoalPill = "UI_GoalPill";
        public const string StubInventorySlot = "UI_Inventory_Slot";
        public const string StubMayaBubble = "UI_Maya_Bubble";

        /// <summary>DES-003/004 drop folder (optional). Bottom dock + teach rings overlay the pack.</summary>
        public const string DesignDropRelative = "design/unity-drop";

        /// <summary>Programmer stubs (Charged crate, etc.) sit under this size. Idle crate is ~338KB.</summary>
        public const long TinyStubBytes = 20000;

        private static Dictionary<string, Sprite>? _byStub;
        private static HashSet<string>? _tinyStubs;
        private static Sprite? _white;
        private static Sprite? _glowRing;
        private static Sprite? _goalChip;

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
            _tinyStubs = new HashSet<string>(System.StringComparer.Ordinal);
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

                    if (new FileInfo(path).Length < TinyStubBytes)
                    {
                        _tinyStubs.Add(asset.Stub);
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

        /// <summary>
        /// Gold teach ring with a punched-out backdrop. Never falls back to CellHighlight
        /// (that photo is an opaque square and reads as a missing-texture overlay).
        /// </summary>
        public static Sprite? TeachRingSprite =>
            Get(StubTeachRing) ?? GlowRingSprite();

        public static Sprite? TeachHandSprite =>
            Get(StubTeachHand);

        /// <summary>5-slot inventory chrome. Do not stretch this over the order tray.</summary>
        public static Sprite? HudDockSprite =>
            Get(StubHudDock);

        /// <summary>
        /// Play crate: production Idle planter, not the tiny Charged/Empty stubs
        /// (black field + yellow halo that covered the left board).
        /// </summary>
        public static Sprite? PlayCrateSprite(bool hasCharges)
        {
            var idle = Get(StubCrateIdle);
            if (idle != null && IsTinyStub(StubCrateCharged) && IsTinyStub(StubCrateEmpty))
            {
                return idle;
            }

            if (!hasCharges)
            {
                return Get(StubCrateEmpty) ?? idle ?? Get(StubCrateCharged);
            }

            return Get(StubCrateCharged) ?? idle ?? Get(StubCrateEmpty);
        }

        public static bool IsTinyStub(string stub) =>
            _tinyStubs != null && _tinyStubs.Contains(stub);

        public static Sprite? GoalPillSprite =>
            GoalChipSprite() ?? Get(StubGoalPill) ?? Get(StubOrderCard);

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
                UnityEngine.Object.Destroy(tex);
                return null;
            }

            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.name = asset.Stub;
            KnockOutBakedBackdrop(tex, asset.Stub);
            PunchStudioPlate(tex, asset.Stub, asset.Category);
            var ppu = asset.PixelsPerUnit > 0.01f ? asset.PixelsPerUnit : 100f;
            var pivot = new Vector2(asset.PivotX, asset.PivotY);
            var border = asset.Stub == StubButton ? new Vector4(48f, 36f, 48f, 36f) : Vector4.zero;
            return Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                pivot,
                ppu,
                0,
                SpriteMeshType.FullRect,
                border);
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

            yield return Path.Combine(System.Environment.CurrentDirectory, DesignDropRelative);
        }

        private static string? StubForDropFile(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            var n = name.Replace('-', '_');
            if (ContainsInsensitive(n, "LayoutMock") || ContainsInsensitive(n, "Layout_Mock") ||
                ContainsInsensitive(n, "PlayHud_Layout"))
            {
                return null;
            }

            if (ContainsInsensitive(n, "Teach_Hand") || ContainsInsensitive(n, "TeachHand") ||
                ContainsInsensitive(n, "Hand_Teach"))
            {
                return StubTeachHand;
            }

            if (ContainsInsensitive(n, "TeachRing") || ContainsInsensitive(n, "Teach_Ring") ||
                ContainsInsensitive(n, "Ring_Teach") || ContainsInsensitive(n, "UI_Teach_Ring"))
            {
                return StubTeachRing;
            }

            if (ContainsInsensitive(n, "GoalPill") || ContainsInsensitive(n, "Goal_Pill"))
            {
                return StubGoalPill;
            }

            if (ContainsInsensitive(n, "Maya_Bubble") || ContainsInsensitive(n, "Maya_Speech") ||
                ContainsInsensitive(n, "UI_Maya_Bubble"))
            {
                return StubMayaBubble;
            }

            if (ContainsInsensitive(n, "OrderDock") || ContainsInsensitive(n, "Dock_Panel") ||
                ContainsInsensitive(n, "OrderTray_Dock") || ContainsInsensitive(n, "UI_OrderDock"))
            {
                return StubHudDock;
            }

            return null;
        }

        private static bool ContainsInsensitive(string haystack, string needle) =>
            haystack.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>
        /// DES-004 ring/hand shipped as RGB with the export checkerboard baked in.
        /// Punch that to alpha so Play is a gold ring / ghost hand, not a black overlay.
        /// </summary>
        private static void KnockOutBakedBackdrop(Texture2D tex, string stub)
        {
            if (tex == null || string.IsNullOrEmpty(stub))
            {
                return;
            }

            if (stub == StubTeachRing)
            {
                KnockOutDarkCheckerboard(tex);
            }
            else if (stub == StubTeachHand)
            {
                KnockOutLightCheckerboard(tex);
            }
        }

        private static void PunchStudioPlate(Texture2D tex, string stub, string category)
        {
            if (tex == null || !StudioPlatePunch.ShouldPunch(stub, category))
            {
                return;
            }

            var colors = tex.GetPixels();
            var width = tex.width;
            var height = tex.height;
            var rgba = new byte[width * height * 4];
            for (var i = 0; i < colors.Length; i++)
            {
                var c = colors[i];
                rgba[i * 4] = (byte)Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255);
                rgba[i * 4 + 1] = (byte)Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255);
                rgba[i * 4 + 2] = (byte)Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255);
                rgba[i * 4 + 3] = (byte)Mathf.Clamp(Mathf.RoundToInt(c.a * 255f), 0, 255);
            }

            if (StudioPlatePunch.Punch(rgba, width, height) <= 0)
            {
                return;
            }

            for (var i = 0; i < colors.Length; i++)
            {
                colors[i] = new Color(
                    rgba[i * 4] / 255f,
                    rgba[i * 4 + 1] / 255f,
                    rgba[i * 4 + 2] / 255f,
                    rgba[i * 4 + 3] / 255f);
            }

            tex.SetPixels(colors);
            tex.Apply(false, false);
        }

        private static void KnockOutDarkCheckerboard(Texture2D tex)
        {
            var pixels = tex.GetPixels();
            for (var i = 0; i < pixels.Length; i++)
            {
                var c = pixels[i];
                if (c.a < 0.02f)
                {
                    continue;
                }

                var L = (c.r + c.g + c.b) / 3f;
                var sat = Mathf.Max(c.r, Mathf.Max(c.g, c.b)) - Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                var warm = c.r >= c.g && c.g >= c.b - 8f / 255f && c.r >= 70f / 255f;
                var gold = warm && sat >= 25f / 255f && c.r > c.b + 20f / 255f;
                if (L < 38f / 255f && sat < 28f / 255f)
                {
                    c.a = 0f;
                }
                else if (gold || (warm && L > 45f / 255f))
                {
                    c.a = L < 70f / 255f ? Mathf.Clamp01((L - 30f / 255f) * 6f) : 1f;
                }
                else if (sat < 20f / 255f && L < 80f / 255f)
                {
                    c.a = 0f;
                }
                else
                {
                    var a = Mathf.Clamp01((L - 28f / 255f) * 5f);
                    c.a = a < 12f / 255f ? 0f : a;
                }

                pixels[i] = c;
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);
        }

        private static void KnockOutLightCheckerboard(Texture2D tex)
        {
            var pixels = tex.GetPixels();
            for (var i = 0; i < pixels.Length; i++)
            {
                var c = pixels[i];
                if (c.a < 0.02f)
                {
                    continue;
                }

                var sat = Mathf.Max(c.r, Mathf.Max(c.g, c.b)) - Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                if (sat < 16f / 255f)
                {
                    c.a = 0f;
                }
                else if (sat < 28f / 255f)
                {
                    c.a = (sat - 16f / 255f) / (12f / 255f);
                }

                pixels[i] = c;
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);
        }

        /// <summary>Wide Grove-green chip for the Goal line — not the thin studio plate PNG.</summary>
        public static Sprite GoalChipSprite()
        {
            if (_goalChip != null)
            {
                return _goalChip;
            }

            const int w = 512;
            const int h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            var radius = 52f;
            var fill = new Color(0.18f, 0.34f, 0.20f, 1f);
            var hi = new Color(0.26f, 0.46f, 0.28f, 1f);
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var dx = x < radius ? radius - x : (x > w - 1 - radius ? x - (w - 1 - radius) : 0f);
                    var dy = y < radius ? radius - y : (y > h - 1 - radius ? y - (h - 1 - radius) : 0f);
                    var outside = dx * dx + dy * dy > radius * radius && (x < radius || x > w - 1 - radius);
                    if (outside && (y < radius || y > h - 1 - radius))
                    {
                        pixels[y * w + x] = Color.clear;
                        continue;
                    }

                    var t = y / (float)(h - 1);
                    pixels[y * w + x] = Color.Lerp(fill, hi, t * 0.45f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            tex.name = "grove_goal_chip";
            _goalChip = Sprite.Create(
                tex,
                new Rect(0f, 0f, w, h),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(56f, 40f, 56f, 40f));
            return _goalChip;
        }

        private static Sprite GlowRingSprite()
        {
            if (_glowRing != null)
            {
                return _glowRing;
            }

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            var cx = (size - 1) * 0.5f;
            var cy = (size - 1) * 0.5f;
            var mid = size * 0.40f;
            var half = size * 0.06f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - cx;
                    var dy = y - cy;
                    var d = Mathf.Sqrt(dx * dx + dy * dy);
                    var a = 1f - Mathf.Abs(d - mid) / half;
                    a = Mathf.Clamp01(a);
                    a *= a;
                    pixels[y * size + x] = new Color(1f, 0.78f, 0.22f, a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            tex.name = "grove_teach_ring_generated";
            _glowRing = Sprite.Create(
                tex,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            return _glowRing;
        }
    }
}
