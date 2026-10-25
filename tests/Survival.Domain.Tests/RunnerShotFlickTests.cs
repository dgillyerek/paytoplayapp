using System.Numerics;
using Survival.Domain.Heroes;
using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek 2026-10-24: "add is some sort of firing motion for heroes, just make it subtle". Each shot adds a small,
/// rotation-only kick on the held aim pose's arm / hand / upper-spine bones, eased back to the hold; the root never moves.
/// </summary>
public sealed class RunnerShotFlickTests
{
    private const float Dt = 1f / 60f;

    public static IEnumerable<object[]> Shooters() => new[]
    {
        new object[] { "Lyra" },
        new object[] { "Rowan" },
        new object[] { "Oakenshield" },
    };

    private static string[] BonesOf(string roster) => roster switch
    {
        "Lyra" => LyraMotion.BoneNames,
        "Rowan" => RowanMotion.BoneNames,
        _ => OakenshieldMotion.BoneNames,
    };

    [Theory]
    [MemberData(nameof(Shooters))]
    public void Each_shooter_kicks_a_few_degrees_on_real_upper_body_bones_only(string roster)
    {
        var bones = RunnerShotFlick.BonesFor(roster);
        Assert.NotEmpty(bones);
        var total = 0f;
        foreach (var b in bones)
        {
            Assert.Contains(b.Bone, BonesOf(roster));
            Assert.DoesNotContain(b.Bone, RunnerShotFlick.Forbidden);
            Assert.InRange(System.Math.Abs(b.PitchDegrees), 1f, 8f);
            total += System.Math.Abs(b.PitchDegrees);
        }

        // Even summed down every kicked bone the turn stays small.
        Assert.InRange(total, 3f, RunnerShotFlick.MaxDegrees + 1e-4f);
        Assert.InRange(RunnerShotFlick.SecondsFor(roster), 0.15f, 0.25f);
    }

    [Theory]
    [MemberData(nameof(Shooters))]
    public void A_shot_peaks_small_then_eases_back_to_the_hold_and_the_root_never_moves(string roster)
    {
        var bones = RunnerShotFlick.BonesFor(roster);
        var state = new RunnerShotFlickState(RunnerShotFlick.SecondsFor(roster));
        Assert.Equal(0f, state.Weight);
        state.Fire();
        var peak = 0f;
        var peakAt = 0f;
        var t = 0f;
        var last = 0f;
        var root = Vector3.Zero;
        for (var i = 0; i < 30; i++)
        {
            state.Advance(Dt);
            t += Dt;
            var w = state.Weight;
            Assert.InRange(w, 0f, 1f);
            Assert.True(System.Math.Abs(w - last) <= 0.6f, "no pop: " + w + " after " + last);
            last = w;
            var deg = 0f;
            foreach (var b in bones)
            {
                deg += System.Math.Abs(b.PitchDegrees) * w;
            }

            if (deg > peak)
            {
                peak = deg;
                peakAt = t;
            }

            // The kick is rotation only: nothing in it offsets the root forward / back.
            Assert.Equal(Vector3.Zero, root);
        }

        Assert.InRange(peak, 3f, RunnerShotFlick.MaxDegrees);
        Assert.True(peakAt <= RunnerShotFlick.RiseSeconds + Dt + 1e-4f, "peaks on the shot: " + peakAt);
        Assert.Equal(0f, state.Weight);
    }

    [Fact]
    public void Fast_fire_never_stacks_past_one_kick_and_never_pops()
    {
        var state = new RunnerShotFlickState(0.2f);
        var last = 0f;
        for (var i = 0; i < 600; i++)
        {
            if (i % 7 == 0)
            {
                state.Fire();
            }

            state.Advance(Dt);
            var w = state.Weight;
            Assert.InRange(w, 0f, 1f);
            Assert.True(System.Math.Abs(w - last) <= 0.6f);
            last = w;
        }
    }

    [Fact]
    public void The_envelope_rises_then_eases_out_to_exactly_zero()
    {
        Assert.Equal(0f, RunnerShotFlick.Weight(0f, 0.2f));
        Assert.Equal(1f, RunnerShotFlick.Weight(RunnerShotFlick.RiseSeconds, 0.2f), 4);
        Assert.Equal(0f, RunnerShotFlick.Weight(0.2f, 0.2f));
        Assert.True(RunnerShotFlick.Weight(0.19f, 0.2f) < 0.01f, "eases out into the hold");
        Assert.Equal(0f, RunnerShotFlick.Weight(-0.1f, 0.2f));
    }

    [Theory]
    [InlineData("Sir Aldric")]
    [InlineData("Blightroot")]
    [InlineData("Ironhowl")]
    [InlineData("Emberfang")]
    public void Other_rigs_do_not_kick(string roster)
    {
        Assert.Empty(RunnerShotFlick.BonesFor(roster));
        Assert.Equal(0f, RunnerShotFlick.SecondsFor(roster));
    }

    [Fact]
    public void The_runner_rig_applies_it_rotation_only_on_fired_and_the_battle_never_does()
    {
        var root = Layouts.RepoRoot();
        var rig = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "Runner", "RunnerRig.cs"));
        Assert.Contains("if (!_aimHeld || bones.Length == 0)", rig);
        Assert.Contains("b.Set(Quaternion.AngleAxis(b.PitchDegrees * w, Vector3.right) * b.T.rotation);", rig);
        var tick = rig.Substring(rig.IndexOf("private void TickFlick(float dt)", System.StringComparison.Ordinal));
        tick = tick.Substring(0, tick.IndexOf("private sealed class FlickBone", System.StringComparison.Ordinal));
        Assert.DoesNotContain("position", tick);
        Assert.DoesNotContain("Host", tick);
        var view = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "Runner", "RunnerLevelView.cs"));
        var fired = view.Substring(view.IndexOf("case RunnerEventKind.Fired:", System.StringComparison.Ordinal));
        fired = fired.Substring(0, fired.IndexOf("case RunnerEventKind.Hit:", System.StringComparison.Ordinal));
        Assert.Contains("m.ShotFlick();", fired);
        foreach (var f in Directory.GetFiles(Path.Combine(root, "Assets", "Survival", "Unity"), "*.cs", SearchOption.AllDirectories))
        {
            if (f.Contains(Path.DirectorySeparatorChar + "Runner" + Path.DirectorySeparatorChar))
            {
                continue;
            }

            Assert.DoesNotContain("RunnerShotFlick", File.ReadAllText(f));
        }
    }
}
