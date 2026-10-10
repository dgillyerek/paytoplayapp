using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Vespera Theme A attack (Design 2026-10-07 intent, re-authored on the no-cape rig 2026-10-09: left hand on hip, right hand
    /// cocks back by the hip then drives forward at chest height; bolt leaves 12 cm ahead of the RightHand tail at f13).
    /// Original add-on: Shadow Bolt — empty-hand cast, violet/black shadow bolt from the right hand at f13.
    /// New 0–30 frame Scene take at 30 fps on the existing blenderig (same bones, no rebind; rest and walk unchanged).
    /// Frames below are baked from Design attack_meta drivers in Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class VesperaAttack
    {
        public const string FileName = "VESPERA_blenderig_attack.fbx";
        /// <summary>ThemePack attack FBX: Design blender_rig_nocape_20261009 VESPERA_nocape_attack.fbx (no cape, 22 bones, re-rigged).</summary>
        public const string FileMd5 = "3e74d719d4aa48097c94a61197f1b78f";
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
            new[] { "mixamorig:Hips", "mixamorig:Head", "mixamorig:LeftHand", "mixamorig:RightHand" },
            new[] { 0.015f, 0.93f, 0.175f, 0.055f, 1.6f, 0.185f, -0.152f, 1.01f, 0.352f, 0.338f, 1f, 0.095f },
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
                        new AttackFrame(0.10996f, 1.2148f, 0.86661f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.10996f, 1.2148f, 0.86661f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.10996f, 1.2148f, 0.86661f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.10996f, 1.2148f, 0.86661f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.10996f, 1.2148f, 0.86661f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.10996f, 1.2148f, 0.86661f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.10996f, 1.2148f, 0.86661f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.10996f, 1.2148f, 0.86661f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.10996f, 1.2148f, 0.86661f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.10996f, 1.2148f, 0.86661f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.10996f, 1.2148f, 0.86661f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.10996f, 1.2148f, 0.86661f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.10996f, 1.2148f, 0.86661f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.10996f, 1.2148f, 0.86661f, 0f, 0f, 0f, 1f, 0.35f, true),
                        new AttackFrame(0.10996f, 1.2148f, 1.16661f, 0f, 0f, 0f, 1f, 0.56667f, true),
                        new AttackFrame(0.10996f, 1.2148f, 1.46661f, 0f, 0f, 0f, 1f, 0.78333f, true),
                        new AttackFrame(0.10996f, 1.2148f, 1.76661f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.10996f, 1.2148f, 2.06661f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.10996f, 1.2148f, 2.36661f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.10996f, 1.2148f, 2.66661f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.10996f, 1.2148f, 2.96661f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.10996f, 1.2148f, 3.26661f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.10996f, 1.2148f, 3.56661f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.10996f, 1.2148f, 3.86661f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.10996f, 1.2148f, 4.16661f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.10996f, 1.2148f, 4.46661f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.10996f, 1.2148f, 4.76661f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.10996f, 1.2148f, 5.06661f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.10996f, 1.2148f, 5.36661f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.10996f, 1.2148f, 5.66661f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.10996f, 1.2148f, 5.96661f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "shadow bolt spawns 12cm ahead of RightHand tail, travels character-forward")
            });
    }
}
