using System;
using System.Collections.Generic;
using System.Linq;
using Survival.Domain.Campaign;
using Survival.Domain.Roster;
using Xunit;

namespace Survival.Domain.Tests;

public sealed class BattleArenaTests
{
    private static float Half(LockedBattleFighter f) =>
        BattleSpacing.HalfDepthOf(f.Name, f.DesignHeightMetres, LockedBattleRoster.BattleScaleOf(f));

    [Fact]
    public void Starts_put_each_side_in_one_line_with_no_bodies_overlapping()
    {
        var all = LockedBattleRoster.All.ToArray();
        var widths = all.Take(5).Select(f => BattleHpBarLayout.HalfWidthOf(f.Name, f.DesignHeightMetres, LockedBattleRoster.BattleScaleOf(f))).ToArray();
        var heroes = BattleArena.Starts(widths, LockedBattleRoster.StartHalfGapMetres, true);
        var foes = BattleArena.Starts(widths, LockedBattleRoster.StartHalfGapMetres, false);
        Assert.All(heroes, p => Assert.True(p.Z < 0f));
        Assert.All(foes, p => Assert.True(p.Z > 0f));
        Assert.Equal(0f, (heroes[0].X - widths[0]) + (heroes[^1].X + widths[^1]), 1);
        for (var i = 1; i < heroes.Length; i++)
        {
            Assert.True(heroes[i].X - heroes[i - 1].X >= widths[i] + widths[i - 1], "neighbours' bodies clear");
        }
    }

    [Fact]
    public void Separate_leaves_no_pair_of_bodies_overlapping_even_when_all_crowd_one_spot()
    {
        var bodies = LockedBattleRoster.All.Take(6).Select(Half).ToArray();
        var rnd = new Random(3);
        var pos = new List<(float X, float Z)>();
        for (var i = 0; i < bodies.Length; i++)
        {
            pos.Add(((float)rnd.NextDouble() * 0.3f, (float)rnd.NextDouble() * 0.3f));
        }

        pos.Add(pos[0]);
        var halves = bodies.Append(bodies[0]).ToArray();
        for (var frame = 0; frame < 40; frame++)
        {
            BattleArena.Separate(pos, halves);
        }

        for (var i = 0; i < pos.Count; i++)
        {
            for (var j = i + 1; j < pos.Count; j++)
            {
                var d = MathF.Sqrt(MathF.Pow(pos[i].X - pos[j].X, 2) + MathF.Pow(pos[i].Z - pos[j].Z, 2));
                Assert.True(BattleSpacing.Overlap(d, halves[i], halves[j]) < 0.01f, $"{i} and {j} overlap at {d:0.00} m");
            }
        }
    }

    [Fact]
    public void Separate_moves_only_the_free_body_when_one_is_pinned()
    {
        var pos = new List<(float X, float Z)> { (0f, 0f), (0.1f, 0f) };
        BattleArena.Separate(pos, new[] { 0.4f, 0.4f }, new[] { true, false });
        Assert.Equal((0f, 0f), pos[0]);
        Assert.True(pos[1].X >= BattleSpacing.MinDistance(0.4f, 0.4f) - 0.01f);
    }

    [Theory]
    [InlineData(7f)]
    [InlineData(10f)]
    [InlineData(14f)]
    public void Props_stay_outside_the_fighting_area_and_apart(float radius)
    {
        var props = BattleArena.Props(radius);
        Assert.True(props.Count > 60, "a full ring of ruins, rocks and trees");
        Assert.Contains(props, p => p.Kind == BattleArena.PropKind.Pillar);
        Assert.Contains(props, p => p.Kind == BattleArena.PropKind.Tree);
        foreach (var p in props)
        {
            Assert.True(p.Distance - p.Radius >= radius + BattleArena.PropClearanceMetres - 1e-3f, $"{p.Kind} at {p.Distance:0.0} m reaches into the arena");
        }

        for (var i = 0; i < props.Count; i++)
        {
            for (var j = i + 1; j < props.Count; j++)
            {
                var d = MathF.Sqrt(MathF.Pow(props[i].X - props[j].X, 2) + MathF.Pow(props[i].Z - props[j].Z, 2));
                Assert.True(d >= props[i].Radius + props[j].Radius, "props do not intersect");
            }
        }

        Assert.Equal(props.Count, BattleArena.Props(radius).Count);
    }

    [Fact]
    public void The_arena_is_flat_and_the_ground_rolls_beyond_it()
    {
        const float r = 9f;
        for (var a = 0; a < 360; a += 20)
        {
            var (s, c) = MathF.SinCos(a * MathF.PI / 180f);
            Assert.Equal(0f, BattleArena.Height(c * r, s * r, r));
        }

        var outside = Enumerable.Range(0, 36).Select(a => BattleArena.Height(MathF.Cos(a / 6f) * (r + 12f), MathF.Sin(a / 6f) * (r + 12f), r)).ToArray();
        Assert.True(outside.Max() > 0.3f && outside.Max() <= BattleArena.HillMetres * 1.4f);
        Assert.True(outside.Max() - outside.Min() > 0.1f, "varied, not a flat rim");
        Assert.True(BattleArena.Dirt(0f, 0f, r) > 0.6f, "trampled centre");
        Assert.True(Enumerable.Range(0, 36).Count(a => BattleArena.Stone(MathF.Cos(a / 6f) * r, MathF.Sin(a / 6f) * r, r) > 0.5f) >= 6, "flagstones on the arena edge");
    }

    [Fact]
    public void Camera_orbit_is_slow_and_at_30_degrees()
    {
        Assert.Equal(30f, BattleArena.OrbitPitchDegrees);
        Assert.InRange(BattleArena.OrbitSeconds, 40f, 60f);
        Assert.Equal(360f / BattleArena.OrbitSeconds, BattleArena.OrbitDegreesPerSecond, 4);
    }
}

public sealed class OpenArenaTargetingTests
{
    [Fact]
    public void Foes_spread_over_the_enemies_instead_of_piling_on_one()
    {
        var s = Camp.Session(new Survival.Domain.Clock.ManualClock());
        var combat = new BattleCombat(s.Setup(s.Catalog.Stage("stage.2")!).Build());
        var heroes = combat.Units.Where(u => u.Side == CampaignSide.Hero).ToList();
        var foes = combat.Units.Where(u => u.Side != CampaignSide.Hero).ToList();
        if (heroes.Count < 2 || foes.Count < 2)
        {
            return;
        }

        // Every foe the same distance away: the first pick takes one, the next hero goes for another.
        (float, float) Pos(CombatUnit u) => u.Side == CampaignSide.Hero ? (0f, 0f) : (foes.IndexOf(u) * 0.2f, 4f);
        var picked = new Dictionary<int, int>();
        var first = combat.NearestFoe(heroes[0], Pos, u => picked.TryGetValue(u.Uid, out var c) ? c : 0)!;
        picked[first.Uid] = 1;
        var second = combat.NearestFoe(heroes[1], Pos, u => picked.TryGetValue(u.Uid, out var c) ? c : 0)!;
        Assert.NotEqual(first.Uid, second.Uid);
    }
}
