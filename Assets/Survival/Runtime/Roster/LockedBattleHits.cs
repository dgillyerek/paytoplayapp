using System;

namespace Survival.Domain.Roster
{
    /// <summary>Impact burst each attacker leaves where its attack lands.</summary>
    public enum BattleImpactKind
    {
        /// <summary>Emberfang fireball: fire burst, embers, sparks, short scorch flash.</summary>
        FireBurst = 0,

        /// <summary>Stormcrest storm bolt: lightning crackle, bright flash, arcing sparks.</summary>
        StormCrackle = 1,

        /// <summary>Nightfang claw rake: violet claw streaks and dark wisps.</summary>
        ShadowRake = 2,

        /// <summary>Ironhowl claw: claw sparks, dust and debris.</summary>
        IronClaw = 3,

        /// <summary>Ashwyrm void-fire fireball: purple fire burst, a gust of dark smoke, embers.</summary>
        VoidFireBreath = 4,

        /// <summary>Vespera shadow orb: violet flash, purple motes and dark wisps.</summary>
        ArcaneConjure = 5,

        /// <summary>Lyra arcane bolt: white-gold flash, halo and rising light motes.</summary>
        StaffLight = 6,

        /// <summary>Oakenshield thorn spear: leaves, splinters and earth dust.</summary>
        LeafEarth = 7,

        /// <summary>Sir Aldric sword thrust: white steel flash, blue (#1b3060) slash streaks, steel sparks.</summary>
        SwordSteel = 8,

        /// <summary>Blightroot rot claw: purple rot flash, claw streaks, spore puff and falling bark splinters.</summary>
        BlightRot = 9,

        /// <summary>Rowan arrow: hero-blue flash, wood splinters, green leaf flecks, blue streak along the shot.</summary>
        RangerArrow = 10,

        /// <summary>Bonequill bone arrow: violet flash, bone shards, dark purple wisps.</summary>
        BoneShard = 11,

        /// <summary>Lyra staff sweep (attack 2): hero-blue flash, blue streaks along the sweep, light motes.</summary>
        HeroStaffSweep = 12,

        /// <summary>Lyra arcane nova (attack 3): hero-blue flash, a wide ring of blue motes, rising light, blue light.</summary>
        HeroArcaneNova = 13,

        /// <summary>Ironhowl overhead smash (attack 2): purple flash, a purple crack ring on the ground, dust and debris.</summary>
        GroundSlam = 14,

        /// <summary>Oakenshield root snare (attack 3): hero-blue flash, blue-glowing roots bursting up from the ground, leaves and earth dust.</summary>
        HeroRootSnare = 15,

        /// <summary>Vespera shadow lash (attack 2): violet flash, purple lash streaks along the strike, dark wisps.</summary>
        VillainShadowLash = 16,

        /// <summary>Vespera hex rain (attack 3): a purple hex glyph on the floor under each hit, violet motes falling into it, dark wisps.</summary>
        VillainHexRain = 17
    }

    /// <summary>
    /// Body box in the defender's own frame, Design metres: x across, y up from the ground,
    /// z forward. Fitted inside the rest mesh extents measured from the locked FBXs, without
    /// wings or T-pose arms, so hits land on the body.
    /// </summary>
    public readonly struct BattleHitBox
    {
        public BattleHitBox(float halfWidth, float front, float back, float height)
        {
            HalfWidth = halfWidth;
            Front = front;
            Back = back;
            Height = height;
        }

        public float HalfWidth { get; }
        public float Front { get; }
        public float Back { get; }
        public float Height { get; }

        public bool Contains(float x, float y, float z)
        {
            return x >= -HalfWidth && x <= HalfWidth && y >= 0f && y <= Height && z >= -Back && z <= Front;
        }

        /// <summary>
        /// First point of segment a→b inside the box, as a fraction 0..1 along it (slab test).
        /// A segment that starts inside hits at 0.
        /// </summary>
        public bool SegmentEntry(float ax, float ay, float az, float bx, float by, float bz, out float t)
        {
            var t0 = 0f;
            var t1 = 1f;
            t = 0f;
            if (!Slab(ax, bx - ax, -HalfWidth, HalfWidth, ref t0, ref t1)
                || !Slab(ay, by - ay, 0f, Height, ref t0, ref t1)
                || !Slab(az, bz - az, -Back, Front, ref t0, ref t1))
            {
                return false;
            }

            t = t0;
            return true;
        }

        public void ClosestPoint(float x, float y, float z, out float cx, out float cy, out float cz)
        {
            cx = Math.Max(-HalfWidth, Math.Min(HalfWidth, x));
            cy = Math.Max(0f, Math.Min(Height, y));
            cz = Math.Max(-Back, Math.Min(Front, z));
        }

        private static bool Slab(float start, float delta, float min, float max, ref float t0, ref float t1)
        {
            if (Math.Abs(delta) < 1e-7f)
            {
                return start >= min && start <= max;
            }

            var a = (min - start) / delta;
            var b = (max - start) / delta;
            if (a > b)
            {
                var swap = a;
                a = b;
                b = swap;
            }

            t0 = Math.Max(t0, a);
            t1 = Math.Min(t1, b);
            return t0 <= t1;
        }
    }

