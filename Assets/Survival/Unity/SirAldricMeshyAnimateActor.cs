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
    /// World LIGHT + MATERIAL punch (same FBX albedo, no rebake). Design PASS not claimed.
    /// </summary>
    public sealed class SirAldricMeshyAnimateActor : MonoBehaviour
    {
        public const string ThemePackFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/sir_aldric_meshy_animate_walk.fbx";
        public const string ClipHint = "Walking";

        private Animator? _animator;
        private PlayableGraph _graph;
        private AnimationClipPlayable _clipPlayable;
        private float _clipLength;
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
            PunchMaterials(_instance);

            _animator = _instance.GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = _instance.AddComponent<Animator>();
            }

            if (_animator.avatar == null)
            {
                _animator.avatar = LoadHumanoidAvatar();
            }

            var clip = LoadWalkingClip();
            if (clip == null)
            {
                Debug.LogError("Meshy Animate FBX has no Walking clip after Humanoid import.");
                return;
            }

            clip.wrapMode = WrapMode.Loop;
            _clipLength = clip.length;
            _graph = PlayableGraph.Create("SirAldricMeshyAnimate");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output = AnimationPlayableOutput.Create(_graph, "Aldric", _animator);
            _clipPlayable = AnimationClipPlayable.Create(_graph, clip);
            output.SetSourcePlayable(_clipPlayable);
            _graph.Play();
            _graphReady = true;
            SampleWalk(0f);
        }

        private void Update()
        {
            if (!_graphReady || _clipLength <= 0f)
            {
                return;
            }

            SampleWalk(Time.unscaledTime % _clipLength);
        }

        private void SampleWalk(float time)
        {
            if (!_graph.IsValid())
            {
                return;
            }

            _clipPlayable.SetTime(time);
            _graph.Evaluate();
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

        private static string FbxAssetPath => "Assets/" + ThemePackFbx.Replace('\\', '/');

        private static AnimationClip? LoadWalkingClip()
        {
#if UNITY_EDITOR
            var clip = PickWalkingClip(FbxAssetPath);
            if (clip != null)
            {
                return clip;
            }

            // clipAnimations.takeName must match the FBX stack. A short name like
            // "Walking" drops every clip on Humanoid import (mesh stays, clip list empty).
            if (RepairWalkingTake(FbxAssetPath))
            {
                return PickWalkingClip(FbxAssetPath);
            }

            return null;
#else
            return null;
#endif
        }

        private static Avatar? LoadHumanoidAvatar()
        {
#if UNITY_EDITOR
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(FbxAssetPath))
            {
                if (obj is Avatar avatar)
                {
                    return avatar;
                }
            }
#endif
            return null;
        }

        private static AnimationClip? PickWalkingClip(string rel)
        {
#if UNITY_EDITOR
            AnimationClip? exact = null;
            AnimationClip? walk = null;
            AnimationClip? longest = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(rel))
            {
                if (obj is not AnimationClip clip || clip.name.StartsWith("__preview", StringComparison.Ordinal))
                {
                    continue;
                }

                if (longest == null || clip.length > longest.length)
                {
                    longest = clip;
                }

                var isWalk = clip.name.IndexOf(ClipHint, StringComparison.OrdinalIgnoreCase) >= 0
                    || clip.name.IndexOf("Walk", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!isWalk)
                {
                    continue;
                }

                if (string.Equals(clip.name, ClipHint, StringComparison.OrdinalIgnoreCase))
                {
                    if (exact == null || clip.length > exact.length)
                    {
                        exact = clip;
                    }

                    continue;
                }

                // Walking.001 is a 2-frame stub. Keep the longer cycle.
                if (walk == null || clip.length > walk.length)
                {
                    walk = clip;
                }
            }

            return exact ?? walk ?? longest;
#else
            return null;
