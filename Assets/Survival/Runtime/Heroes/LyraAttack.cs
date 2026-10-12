using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Lyra Theme A fitted/tighten pack + attack (Design 2026-10-08). Staff Cast: Arcane Bolt — starts front-on, right foot steps forward as the body turns side-on (80 deg left of the camera; 72 deg in clip space plus the -8 deg spawn yaw), head stays on the target, two-handed with the left hand low on the shaft (staff parented to RightHand, LeftHand about 61 cm lower on the shaft f8-f21), bolt released at f12 at 9 m/s, aimed 8 deg to her right in clip space so it flies straight at the camera after LyraMotion.FacingYawDegrees.
    /// Rest, walk and attack FBXs replaced; 20 mixamorig bones (no ToeBase); bones bound by name.
    /// Frames baked from Design attack_meta / work blend in Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class LyraAttack
    {
        public const string FileName = "LYRA_blenderig_attack.fbx";
        public const string FileMd5 = "ab3e887dde3d8a142278ff8972e641e8";
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
                        new AttackFrame(0.53308f, 1.27911f, 0.2009f, 0.01584f, -0.03507f, -0.07271f, 0.99661f, 1f, true),
                        new AttackFrame(0.47917f, 1.26733f, 0.37333f, 0.02356f, -0.1245f, -0.15069f, 0.98043f, 1f, true),
                        new AttackFrame(0.35416f, 1.2463f, 0.57866f, 0.05302f, -0.2444f, -0.25689f, 0.93352f, 1f, true),
                        new AttackFrame(0.16807f, 1.21185f, 0.73744f, 0.11171f, -0.36561f, -0.36744f, 0.84784f, 1f, true),
                        new AttackFrame(-0.02653f, 1.16446f, 0.80965f, 0.19345f, -0.46196f, -0.46007f, 0.73315f, 1f, true),
                        new AttackFrame(-0.18083f, 1.11409f, 0.81421f, 0.2796f, -0.5207f, -0.52218f, 0.61484f, 1f, true),
                        new AttackFrame(-0.27705f, 1.07536f, 0.794f, 0.34656f, -0.54619f, -0.55405f, 0.52402f, 1f, true),
                        new AttackFrame(-0.31092f, 1.0604f, 0.78257f, 0.37288f, -0.55219f, -0.56337f, 0.48853f, 1f, true),
                        new AttackFrame(-0.31371f, 1.0604f, 0.76276f, 0.37288f, -0.55219f, -0.56337f, 0.48853f, 1f, true),
                        new AttackFrame(-0.31649f, 1.0604f, 0.74296f, 0.37288f, -0.55219f, -0.56337f, 0.48853f, 1f, true),
                        new AttackFrame(-0.3151f, 1.0604f, 0.75286f, 0.37288f, -0.55219f, -0.56337f, 0.48853f, 1f, true),
                        new AttackFrame(-0.30397f, 1.0604f, 0.83208f, 0.37288f, -0.55219f, -0.56337f, 0.48853f, 1f, true),
                        new AttackFrame(-0.30536f, 1.0604f, 0.82218f, 0.37288f, -0.55219f, -0.56337f, 0.48853f, 1f, true),
                        new AttackFrame(-0.30814f, 1.0604f, 0.80237f, 0.37288f, -0.55219f, -0.56337f, 0.48853f, 1f, true),
                        new AttackFrame(-0.31092f, 1.0604f, 0.78257f, 0.37288f, -0.55219f, -0.56337f, 0.48853f, 1f, true),
                        new AttackFrame(-0.31092f, 1.0604f, 0.78257f, 0.37288f, -0.55219f, -0.56337f, 0.48853f, 1f, true),
                        new AttackFrame(-0.31092f, 1.0604f, 0.78257f, 0.37288f, -0.55219f, -0.56337f, 0.48853f, 1f, true),
                        new AttackFrame(-0.31092f, 1.0604f, 0.78257f, 0.37288f, -0.55219f, -0.56337f, 0.48853f, 1f, true),
                        new AttackFrame(-0.31092f, 1.0604f, 0.78257f, 0.37288f, -0.55219f, -0.56337f, 0.48853f, 1f, true),
                        new AttackFrame(-0.31092f, 1.0604f, 0.78257f, 0.37288f, -0.55219f, -0.56337f, 0.48853f, 1f, true),
                        new AttackFrame(-0.31092f, 1.0604f, 0.78257f, 0.37288f, -0.55219f, -0.56337f, 0.48853f, 1f, true),
                        new AttackFrame(-0.27705f, 1.07536f, 0.794f, 0.34656f, -0.54619f, -0.55405f, 0.52402f, 1f, true),
                        new AttackFrame(-0.18083f, 1.11409f, 0.81421f, 0.2796f, -0.5207f, -0.52218f, 0.61484f, 1f, true),
                        new AttackFrame(-0.02653f, 1.16446f, 0.80965f, 0.19345f, -0.46196f, -0.46007f, 0.73315f, 1f, true),
                        new AttackFrame(0.16807f, 1.21185f, 0.73744f, 0.11171f, -0.36561f, -0.36744f, 0.84784f, 1f, true),
                        new AttackFrame(0.35416f, 1.2463f, 0.57866f, 0.05302f, -0.2444f, -0.25689f, 0.93352f, 1f, true),
                        new AttackFrame(0.47917f, 1.26733f, 0.37333f, 0.02356f, -0.1245f, -0.15069f, 0.98043f, 1f, true),
                        new AttackFrame(0.53308f, 1.27911f, 0.2009f, 0.01584f, -0.03507f, -0.07271f, 0.99661f, 1f, true),
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
                        new AttackFrame(-0.22317f, 1.10686f, 1.34882f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.22317f, 1.10686f, 1.34882f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.22317f, 1.10686f, 1.34882f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.22317f, 1.10686f, 1.34882f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.22317f, 1.10686f, 1.34882f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.22317f, 1.10686f, 1.34882f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.22317f, 1.10686f, 1.34882f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.22317f, 1.10686f, 1.34882f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.22317f, 1.10686f, 1.34882f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.22317f, 1.10686f, 1.34882f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.22317f, 1.10686f, 1.34882f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.22317f, 1.10686f, 1.34882f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.22317f, 1.10686f, 1.34882f, 0f, 0f, 0f, 1f, 0.35f, true),
                        new AttackFrame(-0.18141f, 1.10686f, 1.6459f, 0f, 0f, 0f, 1f, 0.56667f, true),
                        new AttackFrame(-0.13966f, 1.10686f, 1.94298f, 0f, 0f, 0f, 1f, 0.78333f, true),
                        new AttackFrame(-0.09791f, 1.10686f, 2.24006f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.05616f, 1.10686f, 2.53714f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.01441f, 1.10686f, 2.83422f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.02734f, 1.10686f, 3.1313f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.0691f, 1.10686f, 3.42838f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.11085f, 1.10686f, 3.72547f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.1526f, 1.10686f, 4.02255f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.19435f, 1.10686f, 4.31963f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.2361f, 1.10686f, 4.61671f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.27786f, 1.10686f, 4.91379f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.31961f, 1.10686f, 5.21087f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.36136f, 1.10686f, 5.50795f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40311f, 1.10686f, 5.80503f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.44486f, 1.10686f, 6.10211f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.48662f, 1.10686f, 6.39919f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.52837f, 1.10686f, 6.69627f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "arcane bolt spawns at staff crystal, travels toward the camera (clip -Y rotated 8 deg to her right, cancelling the spawn yaw)")
            });
    }
}
