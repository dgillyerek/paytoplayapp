using Survival.Domain.Runner;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>
/// progression "level_tweaks" (Derek 2026-10-27): a few levels re-roll their layout (the "layout" salt) where the seed's
/// arrangement came out far harder or easier than the curve, so no level needs more than 1.6x the one before and the walls
/// stay put. A re-roll moves the beats and changes their set pieces but keeps exactly what is on the road.
/// </summary>
public sealed class RunnerLevelTweakTests
{
    private static readonly int[] Rerolled = { 2, 3, 4, 5, 6, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 25, 26, 27, 28, 29, 30, 31, 33, 34, 35, 36, 37, 39, 41, 42, 43, 44, 45, 46, 48, 49, 50 };

    [Fact]
    public void The_rerolled_levels_keep_their_boxes_enemies_traps_and_hp_and_only_move_them()
    {
        var c = Runner.Catalog();
        var p = c.Progression!;
        Assert.Equal(Rerolled, p.Tweaks.Keys.OrderBy(k => k).ToArray());
        Assert.Equal(RunnerLevelTweak.None, p.Tweak(7));
        var plain = p with { Tweaks = new Dictionary<int, RunnerLevelTweak>() };
        foreach (var n in Rerolled)
        {
            var tw = p.Tweak(n);
            Assert.Equal(RunnerLevelTweak.None with { Layout = tw.Layout }, tw);
            Assert.NotEqual(0, tw.Layout);
            var tweaked = c.ByNumber(n);
            var seed = RunnerProgression.Generate(c.Levels[0], plain, n, c.Units.ContainsKey);
            Assert.Equal(seed.BoxCount, tweaked.BoxCount);
            Assert.Equal(seed.EnemyCount, tweaked.EnemyCount);
            Assert.Equal(seed.HazardCount, tweaked.HazardCount);
            Assert.Equal(seed.BarrierCount, tweaked.BarrierCount);
            // Box and enemy HP climb a little along the road, so a row that moves a few metres changes by a few HP.
            Assert.InRange(tweaked.BoxHpTotal, seed.BoxHpTotal * 0.98, seed.BoxHpTotal * 1.02);
            Assert.InRange(tweaked.FoeHpTotal(c), seed.FoeHpTotal(c) * 0.98, seed.FoeHpTotal(c) * 1.02);
            Assert.Equal(seed.FoeHpMul, tweaked.FoeHpMul, 4);
            Assert.Equal(seed.Length, tweaked.Length);
            Assert.NotEqual(seed.Rows.Select(r => (r.At, r.X)).ToList(), tweaked.Rows.Select(r => (r.At, r.X)).ToList());
        }
    }
}
