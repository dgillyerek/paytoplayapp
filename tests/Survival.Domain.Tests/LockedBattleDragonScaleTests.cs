using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek on #70 (2026-10-18): "make the two dragon looking characters much larger". Emberfang and Ashwyrm play at 2x their
/// design-height battle size, and (Derek on 8fbc6d1) duel each other in lane 3; Oakenshield and Ironhowl pair up in lane 0.
/// The lanes keep every neighbour clear, the dragons stop with their bodies apart and their fireballs land on each other,
/// and hit boxes, impact bursts, contact shadows and the walk speed grow with them. Clips and imports are untouched.
/// </summary>
public sealed class LockedBattleDragonScaleTests
{
    // Measured on the locked FBXs (Blender, every 3rd frame of rest/walk/attack) times the battle fit, metres.
    private const float EmberfangHalfWidth = 0.71f * 0.869f;
    private const float EmberfangSnout = 1.03f * 0.869f;
    private const float AshwyrmHalfSpan = 0.97f * 0.832f;
    private const float StormcrestHalfSpan = 1.25f * 0.869f;
    private const float LyraHalfWidth = 0.60f * 0.924f;
    private const float NightfangHalfWidth = 0.79f * 0.812f;
    private const float ArcherHalfWidth = 0.49f;
    private const float AshwyrmLunge = 0.776f * 0.832f;
    private const float EmberfangFit = 0.869f;
    private const float AshwyrmFit = 0.832f;
    private const float IronhowlHalfWidth = 0.626f * 0.892f;
    private const float IronhowlFront = 0.442f * 0.892f;
    private const float IronhowlClaw = 1.181f * 0.892f;
    private const float OakenshieldFront = 0.195f * 0.955f;

    [Fact]
    public void Only_the_two_dragons_are_scaled_and_about_twice_as_big()
    {
        foreach (var f in LockedBattleRoster.All)
        {
            var s = LockedBattleRoster.BattleScaleOf(f);
            if (f.Name == "Emberfang" || f.Name == "Ashwyrm")
            {
                Assert.InRange(s, 1.8f, 2.2f);
            }
            else
            {
                Assert.Equal(1f, s);
            }

            Assert.Equal(f.WalkSpeedMetresPerSecond * s, LockedBattleRoster.BattleWalkSpeedOf(f), 4);
        }
    }

    [Fact]
    public void Dragons_share_a_lane_and_the_old_opponents_pair_up()
    {
        Assert.Equal(LockedBattleRoster.Ashwyrm.Lane, LockedBattleRoster.Emberfang.Lane);
        Assert.Equal("Ashwyrm", LockedBattleRoster.TargetOf(LockedBattleRoster.Emberfang).Name);
        Assert.Equal("Emberfang", LockedBattleRoster.TargetOf(LockedBattleRoster.Ashwyrm).Name);
        Assert.Equal(LockedBattleRoster.Ironhowl.Lane, LockedBattleRoster.Oakenshield.Lane);
        Assert.Equal("Ironhowl", LockedBattleRoster.TargetOf(LockedBattleRoster.Oakenshield).Name);
        Assert.Equal("Oakenshield", LockedBattleRoster.TargetOf(LockedBattleRoster.Ironhowl).Name);
        // every other matchup stays in its lane
        Assert.Equal("Nightfang", LockedBattleRoster.TargetOf(LockedBattleRoster.Stormcrest).Name);
        Assert.Equal("Vespera", LockedBattleRoster.TargetOf(LockedBattleRoster.Lyra).Name);
        Assert.Equal("Blightroot", LockedBattleRoster.TargetOf(LockedBattleRoster.SirAldric).Name);
        Assert.Equal("Bonequill", LockedBattleRoster.TargetOf(LockedBattleRoster.Rowan).Name);
    }

    [Fact]
    public void Lanes_clear_their_neighbours_and_the_field_is_narrower_than_the_spread_layout()
    {
        var ember = EmberfangHalfWidth * LockedBattleRoster.EmberfangBattleScale;
        var ash = AshwyrmHalfSpan * LockedBattleRoster.AshwyrmBattleScale;
        var dragon = Math.Max(ember, ash);
        const float gap = 0.25f;
        float X(int lane) => LockedBattleRoster.LaneX(lane);
        // left to right: 4, 2, 0, 1, 3 (dragons), 5
        Assert.True(X(1) - X(0) >= IronhowlHalfWidth + StormcrestHalfSpan + gap);
        Assert.True(X(0) - X(2) >= IronhowlHalfWidth + LyraHalfWidth + gap);
        Assert.True(X(3) - X(1) >= dragon + StormcrestHalfSpan + gap);
        Assert.True(X(3) - X(1) >= dragon + NightfangHalfWidth + gap);
        Assert.True(X(5) - X(3) >= dragon + ArcherHalfWidth + gap);
        Assert.True(X(2) - X(4) >= 1.2f);
        // 8fbc6d1 spread the lanes from -4.85 to 6.9 m; the duel layout gives some of it back.
        Assert.True(X(5) - X(4) < 6.9f + 4.85f - 1f);
    }

