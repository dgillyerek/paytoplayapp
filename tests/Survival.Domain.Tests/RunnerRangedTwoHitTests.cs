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
                var foe = new RunnerFoe(1, c.Unit(u), 0f, 0f, false, level.FoeHpMul, level.FoeDmgMul);
                if (foe.Damage >= RunnerRules.FollowerHp || foe.Damage * 2 < RunnerRules.FollowerHp)
                {
                    off.Add($"L{level.Number} {u}: {foe.Damage}");
                }
            }
        }

        Assert.True(off.Count == 0, string.Join("\n", off));
    }
}
