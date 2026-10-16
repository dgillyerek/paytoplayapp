using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Survival.Domain.Enemies;
using Survival.Domain.Roster;
using Xunit;
using Xunit.Abstractions;

namespace Survival.Domain.Tests;

/// <summary>
/// Replays the baked Bonequill attack (60 Hz samples exported from Blender: cape chain targets following their body anchors
/// plus the body bones for the colliders) through <see cref="ClothSpringSolver"/> and checks the cape settles after the shot.
/// </summary>
public sealed class BonequillCapeSettleTests
{
    private readonly ITestOutputHelper _out;

    public BonequillCapeSettleTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public void Cape_settles_smoothly_within_half_a_second_of_the_shot()
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, BonequillMotion.DesignDir, "work", "cape_attack_sim.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var samples = doc.RootElement.GetProperty("samples").EnumerateArray().ToArray();
        var release = doc.RootElement.GetProperty("release_frame").GetInt32();
        var releaseIndex = Array.FindIndex(samples, s => s.GetProperty("frame").GetDouble() >= release);
        var stopIndex = Array.FindIndex(samples, x => x.GetProperty("frame").GetDouble() >= 30.0); // body back at rest (0.53 s after the loose)
        var settleIndex = stopIndex + 15;                                                            // +0.25 s
        var reversals = 0;
        var colliderSpecs = BonequillCape.Spec.Colliders;

        var worstJitter = 0f;
        var worstLateSpeed = 0f;
        var worstLateDev = 0f;
        foreach (var chain in BonequillCape.Chains)
        {
            var solver = new ClothSpringSolver(chain, 1f);
            var n = chain.JointCount;
            Vector3[] Targets(JsonElement s)
            {
                var a = s.GetProperty("targets").GetProperty(chain.Name).EnumerateArray().Select(e => e.GetSingle()).ToArray();
                return Enumerable.Range(0, n).Select(i => new Vector3(a[i * 3], a[(i * 3) + 1], a[(i * 3) + 2])).ToArray();
            }

            solver.Bind(Targets(samples[0]), Colliders(samples[0], colliderSpecs));
            var rel = new List<Vector3[]>();
            foreach (var s in samples)
            {
                var t = Targets(s);
                solver.Step(1f / 60f, t, Colliders(s, colliderSpecs), new Vector3(0, 0, -1), Vector3.Zero);
                rel.Add(Enumerable.Range(0, n).Select(i => solver[i] - t[i]).ToArray());
            }

            // jitter: frame-to-frame acceleration of the cloth relative to its animated rest (m/s^2), after the loose
            for (var k = releaseIndex + 1; k < rel.Count - 1; k++)
            {
                for (var i = 1; i < n; i++)
                {
                    var acc = (rel[k + 1][i] - (2 * rel[k][i]) + rel[k - 1][i]).Length() * 3600f;
                    worstJitter = Math.Max(worstJitter, acc);
                }
            }

            for (var k = settleIndex; k < rel.Count; k++)
            {
                for (var i = 1; i < n; i++)
                {
                    worstLateSpeed = Math.Max(worstLateSpeed, (rel[k][i] - rel[k - 1][i]).Length() * 60f);
                    worstLateDev = Math.Max(worstLateDev, rel[k][i].Length());
                }
            }
        }

        _out.WriteLine($"shake reversals after the loose {reversals}");
        _out.WriteLine($"worst post-release jitter {worstJitter:F2} m/s^2, worst relative speed from body-stop +0.25 s {worstLateSpeed:F3} m/s, worst deviation {worstLateDev:F3} m");
        Assert.True(worstLateSpeed < 0.1f, $"cape still moving {worstLateSpeed:F3} m/s 0.25 s after the body stops");
        Assert.True(reversals <= 2, $"cape shakes: {reversals} velocity reversals after the loose");
        Assert.True(worstJitter < 40f, $"cape jitter {worstJitter:F2} m/s^2");
    }

    private static ClothCollider[] Colliders(JsonElement s, ClothColliderSpec[] specs)
    {
        var bones = s.GetProperty("bones");
        Vector3 Get(string bone, int off)
        {
            var a = bones.GetProperty(bone).EnumerateArray().Select(e => e.GetSingle()).ToArray();
            return new Vector3(a[off], a[off + 1], a[off + 2]);
        }

        return specs.Select(c =>
        {
            var head = Get(c.Bone, 0);
            var tail = c.Kind == ClothColliderKind.Sphere ? head : Get(c.TailBone, 0);
            if (c.Kind == ClothColliderKind.Capsule && c.ExtendMetres > 0f)
            {
                tail += Vector3.Normalize(Get(c.TailBone, 3) - Get(c.TailBone, 0)) * c.ExtendMetres;
            }

            return new ClothCollider(head, tail, c.RadiusMetres);
        }).ToArray();
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
