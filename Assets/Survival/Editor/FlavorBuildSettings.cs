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
                new EditorBuildSettingsScene(GroveBoard, true)
            };
            Debug.Log("Survival flavor " + SurvIds.FlavorIdFantasyKingdomA + " → Splash then Play. Pack " + SurvIds.ThemeIdFantasyKingdomA);
        }
    }
}
