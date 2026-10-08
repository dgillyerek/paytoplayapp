using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Oakenshield Theme A attack add-on (Design 2026-10-07). Thorn Spear — spear conjured in RightHand f3–8 (scales up), thrown at f14 with a gold/leaf trail.
    /// New 0–30 frame Scene take at 30 fps on the existing blenderig (same bones, no rebind; rest and walk unchanged).
    /// Frames below are baked from Design attack_meta drivers in Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class OakenshieldAttack
    {
        public const string FileName = "OAKENSHIELD_blenderig_attack.fbx";
        /// <summary>ThemePack attack FBX: Design clothsplit_20261007 OAKENSHIELD_clothsplit_attack.fbx (body bones only, cloth at rest).</summary>
        public const string FileMd5 = "769543a3e39321857dc50e734aadffd2";
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
            new[] { -0.01317f, 0.91965f, 0.00679f, 0f, 1.62933f, -0.05913f, -0.18522f, 1.03887f, 0.08206f, 0.19503f, 1.05566f, -0.05274f },
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
                        new AttackFrame(0.21364f, 1.00867f, -0.06615f, -0.0683f, 0.07017f, -0.4591f, 0.88297f, 0.05f, false),
                        new AttackFrame(0.22606f, 1.01033f, -0.06207f, -0.07279f, 0.06369f, -0.45156f, 0.88698f, 0.05f, false),
                        new AttackFrame(0.26194f, 1.01777f, -0.05228f, -0.08345f, 0.0461f, -0.42829f, 0.8986f, 0.05f, false),
                        new AttackFrame(0.31924f, 1.03739f, -0.04221f, -0.09448f, 0.02102f, -0.38669f, 0.91712f, 0.05f, true),
                        new AttackFrame(0.39163f, 1.07622f, -0.03904f, -0.09926f, -0.00567f, -0.32564f, 0.94025f, 0.2f, true),
                        new AttackFrame(0.46599f, 1.13633f, -0.0493f, -0.09286f, -0.02632f, -0.24963f, 0.96352f, 0.4f, true),
                        new AttackFrame(0.52686f, 1.21031f, -0.07485f, -0.07488f, -0.03484f, -0.16953f, 0.98206f, 0.6f, true),
                        new AttackFrame(0.56544f, 1.28375f, -0.11074f, -0.04998f, -0.03042f, -0.0981f, 0.99345f, 0.8f, true),
                        new AttackFrame(0.58337f, 1.34338f, -0.14754f, -0.0252f, -0.018f, -0.04423f, 0.99854f, 1f, true),
                        new AttackFrame(0.58861f, 1.3817f, -0.17533f, -0.00698f, -0.00541f, -0.01127f, 0.9999f, 1f, true),
                        new AttackFrame(0.58923f, 1.39519f, -0.18598f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.6174f, 1.41005f, -0.06062f, -0.09546f, -0.05939f, 0.0129f, 0.99358f, 1f, true),
                        new AttackFrame(0.6272f, 1.43417f, 0.18654f, -0.30808f, -0.13625f, -0.0024f, 0.94155f, 1f, true),
                        new AttackFrame(0.59489f, 1.40728f, 0.37447f, -0.47437f, -0.12274f, -0.06524f, 0.86928f, 1f, true),
                        new AttackFrame(0.55463f, 1.34555f, 0.45729f, -0.50596f, -0.09251f, -0.11823f, 0.84939f, 1f, false),
                        new AttackFrame(0.52383f, 1.30193f, 0.48957f, -0.47227f, -0.10368f, -0.15199f, 0.86204f, 1f, false),
                        new AttackFrame(0.4916f, 1.26887f, 0.51796f, -0.43567f, -0.12954f, -0.17986f, 0.87239f, 1f, false),
                        new AttackFrame(0.4578f, 1.2432f, 0.5425f, -0.3975f, -0.166f, -0.20374f, 0.87916f, 1f, false),
                        new AttackFrame(0.42254f, 1.22235f, 0.56225f, -0.35869f, -0.20859f, -0.22465f, 0.88168f, 1f, false),
                        new AttackFrame(0.38689f, 1.20423f, 0.57583f, -0.32036f, -0.25209f, -0.24322f, 0.88015f, 1f, false),
                        new AttackFrame(0.35311f, 1.18715f, 0.58211f, -0.28406f, -0.29055f, -0.26012f, 0.87591f, 1f, false),
                        new AttackFrame(0.33267f, 1.16682f, 0.56622f, -0.24901f, -0.30832f, -0.27605f, 0.87563f, 1f, false),
                        new AttackFrame(0.33078f, 1.14158f, 0.51874f, -0.21361f, -0.29789f, -0.29234f, 0.88327f, 1f, false),
                        new AttackFrame(0.33798f, 1.11386f, 0.44545f, -0.17913f, -0.26441f, -0.3109f, 0.89518f, 1f, false),
                        new AttackFrame(0.3434f, 1.08614f, 0.35273f, -0.14726f, -0.21329f, -0.33303f, 0.90659f, 1f, false),
                        new AttackFrame(0.33808f, 1.06092f, 0.24979f, -0.11985f, -0.15115f, -0.35875f, 0.91328f, 1f, false),
                        new AttackFrame(0.31829f, 1.04025f, 0.14823f, -0.09835f, -0.0857f, -0.38656f, 0.91299f, 1f, false),
                        new AttackFrame(0.28692f, 1.02515f, 0.05939f, -0.08328f, -0.02476f, -0.41376f, 0.90623f, 1f, false),
                        new AttackFrame(0.2522f, 1.01547f, -0.00854f, -0.07412f, 0.02493f, -0.43705f, 0.89603f, 1f, false),
                        new AttackFrame(0.22456f, 1.0103f, -0.05121f, -0.06959f, 0.05807f, -0.45315f, 0.88681f, 1f, false),
                        new AttackFrame(0.21364f, 1.00867f, -0.06615f, -0.0683f, 0.07017f, -0.4591f, 0.88297f, 1f, false)
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
                        new AttackFrame(0.55463f, 1.34555f, 0.45729f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.55463f, 1.34555f, 0.45729f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.55463f, 1.34555f, 0.45729f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.55463f, 1.34555f, 0.45729f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.55463f, 1.34555f, 0.45729f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.55463f, 1.34555f, 0.45729f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.55463f, 1.34555f, 0.45729f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.55463f, 1.34555f, 0.45729f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.55463f, 1.34555f, 0.45729f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.55463f, 1.34555f, 0.45729f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.55463f, 1.34555f, 0.45729f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.55463f, 1.34555f, 0.45729f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.55463f, 1.34555f, 0.45729f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.55463f, 1.34555f, 0.45729f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.55463f, 1.34555f, 0.45729f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.55463f, 1.34555f, 0.82396f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.55463f, 1.34555f, 1.19062f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.55463f, 1.34555f, 1.55729f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.55463f, 1.34555f, 1.92396f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.55463f, 1.34555f, 2.29062f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.55463f, 1.34555f, 2.65729f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.55463f, 1.34555f, 3.02396f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.55463f, 1.34555f, 3.39062f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.55463f, 1.34555f, 3.75729f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.55463f, 1.34555f, 4.12396f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.55463f, 1.34555f, 4.49062f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.55463f, 1.34555f, 4.85729f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.55463f, 1.34555f, 5.22396f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.55463f, 1.34555f, 5.59062f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.55463f, 1.34555f, 5.95729f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.55463f, 1.34555f, 6.32396f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "thrown thorn spear, travels character-forward")
            });
    }
}
