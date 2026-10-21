using Survival.Domain.Enemies;
using Survival.Domain.Heroes;

namespace Survival.Domain.Roster
{
    /// <summary>
    /// Attacks 2 and 3 for the four Blender-rigged creatures (2026-10-19). Mixamo only auto-rigs humanoids, so there is no
    /// premade Mixamo take for a dragon, a griffin or a wolf; each extra attack replays a locked clip the creature already
    /// ships (wing flap, walk, trot, or its own attack) with its own hit frames, strike bones and impact. No clip, rig or
    /// import is touched and nothing is hand-keyed.
    /// <para>
    /// Every frame below is a clip frame at 30 fps, measured in Blender 5.2 on the locked FBX (bone tail world positions,
    /// Blender frame N = clip frame N - 1). Clips loop in the actor, so a 2 s walk attack is two walk loops.
    /// </para>
    /// <para>
    /// The dragons' extra attacks never replay their "attack" pose: that pose fires the locked fireball driver, which only
    /// belongs to attack 1.
    /// </para>
    /// </summary>
    public static class CreatureBattleAttacks
    {
        /// <summary>Wing gusts and stomp cracks travel this many frames from the wing tip / foot to the target.</summary>
        public const int GustFlightFrames = 5;

        public const int QuakeFlightFrames = 5;

        public const int ClawWaveFlightFrames = 5;

        /// <summary>Two walk (or trot) loops.</summary>
        public const float TwoLoopSeconds = 2f;

        // Emberfang. Wing flap: both outer wing fingers (wing_f1_b) are highest on frame 0, sweep down fastest around
        // frames 8-10 and bottom out on frame 15. The right gust leaves one frame after the left so both hits register.
        // Walk: toe.R comes down onto the floor on frame 17, toe.L on frame 3 (33 in the second loop).
        public const string EmberfangGustBoneL = "wing_f1_b.L";
        public const string EmberfangGustBoneR = "wing_f1_b.R";
        public static readonly int[] EmberfangGustFrames = { 15, 16 };
        public static readonly string[] EmberfangStompBones = { "toe.R", "toe.L", "toe.R" };
        public static readonly int[] EmberfangFootfallFrames = { 17, 33, 47 };

        // Stormcrest. Wing flap: wing_hand.L/R bottom out on frame 16 after the downstroke from frame 2.
        // Walk: toe.R lands on frame 15, toe.L on frame 30 (back to the floor at the loop seam).
        public const string StormcrestGustBoneL = "wing_hand.L";
        public const string StormcrestGustBoneR = "wing_hand.R";
        public static readonly int[] StormcrestGustFrames = { 16, 17 };
        public static readonly string[] StormcrestStompBones = { "toe.R", "toe.L", "toe.R" };
        public static readonly int[] StormcrestFootfallFrames = { 15, 30, 45 };

        // Ashwyrm. Wing flap: the clip opens on the upstroke, the wing tips (wing_f1) peak on frame 8, sweep down fastest
        // around frames 15-19 and bottom out on frame 23. Walk: shin.L comes down on frame 18, shin.R on
        // frame 2 (32 in the second loop).
        public const string AshwyrmGustBoneL = "wing_f1.L";
        public const string AshwyrmGustBoneR = "wing_f1.R";
        public static readonly int[] AshwyrmGustFrames = { 23, 24 };
        public static readonly string[] AshwyrmStompBones = { "shin.L", "shin.R", "shin.L" };
        public static readonly int[] AshwyrmFootfallFrames = { 18, 32, 48 };

        // Nightfang. Locked attack: the snout reaches furthest forward on frame 12 (the bite), both fore paws on frame 14
        // (toe.L is attack 1's claw rake). Trot: the left fore paw comes down onto the floor on frame 17, the right fore paw on frame 27,
        // the left again on frame 47 in the second loop.
        public const int NightfangBiteFrame = 12;
        public const int NightfangRightClawFrame = 14;
        public static readonly string[] NightfangWaveBones = { "toe.L", "toe.R", "toe.L" };
        public static readonly int[] NightfangPawfallFrames = { 17, 27, 47 };

        /// <summary>Nightfang's pounce: a short host slide toward the target through the bite and claw.</summary>
        public static readonly LockedBattleReachShift NightfangPounce = new LockedBattleReachShift(0.30f, 5f, 11f, 16f, 26f);

