using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Survival.Domain.Enemies;
using Survival.Domain.Roster;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>
/// Deterministic mirror of Unity's FBX import for the bow hand: Blender bone space to Unity bone space is a local-X flip
/// (points (-x, y, z), quaternions (x, -y, -z, w)). The bow is a child of mixamorig:LeftHand at that converted offset, so the
/// grip centre must land within 1 cm of the closed-hand centre in rest, walk and attack.
/// </summary>
public class BonequillBowGripUnityTests
{
    private static JsonElement Grip()
    {
        var p = Path.Combine(FindRepoRoot(), BonequillMotion.DesignDir, "work", "grip_unity.json");
        return JsonDocument.Parse(File.ReadAllText(p)).RootElement;
    }

    private static float[] F(JsonElement e) => e.EnumerateArray().Select(x => (float)x.GetDouble()).ToArray();

    [Fact]
    public void Bow_is_bound_to_the_left_hand_bone_with_the_Blender_grip()
    {
        var g = Grip();
        var spec = BonequillAttack.IdleProps.Single(p => p.ObjectName == "BONEQUILL_bow_rest");
        Assert.Equal("mixamorig:LeftHand", spec.Bone);
        Assert.Equal(g.GetProperty("bone").GetString(), spec.Bone);
        Assert.NotNull(spec.BoneGrip);
        var gp = F(g.GetProperty("grip_pos_bone_m"));
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(gp[i], spec.BoneGrip!.GripPos[i], 4);
        }

        var kids = g.GetProperty("child_heads_bone_m");
        Assert.Equal(F(kids.GetProperty("LeftHandFingers1"))[1], spec.BoneGrip!.ChildAHead[1], 4);
        Assert.Equal(F(kids.GetProperty("LeftHandThumb1"))[0], spec.BoneGrip!.ChildBHead[0], 4);
    }

    [Theory]
    [InlineData("rest")]
    [InlineData("walk")]
    [InlineData("attack")]
    public void Grip_centre_sits_within_1cm_of_the_palm_centre_after_Unity_axis_conversion(string clip)
    {
        var grip = BonequillAttack.BowGrip;
        var u = BlenderBoneGrip.ToUnityPoint(grip.GripPos);
        var samples = Grip().GetProperty("hand_centre").GetProperty(clip).GetProperty("per_sample").EnumerateArray().ToArray();
        Assert.Equal(61, samples.Length);
        foreach (var s in samples)
        {
            var c = BlenderBoneGrip.ToUnityPoint(new[] { (float)s[1].GetDouble(), (float)s[2].GetDouble(), (float)s[3].GetDouble() });
            var d = Math.Sqrt(Math.Pow(u[0] - c[0], 2) + Math.Pow(u[1] - c[1], 2) + Math.Pow(u[2] - c[2], 2));
            Assert.True(d <= 0.01, $"{clip} f{s[0].GetDouble()}: grip {d * 100:0.0} cm from the closed-hand centre");
        }

        Assert.True(grip.GripToHandCentreUnity() <= 0.01f);
        // On the hand centreline: the finger chain root runs through the grip plane.
        var f1 = BlenderBoneGrip.ToUnityPoint(grip.ChildAHead);
        Assert.True(Math.Abs(f1[2] - u[2]) <= 0.002, "grip off the hand centreline");
    }

    [Fact]
    public void Rotation_conversion_matches_the_point_conversion()
    {
        var q = BonequillAttack.BowGrip.GripRot;
        var uq = BlenderBoneGrip.ToUnityRotation(q);
        foreach (var v in new[] { new[] { 0f, 0f, 1f }, new[] { 0f, 1f, 0f }, new[] { 0.3f, -0.2f, 0.9f } })
        {
            var a = BlenderBoneGrip.ToUnityPoint(Rotate(q, v));
            var b = Rotate(uq, BlenderBoneGrip.ToUnityPoint(v));
            for (var i = 0; i < 3; i++)
            {
                Assert.Equal(a[i], b[i], 5);
            }
        }

        // Bow axis (mesh z) runs along the hand's knuckle line (bone z), string (mesh +y) toward the palm root (bone -y).
        var axis = Rotate(uq, new[] { 0f, 0f, 1f });
        Assert.True(axis[2] > 0.99f);
    }

    [Fact]
    public void Finger_curl_is_keyed_in_the_walk_and_attack_FBX_and_rest_shows_the_keyed_carry()
    {
        var root = FindRepoRoot();
        var pack = Path.Combine(root, "Assets", BonequillMotion.ThemePackDir);
        foreach (var fbx in new[] { "BONEQUILL_blenderig_walk.fbx", "BONEQUILL_blenderig_attack.fbx" })
        {
            var bytes = File.ReadAllBytes(Path.Combine(pack, fbx));
            var text = Encoding.ASCII.GetString(bytes);
            foreach (var b in BonequillMotion.LeftGripBoneNames)
            {
                Assert.Contains(b, text);
            }

            Assert.Contains("AnimationCurveNode", text);
        }

        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BonequillActor.cs"));
        Assert.Contains("restAtAttackStart: true", actor);
        var driver = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlenderRigAttackDriver.cs"));
        Assert.Contains("AttachAtBoneGrip(view, root, prop.BoneGrip)", driver);
        Assert.Contains("t.SetParent(bone, false)", driver);
    }

    [Theory]
    [InlineData("rest")]
    [InlineData("walk")]
    [InlineData("attack")]
    public void Bowstring_keeps_1cm_clear_of_the_bow_hand_except_during_the_draw(string clip)
    {
        var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(FindRepoRoot(), BonequillMotion.DesignDir, "work", "string_clearance.json"))).RootElement;
        // The clearance file was computed with the same bow roll the runtime grip uses.
        var roll = doc.GetProperty("bow_roll_deg").GetDouble();
        var q = BonequillAttack.BowGrip.GripRot;
        var expected = 180.0 + roll;
        var angle = 2.0 * Math.Acos(Math.Abs(q[3])) * 180.0 / Math.PI;
        Assert.Equal(360.0 - expected, angle, 1);
        var samples = doc.GetProperty("per_sample").GetProperty(clip).EnumerateArray().ToArray();
        Assert.Equal(61, samples.Length);
        foreach (var s in samples)
        {
            var f = s[0].GetDouble();
            var drawing = s[3].GetBoolean();
            Assert.Equal(clip == "attack" && f >= 4 && f <= 17, drawing);
            if (drawing)
            {
                continue;
            }

            Assert.True(s[1].GetDouble() >= 0.01, $"{clip} f{f}: string {s[1].GetDouble() * 100:0.0} cm from hand/finger/forearm mesh");
            Assert.True(s[2].GetDouble() >= 0.01, $"{clip} f{f}: string {s[2].GetDouble() * 100:0.0} cm from hand/finger/forearm bones");
        }
    }

    private static float[] Rotate(float[] q, float[] v)
    {
        float x = q[0], y = q[1], z = q[2], w = q[3];
        float tx = 2 * (y * v[2] - z * v[1]), ty = 2 * (z * v[0] - x * v[2]), tz = 2 * (x * v[1] - y * v[0]);
        return new[] { v[0] + w * tx + (y * tz - z * ty), v[1] + w * ty + (z * tx - x * tz), v[2] + w * tz + (x * ty - y * tx) };
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Assets", "ThemePack", "fantasy_kingdom_a", "pack.json")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("repo root");
    }
}
