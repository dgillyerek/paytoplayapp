using System;
using System.Collections.Generic;
using Survival.Domain.Enemies;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek (2026-10-19): "if someone gets hit they need a minor and appropriate reaction." Every fighter reacts to every hit
/// with a short procedural additive (flinch, head snap, stagger, gust lean) on top of its pose plus the root step back, a
/// lighter one when struck inside its own attack clip.
/// </summary>
public sealed class LockedBattleReactionTests
{
    private static LockedBattleFighter Fighter(string name)
    {
        foreach (var f in LockedBattleRoster.All)
        {
            if (f.Name == name)
            {
                return f;
            }
        }

        throw new InvalidOperationException(name);
    }

    private static BattleReactionKind KindOf(string attacker, int attack)
    {
        var f = Fighter(attacker);
        return LockedBattleReactions.ForHit(f, LockedBattleHits.ProfileOf(attacker), attack).Kind;
    }

    [Fact]
    public void Every_impact_kind_maps_to_a_fitting_reaction()
    {
        foreach (BattleImpactKind kind in Enum.GetValues(typeof(BattleImpactKind)))
        {
            Assert.True(Enum.IsDefined(typeof(BattleReactionKind), LockedBattleReactions.KindFor(kind)), kind.ToString());
        }

        foreach (var light in new[] { BattleImpactKind.RangerArrow, BattleImpactKind.BoneShard, BattleImpactKind.StormCrackle, BattleImpactKind.StaffLight, BattleImpactKind.ArcaneConjure })
        {
            Assert.Equal(BattleReactionKind.Flinch, LockedBattleReactions.KindFor(light));
        }

        foreach (var melee in new[] { BattleImpactKind.IronClaw, BattleImpactKind.ShadowRake, BattleImpactKind.SwordSteel, BattleImpactKind.LeafEarth, BattleImpactKind.BlightRot, BattleImpactKind.VillainShadowLash, BattleImpactKind.HeroStaffSweep, BattleImpactKind.HeroShieldShock, BattleImpactKind.ClawWave })
        {
            Assert.Equal(BattleReactionKind.HeadSnap, LockedBattleReactions.KindFor(melee));
        }

        foreach (var heavy in new[] { BattleImpactKind.FireBurst, BattleImpactKind.VoidFireBreath, BattleImpactKind.GroundSlam, BattleImpactKind.StompQuake })
        {
            Assert.Equal(BattleReactionKind.Stagger, LockedBattleReactions.KindFor(heavy));
        }

        Assert.Equal(BattleReactionKind.GustLean, LockedBattleReactions.KindFor(BattleImpactKind.WingGust));
    }

    [Fact]
    public void Each_attack_in_the_battle_gets_its_reaction()
    {
        // Heavy: the dragon fireballs, the stomp quakes, Ironhowl's overhead smash, Blightroot's jump smash.
        Assert.Equal(BattleReactionKind.Stagger, KindOf("Emberfang", 0));
        Assert.Equal(BattleReactionKind.Stagger, KindOf("Ashwyrm", 0));
        Assert.Equal(BattleReactionKind.Stagger, KindOf("Emberfang", 2));
        Assert.Equal(BattleReactionKind.Stagger, KindOf("Stormcrest", 2));
        Assert.Equal(BattleReactionKind.Stagger, KindOf("Ironhowl", 1));
        Assert.Equal(BattleReactionKind.Stagger, KindOf("Blightroot", 2));

        // Gusts.
        Assert.Equal(BattleReactionKind.GustLean, KindOf("Emberfang", 1));
        Assert.Equal(BattleReactionKind.GustLean, KindOf("Stormcrest", 1));
        Assert.Equal(BattleReactionKind.GustLean, KindOf("Ashwyrm", 1));

        // Arrows and bolts.
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(BattleReactionKind.Flinch, KindOf("Rowan", i));
            Assert.Equal(BattleReactionKind.Flinch, KindOf("Bonequill", i));
        }

