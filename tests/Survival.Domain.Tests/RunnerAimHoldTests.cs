using System.Numerics;
using Survival.Domain.Heroes;
using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek 2026-10-24: "just keep the heros body in firing position constantly. moving back and forth is wild." Then
/// 2026-10-27: "not all heros have a firing animation in the runner. some of them are just walking". The squad shooters'
/// bodies stay still (no hit step, flinch or lean), but every shot plays the hero's real attack take on the upper body
/// over the walk, its release frame on the shot.
/// </summary>
public sealed class RunnerAimHoldTests
{
    private const float Dt = 1f / 30f;

    [Theory]
    [InlineData("Lyra", LyraAttack.ReleaseFrame)]
    [InlineData("Rowan", RowanAttack.ReleaseFrame)]
    [InlineData("Oakenshield", OakenshieldAttack.ReleaseFrame)]
    public void Shooters_play_their_take_per_shot_and_take_no_body_reaction(string roster, int release)
    {
        Assert.True(RunnerAimHold.Applies(roster));
        Assert.True(RunnerUpperBodyAttack.Applies(roster), "the take plays on the upper body; the legs keep walking");
        Assert.Null(RunnerAimHold.HoldFrameFor(roster));
        Assert.Equal(release - 1, RunnerAimHold.HeldFrameWas(roster));
        Assert.Equal(0f, RunnerAimHold.BodyReactionScale(roster));
        Assert.Equal(0f, RunnerAimHold.LeanScale(roster));
    }

    [Theory]
    [InlineData("Sir Aldric")]
    [InlineData("Blightroot")]
    [InlineData("Ironhowl")]
    public void Other_rigs_keep_their_takes_and_reactions(string roster)
    {
        Assert.False(RunnerAimHold.Applies(roster));
        Assert.Null(RunnerAimHold.HoldFrameFor(roster));
        Assert.Equal(1f, RunnerAimHold.BodyReactionScale(roster));
        Assert.Equal(1f, RunnerAimHold.LeanScale(roster));
    }

    /// <summary>
    /// Every runner hero (a unit naming a campaign hero) resolves a real attack take with a release inside it, and in a run
    /// each of its takes starts (AttackStarted) and releases its shot (Fired) one release-time later, once per take.
    /// </summary>
    [Fact]
    public void Every_runner_hero_has_an_attack_take_with_a_release_and_fires_on_it()
    {
        var c = Runner.Catalog();
        var heroes = c.Units.Values.Where(u => u.HeroId.Length > 0).ToList();
        Assert.Equal(new[] { "Lyra", "Oakenshield", "Rowan" }, heroes.Select(u => u.Roster).OrderBy(r => r).ToArray());
        foreach (var u in heroes)
        {
            var clip = RunnerAttackTiming.ClipFor(u.Roster);
            Assert.True(clip != null, u.Roster + " has no attack take");
            Assert.True(clip!.Value.ClipSeconds > 0.2f, u.Roster + " take length");
            Assert.InRange(clip.Value.ReleaseNormalized, 0.05f, 0.95f);
            var plan = RunnerAttackTiming.PlanFor(u);
            Assert.True(plan.Release > 0f && plan.Release < plan.Interval, u.Roster + " release inside the take");
            Assert.Null(RunnerAimHold.HoldFrameFor(u.Roster));
        }

        var run = new RunnerRun(c, c.Levels[2]);
        var started = new Dictionary<int, float>();
        var takes = 0;
        var fired = 0;
        while (!run.Over && run.Seconds < 120f)
        {
            run.SteerTo(Runner.AimLikeAPlayer(run));
            foreach (var e in run.Step(Dt))
            {
                if (run.Member(e.Id) is not { } m || m.Unit.HeroId.Length == 0)
                {
                    continue;
                }

                if (e.Kind == RunnerEventKind.AttackStarted)
                {
                    Assert.True(e.Amount > 0f, "the take plays at a positive speed");
                    started[e.Id] = run.Seconds;
                    takes++;
                }
                else if (e.Kind == RunnerEventKind.Fired && started.Remove(e.Id, out var at))
                {
                    fired++;
                    Assert.InRange(run.Seconds - at, m.Cycle.Release - Dt - 1e-3f, m.Cycle.Release + Dt + 1e-3f);
                }
            }
        }

        Assert.True(takes >= 10, "takes " + takes);
        Assert.True(fired >= takes - 12, $"fired {fired} of {takes}");
    }

    [Fact]
    public void The_runner_rig_plays_the_take_and_keeps_the_body_still()
    {
        var root = Layouts.RepoRoot();
        var rig = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "Runner", "RunnerRig.cs"));
        Assert.Equal(3, rig.Split("rig._steady = RunnerAimHold.Applies(data.Name);").Length - 1);
        Assert.Equal(3, rig.Split("rig.Bind(a.PlayPose, () => a.Pose, v => a.AttackSpeed = v, a.SetWalkUnderAttack);").Length - 1);
        Assert.Contains("var w = _steady ? 0f : _reaction.Weight(_hitAge);", rig);
        Assert.Contains("_steady ? 0f : _leanTarget", rig);
        var view = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "Runner", "RunnerLevelView.cs"));
        Assert.Contains("m.StartTake(e.Amount);", view);
    }
}
