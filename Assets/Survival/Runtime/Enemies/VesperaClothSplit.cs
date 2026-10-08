using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Vespera Design cloth-rule split (2026-10-07, clothsplit_20261007): body on the 22 mixamorig bones,
    /// loose cloth below z 0.906 m on 8 chains parented to mixamorig:Spine (50 bones total).
    /// The ThemePack rest/walk/attack FBXs are these files under the existing names. Cloth bones are bound by name and
    /// driven only by ClothSpringRig at runtime (never baked). Values from NOTE.md; HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class VesperaClothSplit
    {
        public const string DesignDir = "design/survival-theme-a-fantasy/enemies/anim/vespera/clothsplit_20261007";
        public const string RestFileName = "VESPERA_clothsplit.fbx";
        public const string WalkFileName = "VESPERA_clothsplit_walk.fbx";
        public const string AttackFileName = "VESPERA_clothsplit_attack.fbx";
        public const string BlendFileName = "VESPERA_clothsplit.blend";
        public const string RestMd5 = "25e3a3c6b89923e8f58d579a0f8e6c74";
        public const string WalkMd5 = "d7f30d54e3a0b503c3dcb01828ff2c7b";
        public const string AttackMd5 = "c0abe43d57027edef1879a283efcd8f7";
        public const string BlendMd5 = "4edd4285e23a504513800d6df314bf27";
        public const int RigBoneCount = 50;
        public const float SplitHeightMetres = 0.906f;

        /// <summary>ThemePack MD5s before the swap (attack tip), kept for the record.</summary>
        public const string PreviousRestMd5 = "969964e9fc0d5818b8877548d83bdecc";
        public const string PreviousWalkMd5 = "25fff5842afb46fe50d0703236754cfb";
        public const string PreviousAttackMd5 = "082b741eae52d9144a612566380766bc";

        public static readonly ClothSplitSpec Spec = new ClothSplitSpec(
            "Vespera",
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
                    0.48f,
                    780,
                    0.35f,
                    0.4f,
                    0.45f,
                    0.4f,
                    0.05f,
                    60.0f,
                    new[] { -0.07626f, -0.3597f, 0.906f, -0.09635f, -0.3745f, 0.74943f, -0.10431f, -0.35364f, 0.59285f, -0.12401f, -0.33404f, 0.43628f }),
                new ClothChainSpec(
                    "skirt_FL",
                    "FL",
                    new[] { "skirt_FL_01", "skirt_FL_02", "skirt_FL_03" },
                    "mixamorig:Spine",
                    0.61f,
                    1163,
                    0.35f,
                    0.4f,
                    0.45f,
                    0.4f,
                    0.05f,
                    60.0f,
                    new[] { 0.08833f, -0.32594f, 0.906f, 0.09288f, -0.354f, 0.7181f, 0.15228f, -0.35692f, 0.5302f, 0.27002f, -0.37016f, 0.3423f }),
                new ClothChainSpec(
                    "skirt_L",
                    "L",
                    new[] { "skirt_L_01", "skirt_L_02", "skirt_L_03", "skirt_L_04" },
                    "mixamorig:Spine",
                    0.9f,
                    7166,
                    0.25f,
                    0.35f,
                    0.4f,
                    0.6f,
                    0.06f,
                    75.0f,
                    new[] { 0.14387f, -0.25344f, 0.906f, 0.22466f, -0.23264f, 0.69415f, 0.27744f, -0.20553f, 0.48231f, 0.34444f, -0.1992f, 0.27046f, 0.42911f, -0.16556f, 0.05861f }),
                new ClothChainSpec(
                    "cape_L",
                    "BL",
                    new[] { "cape_L_01", "cape_L_02", "cape_L_03", "cape_L_04" },
                    "mixamorig:Spine",
                    0.89f,
                    5516,
                    0.25f,
                    0.35f,
                    0.4f,
                    0.6f,
                    0.06f,
                    75.0f,
                    new[] { 0.10376f, -0.03156f, 0.906f, 0.16822f, 0.01782f, 0.70643f, 0.23126f, -0.00761f, 0.50686f, 0.21918f, 0.15225f, 0.30728f, 0.26854f, 0.16763f, 0.10771f }),
                new ClothChainSpec(
                    "cape_C",
                    "B",
                    new[] { "cape_C_01", "cape_C_02", "cape_C_03", "cape_C_04" },
                    "mixamorig:Spine",
                    0.84f,
                    5338,
                    0.25f,
                    0.35f,
                    0.4f,
                    0.6f,
                    0.06f,
                    75.0f,
                    new[] { -0.0901f, 0.02284f, 0.906f, -0.12912f, 0.0949f, 0.70983f, -0.11914f, 0.17383f, 0.51366f, -0.14291f, 0.24316f, 0.3175f, -0.12641f, 0.29074f, 0.12133f }),
                new ClothChainSpec(
                    "cape_R",
                    "BR",
                    new[] { "cape_R_01", "cape_R_02", "cape_R_03", "cape_R_04" },
                    "mixamorig:Spine",
                    0.88f,
                    5252,
                    0.25f,
                    0.35f,
                    0.4f,
                    0.6f,
                    0.06f,
                    75.0f,
                    new[] { -0.29459f, -0.02099f, 0.906f, -0.27791f, 0.01444f, 0.69501f, -0.32988f, 0.06324f, 0.48403f, -0.41664f, 0.08416f, 0.27304f, -0.46934f, 0.07751f, 0.06205f }),
                new ClothChainSpec(
                    "skirt_R",
                    "R",
                    new[] { "skirt_R_01", "skirt_R_02", "skirt_R_03" },
                    "mixamorig:Spine",
                    0.76f,
                    1838,
                    0.25f,
                    0.35f,
                    0.4f,
                    0.6f,
                    0.06f,
                    75.0f,
                    new[] { -0.28289f, -0.22415f, 0.906f, -0.33929f, -0.19568f, 0.69004f, -0.41479f, -0.14249f, 0.47408f, -0.27832f, -0.29037f, 0.25812f }),
                new ClothChainSpec(
                    "skirt_FR",
                    "FR",
                    new[] { "skirt_FR_01", "skirt_FR_02", "skirt_FR_03" },
                    "mixamorig:Spine",
                    0.61f,
                    1097,
                    0.35f,
                    0.4f,
                    0.45f,
                    0.4f,
                    0.05f,
                    60.0f,
                    new[] { -0.19505f, -0.2917f, 0.906f, -0.19748f, -0.29924f, 0.70299f, -0.20753f, -0.31147f, 0.49997f, -0.2205f, -0.30502f, 0.29696f })
            },
            ClothSplitSpec.MixamoLegColliders(0.07f, 0.055f, 0.15f));
    }
}
