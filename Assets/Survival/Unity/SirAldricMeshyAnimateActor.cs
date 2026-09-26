using System;
using System.IO;
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
    /// Aldric World proof actor: Design SEP Meshy Animate walk + attack on one Humanoid.
    /// SoT = authored clips only. ClipSword / AimChain / procedural sword are not SoT.
    /// Path A weight-paint is CANCELLED. Old fused walk + Standing Sword Slash stay on
    /// disk and are not played. Body-only AccuRIG (no sword prop). Atlas may be missing.
    /// Design PASS not claimed.
    /// </summary>
    public sealed class SirAldricMeshyAnimateActor : MonoBehaviour
    {
        public const string ThemePackFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/SirAldric_SEP_meshy_animate_walk.fbx";
        public const string ThemePackAttackFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/SirAldric_SEP_meshy_animate_attack.fbx";
        /// <summary>Old fused Meshy walk. On disk only — not SoT.</summary>
        public const string LeftoverFusedWalkFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/sir_aldric_meshy_animate_walk.fbx";
        /// <summary>Old Standing Sword Slash. On disk only — not SoT.</summary>
        public const string LeftoverStandingSlashFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/sir_aldric_meshy_animate_attack.fbx";
        public const string ClipHint = "Walking";
        public const string AttackClipHint = "Attack";
        /// <summary>
        /// SEP Meshy clips only: Walking + Draw Slash Forward on the walk Humanoid Avatar.
        /// ClipSword / AimChain are not SoT. Path A cancelled. Body-only AccuRIG.
        /// Atlas may be missing. No Design PASS.
        /// </summary>
        public const string AttackAuthoredReason =
            "SEP Meshy Animate clips only: Walking + Draw Slash Forward on the same Humanoid Avatar. ClipSword / AimChain are not SoT. Path A cancelled. Body-only AccuRIG (no sword prop). Atlas may be missing. No Design PASS.";
        /// <summary>
        /// Yaw so imported Mixamo forward (−Z, face to Play cam) becomes world +Z.
        /// Camera SoT: SirAldricDemo (0, 2.80, −5.40) LookAt (0, 0.90, 0.50) → view +Z.
        /// SirAldric3DMotion: march / character forward = +Z = screen TOP; rear view = back to camera.
        /// </summary>
        public const float RearYawDegrees = SirAldric3DMotion.MixamoImportRearYawDegrees;

        private Animator? _walkAnimator;
        private PlayableGraph _walkGraph;
        private AnimationPlayableOutput _walkOutput;
        private AnimationClipPlayable _walkPlayable;
        private AnimationClipPlayable _attackPlayable;
        private float _walkLength;
        private float _attackLength;
        private bool _walkGraphReady;
        private bool _attackPlayableReady;
        private GameObject? _walkInstance;

        public bool Built => _walkGraphReady;

        /// <summary>True when the SEP Draw Slash Forward clip is loaded onto the walk Humanoid.</summary>
        public bool HasAttackClip => _attackPlayableReady;

        /// <summary>False: attack clip is Humanoid-retargeted onto the walk instance, not a second mesh.</summary>
        public bool AttackOnNativeInstance => false;

        /// <summary>True: both SEP clips play on the walk Mixamo Humanoid Avatar.</summary>
        public bool AttackOnWalkHumanoid => _walkGraphReady && _attackPlayableReady;

        public float WalkLength => _walkLength;

        public float AttackLength => _attackLength > 0.05f ? _attackLength : 0f;

        public void Build()
        {
            BuildLights();
            var walkPrefab = LoadFbxPrefab(ThemePackFbx, "sir_aldric_meshy_animate_walk");
            if (walkPrefab == null)
            {
                Debug.LogError(
                    "Meshy Animate FBX not imported yet. Open the project in Unity so " +
                    ThemePackFbx + " Humanoid-imports, then Play SirAldric.");
                return;
            }

            _walkInstance = Instantiate(walkPrefab, transform);
            _walkInstance.name = "SirAldricSepMeshyAnimate";
            HideJunk(_walkInstance);
            FaceWorldTop(_walkInstance);
            PunchMaterials(_walkInstance, ThemePackFbx);

            var walk = LoadWalkingClip();
            if (walk == null)
            {
                Debug.LogError("SEP walk FBX has no Walking clip after Humanoid import.");
                return;
            }

            walk.wrapMode = WrapMode.Loop;
            _walkLength = walk.length;
            _walkAnimator = EnsureAnimator(_walkInstance, FbxAssetPath);
            _walkGraph = PlayableGraph.Create("SirAldricSepMeshy");
            _walkGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            _walkOutput = AnimationPlayableOutput.Create(_walkGraph, "AldricSep", _walkAnimator);
            _walkPlayable = AnimationClipPlayable.Create(_walkGraph, walk);
            var attack = LoadAttackClip();
            if (attack != null)
            {
                attack.wrapMode = WrapMode.Once;
                _attackLength = attack.length;
                _attackPlayable = AnimationClipPlayable.Create(_walkGraph, attack);
                _attackPlayableReady = true;
            }
            else
            {
                Debug.LogError("SEP attack FBX has no Draw Slash Forward clip after Humanoid import.");
            }

            _walkOutput.SetSourcePlayable(_walkPlayable);
            _walkGraph.Play();
            _walkGraphReady = true;

            SampleAt(0f);
        }

        private void Update()
        {
            if (!_walkGraphReady || _walkLength <= 0f)
            {
                return;
            }

            SampleAt(Time.unscaledTime);
        }

        /// <summary>Drive the walk Humanoid to a loop time (walk plant, then authored draw→strike).</summary>
        public void SampleAt(float timeSeconds)
        {
            if (!_walkGraphReady || !_walkGraph.IsValid() || _walkLength <= 0f)
            {
                return;
            }

            var walkBlock = _walkLength * SirAldric3DMotion.WalkCyclesBeforeAttack;
            var attackLen = _attackPlayableReady ? _attackLength : 0f;
            var loop = walkBlock + Mathf.Max(attackLen, 0.01f);
            var t = timeSeconds % loop;
            if (t < 0f)
            {
                t += loop;
            }

            if (t < walkBlock || !_attackPlayableReady)
            {
                _walkOutput.SetSourcePlayable(_walkPlayable);
                _walkPlayable.SetTime(t % _walkLength);
                _walkGraph.Evaluate();
                return;
            }

            _walkOutput.SetSourcePlayable(_attackPlayable);
            _attackPlayable.SetTime(Mathf.Clamp(t - walkBlock, 0f, _attackLength));
            _walkGraph.Evaluate();
        }

        public string PhaseLabel(float timeSeconds)
        {
            if (_walkLength > 0f)
            {
                var walkBlock = _walkLength * SirAldric3DMotion.WalkCyclesBeforeAttack;
                var loop = walkBlock + Mathf.Max(_attackLength, 0.01f);
                var t = loop > 0f ? timeSeconds % loop : 0f;
                if (t < 0f)
                {
                    t += loop;
                }

                if (t >= walkBlock && _attackPlayableReady)
                {
                    var u = _attackLength > 0f ? (t - walkBlock) / _attackLength : 0f;
                    if (u < 0.22f)
                    {
                        return "ATTACK  ·  DRAW  ·  SEP clip";
                    }

                    if (u < 0.62f)
                    {
                        return "ATTACK  ·  STRIKE TOP  ·  SEP clip";
                    }

                    return "ATTACK  ·  RECOVER  ·  SEP clip";
                }
            }

            return "WALK  ·  toward TOP  ·  SEP Meshy Animate";
        }

        private void OnDestroy()
        {
            if (_walkGraphReady && _walkGraph.IsValid())
            {
                _walkGraph.Destroy();
            }
        }

        private static Animator EnsureAnimator(GameObject root, string avatarRel)
        {
            var animator = root.GetComponent<Animator>();
            if (animator == null)
            {
                animator = root.AddComponent<Animator>();
            }

            if (animator.avatar == null)
            {
                animator.avatar = LoadHumanoidAvatar(avatarRel);
            }

            return animator;
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
            // Verified (not guessed):
            // - Play cam: SirAldricDemo sets (0, 2.80, −5.40) LookAt (0, 0.90, 0.50) → view +Z.
            // - SirAldric3DMotion: character forward / march = world +Z = screen TOP; enemy +Z.
            // - Chevrons / EnemyTop sit at +Z. WorldMarchLoop is a node timer, not a heading.
            // - Humanoid Mixamo on this FBX instantiates face-to-camera (−Z) = frontal FAIL.
            // Yaw 180° so transform.forward = +Z: back to camera, walk toward TOP.
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.Euler(0f, RearYawDegrees, 0f);
        }

        private static GameObject? LoadFbxPrefab(string themePackRel, string resourcesName)
        {
#if UNITY_EDITOR
            var data = Application.dataPath;
            var rel = "Assets/" + themePackRel.Replace('\\', '/');
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
            return Resources.Load<GameObject>(resourcesName);
        }

        private static string FbxAssetPath => "Assets/" + ThemePackFbx.Replace('\\', '/');

        /// <summary>SEP walk is mixamorig:*. SEP attack is MeshyRig (Hips/Spine02) Humanoid-retargeted onto this Avatar.</summary>
        public static bool FbxLooksMixamo(string assetsRel)
        {
#if UNITY_EDITOR
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetsRel);
            if (go != null)
            {
                foreach (var t in go.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.IndexOf("mixamorig", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }

                return false;
            }
#endif
            var path = ResolveFbxPath(assetsRel.Replace("Assets/", "").Replace("Assets\\", ""));
            if (path == null)
            {
                return false;
            }

            try
            {
                var bytes = File.ReadAllBytes(path);
                var needle = System.Text.Encoding.ASCII.GetBytes("mixamorig");
                return IndexOf(bytes, needle, 0) >= 0;
            }
            catch (IOException)
            {
                return false;
            }
        }

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

        private static AnimationClip? LoadAttackClip()
        {
#if UNITY_EDITOR
            var rel = AttackFbxAssetPath;
            var clip = PickAttackClip(rel);
            if (clip != null)
            {
                return clip;
            }

            if (RepairAttackTake(rel))
            {
                return PickAttackClip(rel);
            }

            return null;
#else
            return null;
#endif
        }

        private static string AttackFbxAssetPath => "Assets/" + ThemePackAttackFbx.Replace('\\', '/');

        private static AnimationClip? PickAttackClip(string rel)
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

                if (string.Equals(clip.name, AttackClipHint, StringComparison.OrdinalIgnoreCase))
                {
                    if (exact == null || clip.length > exact.length)
                    {
                        exact = clip;
                    }

                    continue;
                }

                var label = clip.name;
                if (label.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0
                    || label.IndexOf("Slash", StringComparison.OrdinalIgnoreCase) >= 0
                    || label.IndexOf("rigify_clip", StringComparison.OrdinalIgnoreCase) >= 0
                    || label.IndexOf("BaseLayer", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (named == null || clip.length > named.length)
                    {
                        named = clip;
                    }
                }
            }

            return exact ?? named ?? longest;
#else
            return null;
#endif
        }

        private static bool RepairAttackTake(string rel)
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
                var span = candidate.lastFrame - candidate.firstFrame;
                var named = label.IndexOf("rigify_clip", StringComparison.OrdinalIgnoreCase) >= 0
                            || label.IndexOf("BaseLayer", StringComparison.OrdinalIgnoreCase) >= 0
                            || label.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0
                            || label.IndexOf("Slash", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!named && span < 20f)
                {
                    continue;
                }

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

            best.name = AttackClipHint;
            best.loopTime = false;
            best.loop = false;
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

        private static Texture2D? _cachedAlbedo;
        private static Texture2D? _cachedMetallic;
        private static Texture2D? _cachedRoughness;

        private static void PunchMaterials(GameObject root, string themePackRel)
        {
            var albedo = LoadMeshyAlbedo(themePackRel);
            var metallic = LoadMeshyPackedMap(themePackRel, grayIndex: 0);
            var roughness = LoadMeshyPackedMap(themePackRel, grayIndex: 1);
            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                var shared = rend.sharedMaterials;
                if (shared == null || shared.Length == 0)
                {
                    var lone = NewLit();
                    BindMapsAndPunch(lone, albedo, metallic, roughness);
                    rend.material = lone;
                    continue;
                }

                var copies = new Material[shared.Length];
                for (var i = 0; i < shared.Length; i++)
                {
                    var src = shared[i] != null ? shared[i] : NewLit();
                    var mat = new Material(src);
                    BindMapsAndPunch(mat, albedo, metallic, roughness);
                    copies[i] = mat;
                }

                rend.materials = copies;
            }
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
            }

            if (metallic != null && mat.HasProperty("_MetallicGlossMap"))
            {
                mat.SetTexture("_MetallicGlossMap", metallic);
            }

            if (roughness != null && mat.HasProperty("_SpecGlossMap"))
            {
                mat.SetTexture("_SpecGlossMap", roughness);
            }

            if (mat.HasProperty("_BaseColor"))
            {
                var c = mat.GetColor("_BaseColor");
                if (c.maxColorComponent < 0.08f)
                {
                    c = Color.white;
                }

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
        }

        private static Material NewLit()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Unlit/Texture")
                         ?? Shader.Find("Sprites/Default");
            return new Material(shader);
        }

        private static Texture2D? LoadMeshyAlbedo(string themePackRel)
        {
            var useCache = string.Equals(themePackRel, ThemePackFbx, StringComparison.Ordinal);
            if (useCache && _cachedAlbedo != null)
            {
                return _cachedAlbedo;
            }

#if UNITY_EDITOR
            var rel = "Assets/" + themePackRel.Replace('\\', '/');
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(rel))
            {
                if (obj is not Texture2D tex || tex.width < 256)
                {
                    continue;
                }

                var n = tex.name.ToLowerInvariant();
                if ((n.Contains("texture_0") || n.Contains("basecolor") || n.Contains("albedo"))
                    && !n.Contains("metallic")
                    && !n.Contains("rough")
                    && !n.Contains("normal"))
                {
                    if (useCache)
                    {
                        _cachedAlbedo = tex;
                    }

                    return tex;
                }
            }
