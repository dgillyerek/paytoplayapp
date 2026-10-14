using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Bonequill cape (gritty_20261013: tattered purple hooded cloak from the approved gritty look): a separate mesh + FBX
    /// (BONEQUILL_cape.fbx, 39,999 faces) on its own armature. Anchors cape_Head / cape_Spine2 / cape_LeftShoulder / cape_RightShoulder
    /// copy the body Head, Spine2 and shoulders (same rest matrices); the hood is rigid on Head (rides the skull) and the mantle
    /// (above ~1.30 m) is rigid on Spine2/shoulders. Fresh_20261013 v2: thrown back over the shoulders (open front, arms fully outside); seven 3-bone chains hang
    /// down the back and sides (B, BL, BR, L, R on cape_Spine2; FL/FR on the shoulder anchors). No leg influence and never baked: BonequillActor (Survival.Unity) reparents the anchors onto
    /// the body bones and ClothSpringRig springs the chains with leg colliders. Arm capsules (upper arm, forearm, hand) join the leg colliders. Stiffness falls off down each chain so the hem swings.
    /// </summary>
    public static class BonequillCape
    {
        public const string FileName = "BONEQUILL_cape.fbx";
        public const string FileMd5 = "bca09353872255eb5ec9a3d5e2c1893d";
        public const string TextureFolder = "BONEQUILL_cape.fbm";
        public const string BaseColorFile = "BONEQUILL_cape_basecolor_0.jpg";
        public const string NormalFile = "BONEQUILL_cape_normal_2.jpg";
        public const int CapeBoneCount = 25;

        /// <summary>Cape anchor bone, then the body bone it is reparented onto.</summary>
        public static readonly string[,] Anchors =
        {
            { "cape_Head", "mixamorig:Head" },
            { "cape_LeftShoulder", "mixamorig:LeftShoulder" },
            { "cape_RightShoulder", "mixamorig:RightShoulder" },
            { "cape_Spine2", "mixamorig:Spine2" }
        };

        /// <summary>Per-joint stiffness down each chain: firm at the mantle seam, loose at the hem so it swings.</summary>
        public static readonly float[] JointStiffness = { 0.4f, 0.16f, 0.07f };

        /// <summary>Upper arm, forearm and hand (wrist to fingertips) capsules keep the cape behind the arms.</summary>
        public static readonly ClothColliderSpec[] ArmColliders =
        {
            new ClothColliderSpec("LeftUpperArm", ClothColliderKind.Capsule, "mixamorig:LeftArm", "mixamorig:LeftForeArm", 0.055f),
            new ClothColliderSpec("LeftForeArm", ClothColliderKind.Capsule, "mixamorig:LeftForeArm", "mixamorig:LeftHand", 0.05f),
            new ClothColliderSpec("LeftHand", ClothColliderKind.Capsule, "mixamorig:LeftHand", "mixamorig:LeftHand", 0.055f, 0.17f),
            new ClothColliderSpec("RightUpperArm", ClothColliderKind.Capsule, "mixamorig:RightArm", "mixamorig:RightForeArm", 0.055f),
            new ClothColliderSpec("RightForeArm", ClothColliderKind.Capsule, "mixamorig:RightForeArm", "mixamorig:RightHand", 0.05f),
            new ClothColliderSpec("RightHand", ClothColliderKind.Capsule, "mixamorig:RightHand", "mixamorig:RightHand", 0.055f, 0.17f)
        };

        public static readonly ClothChainSpec[] Chains =
        {
            new ClothChainSpec(
                "cape_B",
                "B",
                new[] { "cape_B_01", "cape_B_02", "cape_B_03" },
                "cape_Spine2",
                1.11425f,
                0,
                0.3f,
                0.12f,
                0.25f,
                0.15f,
                0.04f,
                60f,
                new[] { -0.00293f, 0.15126f, 1.3f, -0.01371f, 0.17615f, 0.92677f, -0.00416f, 0.21028f, 0.55353f, -0.02463f, 0.21126f, 0.18899f },
                JointStiffness),
            new ClothChainSpec(
                "cape_BL",
                "BL",
                new[] { "cape_BL_01", "cape_BL_02", "cape_BL_03" },
                "cape_Spine2",
                1.51029f,
                0,
                0.3f,
                0.12f,
                0.25f,
                0.15f,
                0.04f,
                60f,
                new[] { 0.07405f, 0.1899f, 1.3f, 0.14694f, 0.29109f, 0.80661f, 0.21781f, 0.41147f, 0.31322f, 0.21495f, 0.3958f, -0.17512f },
                JointStiffness),
            new ClothChainSpec(
                "cape_L",
                "L",
                new[] { "cape_L_01", "cape_L_02", "cape_L_03" },
                "cape_Spine2",
                1.2517f,
                0,
                0.3f,
                0.12f,
                0.25f,
                0.15f,
                0.04f,
                60f,
                new[] { 0.21748f, 0.23233f, 1.3f, 0.24583f, 0.34975f, 0.88985f, 0.27465f, 0.29649f, 0.47969f, 0.29516f, 0.36873f, 0.07711f },
                JointStiffness),
            new ClothChainSpec(
                "cape_FL",
                "FL",
                new[] { "cape_FL_01", "cape_FL_02", "cape_FL_03" },
                "cape_LeftShoulder",
                0.38436f,
                0,
                0.3f,
                0.12f,
                0.25f,
                0.15f,
                0.04f,
                60f,
                new[] { 0.27096f, 0.2013f, 1.3f, 0.26525f, 0.19454f, 1.1669f, 0.25732f, 0.1997f, 1.0338f, 0.24826f, 0.20297f, 0.91666f },
                JointStiffness),
            new ClothChainSpec(
                "cape_FR",
                "FR",
                new[] { "cape_FR_01", "cape_FR_02", "cape_FR_03" },
                "cape_RightShoulder",
                0.22637f,
                0,
                0.3f,
                0.12f,
                0.25f,
                0.15f,
                0.04f,
                60f,
                new[] { -0.30475f, 0.21095f, 1.3f, -0.29072f, 0.21042f, 1.21919f, -0.28424f, 0.21132f, 1.13839f, -0.28604f, 0.2119f, 1.07513f },
                JointStiffness),
            new ClothChainSpec(
                "cape_R",
                "R",
                new[] { "cape_R_01", "cape_R_02", "cape_R_03" },
                "cape_Spine2",
                1.28994f,
                0,
                0.3f,
                0.12f,
                0.25f,
                0.15f,
                0.04f,
                60f,
                new[] { -0.21518f, 0.24807f, 1.3f, -0.24514f, 0.33327f, 0.87209f, -0.2709f, 0.33702f, 0.44417f, -0.29334f, 0.38217f, 0.02329f },
                JointStiffness),
            new ClothChainSpec(
                "cape_BR",
                "BR",
                new[] { "cape_BR_01", "cape_BR_02", "cape_BR_03" },
                "cape_Spine2",
                1.48715f,
                0,
                0.3f,
                0.12f,
                0.25f,
                0.15f,
                0.04f,
                60f,
                new[] { -0.0909f, 0.1909f, 1.3f, -0.12983f, 0.2701f, 0.81221f, -0.19121f, 0.39442f, 0.32442f, -0.17035f, 0.35881f, -0.15815f },
                JointStiffness)
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
            System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(ClothSplitSpec.MixamoLegColliders(0.08f, 0.065f, 0.17f), ArmColliders)));
    }
}
