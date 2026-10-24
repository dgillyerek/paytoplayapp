using System.Collections.Generic;
using System.Linq;
using Survival.Domain.Runner;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>Derek 2026-10-23: "the secondary heros should have more health but not by much. maybe like 25".</summary>
public sealed class RunnerFollowerHpTests
{
    [Fact]
    public void Followers_have_25_hp_and_the_lead_keeps_his_units_hp()
    {
        Assert.Equal(25, RunnerRules.FollowerHp);
        var run = Runner.Run(Runner.Tiny(@"{ ""at"": 4, ""kind"": ""box"", ""hp"": 1, ""reward"": ""followers"", ""unit"": ""archer"", ""count"": 3 }", length: 30f));
        Runner.Play(run, null, 6f);
        Assert.Equal(3, run.Followers);
        Assert.Equal(100, run.Hero.MaxHp);
        Assert.Equal(100, run.Hero.Hp);
        Assert.All(run.Squad.Where(m => !m.IsHero), m =>
        {
            Assert.Equal(25, m.MaxHp);
            Assert.Equal(25, m.Hp);
            Assert.Equal(1f, m.HpFraction);
        });

        // The shipped catalogue too: a follower is 25 whatever its unit's own HP, the lead is the unit.
        var c = Runner.Catalog();
        var shipped = new RunnerRun(c, c.First);
        Assert.Equal(c.Unit(c.First.Hero).Hp, shipped.Hero.MaxHp);
    }

    [Fact]
    public void A_follower_lives_through_a_small_hit_and_falls_to_a_big_one()
    {
        // An enemy the squad cannot kill, swinging for 10: a follower takes two hits and falls on the third.
        static RunnerRun Road(int damage) => Runner.Run(RunnerCatalog.Parse(@"{
  ""units"": {
    ""hero"": { ""roster"": ""Lyra"", ""hp"": 100, ""damage"": 1, ""cycle"": 0.5, ""range"": 10, ""ranged"": true, ""shot_speed"": 20 },
    ""archer"": { ""roster"": ""Rowan"", ""hp"": 1, ""damage"": 1, ""cycle"": 0.5, ""range"": 10, ""ranged"": true, ""shot_speed"": 20 },
    ""pest"": { ""roster"": ""Blightroot"", ""hp"": 100000, ""damage"": " + damage + @", ""cycle"": 1.0, ""range"": 1.5, ""speed"": 2.0, ""radius"": 0.6 }
  },
  ""levels"": [ { ""id"": ""t"", ""hero"": ""hero"", ""length"": 40, ""speed"": 2, ""half_width"": 4, ""max_followers"": 6,
    ""rewards"": { ""win_gold"": 100, ""gold_per_kill"": 5, ""gold_per_box"": 10, ""shards"": 3, ""shard_hero"": ""hero.lyra"" },
    ""rows"": [ { ""at"": 4, ""kind"": ""box"", ""hp"": 1, ""reward"": ""followers"", ""unit"": ""archer"", ""count"": 4 },
      { ""at"": 20, ""kind"": ""enemy"", ""unit"": ""pest"", ""x"": 0 } ] } ] }", false));

        var run = Road(10);
        var events = Runner.Play(run, _ => 0f, 30f);
        var hits = events.Where(e => e.Kind == RunnerEventKind.FollowerHit).ToList();
        Assert.NotEmpty(hits);
        var lost = events.Where(e => e.Kind == RunnerEventKind.FollowerLost).Select(e => e.Id).ToList();
        foreach (var id in lost)
        {
            // Nobody falls to his first small hit.
            Assert.Contains(hits, h => h.Id == id);
        }

        // The lead is not hurt while a follower is left to take the hits.
        Assert.True(run.Hero.Hp == run.Hero.MaxHp || run.Followers == 0, "hero " + run.Hero.Hp);

        // A 40 swing (over the 25) still drops a follower in one.
        var big = Road(40);
        var bigEvents = Runner.Play(big, _ => 0f, 30f);
        Assert.Contains(bigEvents, e => e.Kind == RunnerEventKind.FollowerLost);
        Assert.DoesNotContain(bigEvents, e => e.Kind == RunnerEventKind.FollowerHit && e.Amount >= RunnerRules.FollowerHp);
    }
}