    /// <summary>
    /// How one locked fighter lands hits and takes them in the battle. Presentation only.
    /// Flying attacks stop on the target's <see cref="Box"/>. Melee attacks land on
    /// <see cref="MeleeContactFrame"/>, the clip frame where <see cref="StrikeBone"/> reaches
    /// furthest forward (measured on the locked attack FBX).
    /// </summary>
    public readonly struct LockedBattleHitProfile
    {
        public LockedBattleHitProfile(
            string name,
            BattleImpactKind impact,
            bool projectile,
            int meleeContactFrame,
            string strikeBone,
            BattleHitBox box,
            float recoilMetres,
            float recoilDegrees,
            float flashR,
            float flashG,
            float flashB,
            float shakeMetres)
        {
            Name = name;
            Impact = impact;
            Projectile = projectile;
            MeleeContactFrame = meleeContactFrame;
            StrikeBone = strikeBone;
            Box = box;
            RecoilMetres = recoilMetres;
            RecoilDegrees = recoilDegrees;
            FlashR = flashR;
            FlashG = flashG;
            FlashB = flashB;
            ShakeMetres = shakeMetres;
        }

        public string Name { get; }
        public BattleImpactKind Impact { get; }

        /// <summary>True when the attack flies (stopped by the target box), false for melee.</summary>
        public bool Projectile { get; }

        /// <summary>Attack clip frame (30 fps) of the melee hit. -1 for flying attacks.</summary>
        public int MeleeContactFrame { get; }

        /// <summary>Bone that lands the melee hit. Empty for flying attacks.</summary>
        public string StrikeBone { get; }

        /// <summary>This fighter's body box when it is the one being hit.</summary>
        public BattleHitBox Box { get; }

        /// <summary>Knock-back away from the attacker when hit. Smaller for bigger bodies.</summary>
        public float RecoilMetres { get; }

        /// <summary>Lean away from the attacker when hit.</summary>
        public float RecoilDegrees { get; }

        public float FlashR { get; }
        public float FlashG { get; }
        public float FlashB { get; }

        /// <summary>Camera shake this fighter's hits cause. Zero for no shake.</summary>
        public float ShakeMetres { get; }

        public float MeleeContactSeconds => MeleeContactFrame / (float)BlenderRigAttackSpec.FrameRate;
    }

    public static class LockedBattleHits
    {
        public const float RecoilPeakSeconds = 0.05f;
        public const float RecoilSeconds = 0.25f;
        public const float FlashSeconds = 0.10f;

        /// <summary>Nightfang's claw rake: left fore paw tip reaches furthest forward (1.13 m) on frame 14, the Design release.</summary>
        public const int NightfangContactFrame = 14;

        /// <summary>Ironhowl's swipe: right hand reaches furthest forward (1.16 m) on frame 11 of 26.</summary>
        public const int IronhowlContactFrame = 11;

        /// <summary>
        /// Sir Aldric's "Sword And Shield attack (4)": the right hand reaches furthest forward on
        /// frame 16 of the 0-32 thrust, with the 1.01 m blade tip 0.94 m ahead of his origin.
        /// </summary>
        public const int SirAldricContactFrame = 16;

        /// <summary>
        /// Blightroot's "mutant swiping": the left claw reaches furthest forward (1.36 m, hips
        /// lunged 0.55 m) at 52.6 % of the take, frame 42 of 80.
        /// </summary>
        public const int BlightrootContactFrame = 42;

        /// <summary>Sir Aldric's blue, #1b3060 (27, 48, 96).</summary>
        public static readonly float[] SirAldricBlueRgb = { 27f / 255f, 48f / 255f, 96f / 255f };

        public static readonly LockedBattleHitProfile Emberfang = new LockedBattleHitProfile(
            "Emberfang",
            BattleImpactKind.FireBurst,
            true,
            -1,
            string.Empty,
            new BattleHitBox(0.40f, 0.85f, 0.60f, 0.95f),
            0.07f,
            6f,
            1f,
            0.55f,
            0.45f,
            0.025f);

