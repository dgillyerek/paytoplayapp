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
    /// Headless Android build for GitHub Actions (.github/workflows/android-build.yml, game-ci/unity-builder
    /// <c>buildMethod: Survival.Editor.CiAndroidBuild.Build</c>). Applies the store flavor through
    /// <see cref="FlavorBuildSettings.TryApplyFlavor"/> (product name, company, bundle id, scenes, icons, IL2CPP, ARM64,
    /// target API), bakes and verifies the player assets, then builds a signed Play Store .aab (or a phone-install .apk),
    /// and exits with a non-zero code on any failure so CI goes red.
    /// </summary>
    /// <remarks>
    /// Arguments (game-ci passes the first group itself): -customBuildPath, -buildVersion, -androidVersionCode,
    /// -androidExportType (androidAppBundle = .aab, androidPackage = .apk), -androidKeystoreName (relative to the project
    /// root), -androidKeystorePass, -androidKeyaliasName, -androidKeyaliasPass. Extra: -flavor (default fantasy_kingdom_a),
    /// -requireKeystore (fail rather than sign with Unity's debug key), -developmentBuild.
    /// Each signing value falls back to the environment variable of the same name in upper snake case
    /// (ANDROID_KEYSTORE_NAME, ANDROID_KEYSTORE_PASS, ANDROID_KEYALIAS_NAME, ANDROID_KEYALIAS_PASS), so a local command-line
    /// build can sign without putting a password on the command line. Nothing secret is written to ProjectSettings: the
    /// keystore settings are cleared again after the build.
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

        private static string ArgOrEnv(Dictionary<string, string> args, string name, string env)
        {
            var v = Arg(args, name);
            return v.Length > 0 ? v : (Environment.GetEnvironmentVariable(env) ?? "").Trim();
        }

        private static int Run(Dictionary<string, string> args)
        {
            try
            {
                return RunBuild(args);
            }
            finally
            {
                // Keep the signing key out of ProjectSettings if the editor saves after this (local runs).
                PlayerSettings.Android.useCustomKeystore = false;
                PlayerSettings.Android.keystoreName = "";
                PlayerSettings.Android.keystorePass = "";
                PlayerSettings.Android.keyaliasName = "";
                PlayerSettings.Android.keyaliasPass = "";
            }
        }

        private static int RunBuild(Dictionary<string, string> args)
        {
            var exportType = Arg(args, "androidExportType");
            var appBundle = exportType.Length == 0 || exportType == "androidAppBundle";
            if (!appBundle && exportType != "androidPackage")
            {
                Debug.LogError("Survival CI Android build: unsupported -androidExportType '" + exportType + "' (androidAppBundle or androidPackage).");
                return 7;
            }

            var extension = appBundle ? ".aab" : ".apk";
            var output = Arg(args, "customBuildPath");
            if (output.Length == 0)
            {
                output = appBundle ? Path.ChangeExtension(PlayerBuildMenu.AndroidApkPath, ".aab") : PlayerBuildMenu.AndroidApkPath;
            }

            if (!output.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                output += extension;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("Survival CI Android build: could not switch to Android (is Android Build Support in this editor image?).");
                return 2;
            }

            var flavor = Arg(args, "flavor");
            if (flavor.Length == 0)
            {
                flavor = FlavorBuildSettings.ThemeAFlavorId;
            }

            if (!FlavorBuildSettings.TryApplyFlavor(flavor))
            {
                Debug.LogError("Survival CI Android build: unknown -flavor '" + flavor + "'.");
                return 8;
            }

            var iconProblems = FlavorBuildSettings.MissingAndroidIcons();
            if (iconProblems.Count > 0)
            {
                Debug.LogError("Survival CI Android build: Android icon slots without art:\n" + string.Join("\n", iconProblems));
                return 9;
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

            // The flavor already set these; set again so nothing between there and here (bake) can change them.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.targetSdkVersion = FlavorBuildSettings.PlayTargetSdk;
            EditorUserBuildSettings.buildAppBundle = appBundle;
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

            var keystore = ArgOrEnv(args, "androidKeystoreName", "ANDROID_KEYSTORE_NAME");
            if (keystore.Length > 0 && File.Exists(keystore))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = Path.GetFullPath(keystore);
                PlayerSettings.Android.keystorePass = ArgOrEnv(args, "androidKeystorePass", "ANDROID_KEYSTORE_PASS");
                PlayerSettings.Android.keyaliasName = ArgOrEnv(args, "androidKeyaliasName", "ANDROID_KEYALIAS_NAME");
                PlayerSettings.Android.keyaliasPass = ArgOrEnv(args, "androidKeyaliasPass", "ANDROID_KEYALIAS_PASS");
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
            if (development && appBundle)
            {
                Debug.LogError("Survival CI Android build: a development build cannot go to Google Play; use -androidExportType androidPackage.");
                return 7;
            }

            var bundleId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            EditorUserBuildSettings.development = development;
            var dir = Path.GetDirectoryName(output);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            Debug.Log("Survival CI Android build: " + PlayerSettings.productName + " (" + bundleId + "), " + scenes.Length + " scene(s), version " + PlayerSettings.bundleVersion +
                      " (" + PlayerSettings.Android.bundleVersionCode + "), " + (development ? "development" : "release") +
                      " " + (appBundle ? "AAB" : "APK") + ", target API " + (int)PlayerSettings.Android.targetSdkVersion +
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
