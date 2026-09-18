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
        public const float WalkPeriodSeconds = 0.80f;
        public const int WalkCyclesBeforeAttack = 2;
        public const float AttackSeconds = 1.40f;
        public const float MarchMetersPerSecond = 0.42f;

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

            /// <summary>Walk/sheath: right hand stays near the hip, not overhead, scabbard on +X.</summary>
            public bool SheathedOnCharacterRight => !SwordDrawn && ArmR.X > -50f && ArmR.X < 5f;

            /// <summary>Hands/blade on the far side (world +Z / TOP), not toward the camera (−Z).</summary>
            public bool ArmsTowardTop => ArmL.X < 0f && ArmR.X < 0f && ForeL.X <= 0f && ForeR.X < 0f;

            /// <summary>Right arm / blade swinging toward world +Z (TOP of rear camera).</summary>
            public bool StrikeTowardTop => SwordDrawn && ArmR.X <= -80f && ArmR.X >= -175f;

            /// <summary>Thigh −X = foot toward +Z = TOP. Used to reject moonwalk / down-screen stride.</summary>
            public bool LeadLegTowardTop => UpLegL.X < -8f || UpLegR.X < -8f;

            /// <summary>Passing / lifted leg shows a deep knee (not a straight-leg pivot).</summary>
            public bool PassingKneeBent => LegL.X >= 55f || LegR.X >= 55f;
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

        private static Pose WalkPose(float loopT, float rootZ)
        {
            var phase = (float)(loopT / WalkPeriodSeconds * (Math.PI * 2.0));
            var step = MathF.Sin(phase);
            var cos = MathF.Cos(phase);
            var bob = 0.030f * Math.Abs(step);
            var sway = 5.5f * step;
            // −X thigh = toward world +Z = TOP of Game view (not toward camera).
            // Swing/pass (cos) lifts the moving leg; trail (sin) keeps some flex on the back leg.
            var swingL = Math.Max(0f, cos);
            var swingR = Math.Max(0f, -cos);
            var trailL = Math.Max(0f, -step);
            var trailR = Math.Max(0f, step);
            var leftX = -38f * step - 32f * swingL;
            var rightX = 38f * step - 32f * swingR;
            var kneeL = 16f + 74f * swingL + 36f * trailL;
            var kneeR = 16f + 74f * swingR + 36f * trailR;
            return new Pose(
                attacking: false,
                swordDrawn: false,
                rootZ,
                bob,
                hips: new Euler(0f, sway * 0.15f, 0f),
                spine: new Euler(4f, sway, 0f),
                chest: new Euler(0f, sway * 0.4f, 0f),
                head: new Euler(6f, 0f, 0f),
                upLegL: new Euler(leftX, 0f, -18f * swingL),
                legL: new Euler(kneeL, 0f, 0f),
                footL: new Euler(-6f - 18f * swingL - 8f * trailL, 0f, 0f),
                upLegR: new Euler(rightX, 0f, 18f * swingR),
                legR: new Euler(kneeR, 0f, 0f),
                footR: new Euler(-6f - 18f * swingR - 8f * trailR, 0f, 0f),
                // Arm −X = in front toward TOP. Never +X (that hangs toward the camera).
                armL: new Euler(-20f - 14f * step, 0f, 8f),
                foreL: new Euler(-18f - 10f * Math.Max(0f, step), 0f, 0f),
                armR: new Euler(-16f - 8f * step, 6f, -8f),
                foreR: new Euler(-22f, 0f, 0f),
                handR: new Euler(0f, 0f, 0f),
                sword: new Euler(-6f, 0f, 8f));
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
