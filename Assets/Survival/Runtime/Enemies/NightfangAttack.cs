using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Nightfang Theme A attack add-on, rebaked 2026-10-08 on Derek's hand-edited rig.
    /// Lunge bite + claw rake — in-place lunge (root forward ≤32 cm and back by f30) with a three-claw violet slash in front of the jaw, visible f13–18.
    /// 0–30 frame Scene take at 30 fps. Forelegs reach and rake. Starts and ends at rest. No cloth.
    /// Frames below are Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class NightfangAttack
    {
        public const string FileName = "NIGHTFANG_blenderig_attack.fbx";
        public const string FileMd5 = "3e1d6cfa12e353f207a4e610804f9a3b";
        public const string DesignDir = "design/survival-theme-a-fantasy/enemies/anim/nightfang/attack_20261007";
        public const int ReleaseFrame = 14;
        public const bool InPlace = true;

        /// <summary>Prop FBXs copied next to the character FBXs, with Design MD5s.</summary>
        public static readonly string[] PropFileNames =
        {

        };

        public static readonly string[] PropFileMd5s =
        {

        };

        public static readonly BlenderRigAttackSpec Spec = new BlenderRigAttackSpec(
            "Nightfang",
            FileName,
            FileMd5,
            DesignDir,
            ReleaseFrame,
            InPlace,
            new[] { "hips", "head", "toe.L", "toe.R" },
            new[] { 0.12543f, 0.8062f, 0.0984f, 0.25356f, 0.80282f, 0.54895f, -0.05184f, 0.06694f, 0.49177f, 0.60218f, 0.05455f, 0.22724f },
            new[]
            {
                new AttackTrack(
                    "NIGHTFANG_clawslash_vfx",
                    AttackTrackKind.Fixed,
                    "",
                    "",
                    0,
                    0,
                    0f,
                    AttackVfxShape.Slash,
                    0.8f,
                    0.4f,
                    new[] { 0.93092f, 0.82666f, 1f },
                    new[] { 0.67998f, 0.22092f, 1f },
                    new[] { 0.67998f, 0.22092f, 1f },
                    new[] { 0.79512f, 0.40902f, 0.17231f },
                    new[]
                    {
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 0.4f, true),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 0.7f, true),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 0.38074f, 1.07031f, 0f, 0f, 0f, 1f, 1f, false)
                    },
                    "claw-rake slash arcs in front of the jaw at bite/strike frame")
            });
    }
}
