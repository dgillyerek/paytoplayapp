using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Survival.Domain.Runner;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek 2026-10-27: "there's too much empty space at the end of the runner levels that don't have a larger enemy".
/// A level without a boss now closes with a set piece (trap gauntlet on both edges, a fodder push down the middle, the
/// archer line, a captain heavy) and its finish gate stands just past it; once the road ahead is clear the squad runs for
/// the gate, so the walk on an empty road before the finish is never more than about a second and a half.
/// </summary>
public sealed class RunnerFinaleTests
{
    [Fact]
    public void Levels_without_a_boss_end_with_a_finale_right_before_the_finish_gate()
    {
        var c = Runner.Catalog();
        foreach (var l in c.Levels.Where(l => l.Number >= 2))
        {
            var last = l.Rows[^1];
            if (l.HasBoss)
            {
                Assert.DoesNotContain(l.Rows, r => r.Finale);
                Assert.Equal(RunnerRowKind.Boss, last.Kind);
                continue;
            }

            Assert.InRange(l.Length - last.At, 0f, RunnerProgression.FinishPad + 1f);
            var finale = l.Rows.Where(r => r.Finale).ToList();
            // The captain (a heavy, two from FinaleSecondCaptainFrom) is the last thing before the gate.
            Assert.True(last.Finale, $"L{l.Number}: last row is not the finale's");
            Assert.Contains(last.Unit, new[] { "blightroot", "ashwyrm" });
            Assert.Equal(l.Number >= RunnerProgression.FinaleSecondCaptainFrom ? 2 : 1, last.Count);
            // A trap on each edge, the middle left open, and a fodder push down it.
            var traps = finale.Where(r => r.Kind == RunnerRowKind.Hazard).ToList();
            Assert.Equal(2, traps.Count);
            Assert.True(traps[0].X * traps[1].X < 0, $"L{l.Number}: gauntlet traps on both edges");
            Assert.Contains(finale, r => r.Kind == RunnerRowKind.Enemy && r.Unit == "nightfang");
            // The archer line still stands in front of the captain.
            Assert.Contains(l.Rows, r => !r.Finale && r.Unit == "bonequill" && r.At > finale.Min(f => f.At) && r.At < last.At);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(9)]
    [InlineData(17)]
    [InlineData(31)]
    [InlineData(49)]
    public void No_long_empty_walk_before_the_finish(int number)
    {
        var c = Runner.Catalog();
        var level = c.ByNumber(number);
        var required = JsonDocument.Parse(File.ReadAllText(BalanceGolden.SourcePath()))
            .RootElement.GetProperty("required_power")[number - 1].GetDouble();
        // Well above the level's required power, so the road clears as early as it ever does.
        var run = new RunnerRun(c, level, RunnerBalance.Uniform(c, Math.Sqrt(required) * 2.0));
        var lastThing = level.Rows.Where(r => r.Kind != RunnerRowKind.Enemy).Max(r => r.At);
        var busy = 0f;
        while (!run.Over && run.Seconds < 600f)
        {
            run.SteerTo(RunnerBossQuakeTests.DodgingPlayer(run));
            run.Step(1f / 30f);
            if (run.Foes.Count > 0 || run.Distance < lastThing)
            {
                busy = run.Seconds;
            }
        }

        Assert.Equal(RunnerOutcome.Won, run.Outcome);
        Assert.True(run.Seconds - busy <= 2f, $"L{number}: {run.Seconds - busy:0.0} s of empty road before the finish");
    }

    [Fact]
    public void The_gate_charge_is_a_level_setting_and_never_slower_than_the_walk()
    {
        var off = Runner.Run(Runner.Tiny(""));
        off.Step(1f);
        Assert.False(off.Charging);
        Assert.Equal(off.Level.Speed, off.Pace);

        var on = Runner.Run(Runner.Tiny("", extra: @"""gate_charge"": true,"));
        on.Step(1f / 30f);
        Assert.True(on.Charging);
        while (!on.Over && on.Seconds < 10f)
        {
            on.Step(1f / 30f);
        }

        Assert.Equal(RunnerOutcome.Won, on.Outcome);
        Assert.InRange(on.Seconds, 0f, RunnerRules.GateChargeSeconds + 0.1f);
    }
}
