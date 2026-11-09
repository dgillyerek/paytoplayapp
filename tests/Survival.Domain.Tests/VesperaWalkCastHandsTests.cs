using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Survival.Domain.Runner;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>Derek 2026-10-31: "shes walking but she misses her hands rotating around the ball".</summary>
public class VesperaWalkCastHandsTests
{
    private const string Hips = "VESPERA_rig";
    private const string Spine = "mixamorig:Spine1";
    private const string Chest = Hips + "/" + Spine + "/mixamorig:Spine2";

    [Theory]
    [InlineData("Left")]
    [InlineData("Right")]
    public void Vesperas_arms_hands_and_fingers_take_the_cast(string side)
    {
        var arm = Chest + "/mixamorig:" + side + "Arm";
        var fore = arm + "/mixamorig:" + side + "ForeArm";
        var hand = fore + "/mixamorig:" + side + "Hand";
        foreach (var p in new[] { Hips + "/" + Spine, Chest, arm, fore, hand, hand + "/mixamorig:" + side + "HandIndex1", hand + "/mixamorig:" + side + "HandThumb1/mixamorig:" + side + "HandThumb2" })
        {
            Assert.True(RunnerUpperBodyAttack.IsUpperBody(p, Hips, Spine), p);
        }
    }

    [Theory]
    [InlineData("Left")]
    [InlineData("Right")]
    public void Vesperas_legs_stay_on_the_walk(string side)
    {
        var up = Hips + "/mixamorig:" + side + "UpLeg";
        foreach (var p in new[] { Hips, up, up + "/mixamorig:" + side + "Leg", up + "/mixamorig:" + side + "Leg/mixamorig:" + side + "Foot" })
        {
            Assert.False(RunnerUpperBodyAttack.IsUpperBody(p, Hips, Spine), p);
        }
    }

    [Fact]
    public void The_copied_upper_body_follows_the_take_weights()
    {
        Assert.Equal(0f, RunnerUpperBodyAttack.CopyWeight(0f, 0f));
        Assert.Equal(1f, RunnerUpperBodyAttack.CopyWeight(1f, 0f));
        Assert.Equal(1f, RunnerUpperBodyAttack.CopyWeight(0.3f, 1f));
        Assert.InRange(RunnerUpperBodyAttack.CopyWeight(0.5f, 0f), 0.499f, 0.501f);
    }

    [Fact]
    public void The_stand_in_hips_rig_copies_the_take_onto_its_upper_body()
    {
        var src = File.ReadAllText(Path.Combine(Layouts.RepoRoot(), "Assets", "Survival", "Unity", "BlenderRigPlayer.cs"));
        Assert.Contains("_hipsStandIn = true;", src);
        Assert.Contains("CopyUpperFromTake(take, prev);", src);
        Assert.Contains("#if UNITY_EDITOR || DEVELOPMENT_BUILD", src);
    }

    /// <summary>
    /// Her cast plays at no more than its authored speed in the runner and its circling (f4-f26) is never cut by a
    /// restart or a walk fade: each take shows it for close to a second.
    /// </summary>
    [Fact]
    public void Vesperas_circling_plays_out_whole_in_the_runner()
    {
        var c = Runner.Catalog();
        const float dt = 1f / 60f;
        var takes = 0;
        var cut = 0;
        var speeds = new List<float>();
        foreach (var level in c.Levels)
        {
            var run = new RunnerRun(c, level);
            var clock = new Dictionary<int, (float T, float Speed, bool On)>();
            while (!run.Over && run.Seconds < 150f)
            {
                run.SteerTo(Runner.AimLikeAPlayer(run));
                foreach (var e in run.Step(dt))
                {
                    if (e.Kind != RunnerEventKind.AttackStarted || run.Foe(e.Id) is not { } f || f.Unit.Roster != "Vespera")
                    {
                        continue;
                    }

                    if (clock.TryGetValue(e.Id, out var k) && k.On && k.T * 30f < 26f)
                    {
                        cut++;
                    }

                    takes++;
                    speeds.Add(e.Amount);
                    clock[e.Id] = (0f, e.Amount, true);
                }

                foreach (var f in run.Foes.Where(f => f.Unit.Roster == "Vespera" && f.Hp > 0))
                {
                    if (!clock.TryGetValue(f.Id, out var k))
                    {
                        continue;
                    }

                    var body = RunnerWalkingShooterFoes.BodyFor(f.Walking, f.Attacking, f.InTake, true);
                    if (k.On && (body == RunnerFoeBody.Walk || body == RunnerFoeBody.Rest) && k.T * 30f < 26f)
                    {
                        cut++;
                        k.On = false;
                    }

                    k.T += dt * k.Speed;
                    clock[f.Id] = k;
                }
            }
        }

        Assert.True(takes >= 20, "takes " + takes);
        Assert.Equal(0, cut);
        Assert.All(speeds, s => Assert.InRange(s, 0.5f, 1.0f));
        // f4-f26 at the median speed: about 0.86 s on screen.
        var median = speeds.OrderBy(s => s).ElementAt(speeds.Count / 2);
        Assert.InRange(22f / 30f / median, 0.7f, 1.5f);
    }
}
