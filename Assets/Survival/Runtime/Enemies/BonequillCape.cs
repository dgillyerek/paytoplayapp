using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Bonequill cape (fresh_20261013): a separate mesh + FBX (BONEQUILL_cape.fbx, 39,999 faces) on its own armature. Anchors
    /// cape_Spine2 / cape_LeftShoulder / cape_RightShoulder copy the body Spine2 and shoulders (same rest matrices); the hood and
    /// mantle (above ~1.30 m) are rigid on them. Seven 3-bone chains hang below (B, BL, BR, L, R on cape_Spine2; FL/FR on the
    /// shoulder anchors). No leg influence and never baked: BonequillActor (Survival.Unity) reparents the anchors onto
    /// the body bones and ClothSpringRig springs the chains with leg colliders. Low gravity and high stiffness keep the rest shape.
    /// </summary>
    public static class BonequillCape
    {
        public const string FileName = "BONEQUILL_cape.fbx";
        public const string FileMd5 = "b0ee4a8b7dbd0d900a4bf81e4961b6b1";
        public const string TextureFolder = "BONEQUILL_cape.fbm";
        public const string BaseColorFile = "BONEQUILL_cape_basecolor_0.jpg";
        public const string NormalFile = "BONEQUILL_cape_normal_2.jpg";
        public const int CapeBoneCount = 24;

        /// <summary>Cape anchor bone, then the body bone it is reparented onto.</summary>
        public static readonly string[,] Anchors =
        {
            { "cape_LeftShoulder", "mixamorig:LeftShoulder" },
            { "cape_RightShoulder", "mixamorig:RightShoulder" },
            { "cape_Spine2", "mixamorig:Spine2" }
        };

        public static readonly ClothChainSpec[] Chains =
        {
            new ClothChainSpec(
                "cape_B",
                "B",
                new[] { "cape_B_01", "cape_B_02", "cape_B_03" },
                "cape_Spine2",
                1.29578f,
                0,
                0.55f,
                0.35f,
                0.4f,
                0.12f,
                0.04f,
                45f,
                new[] { 0.00152f, 0.19598f, 1.3f, -0.00051f, 0.18355f, 0.86588f, 0.00459f, 0.18035f, 0.43175f, 0.0062f, 0.17532f, 0.00447f }),
            new ClothChainSpec(
                "cape_BL",
                "BL",
                new[] { "cape_BL_01", "cape_BL_02", "cape_BL_03" },
                "cape_Spine2",
                1.37408f,
                0,
                0.55f,
                0.35f,
                0.4f,
                0.12f,
                0.04f,
                45f,
                new[] { 0.12049f, 0.17508f, 1.3f, 0.13564f, 0.18044f, 0.84111f, 0.16007f, 0.1785f, 0.38221f, 0.1433f, 0.13325f, -0.07059f }),
            new ClothChainSpec(
                "cape_L",
                "L",
                new[] { "cape_L_01", "cape_L_02", "cape_L_03" },
                "cape_Spine2",
                1.35556f,
                0,
                0.55f,
                0.35f,
                0.4f,
                0.12f,
                0.04f,
                45f,
                new[] { 0.23889f, -0.01135f, 1.3f, 0.28151f, -0.00472f, 0.85468f, 0.27741f, 0.00241f, 0.40935f, 0.15923f, 0.08968f, -0.02946f }),
            new ClothChainSpec(
                "cape_FL",
                "FL",
                new[] { "cape_FL_01", "cape_FL_02", "cape_FL_03" },
                "cape_LeftShoulder",
                1.18667f,
                0,
                0.55f,
                0.35f,
                0.4f,
                0.12f,
                0.04f,
                45f,
                new[] { 0.16978f, -0.0928f, 1.3f, 0.17892f, -0.17618f, 0.90846f, 0.1561f, -0.11397f, 0.51691f, 0.20111f, -0.06502f, 0.13351f }),
            new ClothChainSpec(
                "cape_FR",
                "FR",
                new[] { "cape_FR_01", "cape_FR_02", "cape_FR_03" },
                "cape_RightShoulder",
                1.17117f,
                0,
                0.55f,
                0.35f,
                0.4f,
                0.12f,
                0.04f,
                45f,
                new[] { -0.16326f, -0.08517f, 1.3f, -0.15576f, -0.13021f, 0.90917f, -0.17586f, -0.10277f, 0.51834f, -0.20071f, -0.06465f, 0.13566f }),
            new ClothChainSpec(
                "cape_R",
                "R",
                new[] { "cape_R_01", "cape_R_02", "cape_R_03" },
                "cape_Spine2",
                1.3744f,
                0,
                0.55f,
                0.35f,
                0.4f,
                0.12f,
                0.04f,
                45f,
                new[] { -0.24063f, -0.00906f, 1.3f, -0.2844f, -0.00423f, 0.84639f, -0.26768f, 0.02287f, 0.39278f, -0.16739f, 0.09388f, -0.05458f }),
            new ClothChainSpec(
                "cape_BR",
                "BR",
                new[] { "cape_BR_01", "cape_BR_02", "cape_BR_03" },
                "cape_Spine2",
                1.37181f,
                0,
                0.55f,
                0.35f,
                0.4f,
                0.12f,
                0.04f,
                45f,
                new[] { -0.11874f, 0.17808f, 1.3f, -0.16182f, 0.17169f, 0.84217f, -0.16207f, 0.17653f, 0.38434f, -0.1415f, 0.1352f, -0.06737f })
        };

        public static readonly ClothSplitSpec Spec = new ClothSplitSpec(
            "Bonequill",
            BonequillMotion.DesignDir,
            BonequillMotion.BoneCount,
            1.30f,
            BonequillMotion.RestFileName,
            BonequillMotion.WalkFileName,
            BonequillAttack.FileName,
            FileName,
            BonequillMotion.RestMd5,
            BonequillMotion.WalkMd5,
            BonequillAttack.FileMd5,
            FileMd5,
            Chains,
            ClothSplitSpec.MixamoLegColliders(0.08f, 0.065f, 0.17f));
    }
}
