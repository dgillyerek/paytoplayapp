using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Lyra Theme A attack add-on (Design 2026-10-07). Staff Cast: Arcane Bolt — gold crystal staff (new held prop, RightHand) + royal-blue arcane bolt with gold sparks and blue trail.
    /// New 0–30 frame Scene take at 30 fps on the existing blenderig (same bones, no rebind; rest and walk unchanged).
    /// Frames below are baked from Design attack_meta drivers in Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class LyraAttack
    {
        public const string FileName = "LYRA_blenderig_attack.fbx";
        public const string FileMd5 = "efd19534168581a65e3e7b4b21c10813";
        public const string DesignDir = "design/survival-theme-a-fantasy/heroes/anim/lyra/attack_20261007";
        public const int ReleaseFrame = 12;
        public const bool InPlace = false;

        /// <summary>Prop FBXs copied next to the character FBXs, with Design MD5s.</summary>
        public static readonly string[] PropFileNames =
        {
            "LYRA_staff_goldcrystal.fbx"
        };

        public static readonly string[] PropFileMd5s =
        {
            "cc8470fb650d01f27fda54913b4038a1"
        };

        public static readonly BlenderRigAttackSpec Spec = new BlenderRigAttackSpec(
            "Lyra",
            FileName,
            FileMd5,
            DesignDir,
            ReleaseFrame,
            InPlace,
            new[] { "mixamorig:Hips", "mixamorig:Head", "mixamorig:LeftHand", "mixamorig:RightHand" },
            new[] { 0.01259f, 0.94954f, -0.00375f, 0f, 1.8358f, 0.16761f, -0.26942f, 1.03762f, 0.17877f, 0.27714f, 1.04356f, 0.18079f },
            new[]
            {
                new AttackTrack(
                    "LYRA_staff_goldcrystal",
                    AttackTrackKind.Held,
                    "LYRA_staff_goldcrystal.fbx",
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
                    new[] { 0.19636f, 2.12214f, 0.07656f },
                    new[]
                    {
                        new AttackFrame(0.28404f, 1.01553f, 0.15251f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.2901f, 1.01544f, 0.15169f, -0.0127f, 0.01368f, 0.00508f, 0.99981f, 1f, true),
                        new AttackFrame(0.30685f, 1.01586f, 0.14911f, -0.04639f, 0.04902f, 0.0193f, 0.99753f, 1f, true),
                        new AttackFrame(0.33235f, 1.01823f, 0.14401f, -0.09426f, 0.09675f, 0.04135f, 0.98997f, 1f, true),
                        new AttackFrame(0.36373f, 1.0238f, 0.1355f, -0.14898f, 0.14769f, 0.06917f, 0.9753f, 1f, true),
                        new AttackFrame(0.39676f, 1.03277f, 0.12368f, -0.20321f, 0.19411f, 0.09912f, 0.95457f, 1f, true),
                        new AttackFrame(0.4265f, 1.04382f, 0.11081f, -0.25069f, 0.23088f, 0.12611f, 0.93163f, 1f, true),
                        new AttackFrame(0.4484f, 1.05433f, 0.10144f, -0.28673f, 0.25559f, 0.14419f, 0.91196f, 1f, true),
                        new AttackFrame(0.45869f, 1.06109f, 0.10124f, -0.30743f, 0.26746f, 0.14738f, 0.90124f, 1f, true),
                        new AttackFrame(0.41441f, 1.07855f, 0.16046f, -0.24181f, 0.22634f, 0.078f, 0.94033f, 1f, true),
                        new AttackFrame(0.30141f, 1.13407f, 0.22986f, -0.03253f, 0.09133f, -0.04134f, 0.99443f, 1f, true),
                        new AttackFrame(0.22146f, 1.21126f, 0.25038f, 0.2041f, -0.08283f, -0.09483f, 0.97082f, 1f, true),
                        new AttackFrame(0.19975f, 1.24506f, 0.26296f, 0.3137f, -0.16955f, -0.09771f, 0.92914f, 1f, true),
                        new AttackFrame(0.1952f, 1.24426f, 0.27197f, 0.33197f, -0.17827f, -0.10322f, 0.92052f, 1f, true),
                        new AttackFrame(0.19096f, 1.24343f, 0.27839f, 0.34523f, -0.1836f, -0.10935f, 0.91387f, 1f, true),
                        new AttackFrame(0.18715f, 1.24267f, 0.28267f, 0.35428f, -0.18653f, -0.11571f, 0.90901f, 1f, true),
                        new AttackFrame(0.18385f, 1.24201f, 0.28528f, 0.3599f, -0.18799f, -0.12185f, 0.9057f, 1f, true),
                        new AttackFrame(0.18109f, 1.24148f, 0.28666f, 0.36282f, -0.18881f, -0.12727f, 0.90361f, 1f, true),
                        new AttackFrame(0.17889f, 1.24107f, 0.28723f, 0.36376f, -0.18985f, -0.13144f, 0.90242f, 1f, true),
                        new AttackFrame(0.17727f, 1.24076f, 0.28741f, 0.36345f, -0.19197f, -0.13381f, 0.90174f, 1f, true),
                        new AttackFrame(0.17845f, 1.23569f, 0.28678f, 0.35417f, -0.19004f, -0.1339f, 0.90582f, 1f, true),
                        new AttackFrame(0.18434f, 1.22193f, 0.28446f, 0.32978f, -0.17936f, -0.13136f, 0.91751f, 1f, true),
                        new AttackFrame(0.19459f, 1.20066f, 0.27956f, 0.29347f, -0.16139f, -0.12535f, 0.93387f, 1f, true),
                        new AttackFrame(0.20858f, 1.17338f, 0.27082f, 0.24871f, -0.13786f, -0.11492f, 0.95181f, 1f, true),
                        new AttackFrame(0.22502f, 1.14223f, 0.25717f, 0.19928f, -0.11089f, -0.09967f, 0.96853f, 1f, true),
                        new AttackFrame(0.24197f, 1.10994f, 0.23851f, 0.14911f, -0.08294f, -0.08025f, 0.98206f, 1f, true),
                        new AttackFrame(0.25732f, 1.07941f, 0.21621f, 0.10189f, -0.05646f, -0.0584f, 0.99147f, 1f, true),
                        new AttackFrame(0.26948f, 1.05315f, 0.19302f, 0.06082f, -0.03351f, -0.03667f, 0.99691f, 1f, true),
                        new AttackFrame(0.27781f, 1.03298f, 0.17244f, 0.0286f, -0.01567f, -0.0179f, 0.99931f, 1f, true),
                        new AttackFrame(0.28251f, 1.0201f, 0.15792f, 0.00756f, -0.00413f, -0.00485f, 0.99995f, 1f, true),
                        new AttackFrame(0.28404f, 1.01553f, 0.15251f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "staff grip in RightHand"),
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
                        new AttackFrame(0.27945f, 2.0762f, 0.91599f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.27945f, 2.0762f, 0.91599f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.27945f, 2.0762f, 0.91599f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.27945f, 2.0762f, 0.91599f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.27945f, 2.0762f, 0.91599f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.27945f, 2.0762f, 0.91599f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.27945f, 2.0762f, 0.91599f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.27945f, 2.0762f, 0.91599f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.27945f, 2.0762f, 0.91599f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.27945f, 2.0762f, 0.91599f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.27945f, 2.0762f, 0.91599f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.27945f, 2.0762f, 0.91599f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.27945f, 2.0762f, 0.91599f, 0f, 0f, 0f, 1f, 0.35f, true),
                        new AttackFrame(0.27945f, 2.0762f, 1.21599f, 0f, 0f, 0f, 1f, 0.56667f, true),
                        new AttackFrame(0.27945f, 2.0762f, 1.51599f, 0f, 0f, 0f, 1f, 0.78333f, true),
                        new AttackFrame(0.27945f, 2.0762f, 1.81599f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.27945f, 2.0762f, 2.11599f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.27945f, 2.0762f, 2.41599f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.27945f, 2.0762f, 2.71599f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.27945f, 2.0762f, 3.01599f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.27945f, 2.0762f, 3.31599f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.27945f, 2.0762f, 3.61599f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.27945f, 2.0762f, 3.91599f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.27945f, 2.0762f, 4.21599f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.27945f, 2.0762f, 4.516f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.27945f, 2.0762f, 4.81599f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.27945f, 2.0762f, 5.11599f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.27945f, 2.0762f, 5.41599f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.27945f, 2.0762f, 5.71599f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.27945f, 2.0762f, 6.01599f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.27945f, 2.0762f, 6.31599f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "arcane bolt spawns at staff crystal, travels character-forward (-Y Blender)")
            });
    }
}