        public static LockedBattleAttack[] EmberfangAttacks()
        {
            return new[]
            {
                new LockedBattleAttack("fire breath fireball", EmberfangMotion.AttackPoseName, BlenderRigAttackSpec.ClipSeconds),
                WingGust("ember wing gust", EmberfangMotion.FlapPoseName, EmberfangMotion.FlapSeconds, EmberfangGustBoneL, EmberfangGustBoneR, EmberfangGustFrames, 0.03f),
                StompQuake("stomp quake", EmberfangMotion.WalkPoseName, EmberfangStompBones, EmberfangFootfallFrames, 0.05f)
            };
        }

        public static LockedBattleAttack[] StormcrestAttacks()
        {
            return new[]
            {
                new LockedBattleAttack("wing buffet storm bolt", StormcrestMotion.AttackPoseName, BlenderRigAttackSpec.ClipSeconds),
                WingGust("gale gust", StormcrestMotion.WingFlapPoseName, StormcrestMotion.WalkSeconds, StormcrestGustBoneL, StormcrestGustBoneR, StormcrestGustFrames, 0.025f),
                StompQuake("thunder stomp", StormcrestMotion.WalkPoseName, StormcrestStompBones, StormcrestFootfallFrames, 0.035f)
            };
        }

        public static LockedBattleAttack[] AshwyrmAttacks()
        {
            return new[]
            {
                new LockedBattleAttack("void-fire breath fireball", AshwyrmMotion.AttackPoseName, BlenderRigAttackSpec.ClipSeconds),
                WingGust("void wing gust", AshwyrmMotion.FlapPoseName, AshwyrmMotion.FlapSeconds, AshwyrmGustBoneL, AshwyrmGustBoneR, AshwyrmGustFrames, 0.03f),
                StompQuake("void stomp quake", AshwyrmMotion.WalkPoseName, AshwyrmStompBones, AshwyrmFootfallFrames, 0.05f)
            };
        }

        public static LockedBattleAttack[] NightfangAttacks()
        {
            return new[]
            {
                new LockedBattleAttack("lunge bite + claw rake", NightfangMotion.AttackPoseName, BlenderRigAttackSpec.ClipSeconds),
                new LockedBattleAttack(
                    "shadow pounce bite",
                    NightfangMotion.AttackPoseName,
                    BlenderRigAttackSpec.ClipSeconds,
                    NightfangBiteFrame,
                    "snout",
                    0.03f,
                    false,
                    NightfangPounce,
                    BattleImpactKind.VillainShadowLash,
                    new[] { new LockedBattleHitMark(NightfangRightClawFrame, "toe.R") }),
                new LockedBattleAttack(
                    "shadow claw wave",
                    NightfangMotion.TrotPoseName,
                    TwoLoopSeconds,
                    NightfangPawfallFrames[0] + ClawWaveFlightFrames,
                    NightfangWaveBones[0],
                    0.02f,
                    false,
                    default,
                    BattleImpactKind.ClawWave,
                    new[]
                    {
                        new LockedBattleHitMark(NightfangPawfallFrames[1] + ClawWaveFlightFrames, NightfangWaveBones[1]),
                        new LockedBattleHitMark(NightfangPawfallFrames[2] + ClawWaveFlightFrames, NightfangWaveBones[2])
                    },
                    ClawWaveFlightFrames,
                    0f,
                    0.25f)
            };
        }

        /// <summary>
        /// One wing flap: each wing tip looses a gust at the end of its downstroke (hit frame minus the flight) that streaks
        /// to the target and bursts there; the two gusts land 0.4 m apart.
        /// </summary>
        private static LockedBattleAttack WingGust(string label, string pose, float seconds, string boneL, string boneR, int[] bottomFrames, float shake)
        {
            return new LockedBattleAttack(
                label,
                pose,
                seconds,
                bottomFrames[0] + GustFlightFrames,
                boneL,
                shake,
                false,
                default,
                BattleImpactKind.WingGust,
                new[] { new LockedBattleHitMark(bottomFrames[1] + GustFlightFrames, boneR) },
                GustFlightFrames,
                0.15f,
                0.4f);
        }

        /// <summary>
        /// Two walk loops in place: each footfall sends a ground crack (no loft) from the stamping foot to the target, where
        /// it bursts as a quake.
        /// </summary>
        private static LockedBattleAttack StompQuake(string label, string pose, string[] bones, int[] footfalls, float shake)
        {
            return new LockedBattleAttack(
                label,
                pose,
                TwoLoopSeconds,
                footfalls[0] + QuakeFlightFrames,
                bones[0],
                shake,
                false,
                default,
                BattleImpactKind.StompQuake,
                new[]
                {
                    new LockedBattleHitMark(footfalls[1] + QuakeFlightFrames, bones[1]),
                    new LockedBattleHitMark(footfalls[2] + QuakeFlightFrames, bones[2])
                },
                QuakeFlightFrames);
        }
    }
}
