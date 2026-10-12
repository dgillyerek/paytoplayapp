namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Oakenshield rigid-leaves pack (Design rigidleaves_20261010). Derek's cloth-rule fallback, same call as Rowan and Vespera
    /// no-cape: the hanging leaf skirt/tabard (OAKENSHIELD_cloth) and its 14 cloth bones are gone, the body is re-rigged on 22
    /// (now 20, see below) mixamorig bones refit to the mesh, every separate hard piece (leaf plates, bark, vines, shin and foot spikes, claw tips)
    /// is skinned 100% to one bone, and the rest of the skin is split by part (legs only on leg bones, forearm/hand weights off
    /// the torso, max 4 per vertex). Rest/walk/attack are re-authored on that rig. No runtime spring bones or leg colliders.
    /// ThemePack file names are kept (metas and GUIDs unchanged). One source blend carries rig + all three actions.
    /// 2026-10-11: rebuilt on Derek's own blend edit (329ae0c): his bones kept exactly (both ToeBases deleted, ankles and foot
    /// tips moved, Head tail shortened/tilted), toe weights moved onto Foot, hip/thigh weights smoothed (right hip crease), 46 long
    /// web faces under the skirt/between the thighs removed, walk at hip width (19 cm ankles), head forward in rest/walk/attack.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class OakenshieldRigidLeaves
    {
        public const string DesignDir = "design/survival-theme-a-fantasy/heroes/anim/oakenshield/rigidleaves_20261010";
        public const string RestFileName = "OAKENSHIELD_rigidleaves.fbx";
        public const string WalkFileName = "OAKENSHIELD_rigidleaves_walk.fbx";
        public const string AttackFileName = "OAKENSHIELD_rigidleaves_attack.fbx";
        public const string BlendFileName = "OAKENSHIELD_rigidleaves.blend";
        public const string RestMd5 = "f624d75b76a2d52c190e712adb9c1253";
        public const string WalkMd5 = "0d452682a9189372bde13f6e29e343f6";
        public const string AttackMd5 = "7dd6b5add1220440c6f8438dd55eb2f8";
        public const string BlendMd5 = "3f73aa0b3ec0302db2a11756a27910b3";
        public const int RigBoneCount = 20;

        /// <summary>Blend actions (frames 0-30 at 30 fps; rest is the bind pose with Neck/Head turned so the face looks forward).</summary>
        public static readonly string[] BlendActions = { "OAKENSHIELD_rest", "OAKENSHIELD_walk", "OAKENSHIELD_attack" };

        /// <summary>ThemePack MD5s of the cloth split this replaces (clothsplit_20261007, PR #48 4b7f231, still in git history).</summary>
        public const string ClothSplitRestMd5 = "6d754014dd09a23ad616378a70826b14";
        public const string ClothSplitWalkMd5 = "57fba709bf3c367dd9ac0b4bc552be26";
        public const string ClothSplitAttackMd5 = "769543a3e39321857dc50e734aadffd2";
    }
}
