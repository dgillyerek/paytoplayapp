using System.Collections.Generic;
using Survival.Domain.Enemies;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Locked battle, Game view 1080×1920. Every locked, merged character with a walk and an
    /// attack: heroes start at the bottom (−Z) facing up, villains at the top (+Z) facing down.
    /// Each one holds rest, walks at its opponent on its own locked walk clip, stops at its
    /// attack range, and plays its own locked attack (with its existing attack effects) with a
    /// short rest between attacks. Five lanes: the four locked matchups plus lane 5,
    /// Sir Aldric vs Blightroot (Derek approved 2026-10-15). Flying attacks stop on the target's body box and melee
    /// attacks land on their contact frame. Each landing spawns the attacker's impact burst,
    /// and the defender flinches (a small knock-back on its actor root, skipped mid-attack) and
    /// flashes. Pure presentation: the existing actors play the locked FBXs as-is, and the
    /// flinch moves only the actor root. Nobody takes damage. HOLD merge until Derek Game-view PASS.
    /// Attack rotation (2026-10-17, humanoids): each fighter cycles its attack list 1, 2, 3 with the
    /// rest gap after each (<see cref="LockedBattleRotation"/>); fighters with one attack (the
    /// creatures, on hold) loop it exactly as before. Blightroot: swipe, punch, jump smash.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed class LockedBattleDemo : MonoBehaviour
    {
        public const string RestartButtonObjectName = "LockedBattleRestart";

        private readonly List<Fighter> _fighters = new List<Fighter>();
        private LockedBattleCamera? _camera;
        private bool _booted;
        private float _time;

        private enum Phase
        {
            Hold = 0,
            Walk = 1,
            Attack = 2
        }

        private void Awake() => Boot();

        private void OnEnable() => Boot();

        private void Start() => Boot();

        /// <summary>Puts everyone back at the start in rest and runs the fight again.</summary>
        public void Restart()
        {
            _time = 0f;
            foreach (var f in _fighters)
            {
                if (!f.Built)
                {
                    continue;
                }

                f.Slot.SetPositionAndRotation(f.StartPosition, f.StartRotation);
                f.Play(f.Data.RestPoseName);
                f.Phase = Phase.Hold;
                f.PhaseTime = 0f;
                f.PreviousPhaseTime = 0f;
                ClearHit(f);
                f.AttackClipOn = false;
                f.AttackIndex = 0;
                f.AttackRootStart = Vector3.zero;
                f.AttackOffset = Vector3.zero;
                f.Host.localPosition = f.BaseLocal;
                f.PreviousForward = 0f;
                f.RestForward = 0f;
            }
        }

        private void Update()
        {
            if (!_booted)
            {
                Boot();
                return;
            }

            var dt = Time.unscaledDeltaTime;
            _time += dt;
            foreach (var f in _fighters)
            {
                if (!f.Built || f.Target == null || !f.Target.Built)
                {
                    continue;
                }

                f.PreviousPhaseTime = f.PhaseTime;
                f.PhaseTime += dt;
                switch (f.Phase)
                {
                    case Phase.Hold:
                        f.RestForward = StrideForward(f);
                        if (_time >= LockedBattleRoster.OpeningHoldSeconds)
                        {
                            BeginWalk(f);
                        }

                        break;
                    case Phase.Walk:
                        TickWalk(f, dt);
                        break;
                    case Phase.Attack:
                        TickAttack(f);
                        break;
                }
            }

            foreach (var f in _fighters)
            {
                if (f.Built)
                {
                    TickHitReaction(f, dt);
                }
            }

        }

        private void BeginWalk(Fighter f)
        {
            f.Phase = Phase.Walk;
            f.PhaseTime = 0f;
            f.Play(f.Data.WalkPoseName);
            f.PreviousForward = StrideForward(f);
        }

        private void TickWalk(Fighter f, float dt)
        {
            var target = f.Target!;
            var self = VisualPosition(f);
            var to = VisualPosition(target) - self;
            to.y = 0f;
            var distance = to.magnitude;
            if (distance > 1e-4f)
            {
                Face(f, to);
            }

            if (f.Data.WalkCarriesRoot)
            {
                var forward = StrideForward(f);
                var snap = LockedBattleRoster.LoopSnapBack(f.PreviousForward, forward, f.Data.WalkRootStrideMetres);
                if (snap > 0f)
                {
                    f.Slot.position += Flat(f.Slot.forward) * snap;
                }

                f.PreviousForward = forward;
            }
            else if (distance > 1e-4f)
            {
                var step = LockedBattleRoster.ApproachStep(distance, f.Data.AttackRangeMetres, f.Data.WalkSpeedMetresPerSecond, dt);
                f.Slot.position += (to / distance) * step;
            }

            if (distance <= f.Data.AttackRangeMetres)
            {
                BeginAttack(f);
            }
        }

        private void BeginAttack(Fighter f)
        {
            if (f.Data.WalkCarriesRoot)
            {
                // Keep the body where the walk left it when the clip stops carrying it.
                f.Slot.position += Flat(f.Slot.forward) * (StrideForward(f) - f.RestForward);
            }

            f.Phase = Phase.Attack;
            f.PhaseTime = 0f;
            f.PreviousPhaseTime = 0f;
            f.AttackClipOn = true;
            StartAttackClip(f, 0);
            MatchEffectWidths(f);
        }

        /// <summary>Plays attack <paramref name="index"/> of the fighter's rotation (attack 1 is the locked attack).</summary>
        private static void StartAttackClip(Fighter f, int index)
        {
            f.AttackIndex = index;
            f.AttackOffset = Vector3.zero;
            f.Play(LockedBattleRotation.AttackAt(f.Data, index).PoseName);
            f.AttackRootStart = RawStride(f);
        }

        /// <summary>
        /// Stride bone in the slot frame (metres) with this battle's own host offsets (reach slide,
        /// recoil) taken out, so the in-place hold does not chase itself.
        /// </summary>
        private static Vector3 RawStride(Fighter f)
        {
            if (f.StrideBone == null)
            {
                return Vector3.zero;
            }

            return f.Slot.InverseTransformPoint(f.StrideBone.position) - (f.Host.localPosition - f.BaseLocal);
        }

        /// <summary>
        /// Host offset for the attack playing now: the reach slide toward the defender, and for a
        /// leap (in place) the stride bone's own forward carry held back on the spot.
        /// </summary>
        private static void UpdateAttackOffset(Fighter f, bool inClip, float secondsIntoAttack)
        {
            var attack = LockedBattleRotation.AttackAt(f.Data, f.AttackIndex);
            var offset = Vector3.zero;
            if (inClip)
            {
                offset.z = attack.ReachShift.At(secondsIntoAttack * BlenderRigAttackSpec.FrameRate);
            }

            if (attack.InPlace && f.StrideBone != null)
            {
                // Held through the rest gap too: the actor may crossfade from the leap's last
                // frame back to rest, and the hold fades out with it.
                var carried = RawStride(f) - f.AttackRootStart;
                offset.x -= carried.x;
                offset.z -= carried.z;
            }

            f.AttackOffset = offset;
        }

        private void TickAttack(Fighter f)
        {
            var target = f.Target!;
            var to = VisualPosition(target) - VisualPosition(f);
            to.y = 0f;
            if (to.sqrMagnitude > 1e-8f)
            {
                Face(f, to, f.Data.AimYawDegrees);
            }

            // Projectile attacks land through their projectile; only explicit melee contacts land here.
            if (LockedBattleRotation.MeleeHitCrossed(f.Data, f.Profile, f.PreviousPhaseTime, f.PhaseTime, out var hitAttack, out var hitIndex))
            {
                LandHit(f, MeleeContact(f, hitAttack, hitIndex), hitAttack);
            }

            LockedBattleRotation.Locate(f.Data, f.PhaseTime, out var index, out var into);
            var attack = LockedBattleRotation.AttackAt(f.Data, index);
            if (f.Data.AttackCyclesItself && index == 0)
            {
                // The actor loops its own attack + rest. With one attack (the creatures) leave it
                // alone; with more, restart it when the rotation comes back to it. Its extra
                // attacks are plain clips, played once then rest like any other fighter's.
                if (LockedBattleRotation.Count(f.Data) > 1 && index != f.AttackIndex)
                {
                    f.AttackClipOn = false;
                    StartAttackClip(f, index);
                }

                UpdateAttackOffset(f, into < attack.Seconds, into);
                return;
            }

            var on = into < attack.Seconds;
            if (on && (!f.AttackClipOn || index != f.AttackIndex))
            {
                f.AttackClipOn = true;
                StartAttackClip(f, index);
            }
            else if (!on && f.AttackClipOn)
            {
                f.AttackClipOn = false;
                f.Play(f.Data.RestPoseName);
            }

            UpdateAttackOffset(f, on, into);
        }

        private static void Face(Fighter f, Vector3 flatDirection, float aimYawDegrees = 0f)
        {
            // Aim yaw (attack only, Rowan) turns the body so its locked arrow line meets the target.
            var want = Quaternion.LookRotation(flatDirection.normalized, Vector3.up) * Quaternion.Euler(0f, aimYawDegrees, 0f);
            f.Slot.rotation = Quaternion.RotateTowards(f.Slot.rotation, want, 240f * Time.unscaledDeltaTime);
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-8f ? v.normalized : Vector3.forward;
        }

        /// <summary>Forward offset in metres that the walk clip itself has carried the body (stride bone only).</summary>
        private static float StrideForward(Fighter f)
        {
            if (f.StrideBone == null)
            {
                return 0f;
            }

            // The slot is unscaled, so its local units are metres.
            return f.Slot.InverseTransformPoint(f.StrideBone.position).z;
        }

        /// <summary>
        /// How far the body is ahead of the slot right now: the walk clip's own carry, plus the
        /// current attack's reach slide / in-place hold (so the body box moves with the body).
        /// </summary>
        private static float WalkOffset(Fighter f)
        {
            var walk = f.StrideBone != null && f.Phase == Phase.Walk ? StrideForward(f) - f.RestForward : 0f;
            return walk + f.AttackOffset.z;
        }

        private static Vector3 VisualPosition(Fighter f)
        {
            return f.Slot.position + (Flat(f.Slot.forward) * WalkOffset(f));
        }

        /// <summary>World point into the defender's body-box frame (slot metres, walk travel removed).</summary>
        private static Vector3 ToBox(Fighter defender, Vector3 world)
        {
            var p = defender.Slot.InverseTransformPoint(world);
            p.z -= WalkOffset(defender);
            return p;
        }

        private static Vector3 FromBox(Fighter defender, Vector3 local)
        {
            local.z += WalkOffset(defender);
            return defender.Slot.TransformPoint(local);
        }

        /// <summary>
        /// Blocker for flying tracks: the segment the projectile's leading point covered this frame,
        /// tested against the lane target's body box. Stops it at the entry point.
        /// </summary>
        private static bool ProjectileHit(Fighter attacker, Vector3 from, Vector3 to, out Vector3 contact)
        {
            contact = to;
            var target = attacker.Target;
            if (target == null || !target.Built || attacker.Phase != Phase.Attack)
            {
                return false;
            }

            var a = ToBox(target, from);
            var b = ToBox(target, to);
            if (!target.Profile.Box.SegmentEntry(a.x, a.y, a.z, b.x, b.y, b.z, out var t))
            {
                return false;
            }

            contact = FromBox(target, Vector3.Lerp(a, b, t));
            return true;
        }

        /// <summary>
        /// Melee contact point: where the line from the attacker to its striking bone enters the
        /// target's body box, or the closest box point when the bone stops just short of it.
        /// </summary>
        private static Vector3 MeleeContact(Fighter attacker, int attackIndex, int hitIndex = 0)
        {
            var target = attacker.Target!;
            var strike = attacker.StrikeBoneFor(attackIndex, hitIndex);
            var bone = strike != null ? strike.position : attacker.Slot.position + (Vector3.up * 0.5f);
            var origin = attacker.Slot.position;
            origin.y = bone.y;
            var a = ToBox(target, origin);
            var b = ToBox(target, bone);
            var box = target.Profile.Box;
            if (box.SegmentEntry(a.x, a.y, a.z, b.x, b.y, b.z, out var t))
            {
                return FromBox(target, Vector3.Lerp(a, b, t));
            }

            box.ClosestPoint(b.x, b.y, b.z, out var cx, out var cy, out var cz);
            return FromBox(target, new Vector3(cx, cy, cz));
        }

        /// <summary>Spawns the attacker's impact burst at the contact and starts the defender's reaction.</summary>
        private void LandHit(Fighter attacker, Vector3 contact, int attackIndex = 0)
        {
            var target = attacker.Target;
            if (target == null || !target.Built)
            {
                return;
            }

            var travel = target.Slot.position - attacker.Slot.position;
            travel.y = 0f;
            LockedBattleImpact.Spawn(LockedBattleRotation.Impact(attacker.Data, attacker.Profile, attackIndex), contact, travel, attacker.Core, attacker.Glow, attacker.Edge);
            _camera?.Shake(LockedBattleRotation.ShakeMetres(attacker.Data, attacker.Profile, attackIndex));
            var away = target.Slot.InverseTransformDirection(travel);
            away.y = 0f;
            target.HitAway = away.sqrMagnitude > 1e-8f ? away.normalized : Vector3.back;
            target.HitAge = 0f;
            target.RecoilOn = LockedBattleHits.RecoilAllowed(target.Data, target.Phase == Phase.Attack, target.PhaseTime);
        }

        /// <summary>
        /// Knock-back and lean on the actor root (never the rig bones or clips), plus the hit flash
        /// as a brief HDR tint through a property block.
        /// </summary>
        private static void TickHitReaction(Fighter f, float dt)
        {
            f.HitAge += dt;
            var w = f.RecoilOn ? LockedBattleHits.RecoilWeight(f.HitAge) : 0f;
            if (w > 0f)
            {
                var lean = f.Profile.RecoilDegrees * w * Mathf.Deg2Rad;
                f.Host.localPosition = f.BaseLocal + f.AttackOffset + (f.HitAway * (f.Profile.RecoilMetres * w));
                f.Host.localRotation = Quaternion.FromToRotation(Vector3.up, (Vector3.up + (f.HitAway * Mathf.Tan(lean))).normalized);
            }
            else
            {
                if (f.Recoiling)
                {
                    f.Host.localRotation = Quaternion.identity;
                }

                // Attack reach slide / in-place hold (zero outside the attacks that use one).
                f.Host.localPosition = f.BaseLocal + f.AttackOffset;
            }

            f.Recoiling = w > 0f;
            var flash = LockedBattleHits.FlashWeight(f.HitAge);
            if (flash > 0f)
            {
                var tint = new Color(f.Profile.FlashR, f.Profile.FlashG, f.Profile.FlashB, 1f);
                var gain = Color.Lerp(Color.white, tint * 2.4f, flash);
                foreach (var slot in f.FlashSlots)
                {
                    if (slot.Renderer == null)
                    {
                        continue;
                    }

                    var lit = slot.BaseColor * gain;
                    lit.a = slot.BaseColor.a;
                    f.Block.Clear();
                    f.Block.SetColor(BaseColorId, lit);
                    f.Block.SetColor(ColorId, lit);
                    f.Block.SetColor(EmissionColorId, tint * (1.5f * flash));
                    slot.Renderer.SetPropertyBlock(f.Block, slot.Index);
                }

                f.Flashing = true;
            }
            else if (f.Flashing)
            {
                ClearFlash(f);
            }
        }

        /// <summary>Puts each material's own base colour back (emission override dropped).</summary>
        private static void ClearFlash(Fighter f)
        {
            if (!f.Flashing)
            {
                return;
            }

            foreach (var slot in f.FlashSlots)
            {
                if (slot.Renderer == null)
                {
                    continue;
                }

                f.Block.Clear();
                f.Block.SetColor(BaseColorId, slot.BaseColor);
                f.Block.SetColor(ColorId, slot.BaseColor);
                slot.Renderer.SetPropertyBlock(f.Block, slot.Index);
            }

            f.Flashing = false;
        }

        private static void ClearHit(Fighter f)
        {
            f.HitAge = 10f;
            f.RecoilOn = false;
            f.Recoiling = false;
            f.Host.localPosition = f.BaseLocal + f.AttackOffset;
            f.Host.localRotation = Quaternion.identity;
            ClearFlash(f);
        }

        /// <summary>
        /// Trail and line widths do not follow transform scale. The attack driver sizes them for an
        /// unscaled actor, so match them to the body scale once.
        /// </summary>
        private static void MatchEffectWidths(Fighter f)
        {
            if (Mathf.Abs(f.Scale - 1f) < 1e-3f)
            {
                return;
            }

            foreach (var trail in f.Host.GetComponentsInChildren<TrailRenderer>(true))
            {
                if (f.ScaledEffects.Add(trail.GetInstanceID()))
                {
                    trail.widthMultiplier *= f.Scale;
                    trail.minVertexDistance *= f.Scale;
                }
            }

            foreach (var line in f.Host.GetComponentsInChildren<LineRenderer>(true))
            {
                if (f.ScaledEffects.Add(line.GetInstanceID()))
                {
                    line.widthMultiplier *= f.Scale;
                }
            }
        }

        private void Boot()
        {
            if (_booted || !Application.isPlaying)
            {
                return;
            }

            // Multi-scene Editor hierarchy: a prior Blightroot (or any) demo left open alongside
            // BattleLocked would otherwise keep playing in Game view. Strip every stray Blightroot
            // demo actor before we spawn the ten roster fighters. The lane-5 Blightroot this
            // battle spawns under its own root is exempt.
            StripLeftoverDemoActors();
            _booted = true;
            SurvivalVisuals.EnsureEventSystem();
            foreach (var data in LockedBattleRoster.All)
            {
                var fighter = Spawn(data);
                _fighters.Add(fighter);
            }

            foreach (var f in _fighters)
            {
                var target = LockedBattleRoster.TargetOf(f.Data);
                foreach (var other in _fighters)
                {
                    if (string.Equals(other.Data.Name, target.Name, System.StringComparison.Ordinal))
                    {
                        f.Target = other;
                    }
                }
            }

            BuildLights();
            BuildGround();
            _camera = gameObject.GetComponent<LockedBattleCamera>() ?? gameObject.AddComponent<LockedBattleCamera>();
            _camera.SetField(FieldBounds());
            BuildHud();
            Restart();
        }


        /// <summary>
        /// Destroys leftover Survival demo actors from other scenes still loaded in the Editor
        /// (Blightroot in particular: its demo scene is often left open next to BattleLocked).
        /// Lane 5 exception: a BlightrootActor under this battle's own root is the roster
        /// Blightroot and is kept. Roster fighters spawn under this transform after this runs.
        /// </summary>
        private void StripLeftoverDemoActors()
        {
            foreach (var actor in Object.FindObjectsByType<BlightrootActor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (actor != null && !IsOwnLaneFighter(actor.transform))
                {
                    Object.Destroy(actor.gameObject);
                }
            }

            foreach (var demo in Object.FindObjectsByType<BlightrootDemo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (demo != null)
                {
                    Object.Destroy(demo.gameObject);
                }
            }
        }

        /// <summary>True for an actor this battle spawned itself (the lane-5 Blightroot).</summary>
        private bool IsOwnLaneFighter(Transform actor)
        {
            return actor.IsChildOf(transform);
        }

        /// <summary>
        /// Bounds of the visible body renderers in the host frame. Only used for the two lane-5
        /// actors, which have no VisibleBounds of their own. The host sits at the origin,
        /// unrotated and unscaled, when this runs.
        /// </summary>
        private static Bounds MeasureVisibleBounds(Transform host)
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

        private Fighter Spawn(LockedBattleFighter data)
        {
            // Slot carries the battle position and facing. The actor host under it carries the
            // body scale and the hit flinch, so neither touches the rig bones or the clips.
            var slot = new GameObject(data.Name + "Slot");
            slot.transform.SetParent(transform, false);
            slot.transform.localPosition = Vector3.zero;
            slot.transform.localRotation = Quaternion.identity;
            slot.transform.localScale = Vector3.one;
            var host = new GameObject(data.Name + "Actor");
            host.transform.SetParent(slot.transform, false);
            host.transform.localPosition = Vector3.zero;
            host.transform.localRotation = Quaternion.identity;
            host.transform.localScale = Vector3.one;
            var f = new Fighter(data, slot.transform, host.transform, LockedBattleHits.ProfileOf(data.Name));
            BlenderRigAttackDriver? driver = null;
            switch (data.Name)
            {
                case "Emberfang":
                {
                    var actor = host.AddComponent<EmberfangActor>();
                    actor.Build();
                    f.Bind(actor.PlayPose, () => actor.Pose, actor.Built, actor.VisibleBounds);
                    driver = actor.AttackDriver;
                    break;
                }

                case "Stormcrest":
                {
                    var actor = host.AddComponent<StormcrestActor>();
                    actor.Build();
                    f.Bind(actor.PlayPose, () => actor.Pose, actor.Built, actor.VisibleBounds);
                    driver = actor.AttackDriver;
                    break;
                }

                case "Ironhowl":
                {
                    var actor = host.AddComponent<IronhowlActor>();
                    actor.Build();
                    f.Bind(actor.PlayPose, () => actor.Pose, actor.Built, actor.VisibleBounds);
                    break;
                }

                case "Nightfang":
                {
                    var actor = host.AddComponent<NightfangActor>();
                    actor.Build();
                    f.Bind(actor.PlayPose, () => actor.Pose, actor.Built, actor.VisibleBounds);
                    driver = actor.AttackDriver;
                    break;
                }

                case "Lyra":
                {
                    var actor = host.AddComponent<LyraActor>();
                    actor.Build();
                    f.Bind(actor.PlayPose, () => actor.Pose, actor.Built, actor.VisibleBounds);
                    driver = actor.AttackDriver;
                    break;
                }

                case "Oakenshield":
                {
                    var actor = host.AddComponent<OakenshieldActor>();
                    actor.Build();
                    f.Bind(actor.PlayPose, () => actor.Pose, actor.Built, actor.VisibleBounds);
                    driver = actor.AttackDriver;
                    break;
                }

                case "Vespera":
                {
                    var actor = host.AddComponent<VesperaActor>();
                    actor.Build();
                    f.Bind(actor.PlayPose, () => actor.Pose, actor.Built, actor.VisibleBounds);
                    driver = actor.AttackDriver;
                    break;
                }

                case "Ashwyrm":
                {
                    var actor = host.AddComponent<AshwyrmActor>();
                    actor.Build();
                    f.Bind(actor.PlayPose, () => actor.Pose, actor.Built, actor.VisibleBounds);
                    driver = actor.AttackDriver;
                    break;
                }

                case "Sir Aldric":
                {
                    // The pilot actor tunes the global ambient and reflections for its own
                    // scene; keep the battle's.
                    var reflections = RenderSettings.reflectionIntensity;
                    var ambientIntensity = RenderSettings.ambientIntensity;
                    var actor = host.AddComponent<SirAldricMeshyAnimateActor>();
                    actor.Build();
                    RenderSettings.reflectionIntensity = reflections;
                    RenderSettings.ambientIntensity = ambientIntensity;
                    var measured = new Bounds(Vector3.zero, Vector3.zero);
                    if (actor.Built)
                    {
                        // Size him on the upright walk body (the idle is a crouched stance).
                        actor.PlayBattlePose(SirAldricBattlePoses.Walk);
                        measured = MeasureVisibleBounds(host.transform);
                        actor.PlayBattlePose(SirAldricBattlePoses.Rest);
                    }

                    f.Bind(actor.PlayBattlePose, () => actor.BattlePose, actor.Built && measured.size.y > 1e-3f, measured);
                    break;
                }

                case "Blightroot":
                {
                    // Own pivot: the actor's stand-up turn and foot plant write its transform,
                    // and the host's transform belongs to the body scale and the hit flinch.
                    var pivot = new GameObject("BlightrootPivot");
                    pivot.transform.SetParent(host.transform, false);
                    var actor = pivot.AddComponent<BlightrootActor>();
                    actor.Build();
                    var measured = new Bounds(Vector3.zero, Vector3.zero);
                    if (actor.Built && actor.PlayBattlePose(BlightrootBattlePoses.Rest))
                    {
                        actor.FaceWalkForward();
                        measured = MeasureVisibleBounds(host.transform);
                    }

                    f.Bind(actor.PlayBattlePose, () => actor.BattlePose, actor.Built && measured.size.y > 1e-3f, measured);
                    break;
                }

                case "Rowan":
                {
                    var actor = host.AddComponent<RowanActor>();
                    actor.Build();
                    f.Bind(actor.PlayPose, () => actor.Pose, actor.Built, actor.VisibleBounds);
                    driver = actor.AttackDriver;
                    break;
                }

                case "Bonequill":
                {
                    var actor = host.AddComponent<BonequillActor>();
                    actor.Build();
                    f.Bind(actor.PlayPose, () => actor.Pose, actor.Built, actor.VisibleBounds);
                    driver = actor.AttackDriver;
                    break;
                }

                default:
                    Debug.LogError("Locked battle has no actor for " + data.Name);
                    break;
            }

            if (!f.Built)
            {
                Debug.LogError("Locked battle: " + data.Name + " did not build and sits out.");
                return f;
            }

            // Bounds were measured with the actor at the origin, unrotated and unscaled.
            var bounds = f.LocalBounds;
            var height = bounds.size.y;
            f.Scale = height > 1e-4f ? data.DesignHeightMetres / height : 1f;
            host.transform.localScale = new Vector3(f.Scale, f.Scale, f.Scale);
            f.BaseLocal = new Vector3(0f, -bounds.min.y * f.Scale, 0f);
            host.transform.localPosition = f.BaseLocal;
            LockedBattleRoster.StartPoint(data, out var x, out var z);
            f.StartPosition = new Vector3(x, 0f, z);
            f.StartRotation = Quaternion.Euler(0f, LockedBattleRoster.StartYawDegrees(data), 0f);
            slot.transform.SetPositionAndRotation(f.StartPosition, f.StartRotation);
            LockedBattleGround.AddContactShadow(slot.transform, bounds.size.x * f.Scale, bounds.size.z * f.Scale);
            var flashSlots = new List<FlashSlot>();
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
                    var baseColor = Color.white;
                    if (m != null && m.HasProperty(BaseColorId))
                    {
                        baseColor = m.GetColor(BaseColorId);
                    }
                    else if (m != null && m.HasProperty(ColorId))
                    {
                        baseColor = m.GetColor(ColorId);
                    }

                    flashSlots.Add(new FlashSlot(r, i, baseColor));
                }
            }

            f.FlashSlots = flashSlots.ToArray();
            ImpactColors(data.Name, out var core, out var glow, out var edge);
            f.Core = core;
            f.Glow = glow;
            f.Edge = edge;
            if (f.Profile.StrikeBone.Length > 0)
            {
                f.StrikeBone = FindBone(host.transform, f.Profile.StrikeBone);
                if (f.StrikeBone == null)
                {
                    Debug.LogWarning("Locked battle: " + data.Name + " strike bone missing. " + f.Profile.StrikeBone);
                }
            }

            var attackCount = LockedBattleRotation.Count(data);
            f.AttackStrikeBones = new Transform?[attackCount];
            f.AttackHitBones = new Transform?[attackCount][];
            for (var a = 0; a < attackCount; a++)
            {
                var attackData = LockedBattleRotation.AttackAt(data, a);
                var boneName = attackData.StrikeBone;
                if (!string.IsNullOrEmpty(boneName))
                {
                    f.AttackStrikeBones[a] = FindBone(host.transform, boneName);
                    if (f.AttackStrikeBones[a] == null)
                    {
                        Debug.LogWarning("Locked battle: " + data.Name + " attack " + (a + 1) + " strike bone missing. " + boneName);
                    }
                }

                // Combo hits that name their own bone (a left-claw hit inside a right-claw combo).
                f.AttackHitBones[a] = new Transform?[attackData.ExtraHits.Length];
                for (var h = 0; h < attackData.ExtraHits.Length; h++)
                {
                    var hitBone = attackData.ExtraHits[h].StrikeBone;
                    if (string.IsNullOrEmpty(hitBone))
                    {
                        continue;
                    }

                    f.AttackHitBones[a][h] = FindBone(host.transform, hitBone);
                    if (f.AttackHitBones[a][h] == null)
                    {
                        Debug.LogWarning("Locked battle: " + data.Name + " attack " + (a + 1) + " hit " + (h + 2) + " strike bone missing. " + hitBone);
                    }
                }
            }

            if (driver != null && f.Profile.Projectile)
            {
                var captured = f;
                bool Block(AttackTrack track, Vector3 from, Vector3 to, out Vector3 contact) =>
                    ProjectileHit(captured, from, to, out contact);
                driver.Blocker = Block;
                driver.Impact = (track, contact) => LandHit(captured, contact);
                driver.LaunchFrame = () => captured.Slot.localToWorldMatrix
                    * Matrix4x4.TRS(captured.BaseLocal, Quaternion.identity, Vector3.one * captured.Scale);
            }
            if (data.WalkCarriesRoot)
            {
                f.StrideBone = FindBone(host.transform, data.StrideBone);
                if (f.StrideBone == null)
                {
                    Debug.LogWarning("Locked battle: " + data.Name + " stride bone missing. " + data.StrideBone);
                }
            }

            Debug.Log(
                "Locked battle " + data.Name + " (" + data.Side + "): imported height " + height.ToString("0.###") +
                " units, scaled x" + f.Scale.ToString("0.####") + " to " + data.DesignHeightMetres.ToString("0.###") +
                " m. Walk '" + data.WalkPoseName + "', attack '" + data.AttackPoseName + "' at " +
                data.AttackRangeMetres.ToString("0.0") + " m, rotation of " + attackCount + " attack(s).");
            return f;
        }

        /// <summary>Impact colours: the looksheet colours of each attack's flying or slash track, Ironhowl warm steel sparks.</summary>
        private static void ImpactColors(string name, out Color core, out Color glow, out Color edge)
        {
            switch (name)
            {
                case "Sir Aldric":
                {
                    // Steel white core, bright blue glow, Sir Aldric's #1b3060 blue at the edge.
                    var blue = LockedBattleHits.SirAldricBlueRgb;
                    core = new Color(0.92f, 0.96f, 1f, 1f);
                    glow = new Color(0.40f, 0.58f, 1f, 0.85f);
                    edge = new Color(blue[0], blue[1], blue[2], 0.6f);
                    return;
                }

                case "Blightroot":
                    core = new Color(0.90f, 0.70f, 1f, 1f);
                    glow = new Color(0.58f, 0.22f, 0.86f, 0.85f);
                    edge = new Color(0.26f, 0.08f, 0.38f, 0.55f);
                    return;
            }

            BlenderRigAttackSpec? spec = null;
            switch (name)
            {
                case "Emberfang":
                    spec = EmberfangAttack.Spec;
                    break;
                case "Stormcrest":
                    spec = StormcrestAttack.Spec;
                    break;
                case "Nightfang":
                    spec = NightfangAttack.Spec;
                    break;
                case "Lyra":
                    spec = LyraAttack.Spec;
                    break;
                case "Oakenshield":
                    spec = OakenshieldAttack.Spec;
                    break;
                case "Vespera":
                    spec = VesperaAttack.Spec;
                    break;
                case "Ashwyrm":
                    spec = AshwyrmAttack.Spec;
                    break;
                case "Rowan":
                case "Bonequill":
                {
                    // Archers: the arrow mesh track carries no colours, so take the arrow trail's.
                    var archer = name == "Rowan" ? RowanAttack.Spec : BonequillAttack.Spec;
                    foreach (var trail in archer.Tracks)
                    {
                        if (trail.Kind == AttackTrackKind.Projectile && trail.CoreRgb != null && trail.CoreRgb.Length >= 3)
                        {
                            core = Rgb(trail.CoreRgb, 1f, Color.white);
                            glow = Rgb(trail.GlowRgb, 0.85f, Color.white);
                            edge = Rgb(trail.EdgeRgb, 0.5f, Color.white);
                            return;
                        }
                    }

                    spec = archer;
                    break;
                }
            }

            core = new Color(1f, 0.95f, 0.80f, 1f);
            glow = new Color(1f, 0.62f, 0.25f, 0.85f);
            edge = new Color(0.55f, 0.30f, 0.12f, 0.5f);
            if (spec == null)
            {
                return;
            }

            AttackTrack? pick = null;
            foreach (var track in spec.Tracks)
            {
                if (track.Kind == AttackTrackKind.Projectile || track.Shape == AttackVfxShape.Slash)
                {
                    pick = track;
                    break;
                }
            }

            if (pick == null && spec.Tracks.Length > 0)
            {
                pick = spec.Tracks[0];
            }

            if (pick == null)
            {
                return;
            }

            core = Rgb(pick.CoreRgb, 1f, core);
            glow = Rgb(pick.GlowRgb, 0.85f, glow);
            edge = Rgb(pick.EdgeRgb, 0.5f, edge);
        }

        private static Color Rgb(float[] rgb, float alpha, Color fallback)
        {
            if (rgb == null || rgb.Length < 3)
            {
                return fallback;
            }

            return new Color(rgb[0], rgb[1], rgb[2], alpha);
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

        private Bounds FieldBounds()
        {
            var seeded = false;
            var field = new Bounds(new Vector3(0f, 1f, 0f), new Vector3(6f, 2f, 8f));
            foreach (var f in _fighters)
            {
                if (!f.Built)
                {
                    continue;
                }

                var b = f.LocalBounds;
                for (var i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        (i & 1) == 0 ? b.min.x : b.max.x,
                        (i & 2) == 0 ? b.min.y : b.max.y,
                        (i & 4) == 0 ? b.min.z : b.max.z);
                    var world = f.Host.TransformPoint(corner);
                    if (!seeded)
                    {
                        field = new Bounds(world, Vector3.zero);
                        seeded = true;
                    }
                    else
                    {
                        field.Encapsulate(world);
                    }
                }
            }

            field.Expand(new Vector3(0.3f, 0.2f, 0.3f));
            return field;
        }

        /// <summary>
        /// Each actor adds its own key and fill light. Four sets would quadruple the light, so
        /// keep one set with the Blender rig demo values.
        /// </summary>
        private void BuildLights()
        {
            foreach (var f in _fighters)
            {
                DestroyNamed(f.Data.Name + "KeyLight");
                DestroyNamed(f.Data.Name + "FillLight");
            }

            // Sir Aldric's pilot actor names its lights without "Sir ".
            DestroyNamed("AldricKeyLight");
            DestroyNamed("AldricFillLight");

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.36f, 0.36f, 0.38f, 1f);
            var key = new GameObject("LockedBattleKeyLight");
            var light = key.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.96f, 0.88f, 1f);
            light.shadows = LightShadows.Soft;
            key.transform.rotation = Quaternion.Euler(40f, 160f, 0f);
            var fillGo = new GameObject("LockedBattleFillLight");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.45f;
            fill.color = new Color(0.68f, 0.74f, 0.88f, 1f);
            fillGo.transform.rotation = Quaternion.Euler(18f, -24f, 0f);
        }

        private static void DestroyNamed(string name)
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                go.SetActive(false);
                Destroy(go);
            }
        }

        /// <summary>World-map grass with a dirt track per lane and a hazy horizon (<see cref="LockedBattleGround"/>).</summary>
        private void BuildGround()
        {
            LockedBattleGround.Build(transform);
        }

        private void BuildHud()
        {
            var canvas = SurvivalVisuals.Canvas(transform, "LockedBattleHud", 80);
            var title = SurvivalVisuals.Text(canvas, "Caption", LockedBattleRoster.Title, 30, TextAnchor.MiddleCenter, SurvivalVisuals.Cream);
            Anchor(title.rectTransform, 0.06f, 0.952f, 0.94f, 0.992f);

            var teams = SurvivalVisuals.Text(canvas, "Teams", TeamsLine(), 22, TextAnchor.MiddleCenter, SurvivalVisuals.Gold);
            Anchor(teams.rectTransform, 0.04f, 0.918f, 0.96f, 0.950f);

            var restart = SurvivalVisuals.Button(canvas, RestartButtonObjectName, new Color(0.16f, 0.14f, 0.10f, 0.94f));
            Anchor(restart.GetComponent<RectTransform>(), 0.68f, 0.840f, 0.96f, 0.910f);
            var label = SurvivalVisuals.Text(restart.transform, "Label", "RESTART", 26, TextAnchor.MiddleCenter, SurvivalVisuals.Gold);
            SurvivalVisuals.Stretch(label.rectTransform);
            restart.onClick.AddListener(Restart);
        }

        private static void Anchor(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static string TeamsLine()
        {
            var heroes = new List<string>();
            var villains = new List<string>();
            foreach (var f in LockedBattleRoster.All)
            {
                (f.Side == BattleSide.Hero ? heroes : villains).Add(f.Name);
            }

            return "HEROES " + string.Join(" · ", heroes) + "   vs   VILLAINS " + string.Join(" · ", villains);
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        /// <summary>One body material slot and its own base colour, for the hit flash.</summary>
        private readonly struct FlashSlot
        {
            public FlashSlot(Renderer renderer, int index, Color baseColor)
            {
                Renderer = renderer;
                Index = index;
                BaseColor = baseColor;
            }

            public Renderer Renderer { get; }
            public int Index { get; }
            public Color BaseColor { get; }
        }

        private sealed class Fighter
        {
            private System.Func<string, bool> _play = _ => false;

            public Fighter(LockedBattleFighter data, Transform slot, Transform host, LockedBattleHitProfile profile)
            {
                Data = data;
                Slot = slot;
                Host = host;
                Profile = profile;
                StartRotation = Quaternion.identity;
            }

            public LockedBattleFighter Data { get; }
            public LockedBattleHitProfile Profile { get; }

            /// <summary>Battle position and facing. Unscaled, so local units are metres.</summary>
            public Transform Slot { get; }

            /// <summary>Actor root: body scale, plus the hit flinch offset on top of <see cref="BaseLocal"/>.</summary>
            public Transform Host { get; }
            public Vector3 BaseLocal { get; set; }
            public Transform? StrikeBone { get; set; }
            public FlashSlot[] FlashSlots { get; set; } = System.Array.Empty<FlashSlot>();
            public MaterialPropertyBlock Block { get; } = new MaterialPropertyBlock();
            public Color Core { get; set; } = Color.white;
            public Color Glow { get; set; } = Color.white;
            public Color Edge { get; set; } = Color.white;
            public float PreviousPhaseTime { get; set; }
            public float HitAge { get; set; } = 10f;
            public Vector3 HitAway { get; set; } = Vector3.back;
            public bool RecoilOn { get; set; }
            public bool Recoiling { get; set; }
            public bool Flashing { get; set; }
            public bool Built { get; private set; }
            public Bounds LocalBounds { get; private set; }
            public System.Func<string> Pose { get; private set; } = () => "rest";
            public float Scale { get; set; } = 1f;
            public Vector3 StartPosition { get; set; }
            public Quaternion StartRotation { get; set; }
            public Transform? StrideBone { get; set; }
            public Fighter? Target { get; set; }
            public Phase Phase { get; set; }
            public float PhaseTime { get; set; }
            public bool AttackClipOn { get; set; }

            /// <summary>Attack of the rotation playing now (0 = the locked attack 1).</summary>
            public int AttackIndex { get; set; }

            /// <summary>Stride bone forward offset when the current attack clip started (in-place hold).</summary>
            public Vector3 AttackRootStart { get; set; }

            /// <summary>Host offset (slot metres) from the current attack's reach slide and in-place hold.</summary>
            public Vector3 AttackOffset { get; set; }

            /// <summary>Per-attack strike bones; null entries fall back to <see cref="StrikeBone"/>.</summary>
            public Transform?[] AttackStrikeBones { get; set; } = System.Array.Empty<Transform?>();

            public Transform?[][] AttackHitBones { get; set; } = System.Array.Empty<Transform?[]>();

            public Transform? StrikeBoneFor(int attackIndex, int hitIndex)
            {
                if (hitIndex > 0 && attackIndex >= 0 && attackIndex < AttackHitBones.Length)
                {
                    var hits = AttackHitBones[attackIndex];
                    if (hitIndex - 1 < hits.Length && hits[hitIndex - 1] != null)
                    {
                        return hits[hitIndex - 1];
                    }
                }

                return StrikeBoneFor(attackIndex);
            }

            public Transform? StrikeBoneFor(int attackIndex)
            {
                if (attackIndex >= 0 && attackIndex < AttackStrikeBones.Length && AttackStrikeBones[attackIndex] != null)
                {
                    return AttackStrikeBones[attackIndex];
                }

                return StrikeBone;
            }
            public float PreviousForward { get; set; }
            public float RestForward { get; set; }
            public HashSet<int> ScaledEffects { get; } = new HashSet<int>();

            public void Bind(System.Func<string, bool> play, System.Func<string> pose, bool built, Bounds bounds)
            {
                _play = play;
                Pose = pose;
                Built = built;
                LocalBounds = bounds;
            }

            public bool Play(string poseName)
            {
                return Built && _play(poseName);
            }
        }
    }
}
