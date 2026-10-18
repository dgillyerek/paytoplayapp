using System;
using System.Collections.Generic;

namespace Survival.Domain.Roster
{
    /// <summary>
    /// Forward slide of the actor root (metres, along the fighter's facing) that closes the gap
    /// between a short-reach clip and the defender's body box. Zero before <see cref="StartFrame"/>,
    /// eases up to <see cref="Metres"/> by <see cref="FullFrame"/>, holds to <see cref="HoldFrame"/>,
    /// eases back to zero by <see cref="EndFrame"/>. Frames are attack clip frames at 30 fps.
    /// Presentation only: it moves the actor host, never a rig bone or a clip.
    /// </summary>
    public readonly struct LockedBattleReachShift
    {
        public LockedBattleReachShift(float metres, float startFrame, float fullFrame, float holdFrame, float endFrame)
        {
            Metres = metres;
            StartFrame = startFrame;
            FullFrame = fullFrame;
            HoldFrame = holdFrame;
            EndFrame = endFrame;
        }

        public float Metres { get; }
        public float StartFrame { get; }
        public float FullFrame { get; }
        public float HoldFrame { get; }
        public float EndFrame { get; }

        public bool IsNone => Metres == 0f;

        /// <summary>Slide in metres at an attack clip frame.</summary>
        public float At(float frame)
        {
            if (IsNone || frame <= StartFrame || frame >= EndFrame)
            {
                return 0f;
            }

            if (frame < FullFrame)
            {
                return Metres * Smooth((frame - StartFrame) / Math.Max(1e-3f, FullFrame - StartFrame));
            }

            if (frame <= HoldFrame)
            {
                return Metres;
            }

            return Metres * (1f - Smooth((frame - HoldFrame) / Math.Max(1e-3f, EndFrame - HoldFrame)));
        }

        private static float Smooth(float u)
        {
            u = u < 0f ? 0f : (u > 1f ? 1f : u);
            return u * u * (3f - (2f * u));
        }
    }

    /// <summary>
    /// One attack in a fighter's rotation. Attack 1 is always the locked, approved attack the
    /// fighter's own fields describe; attacks 2 and 3 are added per character as their clips land.
    /// </summary>
    public readonly struct LockedBattleAttack
    {
        /// <summary>Use the fighter's hit profile contact frame (attack 1).</summary>
        public const int ProfileContactFrame = -2;

        public LockedBattleAttack(
            string label,
            string poseName,
            float seconds,
            int meleeContactFrame = ProfileContactFrame,
            string strikeBone = "",
            float shakeMetres = -1f,
            bool inPlace = false,
            LockedBattleReachShift reachShift = default,
            BattleImpactKind? impact = null)
        {
            Label = label;
            PoseName = poseName;
            Seconds = seconds;
            MeleeContactFrame = meleeContactFrame;
            StrikeBone = strikeBone;
            ShakeMetres = shakeMetres;
            InPlace = inPlace;
            ReachShift = reachShift;
            Impact = impact;
        }

        /// <summary>Impact burst for this attack's hits. Null for the profile's.</summary>
        public BattleImpactKind? Impact { get; }

        public string Label { get; }

        /// <summary>Pose name the fighter's own actor plays for this attack.</summary>
        public string PoseName { get; }

        /// <summary>Clip length in seconds (rest gap not included).</summary>
        public float Seconds { get; }

        /// <summary>Melee contact frame (30 fps), <see cref="ProfileContactFrame"/> for the profile's, -1 for none.</summary>
        public int MeleeContactFrame { get; }

        /// <summary>Bone that lands this attack's melee hit. Empty for the profile's strike bone.</summary>
        public string StrikeBone { get; }

        /// <summary>Camera shake for this attack's hits. Negative for the profile's.</summary>
        public float ShakeMetres { get; }

        /// <summary>
        /// True when the clip carries the body (a leap) and the battle should hold the stride bone
        /// on its spot across the ground (forward and sideways), through the clip and its rest gap
        /// (so a crossfade back to rest does not pop), and the fighter lands where it took off.
        /// </summary>
        public bool InPlace { get; }

        public LockedBattleReachShift ReachShift { get; }
    }

    /// <summary>
    /// Attack rotation: each fighter plays attack 1, rest gap, attack 2, rest gap, attack 3,
    /// rest gap, and repeats. A fighter with one attack (the creatures, on hold) just loops it,
    /// which is exactly the single-attack cycle the battle had before.
    /// </summary>
    public static class LockedBattleRotation
    {
        public const int MaxAttacks = 3;

        public static int Count(LockedBattleFighter fighter)
        {
            var list = fighter.Attacks;
            return list == null || list.Count == 0 ? 1 : list.Count;
        }

        /// <summary>Attack at <paramref name="index"/> (wrapped). Attack 1 when the list is empty.</summary>
        public static LockedBattleAttack AttackAt(LockedBattleFighter fighter, int index)
        {
            var list = fighter.Attacks;
            if (list == null || list.Count == 0)
            {
                return new LockedBattleAttack(fighter.AttackLabel, fighter.AttackPoseName, fighter.AttackSeconds);
            }

            var n = list.Count;
            var i = ((index % n) + n) % n;
            return list[i];
        }

