using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Rowan coat tails (A-pose rebuild 2026-10-14, Derek's cloth rule): the long coat below the belt (z &lt; 0.90 m) is split off the
    /// body into its own mesh + FBX (ROWAN_coat.fbx) on its own armature. Anchor coat_Hips copies the body Hips (same rest matrix);
    /// the coat is rigid on it above 0.93 m, blending to the chains from 0.93 to 0.85 m. Seven 3-bone chains (B, BL, L, FL, FR, R, BR)
    /// hang from coat_Hips, never from the legs. Never baked: RowanActor reparents coat_Hips onto mixamorig:Hips and ClothSpringRig
    /// springs the chains with leg, hips and arm colliders. Stiffness falls off down each chain; damping 0.45, angle 35 deg,
    /// ClothSpringSolver velocity/push clamps. The body walk/attack moves body bones only.
    /// </summary>
    public static class RowanCoat
    {
        public const string FileName = "ROWAN_coat.fbx";
        public const string FileMd5 = "74c730bf44a05c17c77071d08a57bcfb";
        public const string TextureFolder = "ROWAN_coat.fbm";
        public const string BaseColorFile = "ROWAN_coat_basecolor_0.jpg";
        public const string NormalFile = "ROWAN_coat_normal_2.jpg";
        public const int CoatBoneCount = 22;
        public const float SplitHeightMetres = 0.90f;

        /// <summary>Coat anchor bone, then the body bone it is reparented onto.</summary>
        public static readonly string[,] Anchors =
        {
            { "coat_Hips", "mixamorig:Hips" }
        };

        /// <summary>Per-joint stiffness down each chain: firm at the belt seam, loose at the hem so it swings.</summary>
        public static readonly float[] JointStiffness = { 0.55f, 0.4f, 0.3f };

        /// <summary>Forearm and hand capsules keep the tails off the hands in the walk swing.</summary>
        public static readonly ClothColliderSpec[] ArmColliders =
        {
            new ClothColliderSpec("LeftForeArm", ClothColliderKind.Capsule, "mixamorig:LeftForeArm", "mixamorig:LeftHand", 0.05f),
            new ClothColliderSpec("LeftHand", ClothColliderKind.Capsule, "mixamorig:LeftHand", "mixamorig:LeftHand", 0.055f, 0.17f),
            new ClothColliderSpec("RightForeArm", ClothColliderKind.Capsule, "mixamorig:RightForeArm", "mixamorig:RightHand", 0.05f),
            new ClothColliderSpec("RightHand", ClothColliderKind.Capsule, "mixamorig:RightHand", "mixamorig:RightHand", 0.055f, 0.17f)
        };

        public static readonly ClothChainSpec[] Chains =
        {
            new ClothChainSpec(
                "coat_B",
                "B",
                new[] { "coat_B_01", "coat_B_02", "coat_B_03" },
                "coat_Hips",
                0.37751f,
                854,
                0.3f,
                0.45f,
                0.25f,
                0.15f,
                0.04f,
                35f,
                new[] { 0.00716f, 0.15092f, 0.9f, 0.01878f, 0.1631f, 0.77289f, 0.02809f, 0.17417f, 0.64579f, -0.02022f, 0.18343f, 0.53483f },
                JointStiffness),
            new ClothChainSpec(
                "coat_BL",
                "BL",
                new[] { "coat_BL_01", "coat_BL_02", "coat_BL_03" },
                "coat_Hips",
                0.43237f,
                982,
                0.3f,
                0.45f,
                0.25f,
                0.15f,
                0.04f,
                35f,
                new[] { 0.08961f, 0.12944f, 0.9f, 0.1698f, 0.12772f, 0.76067f, 0.19861f, 0.13915f, 0.62133f, 0.23389f, 0.1488f, 0.49777f },
                JointStiffness),
            new ClothChainSpec(
                "coat_L",
                "L",
                new[] { "coat_L_01", "coat_L_02", "coat_L_03" },
                "coat_Hips",
                0.52456f,
                2667,
                0.3f,
                0.45f,
                0.25f,
                0.15f,
                0.04f,
                35f,
                new[] { 0.21601f, 0.02775f, 0.9f, 0.24881f, 0.05504f, 0.72883f, 0.29246f, 0.06177f, 0.55765f, 0.32318f, -0.00128f, 0.40129f },
                JointStiffness),
            new ClothChainSpec(
                "coat_FL",
                "FL",
                new[] { "coat_FL_01", "coat_FL_02", "coat_FL_03" },
                "coat_Hips",
                0.51821f,
                1074,
                0.3f,
                0.45f,
                0.25f,
                0.15f,
                0.04f,
                35f,
                new[] { 0.21285f, -0.02444f, 0.9f, 0.24542f, -0.01518f, 0.72564f, 0.28741f, -0.0183f, 0.55128f, 0.30984f, -0.01988f, 0.39164f },
                JointStiffness),
            new ClothChainSpec(
                "coat_FR",
                "FR",
                new[] { "coat_FR_01", "coat_FR_02", "coat_FR_03" },
                "coat_Hips",
                0.51981f,
                1130,
                0.3f,
                0.45f,
                0.25f,
                0.15f,
                0.04f,
                35f,
                new[] { -0.21214f, -0.04421f, 0.9f, -0.23375f, -0.0514f, 0.72588f, -0.27727f, -0.04026f, 0.55177f, -0.31206f, -0.02008f, 0.39237f },
                JointStiffness),
            new ClothChainSpec(
                "coat_R",
                "R",
                new[] { "coat_R_01", "coat_R_02", "coat_R_03" },
                "coat_Hips",
                0.52789f,
                2439,
                0.3f,
                0.45f,
                0.25f,
                0.15f,
                0.04f,
                35f,
                new[] { -0.19786f, 0.07924f, 0.9f, -0.26016f, 0.05282f, 0.73009f, -0.29809f, 0.0668f, 0.56019f, -0.32683f, 0.00238f, 0.40513f },
                JointStiffness),
            new ClothChainSpec(
                "coat_BR",
                "BR",
                new[] { "coat_BR_01", "coat_BR_02", "coat_BR_03" },
                "coat_Hips",
                0.42183f,
                637,
                0.3f,
                0.45f,
                0.25f,
                0.15f,
                0.04f,
                35f,
                new[] { -0.15903f, 0.11068f, 0.9f, -0.18103f, 0.12763f, 0.7612f, -0.17128f, 0.13814f, 0.62239f, -0.23884f, 0.14867f, 0.49938f },
                JointStiffness)
        };

        public static readonly ClothSplitSpec Spec = new ClothSplitSpec(
            "Rowan",
            RowanMotion.DesignDir,
            RowanMotion.BoneCount,
            SplitHeightMetres,
            RowanMotion.RestFileName,
            RowanMotion.WalkFileName,
            RowanAttack.FileName,
            FileName,
            RowanMotion.RestMd5,
            RowanMotion.WalkMd5,
            RowanAttack.FileMd5,
            FileMd5,
            Chains,
            System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(ClothSplitSpec.MixamoLegColliders(0.085f, 0.065f, 0.14f), ArmColliders)));
    }
}
