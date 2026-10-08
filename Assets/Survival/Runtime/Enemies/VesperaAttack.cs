using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Vespera Theme A attack add-on (Design 2026-10-07). Shadow Bolt — empty-hand cast, violet/black shadow bolt from the right hand at f13.
    /// New 0–30 frame Scene take at 30 fps on the existing blenderig (same bones, no rebind; rest and walk unchanged).
    /// Frames below are baked from Design attack_meta drivers in Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class VesperaAttack
    {
        public const string FileName = "VESPERA_blenderig_attack.fbx";
        public const string FileMd5 = "082b741eae52d9144a612566380766bc";
        public const string DesignDir = "design/survival-theme-a-fantasy/enemies/anim/vespera/attack_20261007";
        public const int ReleaseFrame = 13;
        public const bool InPlace = false;

        /// <summary>Prop FBXs copied next to the character FBXs, with Design MD5s.</summary>
        public static readonly string[] PropFileNames =
        {

        };

        public static readonly string[] PropFileMd5s =
        {

        };

        public static readonly BlenderRigAttackSpec Spec = new BlenderRigAttackSpec(
            "Vespera",
            FileName,
            FileMd5,
            DesignDir,
            ReleaseFrame,
            InPlace,
            new[] { "mixamorig:Hips", "mixamorig:Head", "mixamorig:LeftHand", "mixamorig:RightHand" },
            new[] { -0.02682f, 0.93697f, 0.14547f, 0f, 1.83386f, 0.16847f, -0.19323f, 1.04326f, 0.31109f, 0.24517f, 1.07846f, 0.08753f },
            new[]
            {
                new AttackTrack(
                    "VESPERA_shadowbolt_vfx",
                    AttackTrackKind.Projectile,
                    "",
                    "",
                    0,
                    13,
                    9f,
                    AttackVfxShape.Orb,
                    0.338f,
                    1.3f,
                    new[] { 0.95469f, 0.88083f, 1f },
                    new[] { 0.70141f, 0.2478f, 1f },
                    new[] { 0.76738f, 0.42358f, 1f },
                    new[] { 0.33825f, 0.338f, 1.29702f },
                    new[]
                    {
                        new AttackFrame(0.16727f, 1.34921f, 0.86685f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16727f, 1.34921f, 0.86685f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16727f, 1.34921f, 0.86685f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16727f, 1.34921f, 0.86685f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16727f, 1.34921f, 0.86685f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16727f, 1.34921f, 0.86685f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16727f, 1.34921f, 0.86685f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16727f, 1.34921f, 0.86685f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16727f, 1.34921f, 0.86685f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16727f, 1.34921f, 0.86685f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16727f, 1.34921f, 0.86685f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16727f, 1.34921f, 0.86685f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16727f, 1.34921f, 0.86685f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16727f, 1.34921f, 0.86685f, 0f, 0f, 0f, 1f, 0.35f, true),
                        new AttackFrame(0.16727f, 1.34921f, 1.16685f, 0f, 0f, 0f, 1f, 0.56667f, true),
                        new AttackFrame(0.16727f, 1.34921f, 1.46685f, 0f, 0f, 0f, 1f, 0.78333f, true),
                        new AttackFrame(0.16727f, 1.34921f, 1.76685f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16727f, 1.34921f, 2.06685f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16727f, 1.34921f, 2.36685f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16727f, 1.34921f, 2.66685f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16727f, 1.34921f, 2.96685f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16727f, 1.34921f, 3.26685f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16727f, 1.34921f, 3.56685f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16727f, 1.34921f, 3.86685f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16727f, 1.34921f, 4.16685f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16727f, 1.34921f, 4.46685f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16727f, 1.34921f, 4.76685f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16727f, 1.34921f, 5.06685f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16727f, 1.34921f, 5.36685f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16727f, 1.34921f, 5.66685f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16727f, 1.34921f, 5.96685f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "shadow bolt spawns 12cm ahead of RightHand tail, travels character-forward")
            });
    }
}
