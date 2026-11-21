using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>Derek 2026-10-29: "make it so ranged attacks kill secondary units in 2 hits. they can be dodged".</summary>
public sealed class RunnerRangedTwoHitTests
{
    [Fact]
    public void A_ranged_hit_kills_a_follower_in_exactly_two_hits_on_every_level()
    {
        var c = Runner.Catalog();
        var off = new List<string>();
        foreach (var level in c.Levels)
        {
            foreach (var u in new[] { "bonequill", "vespera" })
            {
                // Without the crowd the hit itself is the 2-hit kill; with it (Derek 2026-11-20, more and lighter enemies) the
                // follower takes the crowd's share of that 2-hit hit (RunnerCrowdFollowerHitTests), so 2 to 1/cut x 2 hits.
                var plain = new RunnerFoe(1, c.Unit(u), 0f, 0f, false, level.FoeHpMul, level.FoeDmgMul / level.CrowdCut);
                if (plain.Damage >= RunnerRules.FollowerHp || plain.Damage * 2 < RunnerRules.FollowerHp)
                {
                    off.Add($"L{level.Number} {u}: {plain.Damage}");
                }

                var foe = new RunnerFoe(1, c.Unit(u), 0f, 0f, false, level.FoeHpMul, level.FoeDmgMul, level.CrowdCut);
                if (foe.FollowerDamage != RunnerRules.CrowdFollowerHit(plain.Damage, level.CrowdCut))
                {
                    off.Add($"L{level.Number} {u}: follower hit {foe.FollowerDamage}");
                }
            }
        }

        Assert.True(off.Count == 0, string.Join("\n", off));
    }
}
