using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Rowan Theme A nocape pack + attack (Design 2026-10-08). Aimed Shot — separate bow (baked character-space), blue-fletch arrow released at f14 at 14 m/s toward top of screen.
    /// Rest, walk and attack FBXs replaced; 22 mixamorig bones; bones bound by name.
    /// Frames baked from Design attack_meta / work blend in Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class RowanAttack
    {
        public const string FileName = "ROWAN_male_blenderig_attack.fbx";
        public const string FileMd5 = "9cc2f377859ae08ec5ee75a1f1932caf";
        public const string DesignDir = "design/survival-theme-a-fantasy/heroes/anim/rowan/attack_20261008";
        public const int ReleaseFrame = 14;
        public const bool InPlace = false;

        /// <summary>Prop FBXs copied next to the character FBXs, with Design MD5s.</summary>
        public static readonly string[] PropFileNames =
        {
            "ROWAN_bow.fbx",
            "ROWAN_arrow_blue_fletch.fbx"
        };

        public static readonly string[] PropFileMd5s =
        {
            "80758fce88e2a9313a34d171e334ae00",
            "01821e47d08300807bbef18ef241ab2e"
        };

        public static readonly BlenderRigAttackSpec Spec = new BlenderRigAttackSpec(
            "Rowan",
            FileName,
            FileMd5,
            DesignDir,
            ReleaseFrame,
            InPlace,
            new[] { "mixamorig:Hips", "mixamorig:Head", "mixamorig:LeftHand", "mixamorig:RightHand" },
            new[] { -0.00866f, 1.06529f, 0.09411f, 0f, 1.635f, 0.035f, -0.335f, 0.99f, 0.17f, 0.45f, 1f, 0.025f },
            new[]
            {
                new AttackTrack(
                    "ROWAN_bow",
                    AttackTrackKind.Held,
                    "ROWAN_bow.fbx",
                    "",
                    0,
                    0,
                    0f,
                    AttackVfxShape.None,
                    0f,
                    0f,
                    new float[0],
                    new float[0],
                    new float[0],
                    new[] { 0.32532f, 1.48009f, 0.28893f },
                    new[]
                    {
                        new AttackFrame(0.50543f, 0.89572f, 0.09054f, 0.6815f, 0.34364f, -0.38444f, 0.5193f, 1f, true),
                        new AttackFrame(0.36528f, 0.887f, 0.18518f, 0.75391f, 0.23081f, -0.32603f, 0.52159f, 1f, true),
                        new AttackFrame(0.12992f, 1.01277f, 0.32412f, 0.77357f, 0.0512f, -0.11992f, 0.62015f, 1f, true),
                        new AttackFrame(0.04f, 1.13f, 0.36f, 0.70711f, 0f, 0f, 0.70711f, 1f, true),
                        new AttackFrame(0.04f, 1.13f, 0.36f, 0.70711f, 0f, 0f, 0.70711f, 1f, true),
                        new AttackFrame(0.00408f, 1.14746f, 0.40652f, 0.71686f, -0.03392f, -0.01666f, 0.69619f, 1f, true),
                        new AttackFrame(-0.0489f, 1.23421f, 0.51048f, 0.73052f, -0.08002f, -0.02351f, 0.67778f, 1f, true),
                        new AttackFrame(-0.05355f, 1.38582f, 0.59116f, 0.72844f, -0.06047f, -0.00871f, 0.68238f, 1f, true),
                        new AttackFrame(-0.03f, 1.5f, 0.62f, 0.70711f, 0f, 0f, 0.70711f, 1f, true),
                        new AttackFrame(-0.01529f, 1.54631f, 0.64285f, 0.68884f, 0.03448f, 0.00567f, 0.72407f, 1f, true),
                        new AttackFrame(-0.00925f, 1.56981f, 0.66806f, 0.68519f, 0.0391f, 0.00535f, 0.72729f, 1f, true),
                        new AttackFrame(-0.01112f, 1.5764f, 0.69317f, 0.69191f, 0.02591f, 0.00352f, 0.72151f, 1f, true),
                        new AttackFrame(-0.01673f, 1.57336f, 0.71239f, 0.70193f, 0.00858f, 0.00128f, 0.71219f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 0.72f, 0.70711f, 0f, 0f, 0.70711f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 0.72f, 0.70711f, 0f, 0f, 0.70711f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 0.72f, 0.70711f, 0f, 0f, 0.70711f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 0.72f, 0.70711f, 0f, 0f, 0.70711f, 1f, true),
                        new AttackFrame(-0.01991f, 1.57035f, 0.71992f, 0.70675f, 0.00002f, -0.00047f, 0.70747f, 1f, true),
                        new AttackFrame(-0.01978f, 1.57092f, 0.7198f, 0.70616f, 0.00004f, -0.00122f, 0.70805f, 1f, true),
                        new AttackFrame(-0.01979f, 1.57102f, 0.71982f, 0.70606f, 0.00004f, -0.00132f, 0.70815f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 0.72f, 0.70711f, 0f, 0f, 0.70711f, 1f, true),
                        new AttackFrame(-0.07068f, 1.24543f, 0.63286f, 0.72439f, -0.0854f, -0.02705f, 0.68355f, 1f, true),
                        new AttackFrame(0.04f, 1.13f, 0.36f, 0.70711f, 0f, 0f, 0.70711f, 1f, true),
                        new AttackFrame(0.04f, 1.13f, 0.36f, 0.70711f, 0f, 0f, 0.70711f, 1f, true),
                        new AttackFrame(0.06178f, 1.0934f, 0.35216f, 0.72276f, 0.01224f, -0.02404f, 0.69058f, 1f, true),
                        new AttackFrame(0.12028f, 1.01005f, 0.32186f, 0.75441f, 0.04993f, -0.08758f, 0.64862f, 1f, true),
                        new AttackFrame(0.20561f, 0.92482f, 0.26556f, 0.77526f, 0.11387f, -0.17469f, 0.59622f, 1f, true),
                        new AttackFrame(0.30696f, 0.87862f, 0.1999f, 0.76729f, 0.19426f, -0.26304f, 0.55167f, 1f, true),
                        new AttackFrame(0.40601f, 0.87491f, 0.14322f, 0.7343f, 0.2707f, -0.33162f, 0.52683f, 1f, true),
                        new AttackFrame(0.47807f, 0.88749f, 0.10453f, 0.6978f, 0.3241f, -0.37154f, 0.51961f, 1f, true),
                        new AttackFrame(0.50543f, 0.89572f, 0.09054f, 0.6815f, 0.34364f, -0.38444f, 0.5193f, 1f, true)
                    },
                    "bow_grip baked character-space (ROWAN_bow_attack); no parent switching"),
                new AttackTrack(
                    "ROWAN_arrow_blue_fletch",
                    AttackTrackKind.Held,
                    "ROWAN_arrow_blue_fletch.fbx",
                    "",
                    0,
                    0,
                    0f,
                    AttackVfxShape.None,
                    0f,
                    0f,
                    new float[0],
                    new float[0],
                    new float[0],
                    new[] { 0.03886f, 0.0343f, 0.8025f },
                    new[]
                    {
                        new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00863f, 1.14675f, 0.383f, 0.3269f, 0.04566f, 0.08468f, 0.94015f, 0.33333f, true),
                        new AttackFrame(-0.03992f, 1.21933f, 0.43096f, 0.25752f, -0.00169f, 0.02947f, 0.96582f, 0.66667f, true),
                        new AttackFrame(-0.06104f, 1.35336f, 0.46666f, 0.13778f, -0.0339f, -0.01039f, 0.98983f, 1f, true),
                        new AttackFrame(-0.03f, 1.5f, 0.47694f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02221f, 1.53734f, 0.46702f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02291f, 1.55163f, 0.37083f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02511f, 1.55709f, 0.23917f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02225f, 1.5655f, 0.17347f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false)
                    },
                    "held RH f5-7 scale-in, nocked on string f8-13"),
                new AttackTrack(
                    "ROWAN_arrow_blue_fletch_inflight",
                    AttackTrackKind.Projectile,
                    "ROWAN_arrow_blue_fletch.fbx",
                    "",
                    0,
                    14,
                    14f,
                    AttackVfxShape.None,
                    0f,
                    0f,
                    new float[0],
                    new float[0],
                    new float[0],
                    new[] { 0.03886f, 0.0343f, 0.8025f },
                    new[]
                    {
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 0.61667f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 1.08333f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 1.55f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 2.01667f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 2.48333f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 2.95f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 3.41667f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 3.88333f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 4.35f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 4.81667f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 5.28333f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 5.75f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 6.21667f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 6.68333f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 7.15f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 7.61667f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "arrow flies character-forward (-Y Blender) at 14 m/s"),
                new AttackTrack(
                    "ROWAN_arrow_trail_vfx",
                    AttackTrackKind.Projectile,
                    "",
                    "",
                    0,
                    14,
                    14f,
                    AttackVfxShape.Trail,
                    0.05f,
                    0.9f,
                    new[] { 0.73536f, 0.85431f, 1f },
                    new[] { 0.22092f, 0.39588f, 0.85431f },
                    new[] { 0.73536f, 0.85431f, 1f },
                    new[] { 0.026f, 0.02473f, 0.9f },
                    new[]
                    {
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.02f, 1.57f, 0.15f, 0f, 0f, 0f, 1f, 0.35f, true),
                        new AttackFrame(-0.02f, 1.57f, 0.61667f, 0f, 0f, 0f, 1f, 0.56667f, true),
                        new AttackFrame(-0.02f, 1.57f, 1.08333f, 0f, 0f, 0f, 1f, 0.78333f, true),
                        new AttackFrame(-0.02f, 1.57f, 1.55f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 2.01667f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 2.48333f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 2.95f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 3.41667f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 3.88333f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 4.35f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 4.81667f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 5.28333f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 5.75f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 6.21667f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 6.68333f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 7.15f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02f, 1.57f, 7.61667f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "pale-blue trail behind arrow")
            });
    }
}
