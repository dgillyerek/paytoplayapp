using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Vespera Theme A attack (Design 2026-10-07 intent, re-authored on the no-cape rig 2026-10-09, rebuilt on Derek's 16-bone rig 2026-10-10).
    /// 2026-10-11 orb conjure (Derek picked option C of the weapon sheet). Rave rework (Derek on a470cea: "make her hands go in
    /// different directions as if she's outlining the orb. almost like a rave dance."): the hands sit on opposite sides of the orb
    /// and sweep in contrary directions around its outline - left over the top while right goes under, swap, and again (hits on
    /// f8, f11, f14, settle f17) - palms facing the orb at orb radius while it grows (visible f4, full size f17), with a small
    /// shoulder/hip groove on the hits; two-hand push f17-f21; the orb leaves as the Shadow Bolt at f21 from the orb centre,
    /// travels character-forward at 9 m/s; back to rest by f30.
    /// 2026-10-12 (Derek on d40bbca: "that's perfect.. just slow down the conjuring"): the conjure plays 1.5x slower - lift-in
    /// f0-5 unchanged, contrary sweeps f5-23 (hits ~f9-10, f14, ~f18-19, settle f23), push f23-27, orb released at f27,
    /// follow-through to f30, back to rest by f36 (push/return shapes identical, shifted +6). Take is f0-36.
    /// The orb is hidden at rest and walk. Prop mesh VESPERA_orb.fbx (dark-violet orb, silver claw, floating rune ring) rides
    /// both tracks; Unity adds the orb glow particles and trail around it.
    /// New 0–30 frame Scene take at 30 fps on the existing blenderig (same bones, no rebind).
    /// Frames below are baked from the Design anim report (work/slow_20261012/anim.json) in Unity convention (character forward = +Z).
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class VesperaAttack
    {
        public const string FileName = "VESPERA_blenderig_attack.fbx";
        /// <summary>ThemePack attack FBX: Design blender_rig_nocape_20261009 VESPERA_nocape_attack.fbx (Derek's 16-bone rig, orb conjure 2026-10-11, rave conjure 2026-10-11, slowed 2026-10-12).</summary>
        public const string FileMd5 = "3ba1e081c79de8cbf65fc5f0d4749edb";
        /// <summary>Original attack FBX in VesperaAttack.DesignDir (attack_20261007), unchanged.</summary>
        public const string DesignFileMd5 = "082b741eae52d9144a612566380766bc";
        public const string DesignDir = "design/survival-theme-a-fantasy/enemies/anim/vespera/attack_20261007";
        public const int ReleaseFrame = 27;

        /// <summary>Attack take f0-36 (1.2 s at 30 fps): the conjure was slowed 1.5x on 2026-10-12, so it is longer than the shared 30.</summary>
        public const int LastFrame = 36;
        public const float ClipSeconds = LastFrame / 30f;
        public const bool InPlace = false;

        /// <summary>First frame the conjured orb shows between her hands, and the frame it reaches full size.</summary>
        public const int OrbAppearFrame = 4;
        public const int OrbFullFrame = 23;

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
                        new AttackFrame(0.0799f, 0.9992f, 0.209f, 0f, 0f, 0f, 1f, 0.18f, false),
                        new AttackFrame(0.0771f, 1.0319f, 0.3085f, 0f, 0f, 0f, 1f, 0.18f, false),
                        new AttackFrame(0.0759f, 1.1303f, 0.4435f, 0f, 0f, 0f, 1f, 0.18f, false),
                        new AttackFrame(0.0754f, 1.2416f, 0.5488f, 0f, 0f, 0f, 1f, 0.18f, false),
                        new AttackFrame(0.078f, 1.3267f, 0.6061f, 0f, 0f, 0f, 1f, 0.18f, true),
                        new AttackFrame(0.08f, 1.36f, 0.595f, 0f, 0f, 0f, 1f, 0.194f, true),
                        new AttackFrame(0.08f, 1.36f, 0.595f, 0.05669f, 0f, 0f, 0.99839f, 0.217f, true),
                        new AttackFrame(0.08f, 1.36f, 0.595f, 0.28987f, 0f, 0f, 0.95707f, 0.25f, true),
                        new AttackFrame(0.0791f, 1.3574f, 0.5933f, 0.55339f, 0f, 0f, 0.83292f, 0.291f, true),
                        new AttackFrame(0.0749f, 1.3532f, 0.5887f, 0.66913f, 0f, 0f, 0.74314f, 0.339f, true),
                        new AttackFrame(0.0762f, 1.3543f, 0.5891f, 0.66393f, 0f, 0f, 0.7478f, 0.393f, true),
                        new AttackFrame(0.08f, 1.3567f, 0.595f, 0.43366f, 0f, 0f, 0.90108f, 0.451f, true),
                        new AttackFrame(0.08f, 1.3582f, 0.595f, -0.10019f, 0f, 0f, 0.99497f, 0.512f, true),
                        new AttackFrame(0.0848f, 1.3497f, 0.5937f, -0.52101f, 0f, 0f, 0.85355f, 0.574f, true),
                        new AttackFrame(0.0873f, 1.3449f, 0.5924f, -0.60876f, 0f, 0f, 0.79335f, 0.637f, true),
                        new AttackFrame(0.0848f, 1.3497f, 0.5937f, -0.52101f, 0f, 0f, 0.85355f, 0.699f, true),
                        new AttackFrame(0.08f, 1.3576f, 0.595f, -0.10019f, 0f, 0f, 0.99497f, 0.759f, true),
                        new AttackFrame(0.08f, 1.3573f, 0.595f, 0.43366f, 0f, 0f, 0.90108f, 0.815f, true),
                        new AttackFrame(0.0762f, 1.3541f, 0.5889f, 0.66393f, 0f, 0f, 0.7478f, 0.866f, true),
                        new AttackFrame(0.0764f, 1.3548f, 0.5883f, 0.66913f, 0f, 0f, 0.74314f, 0.911f, true),
                        new AttackFrame(0.0792f, 1.3573f, 0.5924f, 0.55339f, 0f, 0f, 0.83292f, 0.948f, true),
                        new AttackFrame(0.08f, 1.3582f, 0.595f, 0.28987f, 0f, 0f, 0.95707f, 0.976f, true),
                        new AttackFrame(0.08f, 1.3556f, 0.595f, 0.05669f, 0f, 0f, 0.99839f, 0.994f, true),
                        new AttackFrame(0.08f, 1.352f, 0.595f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.08f, 1.3698f, 0.6075f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.08f, 1.3993f, 0.635f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.08f, 1.4274f, 0.6625f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.4412f, 0.6762f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.4436f, 0.6786f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.4448f, 0.6798f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.0786f, 1.4145f, 0.6919f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.0767f, 1.3356f, 0.6456f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.0763f, 1.228f, 0.556f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.0779f, 1.1149f, 0.4321f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.0777f, 1.0267f, 0.2974f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.0799f, 0.9992f, 0.209f, 0f, 0f, 0f, 1f, 1f, false)
                    },
                    "orb conjured between the palms f4-f26 (midpoint of the two palms, grows to full size by f23, spins with the contrary sweeps)"),
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
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.08f, 1.44f, 0.675f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.08f, 1.44f, 0.975f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.08f, 1.44f, 1.275f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.08f, 1.44f, 1.575f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.08f, 1.44f, 1.875f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.08f, 1.44f, 2.175f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.08f, 1.44f, 2.475f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.08f, 1.44f, 2.775f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.08f, 1.44f, 3.075f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.08f, 1.44f, 3.375f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "the orb leaves as the shadow bolt from the orb centre at release, travels character-forward")
            },
            LastFrame);
    }
}
