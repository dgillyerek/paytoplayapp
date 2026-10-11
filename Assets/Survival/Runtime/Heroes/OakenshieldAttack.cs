using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Oakenshield Theme A attack (Design 2026-10-07 intent, re-authored on the rigid-leaves rig 2026-10-10: feet planted, spear
    /// conjured in RightHand f3-8, cocked beside the right shoulder at f10, kept pointing forward while the arm whips over, thrown
    /// at f14; held frames = RightHand grip 7 cm along the hand, bound at f10; projectile spawns at the f14 grip).
    /// Original add-on: Thorn Spear — spear conjured in RightHand f3–8 (scales up), thrown at f14 with a gold/leaf trail.
    /// New 0–30 frame Scene take at 30 fps on the existing blenderig (same bones, no rebind; rest and walk unchanged).
    /// Frames below are baked from Design attack_meta drivers in Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class OakenshieldAttack
    {
        public const string FileName = "OAKENSHIELD_blenderig_attack.fbx";
        /// <summary>ThemePack attack FBX: Design rigidleaves_20261010 OAKENSHIELD_rigidleaves_attack.fbx (cloth removed, 22 bones, re-rigged).</summary>
        public const string FileMd5 = "494efd02f36e84ec97253f3f00572fee";
        /// <summary>Original attack FBX in OakenshieldAttack.DesignDir (attack_20261007), unchanged.</summary>
        public const string DesignFileMd5 = "3fe681bc07658c519dbc1e903614c766";
        public const string DesignDir = "design/survival-theme-a-fantasy/heroes/anim/oakenshield/attack_20261007";
        public const int ReleaseFrame = 14;
        public const bool InPlace = false;

        /// <summary>Prop FBXs copied next to the character FBXs, with Design MD5s.</summary>
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
            new[] { 0.045f, 0.9f, 0f, -0.005f, 1.635f, -0.035f, -0.306f, 0.965f, 0.204f, 0.331f, 0.965f, -0.182f },
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
                        new AttackFrame(0.34483f, 0.89729f, -0.19312f, -0.03002f, 0.02915f, -0.93683f, 0.34727f, 0.05f, false),
                        new AttackFrame(0.34681f, 0.93224f, -0.19773f, 0.00745f, -0.03302f, -0.92474f, 0.37909f, 0.05f, false),
                        new AttackFrame(0.34964f, 0.99723f, -0.21256f, 0.06822f, -0.08529f, -0.90247f, 0.41667f, 0.05f, false),
                        new AttackFrame(0.35114f, 1.02758f, -0.22059f, 0.12441f, -0.10275f, -0.90461f, 0.39453f, 0.05f, true),
                        new AttackFrame(0.34811f, 1.04324f, -0.22966f, 0.17227f, -0.1053f, -0.91632f, 0.34582f, 0.24f, true),
                        new AttackFrame(0.34052f, 1.09599f, -0.25856f, 0.23275f, -0.09492f, -0.90519f, 0.3427f, 0.43f, true),
                        new AttackFrame(0.3442f, 1.18024f, -0.29961f, 0.27001f, -0.04202f, -0.82496f, 0.49474f, 0.62f, true),
                        new AttackFrame(0.35097f, 1.31473f, -0.34572f, 0.1963f, 0.06753f, -0.53514f, 0.81886f, 0.81f, true),
                        new AttackFrame(0.29169f, 1.44478f, -0.41607f, -0.1134f, 0.18092f, -0.06826f, 0.97455f, 1f, true),
                        new AttackFrame(0.27252f, 1.50602f, -0.43332f, -0.07944f, 0.06362f, -0.00759f, 0.99478f, 1f, true),
                        new AttackFrame(0.26632f, 1.52869f, -0.43806f, -0.05968f, 0f, 0f, 0.99822f, 1f, true),
                        new AttackFrame(0.31465f, 1.56374f, -0.34145f, -0.05349f, 0f, 0f, 0.99857f, 1f, true),
                        new AttackFrame(0.37456f, 1.63417f, -0.11059f, -0.03984f, 0f, 0f, 0.99921f, 1f, true),
                        new AttackFrame(0.36616f, 1.69507f, 0.131f, -0.02619f, 0f, 0f, 0.99966f, 1f, true),
                        new AttackFrame(0.33918f, 1.71949f, 0.23937f, -0.01999f, 0f, 0f, 0.9998f, 1f, false),
                        new AttackFrame(0.32251f, 1.68522f, 0.26856f, 0.03905f, -0.18353f, -0.00335f, 0.98223f, 1f, false),
                        new AttackFrame(0.25854f, 1.58447f, 0.33565f, 0.18302f, -0.60077f, -0.08716f, 0.77329f, 1f, false),
                        new AttackFrame(0.16982f, 1.44508f, 0.38165f, 0.25277f, -0.83256f, -0.31061f, 0.38273f, 1f, false),
                        new AttackFrame(0.10797f, 1.31468f, 0.41685f, 0.21246f, -0.79836f, -0.5357f, 0.17466f, 1f, false),
                        new AttackFrame(0.06411f, 1.21634f, 0.44252f, 0.14904f, -0.70485f, -0.69142f, 0.05399f, 1f, false),
                        new AttackFrame(0.04771f, 1.17705f, 0.45273f, 0.1162f, -0.65267f, -0.74844f, 0.01901f, 1f, false),
                        new AttackFrame(0.05823f, 1.17043f, 0.4381f, 0.12211f, -0.65321f, -0.74664f, 0.03052f, 1f, false),
                        new AttackFrame(0.08669f, 1.15148f, 0.39799f, 0.13505f, -0.64718f, -0.74771f, 0.062f, 1f, false),
                        new AttackFrame(0.12821f, 1.12123f, 0.33743f, 0.14772f, -0.62004f, -0.76296f, 0.10778f, 1f, false),
                        new AttackFrame(0.17762f, 1.0816f, 0.2607f, 0.15378f, -0.56045f, -0.79799f, 0.15956f, 1f, false),
                        new AttackFrame(0.22929f, 1.03666f, 0.17226f, 0.14684f, -0.46731f, -0.84657f, 0.20826f, 1f, false),
                        new AttackFrame(0.27758f, 0.99205f, 0.07857f, 0.12364f, -0.3518f, -0.89388f, 0.24885f, 1f, false),
                        new AttackFrame(0.32076f, 0.95305f, -0.01646f, 0.07937f, -0.2281f, -0.92413f, 0.29607f, 1f, false),
                        new AttackFrame(0.34569f, 0.92311f, -0.10471f, 0.02657f, -0.10901f, -0.93476f, 0.33709f, 1f, false),
                        new AttackFrame(0.34841f, 0.90399f, -0.16877f, -0.01382f, -0.01274f, -0.93673f, 0.34955f, 1f, false),
                        new AttackFrame(0.34483f, 0.89729f, -0.19312f, -0.03002f, 0.02915f, -0.93683f, 0.34727f, 1f, false)
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
                        new AttackFrame(0.33918f, 1.71949f, 0.23937f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.33918f, 1.71949f, 0.23937f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.33918f, 1.71949f, 0.23937f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.33918f, 1.71949f, 0.23937f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.33918f, 1.71949f, 0.23937f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.33918f, 1.71949f, 0.23937f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.33918f, 1.71949f, 0.23937f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.33918f, 1.71949f, 0.23937f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.33918f, 1.71949f, 0.23937f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.33918f, 1.71949f, 0.23937f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.33918f, 1.71949f, 0.23937f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.33918f, 1.71949f, 0.23937f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.33918f, 1.71949f, 0.23937f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.33918f, 1.71949f, 0.23937f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.33918f, 1.71949f, 0.23937f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.33918f, 1.71949f, 0.60604f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.33918f, 1.71949f, 0.9727f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.33918f, 1.71949f, 1.33937f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.33918f, 1.71949f, 1.70604f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.33918f, 1.71949f, 2.0727f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.33918f, 1.71949f, 2.43937f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.33918f, 1.71949f, 2.80604f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.33918f, 1.71949f, 3.1727f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.33918f, 1.71949f, 3.53937f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.33918f, 1.71949f, 3.90604f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.33918f, 1.71949f, 4.2727f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.33918f, 1.71949f, 4.63937f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.33918f, 1.71949f, 5.00604f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.33918f, 1.71949f, 5.3727f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.33918f, 1.71949f, 5.73937f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.33918f, 1.71949f, 6.10604f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "thrown thorn spear, travels character-forward")
            });
    }
}
