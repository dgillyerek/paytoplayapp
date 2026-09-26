namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Derek OVERRIDE: draw → forward strike authored on the existing walk Humanoid.
    /// Character space = Unity Y-up, +Z = march / World TOP / enemy (same SoT as
    /// <see cref="SirAldric3DMotion"/>). Hand offsets are hips-local metres.
    /// No Design attack FBX. No Path A. No Design PASS.
    /// </summary>
    public static class SirAldricHumanoidAttack
    {
        public const float Seconds = SirAldric3DMotion.AttackSeconds;

        /// <summary>Leftover AimChain math. Not SoT — SEP Meshy attack clip is played instead.</summary>
        public const string Authorship =
            "LEFTOVER (not SoT): Humanoid bone-aim clip on the walk Mixamo Avatar: GetBoneTransform + FromToRotation AimChain (no scale). Clip-only sword parented to RightHand. Play path is SEP Meshy Draw Slash Forward on the walk Humanoid.";

        public enum PhaseKind
        {
            Draw,
            Guard,
            Strike,
            Recover
        }

        public readonly struct Sample
        {
            public Sample(
                PhaseKind phase,
                bool swordDrawn,
                float handX,
                float handY,
                float handZ,
                float spineLeanDegrees)
            {
                Phase = phase;
                SwordDrawn = swordDrawn;
                HandX = handX;
                HandY = handY;
                HandZ = handZ;
                SpineLeanDegrees = spineLeanDegrees;
            }

            public PhaseKind Phase { get; }
            public bool SwordDrawn { get; }

            /// <summary>Hips-local character-right (+X).</summary>
            public float HandX { get; }

            /// <summary>Hips-local up (+Y).</summary>
            public float HandY { get; }

            /// <summary>Hips-local forward (+Z = World TOP after RearYaw 180).</summary>
            public float HandZ { get; }

            public float SpineLeanDegrees { get; }

            /// <summary>Right hand is past the hip, toward +Z / TOP.</summary>
            public bool StrikeTowardTop => Phase == PhaseKind.Strike && HandZ >= 0.45f;
        }

        public static Sample Evaluate(float attackT)
        {
            var u = SirAldric3DMotion.Clamp01(attackT / Seconds);
            if (u < 0.20f)
            {
                var k = SirAldric3DMotion.Smooth01(u / 0.20f);
                return new Sample(
                    PhaseKind.Draw,
                    swordDrawn: k > 0.35f,
                    // Rear PASS still: painted scabbard is character-right = viewer-right = +X.
                    handX: SirAldric3DMotion.Lerp(0.14f, 0.20f, k),
                    handY: SirAldric3DMotion.Lerp(-0.02f, -0.10f, k),
                    handZ: SirAldric3DMotion.Lerp(0.04f, -0.05f, k),
                    spineLeanDegrees: SirAldric3DMotion.Lerp(2f, 6f, k));
            }

            if (u < 0.38f)
            {
                var k = SirAldric3DMotion.Smooth01((u - 0.20f) / 0.18f);
                return new Sample(
                    PhaseKind.Guard,
                    swordDrawn: true,
                    handX: SirAldric3DMotion.Lerp(0.20f, 0.10f, k),
                    handY: SirAldric3DMotion.Lerp(-0.10f, 0.22f, k),
                    handZ: SirAldric3DMotion.Lerp(-0.05f, 0.22f, k),
                    spineLeanDegrees: SirAldric3DMotion.Lerp(6f, 10f, k));
            }

            if (u < 0.62f)
            {
                var k = SirAldric3DMotion.Smooth01((u - 0.38f) / 0.24f);
                return new Sample(
                    PhaseKind.Strike,
                    swordDrawn: true,
                    handX: SirAldric3DMotion.Lerp(0.10f, 0.04f, k),
                    handY: SirAldric3DMotion.Lerp(0.22f, 0.12f, k),
                    handZ: SirAldric3DMotion.Lerp(0.22f, 0.72f, k),
                    spineLeanDegrees: SirAldric3DMotion.Lerp(10f, 14f, k));
            }

            var r = SirAldric3DMotion.Smooth01((u - 0.62f) / 0.38f);
            return new Sample(
                PhaseKind.Recover,
                swordDrawn: r < 0.55f,
                handX: SirAldric3DMotion.Lerp(0.04f, 0.14f, r),
                handY: SirAldric3DMotion.Lerp(0.12f, -0.02f, r),
                handZ: SirAldric3DMotion.Lerp(0.72f, 0.04f, r),
                spineLeanDegrees: SirAldric3DMotion.Lerp(14f, 2f, r));
        }

        public static float StrikePeakSeconds => Seconds * 0.52f;
    }
}
