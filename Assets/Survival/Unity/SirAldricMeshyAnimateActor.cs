using System;
using System.IO;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Survival.Unity
{
    /// <summary>
    /// Aldric World proof actor: Meshy Animate FBX humanoid + Walking clip AS-IS.
    /// Path A weight-paint is CANCELLED. Do not remap sheath / Hand_R / ghost legs.
    /// Hub PNG HOLD. Design PASS not claimed.
    /// </summary>
    public sealed class SirAldricMeshyAnimateActor : MonoBehaviour
    {
        public const string ThemePackFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/sir_aldric_meshy_animate_walk.fbx";
        public const string ClipHint = "Walking";

        private Animator? _animator;
        private PlayableGraph _graph;
        private bool _graphReady;
        private GameObject? _instance;

        public bool Built => _graphReady;

        public void Build()
        {
            BuildLights();
            var prefab = LoadFbxPrefab();
            if (prefab == null)
            {
                Debug.LogError(
                    "Meshy Animate FBX not imported yet. Open the project in Unity so " +
                    ThemePackFbx + " Humanoid-imports, then Play SirAldric.");
                return;
            }

            _instance = Instantiate(prefab, transform);
            _instance.name = "SirAldricMeshyAnimate";
            HideJunk(_instance);
            FaceWorldTop(_instance);

            _animator = _instance.GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = _instance.AddComponent<Animator>();
            }

            var clip = LoadWalkingClip();
            if (clip == null)
            {
                Debug.LogError("Meshy Animate FBX has no Walking clip after Humanoid import.");
                return;
            }

            clip.wrapMode = WrapMode.Loop;
            _graph = PlayableGraph.Create("SirAldricMeshyAnimate");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.UnscaledGameTime);
            var output = AnimationPlayableOutput.Create(_graph, "Aldric", _animator);
            var playable = AnimationClipPlayable.Create(_graph, clip);
            playable.SetDuration(clip.length);
            output.SetSourcePlayable(playable);
            _graph.Play();
            _graphReady = true;
        }

        public string PhaseLabel(float timeSeconds)
        {
            return "WALK  ·  toward TOP  ·  Meshy Animate";
        }

        private void OnDestroy()
        {
            if (_graphReady && _graph.IsValid())
            {
                _graph.Destroy();
            }
        }

        private static void HideJunk(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.IndexOf("Ico", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    t.gameObject.SetActive(false);
                }
            }
        }

        private static void FaceWorldTop(GameObject root)
        {
            // Mixamo FBX is typically Y-up / Z-forward after Unity import.
            // World gate: walk toward TOP = +Z. Leave identity if already facing +Z.
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
        }

        private static GameObject? LoadFbxPrefab()
        {
#if UNITY_EDITOR
            var data = Application.dataPath;
            var rel = "Assets/" + ThemePackFbx.Replace('\\', '/');
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(rel);
            if (go != null)
            {
                return go;
            }

            var abs = Path.Combine(Directory.GetParent(data)!.FullName, rel);
            if (File.Exists(abs))
            {
                AssetDatabase.ImportAsset(rel);
                return AssetDatabase.LoadAssetAtPath<GameObject>(rel);
            }
#endif
            return Resources.Load<GameObject>("sir_aldric_meshy_animate_walk");
        }

        private static AnimationClip? LoadWalkingClip()
        {
#if UNITY_EDITOR
            var rel = "Assets/" + ThemePackFbx.Replace('\\', '/');
            AnimationClip? walk = null;
            AnimationClip? longest = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(rel))
            {
                if (obj is not AnimationClip clip || clip.name.StartsWith("__preview", StringComparison.Ordinal))
                {
                    continue;
                }

                if (clip.name.IndexOf(ClipHint, StringComparison.OrdinalIgnoreCase) >= 0
                    || clip.name.IndexOf("Walk", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    walk = clip;
                    break;
                }

                if (longest == null || clip.length > longest.length)
                {
                    longest = clip;
                }
            }

            return walk ?? longest;
#else
            return null;
#endif
        }

        private static void BuildLights()
        {
            if (UnityEngine.Object.FindFirstObjectByType<Light>() != null)
            {
                return;
            }

            var sun = new GameObject("AldricKeyLight");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.15f;
            sun.transform.rotation = Quaternion.Euler(48f, -18f, 0f);
        }
    }
}
