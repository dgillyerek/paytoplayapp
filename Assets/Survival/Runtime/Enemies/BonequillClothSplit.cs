using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Bonequill Design cloth-rule split (2026-10-07, clothsplit_20261007): body on the 22 mixamorig bones,
    /// loose cloth below z 0.906 m on 8 chains parented to mixamorig:Spine (49 bones total).
    /// The ThemePack rest/walk/attack FBXs are these files under the existing names. Cloth bones are bound by name and
    /// driven only by ClothSpringRig at runtime (never baked). Values from NOTE.md; HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class BonequillClothSplit
    {
        public const string DesignDir = "design/survival-theme-a-fantasy/enemies/anim/bonequill/clothsplit_20261007";
        public const string RestFileName = "BONEQUILL_clothsplit.fbx";
        public const string WalkFileName = "BONEQUILL_clothsplit_walk.fbx";
        public const string AttackFileName = "BONEQUILL_clothsplit_attack.fbx";
        public const string BlendFileName = "BONEQUILL_clothsplit.blend";
        public const string RestMd5 = "fffd8059f34c0b0af1dfe7a0daf52135";
        public const string WalkMd5 = "4c0b25ec1d5f7bc0646a234f59a965f0";
        public const string AttackMd5 = "bbdf2f41a9a6bfe48a4bed2c422472a9";
        public const string BlendMd5 = "86b1ffffc58030831499375f9cff1b4e";
        public const int RigBoneCount = 49;
        public const float SplitHeightMetres = 0.906f;

        /// <summary>ThemePack MD5s before the swap (attack tip), kept for the record.</summary>
        public const string PreviousRestMd5 = "301d285e17d06fe17a6bf76184131a4b";
        public const string PreviousWalkMd5 = "f9f8bb27a40bcf6faf8ef0615a6cd645";
        public const string PreviousAttackMd5 = "16cfb8a07ffe13e6ac1f48b31791523c";

        public static readonly ClothSplitSpec Spec = new ClothSplitSpec(
            "Bonequill",
            DesignDir,
            RigBoneCount,
            SplitHeightMetres,
            RestFileName,
            WalkFileName,
            AttackFileName,
            BlendFileName,
            RestMd5,
            WalkMd5,
            AttackMd5,
            BlendMd5,
            new[]
            {
                new ClothChainSpec(
                    "skirt_F",
                    "F",
                    new[] { "skirt_F_01", "skirt_F_02", "skirt_F_03" },
                    "mixamorig:Spine",
                    0.57f,
                    1741,
                    0.35f,
                    0.4f,
                    0.45f,
                    0.4f,
                    0.05f,
                    60.0f,
                    new[] { 0.16384f, -0.21273f, 0.906f, 0.1729f, -0.21188f, 0.71899f, 0.12443f, -0.18097f, 0.53198f, 0.09175f, -0.15686f, 0.34497f }),
                new ClothChainSpec(
                    "skirt_FL",
                    "FL",
                    new[] { "skirt_FL_01", "skirt_FL_02", "skirt_FL_03" },
                    "mixamorig:Spine",
                    0.44f,
                    953,
                    0.35f,
                    0.4f,
                    0.45f,
                    0.4f,
                    0.05f,
                    60.0f,
                    new[] { 0.33957f, -0.17325f, 0.906f, 0.32f, -0.20819f, 0.77904f, 0.42516f, -0.19504f, 0.65208f, 0.47495f, -0.17836f, 0.52512f }),
                new ClothChainSpec(
                    "skirt_L",
                    "L",
                    new[] { "skirt_L_01", "skirt_L_02", "skirt_L_03", "skirt_L_04" },
                    "mixamorig:Spine",
                    0.77f,
                    1938,
                    0.25f,
                    0.35f,
                    0.4f,
                    0.6f,
                    0.06f,
                    75.0f,
                    new[] { 0.38353f, -0.03328f, 0.906f, 0.43906f, -0.05284f, 0.7239f, 0.52524f, -0.03294f, 0.54179f, 0.57922f, -0.00456f, 0.35969f, 0.60322f, -0.03213f, 0.17758f }),
                new ClothChainSpec(
                    "robe_BL",
                    "BL",
                    new[] { "robe_BL_01", "robe_BL_02", "robe_BL_03", "robe_BL_04" },
                    "mixamorig:Spine",
                    0.8f,
                    1545,
                    0.25f,
                    0.35f,
                    0.4f,
                    0.6f,
                    0.06f,
                    75.0f,
                    new[] { 0.26274f, 0.16422f, 0.906f, 0.26953f, 0.14391f, 0.71569f, 0.3482f, 0.16422f, 0.52538f, 0.40694f, 0.21716f, 0.33508f, 0.43249f, 0.24528f, 0.14477f }),
                new ClothChainSpec(
                    "robe_B",
                    "B",
                    new[] { "robe_B_01", "robe_B_02", "robe_B_03", "robe_B_04" },
                    "mixamorig:Spine",
                    0.85f,
                    1582,
                    0.25f,
                    0.35f,
                    0.4f,
                    0.6f,
                    0.06f,
                    75.0f,
                    new[] { 0.14625f, 0.21201f, 0.906f, 0.15653f, 0.19219f, 0.70465f, 0.17441f, 0.2681f, 0.5033f, 0.1494f, 0.32842f, 0.30195f, 0.23741f, 0.37167f, 0.1006f }),
                new ClothChainSpec(
                    "robe_BR",
                    "BR",
                    new[] { "robe_BR_01", "robe_BR_02", "robe_BR_03" },
                    "mixamorig:Spine",
                    0.75f,
                    4272,
                    0.25f,
                    0.35f,
                    0.4f,
                    0.6f,
                    0.06f,
                    75.0f,
                    new[] { -0.04436f, 0.14015f, 0.906f, -0.08051f, 0.19105f, 0.67101f, -0.15454f, 0.28916f, 0.43603f, -0.1084f, 0.34109f, 0.20104f }),
                new ClothChainSpec(
                    "skirt_R",
                    "R",
                    new[] { "skirt_R_01", "skirt_R_02", "skirt_R_03" },
                    "mixamorig:Spine",
                    0.59f,
                    2111,
                    0.35f,
                    0.4f,
                    0.45f,
                    0.4f,
                    0.05f,
                    75.0f,
                    new[] { -0.149f, 0.00634f, 0.906f, -0.14748f, 0.00572f, 0.72403f, -0.20536f, 0.02087f, 0.54206f, -0.29488f, 0.10957f, 0.36009f }),
                new ClothChainSpec(
                    "skirt_FR",
                    "FR",
                    new[] { "skirt_FR_01", "skirt_FR_02", "skirt_FR_03" },
                    "mixamorig:Spine",
                    0.56f,
                    1502,
                    0.35f,
                    0.4f,
                    0.45f,
                    0.4f,
                    0.05f,
                    60.0f,
                    new[] { -0.00355f, -0.15973f, 0.906f, -0.00608f, -0.14609f, 0.72149f, 0.02046f, -0.15671f, 0.53698f, 0.06895f, -0.15231f, 0.35248f })
            },
            ClothSplitSpec.MixamoLegColliders(0.07f, 0.055f, 0.15f));
    }
}
