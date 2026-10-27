using Survival.Domain.Runner;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>
/// progression "level_tweaks" (Derek 2026-10-27: keep the seed, smooth L19, L22 and L26 below a 1.6x jump): a tweak trims a
/// level's waves or enemy HP but leaves where every row stands and what it is; levels without one are generated as before.
/// </summary>
public sealed class RunnerLevelTweakTests
{
    [Fact]
    public void Only_L19_L22_and_L26_are_tweaked_and_the_layout_stays_the_seeds()
    {
        var c = Runner.Catalog();
        var p = c.Progression!;
        Assert.Equal(new[] { 19, 22, 26 }, p.Tweaks.Keys.OrderBy(k => k).ToArray());
        Assert.Equal(RunnerLevelTweak.None, p.Tweak(18));
        var plain = p with { Tweaks = new Dictionary<int, RunnerLevelTweak>() };
        foreach (var n in new[] { 18, 19, 22, 26 })
        {
            var tweaked = c.ByNumber(n);
            var seed = RunnerProgression.Generate(c.Levels[0], plain, n, c.Units.ContainsKey);
            // Boxes, traps, barriers and the boss stand where the seed put them; enemy groups keep their spot and unit and
            // only lose back ranks.
            static bool Enemy(RunnerRowDef r) => r.Kind == RunnerRowKind.Enemy;
            Assert.Equal(seed.Rows.Where(r => !Enemy(r)).ToList(), tweaked.Rows.Where(r => !Enemy(r)).ToList());
            var spots = seed.Rows.Where(Enemy).Select(r => (r.At, r.Unit)).ToHashSet();
            Assert.All(tweaked.Rows.Where(Enemy), r => Assert.Contains((r.At, r.Unit), spots));

            var tw = p.Tweak(n);
            if (tw.Wave < 1f)
            {
                Assert.True(tweaked.EnemyCount < seed.EnemyCount, $"L{n} enemies {seed.EnemyCount} -> {tweaked.EnemyCount}");
            }
            else
            {
                Assert.Equal(seed.EnemyCount, tweaked.EnemyCount);
            }

            Assert.Equal(seed.FoeHpMul * tw.FoeHp, tweaked.FoeHpMul, 4);
        }
    }
}
