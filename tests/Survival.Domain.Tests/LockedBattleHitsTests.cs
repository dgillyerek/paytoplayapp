using Survival.Domain.Enemies;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

public sealed class LockedBattleHitsTests
{
    private static LockedBattleFighter Fighter(string name) => LockedBattleRoster.All.Single(f => f.Name == name);

    [Fact]
    public void Every_fighter_has_its_own_impact_and_flying_attacks_are_the_ones_with_projectile_tracks()
    {
        var kinds = LockedBattleRoster.All.Select(f => LockedBattleHits.ProfileOf(f.Name).Impact).ToArray();
        Assert.Equal(kinds.Length, kinds.Distinct().Count());
        Assert.Equal(BattleImpactKind.FireBurst, LockedBattleHits.Emberfang.Impact);
        Assert.Equal(BattleImpactKind.StormCrackle, LockedBattleHits.Stormcrest.Impact);
        Assert.Equal(BattleImpactKind.ShadowRake, LockedBattleHits.Nightfang.Impact);
        Assert.Equal(BattleImpactKind.IronClaw, LockedBattleHits.Ironhowl.Impact);

        Assert.True(LockedBattleHits.Emberfang.Projectile);
        Assert.Contains(EmberfangAttack.Spec.Tracks, t => t.Kind == AttackTrackKind.Projectile);
        Assert.True(LockedBattleHits.Stormcrest.Projectile);
        Assert.Contains(StormcrestAttack.Spec.Tracks, t => t.Kind == AttackTrackKind.Projectile && t.Shape == AttackVfxShape.Lightning);
        Assert.False(LockedBattleHits.Nightfang.Projectile);
        Assert.DoesNotContain(NightfangAttack.Spec.Tracks, t => t.Kind == AttackTrackKind.Projectile);
        Assert.False(LockedBattleHits.Ironhowl.Projectile);
        Assert.Throws<ArgumentException>(() => LockedBattleHits.ProfileOf("Ashwyrm"));
    }

    [Fact]
    public void Melee_contact_frames_are_where_the_striking_bone_reaches_furthest_inside_the_clip()
    {
        // Measured on the locked FBXs: Nightfang toe.L peaks 1.13 m forward on frame 14 (the
        // Design release, inside the claw slash), Ironhowl RightHand peaks 1.16 m on frame 11.
        Assert.Equal(-1, LockedBattleHits.Emberfang.MeleeContactFrame);
        Assert.Equal(-1, LockedBattleHits.Stormcrest.MeleeContactFrame);

        Assert.Equal(NightfangAttack.ReleaseFrame, LockedBattleHits.Nightfang.MeleeContactFrame);
        Assert.Equal("toe.L", LockedBattleHits.Nightfang.StrikeBone);
        var slash = NightfangAttack.Spec.Tracks.Single(t => t.Shape == AttackVfxShape.Slash);
        Assert.True(slash.Frames[LockedBattleHits.Nightfang.MeleeContactFrame].Visible);

        Assert.Equal(11, LockedBattleHits.Ironhowl.MeleeContactFrame);
        Assert.Equal("mixamorig:RightHand", LockedBattleHits.Ironhowl.StrikeBone);
        Assert.InRange(LockedBattleHits.Ironhowl.MeleeContactFrame, 1, IronhowlMotion.AttackLastFrame - 1);
        Assert.Equal(11f / 30f, LockedBattleHits.Ironhowl.MeleeContactSeconds, 4);
    }

    [Theory]
    [InlineData("Nightfang")]
    [InlineData("Ironhowl")]
    public void Melee_lands_once_per_attack_cycle_on_its_contact_frame_and_never_in_the_rest_gap(string name)
    {
        var fighter = Fighter(name);
        var profile = LockedBattleHits.ProfileOf(name);
        var hits = new List<float>();
        var dt = 1f / 60f;
        var previous = 0f;
        for (var i = 1; i <= 600; i++)
        {
            var now = i * dt;
            if (LockedBattleHits.MeleeHitCrossed(fighter, profile, previous, now))
            {
                hits.Add(now);
            }

            previous = now;
        }

        var cycles = (int)Math.Floor(((600 * dt) - profile.MeleeContactSeconds) / fighter.AttackCycleSeconds) + 1;
        Assert.Equal(cycles, hits.Count);
        for (var n = 0; n < hits.Count; n++)
        {
            var expected = (n * fighter.AttackCycleSeconds) + profile.MeleeContactSeconds;
            Assert.InRange(hits[n], expected, expected + dt + 1e-4f);
            Assert.True(LockedBattleRoster.InAttackClip(fighter, hits[n]), name + " hit in rest gap at " + hits[n]);
        }

        Assert.False(LockedBattleHits.MeleeHitCrossed(fighter, profile, 0.5f, 0.5f));
        Assert.False(LockedBattleHits.MeleeHitCrossed(fighter, LockedBattleHits.Emberfang, 0f, 10f));
    }

