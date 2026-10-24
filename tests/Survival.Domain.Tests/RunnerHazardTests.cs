using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>Lane traps, the range / heal crates, runner enemy scale, longer squad range and the denser levels.</summary>
public sealed class RunnerHazardTests
{
    [Fact]
    public void Spikes_rattle_before_they_rise_and_catch_the_hero_once_and_steering_round_them_is_safe()
    {
        const string rows = @"{ ""at"": 6, ""kind"": ""hazard"", ""hazard"": ""spikes"", ""width"": 2, ""damage"": 25 }";
        var run = Runner.Run(Runner.Tiny(rows));
        var states = new List<RunnerHazardState>();
        var hits = new List<RunnerEvent>();
        while (!run.Over && run.Seconds < 6f)
        {
            foreach (var e in run.Step(1f / 30f))
            {
                if (e.Kind == RunnerEventKind.HazardHit)
                {
                    hits.Add(e);
                    Assert.True(run.Hazards[0].Hurts);
                }
            }

            if (run.Hazards.Count > 0)
            {
                states.Add(run.Hazards[0].State);
            }
        }

        // The rattle (warning) always comes before the spikes are up.
        var firstUp = states.IndexOf(RunnerHazardState.Active);
        Assert.True(firstUp > 0);
        Assert.Equal(RunnerHazardState.Warning, states[firstUp - 1]);
        Assert.True(states.Take(firstUp).Count(st => st == RunnerHazardState.Warning) >= (int)(RunnerRules.SpikeWarn * 30f) - 1);

        var hit = Assert.Single(hits);
        Assert.Equal("spikes", hit.Unit);
        Assert.Equal(25f, hit.Amount);
        Assert.Equal(75, run.Hero.Hp);
        Assert.Equal(1, run.HazardHits);

        // The same road, steering the hero to the far side: untouched.
        var dodge = Runner.Run(Runner.Tiny(rows));
        var evs = Runner.Play(dodge, _ => 3f, 6f);
        Assert.DoesNotContain(evs, e => e.Kind == RunnerEventKind.HazardHit);
        Assert.Equal(100, dodge.Hero.Hp);
    }

