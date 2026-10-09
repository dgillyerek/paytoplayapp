using System.Security.Cryptography;
using System.Text.Json;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

/// <summary>Lyra rest/walk prop (Design NOTE: staff in RightHand the whole time). Hidden while the attack prop is active.</summary>
public sealed class LyraIdlePropTests
{
    private const string PropsDir = "design/survival-theme-a-fantasy/heroes/anim/lyra/blender_rig_tighten_20261008/props";

    [Fact]
    public void Lyra_idle_prop_rides_right_hand_per_design_note()
    {
        var props = LyraMotion.IdleProps;
        Assert.Single(props);
        var prop = props[0];
        Assert.Equal("mixamorig:RightHand", prop.Bone);
        Assert.Contains(prop.Bone, LyraMotion.BoneNames);
        Assert.Equal("LYRA_staff_rest", prop.ObjectName);
        Assert.Equal("LYRA_staff_rest.fbx", prop.PropFileName);
        Assert.Equal(7, prop.DesignRest.Length);
        Assert.Equal(3, prop.PropExtent.Length);
        var q = prop.DesignRest;
        var len = Math.Sqrt((q[3] * q[3]) + (q[4] * q[4]) + (q[5] * q[5]) + (q[6] * q[6]));
        Assert.InRange(len, 0.999, 1.001);
        Assert.All(prop.PropExtent, e => Assert.True(e > 0f));

        using var meta = JsonDocument.Parse(File.ReadAllText(Path.Combine(FindRepoRoot(), PropsDir, "LYRA_staff_meta.json")));
        Assert.Equal(prop.Bone, meta.RootElement.GetProperty("parent_bone").GetString());

        var track = prop.ToTrack();
        Assert.Equal(AttackTrackKind.Held, track.Kind);
        Assert.Equal(prop.Bone, track.Bone);
        Assert.Single(track.Frames);
        Assert.True(track.Frames[0].Visible);
        Assert.True(track.HasMesh);
    }

    [Fact]
    public void Lyra_idle_prop_rest_matches_design_rest_world_in_unity_axes()
    {
        var prop = LyraMotion.IdleProps[0];
        using var meta = JsonDocument.Parse(File.ReadAllText(Path.Combine(FindRepoRoot(), PropsDir, "LYRA_staff_meta.json")));
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
    public void Lyra_idle_prop_file_matches_design_props_md5()
    {
        var root = FindRepoRoot();
        var prop = LyraMotion.IdleProps[0];
        Assert.Equal("69872fb5cf0f1a515b0e17ca884bccc1", prop.PropFileMd5);
        Assert.Equal(prop.PropFileMd5, Md5(Path.Combine(root, "Assets", LyraMotion.ThemePackDir, prop.PropFileName)));
        Assert.Equal(prop.PropFileMd5, Md5(Path.Combine(root, PropsDir, "LYRA_staff.fbx")));
        Assert.True(File.Exists(Path.Combine(root, "Assets", LyraMotion.ThemePackDir, prop.PropFileName + ".meta")));
    }

    [Fact]
    public void Lyra_attack_keeps_its_own_prop_track()
    {
        var idle = LyraMotion.IdleProps[0];
        Assert.DoesNotContain(idle.PropFileName, LyraAttack.PropFileNames);
        Assert.Contains("LYRA_staff.fbx", LyraAttack.PropFileNames);
        Assert.Contains(LyraAttack.Spec.Tracks, t => t.PropFileName == "LYRA_staff.fbx");
        Assert.DoesNotContain(LyraAttack.Spec.Tracks, t => t.PropFileName == idle.PropFileName);
    }

    [Fact]
    public void Lyra_one_textured_staff_serves_rest_walk_and_attack()
    {
        // The attack's own LYRA_staff.fbx imported with an untextured material (white staff in Game view).
        // The rest/walk staff rides RightHand under the rig and gets the rig's bound atlas, so it stands in.
        var idle = LyraMotion.IdleProps[0];
        Assert.Equal("LYRA_staff", idle.AttackTrackName);
        Assert.True(idle.StandsInForAttackTrack);
        var held = Assert.Single(LyraAttack.Spec.Tracks, t => t.ObjectName == idle.AttackTrackName);
        Assert.Equal(AttackTrackKind.Held, held.Kind);
        Assert.Equal(idle.Bone, held.Bone);
        // Same grip: the attack's frame 0 is the rest pose, so its staff key equals the idle Design rest.
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(idle.DesignRest[i], new[] { held.Frames[0].X, held.Frames[0].Y, held.Frames[0].Z }[i], 4);
        }

        var driver = File.ReadAllText(Path.Combine(FindRepoRoot(), "Assets", "Survival", "Unity", "BlenderRigAttackDriver.cs"));
        Assert.Contains("IdleStandIn(track.ObjectName)", driver);
        Assert.Contains("if (!visible && view.StandsInForAttack)", driver);
    }

    [Fact]
    public void Lyra_idle_prop_is_bound_on_the_bone_and_hidden_during_attack()
    {
        var unity = Path.Combine(FindRepoRoot(), "Assets", "Survival", "Unity");
        var actor = File.ReadAllText(Path.Combine(unity, "LyraActor.cs"));
        Assert.Contains("LyraMotion.IdleProps", actor);

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