        Assert.Equal(BattleReactionKind.Flinch, KindOf("Stormcrest", 0));
        Assert.Equal(BattleReactionKind.Flinch, KindOf("Lyra", 0));
        Assert.Equal(BattleReactionKind.Flinch, KindOf("Vespera", 0));

        // Melee.
        Assert.Equal(BattleReactionKind.HeadSnap, KindOf("Ironhowl", 0));
        Assert.Equal(BattleReactionKind.HeadSnap, KindOf("Ironhowl", 2));
        Assert.Equal(BattleReactionKind.HeadSnap, KindOf("Nightfang", 0));
        Assert.Equal(BattleReactionKind.HeadSnap, KindOf("Nightfang", 1));
        Assert.Equal(BattleReactionKind.HeadSnap, KindOf("Nightfang", 2));
        Assert.Equal(BattleReactionKind.HeadSnap, KindOf("Sir Aldric", 0));
        Assert.Equal(BattleReactionKind.HeadSnap, KindOf("Sir Aldric", 2));
        Assert.Equal(BattleReactionKind.HeadSnap, KindOf("Oakenshield", 0));
        Assert.Equal(BattleReactionKind.HeadSnap, KindOf("Blightroot", 0));
    }

    [Fact]
    public void Reactions_are_short_and_minor_and_settle_back_to_the_pose()
    {
        foreach (BattleReactionKind kind in Enum.GetValues(typeof(BattleReactionKind)))
        {
            var r = LockedBattleReactions.For(kind);
            Assert.Equal(kind, r.Kind);
            Assert.InRange(r.Seconds, 0.3f, 0.6f);
            Assert.InRange(r.PeakSeconds, 0.03f, r.Seconds * 0.35f);
            Assert.Equal(0f, r.Weight(0f));
            Assert.Equal(0f, r.Weight(r.Seconds));
            Assert.Equal(0f, r.Weight(r.Seconds + 1f));
            Assert.Equal(1f, r.Weight(r.PeakSeconds), 3);

            // A hit snaps in over 2-6 frames at 60 fps; the settle back is smooth (no frame-to-frame jump over 0.15).
            var last = 0f;
            for (var t = 0f; t <= r.Seconds + 0.05f; t += 1f / 60f)
            {
                var w = r.Weight(t);
                Assert.InRange(w, 0f, 1f);
                Assert.True(Math.Abs(w - last) < (t <= r.PeakSeconds + 1e-4f ? 0.6f : 0.15f), kind + " at " + t);
                last = w;
            }

            // Minor: no knockdown-size bends.
            Assert.InRange(Math.Abs(r.SpineDegrees), 0f, 12f);
            Assert.InRange(r.HeadDegrees, 0f, 15f);
            Assert.InRange(r.WingDegrees, 0f, 18f);
            Assert.InRange(r.TailDegrees, 0f, 18f);
            Assert.InRange(r.RootScale, 0f, 2f);

            // The largest root step stays small (biggest recoil metres in the roster times the scale).
            foreach (var f in LockedBattleRoster.All)
            {
                Assert.True(LockedBattleHits.ProfileOf(f.Name).RecoilMetres * r.RootScale <= 0.16f, f.Name + " " + kind);
            }
        }

        Assert.True(LockedBattleReactions.Flinch.RootScale < LockedBattleReactions.HeadSnap.RootScale);
        Assert.True(LockedBattleReactions.HeadSnap.RootScale < LockedBattleReactions.Stagger.RootScale);
        Assert.True(LockedBattleReactions.Flinch.Seconds < LockedBattleReactions.HeadSnap.Seconds);
        Assert.True(LockedBattleReactions.HeadSnap.Seconds < LockedBattleReactions.Stagger.Seconds);
        Assert.True(LockedBattleReactions.Flinch.HeadDegrees < LockedBattleReactions.HeadSnap.HeadDegrees);

        // The gust: chest braces into it (toward the attacker), head and wings blown back, no root tilt.
        Assert.True(LockedBattleReactions.GustLean.SpineDegrees < 0f);
        Assert.True(LockedBattleReactions.GustLean.HeadDegrees > 0f);
        Assert.Equal(0f, LockedBattleReactions.GustLean.RootLeanScale);
    }

    [Fact]
    public void Struck_mid_attack_gets_a_lighter_flinch_and_no_step()
    {
        Assert.InRange(LockedBattleReactions.MidAttackScale, 0.2f, 0.5f);
        foreach (var f in LockedBattleRoster.All)
        {
            // Inside attack 1's clip: lighter; in its rest gap or walking: full.
            Assert.Equal(LockedBattleReactions.MidAttackScale, LockedBattleReactions.Scale(f, true, 0.1f));
            Assert.False(LockedBattleHits.RecoilAllowed(f, true, 0.1f));
            Assert.Equal(1f, LockedBattleReactions.Scale(f, true, LockedBattleRotation.AttackAt(f, 0).Seconds + 0.05f));
            Assert.Equal(1f, LockedBattleReactions.Scale(f, false, 0.1f));
        }
    }

    [Fact]
    public void Every_fighter_has_reaction_bones_that_exist_and_never_carry_the_legs()
    {
        var legs = new[] { "root", "hips", "spine", "chest", "spine_01", "spine_02", "spine_03", "mixamorig:Hips", "mixamorig:Spine" };
        foreach (var f in LockedBattleRoster.All)
        {
            var rig = LockedBattleReactions.RigFor(f);
            var all = new List<string>();
            all.AddRange(rig.Spine);
            all.AddRange(rig.Head);
            all.AddRange(rig.Wings);
            all.AddRange(rig.Tail);
            Assert.NotEmpty(all);
            foreach (var bone in all)
            {
                Assert.DoesNotContain(bone, legs);
                Assert.DoesNotContain("thigh", bone, StringComparison.Ordinal);
                Assert.DoesNotContain("arm.", bone == "wing_arm.L" || bone == "wing_arm.R" ? "x" : bone, StringComparison.Ordinal);
            }

            var known = BoneNamesOf(f.Name);
            if (known != null)
            {
                foreach (var bone in all)
                {
                    Assert.Contains(bone, known);
                }
            }
        }

        // Humanoids bend Spine1/Spine2/Neck/Head (the Mixamo bones above the hips).
        foreach (var name in new[] { "Ironhowl", "Lyra", "Oakenshield", "Vespera", "Sir Aldric", "Blightroot", "Rowan", "Bonequill" })
        {
            var rig = LockedBattleReactions.RigFor(Fighter(name));
            Assert.Equal(new[] { "mixamorig:Spine1", "mixamorig:Spine2" }, rig.Spine);
            Assert.Equal(new[] { "mixamorig:Neck", "mixamorig:Head" }, rig.Head);
            Assert.Empty(rig.Wings);
        }

        // Wing flinch for the dragons and the griffin; the creatures' chest/spine carry their legs, so they never bend.
        foreach (var name in new[] { "Emberfang", "Stormcrest", "Ashwyrm" })
        {
            Assert.Equal(2, LockedBattleReactions.RigFor(Fighter(name)).Wings.Length);
        }

        foreach (var name in new[] { "Emberfang", "Stormcrest", "Ashwyrm", "Nightfang" })
        {
            Assert.Empty(LockedBattleReactions.RigFor(Fighter(name)).Spine);
            Assert.NotEmpty(LockedBattleReactions.RigFor(Fighter(name)).Tail);
        }
    }

    private static string[]? BoneNamesOf(string name)
    {
        switch (name)
        {
            case "Emberfang":
                return EmberfangMotion.BoneNames;
            case "Stormcrest":
                return StormcrestMotion.BoneNames;
            case "Nightfang":
                return NightfangMotion.BoneNames;
            case "Ashwyrm":
                return AshwyrmMotion.BoneNames;
            case "Lyra":
                return LyraMotion.BoneNames;
            case "Oakenshield":
                return OakenshieldMotion.BoneNames;
            case "Rowan":
                return RowanMotion.BoneNames;
            case "Vespera":
                return VesperaMotion.BoneNames;
            case "Bonequill":
                return BonequillMotion.BoneNames;
            default:
                return null;
        }
    }
}
