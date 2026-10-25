using System.Numerics;
using Survival.Domain.Heroes;
using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek 2026-10-24: "just keep the heros body in firing position constantly. moving back and forth is wild." The runner's
/// squad shooters hold one aim pose on the upper body (legs still walk), whatever the shots, hits or steering.
/// </summary>
public sealed class RunnerAimHoldTests
{
    private const float Dt = 1f / 30f;

    [Theory]
    [InlineData("Lyra", LyraAttack.ReleaseFrame)]
    [InlineData("Rowan", RowanAttack.ReleaseFrame)]
    [InlineData("Oakenshield", OakenshieldAttack.ReleaseFrame)]
    public void Shooters_hold_the_frame_before_release_and_take_no_body_reaction(string roster, int release)
    {
        Assert.True(RunnerAimHold.Applies(roster));
        Assert.Equal(release - 1, RunnerAimHold.HoldFrameFor(roster));
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

    [Theory]
    [InlineData("Lyra")]
    [InlineData("Rowan")]
    [InlineData("Oakenshield")]
    public void Sustained_fire_keeps_the_upper_body_pose_and_root_still_across_shots(string roster)
    {
        var c = Runner.Catalog();
        var hold = RunnerAimHold.HoldFrameFor(roster)!.Value;
        var layer = new RunnerAimHoldLayer(hold);
        var run = new RunnerRun(c, c.Levels[2]);
        var shots = 0;
        var hits = 0;
        var walkReturns = 0;
        var speeds = new HashSet<float>();
        var wasAttacking = false;
        float? firstZ = null;
        Quaternion? firstTurn = null;
        var oldWeights = new List<float>();
        var sinceTake = 10f;
        var nudge = Vector3.Zero;
        var lean = 0f;
        while (!run.Over && run.Seconds < 120f)
        {
            run.SteerTo(Runner.AimLikeAPlayer(run));
            nudge *= 0.8f;
            foreach (var e in run.Step(Dt))
            {
                if (e.Kind == RunnerEventKind.AttackStarted && e.Id == run.Hero.Id)
                {
                    layer.Play(RunnerAimHoldPose.Attack);
                    shots++;
                    speeds.Add(e.Amount);
                    sinceTake = 0f;
                }
                else if (e.Kind == RunnerEventKind.HeroHit)
                {
                    // The rig's raw flinch (capped) pushes the body back; a held shooter shows none of it.
                    hits++;
                    nudge = new Vector3(0f, 0f, -RunnerHitNudge.MaxMetres);
                }
            }

            sinceTake += Dt;
            var attacking = run.Hero.Attacking;
            if (wasAttacking && !attacking)
            {
                walkReturns++;
            }

            wasAttacking = attacking;
            layer.Play(attacking ? RunnerAimHoldPose.Attack : run.Halted ? RunnerAimHoldPose.Rest : RunnerAimHoldPose.Walk);
            layer.SetLegsWalk(!run.Halted);

            // Upper body: full weight on the one held frame, every frame of the run.
            Assert.Equal(1f, layer.Weight);
            Assert.Equal(hold, layer.Frame);

            // The old per-shot take eased in from zero on each shot (what made the arms swing in and out).
            oldWeights.Add(RunnerUpperBodyAttack.FadeIn(sinceTake));

            // Root: hit nudge and steering roll scaled away for a shooter, so the host never moves forward / back.
            lean = 7f * (float)System.Math.Sin(run.Seconds * 3f);
            var applied = nudge * RunnerAimHold.BodyReactionScale(roster);
            var roll = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, lean * RunnerAimHold.LeanScale(roster) * (float)System.Math.PI / 180f);
            var host = RunnerRootPlacement.HostPosition(Vector3.Zero, applied, 0f, roll, Vector3.One, new Vector3(0f, 0.9f, 0f), new Vector3(0f, 0.9f, 0f), false);
            firstZ ??= host.Z;
            firstTurn ??= roll;
            Assert.InRange(host.Z - firstZ.Value, -1e-5f, 1e-5f);
            Assert.InRange(host.X, -1e-5f, 1e-5f);
            Assert.True(Quaternion.Dot(roll, firstTurn.Value) > 0.99999f, "the held body never rolls");
        }

        Assert.True(shots >= 10, "sustained fire: " + shots + " shots");
        Assert.True(hits >= 0);
        Assert.True(oldWeights.Min() < 0.5f, "the old path did blend per shot (so this test would catch it)");
    }

    [Fact]
    public void The_hold_is_built_once_and_never_restarted_by_shots_or_walk_returns()
    {
        var layer = new RunnerAimHoldLayer(13f);
        Assert.Equal(0f, layer.Weight);
        Assert.True(layer.Play(RunnerAimHoldPose.Walk), "first request builds the held take");
        for (var i = 0; i < 50; i++)
        {
            Assert.False(layer.Play(i % 3 == 0 ? RunnerAimHoldPose.Walk : RunnerAimHoldPose.Attack));
            Assert.Equal(1f, layer.Weight);
            Assert.Equal(13f, layer.Frame);
        }

        Assert.False(layer.Play(RunnerAimHoldPose.Rest));
        Assert.False(layer.LegsWalk);
        Assert.Equal(1f, layer.Weight);
        layer.Reset();
        Assert.True(layer.Play(RunnerAimHoldPose.Attack));
    }

    [Fact]
    public void The_rig_player_and_runner_rig_use_the_hold()
    {
        var root = Layouts.RepoRoot();
        var player = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlenderRigPlayer.cs"));
        Assert.Contains("return PlayAimHold(poseName, aimPose.Value);", player);
        Assert.Contains("_takeFrame = _aim.Frame;", player);
        Assert.Contains("return _aim.Weight;", player);
        var rig = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "Runner", "RunnerRig.cs"));
        Assert.Equal(3, rig.Split("a.AimHoldFrame = a.UpperBodyAttack ? RunnerAimHold.HoldFrameFor(data.Name) : null;").Length - 1);
        Assert.Contains("var w = _aimHeld ? 0f : _reaction.Weight(_hitAge);", rig);
        Assert.Contains("_aimHeld ? 0f : _leanTarget", rig);
        Assert.Contains("!Data.AttackCyclesItself && !_aimHeld", rig);
    }
}
