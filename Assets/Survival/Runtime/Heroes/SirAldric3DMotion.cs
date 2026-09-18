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

            /// <summary>Hips stay squared to world +Z (screen TOP), not yawed to a side. |Z| allows a readable hip drop.</summary>
            public bool FacesTop => Math.Abs(Hips.Y) < 18f && Math.Abs(Hips.Z) < 16f;

            /// <summary>Walk/sheath: right hand stays near the hip (pendulum may go slightly +X), scabbard on +X.</summary>
            public bool SheathedOnCharacterRight => !SwordDrawn && ArmR.X > -50f && ArmR.X < 28f;

            /// <summary>
            /// Sword / right arm stay on the far / TOP side as a rest — not a dual hang toward camera.
            /// Walk allows the trailing arm to swing +X (contralateral pendulum); attack keeps both −X.
            /// </summary>
            public bool ArmsTowardTop =>
                SwordDrawn
                    ? ArmL.X < 0f && ArmR.X < 0f && ForeL.X <= 0f && ForeR.X < 0f
                    : ArmR.X < 28f && ForeR.X < 5f;

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

        // Four Game-view keys solved against WALK_GAIT_BAR rear poses on this hang −Y rig.
        // Root cause of f1e4770 FAIL: BVH Leg_L.X was 66° but a forward thigh put that flex
        // along the ground (11 cm lift) so high-rear still read as a straight toy-soldier.
        // Pass thigh is now slightly +X (back) so 70° knee lifts the foot ~25 cm.
        // u=0 pass L, 0.25 contact L, 0.50 pass R, 0.75 contact R.
        private static readonly float[] HipsY = { -6f, 10f, 6f, -10f };
        private static readonly float[] HipsZ = { 12f, 2f, -12f, 2f };
        private static readonly float[] SpineY = { 12f, -12f, -12f, 12f };
        private static readonly float[] UpLX = { 14f, -28f, 6f, 18f };
        private static readonly float[] LegL = { 70f, 14f, 12f, 22f };
        private static readonly float[] FootL = { 28f, -12f, -8f, 24f };
        private static readonly float[] UpRX = { 4f, 18f, 14f, -28f };
        private static readonly float[] LegR = { 10f, 16f, 70f, 12f };
        private static readonly float[] FootR = { -8f, 24f, 28f, -12f };
        private static readonly float[] ArmLX = { 34f, 30f, -30f, -32f };
        private static readonly float[] ArmRX = { -30f, -32f, 26f, 24f };

        private static Pose WalkPose(float loopT, float rootZ)
        {
            var u = Repeat(loopT / WalkPeriodSeconds, 1f);
            var hipsY = Sample(HipsY, u);
            var hipsZ = Sample(HipsZ, u);
            var spineY = Sample(SpineY, u);
            var armLX = Sample(ArmLX, u);
            var armRX = Sample(ArmRX, u);
            var bob = 0.012f + 0.018f * Math.Abs(MathF.Cos(u * (float)Math.PI * 2f));
            return new Pose(
                attacking: false,
                swordDrawn: false,
                rootZ,
                bob,
                hips: new Euler(0f, hipsY, hipsZ),
                spine: new Euler(5f, spineY, -hipsZ * 0.28f),
                chest: new Euler(2f, spineY * 0.6f, 0f),
                head: new Euler(6f, spineY * 0.25f, 0f),
                upLegL: new Euler(Sample(UpLX, u), 0f, -4f),
                legL: new Euler(Sample(LegL, u), 0f, 0f),
                footL: new Euler(Sample(FootL, u), 0f, 0f),
                upLegR: new Euler(Sample(UpRX, u), 0f, 4f),
                legR: new Euler(Sample(LegR, u), 0f, 0f),
                footR: new Euler(Sample(FootR, u), 0f, 0f),
                armL: new Euler(armLX, 0f, -16f),
                foreL: new Euler(-18f, 0f, 0f),
                armR: new Euler(armRX, 4f, 16f),
                foreR: new Euler(-22f, 0f, 0f),
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
