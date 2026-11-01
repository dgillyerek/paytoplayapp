using System;
using System.Collections.Generic;
using System.IO;
using Survival.Domain.Flavor;
using Survival.Domain.Ids;
using Survival.Domain.Theme;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>Loads ThemePack PNGs at runtime via Texture2D.LoadImage so Play Mode does not wait on sprite import.</summary>
    public static class SurvivalArt
    {
        public const string SplashKey = "splash";
        public const string TerrainKey = "terrain";
        public const string KnightKey = SurvIds.ThemeAHeroKnight01;

        private static Dictionary<string, Sprite>? _sprites;
        private static readonly Dictionary<string, Texture2D> Textures = new(StringComparer.OrdinalIgnoreCase);
        private static IReadOnlyDictionary<string, ArtSlice>? _slices;
        private static Sprite? _white;
        private static Sprite? _circle;
        private static Sprite? _ring;
        private static Sprite? _bleed;

        public static void EnsureLoaded(AppFlavorConfig flavor, ThemePackBinder pack)
        {
            if (_sprites != null)
            {
                return;
            }

            _sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            _slices = pack.ArtSlices;
            var packRoot = ResolvePackRoot(flavor);
            TryLoad(packRoot, pack, SurvIds.FlavorSplash, SplashKey);
            if (pack.Art.TryGetValue("map.terrain", out var terrainRel))
            {
                LoadPath(Path.Combine(packRoot, terrainRel.Replace('/', Path.DirectorySeparatorChar)), TerrainKey);
            }

            foreach (var pair in pack.Art)
            {
                LoadPath(Path.Combine(packRoot, pair.Value.Replace('/', Path.DirectorySeparatorChar)), pair.Key);
            }

            // Atlases: an "art" key ending in ".atlas" points at one PNG; its sibling .txt lists the sub-sprites,
            // registered as "<key without atlas>" + name (e.g. theme_a.hook.track.cell_royal). One texture, one draw batch.
            foreach (var pair in pack.Art)
            {
                if (pair.Key.EndsWith(AtlasSuffix, StringComparison.Ordinal))
                {
                    LoadAtlas(Path.Combine(packRoot, pair.Value.Replace('/', Path.DirectorySeparatorChar)), pair.Key.Substring(0, pair.Key.Length - "atlas".Length));
                }
            }

            // Painted art wins; the code-drawn kit in "art_fallback" only fills keys whose painted file is missing or unreadable.
            foreach (var pair in pack.ArtFallback)
            {
                LoadPath(Path.Combine(packRoot, pair.Value.Replace('/', Path.DirectorySeparatorChar)), pair.Key, pack.ArtFallbackSlices);
            }
        }

        public static Sprite? Get(string key)
        {
            if (_sprites != null && _sprites.TryGetValue(key, out var sprite))
            {
                return sprite;
            }

            return null;
        }

        /// <summary>True when the sprite was loaded with a 9-slice border from pack.json "art_slices".</summary>
        public static bool IsSliced(Sprite? sprite) => sprite != null && sprite.border.sqrMagnitude > 0f;

        public static Sprite White()
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
            tex.name = "survival_ui_white";
            _white = Sprite.Create(tex, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 4f, 0, SpriteMeshType.FullRect);
            return _white;
        }

        public static Sprite Circle()
        {
            if (_circle != null)
            {
                return _circle;
            }

            _circle = MakeRadial(64, (d, r) => Mathf.Clamp01(r - d + 0.75f));
            _circle.name = "survival_ui_circle";
            return _circle;
        }

        public static Sprite Ring()
        {
            if (_ring != null)
            {
                return _ring;
            }

            _ring = MakeRadial(96, (d, r) =>
            {
                var outer = r;
                var inner = r * 0.62f;
                var o = Mathf.Clamp01(outer - d + 0.8f);
                var i = Mathf.Clamp01(d - inner + 0.8f);
                return o * i;
            });
            _ring.name = "survival_ui_ring";
            return _ring;
        }

        public static Sprite EdgeBleed(Sprite? source)
        {
            if (_bleed != null)
            {
                return _bleed;
            }

            if (source == null || source.texture == null)
            {
                return White();
            }

            var src = source.texture;
            const int bw = 32;
            const int bh = 64;
            var tex = new Texture2D(bw, bh, TextureFormat.RGB24, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.name = "survival_splash_bleed";
            var sw = src.width;
            var sh = src.height;
            for (var y = 0; y < bh; y++)
            {
                var sy = Mathf.Clamp(Mathf.RoundToInt(y / (float)(bh - 1) * (sh - 1)), 0, sh - 1);
                var left = src.GetPixel(0, sy);
                var right = src.GetPixel(sw - 1, sy);
                for (var x = 0; x < bw; x++)
                {
                    var t = x / (float)(bw - 1);
                    tex.SetPixel(x, y, Color.Lerp(left, right, t));
                }
            }

            tex.Apply();
            _bleed = Sprite.Create(tex, new Rect(0f, 0f, bw, bh), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
            return _bleed;
        }

        private static Sprite MakeRadial(int n, Func<float, float, float> alphaAt)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[n * n];
            var c = (n - 1) * 0.5f;
            var r = c - 1.2f;
            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                {
                    var dx = x - c;
                    var dy = y - c;
                    var d = Mathf.Sqrt(dx * dx + dy * dy);
                    var a = alphaAt(d, r);
                    pixels[y * n + x] = new Color(1f, 1f, 1f, a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), n, 0, SpriteMeshType.FullRect);
        }

        public static string ResolvePackRoot(AppFlavorConfig flavor)
        {
            var data = Application.dataPath;
            if (!string.IsNullOrEmpty(data))
            {
                var fromAssets = Path.Combine(data, flavor.PackPath.Replace('/', Path.DirectorySeparatorChar));
                if (PlayerAssets.DirectoryExists(fromAssets))
                {
                    return fromAssets;
                }
            }

            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Assets", flavor.PackPath));
        }

        private static void TryLoad(string packRoot, ThemePackBinder pack, string flavorKey, string spriteKey)
        {
            if (pack.Art.TryGetValue(flavorKey, out var rel))
            {
                LoadPath(Path.Combine(packRoot, rel.Replace('/', Path.DirectorySeparatorChar)), spriteKey);
            }
        }

        public const string AtlasSuffix = ".atlas";

        /// <summary>Slices a packed atlas PNG by its sibling "name x y w h l b r t" text file (pixel rects, top-left origin).</summary>
        private static void LoadAtlas(string pngPath, string keyPrefix)
        {
            var full = Path.GetFullPath(pngPath);
            if (!Textures.TryGetValue(full, out var tex) || tex == null)
            {
                return;
            }

            var listPath = Path.ChangeExtension(full, ".txt");
            if (!PlayerAssets.FileExists(listPath))
            {
                return;
            }

            foreach (var raw in PlayerAssets.ReadAllLines(listPath))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                var f = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (f.Length < 9 || !TryInts(f, out var v))
                {
                    continue;
                }

                var key = keyPrefix + f[0];
                if (_sprites!.ContainsKey(key))
                {
                    continue;
                }

                var rect = new Rect(v[0], tex.height - v[1] - v[3], v[2], v[3]);
                var sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(v[4], v[5], v[6], v[7]));
                sprite.name = key;
                _sprites[key] = sprite;
            }
        }

        private static bool TryInts(string[] f, out int[] v)
        {
            v = new int[8];
            for (var i = 0; i < 8; i++)
            {
                if (!int.TryParse(f[i + 1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out v[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static void LoadPath(string path, string key, IReadOnlyDictionary<string, ArtSlice>? slices = null)
        {
            slices ??= _slices;
            if (_sprites!.ContainsKey(key) || !PlayerAssets.FileExists(path))
            {
                return;
            }

            var full = Path.GetFullPath(path);
            if (!Textures.TryGetValue(full, out var tex) || tex == null)
            {
                var bytes = PlayerAssets.ReadAllBytes(full);
                tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(bytes, markNonReadable: false))
                {
                    UnityEngine.Object.Destroy(tex);
                    return;
                }

                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.alphaIsTransparency = true;
                tex.name = key;
                Textures[full] = tex;
            }

            var border = Vector4.zero;
            if (slices != null && slices.TryGetValue(key, out var slice))
            {
                border = new Vector4(
                    Mathf.Min(slice.Left, tex.width * 0.49f),
                    Mathf.Min(slice.Bottom, tex.height * 0.49f),
                    Mathf.Min(slice.Right, tex.width * 0.49f),
                    Mathf.Min(slice.Top, tex.height * 0.49f));
            }

            var sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border);
            sprite.name = key;
            _sprites[key] = sprite;
        }
    }
}
