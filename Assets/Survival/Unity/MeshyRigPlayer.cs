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
    /// Root motion off. Honours the FBX file scale (Meshy exports are cm with a 100x node scale).
    /// Hides stray meshes by name. Packs separate metallic + roughness maps into URP metallic (R) and
    /// smoothness (A = 1 - roughness) so the body does not render as chrome. Does not re-rig, re-skin,
    /// bake axis conversion, or touch the skinned bow.
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
            SetYaw(attack ? _spec.AttackYawDegrees : 0f);
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
        }

        public void Dispose()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }

        private void SetYaw(float degrees)
        {
            if (_instance != null)
            {
                _instance.transform.localRotation = Quaternion.Euler(0f, degrees, 0f);
            }
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

            // Meshy FBX: UnitScaleFactor 1 (cm) with a 100x node scale. Honour the file scale so Rowan is ~1.7 m.
            if (!importer.useFileScale)
            {
                importer.useFileScale = true;
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
            var defaults = importer.defaultClipAnimations;
            if (defaults == null || defaults.Length == 0)
            {
                Debug.LogError(_spec.Name + " Meshy clip has no take. Wanted " + spec.TakeName);
                return false;
            }

            ModelImporterClipAnimation? best = null;
            foreach (var candidate in defaults)
            {
                var take = candidate.takeName ?? string.Empty;
                if (string.Equals(take, spec.TakeName, System.StringComparison.Ordinal)
                    || take.EndsWith("|" + spec.MeshyAnimation, System.StringComparison.Ordinal))
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

            var loop = !string.Equals(spec.PoseName, MeshyRigSpec.AttackPoseName, System.StringComparison.Ordinal);
            var already = importer.clipAnimations;
            if (already != null && already.Length == 1
                && string.Equals(already[0].name, spec.PoseName, System.StringComparison.Ordinal)
                && string.Equals(already[0].takeName, best.takeName, System.StringComparison.Ordinal)
                && already[0].loopTime == loop
                && !already[0].mirror)
            {
                return false;
            }

            // Keep the take's own frame range (file frame rate); only name, loop, and in-place flags change.
            best.name = spec.PoseName;
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
    }
}
