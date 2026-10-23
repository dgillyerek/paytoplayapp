using System.Collections.Generic;
using Survival.Domain.Roster;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Survival.Unity
{
    /// <summary>
    /// Drives the Design Theme A attack add-on props and effects next to a Blender rig.
    /// Held props ride their attach bone with the attack_meta offset. Projectiles spawn at the
    /// release frame at the Design spawn point and fly character-forward (+Z, top of the screen
    /// in the rear camera) at the Design speed, then despawn at the end of the clip. A slash
    /// leaves a muzzle at the Design mouth point and shoots along the actor root forward
    /// (the same axis as the lunge), not along a head or jaw bone axis. Design VFX
    /// meshes are look reference only, so effects are procedural URP particles, trails, and lines
    /// in the looksheet colours. Design space is mapped onto the imported rig from rest bone
    /// heads, so FBX unit scale and axis import settings do not matter.
    /// </summary>
    public sealed class BlenderRigAttackDriver
    {
        private readonly BlenderRigAttackSpec _spec;
        private readonly string _themePackDir;
        private readonly Transform _actor;
        private readonly List<TrackView> _views = new List<TrackView>();
        private readonly List<TrackView> _idleViews = new List<TrackView>();
        private readonly Dictionary<string, Transform> _bones = new Dictionary<string, Transform>(System.StringComparer.Ordinal);
        private Material? _additive;
        private Texture2D? _dot;
        private float _scale = 1f;
        private Quaternion _rotation = Quaternion.identity;
        private Vector3 _offset;
        private bool _calibrated;
        private bool _built;
        private int _cycle = -1;
        private int _lightningStep = -1;

        /// <summary>
        /// Optional hit test for flying tracks. Gets the track and the world segment its leading
        /// point covered this frame. Return true with a world contact point to stop it there.
        /// </summary>
        public delegate bool ProjectileBlock(AttackTrack track, Vector3 from, Vector3 to, out Vector3 contact);

        /// <summary>Null by default, so the single-character demos fly the full Design path.</summary>
        public ProjectileBlock? Blocker { get; set; }

        /// <summary>Raised once per attack cycle when <see cref="Blocker"/> stops a flying track.</summary>
        public System.Action<AttackTrack, Vector3>? Impact { get; set; }

        /// <summary>
        /// Actor local-to-world without any hit flinch, sampled once per shot at release. Null: the live actor transform.
        /// </summary>
        public System.Func<Matrix4x4>? LaunchFrame { get; set; }

        public BlenderRigAttackDriver(BlenderRigAttackSpec spec, string themePackDir, Transform actor)
        {
            _spec = spec;
            _themePackDir = themePackDir;
            _actor = actor;
        }

        /// <summary>Unity units per Design metre on this import.</summary>
        public float Scale => _scale;

        public bool Calibrated => _calibrated;

        /// <summary>Call right after the rig is instantiated, before any clip evaluates (bind = Design rest).</summary>
        public void Calibrate(GameObject rig)
        {
            _bones.Clear();
            foreach (var t in rig.GetComponentsInChildren<Transform>(true))
            {
                if (!_bones.ContainsKey(t.name))
                {
                    _bones.Add(t.name, t);
                }
            }

            var design = new List<Vector3>();
            var unity = new List<Vector3>();
            var names = _spec.CalibrationBones;
            for (var i = 0; i < names.Length; i++)
            {
                if (!_bones.TryGetValue(names[i], out var bone))
                {
                    Debug.LogWarning(_spec.Character + " attack calibration bone missing. " + names[i]);
                    continue;
                }

                design.Add(new Vector3(
                    _spec.CalibrationRest[(i * 3) + 0],
                    _spec.CalibrationRest[(i * 3) + 1],
                    _spec.CalibrationRest[(i * 3) + 2]));
                unity.Add(_actor.InverseTransformPoint(bone.position));
            }

            if (design.Count < 3)
            {
                Debug.LogError(_spec.Character + " attack calibration needs 3 bones. Props use Design metres as-is.");
                _scale = 1f;
                _rotation = Quaternion.identity;
                _offset = Vector3.zero;
                _calibrated = false;
                return;
            }

            var cd = Vector3.zero;
            var cu = Vector3.zero;
            for (var i = 0; i < design.Count; i++)
            {
                cd += design[i];
                cu += unity[i];
            }

            cd /= design.Count;
            cu /= design.Count;
            var sd = 0f;
            var su = 0f;
            for (var i = 0; i < design.Count; i++)
            {
                sd += (design[i] - cd).sqrMagnitude;
                su += (unity[i] - cu).sqrMagnitude;
            }

            _scale = sd > 1e-10f ? Mathf.Sqrt(su / sd) : 1f;
            var lateralD = design.Count >= 4 ? design[3] - design[2] : design[2] - design[0];
            var lateralU = unity.Count >= 4 ? unity[3] - unity[2] : unity[2] - unity[0];
            var qd = Frame(design[1] - design[0], lateralD);
            var qu = Frame(unity[1] - unity[0], lateralU);
            _rotation = qu * Quaternion.Inverse(qd);
            _offset = cu - (_scale * (_rotation * cd));
            _calibrated = true;
            var worst = 0f;
            for (var i = 0; i < design.Count; i++)
            {
                worst = Mathf.Max(worst, (MapLocal(design[i]) - unity[i]).magnitude);
            }

            Debug.Log(
                _spec.Character + " attack calibration: " + _scale.ToString("0.####") + " units per Design metre, rotation " +
                _rotation.eulerAngles.ToString("F1") + ", worst rest bone residual " +
                (worst / Mathf.Max(_scale, 1e-6f) * 100f).ToString("0.0") + " cm.");
        }

        /// <summary>Builds props and effects on first use, then binds held props at their ref frames.</summary>
        public void Activate(System.Action<float> sampleFrame)
        {
            if (!_built)
            {
                Build();
            }

            foreach (var view in _views)
            {
                if (view.Track.Kind != AttackTrackKind.Held || view.Bone == null)
                {
                    continue;
                }

                sampleFrame(view.Track.RefFrame);
                var key = view.Track.Frames[Mathf.Clamp(view.Track.RefFrame, 0, view.Track.Frames.Length - 1)];
                var worldPos = WorldPoint(key);
                var worldRot = WorldRotation(key);
                view.BoneLocalPos = view.Bone.InverseTransformPoint(worldPos);
                view.BoneLocalRot = Quaternion.Inverse(view.Bone.rotation) * worldRot;
                view.BoneBound = true;
            }

            sampleFrame(BlenderRigAttackSpec.FirstFrame);
            _cycle = -1;
            _impactCycle = -1;
            foreach (var view in _views)
            {
                view.Root.SetActive(true);
                SetShown(view, false, true);
            }
        }

        /// <summary>Call after the rig evaluated <paramref name="frame"/>.</summary>
        public void Tick(float frame, bool inClip, int cycle)
        {
            ApplyGripRoll(true, frame);
            SetStringMiddle(null);
            if (!_built)
            {
                return;
            }

            var newCycle = cycle != _cycle;
            _cycle = cycle;
            var step = Mathf.FloorToInt(frame / 2f);
            var reroll = step != _lightningStep;
            _lightningStep = step;
            foreach (var view in _views)
            {
                var track = view.Track;
                if (newCycle)
                {
                    ResetBlock(view);
                }

                if (view.Stopped)
                {
                    // A stopped projectile mesh rests on the body at contact for a beat, then goes.
                    if (view.Shown && view.Mesh != null && Time.time - view.StopTime > StuckSeconds)
                    {
                        SetShown(view, false, false);
                    }

                    continue;
                }

                bool visible;
                if (track.Kind == AttackTrackKind.Held)
                {
                    visible = track.VisibleAt(frame);
                    if (visible && _grip != null && frame >= _grip.ReleaseFrame
                        && string.Equals(track.ObjectName, _grip.ArrowTrack, System.StringComparison.Ordinal))
                    {
                        // The nocked arrow leaves the string at release; the flying projectile track takes over.
                        visible = false;
                    }
                }
                else
                {
                    visible = inClip && frame < _spec.ClipLastFrame && track.VisibleAt(frame);
                }

                if (newCycle && view.Shown && track.Kind != AttackTrackKind.Held)
                {
                    SetShown(view, false, true);
                }

                if (!visible)
                {
                    if (track.Kind == AttackTrackKind.Projectile && view.Shown && Blocker != null
                        && _impactCycle != _cycle && !view.Warned)
                    {
                        view.Warned = true;
                        Debug.LogWarning(_spec.Character + " " + track.ObjectName + " ended its flight with no contact at " +
                                         view.Root.transform.position.ToString("F2") + ".");
                    }

                    if (view.Shown)
                    {
                        SetShown(view, false, false);
                    }

                    continue;
                }

                var key = track.Sample(frame);
                Place(view, key);
                if (_grip != null && string.Equals(track.ObjectName, _grip.ArrowTrack, System.StringComparison.Ordinal))
                {
                    AimNockedArrow(view, frame);
                }

                if (!view.Shown)
                {
                    SetShown(view, true, true);
                }

                if (track.Kind == AttackTrackKind.Projectile && Blocker != null && TryBlock(view))
                {
                    continue;
                }

                if (track.Shape == AttackVfxShape.Lightning && reroll)
                {
                    RollLightning(view);
                }

                if (track.Shape == AttackVfxShape.Slash)
                {
                    SweepSlash(view, frame);
                }
            }
        }

        /// <summary>Hides every prop and effect (another pose was picked).</summary>
        public void Deactivate()
        {
            ApplyGripRoll(false, 0f);
            SetStringMiddle(null);
            foreach (var view in _views)
            {
                ResetBlock(view);
                SetShown(view, false, true);
                view.Root.SetActive(false);
            }

            _cycle = -1;
            _impactCycle = -1;
        }

        /// <summary>
        /// Extra archer clip (attacks 2 and 3, Mixamo): the idle bow stays in the locked LeftHand grip and only rolls in it per
        /// shot; the attack's held arrow rides the draw hand (its RightHand binding from the attack) from <see cref="BlenderBowShot.ArrowInHand"/>
        /// to the release, aimed from its nock through the arrow rest beside the riser; while nocked a drawn string (if the bow has one) meets the nock.
        /// Call after the rig evaluated <paramref name="frame"/>.
        /// </summary>
        public void TickBowShots(BlenderBowShots shots, float frame)
        {
            if (_gripRoot != null && _grip != null)
            {
                var q = _grip.UnityRotation(_grip.RollDeg(shots, frame));
                _gripRoot.localRotation = new Quaternion(q[0], q[1], q[2], q[3]);
            }

            var view = BowShotArrow(shots);
            if (view == null || _gripRoot == null || view.Bone == null || !view.BoneBound || !shots.ArrowHeld(frame))
            {
                if (view != null && view.Shown)
                {
                    SetShown(view, false, true);
                }

                SetStringMiddle(null);
                return;
            }

            view.Root.SetActive(true);
            var t = view.Root.transform;
            t.SetPositionAndRotation(view.Bone.TransformPoint(view.BoneLocalPos), view.Bone.rotation * view.BoneLocalRot);
            t.localScale = Vector3.one;
            var rest = _gripRoot.TransformPoint(PropPoint(shots.ArrowRest));
            var dir = rest - t.position;
            if (dir.sqrMagnitude > 1e-10f)
            {
                t.rotation = Quaternion.FromToRotation(t.forward, dir.normalized) * t.rotation;
            }

            if (!view.Shown)
            {
                SetShown(view, true, true);
            }

            SetStringMiddle(shots.Nocked(frame) ? t.position : (Vector3?)null);
        }

        /// <summary>Another pose took over from an extra archer clip: bow back to its rest roll, arrow hidden, string straight.</summary>
        public void EndBowShots(BlenderBowShots shots)
        {
            ApplyGripRoll(false, 0f);
            SetStringMiddle(null);
            var view = BowShotArrow(shots);
            if (view != null)
            {
                SetShown(view, false, true);
                view.Root.SetActive(false);
            }
        }

        /// <summary>True once the attack has bound <paramref name="trackName"/> to its bone (Activate on the attack clip).</summary>
        public bool HeldBound(string trackName)
        {
            var view = HeldView(trackName);
            return view != null && view.Bone != null && view.BoneBound;
        }

        /// <summary>
        /// Extra clip with the attack's held prop (Oakenshield attack 2): the prop rides its bone at the attack binding, turns in
        /// the grip toward the two-hand line by <see cref="BlenderHeldRealign.Weight"/> and slides forward through the fist.
        /// The hand and arm are not touched. Call after the rig evaluated <paramref name="frame"/>.
        /// </summary>
        public void TickHeldRealign(BlenderHeldRealign realign, float frame)
        {
            var view = HeldView(realign.TrackName);
            if (view == null || view.Bone == null || !view.BoneBound)
            {
                return;
            }

            if (!realign.Visible(frame))
            {
                if (view.Shown)
                {
                    SetShown(view, false, true);
                }

                view.Root.SetActive(false);
                return;
            }

            view.Root.SetActive(true);
            var pos = view.Bone.TransformPoint(view.BoneLocalPos);
            var rot = view.Bone.rotation * view.BoneLocalRot;
            var w = realign.Weight(frame);
            if (w > 0f && _bones.TryGetValue(realign.FrontBone, out var front) && _bones.TryGetValue(realign.FrontParentBone, out var frontParent))
            {
                var fore = front.position - frontParent.position;
                var want = front.position + (fore * realign.PalmFraction) - pos;
                if (want.sqrMagnitude > 1e-10f)
                {
                    rot = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(rot * Vector3.forward, want.normalized), w) * rot;
                    var metre = fore.magnitude / Mathf.Max(realign.ForearmMetres, 1e-4f);
                    pos += rot * Vector3.forward * (realign.SlideMetres * w * metre);
                }
            }

            var t = view.Root.transform;
            t.SetPositionAndRotation(pos, rot);
            var s = realign.Scale(frame);
            t.localScale = new Vector3(s, s, s);
            if (!view.Shown)
            {
                SetShown(view, true, true);
            }
        }

        /// <summary>Another pose took over from a held-realign clip: the prop hides until its attack shows it again.</summary>
        public void EndHeldRealign(BlenderHeldRealign realign)
        {
            var view = HeldView(realign.TrackName);
            if (view != null)
            {
                SetShown(view, false, true);
                view.Root.SetActive(false);
            }
        }

        private TrackView? HeldView(string trackName)
        {
            foreach (var view in _views)
            {
                if (view.Track.Kind == AttackTrackKind.Held && string.Equals(view.Track.ObjectName, trackName, System.StringComparison.Ordinal))
                {
                    return view;
                }
            }

            return null;
        }

        private TrackView? BowShotArrow(BlenderBowShots shots)
        {
            foreach (var view in _views)
            {
                if (view.Track.Kind == AttackTrackKind.Held && string.Equals(view.Track.ObjectName, shots.ArrowTrack, System.StringComparison.Ordinal))
                {
                    return view;
                }
            }

            return null;
        }

        /// <summary>
        /// Builds rest/walk held props on named bones (Design NOTE). They stay visible until
        /// <see cref="SetIdlePropsVisible"/> hides them for the attack add-on.
        /// </summary>
        public void BindIdleProps(BlenderRigIdlePropSpec[] props)
        {
            if (props == null || props.Length == 0)
            {
                return;
            }

            if (!_calibrated)
            {
                Debug.LogWarning(_spec.Character + " idle props need attack calibration first.");
            }

            foreach (var prop in props)
            {
                if (_idleViews.Exists(v => v.Track.ObjectName == prop.ObjectName))
                {
                    continue;
                }

                var track = prop.ToTrack();
                var root = new GameObject(_spec.Character + "Idle_" + prop.ObjectName);
                root.transform.SetParent(_actor, false);
                var view = new TrackView(track, root);
                if (prop.Bone.Length > 0)
                {
                    if (_bones.TryGetValue(prop.Bone, out var bone))
                    {
                        view.Bone = bone;
                    }
                    else
                    {
                        Debug.LogError(_spec.Character + " idle attach bone missing. " + prop.Bone);
                    }
                }

                if (track.HasMesh)
                {
                    view.Mesh = LoadProp(track, root);
                }

                if (view.Bone != null && prop.BoneGrip != null)
                {
                    // Child of the hand bone at the Blender grip, converted bone space to bone space (no world-space fit).
                    AttachAtBoneGrip(view, root, prop.BoneGrip);
                }
                else if (view.Bone != null)
                {
                    var key = track.Frames[0];
                    var worldPos = WorldPoint(key);
                    var worldRot = WorldRotation(key);
                    view.BoneLocalPos = view.Bone.InverseTransformPoint(worldPos);
                    view.BoneLocalRot = Quaternion.Inverse(view.Bone.rotation) * worldRot;
                    view.BoneBound = true;
                    Place(view, key);
                    // Ride the bone directly so rest and walk carry the prop with no per-frame update.
                    root.transform.SetParent(view.Bone, true);
                }

                view.AttackTrackName = prop.AttackTrackName;
                _idleViews.Add(view);
                root.SetActive(true);
                SetShown(view, true, true);
            }
        }

        /// <summary>Shows or hides rest/walk props. Call false before the attack plays so nothing doubles.
        /// Props that stand in for an attack held track stay visible: they are the attack's prop.</summary>
        public void SetIdlePropsVisible(bool visible)
        {
            foreach (var view in _idleViews)
            {
                if (!visible && view.StandsInForAttack)
                {
                    continue;
                }

                if (view.Root != null)
                {
                    view.Root.SetActive(visible);
                }

                SetShown(view, visible, !visible);
            }
        }

        public void Dispose()
        {
            foreach (var view in _views)
            {
                if (view.Root != null)
                {
                    Object.Destroy(view.Root);
                }
            }

            _views.Clear();
            foreach (var view in _idleViews)
            {
                if (view.Root != null)
                {
                    Object.Destroy(view.Root);
                }
            }

            _idleViews.Clear();
            if (_additive != null)
            {
                Object.Destroy(_additive);
            }

            if (_dot != null)
            {
                Object.Destroy(_dot);
            }

            _built = false;
        }

        private Vector3 MapLocal(Vector3 design)
        {
            return _offset + (_scale * (_rotation * design));
        }

        private Vector3 WorldPoint(AttackFrame key)
        {
            return _actor.TransformPoint(MapLocal(new Vector3(key.X, key.Y, key.Z)));
        }

        private Quaternion WorldRotation(AttackFrame key)
        {
            var q = new Quaternion(key.Qx, key.Qy, key.Qz, key.Qw);
            return _actor.rotation * _rotation * q;
        }

        private static Quaternion Frame(Vector3 primary, Vector3 lateral)
        {
            if (primary.sqrMagnitude < 1e-12f)
            {
                primary = Vector3.up;
            }

            var p = primary.normalized;
            var up = Vector3.ProjectOnPlane(lateral, p);
            if (up.sqrMagnitude < 1e-12f)
            {
                up = Vector3.ProjectOnPlane(Vector3.right, p);
            }

            return Quaternion.LookRotation(p, up.normalized);
        }

        private void Place(TrackView view, AttackFrame key)
        {
            var t = view.Root.transform;
            if (view.Track.Kind == AttackTrackKind.Held && view.BoneBound && view.Bone != null)
            {
                t.SetPositionAndRotation(view.Bone.TransformPoint(view.BoneLocalPos), view.Bone.rotation * view.BoneLocalRot);
            }
            else if (view.Track.Kind == AttackTrackKind.Projectile && LaunchFrame != null)
            {
                // Once released, a shot flies in the frame it was launched in: a hit flinch on the
                // shooter (which leans this actor) must not swing it off its line.
                if (!view.HasLaunch)
                {
                    view.Launch = LaunchFrame();
                    view.HasLaunch = true;
                }

                var launch = view.Launch;
                t.SetPositionAndRotation(
                    launch.MultiplyPoint3x4(MapLocal(new Vector3(key.X, key.Y, key.Z))),
                    launch.rotation * _rotation * new Quaternion(key.Qx, key.Qy, key.Qz, key.Qw));
            }
            else
            {
                t.SetPositionAndRotation(WorldPoint(key), WorldRotation(key));
            }

            var s = Mathf.Max(key.Scale, 0.001f);
            t.localScale = new Vector3(s, s, s);
        }

        private void SetShown(TrackView view, bool shown, bool clear)
        {
            view.Shown = shown;
            if (view.Mesh != null)
            {
                view.Mesh.SetActive(shown);
            }

            foreach (var ps in view.Particles)
            {
                if (shown)
                {
                    if (clear)
                    {
                        ps.Clear(true);
                    }

                    ps.Play(true);
                }
                else
                {
                    ps.Stop(true, clear ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
                }
            }

            foreach (var trail in view.Trails)
            {
                if (shown && clear)
                {
                    trail.Clear();
                }

                trail.emitting = shown;
                if (!shown && clear)
                {
                    trail.Clear();
                }
            }

            foreach (var line in view.Lines)
            {
                line.enabled = shown;
            }
        }

        private void Build()
        {
            _built = true;
            _additive = MakeAdditive();
            foreach (var track in _spec.Tracks)
            {
                var root = new GameObject(_spec.Character + "Attack_" + track.ObjectName);
                root.transform.SetParent(_actor, false);
                var view = new TrackView(track, root);
                if (track.Kind == AttackTrackKind.Held && track.Bone.Length > 0)
                {
                    if (_bones.TryGetValue(track.Bone, out var bone))
                    {
                        view.Bone = bone;
                    }
                    else
                    {
                        Debug.LogError(_spec.Character + " attack attach bone missing. " + track.Bone);
                    }
                }

                var standIn = track.Kind == AttackTrackKind.Held ? IdleStandIn(track.ObjectName) : null;
                if (standIn != null)
                {
                    // The rest/walk prop already rides this bone with the same grip and the rig's bound
                    // textures. Reuse it rather than load a second copy whose imported material is untextured.
                    view.Bone = null;
                }
                else if (track.HasMesh)
                {
                    view.Mesh = LoadProp(track, root);
                }

                BuildEffect(view);
                _views.Add(view);
                SetShown(view, false, true);
                root.SetActive(false);
            }
        }

        private GameObject? LoadProp(AttackTrack track, GameObject root)
        {
#if UNITY_EDITOR
            var path = "Assets/" + _themePackDir + "/" + track.PropFileName;
            EnsurePropImport(path);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError(_spec.Character + " attack prop missing. " + path);
                return null;
            }

            var holder = new GameObject("Prop");
            var instance = Object.Instantiate(prefab, holder.transform);
            instance.name = track.ObjectName;
            foreach (var anim in instance.GetComponentsInChildren<Animator>(true))
            {
                Object.Destroy(anim);
            }

            var measured = Measure(holder.transform, instance);
            var expected = Mathf.Max(track.PropExtent[0], Mathf.Max(track.PropExtent[1], track.PropExtent[2]));
            var got = Mathf.Max(measured.x, Mathf.Max(measured.y, measured.z));
            var k = expected > 1e-6f && got > 1e-6f ? got / expected : 1f;
            if (LongAxis(measured) != LongAxis(new Vector3(track.PropExtent[0], track.PropExtent[1], track.PropExtent[2])))
            {
                Debug.LogWarning(
                    _spec.Character + " attack prop " + track.ObjectName + " long axis differs from Design. measured " +
                    measured.ToString("F3") + " Design metres " + track.PropExtent[0].ToString("F3") + "," +
                    track.PropExtent[1].ToString("F3") + "," + track.PropExtent[2].ToString("F3"));
            }

            var baseScale = _scale / k;
            holder.transform.SetParent(root.transform, false);
            holder.transform.localPosition = Vector3.zero;
            holder.transform.localRotation = Quaternion.identity;
            holder.transform.localScale = new Vector3(baseScale, baseScale, baseScale);
            FixPropMaterials(instance);
            return holder;
#else
            return null;
#endif
        }

        /// <summary>
        /// Parents the prop to its bone at the fixed Blender grip. Unity keeps the bone's local axes and flips handedness by
        /// negating local X; the import scale and that mapping are checked against two child bone heads of the real imported
        /// skeleton. The prop mesh is then turned so its Blender axes (long axis z, string side +y) match the converted grip,
        /// whatever node rotation the prop FBX import added.
        /// </summary>
        private void AttachAtBoneGrip(TrackView view, GameObject root, BlenderBoneGrip grip)
        {
            var bone = view.Bone!;
            var unitsPerMetre = 1f;
            if (_bones.TryGetValue(grip.ChildA, out var a) && _bones.TryGetValue(grip.ChildB, out var b))
            {
                var ea = V(BlenderBoneGrip.ToUnityPoint(grip.ChildAHead));
                var eb = V(BlenderBoneGrip.ToUnityPoint(grip.ChildBHead));
                unitsPerMetre = (a.localPosition.magnitude + b.localPosition.magnitude) / Mathf.Max(ea.magnitude + eb.magnitude, 1e-6f);
                var angA = Vector3.Angle(a.localPosition, ea);
                var angB = Vector3.Angle(b.localPosition, eb);
                Debug.Log(_spec.Character + " bone grip on " + bone.name + ": " + unitsPerMetre.ToString("0.####") +
                          " bone units per metre, child axis check " + angA.ToString("0.0") + " / " + angB.ToString("0.0") + " deg.");
                if (angA > 10f || angB > 10f)
                {
                    Debug.LogError(_spec.Character + " imported " + bone.name + " axes differ from Blender bone space (mirror-X expected); grip may be off.");
                }
            }
            else
            {
                Debug.LogWarning(_spec.Character + " bone grip: child bones " + grip.ChildA + "/" + grip.ChildB + " missing; assuming 1 unit per metre.");
            }

            var t = root.transform;
            t.SetParent(bone, false);
            t.localPosition = V(grip.UnityGripPos(unitsPerMetre));
            var q = grip.UnityGripRot();
            t.localRotation = new Quaternion(q[0], q[1], q[2], q[3]);
            var bs = bone.lossyScale.x;
            var s = Mathf.Abs(bs) > 1e-6f ? _actor.lossyScale.x / bs : 1f;
            t.localScale = new Vector3(s, s, s);
            view.BoneLocalPos = t.localPosition;
            view.BoneLocalRot = t.localRotation;
            view.BoneBound = true;
            if (view.Mesh != null)
            {
                foreach (var filter in view.Mesh.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == null)
                    {
                        continue;
                    }

                    var mb = filter.sharedMesh.bounds;
                    var ext = mb.extents;
                    var longAxis = LongAxis(ext);
                    var lv = Axis(longAxis);
                    int side = -1;
                    var best = -1f;
                    for (var i = 0; i < 3; i++)
                    {
                        if (i != longAxis && Mathf.Abs(mb.center[i]) > best)
                        {
                            best = Mathf.Abs(mb.center[i]);
                            side = i;
                        }
                    }

                    var sv = Axis(side) * Mathf.Sign(mb.center[side] == 0f ? 1f : mb.center[side]);
                    var meshFrame = Quaternion.LookRotation(lv, sv);   // mesh-space long axis -> z, string side -> y
                    filter.transform.SetPositionAndRotation(t.position, t.rotation * Quaternion.Inverse(meshFrame));
                }
            }

            _gripRoot = t;
            _grip = grip;
            _gripPerMetre = Mathf.Abs(s) > 1e-6f ? unitsPerMetre / s : unitsPerMetre;
            BuildDrawnString(view, grip);
            ApplyGripRoll(false, 0f);
        }

        /// <summary>
        /// Replaces the prop's static (braced) string with a line tip, middle, tip in the grip root's space. The middle point
        /// sits on the straight string at rest and walk, and on the arrow nock from nock to release, so the string is drawn
        /// to the arrow (Derek 10-15 23:29).
        /// </summary>
        private void BuildDrawnString(TrackView view, BlenderBoneGrip grip)
        {
            if (!grip.HasDrawnString || view.Mesh == null || _gripRoot == null)
            {
                return;
            }

            Material? stringMat = null;
            Vector3? measuredTop = null;
            Vector3? measuredBottom = null;
            foreach (var r in view.Mesh.GetComponentsInChildren<MeshRenderer>(true))
            {
                var filter = r.GetComponent<MeshFilter>();
                var mats = r.sharedMaterials;
                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                for (var i = 0; i < mats.Length && i < filter.sharedMesh.subMeshCount; i++)
                {
                    if (mats[i] == null || grip.StringMaterial.Length == 0 || mats[i].name.IndexOf(grip.StringMaterial, System.StringComparison.Ordinal) < 0)
                    {
                        continue;
                    }

                    stringMat = mats[i];
                    if (!filter.sharedMesh.isReadable)
                    {
                        // Read/Write off on the import: the CPU can't read or cut the mesh (that only logs "Not allowed to
                        // access vertices" errors). Use the spec's design string tips, and hide the braced string by
                        // dropping its material slot when it is the mesh's last submesh (the GPU skips unmaterialed
                        // submeshes); otherwise it stays alongside the drawn one.
                        Debug.LogWarning(_spec.Character + " bow mesh " + filter.sharedMesh.name + " has Read/Write off; using the design string tips.");
                        if (i == mats.Length - 1 && i == filter.sharedMesh.subMeshCount - 1 && i > 0)
                        {
                            var kept = new Material[i];
                            System.Array.Copy(mats, kept, i);
                            r.sharedMaterials = kept;
                        }

                        continue;
                    }

                    // measure the braced string where the import actually put it (grip-root space): tips = its two ends
                    var verts = filter.sharedMesh.vertices;
                    var tris = filter.sharedMesh.GetTriangles(i);
                    var lo = new Vector3(0f, 0f, float.MaxValue);
                    var hi = new Vector3(0f, 0f, float.MinValue);
                    foreach (var vi in tris)
                    {
                        var lp = _gripRoot.InverseTransformPoint(filter.transform.TransformPoint(verts[vi]));
                        if (lp.z < lo.z)
                        {
                            lo = lp;
                        }

                        if (lp.z > hi.z)
                        {
                            hi = lp;
                        }
                    }

                    if (tris.Length > 0)
                    {
                        measuredTop = hi;
                        measuredBottom = lo;
                    }

                    var copy = Object.Instantiate(filter.sharedMesh);
                    copy.name = filter.sharedMesh.name + "_nostring";
                    copy.SetTriangles(System.Array.Empty<int>(), i);
                    filter.sharedMesh = copy;
                }
            }

            if (stringMat == null)
            {
                Debug.LogWarning(_spec.Character + " bow string material " + grip.StringMaterial + " not found; drawn string skipped.");
                return;
            }

            var go = new GameObject("DrawnString");
            go.transform.SetParent(_gripRoot, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 3;
            line.sharedMaterial = stringMat;
            line.widthMultiplier = 0.004f * _scale * Mathf.Abs(_actor.lossyScale.x);
            line.numCapVertices = 0;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = ShadowCastingMode.Off;
            _stringTop = PropPoint(grip.StringTop!);
            _stringBottom = PropPoint(grip.StringBottom!);
            if (measuredTop.HasValue && measuredBottom.HasValue)
            {
                // trust the imported mesh: its string length fixes grip-root units per metre for the arrow rest too
                var designLen = Mathf.Abs(grip.StringTop![2] - grip.StringBottom![2]);
                var got = (measuredTop.Value - measuredBottom.Value).magnitude;
                if (designLen > 1e-4f && got > 1e-6f)
                {
                    Debug.Log(_spec.Character + " bow string measured " + got.ToString("0.###") + " root units (expected " +
                              (designLen * _gripPerMetre).ToString("0.###") + "); top " + measuredTop.Value.ToString("F3") + ", bottom " + measuredBottom.Value.ToString("F3"));
                    _gripPerMetre = got / designLen;
                }

                _stringTop = measuredTop.Value;
                _stringBottom = measuredBottom.Value;
            }
            _string = line;
            SetStringMiddle(null);
        }

        private Vector3 PropPoint(float[] p) => V(BlenderBoneGrip.PropLocalToUnity(p)) * _gripPerMetre;

        private void SetStringMiddle(Vector3? nockWorld)
        {
            if (_string == null || _gripRoot == null)
            {
                return;
            }

            var mid = nockWorld.HasValue ? _gripRoot.InverseTransformPoint(nockWorld.Value) : (_stringTop + _stringBottom) * 0.5f;
            _string.SetPosition(0, _stringTop);
            _string.SetPosition(1, mid);
            _string.SetPosition(2, _stringBottom);
        }

        /// <summary>From nock to release: aim the nocked arrow from its nock through the arrow rest beside the riser (the same
        /// line Blender bakes) and draw the string to the nock. The arrow's long axis is its root +Z (character forward in the keys).</summary>
        private void AimNockedArrow(TrackView view, float frame)
        {
            if (_grip == null || _gripRoot == null || _grip.ArrowRest == null || !_grip.Nocked(true, frame))
            {
                return;
            }

            var t = view.Root.transform;
            var rest = _gripRoot.TransformPoint(PropPoint(_grip.ArrowRest));
            var dir = rest - t.position;
            if (dir.sqrMagnitude > 1e-10f)
            {
                t.rotation = Quaternion.FromToRotation(t.forward, dir.normalized) * t.rotation;
            }

            SetStringMiddle(t.position);
        }

        /// <summary>Rolls the bone-gripped prop about its long axis: rest/walk carry (string away from the body), eased to the draw
        /// roll from nock to release in the attack. The mesh axis correction lives below the root, so only the root turns.</summary>
        private void ApplyGripRoll(bool attack, float frame)
        {
            if (_gripRoot == null || _grip == null)
            {
                return;
            }

            var q = _grip.UnityRotation(_grip.RollDeg(attack, frame));
            _gripRoot.localRotation = new Quaternion(q[0], q[1], q[2], q[3]);
        }

        /// <summary>How long a stopped arrow stays on the body before it is hidden.</summary>
        private const float StuckSeconds = 0.15f;

        /// <summary>Flying tracks this close to a stopped one are the same shot (its trail or particles).</summary>
        private const float TwinRadius = 0.75f;

        private int _impactCycle = -1;
        private Transform? _gripRoot;
        private BlenderBoneGrip? _grip;
        private float _gripPerMetre = 1f;
        private LineRenderer? _string;
        private Vector3 _stringTop;
        private Vector3 _stringBottom;

        private static Vector3 V(float[] p) => new Vector3(p[0], p[1], p[2]);

        private static Vector3 Axis(int i) => i == 0 ? Vector3.right : i == 1 ? Vector3.up : Vector3.forward;

        private TrackView? IdleStandIn(string attackTrackName)
        {
            foreach (var view in _idleViews)
            {
                if (view.StandsInForAttack && view.Mesh != null
                    && string.Equals(view.AttackTrackName, attackTrackName, System.StringComparison.Ordinal))
                {
                    return view;
                }
            }

            return null;
        }

        private static int LongAxis(Vector3 v)
        {
            if (v.x >= v.y && v.x >= v.z)
            {
                return 0;
            }

            return v.y >= v.z ? 1 : 2;
        }

        private static Vector3 Measure(Transform space, GameObject instance)
        {
            var seeded = false;
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var filter in instance.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                var m = space.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                var b = filter.sharedMesh.bounds;
                for (var i = 0; i < 8; i++)
                {
                    var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    var p = m.MultiplyPoint3x4(corner);
                    if (!seeded)
                    {
                        bounds = new Bounds(p, Vector3.zero);
                        seeded = true;
                    }
                    else
                    {
                        bounds.Encapsulate(p);
                    }
                }
            }

            return bounds.size;
        }

        private static void FixPropMaterials(GameObject root)
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                rend.shadowCastingMode = ShadowCastingMode.On;
                var shared = rend.sharedMaterials;
                for (var i = 0; i < shared.Length; i++)
                {
                    var mat = shared[i];
                    if (lit == null)
                    {
                        continue;
                    }

                    var color = Color.white;
                    if (mat != null)
                    {
                        if (mat.HasProperty("_BaseColor"))
                        {
                            color = mat.GetColor("_BaseColor");
                        }
                        else if (mat.HasProperty("_Color"))
                        {
                            color = mat.GetColor("_Color");
                        }
                    }

                    if (mat == null || mat.shader == null || mat.shader.name.IndexOf("Universal", System.StringComparison.Ordinal) < 0)
                    {
                        var next = new Material(lit);
                        next.name = mat != null ? mat.name : "AttackProp";
                        if (next.HasProperty("_BaseColor"))
                        {
                            next.SetColor("_BaseColor", color);
                        }

                        next.color = color;
                        mat = next;
                        shared[i] = mat;
                    }

                    var lower = mat.name.ToLowerInvariant();
                    if (lower.Contains("crystal") || lower.Contains("rune") || lower.Contains("spiral") || lower.Contains("band"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        if (mat.HasProperty("_EmissionColor"))
                        {
                            mat.SetColor("_EmissionColor", color * 1.4f);
                        }
                    }
                }

                rend.sharedMaterials = shared;
            }
        }

