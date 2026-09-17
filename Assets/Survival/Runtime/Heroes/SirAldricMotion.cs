using System;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Deterministic rear-view pose from ONE locked master.
    /// Canvas +Y / bone +Y = TOP of screen = walk and attack direction.
    /// Sword rest is viewer-RIGHT hip. Attack draws, strikes TOP, then re-sheaths.
    /// Bones are deltas from the painted rest pose (identity = pixel-identical to the master).
    /// </summary>
    public static class SirAldricMotion
    {
        public const float WalkPeriodSeconds = 0.70f;
        public const int WalkCyclesBeforeAttack = 2;
        public const float AttackSeconds = 1.35f;

        public static float WalkBlockSeconds => WalkPeriodSeconds * WalkCyclesBeforeAttack;

        public static float LoopSeconds => WalkBlockSeconds + AttackSeconds;

        /// <summary>Opaque-bbox UV (origin bottom-left) for runtime crops of the locked rear master.</summary>
        public static class Layout
        {
            public static readonly NRect Torso = new(0.10f, 0.34f, 0.86f, 1.00f);
            public static readonly NRect LegL = new(0.12f, 0.00f, 0.54f, 0.46f);
            public static readonly NRect LegR = new(0.42f, 0.00f, 0.86f, 0.46f);
            public static readonly NRect Sword = new(0.64f, 0.00f, 0.98f, 0.54f);

            public static readonly NRect SwordPivot = new(0.30f, 0.86f, 0.30f, 0.86f);
            public static readonly NRect LegPivot = new(0.50f, 0.90f, 0.50f, 0.90f);
            public static readonly NRect TorsoPivot = new(0.50f, 0.18f, 0.50f, 0.18f);
        }

        public readonly struct NRect
        {
            public NRect(float xMin, float yMin, float xMax, float yMax)
            {
                XMin = xMin;
                YMin = yMin;
                XMax = xMax;
                YMax = yMax;
            }

            public float XMin { get; }
            public float YMin { get; }
            public float XMax { get; }
            public float YMax { get; }
            public float Width => XMax - XMin;
            public float Height => YMax - YMin;
            public float CenterX => (XMin + XMax) * 0.5f;
            public float CenterY => (YMin + YMax) * 0.5f;
        }

        public readonly struct Bone
        {
            public Bone(float x, float y, float rotZ, float scaleX = 1f, float scaleY = 1f)
            {
                X = x;
                Y = y;
                RotZ = rotZ;
                ScaleX = scaleX;
                ScaleY = scaleY;
            }

            public float X { get; }
            public float Y { get; }
            public float RotZ { get; }
            public float ScaleX { get; }
            public float ScaleY { get; }
        }

        public readonly struct Pose
        {
            public Pose(
                bool attacking,
                bool swordDrawn,
                float marchY,
                Bone root,
                Bone torso,
                Bone legL,
                Bone legR,
                Bone sword)
            {
                Attacking = attacking;
                SwordDrawn = swordDrawn;
                MarchY = marchY;
                Root = root;
                Torso = torso;
                LegL = legL;
                LegR = legR;
                Sword = sword;
            }

            public bool Attacking { get; }
            public bool SwordDrawn { get; }
            public float MarchY { get; }
            public Bone Root { get; }
            public Bone Torso { get; }
            public Bone LegL { get; }
            public Bone LegR { get; }
            public Bone Sword { get; }

            public bool FacesTop => Math.Abs(Root.RotZ) < 22f;
            public bool SheathedOnViewerRight => !SwordDrawn && Sword.X >= -0.02f;
            public bool StrikeTowardTop => SwordDrawn && Sword.RotZ >= 90f && Sword.RotZ <= 190f;
        }

        public static Pose Evaluate(float timeSeconds)
        {
            var loopT = Repeat(timeSeconds, LoopSeconds);
            var attacking = loopT >= WalkBlockSeconds;
            var marchY = MarchTowardTop(timeSeconds, attacking);
            return attacking
                ? AttackPose(loopT - WalkBlockSeconds, marchY)
                : WalkPose(loopT, marchY);
        }

        public static bool IsAttacking(float timeSeconds) =>
            Repeat(timeSeconds, LoopSeconds) >= WalkBlockSeconds;

        public static float MarchTowardTop(float timeSeconds, bool attacking)
        {
            var speed = attacking ? 0.018f : 0.11f;
            var u = Repeat(timeSeconds * speed, 1f);
            return 0.22f + 0.52f * u;
        }

        private static Pose WalkPose(float loopT, float marchY)
        {
            var phase = (float)(loopT / WalkPeriodSeconds * (Math.PI * 2.0));
            var step = MathF.Sin(phase);
            var bob = 0.034f * Math.Abs(MathF.Sin(phase));
            var squash = 1f - 0.045f * Math.Abs(MathF.Sin(phase));
            var sway = 6.5f * step;
            return new Pose(
                attacking: false,
                swordDrawn: false,
                marchY,
                root: new Bone(0f, bob, sway * 0.12f, 1f, squash),
                torso: new Bone(0f, bob * 0.40f, sway, 1f, 1f),
                legL: new Bone(-0.024f * step, 0.125f * step, 20f * step),
                legR: new Bone(0.024f * step, -0.125f * step, -20f * step),
                sword: new Bone(0.004f * step, 0.010f * step, 5f * step));
        }

        private static Pose AttackPose(float attackT, float marchY)
        {
            var u = Clamp01(attackT / AttackSeconds);
            float swordRot;
            float swordY;
            float swordX = 0f;
            float lunge;
            float torsoRot;
            if (u < 0.16f)
            {
                var k = Smooth01(u / 0.16f);
                swordRot = Lerp(0f, 42f, k);
                swordY = Lerp(0f, 0.07f, k);
                lunge = 0f;
                torsoRot = Lerp(0f, -7f, k);
            }
            else if (u < 0.36f)
            {
                var k = Smooth01((u - 0.16f) / 0.20f);
                swordRot = Lerp(42f, 118f, k);
                swordY = Lerp(0.07f, 0.16f, k);
                swordX = Lerp(0f, -0.03f, k);
                lunge = Lerp(0f, 0.035f, k);
                torsoRot = Lerp(-7f, -11f, k);
            }
            else if (u < 0.52f)
            {
                var k = Smooth01((u - 0.36f) / 0.16f);
                swordRot = Lerp(118f, 178f, k);
                swordY = Lerp(0.16f, 0.30f, k);
                swordX = Lerp(-0.03f, 0.01f, k);
                lunge = Lerp(0.035f, 0.08f, k);
                torsoRot = Lerp(-11f, 8f, k);
            }
            else if (u < 0.78f)
            {
                var k = Smooth01((u - 0.52f) / 0.26f);
                swordRot = Lerp(178f, 58f, k);
                swordY = Lerp(0.30f, 0.08f, k);
                swordX = Lerp(0.01f, 0.02f, k);
                lunge = Lerp(0.08f, 0.015f, k);
                torsoRot = Lerp(8f, -3f, k);
            }
            else
            {
                var k = Smooth01((u - 0.78f) / 0.22f);
                swordRot = Lerp(58f, 0f, k);
                swordY = Lerp(0.08f, 0f, k);
                swordX = Lerp(0.02f, 0f, k);
                lunge = Lerp(0.015f, 0f, k);
                torsoRot = Lerp(-3f, 0f, k);
            }

            var drawn = swordRot > 22f;
            return new Pose(
                attacking: true,
                swordDrawn: drawn,
                marchY,
                root: new Bone(0f, lunge, 0f, 1f, 1f - lunge * 0.35f),
                torso: new Bone(0f, lunge * 0.25f, torsoRot),
                legL: new Bone(-0.01f, 0.01f, 4f),
                legR: new Bone(0.012f, -0.004f, -5f),
                sword: new Bone(swordX, swordY, swordRot));
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