    [Fact]
    public void Dragons_stop_with_their_bodies_apart_and_their_fireballs_land_on_each_other()
    {
        var range = LockedBattleRoster.Emberfang.AttackRangeMetres;
        Assert.Equal(range, LockedBattleRoster.Ashwyrm.AttackRangeMetres, 4);
        Assert.True(range < LockedBattleRoster.StartHalfGapMetres * 2f);
        var es = EmberfangFit * LockedBattleRoster.EmberfangBattleScale;
        var ws = AshwyrmFit * LockedBattleRoster.AshwyrmBattleScale;
        // both lunging at once, the snouts stay apart
        Assert.True(range - (EmberfangSnout * LockedBattleRoster.EmberfangBattleScale) - (AshwyrmLunge * LockedBattleRoster.AshwyrmBattleScale) > 0.5f);

        AssertFireballLands("Emberfang", EmberfangAttack(), es, LockedBattleHits.Ashwyrm.Box, LockedBattleRoster.AshwyrmBattleScale, range);
        AssertFireballLands("Ashwyrm", AshwyrmAttack(), ws, LockedBattleHits.Emberfang.Box, LockedBattleRoster.EmberfangBattleScale, range);
    }

    private static AttackTrack EmberfangAttack() =>
        Survival.Domain.Heroes.EmberfangAttack.Spec.Tracks.Single(t => t.Kind == AttackTrackKind.Projectile);

    private static AttackTrack AshwyrmAttack() =>
        Survival.Domain.Enemies.AshwyrmAttack.Spec.Tracks.Single(t => t.Kind == AttackTrackKind.Projectile);

    /// <summary>
    /// Flies the authored fireball (attacker frame, scaled to the battle body) at the defender's 2x box set at the duel
    /// range, the way <c>ProjectileHit</c> does: world point → defender box frame (facing back, divided by its battle scale).
    /// </summary>
    private static void AssertFireballLands(string name, AttackTrack track, float scale, BattleHitBox box, float defenderScale, float range)
    {
        float LocalZ(float forward) => (range - (forward * scale)) / defenderScale;
        var release = -1;
        for (var f = 0; f < track.Frames.Length; f++)
        {
            if (track.Frames[f].Visible)
            {
                release = f;
                break;
            }
        }

        Assert.True(release >= 0, name);
        var r = track.Frames[release];
        // released outside the target box, so the flight is seen
        Assert.True(LocalZ(r.Z) > box.Front + (0.5f / defenderScale), name + " fireball starts inside its target");
        var hit = false;
        for (var f = release + 1; f < track.Frames.Length && !hit; f++)
        {
            var a = track.Frames[f - 1];
            var b = track.Frames[f];
            if (box.SegmentEntry(-a.X * scale / defenderScale, a.Y * scale / defenderScale, LocalZ(a.Z), -b.X * scale / defenderScale, b.Y * scale / defenderScale, LocalZ(b.Z), out var t))
            {
                Assert.Equal(box.Front, LocalZ(a.Z) + (t * (LocalZ(b.Z) - LocalZ(a.Z))), 3);
                hit = true;
            }
        }

        Assert.True(hit, name + " fireball misses (over, under or beside) the other dragon");
    }

    [Fact]
    public void Ironhowl_stops_clear_of_Oakenshield_and_his_claws_still_reach_him()
    {
        var range = LockedBattleRoster.Ironhowl.AttackRangeMetres;
        // body (plus the 0.3 m reach slide of his smash and combo) stays off Oakenshield
        Assert.True(range - IronhowlFront - 0.3f - OakenshieldFront > 0.3f, "Ironhowl walks into Oakenshield");
        // claw lunge reaches Oakenshield's box front
        Assert.True(range - IronhowlClaw <= LockedBattleHits.Oakenshield.Box.Front);
        Assert.True(range < LockedBattleRoster.Oakenshield.AttackRangeMetres);
    }

    [Fact]
    public void Battle_code_scales_the_body_its_boxes_impacts_and_shadow_but_not_the_clips()
    {
        var root = ArcherExtraAttackTests.FindRepoRoot();
        string Unity(string file) => File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", file));
        var demo = Unity("LockedBattleDemo.cs");
        Assert.Contains("f.Scale *= f.BattleScale;", demo, StringComparison.Ordinal);
        Assert.Contains("return p / defender.BattleScale;", demo, StringComparison.Ordinal);
        Assert.Contains("local *= defender.BattleScale;", demo, StringComparison.Ordinal);
        Assert.Contains("attacker.Edge, attacker.BattleScale);", demo, StringComparison.Ordinal);
        Assert.Contains("LockedBattleRoster.BattleWalkSpeedOf(f.Data)", demo, StringComparison.Ordinal);
        Assert.Contains("Matrix4x4.TRS(captured.Host.localPosition, Quaternion.identity, Vector3.one * captured.Scale)", demo, StringComparison.Ordinal);
        Assert.Contains("f.BattleScale);", demo, StringComparison.Ordinal);
        Assert.Contains("f.Ground = new LockedBattleGrounding(host.transform);", demo, StringComparison.Ordinal);
        var impact = Unity("LockedBattleImpact.cs");
        Assert.Contains("main.startSize = new ParticleSystem.MinMaxCurve(sizeMin * _size, sizeMax * _size);", impact, StringComparison.Ordinal);
        Assert.Contains("light.range = range * _size;", impact, StringComparison.Ordinal);
        foreach (var actor in new[] { "EmberfangActor.cs", "AshwyrmActor.cs" })
        {
            Assert.DoesNotContain("BattleScale", Unity(actor), StringComparison.Ordinal);
        }
    }
}
