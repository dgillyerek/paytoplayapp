using System;
using Survival.Domain.Enemies;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Survival.Unity
{
    /// <summary>
    /// Plays creature-pack takes on the Dual Weapon Combo skinned body.
    /// Same mixamorig hierarchy, Generic curves, take mixamo.com. No retarget,
    /// no bone rewrite, no mirror, no time reverse. Empty takes are rejected.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class BlightrootActor : MonoBehaviour
    {
        public const string ThemePackBody = BlightrootMotion.BodyThemePackRel;

        private Animator? _animator;
        private PlayableGraph _graph;
        private AnimationPlayableOutput _output;
        private AnimationClipPlayable _playable;
        private GameObject? _instance;
        private bool _ready;
        private bool _loop;
        private float _length;
        private float _time;
        private string? _exactName;

        public bool Built => _ready;
        public string? ExactName => _exactName;
        public float ClipLength => _length;

        public void Build()
        {
            BuildLights();
#if UNITY_EDITOR
            var prefab = LoadBodyPrefab();
            if (prefab == null)
            {
                Debug.LogError("Blightroot skinned body missing. " + AssetPath(ThemePackBody));
                return;
            }

            _instance = Instantiate(prefab, transform);
            _instance.name = "BlightrootDualWeaponCombo";
            EnableSkin(_instance);
            _animator = _instance.GetComponent<Animator>() ?? _instance.AddComponent<Animator>();
            var avatar = LoadAvatar(AssetPath(ThemePackBody));
            if (avatar != null && avatar.isHuman)
            {
                Debug.LogWarning("Blightroot: ignoring a human avatar so the mixamorig curves are not retargeted.");
                avatar = null;
            }

            if (avatar != null)
            {
                _animator.avatar = avatar;
            }

            _animator.runtimeAnimatorController = null;
            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _animator.updateMode = AnimatorUpdateMode.UnscaledTime;

            _graph = PlayableGraph.Create("BlightrootCreaturePack");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            _output = AnimationPlayableOutput.Create(_graph, "Blightroot", _animator);
            _graph.Play();
            _ready = true;
#else
            Debug.LogError("Blightroot creature pack plays in the Editor Game view.");
#endif
        }

        public bool PlayNamedClip(string exactName)
        {
            if (!_ready || _animator == null)
            {
                Debug.LogWarning("Blightroot PlayNamedClip before build. " + exactName);
                return false;
            }

            if (!BlightrootMotion.TryGet(exactName, out var entry))
            {
                Debug.LogWarning("Blightroot unknown Mixamo name. " + exactName);
                return false;
            }

            var clip = LoadClip(entry);
            if (clip == null || clip.empty || BlightrootMotion.IsEmptyClip(clip.length))
            {
                Debug.LogError(
                    "Blightroot rejected empty clip. exactName=" + entry.ExactName +
                    " take=" + BlightrootMotion.TakeName +
                    " file=" + entry.FileName);
                _exactName = null;
                return false;
            }

            if (_playable.IsValid())
            {
                _playable.Destroy();
            }

            _playable = AnimationClipPlayable.Create(_graph, clip);
            _output.SetSourcePlayable(_playable);

            _playable.SetDuration(clip.length);
            _playable.SetTime(0d);
            _playable.SetSpeed(1d);
            _length = clip.length;
            _time = 0f;
            _loop = entry.Loop;
            _exactName = entry.ExactName;
            _graph.Evaluate(0f);
            return true;
        }

        private void Update()
        {
            if (!_ready || !_playable.IsValid() || _exactName == null)
            {
                return;
            }

            _time += Time.unscaledDeltaTime;
            if (_loop && _length > BlightrootMotion.MinClipSeconds)
            {
                _time %= _length;
            }
            else if (_time > _length)
            {
                _time = _length;
            }

            _playable.SetTime(_time);
            _graph.Evaluate(0f);
        }

        private void OnDestroy()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }

        private static string AssetPath(string themePackRel) => "Assets/" + themePackRel.Replace('\\', '/');

        private GameObject? LoadBodyPrefab()
        {
#if UNITY_EDITOR
            var path = AssetPath(ThemePackBody);
            EnsureBodyImport(path);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
#else
            return null;
#endif
        }

        private AnimationClip? LoadClip(BlightrootMotion.Clip entry)
        {
#if UNITY_EDITOR
            var path = AssetPath(entry.ThemePackRel);
            var clip = PickClip(path, entry.ExactName);
            if (clip != null && !clip.empty && !BlightrootMotion.IsEmptyClip(clip.length))
            {
                return clip;
            }

            if (!RepairTake(path, entry))
            {
                return null;
            }

            clip = PickClip(path, entry.ExactName);
            if (clip == null || clip.empty || BlightrootMotion.IsEmptyClip(clip.length))
            {
                Debug.LogError("Blightroot take still empty after repair. " + entry.ExactName + " " + path);
                return null;
            }

            return clip;
#else
            return null;
#endif
        }

#if UNITY_EDITOR
        private static AnimationClip? PickClip(string path, string exactName)
        {
            AnimationClip? exact = null;
            AnimationClip? take = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (obj is not AnimationClip clip || clip.name.StartsWith("__preview", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(clip.name, BlightrootMotion.RejectedEmptyTakeName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(clip.name, exactName, StringComparison.Ordinal))
                {
                    exact = clip;
                    continue;
                }

                if (string.Equals(clip.name, BlightrootMotion.TakeName, StringComparison.Ordinal))
                {
                    take = clip;
                }
            }

            return exact ?? take;
        }

        private static bool RepairTake(string path, BlightrootMotion.Clip entry)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            {
                Debug.LogError("Blightroot importer missing. " + path);
                return false;
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
            importer.importAnimation = true;
            importer.useFileScale = false;
            importer.globalScale = 1f;
            importer.optimizeBones = false;
            importer.bakeAxisConversion = false;
            var defaults = importer.defaultClipAnimations;
            if (defaults == null || defaults.Length == 0)
            {
                Debug.LogError("Blightroot defaultClipAnimations empty. " + entry.ExactName);
                return false;
            }

            ModelImporterClipAnimation? best = null;
            var bestSpan = -1f;
            foreach (var candidate in defaults)
            {
                var span = candidate.lastFrame - candidate.firstFrame;
                var take = candidate.takeName ?? string.Empty;
                if (string.Equals(take, BlightrootMotion.RejectedEmptyTakeName, StringComparison.Ordinal) && span < 1f)
                {
                    continue;
                }

                var prefer = string.Equals(take, BlightrootMotion.TakeName, StringComparison.OrdinalIgnoreCase);
                if (prefer && span > 1f)
                {
                    best = candidate;
                    bestSpan = span;
                    break;
                }

                if (span > bestSpan)
                {
                    best = candidate;
                    bestSpan = span;
                }
            }

            if (best == null || bestSpan < 1f)
            {
                Debug.LogError(
                    "Blightroot rejected empty take. Wanted " + BlightrootMotion.TakeName +
                    " for " + entry.ExactName);
                return false;
            }

            best.name = entry.ExactName;
            if (string.IsNullOrEmpty(best.takeName)
                || string.Equals(best.takeName, BlightrootMotion.TakeName, StringComparison.OrdinalIgnoreCase))
            {
                best.takeName = BlightrootMotion.TakeName;
            }

            best.mirror = false;
            best.loopTime = entry.Loop;
            best.loop = entry.Loop;
            best.keepOriginalOrientation = true;
            best.keepOriginalPositionY = true;
            best.keepOriginalPositionXZ = true;
            best.heightFromFeet = false;
            importer.clipAnimations = new[] { best };
            importer.SaveAndReimport();
            Debug.Log("Blightroot repair " + entry.ExactName + " takeName=" + best.takeName + " span=" + bestSpan);
            return true;
        }

        private static void EnsureBodyImport(string path)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            {
                return;
            }

            var dirty = false;
            if (importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                dirty = true;
            }

            if (importer.useFileScale)
            {
                importer.useFileScale = false;
                dirty = true;
            }

            if (Mathf.Abs(importer.globalScale - 1f) > 0.001f)
            {
                importer.globalScale = 1f;
                dirty = true;
            }

            if (importer.optimizeBones)
            {
                importer.optimizeBones = false;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }

        private static Avatar? LoadAvatar(string path)
        {
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (obj is Avatar avatar && !avatar.isHuman)
                {
                    return avatar;
                }
            }

            return null;
        }
#endif

        private static void EnableSkin(GameObject root)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skin.updateWhenOffscreen = true;
                skin.enabled = true;
                if (shader == null)
                {
                    continue;
                }

                var shared = skin.sharedMaterials;
                for (var i = 0; i < shared.Length; i++)
                {
                    var mat = shared[i];
                    if (mat != null && mat.shader != null && mat.shader.name.IndexOf("InternalError", StringComparison.Ordinal) < 0
                        && mat.shader.name.IndexOf("Universal", StringComparison.Ordinal) >= 0)
                    {
                        continue;
                    }

                    var next = new Material(shader);
                    if (next.HasProperty("_BaseColor"))
                    {
                        next.SetColor("_BaseColor", new Color(0.42f, 0.48f, 0.28f, 1f));
                    }

                    next.color = new Color(0.42f, 0.48f, 0.28f, 1f);
                    shared[i] = next;
                }

                skin.sharedMaterials = shared;
            }
        }

        private static void BuildLights()
        {
            var key = new GameObject("BlightrootKeyLight");
            var light = key.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.96f, 0.88f, 1f);
            key.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            var fill = new GameObject("BlightrootFillLight");
            var fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.35f;
            fillLight.color = new Color(0.65f, 0.75f, 0.85f, 1f);
            fill.transform.rotation = Quaternion.Euler(20f, 150f, 0f);
        }
    }
}
