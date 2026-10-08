using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Stormcrest Theme A attack (Design 2026-10-07 concept, retargeted onto Derek's rig).
    /// Wing Buffet / Storm Bolt — jagged blue-white lightning from the beak (2.76 m, visible f14–19).
    /// Fresh 0–30 Scene take at 30 fps. Starts and ends at rest. Body bones only. No cloth.
    /// Frames are Unity convention (character forward = +Z). Prop meshes are look reference; Unity draws particles.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class StormcrestAttack
    {
        public const string FileName = "STORMCREST_blenderig_attack.fbx";
        public const string FileMd5 = "9bae7b662f48a1c1eec33fafc0fed6d2";
        public const string DesignDir = "design/survival-theme-a-fantasy/heroes/anim/stormcrest/attack_20261007";
        public const int ReleaseFrame = 14;
        public const bool InPlace = false;

        /// <summary>Prop FBXs copied next to the character FBXs, with Design MD5s.</summary>
        public static readonly string[] PropFileNames =
        {

        };

        public static readonly string[] PropFileMd5s =
        {

        };

        public static readonly BlenderRigAttackSpec Spec = new BlenderRigAttackSpec(
            "Stormcrest",
            FileName,
            FileMd5,
            DesignDir,
            ReleaseFrame,
            InPlace,
            new[] { "hips", "head", "toe.L", "toe.R" },
            new[]
            {
                -0.15729f, 0.56257f, 0.69701f,
                -0.18242f, 0.79596f, 0.69561f,
                0.00011f, 0.04356f, 0.70101f,
                -0.3467f, 0.07142f, 0.65785f
            },
            new[]
            {
                new AttackTrack(
                    "STORMCREST_stormbolt_vfx",
                    AttackTrackKind.Projectile,
                    "",
                    "",
                    0,
                    14,
                    4f,
                    AttackVfxShape.Lightning,
                    0.3f,
                    2.76f,
                    new[] { 0.93092f, 0.96858f, 1f },
                    new[] { 0.3133f, 0.58383f, 1f },
                    new[] { 1f, 0.86504f, 0.58383f },
                    new[] { 0.55285f, 0.53753f, 2.76231f },
                    new[]
                    {
                        new AttackFrame(-0.17318f, 0.64927f, 0.9018f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 0.9018f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 0.9018f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 0.9018f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 0.9018f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 0.9018f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 0.9018f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 0.9018f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 0.9018f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 0.9018f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 0.9018f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 0.9018f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 0.9018f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 0.9018f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 0.9018f, 0f, 0f, 0f, 1f, 0.35f, true),
                        new AttackFrame(-0.17318f, 0.64927f, 1.03513f, 0f, 0f, 0f, 1f, 0.56667f, true),
                        new AttackFrame(-0.17318f, 0.64927f, 1.16847f, 0f, 0f, 0f, 1f, 0.78333f, true),
                        new AttackFrame(-0.17318f, 0.64927f, 1.3018f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.17318f, 0.64927f, 1.43513f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.17318f, 0.64927f, 1.56847f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.17318f, 0.64927f, 1.7018f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 1.83513f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 1.96847f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 2.1018f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 2.23513f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 2.36847f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 2.5018f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 2.63513f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 2.76847f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 2.9018f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.17318f, 0.64927f, 3.03513f, 0f, 0f, 0f, 1f, 1f, false)
                    },
                    "storm bolt from the beak at release f14; lightning flies character-forward at 4 m/s, visible frames 14-19")
            });
    }
}
