using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Nightfang Theme A attack add-on (Design 2026-10-07). Lunge + Claw Rake — in-place lunge (root out ≤32 cm and back by f30) with a three-claw violet slash in front of the jaw f13–18.
    /// New 0–30 frame Scene take at 30 fps on the existing blenderig (same bones, no rebind; rest and walk unchanged).
    /// Frames below are baked from Design attack_meta drivers in Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class NightfangAttack
    {
        public const string FileName = "NIGHTFANG_blenderig_attack.fbx";
        public const string FileMd5 = "389229e0dd405ebf18a8c2aa38ec4577";
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
            new[] { "hips", "head", "thigh.L", "thigh.R" },
            new[] { 0.02447f, 0.49046f, -0.36983f, 0f, 0.76294f, 0.8593f, -0.26142f, 0.46866f, -0.46659f, 0.1509f, 0.46866f, -0.31065f },
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
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 0.25f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 0.4f, true),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 0.7f, true),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0f, 1.00622f, 1.29866f, 0f, 0f, 0f, 1f, 1f, false)
                    },
                    "claw-rake slash arcs in front of the jaw at bite/strike frame")
            });
    }
}