        public static readonly LockedBattleHitProfile Stormcrest = new LockedBattleHitProfile(
            "Stormcrest",
            BattleImpactKind.StormCrackle,
            true,
            -1,
            string.Empty,
            new BattleHitBox(0.40f, 0.85f, 0.70f, 1.10f),
            0.06f,
            5f,
            0.85f,
            0.92f,
            1f,
            0.045f);

        public static readonly LockedBattleHitProfile Nightfang = new LockedBattleHitProfile(
            "Nightfang",
            BattleImpactKind.ShadowRake,
            false,
            NightfangContactFrame,
            "toe.L",
            new BattleHitBox(0.35f, 0.90f, 0.70f, 1.00f),
            0.06f,
            5f,
            1f,
            0.40f,
            0.40f,
            0f);

        public static readonly LockedBattleHitProfile Ironhowl = new LockedBattleHitProfile(
            "Ironhowl",
            BattleImpactKind.IronClaw,
            false,
            IronhowlContactFrame,
            "mixamorig:RightHand",
            new BattleHitBox(0.35f, 0.28f, 0.28f, 1.85f),
            0.03f,
            2.5f,
            1f,
            0.35f,
            0.30f,
            0.055f);

        /// <summary>
        /// Body box covers the chest and snout facing the heroes. Height reaches 1.70 m so a
        /// human-height spear (Oakenshield releases at ~1.57 m) stops on the upper body instead of
        /// flying over the 1.34 m rest mesh; from the rear battle camera that reads as a body hit.
        /// </summary>
        public static readonly LockedBattleHitProfile Ashwyrm = new LockedBattleHitProfile(
            "Ashwyrm",
            BattleImpactKind.VoidFireBreath,
            true,
            -1,
            string.Empty,
            new BattleHitBox(0.50f, 0.55f, 0.45f, 1.70f),
            0.05f,
            4f,
            1f,
            0.45f,
            0.45f,
            0.035f);

        public static readonly LockedBattleHitProfile Vespera = new LockedBattleHitProfile(
            "Vespera",
            BattleImpactKind.ArcaneConjure,
            true,
            -1,
            string.Empty,
            new BattleHitBox(0.25f, 0.20f, 0.20f, 1.85f),
            0.04f,
            3.5f,
            1f,
            0.40f,
            0.40f,
            0.025f);

        public static readonly LockedBattleHitProfile Lyra = new LockedBattleHitProfile(
            "Lyra",
            BattleImpactKind.StaffLight,
            true,
            -1,
            string.Empty,
            new BattleHitBox(0.25f, 0.20f, 0.20f, 1.85f),
            0.04f,
            3.5f,
            1f,
            0.45f,
            0.45f,
            0.02f);

        public static readonly LockedBattleHitProfile Oakenshield = new LockedBattleHitProfile(
            "Oakenshield",
            BattleImpactKind.LeafEarth,
            true,
            -1,
            string.Empty,
            new BattleHitBox(0.30f, 0.25f, 0.15f, 1.75f),
            0.04f,
            3f,
            1f,
            0.45f,
            0.40f,
            0.03f);

        /// <summary>
        /// Lane 5 hero. Body box fits the 1.80 m armoured body with the shield side in front
        /// (tight plate, no cape: the soft cape is never attached, so no loose cloth to clip).
        /// </summary>
        public static readonly LockedBattleHitProfile SirAldric = new LockedBattleHitProfile(
            "Sir Aldric",
            BattleImpactKind.SwordSteel,
            false,
            SirAldricContactFrame,
            "mixamorig:RightHand",
            new BattleHitBox(0.28f, 0.25f, 0.20f, 1.75f),
            0.04f,
            3f,
            1f,
            0.45f,
            0.40f,
            0.035f);

        /// <summary>Lane 5 villain. Body box fits the 1.90 m trunk and antlers with arms down at idle.</summary>
        public static readonly LockedBattleHitProfile Blightroot = new LockedBattleHitProfile(
            "Blightroot",
            BattleImpactKind.BlightRot,
            false,
            BlightrootContactFrame,
            "mixamorig:LeftHand",
            new BattleHitBox(0.45f, 0.38f, 0.35f, 1.85f),
            0.03f,
            2.5f,
            1f,
            0.45f,
            0.45f,
            0.05f);

