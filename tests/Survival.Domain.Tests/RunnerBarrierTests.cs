using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Survival.Domain.Runner;
using Xunit;
using Xunit.Abstractions;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek 2026-10-26: "there hsould be some obstacles that can get destroyed but do damage if you don't avoid them. maybe
/// some type of gate or stone fence. something time period appropriate". Palisade gates, stone walls and spiked
/// barricades stand on the road: the squad's shots chip them down, and walking into one hurts whoever touches it and
/// smashes it. Most leave a gap; now and then one spans the road and has to be shot down (or walked through).
/// </summary>
public sealed class RunnerBarrierTests
{
    private readonly ITestOutputHelper _out;

    public RunnerBarrierTests(ITestOutputHelper output) => _out = output;

    /// <summary>The shipped units on a bare road with one barrier (and an optional follower box before it).</summary>
    private static RunnerRun Road(string barrierRow, int followers = 0)
    {
        var json = JsonNode.Parse(File.ReadAllText(Path.Combine(TestPaths.CatalogDir, RunnerCatalog.FileName)))!;
        json.AsObject().Remove("progression");
        var level = json["levels"]![0]!;
        level["length"] = 120;
        var rows = new JsonArray();
        if (followers > 0)
        {
            rows.Add(JsonNode.Parse(@"{ ""at"": 4, ""kind"": ""box"", ""x"": 0, ""hp"": 1, ""reward"": ""followers"", ""unit"": ""rowan"", ""count"": " + followers + " }"));
        }

        rows.Add(JsonNode.Parse(barrierRow));
        rows.Add(JsonNode.Parse(@"{ ""at"": 110, ""kind"": ""enemy"", ""x"": 0, ""unit"": ""nightfang"" }"));
        level["rows"] = rows;
        var c = RunnerCatalog.Parse(json.ToJsonString());
        return new RunnerRun(c, c.First);
    }

    private static (System.Collections.Generic.List<RunnerEvent> Events, RunnerRun Run) PlayPast(RunnerRun run, float z, Func<RunnerRun, float> steer)
    {
        var events = new System.Collections.Generic.List<RunnerEvent>();
        while (!run.Over && run.Hero.Z < z && run.Seconds < 900f)
        {
            run.SteerTo(steer(run));
            events.AddRange(run.Step(1f / 30f));
        }

        return (events, run);
    }

    [Fact]
    public void A_barrier_row_parses_from_the_catalog_json()
    {
        var run = Road(@"{ ""at"": 30, ""kind"": ""barrier"", ""x"": 1, ""barrier"": ""stone_wall"", ""width"": 3.6, ""hp"": 250, ""damage"": 60 }");
        var row = run.Level.Rows.Single(r => r.Kind == RunnerRowKind.Barrier);
        Assert.Equal(new RunnerBarrierDef(RunnerBarrierKind.StoneWall, 3.6f, 250, 60), row.Barrier);
        Assert.Equal(1f, row.X);
        Assert.Equal(1, run.Level.BarrierCount);
        Assert.Equal(0, run.Level.FullWidthBarrierCount);
        foreach (var kind in Enum.GetValues<RunnerBarrierKind>())
        {
            Assert.Equal(kind, RunnerBarrierDef.Parse(RunnerBarrierDef.Name(kind)));
        }
    }

    [Fact]
    public void Shots_chip_a_barrier_down_until_it_breaks_and_the_hero_walks_on_unhurt()
    {
        var run = Road(@"{ ""at"": 40, ""kind"": ""barrier"", ""x"": 0, ""barrier"": ""palisade"", ""width"": 8, ""hp"": 300, ""damage"": 80 }");
        var (events, _) = PlayPast(run, 45f, _ => 0f);
        var hits = events.Where(e => e.Kind == RunnerEventKind.BarrierHit).ToList();
        Assert.True(hits.Count >= 2, "hits " + hits.Count);
        Assert.True(hits.Sum(e => e.Amount) >= 300f, "chipped " + hits.Sum(e => e.Amount));
        var broken = Assert.Single(events, e => e.Kind == RunnerEventKind.BarrierBroken);
        Assert.False(broken.Big);
        Assert.Equal("palisade", broken.Unit);
        Assert.Equal(1, run.BarriersBroken);
        Assert.Equal(0, run.BarriersSmashed);
        Assert.Empty(run.Barriers);
        Assert.Equal(run.Hero.MaxHp, run.Hero.Hp);
    }

    [Fact]
    public void Walking_into_a_barrier_hurts_the_hero_and_smashes_it()
    {
        var run = Road(@"{ ""at"": 20, ""kind"": ""barrier"", ""x"": 0, ""barrier"": ""stone_wall"", ""width"": 8, ""hp"": 1000000, ""damage"": 90 }");
        var (events, _) = PlayPast(run, 25f, _ => 0f);
        var smashed = Assert.Single(events, e => e.Kind == RunnerEventKind.BarrierBroken);
        Assert.True(smashed.Big);
        Assert.Contains(events, e => e.Kind == RunnerEventKind.HeroHit && e.Other == smashed.Id && e.Amount == 90f);
        Assert.Equal(run.Hero.MaxHp - 90, run.Hero.Hp);
        Assert.Equal(1, run.BarriersSmashed);
        Assert.Empty(run.Barriers);
    }

