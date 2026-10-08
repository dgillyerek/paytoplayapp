using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Ashwyrm Theme A attack add-on (Design 2026-10-07). Void-fire Breath + Fireball — purple breath flare at the snout f13–19, void-fire fireball released at f14.
    /// New 0–30 frame Scene take at 30 fps on the existing blenderig (same bones, no rebind; rest and walk unchanged).
    /// Frames below are baked from Design attack_meta drivers in Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles.
    /// Attack is Derek Game-view PASS (2026-10-07). Wing flap and walk still await Game-view.
    /// </summary>
    public static class AshwyrmAttack
    {
        public const string FileName = "ASHWYRM_blenderig_attack.fbx";
        public const string FileMd5 = "b0d240cc17cf79af0af3ed22be5f07ef";
        public const string DesignDir = "design/survival-theme-a-fantasy/enemies/anim/ashwyrm/attack_20261007";
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
            "Ashwyrm",
            FileName,
            FileMd5,
            DesignDir,
            ReleaseFrame,
            InPlace,
            new[] { "root", "spine", "thigh.L", "thigh.R" },
            new[] { 0f, 0f, 0.101f, -0.0127f, 0.93121f, 0.17731f, -0.12647f, 0.57411f, 0.12838f, 0.04842f, 0.56241f, 0.13716f },
            new[]
            {
                new AttackTrack(
                    "ASHWYRM_breathflare_vfx",
                    AttackTrackKind.Fixed,
                    "",
                    "",
                    0,
                    0,
                    0f,
                    AttackVfxShape.Flare,
                    0.33f,
                    0.45f,
                    new[] { 0.96398f, 0.88083f, 1f },
                    new[] { 0.72203f, 0.2717f, 1f },
                    new[] { 0.50639f, 0.09985f, 0.73536f },
                    new[] { 0.33064f, 0.33112f, 0.45423f },
                    new[]
                    {
                        new AttackFrame(0.01293f, 1.04748f, 0.36949f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.01293f, 1.0486f, 0.3651f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.01293f, 1.05149f, 0.35332f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.01293f, 1.05545f, 0.3362f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.01293f, 1.05979f, 0.31583f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.01293f, 1.06392f, 0.29432f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.01293f, 1.06745f, 0.2738f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.01293f, 1.07011f, 0.25643f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.01293f, 1.07179f, 0.24438f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.01293f, 1.07239f, 0.23983f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.01293f, 1.06668f, 0.27849f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.01293f, 1.04733f, 0.3701f, 0f, 0f, 0f, 1f, 0.28f, false),
                        new AttackFrame(0.01293f, 1.01388f, 0.47696f, 0f, 0f, 0f, 1f, 0.46f, false),
                        new AttackFrame(0.01293f, 0.97758f, 0.56321f, 0f, 0f, 0f, 1f, 0.64f, true),
                        new AttackFrame(0.01293f, 0.96024f, 0.59824f, 0f, 0f, 0f, 1f, 0.82f, true),
                        new AttackFrame(0.01293f, 0.96052f, 0.59769f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01293f, 0.96154f, 0.59572f, 0f, 0f, 0f, 1f, 0.82f, true),
                        new AttackFrame(0.01293f, 0.96352f, 0.59185f, 0f, 0f, 0f, 1f, 0.64f, true),
                        new AttackFrame(0.01293f, 0.96668f, 0.58559f, 0f, 0f, 0f, 1f, 0.46f, true),
                        new AttackFrame(0.01293f, 0.97122f, 0.57643f, 0f, 0f, 0f, 1f, 0.28f, true),
                        new AttackFrame(0.01293f, 0.97728f, 0.56386f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.01293f, 0.98494f, 0.5473f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.01293f, 0.9941f, 0.52651f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.01293f, 1.00404f, 0.50251f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.01293f, 1.01398f, 0.4767f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.01293f, 1.02329f, 0.45055f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.01293f, 1.03147f, 0.42561f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.01293f, 1.03819f, 0.40344f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.01293f, 1.04321f, 0.38563f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.01293f, 1.04637f, 0.37378f, 0f, 0f, 0f, 1f, 0.2f, false),
                        new AttackFrame(0.01293f, 1.04748f, 0.36949f, 0f, 0f, 0f, 1f, 0.2f, false)
                    },
                    "breath flare at snout vertex #35426"),
                new AttackTrack(
                    "ASHWYRM_voidfire_fireball_vfx",
                    AttackTrackKind.Projectile,
                    "",
                    "",
                    0,
                    14,
                    9f,
                    AttackVfxShape.Orb,
                    0.4f,
                    1.27f,
                    new[] { 0.96398f, 0.88083f, 1f },
                    new[] { 0.72203f, 0.2717f, 1f },
                    new[] { 0.80947f, 0.5371f, 1f },
                    new[] { 0.39951f, 0.37915f, 1.27069f },
                    new[]
                    {
                        new AttackFrame(0.01293f, 0.96024f, 0.69824f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.01293f, 0.96024f, 0.69824f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.01293f, 0.96024f, 0.69824f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.01293f, 0.96024f, 0.69824f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.01293f, 0.96024f, 0.69824f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.01293f, 0.96024f, 0.69824f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.01293f, 0.96024f, 0.69824f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.01293f, 0.96024f, 0.69824f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.01293f, 0.96024f, 0.69824f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.01293f, 0.96024f, 0.69824f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.01293f, 0.96024f, 0.69824f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.01293f, 0.96024f, 0.69824f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.01293f, 0.96024f, 0.69824f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.01293f, 0.96024f, 0.69824f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.01293f, 0.96024f, 0.69824f, 0f, 0f, 0f, 1f, 0.35f, true),
                        new AttackFrame(0.01293f, 0.96024f, 0.99824f, 0f, 0f, 0f, 1f, 0.56667f, true),
                        new AttackFrame(0.01293f, 0.96024f, 1.29824f, 0f, 0f, 0f, 1f, 0.78333f, true),
                        new AttackFrame(0.01293f, 0.96024f, 1.59824f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01293f, 0.96024f, 1.89824f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01293f, 0.96024f, 2.19824f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01293f, 0.96024f, 2.49824f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01293f, 0.96024f, 2.79824f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01293f, 0.96024f, 3.09824f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01293f, 0.96024f, 3.39824f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01293f, 0.96024f, 3.69824f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01293f, 0.96024f, 3.99824f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01293f, 0.96024f, 4.29824f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01293f, 0.96024f, 4.59824f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01293f, 0.96024f, 4.89824f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01293f, 0.96024f, 5.19824f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01293f, 0.96024f, 5.49824f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "void-fire fireball spawns 10cm ahead of snout vertex #35426 (rides spine bone), travels character-forward")
            });
    }
}
