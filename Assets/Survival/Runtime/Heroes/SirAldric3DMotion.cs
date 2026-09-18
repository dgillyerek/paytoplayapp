using System;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Animator-ready 3D bone eulers for Sir Aldric.
    /// Unity Y-up. Character forward / march = world +Z = TOP of the high-angle rear Game view
    /// (camera sits at −Z, looking toward +Z; enemy is up-screen).
    /// Thigh −X swings the foot toward +Z (TOP); +X swings toward the camera (down-screen).
    /// Arm/forearm −X swings the hand/blade in front of the body toward +Z (TOP / enemy);
    /// +X hangs them toward the camera (FAIL). Character-right = +X = viewer-right from behind.
    /// </summary>
    public static class SirAldric3DMotion
    {
        public const float WalkPeriodSeconds = 1.00f;
        public const int WalkCyclesBeforeAttack = 2;
        public const float AttackSeconds = 1.40f;
        public const float MarchMetersPerSecond = 0.80f;

        public static float WalkBlockSeconds => WalkPeriodSeconds * WalkCyclesBeforeAttack;

        public static float LoopSeconds => WalkBlockSeconds + AttackSeconds;

        public readonly struct Euler
        {
            public Euler(float x, float y, float z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public float X { get; }
            public float Y { get; }
            public float Z { get; }
        }

        public readonly struct Pose
        {
            public Pose(
                bool attacking,
                bool swordDrawn,
                float rootZ,
                float rootY,
                Euler hips,
                Euler spine,
                Euler chest,
                Euler head,
                Euler upLegL,
                Euler legL,
                Euler footL,
                Euler upLegR,
                Euler legR,
                Euler footR,
                Euler armL,
                Euler foreL,
                Euler armR,
                Euler foreR,
                Euler handR,
                Euler sword)
            {
                Attacking = attacking;
                SwordDrawn = swordDrawn;
                RootZ = rootZ;
                RootY = rootY;
                Hips = hips;
                Spine = spine;
                Chest = chest;
                Head = head;
                UpLegL = upLegL;
                LegL = legL;
                FootL = footL;
                UpLegR = upLegR;
                LegR = legR;
                FootR = footR;
                ArmL = armL;
                ForeL = foreL;
                ArmR = armR;
                ForeR = foreR;
                HandR = handR;
                Sword = sword;
            }

            public bool Attacking { get; }
            public bool SwordDrawn { get; }
            public float RootZ { get; }
            public float RootY { get; }
            public Euler Hips { get; }
            public Euler Spine { get; }
            public Euler Chest { get; }
            public Euler Head { get; }
            public Euler UpLegL { get; }
            public Euler LegL { get; }
            public Euler FootL { get; }
            public Euler UpLegR { get; }
            public Euler LegR { get; }
            public Euler FootR { get; }
            public Euler ArmL { get; }
            public Euler ForeL { get; }
            public Euler ArmR { get; }
            public Euler ForeR { get; }
            public Euler HandR { get; }
            public Euler Sword { get; }

            /// <summary>Hips stay squared to world +Z (screen TOP), not yawed to a side.</summary>
            public bool FacesTop => Math.Abs(Hips.Y) < 18f && Math.Abs(Hips.Z) < 12f;

            /// <summary>Walk/sheath: right hand stays near the hip (pendulum may go slightly +X), scabbard on +X.</summary>
            public bool SheathedOnCharacterRight => !SwordDrawn && ArmR.X > -50f && ArmR.X < 18f;

            /// <summary>
            /// Sword / right arm stay on the far / TOP side as a rest — not a dual hang toward camera.
            /// Walk allows the trailing arm to swing +X (contralateral pendulum); attack keeps both −X.
            /// </summary>
            public bool ArmsTowardTop =>
                SwordDrawn
                    ? ArmL.X < 0f && ArmR.X < 0f && ForeL.X <= 0f && ForeR.X < 0f
                    : ArmR.X < 18f && ForeR.X < 5f;

            /// <summary>Loose contralateral pendulum: one arm back (+X / camera), the other forward (−X / TOP).</summary>
            public bool WalkArmPendulum =>
                !Attacking
                && ((ArmL.X > 8f && ArmR.X < -4f) || (ArmL.X < -6f && ArmR.X > 4f));

            /// <summary>Right arm / blade swinging toward world +Z (TOP of rear camera).</summary>
            public bool StrikeTowardTop => SwordDrawn && ArmR.X <= -80f && ArmR.X >= -175f;

            /// <summary>Thigh −X = foot toward +Z = TOP. Used to reject moonwalk / down-screen stride.</summary>
            public bool LeadLegTowardTop => UpLegL.X < -8f || UpLegR.X < -8f;

            /// <summary>Passing knee in the gait-bar band (~60–70°), not stiff and not a 90° cartoon march.</summary>
            public bool PassingKneeBent =>
                (LegL.X >= 52f && LegL.X <= 78f) || (LegR.X >= 52f && LegR.X <= 78f);

            /// <summary>Spine yaws opposite the pelvis (shoulder–hip counter-rotation).</summary>
            public bool ShoulderHipCounter =>
                Math.Abs(Hips.Y) < 2f || Math.Sign(Spine.Y) == -Math.Sign(Hips.Y);

            /// <summary>Pelvis rolls so the unweighted / passing hip drops.</summary>
            public bool HipDropOnPass => Math.Abs(Hips.Z) >= 5f;
        }

        public static Pose Evaluate(float timeSeconds)
        {
            var loopT = Repeat(timeSeconds, LoopSeconds);
            var attacking = loopT >= WalkBlockSeconds;
            var rootZ = loopT * MarchMetersPerSecond;
            return attacking
                ? AttackPose(loopT - WalkBlockSeconds, rootZ)
                : WalkPose(loopT, rootZ);
        }

        public static bool IsAttacking(float timeSeconds) =>
            Repeat(timeSeconds, LoopSeconds) >= WalkBlockSeconds;

        // Retargeted from CreativeInquiry BVH-Examples walk-cycle.bvh (Mixamo-style humanoid,
        // 42 frames @ 24fps, 1.75s), time-warped to 1.00s / 120 spm to match WALK_GAIT_BAR.
        // u=0 pass L, 0.25 contact L, 0.50 pass R, 0.75 contact R. Hang −Y; −X = TOP.
        // https://github.com/CreativeInquiry/BVH-Examples
        private static readonly float[] Bob = { 0.028f, 0.027f, 0.025f, 0.021f, 0.017f, 0.012f, 0.017f, 0.021f, 0.025f, 0.027f, 0.028f, 0.027f, 0.025f, 0.021f, 0.017f, 0.012f, 0.017f, 0.021f, 0.025f, 0.027f };
        private static readonly float[] HipsY = { -3.665f, -1.170f, 1.550f, 4.850f, 7.731f, 9.164f, 9.392f, 8.572f, 7.086f, 5.240f, 2.659f, -0.522f, -3.597f, -6.627f, -8.674f, -9.366f, -9.106f, -8.336f, -7.545f, -5.778f };
        private static readonly float[] HipsZ = { 7.345f, 7.048f, 5.486f, 2.888f, 0.446f, -0.526f, -0.665f, -1.376f, -3.347f, -5.206f, -5.968f, -4.838f, -2.639f, 0.291f, 1.477f, 1.844f, 2.331f, 2.318f, 3.372f, 5.976f };
        private static readonly float[] SpineY = { 4.444f, -6.727f, -7.459f, -7.612f, -7.441f, -7.486f, -7.299f, -6.440f, -5.116f, -2.872f, -1.460f, 6.016f, 8.442f, 10.170f, 11.111f, 10.850f, 9.181f, 5.798f, 2.590f, 1.342f };
        private static readonly float[] UpLX = { -16.208f, -22.635f, -25.264f, -25.288f, -23.607f, -22.233f, -21.451f, -20.034f, -16.924f, -12.922f, -8.011f, -3.490f, -0.194f, 2.962f, 6.640f, 9.814f, 10.664f, 8.028f, 3.749f, -6.587f };
        private static readonly float[] UpLZ = { -1.297f, -0.041f, 1.052f, 1.541f, 1.272f, 0.900f, 0.895f, 1.408f, 1.774f, 1.720f, 1.419f, 1.181f, 1.265f, 1.621f, 1.769f, 1.334f, 0.267f, -0.975f, -1.549f, -1.779f };
        private static readonly float[] LegL = { 65.911f, 61.733f, 48.831f, 29.206f, 11.845f, 7.064f, 12.270f, 20.857f, 23.986f, 22.512f, 18.937f, 16.014f, 15.030f, 15.685f, 17.177f, 19.957f, 25.565f, 33.527f, 41.852f, 57.840f };
        private static readonly float[] FootL = { 19.829f, 13.055f, 3.219f, -5.095f, -7.766f, -9.555f, -10.000f, -10.000f, -10.000f, -10.000f, -10.000f, -10.000f, -10.000f, -10.000f, -10.000f, -6.900f, -0.866f, 4.484f, 9.512f, 18.075f };
        private static readonly float[] UpRX = { -4.809f, -1.322f, 1.570f, 4.466f, 7.772f, 11.137f, 12.977f, 10.104f, 2.175f, -6.861f, -15.696f, -21.134f, -22.681f, -21.194f, -18.048f, -16.379f, -16.331f, -16.116f, -13.880f, -8.698f };
        private static readonly float[] UpRZ = { -1.595f, -1.260f, -1.068f, -1.299f, -1.936f, -2.526f, -2.599f, -1.215f, 1.007f, 2.407f, 2.719f, 2.233f, 1.756f, 1.557f, 1.507f, 1.397f, 1.056f, 0.153f, -0.796f, -1.575f };
        private static readonly float[] LegR = { 10.327f, 8.395f, 7.317f, 7.349f, 8.468f, 11.042f, 17.306f, 31.267f, 49.313f, 61.905f, 64.097f, 52.625f, 34.998f, 13.484f, 6.000f, 6.000f, 7.923f, 15.984f, 16.332f, 12.620f };
        private static readonly float[] FootR = { -10.000f, -10.000f, -10.000f, -10.000f, -10.000f, -9.206f, -4.301f, 2.262f, 8.900f, 13.019f, 10.753f, 2.167f, -4.875f, -8.293f, -9.210f, -10.000f, -10.000f, -10.000f, -10.000f, -10.000f };
        private static readonly float[] ArmLX = { 20.657f, 24.257f, 26.699f, 28.858f, 29.993f, 29.188f, 26.195f, 20.307f, 12.112f, 3.149f, -7.229f, -16.407f, -22.463f, -26.352f, -26.130f, -21.625f, -13.907f, -3.901f, 5.321f, 15.909f };
        private static readonly float[] ArmLZ = { 17.657f, 15.379f, 13.094f, 11.070f, 9.980f, 9.721f, 9.780f, 9.755f, 10.307f, 11.893f, 14.229f, 16.140f, 17.129f, 17.156f, 16.450f, 16.320f, 16.752f, 17.107f, 17.758f, 18.588f };
        private static readonly float[] ForeL = { -15.196f, -13.767f, -12.808f, -12.564f, -12.848f, -13.178f, -12.839f, -12.078f, -11.634f, -11.330f, -11.354f, -12.041f, -13.600f, -15.944f, -18.272f, -19.348f, -18.602f, -16.949f, -16.340f, -16.232f };
        private static readonly float[] ArmRX = { -9.906f, -14.138f, -16.869f, -18.876f, -19.356f, -17.604f, -13.548f, -7.220f, -0.291f, 6.120f, 12.772f, 16.000f, 16.000f, 16.000f, 16.000f, 16.000f, 16.000f, 11.073f, 4.560f, -4.311f };
        private static readonly float[] ArmRZ = { -11.216f, -13.244f, -14.717f, -15.443f, -15.351f, -15.261f, -15.680f, -16.429f, -16.982f, -17.481f, -18.128f, -18.688f, -19.027f, -18.804f, -18.245f, -17.300f, -14.905f, -11.624f, -9.797f, -9.497f };
        private static readonly float[] ForeR = { -21.387f, -26.717f, -31.035f, -32.000f, -32.000f, -32.000f, -30.582f, -26.622f, -24.302f, -23.984f, -23.729f, -23.241f, -23.176f, -23.164f, -22.902f, -22.393f, -21.284f, -19.746f, -18.477f, -18.084f };

        private static Pose WalkPose(float loopT, float rootZ)
        {
            var u = Repeat(loopT / WalkPeriodSeconds, 1f);
            var hipsY = Sample(HipsY, u);
            var hipsZ = Sample(HipsZ, u);
            var spineY = Sample(SpineY, u);
            var kneeL = Sample(LegL, u);
            var kneeR = Sample(LegR, u);
            var upLX = Sample(UpLX, u);
            var upRX = Sample(UpRX, u);
            var footL = Sample(FootL, u);
            var footR = Sample(FootR, u);
            // High-rear: passing thigh nearer vertical so the 65° knee actually lifts the foot.
            if (kneeL > 48f)
            {
                var k = (kneeL - 48f) / 20f;
                upLX *= 1f - 0.45f * k;
                footL += 8f * k;
            }

            if (kneeR > 48f)
            {
                var k = (kneeR - 48f) / 20f;
                upRX *= 1f - 0.45f * k;
                footR += 8f * k;
            }

            var armLX = Sample(ArmLX, u);
            var armRX = Sample(ArmRX, u);
            // −Z left / +Z right = off the ribs. Keep it a hang-and-swing, not a T-pose.
            var armLZ = armLX > 0f ? -18f : -14f;
            var armRZ = armRX < 0f ? 18f : 14f;
            return new Pose(
                attacking: false,
                swordDrawn: false,
                rootZ,
                Sample(Bob, u),
                hips: new Euler(0f, hipsY, hipsZ),
                spine: new Euler(5f, spineY, -hipsZ * 0.25f),
                chest: new Euler(2f, spineY * 0.55f, 0f),
                head: new Euler(6f, spineY * 0.22f, 0f),
                upLegL: new Euler(upLX, 0f, Sample(UpLZ, u)),
                legL: new Euler(kneeL, 0f, 0f),
                footL: new Euler(footL, 0f, 0f),
                upLegR: new Euler(upRX, 0f, Sample(UpRZ, u)),
                legR: new Euler(kneeR, 0f, 0f),
                footR: new Euler(footR, 0f, 0f),
                armL: new Euler(armLX, 0f, armLZ),
                foreL: new Euler(Sample(ForeL, u), 0f, 0f),
                armR: new Euler(armRX, 4f, armRZ),
                foreR: new Euler(Sample(ForeR, u), 0f, 0f),
                handR: new Euler(0f, 0f, 0f),
                sword: new Euler(-6f, 0f, 8f));
        }

        private static float Sample(float[] keys, float u)
        {
            var n = keys.Length;
            var x = Repeat(u, 1f) * n;
            var i0 = (int)MathF.Floor(x) % n;
            if (i0 < 0)
            {
                i0 += n;
            }

            var i1 = (i0 + 1) % n;
            var t = x - MathF.Floor(x);
            return Lerp(keys[i0], keys[i1], t);
        }

        private static Pose AttackPose(float attackT, float rootZ)
        {
            var u = Clamp01(attackT / AttackSeconds);
            float armX;
            float armY;
            float foreX;
            float lunge;
            float spineX;
            float drawnK;
            if (u < 0.18f)
            {
                var k = Smooth01(u / 0.18f);
                armX = Lerp(-16f, -85f, k);
                armY = Lerp(6f, 2f, k);
                foreX = Lerp(-22f, -8f, k);
                lunge = 0f;
                spineX = Lerp(4f, 10f, k);
                drawnK = k;
            }
            else if (u < 0.40f)
            {
                var k = Smooth01((u - 0.18f) / 0.22f);
                armX = Lerp(-85f, -155f, k);
                armY = Lerp(2f, 0f, k);
                foreX = Lerp(-8f, -28f, k);
                lunge = Lerp(0f, 0.04f, k);
                spineX = Lerp(10f, 14f, k);
                drawnK = 1f;
            }
            else if (u < 0.56f)
            {
                var k = Smooth01((u - 0.40f) / 0.16f);
                armX = Lerp(-155f, -118f, k);
                armY = Lerp(0f, 0f, k);
                foreX = Lerp(-28f, -6f, k);
                lunge = Lerp(0.04f, 0.10f, k);
                spineX = Lerp(14f, 6f, k);
                drawnK = 1f;
            }
            else if (u < 0.78f)
            {
                var k = Smooth01((u - 0.56f) / 0.22f);
                armX = Lerp(-118f, -48f, k);
                armY = Lerp(0f, 4f, k);
                foreX = Lerp(-6f, -16f, k);
                lunge = Lerp(0.10f, 0.02f, k);
                spineX = Lerp(6f, 2f, k);
                drawnK = 1f - k * 0.35f;
            }
            else
            {
                var k = Smooth01((u - 0.78f) / 0.22f);
                armX = Lerp(-48f, -16f, k);
                armY = Lerp(4f, 6f, k);
                foreX = Lerp(-16f, -22f, k);
                lunge = Lerp(0.02f, 0f, k);
                spineX = Lerp(2f, 4f, k);
                drawnK = 1f - k;
            }

            var drawn = drawnK > 0.22f;
            return new Pose(
                attacking: true,
                swordDrawn: drawn,
                rootZ,
                lunge,
                hips: new Euler(lunge * 20f, 0f, 0f),
                spine: new Euler(spineX, 0f, 0f),
                chest: new Euler(spineX * 0.4f, 0f, 0f),
                head: new Euler(8f, 0f, 0f),
                upLegL: new Euler(8f, 0f, 0f),
                legL: new Euler(12f, 0f, 0f),
                footL: new Euler(-6f, 0f, 0f),
                upLegR: new Euler(-6f, 0f, 0f),
                legR: new Euler(16f, 0f, 0f),
                footR: new Euler(-4f, 0f, 0f),
                armL: new Euler(-22f, 0f, 10f),
                foreL: new Euler(-18f, 0f, 0f),
                armR: new Euler(armX, armY, -8f),
                foreR: new Euler(foreX, 0f, 0f),
                handR: new Euler(drawn ? -12f : 0f, 0f, 0f),
                sword: new Euler(drawn ? -10f : -6f, 0f, drawn ? 0f : 8f));
        }

        public static float Repeat(float t, float length)
        {
            if (length <= 0f)
            {
                return 0f;
            }

            var r = t % length;
            return r < 0f ? r + length : r;
        }

        public static float Clamp01(float x)
        {
            if (x < 0f)
            {
                return 0f;
            }

            return x > 1f ? 1f : x;
        }

        public static float Lerp(float a, float b, float t) => a + (b - a) * t;

        public static float Smooth01(float x)
        {
            x = Clamp01(x);
            return x * x * (3f - 2f * x);
        }
    }
}
