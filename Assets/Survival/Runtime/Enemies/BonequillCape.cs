using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Bonequill cape (gritty_20261013: tattered purple hooded cloak from the approved gritty look): a separate mesh + FBX
    /// (BONEQUILL_cape.fbx, 39,999 faces) on its own armature. Anchors cape_Head / cape_Spine2 / cape_LeftShoulder / cape_RightShoulder
    /// copy the body Head, Spine2 and shoulders (same rest matrices); the hood is rigid on Head (rides the skull) and the mantle
    /// (above ~1.30 m) is rigid on Spine2/shoulders. Fresh_20261013 v2: thrown back over the shoulders (open front, arms fully outside); seven 3-bone chains hang
    /// down the back and sides (B, BL, BR, L, R on cape_Spine2; FL/FR on the shoulder anchors). No leg influence and never baked: BonequillActor (Survival.Unity) reparents the anchors onto
    /// the body bones and ClothSpringRig springs the chains with leg colliders. Arm capsules (upper arm, forearm, hand) join the leg colliders. Outer mantle over the deltoids rides cape_LeftArm / cape_RightArm so the arms never push through the cloak at full draw. Stiffness falls off down each chain; damping 0.45, angle 35 deg, ClothSpringSolver velocity/push clamps stop the post-shot shake.
    /// </summary>
    public static class BonequillCape
    {
        public const string FileName = "BONEQUILL_cape.fbx";
        public const string FileMd5 = "72bf9d19af4eeff63985343c37811a15";
        public const string TextureFolder = "BONEQUILL_cape.fbm";
        public const string BaseColorFile = "BONEQUILL_cape_basecolor_0.jpg";
        public const string NormalFile = "BONEQUILL_cape_normal_2.jpg";
        public const int CapeBoneCount = 27;

        /// <summary>Cape anchor bone, then the body bone it is reparented onto.</summary>
        public static readonly string[,] Anchors =
        {
            { "cape_Head", "mixamorig:Head" },
            { "cape_LeftArm", "mixamorig:LeftArm" },
            { "cape_RightArm", "mixamorig:RightArm" },
            { "cape_LeftShoulder", "mixamorig:LeftShoulder" },
            { "cape_RightShoulder", "mixamorig:RightShoulder" },
            { "cape_Spine2", "mixamorig:Spine2" }
        };

        /// <summary>Per-joint stiffness down each chain: firm at the mantle seam, loose at the hem so it swings.</summary>
        public static readonly float[] JointStiffness = { 0.55f, 0.4f, 0.3f };

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
                1.11444f,
                0,
                0.3f,
                0.45f,
                0.25f,
                0.15f,
                0.04f,
                35f,
                new[] { -0.00765f, 0.14916f, 1.3f, -0.01724f, 0.17231f, 0.92683f, -0.0039f, 0.2106f, 0.55366f, -0.02463f, 0.21128f, 0.18918f },
                JointStiffness),
            new ClothChainSpec(
                "cape_BL",
                "BL",
                new[] { "cape_BL_01", "cape_BL_02", "cape_BL_03" },
                "cape_Spine2",
                1.51047f,
                0,
                0.3f,
                0.45f,
                0.25f,
                0.15f,
                0.04f,
                35f,
                new[] { 0.07609f, 0.18781f, 1.3f, 0.14673f, 0.29043f, 0.80662f, 0.21801f, 0.41156f, 0.31324f, 0.21499f, 0.39585f, -0.17509f },
                JointStiffness),
            new ClothChainSpec(
                "cape_L",
                "L",
                new[] { "cape_L_01", "cape_L_02", "cape_L_03" },
                "cape_Spine2",
                1.25134f,
                0,
                0.3f,
                0.45f,
                0.25f,
                0.15f,
                0.04f,
                35f,
                new[] { 0.21753f, 0.23349f, 1.3f, 0.24605f, 0.34998f, 0.88986f, 0.27511f, 0.29684f, 0.47972f, 0.29519f, 0.36876f, 0.07715f },
                JointStiffness),
            new ClothChainSpec(
                "cape_FL",
                "FL",
                new[] { "cape_FL_01", "cape_FL_02", "cape_FL_03" },
                "cape_LeftShoulder",
                0.38435f,
                0,
                0.3f,
                0.45f,
                0.25f,
                0.15f,
                0.04f,
                35f,
                new[] { 0.2697f, 0.20026f, 1.3f, 0.26341f, 0.19376f, 1.16688f, 0.25658f, 0.19923f, 1.03377f, 0.24801f, 0.20283f, 0.91661f },
                JointStiffness),
            new ClothChainSpec(
                "cape_FR",
                "FR",
                new[] { "cape_FR_01", "cape_FR_02", "cape_FR_03" },
                "cape_RightShoulder",
                0.22657f,
                0,
                0.3f,
                0.45f,
                0.25f,
                0.15f,
                0.04f,
                35f,
                new[] { -0.30624f, 0.21056f, 1.3f, -0.29014f, 0.20992f, 1.2192f, -0.28647f, 0.21239f, 1.1384f, -0.28781f, 0.2127f, 1.07515f },
                JointStiffness),
            new ClothChainSpec(
                "cape_R",
                "R",
                new[] { "cape_R_01", "cape_R_02", "cape_R_03" },
                "cape_Spine2",
                1.28964f,
                0,
                0.3f,
                0.45f,
                0.25f,
                0.15f,
                0.04f,
                35f,
                new[] { -0.21647f, 0.24884f, 1.3f, -0.24502f, 0.33334f, 0.87212f, -0.27112f, 0.33673f, 0.44424f, -0.29329f, 0.38213f, 0.02339f },
                JointStiffness),
            new ClothChainSpec(
                "cape_BR",
                "BR",
                new[] { "cape_BR_01", "cape_BR_02", "cape_BR_03" },
                "cape_Spine2",
                1.48559f,
                0,
                0.3f,
                0.45f,
                0.25f,
                0.15f,
                0.04f,
                35f,
                new[] { -0.09194f, 0.19088f, 1.3f, -0.12992f, 0.27014f, 0.81229f, -0.1914f, 0.39467f, 0.32457f, -0.1916f, 0.37436f, -0.15792f },
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
