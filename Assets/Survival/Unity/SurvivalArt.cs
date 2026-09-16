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
        private static Sprite? _white;

        public static void EnsureLoaded(AppFlavorConfig flavor, ThemePackBinder pack)
        {
            if (_sprites != null)
            {
                return;
            }

            _sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
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
        }

        public static Sprite? Get(string key)
        {
            if (_sprites != null && _sprites.TryGetValue(key, out var sprite))
            {
                return sprite;
            }

            return null;
        }

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

        public static string ResolvePackRoot(AppFlavorConfig flavor)
        {
            var data = Application.dataPath;
            if (!string.IsNullOrEmpty(data))
            {
                var fromAssets = Path.Combine(data, flavor.PackPath.Replace('/', Path.DirectorySeparatorChar));
                if (Directory.Exists(fromAssets))
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

        private static void LoadPath(string path, string key)
        {
            if (_sprites!.ContainsKey(key) || !File.Exists(path))
            {
                return;
            }

            var bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes, markNonReadable: false))
            {
                UnityEngine.Object.Destroy(tex);
                return;
            }

            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.alphaIsTransparency = true;
            tex.name = key;
            var sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            _sprites[key] = sprite;
        }
    }
}
