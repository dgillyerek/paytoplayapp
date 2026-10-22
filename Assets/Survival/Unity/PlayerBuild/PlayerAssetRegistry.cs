#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Player-build stand-in for AssetDatabase (Android / iOS): every model, clip, avatar, texture and raw JSON / PNG the
    /// runner, Locked Battle, loading screen and hub read by "Assets/..." path, baked in the Editor by
    /// Survival/Player Build/Bake Player Assets into Assets/Survival/PlayerBake/Resources. The Editor never reads it at
    /// play time (it keeps using AssetDatabase); the build preprocessor refuses to build when it is missing or stale.
    /// </summary>
    public sealed class PlayerAssetRegistry : ScriptableObject
    {
        public const string ResourcesName = "SurvivalPlayerAssetRegistry";

        [Serializable]
        public sealed class AssetEntry
        {
            /// <summary>"Assets/..." path as the Editor code asks for it.</summary>
            public string Path = "";

            /// <summary>AssetDatabase.LoadAssetAtPath&lt;Object&gt; (the model prefab, texture, ...).</summary>
            public UnityEngine.Object? Main;

            /// <summary>AssetDatabase.LoadAllAssetsAtPath order, kept to clips, avatars, textures, meshes and materials.</summary>
            public UnityEngine.Object[] Subs = Array.Empty<UnityEngine.Object>();
        }

        [Serializable]
        public sealed class FileEntry
        {
            public string Path = "";
            public TextAsset? Data;
        }

        public string Stamp = "";
        public string BakedAt = "";
        public string UnityVersion = "";
        public List<AssetEntry> Assets = new List<AssetEntry>();
        public List<FileEntry> Files = new List<FileEntry>();

        /// <summary>Directories (normalized, no trailing slash) that hold at least one baked raw file.</summary>
        public List<string> Directories = new List<string>();

        /// <summary>Materials that keep the runtime Shader.Find shaders and keyword variants in the build.</summary>
        public List<Material> ShaderKeepAlive = new List<Material>();

        public List<Shader> Shaders = new List<Shader>();

        [NonSerialized] private Dictionary<string, AssetEntry>? _assets;
        [NonSerialized] private Dictionary<string, TextAsset?>? _files;
        [NonSerialized] private HashSet<string>? _dirs;

        public AssetEntry? FindAsset(string path)
        {
            Index();
            return _assets!.TryGetValue(path, out var e) ? e : null;
        }

        public TextAsset? FindFile(string path)
        {
            Index();
            return _files!.TryGetValue(path, out var t) ? t : null;
        }

        public bool HasFile(string path)
        {
            Index();
            return _files!.ContainsKey(path);
        }

        public bool HasDirectory(string path)
        {
            Index();
            return _dirs!.Contains(path);
        }

        public void ClearIndex()
        {
            _assets = null;
            _files = null;
            _dirs = null;
        }

        private void Index()
        {
            if (_assets != null)
            {
                return;
            }

            _assets = new Dictionary<string, AssetEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in Assets)
            {
                if (e != null && !string.IsNullOrEmpty(e.Path))
                {
                    _assets[e.Path] = e;
                }
            }

            _files = new Dictionary<string, TextAsset?>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in Files)
            {
                if (f != null && !string.IsNullOrEmpty(f.Path))
                {
                    _files[f.Path] = f.Data;
                }
            }

            _dirs = new HashSet<string>(Directories, StringComparer.OrdinalIgnoreCase);
        }
    }
}
