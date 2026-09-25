using System.Collections;
using Survival.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Survival.Editor
{
    /// <summary>Play SirAldric and Camera.Render the authored Humanoid draw→strike (Game view).</summary>
    public static class SirAldricGameViewCaptureMenu
    {
        private const string Scene = "Assets/Survival/Scenes/SirAldric.unity";

        [MenuItem("Survival/Capture Sir Aldric Humanoid Attack (Game view)")]
        public static void Capture()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.OpenScene(Scene);
                EditorApplication.playModeStateChanged -= OnPlay;
                EditorApplication.playModeStateChanged += OnPlay;
                EditorApplication.isPlaying = true;
                return;
            }

            EditorApplication.delayCall += () => CaptureNow();
        }

        private static void OnPlay(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            EditorApplication.playModeStateChanged -= OnPlay;
            EditorApplication.delayCall += () =>
            {
                var host = Object.FindFirstObjectByType<SirAldricDemo>();
                if (host != null)
                {
                    host.StartCoroutine(Run());
                }
                else
                {
                    CaptureNow();
                }
            };
        }

        private static IEnumerator Run()
        {
            yield return null;
            yield return null;
            CaptureNow();
            EditorApplication.isPlaying = false;
        }

        private static void CaptureNow()
        {
            var actor = Object.FindFirstObjectByType<SirAldricMeshyAnimateActor>();
            var cam = Camera.main;
            if (actor == null || cam == null)
            {
                Debug.LogError("SirAldric Game-view capture: actor or Camera.main missing. Play SirAldric first.");
                return;
            }

            if (!actor.Built)
            {
                actor.Build();
            }

            var dir = SirAldricGameViewCapture.CaptureAttack(actor, cam);
            Debug.Log("Wrote Unity Game-view attack proofs to " + dir);
        }
    }
}
