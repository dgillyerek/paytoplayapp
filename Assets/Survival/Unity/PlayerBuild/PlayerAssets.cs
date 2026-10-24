#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Object = UnityEngine.Object;

namespace Survival.Unity
{
    /// <summary>
    /// The one door the runtime uses to read project files by "Assets/..." path (models, clips, avatars, textures, and the
    /// raw JSON / PNG the hub, runner and battle parse). In the Editor it is exactly AssetDatabase / System.IO (same
    /// results as before) and records every path asked for, so the player bake can include it. In a player build
    /// (Android, iOS, desktop) it reads the baked <see cref="PlayerAssetRegistry"/> from Resources; AssetDatabase and
    /// Application.dataPath files do not exist there.
    /// </summary>
    public static class PlayerAssets
    {
#if UNITY_EDITOR
        /// <summary>Editor only: every existing path a runtime read touched (normalized "Assets/..."), true = raw file.</summary>
        public static event Action<string, bool>? Requested;
#endif

        private static PlayerAssetRegistry? _registry;
        private static bool _registryTried;
        private static readonly HashSet<string> Missing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// "Assets/..." with forward slashes. Accepts an "Assets/" path, a path under Application.dataPath, or an absolute
        /// path that contains "/Assets/" under the current directory (the Editor's Directory.GetCurrentDirectory fallback).
        /// </summary>
        public static string Normalize(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "";
            }

            var p = path.Replace('\\', '/');
            var data = (Application.dataPath ?? "").Replace('\\', '/').TrimEnd('/');
            if (data.Length > 0 && p.StartsWith(data + "/", StringComparison.OrdinalIgnoreCase))
            {
                return Collapse("Assets/" + p.Substring(data.Length + 1));
            }

            if (p.StartsWith("Assets/", StringComparison.Ordinal))
            {
                return Collapse(p);
            }

            var cwd = SafeCwd();
            if (cwd.Length > 0 && p.StartsWith(cwd + "/Assets/", StringComparison.OrdinalIgnoreCase))
            {
                return Collapse(p.Substring(cwd.Length + 1));
            }

#if !UNITY_EDITOR
            // A player has no project folder: "<anything>/Assets/x" built from a guessed repo root still means "Assets/x".
            var marker = p.IndexOf("/Assets/", StringComparison.Ordinal);
            if (marker >= 0)
            {
                return Collapse(p.Substring(marker + 1));
            }
#endif
            return Collapse(p);
        }

        public static T? Load<T>(string assetPath) where T : Object
        {
#if UNITY_EDITOR
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                Note(assetPath, false);
            }

            return asset;
#else
            var entry = Registry()?.FindAsset(Normalize(assetPath));
            if (entry == null)
            {
                Miss(assetPath);
                return null;
            }

            if (entry.Main is T main)
            {
                return main;
            }

            foreach (var sub in entry.Subs)
            {
                if (sub is T t)
                {
                    return t;
                }
            }

