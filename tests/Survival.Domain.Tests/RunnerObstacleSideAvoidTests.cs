using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek 2026-10-27: "the obstacles should do more damage when heroes side run into them" and "the enemies should avoid
/// the obstacles and not run over them".
/// </summary>
public sealed class RunnerObstacleSideAvoidTests
{
    private const float Dt = 1f / 30f;

    [Fact]
    public void A_side_hit_costs_double_and_a_followers_never_takes_all_of_a_fresh_ones_hp()
    {
        Assert.Equal(2f, RunnerRules.SideHitMul);
        Assert.Equal(90, RunnerRun.SideDamage(90, false));
        Assert.Equal(180, RunnerRun.SideDamage(90, true));

        // Followers: the usual trap share, doubled from the side, but one hit never empties a fresh follower.
        Assert.Equal(RunnerRun.FollowerTrapDamage(10) * 2, RunnerRun.FollowerTrapDamage(10, true));
        Assert.Equal(RunnerRun.FollowerTrapDamage(40), RunnerRun.FollowerTrapDamage(40, false));
        Assert.Equal(RunnerRules.FollowerHp - 1, RunnerRun.FollowerTrapDamage(1000, true));
    }

    [Fact]
    public void Steering_into_a_barrier_from_the_side_hurts_the_hero_twice_as_much_as_walking_into_it()
    {
        const string wall = @"{ ""at"": 14, ""kind"": ""barrier"", ""x"": 0, ""barrier"": ""stone_wall"", ""width"": 2, ""hp"": 1000000, ""damage"": 30 }";

        var headOn = Runner.Run(Runner.Tiny(wall));
        var a = Runner.Play(headOn, _ => 0f, 12f);
        var straight = Assert.Single(a, e => e.Kind == RunnerEventKind.HeroHit);
        Assert.Equal(30f, straight.Amount);

        // Runs alongside it, then swerves into it while level with it.
        var side = Runner.Run(Runner.Tiny(wall));
        var b = Runner.Play(side, r => r.Barriers.Count > 0 && r.Hero.Z >= r.Barriers[0].Z - r.Barriers[0].HalfDepth ? 0f : 3f, 12f);
        var swerve = Assert.Single(b, e => e.Kind == RunnerEventKind.HeroHit);
        Assert.Equal(60f, swerve.Amount);
        Assert.Equal(1, side.SideHits);
    }

    /// <summary>Plays with the hero parked out of the way, checking every frame that no foe's body is on a trap or a standing barrier.</summary>
    private static (List<RunnerEvent> Events, bool SawAvoid, bool SawBlocked, float MinZ) Watch(RunnerRun run, float heroX, float seconds, int foeId)
    {
        var events = new List<RunnerEvent>();
        var sawAvoid = false;
        var sawBlocked = false;
        var minZ = float.MaxValue;
        while (!run.Over && run.Seconds < seconds)
        {
            run.SteerTo(heroX);
            events.AddRange(run.Step(Dt));
            foreach (var f in run.Foes.Where(f => f.Id == foeId))
            {
                var body = f.Unit.Radius * RunnerRules.HazardBodyShare;
                foreach (var h in run.Hazards)
                {
                    Assert.False(RunnerObstacle.Overlaps(h.X, h.Z, h.HalfWidth, h.HalfDepth, f.X, f.Z, body), $"t{run.Seconds:0.00} foe on the {h.Kind} at x{f.X:0.00} z{f.Z:0.00}");
                }

                foreach (var w in run.Barriers.Where(w => w.Hp > 0))
                {
                    Assert.False(RunnerObstacle.Overlaps(w.X, w.Z, w.HalfWidth, w.HalfDepth, f.X, f.Z, body), $"t{run.Seconds:0.00} foe on the barrier at x{f.X:0.00} z{f.Z:0.00}");
                }

                sawAvoid |= f.Avoiding;
                sawBlocked |= f.Blocked;
                minZ = Math.Min(minZ, f.Z);
            }
        }

        return (events, sawAvoid, sawBlocked, minZ);
    }

