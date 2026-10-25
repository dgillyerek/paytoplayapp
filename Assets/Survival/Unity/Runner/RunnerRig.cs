#nullable enable
using System.Collections.Generic;
using Survival.Domain.Enemies;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;
using Survival.Domain.Runner;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// One locked battle rig in the runner: the same actor components, FBXs, materials and walk / attack clips the
    /// Locked Battle builds (<see cref="LockedBattleDemo"/> is the source of truth; nothing is re-rigged), sized to its
    /// design height the same way, with the battle's own hit reaction (<see cref="LockedBattleReactions"/> on the root
    /// plus <see cref="LockedBattleReactionPose"/> on the bones), the hit flash, foot planting and a contact shadow.
    /// The slot carries lane position and facing; the host under it carries the body scale and the flinch.
    /// </summary>
    public sealed class RunnerRig
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private readonly List<(Renderer Renderer, int Index, Color Base)> _flash = new List<(Renderer, int, Color)>();
        private System.Func<string, bool> _play = _ => false;
        private System.Func<string> _pose = () => "rest";
        private LockedBattleGrounding? _ground;
        private LockedBattleReactionPose? _reactionPose;
        private LockedBattleReaction _reaction = LockedBattleReactions.Flinch;
        private Vector3 _away = Vector3.back;
        private float _hitAge = 10f;

        /// <summary>Body roll toward the sideways move (degrees, + = top to the left), eased.</summary>
        private float _lean;
        private float _leanTarget;

        /// <summary>
        /// A squad shooter holding its aim (<see cref="RunnerAimHold"/>): its body takes no hit step, hit lean, flinch
        /// pose or steering roll, so the firing stance never moves (the hit flash still shows).
        /// </summary>
        private bool _aimHeld;

        /// <summary>
        /// A held shooter's per-shot kick (<see cref="RunnerShotFlick"/>): a readable recoil on its arm / hand / shoulder / upper-spine
        /// bones, rotation only, eased back to the hold. Null for every other rig (and never in the battle).
        /// </summary>
        private RunnerShotFlickState? _flick;
        private readonly List<FlickBone> _flickBones = new List<FlickBone>();

        /// <summary>Degrees of lean per m/s of sideways speed, and the most it ever leans.</summary>
        public const float LeanPerMetrePerSecond = 0.75f;
        public const float MaxLeanDegrees = 7f;
        private bool _flashing;
        private float _deathAge = -1f;
        private Vector3 _baseLocal;
        private float _punch;
        private float _attackStarted = -99f;
        private int _swingIndex;
        private string _swingPose = "";
        private float _swingSeconds;
        private System.Action<float>? _attackSpeed;
        private System.Action<bool>? _walkUnderAttack;

        /// <summary>The bone a root-carrying walk moves (Blightroot); null for a rig whose clips stay in place.</summary>
        private Transform? _strideBone;

        /// <summary>The body's pivot, host-local: the stride bone's rest place, else mid height (RunnerRootPlacement).</summary>
        private Vector3 _pivotRest;

        private RunnerRig(LockedBattleFighter data, Transform slot, Transform host)
        {
            Data = data;
            Slot = slot;
            Host = host;
            Profile = LockedBattleHits.ProfileOf(data.Name);
        }

        public LockedBattleFighter Data { get; }
        public LockedBattleHitProfile Profile { get; }
        public Transform Slot { get; }
        public Transform Host { get; }
        public bool Built { get; private set; }
        public float Height { get; private set; } = 1.8f;
        public bool Dead => _deathAge >= 0f;
        public bool Gone => _deathAge > 1.1f;
        public string Pose => _pose();

        public string WalkPose => Data.WalkPoseName;
        public string AttackPose => Data.AttackPoseName;
        public string RestPose => Data.RestPoseName;

        /// <summary>
        /// Ironhowl swings through his whole battle rotation in the runner, not only the claw swipe (Derek 2026-10-24:
        /// "he just punches"). The others keep their one attack take.
        /// </summary>
        public bool RotatesAttacks => Data.Name == LockedBattleRoster.Ironhowl.Name && LockedBattleRotation.Count(Data) > 1;

        public static LockedBattleFighter? Find(string rosterName)
        {
            foreach (var f in LockedBattleRoster.All)
            {
                if (string.Equals(f.Name, rosterName, System.StringComparison.Ordinal))
                {
                    return f;
                }
            }

            return null;
        }

        /// <summary>Spawn <paramref name="rosterName"/> under <paramref name="parent"/>, <paramref name="extraScale"/> x its battle size.</summary>
        public static RunnerRig? Spawn(string rosterName, Transform parent, string name, float extraScale = 1f)
        {
            var found = Find(rosterName);
            if (found == null)
            {
                Debug.LogError("Runner: no locked battle fighter named " + rosterName);
                return null;
            }

            var data = found.Value;
            var slot = new GameObject(name);
            slot.transform.SetParent(parent, false);
            var host = new GameObject(name + "Actor");
            host.transform.SetParent(slot.transform, false);
            var rig = new RunnerRig(data, slot.transform, host.transform);
            var measured = new Bounds(Vector3.zero, Vector3.zero);
            var built = false;
            try
            {
                switch (data.Name)
                {
                    case "Emberfang":
                    {
                        var a = host.AddComponent<EmberfangActor>();
                        a.Build();
                        rig.Bind(a.PlayPose, () => a.Pose);
                        rig.OwnShotsOff(a.AttackDriver);
                        built = a.Built;
                        measured = a.VisibleBounds;
                        break;
                    }

                    case "Stormcrest":
                    {
                        var a = host.AddComponent<StormcrestActor>();
                        a.Build();
                        rig.Bind(a.PlayPose, () => a.Pose);
                        rig.OwnShotsOff(a.AttackDriver);
                        built = a.Built;
                        measured = a.VisibleBounds;
                        break;
                    }

                    case "Ironhowl":
                    {
                        var a = host.AddComponent<IronhowlActor>();
                        a.Build();
                        rig.Bind(a.PlayPose, () => a.Pose);
                        built = a.Built;
                        measured = a.VisibleBounds;
                        break;
                    }

                    case "Nightfang":
                    {
                        var a = host.AddComponent<NightfangActor>();
                        a.Build();
                        rig.Bind(a.PlayPose, () => a.Pose);
                        rig.OwnShotsOff(a.AttackDriver);
                        built = a.Built;
                        measured = a.VisibleBounds;
                        break;
                    }

                    case "Lyra":
                    {
                        var a = host.AddComponent<LyraActor>();
                        a.Build();
                        a.UpperBodyAttack = RunnerUpperBodyAttack.Applies(data.Name);
                        a.AimHoldFrame = a.UpperBodyAttack ? RunnerAimHold.HoldFrameFor(data.Name) : null;
                        rig._aimHeld = a.AimHoldFrame != null;
                        rig.Bind(a.PlayPose, () => a.Pose, v => a.AttackSpeed = v, a.SetWalkUnderAttack);
                        rig.OwnShotsOff(a.AttackDriver);
                        built = a.Built;
                        measured = a.VisibleBounds;
                        break;
                    }

                    case "Oakenshield":
                    {
                        var a = host.AddComponent<OakenshieldActor>();
                        a.Build();
                        a.UpperBodyAttack = RunnerUpperBodyAttack.Applies(data.Name);
                        a.AimHoldFrame = a.UpperBodyAttack ? RunnerAimHold.HoldFrameFor(data.Name) : null;
                        rig._aimHeld = a.AimHoldFrame != null;
                        rig.Bind(a.PlayPose, () => a.Pose, v => a.AttackSpeed = v, a.SetWalkUnderAttack);
                        rig.OwnShotsOff(a.AttackDriver);
                        built = a.Built;
                        measured = a.VisibleBounds;
                        break;
                    }

                    case "Vespera":
                    {
                        var a = host.AddComponent<VesperaActor>();
                        a.Build();
                        rig.Bind(a.PlayPose, () => a.Pose);
                        rig.OwnShotsOff(a.AttackDriver);
                        built = a.Built;
                        measured = a.VisibleBounds;
                        break;
                    }

                    case "Ashwyrm":
                    {
                        var a = host.AddComponent<AshwyrmActor>();
                        a.Build();
                        rig.Bind(a.PlayPose, () => a.Pose);
                        rig.OwnShotsOff(a.AttackDriver);
                        built = a.Built;
                        measured = a.VisibleBounds;
                        break;
                    }

                    case "Rowan":
                    {
                        var a = host.AddComponent<RowanActor>();
                        a.Build();
                        a.UpperBodyAttack = RunnerUpperBodyAttack.Applies(data.Name);
                        a.AimHoldFrame = a.UpperBodyAttack ? RunnerAimHold.HoldFrameFor(data.Name) : null;
                        rig._aimHeld = a.AimHoldFrame != null;
                        rig.Bind(a.PlayPose, () => a.Pose, v => a.AttackSpeed = v, a.SetWalkUnderAttack);
                        rig.OwnShotsOff(a.AttackDriver);
                        built = a.Built;
                        measured = a.VisibleBounds;
                        break;
                    }

                    case "Bonequill":
                    {
                        var a = host.AddComponent<BonequillActor>();
                        a.Build();
                        rig.Bind(a.PlayPose, () => a.Pose);
                        rig.OwnShotsOff(a.AttackDriver);
                        built = a.Built;
                        measured = a.VisibleBounds;
                        break;
                    }

                    case "Sir Aldric":
                    {
                        // Same as the battle: the pilot actor tunes the global ambient for its own scene; keep ours.
                        var reflections = RenderSettings.reflectionIntensity;
                        var ambient = RenderSettings.ambientIntensity;
                        var a = host.AddComponent<SirAldricMeshyAnimateActor>();
                        a.Build();
                        RenderSettings.reflectionIntensity = reflections;
                        RenderSettings.ambientIntensity = ambient;
                        if (a.Built)
                        {
                            // Sized on the upright walk body (the idle is a crouched stance).
                            a.PlayBattlePose(SirAldricBattlePoses.Walk);
                            measured = Measure(host.transform);
                            a.PlayBattlePose(SirAldricBattlePoses.Rest);
                        }

                        rig.Bind(a.PlayBattlePose, () => a.BattlePose, v => a.BattleSpeed = v);
                        built = a.Built && measured.size.y > 1e-3f;
                        break;
                    }

                    case "Blightroot":
                    {
                        // Own pivot, as in the battle: its stand-up turn and foot plant write that transform.
                        var pivot = new GameObject("BlightrootPivot");
                        pivot.transform.SetParent(host.transform, false);
                        var a = pivot.AddComponent<BlightrootActor>();
                        a.Build();
                        if (a.Built && a.PlayBattlePose(BlightrootBattlePoses.Rest))
                        {
                            a.FaceWalkForward();
                            measured = Measure(host.transform);
                        }

                        rig.Bind(a.PlayBattlePose, () => a.BattlePose);
                        built = a.Built && measured.size.y > 1e-3f;
                        break;
                    }

                    default:
                        Debug.LogError("Runner: no actor for " + data.Name);
                        break;
                }
            }
            catch (System.Exception ex)
            {
                // One actor failing to build must not take the run down with it: that fighter is skipped.
                Debug.LogException(ex);
                built = false;
            }

            if (!built)
            {
                Debug.LogError("Runner: " + data.Name + " did not build.");
                Object.Destroy(slot);
                return null;
            }

            rig.Built = true;
            var height = measured.size.y;
            var scale = (height > 1e-4f ? data.DesignHeightMetres / height : 1f) * LockedBattleRoster.BattleScaleOf(data) * extraScale;
            host.transform.localScale = Vector3.one * scale;
            // Most actors measure world-space renderer bounds, and the pool builds its rigs on a shelf 500 m below the
            // road, so that box's height is right but its min / centre are not in the host's frame. The feet and the
            // turn pivot need the host's own frame: a pivot ~500 m off swung the host metres sideways with the steer lean.
            var hostBox = Measure(host.transform);
            if (hostBox.size.y < 1e-4f)
            {
                hostBox = new Bounds(new Vector3(0f, height * 0.5f, 0f), measured.size);
            }

            rig._baseLocal = new Vector3(0f, -hostBox.min.y * scale, 0f);
            host.transform.localPosition = rig._baseLocal;
            rig.Height = data.DesignHeightMetres * LockedBattleRoster.BattleScaleOf(data) * extraScale;
            rig._ground = new LockedBattleGrounding(host.transform);
            rig._pivotRest = new Vector3(0f, hostBox.center.y, 0f);
            if (RunnerStrideHold.Applies(data))
            {
                rig._strideBone = FindBone(host.transform, data.StrideBone);
                if (rig._strideBone != null)
                {
                    rig._pivotRest = host.transform.InverseTransformPoint(rig._strideBone.position);
                }
                else
                {
                    Debug.LogWarning("Runner: " + data.Name + " stride bone " + data.StrideBone + " not found; its walk is not held.");
                }
            }

            LockedBattleGround.AddContactShadow(slot.transform, measured.size.x * scale, measured.size.z * scale, LockedBattleRoster.BattleScaleOf(data) * extraScale);
            foreach (var r in host.GetComponentsInChildren<Renderer>(true))
            {
                if (!(r is SkinnedMeshRenderer) && !(r is MeshRenderer))
                {
                    continue;
                }

                var mats = r.sharedMaterials;
                for (var i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    var c = Color.white;
                    if (m != null && m.HasProperty(BaseColorId))
                    {
                        c = m.GetColor(BaseColorId);
                    }
                    else if (m != null && m.HasProperty(ColorId))
                    {
                        c = m.GetColor(ColorId);
                    }

                    rig._flash.Add((r, i, c));
                }
            }

            rig._reactionPose = LockedBattleReactionPose.Build(host.transform, LockedBattleReactions.RigFor(data));
            rig.BuildFlick(host.transform);
            rig.Play(rig.RestPose);
            return rig;
        }

        /// <summary>Finds the kicked bones of a held shooter (parent before child); no-op for every other rig.</summary>
        private void BuildFlick(Transform host)
        {
            var bones = RunnerShotFlick.BonesFor(Data.Name);
            if (!_aimHeld || bones.Length == 0)
            {
                return;
            }

            var all = host.GetComponentsInChildren<Transform>(true);
            foreach (var bone in bones)
            {
                foreach (var t in all)
                {
                    if (string.Equals(t.name, bone.Bone, System.StringComparison.Ordinal))
                    {
                        _flickBones.Add(new FlickBone(t, bone.PitchDegrees));
                        break;
                    }
                }
            }

            _flick = new RunnerShotFlickState(RunnerShotFlick.SecondsFor(Data.Name));
        }

        /// <summary>The sim fired this rig's shot (the scene draws it this frame): start the firing recoil.</summary>
        public void ShotFlick()
        {
            if (!Dead)
            {
                _flick?.Fire();
            }
        }

        /// <summary>
        /// After the clips posed the bones: the kick as a pitch about the road's right axis on each kicked bone, scaled by
        /// the kick's weight. Rotation only, never the host, hips or legs, so the body does not move forward or back and
        /// the legs keep walking. The clip value is put back first whenever nothing re-posed the bone (a skipped
        /// evaluation), so the kick never accumulates.
        /// </summary>
        private void TickFlick(float dt)
        {
            if (_flick == null)
            {
                return;
            }

            foreach (var b in _flickBones)
            {
                b.Restore();
            }

            _flick.Advance(dt);
            var w = Dead ? 0f : _flick.Weight;
            if (w <= 1e-4f)
            {
                return;
            }

            foreach (var b in _flickBones)
            {
                b.Set(Quaternion.AngleAxis(b.PitchDegrees * w, Vector3.right) * b.T.rotation);
            }
        }

        private sealed class FlickBone
        {
            private Quaternion _base;
            private Quaternion _applied;
            private bool _has;

            public FlickBone(Transform t, float pitchDegrees)
            {
                T = t;
                PitchDegrees = pitchDegrees;
            }

            public Transform T { get; }

            public float PitchDegrees { get; }

            public void Restore()
            {
                if (_has && T != null && Quaternion.Angle(T.localRotation, _applied) < 0.01f)
                {
                    T.localRotation = _base;
                }

                _has = false;
            }

            public void Set(Quaternion world)
            {
                if (T == null)
                {
                    return;
                }

                _base = T.localRotation;
                T.rotation = world;
                _applied = T.localRotation;
                _has = true;
            }
        }

        /// <summary>
        /// The runner draws every shot itself (the sim's shot, one per take, at the take's release), so the rig's own
        /// flying-projectile track (the battle's thrown spear, arrow and bolt) must not draw a second one: the held
        /// props and the clip still play, only the flight is off.
        /// </summary>
        private void OwnShotsOff(BlenderRigAttackDriver? driver)
        {
            if (driver != null)
            {
                driver.ProjectilesEnabled = false;
            }
        }

        private void Bind(
            System.Func<string, bool> play,
            System.Func<string> pose,
            System.Action<float>? attackSpeed = null,
            System.Action<bool>? walkUnderAttack = null)
        {
            _play = play;
            _pose = pose;
            _attackSpeed = attackSpeed;
            _walkUnderAttack = walkUnderAttack;
        }

        /// <summary>
        /// Shooters that fire on the upper body over the walk (<see cref="RunnerUpperBodyAttack"/>): their legs walk under
        /// the attack (true) or stand (the squad halted at the boss's arena line). No-op for the other rigs.
        /// </summary>
        public void WalkUnderAttack(bool walking)
        {
            if (!Dead)
            {
                _walkUnderAttack?.Invoke(walking);
            }
        }

        /// <summary>True when the actor can play its attack take faster (the squad shooters and Sir Aldric).</summary>
        public bool HasAttackSpeed => _attackSpeed != null;

        /// <summary>
        /// One attack take from its first frame at <paramref name="speed"/>x (the runner sim's AttackStarted). The take is
        /// as long as the fire interval, so this lands exactly as the last take reaches its end frame: both are the rest
        /// pose, and the restart does not pop. The shot follows at the take's release (the sim's Fired).
        /// </summary>
        public void StartTake(float speed)
        {
            if (Dead)
            {
                return;
            }

            _attackSpeed?.Invoke(speed > 0.01f ? speed : 1f);
            if (!Data.AttackCyclesItself && !_aimHeld)
            {
                // Sir Aldric's named clips restart from their origin on each attack pose.
                Play(RestPose);
            }

            Play(AttackPose);
            _attackStarted = Time.time;
        }

        public bool Play(string pose)
        {
            if (Dead)
            {
                return false;
            }

            return _play(pose);
        }

        /// <summary>Walk unless already walking.</summary>
        public void Walk()
        {
            if (!Dead && Pose != WalkPose)
            {
                Play(WalkPose);
            }
        }

        public void Rest()
        {
            if (!Dead && Pose != RestPose)
            {
                Play(RestPose);
            }
        }

        /// <summary>
        /// One attack. Rigs whose attack pose cycles itself (attack + rest gap, repeating) keep going; the others
        /// (Sir Aldric, Blightroot, Ironhowl) restart their clip for each attack.
        /// </summary>
        public void Attack()
        {
            if (Dead)
            {
                return;
            }

            if (RotatesAttacks)
            {
                // Let the current swing mostly play, then the next attack of his rotation.
                if (Pose == _swingPose && Time.time - _attackStarted < Mathf.Max(0.3f, _swingSeconds * 0.6f))
                {
                    return;
                }

                var next = LockedBattleRotation.AttackAt(Data, _swingIndex % LockedBattleRotation.Count(Data));
                _swingIndex++;
                Swing(next.PoseName, next.Seconds);
                return;
            }

            if (Pose != AttackPose)
            {
                Play(AttackPose);
                _attackStarted = Time.time;
            }
            else if (!Data.AttackCyclesItself && Time.time - _attackStarted >= Mathf.Max(0.3f, Data.AttackSeconds * 0.6f))
            {
                // The runner fires ~3x faster than these clips last: restart the swing only once most of it has played,
                // so it reads as a swing and not a twitch (the hits still land at the runner's rate).
                Play(RestPose);
                Play(AttackPose);
                _attackStarted = Time.time;
            }
        }

        /// <summary>
        /// One named attack take from its first frame (the boss quake's ground pound). A later <see cref="Attack"/>
        /// waits for most of it before the next swing.
        /// </summary>
        public void Swing(string pose, float seconds)
        {
            if (Dead)
            {
                return;
            }

            Play(RestPose);
            Play(pose);
            _swingPose = pose;
            _swingSeconds = seconds;
            _attackStarted = Time.time;
        }

        /// <summary>Face a world direction on the ground (yaw only), plus an aim offset (Rowan's bow).</summary>
        /// <summary>Lean with a sideways speed (m/s, + = right): the top of the body tips toward the move.</summary>
        public void LeanWith(float sidewaysSpeed) =>
            _leanTarget = Mathf.Clamp(-sidewaysSpeed * LeanPerMetrePerSecond, -MaxLeanDegrees, MaxLeanDegrees);

        public void Face(Vector3 direction, float aimYaw = 0f)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-6f)
            {
                return;
            }

            var target = Quaternion.LookRotation(direction.normalized, Vector3.up) * Quaternion.Euler(0f, aimYaw, 0f);
            Slot.rotation = Quaternion.Slerp(Slot.rotation, target, 1f - Mathf.Exp(-Time.deltaTime * 12f));
        }

        /// <summary>The battle's hit reaction for an attack by <paramref name="attacker"/> (null: a plain flinch), pushed along <paramref name="away"/>.</summary>
        public void Hit(LockedBattleFighter? attacker, Vector3 away, float strength = 1f)
        {
            if (Dead)
            {
                return;
            }

            _reaction = attacker is LockedBattleFighter a ? LockedBattleReactions.ForHit(a, LockedBattleHits.ProfileOf(a.Name), 0) : LockedBattleReactions.Flinch;
            away.y = 0f;
            _away = away.sqrMagnitude > 1e-6f ? away.normalized : -Slot.forward;
            _hitAge = 0f;
            _punch = Mathf.Clamp01(strength);
        }

        /// <summary>Tip over and sink into the road, then <see cref="Gone"/>.</summary>
        public void Die(Vector3 away)
        {
            if (Dead)
            {
                return;
            }

            away.y = 0f;
            _away = away.sqrMagnitude > 1e-6f ? away.normalized : -Slot.forward;
            _deathAge = 0f;
            _hitAge = 0f;
            _reaction = LockedBattleReactions.Stagger;
        }

        /// <summary>
        /// Puts the host so the body's pivot shows where the sim puts the slot (plus the hit nudge, minus the death sink)
        /// whatever the host's turn, cancelling a root-carrying clip's travel in the host's own frame (RunnerRootPlacement).
        /// </summary>
        private void PlaceHost(Vector3 nudge, float sink)
        {
            var bone = _strideBone != null ? Host.InverseTransformPoint(_strideBone.position) : _pivotRest;
            var placed = RunnerRootPlacement.HostPosition(
                N(_baseLocal), N(nudge), sink, NQ(Host.localRotation), N(Host.localScale), N(_pivotRest), N(bone), _strideBone != null);
            Host.localPosition = new Vector3(placed.X, placed.Y, placed.Z);
        }

        private static System.Numerics.Vector3 N(Vector3 v) => new System.Numerics.Vector3(v.x, v.y, v.z);

        private static System.Numerics.Quaternion NQ(Quaternion q) => new System.Numerics.Quaternion(q.x, q.y, q.z, q.w);

        /// <summary>A bone by name under <paramref name="root"/>; "Scope/Bone" looks inside the named child (as the battle's).</summary>
        private static Transform? FindBone(Transform root, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            var slash = name.LastIndexOf('/');
            if (slash > 0)
            {
                var scope = FindBone(root, name.Substring(0, slash));
                return scope != null ? FindBone(scope, name.Substring(slash + 1)) : null;
            }

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(t.name, name, System.StringComparison.Ordinal))
                {
                    return t;
                }
            }

            return null;
        }

        /// <summary>Called from the scene's LateUpdate after the clips posed the bones.</summary>
        public void Late(float dt)
        {
            _hitAge += dt;
            TickFlick(dt);
            // A held shooter's body never reacts (RunnerAimHold.BodyReactionScale): no step, lean or flinch pose.
            var w = _aimHeld ? 0f : _reaction.Weight(_hitAge);
            var local = Slot.InverseTransformDirection(_away);
            // The battle's root reaction: step back and lean, sized by the defender's recoil profile.
            var punch = 0.6f + (0.4f * _punch);
            var step = local * (_reaction.RootScale * w * punch);
            var tilt = local * (_reaction.RootLeanScale * w * punch);
            // A small flinch only, never a pushback (Derek 2026-10-23): capped for every enemy whatever its profile and scale.
            var nudge = step * Profile.RecoilMetres;
            nudge.y = 0f;
            nudge *= RunnerHitNudge.Scale(nudge.x, nudge.z);
            var lean = Mathf.Tan(Profile.RecoilDegrees * Mathf.Deg2Rad);
            // Lean into the sideways move: roll about the road's forward axis (in the slot's frame), eased in and out.
            // (Not for a held shooter: the aim stays level, RunnerAimHold.LeanScale.)
            _lean = Mathf.Lerp(_lean, _aimHeld ? 0f : _leanTarget, 1f - Mathf.Exp(-12f * dt));
            var roll = Quaternion.AngleAxis(_lean, Slot.InverseTransformDirection(Vector3.forward));
            Host.localRotation = roll * Quaternion.FromToRotation(Vector3.up, (Vector3.up + (tilt * lean)).normalized);
            if (_deathAge >= 0f)
            {
                _deathAge += dt;
                var t = Mathf.Clamp01(_deathAge / 0.9f);
                var fall = Vector3.Cross(Vector3.up, local).normalized;
                Host.localRotation = Quaternion.AngleAxis(-80f * Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t * 1.6f)), fall == Vector3.zero ? Vector3.right : fall);
                PlaceHost(nudge, Height * 0.9f * t * t);
            }
            else
            {
                PlaceHost(nudge, 0f);
                _ground?.Plant(Slot.position.y);
                if (_reactionPose != null && w > 0f)
                {
                    _reactionPose.Apply(Slot, _reaction, _away, w, _reaction, _away, 0f);
                }
            }

            var flash = _deathAge >= 0f ? 0f : LockedBattleHits.FlashWeight(_hitAge);
            if (flash > 0f)
            {
                var tint = new Color(Profile.FlashR, Profile.FlashG, Profile.FlashB, 1f);
                var gain = Color.Lerp(Color.white, tint * 2.4f, flash);
                foreach (var (r, i, c) in _flash)
                {
                    if (r == null)
                    {
                        continue;
                    }

                    var lit = c * gain;
                    lit.a = c.a;
                    _block.Clear();
                    _block.SetColor(BaseColorId, lit);
                    _block.SetColor(ColorId, lit);
                    _block.SetColor(EmissionColorId, tint * (1.5f * flash));
                    r.SetPropertyBlock(_block, i);
                }

                _flashing = true;
            }
            else if (_flashing)
            {
                foreach (var (r, i, c) in _flash)
                {
                    if (r == null)
                    {
                        continue;
                    }

                    _block.Clear();
                    _block.SetColor(BaseColorId, c);
                    _block.SetColor(ColorId, c);
                    r.SetPropertyBlock(_block, i);
                }

                _flashing = false;
            }
        }

        public void Destroy()
        {
            SetEvaluateEvery(1);
            if (Slot != null)
            {
                Object.Destroy(Slot.gameObject);
            }
        }

        // ---------- pooling (RunnerRigPool) ----------

        private readonly List<Renderer> _parkedRenderers = new List<Renderer>();
        private readonly List<Behaviour> _parkedBehaviours = new List<Behaviour>();
        private readonly List<(Animator Animator, AnimatorCullingMode Mode)> _parkedAnimators = new List<(Animator, AnimatorCullingMode)>();
        private static readonly List<Renderer> ScratchRenderers = new List<Renderer>();
        private static readonly List<Behaviour> ScratchBehaviours = new List<Behaviour>();

        /// <summary>The pool shelf this rig goes back to: roster name and extra scale.</summary>
        public (string Roster, int ScaleKey) PoolKey { get; internal set; }

        public bool Parked { get; private set; }

        private int _evaluateEvery = 1;

        /// <summary>
        /// Sample this rig's walk / idle loop every <paramref name="frames"/>th frame (the squad's back rows: small on
        /// screen, half hidden behind the rows in front). Attacks always run every frame.
        /// </summary>
        public void SetEvaluateEvery(int frames)
        {
            frames = Mathf.Max(1, frames);
            if (frames == _evaluateEvery)
            {
                return;
            }

            _evaluateEvery = frames;
            BlenderRigPlayer.SetEvaluateEvery(Host, frames);
        }

        /// <summary>
        /// Every pose the runner asks for, once, so the lazy work (clip lookups, the attack driver's props and effects,
        /// Sir Aldric's named takes) happens now (the loading screen) and not on this rig's first swing mid-run.
        /// </summary>
        public void WarmPoses()
        {
            if (RotatesAttacks)
            {
                for (var i = 0; i < LockedBattleRotation.Count(Data); i++)
                {
                    Play(LockedBattleRotation.AttackAt(Data, i).PoseName);
                }
            }

            Play(AttackPose);
            Play(RestPose);
            Play(WalkPose);
            Play(RestPose);
        }

        /// <summary>
        /// Off the road without deactivating anything: renderers and scripts off (so no skinning, no clip evaluation, no
        /// cloth), animators culled, the slot moved under <paramref name="shelf"/>. Deactivating would make every
        /// Animator rebind on the way back, which is the cost the pool exists to avoid.
        /// </summary>
        public void Park(Transform shelf)
        {
            if (Parked || Slot == null)
            {
                return;
            }

            Parked = true;
            SetEvaluateEvery(1);
            _parkedRenderers.Clear();
            _parkedBehaviours.Clear();
            _parkedAnimators.Clear();
            Slot.GetComponentsInChildren(false, ScratchRenderers);
            foreach (var r in ScratchRenderers)
            {
                if (r.enabled)
                {
                    r.enabled = false;
                    _parkedRenderers.Add(r);
                }
            }

            Slot.GetComponentsInChildren(false, ScratchBehaviours);
            foreach (var b in ScratchBehaviours)
            {
                if (b is Animator a)
                {
                    _parkedAnimators.Add((a, a.cullingMode));
                    a.cullingMode = AnimatorCullingMode.CullCompletely;
                    continue;
                }

                if (b.enabled)
                {
                    b.enabled = false;
                    _parkedBehaviours.Add(b);
                }
            }

            ScratchRenderers.Clear();
            ScratchBehaviours.Clear();
            Slot.SetParent(shelf, false);
            Slot.localPosition = Vector3.zero;
            Slot.localRotation = Quaternion.identity;
        }

        /// <summary>Back on the road under <paramref name="parent"/> as <paramref name="name"/>, alive and un-hit.</summary>
        public void Unpark(Transform parent, string name)
        {
            if (!Parked || Slot == null)
            {
                return;
            }

            Parked = false;
            Slot.SetParent(parent, false);
            Slot.gameObject.name = name;
            foreach (var (a, mode) in _parkedAnimators)
            {
                if (a != null)
                {
                    a.cullingMode = mode;
                }
            }

            foreach (var b in _parkedBehaviours)
            {
                if (b != null)
                {
                    b.enabled = true;
                }
            }

            foreach (var r in _parkedRenderers)
            {
                if (r != null)
                {
                    r.enabled = true;
                }
            }

            _parkedAnimators.Clear();
            _parkedBehaviours.Clear();
            _parkedRenderers.Clear();
            ResetState();
        }

        private void ResetState()
        {
            _flick?.Reset();
            foreach (var b in _flickBones)
            {
                b.Restore();
            }

            var wasDead = _deathAge >= 0f;
            _deathAge = -1f;
            _hitAge = 10f;
            _punch = 0f;
            _lean = 0f;
            _leanTarget = 0f;
            _attackStarted = -99f;
            _reaction = LockedBattleReactions.Flinch;
            _away = Vector3.back;
            Host.localPosition = _baseLocal;
            Host.localRotation = Quaternion.identity;
            if (_flashing)
            {
                foreach (var (r, i, c) in _flash)
                {
                    if (r == null)
                    {
                        continue;
                    }

                    _block.Clear();
                    _block.SetColor(BaseColorId, c);
                    _block.SetColor(ColorId, c);
                    r.SetPropertyBlock(_block, i);
                }

                _flashing = false;
            }

            if (wasDead)
            {
                // A rig that died last went out mid-stagger: start it from its rest pose.
                Play(RestPose);
            }
        }

        private static Bounds Measure(Transform host)
        {
            var seeded = false;
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var r in host.GetComponentsInChildren<Renderer>(false))
            {
                if (!r.enabled || (!(r is SkinnedMeshRenderer) && !(r is MeshRenderer)))
                {
                    continue;
                }

                var b = r.bounds;
                var min = host.InverseTransformPoint(b.min);
                var max = host.InverseTransformPoint(b.max);
                var local = new Bounds((min + max) * 0.5f, Vector3.zero);
                local.Encapsulate(min);
                local.Encapsulate(max);
                if (!seeded)
                {
                    bounds = local;
                    seeded = true;
                }
                else
                {
                    bounds.Encapsulate(local);
                }
            }

            return bounds;
        }
    }
}
