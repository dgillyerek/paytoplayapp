#nullable enable
using System;
using System.Numerics;
using Survival.Domain.Roster;

namespace Survival.Domain.Runner
{
    /// <summary>How a squad hero's weapon is posed in the runner (Derek 2026-10-27).</summary>
    public enum RunnerAimStyle
    {
        /// <summary>The hero plays its own attack take (Oakenshield's spear throw, Sir Aldric, every enemy).</summary>
        None,

        /// <summary>A staff held level, pointed down the road at the enemy while walking, kicking back on each shot.</summary>
        Staff,

        /// <summary>A bow held out at the enemy: the string hand draws over the fire interval and releases on the shot.</summary>
        Bow,
    }

    /// <summary>
    /// The runner's procedural weapon pose (Derek 2026-10-27: "rowan is just turning. it doesn't look like he's firing his
    /// weapon. characters with staffs the staff should be pointed toward the enemy parallel to the ground while walking.
    /// it should then kickback when firing"). The legs keep the walk clip; after the clips pose the bones the rig puts the
    /// weapon hand at a hold point in front of the chest, turns the weapon to the aim and solves the arms with
    /// <see cref="RunnerTwoBoneIk"/>. All distances are metres of a 1.8 m hero (the rig scales them by its height).
    /// </summary>
    public static class RunnerWeaponAim
    {
        public static RunnerAimStyle StyleFor(string rosterName) =>
            rosterName == LockedBattleRoster.Lyra.Name ? RunnerAimStyle.Staff
            : rosterName == LockedBattleRoster.Rowan.Name ? RunnerAimStyle.Bow
            : RunnerAimStyle.None;

        /// <summary>The heroes that hold a staff (Lyra; Oakenshield's thorn spear is thrown by his own take, not held).</summary>
        public static readonly string[] StaffHeroes = { LockedBattleRoster.Lyra.Name };

        // ---- staff (Lyra) ----

        /// <summary>The staff hand's hold point from its shoulder: forward, down and out to the side (right hand: +x).</summary>
        public static readonly Vector3 StaffHold = new Vector3(0.10f, -0.28f, 0.30f);

        /// <summary>
        /// Where the staff hand holds the shaft, as a share of its length from the butt: the walk clip carries the staff by
        /// its head, which held level would put most of it behind her; levelled it slides through the hand so the head
        /// leads, like a pointed quarterstaff.
        /// </summary>
        public const float StaffGripFromButt = 0.36f;

        /// <summary>The off hand grips the shaft this far ahead of the staff hand (two-handed, like a levelled quarterstaff).</summary>
        public const float StaffOffHandAlong = 0.36f;

        /// <summary>Kick: the staff slides back along its own axis by this much at the peak.</summary>
        public const float StaffRecoilMetres = 0.10f;

        /// <summary>Kick: the tip rises by this many degrees at the peak.</summary>
        public const float StaffTipUpDegrees = 8f;

        /// <summary>Kick: the chest gives (pitches back) by this many degrees at the peak.</summary>
        public const float ShoulderGiveDegrees = 4f;

        // ---- bow (Rowan) ----

        /// <summary>The bow hand's hold point from its shoulder (left hand: -x): the bow arm out at the enemy, near shoulder height.</summary>
        public static readonly Vector3 BowHold = new Vector3(-0.06f, 0.02f, 0.56f);

        /// <summary>The nock (string hand at rest on the string) behind the bow hand, along the aim.</summary>
        public const float NockBehind = 0.18f;

        /// <summary>Full draw: how far the string hand has come back from the nock toward the anchor (kept for the draw-length check).</summary>
        public const float FullDrawBehind = 0.58f;

        /// <summary>
        /// The full-draw anchor from the DRAW (right) shoulder, in (right, up, aim) metres: at the right cheek, in front of the
        /// face. It used to be the bow hand pulled 0.58 m straight back along the aim, which lands behind the bow-side
        /// (left) shoulder: the right hand had to cross behind the neck and the elbow wrapped up behind the head
        /// (Derek 2026-10-28: "his right arm is bent completely behind his head").
        /// </summary>
        public static readonly Vector3 FullDrawAnchor = new Vector3(-0.12f, 0.10f, 0.17f);

        /// <summary>Release: the string hand flies back and out past the anchor by this much.</summary>
        public const float ReleaseFollowThrough = 0.10f;

        /// <summary>Bow kick: the bow tips up by this many degrees on the release.</summary>
        public const float BowTipUpDegrees = 6f;

