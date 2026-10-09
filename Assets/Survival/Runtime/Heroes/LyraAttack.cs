using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Lyra Theme A fitted/tighten pack + attack (Design 2026-10-08). Staff Cast: Arcane Bolt — staff stays in RightHand, bolt released at f12 at 9 m/s toward top of screen.
    /// Rest, walk and attack FBXs replaced; 20 mixamorig bones (no ToeBase); bones bound by name.
    /// Frames baked from Design attack_meta / work blend in Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class LyraAttack
    {
        public const string FileName = "LYRA_blenderig_attack.fbx";
        public const string FileMd5 = "835b0502ef40dc428424f5a790ffaedd";
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
                        new AttackFrame(0.555f, 1.30567f, 0.117f, -0.0074f, -0.00706f, -0.0413f, 0.99909f, 1f, true),
                        new AttackFrame(0.5692f, 1.34425f, 0.0866f, -0.04888f, -0.00535f, -0.03226f, 0.99827f, 1f, true),
                        new AttackFrame(0.57424f, 1.39742f, 0.04554f, -0.10428f, -0.01383f, -0.01262f, 0.99437f, 1f, true),
                        new AttackFrame(0.55745f, 1.4539f, 0.00407f, -0.15831f, -0.04881f, 0.01367f, 0.98609f, 1f, true),
                        new AttackFrame(0.52146f, 1.50244f, -0.02805f, -0.19732f, -0.1082f, 0.03371f, 0.97377f, 1f, true),
                        new AttackFrame(0.4816f, 1.53521f, -0.04426f, -0.21538f, -0.17763f, 0.03098f, 0.95974f, 1f, true),
                        new AttackFrame(0.45393f, 1.5483f, -0.04106f, -0.21359f, -0.24334f, -0.00596f, 0.94611f, 1f, true),
                        new AttackFrame(0.44826f, 1.54075f, -0.01106f, -0.19378f, -0.29855f, -0.06891f, 0.93197f, 1f, true),
                        new AttackFrame(0.45653f, 1.51275f, 0.08782f, -0.13031f, -0.37113f, -0.14318f, 0.90818f, 1f, true),
                        new AttackFrame(0.42624f, 1.47547f, 0.24682f, 0.01186f, -0.47963f, -0.15862f, 0.86294f, 1f, true),
                        new AttackFrame(0.36835f, 1.46005f, 0.35842f, 0.17663f, -0.57639f, -0.10561f, 0.79084f, 1f, true),
                        new AttackFrame(0.3407f, 1.44275f, 0.42383f, 0.24565f, -0.60672f, -0.07042f, 0.75272f, 1f, true),
                        new AttackFrame(0.34531f, 1.42687f, 0.44855f, 0.27515f, -0.60784f, -0.05999f, 0.74244f, 1f, true),
                        new AttackFrame(0.35192f, 1.40753f, 0.46473f, 0.29046f, -0.60745f, -0.06196f, 0.73675f, 1f, true),
                        new AttackFrame(0.3543f, 1.39333f, 0.47599f, 0.29646f, -0.6058f, -0.06291f, 0.73564f, 1f, true),
                        new AttackFrame(0.35177f, 1.38527f, 0.48211f, 0.29448f, -0.60461f, -0.06474f, 0.73725f, 1f, true),
                        new AttackFrame(0.3471f, 1.38291f, 0.48407f, 0.28597f, -0.6026f, -0.06549f, 0.74217f, 1f, true),
                        new AttackFrame(0.34079f, 1.38439f, 0.48389f, 0.27129f, -0.59749f, -0.06361f, 0.75191f, 1f, true),
                        new AttackFrame(0.33679f, 1.3868f, 0.48346f, 0.25162f, -0.5855f, -0.05818f, 0.76844f, 1f, true),
                        new AttackFrame(0.34395f, 1.38714f, 0.48237f, 0.22827f, -0.55943f, -0.05057f, 0.79522f, 1f, true),
                        new AttackFrame(0.36403f, 1.38328f, 0.4779f, 0.20148f, -0.51607f, -0.04752f, 0.83115f, 1f, true),
                        new AttackFrame(0.39369f, 1.37505f, 0.46653f, 0.17152f, -0.45821f, -0.05205f, 0.87058f, 1f, true),
                        new AttackFrame(0.43234f, 1.36361f, 0.44347f, 0.14065f, -0.38785f, -0.05954f, 0.90898f, 1f, true),
                        new AttackFrame(0.47592f, 1.34974f, 0.40492f, 0.11141f, -0.30853f, -0.06515f, 0.94242f, 1f, true),
                        new AttackFrame(0.51463f, 1.33412f, 0.35305f, 0.08366f, -0.22858f, -0.06662f, 0.96763f, 1f, true),
                        new AttackFrame(0.5409f, 1.31817f, 0.29489f, 0.05779f, -0.1557f, -0.06322f, 0.98408f, 1f, true),
                        new AttackFrame(0.55382f, 1.304f, 0.23739f, 0.03685f, -0.09364f, -0.0561f, 0.99334f, 1f, true),
                        new AttackFrame(0.55608f, 1.29329f, 0.18733f, 0.0232f, -0.04549f, -0.04826f, 0.99753f, 1f, true),
                        new AttackFrame(0.55178f, 1.28678f, 0.15181f, 0.01671f, -0.01492f, -0.04348f, 0.9988f, 1f, true),
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
                        new AttackFrame(0.2464f, 1.90249f, 0.65932f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.2464f, 1.90249f, 0.65932f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.2464f, 1.90249f, 0.65932f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.2464f, 1.90249f, 0.65932f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.2464f, 1.90249f, 0.65932f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.2464f, 1.90249f, 0.65932f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.2464f, 1.90249f, 0.65932f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.2464f, 1.90249f, 0.65932f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.2464f, 1.90249f, 0.65932f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.2464f, 1.90249f, 0.65932f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.2464f, 1.90249f, 0.65932f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.2464f, 1.90249f, 0.65932f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.2464f, 1.90249f, 0.65932f, 0f, 0f, 0f, 1f, 0.35f, true),
                        new AttackFrame(0.2464f, 1.90249f, 0.95932f, 0f, 0f, 0f, 1f, 0.56667f, true),
                        new AttackFrame(0.2464f, 1.90249f, 1.25932f, 0f, 0f, 0f, 1f, 0.78333f, true),
                        new AttackFrame(0.2464f, 1.90249f, 1.55932f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.2464f, 1.90249f, 1.85932f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.2464f, 1.90249f, 2.15932f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.2464f, 1.90249f, 2.45932f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.2464f, 1.90249f, 2.75932f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.2464f, 1.90249f, 3.05932f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.2464f, 1.90249f, 3.35932f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.2464f, 1.90249f, 3.65932f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.2464f, 1.90249f, 3.95932f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.2464f, 1.90249f, 4.25932f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.2464f, 1.90249f, 4.55932f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.2464f, 1.90249f, 4.85932f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.2464f, 1.90249f, 5.15932f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.2464f, 1.90249f, 5.45932f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.2464f, 1.90249f, 5.75932f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.2464f, 1.90249f, 6.05932f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "arcane bolt spawns at staff crystal, travels character-forward (-Y Blender)")
            });
    }
}
