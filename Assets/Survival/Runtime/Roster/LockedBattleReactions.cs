using System;

namespace Survival.Domain.Roster
{
    /// <summary>How a struck fighter reacts (Derek 2026-10-19: "a minor and appropriate reaction").</summary>
    public enum BattleReactionKind
    {
        /// <summary>Arrows, bolts, orbs: a light flinch of the head and chest, a small step back.</summary>
        Flinch = 0,

        /// <summary>Melee claws, blades, spears, lashes: the head snaps back, the chest follows, a step back.</summary>
        HeadSnap = 1,

        /// <summary>Heavy hits (fireballs, stomp quakes, overhead and jump smashes): a bigger, slower stagger back.</summary>
        Stagger = 2,

        /// <summary>Wing gusts: the chest braces into the wind while the head and wings are blown back.</summary>
        GustLean = 3
    }

    /// <summary>
    /// One hit reaction: a short additive on top of whatever the fighter is playing (never a clip, never a bone the legs hang
    /// from, so the feet stay planted), plus the actor-root step back and lean the battle already had. Angles are degrees away
    /// from the attacker (negative = toward it); root scales multiply the defender's own <see cref="LockedBattleHitProfile"/>
    /// recoil metres / degrees.
    /// </summary>
    public readonly struct LockedBattleReaction
    {
        public LockedBattleReaction(
            BattleReactionKind kind,
            float seconds,
            float peakSeconds,
            float rootScale,
            float rootLeanScale,
            float spineDegrees,
            float headDegrees,
            float wingDegrees,
            float tailDegrees)
        {
            Kind = kind;
            Seconds = seconds;
            PeakSeconds = peakSeconds;
            RootScale = rootScale;
            RootLeanScale = rootLeanScale;
            SpineDegrees = spineDegrees;
            HeadDegrees = headDegrees;
            WingDegrees = wingDegrees;
            TailDegrees = tailDegrees;
        }

        public BattleReactionKind Kind { get; }

        /// <summary>Whole reaction, after which the fighter is back on its own pose (idle, walk or next attack).</summary>
        public float Seconds { get; }

        /// <summary>Time to the peak of the reaction.</summary>
        public float PeakSeconds { get; }

        public float RootScale { get; }
        public float RootLeanScale { get; }

        /// <summary>Total over the upper-body bones (shared between them).</summary>
        public float SpineDegrees { get; }

        /// <summary>Total over the neck and head bones (shared between them).</summary>
        public float HeadDegrees { get; }

        /// <summary>Per wing root: raised (flinch, stagger) or swept back (gust).</summary>
        public float WingDegrees { get; }

        /// <summary>Total over the tail bones (shared between them).</summary>
        public float TailDegrees { get; }

        /// <summary>0 → 1 by <see cref="PeakSeconds"/> (eased), back to 0 by <see cref="Seconds"/> (eased). 0 outside.</summary>
        public float Weight(float secondsSinceHit)
        {
            if (secondsSinceHit <= 0f || secondsSinceHit >= Seconds)
            {
                return 0f;
            }

            if (secondsSinceHit < PeakSeconds)
            {
                var u = secondsSinceHit / PeakSeconds;
                return u * (2f - u);
            }

            var v = (secondsSinceHit - PeakSeconds) / (Seconds - PeakSeconds);
            return 1f - (v * v * (3f - (2f * v)));
        }
    }

    /// <summary>Bones the reaction additive bends on one fighter. Missing names are skipped by the battle.</summary>
    public readonly struct LockedBattleReactionRig
    {
        public LockedBattleReactionRig(string[] spine, string[] head, string[] wings, string[] tail)
        {
            Spine = spine;
            Head = head;
            Wings = wings;
            Tail = tail;
        }

        /// <summary>Upper-body bones the legs do not hang from (Spine1/Spine2 on the Mixamo rigs). Empty for the creatures.</summary>
        public string[] Spine { get; }

        public string[] Head { get; }

        /// <summary>Wing roots (the dragons and Stormcrest).</summary>
        public string[] Wings { get; }

        public string[] Tail { get; }
    }

    public static class LockedBattleReactions
    {
        /// <summary>A defender struck inside its own attack clip only gets this much of the bone additive and no step back.</summary>
        public const float MidAttackScale = 0.35f;

        /// <summary>Attacks with at least this much camera shake are heavy: their hit staggers whatever the impact kind.</summary>
        public const float HeavyShakeMetres = 0.08f;

        public static readonly LockedBattleReaction Flinch =
            new LockedBattleReaction(BattleReactionKind.Flinch, 0.30f, 0.05f, 0.5f, 0.5f, 4f, 7f, 6f, 6f);

        public static readonly LockedBattleReaction HeadSnap =
            new LockedBattleReaction(BattleReactionKind.HeadSnap, 0.40f, 0.06f, 1f, 1f, 6f, 14f, 8f, 10f);

