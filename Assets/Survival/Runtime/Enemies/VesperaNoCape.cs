namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Vespera no-cape pack (Design blender_rig_nocape_20261009). Derek's cloth-rule fallback, same call as Rowan no-cape:
    /// the cape/cloak and the cloth split's 28 cloth bones are gone, the body is re-rigged on 22 mixamorig bones with
    /// clean weights (max 4 per vertex), and rest/walk/attack are re-authored on that rig. No runtime spring bones or leg
    /// colliders. ThemePack file names are kept (metas and GUIDs unchanged). One source blend carries rig + all three actions.
    /// 2026-10-10: the rig is Derek's own edit (265ec84, Blender 5.2, 16 bones), kept exactly. Skin weights were then re-solved
    /// on his joints (orphaned Hips/Spine/Shoulder/ToeBase weights moved to the bones he kept), the 507fb9a arm split seams
    /// were re-welded, and rest/walk/attack were rebuilt on his rig.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class VesperaNoCape
    {
        public const string DesignDir = "design/survival-theme-a-fantasy/enemies/anim/vespera/blender_rig_nocape_20261009";
        public const string RestFileName = "VESPERA_nocape.fbx";
        public const string WalkFileName = "VESPERA_nocape_walk.fbx";
        public const string AttackFileName = "VESPERA_nocape_attack.fbx";
        public const string BlendFileName = "VESPERA_nocape.blend";
        public const string RestMd5 = "e971308c85fd2e5548645ac41b815bf6";
        public const string WalkMd5 = "7183a0a06ba29bc7bf8af60127252f39";
        public const string AttackMd5 = "6af075b085a879ff135b87424c377279";
        public const string BlendMd5 = "470ea86ffcf4417468baca04f0610337";
        public const int RigBoneCount = 16;

        /// <summary>Bones Derek deleted in 265ec84. Their vertex groups are gone from the mesh; those weights now live on the kept bones.</summary>
        public static readonly string[] DeletedBones =
        {
            "mixamorig:Hips", "mixamorig:Spine", "mixamorig:LeftShoulder", "mixamorig:RightShoulder", "mixamorig:LeftToeBase", "mixamorig:RightToeBase"
        };

        /// <summary>Blend actions (frames 0-30 at 30 fps; rest is the bind pose).</summary>
        public static readonly string[] BlendActions = { "VESPERA_nocape_rest", "VESPERA_nocape_walk", "VESPERA_nocape_attack" };

        /// <summary>ThemePack MD5s of the cloth split this replaces (clothsplit_20261007, still in git history).</summary>
        public const string ClothSplitRestMd5 = "25e3a3c6b89923e8f58d579a0f8e6c74";
        public const string ClothSplitWalkMd5 = "d7f30d54e3a0b503c3dcb01828ff2c7b";
        public const string ClothSplitAttackMd5 = "c0abe43d57027edef1879a283efcd8f7";
    }
}