    [Theory]
    [InlineData(@"{ ""at"": 24, ""kind"": ""hazard"", ""x"": 0, ""hazard"": ""spikes"", ""width"": 2, ""damage"": 25 }")]
    [InlineData(@"{ ""at"": 24, ""kind"": ""hazard"", ""x"": 0, ""hazard"": ""fire"", ""width"": 2, ""damage"": 25 }")]
    [InlineData(@"{ ""at"": 24, ""kind"": ""barrier"", ""x"": 0, ""barrier"": ""palisade"", ""width"": 2.4, ""hp"": 1000000, ""damage"": 25 }")]
    public void An_enemy_goes_round_a_trap_or_a_part_width_barrier_in_its_path_and_gets_past_it(string obstacle)
    {
        // A tough foe straight down the middle at a trap / barrier; the hero walks the far right edge so it is never in the way.
        var run = Runner.Run(Runner.Tiny(obstacle + @", { ""at"": 34, ""kind"": ""enemy"", ""x"": 0, ""unit"": ""brute"" }", length: 60f));
        var foe = Assert.Single(run.Foes);
        var obstacleZ = run.Hazards.Count > 0 ? run.Hazards[0].Z : run.Barriers[0].Z;
        var (_, sawAvoid, _, minZ) = Watch(run, 3.4f, 9f, foe.Id);
        Assert.True(sawAvoid, "never side-stepped");
        Assert.True(minZ < obstacleZ - 2f, $"never got past it (min z {minZ:0.0})");
    }

    [Fact]
    public void An_enemy_waits_behind_a_full_width_barrier_and_batters_it_down_then_walks_on()
    {
        var run = Runner.Run(Runner.Tiny(@"{ ""at"": 24, ""kind"": ""barrier"", ""x"": 0, ""barrier"": ""palisade"", ""width"": 8, ""hp"": 400, ""damage"": 25 },
            { ""at"": 30, ""kind"": ""enemy"", ""x"": 0, ""unit"": ""brute"" }", length: 200f));
        var foe = Assert.Single(run.Foes);
        var wall = Assert.Single(run.Barriers);
        var wallBack = wall.Z - wall.HalfDepth;

        // Until the wall is down (the brute battering it, the squad shooting it once in range) the foe never gets past it.
        var held = new List<RunnerEvent>();
        var sawBlocked = false;
        while (run.Barriers.Count > 0 && run.Seconds < 60f)
        {
            run.SteerTo(0f);
            held.AddRange(run.Step(Dt));
            var f = run.Foes.Single(x => x.Id == foe.Id);
            if (run.Barriers.Count > 0)
            {
                Assert.True(f.Z > wallBack, $"t{run.Seconds:0.00} walked through the wall (z {f.Z:0.00})");
            }

            sawBlocked |= f.Blocked && f.BlockedBy == wall.Id;
        }

        Assert.True(sawBlocked, "never held at the wall");
        Assert.Contains(held, e => e.Kind == RunnerEventKind.BarrierHit && e.Other == foe.Id);
        Assert.Empty(run.Barriers);
        Assert.Contains(held, e => e.Kind == RunnerEventKind.BarrierBroken);
    }

    [Fact]
    public void On_a_clear_road_enemies_walk_at_their_usual_pace()
    {
        var run = Runner.Run(Runner.Tiny(@"{ ""at"": 36, ""kind"": ""enemy"", ""x"": 0, ""unit"": ""brute"" }", length: 60f));
        var foe = Assert.Single(run.Foes);
        var z = foe.Z;
        for (var i = 0; i < 30; i++)
        {
            run.SteerTo(0f);
            run.Step(Dt);
            Assert.False(foe.Avoiding);
            Assert.False(foe.Blocked);
            Assert.Equal(z - (foe.Unit.Speed * Dt), foe.Z, 3);
            z = foe.Z;
        }
    }
}
