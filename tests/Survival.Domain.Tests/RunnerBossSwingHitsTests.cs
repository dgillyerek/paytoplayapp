using Survival.Domain.Roster;
using Survival.Domain.Runner;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek 2026-11-03: "on iron howls double move only one causes damage. if he swings his arms twice both should cause damage
/// if they strike the hero". Each slam plays one move of his rotation; a multi-swing move (the claw combo) lands every swing
/// on its own contact frame, each for its share of the slam, and only on whoever is still in reach at that frame.
/// </summary>
public sealed class RunnerBossSwingHitsTests
{
    private const float Dt = 1f / 30f;

    private static RunnerCatalog Catalog() => RunnerCatalog.Parse(@"{
  ""units"": {
    ""hero"": { ""roster"": ""Lyra"", ""hp"": 50000, ""damage"": 1, ""cycle"": 0.5, ""range"": 10, ""ranged"": true, ""shot_speed"": 20 },
    ""boss"": { ""roster"": ""Ironhowl"", ""hp"": 5000000, ""damage"": 40, ""cycle"": 2.4, ""range"": 1.8, ""radius"": 0.67, ""throws"": false }
  },
  ""levels"": [ { ""id"": ""t"", ""hero"": ""hero"", ""length"": 80, ""speed"": 8, ""half_width"": 4, ""max_followers"": 9,
    ""rewards"": { ""win_gold"": 100, ""gold_per_kill"": 5, ""gold_per_box"": 10, ""shards"": 3, ""shard_hero"": ""hero.lyra"" },
    ""rows"": [ { ""at"": 40, ""kind"": ""boss"", ""x"": 0, ""unit"": ""boss"" } ] } ] }");

    private static int ComboIndex()
    {
        var f = LockedBattleRoster.Ironhowl;
        for (var i = 0; i < LockedBattleRotation.Count(f); i++)
        {
            if (LockedBattleRotation.HitCount(f, LockedBattleHits.ProfileOf(f.Name), i) > 1)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Plays to the first claw-combo slam and returns its swing hits (time, struck the hero, hero HP lost by it).</summary>
    private static List<(float At, bool Struck, int Lost)> ComboSwings(bool stepOutAfterFirst)
    {
        var c = Catalog();
        var run = new RunnerRun(c, c.First);
        var combo = ComboIndex();
        var hits = new List<(float, bool, int)>();
        var inCombo = false;
        var hasStruck = false;
        while (!run.Over && run.Seconds < 120f)
        {
            run.SteerTo(stepOutAfterFirst && hasStruck ? 3.5f : 0f);
            var hp = run.Hero.Hp;
            foreach (var e in run.Step(Dt))
            {
                if (e.Kind == RunnerEventKind.BossSlam)
                {
                    inCombo = run.Foes.First(f => f.Boss).SlamSwing == combo;
                }

                if (inCombo && e.Kind == RunnerEventKind.BossSwingHit)
                {
                    hits.Add((run.Seconds, e.Big, hp - run.Hero.Hp));
                    hasStruck = true;
                }
            }

            if (inCombo && hits.Count > 0 && !run.Foes.First(f => f.Boss).SlamSwinging)
            {
                return hits;
            }
        }

        return hits;
    }

    [Fact]
    public void Ironhowl_has_a_multi_swing_move_and_its_swings_split_one_slam()
    {
        Assert.True(ComboIndex() >= 0);
        Assert.Equal(40, Enumerable.Range(0, 4).Sum(i => RunnerRun.SlamSwingDamage(40, 4, i)));
        Assert.Equal(41, Enumerable.Range(0, 2).Sum(i => RunnerRun.SlamSwingDamage(41, 2, i)));
    }

    [Fact]
    public void Every_swing_strikes_a_standing_hero_each_on_its_own_frame()
    {
        var hits = ComboSwings(false);
        var f = LockedBattleRoster.Ironhowl;
        var n = LockedBattleRotation.HitCount(f, LockedBattleHits.ProfileOf(f.Name), ComboIndex());
        Assert.Equal(n, hits.Count);
        Assert.All(hits, h => Assert.True(h.Struck));
        Assert.All(hits, h => Assert.True(h.Lost > 0, "hp lost " + h.Lost));
        Assert.Equal(40, hits.Sum(h => h.Lost));
        for (var i = 1; i < hits.Count; i++)
        {
            var gap = (LockedBattleRotation.HitFrame(f, LockedBattleHits.ProfileOf(f.Name), ComboIndex(), i)
                - LockedBattleRotation.HitFrame(f, LockedBattleHits.ProfileOf(f.Name), ComboIndex(), i - 1)) / 30f;
            Assert.InRange(hits[i].At - hits[i - 1].At, gap - 0.05f, gap + 0.05f);
        }
    }

    [Fact]
    public void A_hero_who_steps_out_after_the_first_swing_takes_only_that_one()
    {
        var hits = ComboSwings(true);
        Assert.True(hits.Count >= 2);
        Assert.True(hits[0].Struck);
        Assert.True(hits[0].Lost > 0);
        Assert.All(hits.Skip(1), h => Assert.False(h.Struck));
        Assert.Equal(hits[0].Lost, hits.Sum(h => h.Lost));
    }
}
