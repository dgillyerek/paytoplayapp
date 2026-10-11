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
        IronClaw = 3
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
                default:
                    throw new ArgumentException("No locked battle hit profile for " + name, nameof(name));
            }
        }

        /// <summary>
        /// True when a melee contact time was passed between two readings of the time since the
        /// first attack started (one hit per attack cycle, never during the rest gap).
        /// </summary>
        public static bool MeleeHitCrossed(LockedBattleFighter fighter, LockedBattleHitProfile profile, float previousSeconds, float seconds)
        {
            if (profile.Projectile || profile.MeleeContactFrame < 0 || seconds <= previousSeconds || seconds < 0f)
            {
                return false;
            }

            var cycle = fighter.AttackCycleSeconds;
            var contact = profile.MeleeContactSeconds;
            if (cycle <= 0f || contact > fighter.AttackSeconds)
            {
                return false;
            }

            var n = (float)Math.Floor((seconds - contact) / cycle);
            if (n < 0f)
            {
                return false;
            }

            var at = (n * cycle) + contact;
            return at > previousSeconds && at <= seconds;
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
