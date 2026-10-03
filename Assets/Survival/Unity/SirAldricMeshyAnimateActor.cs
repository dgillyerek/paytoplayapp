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
    /// Sir Aldric PILOT: Mixamo-skinned holefixed mid280k is the ONLY visible body.
    /// Generic Mixamo clips (Humanoid Playable collapses this skin). AccuRIG superseded.
    /// Walk = Standard Walk. Strike = Mixamo DIAG Stable Sword Inward Slash
    /// (human take, md5 72412be4) — arm/shoulder/torso together. Sword starts
    /// at LEFT hip then RH. No held-pose robot clip. No post-Evaluate arm sculpt.
    /// Clip.name must match SirAldric_DIAG_InwardSlash. HOLD merge.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class SirAldricMeshyAnimateActor : MonoBehaviour
    {
        public const string ThemePackFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot/SirAldric_body_holefixed_walk.fbx";
        public const string ThemePackLookFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot/SirAldric_body_holefixed_mid280k.fbx";
        public const string ThemePackWalkFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot/SirAldric_body_holefixed_walk.fbx";
        public const string ThemePackAttackFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot/SirAldric_body_holefixed_slash_Stable_Sword_Inward_Slash_DIAG.fbx";
        /// <summary>Same DIAG bytes as ThemePackAttackFbx (md5 72412be4). Not DIAG_REV 0beb3c77 (broke play). Not DIAG_MIRR 70fd9483. Not MILD 8d5b78b0. Not baseline 4a143441.</summary>
        public const string ThemePackAttackFbxAlias = "ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot/SirAldric_body_holefixed_slash.fbx";
        /// <summary>Dev-authored attack clip. File name and clip.name must both be SirAldric_DIAG_InwardSlash (Unity compile break if they differ).</summary>
        public const string ThemePackAttackAnim = "Survival/Unity/Anims/SirAldric_DIAG_InwardSlash.anim";
        /// <summary>Must match the .anim file name. Naming the clip "Attack" does not match SirAldric_DIAG_InwardSlash.</summary>
        public const string AttackClipAssetName = "SirAldric_DIAG_InwardSlash";
        /// <summary>Editor-only AnimatorController so Animation-window Preview can sample the .anim. Not used by PlayableGraph.</summary>
        public const string ThemePackAttackController = "Survival/Unity/Anims/SirAldric_DIAG_InwardSlash.controller";
        public const string ThemePackSwordFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot/SirAldric_PILOT_sword.fbx";
        public const string ThemePackCapeFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot/SirAldric_PILOT_cape.fbx";
        public const string PaintedLookDir = "ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot";
        public const string LeftoverMeshyWalkFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/sir_aldric_meshy_animate_walk.fbx";
        public const string ClipHint = "Walking";
        public const string AttackClipHint = "Attack";
        public const float RearYawDegrees = SirAldric3DMotion.MixamoImportRearYawDegrees;
        public const string AttackAuthoredReason =
            "PILOT Mixamo sep Generic: holefixed body + Standard Walk + Mixamo DIAG " +
            "Stable Sword Inward Slash (md5 72412be4, human take — arm/shoulder/torso together). " +
            "SirAldric_DIAG_InwardSlash.anim is that take (name matches the file — Attack as clip.name does not match SirAldric_DIAG_InwardSlash and Unity will not compile). " +
            "Not the 7958283 held-pose robot clip. Not DIAG_REV 0beb3c77 e90b654 no-attack FAIL; not DIAG_MIRR 70fd9483 Derek reject 19e6efe; not MILD 8d5b78b0; not baseline 4a143441. " +
            "Sword never a whole-body X-flip. No Mixamo Mirror. " +
            "Walk: sword gripped in mixamorig:RightHand after draw (Derek 0fc0930 Game-view; 879a6f3 left-hand FAIL discarded). " +
            "Dev owns the strike story: sword starts at LEFT hip and is drawn, Mixamo rise above the head on the RIGHT, strike FORWARD, then DOWN toward the foot. " +
            "Mixer plays the DIAG clip only (leftover AttackLeftHipDrawReach / AimArmAlong / AttackRaiseThenCutReach unused — 95aba89 FAIL). " +
            "YouTube iQ1s3nN1330 leftover / backswing history. RH sword 1.01m. Mixer 0.20s. FOV 42. Never AccuRIG. HOLD merge.";
        public const float WalkToAttackBlendSeconds = 0.20f;
        public const float SwordBladeMeters = 1.01f;
        private const float BodyHeightMinMeters = 0.5f;
        private const float BodyHeightMaxMeters = 5f;
        private const float BodyHeightTargetMeters = 1.80f;

        private Animator? _animator;
        private PlayableGraph _graph;
        private AnimationPlayableOutput _output;
        private AnimationMixerPlayable _mixer;
        private AnimationClipPlayable _walkPlayable;
        private AnimationClipPlayable _attackPlayable;
        private float _walkLength;
        private float _attackLength;
        private bool _graphReady;
        private bool _attackReady;
        private GameObject? _instance;
        private Transform? _rightArm;
        private Transform? _rightForeArm;
        private Transform? _rightHand;
        private Transform? _leftArm;
        private Transform? _leftForeArm;
        private Transform? _leftHand;
        private Transform? _spine;
        private Transform? _hips;
        private Transform? _rightUpLeg;
        private Transform? _rightLeg;
        private Transform? _leftUpLeg;
        private Transform? _heldSword;
        private Transform? _boundSwordParent;
        private Transform? _sheathSocket;
        private Transform? _leftHipSocket;
        /// <summary>
        /// Mesh-local blade axis. Unity FBX bakeAxisConversion turns Design +Z into
        /// local +Y — aiming transform.forward at desired left the visible blade
        /// on world +Y (Derek FAIL b0a9509 Game-view sky). Rematch blender has no bake
        /// so it still aims local +Z.
        /// </summary>
        private Vector3 _swordLocalBlade = Vector3.up;
        private Vector3 _sheathHiltLocal;
        private bool _sheathHiltReady;
        private float _poseTime;
        private bool _poseReady;

        public bool Built => _graphReady;
        public bool HasAttackClip => _attackReady;
        public float WalkLength => _walkLength;
        public float AttackLength => _attackLength > 0.05f ? _attackLength : 0f;

        public void Build()
        {
            BuildLights();
            var prefab = LoadFbxPrefab(ThemePackFbx, "SirAldric_body_holefixed_walk");
            if (prefab == null)
            {
                Debug.LogError(
                    "PILOT Mixamo body FBX not imported yet. Open Unity so " +
                    ThemePackFbx + " Generic-imports, then Play SirAldric.");
                return;
            }

            _instance = Instantiate(prefab, transform);
            _instance.name = "SirAldricPilotMixamo";
            HideJunk(_instance);
            StripEmbeddedClipMeshes(_instance);
            NormalizeMixamoCmRoot(_instance);
            FaceWorldTop(_instance);
            BindPaintedLook(_instance);
            EnableSkinAlways(_instance);

            _animator = _instance.GetComponent<Animator>() ?? _instance.AddComponent<Animator>();
            var mixamoAvatar = LoadImportedAvatar(AssetPath(ThemePackFbx));
            if (mixamoAvatar != null && mixamoAvatar.isHuman)
            {
                Debug.LogWarning(
                    "PILOT Mixamo: ignoring Humanoid avatar — Playable retarget collapses holefixed skin " +
                    "while mixamorig:RightHand still moves (sword-only FAIL). Generic Mixamo bones.");
                mixamoAvatar = null;
            }

            if (mixamoAvatar != null)
            {
                _animator.avatar = mixamoAvatar;
            }

            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _animator.updateMode = AnimatorUpdateMode.UnscaledTime;

            var walk = LoadClip(ThemePackWalkFbx, ClipHint, repairWalk: true);
            if (walk == null)
            {
                Debug.LogError("PILOT walk FBX has no Walking clip after Generic import.");
                return;
            }

            walk.wrapMode = WrapMode.Loop;
            _walkLength = walk.length;

            _graph = PlayableGraph.Create("SirAldricPilotMixamo");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            _output = AnimationPlayableOutput.Create(_graph, "AldricMixamo", _animator);
            _mixer = AnimationMixerPlayable.Create(_graph, 2);
            _walkPlayable = AnimationClipPlayable.Create(_graph, walk);
            _graph.Connect(_walkPlayable, 0, _mixer, 0);
            _mixer.SetInputWeight(0, 1f);
            _mixer.SetInputWeight(1, 0f);
            _output.SetSourcePlayable(_mixer);

            EnsureEditableAttackClip();
            var attack = LoadClip(ThemePackAttackAnim, AttackClipHint, repairWalk: false);
#if UNITY_EDITOR
            if (attack != null && AnimationUtility.GetCurveBindings(attack).Length <= 8)
            {
                attack = null;
            }
#endif
            if (attack == null)
            {
                attack = LoadClip(ThemePackAttackFbx, AttackClipHint, repairWalk: false);
            }

            if (attack != null)
            {
                attack.wrapMode = WrapMode.Once;
                _attackLength = attack.length;
                _attackPlayable = AnimationClipPlayable.Create(_graph, attack);
                _graph.Connect(_attackPlayable, 0, _mixer, 1);
                _attackReady = true;
            }
            else
            {
                Debug.LogWarning("PILOT Mixamo DIAG attack take missing after Generic import.");
            }

            _graph.Play();
            _graphReady = true;
            SampleAt(0f);
            CorrectBodyScaleIfNeeded(_instance);
            SampleAt(0f);
            AttachHeldSword(_instance);
            HideEmbeddedSwords(_instance);
            CacheAttackBones(_instance);
            SampleAt(0f);
            LogSkinAndFailIfBad(_instance);
            BindRenderHook();
            Debug.Log(
                "PILOT actor built walkLen=" + _walkLength + " attackLen=" + _attackLength +
                " visible=Mixamo walkClip=" + ThemePackWalkFbx +
                " attackClip=" + ThemePackAttackAnim + " " + AttackAuthoredReason);
        }

        /// <summary>
        /// Slash FBX may embed a With-Skin mesh. Mixamo holefixed walk instance is the only visible body.
        /// </summary>
        private static void StripEmbeddedClipMeshes(GameObject root)
        {
            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                var n = rend.name;
                if (n.IndexOf("Ico", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("withSkin", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("with_skin", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    rend.enabled = false;
                    rend.gameObject.SetActive(false);
                }
            }
        }

        private void OnEnable()
        {
            BindRenderHook();
        }

        private void OnDisable()
        {
            UnbindRenderHook();
        }

        private void BindRenderHook()
        {
            UnbindRenderHook();
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            Camera.onPreRender += OnPreRenderCamera;
        }

        private void UnbindRenderHook()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            Camera.onPreRender -= OnPreRenderCamera;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            _ = context;
            _ = camera;
            if (_poseReady)
            {
                ApplySampledPose();
            }
        }

        private void OnPreRenderCamera(Camera camera)
        {
            _ = camera;
            if (_poseReady)
            {
                ApplySampledPose();
            }
        }

        private void LateUpdate()
        {
            if (!_graphReady)
            {
                return;
            }

            SampleAt(Time.unscaledTime);
        }

        public void SampleAt(float timeSeconds)
        {
            _poseTime = timeSeconds;
            _poseReady = true;
            ApplySampledPose();
        }

        private void ApplySampledPose()
        {
            if (!_graph.IsValid() || !_mixer.IsValid())
            {
                return;
            }

            var timeSeconds = _poseTime;
            var walkLen = _walkLength > 0.05f ? _walkLength : 1f;
            var walkBlock = walkLen * SirAldric3DMotion.WalkCyclesBeforeAttack;
            var attackLen = _attackReady ? Mathf.Max(_attackLength, 0.01f) : walkBlock;
            var loop = walkBlock + (_attackReady ? attackLen : 0f);
            if (loop < 0.05f)
            {
                loop = walkLen;
            }

            var t = timeSeconds % loop;
            var blend = Mathf.Clamp(WalkToAttackBlendSeconds, 0.15f, 0.25f);
            if (!_attackReady)
            {
                _mixer.SetInputWeight(0, 1f);
                _mixer.SetInputWeight(1, 0f);
                _walkPlayable.SetTime(t % walkLen);
                _graph.Evaluate();
                ApplyAttackWindupLift(0f, 0f);
                return;
            }

            float walkW;
            float attackW;
            float attackU;
            if (t < walkBlock)
            {
                var into = t >= walkBlock - blend
                    ? Mathf.InverseLerp(walkBlock - blend, walkBlock, t)
                    : 0f;
                walkW = 1f - into;
                attackW = into;
                attackU = 0f;
                _walkPlayable.SetTime(t % walkLen);
                _attackPlayable.SetTime(0f);
            }
            else
            {
                var at = t - walkBlock;
                var back = at >= attackLen - blend
                    ? Mathf.InverseLerp(attackLen - blend, attackLen, at)
                    : 0f;
                walkW = back;
                attackW = 1f - back;
                attackU = at / attackLen;
                _attackPlayable.SetTime(at);
                _walkPlayable.SetTime(0f);
            }

            _mixer.SetInputWeight(0, walkW);
            _mixer.SetInputWeight(1, attackW);
            _graph.Evaluate();
            ApplyAttackWindupLift(attackW, attackU);
        }

        /// <summary>
        /// Mixer plays Mixamo DIAG (human take). After Evaluate, parent the
        /// sword only — no AimArmAlong / leftover AttackRaiseThenCutReach /
        /// leftover AttackSlashReach / leftover AttackLeftHipDrawReach.
        /// Draw starts at LEFT hip; strike on mixamorig:RightHand.
        /// </summary>
        private void ApplyAttackWindupLift(float attackWeight, float attackNormalized01)
        {
            if (attackWeight < 0.05f)
            {
                BindSwordTo(_rightHand, strikeGrip: true);
                if (_heldSword != null)
                {
                    _heldSword.localRotation = SwordRestLocal();
                }

                return;
            }

            if (attackNormalized01 < SirAldric3DMotion.MixamoDiagDrawEndU)
            {
                BindSwordTo(EnsureLeftHipSocket(), strikeGrip: false);
                AimHeldSwordWorld(Vector3.down);
                return;
            }

            BindSwordTo(_rightHand, strikeGrip: true);
            if (_heldSword != null)
            {
                _heldSword.localRotation = SwordRestLocal();
            }
        }

        // Discarded 7958283 held-pose robot bake. Mixer plays Mixamo DIAG.


        private Transform? EnsureLeftHipSocket()
        {
            if (_hips == null)
            {
                return null;
            }

            if (_leftHipSocket == null)
            {
                var go = new GameObject("LeftHipDrawSocket");
                _leftHipSocket = go.transform;
            }

            if (_leftHipSocket.parent != _hips)
            {
                _leftHipSocket.SetParent(_hips, false);
            }

            var hip = _leftUpLeg != null ? _leftUpLeg.position : _hips.position;
            _leftHipSocket.position = hip + CharacterLeft() * 0.10f + Vector3.up * 0.08f + Vector3.back * 0.03f;
            return _leftHipSocket;
        }

        private static Vector3 CharacterLeft() => Vector3.left;

        private void AimHeldSwordWorld(Vector3 worldAxis)
        {
            if (_heldSword == null || worldAxis.sqrMagnitude < 1e-8f)
            {
                return;
            }

            var blade = _heldSword.rotation * BladeLocalAxis();
            if (blade.sqrMagnitude < 1e-8f)
            {
                return;
            }

            _heldSword.rotation = Quaternion.Normalize(
                Quaternion.FromToRotation(blade.normalized, worldAxis.normalized) * _heldSword.rotation);
        }

        /// <summary>
        /// Walk (Derek 0fc0930): sword in the right hand, tip along the hanging arm.
        /// HipSheathSocket / HipSheathBladeLocal hip-float superseded.
        /// </summary>
        private void ApplySheathedSword()
        {
            if (_heldSword == null || _rightHand == null)
            {
                return;
            }

            BindSwordTo(_rightHand, strikeGrip: true);
            SnapHeldSwordToArmAxis(Vector3.zero);
        }

        private Transform? EnsureHipSheathSocket()
        {
            if (_hips == null)
            {
                return null;
            }

            if (_sheathSocket == null)
            {
                var go = new GameObject("HipSheathSocket");
                _sheathSocket = go.transform;
                _sheathSocket.SetParent(_hips, false);
            }
            else if (_sheathSocket.parent != _hips)
            {
                _sheathSocket.SetParent(_hips, false);
            }

            return _sheathSocket;
        }

        /// <summary>
        /// Rear Play-cam character-right is world +X after FaceWorldTop. Fixed —
        /// following RightUpLeg/RH during walk made the sheath jump (Derek 52aba6b).
        /// </summary>
        private static Vector3 CharacterRight() => Vector3.right;

        /// <summary>
        /// Rematch set_sword_world puts the hilt at the hip grip. A centered FBX
        /// pivot otherwise hangs half the blade across the ribs (Derek 7187204).
        /// </summary>
        private void SnapSwordHiltTo(Vector3 worldHilt)
        {
            if (_heldSword == null)
            {
                return;
            }

            var blade = (_heldSword.rotation * BladeLocalAxis()).normalized;
            if (blade.sqrMagnitude < 1e-8f)
            {
                return;
            }

            var minT = float.PositiveInfinity;
            var hilt = _heldSword.position;
            var found = false;
            foreach (var filter in _heldSword.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                var b = mesh.bounds;
                var c = b.center;
                var e = b.extents;
                for (var i = 0; i < 8; i++)
                {
                    var local = c + new Vector3(
                        (i & 1) == 0 ? -e.x : e.x,
                        (i & 2) == 0 ? -e.y : e.y,
                        (i & 4) == 0 ? -e.z : e.z);
                    var w = filter.transform.TransformPoint(local);
                    var t = Vector3.Dot(w, blade);
                    if (t < minT)
                    {
                        minT = t;
                        hilt = w;
                        found = true;
                    }
                }
            }

            if (!found)
            {
                return;
            }

            _heldSword.position += worldHilt - hilt;
        }

        /// <summary>
        /// Reparent the prop only when the socket changes, then normalize the 1.01 m blade
        /// against the new parent's lossyScale. Strike grip is Mixamo hand +Y (fingers).
        /// </summary>
        private void BindSwordTo(Transform? parent, bool strikeGrip)
        {
            if (_heldSword == null || parent == null)
            {
                return;
            }

            if (_boundSwordParent != parent)
            {
                _heldSword.SetParent(parent, worldPositionStays: false);
                _boundSwordParent = parent;
                var newScale = Mathf.Max(Mathf.Abs(parent.lossyScale.x), 1e-5f);
                _heldSword.localScale = Vector3.one * (1f / newScale);
                NormalizeSwordWorldBlade(_heldSword);
            }

            var parentScale = Mathf.Max(Mathf.Abs(parent.lossyScale.x), 1e-5f);
            var inv = 1f / parentScale;

            if (strikeGrip)
            {
                _heldSword.localPosition = new Vector3(0f, 0.08f, 0f) * inv;
            }
            else
            {
                _heldSword.localPosition = Vector3.zero;
            }
        }

        /// <summary>
        /// Blade along forearm→hand (grip in palm, tip beyond the fingers).
        /// If prefer is set, keep the tip in that hemisphere (UR→LL).
        /// </summary>
        private void SnapHeldSwordToArmAxis(Vector3 prefer)
        {
            if (_heldSword == null)
            {
                return;
            }

            _heldSword.localRotation = SwordRestLocal();
            var axis = Vector3.zero;
            if (_rightForeArm != null && _rightHand != null)
            {
                axis = _rightHand.position - _rightForeArm.position;
            }

            if (axis.sqrMagnitude < 1e-8f && _rightArm != null && _rightHand != null)
            {
                axis = _rightHand.position - _rightArm.position;
            }

            if (axis.sqrMagnitude < 1e-8f)
            {
                return;
            }

            if (prefer.sqrMagnitude > 1e-6f && Vector3.Dot(axis, prefer) < 0f)
            {
                axis = -axis;
            }

            var blade = _heldSword.rotation * BladeLocalAxis();
            if (blade.sqrMagnitude < 1e-8f)
            {
                return;
            }

            _heldSword.rotation = Quaternion.Normalize(
                Quaternion.FromToRotation(blade.normalized, axis.normalized) * _heldSword.rotation);
        }

        private Vector3 BladeLocalAxis()
        {
            return _swordLocalBlade.sqrMagnitude > 1e-8f ? _swordLocalBlade : Vector3.up;
        }

        private Quaternion SwordRestLocal()
        {
            // HeldSwordRestEulerX is rematch blender (+Z mesh). Unity uses measured blade.
            _ = SirAldric3DMotion.HeldSwordRestEulerX;
            var blade = BladeLocalAxis().normalized;
            return Quaternion.Normalize(Quaternion.FromToRotation(blade, Vector3.up));
        }

        private void CacheAttackBones(GameObject root)
        {
            _rightArm = FindNamedBone(root, "mixamorig:RightArm", "RightArm");
            _rightForeArm = FindNamedBone(root, "mixamorig:RightForeArm", "RightForeArm");
            _rightHand = FindNamedBone(root, "mixamorig:RightHand", "RightHand");
            _leftArm = FindNamedBone(root, "mixamorig:LeftArm", "LeftArm");
            _leftForeArm = FindNamedBone(root, "mixamorig:LeftForeArm", "LeftForeArm");
            _leftHand = FindNamedBone(root, "mixamorig:LeftHand", "LeftHand");
            _spine = FindNamedBone(root, "mixamorig:Spine2", "Spine2", "Spine1", "Spine");
            _hips = FindNamedBone(root, "mixamorig:Hips", "Hips");
            _rightUpLeg = FindNamedBone(root, "mixamorig:RightUpLeg", "RightUpLeg");
            _rightLeg = FindNamedBone(root, "mixamorig:RightLeg", "RightLeg");
            _leftUpLeg = FindNamedBone(root, "mixamorig:LeftUpLeg", "LeftUpLeg");
            _heldSword = FindNamedBone(root, "SirAldricPilotSword");
            _swordLocalBlade = MeasureLocalBladeAxis(_heldSword);
            if (_heldSword != null)
            {
                _boundSwordParent = _heldSword.parent;
            }
        }

        private static Vector3 MeasureLocalBladeAxis(Transform? sword)
        {
            if (sword == null)
            {
                return Vector3.up;
            }

            var bestAxis = Vector3.up;
            var best = 0f;
            foreach (var filter in sword.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                var s = filter.sharedMesh.bounds.size;
                var c = filter.sharedMesh.bounds.center;
                Vector3 axis;
                float len;
                if (s.y >= s.x && s.y >= s.z)
                {
                    axis = c.y < 0f ? Vector3.down : Vector3.up;
                    len = s.y;
                }
                else if (s.z >= s.x)
                {
                    axis = c.z < 0f ? Vector3.back : Vector3.forward;
                    len = s.z;
                }
                else
                {
                    axis = c.x < 0f ? Vector3.left : Vector3.right;
                    len = s.x;
                }

                if (len > best)
                {
                    best = len;
                    bestAxis = axis;
                }
            }

            return bestAxis;
        }

        public string PhaseLabel(float timeSeconds)
        {
            var walkLen = _walkLength > 0.05f ? _walkLength : 1f;
            var walkBlock = walkLen * SirAldric3DMotion.WalkCyclesBeforeAttack;
            var attackLen = _attackReady ? Mathf.Max(_attackLength, 0.01f) : walkBlock;
            var loop = walkBlock + (_attackReady ? attackLen : 0f);
            var t = loop > 0.05f ? timeSeconds % loop : timeSeconds;
            var blend = Mathf.Clamp(WalkToAttackBlendSeconds, 0.15f, 0.25f);
            if (_attackReady && t >= walkBlock - blend * 0.5f && t < walkBlock + attackLen - blend * 0.5f)
            {
                var u = Mathf.Clamp01((t - walkBlock) / attackLen);
                if (u < SirAldric3DMotion.MixamoDiagDrawEndU)
                {
                    return "DRAW  ·  LEFT hip  ·  Mixamo DIAG";
                }

                if (u < 0.70f)
                {
                    return "RAISE  ·  overhead RIGHT  ·  Mixamo DIAG";
                }

                if (u < 0.85f)
                {
                    return "STRIKE  ·  FORWARD  ·  Mixamo DIAG";
                }

                return "STRIKE  ·  DOWN to foot  ·  Mixamo DIAG";
            }

            return "WALK  ·  toward TOP  ·  RH grip";
        }

        private void OnDestroy()
        {
            UnbindRenderHook();
            if (_graph.IsValid())
            {
                _graph.Destroy();
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

        /// <summary>
        /// Mixamo slash-with-skin can leave a second sword in the RH. Keep only
        /// SirAldricPilotSword so Game-view matches rematch (one hip sheath).
        /// </summary>
        private void HideEmbeddedSwords(GameObject root)
        {
            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                if (_heldSword != null && (rend.transform == _heldSword || rend.transform.IsChildOf(_heldSword)))
                {
                    continue;
                }

                var n = rend.name;
                if (n.IndexOf("sword", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("weapon", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    rend.enabled = false;
                }
            }
        }

        private static void FaceWorldTop(GameObject root)
        {
            // Do not scale.x = -1: Derek 212c6da Play put the sword on the left hand.
            // Blind yaw 180 when the FBX already faces +Z puts mixamorig:RightHand on
            // world −X (screen-left). Only yaw if RH is on the left of LH at rest.
            root.transform.localPosition = Vector3.zero;
            var s = root.transform.localScale;
            root.transform.localScale = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
            root.transform.localRotation = Quaternion.identity;
            var rh = FindNamedBone(root, "mixamorig:RightHand", "RightHand");
            var lh = FindNamedBone(root, "mixamorig:LeftHand", "LeftHand");
            var yaw = RearYawDegrees;
            if (rh != null && lh != null && rh.position.x >= lh.position.x)
            {
                yaw = 0f;
            }

            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Debug.Log(
                "PILOT FaceWorldTop yaw=" + yaw +
                " RH_x=" + (rh != null ? rh.position.x.ToString("0.000") : "?") +
                " LH_x=" + (lh != null ? lh.position.x.ToString("0.000") : "?"));
        }

        /// <summary>
        /// Mixamo With-Skin FBX keeps Armature Lcl Scaling 0.01 (cm). Unity FileScale also
        /// applies 0.01. Stacked → ~2 cm body. Humanoid then drives bones at 1.8 m so only
        /// the RH sword is visible. Flatten every 0.01 root to 1×. Do not hide the body.
        /// </summary>
        private static void NormalizeMixamoCmRoot(GameObject root)
        {
            var n = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var s = t.localScale;
                if (IsMixamoCmScale(s))
                {
                    t.localScale = Vector3.one;
                    n++;
                    Debug.Log("PILOT Mixamo cm-root " + t.name + " scale 0.01 → 1");
                }
            }

            if (n == 0)
            {
                Debug.Log("PILOT Mixamo cm-root none (already 1×)");
            }
        }

        private static bool IsMixamoCmScale(Vector3 s) =>
            Mathf.Abs(s.x - 0.01f) < 0.003f
            && Mathf.Abs(s.y - 0.01f) < 0.003f
            && Mathf.Abs(s.z - 0.01f) < 0.003f;

        private static void EnableSkinAlways(GameObject root)
        {
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.enabled = true;
                smr.updateWhenOffscreen = true;
            }
        }

        private static bool IsBodySkin(SkinnedMeshRenderer smr)
        {
            var n = smr.name;
            return n.IndexOf("sword", StringComparison.OrdinalIgnoreCase) < 0
                   && n.IndexOf("cape", StringComparison.OrdinalIgnoreCase) < 0
                   && n.IndexOf("Ico", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static float MeasureBodyHeight(GameObject root)
        {
            var best = 0f;
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!IsBodySkin(smr))
                {
                    continue;
                }

                var s = smr.bounds.size;
                best = Mathf.Max(best, s.x, s.y, s.z);
            }

            return best;
        }

        private static void CorrectBodyScaleIfNeeded(GameObject root)
        {
            var height = MeasureBodyHeight(root);
            if (height >= BodyHeightMinMeters && height <= BodyHeightMaxMeters)
            {
                return;
            }

            if (height < 0.0001f)
            {
                Debug.LogError("PILOT skin FAIL no measurable Mixamo body bounds before scale correct.");
                return;
            }

            var factor = BodyHeightTargetMeters / height;
            root.transform.localScale *= factor;
            Debug.Log("PILOT Mixamo body scale correct height=" + height + " ×" + factor);
        }

        private static void LogSkinAndFailIfBad(GameObject root)
        {
            var anyBody = false;
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var c = smr.bounds.center;
                var s = smr.bounds.size;
                var ls = smr.transform.lossyScale;
                Debug.Log(
                    "PILOT skin " + smr.name +
                    " enabled=" + smr.enabled +
                    " bounds.center=(" + c.x + "," + c.y + "," + c.z + ")" +
                    " bounds.size=(" + s.x + "," + s.y + "," + s.z + ")" +
                    " lossyScale=(" + ls.x + "," + ls.y + "," + ls.z + ")");
                if (!IsBodySkin(smr))
                {
                    continue;
                }

                anyBody = true;
                var height = Mathf.Max(s.x, s.y, s.z);
                if (height < BodyHeightMinMeters || height > BodyHeightMaxMeters)
                {
                    Debug.LogError(
                        "PILOT skin FAIL " + smr.name +
                        " body height " + height + "m (need 0.5–5m). " +
                        "Mixamo Armature 0.01 stacked on FileScale, or Humanoid skin collapse.");
                }
            }

            if (!anyBody)
            {
                Debug.LogError("PILOT skin FAIL no SkinnedMeshRenderer on Mixamo body.");
            }
        }

        private static Transform? FindNamedBone(GameObject root, params string[] names)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                foreach (var name in names)
                {
                    if (string.Equals(t.name, name, StringComparison.OrdinalIgnoreCase)
                        || t.name.EndsWith(":" + name, StringComparison.OrdinalIgnoreCase))
                    {
                        return t;
                    }
                }
            }

            return null;
        }

        private void AttachHeldSword(GameObject root)
        {
            var hand = FindNamedBone(root, "mixamorig:RightHand", "RightHand");
            var hips = FindNamedBone(root, "mixamorig:Hips", "Hips");
            if (hand == null)
            {
                Debug.LogWarning("PILOT Mixamo RightHand missing — sword prop skipped.");
                return;
            }

            var prefab = LoadFbxPrefab(ThemePackSwordFbx, "SirAldric_PILOT_sword");
            if (prefab == null)
            {
                Debug.LogWarning("PILOT sword FBX not imported: " + ThemePackSwordFbx);
                return;
            }

            var sword = Instantiate(prefab, hand);
            sword.name = "SirAldricPilotSword";
            // Design wire: RH only. Hip sheath leftover (mixamorig:Hips) unused.
            // Derek 9075270: compensate parent lossyScale so world blade ≈ 1.01 m.
            var parent = Mathf.Max(Mathf.Abs(hand.lossyScale.x), 1e-5f);
            var inv = 1f / parent;
            sword.transform.localPosition = new Vector3(0f, 0.08f, 0f) * inv;
            sword.transform.localRotation = Quaternion.identity;
            sword.transform.localScale = Vector3.one * inv;
            NormalizeSwordWorldBlade(sword.transform);
            _heldSword = sword.transform;
            _boundSwordParent = hand;
            var world = MeasureRendererWorldSize(sword);
            var blade = Mathf.Max(world.x, world.y, world.z);
            var ls = hand.lossyScale;
            Debug.Log(
                "PILOT sword parented to " + hand.name +
                " worldBounds.size=(" + world.x + "," + world.y + "," + world.z + ")" +
                " blade=" + blade +
                " hand.lossyScale=(" + ls.x + "," + ls.y + "," + ls.z + ")" +
                (hips != null ? " hipsLeftover=" + hips.name : ""));
        }

        private static void NormalizeSwordWorldBlade(Transform sword)
        {
            var blade = MeasureMeshWorldBlade(sword.gameObject);
            if (blade < 1e-4f)
            {
                var world = MeasureRendererWorldSize(sword.gameObject);
                blade = Mathf.Max(world.x, world.y, world.z);
            }

            if (blade < 1e-4f)
            {
                Debug.LogError("PILOT sword FAIL no measurable world bounds.");
                return;
            }

            sword.localScale *= SwordBladeMeters / blade;
        }

        private static float MeasureMeshWorldBlade(GameObject root)
        {
            var best = 0f;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                var s = filter.sharedMesh.bounds.size;
                var ls = filter.transform.lossyScale;
                best = Mathf.Max(best, Mathf.Abs(s.x * ls.x), Mathf.Abs(s.y * ls.y), Mathf.Abs(s.z * ls.z));
            }

            return best;
        }

        private static Vector3 MeasureRendererWorldSize(GameObject root)
        {
            var best = Vector3.zero;
            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                var s = rend.bounds.size;
                if (Mathf.Max(s.x, s.y, s.z) > Mathf.Max(best.x, best.y, best.z))
                {
                    best = s;
                }
            }

            return best;
        }

        private static void AttachSoftCape(GameObject root)
        {
            var prefab = LoadFbxPrefab(ThemePackCapeFbx, "SirAldric_PILOT_cape");
            if (prefab == null)
            {
                return;
            }

            var cape = Instantiate(prefab, root.transform);
            cape.name = "SirAldricPilotCape";
            cape.transform.localPosition = Vector3.zero;
            cape.transform.localRotation = Quaternion.identity;
            cape.transform.localScale = Vector3.one;
            var spine = FindNamedBone(root, "mixamorig:Spine2", "Spine2");
            if (spine != null)
            {
                cape.transform.SetParent(spine, true);
            }

            Debug.Log("PILOT soft cape parented to " + (spine != null ? spine.name : "root"));
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

        private static Avatar? LoadImportedAvatar(string rel)
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

        /// <summary>
        /// Duplicate the Mixamo DIAG take into SirAldric_DIAG_InwardSlash.anim.
        /// Clip.name must equal the file name (Attack does not match).
        /// Overwrites the discarded 7958283 held-pose bake.
        /// </summary>
        public static void EnsureEditableAttackClip(bool forceReextract = false)
        {
            _ = forceReextract;
#if UNITY_EDITOR
            var animRel = AssetPath(ThemePackAttackAnim);
            var srcRel = AssetPath(ThemePackAttackFbx);
            var src = PickClip(srcRel, AttackClipHint);
            if (src == null)
            {
                RepairAttackTake(srcRel);
                src = PickClip(srcRel, AttackClipHint);
            }

            if (src == null)
            {
                Debug.LogWarning("PILOT cannot extract Mixamo DIAG take. " + srcRel);
                return;
            }

            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(animRel);
            if (existing != null)
            {
                EditorUtility.CopySerialized(src, existing);
                existing.name = AttackClipAssetName;
                existing.wrapMode = WrapMode.Once;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                Debug.Log("PILOT extracted Mixamo DIAG → " + animRel + " name=" + AttackClipAssetName);
                return;
            }

            var folder = "Assets/Survival/Unity/Anims";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Survival/Unity", "Anims");
            }

            var copy = UnityEngine.Object.Instantiate(src);
            copy.name = AttackClipAssetName;
            copy.wrapMode = WrapMode.Once;
            AssetDatabase.CreateAsset(copy, animRel);
            AssetDatabase.SaveAssets();
            Debug.Log("PILOT created Mixamo DIAG .anim " + animRel);
#else
            _ = forceReextract;
#endif
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
                          || clip.name.IndexOf(AttackClipAssetName, StringComparison.OrdinalIgnoreCase) >= 0
                          || (hint == AttackClipHint && (clip.name.IndexOf("BaseLayer", StringComparison.OrdinalIgnoreCase) >= 0
                              || clip.name.IndexOf("Slash", StringComparison.OrdinalIgnoreCase) >= 0
                              || clip.name.IndexOf("Sword", StringComparison.OrdinalIgnoreCase) >= 0));
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

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.useFileScale = false;
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
            if (!walk)
            {
                best.firstFrame = SirAldric3DMotion.MixamoSlashFirstFrame;
                best.lastFrame = SirAldric3DMotion.MixamoSlashLastFrame;
            }

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
                    if (label.IndexOf("Walk", StringComparison.OrdinalIgnoreCase) < 0
                        && label.IndexOf("mixamo", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                }
                else
                {
                    var named = label.IndexOf("rigify", StringComparison.OrdinalIgnoreCase) >= 0
                                || label.IndexOf("BaseLayer", StringComparison.OrdinalIgnoreCase) >= 0
                                || label.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0
                                || label.IndexOf("Slash", StringComparison.OrdinalIgnoreCase) >= 0
                                || label.IndexOf("Sword", StringComparison.OrdinalIgnoreCase) >= 0
                                || label.IndexOf("mixamo", StringComparison.OrdinalIgnoreCase) >= 0
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
            var metallic = LoadPilotMap("mixamo_tex/Meshy_AI_Lionheart_Sentinel_0929004215_texture_metallic.png", sRgb: false)
                           ?? LoadPilotMap("Meshy_AI_Lionheart_Sentinel_0929004215_texture_metallic.png", sRgb: false)
                           ?? LoadPilotMap("Meshy_AI_SirAldric_PILOT_mid28_biped_texture_0_metallic.png", sRgb: false)
                           ?? LoadMeshyPackedMap(0);
            var roughness = LoadPilotMap("mixamo_tex/Meshy_AI_Lionheart_Sentinel_0929004215_texture_roughness.png", sRgb: false)
                            ?? LoadPilotMap("Meshy_AI_Lionheart_Sentinel_0929004215_texture_roughness.png", sRgb: false)
                            ?? LoadPilotMap("Meshy_AI_SirAldric_PILOT_mid28_biped_texture_0_roughness.png", sRgb: false)
                            ?? LoadMeshyPackedMap(1);
            if (albedo == null)
            {
                Debug.LogError("PILOT albedo missing. Bind Lionheart mixamo_tex maps or extract from walk FBX.");
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

            _cachedAlbedo = LoadPilotMap("mixamo_tex/Meshy_AI_Lionheart_Sentinel_0929004215_texture.png", sRgb: true)
                            ?? LoadPilotMap("Meshy_AI_Lionheart_Sentinel_0929004215_texture.png", sRgb: true)
                            ?? LoadPilotMap("pilot_albedo.png", sRgb: true);
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
