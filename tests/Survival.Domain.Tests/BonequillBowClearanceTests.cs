using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Survival.Domain.Enemies;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>
/// The bow must never pass through Bonequill's body (shoulder, shoulder guard, leg, quiver) or cloak in any clip. The Blender check
/// (design .../work/bowpen.py) samples rest, walk and attack at every frame and half frame and writes work/bow_clearance.json together
/// with the MD5s of the FBXs it measured; this test pins that report to the shipped FBXs and requires zero penetration everywhere.
/// </summary>
public sealed class BonequillBowClearanceTests
{
    [Fact]
    public void Bow_clears_body_and_cloak_on_every_frame_and_half_frame()
    {
        var root = FindRepoRoot();
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, BonequillMotion.DesignDir, "work", "bow_clearance.json")));
        var r = doc.RootElement;

        var md5 = r.GetProperty("fbx_md5");
        Assert.Equal(BonequillMotion.RestMd5, md5.GetProperty("rest").GetString());
        Assert.Equal(BonequillMotion.WalkMd5, md5.GetProperty("walk").GetString());
        Assert.Equal(BonequillAttack.FileMd5, md5.GetProperty("attack").GetString());

        foreach (var clip in new[] { "rest", "walk", "attack" })
        {
            var samples = r.GetProperty("per_sample").GetProperty(clip).EnumerateArray().ToArray();
            Assert.Equal(61, samples.Length); // f0..f30 in half-frame steps
            foreach (var s in samples)
            {
                var frame = s[0].GetDouble();
                Assert.True(s[1].GetInt32() == 0, $"{clip} f{frame}: {s[1].GetInt32()} bow vertices inside the body");
                Assert.True(s[2].GetInt32() == 0, $"{clip} f{frame}: {s[2].GetInt32()} bow edges through the cloak");
                Assert.True(s[3].GetDouble() > 0.0, $"{clip} f{frame}: bow clearance {s[3].GetDouble()} m");
                Assert.True(s[4].GetInt32() == 0, $"{clip} f{frame}: {s[4].GetInt32()} hand/finger vertices inside the bow");
                Assert.True(s[5].GetInt32() >= 270, $"{clip} f{frame}: grip axis wrapped only {s[5].GetInt32()} deg by palm/fingers/thumb");
                Assert.True(s[6].GetDouble() >= 0.0255, $"{clip} f{frame}: hand {s[6].GetDouble()} m from the grip axis (grip radius 2.4 cm)");
            }
        }
    }

    [Fact]
    public void Bow_hand_is_a_closed_grip_on_finger_bones_without_stretch()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(FindRepoRoot(), BonequillMotion.DesignDir, "work", "bow_clearance.json")));
        var g = doc.RootElement.GetProperty("grip");
        Assert.True(Math.Abs(g.GetProperty("finger_len_change_pct").GetDouble()) <= 3.0, "finger length must stay within 3% of the Meshy hand");
        Assert.True(g.GetProperty("hole_radius").GetDouble() >= 0.0255, "grip hole must clear the 2.4 cm bow grip");
        Assert.True(g.GetProperty("grip_coverage_deg").GetInt32() >= 270, "hand must close most of the way around the grip");
        Assert.Equal(5, BonequillMotion.LeftGripBoneNames.Length);
        foreach (var b in BonequillMotion.LeftGripBoneNames)
        {
            Assert.Contains(b, BonequillMotion.BoneNames);
        }
    }

    [Fact]
    public void Idle_bow_is_bound_to_the_hand_in_the_keyed_attack_pose_not_the_bind_pose()
    {
        var src = File.ReadAllText(Path.Combine(FindRepoRoot(), "Assets", "Survival", "Unity", "BlenderRigPlayer.cs"));
        int bind = src.IndexOf("_attack.BindIdleProps(_idleProps)", StringComparison.Ordinal);
        int sample = src.LastIndexOf("SampleAttackFrame(BlenderRigAttackSpec.FirstFrame)", bind, StringComparison.Ordinal);
        Assert.True(bind > 0 && sample > 0 && src.IndexOf("BindIdlePropsAtAttackRest", StringComparison.Ordinal) > 0,
            "idle props must be bound after the attack clip is sampled at frame 0, or the bow floats beside the palm");
        Assert.Equal(1, System.Text.RegularExpressions.Regex.Matches(src, @"_attack\.BindIdleProps\(").Count);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Assets", "ThemePack", "fantasy_kingdom_a", "pack.json")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("repo root");
    }
}
