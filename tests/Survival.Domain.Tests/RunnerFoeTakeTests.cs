using System;
using System.Collections.Generic;
using System.Linq;
using Survival.Domain.Enemies;
using Survival.Domain.Roster;
using Survival.Domain.Runner;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>
/// Enemy attack takes in the runner (Derek 2026-10-27: Vespera's bolt left before her hands pushed it out, and her
/// take drifted off her fire rate). The shot leaves on the take's release frame; the take fills one fire interval.
/// </summary>
public class RunnerFoeTakeTests
{
    [Theory]
    [InlineData("vespera")]
    [InlineData("bonequill")]
    [InlineData("nightfang")]
    [InlineData("ashwyrm")]
    public void An_enemy_take_plus_its_rest_beat_is_exactly_one_fire_interval_and_releases_on_its_release_frame(string id)
    {
        var unit = Runner.Catalog().Unit(id);
        var clip = RunnerAttackTiming.ClipFor(unit.Roster)!.Value;
        var plan = RunnerAttackTiming.FoePlanFor(unit);

        // Same rate as before (same damage per second): only the phase of the shot moved.
        Assert.Equal(unit.Cycle, plan.Interval, 4);
        // The rig plays take + rest beat and repeats: at this speed that is one fire interval, so it never drifts.
        Assert.Equal(plan.Interval, (clip.ClipSeconds + BlenderRigAttackSpec.RestGapSeconds) / plan.Speed, 3);
        // The shot leaves at the same point of the take whatever the speed.
        Assert.Equal(clip.ReleaseSeconds, plan.Release * plan.Speed, 4);
    }

    [Fact]
    public void Vespera_lets_go_at_her_hands_push_frame_27_of_36()
    {
        var clip = RunnerAttackTiming.ClipFor("Vespera")!.Value;
        Assert.Equal(VesperaAttack.ReleaseFrame / (float)VesperaAttack.LastFrame, clip.ReleaseNormalized, 3);
        var plan = RunnerAttackTiming.FoePlanFor(Runner.Catalog().Unit("vespera"));
        Assert.InRange(plan.Release, 0.5f, plan.Interval);
    }

    [Fact]
    public void Enemy_shots_leave_at_their_takes_release_frame()
    {
        var c = Runner.Catalog();
        var timed = 0;
        var onFrame = 0;
        foreach (var level in c.Levels.Take(10))
        {
            var run = new RunnerRun(c, level);
            var open = new Dictionary<int, (float At, float Speed)>();
            while (!run.Over && run.Seconds < 400f)
            {
                run.SteerTo(Runner.AimLikeAPlayer(run));
                foreach (var e in run.Step(1f / 60f))
                {
                    if (run.Member(e.Id) != null || run.Foes.FirstOrDefault(x => x.Id == e.Id) is not { } f || f.Plan.Release <= 0f)
                    {
                        continue;
                    }

                    if (e.Kind == RunnerEventKind.AttackStarted)
                    {
                        Assert.InRange(e.Amount, RunnerAttackTiming.MinFoeSpeed - 1e-3f, RunnerAttackTiming.MaxSpeed + 1e-3f);
                        open[e.Id] = (run.Seconds, e.Amount);
                    }
                    else if (e.Kind == RunnerEventKind.Fired)
                    {
                        Assert.True(open.Remove(e.Id, out var take), $"L{level.Number} {f.Unit.Id}: fired with no take started");
                        var waited = run.Seconds - take.At;
                        if (waited < 1e-4f)
                        {
                            continue; // came into reach unannounced: fires as it always did, take starts with it
                        }

                        timed++;
                        var releaseFrame = f.Plan.Release * f.Plan.Speed / take.Speed;
                        onFrame += Math.Abs(waited - releaseFrame) <= 0.05f ? 1 : 0;
                    }
                }
            }
        }

        Assert.True(timed > 1000, "timed shots " + timed);
        // Most land within 50 ms of the hands' push frame (the rest: the squad stepped out of reach mid take).
        Assert.True(onFrame >= timed * 0.8f, $"{onFrame}/{timed} shots on the release frame");
    }
}
