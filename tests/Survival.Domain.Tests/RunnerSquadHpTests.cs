using System.Collections.Generic;
using System.Linq;
using Survival.Domain.Runner;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>Derek 2026-10-28: "new rows with the cumulative health of the secondary units split up by unit type".</summary>
public sealed class RunnerSquadHpTests
{
    [Fact]
    public void Sums_count_hp_and_max_per_type_in_join_order()
    {
        var into = new List<RunnerSquadHp.TypeHp>();
        RunnerSquadHp.Accumulate(into, "Rowan", 25, 25);
        RunnerSquadHp.Accumulate(into, "Sir Aldric", 10, 25);
        RunnerSquadHp.Accumulate(into, "Rowan", 5, 25);
        Assert.Equal(new[] { "Rowan", "Sir Aldric" }, into.Select(t => t.Roster));
        Assert.Equal((2, 30, 50), (into[0].Count, into[0].Hp, into[0].MaxHp));
        Assert.Equal(0.6f, into[0].Fraction, 3);
        Assert.Equal((1, 10, 25), (into[1].Count, into[1].Hp, into[1].MaxHp));
    }

    [Fact]
    public void A_real_squad_groups_its_followers_and_leaves_the_hero_out()
    {
        var run = Runner.Run(Runner.Tiny(
            @"{ ""at"": 4, ""kind"": ""box"", ""hp"": 1, ""reward"": ""followers"", ""unit"": ""archer"", ""count"": 3 },
              { ""at"": 8, ""kind"": ""box"", ""hp"": 1, ""reward"": ""followers"", ""unit"": ""knight"", ""count"": 2 }", length: 30f, allowMelee: true));
        Runner.Play(run, null, 8f);
        var into = new List<RunnerSquadHp.TypeHp>();
        RunnerSquadHp.ByType(run.Squad, into);
        var followers = run.Squad.Where(m => !m.IsHero).ToList();
        Assert.Equal(followers.Select(m => m.Unit.Roster).Distinct(), into.Select(t => t.Roster));
        Assert.Equal(followers.Count, into.Sum(t => t.Count));
        Assert.Equal(followers.Sum(m => m.Hp), into.Sum(t => t.Hp));
        Assert.Equal(followers.Sum(m => m.MaxHp), into.Sum(t => t.MaxHp));
        Assert.DoesNotContain(into, t => t.Roster == run.Hero.Unit.Roster && t.Count == 1 && followers.All(f => f.Unit.Roster != t.Roster));
        Assert.True(into.Count >= 1);
    }
}