    [Fact]
    public void A_trap_knocks_out_a_follower_it_catches_but_not_the_whole_squad()
    {
        var run = Runner.Run(Runner.Tiny(@"{ ""at"": 4, ""kind"": ""box"", ""hp"": 1, ""reward"": ""followers"", ""unit"": ""archer"", ""count"": 5 },
            { ""at"": 16, ""kind"": ""hazard"", ""hazard"": ""fire"", ""width"": 7.6, ""damage"": 10 }"));
        var fire = Assert.Single(run.Hazards).Id;
        var evs = Runner.Play(run, null, 16f);
        var lost = evs.Where(e => e.Kind == RunnerEventKind.FollowerLost && e.Other == fire).ToList();
        Assert.NotEmpty(lost);
        Assert.All(evs.Where(e => e.Kind == RunnerEventKind.HazardHit && !e.Big), e => Assert.Equal(0f, e.Amount));
        Assert.True(run.Followers >= 1, "a fire patch burns one at a time; the rest walk through: left " + run.Followers);
        Assert.Equal(5, run.Followers + lost.Count + evs.Count(e => e.Kind == RunnerEventKind.FollowerLost && e.Other != fire));
    }

    [Fact]
    public void A_log_wobbles_when_the_squad_comes_near_then_rolls_down_the_lane_at_it()
    {
        var run = Runner.Run(Runner.Tiny(@"{ ""at"": 30, ""kind"": ""hazard"", ""hazard"": ""log"", ""width"": 2.2, ""damage"": 30 }", length: 60f));
        float warnAt = -1f, rollAt = -1f, startZ = 0f;
        var hitHero = false;
        while (!run.Over && run.Seconds < 14f)
        {
            foreach (var e in run.Step(1f / 30f))
            {
                hitHero |= e.Kind == RunnerEventKind.HazardHit && e.Big;
            }

            if (run.Hazards.Count == 0)
            {
                continue;
            }

            var h = run.Hazards[0];
            startZ = h.StartZ;
            if (warnAt < 0f && h.State == RunnerHazardState.Warning)
            {
                warnAt = run.Seconds;
                Assert.InRange(h.Z - run.Hero.Z, RunnerRules.LogTrigger - 0.2f, RunnerRules.LogTrigger + 0.01f);
                Assert.Equal(h.StartZ, h.Z);
            }

            if (rollAt < 0f && h.State == RunnerHazardState.Active)
            {
                rollAt = run.Seconds;
            }
        }

        Assert.True(warnAt > 0f && rollAt > warnAt);
        Assert.InRange(rollAt - warnAt, RunnerRules.LogWarn - 0.05f, RunnerRules.LogWarn + 0.1f);
        Assert.True(hitHero, "the log rolled into the hero walking straight at it");
        Assert.Equal(70, run.Hero.Hp);
        Assert.True(startZ == 30f);
    }

    [Fact]
    public void Range_crates_lengthen_every_shot_and_heal_crates_mend_the_hero_up_to_full()
    {
        var run = Runner.Run(Runner.Tiny(@"{ ""at"": 6, ""kind"": ""box"", ""hp"": 1, ""reward"": ""range"", ""amount"": 0.5 },
            { ""at"": 30, ""kind"": ""box"", ""hp"": 99999, ""reward"": ""weapon"" }", length: 60f));
        Assert.Equal(10f, run.ReachOf(run.Hero));
        var evs = new List<RunnerEvent>();
        while (!run.Over && run.Seconds < 10f)
        {
            foreach (var e in run.Step(1f / 30f))
            {
                evs.Add(e);
                if (e.Kind == RunnerEventKind.Fired && run.RangeMul > 1f)
                {
                    Assert.InRange(e.Z - run.Hero.Z, 15f - 0.2f, 15f + 0.01f);
                }
            }
        }

        Assert.Contains(evs, e => e.Kind == RunnerEventKind.Upgrade && e.Unit == "range");
        Assert.Equal(1.5f, run.RangeMul, 3);
        Assert.Equal(15f, run.ReachOf(run.Hero), 3);
        Assert.True(run.Shots.All(s => s.Range <= 15f + RunnerRules.ShotOvershoot + 1e-3f));

        // Burnt to 70 by a fire patch, then a 35% heal crate: 100 is the cap.
        var heal = Runner.Run(Runner.Tiny(@"{ ""at"": 3, ""kind"": ""hazard"", ""hazard"": ""fire"", ""width"": 2, ""damage"": 30 },
            { ""at"": 18, ""kind"": ""box"", ""hp"": 1, ""reward"": ""heal"", ""amount"": 0.35 }"));
        var hpBefore = 0;
        var healed = false;
        while (!heal.Over && heal.Seconds < 10f && !healed)
        {
            hpBefore = heal.Hero.Hp;
            foreach (var e in heal.Step(1f / 30f))
            {
                if (e.Kind == RunnerEventKind.Upgrade && e.Unit == "heal")
                {
                    healed = true;
                    Assert.True(hpBefore < 100);
                    Assert.Equal(Math.Min(100, hpBefore + 35), heal.Hero.Hp);
                }
            }
        }

        Assert.True(healed);
    }

    [Fact]
    public void Runner_scale_grows_the_body_and_the_shipped_enemies_are_bigger_blightroot_most_and_the_boss_bigger_still()
    {
        var c = Runner.Tiny("", extra: "");
        Assert.Equal(1f, c.Unit("wolf").Scale);
        var big = RunnerCatalog.Parse(@"{ ""units"": {
    ""hero"": { ""roster"": ""Lyra"", ""hp"": 100, ""damage"": 10, ""cycle"": 0.5, ""range"": 10, ""ranged"": true, ""shot_speed"": 20 },
    ""wolf"": { ""roster"": ""Nightfang"", ""hp"": 20, ""damage"": 40, ""cycle"": 1.0, ""range"": 1.2, ""speed"": 2.0, ""radius"": 0.6, ""scale"": 2 } },
  ""levels"": [ { ""id"": ""t"", ""hero"": ""hero"", ""length"": 40, ""speed"": 2, ""half_width"": 4, ""max_followers"": 6,
    ""rewards"": { ""win_gold"": 1, ""gold_per_kill"": 1, ""gold_per_box"": 1, ""shards"": 1, ""shard_hero"": ""hero.lyra"" },
    ""rows"": [ { ""at"": 10, ""kind"": ""enemy"", ""x"": 4, ""unit"": ""wolf"" } ] } ] }");
        Assert.Equal(2f, big.Unit("wolf").Scale);
        Assert.Equal(1.2f, big.Unit("wolf").Radius, 3);

        // A big body still keeps inside the lane.
        var run = new RunnerRun(big, big.First);
        run.Step(0.1f);
        Assert.InRange(run.Foes.Single().X, 0f, 4f - 1.2f + 1e-3f);

        var s = Runner.Catalog();
        foreach (var id in new[] { "bonequill", "nightfang", "vespera", "ashwyrm" })
        {
            Assert.InRange(s.Unit(id).Scale, 1.3f, 1.5f);
        }

        Assert.InRange(s.Unit("blightroot").Scale, 1.8f, 2.0f);
        Assert.True(s.Unit("ironhowl").Scale >= s.Unit("blightroot").Scale);
        Assert.True(s.Unit("blightroot").Radius > s.Unit("nightfang").Radius);
        Assert.All(new[] { "lyra", "rowan", "oakenshield" }, id => Assert.Equal(1f, s.Unit(id).Scale));
    }

    [Fact]
    public void Shipped_squad_range_is_about_one_and_a_half_to_twice_the_old_range_and_levels_are_denser_with_weaker_enemies()
    {
        var c = Runner.Catalog();
        Assert.InRange(c.Unit("lyra").Range, 11.5f * 1.5f, 11.5f * 2f);
        Assert.InRange(c.Unit("rowan").Range, 12.5f * 1.5f, 12.5f * 2f);
        Assert.InRange(c.Unit("oakenshield").Range, 10f * 1.5f, 10f * 2f);
        Assert.Throws<KeyNotFoundException>(() => c.Unit("sir_aldric"));

        // Before this pass: 25 enemies / 10 boxes / 3198 enemy HP on Level 1, 93 / 28 / 43295 on Level 12.
        var old = new[] { (25, 10, 3198), (26, 15, 3512), (32, 16, 4953), (40, 18, 8386), (45, 19, 10938), (54, 21, 13773), (56, 22, 20391), (65, 24, 24817), (70, 25, 31705), (78, 27, 34216), (80, 27, 36987), (93, 28, 43295) };
        for (var i = 0; i < old.Length && i < c.Levels.Count; i++)
        {
            var l = c.Levels[i];
            Assert.True(l.EnemyCount >= 2 * old[i].Item1, $"L{l.Number} enemies {l.EnemyCount}");
            // Boxes were cut about 40% (Derek 2026-10-23: "theres too many upgrades to enimies"): about one box for every 5 enemies on
            // Level 1, falling to about one for every 10 on Level 12, while the count itself still climbs.
            Assert.InRange((float)l.BoxCount / l.EnemyCount, 0.08f, 0.21f);
            Assert.True(i == 0 || (float)l.BoxCount / l.EnemyCount < (float)c.Levels[i - 1].BoxCount / c.Levels[i - 1].EnemyCount + 0.005f, $"L{l.Number} boxes per enemy");
            // Enemies walk 1.5x faster now and the squad is capped at 10, so each one is lighter (about 0.45x-0.8x the old total HP) and the
            // total still climbs every level.
            Assert.InRange(l.FoeHpTotal(c), old[i].Item3 * 0.4, old[i].Item3 * 1.0);
            if (i > 0)
            {
                Assert.True(l.FoeHpTotal(c) > c.Levels[i - 1].FoeHpTotal(c), $"L{l.Number} enemy HP total climbs");
            }
        }

        // Waves come as mixed groups: on Level 1, some 6 m stretch holds two or more kinds of enemy.
        var enemyRows = c.First.Rows.Where(r => r.Kind == RunnerRowKind.Enemy).ToList();
        Assert.Contains(enemyRows, a => enemyRows.Any(b => b.Unit != a.Unit && b.At > a.At && b.At - a.At <= 6f));

        // Every power-up kind by Level 2 (Level 1 lost its heal crate in the box cut), each a box with HP to break.
        foreach (var k in new[] { RunnerBoxKind.Weapon, RunnerBoxKind.Rate, RunnerBoxKind.Range, RunnerBoxKind.Heal })
        {
            Assert.Contains(k == RunnerBoxKind.Heal ? c.Levels[1].Rows : c.First.Rows, r => r.Box?.Kind == k && r.Box.Hp >= 100);
        }
    }
}
