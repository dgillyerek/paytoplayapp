using Survival.Domain.Runner;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek 2026-11-03: "iron howl at the end of levels takes a while to start attack. Can we add something like he picks up
/// and throws rocks ... where the heros can dodge before they get close to his melee attacks". From the moment he is on the
/// road he lobs a boulder every 2 s at where the lead is heading; a ring marks the landing 1.1 s ahead. At his arena line he
/// stops throwing and goes back to the slam and quake.
/// </summary>
public sealed class RunnerIronhowlRockTests
{
    private const float Dt = 1f / 30f;

    private static RunnerCatalog Catalog(bool throws = true) => RunnerCatalog.Parse(@"{
  ""units"": {
    ""hero"": { ""roster"": ""Lyra"", ""hp"": 5000, ""damage"": 1, ""cycle"": 0.5, ""range"": 10, ""ranged"": true, ""shot_speed"": 20 },
    ""boss"": { ""roster"": ""Ironhowl"", ""hp"": 5000000, ""damage"": 40, ""cycle"": 2.4, ""range"": 1.8, ""radius"": 0.67, ""throws"": " + (throws ? "true" : "false") + @" }
  },
  ""levels"": [ { ""id"": ""t"", ""hero"": ""hero"", ""length"": 80, ""speed"": 8, ""half_width"": 4, ""max_followers"": 9,
    ""rewards"": { ""win_gold"": 100, ""gold_per_kill"": 5, ""gold_per_box"": 10, ""shards"": 3, ""shard_hero"": ""hero.lyra"" },
    ""rows"": [ { ""at"": 76, ""kind"": ""boss"", ""x"": 0, ""unit"": ""boss"" } ] } ] }");

    private sealed record Log(RunnerRun Run, List<(float At, RunnerEvent E)> Events);

    private static Log Play(RunnerCatalog c, Func<RunnerRun, float> steer, float seconds)
    {
        var run = new RunnerRun(c, c.First);
        var log = new List<(float, RunnerEvent)>();
        while (!run.Over && run.Seconds < seconds)
        {
            run.SteerTo(steer(run));
            foreach (var e in run.Step(Dt))
            {
                log.Add((run.Seconds, e));
            }
        }

        return new Log(run, log);
    }

    private static List<float> At(Log l, RunnerEventKind k) => l.Events.Where(x => x.E.Kind == k).Select(x => x.At).ToList();

    [Fact]
    public void He_starts_throwing_right_after_he_appears_every_two_seconds_with_a_one_second_warning()
    {
        var l = Play(Catalog(), _ => 0f, 12f);
        var spawn = l.Events.First(x => x.E.Kind == RunnerEventKind.FoeSpawned).At;
        var thrown = At(l, RunnerEventKind.BossRockThrown);
        var landed = At(l, RunnerEventKind.BossRockLanded);
        var engaged = At(l, RunnerEventKind.BossEngaged).First();

        Assert.True(thrown.Count >= 2, "throws " + thrown.Count);
        Assert.InRange(thrown[0] - spawn, 0.2f, 0.45f);
        for (var i = 1; i < thrown.Count; i++)
        {
            Assert.Equal(RunnerRules.BossRockEvery, thrown[i] - thrown[i - 1], 1);
        }

        for (var i = 0; i < landed.Count; i++)
        {
            Assert.Equal(RunnerRules.BossRockTelegraph, landed[i] - thrown[i], 1);
            Assert.InRange(landed[i] - thrown[i], 0.8f, 1.2f);
        }

        // The first boulder is on its way well before his reach: that was the dead time Derek saw.
        Assert.True(engaged - thrown[0] > 2f, $"first throw {thrown[0]:0.0}s, engaged {engaged:0.0}s");
        Assert.InRange(RunnerRules.BossRockRelease, 0.3f, RunnerRules.BossRockTelegraph - 0.3f);
    }

    [Fact]
    public void Once_the_squad_is_in_his_reach_he_stops_throwing_and_goes_back_to_his_melee()
    {
        var l = Play(Catalog(), _ => 0f, 20f);
        var engaged = At(l, RunnerEventKind.BossEngaged).First();
        var thrown = At(l, RunnerEventKind.BossRockThrown);
        Assert.All(thrown, t => Assert.True(t <= engaged + 1e-3f, $"threw at {t:0.00} after engaging at {engaged:0.00}"));
        Assert.Equal(thrown.Count, At(l, RunnerEventKind.BossRockLanded).Count);
        Assert.Contains(l.Events, x => x.At >= engaged && x.E.Kind is RunnerEventKind.BossWindup or RunnerEventKind.BossQuakeWindup);
    }

    [Fact]
    public void A_boulder_lands_on_a_lead_who_stands_still_and_misses_one_who_steps_out()
    {
        var still = Play(Catalog(), _ => 0f, 9f);
        var stillHits = still.Events.Count(x => x.E.Kind == RunnerEventKind.HeroHit && x.E.Other != 0 && still.Events.Any(y => y.At == x.At && y.E.Kind == RunnerEventKind.BossRockLanded));
        Assert.True(stillHits >= 2, "hits on a still lead " + stillHits);
        Assert.Equal(40 * RunnerRules.BossRockDamage, still.Events.First(x => x.E.Kind == RunnerEventKind.HeroHit).E.Amount, 0);

        var dodger = Play(Catalog(), r => RunnerBossQuakeTests.RockDodge(r) ?? r.Hero.X, 9f);
        var dodgeHits = dodger.Events.Count(x => x.E.Kind == RunnerEventKind.HeroHit);
        Assert.True(At(dodger, RunnerEventKind.BossRockLanded).Count >= 2);
        Assert.Equal(0, dodgeHits);
    }

    [Fact]
    public void The_aim_leads_the_squad_but_never_past_his_arena_line()
    {
        var (x, z) = RunnerRun.RockAim(1f, 10f, 8f, 100f, 4f);
        Assert.Equal(1f, x);
        Assert.Equal(10f + (8f * RunnerRules.BossRockTelegraph), z, 3);
        Assert.Equal(12f, RunnerRun.RockAim(0f, 10f, 8f, 12f, 4f).Z, 3);
        Assert.True(RunnerRun.RockCatches(0f, 0f, 1.5f, 0f, 0.45f));
        Assert.False(RunnerRun.RockCatches(0f, 0f, 2.2f, 0f, 0.45f));
    }

    [Fact]
    public void A_boss_without_the_flag_does_not_throw()
    {
        var l = Play(Catalog(false), _ => 0f, 10f);
        Assert.Empty(At(l, RunnerEventKind.BossRockThrown));
    }

    [Fact]
    public void Ironhowl_throws_in_the_real_catalog()
    {
        Assert.True(Runner.Catalog().Unit("ironhowl").Throws);
    }
}
