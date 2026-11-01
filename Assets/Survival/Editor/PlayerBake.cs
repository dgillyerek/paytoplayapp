#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Survival.Domain.Roster;
using Survival.Unity;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Survival.Editor
{
    /// <summary>
    /// Player (phone) build bake. The runtime builds every hero, villain and captive from "Assets/..." paths; in the
    /// Editor that is AssetDatabase, in a player it is <see cref="PlayerAssetRegistry"/> (Resources). This bakes that
    /// registry: it spawns every roster fighter in edit mode, plays every pose (rest, walk, each rotation attack) while
    /// recording each path <see cref="PlayerAssets"/> touches, adds <see cref="PlayerBakeManifest"/> and the raw JSON /
    /// PNG roots, and writes Assets/Survival/PlayerBake (git-ignored, re-made on demand).
    /// </summary>
    public static class PlayerBake
    {
        public const string OutDir = "Assets/Survival/PlayerBake";
        public const string ResourcesDir = OutDir + "/Resources";
        public const string FilesDir = OutDir + "/Files";
        public const string ShadersDir = OutDir + "/Shaders";
        public const string RegistryPath = ResourcesDir + "/" + PlayerAssetRegistry.ResourcesName + ".asset";
        public const string ReportPath = OutDir + "/bake_report.txt";

        /// <summary>Paths the Editor Play sessions read (PlayerBakeRecorder); unioned into the next bake.</summary>
        public const string SeenLogPath = "Library/SurvivalPlayerBakeSeen.txt";

        /// <summary>Code folders whose source feeds the stamp: a runtime code change can change what gets loaded.</summary>
        private static readonly string[] CodeDirs = { "Assets/Survival/Runtime", "Assets/Survival/Unity" };

        private static readonly (string Shader, string[] Keywords, bool Transparent)[] KeepAlive =
        {
            ("Universal Render Pipeline/Lit", new string[0], false),
            ("Universal Render Pipeline/Lit", new[] { "_NORMALMAP" }, false),
            ("Universal Render Pipeline/Lit", new[] { "_NORMALMAP", "_METALLICSPECGLOSSMAP" }, false),
            ("Universal Render Pipeline/Lit", new[] { "_METALLICSPECGLOSSMAP" }, false),
            ("Universal Render Pipeline/Lit", new[] { "_EMISSION" }, false),
            ("Universal Render Pipeline/Lit", new[] { "_NORMALMAP", "_METALLICSPECGLOSSMAP", "_EMISSION" }, false),
            ("Universal Render Pipeline/Lit", new[] { "_SURFACE_TYPE_TRANSPARENT" }, true),
            ("Universal Render Pipeline/Unlit", new string[0], false),
            ("Universal Render Pipeline/Unlit", new[] { "_SURFACE_TYPE_TRANSPARENT" }, true),
            ("Universal Render Pipeline/Particles/Unlit", new[] { "_SURFACE_TYPE_TRANSPARENT" }, true),
            ("Universal Render Pipeline/Particles/Unlit", new string[0], false),
            ("Sprites/Default", new string[0], true)
        };

        [MenuItem("Survival/Player Build/Bake Player Assets")]
        public static void BakeMenu()
        {
            var ok = Bake(out var summary);
            EditorUtility.DisplayDialog(ok ? "Survival player bake done" : "Survival player bake has problems", summary, "OK");
        }

        [MenuItem("Survival/Player Build/Verify Player Bake")]
        public static void VerifyMenu()
        {
            var problems = Verify();
            if (problems.Count == 0)
            {
                EditorUtility.DisplayDialog("Survival player bake", "Up to date. A player build will carry every roster character.", "OK");
                return;
            }

            Debug.LogError("Survival player bake: " + string.Join("\n", problems));
            EditorUtility.DisplayDialog("Survival player bake is missing or stale", Head(problems) +
                "\n\nRun Survival/Player Build/Bake Player Assets.", "OK");
        }

        /// <summary>Bake the registry. False when a roster fighter failed to warm up or a manifest file is missing.</summary>
        public static bool Bake(out string summary)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                summary = "Leave Play mode first.";
                return false;
            }

            var problems = new List<string>();
            var seen = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            void OnRequested(string path, bool raw)
            {
                seen[path] = seen.TryGetValue(path, out var r) ? r || raw : raw;
            }

            try
            {
                EditorUtility.DisplayProgressBar("Survival player bake", "Warming every roster character", 0.05f);
                PlayerAssets.Requested += OnRequested;
                WarmUp(problems);
            }
            finally
            {
                PlayerAssets.Requested -= OnRequested;
            }

            try
            {
                foreach (var line in ReadSeenLog())
                {
                    var raw = line.StartsWith("raw ", StringComparison.Ordinal);
                    var path = line.Substring(4);
                    seen[path] = seen.TryGetValue(path, out var r) ? r || raw : raw;
                }

                EditorUtility.DisplayProgressBar("Survival player bake", "Collecting assets", 0.4f);
                var assetPaths = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var rel in PlayerBakeManifest.AllAssets())
                {
                    var p = "Assets/" + rel;
                    if (AssetDatabase.LoadMainAssetAtPath(p) == null)
                    {
                        problems.Add("Manifest asset missing or not imported: " + p);
                        continue;
                    }

                    assetPaths.Add(p);
                }

                // Theme sounds: SurvivalAudio loads them by path, often after the warm-up, so bake the whole folder.
                if (Directory.Exists("Assets/" + PlayerBakeManifest.AudioDir))
                {
                    foreach (var file in Directory.GetFiles("Assets/" + PlayerBakeManifest.AudioDir, "*", SearchOption.AllDirectories))
                    {
                        var p = file.Replace('\\', '/');
                        if (PlayerBakeManifest.IsAudioClip(p.Substring("Assets/".Length)) && AssetDatabase.LoadMainAssetAtPath(p) != null)
                        {
                            assetPaths.Add(p);
                        }
                    }
                }

                foreach (var kv in seen)
                {
                    if (!kv.Value && kv.Key.StartsWith("Assets/", StringComparison.Ordinal) && !IsSkippedAsset(kv.Key) &&
                        !kv.Key.StartsWith(OutDir + "/", StringComparison.OrdinalIgnoreCase) &&
                        AssetDatabase.LoadMainAssetAtPath(kv.Key) != null)
                    {
                        assetPaths.Add(kv.Key);
                    }
                }

                var rawPaths = new SortedSet<string>(ExpectedRawFiles(), StringComparer.OrdinalIgnoreCase);
                foreach (var kv in seen)
                {
                    if (kv.Value && kv.Key.StartsWith("Assets/", StringComparison.Ordinal) && !IsSkippedRaw(kv.Key) && File.Exists(kv.Key))
                    {
                        rawPaths.Add(kv.Key);
                    }
                }

                EditorUtility.DisplayProgressBar("Survival player bake", "Copying " + rawPaths.Count + " raw files", 0.55f);
                if (Directory.Exists(FilesDir))
                {
                    AssetDatabase.DeleteAsset(FilesDir);
                }

                Directory.CreateDirectory(FilesDir);
                Directory.CreateDirectory(ResourcesDir);
                Directory.CreateDirectory(ShadersDir);
                var copies = new List<(string Source, string Copy)>();
                foreach (var raw in rawPaths)
                {
                    var copy = FilesDir + "/" + ShortHash(raw) + "_" + Path.GetFileName(raw) + ".bytes";
                    File.Copy(raw, copy, true);
                    copies.Add((raw, copy));
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                EditorUtility.DisplayProgressBar("Survival player bake", "Writing the registry", 0.75f);
                var registry = ScriptableObject.CreateInstance<PlayerAssetRegistry>();
                foreach (var path in assetPaths)
                {
                    registry.Assets.Add(new PlayerAssetRegistry.AssetEntry
                    {
                        Path = path,
                        Main = AssetDatabase.LoadMainAssetAtPath(path),
                        Subs = SubAssets(path)
                    });
                }

                var dirs = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var (source, copy) in copies)
                {
                    var text = AssetDatabase.LoadAssetAtPath<TextAsset>(copy);
                    if (text == null)
                    {
                        problems.Add("Raw copy did not import as a TextAsset: " + copy + " (" + source + ")");
                    }

                    registry.Files.Add(new PlayerAssetRegistry.FileEntry { Path = source, Data = text });
                    for (var dir = Path.GetDirectoryName(source)?.Replace('\\', '/'); !string.IsNullOrEmpty(dir) && dir != "Assets";
                         dir = Path.GetDirectoryName(dir)?.Replace('\\', '/'))
                    {
                        dirs.Add(dir!);
                    }
                }

                registry.Directories.AddRange(dirs);
                AddShaderKeepAlive(registry, problems);
                registry.Stamp = ComputeStamp(assetPaths, rawPaths);
                registry.BakedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz");
                registry.UnityVersion = Application.unityVersion;

                if (File.Exists(RegistryPath))
                {
                    AssetDatabase.DeleteAsset(RegistryPath);
                }

                AssetDatabase.CreateAsset(registry, RegistryPath);
                AssetDatabase.SaveAssets();
                PlayerAssets.ResetRegistry();

                var size = rawPaths.Sum(p => new FileInfo(p).Length);
                summary = "Baked " + assetPaths.Count + " assets and " + rawPaths.Count + " raw files (" + (size / (1024 * 1024)) +
                          " MB) for " + LockedBattleRoster.All.Count + " roster characters.\n" +
                          (problems.Count == 0 ? "No problems." : problems.Count + " problem(s):\n" + Head(problems));
                File.WriteAllText(ReportPath, summary + "\n\nAssets:\n" + string.Join("\n", assetPaths) + "\n\nRaw files:\n" +
                                              string.Join("\n", rawPaths) + "\n\nProblems:\n" + string.Join("\n", problems) + "\n");
                (problems.Count == 0 ? (Action<object>)Debug.Log : Debug.LogError)("Survival player bake: " + summary + "\nReport: " + ReportPath);
                return problems.Count == 0;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>Empty when the registry exists, every entry resolves, and nothing it was baked from has changed.</summary>
        public static List<string> Verify()
        {
            var problems = new List<string>();
            var registry = AssetDatabase.LoadAssetAtPath<PlayerAssetRegistry>(RegistryPath);
            if (registry == null)
            {
                problems.Add("No player bake at " + RegistryPath + ". Phones would show no characters.");
                return problems;
            }

            var assets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in registry.Assets)
            {
                assets.Add(e.Path);
                if (e.Main == null)
                {
                    problems.Add("Baked asset no longer loads: " + e.Path);
                }
            }

            foreach (var rel in PlayerBakeManifest.AllAssets())
            {
                if (!assets.Contains("Assets/" + rel))
                {
                    problems.Add("Roster asset not in the bake: Assets/" + rel);
                }
            }

            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in registry.Files)
            {
                files.Add(f.Path);
                if (f.Data == null)
                {
                    problems.Add("Baked raw file copy missing: " + f.Path);
                }

                if (!File.Exists(f.Path))
                {
                    problems.Add("Baked raw file was deleted: " + f.Path);
                }
            }

            foreach (var raw in ExpectedRawFiles())
            {
                if (!files.Contains(raw))
                {
                    problems.Add("New raw file not in the bake: " + raw);
                }
            }

            if (problems.Count == 0)
            {
                var stamp = ComputeStamp(registry.Assets.Select(e => e.Path), registry.Files.Select(f => f.Path));
                if (!string.Equals(stamp, registry.Stamp, StringComparison.Ordinal))
                {
                    problems.Add("Player bake is stale: a character file, a .meta import setting, a raw JSON / PNG or the " +
                                 "Survival runtime code changed since " + registry.BakedAt + ".");
                }
            }

            return problems;
        }

        // ---------- warm-up ----------

        private static void WarmUp(List<string> problems)
        {
            var root = new GameObject("SurvivalPlayerBakeWarmup") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var i = 0;
                foreach (var fighter in LockedBattleRoster.All)
                {
                    EditorUtility.DisplayProgressBar("Survival player bake", "Warming " + fighter.Name,
                        0.05f + (0.3f * i++ / Math.Max(1, LockedBattleRoster.All.Count)));
                    try
                    {
                        var rig = RunnerRig.Spawn(fighter.Name, root.transform, "Warm_" + fighter.Name);
                        if (rig == null)
                        {
                            problems.Add("Warm-up could not spawn " + fighter.Name);
                            continue;
                        }

                        rig.Play(fighter.RestPoseName);
                        rig.Play(fighter.WalkPoseName);
                        foreach (var attack in fighter.Attacks)
                        {
                            rig.Play(attack.PoseName);
                            rig.Play(fighter.RestPoseName);
                        }

                        rig.WarmPoses();
                    }
                    catch (Exception e)
                    {
                        problems.Add("Warm-up threw for " + fighter.Name + ": " + e.GetType().Name + " " + e.Message);
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ---------- helpers ----------

        /// <summary>Raw files every player build must carry: the JSON / PNG roots and each fighter's byte-read maps.</summary>
        public static List<string> ExpectedRawFiles()
        {
            var list = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in PlayerBakeManifest.RawFileRoots)
            {
                var dir = "Assets/" + root;
                if (!Directory.Exists(dir))
                {
                    continue;
                }

                foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                {
                    var p = file.Replace('\\', '/');
                    if (PlayerBakeManifest.IsRawFile(p.Substring("Assets/".Length)))
                    {
                        list.Add(p);
                    }
                }
            }

            foreach (var fighter in PlayerBakeManifest.All)
            {
                foreach (var rel in fighter.RawFiles)
                {
                    if (File.Exists("Assets/" + rel))
                    {
                        list.Add("Assets/" + rel);
                    }
                }
            }

            return list.ToList();
        }

        private static bool IsSkippedAsset(string path) =>
            path.EndsWith(".blend", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase);

        private static bool IsSkippedRaw(string path) =>
            path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".blend", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) || path.StartsWith(OutDir + "/", StringComparison.OrdinalIgnoreCase);

        private static Object[] SubAssets(string path)
        {
            var main = AssetDatabase.LoadMainAssetAtPath(path);
            var list = new List<Object>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (o == null || o == main || o.name.StartsWith("__preview__", StringComparison.Ordinal))
                {
                    continue;
                }

                if (o is AnimationClip || o is Avatar || o is Texture2D || o is Mesh || o is Material)
                {
                    list.Add(o);
                }
            }

            return list.ToArray();
        }

        private static void AddShaderKeepAlive(PlayerAssetRegistry registry, List<string> problems)
        {
            var index = 0;
            foreach (var (shaderName, keywords, transparent) in KeepAlive)
            {
                var shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    problems.Add("Shader not found for the keep-alive set: " + shaderName);
                    continue;
                }

                if (!registry.Shaders.Contains(shader))
                {
                    registry.Shaders.Add(shader);
                }

                var mat = new Material(shader) { name = "KeepAlive_" + index };
                foreach (var k in keywords)
                {
                    mat.EnableKeyword(k);
                }

                if (transparent)
                {
                    if (mat.HasProperty("_Surface"))
                    {
                        mat.SetFloat("_Surface", 1f);
                    }

                    mat.renderQueue = 3000;
                }

                var path = ShadersDir + "/KeepAlive_" + index++ + ".mat";
                if (File.Exists(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }

                AssetDatabase.CreateAsset(mat, path);
                registry.ShaderKeepAlive.Add(mat);
            }
        }

        /// <summary>SHA1 over every baked source (bytes and .meta import settings), every raw file, and the runtime code.</summary>
        public static string ComputeStamp(IEnumerable<string> assetPaths, IEnumerable<string> rawPaths)
        {
            using var sha = SHA1.Create();
            var buffer = new byte[1 << 20];
            void Text(string s)
            {
                var b = Encoding.UTF8.GetBytes(s + "\n");
                sha.TransformBlock(b, 0, b.Length, null, 0);
            }

            void FileBytes(string path)
            {
                if (!File.Exists(path))
                {
                    Text("missing " + path);
                    return;
                }

                using var stream = File.OpenRead(path);
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    sha.TransformBlock(buffer, 0, read, null, 0);
                }
            }

            Text("v1 " + Application.unityVersion);
            foreach (var p in assetPaths.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                Text("asset " + p);
                FileBytes(p);
                FileBytes(p + ".meta");
            }

            foreach (var p in rawPaths.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                Text("raw " + p);
                FileBytes(p);
            }

            foreach (var dir in CodeDirs)
            {
                if (!Directory.Exists(dir))
                {
                    continue;
                }

                foreach (var cs in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories).Select(f => f.Replace('\\', '/'))
                             .OrderBy(f => f, StringComparer.Ordinal))
                {
                    Text("code " + cs);
                    FileBytes(cs);
                }
            }

            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return BitConverter.ToString(sha.Hash!).Replace("-", "").ToLowerInvariant();
        }

        private static string ShortHash(string s)
        {
            using var sha = SHA1.Create();
            var h = sha.ComputeHash(Encoding.UTF8.GetBytes(s.ToLowerInvariant()));
            return BitConverter.ToString(h, 0, 6).Replace("-", "").ToLowerInvariant();
        }

        internal static IEnumerable<string> ReadSeenLog()
        {
            if (!File.Exists(SeenLogPath))
            {
                yield break;
            }

            foreach (var line in File.ReadAllLines(SeenLogPath))
            {
                if (line.Length > 4 && (line.StartsWith("raw ", StringComparison.Ordinal) || line.StartsWith("ast ", StringComparison.Ordinal)))
                {
                    yield return line;
                }
            }
        }

        internal static string Head(List<string> lines) =>
            string.Join("\n", lines.Take(12)) + (lines.Count > 12 ? "\n… and " + (lines.Count - 12) + " more (see the Console)." : "");
    }
}