        /// <summary>Attack clip plus the rest gap after it.</summary>
        public static float CycleSeconds(LockedBattleFighter fighter, int index)
        {
            return AttackAt(fighter, index).Seconds + BlenderRigAttackSpec.RestGapSeconds;
        }

        /// <summary>One full pass through every attack, rest gaps included.</summary>
        public static float RotationSeconds(LockedBattleFighter fighter)
        {
            var total = 0f;
            var count = Count(fighter);
            for (var i = 0; i < count; i++)
            {
                total += CycleSeconds(fighter, i);
            }

            return total;
        }

        /// <summary>Seconds from the start of the rotation to the start of attack <paramref name="index"/>.</summary>
        public static float OffsetSeconds(LockedBattleFighter fighter, int index)
        {
            var total = 0f;
            for (var i = 0; i < index; i++)
            {
                total += CycleSeconds(fighter, i);
            }

            return total;
        }

        /// <summary>
        /// Which attack is up at <paramref name="seconds"/> since the first attack started, and the
        /// time into that attack's cycle. False before the first attack.
        /// </summary>
        public static bool Locate(LockedBattleFighter fighter, float seconds, out int index, out float secondsIntoAttack)
        {
            index = 0;
            secondsIntoAttack = 0f;
            if (seconds < 0f)
            {
                return false;
            }

            var rotation = RotationSeconds(fighter);
            if (rotation <= 0f)
            {
                secondsIntoAttack = seconds;
                return true;
            }

            var t = seconds % rotation;
            var count = Count(fighter);
            for (var i = 0; i < count; i++)
            {
                var cycle = CycleSeconds(fighter, i);
                if (t < cycle || i == count - 1)
                {
                    index = i;
                    secondsIntoAttack = t;
                    return true;
                }

                t -= cycle;
            }

            return true;
        }

        /// <summary>True while an attack clip should play, false in the rest gap after it.</summary>
        public static bool InAttackClip(LockedBattleFighter fighter, float seconds)
        {
            if (!Locate(fighter, seconds, out var index, out var into))
            {
                return false;
            }

            var attack = AttackAt(fighter, index);
            if (attack.Seconds + BlenderRigAttackSpec.RestGapSeconds <= 0f)
            {
                return true;
            }

            return into < attack.Seconds;
        }

        /// <summary>Melee contact frame of attack <paramref name="index"/>, -1 when it has none.</summary>
        public static int ContactFrame(LockedBattleFighter fighter, LockedBattleHitProfile profile, int index)
        {
            if (profile.Projectile)
            {
                return -1;
            }

            var frame = AttackAt(fighter, index).MeleeContactFrame;
            return frame == LockedBattleAttack.ProfileContactFrame ? profile.MeleeContactFrame : frame;
        }

        public static string StrikeBone(LockedBattleFighter fighter, LockedBattleHitProfile profile, int index)
        {
            var bone = AttackAt(fighter, index).StrikeBone;
            return string.IsNullOrEmpty(bone) ? profile.StrikeBone : bone;
        }

        public static BattleImpactKind Impact(LockedBattleFighter fighter, LockedBattleHitProfile profile, int index)
        {
            return AttackAt(fighter, index).Impact ?? profile.Impact;
        }

        public static float ShakeMetres(LockedBattleFighter fighter, LockedBattleHitProfile profile, int index)
        {
            var shake = AttackAt(fighter, index).ShakeMetres;
            return shake < 0f ? profile.ShakeMetres : shake;
        }

        /// <summary>
        /// True when a melee contact was passed between two readings of the time since the first
        /// attack started: one hit per attack, on that attack's own contact frame, never in a rest gap.
        /// </summary>
        public static bool MeleeHitCrossed(
            LockedBattleFighter fighter,
            LockedBattleHitProfile profile,
            float previousSeconds,
            float seconds,
            out int attackIndex)
        {
            attackIndex = 0;
            if (profile.Projectile || seconds <= previousSeconds || seconds < 0f)
            {
                return false;
            }

            var rotation = RotationSeconds(fighter);
            if (rotation <= 0f)
            {
                return false;
            }

            var count = Count(fighter);
            for (var i = 0; i < count; i++)
            {
                var frame = ContactFrame(fighter, profile, i);
                if (frame < 0)
                {
                    continue;
                }

                var contact = frame / (float)BlenderRigAttackSpec.FrameRate;
                if (contact > AttackAt(fighter, i).Seconds)
                {
                    continue;
                }

                var at0 = OffsetSeconds(fighter, i) + contact;
                var n = (float)Math.Floor((seconds - at0) / rotation);
                if (n < 0f)
                {
                    continue;
                }

                var at = (n * rotation) + at0;
                if (at > previousSeconds && at <= seconds)
                {
                    attackIndex = i;
                    return true;
                }
            }

            return false;
        }
    }
}
