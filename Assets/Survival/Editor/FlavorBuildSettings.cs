using System.IO;
using Survival.Domain.Ids;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Survival.Editor
{
    /// <summary>Pins Editor Play/build to the Theme A survival entry (splash → Play) plus the Locked Battle. Grove Board stays in the list.
    /// The one-character demo scenes were removed 2026-10-19; their characters play in the Locked Battle.</summary>
    public static class FlavorBuildSettings
    {
        private const string Splash = "Assets/Survival/Scenes/Splash.unity";
        private const string Play = "Assets/Survival/Scenes/Play.unity";
        private const string BattleLocked = "Assets/Survival/Scenes/BattleLocked.unity";
        private const string Runner = Survival.Unity.RunnerLevelView.ScenePath;
        private const string GroveBoard = "Assets/Grove/Scenes/Board.unity";

        [MenuItem("Survival/Use Theme A Flavor (fantasy_kingdom_a)")]
        public static void UseThemeA()
        {
            PlayerSettings.productName = "Kingdom Defense";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.paytoplay.fantasykingdoma");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.paytoplay.fantasykingdoma");
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(Splash, true),
                new EditorBuildSettingsScene(Play, true),
                new EditorBuildSettingsScene(BattleLocked, true),
                new EditorBuildSettingsScene(Runner, true),
                new EditorBuildSettingsScene(GroveBoard, true)
            };
            Debug.Log("Survival flavor " + SurvIds.FlavorIdFantasyKingdomA + " → Splash then Play. Pack " + SurvIds.ThemeIdFantasyKingdomA);
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
