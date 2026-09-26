using System;
using System.Collections.Generic;
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
    /// Aldric World proof actor: Design SEP Meshy Animate walk on one Humanoid.
    /// Walk motion SoT = SEP Walking clip (Derek Play PASS). Look = painted mid450k
    /// atlas-stamped AccuRIG + mid80k sword GLB split: empty LookScabbard on
    /// character-LEFT hip, LookSword (blade+hilt) sheathed until draw then parented
    /// to RightHand (2H midpoint on overhead→strike).
    /// Attack SoT = 2H LEFT-hip draw → overhead → two-hand downstrike yawlock.
    /// Old 1H yawlock / nospin / spin / raw 2H, ClipSword / AimChain are not SoT.
    /// Path A weight-paint is CANCELLED.
    /// </summary>
    public sealed class SirAldricMeshyAnimateActor : MonoBehaviour
    {
        public const string ThemePackFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/SirAldric_SEP_meshy_animate_walk.fbx";
        public const string ThemePackAttackFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/SirAldric_SEP_meshy_animate_attack_2h_downstrike_yawlock.fbx";
        public const string PaintedBodyGlb = "ThemePack/fantasy_kingdom_a/art/heroes/3d/SirAldric_SEP_body_nosword_PAINTED_mid450k.glb";
        public const string PaintedSwordGlb = "ThemePack/fantasy_kingdom_a/art/heroes/3d/SirAldric_SEP_sword_scabbard_PAINTED_mid80k.glb";
        /// <summary>mid200k body leftover. On disk only — not look SoT.</summary>
        public const string LeftoverPaintedBodyMid200k = "ThemePack/fantasy_kingdom_a/art/heroes/3d/SirAldric_SEP_body_nosword_PAINTED_mid200k.fbx";
        /// <summary>mid50k sword leftover. On disk only — not look SoT.</summary>
        public const string LeftoverPaintedSwordMid50k = "ThemePack/fantasy_kingdom_a/art/heroes/3d/SirAldric_SEP_sword_scabbard_PAINTED_mid.fbx";
        public const string PaintedLookDir = "ThemePack/fantasy_kingdom_a/art/heroes/3d/sep_paint";
        /// <summary>Old fused Meshy walk. On disk only — not SoT.</summary>
        public const string LeftoverFusedWalkFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/sir_aldric_meshy_animate_walk.fbx";
        /// <summary>Old Standing Sword Slash. On disk only — not SoT.</summary>
        public const string LeftoverStandingSlashFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/sir_aldric_meshy_animate_attack.fbx";
        /// <summary>SEP attack that spins ~180°. On disk only — not SoT.</summary>
        public const string LeftoverSpinAttackFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/SirAldric_SEP_meshy_animate_attack.fbx";
        /// <summary>SEP NoSpin re-author that still spun ~180°. Not imported as SoT.</summary>
        public const string LeftoverNospinAttackFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/SirAldric_SEP_meshy_animate_attack_nospin.fbx";
        /// <summary>1H yawlock leftover. On disk only — not attack SoT.</summary>
        public const string LeftoverOneHandYawlockFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/SirAldric_SEP_meshy_animate_attack_yawlock.fbx";
        /// <summary>Raw Meshy 2H that spins ~177°. Not imported as SoT.</summary>
        public const string LeftoverRawTwoHandAttackFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/SirAldric_SEP_meshy_animate_attack_2h_downstrike.fbx";
        public const string ClipHint = "Walking";
        public const string AttackClipHint = "Attack";
        /// <summary>
        /// Walk motion SoT = SEP Walking. Attack SoT = 2H yawlock clip.
        /// Look = mid450k body + mid80k sword GLB split (empty LEFT-hip scabbard,
        /// blade+hilt parents to RightHand on draw). Path A cancelled.
        /// </summary>
        public const string AttackAuthoredReason =
            "SEP Walking is motion SoT (Derek Play PASS). Attack SoT = SirAldric_SEP_meshy_animate_attack_2h_downstrike_yawlock (LEFT-hip RH draw → overhead → 2H downstrike, ground yaw locked). Raw 2H / 1H yawlock / nospin / spin not SoT. Look: mid450k body + mid80k sword GLB, sep_paint atlas. LookScabbard empty sheath stays character-LEFT hip; LookSword blade+hilt parents to RightHand on draw and follows 2H grip. Combined LookSwordScabbard is not glued for the whole clip. ClipSword / AimChain are not SoT. Path A cancelled.";
        /// <summary>Attack clip normalized time when LookSword leaves the LEFT sheath.</summary>
        public const float LookSwordDrawParentU = 0.05f;
        /// <summary>Overhead onward: blade sits in both hands (RH-biased midpoint).</summary>
        public const float LookSwordTwoHandU = 0.16f;
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
        private Transform? _lookScabbard;
        private Transform? _lookSword;
        private Vector3 _swordHiltLocal;
        private Vector3 _swordTipLocal;

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
            BindPaintedLook(_walkInstance);

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
                Debug.LogError("SEP 2H yawlock attack FBX has no Attack clip after Humanoid import.");
            }

            _walkOutput.SetSourcePlayable(_walkPlayable);
            _walkGraph.Play();
            _walkGraphReady = true;

            SampleAt(0f);
            EnsureLookScabbard();
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
                ParentLookSwordOnDraw(attacking: false, attackU: 0f);
                return;
            }

            var attackT = Mathf.Clamp(t - walkBlock, 0f, _attackLength);
            _walkOutput.SetSourcePlayable(_attackPlayable);
            _attackPlayable.SetTime(attackT);
            _walkGraph.Evaluate();
            ParentLookSwordOnDraw(attacking: true, attackU: _attackLength > 0f ? attackT / _attackLength : 0f);
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
                    if (u < 0.16f)
                    {
                        return "ATTACK  ·  DRAW LEFT  ·  2H YAWLOCK";
                    }

                    if (u < 0.38f)
                    {
                        return "ATTACK  ·  OVERHEAD  ·  2H YAWLOCK";
                    }

                    if (u < 0.55f)
                    {
                        return "ATTACK  ·  DOWNSTRIKE TOP  ·  2H YAWLOCK";
                    }

                    return "ATTACK  ·  RECOVER  ·  2H YAWLOCK";
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
                    || label.IndexOf("BaseLayer", StringComparison.OrdinalIgnoreCase) >= 0
                    || label.IndexOf("Scene", StringComparison.OrdinalIgnoreCase) >= 0
                    || label.IndexOf("yawlock", StringComparison.OrdinalIgnoreCase) >= 0
                    || label.IndexOf("downstrike", StringComparison.OrdinalIgnoreCase) >= 0
                    || label.IndexOf("2h", StringComparison.OrdinalIgnoreCase) >= 0)
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
                            || label.IndexOf("Scene", StringComparison.OrdinalIgnoreCase) >= 0
                            || label.IndexOf("yawlock", StringComparison.OrdinalIgnoreCase) >= 0
                            || label.IndexOf("downstrike", StringComparison.OrdinalIgnoreCase) >= 0
                            || label.IndexOf("2h", StringComparison.OrdinalIgnoreCase) >= 0
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

        private void BindPaintedLook(GameObject root)
        {
            StampLookUvsFromPaintedBody(root);
            var albedo = LoadSepPaintMap("sep_body_basecolor.jpg", sRgb: true);
            var metallic = LoadSepPaintMap("sep_body_metallic.jpg", sRgb: false);
            var roughness = LoadSepPaintMap("sep_body_roughness.jpg", sRgb: false);
            var normal = LoadSepPaintMap("sep_body_normal.jpg", sRgb: false);
            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                if (rend.name.IndexOf("Ico", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                ApplyLookMaterial(rend, albedo, metallic, roughness, normal);
            }
        }

        private void EnsureLookScabbard()
        {
            if (_walkAnimator == null || _walkInstance == null)
            {
                return;
            }

            var hips = _walkAnimator.GetBoneTransform(HumanBodyBones.Hips);
            var thigh = _walkAnimator.GetBoneTransform(HumanBodyBones.LeftUpperLeg) ?? hips;
            if (thigh == null)
            {
                return;
            }

            var prefab = LoadFbxPrefab(PaintedSwordGlb, "SirAldric_SEP_sword_scabbard_PAINTED_mid80k");
            if (prefab == null)
            {
                return;
            }

            var source = Instantiate(prefab, _walkInstance.transform);
            source.name = "LookSwordScabbard";
            var srcMesh = FirstMesh(source);
            if (srcMesh == null)
            {
                return;
            }

            SplitLookSwordAndScabbard(srcMesh, out var swordMesh, out var scabbardMesh);
            var scabbardGo = Instantiate(source, _walkInstance.transform);
            scabbardGo.name = "LookScabbard";
            var swordGo = Instantiate(source, _walkInstance.transform);
            swordGo.name = "LookSword";
            source.SetActive(false);
            AssignMesh(scabbardGo, scabbardMesh);
            AssignMesh(swordGo, swordMesh);

            var albedo = LoadSepPaintMap("sword_basecolor.jpg", sRgb: true);
            var metallic = LoadSepPaintMap("sword_metallic.jpg", sRgb: false);
            var roughness = LoadSepPaintMap("sword_roughness.jpg", sRgb: false);
            var normal = LoadSepPaintMap("sword_normal.jpg", sRgb: false);
            foreach (var rend in scabbardGo.GetComponentsInChildren<Renderer>(true))
            {
                ApplyLookMaterial(rend, albedo, metallic, roughness, normal);
            }

            foreach (var rend in swordGo.GetComponentsInChildren<Renderer>(true))
            {
                ApplyLookMaterial(rend, albedo, metallic, roughness, normal);
            }

            // World after RearYaw 180: character-left = −root.right = −X = viewer-left from behind.
            // Thin-lateral hang mirrored from the 1ac345a RIGHT-hip pose. Parent LeftUpperLeg.
            // Derek SoT: RH draws from LEFT sheath. Empty scabbard stays LEFT hip.
            // LookSword (blade+hilt) sheathes here until draw, then parents to RightHand.
            var pos = thigh.position
                      + _walkInstance.transform.right * -0.06f
                      + Vector3.up * -0.12f;
            var rot = _walkInstance.transform.rotation * Quaternion.Euler(0f, 90f, -90f);
            scabbardGo.transform.SetPositionAndRotation(pos, rot);
            scabbardGo.transform.localScale = Vector3.one * 0.28f;
            scabbardGo.transform.SetParent(thigh, true);
            _lookScabbard = scabbardGo.transform;
            _lookSword = swordGo.transform;
            RememberSwordHiltAndTip(swordMesh);
            SheathLookSword();
        }

        private static Mesh? FirstMesh(GameObject root)
        {
            var skin = root.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skin != null && skin.sharedMesh != null)
            {
                return skin.sharedMesh;
            }

            var filter = root.GetComponentInChildren<MeshFilter>(true);
            return filter != null ? filter.sharedMesh : null;
        }

        private static void AssignMesh(GameObject root, Mesh mesh)
        {
            var skin = root.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skin != null)
            {
                skin.sharedMesh = mesh;
                return;
            }

            var filter = root.GetComponentInChildren<MeshFilter>(true);
            if (filter != null)
            {
                filter.sharedMesh = mesh;
            }
        }

        /// <summary>
        /// Split the combined mid80k sword+scabbard remesh: hilt-end + thin core = blade+hilt;
        /// the rest is the empty LEFT-hip sheath. Not Path A. Not ClipSword.
        /// </summary>
        private static void SplitLookSwordAndScabbard(Mesh src, out Mesh swordMesh, out Mesh scabbardMesh)
        {
            var verts = src.vertices;
            var n = verts.Length;
            var mn = verts[0];
            var mx = verts[0];
            for (var i = 1; i < n; i++)
            {
                mn = Vector3.Min(mn, verts[i]);
                mx = Vector3.Max(mx, verts[i]);
            }

            var c = (mn + mx) * 0.5f;
            var axis = mx - mn;
            axis.y = 0f;
            if (axis.sqrMagnitude < 1e-8f)
            {
                axis = Vector3.right;
            }

            axis.Normalize();
            const float tHilt = 0.70f;
            const float rCore = 0.11f;
            var isSword = new bool[n];
            for (var i = 0; i < n; i++)
            {
                var d = verts[i] - c;
                var t = Vector3.Dot(d, axis);
                var r = (d - axis * t).magnitude;
                isSword[i] = t > tHilt || r < rCore;
            }

            swordMesh = ExtractLabeledVerts(src, isSword, wantSword: true);
            scabbardMesh = ExtractLabeledVerts(src, isSword, wantSword: false);
        }

        private static Mesh ExtractLabeledVerts(Mesh src, bool[] isSword, bool wantSword)
        {
            var srcV = src.vertices;
            var srcUv = src.uv;
            var map = new int[srcV.Length];
            for (var i = 0; i < map.Length; i++)
            {
                map[i] = -1;
            }

            var verts = new List<Vector3>(srcV.Length / 2);
            var uvs = new List<Vector2>(srcV.Length / 2);
            for (var i = 0; i < srcV.Length; i++)
            {
                if (isSword[i] != wantSword)
                {
                    continue;
                }

                map[i] = verts.Count;
                verts.Add(srcV[i]);
                uvs.Add(srcUv != null && i < srcUv.Length ? srcUv[i] : Vector2.zero);
            }

            var tris = src.triangles;
            var outTris = new List<int>(tris.Length / 2);
            for (var i = 0; i < tris.Length; i += 3)
            {
                var a = map[tris[i]];
                var b = map[tris[i + 1]];
                var c = map[tris[i + 2]];
                if (a < 0 || b < 0 || c < 0)
                {
                    continue;
                }

                outTris.Add(a);
                outTris.Add(b);
                outTris.Add(c);
            }

            var mesh = new Mesh { name = src.name + (wantSword ? "_LookSword" : "_LookScabbard") };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(outTris, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private void RememberSwordHiltAndTip(Mesh swordMesh)
        {
            var verts = swordMesh.vertices;
            if (verts == null || verts.Length == 0)
            {
                _swordHiltLocal = Vector3.zero;
                _swordTipLocal = Vector3.right;
                return;
            }

            var mn = verts[0];
            var mx = verts[0];
            for (var i = 1; i < verts.Length; i++)
            {
                mn = Vector3.Min(mn, verts[i]);
                mx = Vector3.Max(mx, verts[i]);
            }

            var c = (mn + mx) * 0.5f;
            var axis = mx - mn;
            axis.y = 0f;
            if (axis.sqrMagnitude < 1e-8f)
            {
                axis = Vector3.right;
            }

            axis.Normalize();
            var hilt = verts[0];
            var tip = verts[0];
            var hiltT = Vector3.Dot(verts[0] - c, axis);
            var tipT = hiltT;
            for (var i = 1; i < verts.Length; i++)
            {
                var t = Vector3.Dot(verts[i] - c, axis);
                if (t > hiltT)
                {
                    hiltT = t;
                    hilt = verts[i];
                }

                if (t < tipT)
                {
                    tipT = t;
                    tip = verts[i];
                }
            }

            _swordHiltLocal = hilt;
            _swordTipLocal = tip;
        }

        /// <summary>
        /// Walk / pre-draw: LookSword sits in the LEFT-hip LookScabbard.
        /// On draw: parent blade+hilt to RightHand. Overhead→strike follows 2H grip
        /// (RH-biased midpoint of RightHand and LeftHand).
        /// </summary>
        private void ParentLookSwordOnDraw(bool attacking, float attackU)
        {
            if (_lookSword == null || _lookScabbard == null || _walkAnimator == null)
            {
                return;
            }

            var rh = _walkAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            if (!attacking || attackU < LookSwordDrawParentU || rh == null)
            {
                SheathLookSword();
                return;
            }

            var lh = _walkAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            var twoHand = attackU >= LookSwordTwoHandU && lh != null;
            var palm = twoHand ? Vector3.Lerp(rh.position, lh!.position, 0.35f) : rh.position;
            var bladeDir = rh.up.sqrMagnitude > 1e-6f ? rh.up.normalized : rh.forward;
            var tipDirLocal = _swordTipLocal - _swordHiltLocal;
            if (tipDirLocal.sqrMagnitude < 1e-8f)
            {
                tipDirLocal = Vector3.right;
            }

            tipDirLocal.Normalize();
            var rot = Quaternion.FromToRotation(tipDirLocal, bladeDir);
            var hiltWorld = rot * (_swordHiltLocal * 0.28f);
            _lookSword.SetParent(null, true);
            _lookSword.SetPositionAndRotation(palm - hiltWorld, rot);
            _lookSword.localScale = Vector3.one * 0.28f;
            _lookSword.SetParent(rh, true);
        }

        private void SheathLookSword()
        {
            if (_lookSword == null || _lookScabbard == null)
            {
                return;
            }

            _lookSword.SetParent(null, true);
            _lookSword.SetPositionAndRotation(_lookScabbard.position, _lookScabbard.rotation);
            _lookSword.localScale = Vector3.one * 0.28f;
            _lookSword.SetParent(_lookScabbard, true);
        }

        private static void ApplyLookMaterial(
            Renderer rend,
            Texture2D? albedo,
            Texture2D? metallic,
            Texture2D? roughness,
            Texture2D? normal)
        {
            var mat = NewLit();
            BindMapsAndPunch(mat, albedo, metallic, roughness);
            if (normal != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normal);
                if (mat.HasProperty("_BumpScale"))
                {
                    mat.SetFloat("_BumpScale", 1f);
                }
            }

            rend.material = mat;
        }

        private static void StampLookUvsFromPaintedBody(GameObject walkRoot)
        {
            var walkFilter = walkRoot.GetComponentInChildren<MeshFilter>();
            var walkSkin = walkRoot.GetComponentInChildren<SkinnedMeshRenderer>();
            Mesh? walkMesh = walkSkin != null ? walkSkin.sharedMesh : walkFilter != null ? walkFilter.sharedMesh : null;
            if (walkMesh == null)
            {
                return;
            }

            var sidecar = LoadLookUvSidecar(walkMesh.vertexCount);
            if (sidecar != null)
            {
                ApplyWalkUv(walkSkin, walkFilter, walkMesh, sidecar);
                return;
            }

            var paintPrefab = LoadFbxPrefab(PaintedBodyGlb, "SirAldric_SEP_body_nosword_PAINTED_mid450k");
            if (paintPrefab == null)
            {
                return;
            }

            var paintFilter = paintPrefab.GetComponentInChildren<MeshFilter>();
            var paintSkin = paintPrefab.GetComponentInChildren<SkinnedMeshRenderer>();
            var paintMesh = paintSkin != null ? paintSkin.sharedMesh : paintFilter != null ? paintFilter.sharedMesh : null;
            if (paintMesh == null || paintMesh.vertexCount < 8 || paintMesh.uv == null || paintMesh.uv.Length == 0)
            {
                return;
            }

            var stamped = StampUvsNearest(walkMesh.vertices, paintMesh.vertices, paintMesh.uv);
            if (stamped != null)
            {
                ApplyWalkUv(walkSkin, walkFilter, walkMesh, stamped);
            }
        }

        private static void ApplyWalkUv(
            SkinnedMeshRenderer? walkSkin,
            MeshFilter? walkFilter,
            Mesh walkMesh,
            Vector2[] uvs)
        {
            var copy = UnityEngine.Object.Instantiate(walkMesh);
            copy.name = walkMesh.name + "_LookUV";
            copy.uv = uvs;
            if (walkSkin != null)
            {
                walkSkin.sharedMesh = copy;
            }
            else if (walkFilter != null)
            {
                walkFilter.sharedMesh = copy;
            }
        }

        private static Vector2[]? LoadLookUvSidecar(int vertexCount)
        {
            var path = ResolveHero3D("sep_paint/sir_aldric_sep_walk_look_uv.bin")
                       ?? ResolveHero3D("sir_aldric_sep_walk_look_uv.bin");
            if (path == null || !File.Exists(path))
            {
                return null;
            }

            try
            {
                var bytes = File.ReadAllBytes(path);
                if (bytes.Length < 8)
                {
                    return null;
                }

                var count = BitConverter.ToInt32(bytes, 0);
                if (count != vertexCount || bytes.Length < 4 + count * 8)
                {
                    return null;
                }

                var uvs = new Vector2[count];
                for (var i = 0; i < count; i++)
                {
                    var o = 4 + i * 8;
                    uvs[i] = new Vector2(BitConverter.ToSingle(bytes, o), BitConverter.ToSingle(bytes, o + 4));
                }

                return uvs;
            }
            catch (IOException)
            {
                return null;
            }
        }

        private static Vector2[]? StampUvsNearest(Vector3[] walk, Vector3[] paint, Vector2[] paintUv)
        {
            if (walk.Length == 0 || paint.Length == 0 || paintUv.Length != paint.Length)
            {
                return null;
            }

            var wMin = walk[0];
            var wMax = walk[0];
            var pMin = paint[0];
            var pMax = paint[0];
            for (var i = 1; i < walk.Length; i++)
            {
                wMin = Vector3.Min(wMin, walk[i]);
                wMax = Vector3.Max(wMax, walk[i]);
            }

            for (var i = 1; i < paint.Length; i++)
            {
                pMin = Vector3.Min(pMin, paint[i]);
                pMax = Vector3.Max(pMax, paint[i]);
            }

            var wSize = wMax - wMin;
            var pSize = pMax - pMin;
            if (pSize.x < 1e-5f || pSize.y < 1e-5f || pSize.z < 1e-5f)
            {
                return null;
            }

            const int cells = 24;
            var buckets = new System.Collections.Generic.List<int>[cells * cells * cells];
            for (var i = 0; i < paint.Length; i++)
            {
                var n = new Vector3(
                    (paint[i].x - pMin.x) / pSize.x,
                    (paint[i].y - pMin.y) / pSize.y,
                    (paint[i].z - pMin.z) / pSize.z);
                var xi = Mathf.Clamp(Mathf.FloorToInt(n.x * cells), 0, cells - 1);
                var yi = Mathf.Clamp(Mathf.FloorToInt(n.y * cells), 0, cells - 1);
                var zi = Mathf.Clamp(Mathf.FloorToInt(n.z * cells), 0, cells - 1);
                var key = xi + cells * (yi + cells * zi);
                buckets[key] ??= new System.Collections.Generic.List<int>(8);
                buckets[key].Add(i);
            }

            var outUv = new Vector2[walk.Length];
            for (var i = 0; i < walk.Length; i++)
            {
                var n = new Vector3(
                    wSize.x > 1e-5f ? (walk[i].x - wMin.x) / wSize.x : 0.5f,
                    wSize.y > 1e-5f ? (walk[i].y - wMin.y) / wSize.y : 0.5f,
                    wSize.z > 1e-5f ? (walk[i].z - wMin.z) / wSize.z : 0.5f);
                var xi = Mathf.Clamp(Mathf.FloorToInt(n.x * cells), 0, cells - 1);
                var yi = Mathf.Clamp(Mathf.FloorToInt(n.y * cells), 0, cells - 1);
                var zi = Mathf.Clamp(Mathf.FloorToInt(n.z * cells), 0, cells - 1);
                var best = 0;
                var bestD = float.MaxValue;
                for (var dx = -1; dx <= 1; dx++)
                {
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        for (var dz = -1; dz <= 1; dz++)
                        {
                            var xx = xi + dx;
                            var yy = yi + dy;
                            var zz = zi + dz;
                            if (xx < 0 || yy < 0 || zz < 0 || xx >= cells || yy >= cells || zz >= cells)
                            {
                                continue;
                            }

                            var list = buckets[xx + cells * (yy + cells * zz)];
                            if (list == null)
                            {
                                continue;
                            }

                            var mapped = new Vector3(
                                pMin.x + n.x * pSize.x,
                                pMin.y + n.y * pSize.y,
                                pMin.z + n.z * pSize.z);
                            foreach (var pi in list)
                            {
                                var d = (paint[pi] - mapped).sqrMagnitude;
                                if (d < bestD)
                                {
                                    bestD = d;
                                    best = pi;
                                }
                            }
                        }
                    }
                }

                outUv[i] = paintUv[best];
            }

            return outUv;
        }

        private static Texture2D? LoadSepPaintMap(string fileName, bool sRgb)
        {
#if UNITY_EDITOR
            var rel = "Assets/" + PaintedLookDir.Replace('\\', '/') + "/" + fileName;
            var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(rel);
            if (asset != null)
            {
                return asset;
            }
#endif
            var path = ResolveHero3D("sep_paint/" + fileName);
            if (path == null)
            {
                return null;
            }

            try
            {
                var tex = new Texture2D(2, 2, sRgb ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
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