        /// <summary>
        /// The bow arm's elbow hint, per metre of design arm from its shoulder (<paramref name="r"/> the aim's right, <paramref name="u"/> up,
        /// <paramref name="f"/> the aim): out to the bow side (the left), down, and a touch back, so the near-straight bow arm never folds the wrong way.
        /// </summary>
        public static Vector3 BowElbowPole(Vector3 r, Vector3 u, Vector3 f) => (-r * 0.30f) - (u * 0.40f) - (f * 0.10f);

        /// <summary>The draw arm's elbow hint from its shoulder: out to the draw side and a touch down, so the elbow stays at or under shoulder height and never folds up behind the head.</summary>
        public static Vector3 DrawElbowPole(Vector3 r, Vector3 u, Vector3 f) => (r * 0.50f) - (u * 0.50f);

        /// <summary>
        /// Where the string (right) hand goes: from the nock behind the bow grip (<paramref name="draw"/> 0) to the cheek anchor
        /// in front of the face (1), flicking back and out by <paramref name="flick"/> metres on the release. <paramref name="k"/>
        /// scales design metres to the rig.
        /// </summary>
        public static Vector3 DrawHand(Vector3 grip, Vector3 drawShoulder, Vector3 r, Vector3 u, Vector3 f, float draw, float flick, float k)
        {
            var nock = grip - (f * (NockBehind * k));
            var a = FullDrawAnchor;
            var full = drawShoulder + (k * ((r * a.X) + (u * a.Y) + (f * a.Z)));
            var fly = Vector3.Normalize((-f * 0.6f) + (r * 0.8f)) * (ReleaseFollowThrough * k * flick);
            return Vector3.Lerp(nock, full, Math.Clamp(draw, 0f, 1f)) + fly;
        }

        /// <summary>
        /// Whether the bow's measured depth axis (riser side) should be flipped so turning the bow to face <paramref name="fwd"/> twists the hand the least.
        /// The riser side can't be read from a non-readable mesh, and a wrong guess spins the bow hand 180 degrees in the wrist (Derek 2026-10-27: arms looked backwards).
        /// Frames follow Unity's LookRotation (x = up cross z).
        /// </summary>
        public static bool FlipBowDepth(Vector3 depth, Vector3 longAxis, Vector3 fwd, Vector3 up)
        {
            return Twist(-depth, longAxis, fwd, up) > Twist(depth, longAxis, fwd, up) + 1e-4f;
        }

        // The trace of the rotation taking the (depth, long) frame onto the (fwd, up) frame: larger is a smaller turn.
        private static float Twist(Vector3 depth, Vector3 longAxis, Vector3 fwd, Vector3 up)
        {
            Frame(depth, longAxis, out var fx, out var fy, out var fz);
            Frame(fwd, up, out var tx, out var ty, out var tz);
            return Vector3.Dot(fx, tx) + Vector3.Dot(fy, ty) + Vector3.Dot(fz, tz);
        }

        private static void Frame(Vector3 z, Vector3 up, out Vector3 x, out Vector3 y, out Vector3 zn)
        {
            zn = Vector3.Normalize(z);
            x = Vector3.Normalize(Vector3.Cross(up, zn));
            y = Vector3.Cross(zn, x);
        }

        /// <summary>
        /// How far the string hand is drawn, 0 (on the string at the nock) to 1 (full draw at the jaw),
        /// <paramref name="sinceShot"/> seconds after the last release with shots every <paramref name="interval"/> seconds:
        /// at the release the hand is at the anchor (it flies back past it with the kick) and reaches forward to the string
        /// over the first 35% of the interval, draws over the next 50%, and holds full draw for the last 15%.
        /// </summary>
        public static float Draw(float sinceShot, float interval)
        {
            interval = Math.Max(interval, 0.05f);
            var start = DrawStart * interval;
            var full = DrawFull * interval;
            if (sinceShot <= start)
            {
                return 1f - Smooth(sinceShot / start);
            }

            if (sinceShot >= full)
            {
                return 1f;
            }

            return Smooth((sinceShot - start) / (full - start));
        }

        /// <summary>The draw starts at this share of the fire interval and reaches full draw at <see cref="DrawFull"/>.</summary>
        public const float DrawStart = 0.35f;
        public const float DrawFull = 0.85f;

        private static float Smooth(float x)
        {
            x = Math.Clamp(x, 0f, 1f);
            return x * x * (3f - (2f * x));
        }
    }

    /// <summary>
    /// The per-shot kick of a held weapon (Derek 2026-10-27): a fast snap back as the shot leaves, then an ease back to the
    /// hold. At slow fire it rises in <see cref="RiseSeconds"/>, falls most of the way by <see cref="SnapSeconds"/> and
    /// settles by <see cref="TotalSeconds"/>; at fast fire every phase shrinks so the kick always ends inside the fire
    /// interval, and two kicks never add (the newer and the older are maxed), so rapid fire does not jitter.
    /// </summary>
    public static class RunnerKick
    {
        public const float RiseSeconds = 0.035f;
        public const float SnapSeconds = 0.12f;
        public const float TotalSeconds = 0.38f;

