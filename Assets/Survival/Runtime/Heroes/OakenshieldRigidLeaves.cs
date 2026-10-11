namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Oakenshield rigid-leaves pack (Design rigidleaves_20261010). Derek's cloth-rule fallback, same call as Rowan and Vespera
    /// no-cape: the hanging leaf skirt/tabard (OAKENSHIELD_cloth) and its 14 cloth bones are gone, the body is re-rigged on 22
    /// mixamorig bones refit to the mesh, every separate hard piece (leaf plates, bark, vines, shin and foot spikes, claw tips)
    /// is skinned 100% to one bone, and the rest of the skin is split by part (legs only on leg bones, forearm/hand weights off
    /// the torso, max 4 per vertex). Rest/walk/attack are re-authored on that rig. No runtime spring bones or leg colliders.
    /// ThemePack file names are kept (metas and GUIDs unchanged). One source blend carries rig + all three actions.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class OakenshieldRigidLeaves
    {
        public const string DesignDir = "design/survival-theme-a-fantasy/heroes/anim/oakenshield/rigidleaves_20261010";
        public const string RestFileName = "OAKENSHIELD_rigidleaves.fbx";
        public const string WalkFileName = "OAKENSHIELD_rigidleaves_walk.fbx";
        public const string AttackFileName = "OAKENSHIELD_rigidleaves_attack.fbx";
        public const string BlendFileName = "OAKENSHIELD_rigidleaves.blend";
        public const string RestMd5 = "e768f9f440ed307aeb854a07b351a484";
        public const string WalkMd5 = "88da839e0de15044ab14a1bf36463ab2";
        public const string AttackMd5 = "494efd02f36e84ec97253f3f00572fee";
        public const string BlendMd5 = "134fa619c6c5c313756a46f33549ce31";
        public const int RigBoneCount = 22;

        /// <summary>Blend actions (frames 0-30 at 30 fps; rest is the bind pose).</summary>
        public static readonly string[] BlendActions = { "OAKENSHIELD_rest", "OAKENSHIELD_walk", "OAKENSHIELD_attack" };

        /// <summary>ThemePack MD5s of the cloth split this replaces (clothsplit_20261007, PR #48 4b7f231, still in git history).</summary>
        public const string ClothSplitRestMd5 = "6d754014dd09a23ad616378a70826b14";
        public const string ClothSplitWalkMd5 = "57fba709bf3c367dd9ac0b4bc552be26";
        public const string ClothSplitAttackMd5 = "769543a3e39321857dc50e734aadffd2";
    }
}
