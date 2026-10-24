#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Survival.Unity;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Survival.Editor
{
    /// <summary>
    /// Refuses any player build (Android, iOS, desktop) whose Survival player bake is missing or stale, so a phone never
    /// gets a build without its characters. Fix: Survival/Player Build/Bake Player Assets (the Build Android menu items
    /// bake first on their own).
    /// </summary>
    public sealed class PlayerBakeBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            var problems = PlayerBake.Verify();
            if (problems.Count == 0)
            {
                Debug.Log("Survival player bake verified for " + report.summary.platform + ".");
                return;
            }

            var message = "Survival player bake is missing or stale, so this build would have no heroes, villains or captives:\n" +
                          string.Join("\n", problems) +
                          "\nRun Survival/Player Build/Bake Player Assets (or build with Survival/Player Build/Build Android Development APK, which bakes first).";
            Debug.LogError(message);
            throw new BuildFailedException(message);
        }
    }

    /// <summary>
    /// Editor Play sessions record each path the runtime reads (Survival.Unity.PlayerAssets) to Library, so the next bake
    /// also carries anything the edit-mode warm-up did not touch, and warns when a played path is missing from the bake.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayerBakeRecorder
    {
        private static readonly HashSet<string> Seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        static PlayerBakeRecorder()
        {
            PlayerAssets.Requested += OnRequested;
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        private static void OnRequested(string path, bool raw)
        {
            if (EditorApplication.isPlaying && path.StartsWith("Assets/", StringComparison.Ordinal))
            {
                Seen.Add((raw ? "raw " : "ast ") + path);
            }
        }

        private static void OnPlayMode(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingPlayMode || Seen.Count == 0)
            {
                return;
            }

            try
            {
                var all = new SortedSet<string>(PlayerBake.ReadSeenLog(), StringComparer.OrdinalIgnoreCase);
                all.UnionWith(Seen);
                Directory.CreateDirectory(Path.GetDirectoryName(PlayerBake.SeenLogPath)!);
                File.WriteAllLines(PlayerBake.SeenLogPath, all);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Survival player bake recorder could not save " + PlayerBake.SeenLogPath + ": " + e.Message);
            }

            var registry = AssetDatabase.LoadAssetAtPath<PlayerAssetRegistry>(PlayerBake.RegistryPath);
            if (registry != null)
            {
                var missing = new List<string>();
                foreach (var line in Seen)
                {
                    var path = line.Substring(4);
                    var raw = line.StartsWith("raw ", StringComparison.Ordinal);
                    if (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) && raw)
                    {
                        continue;
                    }

                    var inBake = raw ? registry.HasFile(path) : registry.FindAsset(path) != null;
                    if (!inBake)
                    {
                        missing.Add(path);
                    }
                }

                if (missing.Count > 0)
                {
                    Debug.LogWarning("Survival player bake: this Play session read " + missing.Count +
                                     " path(s) a phone build would not have. Re-run Survival/Player Build/Bake Player Assets.\n" +
                                     string.Join("\n", missing.Take(40)));
                }
            }

            Seen.Clear();
        }
    }

    /// <summary>One-click Android development build for a phone on USB.</summary>
    public static class PlayerBuildMenu
    {
        public const string AndroidApkPath = "Builds/Android/paytoplayapp-dev.apk";

        [MenuItem("Survival/Player Build/Build Android Development APK")]
        public static void BuildAndroid() => Build(run: false);

        [MenuItem("Survival/Player Build/Build And Run Android Development (USB phone)")]
        public static void BuildAndRunAndroid() => Build(run: true);

        private static void Build(bool run)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Survival Android build", "Leave Play mode first.", "OK");
                return;
            }

            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                EditorUtility.DisplayDialog("Survival Android build",
                    "Could not switch to Android. Install Android Build Support (with OpenJDK, SDK & NDK) for this Unity version in Unity Hub.", "OK");
                return;
            }

            if (!PlayerBake.Bake(out var summary))
            {
                if (!EditorUtility.DisplayDialog("Survival player bake has problems", summary + "\n\nBuild anyway?", "Build", "Stop"))
                {
                    return;
                }
            }

            var problems = PlayerBake.Verify();
            if (problems.Count > 0)
            {
                EditorUtility.DisplayDialog("Survival player bake failed verification", PlayerBake.Head(problems), "OK");
                return;
            }

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.development = true;

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog("Survival Android build", "No scenes in Build Settings. Run Survival/Use Theme A Flavor first.", "OK");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(AndroidApkPath)!);
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = AndroidApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development | (run ? BuildOptions.AutoRunPlayer : BuildOptions.None)
            };

            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            var text = s.result + " in " + s.totalTime.TotalMinutes.ToString("0.0") + " min, " + (s.totalSize / (1024 * 1024)) + " MB, " +
                       s.totalErrors + " error(s). " + Path.GetFullPath(AndroidApkPath);
            Debug.Log("Survival Android build: " + text);
            EditorUtility.DisplayDialog("Survival Android build", text, "OK");
        }
    }
}
