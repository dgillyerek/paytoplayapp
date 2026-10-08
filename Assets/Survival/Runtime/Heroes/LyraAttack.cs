using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Lyra Theme A fitted/tighten pack + attack (Design 2026-10-08). Staff Cast: Arcane Bolt — staff stays in RightHand, bolt released at f12 at 9 m/s toward top of screen.
    /// Rest, walk and attack FBXs replaced; 22 mixamorig bones; bones bound by name.
    /// Frames baked from Design attack_meta / work blend in Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class LyraAttack
    {
        public const string FileName = "LYRA_blenderig_attack.fbx";
        public const string FileMd5 = "90385eda02ee4cad4b379738ad38a74e";
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
            new[] { 0.00357f, 1.06126f, 0.05368f, 0f, 1.665f, 0f, -0.265f, 1.045f, 0.09f, 0.45f, 1.265f, 0.07f },
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
                        new AttackFrame(0.54638f, 1.29533f, 0.11869f, -0.00179f, 0.00852f, -0.03378f, 0.99939f, 1f, true),
                        new AttackFrame(0.54653f, 1.3268f, 0.0844f, -0.04688f, 0.02774f, -0.0087f, 0.99848f, 1f, true),
                        new AttackFrame(0.53645f, 1.3694f, 0.03872f, -0.1061f, 0.0421f, 0.02782f, 0.99307f, 1f, true),
                        new AttackFrame(0.51188f, 1.41243f, -0.00721f, -0.16369f, 0.04018f, 0.06599f, 0.98348f, 1f, true),
                        new AttackFrame(0.47733f, 1.44542f, -0.04351f, -0.20518f, 0.02101f, 0.09077f, 0.97428f, 1f, true),
                        new AttackFrame(0.44386f, 1.46201f, -0.06469f, -0.22234f, -0.00483f, 0.0869f, 0.97108f, 1f, true),
                        new AttackFrame(0.42264f, 1.46041f, -0.0698f, -0.21311f, -0.02054f, 0.04404f, 0.97582f, 1f, true),
                        new AttackFrame(0.42f, 1.44f, -0.06f, -0.17508f, -0.00886f, -0.04201f, 0.98362f, 1f, true),
                        new AttackFrame(0.43831f, 1.40981f, 0.02519f, -0.08374f, 0.00841f, -0.13415f, 0.98738f, 1f, true),
                        new AttackFrame(0.42382f, 1.39247f, 0.19198f, 0.06317f, 0.01093f, -0.15097f, 0.98646f, 1f, true),
                        new AttackFrame(0.35047f, 1.39692f, 0.33867f, 0.18648f, 0.00658f, -0.08791f, 0.9785f, 1f, true),
                        new AttackFrame(0.3f, 1.4f, 0.4f, 0.24859f, 0.00936f, -0.0419f, 0.96766f, 1f, true),
                        new AttackFrame(0.29848f, 1.39075f, 0.40927f, 0.2825f, 0.02374f, -0.04225f, 0.95804f, 1f, true),
                        new AttackFrame(0.29673f, 1.38341f, 0.41532f, 0.3049f, 0.03253f, -0.04224f, 0.95089f, 1f, true),
                        new AttackFrame(0.29498f, 1.37799f, 0.41926f, 0.31655f, 0.03624f, -0.04259f, 0.94693f, 1f, true),
                        new AttackFrame(0.29336f, 1.37424f, 0.422f, 0.31845f, 0.03542f, -0.04334f, 0.94629f, 1f, true),
                        new AttackFrame(0.29197f, 1.37186f, 0.4243f, 0.31155f, 0.03057f, -0.04399f, 0.94872f, 1f, true),
                        new AttackFrame(0.29085f, 1.37054f, 0.42679f, 0.29654f, 0.02212f, -0.04376f, 0.95376f, 1f, true),
                        new AttackFrame(0.29f, 1.37f, 0.43f, 0.27383f, 0.01045f, -0.04164f, 0.96082f, 1f, true),
                        new AttackFrame(0.29616f, 1.36758f, 0.43138f, 0.24992f, -0.00323f, -0.04296f, 0.96731f, 1f, true),
                        new AttackFrame(0.31478f, 1.36121f, 0.42769f, 0.22834f, -0.01774f, -0.05125f, 0.97207f, 1f, true),
                        new AttackFrame(0.34378f, 1.35161f, 0.41731f, 0.20459f, -0.03241f, -0.06307f, 0.97628f, 1f, true),
                        new AttackFrame(0.38042f, 1.33981f, 0.39777f, 0.17543f, -0.04536f, -0.07464f, 0.98061f, 1f, true),
                        new AttackFrame(0.42086f, 1.3271f, 0.36694f, 0.14076f, -0.05358f, -0.08207f, 0.98518f, 1f, true),
                        new AttackFrame(0.46028f, 1.31475f, 0.32472f, 0.10408f, -0.05422f, -0.08271f, 0.98964f, 1f, true),
                        new AttackFrame(0.49394f, 1.30379f, 0.27434f, 0.07071f, -0.0466f, -0.07648f, 0.99347f, 1f, true),
                        new AttackFrame(0.5188f, 1.2949f, 0.22217f, 0.04483f, -0.03293f, -0.0659f, 0.99627f, 1f, true),
                        new AttackFrame(0.53438f, 1.28847f, 0.17615f, 0.0278f, -0.01759f, -0.05464f, 0.99796f, 1f, true),
                        new AttackFrame(0.5423f, 1.28461f, 0.14393f, 0.01867f, -0.00544f, -0.04615f, 0.99875f, 1f, true),
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
                        new AttackFrame(0.34228f, 1.86218f, 0.64558f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.34228f, 1.86218f, 0.64558f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.34228f, 1.86218f, 0.64558f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.34228f, 1.86218f, 0.64558f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.34228f, 1.86218f, 0.64558f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.34228f, 1.86218f, 0.64558f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.34228f, 1.86218f, 0.64558f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.34228f, 1.86218f, 0.64558f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.34228f, 1.86218f, 0.64558f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.34228f, 1.86218f, 0.64558f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.34228f, 1.86218f, 0.64558f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.34228f, 1.86218f, 0.64558f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.34228f, 1.86218f, 0.64558f, 0f, 0f, 0f, 1f, 0.35f, true),
                        new AttackFrame(0.34228f, 1.86218f, 0.94558f, 0f, 0f, 0f, 1f, 0.56667f, true),
                        new AttackFrame(0.34228f, 1.86218f, 1.24558f, 0f, 0f, 0f, 1f, 0.78333f, true),
                        new AttackFrame(0.34228f, 1.86218f, 1.54558f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.34228f, 1.86218f, 1.84558f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.34228f, 1.86218f, 2.14558f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.34228f, 1.86218f, 2.44558f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.34228f, 1.86218f, 2.74558f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.34228f, 1.86218f, 3.04558f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.34228f, 1.86218f, 3.34558f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.34228f, 1.86218f, 3.64558f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.34228f, 1.86218f, 3.94558f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.34228f, 1.86218f, 4.24558f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.34228f, 1.86218f, 4.54558f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.34228f, 1.86218f, 4.84558f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.34228f, 1.86218f, 5.14558f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.34228f, 1.86218f, 5.44558f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.34228f, 1.86218f, 5.74558f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.34228f, 1.86218f, 6.04558f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "arcane bolt spawns at staff crystal, travels character-forward (-Y Blender)")
            });
    }
}
