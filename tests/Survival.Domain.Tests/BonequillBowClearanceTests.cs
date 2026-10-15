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
            }
        }
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
