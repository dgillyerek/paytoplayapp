using System.IO;
using Survival.Domain.Ids;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Survival.Editor
{
    /// <summary>Pins Editor Play/build to the Theme A survival entry (splash → Play). Grove Board stays in the list.</summary>
    public static class FlavorBuildSettings
    {
        private const string Splash = "Assets/Survival/Scenes/Splash.unity";
        private const string Play = "Assets/Survival/Scenes/Play.unity";
        private const string SirAldric = "Assets/Survival/Scenes/SirAldric.unity";
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
                new EditorBuildSettingsScene(SirAldric, true),
                new EditorBuildSettingsScene(GroveBoard, true)
            };
            Debug.Log("Survival flavor " + SurvIds.FlavorIdFantasyKingdomA + " → Splash then Play. Pack " + SurvIds.ThemeIdFantasyKingdomA);
        }

        [MenuItem("Survival/Sir Aldric Demo (3D Animator, high-angle rear)")]
        public static void OpenSirAldricDemo()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SirAldric);
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
