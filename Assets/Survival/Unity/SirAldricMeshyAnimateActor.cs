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
    /// Aldric World proof actor: Meshy Animate FBX humanoid + Walking clip AS-IS.
    /// Path A weight-paint is CANCELLED. Do not remap sheath / Hand_R / ghost legs.
    /// World LIGHT + MATERIAL punch. Binds Meshy albedo from the FBX pack
    /// (Humanoid import often drops texture links). Design PASS not claimed.
    /// </summary>
    public sealed class SirAldricMeshyAnimateActor : MonoBehaviour
    {
        public const string ThemePackFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/sir_aldric_meshy_animate_walk.fbx";
        public const string ThemePackAttackFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/sir_aldric_meshy_animate_attack.fbx";
        public const string ClipHint = "Walking";
        public const string AttackClipHint = "Attack";
        /// <summary>FBX stack take on the Design drop. clipAnimations.takeName must match (#25). Short name "Attack" is the imported clip.</summary>
        public const string AttackTakeName = "target_character|rigify_clip|BaseLayer";
        /// <summary>Design / Meshy package label. Discovery uses Attack / Slash / Sword / clip0 / this take.</summary>
        public const string AttackClipDiscovery = "Meshy Lionguard Knight · Standing Sword Slash Attack";
        /// <summary>
        /// Derek FAIL: playing the Rigify-named attack clip on the Mixamo walk Humanoid
        /// squashes limbs. Attack plays on its own FBX instance until Design re-exports
        /// on the walk Mixamo rig (mixamorig:*). Do not invent bones. Path A cancelled.
        /// </summary>
        public const string AttackNativeInstanceReason =
            "Attack FBX bones are Rigify-named (Hips/Spine02); walk is mixamorig. Same bind lengths, different names/hierarchy — Humanoid retarget across them is the squash. Native instance until same-rig drop.";
        /// <summary>
        /// Yaw so imported Mixamo forward (−Z, face to Play cam) becomes world +Z.
        /// Camera SoT: SirAldricDemo (0, 2.80, −5.40) LookAt (0, 0.90, 0.50) → view +Z.
        /// SirAldric3DMotion: march / character forward = +Z = screen TOP; rear view = back to camera.
        /// </summary>
        public const float RearYawDegrees = SirAldric3DMotion.MixamoImportRearYawDegrees;

        private Animator? _walkAnimator;
        private Animator? _attackAnimator;
        private PlayableGraph _walkGraph;
        private PlayableGraph _attackGraph;
        private AnimationClipPlayable _walkPlayable;
        private AnimationClipPlayable _attackPlayable;
        private float _walkLength;
        private float _attackLength;
        private bool _hasAttack;
        private bool _attackOnNativeInstance;
        private bool _walkGraphReady;
        private bool _attackGraphReady;
        private GameObject? _walkInstance;
        private GameObject? _attackInstance;

        public bool Built => _walkGraphReady;

        /// <summary>True only when Design has dropped an Attack clip. Never a hand-baked fake.</summary>
        public bool HasAttackClip => _hasAttack;

        /// <summary>True when attack plays on the attack FBX instance (incompatible Mixamo↔Rigify retarget avoided).</summary>
        public bool AttackOnNativeInstance => _attackOnNativeInstance;

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
            _walkInstance.name = "SirAldricMeshyAnimateWalk";
            HideJunk(_walkInstance);
            FaceWorldTop(_walkInstance);
            PunchMaterials(_walkInstance, ThemePackFbx);

            var walk = LoadWalkingClip();
            if (walk == null)
            {
                Debug.LogError("Meshy Animate FBX has no Walking clip after Humanoid import.");
                return;
            }

            walk.wrapMode = WrapMode.Loop;
            _walkLength = walk.length;
            _walkAnimator = EnsureAnimator(_walkInstance, FbxAssetPath);
            _walkGraph = PlayableGraph.Create("SirAldricMeshyWalk");
            _walkGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var walkOut = AnimationPlayableOutput.Create(_walkGraph, "AldricWalk", _walkAnimator);
            _walkPlayable = AnimationClipPlayable.Create(_walkGraph, walk);
            walkOut.SetSourcePlayable(_walkPlayable);
            _walkGraph.Play();
            _walkGraphReady = true;

            TryBuildAttack();
            SampleLoop(0f);
        }

        private void TryBuildAttack()
        {
            var attackRel = FirstExistingAttackRel();
            if (attackRel == null)
            {
                return;
            }

            var clip = LoadAttackClipFrom(attackRel);
            if (clip == null || clip.length <= 0.05f)
            {
                return;
            }

            clip.wrapMode = WrapMode.Once;
            _attackLength = clip.length;
            _hasAttack = true;

            // Always play attack on the attack FBX instance + its own avatar.
            // Rigify-named clip on mixamorig walk Humanoid was the squash (Derek FAIL).
            // When Design re-exports on the walk Mixamo rig, this still works (clip+mesh same file).
            var attackPrefab = LoadFbxPrefab(ThemePackAttackFbx, "sir_aldric_meshy_animate_attack");
            if (attackPrefab == null)
            {
                Debug.LogWarning(AttackNativeInstanceReason + " Attack prefab missing after import.");
                _hasAttack = false;
                return;
            }

            _attackInstance = Instantiate(attackPrefab, transform);
            _attackInstance.name = "SirAldricMeshyAnimateAttack";
            HideJunk(_attackInstance);
            FaceWorldTop(_attackInstance);
            PunchMaterials(_attackInstance, ThemePackAttackFbx);
            _attackInstance.SetActive(false);
            _attackAnimator = EnsureAnimator(_attackInstance, attackRel);
            _attackGraph = PlayableGraph.Create("SirAldricMeshyAttack");
            _attackGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var atkOut = AnimationPlayableOutput.Create(_attackGraph, "AldricAttack", _attackAnimator);
            _attackPlayable = AnimationClipPlayable.Create(_attackGraph, clip);
            atkOut.SetSourcePlayable(_attackPlayable);
            _attackGraph.Play();
            _attackGraphReady = true;
            _attackOnNativeInstance = true;
        }

        private void Update()
        {
            if (!_walkGraphReady || _walkLength <= 0f)
            {
                return;
            }

            SampleLoop(Time.unscaledTime);
        }

        private void SampleLoop(float timeSeconds)
        {
            var walkBlock = _walkLength * SirAldric3DMotion.WalkCyclesBeforeAttack;
            var loop = walkBlock + (_hasAttack ? _attackLength : 0f);
            if (loop <= 0f)
            {
                return;
            }

            var t = timeSeconds % loop;
            var attacking = _hasAttack && t >= walkBlock;
            if (_walkInstance != null)
            {
                _walkInstance.SetActive(!attacking);
            }

            if (_attackInstance != null)
            {
                _attackInstance.SetActive(attacking);
            }

            if (!attacking)
            {
                if (_walkGraphReady && _walkGraph.IsValid())
                {
                    _walkPlayable.SetTime(t % _walkLength);
                    _walkGraph.Evaluate();
                }

                return;
            }

            var at = Mathf.Clamp(t - walkBlock, 0f, _attackLength);
            if (_attackGraphReady && _attackGraph.IsValid())
            {
                _attackPlayable.SetTime(at);
                _attackGraph.Evaluate();
            }
        }

        public string PhaseLabel(float timeSeconds)
        {
            if (_hasAttack && _walkLength > 0f)
            {
                var walkBlock = _walkLength * SirAldric3DMotion.WalkCyclesBeforeAttack;
                var loop = walkBlock + _attackLength;
                if (loop > 0f && (timeSeconds % loop) >= walkBlock)
                {
                    return "ATTACK  ·  toward TOP  ·  Meshy Animate";
                }
            }

            return "WALK  ·  toward TOP  ·  Meshy Animate";
        }

        private void OnDestroy()
        {
            if (_walkGraphReady && _walkGraph.IsValid())
            {
                _walkGraph.Destroy();
            }

            if (_attackGraphReady && _attackGraph.IsValid())
            {
                _attackGraph.Destroy();
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

        private static string? FirstExistingAttackRel()
        {
            foreach (var rel in AttackFbxAssetPaths())
            {
                if (ThemePackFileExists(rel))
                {
                    return rel;
                }
            }

            return null;
        }

        private static bool ThemePackFileExists(string assetsRel)
        {
            var cwd = Directory.GetCurrentDirectory();
            var data = Application.dataPath ?? Path.Combine(cwd, "Assets");
            var repo = Directory.GetParent(data)?.FullName ?? cwd;
            var trimmed = assetsRel.Replace('\\', '/');
            if (trimmed.StartsWith("Assets/", StringComparison.Ordinal))
            {
                trimmed = trimmed.Substring("Assets/".Length);
            }

            return File.Exists(Path.Combine(data, trimmed)) || File.Exists(Path.Combine(repo, "Assets", trimmed));
        }

        /// <summary>Walk Mixamo uses mixamorig:*. Design same-rig re-export will too. Current attack does not.</summary>
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

        private static AnimationClip? LoadAttackClipFrom(string rel)
        {
#if UNITY_EDITOR
            var clip = PickAttackClip(rel);
            if (clip != null)
            {
                return clip;
            }

            if (RepairAttackTake(rel))
            {
                return PickAttackClip(rel);
            }
#endif
            return null;
        }

        private static AnimationClip? LoadAttackClip()
        {
            var rel = FirstExistingAttackRel();
            return rel == null ? null : LoadAttackClipFrom(rel);
        }

        private static string[] AttackFbxAssetPaths()
        {
            return new[]
            {
                "Assets/" + ThemePackAttackFbx.Replace('\\', '/'),
                "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/sir_aldric_meshy_animate_attack.fbx",
                "Assets/Survival/Art/sir_aldric_meshy_animate_attack.fbx",
            };
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

        private static AnimationClip? PickAttackClip(string rel)
        {
            return PickNamedClip(rel, AttackClipHint, "Slash", "Strike", "Punch", "Sword", "rigify", "clip0", "baselayer", "Lionguard")
                   ?? PickLongestClip(rel);
        }

        private static AnimationClip? PickLongestClip(string rel)
        {
#if UNITY_EDITOR
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
            }

            return longest;
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
                var hit = label.IndexOf("rigify", StringComparison.OrdinalIgnoreCase) >= 0
                          || label.IndexOf("clip0", StringComparison.OrdinalIgnoreCase) >= 0
                          || label.IndexOf("baselayer", StringComparison.OrdinalIgnoreCase) >= 0
                          || label.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0
                          || label.IndexOf("Slash", StringComparison.OrdinalIgnoreCase) >= 0
                          || label.IndexOf("Sword", StringComparison.OrdinalIgnoreCase) >= 0
                          || string.Equals(candidate.takeName, AttackTakeName, StringComparison.Ordinal);
                if (!hit)
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

            if (best == null)
            {
                foreach (var candidate in defaults)
                {
                    var span = candidate.lastFrame - candidate.firstFrame;
                    if (best == null || span > bestSpan)
                    {
                        best = candidate;
                        bestSpan = span;
                    }
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

        private static AnimationClip? PickNamedClip(string rel, params string[] hints)
        {
#if UNITY_EDITOR
            AnimationClip? best = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(rel))
            {
                if (obj is not AnimationClip clip || clip.name.StartsWith("__preview", StringComparison.Ordinal))
                {
                    continue;
                }

                var hit = false;
                foreach (var hint in hints)
                {
                    if (clip.name.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        hit = true;
                        break;
                    }
                }

                if (!hit)
                {
                    continue;
                }

                if (best == null || clip.length > best.length)
                {
                    best = clip;
                }
            }

            return best;
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
            var extracted = ExtractFbxPng(themePackRel, color: true, grayIndex: -1)
                            ?? (useCache ? LoadSiblingPng("sir_aldric_meshy_atlas.png") : null);
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