#if UNITY_EDITOR
        private void EnsurePropImport(string path)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            {
                Debug.LogError(_spec.Character + " attack prop importer missing. " + path);
                return;
            }

            var dirty = false;
            if (importer.animationType != ModelImporterAnimationType.None)
            {
                importer.animationType = ModelImporterAnimationType.None;
                dirty = true;
            }

            if (importer.importAnimation)
            {
                importer.importAnimation = false;
                dirty = true;
            }

            if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                dirty = true;
            }

            if (importer.bakeAxisConversion)
            {
                importer.bakeAxisConversion = false;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }
#endif

        private void BuildEffect(TrackView view)
        {
            var track = view.Track;
            var s = _scale;
            var core = Rgb(track.CoreRgb, 1f);
            var glow = Rgb(track.GlowRgb, 0.85f);
            var edge = Rgb(track.EdgeRgb, 0.5f);
            var size = Mathf.Max(track.SizeMetres, 0.02f) * s;
            var length = Mathf.Max(track.LengthMetres, 0.05f) * s;
            var particles = new List<ParticleSystem>();
            var trails = new List<TrailRenderer>();
            var lines = new List<LineRenderer>();
            switch (track.Shape)
            {
                case AttackVfxShape.Orb:
                    particles.Add(AddParticles(view.Root, "Core", false, 70f, 0.12f, 0f, size * 0.55f, size * 0.85f, core, glow, edge, ParticleSystemShapeType.Sphere, size * 0.05f, 0f, 0.7f));
                    particles.Add(AddParticles(view.Root, "Embers", true, 90f, 0.35f, 0.35f * s, size * 0.08f, size * 0.22f, glow, edge, edge, ParticleSystemShapeType.Sphere, size * 0.35f, 0f, 0.3f));
                    trails.Add(AddTrail(view.Root, "Trail", 0.18f, size * 0.6f, glow, edge));
                    break;
                case AttackVfxShape.Trail:
                    trails.Add(AddTrail(view.Root, "Trail", 0.14f, Mathf.Max(size, 0.035f * s), core, glow));
                    particles.Add(AddParticles(view.Root, "Sparks", true, 45f, 0.25f, 0.2f * s, 0.02f * s, 0.05f * s, core, glow, edge, ParticleSystemShapeType.Sphere, 0.02f * s, 0f, 0.2f));
                    break;
                case AttackVfxShape.Flare:
                    var cone = AddParticles(view.Root, "Breath", true, 160f, 0.22f, length / 0.22f, size * 0.25f, size * 0.5f, core, glow, edge, ParticleSystemShapeType.Cone, size * 0.08f, 16f, 1.8f);
                    particles.Add(cone);
                    particles.Add(AddParticles(view.Root, "MouthGlow", false, 50f, 0.1f, 0f, size * 0.45f, size * 0.7f, core, glow, edge, ParticleSystemShapeType.Sphere, size * 0.02f, 0f, 0.8f));
                    break;
                case AttackVfxShape.Lightning:
                    lines.Add(AddLine(view.Root, "BoltGlow", 12, 0.12f * s, glow, edge));
                    lines.Add(AddLine(view.Root, "BoltCore", 12, 0.035f * s, core, glow));
                    particles.Add(AddParticles(view.Root, "Sparks", true, 80f, 0.3f, 0.8f * s, 0.02f * s, 0.06f * s, Rgb(track.EdgeRgb, 1f), glow, glow, ParticleSystemShapeType.Sphere, 0.08f * s, 0f, 0.2f));
                    view.LineLength = length;
                    break;
                case AttackVfxShape.Slash:
                    var muzzle = new GameObject("Muzzle");
                    muzzle.transform.SetParent(view.Root.transform, false);
                    view.Muzzle = muzzle.transform;
                    var tips = new List<Transform>();
                    for (var i = 0; i < AttackEmission.SlashClawCount; i++)
                    {
                        var tip = new GameObject("Claw" + i);
                        tip.transform.SetParent(muzzle.transform, false);
                        trails.Add(AddTrail(tip, "Trail", 0.16f, 0.035f * s, core, glow));
                        tips.Add(tip.transform);
                    }

                    view.SlashTips = tips.ToArray();
                    view.SlashWidth = Mathf.Max(track.PropExtent[0], 0.2f) * s;
                    view.SlashHeight = Mathf.Max(track.PropExtent[1], 0.1f) * s;
                    var sparks = AddParticles(
                        muzzle,
                        "Sparks",
                        true,
                        60f,
                        0.25f,
                        0.6f * s,
                        0.02f * s,
                        0.05f * s,
                        core,
                        glow,
                        edge,
                        ParticleSystemShapeType.Cone,
                        0.04f * s,
                        AttackEmission.SlashConeHalfAngleDegrees,
                        0.2f);
                    var sparkShape = sparks.shape;
                    sparkShape.randomDirectionAmount = 0f;
                    sparkShape.sphericalDirectionAmount = 0f;
                    var sparkMain = sparks.main;
                    sparkMain.gravityModifier = 0f;
                    particles.Add(sparks);
                    break;
            }

            view.Particles = particles.ToArray();
            view.Trails = trails.ToArray();
            view.Lines = lines.ToArray();
        }

        private static void ResetBlock(TrackView view)
        {
            view.Stopped = false;
            view.ImpactFired = false;
            view.HasLastHead = false;
            view.Warned = false;
            view.HasLaunch = false;
            view.Reach = -1f;
        }

        /// <summary>
        /// Asks <see cref="Blocker"/> whether this flying track reached something this frame.
        /// A bolt is clipped at the contact and keeps crackling there. Anything else stops at the
        /// contact: its mesh and particles stop emitting and its trail ends there.
        /// Returns true when the track stopped and must not be updated further this cycle.
        /// </summary>
        private bool TryBlock(TrackView view)
        {
            var blocker = Blocker;
            if (blocker == null)
            {
                return false;
            }

            var root = view.Root.transform;
            if (view.Track.Shape == AttackVfxShape.Lightning && view.Lines.Length > 0)
            {
                var basePoint = root.position;
                var tip = view.Lines[0].transform.TransformPoint(new Vector3(0f, 0f, view.LineLength));
                if (!blocker(view.Track, basePoint, tip, out var hit))
                {
                    view.Reach = -1f;
                    return false;
                }

                var scale = Mathf.Max(view.Lines[0].transform.lossyScale.z, 1e-6f);
                view.Reach = Mathf.Clamp(Vector3.Distance(basePoint, hit) / scale, 0.01f, view.LineLength);
                RollLightning(view);
                FireImpact(view, hit);
                return false;
            }

            // Swept contact on the leading tip: the segment from last frame's tip to this frame's tip,
            // then the shaft itself (tail to tip), so a fast arrow cannot step over a thin body box.
            MeasureTip(view);
            var arrowTip = root.TransformPoint(new Vector3(0f, 0f, view.TipZ));
            var arrowTail = root.TransformPoint(new Vector3(0f, 0f, view.TailZ));
            var from = view.HasLastHead ? view.LastHead : arrowTail;
            view.LastHead = arrowTip;
            view.HasLastHead = true;
            if (!blocker(view.Track, from, arrowTip, out var contact)
                && !blocker(view.Track, arrowTail, arrowTip, out contact))
            {
                return false;
            }

            StopAt(view, contact - arrowTip);
            foreach (var other in _views)
            {
                // The trail or particle twin of the same shot (riding with it) ends where the arrow did.
                if (other != view && !other.Stopped && other.Shown && other.Track.Kind == AttackTrackKind.Projectile
                    && other.Track.Shape != AttackVfxShape.Lightning
                    && (other.Root.transform.position - arrowTip).sqrMagnitude < TwinRadius * TwinRadius)
                {
                    StopAt(other, contact - other.Root.transform.position);
                }
            }

            FireImpact(view, contact);
            return true;
        }

        /// <summary>Freezes a flying track, shifted so its tip sits on the contact; a mesh stays visible there briefly.</summary>
        private void StopAt(TrackView view, Vector3 shift)
        {
            var t = view.Root.transform;
            t.position += shift;
            view.Stopped = true;
            view.StopTime = Time.time;
            foreach (var ps in view.Particles)
            {
                var emission = ps.emission;
                emission.enabled = false;
            }

            foreach (var trail in view.Trails)
            {
                trail.emitting = false;
            }

            if (view.Mesh == null)
            {
                SetShown(view, false, false);
            }
        }

        /// <summary>Tip and tail of a projectile mesh along its root +Z (the flight axis), in root units. Point effects use the root.</summary>
        private static void MeasureTip(TrackView view)
        {
            if (view.HasTip)
            {
                return;
            }

            view.HasTip = true;
            view.TipZ = 0f;
            view.TailZ = 0f;
            if (view.Mesh == null)
            {
                return;
            }

            var root = view.Root.transform;
            var lo = float.MaxValue;
            var hi = float.MinValue;
            foreach (var filter in view.Mesh.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                var mb = filter.sharedMesh.bounds;
                for (var corner = 0; corner < 8; corner++)
                {
                    var c = new Vector3(
                        (corner & 1) == 0 ? mb.min.x : mb.max.x,
                        (corner & 2) == 0 ? mb.min.y : mb.max.y,
                        (corner & 4) == 0 ? mb.min.z : mb.max.z);
                    var z = root.InverseTransformPoint(filter.transform.TransformPoint(c)).z;
                    lo = Mathf.Min(lo, z);
                    hi = Mathf.Max(hi, z);
                }
            }

            if (hi >= lo)
            {
                view.TipZ = hi;
                view.TailZ = lo;
            }
        }

        private void FireImpact(TrackView view, Vector3 contact)
        {
            // One burst per shot: the arrow and its trail twin share the attack cycle.
            if (view.ImpactFired || _impactCycle == _cycle)
            {
                view.ImpactFired = true;
                return;
            }

            view.ImpactFired = true;
            _impactCycle = _cycle;
            Impact?.Invoke(view.Track, contact);
        }

        private void RollLightning(TrackView view)
        {
            var count = 12;
            var positions = new Vector3[count];
            var length = view.Reach > 0f ? Mathf.Min(view.Reach, view.LineLength) : view.LineLength;
            var jitter = length * 0.035f;
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)(count - 1);
                var j = i == 0 || i == count - 1 ? 0f : jitter;
                positions[i] = new Vector3(Random.Range(-j, j), Random.Range(-j, j), t * length);
            }

            foreach (var line in view.Lines)
            {
                line.positionCount = count;
                line.SetPositions(positions);
            }
        }

        /// <summary>
        /// Claw trails and the spark cone leave the mouth along the actor root forward.
        /// World positions ignore the effect root's scale keys and the calibration frame,
        /// so a yawed head−hips mapping or a growing lane cannot shove the shot sideways.
        /// </summary>
        private void SweepSlash(TrackView view, float frame)
        {
            if (view.Muzzle == null || view.SlashTips.Length == 0)
            {
                return;
            }

            var mouth = view.Root.transform.position;
            view.Muzzle.SetPositionAndRotation(mouth, _actor.rotation);
            var p = AttackEmission.Progress(frame, view.Track.FirstVisibleFrame, view.Track.LastVisibleFrame);
            var reach = view.SlashWidth;
            var lane = view.SlashHeight * AttackEmission.SlashLaneFraction;
            var right = _actor.right;
            var up = _actor.up;
            var forward = _actor.forward;
            for (var i = 0; i < view.SlashTips.Length; i++)
            {
                AttackEmission.StraightAheadTip(i, view.SlashTips.Length, p, reach, lane, out var x, out var y, out var z);
                view.SlashTips[i].position = mouth + (right * x) + (up * y) + (forward * z);
            }
        }

        private ParticleSystem AddParticles(
            GameObject host,
            string name,
            bool worldSpace,
            float rate,
            float lifetime,
            float speed,
            float sizeMin,
            float sizeMax,
            Color a,
            Color b,
            Color c,
            ParticleSystemShapeType shapeType,
            float radius,
            float angle,
            float sizeEnd)
        {
            var go = new GameObject(name);
            go.transform.SetParent(host.transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = Color.white;
            main.simulationSpace = worldSpace ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 400;
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = shapeType;
            shape.radius = Mathf.Max(radius, 0.0001f);
            shape.angle = angle;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Fade(a, b, c));
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, sizeEnd));
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = _additive;
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
            return ps;
        }

        private TrailRenderer AddTrail(GameObject host, string name, float time, float width, Color a, Color b)
        {
            var go = new GameObject(name);
            go.transform.SetParent(host.transform, false);
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = time;
            trail.widthMultiplier = width;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.colorGradient = Fade(a, b, b);
            trail.sharedMaterial = _additive;
            trail.minVertexDistance = Mathf.Max(0.005f, width * 0.15f);
            trail.numCapVertices = 2;
            trail.alignment = LineAlignment.View;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;
            return trail;
        }

        private LineRenderer AddLine(GameObject host, string name, int count, float width, Color a, Color b)
        {
            var go = new GameObject(name);
            go.transform.SetParent(host.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = count;
            line.widthMultiplier = width;
            line.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.35f);
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.75f, 1f) });
            line.colorGradient = g;
            line.sharedMaterial = _additive;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }

        private static Gradient Fade(Color a, Color b, Color c)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 0.45f), new GradientColorKey(c, 1f) },
                new[] { new GradientAlphaKey(a.a, 0f), new GradientAlphaKey(b.a * 0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        private static Color Rgb(float[] rgb, float alpha)
        {
            if (rgb == null || rgb.Length < 3)
            {
                return new Color(1f, 1f, 1f, alpha);
            }

            return new Color(rgb[0], rgb[1], rgb[2], alpha);
        }

        private Material MakeAdditive()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.name = _spec.Character + "AttackFx";
            _dot = SoftDot();
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", _dot);
            }

            if (mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", _dot);
            }

            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", Color.white);
            }

            SetFloat(mat, "_Surface", 1f);
            SetFloat(mat, "_Blend", 2f);
            SetFloat(mat, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloat(mat, "_DstBlend", (float)BlendMode.One);
            SetFloat(mat, "_SrcBlendAlpha", (float)BlendMode.One);
            SetFloat(mat, "_DstBlendAlpha", (float)BlendMode.One);
            SetFloat(mat, "_ZWrite", 0f);
            SetFloat(mat, "_Cull", (float)CullMode.Off);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            return mat;
        }

        private static void SetFloat(Material mat, string name, float value)
        {
            if (mat.HasProperty(name))
            {
                mat.SetFloat(name, value);
            }
        }

        private static Texture2D SoftDot()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.name = "AttackSoftDot";
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[n * n];
            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                {
                    var dx = ((x + 0.5f) / n * 2f) - 1f;
                    var dy = ((y + 0.5f) / n * 2f) - 1f;
                    var r = Mathf.Clamp01(Mathf.Sqrt((dx * dx) + (dy * dy)));
                    var a = (1f - r) * (1f - r);
                    pixels[(y * n) + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        private sealed class TrackView
        {
            public TrackView(AttackTrack track, GameObject root)
            {
                Track = track;
                Root = root;
            }

            public AttackTrack Track { get; }
            public GameObject Root { get; }
            public GameObject? Mesh { get; set; }
            public Transform? Bone { get; set; }
            public Vector3 BoneLocalPos { get; set; }
            public Quaternion BoneLocalRot { get; set; } = Quaternion.identity;
            public bool BoneBound { get; set; }
            public bool Shown { get; set; }
            public ParticleSystem[] Particles { get; set; } = System.Array.Empty<ParticleSystem>();
            public TrailRenderer[] Trails { get; set; } = System.Array.Empty<TrailRenderer>();
            public LineRenderer[] Lines { get; set; } = System.Array.Empty<LineRenderer>();
            public Transform? Muzzle { get; set; }
            public Transform[] SlashTips { get; set; } = System.Array.Empty<Transform>();
            public float LineLength { get; set; }
            public float SlashWidth { get; set; }
            public float SlashHeight { get; set; }
            public bool Stopped { get; set; }
            public bool ImpactFired { get; set; }
            public bool HasLastHead { get; set; }
            public bool HasTip { get; set; }
            public float TipZ { get; set; }
            public float TailZ { get; set; }
            public float StopTime { get; set; }
            public bool Warned { get; set; }
            public bool HasLaunch { get; set; }
            public Matrix4x4 Launch { get; set; } = Matrix4x4.identity;
            public Vector3 LastHead { get; set; }
            public float Reach { get; set; } = -1f;
            public string AttackTrackName { get; set; } = string.Empty;
            public bool StandsInForAttack => AttackTrackName.Length > 0;
        }
    }
}