#endif
            // SEP pack has no embedded atlas (soft leftover). Do not bind the old fused-walk atlas.
            var extracted = ExtractFbxPng(themePackRel, color: true, grayIndex: -1);
            if (useCache)
            {
                _cachedAlbedo = extracted;
            }

            return extracted;
        }

        private static Texture2D? LoadMeshyPackedMap(string themePackRel, int grayIndex)
        {
            var useCache = string.Equals(themePackRel, ThemePackFbx, StringComparison.Ordinal);
            var cache = grayIndex == 0 ? _cachedMetallic : _cachedRoughness;
            if (useCache && cache != null)
            {
                return cache;
            }

            var extracted = ExtractFbxPng(themePackRel, color: false, grayIndex: grayIndex);
            if (!useCache)
            {
                return extracted;
            }

            if (grayIndex == 0)
            {
                _cachedMetallic = extracted ?? LoadSiblingPng("sir_aldric_meshy_atlas_metallic.png");
                return _cachedMetallic;
            }

            _cachedRoughness = extracted ?? LoadSiblingPng("sir_aldric_meshy_atlas_roughness.png");
            return _cachedRoughness;
        }

        private static Texture2D? LoadSiblingPng(string fileName)
        {
            var path = ResolveHero3D(fileName);
            if (path == null)
            {
                return null;
            }

#if UNITY_EDITOR
            var rel = "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/" + fileName;
            var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(rel);
            if (asset != null)
            {
                return asset;
            }
#endif
            try
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(File.ReadAllBytes(path)))
                {
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.filterMode = FilterMode.Bilinear;
                    tex.name = Path.GetFileNameWithoutExtension(fileName);
                    return tex;
                }
            }
            catch (IOException)
            {
            }

            return null;
        }

        private static string? ResolveHero3D(string fileName)
        {
            var cwd = Directory.GetCurrentDirectory();
            var data = Application.dataPath ?? Path.Combine(cwd, "Assets");
            var repo = Directory.GetParent(data)?.FullName ?? cwd;
            foreach (var path in new[]
                     {
                         Path.Combine(data, ThemePackFbx.Replace("sir_aldric_meshy_animate_walk.fbx", fileName)),
                         Path.Combine(repo, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "heroes", "3d", fileName),
                     })
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private static string? ResolveFbxPath(string? themePackRel = null)
        {
            var cwd = Directory.GetCurrentDirectory();
            var data = Application.dataPath ?? Path.Combine(cwd, "Assets");
            var repo = Directory.GetParent(data)?.FullName ?? cwd;
            var rel = (themePackRel ?? ThemePackFbx).Replace('\\', '/');
            if (rel.StartsWith("Assets/", StringComparison.Ordinal))
            {
                rel = rel.Substring("Assets/".Length);
            }
            foreach (var path in new[]
                     {
                         Path.Combine(data, rel),
                         Path.Combine(repo, "Assets", rel),
                     })
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
                        var isGray = tex.format == TextureFormat.Alpha8
                                     || tex.format == TextureFormat.R8
                                     || LooksGrayscale(tex);
                        var isNormal = LooksLikeNormalMap(tex);
                        if (color && !isGray && !isNormal)
                        {
                            tex.wrapMode = TextureWrapMode.Clamp;
                            tex.filterMode = FilterMode.Bilinear;
                            tex.name = "meshy_animate_albedo";
                            return tex;
                        }

                        if (!color && isGray)
                        {
                            if (graySeen == grayIndex)
                            {
                                tex.wrapMode = TextureWrapMode.Clamp;
                                tex.filterMode = FilterMode.Bilinear;
                                tex.name = grayIndex == 0 ? "meshy_animate_metallic" : "meshy_animate_roughness";
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
            r /= n;
            g /= n;
            b /= n;
            return b > 0.75f && r > 0.35f && r < 0.65f && g > 0.35f && g < 0.65f;
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
