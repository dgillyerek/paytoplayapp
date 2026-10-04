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
    /// Do not write the FBX instance root rotation. The bind mesh is already
    /// tall; the posed head-to-feet line is what the camera sees, and this
    /// actor turns that line to world up.
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
        /// Roots hang this far below the foot bones in the Dual Weapon Combo bind.
        /// </summary>
        private const float RootsBelowAnkleMeters = 0.31f;

        /// <summary>
        /// The bind mesh is Y-up, so measuring it reports "already up" and the
        /// posed body stays flat. After the idle clip evaluates, the head and
        /// feet are the pose the camera sees. Turn this actor (not the FBX
        /// instance root) so that spine is world up. Scale is not written.
        /// </summary>
        private bool StandLongAxisUp()
        {
            if (_instance == null || !TrySpine(out var spine))
            {
                return false;
            }

            var dot = Vector3.Dot(spine.normalized, Vector3.up);
            Debug.Log("Blightroot posed spine dot up " + dot.ToString("0.00") + " len " + spine.magnitude.ToString("0.00"));
            if (dot >= 0.85f)
            {
                return true;
            }

            var correction = Quaternion.FromToRotation(spine, Vector3.up);
            transform.rotation = correction * transform.rotation;
            Debug.Log("Blightroot turned the actor so the posed spine is up.");
            return true;
        }

        private bool TrySpine(out Vector3 spine)
        {
            spine = Vector3.up;
            if (_instance == null)
            {
                return false;
            }

            var head = FindNamed(_instance.transform, "mixamorig:HeadTop_End")
                       ?? FindNamed(_instance.transform, "mixamorig:Head");
            var left = FindNamed(_instance.transform, "mixamorig:LeftFoot");
            var right = FindNamed(_instance.transform, "mixamorig:RightFoot");
            if (head == null || left == null || right == null)
            {
                return false;
            }

            var feet = (left.position + right.position) * 0.5f;
            spine = head.position - feet;
            return spine.sqrMagnitude > 1e-6f;
        }

        private void PlantFeetOnGround()
        {
            if (_feetPlanted || _instance == null)
            {
                return;
            }

            var left = FindNamed(_instance.transform, "mixamorig:LeftFoot");
            var right = FindNamed(_instance.transform, "mixamorig:RightFoot");
            if (left == null || right == null)
            {
                return;
            }

            _feetPlanted = true;
            var ankleY = Mathf.Min(left.position.y, right.position.y);
            var soleY = ankleY - RootsBelowAnkleMeters;
            var lift = BlightrootMotion.GroundY - soleY;
            if (Mathf.Abs(lift) > 0.001f)
            {
                transform.position += Vector3.up * lift;
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
            var painted = FindPaintedTexture();
            foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skin.updateWhenOffscreen = true;
                skin.enabled = true;
                var shared = skin.sharedMaterials;
                for (var i = 0; i < shared.Length; i++)
                {
                    var mat = shared[i];
                    if (mat == null || shader == null)
                    {
                        continue;
                    }

                    if (mat.shader == null || mat.shader.name.IndexOf("InternalError", StringComparison.Ordinal) >= 0
                        || mat.shader.name.IndexOf("Universal", StringComparison.Ordinal) < 0)
                    {
                        var next = new Material(shader);
                        next.name = mat.name;
                        mat = next;
                        shared[i] = mat;
                    }

                    var tex = painted ?? ExistingAlbedo(mat);
                    if (tex != null)
                    {
                        if (mat.HasProperty("_BaseMap"))
                        {
                            mat.SetTexture("_BaseMap", tex);
                        }

                        if (mat.HasProperty("_MainTex"))
                        {
                            mat.SetTexture("_MainTex", tex);
                        }
                    }

                    // The FBX phong diffuse is 0,0,0 and there is no image in the
                    // file. A black base color stays black no matter how bright
                    // the lights are. White lets a bound texture, or the lit
                    // sculpt, read in Game view. Do not repaint a texture.
                    var white = Color.white;
                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", white);
                    }

                    mat.color = white;
                    if (mat.HasProperty("_Metallic"))
                    {
                        mat.SetFloat("_Metallic", 0f);
                    }

                    if (mat.HasProperty("_Smoothness"))
                    {
                        mat.SetFloat("_Smoothness", 0.28f);
                    }

                    if (mat.HasProperty("_Surface"))
                    {
                        mat.SetFloat("_Surface", 0f);
                    }
                }

                skin.sharedMaterials = shared;
            }

            Debug.Log("Blightroot paint " + (painted != null ? painted.name + " " + painted.width + "x" + painted.height : "no texture in the FBX, base color white"));
        }

        private static Texture2D? ExistingAlbedo(Material mat)
        {
            var tex = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") as Texture2D : null;
            if (tex != null)
            {
                return tex;
            }

            return mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") as Texture2D : null;
        }

        private static Texture2D? FindPaintedTexture()
        {
#if UNITY_EDITOR
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(AssetPath(ThemePackBody)))
            {
                if (obj is Texture2D tex && tex.width > 4 && tex.height > 4)
                {
                    return tex;
                }
            }

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/" + BlightrootMotion.ThemePackDir });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null && tex.width > 4)
                {
                    return tex;
                }
            }
#endif
            return null;
        }

        private static void BuildLights()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.52f, 0.48f, 1f);
            var key = new GameObject("BlightrootKeyLight");
            var light = key.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2.4f;
            light.color = new Color(1f, 0.96f, 0.9f, 1f);
            light.shadows = LightShadows.Soft;
            key.transform.rotation = Quaternion.Euler(42f, 170f, 0f);
            var fill = new GameObject("BlightrootFillLight");
            var fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 1.15f;
            fillLight.color = new Color(0.75f, 0.78f, 0.9f, 1f);
            fill.transform.rotation = Quaternion.Euler(18f, -20f, 0f);
        }
    }
}
