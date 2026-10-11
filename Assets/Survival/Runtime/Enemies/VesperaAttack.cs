using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Vespera Theme A attack (Design 2026-10-07 intent, re-authored on the no-cape rig 2026-10-09, rebuilt on Derek's 16-bone rig 2026-10-10; hand path re-routed 2026-10-10:
    /// left hand on hip, right hand winds back OUT to her right side (f9), sweeps forward in an arc outside her hip, waist and chest
    /// and releases in front of her right shoulder; bolt leaves 12 cm ahead of the RightHand tail at f13, back to rest by f30).
    /// Original add-on: Shadow Bolt — empty-hand cast, violet/black shadow bolt from the right hand at f13.
    /// New 0–30 frame Scene take at 30 fps on the existing blenderig (same bones, no rebind; rest and walk unchanged).
    /// Frames below are baked from Design attack_meta drivers in Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class VesperaAttack
    {
        public const string FileName = "VESPERA_blenderig_attack.fbx";
        /// <summary>ThemePack attack FBX: Design blender_rig_nocape_20261009 VESPERA_nocape_attack.fbx (no cape, Derek's 16-bone rig, right hand freed + around-the-body path 2026-10-10).</summary>
        public const string FileMd5 = "7435a2059aed197590f7e33d93590453";
        /// <summary>Original attack FBX in VesperaAttack.DesignDir (attack_20261007), unchanged.</summary>
        public const string DesignFileMd5 = "082b741eae52d9144a612566380766bc";
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
            new[] { "mixamorig:Spine1", "mixamorig:Head", "mixamorig:LeftHand", "mixamorig:RightHand" },
            new[] { 0.0667f, 1.11933f, 0.2049f, 0.05851f, 1.60558f, 0.12055f, -0.17125f, 1.05986f, 0.33079f, 0.32532f, 1.07361f, 0.06788f },
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
                        new AttackFrame(0.21807f, 1.40889f, 0.98023f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21807f, 1.40889f, 0.98023f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21807f, 1.40889f, 0.98023f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21807f, 1.40889f, 0.98023f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21807f, 1.40889f, 0.98023f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21807f, 1.40889f, 0.98023f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21807f, 1.40889f, 0.98023f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21807f, 1.40889f, 0.98023f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21807f, 1.40889f, 0.98023f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21807f, 1.40889f, 0.98023f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21807f, 1.40889f, 0.98023f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21807f, 1.40889f, 0.98023f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21807f, 1.40889f, 0.98023f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.21807f, 1.40889f, 0.98023f, 0f, 0f, 0f, 1f, 0.35f, true),
                        new AttackFrame(0.21807f, 1.40889f, 1.28023f, 0f, 0f, 0f, 1f, 0.56667f, true),
                        new AttackFrame(0.21807f, 1.40889f, 1.58023f, 0f, 0f, 0f, 1f, 0.78333f, true),
                        new AttackFrame(0.21807f, 1.40889f, 1.88023f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21807f, 1.40889f, 2.18023f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21807f, 1.40889f, 2.48023f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21807f, 1.40889f, 2.78023f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21807f, 1.40889f, 3.08023f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21807f, 1.40889f, 3.38023f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21807f, 1.40889f, 3.68023f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21807f, 1.40889f, 3.98023f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21807f, 1.40889f, 4.28023f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21807f, 1.40889f, 4.58023f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21807f, 1.40889f, 4.88023f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21807f, 1.40889f, 5.18023f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21807f, 1.40889f, 5.48023f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21807f, 1.40889f, 5.78023f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.21807f, 1.40889f, 6.08023f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "shadow bolt spawns 12cm ahead of RightHand tail, travels character-forward")
            });
    }
}
