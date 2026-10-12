using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Lyra Theme A fitted/tighten pack + attack (Design 2026-10-08). Staff Cast: Arcane Bolt — whole body turns side-on (80 deg left) with stepped feet, two-handed with the left hand low on the shaft (staff parented to RightHand, LeftHand about 61 cm lower on the shaft f8-f21), bolt released at f12 at 9 m/s toward top of screen.
    /// Rest, walk and attack FBXs replaced; 20 mixamorig bones (no ToeBase); bones bound by name.
    /// Frames baked from Design attack_meta / work blend in Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class LyraAttack
    {
        public const string FileName = "LYRA_blenderig_attack.fbx";
        public const string FileMd5 = "916e0ab97866a084d4ccf1788db7ecbb";
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
                        new AttackFrame(0.52533f, 1.27776f, 0.18344f, 0.01606f, -0.03806f, -0.07266f, 0.9965f, 1f, true),
                        new AttackFrame(0.45467f, 1.26262f, 0.30759f, 0.0252f, -0.13518f, -0.15042f, 0.97901f, 1f, true),
                        new AttackFrame(0.31571f, 1.23792f, 0.44161f, 0.05868f, -0.26496f, -0.25565f, 0.9279f, 1f, true),
                        new AttackFrame(0.12765f, 1.20185f, 0.51872f, 0.12447f, -0.39498f, -0.36332f, 0.83457f, 1f, true),
                        new AttackFrame(-0.05485f, 1.15608f, 0.51288f, 0.21518f, -0.49641f, -0.45032f, 0.71027f, 1f, true),
                        new AttackFrame(-0.18738f, 1.10937f, 0.45333f, 0.30985f, -0.55599f, -0.50481f, 0.58312f, 1f, true),
                        new AttackFrame(-0.26132f, 1.07402f, 0.39f, 0.38278f, -0.57996f, -0.52968f, 0.48639f, 1f, true),
                        new AttackFrame(-0.28505f, 1.0604f, 0.36271f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.28505f, 1.0604f, 0.34271f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.28505f, 1.0604f, 0.32271f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.28505f, 1.0604f, 0.33271f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.28505f, 1.0604f, 0.41271f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.28505f, 1.0604f, 0.40271f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.28505f, 1.0604f, 0.38271f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.28505f, 1.0604f, 0.36271f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.28505f, 1.0604f, 0.36271f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.28505f, 1.0604f, 0.36271f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.28505f, 1.0604f, 0.36271f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.28505f, 1.0604f, 0.36271f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.28505f, 1.0604f, 0.36271f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.28505f, 1.0604f, 0.36271f, 0.41127f, -0.58492f, -0.53598f, 0.44883f, 1f, true),
                        new AttackFrame(-0.26132f, 1.07402f, 0.39f, 0.38278f, -0.57996f, -0.52968f, 0.48639f, 1f, true),
                        new AttackFrame(-0.18738f, 1.10937f, 0.45333f, 0.30985f, -0.55599f, -0.50481f, 0.58312f, 1f, true),
                        new AttackFrame(-0.05485f, 1.15608f, 0.51288f, 0.21518f, -0.49641f, -0.45032f, 0.71027f, 1f, true),
                        new AttackFrame(0.12765f, 1.20185f, 0.51872f, 0.12447f, -0.39498f, -0.36332f, 0.83457f, 1f, true),
                        new AttackFrame(0.31571f, 1.23792f, 0.44161f, 0.05868f, -0.26496f, -0.25565f, 0.9279f, 1f, true),
                        new AttackFrame(0.45467f, 1.26262f, 0.30759f, 0.0252f, -0.13518f, -0.15042f, 0.97901f, 1f, true),
                        new AttackFrame(0.52533f, 1.27776f, 0.18344f, 0.01606f, -0.03806f, -0.07266f, 0.9965f, 1f, true),
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
                        new AttackFrame(-0.27695f, 1.10686f, 0.93567f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.27695f, 1.10686f, 0.93567f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.27695f, 1.10686f, 0.93567f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.27695f, 1.10686f, 0.93567f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.27695f, 1.10686f, 0.93567f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.27695f, 1.10686f, 0.93567f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.27695f, 1.10686f, 0.93567f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.27695f, 1.10686f, 0.93567f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.27695f, 1.10686f, 0.93567f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.27695f, 1.10686f, 0.93567f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.27695f, 1.10686f, 0.93567f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.27695f, 1.10686f, 0.93567f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.27695f, 1.10686f, 0.93567f, 0f, 0f, 0f, 1f, 0.35f, true),
                        new AttackFrame(-0.27695f, 1.10686f, 1.23567f, 0f, 0f, 0f, 1f, 0.56667f, true),
                        new AttackFrame(-0.27695f, 1.10686f, 1.53567f, 0f, 0f, 0f, 1f, 0.78333f, true),
                        new AttackFrame(-0.27695f, 1.10686f, 1.83567f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.27695f, 1.10686f, 2.13567f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.27695f, 1.10686f, 2.43567f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.27695f, 1.10686f, 2.73567f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.27695f, 1.10686f, 3.03567f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.27695f, 1.10686f, 3.33567f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.27695f, 1.10686f, 3.63567f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.27695f, 1.10686f, 3.93567f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.27695f, 1.10686f, 4.23567f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.27695f, 1.10686f, 4.53567f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.27695f, 1.10686f, 4.83567f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.27695f, 1.10686f, 5.13567f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.27695f, 1.10686f, 5.43567f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.27695f, 1.10686f, 5.73567f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.27695f, 1.10686f, 6.03567f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.27695f, 1.10686f, 6.33567f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "arcane bolt spawns at staff crystal, travels character-forward (-Y Blender)")
            });
    }
}
