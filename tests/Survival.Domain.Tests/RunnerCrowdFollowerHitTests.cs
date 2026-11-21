using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek 2026-11-20: more enemies, each weaker. The crowd's damage cut also reaches the followers: a crowd enemy's hit on
/// a follower is the no-crowd hit capped at a follower's HP times the cut, so the 1-hit melee and 2-hit archer kills stretch
/// with it. With no crowd nothing changes.
/// </summary>
public sealed class RunnerCrowdFollowerHitTests
{
    [Fact]
    public void Without_a_cut_the_hit_is_unchanged()
    {
        foreach (var hit in new[] { 1, 13, 24, 25, 60, 400 })
        {
            Assert.Equal(hit, RunnerRules.CrowdFollowerHit(hit, 1f));
        }
    }

    [Fact]
    public void A_cut_stretches_one_and_two_hit_kills()
    {
        // A melee hit that one-shots a follower (60 >= 25) is capped at 25, then 0.6 of it: 15, a 2-hit kill.
        Assert.Equal(15, RunnerRules.CrowdFollowerHit(60, 0.6f));
        // The archer's 2-hit range [13, 24] becomes [8, 14]: 2 to 4 hits.
        Assert.Equal(8, RunnerRules.CrowdFollowerHit(13, 0.6f));
        Assert.Equal(14, RunnerRules.CrowdFollowerHit(24, 0.6f));
        Assert.Equal(1, RunnerRules.CrowdFollowerHit(1, 0.1f));
    }

    [Fact]
    public void A_foe_hits_followers_by_the_cut_but_the_hero_and_bosses_as_before()
    {
        var c = Runner.Catalog();
        var melee = c.Unit("nightfang");
        var archer = c.Unit("bonequill");
        foreach (var u in new[] { melee, archer })
        {
            var plain = new RunnerFoe(1, u, 0f, 0f, false, 1f, 3f);
            Assert.Equal(plain.Damage, plain.FollowerDamage);
            var crowd = new RunnerFoe(2, u, 0f, 0f, false, 1f, 3f * 0.6f, 0.6f);
            Assert.Equal(RunnerRules.CrowdFollowerHit(plain.Damage, 0.6f), crowd.FollowerDamage);
            var boss = new RunnerFoe(3, u, 0f, 0f, true, 1f, 3f * 0.6f, 0.6f);
            Assert.Equal(boss.Damage, boss.FollowerDamage);
        }
    }

    [Fact]
    public void Levels_without_the_crowd_block_have_no_cut()
    {
        var path = Path.Combine(TestPaths.CatalogDir, "runner_levels.json");
        var json = System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(path), @", ""crowd"": \{[^}]*\}", "");
        var c = RunnerCatalog.Parse(json);
        Assert.All(c.Levels, l => Assert.Equal(1f, l.CrowdCut));
        Assert.All(c.Levels, l => Assert.Equal(0, l.ExtraEnemies));
    }
}
