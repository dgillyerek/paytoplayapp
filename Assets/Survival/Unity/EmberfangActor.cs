using Survival.Domain.Heroes;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Survival.Unity
{
    /// <summary>
    /// Plays the Design Blender dragon: rest bind pose, the 1s wing flap, and the walk ExtraClip.
    /// Custom bone names. Rejects a Mixamo EMBERFANG_rig.fbx.
    /// Metallic and roughness always bind. Base and normal stay embedded when import keeps them;
    /// otherwise the loose PNGs bind.
    /// Do not bake axis conversion, do not scale one axis, do not retarget,
    /// and do not write the imported root rotation or edit bones.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class EmberfangActor : MonoBehaviour
    {
        private Animator? _animator;
        private PlayableGraph _graph;
        private AnimationPlayableOutput _output;
        private AnimationClipPlayable _playable;
        private GameObject? _instance;
        private bool _ready;
        private bool _loop;
        private float _length;
        private float _time;
        private string _pose = EmberfangMotion.RestPoseName;

        public bool Built => _ready;
        public string Pose => _pose;
        public float ClipLength => _length;
        public Bounds VisibleBounds { get; private set; }

        public void Build()
        {
            BuildLights();
#if UNITY_EDITOR
            var path = "Assets/" + EmberfangMotion.RestThemePackRel;
            if (path.EndsWith(EmberfangMotion.RejectedMixamoFileName, System.StringComparison.Ordinal))
            {
                Debug.LogError("Emberfang rejected Mixamo file " + EmberfangMotion.RejectedMixamoFileName);
                return;
            }

            EnsureImport(path, poseName: null, takeName: null);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError("Emberfang dragon rig missing. " + path);
                return;
            }

            _instance = Instantiate(prefab, transform);
            _instance.name = "EmberfangDragonRig";
            _animator = _instance.GetComponent<Animator>() ?? _instance.AddComponent<Animator>();
            var avatar = LoadGenericAvatar(path);
            if (avatar != null && avatar.isHuman)
            {
                Debug.LogWarning("Emberfang: ignoring a human avatar. This rig uses the Design bone names.");
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
            WarnIfBoneMissing(EmberfangMotion.BoneRoot);
            WarnIfBoneMissing(EmberfangMotion.BoneWing);
            EnableLit(_instance);
            VisibleBounds = Encapsulate(_instance);
            _graph = PlayableGraph.Create("EmberfangDragonRig");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            _output = AnimationPlayableOutput.Create(_graph, "Emberfang", _animator);
            _graph.Play();
            _ready = true;
            ShowRest();
#else
            Debug.LogError("Emberfang plays in the Editor Game view.");
#endif
        }

        public bool PlayPose(string poseName)
        {
            if (!_ready)
            {
                return false;
            }

            if (string.Equals(poseName, EmberfangMotion.RestPoseName, System.StringComparison.Ordinal))
            {
                ShowRest();
                return true;
            }

            if (string.Equals(poseName, EmberfangMotion.FlapPoseName, System.StringComparison.Ordinal))
            {
                return PlayClip(
                    EmberfangMotion.FlapFileName,
                    EmberfangMotion.FlapPoseName,
                    EmberfangMotion.FlapTakeName,
                    EmberfangMotion.FlapLastFrame);
            }

            foreach (var extra in EmberfangMotion.ExtraClips)
            {
                if (string.Equals(poseName, extra.PoseName, System.StringComparison.Ordinal))
                {
                    return PlayClip(extra.FileName, extra.PoseName, extra.TakeName, extra.LastFrame);
                }
            }

            Debug.LogWarning("Emberfang unknown pose. " + poseName);
            return false;
        }

        private void ShowRest()
        {
            if (_playable.IsValid())
            {
                _playable.Destroy();
            }

            _animator?.Rebind();
            _animator?.Update(0f);
            _pose = EmberfangMotion.RestPoseName;
            _loop = false;
            _length = 0f;
            _time = 0f;
        }

        private bool PlayClip(string fileName, string poseName, string takeName, int lastFrame)
        {
#if UNITY_EDITOR
            var clip = LoadClip(fileName, poseName, takeName, lastFrame);
            if (clip == null || clip.empty || clip.length < 0.2f)
            {
                Debug.LogError(
                    "Emberfang clip missing. take=" + takeName +
                    " file=" + fileName);
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
            _pose = poseName;
            _graph.Evaluate(0f);
            return true;
#else
            return false;
#endif
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
        private static AnimationClip? LoadClip(string fileName, string poseName, string takeName, int lastFrame)
        {
            var path = "Assets/" + EmberfangMotion.ThemePackDir + "/" + fileName;
            EnsureImport(path, poseName, takeName, lastFrame);
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
                    continue;
                }

                if (string.Equals(clip.name, "mixamo.com", System.StringComparison.Ordinal))
                {
                    continue;
                }

                fallback ??= clip;
            }

            return named ?? fallback;
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

        private static void EnsureImport(string path, string? poseName, string? takeName = null, int lastFrame = 0)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            {
                Debug.LogError("Emberfang importer missing. " + path);
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

            if (!string.IsNullOrEmpty(poseName))
            {
                dirty |= PinClip(importer, poseName, takeName ?? string.Empty, lastFrame);
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }

        private static bool PinClip(ModelImporter importer, string poseName, string takeName, int lastFrame)
        {
            var defaults = importer.defaultClipAnimations;
            if (defaults == null || defaults.Length == 0)
            {
                Debug.LogError("Emberfang clip has no take. Wanted " + takeName + " pose=" + poseName);
                return false;
            }

            ModelImporterClipAnimation? best = null;
            var bestSpan = -1f;
            foreach (var candidate in defaults)
            {
                var span = candidate.lastFrame - candidate.firstFrame;
                var take = candidate.takeName ?? string.Empty;
                if (string.Equals(take, takeName, System.StringComparison.Ordinal) && span >= 1f)
                {
                    best = candidate;
                    break;
                }

                if (span > bestSpan)
                {
                    best = candidate;
                    bestSpan = span;
                }
            }

            if (best == null)
            {
                return false;
            }

            var already = importer.clipAnimations;
            if (already != null && already.Length == 1
                && string.Equals(already[0].name, poseName, System.StringComparison.Ordinal)
                && string.Equals(already[0].takeName, takeName, System.StringComparison.Ordinal)
                && already[0].loopTime
                && (lastFrame <= 0 || System.Math.Abs(already[0].lastFrame - lastFrame) < 0.01f))
            {
                return false;
            }

            best.name = poseName;
            best.takeName = takeName;
            if (lastFrame > 0)
            {
                best.lastFrame = lastFrame;
            }
            best.mirror = false;
            best.loopTime = true;
            best.loop = true;
            best.keepOriginalOrientation = true;
            best.keepOriginalPositionY = true;
            best.keepOriginalPositionXZ = true;
            importer.clipAnimations = new[] { best };
            return true;
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

            Debug.LogError("Emberfang bone missing. " + bone);
        }

        private static void EnableLit(GameObject root)
        {
            RosterPaint.Bind(
                root,
                EmberfangMotion.ThemePackDir,
                "emberfang_basecolor.png",
                "emberfang_normal.png",
                "emberfang_metallic.png",
                "emberfang_roughness.png",
                "Emberfang",
                preferEmbeddedBaseAndNormal: true);
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
            RenderSettings.ambientLight = new Color(0.34f, 0.36f, 0.4f, 1f);
            var key = new GameObject("EmberfangKeyLight");
            var light = key.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.95f, 0.86f, 1f);
            light.shadows = LightShadows.Soft;
            key.transform.rotation = Quaternion.Euler(38f, 150f, 0f);
            var fillGo = new GameObject("EmberfangFillLight");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.5f;
            fill.color = new Color(0.62f, 0.72f, 0.9f, 1f);
            fillGo.transform.rotation = Quaternion.Euler(16f, -30f, 0f);
        }
    }
}
