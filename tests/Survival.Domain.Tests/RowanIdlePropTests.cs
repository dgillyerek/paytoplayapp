using System.Security.Cryptography;
using System.Text.Json;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

/// <summary>Rowan rest/walk prop (Design NOTE: bow on mixamorig:RightHand at rest and in the walk). Hidden while the attack prop is active.</summary>
public sealed class RowanIdlePropTests
{
    private const string PropsDir = "design/survival-theme-a-fantasy/heroes/anim/rowan/blender_rig_nocape_20261008/props";

    [Fact]
    public void Rowan_idle_prop_rides_right_hand_per_design_note()
    {
        var props = RowanRemeshMotion.IdleProps;
        Assert.Single(props);
        var prop = props[0];
        Assert.Equal("mixamorig:RightHand", prop.Bone);
        Assert.Contains(prop.Bone, RowanRemeshMotion.BoneNames);
        Assert.Equal("ROWAN_bow_rest", prop.ObjectName);
        Assert.Equal("ROWAN_bow.fbx", prop.PropFileName);
        Assert.Equal(7, prop.DesignRest.Length);
        Assert.Equal(3, prop.PropExtent.Length);
        var q = prop.DesignRest;
        var len = Math.Sqrt((q[3] * q[3]) + (q[4] * q[4]) + (q[5] * q[5]) + (q[6] * q[6]));
        Assert.InRange(len, 0.999, 1.001);
        Assert.All(prop.PropExtent, e => Assert.True(e > 0f));

        using var meta = JsonDocument.Parse(File.ReadAllText(Path.Combine(FindRepoRoot(), PropsDir, "ROWAN_bow_meta.json")));
        Assert.Equal(prop.Bone, meta.RootElement.GetProperty("parent_bone").GetString());

        var track = prop.ToTrack();
        Assert.Equal(AttackTrackKind.Held, track.Kind);
        Assert.Equal(prop.Bone, track.Bone);
        Assert.Single(track.Frames);
        Assert.True(track.Frames[0].Visible);
        Assert.True(track.HasMesh);
    }

    [Fact]
    public void Rowan_idle_prop_rest_matches_design_rest_world_in_unity_axes()
    {
        var prop = RowanRemeshMotion.IdleProps[0];
        using var meta = JsonDocument.Parse(File.ReadAllText(Path.Combine(FindRepoRoot(), PropsDir, "ROWAN_bow_meta.json")));
        var rows = meta.RootElement.GetProperty("rest_world_blender");
        var m = new double[3, 4];
        for (var r = 0; r < 3; r++)
        {
            for (var c = 0; c < 4; c++)
            {
                m[r, c] = rows[r][c].GetDouble();
            }
        }

        // Blender (x, y, z) -> Unity (-x, z, -y); rotation R_u = M R_b M^T with scale stripped.
        Assert.Equal(-m[0, 3], prop.DesignRest[0], 3);
        Assert.Equal(m[2, 3], prop.DesignRest[1], 3);
        Assert.Equal(-m[1, 3], prop.DesignRest[2], 3);

        var rb = new double[3, 3];
        for (var c = 0; c < 3; c++)
        {
            var n = Math.Sqrt((m[0, c] * m[0, c]) + (m[1, c] * m[1, c]) + (m[2, c] * m[2, c]));
            for (var r = 0; r < 3; r++)
            {
                rb[r, c] = m[r, c] / n;
            }
        }

        var mm = new double[,] { { -1, 0, 0 }, { 0, 0, 1 }, { 0, -1, 0 } };
        var ru = Mul(Mul(mm, rb), Transpose(mm));
        var expected = ToQuat(ru);
        var dot = 0.0;
        for (var i = 0; i < 4; i++)
        {
            dot += expected[i] * prop.DesignRest[3 + i];
        }

        Assert.InRange(Math.Abs(dot), 0.9999, 1.0001);
    }

    [Fact]
    public void Rowan_idle_prop_file_matches_design_props_md5()
    {
        var root = FindRepoRoot();
        var prop = RowanRemeshMotion.IdleProps[0];
        Assert.Equal("80758fce88e2a9313a34d171e334ae00", prop.PropFileMd5);
        Assert.Equal(prop.PropFileMd5, Md5(Path.Combine(root, "Assets", RowanRemeshMotion.ThemePackDir, prop.PropFileName)));
        Assert.Equal(prop.PropFileMd5, Md5(Path.Combine(root, PropsDir, "ROWAN_bow.fbx")));
        Assert.True(File.Exists(Path.Combine(root, "Assets", RowanRemeshMotion.ThemePackDir, prop.PropFileName + ".meta")));
    }

