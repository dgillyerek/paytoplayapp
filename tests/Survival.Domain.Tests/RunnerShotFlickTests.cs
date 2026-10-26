using System.Numerics;
using Survival.Domain.Heroes;
using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek 2026-10-24: "add is some sort of firing motion for heroes, just make it subtle", then "add a better firing
/// animation. for a staff it should just shift backwards almost like firing a shotgun". Each shot adds a readable but
/// controlled, rotation-only recoil on the held aim pose's arm / hand / shoulder / upper-spine bones: a fast snap, eased
/// back to the hold over about 0.3 s; the root, hips and legs never move.
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
    public void Each_shooter_recoils_clearly_but_controlled_on_real_upper_body_bones_only(string roster)
    {
        var bones = RunnerShotFlick.BonesFor(roster);
        Assert.NotEmpty(bones);
        var total = 0f;
        foreach (var b in bones)
        {
            Assert.Contains(b.Bone, BonesOf(roster));
            Assert.DoesNotContain(b.Bone, RunnerShotFlick.Forbidden);
            Assert.InRange(System.Math.Abs(b.PitchDegrees), 2f, 8f);
            total += System.Math.Abs(b.PitchDegrees);
        }

        // Readable (well past the old subtle 3-10 degree kick) but controlled, even summed over every kicked bone.
        Assert.InRange(total, 12f, RunnerShotFlick.MaxDegrees + 1e-4f);
        Assert.InRange(RunnerShotFlick.SecondsFor(roster), RunnerShotFlick.RiseSeconds + 0.25f, RunnerShotFlick.RiseSeconds + 0.32f);
        Assert.InRange(RunnerShotFlick.RiseSeconds, 0.04f, 0.06f);
    }

    [Fact]
    public void Lyras_staff_shot_rocks_her_upper_body_back_like_a_shotgun()
    {
        var bones = RunnerShotFlick.Lyra;

        // Every bone pitches back (negative), none forward: the whole upper body and staff shift backwards.
        Assert.All(bones, b => Assert.True(b.PitchDegrees < 0f, b.Bone));

        // The spine chain rocks chest and shoulders back 10-15 degrees; the staff hand adds the kick toward her shoulder.
        var spine = bones.Where(b => RunnerShotFlick.LyraSpine.Contains(b.Bone)).Sum(b => -b.PitchDegrees);
        Assert.InRange(spine, 10f, 15f);
        Assert.Contains(bones, b => b.Bone == "mixamorig:RightHand" && b.PitchDegrees < 0f);

        // Her left hand rides the shaft about 61 cm below the right: the staff hand's own turn stays small enough that the
        // grip slips only a few cm at the peak, the rest of the recoil carries both hands together.
        var hand = -bones.Single(b => b.Bone == "mixamorig:RightHand").PitchDegrees;
        Assert.True(0.61f * System.Math.Sin(hand * System.Math.PI / 180.0) < 0.045, "grip slip at peak");

        // Bones are ordered parent before child so the chain compounds.
        var order = LyraMotion.BoneNames.ToList();
        Assert.Equal(bones.Select(b => order.IndexOf(b.Bone)).OrderBy(i => i), bones.Select(b => order.IndexOf(b.Bone)));
    }

    [Fact]
    public void Oakenshields_arm_snaps_toward_the_throw_and_rowans_bow_and_string_hands_kick_back()
    {
        Assert.All(RunnerShotFlick.Oakenshield, b => Assert.True(b.PitchDegrees > 0f, b.Bone));
        Assert.Contains(RunnerShotFlick.Oakenshield, b => b.Bone == "mixamorig:RightShoulder");
        Assert.True(RunnerShotFlick.Oakenshield.Where(b => b.Bone.StartsWith("mixamorig:Right", System.StringComparison.Ordinal)).Sum(b => b.PitchDegrees) >= 12f);
        Assert.All(RunnerShotFlick.Rowan, b => Assert.True(b.PitchDegrees < 0f, b.Bone));
        Assert.True(RunnerShotFlick.Rowan.Where(b => b.Bone.StartsWith("mixamorig:Left", System.StringComparison.Ordinal)).Sum(b => -b.PitchDegrees) >= 8f, "bow arm recoil");
        Assert.True(RunnerShotFlick.Rowan.Where(b => b.Bone.StartsWith("mixamorig:Right", System.StringComparison.Ordinal)).Sum(b => -b.PitchDegrees) >= 8f, "string hand release");
    }

    [Theory]
    [MemberData(nameof(Shooters))]
    public void A_shot_snaps_to_its_peak_on_the_shot_then_eases_back_to_the_hold_and_the_root_never_moves(string roster)
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
        var settledBy = -1f;
        for (var i = 0; i < 40; i++)
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

            if (settledBy < 0f && w == 0f)
            {
                settledBy = t;
            }

            // The recoil is rotation only: nothing in it offsets the root forward / back.
            Assert.Equal(Vector3.Zero, root);
        }

        Assert.InRange(peak, 12f, RunnerShotFlick.MaxDegrees);
        Assert.InRange(peakAt, RunnerShotFlick.RiseSeconds - Dt - 1e-4f, RunnerShotFlick.RiseSeconds + Dt + 1e-4f);

        // Back exactly on the hold about 0.3 s after the peak, and it stays there.
        Assert.InRange(settledBy, RunnerShotFlick.RiseSeconds + 0.24f, RunnerShotFlick.SecondsFor(roster) + Dt + 1e-4f);
        Assert.Equal(0f, state.Weight);
    }

    [Fact]
    public void Fast_fire_never_stacks_past_one_kick_and_never_pops()
    {
        // Faster than one recoil lasts (a maxed attack-speed squad): the weight never passes one peak, never pops.
        var state = new RunnerShotFlickState(RunnerShotFlick.SecondsFor("Lyra"));
        var last = 0f;
        for (var i = 0; i < 600; i++)
        {
            if (i % 9 == 0)
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
        Assert.Equal(0f, RunnerShotFlick.Weight(0f, 0.35f));
        Assert.Equal(1f, RunnerShotFlick.Weight(RunnerShotFlick.RiseSeconds, 0.35f), 4);
        Assert.Equal(0f, RunnerShotFlick.Weight(0.35f, 0.35f));
        Assert.True(RunnerShotFlick.Weight(0.34f, 0.35f) < 0.01f, "eases out into the hold");
        Assert.Equal(0f, RunnerShotFlick.Weight(-0.1f, 0.35f));
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
