using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Emberfang Theme A attack add-on (Design 2026-10-07). Fire Breath + Fireball — orange breath flare at the mouth f13–19, fireball released at f14.
    /// New 0–30 frame Scene take at 30 fps on the existing blenderig (same bones, no rebind; rest and walk unchanged).
    /// Frames below are baked from Design attack_meta drivers in Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class EmberfangAttack
    {
        public const string FileName = "EMBERFANG_blenderig_attack.fbx";
        public const string FileMd5 = "e832baf41cd41e2c54a50de4af1dbb89";
        public const string DesignDir = "design/survival-theme-a-fantasy/heroes/anim/emberfang/attack_20261007";
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
            "Emberfang",
            FileName,
            FileMd5,
            DesignDir,
            ReleaseFrame,
            InPlace,
            new[] { "hips", "head", "upperarm.L", "upperarm.R" },
            new[] { 0f, 0.36895f, 0.1f, 0f, 0.64895f, 0.69f, -0.13f, 0.40895f, 0.62f, 0.13f, 0.40895f, 0.62f },
            new[]
            {
                new AttackTrack(
                    "EMBERFANG_breathflare_vfx",
                    AttackTrackKind.Fixed,
                    "",
                    "",
                    0,
                    0,
                    0f,
                    AttackVfxShape.Flare,
                    0.33f,
                    0.45f,
                    new[] { 1f, 0.97769f, 0.88083f },
                    new[] { 1f, 0.76738f, 0.3133f },
                    new[] { 1f, 0.54685f, 0.18975f },
                    new[] { 0.33064f, 0.33112f, 0.45423f },
                    new[]
                    {
                        new AttackFrame(0.00668f, 0.59694f, 0.91996f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.00668f, 0.6072f, 0.91534f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.00668f, 0.6342f, 0.9014f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.00668f, 0.67161f, 0.87722f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.00668f, 0.71258f, 0.84265f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.00668f, 0.75073f, 0.79982f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.00668f, 0.78141f, 0.75346f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.00668f, 0.80263f, 0.71019f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.00668f, 0.81506f, 0.67731f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.00668f, 0.82067f, 0.66185f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.00668f, 0.80283f, 0.72316f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.00668f, 0.71535f, 0.85666f, 0f, 0f, 0f, 1f, 0.28f, false),
                        new AttackFrame(0.00668f, 0.55409f, 0.95532f, 0f, 0f, 0f, 1f, 0.46f, false),
                        new AttackFrame(0.00668f, 0.40191f, 0.97238f, 0f, 0f, 0f, 1f, 0.64f, true),
                        new AttackFrame(0.00668f, 0.34072f, 0.9619f, 0f, 0f, 0f, 1f, 0.82f, true),
                        new AttackFrame(0.00668f, 0.34114f, 0.96203f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00668f, 0.34276f, 0.96244f, 0f, 0f, 0f, 1f, 0.82f, true),
                        new AttackFrame(0.00668f, 0.34623f, 0.9632f, 0f, 0f, 0f, 1f, 0.64f, true),
                        new AttackFrame(0.00668f, 0.35225f, 0.96433f, 0f, 0f, 0f, 1f, 0.46f, true),
                        new AttackFrame(0.00668f, 0.36152f, 0.96578f, 0f, 0f, 0f, 1f, 0.28f, true),
                        new AttackFrame(0.00668f, 0.37475f, 0.96733f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.00668f, 0.3927f, 0.9686f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.00668f, 0.41619f, 0.96894f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.00668f, 0.44424f, 0.96745f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.00668f, 0.47477f, 0.96346f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.00668f, 0.5056f, 0.95683f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.00668f, 0.53459f, 0.94808f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.00668f, 0.55978f, 0.93834f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.00668f, 0.5795f, 0.92922f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.00668f, 0.59234f, 0.92252f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.00668f, 0.59694f, 0.91996f, 0f, 0f, 0f, 1f, 0.2f, false)
                    },
                    "breath flare at mouth (head/jaw tips) around release"),
                new AttackTrack(
                    "EMBERFANG_fireball_vfx",
                    AttackTrackKind.Projectile,
                    "",
                    "",
                    0,
                    14,
                    9f,
                    AttackVfxShape.Orb,
                    0.442f,
                    1.27f,
                    new[] { 1f, 0.97769f, 0.88083f },
                    new[] { 1f, 0.76738f, 0.3133f },
                    new[] { 1f, 0.54685f, 0.18975f },
                    new[] { 0.442f, 0.442f, 1.27069f },
                    new[]
                    {
                        new AttackFrame(0.00668f, 0.34072f, 1.0419f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.00668f, 0.34072f, 1.0419f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.00668f, 0.34072f, 1.0419f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.00668f, 0.34072f, 1.0419f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.00668f, 0.34072f, 1.0419f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.00668f, 0.34072f, 1.0419f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.00668f, 0.34072f, 1.0419f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.00668f, 0.34072f, 1.0419f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.00668f, 0.34072f, 1.0419f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.00668f, 0.34072f, 1.0419f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.00668f, 0.34072f, 1.0419f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.00668f, 0.34072f, 1.0419f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.00668f, 0.34072f, 1.0419f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.00668f, 0.34072f, 1.0419f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.00668f, 0.34072f, 1.0419f, 0f, 0f, 0f, 1f, 0.35f, true),
                        new AttackFrame(0.00668f, 0.34072f, 1.3419f, 0f, 0f, 0f, 1f, 0.56667f, true),
                        new AttackFrame(0.00668f, 0.34072f, 1.6419f, 0f, 0f, 0f, 1f, 0.78333f, true),
                        new AttackFrame(0.00668f, 0.34072f, 1.9419f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00668f, 0.34072f, 2.2419f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00668f, 0.34072f, 2.5419f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00668f, 0.34072f, 2.8419f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00668f, 0.34072f, 3.1419f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00668f, 0.34072f, 3.4419f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00668f, 0.34072f, 3.7419f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00668f, 0.34072f, 4.0419f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00668f, 0.34072f, 4.3419f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00668f, 0.34072f, 4.6419f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00668f, 0.34072f, 4.9419f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00668f, 0.34072f, 5.2419f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00668f, 0.34072f, 5.5419f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00668f, 0.34072f, 5.8419f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "fireball spawns 8cm ahead of mouth, travels character-forward")
            });
    }
}
