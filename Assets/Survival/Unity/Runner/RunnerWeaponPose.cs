#nullable enable
using System.Collections.Generic;
using Survival.Domain.Runner;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// The runner's procedural weapon pose (<see cref="RunnerWeaponAim"/>), run in LateUpdate after the walk clip posed the
    /// bones. A staff (Lyra) is held level at the enemy with both hands and kicks back along its axis on each shot; a bow
    /// (Rowan) is held out at the enemy while the string hand draws over the fire interval and flies back on the release.
    /// Only the chest and the arms are touched (rotation only); the hips and legs keep walking. Every touched bone is put
    /// back to its clip value first, so a skipped clip evaluation never accumulates the pose.
    /// </summary>
    public sealed class RunnerWeaponPose
    {
        /// <summary>The arm length (shoulder to wrist) the domain's metres are written for.</summary>
        private const float DesignArm = 0.55f;

        private readonly RunnerAimStyle _style;
        private readonly Transform _host;
        private readonly Transform _chest;
        private readonly Arm _weaponArm;
        private readonly Arm _offArm;
        private readonly List<Kept> _kept = new List<Kept>();
        private readonly RunnerKickState _kick = new RunnerKickState();

        private Transform? _prop;
        private bool _measured;

        /// <summary>Weapon ends and axes in the weapon hand's local space (measured once from the prop's mesh).</summary>
        private Vector3 _tipLocal;
        private Vector3 _gripLocal;
        private Vector3 _longLocal;
        private Vector3 _depthLocal;

        private RunnerWeaponPose(RunnerAimStyle style, Transform host, Transform chest, Arm weaponArm, Arm offArm)
        {
            _style = style;
            _host = host;
            _chest = chest;
            _weaponArm = weaponArm;
            _offArm = offArm;
            foreach (var t in new[] { chest, weaponArm.Upper, weaponArm.Lower, weaponArm.Hand, offArm.Upper, offArm.Lower, offArm.Hand })
            {
                _kept.Add(new Kept(t));
            }
        }

        /// <summary>The weapon's tip (staff head, or the arrow on the bow) this frame, world space; null until measured.</summary>
        public Vector3? Muzzle { get; private set; }

        public static RunnerWeaponPose? Build(Transform host, string rosterName)
        {
            var style = RunnerWeaponAim.StyleFor(rosterName);
            if (style == RunnerAimStyle.None)
            {
                return null;
            }

            var bones = new Dictionary<string, Transform>();
            foreach (var t in host.GetComponentsInChildren<Transform>(true))
            {
                var colon = t.name.LastIndexOf(':');
                var key = colon >= 0 ? t.name.Substring(colon + 1) : t.name;
                if (!bones.ContainsKey(key))
                {
                    bones[key] = t;
                }
            }

            Transform? B(string n) => bones.TryGetValue(n, out var t) ? t : null;
            var chest = B("Spine2") ?? B("Spine1");
            var right = Arm.Find(B("RightArm"), B("RightForeArm"), B("RightHand"));
            var left = Arm.Find(B("LeftArm"), B("LeftForeArm"), B("LeftHand"));
            if (chest == null || right == null || left == null)
            {
                Debug.LogWarning(rosterName + " runner weapon pose: arm bones missing, the weapon stays on the walk clip.");
                return null;
            }

            // The staff rides the right hand, the bow the left.
            return style == RunnerAimStyle.Staff
                ? new RunnerWeaponPose(style, host, chest, right, left)
                : new RunnerWeaponPose(style, host, chest, left, right);
        }

        /// <summary>The sim fired this hero's shot: the kick (and the bow's release) starts now.</summary>
        public void Fire() => _kick.Fire();

        public void Reset()
        {
            _kick.Reset();
            foreach (var k in _kept)
            {
                k.Restore();
            }
        }

        /// <summary>After the clips posed the bones (and the host was placed): aims the weapon down <paramref name="aim"/>.</summary>
        public void Tick(float dt, Vector3 aim, bool dead)
        {
            foreach (var saved in _kept)
            {
                saved.Restore();
            }

            _kick.Advance(dt);
            if (dead)
            {
                Muzzle = null;
                return;
            }

            if (!_measured && !Measure())
            {
                return;
            }

            aim.y = 0f;
            var f = aim.sqrMagnitude > 1e-6f ? aim.normalized : Vector3.forward;
            var u = Vector3.up;
            var r = Vector3.Cross(u, f).normalized;
            var k = _weaponArm.Length / DesignArm;
            var w = _kick.Weight;

            // The chest gives a little with the shot (pitch back about the aim's right axis).
            var give = Quaternion.AngleAxis(-RunnerWeaponAim.ShoulderGiveDegrees * w, r);
            Set(_chest, give * _chest.rotation);

            var hand = _weaponArm.Hand;
            Quaternion want;
            Vector3 hold;
            if (_style == RunnerAimStyle.Staff)
            {
                // Level at the enemy, the tip rising with the kick, the whole staff sliding back along its axis.
                var axis = Quaternion.AngleAxis(-RunnerWeaponAim.StaffTipUpDegrees * w, r) * f;
                var nowAxis = hand.TransformDirection(_longLocal);
                want = Quaternion.FromToRotation(nowAxis, axis) * hand.rotation;
                var s = RunnerWeaponAim.StaffHold;
                hold = _weaponArm.Upper.position + (k * ((r * s.X) + (u * s.Y) + (f * s.Z))) - (axis * (RunnerWeaponAim.StaffRecoilMetres * k * w));
            }
            else
            {
                // The bow upright with its arrow line down the aim; the release tips it up.
                var up = Quaternion.AngleAxis(-RunnerWeaponAim.BowTipUpDegrees * w, r) * u;
                var fwd = Quaternion.AngleAxis(-RunnerWeaponAim.BowTipUpDegrees * w, r) * f;
                var nowLong = hand.TransformDirection(_longLocal);
                var nowDepth = Vector3.ProjectOnPlane(hand.TransformDirection(_depthLocal), nowLong);
                var from = Quaternion.LookRotation(nowDepth.normalized, nowLong.normalized);
                var to = Quaternion.LookRotation(fwd, up);
                want = to * Quaternion.Inverse(from) * hand.rotation;
                var s = RunnerWeaponAim.BowHold;
                hold = _weaponArm.Upper.position + (k * ((r * s.X) + (u * s.Y) + (f * s.Z))) + (u * (0.02f * k * w));
            }

            // Put the weapon's grip (not the wrist) at the hold point.
            var gripOffset = want * Quaternion.Inverse(hand.rotation) * (hand.TransformPoint(_gripLocal) - hand.position);
            var side = _style == RunnerAimStyle.Staff ? r : -r;
            _weaponArm.Reach(hold - gripOffset, _weaponArm.Upper.position + (k * ((side * 0.30f) - (u * 0.40f) - (f * 0.10f))), Set);
            Set(hand, want);

            var grip = hand.TransformPoint(_gripLocal);
            var tip = hand.TransformPoint(_tipLocal);
            var off = _offArm;
            if (_style == RunnerAimStyle.Staff)
            {
                // The off hand on the shaft ahead of the staff hand, just under it.
                var along = (tip - grip).normalized;
                var at = grip + (along * (RunnerWeaponAim.StaffOffHandAlong * k)) - (u * (0.03f * k));
                off.Reach(at, off.Upper.position + (k * ((-r * 0.30f) - (u * 0.40f))), Set);
                Muzzle = tip;
            }
            else
            {
                // The string hand: back from the nock to the jaw over the draw, flying back and out on the release.
                var draw = RunnerWeaponAim.Draw(_kick.SinceShot, _kick.Interval);
                var nock = grip - (f * (RunnerWeaponAim.NockBehind * k));
                var full = grip - (f * (RunnerWeaponAim.FullDrawBehind * k)) + (u * (RunnerWeaponAim.FullDrawUp * k));
                var flick = ((-f * 0.6f) + (r * 0.8f)).normalized * (RunnerWeaponAim.ReleaseFollowThrough * k * w);
                off.Reach(Vector3.Lerp(nock, full, draw) + flick, off.Upper.position + (k * ((-f * 0.45f) + (r * 0.25f))), Set);
                Muzzle = grip + (u * (0.03f * k));
            }
        }

        /// <summary>
        /// The prop on the weapon hand: its long axis (the mesh's longest bounds axis) and ends, the tip being the end that
        /// is higher on the walk clip (the staff head; the bow's top limb). The bow's depth axis (the arrow line) is the
        /// middle bounds axis, signed toward the side the bow's riser bulges (away from the string).
        /// </summary>
        private bool Measure()
        {
            if (_prop == null)
            {
                _prop = FindProp(_weaponArm.Hand);
                if (_prop == null)
                {
                    return false;
                }
            }

            Mesh? mesh = null;
            var filter = _prop.GetComponentInChildren<MeshFilter>(true);
            Transform? meshT = null;
            if (filter != null && filter.sharedMesh != null)
            {
                mesh = filter.sharedMesh;
                meshT = filter.transform;
            }
            else
            {
                var skin = _prop.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (skin != null && skin.sharedMesh != null)
                {
                    mesh = skin.sharedMesh;
                    meshT = skin.transform;
                }
            }

            if (mesh == null || meshT == null)
            {
                return false;
            }

            var b = mesh.bounds;
            var ext = b.extents;
            var axes = new[] { Vector3.right, Vector3.up, Vector3.forward };
            var size = new[] { ext.x, ext.y, ext.z };
            var order = new List<int> { 0, 1, 2 };
            order.Sort((i, j) => size[j].CompareTo(size[i]));
            var longAx = axes[order[0]] * size[order[0]];
            var depthAx = axes[order[1]];
            var endA = meshT.TransformPoint(b.center + longAx);
            var endB = meshT.TransformPoint(b.center - longAx);
            var top = endA.y >= endB.y ? endA : endB;
            var bottom = endA.y >= endB.y ? endB : endA;
            var hand = _weaponArm.Hand;
            _tipLocal = hand.InverseTransformPoint(top);
            var bottomLocal = hand.InverseTransformPoint(bottom);
            _longLocal = (_tipLocal - bottomLocal).normalized;

            // The grip: the point on the weapon's axis nearest the hand.
            _gripLocal = bottomLocal + (_longLocal * Vector3.Dot(-bottomLocal, _longLocal));
            if (_style == RunnerAimStyle.Staff)
            {
                // Slide the staff through the hand (once) so the hand holds it near the butt and the head leads.
                var length = Vector3.Distance(_tipLocal, bottomLocal);
                var at = Vector3.Dot(_gripLocal - bottomLocal, _longLocal);
                var slide = at - (RunnerWeaponAim.StaffGripFromButt * length);
                _prop.position += hand.TransformDirection(_longLocal * slide);
                _tipLocal += _longLocal * slide;
            }

            var sign = 1f;
            if (mesh.isReadable)
            {
                // Mid-bow vertices (the riser) sit ahead of the tips' vertices (on the string side's far side).
                var verts = mesh.vertices;
                var li = order[0];
                var di = order[1];
                float mid = 0f, ends = 0f;
                int nm = 0, ne = 0;
                foreach (var v in verts)
                {
                    var along = (v[li] - b.center[li]) / Mathf.Max(size[li], 1e-6f);
                    if (Mathf.Abs(along) < 0.15f)
                    {
                        mid += v[di];
                        nm++;
                    }
                    else if (Mathf.Abs(along) > 0.85f)
                    {
                        ends += v[di];
                        ne++;
                    }
                }

                if (nm > 0 && ne > 0 && (mid / nm) < (ends / ne))
                {
                    sign = -1f;
                }
            }

            _depthLocal = hand.InverseTransformDirection(meshT.TransformDirection(depthAx * sign)).normalized;
            _measured = true;
            return true;
        }

        private static Transform? FindProp(Transform hand)
        {
            foreach (var r in hand.GetComponentsInChildren<Renderer>(true))
            {
                var t = r.transform;
                while (t.parent != null && t.parent != hand)
                {
                    t = t.parent;
                }

                if (t.parent == hand && t.name.Contains("Idle_"))
                {
                    return t;
                }
            }

            return null;
        }

        private void Set(Transform t, Quaternion world)
        {
            foreach (var k in _kept)
            {
                if (k.T == t)
                {
                    k.Set(world);
                    return;
                }
            }
        }

        private sealed class Arm
        {
            private Arm(Transform upper, Transform lower, Transform hand)
            {
                Upper = upper;
                Lower = lower;
                Hand = hand;
            }

            public Transform Upper { get; }
            public Transform Lower { get; }
            public Transform Hand { get; }

            public float Length => Vector3.Distance(Upper.position, Lower.position) + Vector3.Distance(Lower.position, Hand.position);

            public static Arm? Find(Transform? upper, Transform? lower, Transform? hand) =>
                upper != null && lower != null && hand != null ? new Arm(upper, lower, hand) : null;

            /// <summary>Turns the upper arm and the forearm so the wrist reaches <paramref name="target"/>, the elbow toward <paramref name="pole"/>.</summary>
            public void Reach(Vector3 target, Vector3 pole, System.Action<Transform, Quaternion> set)
            {
                var a = Upper.position;
                var l1 = Vector3.Distance(a, Lower.position);
                var l2 = Vector3.Distance(Lower.position, Hand.position);
                RunnerTwoBoneIk.Solve(N(a), l1, l2, N(target), N(pole), out var e, out var wr);
                var elbow = new Vector3(e.X, e.Y, e.Z);
                var wrist = new Vector3(wr.X, wr.Y, wr.Z);
                set(Upper, Quaternion.FromToRotation(Lower.position - a, elbow - a) * Upper.rotation);
                set(Lower, Quaternion.FromToRotation(Hand.position - Lower.position, wrist - Lower.position) * Lower.rotation);
            }

            private static System.Numerics.Vector3 N(Vector3 v) => new System.Numerics.Vector3(v.x, v.y, v.z);
        }

        /// <summary>A bone this pose turns: its clip rotation is put back before each solve (unless the clip re-posed it).</summary>
        private sealed class Kept
        {
            private Quaternion _base;
            private Quaternion _applied;
            private bool _has;

            public Kept(Transform t)
            {
                T = t;
            }

            public Transform T { get; }

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

                if (!_has)
                {
                    _base = T.localRotation;
                    _has = true;
                }

                T.rotation = world;
                _applied = T.localRotation;
            }
        }
    }
}
