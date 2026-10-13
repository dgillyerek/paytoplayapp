using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Bonequill cape (fresh_20261013): a separate mesh + FBX (BONEQUILL_cape.fbx, 39,999 faces) on its own armature. Anchors
    /// cape_Spine2 / cape_LeftShoulder / cape_RightShoulder copy the body Spine2 and shoulders (same rest matrices); the hood and
    /// mantle (above ~1.30 m) are rigid on them. Fresh_20261013 v2: thrown back over the shoulders (open front, arms fully outside); seven 3-bone chains hang
    /// down the back and sides (B, BL, BR, L, R on cape_Spine2; FL/FR on the shoulder anchors). No leg influence and never baked: BonequillActor (Survival.Unity) reparents the anchors onto
    /// the body bones and ClothSpringRig springs the chains with leg colliders. Arm capsules (upper arm, forearm, hand) join the leg colliders. Stiffness falls off down each chain so the hem swings.
    /// </summary>
    public static class BonequillCape
    {
        public const string FileName = "BONEQUILL_cape.fbx";
        public const string FileMd5 = "d08482d6e60e5618f0ce77b0ae91f0b0";
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
                1.44795f,
                0,
                0.3f,
                0.12f,
                0.25f,
                0.15f,
                0.04f,
                60f,
                new[] { 0.00147f, 0.19448f, 1.3f, -0.00865f, 0.23281f, 0.81857f, -0.00493f, 0.28195f, 0.33714f, 0.0104f, 0.21501f, -0.13887f },
                JointStiffness),
            new ClothChainSpec(
                "cape_BL",
                "BL",
                new[] { "cape_BL_01", "cape_BL_02", "cape_BL_03" },
                "cape_Spine2",
                1.46393f,
                0,
                0.3f,
                0.12f,
                0.25f,
                0.15f,
                0.04f,
                60f,
                new[] { 0.04433f, 0.20469f, 1.3f, 0.08991f, 0.2652f, 0.82016f, 0.11654f, 0.34347f, 0.34033f, 0.08469f, 0.21991f, -0.13405f },
                JointStiffness),
            new ClothChainSpec(
                "cape_L",
                "L",
                new[] { "cape_L_01", "cape_L_02", "cape_L_03" },
                "cape_Spine2",
                1.42394f,
                0,
                0.3f,
                0.12f,
                0.25f,
                0.15f,
                0.04f,
                60f,
                new[] { 0.21513f, 0.24373f, 1.3f, 0.18505f, 0.25905f, 0.82533f, 0.21645f, 0.29932f, 0.35065f, 0.1777f, 0.29785f, -0.11841f },
                JointStiffness),
            new ClothChainSpec(
                "cape_FL",
                "FL",
                new[] { "cape_FL_01", "cape_FL_02", "cape_FL_03" },
                "cape_LeftShoulder",
                0.79204f,
                0,
                0.3f,
                0.12f,
                0.25f,
                0.15f,
                0.04f,
                60f,
                new[] { 0.31008f, 0.19309f, 1.3f, 0.27697f, 0.22083f, 1.03366f, 0.26794f, 0.20695f, 0.76731f, 0.25317f, 0.22307f, 0.5129f },
                JointStiffness),
            new ClothChainSpec(
                "cape_FR",
                "FR",
                new[] { "cape_FR_01", "cape_FR_02", "cape_FR_03" },
                "cape_RightShoulder",
                0.79052f,
                0,
                0.3f,
                0.12f,
                0.25f,
                0.15f,
                0.04f,
                60f,
                new[] { -0.30523f, 0.19689f, 1.3f, -0.27935f, 0.20258f, 1.03336f, -0.26636f, 0.20684f, 0.76672f, -0.25321f, 0.22311f, 0.512f },
                JointStiffness),
            new ClothChainSpec(
                "cape_R",
                "R",
                new[] { "cape_R_01", "cape_R_02", "cape_R_03" },
                "cape_Spine2",
                1.42404f,
                0,
                0.3f,
                0.12f,
                0.25f,
                0.15f,
                0.04f,
                60f,
                new[] { -0.20805f, 0.24276f, 1.3f, -0.19064f, 0.26519f, 0.82482f, -0.21585f, 0.29787f, 0.34965f, -0.17867f, 0.30028f, -0.11993f },
                JointStiffness),
            new ClothChainSpec(
                "cape_BR",
                "BR",
                new[] { "cape_BR_01", "cape_BR_02", "cape_BR_03" },
                "cape_Spine2",
                1.46114f,
                0,
                0.3f,
                0.12f,
                0.25f,
                0.15f,
                0.04f,
                60f,
                new[] { -0.04492f, 0.20314f, 1.3f, -0.08931f, 0.26488f, 0.81951f, -0.11857f, 0.3343f, 0.33903f, -0.08357f, 0.22679f, -0.13602f },
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
