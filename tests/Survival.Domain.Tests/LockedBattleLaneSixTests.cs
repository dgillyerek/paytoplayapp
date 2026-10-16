using Survival.Domain.Enemies;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

/// <summary>Lane 6 (Derek 2026-10-16): Rowan vs Bonequill, both archers.</summary>
public sealed class LockedBattleLaneSixTests
{
    [Fact]
    public void Archers_shoot_from_range_on_their_locked_clips_and_cycle_attack_themselves()
    {
        foreach (var f in new[] { LockedBattleRoster.Rowan, LockedBattleRoster.Bonequill })
        {
            Assert.Equal(LockedBattleRoster.ArcherRangeMetres, f.AttackRangeMetres);
            Assert.True(f.AttackRangeMetres > 3.5f, f.Name + " shoots from range, not melee");
            Assert.True(f.AttackRangeMetres < LockedBattleRoster.StartHalfGapMetres * 2f);
            Assert.True(f.AttackCyclesItself);
            Assert.False(f.WalkCarriesRoot);
            Assert.Equal("walk", f.WalkPoseName);
            Assert.Equal(BlenderRigAttackSpec.PoseName, f.AttackPoseName);
            Assert.True(LockedBattleHits.ProfileOf(f.Name).Projectile);
        }

        Assert.Contains(LockedBattleRoster.Rowan.WalkPoseName, RowanMotion.PoseNames);
        Assert.Contains(LockedBattleRoster.Bonequill.WalkPoseName, BonequillMotion.PoseNames);
        Assert.Equal(0f, LockedBattleRoster.Bonequill.AimYawDegrees);
        foreach (var f in LockedBattleRoster.All)
        {
            if (f.Name != "Rowan")
            {
                Assert.Equal(0f, f.AimYawDegrees);
            }
        }
    }

    [Fact]
    public void Each_arrow_flies_from_release_and_stops_on_the_opponents_body_box()
    {
        Check(RowanAttack.Spec, RowanAttack.ReleaseFrame, LockedBattleRoster.Rowan.AimYawDegrees, LockedBattleHits.Bonequill.Box);
        Check(BonequillAttack.Spec, BonequillAttack.ReleaseFrame, LockedBattleRoster.Bonequill.AimYawDegrees, LockedBattleHits.Rowan.Box);

        // Without the aim yaw Rowan's locked arrow (15.25 deg left of his forward) would miss.
        Assert.False(Hits(RowanAttack.Spec, 0f, LockedBattleHits.Bonequill.Box, LockedBattleRoster.ArcherRangeMetres, out _));
    }

    private static void Check(BlenderRigAttackSpec spec, int release, float aimYaw, BattleHitBox targetBox)
    {
        foreach (var range in new[] { LockedBattleRoster.ArcherRangeMetres - 0.2f, LockedBattleRoster.ArcherRangeMetres })
        {
            Assert.True(Hits(spec, aimYaw, targetBox, range, out var firstVisible), spec.FileName + " at " + range);
            Assert.Equal(release, firstVisible);
        }
    }

    /// <summary>
    /// Walks the arrow track frame by frame in the archer's frame (yawed by the aim), converts each
    /// segment into the target's box frame (target faces the archer at <paramref name="range"/>),
    /// and reports whether it enters the box before the track ends (it then stops there).
    /// </summary>
    private static bool Hits(BlenderRigAttackSpec spec, float aimYaw, BattleHitBox box, float range, out int firstVisible)
    {
        firstVisible = -1;
        AttackTrack? arrow = null;
        foreach (var t in spec.Tracks)
        {
            if (t.Kind == AttackTrackKind.Projectile && t.PropFileName.Length > 0)
            {
                arrow = t;
                break;
            }
        }

        Assert.NotNull(arrow);
        var yaw = aimYaw * Math.PI / 180.0;
        var c = (float)Math.Cos(yaw);
        var s = (float)Math.Sin(yaw);
        float? px = null, py = null, pz = null;
        for (var i = 0; i < arrow!.Frames.Length; i++)
        {
            var fr = arrow.Frames[i];
            if (!fr.Visible)
            {
                continue;
            }

            if (firstVisible < 0)
            {
                firstVisible = i;
            }

            // Archer frame -> world (archer at origin facing +Z, turned by the aim yaw).
            var wx = (fr.X * c) + (fr.Z * s);
            var wz = (-fr.X * s) + (fr.Z * c);
            // World -> target box frame (target at z = range facing -Z).
            var bx = -wx;
            var bz = range - wz;
            if (px.HasValue && box.SegmentEntry(px.Value, py!.Value, pz!.Value, bx, fr.Y, bz, out _))
            {
                return true;
            }

            px = bx;
            py = fr.Y;
            pz = bz;
        }

        return false;
    }
}
