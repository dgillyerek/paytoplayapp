using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Lyra Theme A fitted/tighten pack + attack (Design 2026-10-08). Staff Cast: Arcane Bolt — two-handed side-on cast (staff parented to RightHand, LeftHand joins the shaft f8-f20), bolt released at f12 at 9 m/s toward top of screen.
    /// Rest, walk and attack FBXs replaced; 20 mixamorig bones (no ToeBase); bones bound by name.
    /// Frames baked from Design attack_meta / work blend in Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class LyraAttack
    {
        public const string FileName = "LYRA_blenderig_attack.fbx";
        public const string FileMd5 = "2d6f799e62edbc395b89bad65525404e";
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
                        new AttackFrame(0.53184f, 1.2828f, 0.16812f, -0.00444f, -0.04253f, -0.06045f, 0.99726f, 1f, true),
                        new AttackFrame(0.48663f, 1.28259f, 0.25692f, -0.05209f, -0.15281f, -0.11183f, 0.98052f, 1f, true),
                        new AttackFrame(0.39876f, 1.28494f, 0.36203f, -0.1037f, -0.30411f, -0.19469f, 0.92675f, 1f, true),
                        new AttackFrame(0.27378f, 1.29106f, 0.44663f, -0.13821f, -0.46162f, -0.29856f, 0.82381f, 1f, true),
                        new AttackFrame(0.13655f, 1.30015f, 0.48945f, -0.1458f, -0.5916f, -0.40457f, 0.68196f, 1f, true),
                        new AttackFrame(0.01758f, 1.30979f, 0.4937f, -0.13149f, -0.67515f, -0.49304f, 0.53272f, 1f, true),
                        new AttackFrame(-0.0617f, 1.31715f, 0.48007f, -0.11076f, -0.71466f, -0.55083f, 0.41663f, 1f, true),
                        new AttackFrame(-0.09018f, 1.31998f, 0.47172f, -0.10084f, -0.72509f, -0.57137f, 0.37096f, 1f, true),
                        new AttackFrame(-0.09018f, 1.32533f, 0.45659f, -0.103f, -0.7284f, -0.56714f, 0.37037f, 1f, true),
                        new AttackFrame(-0.09018f, 1.33065f, 0.4414f, -0.10515f, -0.73169f, -0.5629f, 0.36976f, 1f, true),
                        new AttackFrame(-0.09018f, 1.33594f, 0.42864f, -0.1073f, -0.73495f, -0.55863f, 0.36914f, 1f, true),
                        new AttackFrame(-0.09018f, 1.30103f, 0.51544f, -0.09327f, -0.71331f, -0.58602f, 0.37294f, 1f, true),
                        new AttackFrame(-0.09018f, 1.30239f, 0.51234f, -0.09381f, -0.71416f, -0.58498f, 0.3728f, 1f, true),
                        new AttackFrame(-0.09018f, 1.30376f, 0.50924f, -0.09435f, -0.71501f, -0.58394f, 0.37266f, 1f, true),
                        new AttackFrame(-0.09018f, 1.30546f, 0.50473f, -0.09503f, -0.71607f, -0.58264f, 0.37249f, 1f, true),
                        new AttackFrame(-0.09018f, 1.30716f, 0.50022f, -0.09571f, -0.71713f, -0.58134f, 0.37232f, 1f, true),
                        new AttackFrame(-0.09018f, 1.30886f, 0.4957f, -0.09638f, -0.71818f, -0.58003f, 0.37214f, 1f, true),
                        new AttackFrame(-0.09018f, 1.31055f, 0.49117f, -0.09706f, -0.71923f, -0.57872f, 0.37197f, 1f, true),
                        new AttackFrame(-0.09018f, 1.31528f, 0.48147f, -0.09895f, -0.72217f, -0.57505f, 0.37147f, 1f, true),
                        new AttackFrame(-0.09018f, 1.31998f, 0.47172f, -0.10084f, -0.72509f, -0.57137f, 0.37096f, 1f, true),
                        new AttackFrame(-0.07174f, 1.31813f, 0.47735f, -0.10743f, -0.71858f, -0.55808f, 0.40081f, 1f, true),
                        new AttackFrame(-0.01967f, 1.31315f, 0.48899f, -0.12295f, -0.6955f, -0.5203f, 0.48006f, 1f, true),
                        new AttackFrame(0.06131f, 1.30606f, 0.49552f, -0.13899f, -0.6476f, -0.46079f, 0.59074f, 1f, true),
                        new AttackFrame(0.16344f, 1.2982f, 0.48441f, -0.14639f, -0.56902f, -0.38421f, 0.71216f, 1f, true),
                        new AttackFrame(0.27378f, 1.29106f, 0.44663f, -0.13821f, -0.46162f, -0.29856f, 0.82381f, 1f, true),
                        new AttackFrame(0.37617f, 1.28586f, 0.3816f, -0.11249f, -0.33629f, -0.2143f, 0.91013f, 1f, true),
                        new AttackFrame(0.45678f, 1.28312f, 0.29922f, -0.0736f, -0.21023f, -0.14155f, 0.96455f, 1f, true),
                        new AttackFrame(0.50944f, 1.28249f, 0.21725f, -0.0312f, -0.10206f, -0.08726f, 0.99045f, 1f, true),
                        new AttackFrame(0.5366f, 1.28295f, 0.15566f, 0.00249f, -0.02793f, -0.05421f, 0.99814f, 1f, true),
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
                        new AttackFrame(0.21434f, 1.45024f, 0.91632f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21434f, 1.45024f, 0.91632f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21434f, 1.45024f, 0.91632f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21434f, 1.45024f, 0.91632f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21434f, 1.45024f, 0.91632f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21434f, 1.45024f, 0.91632f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21434f, 1.45024f, 0.91632f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21434f, 1.45024f, 0.91632f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21434f, 1.45024f, 0.91632f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21434f, 1.45024f, 0.91632f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21434f, 1.45024f, 0.91632f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21434f, 1.45024f, 0.91632f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21434f, 1.45024f, 0.91632f, 0f, 0f, 0f, 1f, 0.35f, true),
                        new AttackFrame(0.21434f, 1.45024f, 1.21632f, 0f, 0f, 0f, 1f, 0.56667f, true),
                        new AttackFrame(0.21434f, 1.45024f, 1.51632f, 0f, 0f, 0f, 1f, 0.78333f, true),
                        new AttackFrame(0.21434f, 1.45024f, 1.81632f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21434f, 1.45024f, 2.11632f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21434f, 1.45024f, 2.41632f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21434f, 1.45024f, 2.71632f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21434f, 1.45024f, 3.01632f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21434f, 1.45024f, 3.31632f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21434f, 1.45024f, 3.61632f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21434f, 1.45024f, 3.91632f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21434f, 1.45024f, 4.21632f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21434f, 1.45024f, 4.51632f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21434f, 1.45024f, 4.81632f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21434f, 1.45024f, 5.11632f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21434f, 1.45024f, 5.41632f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21434f, 1.45024f, 5.71632f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21434f, 1.45024f, 6.01632f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21434f, 1.45024f, 6.31632f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "arcane bolt spawns at staff crystal, travels character-forward (-Y Blender)")
            });
    }
}
