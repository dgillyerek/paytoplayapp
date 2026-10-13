using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Oakenshield thorn spear attack on the fresh rig (2026-10-12). Beats carried over from #48: feet planted, spear conjured in
    /// RightHand f3-8 (scales up), cocked beside the right shoulder at f10 with the chest turned 26 deg to her right, RightHand held at
    /// its f10 rotation f8-14 so the spear keeps pointing forward while the arm whips over, thrown at f14 (11 m/s, character-forward),
    /// follow-through f20, back to rest by f30. Head stays forward (Head bone kept at its rest orientation every frame).
    /// Held frames = RightHand grip 7 cm along the hand, bound at f10. Frames baked in Unity convention (character forward = +Z).
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class OakenshieldAttack
    {
        public const string FileName = "OAKENSHIELD_blenderig_attack.fbx";
        public const string FileMd5 = "0a3b1ea6e7a8b2b805fbe28c52731ff6";
        public const string DesignDir = "design/survival-theme-a-fantasy/heroes/anim/oakenshield/fresh_20261012";
        public const int ReleaseFrame = 14;
        public const bool InPlace = false;

        /// <summary>Prop FBXs copied next to the character FBXs (thorn spear from #48 attack_20261007, unchanged).</summary>
        public static readonly string[] PropFileNames =
        {
            "OAKENSHIELD_thornspear.fbx"
        };

        public static readonly string[] PropFileMd5s =
        {
            "0b7ebca1eb1946a850320fc44e65c1f5"
        };

        public static readonly BlenderRigAttackSpec Spec = new BlenderRigAttackSpec(
            "Oakenshield",
            FileName,
            FileMd5,
            DesignDir,
            ReleaseFrame,
            InPlace,
            new[] { "mixamorig:Hips", "mixamorig:Head", "mixamorig:LeftHand", "mixamorig:RightHand" },
            new[] { 0f, 0.95f, 0f, 0f, 1.455f, 0f, -0.48f, 0.945f, 0.02f, 0.48f, 0.945f, 0.02f },
            new[]
            {
                new AttackTrack(
                    "OAKENSHIELD_thornspear",
                    AttackTrackKind.Held,
                    "OAKENSHIELD_thornspear.fbx",
                    "mixamorig:RightHand",
                    10,
                    0,
                    0f,
                    AttackVfxShape.None,
                    0f,
                    0f,
                    new float[0],
                    new float[0],
                    new float[0],
                    new[] { 0.12781f, 0.13377f, 1.40074f },
                    new[]
                    {
                        new AttackFrame(0.36195f, 0.79451f, 0.04801f, -0.08661f, -0.16955f, -0.95129f, 0.24248f, 0.05f, false),
                        new AttackFrame(0.37685f, 0.80674f, 0.0878f, -0.07716f, -0.22412f, -0.93784f, 0.25352f, 0.05f, false),
                        new AttackFrame(0.40829f, 0.87156f, 0.20473f, -0.03941f, -0.40216f, -0.86879f, 0.28623f, 0.05f, false),
                        new AttackFrame(0.4109f, 1.026f, 0.32607f, 0.02721f, -0.64089f, -0.69538f, 0.32397f, 0.05f, true),
                        new AttackFrame(0.39005f, 1.14719f, 0.36262f, 0.07213f, -0.76136f, -0.54528f, 0.34323f, 0.24f, true),
                        new AttackFrame(0.38167f, 1.18529f, 0.36791f, 0.08532f, -0.79033f, -0.49626f, 0.34903f, 0.43f, true),
                        new AttackFrame(0.43073f, 1.24144f, 0.32122f, 0.07169f, -0.65174f, -0.36482f, 0.66106f, 0.62f, true),
                        new AttackFrame(0.47631f, 1.38778f, 0.16939f, 0.00374f, -0.20433f, -0.07567f, 0.97597f, 0.81f, true),
                        new AttackFrame(0.48375f, 1.54031f, -0.03023f, -0.05968f, 0f, 0f, 0.99822f, 1f, true),
                        new AttackFrame(0.45513f, 1.61695f, -0.16907f, -0.05968f, 0f, 0f, 0.99822f, 1f, true),
                        new AttackFrame(0.43787f, 1.63545f, -0.21322f, -0.05968f, 0f, 0f, 0.99822f, 1f, true),
                        new AttackFrame(0.44501f, 1.66965f, -0.11713f, -0.05968f, 0f, 0f, 0.99822f, 1f, true),
                        new AttackFrame(0.41541f, 1.69566f, 0.16139f, -0.05968f, 0f, 0f, 0.99822f, 1f, true),
                        new AttackFrame(0.3208f, 1.61516f, 0.38317f, -0.05968f, 0f, 0f, 0.99822f, 1f, true),
                        new AttackFrame(0.274f, 1.57437f, 0.44063f, -0.05968f, 0f, 0f, 0.99822f, 1f, false),
                        new AttackFrame(0.28456f, 1.54757f, 0.47133f, 0.02951f, -0.11193f, -0.07944f, 0.9901f, 1f, false),
                        new AttackFrame(0.29122f, 1.43964f, 0.54874f, 0.20992f, -0.38228f, -0.30232f, 0.84758f, 1f, false),
                        new AttackFrame(0.24305f, 1.24069f, 0.596f, 0.29962f, -0.61027f, -0.57863f, 0.45053f, 1f, false),
                        new AttackFrame(0.17399f, 1.06946f, 0.55294f, 0.2487f, -0.6253f, -0.73024f, 0.11787f, 1f, false),
                        new AttackFrame(0.14098f, 0.98003f, 0.49665f, 0.19035f, -0.5769f, -0.79333f, 0.03969f, 1f, false),
                        new AttackFrame(0.12953f, 0.95268f, 0.47308f, 0.17049f, -0.55512f, -0.81397f, 0.01517f, 1f, false),
                        new AttackFrame(0.13575f, 0.94476f, 0.4655f, 0.1634f, -0.54613f, -0.82141f, 0.0181f, 1f, false),
                        new AttackFrame(0.15335f, 0.92328f, 0.44322f, 0.14355f, -0.52043f, -0.8413f, 0.02745f, 1f, false),
                        new AttackFrame(0.18098f, 0.89239f, 0.40565f, 0.11298f, -0.4793f, -0.86918f, 0.04508f, 1f, false),
                        new AttackFrame(0.21643f, 0.85798f, 0.35252f, 0.07455f, -0.425f, -0.8992f, 0.07248f, 1f, false),
                        new AttackFrame(0.25552f, 0.82732f, 0.28663f, 0.03277f, -0.36267f, -0.92499f, 0.10854f, 1f, false),
                        new AttackFrame(0.29265f, 0.80613f, 0.2154f, -0.00703f, -0.30026f, -0.94219f, 0.14856f, 1f, false),
                        new AttackFrame(0.32311f, 0.79568f, 0.14877f, -0.04055f, -0.24576f, -0.95042f, 0.18616f, 1f, false),
                        new AttackFrame(0.34478f, 0.79301f, 0.0953f, -0.06554f, -0.20443f, -0.95246f, 0.21616f, 1f, false),
                        new AttackFrame(0.3576f, 0.79378f, 0.06047f, -0.08113f, -0.17859f, -0.95186f, 0.23557f, 1f, false),
                        new AttackFrame(0.36195f, 0.79451f, 0.04801f, -0.08661f, -0.16955f, -0.95129f, 0.24248f, 1f, false)
                    },
                    "thorn spear conjured f3-8 in RightHand, thrown at release"),
                new AttackTrack(
                    "OAKENSHIELD_thornspear_inflight",
                    AttackTrackKind.Projectile,
                    "OAKENSHIELD_thornspear.fbx",
                    "",
                    0,
                    14,
                    11f,
                    AttackVfxShape.Trail,
                    0.05f,
                    1.4f,
                    new[] { 1f, 0.86504f, 0.58383f },
                    new[] { 0.2478f, 0.43663f, 0.88083f },
                    new[] { 1f, 0.86504f, 0.58383f },
                    new[] { 0.12781f, 0.13377f, 1.40074f },
                    new[]
                    {
                        new AttackFrame(0.274f, 1.57437f, 0.44063f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.274f, 1.57437f, 0.44063f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.274f, 1.57437f, 0.44063f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.274f, 1.57437f, 0.44063f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.274f, 1.57437f, 0.44063f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.274f, 1.57437f, 0.44063f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.274f, 1.57437f, 0.44063f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.274f, 1.57437f, 0.44063f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.274f, 1.57437f, 0.44063f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.274f, 1.57437f, 0.44063f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.274f, 1.57437f, 0.44063f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.274f, 1.57437f, 0.44063f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.274f, 1.57437f, 0.44063f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.274f, 1.57437f, 0.44063f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.274f, 1.57437f, 0.44063f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.274f, 1.57437f, 0.8073f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.274f, 1.57437f, 1.17396f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.274f, 1.57437f, 1.54063f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.274f, 1.57437f, 1.9073f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.274f, 1.57437f, 2.27396f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.274f, 1.57437f, 2.64063f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.274f, 1.57437f, 3.0073f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.274f, 1.57437f, 3.37396f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.274f, 1.57437f, 3.74063f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.274f, 1.57437f, 4.1073f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.274f, 1.57437f, 4.47396f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.274f, 1.57437f, 4.84063f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.274f, 1.57437f, 5.2073f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.274f, 1.57437f, 5.57396f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.274f, 1.57437f, 5.94063f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.274f, 1.57437f, 6.3073f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "thrown thorn spear, travels character-forward")
            });
    }
}
