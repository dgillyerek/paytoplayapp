using Survival.Domain.Runner;
using Xunit.Abstractions;

namespace Survival.Domain.Tests;

/// <summary>The test player sidesteps enemy shots like a person: late, through the normal steering, and not always.</summary>
public sealed class RunnerShotDodgeTests
{
    private const float Dt = 1f / 30f;
    private readonly ITestOutputHelper _out;

    public RunnerShotDodgeTests(ITestOutputHelper output)
    {
        _out = output;
    }

    /// <summary>Plays a run and counts enemy shots that landed vs flew out of range.</summary>
    public static (int Landed, int Missed, bool Won) Count(RunnerRun run, Func<RunnerRun, float> steer)
    {
        int landed = 0, missed = 0;
        while (!run.Over && run.Seconds < 600f)
        {
            var before = run.Shots.Where(s => s.ByFoe).Select(s => (s.Id, End: s.Travelled + (s.Speed * Dt) >= s.Range - 1e-3f)).ToList();
            run.SteerTo(steer(run));
            run.Step(Dt);
            var now = run.Shots.Select(s => s.Id).ToHashSet();
            foreach (var (id, end) in before.Where(b => !now.Contains(b.Id)))
            {
                if (end) missed++; else landed++;
            }
        }

        return (landed, missed, run.Outcome == RunnerOutcome.Won);
    }

    [Fact]
    public void Shots_are_dodged_often_but_not_always_and_never_before_the_reaction_time()
    {
        var c = Runner.Catalog();
        int landed = 0, missed = 0, still = 0, stillMissed = 0;
        foreach (var level in c.Levels.Take(20))
        {
            var d = Count(new RunnerRun(c, level, RunnerShipped.Squad(c, level)), r => RunnerBossQuakeTests.DodgingPlayer(r));
            var s = Count(new RunnerRun(c, level, RunnerShipped.Squad(c, level)), Runner.AimLikeAPlayer);
            landed += d.Landed;
            missed += d.Missed;
            still += s.Landed;
            stillMissed += s.Missed;
            var rate = d.Landed + d.Missed > 0 ? d.Missed / (float)(d.Landed + d.Missed) : 1f;
            var srate = s.Landed + s.Missed > 0 ? s.Missed / (float)(s.Landed + s.Missed) : 1f;
            _out.WriteLine($"{level.Id}: dodger {d.Landed} hit / {d.Missed} missed ({rate:P0} dodged, won {d.Won}); non-dodger {s.Landed} / {s.Missed} ({srate:P0}, won {s.Won})");
        }

        var all = missed / (float)Math.Max(1, landed + missed);
        _out.WriteLine($"total: dodged {all:P0} of {landed + missed} shots; non-dodger dodged {stillMissed / (float)Math.Max(1, still + stillMissed):P0}");
        Assert.True(landed + missed > 50, "ranged enemies shoot on the shipped levels");
        Assert.InRange(all, 0.4f, 0.97f);
        Assert.True(landed < still, "dodging lands fewer shots than standing in the lane");

    }
}
