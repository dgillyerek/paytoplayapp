#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Survival.Editor
{
    /// <summary>
    /// Headless Android APK build for GitHub Actions (.github/workflows/android-build.yml, game-ci/unity-builder
    /// <c>buildMethod: Survival.Editor.CiAndroidBuild.Build</c>). Same steps as Survival/Player Build/Build Android
    /// Development APK (switch to Android, bake player assets, verify the bake, IL2CPP ARM64 APK), but reads the output
    /// path, version and signing key from the command line and exits with a non-zero code on any failure so CI goes red.
    /// </summary>
    /// <remarks>
    /// Arguments (game-ci passes the first group itself): -customBuildPath, -buildVersion, -androidVersionCode,
    /// -androidKeystoreName (relative to the project root), -androidKeystorePass, -androidKeyaliasName,
    /// -androidKeyaliasPass. Extra: -requireKeystore (fail rather than sign with Unity's debug key), -developmentBuild.
    /// </remarks>
    public static class CiAndroidBuild
    {
        public static void Build()
        {
            var code = 1;
            try
            {
                code = Run(ParseArgs(Environment.GetCommandLineArgs()));
            }
            catch (Exception e)
            {
                Debug.LogError("Survival CI Android build crashed: " + e);
            }

            Debug.Log("Survival CI Android build exit code " + code + ".");
            EditorApplication.Exit(code);
        }

        /// <summary>"-name value" pairs; a flag with no value maps to "".</summary>
        public static Dictionary<string, string> ParseArgs(string[] args)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("-", StringComparison.Ordinal) || args[i].Length < 2)
                {
                    continue;
                }

                var name = args[i].Substring(1);
                var hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal);
                result[name] = hasValue ? args[++i] : "";
            }

            return result;
        }

        private static string Arg(Dictionary<string, string> args, string name) =>
            args.TryGetValue(name, out var v) ? v.Trim() : "";

        private static int Run(Dictionary<string, string> args)
        {
            var output = Arg(args, "customBuildPath");
            if (output.Length == 0)
            {
                output = PlayerBuildMenu.AndroidApkPath;
            }

            if (!output.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
            {
                output += ".apk";
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("Survival CI Android build: could not switch to Android (is Android Build Support in this editor image?).");
                return 2;
            }

            if (!PlayerBake.Bake(out var summary))
            {
                Debug.LogError("Survival CI Android build: player bake had problems, so the phone would miss characters:\n" + summary);
                return 3;
            }

            var problems = PlayerBake.Verify();
            if (problems.Count > 0)
            {
                Debug.LogError("Survival CI Android build: player bake failed verification:\n" + string.Join("\n", problems));
                return 3;
            }

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

            var version = Arg(args, "buildVersion");
            if (version.Length > 0 && version != "none")
            {
                PlayerSettings.bundleVersion = version;
            }

            if (int.TryParse(Arg(args, "androidVersionCode"), out var versionCode) && versionCode > 0)
            {
                PlayerSettings.Android.bundleVersionCode = versionCode;
            }

            var keystore = Arg(args, "androidKeystoreName");
            if (keystore.Length > 0 && File.Exists(keystore))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = Path.GetFullPath(keystore);
                PlayerSettings.Android.keystorePass = Arg(args, "androidKeystorePass");
                PlayerSettings.Android.keyaliasName = Arg(args, "androidKeyaliasName");
                PlayerSettings.Android.keyaliasPass = Arg(args, "androidKeyaliasPass");
            }
            else if (args.ContainsKey("requireKeystore"))
            {
                Debug.LogError("Survival CI Android build: no keystore at '" + keystore +
                               "'. Add the ANDROID_KEYSTORE_* repo secrets so every build is signed with the same key.");
                return 4;
            }
            else
            {
                PlayerSettings.Android.useCustomKeystore = false;
                Debug.LogWarning("Survival CI Android build: no keystore, signing with Unity's debug key (updates will not install over other builds).");
            }

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("Survival CI Android build: no enabled scenes in Build Settings.");
                return 5;
            }

            var development = args.ContainsKey("developmentBuild");
            EditorUserBuildSettings.development = development;
            var dir = Path.GetDirectoryName(output);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            Debug.Log("Survival CI Android build: " + scenes.Length + " scene(s), version " + PlayerSettings.bundleVersion +
                      " (" + PlayerSettings.Android.bundleVersionCode + "), " + (development ? "development" : "release") +
                      ", signed with " + (PlayerSettings.Android.useCustomKeystore ? "the CI key" : "the debug key") + " -> " + output);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = development ? BuildOptions.Development : BuildOptions.None
            });

            var s = report.summary;
            Debug.Log("Survival CI Android build: " + s.result + " in " + s.totalTime.TotalMinutes.ToString("0.0") + " min, " +
                      (s.totalSize / (1024 * 1024)) + " MB, " + s.totalErrors + " error(s).");
            return s.result == BuildResult.Succeeded && File.Exists(output) ? 0 : 6;
        }
    }
}
