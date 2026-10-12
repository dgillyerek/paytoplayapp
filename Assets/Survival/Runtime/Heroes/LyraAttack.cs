using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Lyra Theme A fitted/tighten pack + attack (Design 2026-10-08). Staff Cast: Arcane Bolt — starts front-on, right foot steps forward as the body turns side-on (80 deg left), head stays on the target, two-handed with the left hand low on the shaft (staff parented to RightHand, LeftHand about 61 cm lower on the shaft f8-f21), bolt released at f12 at 9 m/s toward top of screen.
    /// Rest, walk and attack FBXs replaced; 20 mixamorig bones (no ToeBase); bones bound by name.
    /// Frames baked from Design attack_meta / work blend in Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class LyraAttack
    {
        public const string FileName = "LYRA_blenderig_attack.fbx";
        public const string FileMd5 = "81021543e1ee1f7e7a330120d7bcf87f";
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
                        new AttackFrame(0.53271f, 1.27911f, 0.20577f, 0.01606f, -0.03806f, -0.07266f, 0.9965f, 1f, true),
                        new AttackFrame(0.47393f, 1.26733f, 0.38979f, 0.0252f, -0.13518f, -0.15042f, 0.97901f, 1f, true),
                        new AttackFrame(0.33425f, 1.2463f, 0.60614f, 0.05868f, -0.26496f, -0.25565f, 0.9279f, 1f, true),
                        new AttackFrame(0.12542f, 1.21185f, 0.76709f, 0.12447f, -0.39498f, -0.36332f, 0.83457f, 1f, true),
                        new AttackFrame(-0.09134f, 1.16446f, 0.83055f, 0.21518f, -0.49641f, -0.45032f, 0.71027f, 1f, true),
                        new AttackFrame(-0.26056f, 1.11409f, 0.82094f, 0.30985f, -0.55599f, -0.50481f, 0.58312f, 1f, true),
                        new AttackFrame(-0.36397f, 1.07536f, 0.78827f, 0.38278f, -0.57996f, -0.52968f, 0.48639f, 1f, true),
                        new AttackFrame(-0.3998f, 1.0604f, 0.77171f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.3998f, 1.0604f, 0.75171f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.3998f, 1.0604f, 0.73171f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.3998f, 1.0604f, 0.74171f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.3998f, 1.0604f, 0.82171f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.3998f, 1.0604f, 0.81171f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.3998f, 1.0604f, 0.79171f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.3998f, 1.0604f, 0.77171f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.3998f, 1.0604f, 0.77171f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.3998f, 1.0604f, 0.77171f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.3998f, 1.0604f, 0.77171f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.3998f, 1.0604f, 0.77171f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.3998f, 1.0604f, 0.77171f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.3998f, 1.0604f, 0.77171f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.36397f, 1.07536f, 0.78827f, 0.38278f, -0.57996f, -0.52968f, 0.48639f, 1f, true),
                        new AttackFrame(-0.26056f, 1.11409f, 0.82094f, 0.30985f, -0.55599f, -0.50481f, 0.58312f, 1f, true),
                        new AttackFrame(-0.09134f, 1.16446f, 0.83055f, 0.21518f, -0.49641f, -0.45032f, 0.71027f, 1f, true),
                        new AttackFrame(0.12542f, 1.21185f, 0.76709f, 0.12447f, -0.39498f, -0.36332f, 0.83457f, 1f, true),
                        new AttackFrame(0.33425f, 1.2463f, 0.60614f, 0.05868f, -0.26496f, -0.25565f, 0.9279f, 1f, true),
                        new AttackFrame(0.47393f, 1.26733f, 0.38979f, 0.0252f, -0.13518f, -0.15042f, 0.97901f, 1f, true),
                        new AttackFrame(0.53271f, 1.27911f, 0.20577f, 0.01606f, -0.03806f, -0.07266f, 0.9965f, 1f, true),
                        new AttackFrame(0.54468f, 1.2833f, 0.13197f, 0.01582f, -0.00068f, -0.04293f, 0.99895f, 1f, true),
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
                        new AttackFrame(-0.3917f, 1.10686f, 1.34467f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.3917f, 1.10686f, 1.34467f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.3917f, 1.10686f, 1.34467f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.3917f, 1.10686f, 1.34467f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.3917f, 1.10686f, 1.34467f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.3917f, 1.10686f, 1.34467f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.3917f, 1.10686f, 1.34467f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.3917f, 1.10686f, 1.34467f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.3917f, 1.10686f, 1.34467f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.3917f, 1.10686f, 1.34467f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.3917f, 1.10686f, 1.34467f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.3917f, 1.10686f, 1.34467f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.3917f, 1.10686f, 1.34467f, 0f, 0f, 0f, 1f, 0.35f, true),
                        new AttackFrame(-0.3917f, 1.10686f, 1.64467f, 0f, 0f, 0f, 1f, 0.56667f, true),
                        new AttackFrame(-0.3917f, 1.10686f, 1.94467f, 0f, 0f, 0f, 1f, 0.78333f, true),
                        new AttackFrame(-0.3917f, 1.10686f, 2.24467f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.3917f, 1.10686f, 2.54467f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.3917f, 1.10686f, 2.84467f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.3917f, 1.10686f, 3.14467f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.3917f, 1.10686f, 3.44467f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.3917f, 1.10686f, 3.74467f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.3917f, 1.10686f, 4.04467f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.3917f, 1.10686f, 4.34467f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.3917f, 1.10686f, 4.64467f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.3917f, 1.10686f, 4.94467f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.3917f, 1.10686f, 5.24467f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.3917f, 1.10686f, 5.54467f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.3917f, 1.10686f, 5.84467f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.3917f, 1.10686f, 6.14467f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.3917f, 1.10686f, 6.44467f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.3917f, 1.10686f, 6.74467f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "arcane bolt spawns at staff crystal, travels character-forward (-Y Blender)")
            });
    }
}
