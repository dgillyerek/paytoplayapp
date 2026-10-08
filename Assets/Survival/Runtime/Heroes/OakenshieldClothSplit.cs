using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Oakenshield Design cloth-rule split (2026-10-07, clothsplit_20261007): body on the 22 mixamorig bones,
    /// loose cloth below z 0.8160000000000001 m on 5 chains parented to mixamorig:Spine (36 bones total).
    /// The ThemePack rest/walk/attack FBXs are these files under the existing names. Cloth bones are bound by name and
    /// driven only by ClothSpringRig at runtime (never baked). Values from NOTE.md; HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class OakenshieldClothSplit
    {
        public const string DesignDir = "design/survival-theme-a-fantasy/heroes/anim/oakenshield/clothsplit_20261007";
        public const string RestFileName = "OAKENSHIELD_clothsplit.fbx";
        public const string WalkFileName = "OAKENSHIELD_clothsplit_walk.fbx";
        public const string AttackFileName = "OAKENSHIELD_clothsplit_attack.fbx";
        public const string BlendFileName = "OAKENSHIELD_clothsplit.blend";
        public const string RestMd5 = "6d754014dd09a23ad616378a70826b14";
        public const string WalkMd5 = "57fba709bf3c367dd9ac0b4bc552be26";
        public const string AttackMd5 = "769543a3e39321857dc50e734aadffd2";
        public const string BlendMd5 = "9bf48c3405516ffe8740e7f9ae3e7550";
        public const int RigBoneCount = 36;
        public const float SplitHeightMetres = 0.8160000000000001f;

        /// <summary>ThemePack MD5s before the swap (attack tip), kept for the record.</summary>
        public const string PreviousRestMd5 = "9f0e892f6b56eba78c2de4078931bd63";
        public const string PreviousWalkMd5 = "03b747fc974e86dbb9afb52b209036ba";
        public const string PreviousAttackMd5 = "3fe681bc07658c519dbc1e903614c766";

        public static readonly ClothSplitSpec Spec = new ClothSplitSpec(
            "Oakenshield",
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
                    "vine_FL",
                    "FL",
                    new[] { "vine_FL_01", "vine_FL_02", "vine_FL_03" },
                    "mixamorig:Spine",
                    0.36f,
                    360,
                    0.5f,
                    0.45f,
                    0.5f,
                    0.25f,
                    0.04f,
                    60.0f,
                    new[] { 0.10556f, -0.14486f, 0.816f, 0.16202f, -0.16475f, 0.70391f, 0.20003f, -0.18323f, 0.59183f, 0.16524f, -0.16998f, 0.47974f }),
                new ClothChainSpec(
                    "vine_L",
                    "L",
                    new[] { "vine_L_01", "vine_L_02", "vine_L_03" },
                    "mixamorig:Spine",
                    0.46f,
                    3470,
                    0.35f,
                    0.4f,
                    0.45f,
                    0.4f,
                    0.05f,
                    75.0f,
                    new[] { 0.17977f, -0.08654f, 0.816f, 0.23838f, -0.06968f, 0.67572f, 0.23783f, -0.09997f, 0.53543f, 0.18038f, -0.02735f, 0.39515f }),
                new ClothChainSpec(
                    "leaf_BL",
                    "BL",
                    new[] { "leaf_BL_01", "leaf_BL_02", "leaf_BL_03" },
                    "mixamorig:Spine",
                    0.45f,
                    1717,
                    0.35f,
                    0.4f,
                    0.45f,
                    0.4f,
                    0.05f,
                    75.0f,
                    new[] { 0.0955f, 0.10882f, 0.816f, 0.10048f, 0.13715f, 0.67715f, 0.14345f, 0.12261f, 0.5383f, 0.21607f, 0.09982f, 0.39945f }),
                new ClothChainSpec(
                    "leaf_B",
                    "B",
                    new[] { "leaf_B_01", "leaf_B_02" },
                    "mixamorig:Spine",
                    0.25f,
                    1530,
                    0.5f,
                    0.45f,
                    0.5f,
                    0.25f,
                    0.04f,
                    75.0f,
                    new[] { -0.09021f, 0.19555f, 0.816f, -0.0506f, 0.22118f, 0.70346f, 0.01239f, 0.23659f, 0.59091f }),
                new ClothChainSpec(
                    "leaf_BR",
                    "BR",
                    new[] { "leaf_BR_01", "leaf_BR_02", "leaf_BR_03" },
                    "mixamorig:Spine",
                    0.53f,
                    2111,
                    0.35f,
                    0.4f,
                    0.45f,
                    0.4f,
                    0.05f,
                    75.0f,
                    new[] { -0.31479f, 0.18135f, 0.816f, -0.2431f, 0.1925f, 0.65617f, -0.27812f, 0.20167f, 0.49634f, -0.18047f, 0.16556f, 0.3365f })
            },
            ClothSplitSpec.MixamoLegColliders(0.07f, 0.055f, 0.15f));
    }
}