        public static readonly LockedBattleReaction Stagger =
            new LockedBattleReaction(BattleReactionKind.Stagger, 0.60f, 0.10f, 1.6f, 1.4f, 10f, 12f, 14f, 16f);

        /// <summary>The chest braces 5 degrees into the gust (negative), the head and wings are blown back; no root lean.</summary>
        public static readonly LockedBattleReaction GustLean =
            new LockedBattleReaction(BattleReactionKind.GustLean, 0.50f, 0.14f, 0.7f, 0f, -5f, 9f, 16f, 12f);

        public static LockedBattleReaction For(BattleReactionKind kind)
        {
            switch (kind)
            {
                case BattleReactionKind.HeadSnap:
                    return HeadSnap;
                case BattleReactionKind.Stagger:
                    return Stagger;
                case BattleReactionKind.GustLean:
                    return GustLean;
                default:
                    return Flinch;
            }
        }

        /// <summary>Reaction a hit of this impact causes on its own (before the heavy-shake upgrade).</summary>
        public static BattleReactionKind KindFor(BattleImpactKind impact)
        {
            switch (impact)
            {
                // Flying light hits.
                case BattleImpactKind.StormCrackle:
                case BattleImpactKind.ArcaneConjure:
                case BattleImpactKind.StaffLight:
                case BattleImpactKind.RangerArrow:
                case BattleImpactKind.BoneShard:
                case BattleImpactKind.VillainHexRain:
                case BattleImpactKind.HeroArcaneNova:
                    return BattleReactionKind.Flinch;

                // Heavy.
                case BattleImpactKind.FireBurst:
                case BattleImpactKind.VoidFireBreath:
                case BattleImpactKind.GroundSlam:
                case BattleImpactKind.StompQuake:
                    return BattleReactionKind.Stagger;

                case BattleImpactKind.WingGust:
                    return BattleReactionKind.GustLean;

                // Melee: claws, blades, spears, lashes, roots, the shield bash, the claw wave.
                default:
                    return BattleReactionKind.HeadSnap;
            }
        }

        /// <summary>Reaction to hit of attack <paramref name="attackIndex"/> of <paramref name="attacker"/>.</summary>
        public static LockedBattleReaction ForHit(LockedBattleFighter attacker, LockedBattleHitProfile attackerProfile, int attackIndex)
        {
            var kind = KindFor(LockedBattleRotation.Impact(attacker, attackerProfile, attackIndex));
            if (kind != BattleReactionKind.GustLean
                && LockedBattleRotation.ShakeMetres(attacker, attackerProfile, attackIndex) >= HeavyShakeMetres)
            {
                kind = BattleReactionKind.Stagger;
            }

            return For(kind);
        }

        /// <summary>1 normally, <see cref="MidAttackScale"/> when the defender is inside its own attack clip (its wind-up or strike).</summary>
        public static float Scale(LockedBattleFighter defender, bool defenderAttacking, float defenderAttackSeconds)
        {
            return LockedBattleHits.RecoilAllowed(defender, defenderAttacking, defenderAttackSeconds) ? 1f : MidAttackScale;
        }

        private static readonly string[] None = Array.Empty<string>();

        private static readonly LockedBattleReactionRig Mixamo = new LockedBattleReactionRig(
            new[] { "mixamorig:Spine1", "mixamorig:Spine2" },
            new[] { "mixamorig:Neck", "mixamorig:Head" },
            None,
            None);

        /// <summary>
        /// Reaction bones per fighter. The creatures' chest / spine bones carry their legs (measured on the locked FBX
        /// hierarchies), so only the neck, head, wings and tail bend and the feet stay where the clip put them. Ashwyrm's
        /// 30-bone rig has no neck or head bones; it reacts through its wings, tail and the root step.
        /// </summary>
        public static LockedBattleReactionRig RigFor(LockedBattleFighter fighter)
        {
            switch (fighter.Name)
            {
                case "Emberfang":
                    return new LockedBattleReactionRig(
                        None,
                        new[] { "neck_01", "neck_02", "neck_03", "head" },
                        new[] { "wing_root.L", "wing_root.R" },
                        new[] { "tail_01", "tail_02", "tail_03" });
                case "Stormcrest":
                    return new LockedBattleReactionRig(
                        None,
                        new[] { "neck", "neck_02", "head" },
                        new[] { "wing_arm.L", "wing_arm.R" },
                        new[] { "tail_01", "tail_02" });
                case "Ashwyrm":
                    return new LockedBattleReactionRig(
                        None,
                        None,
                        new[] { "wing_root.L", "wing_root.R" },
                        new[] { "tail_01", "tail_02", "tail_03" });
                case "Nightfang":
                    return new LockedBattleReactionRig(
                        None,
                        new[] { "neck", "head" },
                        None,
                        new[] { "tail_01", "tail_02" });
                default:
                    return Mixamo;
            }
        }
    }
}
