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
    /// Plays Ironhowl rest, walk, and attack on the Mixamo body.
    /// Each pose is its own FBX. A missing clip holds the T-pose.
    /// Design loose maps bind on the skinned material. The look JPEG and atlas stay QC-only.
    /// Do not bake axis conversion, do not scale one axis, do not retarget,
    /// and do not write the imported root rotation.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class IronhowlActor : MonoBehaviour
    {
        private Animator? _animator;
        private PlayableGraph _graph;
        private AnimationPlayableOutput _output;
        private AnimationClipPlayable _playable;
        private GameObject? _instance;
        private bool _ready;
        private bool _loop;
        private bool _clipMissing;
        private float _length;
        private float _time;
        private string _pose = IronhowlMotion.RestPoseName;

        public bool Built => _ready;
        public string Pose => _pose;
        public float ClipLength => _length;
        public bool ClipMissing => _clipMissing;
        public Bounds VisibleBounds { get; private set; }

        public void Build()
        {
            BuildLights();
#if UNITY_EDITOR
            var path = "Assets/" + IronhowlMotion.BodyThemePackRel;
            EnsureBodyImport(path);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError("Ironhowl rig missing. " + path);
                return;
            }

            _instance = Instantiate(prefab, transform);
            _instance.name = "IronhowlRig";
            _animator = _instance.GetComponent<Animator>() ?? _instance.AddComponent<Animator>();
            var avatar = LoadGenericAvatar(path);
            if (avatar != null && avatar.isHuman)
            {
                Debug.LogWarning("Ironhowl: ignoring a human avatar so the Mixamo pose is not retargeted.");
                avatar = null;
            }

            if (avatar != null)
            {
                _animator.avatar = avatar;
            }
            else if (_animator.avatar != null && _animator.avatar.isHuman)
            {
                _animator.avatar = null;
            }

            _animator.runtimeAnimatorController = null;
            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            WarnIfBoneMissing(IronhowlMotion.BoneRoot);
            EnableLit(_instance);
            VisibleBounds = Encapsulate(_instance);
            _graph = PlayableGraph.Create("IronhowlRig");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            _output = AnimationPlayableOutput.Create(_graph, "Ironhowl", _animator);
            _graph.Play();
            _ready = true;
            PlayPose(IronhowlMotion.RestPoseName);
            Debug.Log("Ironhowl " + _pose + ". bounds " + VisibleBounds.size);
#else
            Debug.LogError("Ironhowl plays in the Editor Game view.");
#endif
        }

        public bool PlayPose(string poseName)
        {
            if (!_ready)
            {
                return false;
            }

            if (string.Equals(poseName, IronhowlMotion.RestPoseName, System.StringComparison.Ordinal))
            {
                return PlayClipOrTpose(IronhowlMotion.RestThemePackRel, IronhowlMotion.RestPoseName, allowBareTpose: true);
            }

            if (string.Equals(poseName, IronhowlMotion.WalkPoseName, System.StringComparison.Ordinal))
            {
                return PlayClipOrTpose(IronhowlMotion.WalkThemePackRel, IronhowlMotion.WalkPoseName, allowBareTpose: false);
            }

            if (string.Equals(poseName, IronhowlMotion.AttackPoseName, System.StringComparison.Ordinal))
            {
                return PlayClipOrTpose(IronhowlMotion.AttackThemePackRel, IronhowlMotion.AttackPoseName, allowBareTpose: false);
            }

            Debug.LogWarning("Ironhowl unknown pose. " + poseName);
            return false;
        }

        private bool PlayClipOrTpose(string themePackRel, string poseName, bool allowBareTpose)
        {
#if UNITY_EDITOR
            var path = "Assets/" + themePackRel;
            if (!System.IO.File.Exists(path))
            {
                HoldTpose(poseName, clipMissing: !allowBareTpose);
                if (allowBareTpose)
                {
                    Debug.Log("Ironhowl rest is the Mixamo T-pose until " + path + " is dropped.");
                    return true;
                }

                Debug.LogWarning("Ironhowl clip missing. Holding T-pose. " + poseName + " " + path);
                return false;
            }

            var clip = LoadClip(path, poseName);
            if (clip == null)
            {
                HoldTpose(poseName, clipMissing: true);
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
            _loop = true;
            _clipMissing = false;
            _pose = poseName;
            _graph.Evaluate(0f);
            Debug.Log("Ironhowl pose " + poseName + " length " + clip.length.ToString("0.00"));
            return true;
#else
            _ = themePackRel;
            _ = poseName;
            _ = allowBareTpose;
            return false;
#endif
        }

        private void HoldTpose(string poseName, bool clipMissing)
        {
            if (_playable.IsValid())
            {
                _playable.Destroy();
            }

            _animator?.Rebind();
            _animator?.Update(0f);
            _pose = poseName;
            _loop = false;
            _length = 0f;
            _time = 0f;
            _clipMissing = clipMissing;
        }

        private void Update()
        {
            if (!_ready || !_loop || !_playable.IsValid())
            {
                return;
            }

            _time += Time.unscaledDeltaTime;
            if (_length > 0.05f)
            {
                _time %= _length;
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

#if UNITY_EDITOR
        private static AnimationClip? LoadClip(string path, string poseName)
        {
            if (!EnsureClipImport(path, poseName))
            {
                return null;
            }

            AnimationClip? named = null;
            AnimationClip? fallback = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (obj is not AnimationClip clip || clip.name.StartsWith("__preview__"))
                {
                    continue;
                }

                if (string.Equals(clip.name, poseName, System.StringComparison.Ordinal))
                {
                    named = clip;
                }

                fallback ??= clip;
            }

            var found = named ?? fallback;
            if (found == null || found.empty)
            {
                Debug.LogError("Ironhowl clip has no take. " + poseName + " " + path);
                return null;
            }

            return found;
        }

        private static Avatar? LoadGenericAvatar(string path)
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

        private static void EnsureBodyImport(string path)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            {
                return;
            }

            if (ApplyGenericImport(importer))
            {
                importer.SaveAndReimport();
            }
        }

        private static bool EnsureClipImport(string path, string poseName)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(path) as ModelImporter;
            }

            if (importer == null)
            {
                Debug.LogError("Ironhowl importer missing. " + path);
                return false;
            }

            var dirty = ApplyGenericImport(importer);
            if (!importer.importAnimation)
            {
                importer.importAnimation = true;
                dirty = true;
            }

            var pin = PinClip(importer, poseName);
            if (pin == PinResult.Missing)
            {
                return false;
            }

            if (dirty || pin == PinResult.Updated)
            {
                importer.SaveAndReimport();
            }

            return true;
        }

        private static bool ApplyGenericImport(ModelImporter importer)
        {
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

            if (importer.animationCompression != ModelImporterAnimationCompression.Off)
            {
                importer.animationCompression = ModelImporterAnimationCompression.Off;
                dirty = true;
            }

            return dirty;
        }

        /// <summary>
        /// Names the clip after the pose. Keeps the file's take and frame range.
        /// Prefers mixamo.com when that stack is present. Does not write root rotation.
        /// </summary>
        private static PinResult PinClip(ModelImporter importer, string poseName)
        {
            var defaults = importer.defaultClipAnimations;
            if (defaults == null || defaults.Length == 0)
            {
                Debug.LogError("Ironhowl clip has no take. " + poseName);
                return PinResult.Missing;
            }

            ModelImporterClipAnimation? best = null;
            var bestSpan = -1f;
            foreach (var candidate in defaults)
            {
                var take = candidate.takeName ?? string.Empty;
                if (string.Equals(take, IronhowlMotion.TakeName, System.StringComparison.OrdinalIgnoreCase))
                {
                    best = candidate;
                    break;
                }

                var span = candidate.lastFrame - candidate.firstFrame;
                if (span > bestSpan)
                {
                    best = candidate;
                    bestSpan = span;
                }
            }

            if (best == null)
            {
                return PinResult.Missing;
            }

            var takeName = best.takeName;
            if (string.Equals(takeName, IronhowlMotion.TakeName, System.StringComparison.OrdinalIgnoreCase))
            {
                takeName = IronhowlMotion.TakeName;
                best.takeName = takeName;
            }

            var first = best.firstFrame;
            var last = best.lastFrame;
            var already = importer.clipAnimations;
            if (already != null && already.Length == 1
                && string.Equals(already[0].name, poseName, System.StringComparison.Ordinal)
                && string.Equals(already[0].takeName, takeName, System.StringComparison.Ordinal)
                && Mathf.Abs(already[0].firstFrame - first) < 0.01f
                && Mathf.Abs(already[0].lastFrame - last) < 0.01f
                && already[0].loopTime
                && already[0].keepOriginalOrientation
                && already[0].keepOriginalPositionY
                && already[0].keepOriginalPositionXZ
                && !already[0].mirror)
            {
                return PinResult.Unchanged;
            }

            best.name = poseName;
            best.mirror = false;
            best.loopTime = true;
            best.loop = true;
            best.keepOriginalOrientation = true;
            best.keepOriginalPositionY = true;
            best.keepOriginalPositionXZ = true;
            importer.clipAnimations = new[] { best };
            return PinResult.Updated;
        }

        private enum PinResult
        {
            Missing,
            Unchanged,
            Updated
        }