    [Fact]
    public void Rowan_attack_keeps_its_own_prop_track()
    {
        // Attack switches to the baked bow_grip track (world-space, no bone) for the same bow mesh.
        var bow = Assert.Single(RowanAttack.Spec.Tracks, t => t.ObjectName == "ROWAN_bow");
        Assert.Equal(AttackTrackKind.Held, bow.Kind);
        Assert.Equal("ROWAN_bow.fbx", bow.PropFileName);
        Assert.Equal(string.Empty, bow.Bone);
        Assert.True(bow.Frames.Length > 1);
        Assert.NotEqual(bow.ObjectName, RowanRemeshMotion.IdleProps[0].ObjectName);
    }

    [Fact]
    public void Rowan_idle_prop_is_bound_on_the_bone_and_hidden_during_attack()
    {
        var unity = Path.Combine(FindRepoRoot(), "Assets", "Survival", "Unity");
        var actor = File.ReadAllText(Path.Combine(unity, "RowanRemeshActor.cs"));
        Assert.Contains("RowanRemeshMotion.IdleProps", actor);

        var player = File.ReadAllText(Path.Combine(unity, "BlenderRigPlayer.cs"));
        Assert.Contains("_attack.BindIdleProps(_idleProps)", player);
        var hide = player.IndexOf("_attack.SetIdlePropsVisible(false)", StringComparison.Ordinal);
        var activate = player.IndexOf("_attack.Activate(SampleAttackFrame)", StringComparison.Ordinal);
        Assert.True(hide >= 0 && activate > hide, "idle prop must hide before the attack prop shows");
        var deactivate = player.IndexOf("_attack?.Deactivate();", StringComparison.Ordinal);
        var show = player.IndexOf("_attack?.SetIdlePropsVisible(true)", StringComparison.Ordinal);
        Assert.True(deactivate >= 0 && show > deactivate, "idle prop must come back after the attack prop hides");

        var driver = File.ReadAllText(Path.Combine(unity, "BlenderRigAttackDriver.cs"));
        Assert.Contains("public void BindIdleProps(BlenderRigIdlePropSpec[] props)", driver);
        Assert.Contains("public void SetIdlePropsVisible(bool visible)", driver);
        Assert.Contains("root.transform.SetParent(view.Bone, true)", driver);
        Assert.Contains("_idleViews.Clear()", driver);
    }

    private static double[,] Mul(double[,] a, double[,] b)
    {
        var o = new double[3, 3];
        for (var r = 0; r < 3; r++)
        {
            for (var c = 0; c < 3; c++)
            {
                for (var k = 0; k < 3; k++)
                {
                    o[r, c] += a[r, k] * b[k, c];
                }
            }
        }

        return o;
    }

    private static double[,] Transpose(double[,] a)
    {
        var o = new double[3, 3];
        for (var r = 0; r < 3; r++)
        {
            for (var c = 0; c < 3; c++)
            {
                o[r, c] = a[c, r];
            }
        }

        return o;
    }

    // Returns (x, y, z, w).
    private static double[] ToQuat(double[,] m)
    {
        var tr = m[0, 0] + m[1, 1] + m[2, 2];
        double x, y, z, w;
        if (tr > 0)
        {
            var s = Math.Sqrt(tr + 1.0) * 2;
            w = 0.25 * s;
            x = (m[2, 1] - m[1, 2]) / s;
            y = (m[0, 2] - m[2, 0]) / s;
            z = (m[1, 0] - m[0, 1]) / s;
        }
        else if (m[0, 0] > m[1, 1] && m[0, 0] > m[2, 2])
        {
            var s = Math.Sqrt(1.0 + m[0, 0] - m[1, 1] - m[2, 2]) * 2;
            w = (m[2, 1] - m[1, 2]) / s;
            x = 0.25 * s;
            y = (m[0, 1] + m[1, 0]) / s;
            z = (m[0, 2] + m[2, 0]) / s;
        }
        else if (m[1, 1] > m[2, 2])
        {
            var s = Math.Sqrt(1.0 + m[1, 1] - m[0, 0] - m[2, 2]) * 2;
            w = (m[0, 2] - m[2, 0]) / s;
            x = (m[0, 1] + m[1, 0]) / s;
            y = 0.25 * s;
            z = (m[1, 2] + m[2, 1]) / s;
        }
        else
        {
            var s = Math.Sqrt(1.0 + m[2, 2] - m[0, 0] - m[1, 1]) * 2;
            w = (m[1, 0] - m[0, 1]) / s;
            x = (m[0, 2] + m[2, 0]) / s;
            y = (m[1, 2] + m[2, 1]) / s;
            z = 0.25 * s;
        }

        return new[] { x, y, z, w };
    }

    private static string Md5(string path)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(md5.ComputeHash(stream)).ToLowerInvariant();
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "Assets")) && Directory.Exists(Path.Combine(dir.FullName, "tests")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