        /// <summary>Lane 6 hero archer. Body box fits the 1.80 m rest mesh without the A-pose arms or coat tails.</summary>
        public static readonly LockedBattleHitProfile Rowan = new LockedBattleHitProfile(
            "Rowan",
            BattleImpactKind.RangerArrow,
            true,
            -1,
            string.Empty,
            new BattleHitBox(0.25f, 0.20f, 0.20f, 1.75f),
            0.04f,
            3.5f,
            1f,
            0.45f,
            0.45f,
            0.02f);

        /// <summary>Lane 6 villain archer. Body box fits the 1.80 m rest mesh; the cape adds a little at the back.</summary>
        public static readonly LockedBattleHitProfile Bonequill = new LockedBattleHitProfile(
            "Bonequill",
            BattleImpactKind.BoneShard,
            true,
            -1,
            string.Empty,
            new BattleHitBox(0.25f, 0.20f, 0.25f, 1.75f),
            0.04f,
            3.5f,
            1f,
            0.40f,
            0.45f,
            0.02f);

        public static LockedBattleHitProfile ProfileOf(string name)
        {
            switch (name)
            {
                case "Emberfang":
                    return Emberfang;
                case "Stormcrest":
                    return Stormcrest;
                case "Nightfang":
                    return Nightfang;
                case "Ironhowl":
                    return Ironhowl;
                case "Ashwyrm":
                    return Ashwyrm;
                case "Vespera":
                    return Vespera;
                case "Lyra":
                    return Lyra;
                case "Oakenshield":
                    return Oakenshield;
                case "Sir Aldric":
                    return SirAldric;
                case "Blightroot":
                    return Blightroot;
                case "Rowan":
                    return Rowan;
                case "Bonequill":
                    return Bonequill;
                default:
                    throw new ArgumentException("No locked battle hit profile for " + name, nameof(name));
            }
        }

        /// <summary>
        /// True when the flying segment a→b enters the body box. Used by the battle and by tests to
        /// prove a projectile stops on contact (the Unity driver then parks the mesh at the entry
        /// and despawns it, so it never travels past the far face).
        /// </summary>
        public static bool ProjectileSegmentHits(BattleHitBox box, float ax, float ay, float az, float bx, float by, float bz, out float entryT)
        {
            return box.SegmentEntry(ax, ay, az, bx, by, bz, out entryT);
        }

        /// <summary>
        /// True when a→b hits the box and the entry is before the end of the segment (the bolt has
        /// not already cleared the far side).
        /// </summary>
        public static bool ProjectileStopsOnContact(BattleHitBox box, float ax, float ay, float az, float bx, float by, float bz)
        {
            return ProjectileSegmentHits(box, ax, ay, az, bx, by, bz, out var t) && t < 1f - 1e-4f;
        }

        /// <summary>
        /// True when a melee contact time was passed between two readings of the time since the
        /// first attack started (one hit per attack cycle, never during the rest gap).
        /// </summary>
        public static bool MeleeHitCrossed(LockedBattleFighter fighter, LockedBattleHitProfile profile, float previousSeconds, float seconds)
        {
            // Rotation-aware (each attack on its own contact frame); with one attack this is the
            // old one-hit-per-cycle rule.
            return LockedBattleRotation.MeleeHitCrossed(fighter, profile, previousSeconds, seconds, out _);
        }

        /// <summary>Knock-back weight 0..1: quick push out, then ease back to 0 by <see cref="RecoilSeconds"/>.</summary>
        public static float RecoilWeight(float secondsSinceHit)
        {
            if (secondsSinceHit < 0f || secondsSinceHit >= RecoilSeconds)
            {
                return 0f;
            }

            if (secondsSinceHit < RecoilPeakSeconds)
            {
                var u = secondsSinceHit / RecoilPeakSeconds;
                return u * (2f - u);
            }

            var v = (secondsSinceHit - RecoilPeakSeconds) / (RecoilSeconds - RecoilPeakSeconds);
            return 1f - (v * v * (3f - (2f * v)));
        }

        /// <summary>Hit flash weight 1→0 over <see cref="FlashSeconds"/>.</summary>
        public static float FlashWeight(float secondsSinceHit)
        {
            if (secondsSinceHit < 0f || secondsSinceHit >= FlashSeconds)
            {
                return 0f;
            }

            return 1f - (secondsSinceHit / FlashSeconds);
        }

        /// <summary>
        /// Skip the knock-back while the defender is inside its own attack clip, so the recoil
        /// does not fight the strike. The flash still plays.
        /// </summary>
        public static bool RecoilAllowed(LockedBattleFighter defender, bool defenderAttacking, float defenderAttackSeconds)
        {
            return !defenderAttacking || !LockedBattleRoster.InAttackClip(defender, defenderAttackSeconds);
        }
    }
}
