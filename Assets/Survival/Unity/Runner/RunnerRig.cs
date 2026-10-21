#nullable enable
using System.Collections.Generic;
using Survival.Domain.Enemies;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;
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
        private bool _flashing;
        private float _deathAge = -1f;
        private Vector3 _baseLocal;
        private float _punch;

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
            switch (data.Name)
            {
                case "Emberfang":
                {
                    var a = host.AddComponent<EmberfangActor>();
                    a.Build();
                    rig.Bind(a.PlayPose, () => a.Pose);
                    built = a.Built;
                    measured = a.VisibleBounds;
                    break;
                }

                case "Stormcrest":
                {
                    var a = host.AddComponent<StormcrestActor>();
                    a.Build();
                    rig.Bind(a.PlayPose, () => a.Pose);
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
                    built = a.Built;
                    measured = a.VisibleBounds;
                    break;
                }

                case "Lyra":
                {
                    var a = host.AddComponent<LyraActor>();
                    a.Build();
                    rig.Bind(a.PlayPose, () => a.Pose);
                    built = a.Built;
                    measured = a.VisibleBounds;
                    break;
                }

                case "Oakenshield":
                {
                    var a = host.AddComponent<OakenshieldActor>();
                    a.Build();
                    rig.Bind(a.PlayPose, () => a.Pose);
                    built = a.Built;
                    measured = a.VisibleBounds;
                    break;
                }

                case "Vespera":
                {
                    var a = host.AddComponent<VesperaActor>();
                    a.Build();
                    rig.Bind(a.PlayPose, () => a.Pose);
                    built = a.Built;
                    measured = a.VisibleBounds;
                    break;
                }

                case "Ashwyrm":
                {
                    var a = host.AddComponent<AshwyrmActor>();
                    a.Build();
                    rig.Bind(a.PlayPose, () => a.Pose);
                    built = a.Built;
                    measured = a.VisibleBounds;
                    break;
                }

                case "Rowan":
                {
                    var a = host.AddComponent<RowanActor>();
                    a.Build();
                    rig.Bind(a.PlayPose, () => a.Pose);
                    built = a.Built;
                    measured = a.VisibleBounds;
                    break;
                }

                case "Bonequill":
                {
                    var a = host.AddComponent<BonequillActor>();
                    a.Build();
                    rig.Bind(a.PlayPose, () => a.Pose);
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

                    rig.Bind(a.PlayBattlePose, () => a.BattlePose);
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
            rig._baseLocal = new Vector3(0f, -measured.min.y * scale, 0f);
            host.transform.localPosition = rig._baseLocal;
            rig.Height = data.DesignHeightMetres * LockedBattleRoster.BattleScaleOf(data) * extraScale;
            rig._ground = new LockedBattleGrounding(host.transform);
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
            rig.Play(rig.RestPose);
            return rig;
        }

        private void Bind(System.Func<string, bool> play, System.Func<string> pose)
        {
            _play = play;
            _pose = pose;
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

            if (Pose != AttackPose)
            {
                Play(AttackPose);
            }
            else if (!Data.AttackCyclesItself)
            {
                Play(RestPose);
                Play(AttackPose);
            }
        }

        /// <summary>Face a world direction on the ground (yaw only), plus an aim offset (Rowan's bow).</summary>
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

        /// <summary>Called from the scene's LateUpdate after the clips posed the bones.</summary>
        public void Late(float dt)
        {
            _hitAge += dt;
            var w = _reaction.Weight(_hitAge);
            var local = Slot.InverseTransformDirection(_away);
            // The battle's root reaction: step back and lean, sized by the defender's recoil profile.
            var punch = 0.6f + (0.4f * _punch);
            var step = local * (_reaction.RootScale * w * punch);
            var tilt = local * (_reaction.RootLeanScale * w * punch);
            Host.localPosition = _baseLocal + (step * Profile.RecoilMetres);
            var lean = Mathf.Tan(Profile.RecoilDegrees * Mathf.Deg2Rad);
            Host.localRotation = Quaternion.FromToRotation(Vector3.up, (Vector3.up + (tilt * lean)).normalized);
            if (_deathAge >= 0f)
            {
                _deathAge += dt;
                var t = Mathf.Clamp01(_deathAge / 0.9f);
                var fall = Vector3.Cross(Vector3.up, local).normalized;
                Host.localRotation = Quaternion.AngleAxis(-80f * Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t * 1.6f)), fall == Vector3.zero ? Vector3.right : fall);
                Host.localPosition = _baseLocal + (Vector3.down * (Height * 0.9f * t * t));
            }
            else
            {
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
            if (Slot != null)
            {
                Object.Destroy(Slot.gameObject);
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