        /// <summary>The kick ends by this share of the fire interval.</summary>
        public const float IntervalShare = 0.85f;

        /// <summary>Time scale for shots every <paramref name="interval"/> seconds (1 = full length).</summary>
        public static float TimeScale(float interval) => Math.Clamp(IntervalShare * Math.Max(interval, 0.01f) / TotalSeconds, 0.05f, 1f);

        /// <summary>
        /// Peak size for shots every <paramref name="interval"/> seconds: full at 0.5 s or slower, smaller for rapid fire
        /// (never under 60%), so a fast staff reads as a buzz of small kicks, not a shake.
        /// </summary>
        public static float Amplitude(float interval) => Math.Clamp(0.6f + (0.4f * ((interval - 0.2f) / 0.3f)), 0.6f, 1f);

        /// <summary>The kick's weight <paramref name="age"/> seconds after the shot, 0..1 times <see cref="Amplitude"/>.</summary>
        public static float Weight(float age, float interval)
        {
            if (age < 0f)
            {
                return 0f;
            }

            var s = TimeScale(interval);
            var rise = RiseSeconds * s;
            var snap = SnapSeconds * s;
            var total = TotalSeconds * s;
            float w;
            if (age < rise)
            {
                var x = age / rise;
                w = 1f - ((1f - x) * (1f - x));
            }
            else if (age < snap)
            {
                // The snap: from the peak down to 35% of it.
                var x = (age - rise) / (snap - rise);
                w = 1f - (0.65f * x * x * (3f - (2f * x)));
            }
            else if (age < total)
            {
                // The ease: the last 35% eased out to the hold.
                var x = (age - snap) / (total - snap);
                w = 0.35f * (1f - x) * (1f - x);
            }
            else
            {
                w = 0f;
            }

            return w * Amplitude(interval);
        }
    }

    /// <summary>One rig's kicks: the shot times, the measured fire interval and the weight now.</summary>
    public sealed class RunnerKickState
    {
        public const float DefaultInterval = 0.9f;

        private float _age = 10f;
        private float _previousAge = 10f;

        public float Interval { get; private set; } = DefaultInterval;

        /// <summary>Seconds since the last shot.</summary>
        public float SinceShot => _age;

        public void Fire()
        {
            if (_age < 5f)
            {
                // The gap since the last shot is the fire interval (clamped against a stray double fire or a long pause).
                Interval = Math.Clamp(_age, 0.15f, 2.5f);
            }

            _previousAge = _age;
            _age = 0f;
        }

        public void Advance(float dt)
        {
            _age += dt;
            _previousAge += dt;
        }

        public void Reset()
        {
            _age = 10f;
            _previousAge = 10f;
            Interval = DefaultInterval;
        }

        public float Weight => Math.Max(RunnerKick.Weight(_age, Interval), RunnerKick.Weight(_previousAge, Interval));
    }

    /// <summary>
    /// The two-bone (shoulder, elbow, wrist) solve: where the elbow and wrist go so the wrist reaches
    /// <paramref name="target"/> (or as close as the arm's length allows), with the elbow bent toward
    /// <paramref name="pole"/>. Plain projections only, so it is the same in Unity's left-handed space.
    /// </summary>
    public static class RunnerTwoBoneIk
    {
        public static void Solve(Vector3 root, float upper, float lower, Vector3 target, Vector3 pole, out Vector3 elbow, out Vector3 wrist)
        {
            var to = target - root;
            var reach = to.Length();
            var dir = reach > 1e-6f ? to / reach : Vector3.UnitY * -1f;
            var d = Math.Clamp(reach, Math.Abs(upper - lower) + 1e-4f, upper + lower - 1e-4f);
            var a = ((upper * upper) - (lower * lower) + (d * d)) / (2f * d);
            var h = MathF.Sqrt(Math.Max(0f, (upper * upper) - (a * a)));
            var p = pole - root;
            var n = p - (dir * Vector3.Dot(p, dir));
            if (n.LengthSquared() < 1e-10f)
            {
                // Pole on the reach line: bend down (or forward when reaching straight down).
                n = Vector3.UnitY * -1f - (dir * -dir.Y);
                if (n.LengthSquared() < 1e-10f)
                {
                    n = Vector3.UnitZ - (dir * dir.Z);
                }
            }

            n = Vector3.Normalize(n);
            elbow = root + (dir * a) + (n * h);
            wrist = root + (dir * d);
        }
    }
}
