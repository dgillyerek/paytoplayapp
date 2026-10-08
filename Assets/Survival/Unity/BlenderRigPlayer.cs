using Survival.Domain.Roster;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Survival.Unity
{
    /// <summary>
    /// Plays a Design Blender rig: rest bind pose, then each action clip on the spec.
    /// An optional Theme A attack add-on plays 0–30 once, holds rest for a short gap, and repeats,
    /// with its held props and projectile effects driven by <see cref="BlenderRigAttackDriver"/>.
    /// Humanoid when the mixamorig avatar validates, otherwise Generic.
    /// Custom creatures stay Generic. Does not bake axis conversion, scale one axis,
    /// retarget by hand, or rewrite the imported root. Binds embedded basecolor and
    /// normal. Skips metal/roughness unless the spec names an embedded map.
    /// </summary>
    public sealed class BlenderRigPlayer
    {
        private readonly BlenderRigSpec _spec;
        private readonly Transform _parent;
        private Animator? _animator;
        private PlayableGraph _graph;
        private AnimationPlayableOutput _output;
        private AnimationClipPlayable _playable;
        private bool _humanoid;
        private bool _ready;
        private bool _loop;
        private readonly BlenderRigAttackSpec? _attackSpec;
        private readonly BlenderRigIdlePropSpec[] _idleProps;
        private BlenderRigAttackDriver? _attack;
        private bool _attackMode;
        private float _length;
        private float _time;
        private string _pose;

        public BlenderRigPlayer(
            BlenderRigSpec spec,
            Transform parent,
            BlenderRigAttackSpec? attack = null,
            BlenderRigIdlePropSpec[]? idleProps = null)
        {
            _attackSpec = attack;
            _idleProps = idleProps ?? System.Array.Empty<BlenderRigIdlePropSpec>();
            _spec = spec;
            _parent = parent;
            _pose = BlenderRigSpec.RestPoseName;
        }

        public bool Built => _ready;
        public string Pose => _pose;
        public float ClipLength => _length;
        public Bounds VisibleBounds { get; private set; }

        public void Build()
        {
            BuildLights();
#if UNITY_EDITOR
            var path = "Assets/" + _spec.RestThemePackRel;
            if (BlenderRigSpec.IsRejectedMixamoRigFile(path)
                || path.EndsWith(_spec.RejectedMixamoFileName, System.StringComparison.Ordinal))
            {
                Debug.LogError(_spec.Name + " rejected Mixamo file " + _spec.RejectedMixamoFileName);
                return;
            }

            _humanoid = false;
            if (_spec.PreferHumanoid)
            {
                EnsureImport(path, humanoid: true, clip: false);
                _humanoid = HasValidHumanAvatar(path);
                if (!_humanoid)
                {
                    Debug.LogWarning(_spec.Name + " humanoid avatar did not validate. Using Generic.");
                    EnsureImport(path, humanoid: false, clip: false);
                }
            }
            else
            {
                EnsureImport(path, humanoid: false, clip: false);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError(_spec.Name + " blender rig missing. " + path);
                return;
            }

            var instance = Object.Instantiate(prefab, _parent);
            instance.name = _spec.Name + "BlenderRig";
            if (_attackSpec != null)
            {
                _attack = new BlenderRigAttackDriver(_attackSpec, _spec.ThemePackDir, _parent);
                _attack.Calibrate(instance);
                if (_idleProps.Length > 0)
                {
                    _attack.BindIdleProps(_idleProps);
                    _attack.SetIdlePropsVisible(true);
                }
            }
            else if (_idleProps.Length > 0)
            {
                Debug.LogWarning("Idle props need the attack calibration; none shown for " + _spec.ThemePackDir);
            }

            _animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
            var avatar = _humanoid ? LoadHumanAvatar(path) : LoadGenericAvatar(path);
            if (avatar != null)
            {
                _animator.avatar = avatar;
            }

            _animator.runtimeAnimatorController = null;
            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            WarnIfBoneMissing(instance, _spec.BoneRoot);
            BindEmbeddedMaps(instance, path);
            VisibleBounds = Encapsulate(instance);
            _graph = PlayableGraph.Create(_spec.Name + "BlenderRig");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            _output = AnimationPlayableOutput.Create(_graph, _spec.Name, _animator);
            _graph.Play();
            _ready = true;
            ShowRest();
#else
            Debug.LogError(_spec.Name + " plays in the Editor Game view.");
#endif
        }

        public bool PlayPose(string poseName)
        {
            if (!_ready)
            {
                return false;
            }

            if (_attackSpec != null && string.Equals(poseName, BlenderRigAttackSpec.PoseName, System.StringComparison.Ordinal))
            {
                return PlayAttack();
            }

            StopAttack();

            if (string.Equals(poseName, BlenderRigSpec.RestPoseName, System.StringComparison.Ordinal))
            {
                ShowRest();
                return true;
            }

            if (string.Equals(poseName, _spec.ClipPoseName, System.StringComparison.Ordinal))
            {
                return PlayClip(_spec.ClipFileName, _spec.ClipPoseName, _spec.ClipTakeName, _spec.ClipLastFrame);
            }

            foreach (var extra in _spec.ExtraClips)
            {
                if (string.Equals(poseName, extra.PoseName, System.StringComparison.Ordinal))
                {
                    return PlayClip(extra.FileName, extra.PoseName, extra.TakeName, extra.LastFrame);
                }
            }

            Debug.LogWarning(_spec.Name + " unknown pose. " + poseName);
            return false;
        }

        public void Tick()
        {
            if (_attackMode)
            {
                TickAttack();
                return;
            }

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

        public void Dispose()
        {
            _attack?.Dispose();
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }

        private bool PlayAttack()
        {
            if (_attackSpec == null || _attack == null)
            {
                return false;
            }

            StopAttack();
            if (!PlayClip(_attackSpec.FileName, BlenderRigAttackSpec.PoseName, BlenderRigAttackSpec.TakeName, BlenderRigAttackSpec.LastFrame))
            {
                return false;
            }

            _loop = false;
            _attackMode = true;
            _time = 0f;
            _attack.SetIdlePropsVisible(false);
            _attack.Activate(SampleAttackFrame);
            SampleAttackFrame(BlenderRigAttackSpec.FirstFrame);
            _attack.Tick(BlenderRigAttackSpec.FirstFrame, true, 0);
            return true;
        }

        private void TickAttack()
        {
            if (!_playable.IsValid() || _attack == null)
            {
                return;
            }

            _time += Time.unscaledDeltaTime;
            var frame = BlenderRigAttackSpec.CycleFrame(_time, out var inClip, out var cycle);
            SampleAttackFrame(frame);
            _attack.Tick(frame, inClip, cycle);
        }

        private void SampleAttackFrame(float frame)
        {
            if (!_playable.IsValid())
            {
                return;
            }

            var seconds = frame / BlenderRigAttackSpec.FrameRate;
            if (_length > 0.05f)
            {
                seconds = Mathf.Min(seconds, _length - 0.0005f);
            }

            _playable.SetTime(seconds);
            _graph.Evaluate(0f);
        }

        private void StopAttack()
        {
            if (!_attackMode)
            {
                return;
            }

            _attackMode = false;
            _attack?.Deactivate();
            _attack?.SetIdlePropsVisible(true);
        }

        private void ShowRest()
        {
            StopAttack();
            if (_playable.IsValid())
            {
                _playable.Destroy();
            }

            _animator?.Rebind();
            _animator?.Update(0f);
            _pose = BlenderRigSpec.RestPoseName;
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
                    _spec.Name + " clip missing. take=" + takeName +
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

#if UNITY_EDITOR
        private AnimationClip? LoadClip(string fileName, string poseName, string takeName, int lastFrame)
        {
            var path = "Assets/" + _spec.ThemePackDir + "/" + fileName;
            EnsureImport(path, _humanoid, true, poseName, takeName, lastFrame);
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

        private static bool HasValidHumanAvatar(string path)
        {
            var avatar = LoadHumanAvatar(path);
            return avatar != null && avatar.isValid && avatar.isHuman;
        }

        private static Avatar? LoadHumanAvatar(string path)
        {
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (obj is Avatar avatar && avatar.isHuman && avatar.isValid)
                {
                    return avatar;
                }
            }

            return null;
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

        private void EnsureImport(
            string path,
            bool humanoid,
            bool clip,
            string poseName = "",
            string takeName = "",
            int lastFrame = 0)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            {
                Debug.LogError(_spec.Name + " importer missing. " + path);
                return;
            }

            var dirty = false;
            var want = humanoid ? ModelImporterAnimationType.Human : ModelImporterAnimationType.Generic;
            if (importer.animationType != want)
            {
                importer.animationType = want;
                dirty = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                dirty = true;
            }

            if (importer.autoGenerateAvatarMappingIfUnspecified != humanoid)
            {
                importer.autoGenerateAvatarMappingIfUnspecified = humanoid;
                dirty = true;
            }

            if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
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

            if (!importer.importAnimation)
            {
                importer.importAnimation = true;
                dirty = true;
            }

            if (clip)
            {
                dirty |= PinClip(importer, poseName, takeName, lastFrame);
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }

        private bool PinClip(ModelImporter importer, string poseName, string takeName, int lastFrame)
        {
            var defaults = importer.defaultClipAnimations;
            if (defaults == null || defaults.Length == 0)
            {
                Debug.LogError(_spec.Name + " clip has no take. Wanted " + takeName);
                return false;
            }

            ModelImporterClipAnimation? best = null;
            var bestSpan = -1f;
            foreach (var candidate in defaults)
            {
                var take = candidate.takeName ?? string.Empty;
                if (string.Equals(take, "mixamo.com", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var span = candidate.lastFrame - candidate.firstFrame;
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
                && Mathf.Abs(already[0].lastFrame - lastFrame) < 0.01f
                && !already[0].mirror)
            {
                return false;
            }

            best.name = poseName;
            best.takeName = takeName;
            best.firstFrame = 0f;
            best.lastFrame = lastFrame;
            best.mirror = false;
            best.loopTime = true;
            best.loop = true;
            best.keepOriginalOrientation = true;
            best.keepOriginalPositionY = true;
            best.keepOriginalPositionXZ = true;
            best.heightFromFeet = false;
            importer.clipAnimations = new[] { best };
            return true;
        }

        private void BindEmbeddedMaps(GameObject root, string fbxPath)
        {
            var albedo = LoadMap(_spec.BaseColorThemePackRel, _spec.BaseColorFile, fbxPath, asNormal: false);
            var normal = LoadMap(_spec.NormalThemePackRel, _spec.NormalFile, fbxPath, asNormal: true);
            Texture2D? metal = null;
            if (!string.IsNullOrEmpty(_spec.MetallicRoughnessFile))
            {
                var metalRel = _spec.ThemePackDir + "/" + _spec.TextureFolder + "/" + _spec.MetallicRoughnessFile;
                metal = LoadMap(metalRel, _spec.MetallicRoughnessFile, fbxPath, asNormal: false);
            }

            if (albedo == null)
            {
                Debug.LogError(_spec.Name + " basecolor missing. " + _spec.BaseColorFile);
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                rend.enabled = true;
                if (rend is SkinnedMeshRenderer skin)
                {
                    skin.updateWhenOffscreen = true;
                }

                var shared = rend.sharedMaterials;
                if (shared == null || shared.Length == 0)
                {
                    shared = new Material[1];
                }

                for (var i = 0; i < shared.Length; i++)
                {
                    var mat = shared[i];
                    if (shader == null)
                    {
                        continue;
                    }

                    if (mat == null)
                    {
                        mat = new Material(shader);
                        mat.name = _spec.Name;
                        shared[i] = mat;
                    }

                    if (mat.shader == null || mat.shader.name.IndexOf("InternalError", System.StringComparison.Ordinal) >= 0
                        || mat.shader.name.IndexOf("Universal", System.StringComparison.Ordinal) < 0)
                    {
                        var next = new Material(shader);
                        next.name = string.IsNullOrEmpty(mat.name) ? _spec.Name : mat.name;
                        mat = next;
                        shared[i] = mat;
                    }

                    if (albedo != null)
                    {
                        if (mat.HasProperty("_BaseMap"))
                        {
                            mat.SetTexture("_BaseMap", albedo);
                        }

                        if (mat.HasProperty("_MainTex"))
                        {
                            mat.SetTexture("_MainTex", albedo);
                        }

                        mat.EnableKeyword("_BASEMAP");
                        if (mat.HasProperty("_BaseColor"))
                        {
                            mat.SetColor("_BaseColor", Color.white);
                        }

                        mat.color = Color.white;
                    }

                    if (normal != null && mat.HasProperty("_BumpMap"))
                    {
                        mat.SetTexture("_BumpMap", normal);
                        mat.EnableKeyword("_NORMALMAP");
                        if (mat.HasProperty("_BumpScale"))
                        {
                            mat.SetFloat("_BumpScale", 1f);
                        }
                    }

                    if (metal != null && mat.HasProperty("_MetallicGlossMap"))
                    {
                        mat.SetTexture("_MetallicGlossMap", metal);
                        mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                    }
                }

                rend.sharedMaterials = shared;
            }
        }

        private static Texture2D? LoadMap(string themePackRel, string fileName, string fbxPath, bool asNormal)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            var path = "Assets/" + themePackRel;
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                var dirty = false;
                var wantType = asNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                if (importer.textureType != wantType)
                {
                    importer.textureType = wantType;
                    dirty = true;
                }

                if (asNormal && importer.sRGBTexture)
                {
                    importer.sRGBTexture = false;
                    dirty = true;
                }

                if (!asNormal && !importer.sRGBTexture)
                {
                    importer.sRGBTexture = true;
                    dirty = true;
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }

            var onDisk = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (onDisk != null)
            {
                return onDisk;
            }

            var stem = System.IO.Path.GetFileNameWithoutExtension(fileName);
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (obj is Texture2D tex && tex.name.IndexOf(stem, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return tex;
                }
            }

            return null;
        }
#endif

        private static void WarnIfBoneMissing(GameObject instance, string bone)
        {
            foreach (var t in instance.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(t.name, bone, System.StringComparison.Ordinal))
                {
                    return;
                }
            }

            Debug.LogError(instance.name + " bone missing. " + bone);
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

        private void BuildLights()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.36f, 0.36f, 0.38f, 1f);
            var key = new GameObject(_spec.Name + "KeyLight");
            var light = key.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.96f, 0.88f, 1f);
            light.shadows = LightShadows.Soft;
            key.transform.rotation = Quaternion.Euler(40f, 160f, 0f);
            var fillGo = new GameObject(_spec.Name + "FillLight");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.45f;
            fill.color = new Color(0.68f, 0.74f, 0.88f, 1f);
            fillGo.transform.rotation = Quaternion.Euler(18f, -24f, 0f);
        }
    }
}
