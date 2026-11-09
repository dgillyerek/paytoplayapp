using System.IO;
using System.Linq;
using Survival.Domain.Ids;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Survival.Editor
{
    /// <summary>Pins Editor Play/build to the Theme A survival entry (splash → Play) plus the Locked Battle.
    /// The one-character demo scenes were removed 2026-10-19; their characters play in the Locked Battle.</summary>
    public static class FlavorBuildSettings
    {
        private const string Splash = "Assets/Survival/Scenes/Splash.unity";
        private const string Play = "Assets/Survival/Scenes/Play.unity";
        private const string BattleLocked = "Assets/Survival/Scenes/BattleLocked.unity";
        private const string Runner = Survival.Unity.RunnerLevelView.ScenePath;

        /// <summary>Theme A store identity. Must match Assets/AppFlavor/fantasy_kingdom_a/flavor.json (guarded by a test).</summary>
        public const string ThemeAFlavorId = "fantasy_kingdom_a";
        public const string ThemeAProductName = "Kingdom Defense";
        public const string ThemeACompanyName = "PayToPlay";
        public const string ThemeABundleId = "com.paytoplay.fantasykingdoma";

        /// <summary>Google Play has required new apps and updates to target Android 16 (API 36) since 2026-08-31.</summary>
        public const AndroidSdkVersions PlayTargetSdk = AndroidSdkVersions.AndroidApiLevel36;

        private const string ThemeAStoreArt = "Assets/ThemePack/fantasy_kingdom_a/art/store/";
        public const string ThemeAIcon = ThemeAStoreArt + "flavor_icon.png";
        public const string ThemeAIconRound = ThemeAStoreArt + "flavor_icon_round.png";
        public const string ThemeAIconAdaptiveBackground = ThemeAStoreArt + "flavor_icon_adaptive_background.png";
        public const string ThemeAIconAdaptiveForeground = ThemeAStoreArt + "flavor_icon_adaptive_foreground.png";

        [MenuItem("Survival/Use Theme A Flavor (fantasy_kingdom_a)")]
        public static void UseThemeA()
        {
            ApplyThemeA();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Applies a store flavor by id. The CI build (CiAndroidBuild, <c>-flavor</c>) goes through here so a cloud build
        /// is the same app as the menu. Only Theme A exists today; an unknown id returns false.
        /// </summary>
        public static bool TryApplyFlavor(string flavorId)
        {
            if (flavorId == ThemeAFlavorId || flavorId == SurvIds.FlavorIdFantasyKingdomA)
            {
                ApplyThemeA();
                return true;
            }

            return false;
        }

        /// <summary>Product name, company, bundle id, scenes, icons and the Android release basics for Theme A.</summary>
        public static void ApplyThemeA()
        {
            PlayerSettings.productName = ThemeAProductName;
            PlayerSettings.companyName = ThemeACompanyName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ThemeABundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, ThemeABundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, ThemeABundleId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.targetSdkVersion = PlayTargetSdk;
            ApplyThemeAIcons();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(Splash, true),
                new EditorBuildSettingsScene(Play, true),
                new EditorBuildSettingsScene(BattleLocked, true),
                new EditorBuildSettingsScene(Runner, true)
            };
            Debug.Log("Survival flavor " + SurvIds.FlavorIdFantasyKingdomA + " → Splash then Play. Pack " + SurvIds.ThemeIdFantasyKingdomA);
        }

        /// <summary>
        /// Default icon from the locked Direction-5 store icon. The Android legacy, round and adaptive slots are committed
        /// in ProjectSettings; this fills any that are empty (e.g. after a settings reset) from the same art.
        /// </summary>
        private static void ApplyThemeAIcons()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(ThemeAIcon);
            var round = AssetDatabase.LoadAssetAtPath<Texture2D>(ThemeAIconRound);
            var background = AssetDatabase.LoadAssetAtPath<Texture2D>(ThemeAIconAdaptiveBackground);
            var foreground = AssetDatabase.LoadAssetAtPath<Texture2D>(ThemeAIconAdaptiveForeground);
            if (icon == null || round == null || background == null || foreground == null)
            {
                Debug.LogError("Theme A icon art missing under " + ThemeAStoreArt);
                return;
            }

            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            foreach (var kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
            {
                var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
                var changed = false;
                foreach (var slot in icons)
                {
                    if (slot.GetTextures().Any(t => t != null))
                    {
                        continue;
                    }

                    if (slot.maxLayerCount >= 2)
                    {
                        slot.SetTextures(background, foreground);
                    }
                    else
                    {
                        slot.SetTexture(kind.ToString().IndexOf("Round", System.StringComparison.OrdinalIgnoreCase) >= 0 ? round : icon);
                    }

                    changed = true;
                }

                if (changed)
                {
                    PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
                }
            }
        }

        /// <summary>Empty when every Android icon slot has art; otherwise one line per empty slot (the CI build fails on any).</summary>
        public static System.Collections.Generic.List<string> MissingAndroidIcons()
        {
            var missing = new System.Collections.Generic.List<string>();
            foreach (var kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
            {
                foreach (var slot in PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind))
                {
                    var textures = slot.GetTextures();
                    if (textures.Length < slot.minLayerCount || textures.Take(System.Math.Max(1, slot.minLayerCount)).Any(t => t == null))
                    {
                        missing.Add(kind + " " + slot.width + "x" + slot.height);
                    }
                }
            }

            return missing;
        }

        /// <summary>World + quest hook slice: Play scene (skips splash) with Castle / Farm / Lumber Mill, quests, gifts, Gem Shop.</summary>
        [MenuItem("Survival/Play: World + Quest Hook (Game view 1080x1920)")]
        public static void OpenWorldQuestHook()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(Play);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Survival/Locked Battle (heroes vs villains, Game view 1080x1920)")]
        public static void OpenLockedBattleDemo()
        {
            // Single mode closes every other loaded scene, so Game view only shows the roster fighters.
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                BattleLocked,
                UnityEditor.SceneManagement.OpenSceneMode.Single);
        }

        /// <summary>
        /// Runner mini-game on its own (RunnerLevel scene, Play pressed): steer the squad with drag or A / D, shoot the bonus
        /// boxes down for followers and upgrades, beat Ironhowl at the end. Also reachable from Play's Runner button, which
        /// needs the scene in Build Settings; this entry adds it when missing.
        /// </summary>
        [MenuItem("Survival/Runner: Walk & Fight")]
        public static void OpenRunner()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureInBuild(Runner);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(Runner, UnityEditor.SceneManagement.OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        private static void EnsureInBuild(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes)
            {
                if (s.path == path)
                {
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        [MenuItem("Survival/Import locked Sir Aldric rear master")]
        public static void ImportLockedRearMaster()
        {
            var repo = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var drop = Path.Combine(repo, "design", "survival-theme-a-fantasy", "heroes", "anim", "sir_aldric", "UNITY_DROP_LOCKED");
            var dest = Path.Combine(Application.dataPath, "ThemePack", "fantasy_kingdom_a", "art", "heroes");
            Directory.CreateDirectory(dest);
            var names = new[] { "SIR_ALDRIC_REAR_MASTER_LOCKED.png", "SIR_ALDRIC_REAR_MASTER_LOCKED_512.png" };
            foreach (var name in names)
            {
                var from = Path.Combine(drop, name);
                if (!File.Exists(from))
                {
                    Debug.LogError("Missing locked drop " + from);
                    continue;
                }

                File.Copy(from, Path.Combine(dest, name), overwrite: true);
            }

            AssetDatabase.Refresh();
            Debug.Log("Imported squared-up Sir Aldric rear master from UNITY_DROP_LOCKED.");
        }
    }
}