#endif

        private void WarnIfBoneMissing(string bone)
        {
            if (_instance == null)
            {
                return;
            }

            foreach (var t in _instance.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(t.name, bone, System.StringComparison.Ordinal))
                {
                    return;
                }
            }

            Debug.LogError("Ironhowl bone missing. " + bone);
        }

        private static void EnableLit(GameObject root)
        {
            RosterPaint.Bind(
                root,
                IronhowlMotion.ThemePackDir,
                IronhowlMotion.BaseColorFileName,
                IronhowlMotion.NormalFileName,
                IronhowlMotion.MetallicFileName,
                IronhowlMotion.RoughnessFileName,
                "Ironhowl",
                preferEmbeddedBaseAndNormal: false);
        }

        private static Bounds Encapsulate(GameObject root)
        {
            var seeded = false;
            var bounds = new Bounds(root.transform.position, Vector3.one);
            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!seeded)
                {
                    bounds = rend.bounds;
                    seeded = true;
                }
                else
                {
                    bounds.Encapsulate(rend.bounds);
                }
            }

            return bounds;
        }

        private static void BuildLights()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.38f, 0.36f, 0.34f, 1f);
            var key = new GameObject("IronhowlKeyLight");
            var light = key.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.96f, 0.88f, 1f);
            light.shadows = LightShadows.Soft;
            key.transform.rotation = Quaternion.Euler(42f, 170f, 0f);
            var fillGo = new GameObject("IronhowlFillLight");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.45f;
            fill.color = new Color(0.72f, 0.7f, 0.84f, 1f);
            fillGo.transform.rotation = Quaternion.Euler(18f, -20f, 0f);
        }
    }
}
