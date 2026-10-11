using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Lyra Theme A fitted/tighten pack + attack (Design 2026-10-08). Staff Cast: Arcane Bolt — two-handed cast turned left, left hand low on the shaft (staff parented to RightHand, LeftHand about 63 cm lower on the shaft f8-f21), bolt released at f12 at 9 m/s toward top of screen.
    /// Rest, walk and attack FBXs replaced; 20 mixamorig bones (no ToeBase); bones bound by name.
    /// Frames baked from Design attack_meta / work blend in Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class LyraAttack
    {
        public const string FileName = "LYRA_blenderig_attack.fbx";
        public const string FileMd5 = "64f42e009e4c1a4bdae49029f30f5265";
        public const string DesignDir = "design/survival-theme-a-fantasy/heroes/anim/lyra/attack_20261008";
        public const int ReleaseFrame = 12;
        public const bool InPlace = false;

        /// <summary>Prop FBXs copied next to the character FBXs, with Design MD5s.</summary>
        public static readonly string[] PropFileNames =
        {
            "LYRA_staff.fbx"
        };

        public static readonly string[] PropFileMd5s =
        {
            "43aef847341f2e86f88a3020b32bae72"
        };

        public static readonly BlenderRigAttackSpec Spec = new BlenderRigAttackSpec(
            "Lyra",
            FileName,
            FileMd5,
            DesignDir,
            ReleaseFrame,
            InPlace,
            new[] { "mixamorig:Hips", "mixamorig:Head", "mixamorig:LeftHand", "mixamorig:RightHand" },
            new[] { 0.02337f, 1.04778f, 0.00552f, 0f, 1.665f, 0f, -0.28053f, 1.01305f, 0.09289f, 0.49385f, 1.25844f, 0.07523f },
            new[]
            {
                new AttackTrack(
                    "LYRA_staff",
                    AttackTrackKind.Held,
                    "LYRA_staff.fbx",
                    "mixamorig:RightHand",
                    0,
                    0,
                    0f,
                    AttackVfxShape.None,
                    0f,
                    0f,
                    new float[0],
                    new float[0],
                    new float[0],
                    new[] { 0.27652f, 1.88927f, 0.11727f },
                    new[]
                    {
                        new AttackFrame(0.54468f, 1.2833f, 0.13197f, 0.01582f, -0.00068f, -0.04293f, 0.99895f, 1f, true),
                        new AttackFrame(0.54017f, 1.28196f, 0.14976f, 0.01673f, -0.01753f, -0.04265f, 0.9988f, 1f, true),
                        new AttackFrame(0.52484f, 1.26886f, 0.20705f, 0.01856f, -0.06681f, -0.06814f, 0.99526f, 1f, true),
                        new AttackFrame(0.48663f, 1.23946f, 0.29943f, 0.02724f, -0.14463f, -0.13568f, 0.97976f, 1f, true),
                        new AttackFrame(0.41118f, 1.20233f, 0.40438f, 0.05229f, -0.2413f, -0.22779f, 0.94189f, 1f, true),
                        new AttackFrame(0.29806f, 1.16709f, 0.49455f, 0.09723f, -0.34238f, -0.32435f, 0.87643f, 1f, true),
                        new AttackFrame(0.16755f, 1.14159f, 0.54929f, 0.15579f, -0.43072f, -0.40741f, 0.79008f, 1f, true),
                        new AttackFrame(0.05295f, 1.12872f, 0.56743f, 0.21311f, -0.49271f, -0.46739f, 0.7024f, 1f, true),
                        new AttackFrame(-0.01416f, 1.12595f, 0.56649f, 0.25075f, -0.52217f, -0.50474f, 0.64007f, 1f, true),
                        new AttackFrame(-0.0325f, 1.13142f, 0.55448f, 0.25956f, -0.53086f, -0.51656f, 0.61966f, 1f, true),
                        new AttackFrame(-0.0325f, 1.13627f, 0.54429f, 0.25685f, -0.5331f, -0.51424f, 0.62079f, 1f, true),
                        new AttackFrame(-0.0325f, 1.13951f, 0.53413f, 0.25504f, -0.5346f, -0.51269f, 0.62154f, 1f, true),
                        new AttackFrame(-0.0325f, 1.11038f, 0.59483f, 0.27123f, -0.52099f, -0.52651f, 0.61465f, 1f, true),
                        new AttackFrame(-0.0325f, 1.112f, 0.59233f, 0.27033f, -0.52176f, -0.52575f, 0.61504f, 1f, true),
                        new AttackFrame(-0.0325f, 1.11361f, 0.58983f, 0.26944f, -0.52252f, -0.52499f, 0.61543f, 1f, true),
                        new AttackFrame(-0.0325f, 1.11523f, 0.58657f, 0.26854f, -0.52329f, -0.52423f, 0.61582f, 1f, true),
                        new AttackFrame(-0.0325f, 1.11685f, 0.58331f, 0.26765f, -0.52405f, -0.52347f, 0.61621f, 1f, true),
                        new AttackFrame(-0.0325f, 1.11847f, 0.58004f, 0.26675f, -0.52481f, -0.52271f, 0.6166f, 1f, true),
                        new AttackFrame(-0.0325f, 1.12009f, 0.57677f, 0.26585f, -0.52557f, -0.52194f, 0.61699f, 1f, true),
                        new AttackFrame(-0.0325f, 1.12333f, 0.57071f, 0.26406f, -0.52708f, -0.52041f, 0.61776f, 1f, true),
                        new AttackFrame(-0.0325f, 1.12656f, 0.56463f, 0.26226f, -0.5286f, -0.51887f, 0.61853f, 1f, true),
                        new AttackFrame(-0.00548f, 1.12697f, 0.56679f, 0.24737f, -0.51653f, -0.51042f, 0.64146f, 1f, true),
                        new AttackFrame(0.06728f, 1.13001f, 0.56601f, 0.20891f, -0.48125f, -0.4841f, 0.70029f, 1f, true),
                        new AttackFrame(0.17072f, 1.13939f, 0.54806f, 0.15784f, -0.42361f, -0.43699f, 0.77761f, 1f, true),
                        new AttackFrame(0.28475f, 1.1579f, 0.50235f, 0.10588f, -0.34738f, -0.36879f, 0.85563f, 1f, true),
                        new AttackFrame(0.38782f, 1.18528f, 0.42917f, 0.063f, -0.26097f, -0.28503f, 0.92015f, 1f, true),
                        new AttackFrame(0.46443f, 1.21771f, 0.34028f, 0.03475f, -0.17552f, -0.19691f, 0.96396f, 1f, true),
                        new AttackFrame(0.51056f, 1.24895f, 0.25311f, 0.02104f, -0.10134f, -0.11844f, 0.98755f, 1f, true),
                        new AttackFrame(0.53271f, 1.27241f, 0.18368f, 0.01714f, -0.04533f, -0.06321f, 0.99682f, 1f, true),
                        new AttackFrame(0.54181f, 1.28243f, 0.14358f, 0.01642f, -0.01166f, -0.04275f, 0.99888f, 1f, true),
                        new AttackFrame(0.54468f, 1.2833f, 0.13197f, 0.01582f, -0.00068f, -0.04293f, 0.99895f, 1f, true)
                    },
                    "staff grip in RightHand (constant offset)"),
                new AttackTrack(
                    "LYRA_arcane_bolt_vfx",
                    AttackTrackKind.Projectile,
                    "",
                    "",
                    0,
                    12,
                    9f,
                    AttackVfxShape.Orb,
                    0.338f,
                    1.03f,
                    new[] { 0.93092f, 0.96398f, 1f },
                    new[] { 0.48453f, 0.70141f, 1f },
                    new[] { 1f, 0.86504f, 0.58383f },
                    new[] { 0.338f, 0.338f, 1.03096f },
                    new[]
                    {
                        new AttackFrame(0.16628f, 1.268f, 1.05456f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16628f, 1.268f, 1.05456f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16628f, 1.268f, 1.05456f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16628f, 1.268f, 1.05456f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16628f, 1.268f, 1.05456f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16628f, 1.268f, 1.05456f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16628f, 1.268f, 1.05456f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16628f, 1.268f, 1.05456f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16628f, 1.268f, 1.05456f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16628f, 1.268f, 1.05456f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16628f, 1.268f, 1.05456f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16628f, 1.268f, 1.05456f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16628f, 1.268f, 1.05456f, 0f, 0f, 0f, 1f, 0.35f, true),
                        new AttackFrame(0.16628f, 1.268f, 1.35456f, 0f, 0f, 0f, 1f, 0.56667f, true),
                        new AttackFrame(0.16628f, 1.268f, 1.65456f, 0f, 0f, 0f, 1f, 0.78333f, true),
                        new AttackFrame(0.16628f, 1.268f, 1.95456f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16628f, 1.268f, 2.25456f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16628f, 1.268f, 2.55456f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16628f, 1.268f, 2.85456f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16628f, 1.268f, 3.15456f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16628f, 1.268f, 3.45456f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16628f, 1.268f, 3.75456f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16628f, 1.268f, 4.05456f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16628f, 1.268f, 4.35456f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16628f, 1.268f, 4.65456f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16628f, 1.268f, 4.95456f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16628f, 1.268f, 5.25456f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16628f, 1.268f, 5.55456f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16628f, 1.268f, 5.85456f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16628f, 1.268f, 6.15456f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16628f, 1.268f, 6.45456f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "arcane bolt spawns at staff crystal, travels character-forward (-Y Blender)")
            });
    }
}
