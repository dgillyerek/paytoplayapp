using System.Collections.Generic;
using Survival.Domain.Roster;
using Survival.Domain.Runner;
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
    /// normal. Skips metal/roughness unless the spec names an embedded map; a named glTF
    /// metal-rough map (G roughness, B metallic) is imported linear and packed into URP's
    /// metallic (R) + smoothness (A) layout so it does not render as a dark chrome mirror.
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

        /// <summary>Rest shows attack frame 0 (the keyed rest carry, closed bow hand) instead of the FBX bind pose, which has
        /// the arm at the side and the fingers straight because the rest FBX carries no animation.</summary>
        private readonly bool _restAtAttackStart;
        private readonly BlenderRigIdlePropSpec[] _idleProps;
        private BlenderRigAttackDriver? _attack;
        private BlenderBowShots? _bowShots;
        private BlenderHeldRealign? _heldRealign;
        private bool _attackMode;
        private float _length;
        private float _time;
        private string _pose;

        // Upper-body attack layering (UpperBodyAttack): input 0 the legs' base (walk loop, or the take's first frame held
        // while the squad stands), input 1 the previous take fading out, input 2 the current take (_playable).
        private static readonly Dictionary<AnimationClip, Quaternion[]> HipsTurnCache = new Dictionary<AnimationClip, Quaternion[]>();
        private AnimationLayerMixerPlayable _layers;
        private AnimationClipPlayable _base;
        private AnimationClipPlayable _prevTake;
        private AnimationClipPlayable _calib;
        private AnimationClip? _attackClip;
        private AvatarMask? _upperMask;
        private Transform? _hips;
        private Transform? _spine;
        private bool _maskTried;
        private bool _wantWalkBase = true;
        private bool _baseIsWalk;
        private bool _baseLoop;
        private float _baseTime;
        private float _baseLength;
        private float _takeFrame;
        private float _takeAge;
        private float _prevFrame;
        private float _prevFrom;
        private float _prevAge;
        private bool _fadingOut;
        private float _outFrom;
        private float _outAge;

        public BlenderRigPlayer(
            BlenderRigSpec spec,
            Transform parent,
            BlenderRigAttackSpec? attack = null,
            BlenderRigIdlePropSpec[]? idleProps = null,
            bool restAtAttackStart = false)
        {
            _restAtAttackStart = restAtAttackStart;
            _attackSpec = attack;
            _idleProps = idleProps ?? System.Array.Empty<BlenderRigIdlePropSpec>();
            _spec = spec;
            _parent = parent;
            _pose = BlenderRigSpec.RestPoseName;
        }

        public bool Built => _ready;
        public string Pose => _pose;

        // Looping clips (walk, idle) can be sampled less often on rigs nobody watches closely (the runner's back rows):
        // keyed by the actor transform each player was built under; 1 (or absent) = every frame. Attacks always sample
        // every frame so the release stays on its frame.
        private static readonly Dictionary<Transform, int> EveryNth = new Dictionary<Transform, int>();
        private static int _phases;
        private readonly int _phase = _phases++;

        /// <summary>Sample the looping clips of the rig built under <paramref name="host"/> every <paramref name="frames"/>th frame (1 = every frame).</summary>
        public static void SetEvaluateEvery(Transform host, int frames)
        {
            if (host == null)
            {
                return;
            }

            if (frames <= 1)
            {
                EveryNth.Remove(host);
            }
            else
            {
                EveryNth[host] = frames;
            }
        }

        /// <summary>
        /// Playback rate of the attack take (1 = as authored). The runner speeds the take up so one take is one shot;
        /// the battle leaves it at 1.
        /// </summary>
        public float AttackSpeed { get; set; } = 1f;

        /// <summary>
        /// Runner only (the battle leaves it off): the attack take plays on the upper body (spine down, see
        /// <see cref="RunnerUpperBodyAttack"/>) over the walk loop, which keeps going, so the take's hips step does not
        /// show. Each take crossfades in over the last one, and out when the pose goes back to the walk; the take's own
        /// timing, and so its release frame, is unchanged. Rest and any other pose fall back to the full-body path.
        /// </summary>
        public bool UpperBodyAttack { get; set; }
        public float ClipLength => _length;
        public Bounds VisibleBounds { get; private set; }

        /// <summary>Attack props and effects driver, or null when this rig has no attack add-on.</summary>
        public BlenderRigAttackDriver? AttackDriver => _attack;

        public void Build()
        {
            BuildLights();
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
#if UNITY_EDITOR
                EnsureBodyImport(path, humanoid: true);
#endif
                _humanoid = HasValidHumanAvatar(path);
                if (!_humanoid)
                {
                    Debug.LogWarning(_spec.Name + " humanoid avatar did not validate. Using Generic.");
#if UNITY_EDITOR
                    EnsureBodyImport(path, humanoid: false);
#endif
                }
            }
