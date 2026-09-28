using System;
using System.Collections.Generic;
using System.IO;
using Survival.Domain.Heroes;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Survival.Unity
{
    /// <summary>
    /// Sir Aldric PILOT (2026-09-27): AccuRIG Humanoid look + Walk + Attack clips.
    /// Sword is fused in the body mesh — no Path A / ClipSword / AimChain / empty-scabbard magic.
    /// Look SoT = AccuRIG mid280k + AccuRIG metallic/roughness. HOLD merge. No Design/Derek PASS.
    /// </summary>
    public sealed class SirAldricMeshyAnimateActor : MonoBehaviour
    {
        public const string ThemePackFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot/SirAldric_PILOT_accurig_humanoid.fbx";
        public const string ThemePackWalkFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot/SirAldric_PILOT_walk.fbx";
        public const string ThemePackAttackFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot/SirAldric_PILOT_attack.fbx";
        public const string PaintedLookDir = "ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot";
        public const string LeftoverMeshyWalkFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/sir_aldric_meshy_animate_walk.fbx";
        public const string ClipHint = "Walking";
        public const string AttackClipHint = "Attack";
        public const float RearYawDegrees = SirAldric3DMotion.MixamoImportRearYawDegrees;
        public const string AttackAuthoredReason =
            "PILOT SoT: AccuRIG Humanoid + SirAldric_PILOT_walk + SirAldric_PILOT_attack. " +
            "Sword fused — no Path A / ClipSword / AimChain. HOLD merge.";

        private PlayableGraph _walkGraph;
        private PlayableGraph _attackGraph;
        private AnimationClipPlayable _walkPlayable;
        private AnimationClipPlayable _attackPlayable;
        private float _walkLength;
        private float _attackLength;
        private bool _graphReady;
        private bool _attackReady;
        private GameObject? _walkInstance;
        private GameObject? _attackInstance;

        public bool Built => _graphReady;
        public bool HasAttackClip => _attackReady;
        public float WalkLength => _walkLength;
        public float AttackLength => _attackLength > 0.05f ? _attackLength : 0f;

        public void Build()
        {
            BuildLights();
            var walkPrefab = LoadFbxPrefab(ThemePackWalkFbx, "SirAldric_PILOT_walk");
            if (walkPrefab == null)
            {
                Debug.LogError(
                    "PILOT walk FBX not imported yet. Open Unity so " +
                    ThemePackWalkFbx + " Humanoid-imports, then Play SirAldric.");
                return;
            }

            var walk = LoadClip(ThemePackWalkFbx, ClipHint, repairWalk: true);
            if (walk == null)
            {
                Debug.LogError("PILOT walk FBX has no Walking clip after Humanoid import.");
                return;
            }

            walk.wrapMode = WrapMode.Loop;
            _walkLength = walk.length;
            var walkAnimator = BindPlaybackInstance(
                walkPrefab,
                "SirAldricPilotWalk",
                ThemePackWalkFbx,
                out _walkInstance);
            _walkGraph = PlayableGraph.Create("SirAldricPilotWalk");
            _walkGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var walkOut = AnimationPlayableOutput.Create(_walkGraph, "Walk", walkAnimator);
            _walkPlayable = AnimationClipPlayable.Create(_walkGraph, walk);
            walkOut.SetSourcePlayable(_walkPlayable);
            _walkGraph.Play();

            var attack = LoadClip(ThemePackAttackFbx, AttackClipHint, repairWalk: false);
            var attackPrefab = LoadFbxPrefab(ThemePackAttackFbx, "SirAldric_PILOT_attack");
            if (attack != null && attackPrefab != null)
            {
                attack.wrapMode = WrapMode.Once;
                _attackLength = attack.length;
                var attackAnimator = BindPlaybackInstance(
                    attackPrefab,
                    "SirAldricPilotAttack",
                    ThemePackAttackFbx,
                    out _attackInstance);
                _attackGraph = PlayableGraph.Create("SirAldricPilotAttack");
                _attackGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var attackOut = AnimationPlayableOutput.Create(_attackGraph, "Attack", attackAnimator);
                _attackPlayable = AnimationClipPlayable.Create(_attackGraph, attack);
                attackOut.SetSourcePlayable(_attackPlayable);
                _attackGraph.Play();
                _attackReady = true;
                _attackInstance!.SetActive(false);
                Debug.Log(
                    "PILOT clips walkHuman=" + walk.isHumanMotion +
                    " attackHuman=" + attack.isHumanMotion +
                    " walkAvatar=" + (walkAnimator.avatar != null && walkAnimator.avatar.isHuman) +
                    " attackAvatar=" + (attackAnimator.avatar != null && attackAnimator.avatar.isHuman));
            }
            else
            {
                Debug.LogWarning("PILOT attack clip or FBX missing. Walk-only until reimport.");
                Debug.Log("PILOT clips walkHuman=" + walk.isHumanMotion);
            }

            _graphReady = true;
            SampleAt(0f);
            Debug.Log("PILOT actor built walkLen=" + _walkLength + " attackLen=" + _attackLength + " " + AttackAuthoredReason);
        }

        private Animator BindPlaybackInstance(
            GameObject prefab,
            string name,
            string themePackRel,
            out GameObject instance)
        {
            instance = Instantiate(prefab, transform);
            instance.name = name;
            HideJunk(instance);
            FaceWorldTop(instance);
            BindPaintedLook(instance);
            var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
            var avatar = LoadHumanoidAvatar(AssetPath(themePackRel));
            if (avatar != null)
            {
                animator.avatar = avatar;
            }

            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            return animator;
        }

        private void Update()
        {
            if (!_graphReady)
            {
                return;
            }

            SampleAt(Time.unscaledTime);
        }

        public void SampleAt(float timeSeconds)
        {
            var walkLen = _walkLength > 0.05f ? _walkLength : 1f;
            var walkBlock = walkLen * SirAldric3DMotion.WalkCyclesBeforeAttack;
            var loop = walkBlock + (_attackReady ? Mathf.Max(_attackLength, 0.01f) : walkBlock);
            var t = timeSeconds % loop;
            var attacking = _attackReady && t >= walkBlock;
            if (attacking)
            {
                if (_walkInstance != null)
                {
                    _walkInstance.SetActive(false);
                }

                if (_attackInstance != null)
                {
                    _attackInstance.SetActive(true);
                }

                if (_attackGraph.IsValid())
                {
                    _attackPlayable.SetTime(t - walkBlock);
                    _attackGraph.Evaluate();
                }

                return;
            }

            if (_attackInstance != null)
            {
                _attackInstance.SetActive(false);
            }

            if (_walkInstance != null)
            {
                _walkInstance.SetActive(true);
            }

            if (_walkGraph.IsValid())
            {
                _walkPlayable.SetTime(t % walkLen);
                _walkGraph.Evaluate();
            }
        }

        public string PhaseLabel(float timeSeconds)
        {
            var walkLen = _walkLength > 0.05f ? _walkLength : 1f;
            var walkBlock = walkLen * SirAldric3DMotion.WalkCyclesBeforeAttack;
            if (_attackReady && timeSeconds % (walkBlock + Mathf.Max(_attackLength, 0.01f)) >= walkBlock)
            {
                return "ATTACK  ·  TOP  ·  PILOT fused sword";
            }

            return "WALK  ·  toward TOP  ·  PILOT AccuRIG";
        }

        private void OnDestroy()
        {
            if (_walkGraph.IsValid())
            {
                _walkGraph.Destroy();
            }

            if (_attackGraph.IsValid())
            {
                _attackGraph.Destroy();
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
            // AccuRIG / Mixamo instantiate face-to-camera (−Z) = frontal FAIL.
            // Yaw 180 so transform.forward = +Z: back to camera, walk toward TOP.
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.Euler(0f, RearYawDegrees, 0f);
        }

        private static string AssetPath(string themePackRel) => "Assets/" + themePackRel.Replace('\\', '/');

        private static GameObject? LoadFbxPrefab(string themePackRel, string resourcesName)
        {
#if UNITY_EDITOR
            var rel = AssetPath(themePackRel);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(rel);
            if (go != null)
            {
                return go;
            }

            var data = Application.dataPath;
            var abs = Path.Combine(Directory.GetParent(data)!.FullName, rel);
            if (File.Exists(abs))
            {
                AssetDatabase.ImportAsset(rel);
                return AssetDatabase.LoadAssetAtPath<GameObject>(rel);
            }
#endif
            return Resources.Load<GameObject>(resourcesName);
        }

        private static Avatar? LoadHumanoidAvatar(string rel)
        {
#if UNITY_EDITOR
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(rel))
            {
                if (obj is Avatar avatar)
                {
                    return avatar;
                }
            }
#endif
            return null;
        }

        private static AnimationClip? LoadClip(string themePackRel, string hint, bool repairWalk)
        {
#if UNITY_EDITOR
            var rel = AssetPath(themePackRel);
            var clip = PickClip(rel, hint);
            if (clip != null)
            {
                return clip;
            }

            Debug.LogWarning(
                "PILOT LoadClip empty for " + rel + " hint=" + hint +
                " assets=" + DumpAssets(rel) + ". Repairing from defaultClipAnimations.");

            if (RepairClipTake(rel, walk: repairWalk))
            {
                clip = PickClip(rel, hint);
                if (clip != null)
                {
                    return clip;
                }
            }

            Debug.LogError("PILOT LoadClip still empty after repair. " + rel + " assets=" + DumpAssets(rel));
            return PickClip(rel, hint);
#else
            return null;
#endif
        }

        private static string DumpAssets(string rel)
        {
#if UNITY_EDITOR
            var parts = new System.Collections.Generic.List<string>();
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(rel))
            {
                if (obj == null)
                {
                    continue;
                }

                parts.Add(obj.GetType().Name + ":" + obj.name);
            }

            return parts.Count == 0 ? "(none)" : string.Join(", ", parts);
#else
            return "";
#endif
        }

        private static AnimationClip? PickClip(string rel, string hint)
        {
#if UNITY_EDITOR
            AnimationClip? exact = null;
            AnimationClip? named = null;
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

                var hit = clip.name.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0
                          || (hint == AttackClipHint && clip.name.IndexOf("BaseLayer", StringComparison.OrdinalIgnoreCase) >= 0);
                if (!hit)
                {
                    continue;
                }

                if (string.Equals(clip.name, hint, StringComparison.OrdinalIgnoreCase))
                {
                    if (exact == null || clip.length > exact.length)
                    {
                        exact = clip;
                    }

                    continue;
                }

                if (named == null || clip.length > named.length)
                {
                    named = clip;
                }
            }

            return exact ?? named ?? longest;
#else
            return null;
#endif
        }

        private static bool RepairWalkingTake(string rel) => RepairClipTake(rel, walk: true);

        private static bool RepairAttackTake(string rel) => RepairClipTake(rel, walk: false);

        private static bool RepairClipTake(string rel, bool walk)
        {
#if UNITY_EDITOR
            if (AssetImporter.GetAtPath(rel) is not ModelImporter importer)
            {
                return false;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            var defaults = importer.defaultClipAnimations;
            if (defaults == null || defaults.Length == 0)
            {
                Debug.LogError("PILOT repair " + rel + " defaultClipAnimations empty.");
                importer.SaveAndReimport();
                return false;
            }

            var best = PickDefaultTake(defaults, walk);
            if (best == null)
            {
                Debug.LogError("PILOT repair " + rel + " no usable take. " + DumpTakes(defaults));
                return false;
            }

            best.name = walk ? ClipHint : AttackClipHint;
            best.loopTime = walk;
            best.loop = walk;
            best.keepOriginalOrientation = true;
            best.keepOriginalPositionY = true;
            best.keepOriginalPositionXZ = true;
            importer.clipAnimations = new[] { best };
            importer.SaveAndReimport();
            Debug.Log(
                "PILOT repair " + rel + " takeName=" + best.takeName +
                " frames=" + best.firstFrame + "-" + best.lastFrame +
                " " + DumpTakes(defaults));
            return true;
#else
            return false;
#endif
        }

#if UNITY_EDITOR
        private static ModelImporterClipAnimation? PickDefaultTake(ModelImporterClipAnimation[] defaults, bool walk)
        {
            ModelImporterClipAnimation? best = null;
            var bestSpan = -1f;
            foreach (var candidate in defaults)
            {
                var label = (candidate.takeName ?? "") + "\n" + (candidate.name ?? "");
                var span = candidate.lastFrame - candidate.firstFrame;
                if (walk)
                {
                    if (label.IndexOf("Walk", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                }
                else
                {
                    var named = label.IndexOf("rigify", StringComparison.OrdinalIgnoreCase) >= 0
                                || label.IndexOf("BaseLayer", StringComparison.OrdinalIgnoreCase) >= 0
                                || label.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0
                                || label.IndexOf("clip0", StringComparison.OrdinalIgnoreCase) >= 0
                                || label.IndexOf("Scene", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!named && span < 8f)
                    {
                        continue;
                    }
                }

                if (best == null || span > bestSpan)
                {
                    best = candidate;
                    bestSpan = span;
                }
            }

            if (best != null)
            {
                return best;
            }

            foreach (var candidate in defaults)
            {
                var span = candidate.lastFrame - candidate.firstFrame;
                if (best == null || span > bestSpan)
                {
                    best = candidate;
                    bestSpan = span;
                }
            }

            return best;
        }

        private static string DumpTakes(ModelImporterClipAnimation[] defaults)
        {
            var parts = new string[defaults.Length];
            for (var i = 0; i < defaults.Length; i++)
            {
                var c = defaults[i];
                parts[i] = (c.takeName ?? "?") + "[" + c.firstFrame + "-" + c.lastFrame + "]";
            }

            return string.Join(" | ", parts);
        }
#endif

        private static Texture2D? _cachedAlbedo;
        private static Texture2D? _cachedMetallic;
        private static Texture2D? _cachedRoughness;
        private static readonly Dictionary<int, Texture2D> PackedMetSmoothBySource = new();

        private static void BindPaintedLook(GameObject root)
        {
            var albedo = LoadPilotAlbedo();
            var metallic = LoadPilotMap("Meshy_AI_SirAldric_PILOT_mid28_biped_texture_0_metallic.png", sRgb: false)
                           ?? LoadMeshyPackedMap(0);
            var roughness = LoadPilotMap("Meshy_AI_SirAldric_PILOT_mid28_biped_texture_0_roughness.png", sRgb: false)
                            ?? LoadMeshyPackedMap(1);
            if (albedo == null)
            {
                Debug.LogError("PILOT albedo missing. Extract from walk FBX or AccuRIG import.");
            }

            var painted = 0;
            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                if (rend.name.IndexOf("Ico", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                var mat = NewLit();
                BindMapsAndPunch(mat, albedo, metallic, roughness);
                rend.material = mat;
                painted++;
            }

            Debug.Log(
                "PILOT BindPaintedLook renderers=" + painted +
                " albedo=" + (albedo != null ? albedo.width + "x" + albedo.height : "null") +
                " packedMetSmooth=" + PackedMetSmoothBySource.Count);
        }

        private static void BindMapsAndPunch(
            Material mat,
            Texture2D? albedo,
            Texture2D? metallic,
            Texture2D? roughness)
        {
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

                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", Color.white);
                }

                mat.EnableKeyword("_BASEMAP");
                mat.EnableKeyword("_MAINTEX");
            }

            if (mat.HasProperty("_WorkflowMode"))
            {
                mat.SetFloat("_WorkflowMode", 1f);
            }

            mat.DisableKeyword("_SPECULAR_SETUP");
            var packed = PackMetallicSmoothness(metallic, roughness);
            if (packed != null && mat.HasProperty("_MetallicGlossMap"))
            {
                mat.SetTexture("_MetallicGlossMap", packed);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                if (mat.HasProperty("_Metallic"))
                {
                    mat.SetFloat("_Metallic", 1f);
                }

                if (mat.HasProperty("_Smoothness"))
                {
                    mat.SetFloat("_Smoothness", 1f);
                }
            }
        }

        private static Texture2D? PackMetallicSmoothness(Texture2D? metallic, Texture2D? roughness)
        {
            if (metallic == null)
            {
                return null;
            }

            if (PackedMetSmoothBySource.TryGetValue(metallic.GetInstanceID(), out var hit))
            {
                return hit;
            }

            try
            {
                var w = metallic.width;
                var h = metallic.height;
                var met = metallic.GetPixels();
                Color[]? roughPx = null;
                if (roughness != null && roughness.width == w && roughness.height == h)
                {
                    try
                    {
                        roughPx = roughness.GetPixels();
                    }
                    catch (UnityException)
                    {
                    }
                }

                var packed = new Texture2D(w, h, TextureFormat.RGBA32, false, linear: true);
                var outp = new Color[met.Length];
                for (var i = 0; i < met.Length; i++)
                {
                    var m = met[i].r;
                    var sm = roughPx != null ? Mathf.Clamp01(1f - roughPx[i].r) : 0.35f;
                    outp[i] = new Color(m, m, m, sm);
                }

                packed.SetPixels(outp);
                packed.Apply(false, false);
                packed.wrapMode = TextureWrapMode.Repeat;
                packed.filterMode = FilterMode.Bilinear;
                packed.name = metallic.name + "_metallic_smoothness";
                PackedMetSmoothBySource[metallic.GetInstanceID()] = packed;
                return packed;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("PILOT metallic pack skipped: " + ex.Message);
                return null;
            }
        }

        private static Material NewLit()
        {
            var shader = ResolveLookShader();
            if (shader == null)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                var fallback = quad.GetComponent<Renderer>().sharedMaterial;
                UnityEngine.Object.Destroy(quad);
                return new Material(fallback);
            }

            return new Material(shader);
        }

        private static Shader? ResolveLookShader()
        {
            var rp = GraphicsSettings.currentRenderPipeline;
            if (rp != null && rp.defaultMaterial != null && rp.defaultMaterial.shader != null)
            {
                return rp.defaultMaterial.shader;
            }

            foreach (var name in new[]
                     {
                         "Universal Render Pipeline/Lit",
                         "Universal Render Pipeline/Simple Lit",
                         "Standard",
                         "Unlit/Texture"
                     })
            {
                var found = Shader.Find(name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Texture2D? LoadPilotAlbedo()
        {
            if (_cachedAlbedo != null)
            {
                return _cachedAlbedo;
            }

            _cachedAlbedo = LoadPilotMap("pilot_albedo.png", sRgb: true);
            if (_cachedAlbedo != null)
            {
                return _cachedAlbedo;
            }

#if UNITY_EDITOR
            foreach (var rel in new[] { AssetPath(ThemePackFbx), AssetPath(ThemePackWalkFbx) })
            {
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(rel))
                {
                    if (obj is not Texture2D tex || tex.width < 256)
                    {
                        continue;
                    }

                    var n = tex.name.ToLowerInvariant();
                    if ((n.Contains("texture_0") || n.Contains("basecolor") || n.Contains("image_0") || n.Contains("albedo"))
                        && !n.Contains("metallic")
                        && !n.Contains("rough")
                        && !n.Contains("normal"))
                    {
                        _cachedAlbedo = tex;
                        return tex;
                    }
                }
            }
#endif
            _cachedAlbedo = ExtractFbxPng(ThemePackWalkFbx, color: true, grayIndex: -1)
                            ?? ExtractFbxPng(ThemePackFbx, color: true, grayIndex: -1);
            return _cachedAlbedo;
        }

        private static Texture2D? LoadPilotMap(string fileName, bool sRgb)
        {
            var path = ResolveHero3D(fileName);
            if (path == null || !File.Exists(path) || path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            try
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, linear: !sRgb);
                if (tex.LoadImage(File.ReadAllBytes(path)))
                {
                    tex.wrapMode = TextureWrapMode.Repeat;
                    tex.filterMode = FilterMode.Bilinear;
                    tex.name = Path.GetFileNameWithoutExtension(fileName);
                    Debug.Log("PILOT map " + fileName + " " + path);
                    return tex;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            return null;
        }

        private static Texture2D? LoadMeshyPackedMap(int grayIndex)
        {
            var cache = grayIndex == 0 ? _cachedMetallic : _cachedRoughness;
            if (cache != null)
            {
                return cache;
            }

            var extracted = ExtractFbxPng(ThemePackWalkFbx, color: false, grayIndex: grayIndex);
            if (grayIndex == 0)
            {
                _cachedMetallic = extracted;
                return _cachedMetallic;
            }

            _cachedRoughness = extracted;
            return _cachedRoughness;
        }

        private static string? ResolveHero3D(string fileName)
        {
            var cwd = Directory.GetCurrentDirectory();
            var data = Application.dataPath ?? Path.Combine(cwd, "Assets");
            var repo = Directory.GetParent(data)?.FullName ?? cwd;
            var rel = fileName.Replace('\\', '/').TrimStart('/');
            foreach (var path in new[]
                     {
                         Path.Combine(data, PaintedLookDir, rel),
                         Path.Combine(repo, "Assets", PaintedLookDir, rel),
                         Path.Combine(data, "ThemePack/fantasy_kingdom_a/art/heroes/3d", rel),
                         Path.Combine(repo, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "heroes", "3d", rel),
                     })
            {
                if (File.Exists(path) && path.Replace('\\', '/').EndsWith(rel, StringComparison.OrdinalIgnoreCase))
                {
                    return path;
                }
            }

            return null;
        }

        private static string? ResolveFbxPath(string themePackRel)
        {
            var cwd = Directory.GetCurrentDirectory();
            var data = Application.dataPath ?? Path.Combine(cwd, "Assets");
            var repo = Directory.GetParent(data)?.FullName ?? cwd;
            var rel = themePackRel.Replace('\\', '/');
            foreach (var path in new[] { Path.Combine(data, rel), Path.Combine(repo, "Assets", rel) })
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private static Texture2D? ExtractFbxPng(string themePackRel, bool color, int grayIndex)
        {
            var fbx = ResolveFbxPath(themePackRel);
            if (fbx == null)
            {
                return null;
            }

            try
            {
                var bytes = File.ReadAllBytes(fbx);
                var sig = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                var iend = new byte[] { 0x49, 0x45, 0x4E, 0x44 };
                var graySeen = 0;
                var start = 0;
                while (start < bytes.Length)
                {
                    var i = IndexOf(bytes, sig, start);
                    if (i < 0)
                    {
                        break;
                    }

                    var j = IndexOf(bytes, iend, i + 8);
                    if (j < 0)
                    {
                        break;
                    }

                    var end = Math.Min(bytes.Length, j + 8);
                    var png = new byte[end - i];
                    Buffer.BlockCopy(bytes, i, png, 0, png.Length);
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(png) && tex.width >= 256)
                    {
                        var isGray = LooksGrayscale(tex);
                        var isNormal = LooksLikeNormalMap(tex);
                        if (color && !isGray && !isNormal)
                        {
                            tex.wrapMode = TextureWrapMode.Repeat;
                            tex.filterMode = FilterMode.Bilinear;
                            tex.name = "pilot_albedo";
                            return tex;
                        }

                        if (!color && isGray)
                        {
                            if (graySeen == grayIndex)
                            {
                                tex.wrapMode = TextureWrapMode.Repeat;
                                tex.filterMode = FilterMode.Bilinear;
                                tex.name = grayIndex == 0 ? "pilot_metallic" : "pilot_roughness";
                                return tex;
                            }

                            graySeen++;
                        }
                    }

                    start = i + 8;
                }
            }
            catch (IOException)
            {
            }

            return null;
        }

        private static bool LooksGrayscale(Texture2D tex)
        {
            var px = tex.GetPixels(tex.width / 4, tex.height / 4, 1, 1);
            if (px.Length == 0)
            {
                return false;
            }

            var c = px[0];
            return Mathf.Abs(c.r - c.g) < 0.02f && Mathf.Abs(c.g - c.b) < 0.02f;
        }

        private static bool LooksLikeNormalMap(Texture2D tex)
        {
            var px = tex.GetPixels(tex.width / 2, tex.height / 2, 8, 8);
            if (px.Length == 0)
            {
                return false;
            }

            var r = 0f;
            var g = 0f;
            var b = 0f;
            foreach (var c in px)
            {
                r += c.r;
                g += c.g;
                b += c.b;
            }

            var n = px.Length;
            return b / n > 0.75f && r / n > 0.35f && r / n < 0.65f && g / n > 0.35f && g / n < 0.65f;
        }

        private static int IndexOf(byte[] hay, byte[] needle, int start)
        {
            var last = hay.Length - needle.Length;
            for (var i = start; i <= last; i++)
            {
                var ok = true;
                for (var j = 0; j < needle.Length; j++)
                {
                    if (hay[i + j] != needle[j])
                    {
                        ok = false;
                        break;
                    }
                }

                if (ok)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void BuildLights()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.44f, 0.48f);
            RenderSettings.ambientEquatorColor = new Color(0.28f, 0.29f, 0.30f);
            RenderSettings.ambientGroundColor = new Color(0.14f, 0.13f, 0.11f);
            RenderSettings.ambientIntensity = 1.00f;
            RenderSettings.reflectionIntensity = 0.45f;

            var key = UnityEngine.Object.FindFirstObjectByType<Light>();
            if (key == null)
            {
                var sun = new GameObject("AldricKeyLight");
                key = sun.AddComponent<Light>();
            }

            key.type = LightType.Directional;
            key.color = new Color(1f, 0.96f, 0.88f);
            key.intensity = 1.15f;
            key.transform.rotation = Quaternion.Euler(52f, -16f, 0f);

            if (UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length < 2)
            {
                var fillGo = new GameObject("AldricFillLight");
                var fill = fillGo.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.color = new Color(0.82f, 0.88f, 1f);
                fill.intensity = 0.35f;
                fillGo.transform.rotation = Quaternion.Euler(70f, 35f, 0f);
            }
        }
    }
}