            return null;
#endif
        }

        public static Object[] LoadAll(string assetPath)
        {
#if UNITY_EDITOR
            var all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (all != null && all.Length > 0)
            {
                Note(assetPath, false);
            }

            return all ?? Array.Empty<Object>();
#else
            var entry = Registry()?.FindAsset(Normalize(assetPath));
            if (entry == null)
            {
                Miss(assetPath);
                return Array.Empty<Object>();
            }

            var list = new List<Object>(entry.Subs.Length + 1);
            if (entry.Main != null)
            {
                list.Add(entry.Main);
            }

            foreach (var sub in entry.Subs)
            {
                if (sub != null)
                {
                    list.Add(sub);
                }
            }

            return list.ToArray();
#endif
        }

        /// <summary>True when an imported asset (model, texture) is at <paramref name="assetPath"/>: File.Exists in the
        /// Editor, a baked asset entry in a player.</summary>
        public static bool AssetFileExists(string assetPath)
        {
#if UNITY_EDITOR
            var ok = File.Exists(assetPath);
            if (ok)
            {
                Note(assetPath, false);
            }

            return ok;
#else
            return Registry()?.FindAsset(Normalize(assetPath)) != null;
#endif
        }

        public static bool FileExists(string path)
        {
#if UNITY_EDITOR
            var ok = File.Exists(path);
            if (ok)
            {
                Note(path, true);
            }

            return ok;
#else
            return Registry()?.HasFile(Normalize(path)) ?? false;
#endif
        }

        public static bool DirectoryExists(string path)
        {
#if UNITY_EDITOR
            return Directory.Exists(path);
#else
            return Registry()?.HasDirectory(Normalize(path).TrimEnd('/')) ?? false;
#endif
        }

        public static byte[] ReadAllBytes(string path)
        {
#if UNITY_EDITOR
            var bytes = File.ReadAllBytes(path);
            Note(path, true);
            return bytes;
#else
            var data = Registry()?.FindFile(Normalize(path));
            if (data == null)
            {
                Miss(path);
                throw new FileNotFoundException("Not in the player asset bake: " + Normalize(path), path);
            }

            return data.bytes;
#endif
        }

        public static string ReadAllText(string path)
        {
#if UNITY_EDITOR
            var text = File.ReadAllText(path);
            Note(path, true);
            return text;
#else
            return DecodeUtf8(ReadAllBytes(path));
#endif
        }

        public static string[] ReadAllLines(string path)
        {
#if UNITY_EDITOR
            var lines = File.ReadAllLines(path);
            Note(path, true);
            return lines;
#else
            var text = ReadAllText(path);
            var lines = text.Split('\n');
            var count = lines.Length;
            if (count > 0 && lines[count - 1].Length == 0)
            {
                count--;
            }

            var result = new string[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = lines[i].TrimEnd('\r');
            }

            return result;
#endif
        }

        /// <summary>The baked registry, or null in the Editor before a bake / in a player built without one.</summary>
        public static PlayerAssetRegistry? Registry()
        {
            if (_registry == null && !_registryTried)
            {
                _registryTried = true;
                _registry = Resources.Load<PlayerAssetRegistry>(PlayerAssetRegistry.ResourcesName);
#if !UNITY_EDITOR
                if (_registry == null)
                {
                    Debug.LogError("Survival player asset bake missing (Resources/" + PlayerAssetRegistry.ResourcesName +
                                   "). Run Survival/Player Build/Bake Player Assets in the Editor and rebuild.");
                }
#endif
            }

            return _registry;
        }

#if UNITY_EDITOR
        /// <summary>Editor: drop the cached registry after a bake.</summary>
        public static void ResetRegistry()
        {
            _registry = null;
            _registryTried = false;
        }

        private static void Note(string path, bool raw)
        {
            var handler = Requested;
            if (handler != null)
            {
                handler(Normalize(path), raw);
            }
        }
#endif

        private static string DecodeUtf8(byte[] bytes)
        {
            // File.ReadAllText drops a UTF-8 BOM; match it.
            var offset = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
            return Encoding.UTF8.GetString(bytes, offset, bytes.Length - offset);
        }

        private static void Miss(string path)
        {
            if (Missing.Add(path))
            {
                Debug.LogError("Not in the player asset bake: " + Normalize(path) +
                               ". Re-run Survival/Player Build/Bake Player Assets in the Editor and rebuild.");
            }
        }

        private static string SafeCwd()
        {
            try
            {
                return Directory.GetCurrentDirectory().Replace('\\', '/').TrimEnd('/');
            }
            catch (Exception)
            {
                return "";
            }
        }

        private static string Collapse(string p)
        {
            if (p.IndexOf("/./", StringComparison.Ordinal) < 0 && p.IndexOf("/../", StringComparison.Ordinal) < 0 && p.IndexOf("//", StringComparison.Ordinal) < 0)
            {
                return p;
            }

            var parts = new List<string>();
            foreach (var part in p.Split('/'))
            {
                if (part.Length == 0 || part == ".")
                {
                    continue;
                }

                if (part == ".." && parts.Count > 0)
                {
                    parts.RemoveAt(parts.Count - 1);
                    continue;
                }

                parts.Add(part);
            }

            return string.Join("/", parts);
        }
    }
}