#if UNITY_EDITOR
            else
            {
                EnsureBodyImport(path, humanoid: false);
            }
#endif

            var prefab = PlayerAssets.Load<GameObject>(path);
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
            BindIdlePropsAtAttackRest();
            ShowRest();
        }

        public bool PlayPose(string poseName)
        {
            if (!_ready)
            {
                return false;
            }

            EndBowShots();
            var isAttack = _attackSpec != null && string.Equals(poseName, BlenderRigAttackSpec.PoseName, System.StringComparison.Ordinal);
            if (isAttack && UpperBodyAttack && EnsureUpperMask())
            {
                return PlayAttackLayered();
            }

            if (_layers.IsValid())
            {
                if (UpperBodyAttack && string.Equals(poseName, _spec.ClipPoseName, System.StringComparison.Ordinal))
                {
                    // Back to the walk: the legs never left it; the take fades out over the arms.
                    StopAttack();
                    BeginFadeOut();
                    _wantWalkBase = true;
                    SetBase(true);
                    _pose = poseName;
                    return true;
                }

                LeaveLayers();
            }

            if (isAttack)
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
                    if (extra.HeldRealign != null)
                    {
                        BindHeldForExtraClip(extra.HeldRealign);
                    }

                    var ok = PlayClip(extra.FileName, extra.PoseName, extra.TakeName, extra.LastFrame);
                    if (ok && extra.BowShots != null && _attack != null)
                    {
                        _bowShots = extra.BowShots;
                        _attack.TickBowShots(_bowShots, 0f);
                    }

                    if (ok && extra.HeldRealign != null && _attack != null)
                    {
                        _heldRealign = extra.HeldRealign;
                        _attack.TickHeldRealign(_heldRealign, 0f);
                    }

                    return ok;
                }
            }

            Debug.LogWarning(_spec.Name + " unknown pose. " + poseName);
            return false;
        }

        public void Tick()
        {
            if (_layers.IsValid())
            {
                TickLayers();
                return;
            }

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

            if (EveryNth.Count > 0 && EveryNth.TryGetValue(_parent, out var every) && (Time.frameCount + _phase) % every != 0)
            {
                // Time still runs; the pose catches up on the next sampled frame.
                return;
            }

            _playable.SetTime(_time);
            _graph.Evaluate(0f);
            if (_bowShots != null)
            {
                _attack?.TickBowShots(_bowShots, _time * BlenderRigAttackSpec.FrameRate);
            }

            if (_heldRealign != null)
            {
                _attack?.TickHeldRealign(_heldRealign, _time * BlenderRigAttackSpec.FrameRate);
            }
        }

        /// <summary>
        /// Runner: whether the legs walk under an upper-body attack (true) or stand on the take's first frame (the squad
        /// halted at the boss's arena line). Only matters while <see cref="UpperBodyAttack"/> is on.
        /// </summary>
        public void SetWalkUnderAttack(bool walking)
        {
            _wantWalkBase = walking;
            if (_layers.IsValid())
            {
                SetBase(walking);
            }
        }

        private bool PlayAttackLayered()
        {
            if (_attackSpec == null || _attack == null)
            {
                return false;
            }

            var clip = LoadClip(_attackSpec.FileName, BlenderRigAttackSpec.PoseName, BlenderRigAttackSpec.TakeName, _attackSpec.ClipLastFrame);
            if (clip == null || clip.empty || clip.length < 0.2f)
            {
                Debug.LogError(_spec.Name + " clip missing. take=" + BlenderRigAttackSpec.TakeName + " file=" + _attackSpec.FileName);
                return false;
            }

            StopAttack();
            if (_attackClip != clip && _calib.IsValid())
            {
                _calib.Destroy();
            }

            _attackClip = clip;
            EnterLayers();
            CacheHipsTurn(clip);

            // The running take (if any) moves down to the fade-out slot; the new one goes on top and fades in over it.
            if (_prevTake.IsValid())
            {
                _layers.DisconnectInput(1);
                _prevTake.Destroy();
            }

            if (_playable.IsValid())
            {
                var from = TakeWeight();
                _layers.DisconnectInput(2);
                _graph.Connect(_playable, 0, _layers, 1);
                _prevTake = _playable;
                _prevFrom = from;
                _prevAge = 0f;
                _prevFrame = _takeFrame;
            }

            _playable = AnimationClipPlayable.Create(_graph, clip);
            _playable.SetDuration(clip.length);
            _playable.SetTime(0d);
            _playable.SetSpeed(1d);
            _graph.Connect(_playable, 0, _layers, 2);
            _length = clip.length;
            _time = 0f;
            _loop = false;
            _attackMode = true;
            _pose = BlenderRigAttackSpec.PoseName;
            _takeAge = 0f;
            _fadingOut = false;
            _takeFrame = BlenderRigAttackSpec.FirstFrame;
            _attack.SetIdlePropsVisible(false);
            // Held props bind on the full-body take (their keys are in the take's design space, hips included).
            _attack.Activate(SampleAttackFullBody);
            EvaluateLayers();
            _attack.Tick(BlenderRigAttackSpec.FirstFrame, true, 0);
            return true;
        }

        private void TickLayers()
        {
            var dt = Time.unscaledDeltaTime;
            if (_base.IsValid() && _baseLoop)
            {
                _baseTime += dt;
                if (_baseLength > 0.05f)
                {
                    _baseTime %= _baseLength;
                }
            }

            _takeAge += dt;
            _prevAge += dt;
            _outAge += dt;
            var inClip = false;
            var cycle = 0;
            if (_attackMode && _attackSpec != null)
            {
                // Same clock as the full-body take: the release lands on the same frame.
                _time += dt * AttackSpeed;
                _takeFrame = _attackSpec.CycleFrameFor(_time, out inClip, out cycle);
            }

            if (_prevTake.IsValid() && PrevWeight() <= 0f)
            {
                _layers.DisconnectInput(1);
                _prevTake.Destroy();
            }

            if (!_attackMode && _playable.IsValid() && TakeWeight() <= 0f)
            {
                _layers.DisconnectInput(2);
                _playable.Destroy();
            }

            var layered = _attackMode || _playable.IsValid() || _prevTake.IsValid();
            if (!layered && EveryNth.Count > 0 && EveryNth.TryGetValue(_parent, out var every) && (Time.frameCount + _phase) % every != 0)
            {
                return;
            }

            EvaluateLayers();
            if (_attackMode)
            {
                _attack?.Tick(_takeFrame, inClip, cycle);
            }
        }

        private float TakeWeight()
        {
            if (!_playable.IsValid())
            {
                return 0f;
            }

            return _fadingOut ? RunnerUpperBodyAttack.FadeOut(_outFrom, _outAge) : RunnerUpperBodyAttack.FadeIn(_takeAge);
        }

        private float PrevWeight() => _prevTake.IsValid() ? RunnerUpperBodyAttack.FadeOut(_prevFrom, _prevAge) : 0f;

        private void BeginFadeOut()
        {
            if (_fadingOut)
            {
                return;
            }

            _outFrom = TakeWeight();
            _outAge = 0f;
            _fadingOut = true;
        }

        private float FrameSeconds(float frame)
        {
            var seconds = frame / BlenderRigAttackSpec.FrameRate;
            return _length > 0.05f ? Mathf.Min(seconds, _length - 0.0005f) : seconds;
        }

        private void EvaluateLayers()
        {
            var take = TakeWeight();
            var prev = PrevWeight();
            if (_base.IsValid())
            {
                _base.SetTime(_baseTime);
            }

            if (_playable.IsValid())
            {
                _playable.SetTime(FrameSeconds(_takeFrame));
            }

            if (_prevTake.IsValid())
            {
                _prevTake.SetTime(FrameSeconds(_prevFrame));
            }

            _layers.SetInputWeight(0, 1f);
            _layers.SetInputWeight(1, prev);
            _layers.SetInputWeight(2, take);
            _graph.Evaluate(0f);
            CarryHipsTurn(take, prev);
        }

        /// <summary>
        /// The take turns the hips 60-72 degrees to aim; the hips stay on the walk, so that turn goes on the spine
        /// instead and the chest, arms and weapon point where the full-body take points them.
        /// </summary>
        private void CarryHipsTurn(float take, float prev)
        {
            if (_hips == null || _spine == null || _attackClip == null || !HipsTurnCache.TryGetValue(_attackClip, out var turns))
            {
                return;
            }

            var w = 1f - ((1f - take) * (1f - prev));
            if (w <= 1e-3f)
            {
                return;
            }

            var target = take > 0f ? HipsAt(turns, _takeFrame) : HipsAt(turns, _prevFrame);
            if (take > 0f && prev > 0f)
            {
                target = Quaternion.Slerp(HipsAt(turns, _prevFrame), target, take);
            }

            var delta = Quaternion.Inverse(_hips.localRotation) * target;
            _spine.localRotation = Quaternion.Slerp(Quaternion.identity, delta, w) * _spine.localRotation;
        }

        private static Quaternion HipsAt(Quaternion[] turns, float frame)
        {
            var f = Mathf.Clamp(frame, 0f, turns.Length - 1);
            var i = Mathf.Min((int)f, turns.Length - 1);
            var j = Mathf.Min(i + 1, turns.Length - 1);
            return Quaternion.Slerp(turns[i], turns[j], f - i);
        }

        /// <summary>The take's hips rotation at every frame, sampled once per clip (all instances share the FBX).</summary>
        private void CacheHipsTurn(AnimationClip clip)
        {
            if (_hips == null || HipsTurnCache.ContainsKey(clip) || _attackSpec == null)
            {
                return;
            }

            var frames = Mathf.Max(1, _attackSpec.ClipLastFrame - BlenderRigAttackSpec.FirstFrame) + 1;
            var turns = new Quaternion[frames];
            var probe = AnimationClipPlayable.Create(_graph, clip);
            _output.SetSourcePlayable(probe);
            for (var f = 0; f < frames; f++)
            {
                probe.SetTime(Mathf.Min((BlenderRigAttackSpec.FirstFrame + f) / (float)BlenderRigAttackSpec.FrameRate, clip.length - 0.0005f));
                _graph.Evaluate(0f);
                turns[f] = _hips.localRotation;
            }

            _output.SetSourcePlayable(_layers);
            probe.Destroy();
            HipsTurnCache[clip] = turns;
        }

        /// <summary>The attack driver's sampler while layered: poses the whole body on the take, then hands back to the layers.</summary>
        private void SampleAttackFullBody(float frame)
        {
            if (_attackClip == null || !_layers.IsValid())
            {
                return;
            }

            if (!_calib.IsValid())
            {
                _calib = AnimationClipPlayable.Create(_graph, _attackClip);
            }

            _output.SetSourcePlayable(_calib);
            _calib.SetTime(FrameSeconds(frame));
            _graph.Evaluate(0f);
            _output.SetSourcePlayable(_layers);
        }

        private void EnterLayers()
        {
            if (_layers.IsValid())
            {
                return;
            }

            _layers = AnimationLayerMixerPlayable.Create(_graph, 3);
            if (_upperMask != null)
            {
                _layers.SetLayerMaskFromAvatarMask(1u, _upperMask);
                _layers.SetLayerMaskFromAvatarMask(2u, _upperMask);
            }

            var adopt = _playable.IsValid() && _loop && _wantWalkBase
                && string.Equals(_pose, _spec.ClipPoseName, System.StringComparison.Ordinal);
            if (adopt)
            {
                // Already walking: that walk becomes the legs' base and keeps its phase.
                _base = _playable;
                _baseTime = _time;
                _baseLength = _length;
                _baseIsWalk = true;
                _baseLoop = true;
                _playable = default;
                _graph.Connect(_base, 0, _layers, 0);
            }
            else
            {
                if (_playable.IsValid())
                {
                    _playable.Destroy();
                }

                _playable = default;
                _base = default;
                SetBase(_wantWalkBase);
            }

            _layers.SetInputWeight(0, 1f);
            _output.SetSourcePlayable(_layers);
            _prevTake = default;
        }

        /// <summary>Legs' base: the walk loop, or the take's first frame held (the squad standing).</summary>
        private void SetBase(bool walking)
        {
            if (_base.IsValid() && _baseIsWalk == walking)
            {
                return;
            }

            var clip = walking
                ? LoadClip(_spec.ClipFileName, _spec.ClipPoseName, _spec.ClipTakeName, _spec.ClipLastFrame)
                : _attackClip;
            if (clip == null || clip.empty)
            {
                return;
            }

            if (_base.IsValid())
            {
                _layers.DisconnectInput(0);
                _base.Destroy();
            }

            _base = AnimationClipPlayable.Create(_graph, clip);
            _base.SetDuration(clip.length);
            _base.SetTime(0d);
            _base.SetSpeed(1d);
            _graph.Connect(_base, 0, _layers, 0);
            _layers.SetInputWeight(0, 1f);
            _baseIsWalk = walking;
            _baseLoop = walking;
            _baseLength = clip.length;
            _baseTime = 0f;
        }

        /// <summary>Back to the single full-body playable (rest, extra clips, or the layering switched off).</summary>
        private void LeaveLayers()
        {
            StopAttack();
            if (_prevTake.IsValid())
            {
                _prevTake.Destroy();
            }

            if (_base.IsValid())
            {
                _base.Destroy();
            }

            if (_calib.IsValid())
            {
                _calib.Destroy();
            }

            if (_playable.IsValid())
            {
                _playable.Destroy();
            }

            _layers.Destroy();
            _layers = default;
            _prevTake = default;
            _base = default;
            _calib = default;
            _playable = default;
            _fadingOut = false;
            _output.SetSourcePlayable(Playable.Null);
        }

        /// <summary>
        /// Builds the upper-body mask from this instance's hierarchy once: the hips' spine child and everything under it
        /// (and any side branch beside the hips) take the attack. False (full-body path) when the rig has no hips / spine.
        /// </summary>
        private bool EnsureUpperMask()
        {
            if (_maskTried)
            {
                return _upperMask != null;
            }

            _maskTried = true;
            if (!_ready || _animator == null)
            {
                return false;
            }

            var root = _animator.transform;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.EndsWith("Hips", System.StringComparison.Ordinal))
                {
                    _hips = t;
                    break;
                }
            }

            if (_hips != null)
            {
                foreach (Transform child in _hips)
                {
                    if (child.name.EndsWith("Spine", System.StringComparison.Ordinal))
                    {
                        _spine = child;
                        break;
                    }
                }
            }

            if (_hips == null || _spine == null)
            {
                Debug.LogWarning(_spec.Name + " has no hips / spine; its runner attack stays full-body.");
                return false;
            }

            var hipsPath = _hips.name;
            for (var p = _hips.parent; p != null && p != root; p = p.parent)
            {
                hipsPath = p.name + "/" + hipsPath;
            }

            var mask = new AvatarMask();
            mask.AddTransformPath(root, true);
            for (var i = 0; i < mask.transformCount; i++)
            {
                mask.SetTransformActive(i, RunnerUpperBodyAttack.IsUpperBody(mask.GetTransformPath(i), hipsPath, _spine.name));
            }

            _upperMask = mask;
            return true;
        }

        private void EndBowShots()
        {
            if (_heldRealign != null)
            {
                _attack?.EndHeldRealign(_heldRealign);
                _heldRealign = null;
            }

            if (_bowShots == null)
            {
                return;
            }

            _attack?.EndBowShots(_bowShots);
            _bowShots = null;
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
            if (!PlayClip(_attackSpec.FileName, BlenderRigAttackSpec.PoseName, BlenderRigAttackSpec.TakeName, _attackSpec.ClipLastFrame))
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

            _time += Time.unscaledDeltaTime * AttackSpeed;
            bool inClip;
            int cycle;
            var frame = _attackSpec != null
                ? _attackSpec.CycleFrameFor(_time, out inClip, out cycle)
                : BlenderRigAttackSpec.CycleFrame(_time, out inClip, out cycle);
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

        /// <summary>
        /// A held-realign extra clip (Oakenshield attack 2) shows the attack's held prop, which binds to its bone on the attack
        /// clip. If the attack has not played yet, play it once here to bind (idle props hidden while it is active), then stop it.
        /// </summary>
        private void BindHeldForExtraClip(BlenderHeldRealign realign)
        {
            if (_attack == null || _attackSpec == null || _attack.HeldBound(realign.TrackName))
            {
                return;
            }

            if (!PlayClip(_attackSpec.FileName, BlenderRigAttackSpec.PoseName, BlenderRigAttackSpec.TakeName, _attackSpec.ClipLastFrame))
            {
                Debug.LogWarning(_spec.Name + " could not bind " + realign.TrackName + " for the extra clip.");
                return;
            }

            _attack.SetIdlePropsVisible(false);
            _attack.Activate(SampleAttackFrame);
            _attack.Deactivate();
            _attack.SetIdlePropsVisible(true);
        }

        /// <summary>Idle props are keyed in design space at attack frame 0, so the hand they ride must be in that pose when the
        /// bone-local grip is taken. Binding on the freshly instantiated bind pose put the bow beside the palm (fingers closing
        /// in front of an empty grip) because the bind-pose hand sits elsewhere than the keyed hand.</summary>
        private void BindIdlePropsAtAttackRest()
        {
            if (_attack == null || _attackSpec == null || _idleProps.Length == 0)
            {
                return;
            }

            if (PlayClip(_attackSpec.FileName, BlenderRigAttackSpec.PoseName, BlenderRigAttackSpec.TakeName, _attackSpec.ClipLastFrame))
            {
                SampleAttackFrame(BlenderRigAttackSpec.FirstFrame);
            }
            else
            {
                Debug.LogWarning(_spec.Name + " idle props bound without the attack pose; grip may sit off the hand.");
            }

            _attack.BindIdleProps(_idleProps);
            _attack.SetIdlePropsVisible(true);
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
            if (_restAtAttackStart && _attackSpec != null && _ready
                && PlayClip(_attackSpec.FileName, BlenderRigAttackSpec.PoseName, BlenderRigAttackSpec.TakeName, _attackSpec.ClipLastFrame))
            {
                SampleAttackFrame(BlenderRigAttackSpec.FirstFrame);
            }

            _pose = BlenderRigSpec.RestPoseName;
            _loop = false;
            _length = 0f;
            _time = 0f;
        }

        private bool PlayClip(string fileName, string poseName, string takeName, int lastFrame)
        {
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
        }

        private AnimationClip? LoadClip(string fileName, string poseName, string takeName, int lastFrame)
        {
            var path = "Assets/" + _spec.ThemePackDir + "/" + fileName;
            // Once per clip per session: the import check reads the .meta from disk and LoadAllAssetsAtPath walks the
            // whole FBX, and the runner changes pose every few frames per rig.
            var key = path + "|" + _humanoid + "|" + poseName + "|" + takeName + "|" + lastFrame;
            var cached = ActorClipCache.Get(key);
            if (cached != null)
            {
                return cached;
            }

#if UNITY_EDITOR
            EnsureImport(path, _humanoid, true, poseName, takeName, lastFrame);
#endif
            AnimationClip? named = null;
            AnimationClip? fallback = null;
            foreach (var obj in PlayerAssets.LoadAll(path))
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

            return ActorClipCache.Put(key, named ?? fallback);
        }

        private static bool HasValidHumanAvatar(string path)
        {
            var avatar = LoadHumanAvatar(path);
            return avatar != null && avatar.isValid && avatar.isHuman;
        }

        private static Avatar? LoadHumanAvatar(string path)
        {
            foreach (var obj in RigAssetCache.All(path))
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
            foreach (var obj in RigAssetCache.All(path))
            {
                if (obj is Avatar avatar && !avatar.isHuman)
                {
                    return avatar;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        /// <summary>The body import check, once per file per session (it parses the .meta; ten copies of a hero asked ten times).</summary>
        private void EnsureBodyImport(string path, bool humanoid)
        {
            if (RigAssetCache.First("body|" + path + "|" + humanoid))
            {
                EnsureImport(path, humanoid, clip: false);
            }
        }

        private void EnsureImport(
            string path,
            bool humanoid,
            bool clip,
            string poseName = "",
            string takeName = "",
            int lastFrame = 0)
        {
            // Getters can disagree with the saved .meta and still write the same bytes back.
            // Reimporting from Play then reloads the scene and clears the demo dropdown.
            if (SavedSettingsMatch(path, humanoid, clip, poseName, takeName, lastFrame))
            {
                return;
            }

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

        private static bool SavedSettingsMatch(
            string path,
            bool humanoid,
            bool clip,
            string poseName,
            string takeName,
            int lastFrame)
        {
            var metaPath = path + ".meta";
            if (!System.IO.Path.IsPathRooted(metaPath) && !string.IsNullOrEmpty(Application.dataPath))
            {
                var root = System.IO.Directory.GetParent(Application.dataPath);
                if (root != null)
                {
                    metaPath = System.IO.Path.Combine(root.FullName, metaPath.Replace('/', System.IO.Path.DirectorySeparatorChar));
                }
            }

            if (!System.IO.File.Exists(metaPath))
            {
                return false;
            }

            var meta = System.IO.File.ReadAllText(metaPath);
            return !BlenderRigImportMeta.NeedsReimport(meta, humanoid, clip, poseName, takeName, lastFrame);
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
#endif

        private void BindEmbeddedMaps(GameObject root, string fbxPath)
        {
            // Shared between every copy of this rig kind: loaded and (for metal) repacked once per session.
            var albedo = RigAssetCache.Texture("map|" + _spec.BaseColorThemePackRel, () => LoadMap(_spec.BaseColorThemePackRel, _spec.BaseColorFile, fbxPath, asNormal: false));
            var normal = RigAssetCache.Texture("map|" + _spec.NormalThemePackRel + "|n", () => LoadMap(_spec.NormalThemePackRel, _spec.NormalFile, fbxPath, asNormal: true));
            Texture2D? metal = null;
            var wantsMetal = !string.IsNullOrEmpty(_spec.MetallicRoughnessFile);
            if (wantsMetal)
            {
                var metalRel = _spec.ThemePackDir + "/" + _spec.TextureFolder + "/" + _spec.MetallicRoughnessFile;
                metal = RigAssetCache.Texture("metal|" + metalRel, () =>
                {
                    var raw = LoadMap(metalRel, _spec.MetallicRoughnessFile, fbxPath, asNormal: false, linear: true, readable: true);
                    return PackGlTfMetallicRoughness(raw, _spec.Name);
                });
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

                    if (wantsMetal)
                    {
                        BindMetallicSmoothness(mat, metal);
                    }
                }

                rend.sharedMaterials = shared;
            }
        }

        /// <summary>
        /// URP Lit metallic workflow. Packed map: metallic and smoothness come from the map
        /// (scales 1). No usable map: plain dielectric with a modest sheen, never the raw glTF map.
        /// </summary>
        private static void BindMetallicSmoothness(Material mat, Texture2D? packed)
        {
            if (mat.HasProperty("_WorkflowMode"))
            {
                mat.SetFloat("_WorkflowMode", 1f);
            }

            mat.DisableKeyword("_SPECULAR_SETUP");
            if (mat.HasProperty("_SmoothnessTextureChannel"))
            {
                mat.SetFloat("_SmoothnessTextureChannel", 0f);
            }

            mat.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            if (packed != null && mat.HasProperty("_MetallicGlossMap"))
            {
                mat.SetTexture("_MetallicGlossMap", packed);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                SetFloatIfPresent(mat, "_Metallic", GlTfMetalRough.PackedMetallicScale);
                SetFloatIfPresent(mat, "_Smoothness", GlTfMetalRough.PackedSmoothnessScale);
                SetFloatIfPresent(mat, "_Glossiness", GlTfMetalRough.PackedSmoothnessScale);
                return;
            }

            if (mat.HasProperty("_MetallicGlossMap"))
            {
                mat.SetTexture("_MetallicGlossMap", null);
            }

            mat.DisableKeyword("_METALLICSPECGLOSSMAP");
            SetFloatIfPresent(mat, "_Metallic", GlTfMetalRough.FallbackMetallic);
            SetFloatIfPresent(mat, "_Smoothness", GlTfMetalRough.FallbackSmoothness);
            SetFloatIfPresent(mat, "_Glossiness", GlTfMetalRough.FallbackSmoothness);
        }

        private static void SetFloatIfPresent(Material mat, string property, float value)
        {
            if (mat.HasProperty(property))
            {
                mat.SetFloat(property, value);
            }
        }

        /// <summary>
        /// glTF metallic-roughness (G roughness, B metallic) into URP Lit metallic (RGB) and
        /// smoothness (A). Reads the linear, readable import. Does not paint new pixels.
        /// </summary>
        private static Texture2D? PackGlTfMetallicRoughness(Texture2D? source, string specName)
        {
            if (source == null)
            {
                Debug.LogWarning(specName + " metal-rough map missing; using plain dielectric.");
                return null;
            }

            try
            {
                var pixels = source.GetPixels();
                for (var i = 0; i < pixels.Length; i++)
                {
                    var (metallic, smoothness) = GlTfMetalRough.ToUrp(pixels[i].r, pixels[i].g, pixels[i].b);
                    pixels[i] = new Color(metallic, metallic, metallic, smoothness);
                }

                var packed = new Texture2D(source.width, source.height, TextureFormat.RGBA32, true, true);
                packed.SetPixels(pixels);
                packed.Apply(true, false);
                packed.wrapMode = source.wrapMode;
                packed.name = specName + "_metallicSmoothness";
                return packed;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(specName + " metal-rough pack failed; using plain dielectric. " + ex.Message);
                return null;
            }
        }

        private static Texture2D? LoadMap(
            string themePackRel,
            string fileName,
            string fbxPath,
            bool asNormal,
            bool linear = false,
            bool readable = false)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            var path = "Assets/" + themePackRel;
#if UNITY_EDITOR
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

                if (!asNormal && importer.sRGBTexture == linear)
                {
                    importer.sRGBTexture = !linear;
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
#else
            _ = asNormal;
            _ = linear;
            _ = readable;
#endif

            var onDisk = PlayerAssets.Load<Texture2D>(path);
            if (onDisk != null)
            {
                return onDisk;
            }

            var stem = System.IO.Path.GetFileNameWithoutExtension(fileName);
            foreach (var obj in PlayerAssets.LoadAll(fbxPath))
            {
                if (obj is Texture2D tex && tex.name.IndexOf(stem, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return tex;
                }
            }

            return null;
        }

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
            if (ActorSceneLights.Suppressed)
            {
                return;
            }

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
