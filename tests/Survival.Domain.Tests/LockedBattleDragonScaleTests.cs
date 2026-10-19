using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek on #70 (2026-10-18): "make the two dragon looking characters much larger". Emberfang and Ashwyrm play at 2x their
/// design-height battle size; the lanes spread so neither overlaps a neighbour, Ironhowl stops short of Emberfang's snout,
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
    public void Scaled_dragons_clear_the_neighbouring_lanes()
    {
        var ember = EmberfangHalfWidth * LockedBattleRoster.EmberfangBattleScale;
        var ash = AshwyrmHalfSpan * LockedBattleRoster.AshwyrmBattleScale;
        const float gap = 0.25f;
        Assert.True(LockedBattleRoster.LaneX(1) - LockedBattleRoster.LaneX(0) >= ember + StormcrestHalfSpan + gap);
        Assert.True(LockedBattleRoster.LaneX(0) - LockedBattleRoster.LaneX(2) >= ember + LyraHalfWidth + gap);
        Assert.True(LockedBattleRoster.LaneX(3) - LockedBattleRoster.LaneX(1) >= ash + StormcrestHalfSpan + gap);
        Assert.True(LockedBattleRoster.LaneX(3) - LockedBattleRoster.LaneX(1) >= ash + NightfangHalfWidth + gap);
        Assert.True(LockedBattleRoster.LaneX(5) - LockedBattleRoster.LaneX(3) >= ash + ArcherHalfWidth + gap);
        Assert.True(LockedBattleRoster.LaneX(2) - LockedBattleRoster.LaneX(4) >= 1.2f);
    }

    [Fact]
    public void Ironhowl_stops_clear_of_the_big_snout_and_his_claws_still_reach_it()
    {
        var snout = EmberfangSnout * LockedBattleRoster.EmberfangBattleScale;
        var range = LockedBattleRoster.Ironhowl.AttackRangeMetres;
        var front = LockedBattleHits.Ironhowl.Box.Front;
        // body (plus the 0.3 m reach slide of his smash and combo) stays off the snout in the breath lunge
        Assert.True(range - front - 0.3f > snout, "Ironhowl walks into Emberfang");
        // claw lunge (about 1.05 m ahead of his origin) reaches the scaled body box front
        Assert.True(range - 1.05f <= LockedBattleHits.Emberfang.Box.Front * LockedBattleRoster.EmberfangBattleScale);
        Assert.True(range < LockedBattleRoster.Emberfang.AttackRangeMetres);
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
