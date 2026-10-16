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
        public const string FileMd5 = "515d2fa42c962f8c5eeacc8ce224c765";
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
                1.11456f,
                0,
                0.3f,
                0.45f,
                0.25f,
                0.15f,
                0.04f,
                35f,
                new[] { -0.00737f, 0.14935f, 1.3f, -0.01702f, 0.17227f, 0.92676f, -0.00372f, 0.20975f, 0.55353f, -0.02458f, 0.21118f, 0.18898f },
                JointStiffness),
            new ClothChainSpec(
                "cape_BL",
                "BL",
                new[] { "cape_BL_01", "cape_BL_02", "cape_BL_03" },
                "cape_Spine2",
                1.5106f,
                0,
                0.3f,
                0.45f,
                0.25f,
                0.15f,
                0.04f,
                35f,
                new[] { 0.07615f, 0.18766f, 1.3f, 0.14671f, 0.29049f, 0.80659f, 0.21814f, 0.41157f, 0.31318f, 0.21497f, 0.39583f, -0.17518f },
                JointStiffness),
            new ClothChainSpec(
                "cape_L",
                "L",
                new[] { "cape_L_01", "cape_L_02", "cape_L_03" },
                "cape_Spine2",
                1.25285f,
                0,
                0.3f,
                0.45f,
                0.25f,
                0.15f,
                0.04f,
                35f,
                new[] { 0.21807f, 0.23365f, 1.3f, 0.24607f, 0.34987f, 0.88935f, 0.27479f, 0.29696f, 0.4787f, 0.29585f, 0.3695f, 0.0756f },
                JointStiffness),
            new ClothChainSpec(
                "cape_FL",
                "FL",
                new[] { "cape_FL_01", "cape_FL_02", "cape_FL_03" },
                "cape_LeftShoulder",
                0.38469f,
                0,
                0.3f,
                0.45f,
                0.25f,
                0.15f,
                0.04f,
                35f,
                new[] { 0.27491f, 0.20216f, 1.3f, 0.26525f, 0.19427f, 1.16689f, 0.25749f, 0.19945f, 1.03379f, 0.24815f, 0.20305f, 0.91665f },
                JointStiffness),
            new ClothChainSpec(
                "cape_FR",
                "FR",
                new[] { "cape_FR_01", "cape_FR_02", "cape_FR_03" },
                "cape_RightShoulder",
                0.22619f,
                0,
                0.3f,
                0.45f,
                0.25f,
                0.15f,
                0.04f,
                35f,
                new[] { -0.30411f, 0.21044f, 1.3f, -0.28993f, 0.20998f, 1.21929f, -0.28224f, 0.20967f, 1.13858f, -0.28351f, 0.20978f, 1.07543f },
                JointStiffness),
            new ClothChainSpec(
                "cape_R",
                "R",
                new[] { "cape_R_01", "cape_R_02", "cape_R_03" },
                "cape_Spine2",
                1.29497f,
                0,
                0.3f,
                0.45f,
                0.25f,
                0.15f,
                0.04f,
                35f,
                new[] { -0.21677f, 0.24898f, 1.3f, -0.24187f, 0.33274f, 0.87037f, -0.27084f, 0.33807f, 0.44074f, -0.29528f, 0.38443f, 0.01808f },
                JointStiffness),
            new ClothChainSpec(
                "cape_BR",
                "BR",
                new[] { "cape_BR_01", "cape_BR_02", "cape_BR_03" },
                "cape_Spine2",
                1.48582f,
                0,
                0.3f,
                0.45f,
                0.25f,
                0.15f,
                0.04f,
                35f,
                new[] { -0.09167f, 0.19095f, 1.3f, -0.12991f, 0.27012f, 0.81218f, -0.19126f, 0.3944f, 0.32436f, -0.19161f, 0.3744f, -0.15824f },
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
