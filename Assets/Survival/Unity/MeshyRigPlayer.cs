using System.Collections.Generic;
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
    /// Plays a Meshy auto-rigged character as-is: rest (Meshy idle loop), walk loop, and an attack that
    /// plays once, holds its last frame for <see cref="MeshyRigSpec.RestGapSeconds"/>, then repeats.
    /// Generic import with the FBX's own bone names (clips from the other exports bind by name/path).
    /// Root motion off. File scale follows the spec (off for metre exports). Hides stray meshes by name.
    /// Packs separate metallic + roughness maps into URP metallic (R) and smoothness (A = 1 - roughness)
    /// so the body does not render as chrome.
    /// Props: rest/walk show the spec's idle props parented to their bones (bound once from Design's
    /// character-space pose). The attack hides them and samples each baked prop FBX (per-frame motion in
    /// character space, dropped under the character root, never reparented) at the body's clip time.
    /// If a baked file cannot be used, the bone-held fallback stands in. No projectile code: the arrow
    /// flight is baked. Does not re-rig, re-skin, or bake axis conversion.
    /// </summary>
    public sealed class MeshyRigPlayer
    {
        private readonly MeshyRigSpec _spec;
        private readonly Transform _parent;
        private GameObject? _instance;
        private Animator? _animator;
        private PlayableGraph _graph;
        private AnimationPlayableOutput _output;
        private AnimationClipPlayable _playable;
        private readonly Dictionary<string, AnimationClip> _clips = new Dictionary<string, AnimationClip>();
        private readonly List<GameObject> _idleProps = new List<GameObject>();
        private readonly List<BakedView> _baked = new List<BakedView>();
        private readonly List<GameObject> _fallback = new List<GameObject>();
        private bool _attackPropsBuilt;
        private bool _ready;
        private bool _attackMode;
        private float _length;
        private float _time;
        private string _pose;

        public MeshyRigPlayer(MeshyRigSpec spec, Transform parent)
        {
            _spec = spec;
            _parent = parent;
            _pose = MeshyRigSpec.RestPoseName;
        }

        public bool Built => _ready;
        public string Pose => _pose;
        public float ClipLength => _attackMode ? _spec.AttackCycleSeconds : _length;
        public Bounds VisibleBounds { get; private set; }

        public void Build()
        {
            BuildLights();
#if UNITY_EDITOR
            var path = Asset(_spec.Rest.FileName);
            EnsureModelImport(path, null);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError(_spec.Name + " Meshy rest FBX missing. " + path);
                return;
            }

            _instance = Object.Instantiate(prefab, _parent);
            _instance.name = _spec.Name + "MeshyRig";
            HideStrayMeshes(_instance);
            CheckBones(_instance);
            _animator = _instance.GetComponent<Animator>() ?? _instance.AddComponent<Animator>();
            var avatar = LoadGenericAvatar(path);
            if (avatar != null)
            {
                _animator.avatar = avatar;
            }

            _animator.runtimeAnimatorController = null;
            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            Paint(_instance);
            VisibleBounds = Encapsulate(_instance);
            _graph = PlayableGraph.Create(_spec.Name + "MeshyRig");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            _output = AnimationPlayableOutput.Create(_graph, _spec.Name, _animator);
            _graph.Play();
            _ready = true;
            PlayPose(MeshyRigSpec.RestPoseName);
            BuildIdleProps();
            ShowIdleProps(true);
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

            var clipSpec = _spec.FindClip(poseName);
            if (clipSpec == null)
            {
                Debug.LogWarning(_spec.Name + " unknown pose. " + poseName);
                return false;
            }

            var attack = string.Equals(poseName, MeshyRigSpec.AttackPoseName, System.StringComparison.Ordinal);
            if (!PlayClip(clipSpec.Value))
            {
                return false;
            }

            _attackMode = attack;
            if (attack)
            {
                // Hide the rest/walk bow first so no prop ever shows twice.
                ShowIdleProps(false);
                BuildAttackProps();
                ShowAttackProps(true);
                SampleAttackProps(0f);
            }
            else
            {
                ShowAttackProps(false);
                ShowIdleProps(true);
            }

            return true;
        }

        public void Tick()
        {
            if (!_ready || !_playable.IsValid())
            {
                return;
            }

            _time += Time.unscaledDeltaTime;
            float t;
            if (_attackMode)
            {
                t = _spec.AttackClipSeconds(_time, out _, out _);
            }
            else
            {
                t = _length > 0.05f ? _time % _length : 0f;
            }

            if (_length > 0.05f)
            {
                t = Mathf.Min(t, _length - 0.0005f);
            }

            _playable.SetTime(t);
            _graph.Evaluate(0f);
            if (_attackMode)
            {
                SampleAttackProps(t);
            }
        }

        public void Dispose()
        {
            foreach (var view in _baked)
            {
                if (view.Graph.IsValid())
                {
                    view.Graph.Destroy();
                }
            }

            _baked.Clear();
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }

        private void ShowIdleProps(bool shown)
        {
            foreach (var prop in _idleProps)
            {
                if (prop != null)
                {
                    prop.SetActive(shown);
                }
            }
        }

        private void ShowAttackProps(bool shown)
        {
            foreach (var view in _baked)
            {
                if (view.Root != null)
                {
                    view.Root.SetActive(shown);
                }
            }

            foreach (var prop in _fallback)
            {
                if (prop != null)
                {
                    prop.SetActive(shown);
                }
            }
        }

        private void SampleAttackProps(float seconds)
        {
            foreach (var view in _baked)
            {
                if (!view.Graph.IsValid() || !view.Playable.IsValid())
                {
                    continue;
                }

                var t = view.Length > 0.05f ? Mathf.Min(seconds, view.Length - 0.0005f) : 0f;
                view.Playable.SetTime(t);
                view.Graph.Evaluate(0f);
            }
        }

        private void BuildIdleProps()
        {
#if UNITY_EDITOR
            // Body is at rest frame 0 here (PlayPose(rest) just evaluated t = 0).
            foreach (var spec in _spec.IdleProps)
            {
                var prop = BindHeldProp(spec);
                if (prop != null)
                {
                    _idleProps.Add(prop);
                }
            }
#endif
        }

        private void BuildAttackProps()
        {
#if UNITY_EDITOR
            if (_attackPropsBuilt || _instance == null)
            {
                return;
            }

            _attackPropsBuilt = true;
            var bowBaked = true;
            foreach (var spec in _spec.AttackBakedProps)
            {
                var view = LoadBakedProp(spec);
                if (view == null)
                {
                    if (spec.Name.IndexOf("bow", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        bowBaked = false;
                    }

                    continue;
                }

                _baked.Add(view);
            }

            if (!bowBaked)
            {
                // Drop a half-loaded baked bow so the fallback is the only bow in the attack.
                for (var i = _baked.Count - 1; i >= 0; i--)
                {
                    if (_baked[i].Root.name.IndexOf("bow", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _baked[i].Graph.Destroy();
                        Object.Destroy(_baked[i].Root);
                        _baked.RemoveAt(i);
                    }
                }

                Debug.LogWarning(_spec.Name + " baked attack bow unusable. Falling back to Design's LeftHand offset.");
                var time = _time;
                foreach (var spec in _spec.AttackFallbackProps)
                {
                    // Sample the attack at the bind frame, bind to the bone, then return to the clip start.
                    _playable.SetTime(spec.BindFrame / _spec.Attack.FrameRate);
                    _graph.Evaluate(0f);
                    var prop = BindHeldProp(spec);
                    if (prop != null)
                    {
                        _fallback.Add(prop);
                    }
                }

                _playable.SetTime(time);
                _graph.Evaluate(0f);
            }

            ShowAttackProps(false);
#endif
        }

        private bool PlayClip(MeshyRigClip spec)
        {
#if UNITY_EDITOR
            var clip = LoadClip(spec);
            if (clip == null || clip.empty || clip.length < 0.2f)
            {
                Debug.LogError(_spec.Name + " Meshy clip missing. take=" + spec.TakeName + " file=" + spec.FileName);
                return false;
            }

            if (_playable.IsValid())
            {
                _playable.Destroy();
            }

            _playable = AnimationClipPlayable.Create(_graph, clip);
            _playable.SetApplyFootIK(false);
            _output.SetSourcePlayable(_playable);
            _playable.SetDuration(clip.length);
            _playable.SetTime(0d);
            _playable.SetSpeed(1d);
            _length = clip.length;
            _time = 0f;
            _pose = spec.PoseName;
            _graph.Evaluate(0f);
            if (Mathf.Abs(clip.length - spec.Seconds) > 0.1f)
            {
                Debug.LogWarning(
                    _spec.Name + " " + spec.PoseName + " clip length " + clip.length.ToString("0.000") +
                    "s differs from the FBX take " + spec.Seconds.ToString("0.000") + "s");
            }

            return true;
#else
            return false;
#endif
        }

#if UNITY_EDITOR
        private string Asset(string fileName) => "Assets/" + _spec.ThemePackDir + "/" + fileName;

        private AnimationClip? LoadClip(MeshyRigClip spec)
        {
            if (_clips.TryGetValue(spec.PoseName, out var cached) && cached != null)
            {
                return cached;
            }

            var path = Asset(spec.FileName);
            EnsureModelImport(path, spec);
            AnimationClip? named = null;
            AnimationClip? fallback = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (obj is not AnimationClip clip || clip.name.StartsWith("__preview__"))
                {
                    continue;
                }

                if (string.Equals(clip.name, spec.PoseName, System.StringComparison.Ordinal))
                {
                    named = clip;
                    continue;
                }

                if (fallback == null || clip.length > fallback.length)
                {
                    fallback = clip;
                }
            }

            var result = named ?? fallback;
            if (result != null)
            {
                _clips[spec.PoseName] = result;
            }

            return result;
        }

        private void EnsureModelImport(string path, MeshyRigClip? clip)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            {
                Debug.LogError(_spec.Name + " importer missing. " + path);
                return;
            }

            var dirty = false;
            if (importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                dirty = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                dirty = true;
            }

            if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                dirty = true;
            }

            // Raw Meshy cm exports need the file scale; Design's metre re-exports (UnitScaleFactor 100) do not.
            if (importer.useFileScale != _spec.UseFileScale)
            {
                importer.useFileScale = _spec.UseFileScale;
                dirty = true;
            }

            if (Mathf.Abs(importer.globalScale - 1f) > 0.001f)
            {
                importer.globalScale = 1f;
                dirty = true;
            }

            if (importer.optimizeGameObjects)
            {
                importer.optimizeGameObjects = false;
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

            if (clip != null)
            {
                dirty |= PinClip(importer, clip.Value);
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }

        private bool PinClip(ModelImporter importer, MeshyRigClip spec)
        {
            var loop = !string.Equals(spec.PoseName, MeshyRigSpec.AttackPoseName, System.StringComparison.Ordinal);
            return PinTake(importer, spec.PoseName, spec.TakeName, spec.MeshyAnimation, loop);
        }

        private bool PinTake(ModelImporter importer, string clipName, string takeName, string suffix, bool loop)
        {
            var defaults = importer.defaultClipAnimations;
            if (defaults == null || defaults.Length == 0)
            {
                Debug.LogError(_spec.Name + " Meshy clip has no take. Wanted " + takeName);
                return false;
            }

            ModelImporterClipAnimation? best = null;
            foreach (var candidate in defaults)
            {
                var take = candidate.takeName ?? string.Empty;
                if (string.Equals(take, takeName, System.StringComparison.Ordinal)
                    || (suffix.Length > 0 && take.EndsWith("|" + suffix, System.StringComparison.Ordinal)))
                {
                    best = candidate;
                    break;
                }

                if (best == null || (candidate.lastFrame - candidate.firstFrame) > (best.lastFrame - best.firstFrame))
                {
                    best = candidate;
                }
            }

            if (best == null)
            {
                return false;
            }

            var already = importer.clipAnimations;
            if (already != null && already.Length == 1
                && string.Equals(already[0].name, clipName, System.StringComparison.Ordinal)
                && string.Equals(already[0].takeName, best.takeName, System.StringComparison.Ordinal)
                && already[0].loopTime == loop
                && !already[0].mirror)
            {
                return false;
            }

            // Keep the take's own frame range (file frame rate); only name, loop, and in-place flags change.
            best.name = clipName;
            best.mirror = false;
            best.loopTime = loop;
            best.loop = loop;
            best.keepOriginalOrientation = true;
            best.keepOriginalPositionY = true;
            best.keepOriginalPositionXZ = true;
            best.heightFromFeet = false;
            importer.clipAnimations = new[] { best };
            return true;
        }

        private GameObject? BindHeldProp(MeshyHeldProp spec)
        {
            if (_instance == null)
            {
                return null;
            }

            var bone = FindBone(_instance.transform, spec.Bone);
            if (bone == null)
            {
                Debug.LogError(_spec.Name + " prop bone missing. " + spec.Bone + " for " + spec.Name);
                return null;
            }

            var path = Asset(spec.FileName);
            EnsureStaticPropImport(path);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError(_spec.Name + " prop missing. " + path);
                return null;
            }

            var prop = Object.Instantiate(prefab, _instance.transform, false);
            prop.name = spec.Name;
            foreach (var anim in prop.GetComponentsInChildren<Animator>(true))
            {
                Object.Destroy(anim);
            }

            FixPropMaterials(prop);
            var c = spec.CharacterPose;
            var root = _instance.transform;
            var worldPos = root.TransformPoint(new Vector3(c[0], c[1], c[2]));
            var worldRot = root.rotation * new Quaternion(c[3], c[4], c[5], c[6]).normalized;
            prop.transform.SetPositionAndRotation(worldPos, worldRot);
            // Ride the bone from here on (bone-local offset fixed at the bind frame).
            prop.transform.SetParent(bone, true);
            Debug.Log(
                _spec.Name + " " + spec.Name + " on " + spec.Bone + " (bound at " + spec.BindPose + " f" + spec.BindFrame +
                ") local " + prop.transform.localPosition.ToString("F4") + " " + prop.transform.localRotation.eulerAngles.ToString("F1"));
            return prop;
        }

        private BakedView? LoadBakedProp(MeshyBakedProp spec)
        {
            if (_instance == null)
            {
                return null;
            }

            var path = Asset(spec.FileName);
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            {
                Debug.LogError(_spec.Name + " baked prop importer missing. " + path);
                return null;
            }

            var dirty = false;
            if (importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                dirty = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                dirty = true;
            }

            if (importer.useFileScale != _spec.UseFileScale)
            {
                importer.useFileScale = _spec.UseFileScale;
                dirty = true;
            }

            if (Mathf.Abs(importer.globalScale - 1f) > 0.001f)
            {
                importer.globalScale = 1f;
                dirty = true;
            }

            if (importer.bakeAxisConversion)
            {
                importer.bakeAxisConversion = false;
                dirty = true;
            }

            if (importer.optimizeGameObjects)
            {
                importer.optimizeGameObjects = false;
                dirty = true;
            }

            if (!importer.importAnimation)
            {
                importer.importAnimation = true;
                dirty = true;
            }

            if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                dirty = true;
            }

            dirty |= PinTake(importer, spec.Name, spec.TakeName, string.Empty, loop: false);
            if (dirty)
            {
                importer.SaveAndReimport();
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            AnimationClip? clip = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (obj is AnimationClip c && !c.name.StartsWith("__preview__") && (clip == null || c.length > clip.length))
                {
                    clip = c;
                }
            }

            if (prefab == null || clip == null || clip.empty || clip.length < 0.2f)
            {
                Debug.LogError(_spec.Name + " baked prop unusable (prefab or clip missing). " + path);
                return null;
            }

            // Character space: the baked file's root is the character root (both are Design's world origin).
            // Never reparented to a bone; the take carries every frame.
            var root = Object.Instantiate(prefab, _instance.transform, false);
            root.name = spec.Name;
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            FixPropMaterials(root);
            var animator = root.GetComponent<Animator>();
            if (animator == null)
            {
                animator = root.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var graph = PlayableGraph.Create(_spec.Name + spec.Name);
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output = AnimationPlayableOutput.Create(graph, spec.Name, animator);
            var playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(false);
            playable.SetDuration(clip.length);
            output.SetSourcePlayable(playable);
            graph.Play();
            if (Mathf.Abs(clip.length - _spec.Attack.Seconds) > 0.1f)
            {
                Debug.LogWarning(
                    _spec.Name + " baked " + spec.Name + " length " + clip.length.ToString("0.000") +
                    "s vs attack " + _spec.Attack.Seconds.ToString("0.000") + "s");
            }

            return new BakedView(root, graph, playable, clip.length);
        }

        private void EnsureStaticPropImport(string path)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            {
                Debug.LogError(_spec.Name + " prop importer missing. " + path);
                return;
            }

            var dirty = false;
            if (importer.animationType != ModelImporterAnimationType.None)
            {
                importer.animationType = ModelImporterAnimationType.None;
                dirty = true;
            }

            if (importer.importAnimation)
            {
                importer.importAnimation = false;
                dirty = true;
            }

            if (importer.useFileScale != _spec.UseFileScale)
            {
                importer.useFileScale = _spec.UseFileScale;
                dirty = true;
            }

            if (importer.bakeAxisConversion)
            {
                importer.bakeAxisConversion = false;
                dirty = true;
            }

            if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }

        /// <summary>Keeps URP materials from the FBX; rebuilds anything else as URP Lit with its colour and base map.</summary>
        private static void FixPropMaterials(GameObject root)
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                lit = Shader.Find("Standard");
            }

            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                if (rend is SkinnedMeshRenderer skin)
                {
                    skin.updateWhenOffscreen = true;
                }

                var shared = rend.sharedMaterials;
                for (var i = 0; i < shared.Length; i++)
                {
                    var mat = shared[i];
                    if (lit == null || (mat != null && mat.shader != null && mat.shader.name.IndexOf("Universal", System.StringComparison.Ordinal) >= 0))
                    {
                        continue;
                    }

                    var color = Color.white;
                    Texture? tex = null;
                    if (mat != null)
                    {
                        color = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : (mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white);
                        tex = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : (mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null);
                    }

                    var next = new Material(lit);
                    next.name = mat != null ? mat.name : "MeshyProp";
                    next.SetColor("_BaseColor", color);
                    if (tex != null)
                    {
                        next.SetTexture("_BaseMap", tex);
                    }

                    shared[i] = next;
                }

                rend.sharedMaterials = shared;
            }
        }

        private static Transform? FindBone(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(t.name, name, System.StringComparison.Ordinal))
                {
                    return t;
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

        private void Paint(GameObject root)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogError(_spec.Name + " Lit shader missing.");
                return;
            }

            var albedo = LoadMap(_spec.BaseColorFile, TextureImporterType.Default, srgb: true, readable: false);
            var normal = LoadMap(_spec.NormalFile, TextureImporterType.NormalMap, srgb: false, readable: false);
            var packed = PackMetallicSmoothness();
            if (albedo == null)
            {
                Debug.LogError(_spec.Name + " basecolor missing. " + _spec.BaseColorFile);
            }

            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                if (_spec.IsHiddenMesh(rend.gameObject.name))
                {
                    continue;
                }

                if (rend is SkinnedMeshRenderer skin)
                {
                    skin.updateWhenOffscreen = true;
                }

                var slots = rend.sharedMaterials;
                if (slots == null || slots.Length == 0)
                {
                    slots = new Material[1];
                }

                for (var i = 0; i < slots.Length; i++)
                {
                    var mat = new Material(shader);
                    mat.name = _spec.Name + "_" + (slots[i] != null ? slots[i].name : "Body");
                    if (albedo != null)
                    {
                        mat.SetTexture("_BaseMap", albedo);
                        if (mat.HasProperty("_MainTex"))
                        {
                            mat.SetTexture("_MainTex", albedo);
                        }

                        mat.SetColor("_BaseColor", Color.white);
                    }

                    if (normal != null && mat.HasProperty("_BumpMap"))
                    {
                        mat.SetTexture("_BumpMap", normal);
                        mat.EnableKeyword("_NORMALMAP");
                        mat.SetFloat("_BumpScale", 1f);
                    }

                    if (mat.HasProperty("_WorkflowMode"))
                    {
                        mat.SetFloat("_WorkflowMode", 1f);
                    }

                    mat.DisableKeyword("_SPECULAR_SETUP");
                    mat.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
                    if (mat.HasProperty("_SmoothnessTextureChannel"))
                    {
                        mat.SetFloat("_SmoothnessTextureChannel", 0f);
                    }

                    if (packed != null)
                    {
                        // URP Lit: metallic = map.R * 1, smoothness = map.A * _Smoothness.
                        mat.SetTexture("_MetallicGlossMap", packed);
                        mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                        mat.SetFloat("_Metallic", GlTfMetalRough.PackedMetallicScale);
                        mat.SetFloat("_Smoothness", GlTfMetalRough.PackedSmoothnessScale);
                    }
                    else
                    {
                        mat.SetTexture("_MetallicGlossMap", null);
                        mat.DisableKeyword("_METALLICSPECGLOSSMAP");
                        mat.SetFloat("_Metallic", GlTfMetalRough.FallbackMetallic);
                        mat.SetFloat("_Smoothness", GlTfMetalRough.FallbackSmoothness);
                    }

                    slots[i] = mat;
                }

                rend.sharedMaterials = slots;
            }

            Debug.Log(
                _spec.Name + " paint: base=" + (albedo != null ? albedo.name : "missing") +
                " normal=" + (normal != null ? normal.name : "missing") +
                " metallicSmoothness=" + (packed != null ? packed.width + "x" + packed.height + " (R metallic, A 1-roughness)" : "fallback dielectric"));
        }

        private Texture2D? LoadMap(string fileName, TextureImporterType type, bool srgb, bool readable)
        {
            var path = "Assets/" + _spec.TexturePath(fileName);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                var dirty = false;
                if (importer.textureType != type)
                {
                    importer.textureType = type;
                    dirty = true;
                }

                if (type != TextureImporterType.NormalMap && importer.sRGBTexture != srgb)
                {
                    importer.sRGBTexture = srgb;
                    dirty = true;
                }

                if (readable && !importer.isReadable)
                {
                    importer.isReadable = true;
                    dirty = true;
                }

                if (readable && importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    dirty = true;
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }
            else
            {
                Debug.LogError(_spec.Name + " texture importer missing. " + path);
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Separate metallic + roughness (greyscale, read R) into URP RGB = metallic, A = 1 - roughness.</summary>
        private Texture2D? PackMetallicSmoothness()
        {
            var metallic = LoadMap(_spec.MetallicFile, TextureImporterType.Default, srgb: false, readable: true);
            var roughness = LoadMap(_spec.RoughnessFile, TextureImporterType.Default, srgb: false, readable: true);
            if (metallic == null || roughness == null)
            {
                Debug.LogError(_spec.Name + " metallic/roughness missing. Using a plain dielectric.");
                return null;
            }

            if (metallic.width != roughness.width || metallic.height != roughness.height)
            {
                Debug.LogError(
                    _spec.Name + " metallic/roughness size mismatch " + metallic.width + "x" + metallic.height +
                    " vs " + roughness.width + "x" + roughness.height + ". Using a plain dielectric.");
                return null;
            }

            try
            {
                var metalPx = metallic.GetPixels32();
                var roughPx = roughness.GetPixels32();
                var output = new Color32[metalPx.Length];
                for (var i = 0; i < metalPx.Length; i++)
                {
                    GlTfMetalRough.PackSeparateTexel(metalPx[i].r, roughPx[i].r, out var rgb, out var alpha);
                    output[i] = new Color32(rgb, rgb, rgb, alpha);
                }

                var packed = new Texture2D(metallic.width, metallic.height, TextureFormat.RGBA32, true, true);
                packed.SetPixels32(output);
                packed.Apply(true, false);
                packed.wrapMode = TextureWrapMode.Repeat;
                packed.filterMode = FilterMode.Bilinear;
                packed.name = _spec.Name + "_metallicSmoothness";
                return packed;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(_spec.Name + " metallic-smoothness pack failed. " + ex.Message + " Using a plain dielectric.");
                return null;
            }
        }
#endif

        private void HideStrayMeshes(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (_spec.IsHiddenMesh(t.name))
                {
                    t.gameObject.SetActive(false);
                }
            }
        }

        private void CheckBones(GameObject root)
        {
            var names = new HashSet<string>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                names.Add(t.name);
            }

            var missing = 0;
            foreach (var bone in _spec.BoneNames)
            {
                if (!names.Contains(bone))
                {
                    missing++;
                    Debug.LogError(_spec.Name + " bone missing by name. " + bone);
                }
            }

            if (!names.Contains(_spec.BoneRoot))
            {
                Debug.LogError(_spec.Name + " root bone missing. " + _spec.BoneRoot);
            }

            Debug.Log(_spec.Name + " bones bound by name: " + (_spec.BoneNames.Length - missing) + "/" + _spec.BoneNames.Length);
        }

        private Bounds Encapsulate(GameObject root)
        {
            var seeded = false;
            var bounds = new Bounds(root.transform.position + Vector3.up, Vector3.one);
            foreach (var rend in root.GetComponentsInChildren<Renderer>(false))
            {
                if (_spec.IsHiddenMesh(rend.gameObject.name))
                {
                    continue;
                }

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

        private sealed class BakedView
        {
            public BakedView(GameObject root, PlayableGraph graph, AnimationClipPlayable playable, float length)
            {
                Root = root;
                Graph = graph;
                Playable = playable;
                Length = length;
            }

            public GameObject Root { get; }
            public PlayableGraph Graph { get; }
            public AnimationClipPlayable Playable { get; }
            public float Length { get; }
        }
    }
}
