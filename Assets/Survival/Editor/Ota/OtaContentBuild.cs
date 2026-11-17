#nullable enable
#if SURVIVAL_ADDRESSABLES
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Survival.Domain.Ota;
using Survival.Unity;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Survival.Editor.Ota
{
    /// <summary>
    /// Builds an over-the-air content drop (Docs/OTA.md):
    /// 1. bakes the player assets (<see cref="PlayerBake"/>) so the drop has exactly what the build would carry;
    /// 2. splits the baked registry into one <see cref="PlayerAssetRegistry"/> per <see cref="OtaContentGroup"/>
    ///    (Assets/Survival/OtaBake, git-ignored) and makes each an Addressable ("ota/characters", label "ota") in its
    ///    remote group, so a bundle carries the registry plus every model, clip, texture and raw-JSON TextAsset it lists;
    /// 3. runs the Addressables content build into ServerData/[BuildTarget] (remote catalog + bundles) and writes
    ///    content_manifest.json beside it. scripts/ota/publish_content.sh uploads that folder to content/[BuildTarget]/&lt;version&gt;.
    /// CI: <c>-executeMethod Survival.Editor.Ota.OtaContentBuild.BuildFromCommandLine -buildTarget Android -contentVersion 2026.11.15-1</c>.
    /// Content only: bundles never carry code, and new content may only use component types the shipped app already has
    /// (otherwise raise min_supported_version and ship a store build).
    /// </summary>
    public static class OtaContentBuild
    {
        public const string OtaBakeDir = "Assets/Survival/OtaBake";

        [MenuItem("Survival/OTA/Build Content (active build target)")]
        public static void BuildMenu()
        {
            var ok = Build(DefaultVersion(), out var summary);
            EditorUtility.DisplayDialog(ok ? "Survival OTA content built" : "Survival OTA content build failed", summary, "OK");
        }

        public static void BuildFromCommandLine()
        {
            var code = 1;
            try
            {
                var args = CiAndroidBuild.ParseArgs(Environment.GetCommandLineArgs());
                var version = args.TryGetValue("contentVersion", out var v) && v.Length > 0 ? v : DefaultVersion();
                code = Build(version, out var summary) ? 0 : 1;
                Debug.Log("Survival OTA content build: " + summary);
            }
            catch (Exception e)
            {
                Debug.LogError("Survival OTA content build crashed: " + e);
            }

            EditorApplication.Exit(code);
        }

        public static string DefaultVersion() => DateTime.UtcNow.ToString("yyyy.MM.dd-HHmm");

        public static bool Build(string contentVersion, out string summary)
        {
            var activeTarget = EditorUserBuildSettings.activeBuildTarget;
            if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(activeTarget), activeTarget))
            {
                summary = "The active build target (" + activeTarget + ") isn't installed in this Unity Editor. Add its Build Support module in Unity Hub " +
                          "(Installs > 6000.3 > Add modules > Android Build Support with OpenJDK and SDK/NDK), restart Unity, then build again.";
                return false;
            }

            if (activeTarget != BuildTarget.Android && activeTarget != BuildTarget.iOS)
            {
                summary = "OTA content is built per phone platform, but the active build target is " + activeTarget + ". " +
                          "Open File > Build Profiles, pick Android, click Switch Platform, then build again.";
                return false;
            }

            if (!PlayerBake.Bake(out var bake))
            {
                summary = "Player bake had problems:\n" + bake;
                return false;
            }

            var counts = PrepareGroups();
            AddressableAssetSettings.CleanPlayerContent();
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (!string.IsNullOrEmpty(result.Error))
            {
                summary = "Addressables build failed: " + result.Error;
                return false;
            }

            var target = EditorUserBuildSettings.activeBuildTarget.ToString();
            var outDir = Path.Combine("ServerData", target);
            Directory.CreateDirectory(outDir);
            var files = Directory.GetFiles(outDir, "*", SearchOption.TopDirectoryOnly).Select(Path.GetFileName).OrderBy(f => f, StringComparer.Ordinal).ToList();
            var manifest = "{\n  \"content_version\": \"" + contentVersion + "\",\n  \"build_target\": \"" + target + "\",\n  \"content_api\": 1,\n" +
                           "  \"unity\": \"" + Application.unityVersion + "\",\n  \"built_at\": \"" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") + "\",\n" +
                           "  \"groups\": {" + string.Join(",", counts.Select(c => "\"" + c.Key + "\": " + c.Value)) + "},\n" +
                           "  \"files\": [" + string.Join(",", files.Select(f => "\"" + f + "\"")) + "]\n}\n";
            File.WriteAllText(Path.Combine(outDir, "content_manifest.json"), manifest);
            summary = "Content " + contentVersion + " for " + target + ": " + string.Join(", ", counts.Select(c => c.Key + " " + c.Value)) + " entries, " + files.Count +
                      " files in " + outDir + ". Publish with scripts/ota/publish_content.sh " + target + " " + contentVersion + ".";
            return true;
        }

        /// <summary>Split the baked registry into the four OTA registries and register them as Addressables. Returns entries per group.</summary>
        public static Dictionary<string, int> PrepareGroups()
        {
            var settings = OtaAddressablesSetup.Ensure();
            var baked = AssetDatabase.LoadAssetAtPath<PlayerAssetRegistry>(PlayerBake.RegistryPath) ??
                        throw new InvalidOperationException("No player bake at " + PlayerBake.RegistryPath + "; run Survival/Player Build/Bake Player Assets.");
            Directory.CreateDirectory(OtaBakeDir);
            var byGroup = OtaContentGroups.All.ToDictionary(g => g, _ => ScriptableObject.CreateInstance<PlayerAssetRegistry>());
            foreach (var e in baked.Assets)
            {
                byGroup[OtaContentGroups.Classify(e.Path, rawFile: false)].Assets.Add(new PlayerAssetRegistry.AssetEntry { Path = e.Path, Main = e.Main, Subs = e.Subs });
            }

            foreach (var f in baked.Files)
            {
                var reg = byGroup[OtaContentGroups.Classify(f.Path, rawFile: true)];
                reg.Files.Add(new PlayerAssetRegistry.FileEntry { Path = f.Path, Data = f.Data });
                for (var dir = Path.GetDirectoryName(f.Path)?.Replace('\\', '/'); !string.IsNullOrEmpty(dir) && dir != "Assets"; dir = Path.GetDirectoryName(dir)?.Replace('\\', '/'))
                {
                    if (!reg.Directories.Contains(dir!))
                    {
                        reg.Directories.Add(dir!);
                    }
                }
            }

            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var kv in byGroup)
            {
                var reg = kv.Value;
                reg.Stamp = baked.Stamp;
                reg.BakedAt = baked.BakedAt;
                reg.UnityVersion = baked.UnityVersion;
                var registryPath = OtaBakeDir + "/" + kv.Key.ToString().ToLowerInvariant() + ".asset";
                if (File.Exists(registryPath))
                {
                    AssetDatabase.DeleteAsset(registryPath);
                }

                AssetDatabase.CreateAsset(reg, registryPath);
                var group = settings.FindGroup(OtaContentGroups.GroupName(kv.Key));
                var registryEntry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(registryPath), group, false, false);
                registryEntry.address = OtaContentGroups.Address(kv.Key);
                registryEntry.SetLabel(OtaContentGroups.Label, true, true, false);
                if (OtaContentGroups.PacksByLabel(kv.Key))
                {
                    RegisterPackedEntries(settings, group, registryPath, reg);
                }

                counts[kv.Key.ToString()] = reg.Assets.Count + reg.Files.Count;
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return counts;
        }

        /// <summary>
        /// Registers each baked asset as its own Addressable entry labelled by <see cref="OtaContentGroups.PackLabel"/>
        /// so PackTogetherByLabel emits one bundle per character (or art area). Entries do not get the "ota" label;
        /// only the four registries do, so LoadAssetsAsync&lt;PlayerAssetRegistry&gt;("ota") still returns just those.
        /// </summary>
        private static void RegisterPackedEntries(AddressableAssetSettings settings, AddressableAssetGroup group, string registryPath, PlayerAssetRegistry reg)
        {
            var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { AssetDatabase.AssetPathToGUID(registryPath) };
            foreach (var bakedAsset in reg.Assets)
            {
                if (bakedAsset?.Main == null)
                {
                    continue;
                }

                var sourcePath = AssetDatabase.GetAssetPath(bakedAsset.Main);
                if (string.IsNullOrEmpty(sourcePath))
                {
                    continue;
                }

                var guid = AssetDatabase.AssetPathToGUID(sourcePath);
                if (string.IsNullOrEmpty(guid))
                {
                    continue;
                }

                keep.Add(guid);
                var packed = settings.CreateOrMoveEntry(guid, group, false, false);
                packed.address = bakedAsset.Path;
                packed.SetLabel(OtaContentGroups.Label, false, true, false);
                packed.SetLabel(OtaContentGroups.PackLabel(bakedAsset.Path), true, true, false);
            }

            foreach (var bakedFile in reg.Files)
            {
                if (bakedFile?.Data == null)
                {
                    continue;
                }

                var sourcePath = AssetDatabase.GetAssetPath(bakedFile.Data);
                if (string.IsNullOrEmpty(sourcePath))
                {
                    continue;
                }

                var guid = AssetDatabase.AssetPathToGUID(sourcePath);
                if (string.IsNullOrEmpty(guid))
                {
                    continue;
                }

                keep.Add(guid);
                var packed = settings.CreateOrMoveEntry(guid, group, false, false);
                packed.address = bakedFile.Path;
                packed.SetLabel(OtaContentGroups.Label, false, true, false);
                packed.SetLabel(OtaContentGroups.PackLabel(bakedFile.Path), true, true, false);
            }

            var stale = new List<AddressableAssetEntry>();
            foreach (var existing in group.entries)
            {
                if (!keep.Contains(existing.guid))
                {
                    stale.Add(existing);
                }
            }

            foreach (var existing in stale)
            {
                settings.RemoveAssetEntry(existing.guid);
            }
        }
    }
}
#endif