#endif
        }

        private static bool RepairWalkingTake(string rel)
        {
#if UNITY_EDITOR
            if (AssetImporter.GetAtPath(rel) is not ModelImporter importer || !importer.importAnimation)
            {
                return false;
            }

            var defaults = importer.defaultClipAnimations;
            if (defaults == null || defaults.Length == 0)
            {
                return false;
            }

            ModelImporterClipAnimation? best = null;
            var bestSpan = -1f;
            foreach (var candidate in defaults)
            {
                var label = (candidate.takeName ?? "") + "\n" + (candidate.name ?? "");
                if (label.IndexOf("Walk", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                var span = candidate.lastFrame - candidate.firstFrame;
                if (best == null || span > bestSpan)
                {
                    best = candidate;
                    bestSpan = span;
                }
            }

            if (best == null || bestSpan <= 0f)
            {
                return false;
            }

            best.name = ClipHint;
            best.loopTime = true;
            best.loop = true;
            best.keepOriginalOrientation = true;
            best.keepOriginalPositionY = true;
            best.keepOriginalPositionXZ = true;
            importer.clipAnimations = new[] { best };
            importer.SaveAndReimport();
            return true;
#else
            return false;
#endif
        }

        private static void PunchMaterials(GameObject root)
        {
            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                var shared = rend.sharedMaterials;
                if (shared == null || shared.Length == 0)
                {
                    continue;
                }

                var copies = new Material[shared.Length];
                for (var i = 0; i < shared.Length; i++)
                {
                    var src = shared[i];
                    if (src == null)
                    {
                        continue;
                    }

                    var mat = new Material(src);
                    // Same albedo map — lift value slightly so navy does not crush
                    // under World light. Do not replace the FBX base map.
                    if (mat.HasProperty("_BaseColor"))
                    {
                        var c = mat.GetColor("_BaseColor");
                        mat.SetColor("_BaseColor", new Color(
                            Mathf.Clamp01(c.r * 1.12f),
                            Mathf.Clamp01(c.g * 1.10f),
                            Mathf.Clamp01(c.b * 1.16f),
                            c.a));
                    }

                    var hasMetMap = mat.HasProperty("_MetallicGlossMap")
                        && mat.GetTexture("_MetallicGlossMap") != null;
                    if (mat.HasProperty("_Metallic") && hasMetMap)
                    {
                        mat.SetFloat("_Metallic", Mathf.Max(mat.GetFloat("_Metallic"), 0.55f));
                    }

                    if (mat.HasProperty("_Smoothness"))
                    {
                        var floor = hasMetMap ? 0.68f : 0.52f;
                        mat.SetFloat("_Smoothness", Mathf.Max(mat.GetFloat("_Smoothness"), floor));
                    }

                    copies[i] = mat;
                }

                rend.materials = copies;
            }
        }

        private static void BuildLights()
        {
            // Flat grey World + dim key was the mute Game-view. Punch in place
            // even if a leftover light already exists.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.44f, 0.48f);
            RenderSettings.ambientEquatorColor = new Color(0.28f, 0.29f, 0.30f);
            RenderSettings.ambientGroundColor = new Color(0.14f, 0.13f, 0.11f);
            RenderSettings.ambientIntensity = 1.25f;
            RenderSettings.reflectionIntensity = 1.15f;

            var key = UnityEngine.Object.FindFirstObjectByType<Light>();
            if (key == null)
            {
                var sun = new GameObject("AldricKeyLight");
                key = sun.AddComponent<Light>();
            }

            key.type = LightType.Directional;
            key.color = new Color(1f, 0.96f, 0.88f);
            key.intensity = 1.85f;
            key.transform.rotation = Quaternion.Euler(52f, -16f, 0f);

            if (UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length < 2)
            {
                var fillGo = new GameObject("AldricFillLight");
                var fill = fillGo.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.color = new Color(0.82f, 0.88f, 1f);
                fill.intensity = 0.55f;
                fillGo.transform.rotation = Quaternion.Euler(70f, 35f, 0f);
            }
        }
    }
}
