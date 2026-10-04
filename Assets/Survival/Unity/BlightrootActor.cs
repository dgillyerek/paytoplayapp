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
    /// Same mixamorig hierarchy, Generic curves, take mixamo.com. The export is
    /// about 1.65 m wide, 0.77 m deep, and 1.90 m tall once the spine is up.
    /// Do not bake axis conversion and do not scale him to the Aldric height.
    /// Do not write the FBX instance root rotation: clearing it and keeping it
    /// both left the same wide, short Game view. Measure the skinned mesh in
    /// camera space and turn this actor, which the Animator does not drive,
    /// so the long axis is world up and the head end is the top.
    /// No retarget, no bone rewrite, no mirror, no time reverse.
    /// Empty takes are rejected. HOLD merge until Derek Game-view PASS.
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
        private bool _spineAligned;
        private bool _feetPlanted;

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
            LogMeasuredPose("after Build");
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
            AlignVisiblePose();
            return true;
        }

        private void AlignVisiblePose()
        {
            if (_spineAligned || _instance == null)
            {
                return;
            }

            if (!string.Equals(_exactName, "mutant idle", StringComparison.Ordinal))
            {
                return;
            }

            LogMeasuredPose("idle frame 0");
            if (!StandLongAxisUp())
            {
                return;
            }

            _spineAligned = true;
            PlantFeetOnGround();
            LogMeasuredPose("stood up");
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
            AlignVisiblePose();
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

            if (importer.bakeAxisConversion)
            {
                importer.bakeAxisConversion = false;
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

        /// <summary>
        /// The FBX instance root rotation is left alone. Both writing identity and
        /// keeping the import left the same wide, short body, so that rotation is
        /// not the axis the camera is seeing. Bake the posed mesh, find the long
        /// axis, and point the head end at world up by rotating this actor.
        /// Scale is not written. The Mixamo export size is the size.
        /// </summary>
        private bool StandLongAxisUp()
        {
            if (_instance == null || !TryMeshTowardHead(out var towardHead, out var worldSize))
            {
                return false;
            }

            Debug.Log("Blightroot skinned world size " + Fmt(worldSize));

            if (towardHead.sqrMagnitude < 1e-8f)
            {
                return false;
            }

            var dot = Vector3.Dot(towardHead.normalized, Vector3.up);
            if (dot >= 0.5f)
            {
                Debug.Log("Blightroot mesh long axis is already up. dot " + dot.ToString("0.00"));
                return true;
            }

            var correction = Quaternion.FromToRotation(towardHead, Vector3.up);
            transform.rotation = correction * transform.rotation;
            Debug.Log("Blightroot turned the actor so the mesh long axis is up. dot was " + dot.ToString("0.00"));
            return true;
        }

        private void PlantFeetOnGround()
        {
            if (_feetPlanted || _instance == null || !TryWorldFoot(out var minY))
            {
                return;
            }

            _feetPlanted = true;
            var lift = BlightrootMotion.GroundY - minY;
            if (Mathf.Abs(lift) > 0.001f)
            {
                transform.position += Vector3.up * lift;
            }
        }

        private bool TryMeshTowardHead(out Vector3 towardHead, out Vector3 worldSize)
        {
            towardHead = Vector3.up;
            worldSize = Vector3.zero;
            if (_instance == null)
            {
                return false;
            }

            var skin = _instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var head = FindNamed(_instance.transform, "mixamorig:Head");
            if (skin == null)
            {
                return false;
            }

            var baked = new Mesh();
            skin.BakeMesh(baked);
            var localBounds = baked.bounds;
            var size = localBounds.size;
            Destroy(baked);
            if (size.sqrMagnitude < 1e-8f)
            {
                return FallbackSpine(skin, head, out towardHead, out worldSize);
            }

            var axis = 0;
            var longest = size.x;
            if (size.y > longest)
            {
                longest = size.y;
                axis = 1;
            }

            if (size.z > longest)
            {
                axis = 2;
            }

            var local = axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
            if (head != null)
            {
                var localHead = skin.transform.InverseTransformPoint(head.position);
                var along = axis == 0
                    ? localHead.x - localBounds.center.x
                    : axis == 1
                        ? localHead.y - localBounds.center.y
                        : localHead.z - localBounds.center.z;
                if (along < 0f)
                {
                    local = -local;
                }
            }

            towardHead = skin.transform.TransformVector(local);
            worldSize = WorldSize(skin, localBounds);
            return true;
        }

        private bool FallbackSpine(SkinnedMeshRenderer skin, Transform? head, out Vector3 towardHead, out Vector3 worldSize)
        {
            worldSize = skin.bounds.size;
            towardHead = Vector3.up;
            var left = _instance != null ? FindNamed(_instance.transform, "mixamorig:LeftFoot") : null;
            var right = _instance != null ? FindNamed(_instance.transform, "mixamorig:RightFoot") : null;
            if (head == null || left == null || right == null)
            {
                return worldSize.sqrMagnitude > 1e-8f;
            }

            var feet = (left.position + right.position) * 0.5f;
            towardHead = head.position - feet;
            return towardHead.sqrMagnitude > 1e-8f;
        }

        private bool TryWorldFoot(out float minY)
        {
            minY = 0f;
            if (_instance == null)
            {
                return false;
            }

            var skin = _instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skin == null)
            {
                return false;
            }

            var baked = new Mesh();
            skin.BakeMesh(baked);
            var localBounds = baked.bounds;
            Destroy(baked);
            if (localBounds.size.sqrMagnitude < 1e-8f)
            {
                minY = skin.bounds.min.y;
                return skin.bounds.size.sqrMagnitude > 1e-8f;
            }

            minY = WorldMinY(skin, localBounds);
            return true;
        }

        private static Vector3 WorldSize(SkinnedMeshRenderer skin, Bounds localBounds)
        {
            var min = Vector3.one * float.PositiveInfinity;
            var max = Vector3.one * float.NegativeInfinity;
            EncapsulateWorld(skin, localBounds, ref min, ref max);
            return max - min;
        }

        private static float WorldMinY(SkinnedMeshRenderer skin, Bounds localBounds)
        {
            var min = Vector3.one * float.PositiveInfinity;
            var max = Vector3.one * float.NegativeInfinity;
            EncapsulateWorld(skin, localBounds, ref min, ref max);
            return min.y;
        }

        private static void EncapsulateWorld(SkinnedMeshRenderer skin, Bounds localBounds, ref Vector3 min, ref Vector3 max)
        {
            var center = localBounds.center;
            var ext = localBounds.extents;
            for (var i = 0; i < 8; i++)
            {
                var corner = center;
                corner.x += (i & 1) == 0 ? -ext.x : ext.x;
                corner.y += (i & 2) == 0 ? -ext.y : ext.y;
                corner.z += (i & 4) == 0 ? -ext.z : ext.z;
                var world = skin.transform.TransformPoint(corner);
                min = Vector3.Min(min, world);
                max = Vector3.Max(max, world);
            }
        }

        private void LogMeasuredPose(string when)
        {
            if (_instance == null)
            {
                return;
            }

            var skin = _instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var mesh = skin != null ? skin.transform : null;
            var hips = FindNamed(_instance.transform, BlightrootMotion.BoneRoot);
            var bounds = skin != null ? skin.bounds.size : Vector3.zero;
            Debug.Log(
                "Blightroot pose " + when +
                " rendererBounds " + Fmt(bounds) +
                " instanceRot " + Euler(_instance.transform) +
                " instanceScale " + Fmt(_instance.transform.localScale) +
                " meshRot " + (mesh != null ? Euler(mesh) : "none") +
                " meshScale " + (mesh != null ? Fmt(mesh.localScale) : "none") +
                " hipsRot " + (hips != null ? Euler(hips) : "none") +
                " hipsScale " + (hips != null ? Fmt(hips.localScale) : "none"));
        }

        private static string Euler(Transform t)
        {
            var e = t.localEulerAngles;
            return e.x.ToString("0.0") + "," + e.y.ToString("0.0") + "," + e.z.ToString("0.0");
        }

        private static string Fmt(Vector3 v) =>
            v.x.ToString("0.000") + "," + v.y.ToString("0.000") + "," + v.z.ToString("0.000");

        private static Transform? FindNamed(Transform root, string name)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < all.Length; i++)
            {
                if (string.Equals(all[i].name, name, StringComparison.Ordinal))
                {
                    return all[i];
                }
            }

            return null;
        }

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