    [Fact]
    public void Projectiles_stop_on_the_front_of_the_target_body_and_do_not_pass_through()
    {
        foreach (var attacker in LockedBattleRoster.All.Where(f => LockedBattleHits.ProfileOf(f.Name).Projectile))
        {
            var target = LockedBattleRoster.TargetOf(attacker);
            var box = LockedBattleHits.ProfileOf(target.Name).Box;

            // Defender frame (+Z toward the attacker): a shot from attack range flying straight in,
            // covering 0.3 m per frame (Emberfang's 9 m/s fireball at 30 fps).
            var z = attacker.AttackRangeMetres;
            var y = box.Height * 0.5f;
            var stopped = false;
            while (z > -box.Back - 1f)
            {
                var next = z - 0.3f;
                if (box.SegmentEntry(0f, y, z, 0f, y, next, out var t))
                {
                    var contactZ = z + ((next - z) * t);
                    Assert.Equal(box.Front, contactZ, 4);
                    stopped = true;
                    break;
                }

                z = next;
            }

            Assert.True(stopped, attacker.Name + " shot passed through " + target.Name);
        }
    }

    [Fact]
    public void Body_boxes_hit_inside_and_miss_beside_and_closest_point_lands_on_the_surface()
    {
        var box = LockedBattleHits.Ironhowl.Box;
        Assert.True(box.SegmentEntry(0f, 1f, 0f, 0f, 1f, 2f, out var inside));
        Assert.Equal(0f, inside);
        Assert.False(box.SegmentEntry(2f, 1f, 2f, 2f, 1f, -2f, out _));
        Assert.False(box.SegmentEntry(0f, box.Height + 0.5f, 2f, 0f, box.Height + 0.5f, -2f, out _));

        // Nightfang's paw stops just short of a dragon: the impact sits on the box front.
        var dragon = LockedBattleHits.Stormcrest.Box;
        dragon.ClosestPoint(0.05f, 0.17f, dragon.Front + 0.08f, out var cx, out var cy, out var cz);
        Assert.Equal(dragon.Front, cz, 4);
        Assert.Equal(0.17f, cy, 4);
        Assert.Equal(0.05f, cx, 4);

        foreach (var f in LockedBattleRoster.All)
        {
            var b = LockedBattleHits.ProfileOf(f.Name).Box;
            Assert.True(b.Height <= f.DesignHeightMetres * 1.0001f, f.Name);
            Assert.True(b.HalfWidth > 0.1f && b.Front > 0.1f && b.Back > 0.1f, f.Name);
        }
    }

    [Fact]
    public void Flinch_is_short_eases_back_and_is_smaller_for_bigger_bodies()
    {
        Assert.Equal(0f, LockedBattleHits.RecoilWeight(0f));
        Assert.Equal(1f, LockedBattleHits.RecoilWeight(LockedBattleHits.RecoilPeakSeconds), 4);
        Assert.Equal(0f, LockedBattleHits.RecoilWeight(LockedBattleHits.RecoilSeconds));
        Assert.Equal(0.25f, LockedBattleHits.RecoilSeconds, 4);
        var last = 1f;
        for (var t = LockedBattleHits.RecoilPeakSeconds; t < LockedBattleHits.RecoilSeconds; t += 0.01f)
        {
            var w = LockedBattleHits.RecoilWeight(t);
            Assert.True(w <= last + 1e-5f);
            last = w;
        }

        Assert.Equal(1f, LockedBattleHits.FlashWeight(0f), 4);
        Assert.Equal(0f, LockedBattleHits.FlashWeight(LockedBattleHits.FlashSeconds));
        Assert.Equal(0.1f, LockedBattleHits.FlashSeconds, 4);

        var ironhowl = LockedBattleHits.Ironhowl;
        foreach (var dragon in new[] { LockedBattleHits.Emberfang, LockedBattleHits.Stormcrest, LockedBattleHits.Nightfang })
        {
            Assert.True(ironhowl.RecoilMetres < dragon.RecoilMetres, dragon.Name);
            Assert.True(ironhowl.RecoilDegrees < dragon.RecoilDegrees, dragon.Name);
        }

        Assert.True(LockedBattleHits.Emberfang.RecoilMetres >= LockedBattleHits.Stormcrest.RecoilMetres);
        foreach (var f in LockedBattleRoster.All)
        {
            var p = LockedBattleHits.ProfileOf(f.Name);
            Assert.InRange(p.RecoilMetres, 0.01f, 0.10f);
            Assert.InRange(p.RecoilDegrees, 1f, 8f);
            Assert.InRange(p.ShakeMetres, 0f, 0.08f);
        }
    }

    [Fact]
    public void Recoil_is_skipped_while_the_defender_is_inside_its_own_attack_clip()
    {
        var ironhowl = Fighter("Ironhowl");
        Assert.True(LockedBattleHits.RecoilAllowed(ironhowl, false, 0.3f));
        Assert.False(LockedBattleHits.RecoilAllowed(ironhowl, true, 0.3f));
        Assert.True(LockedBattleHits.RecoilAllowed(ironhowl, true, ironhowl.AttackSeconds + 0.1f));
        Assert.False(LockedBattleHits.RecoilAllowed(ironhowl, true, ironhowl.AttackCycleSeconds + 0.1f));
    }
}
