using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Vespera Theme A attack (Design 2026-10-07 intent, re-authored on the no-cape rig 2026-10-09, rebuilt on Derek's 16-bone rig 2026-10-10).
    /// 2026-10-11 orb conjure (Derek picked option C of the weapon sheet): both hands come up in front of her chest, palms facing,
    /// and trace one circle together (f6-f16) while the orb forms and grows between them (visible f4, full size f16); two-hand
    /// push f16-f20; the orb leaves as the Shadow Bolt at f20 from the orb centre, travels character-forward at 9 m/s; back to rest by f30.
    /// The orb is hidden at rest and walk. Prop mesh VESPERA_orb.fbx (dark-violet orb, silver claw, floating rune ring) rides
    /// both tracks; Unity adds the orb glow particles and trail around it.
    /// New 0–30 frame Scene take at 30 fps on the existing blenderig (same bones, no rebind).
    /// Frames below are baked from the Design anim report (work/orb_20261011/anim.json) in Unity convention (character forward = +Z).
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class VesperaAttack
    {
        public const string FileName = "VESPERA_blenderig_attack.fbx";
        /// <summary>ThemePack attack FBX: Design blender_rig_nocape_20261009 VESPERA_nocape_attack.fbx (Derek's 16-bone rig, orb conjure 2026-10-11).</summary>
        public const string FileMd5 = "5b907c6f5ec944e9fb4233bd9d451e5c";
        /// <summary>Original attack FBX in VesperaAttack.DesignDir (attack_20261007), unchanged.</summary>
        public const string DesignFileMd5 = "082b741eae52d9144a612566380766bc";
        public const string DesignDir = "design/survival-theme-a-fantasy/enemies/anim/vespera/attack_20261007";
        public const int ReleaseFrame = 20;
        public const bool InPlace = false;

        /// <summary>First frame the conjured orb shows between her hands, and the frame it reaches full size.</summary>
        public const int OrbAppearFrame = 4;
        public const int OrbFullFrame = 16;

        /// <summary>Prop FBXs copied next to the character FBXs, with Design MD5s.</summary>
        public static readonly string[] PropFileNames =
        {
            "VESPERA_orb.fbx"
        };

        public static readonly string[] PropFileMd5s =
        {
            "45b71d1aa45d4083d922560f59b77cc4"
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
                    "VESPERA_orb",
                    AttackTrackKind.Fixed,
                    "VESPERA_orb.fbx",
                    "",
                    OrbFullFrame,
                    ReleaseFrame,
                    0f,
                    AttackVfxShape.Orb,
                    0.12f,
                    0f,
                    new[] { 0.95469f, 0.88083f, 1f },
                    new[] { 0.70141f, 0.2478f, 1f },
                    new[] { 0.76738f, 0.42358f, 1f },
                    new[] { 0.1f, 0.16023f, 0.15523f },
                    new[]
                    {
                        new AttackFrame(0.0797f, 1.0028f, 0.208f, 0f, 0f, 0f, 1f, 0.18f, false),
                        new AttackFrame(0.0787f, 1.0141f, 0.2836f, 0f, 0f, 0f, 1f, 0.18f, false),
                        new AttackFrame(0.0716f, 1.0692f, 0.3849f, 0f, 0f, 0f, 1f, 0.18f, false),
                        new AttackFrame(0.0682f, 1.1501f, 0.4706f, 0f, 0f, 0f, 1f, 0.18f, false),
                        new AttackFrame(0.0661f, 1.2263f, 0.5242f, 0f, 0f, 0f, 1f, 0.18f, true),
                        new AttackFrame(0.0651f, 1.2801f, 0.5457f, 0f, 0f, 0f, 1f, 0.196f, true),
                        new AttackFrame(0.065f, 1.3f, 0.525f, 0f, 0f, 0f, 1f, 0.241f, true),
                        new AttackFrame(0.0338f, 1.3073f, 0.5281f, 0.2292f, 0f, 0f, 0.97338f, 0.308f, true),
                        new AttackFrame(0.0051f, 1.3338f, 0.5395f, 0.4909f, 0f, 0f, 0.87121f, 0.393f, true),
                        new AttackFrame(-0.0046f, 1.3775f, 0.5582f, 0.74431f, 0f, 0f, 0.66783f, 0.488f, true),
                        new AttackFrame(0.0174f, 1.4213f, 0.577f, 0.93074f, 0f, 0f, 0.36569f, 0.59f, true),
                        new AttackFrame(0.065f, 1.44f, 0.585f, 1f, 0f, 0f, 0f, 0.692f, true),
                        new AttackFrame(0.1126f, 1.4213f, 0.577f, 0.93074f, 0f, 0f, -0.36569f, 0.787f, true),
                        new AttackFrame(0.1346f, 1.3775f, 0.5582f, 0.74431f, 0f, 0f, -0.66783f, 0.872f, true),
                        new AttackFrame(0.1249f, 1.3338f, 0.5395f, 0.4909f, 0f, 0f, -0.87121f, 0.939f, true),
                        new AttackFrame(0.0962f, 1.3073f, 0.5281f, 0.2292f, 0f, 0f, -0.97338f, 0.984f, true),
                        new AttackFrame(0.065f, 1.3f, 0.525f, 0f, 0f, 0f, -1f, 1f, true),
                        new AttackFrame(0.065f, 1.3156f, 0.5422f, 0f, 0f, 0f, -1f, 1f, true),
                        new AttackFrame(0.065f, 1.35f, 0.58f, 0f, 0f, 0f, -1f, 1f, true),
                        new AttackFrame(0.065f, 1.3844f, 0.6178f, 0f, 0f, 0f, -1f, 1f, true),
                        new AttackFrame(0.065f, 1.4f, 0.635f, 0f, 0f, 0f, -1f, 1f, false),
                        new AttackFrame(0.065f, 1.4016f, 0.6362f, 0f, 0f, 0f, -1f, 1f, false),
                        new AttackFrame(0.065f, 1.4044f, 0.6386f, 0f, 0f, 0f, -1f, 1f, false),
                        new AttackFrame(0.0646f, 1.4068f, 0.6371f, 0f, 0f, 0f, -1f, 1f, false),
                        new AttackFrame(0.0617f, 1.3873f, 0.6451f, 0f, 0f, 0f, -1f, 1f, false),
                        new AttackFrame(0.0627f, 1.326f, 0.6268f, 0f, 0f, 0f, -1f, 1f, false),
                        new AttackFrame(0.0658f, 1.2397f, 0.5727f, 0f, 0f, 0f, -1f, 1f, false),
                        new AttackFrame(0.0705f, 1.1463f, 0.4865f, 0f, 0f, 0f, -1f, 1f, false),
                        new AttackFrame(0.0753f, 1.0627f, 0.3804f, 0f, 0f, 0f, -1f, 1f, false),
                        new AttackFrame(0.0789f, 1.0086f, 0.2781f, 0f, 0f, 0f, -1f, 1f, false),
                        new AttackFrame(0.0796f, 1.0062f, 0.2071f, 0f, 0f, 0f, -1f, 1f, false)
                    },
                    "orb conjured between the palms f4-f19 (midpoint of the two palms, grows to full size by f16, spins with the circle)"),
                new AttackTrack(
                    "VESPERA_shadowbolt_vfx",
                    AttackTrackKind.Projectile,
                    "VESPERA_orb.fbx",
                    "",
                    0,
                    ReleaseFrame,
                    9f,
                    AttackVfxShape.Orb,
                    0.2f,
                    1.3f,
                    new[] { 0.95469f, 0.88083f, 1f },
                    new[] { 0.70141f, 0.2478f, 1f },
                    new[] { 0.76738f, 0.42358f, 1f },
                    new[] { 0.1f, 0.16023f, 0.15523f },
                    new[]
                    {
                        new AttackFrame(0.065f, 1.4f, 0.635f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.065f, 1.4f, 0.635f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.065f, 1.4f, 0.635f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.065f, 1.4f, 0.635f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.065f, 1.4f, 0.635f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.065f, 1.4f, 0.635f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.065f, 1.4f, 0.635f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.065f, 1.4f, 0.635f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.065f, 1.4f, 0.635f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.065f, 1.4f, 0.635f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.065f, 1.4f, 0.635f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.065f, 1.4f, 0.635f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.065f, 1.4f, 0.635f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.065f, 1.4f, 0.635f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.065f, 1.4f, 0.635f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.065f, 1.4f, 0.635f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.065f, 1.4f, 0.635f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.065f, 1.4f, 0.635f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.065f, 1.4f, 0.635f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.065f, 1.4f, 0.635f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.065f, 1.4f, 0.635f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.065f, 1.4f, 0.935f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.065f, 1.4f, 1.235f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.065f, 1.4f, 1.535f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.065f, 1.4f, 1.835f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.065f, 1.4f, 2.135f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.065f, 1.4f, 2.435f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.065f, 1.4f, 2.735f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.065f, 1.4f, 3.035f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.065f, 1.4f, 3.335f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.065f, 1.4f, 3.635f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "the orb leaves as the shadow bolt from the orb centre at release, travels character-forward")
            });
    }
}