    [Fact]
    public void A_follower_that_clips_a_barrier_takes_a_trap_share_and_smashes_it()
    {
        // The hero runs the middle; the followers fan out behind, and the right-hand ones clip a narrow wall on that side.
        var run = Road(@"{ ""at"": 30, ""kind"": ""barrier"", ""x"": 1.8, ""barrier"": ""barricade"", ""width"": 2, ""hp"": 1000000, ""damage"": 30 }", followers: 6);
        var (events, _) = PlayPast(run, 40f, _ => 0f);
        var smashed = Assert.Single(events, e => e.Kind == RunnerEventKind.BarrierBroken);
        Assert.True(smashed.Big);
        Assert.DoesNotContain(events, e => e.Kind == RunnerEventKind.HeroHit);
        var followerHits = events.Where(e => e.Kind == RunnerEventKind.FollowerHit && e.Other == smashed.Id).ToList();
        Assert.NotEmpty(followerHits);
        Assert.All(followerHits, e => Assert.Equal(RunnerRun.FollowerTrapDamage(30), e.Amount));
        Assert.Equal(followerHits.Count, (int)smashed.Amount);
        Assert.Equal(1, run.BarriersSmashed);
        Assert.Equal(followerHits.Count, run.BarrierHits);
    }

    [Fact]
    public void Steering_round_a_side_barrier_takes_no_damage()
    {
        var run = Road(@"{ ""at"": 30, ""kind"": ""barrier"", ""x"": 2.4, ""barrier"": ""palisade"", ""width"": 3.2, ""hp"": 1000000, ""damage"": 100 }");
        var (events, _) = PlayPast(run, 40f, _ => -2.5f);
        Assert.DoesNotContain(events, e => e.Kind == RunnerEventKind.HeroHit);
        Assert.Equal(0, run.BarriersSmashed);
    }

    [Fact]
    public void Shipped_barriers_get_tougher_and_more_common_level_by_level_and_most_leave_a_gap()
    {
        var c = Runner.Catalog();
        Assert.Equal(0, c.First.FullWidthBarrierCount);
        Assert.True(c.First.BarrierCount >= 1, "L1 barriers " + c.First.BarrierCount);
        double MeanHp(int n) => c.ByNumber(n).Rows.Where(r => r.Barrier != null).Average(r => r.Barrier!.Hp);
        double MeanDmg(int n) => c.ByNumber(n).Rows.Where(r => r.Barrier != null).Average(r => r.Barrier!.Damage);
        foreach (var (a, b) in new[] { (1, 10), (10, 20), (20, 30), (30, 40), (40, 50) })
        {
            Assert.True(MeanHp(b) > MeanHp(a), $"barrier hp L{a} {MeanHp(a):0} -> L{b} {MeanHp(b):0}");
            Assert.True(MeanDmg(b) > MeanDmg(a), $"barrier damage L{a} {MeanDmg(a):0} -> L{b} {MeanDmg(b):0}");
            // Every beat carries one from Level 25 on, so the count tops out there.
            Assert.True(b <= 30 ? c.ByNumber(b).BarrierCount > c.ByNumber(a).BarrierCount : c.ByNumber(b).BarrierCount >= c.ByNumber(a).BarrierCount, $"barriers L{a} {c.ByNumber(a).BarrierCount} -> L{b} {c.ByNumber(b).BarrierCount}");
        }

        var all = 0;
        var full = 0;
        foreach (var level in c.Levels)
        {
            foreach (var r in level.Rows.Where(r => r.Barrier != null))
            {
                all++;
                if (r.Barrier!.FullWidth(level.HalfWidth))
                {
                    full++;
                    continue;
                }

                // A side or middle barrier leaves at least 2 m of open road beside it.
                var open = Math.Max(level.HalfWidth - (r.X + (r.Barrier.Width * 0.5f)), (r.X - (r.Barrier.Width * 0.5f)) + level.HalfWidth);
                Assert.True(open >= 2f - 1e-3f, $"L{level.Number} barrier at {r.At} leaves {open:0.00} m");
            }
        }

        _out.WriteLine($"{all} barriers, {full} full-width");
        Assert.InRange(full, 1, all / 3);
    }

    [Fact]
    public void Up_to_level_30_the_required_power_shoots_down_every_full_width_barrier_in_time()
    {
        var c = Runner.Catalog();
        var r = BalanceReport.Get();
        var checkedAny = false;
        for (var n = 1; n <= 30; n++)
        {
            var level = c.ByNumber(n);
            foreach (var row in level.Rows.Where(x => x.Barrier != null && x.Barrier.FullWidth(level.HalfWidth)))
            {
                checkedAny = true;
                // The barrier at the same spot, with the level's boxes before it (the followers and crates a player picks up
                // on the way) but no enemies or traps, facing the weakest squad that still clears this level.
                var bare = level with { Rows = level.Rows.Where(x => (x.Kind == RunnerRowKind.Box && x.At < row.At) || x == row).Append(level.Rows.Last()).ToArray() };
                var run = new RunnerRun(c, bare, RunnerBalance.Uniform(c, r.RequiredScale[n]));
                PlayPast(run, row.At + 2f, Runner.AimLikeAPlayer);
                Assert.True(run.Barriers.Count == 0, $"L{n} barrier at {row.At} still standing: {run.Outcome} z {run.Hero.Z:0.0} t {run.Seconds:0.0} hp {run.Barriers.FirstOrDefault()?.Hp} last {level.Rows.Last().Kind} {level.Rows.Last().At}");
                Assert.True(run.BarriersBroken == 1 && run.BarriersSmashed == 0, $"L{n} barrier at {row.At} ({row.Barrier!.Hp} hp) reached the hero before it fell");
                // And walking into it by mistake costs less than half the hero.
                Assert.True(row.Barrier!.Damage < run.Hero.MaxHp / 2, $"L{n} barrier damage {row.Barrier.Damage} vs hero {run.Hero.MaxHp}");
            }
        }

        Assert.True(checkedAny);
    }
}
