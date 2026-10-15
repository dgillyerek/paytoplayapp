using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>
/// Rowan's bow hand (Derek 2026-10-15: the bow floated beside the closed fist, the same bug Bonequill had). The bow is a child of
/// mixamorig:LeftHand at the Blender grip converted to Unity bone space (local-X flip: points (-x, y, z), quaternions (x, -y, -z, w)),
/// rest shows the keyed carry so the fingers curl, and the bow is rolled per frame so its string stays off his body, coat and bow arm.
/// Also covers the narrower walk track and the attack's left-foot step.
/// </summary>
public class RowanBowGripUnityTests
{
    private static string Work => Path.Combine(FindRepoRoot(), RowanMotion.DesignDir, "work", "apose_20261014");

    private static JsonElement Json(string name) => JsonDocument.Parse(File.ReadAllText(Path.Combine(Work, name))).RootElement;

    private static float[] F(JsonElement e) => e.EnumerateArray().Select(x => (float)x.GetDouble()).ToArray();

    [Fact]
    public void Bow_is_bound_to_the_left_hand_bone_with_the_Blender_grip()
    {
        var g = Json("grip_unity.json");
        var spec = RowanAttack.IdleProps.Single(p => p.ObjectName == "ROWAN_bow_rest");
        Assert.Equal("mixamorig:LeftHand", spec.Bone);
        Assert.Equal(g.GetProperty("bone").GetString(), spec.Bone);
        Assert.Same(RowanAttack.BowGrip, spec.BoneGrip);
        var gp = F(g.GetProperty("grip_pos_bone_m"));
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(gp[i], spec.BoneGrip!.GripPos[i], 4);
        }

        var kids = g.GetProperty("child_heads_bone_m");
        var a = F(kids.GetProperty("LeftHandFingers1"));
        var b = F(kids.GetProperty("LeftHandThumb1"));
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(a[i], spec.BoneGrip!.ChildAHead[i], 4);
            Assert.Equal(b[i], spec.BoneGrip!.ChildBHead[i], 4);
        }

        var hc = F(g.GetProperty("hand_centre_mean"));
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(hc[i], spec.BoneGrip!.HandCentre[i], 4);
        }
    }

    [Theory]
    [InlineData("rest")]
    [InlineData("walk")]
    [InlineData("attack")]
    public void Grip_centre_sits_within_1cm_of_the_closed_hand_centre_after_Unity_axis_conversion(string clip)
    {
        var grip = RowanAttack.BowGrip;
        var u = BlenderBoneGrip.ToUnityPoint(grip.GripPos);
        var samples = Json("grip_unity.json").GetProperty("hand_centre").GetProperty(clip).GetProperty("per_sample").EnumerateArray().ToArray();
        Assert.Equal(61, samples.Length);
        foreach (var s in samples)
        {
            var c = BlenderBoneGrip.ToUnityPoint(new[] { (float)s[1].GetDouble(), (float)s[2].GetDouble(), (float)s[3].GetDouble() });
            var d = Math.Sqrt(Math.Pow(u[0] - c[0], 2) + Math.Pow(u[1] - c[1], 2) + Math.Pow(u[2] - c[2], 2));
            Assert.True(d <= 0.01, $"{clip} f{s[0].GetDouble()}: grip {d * 100:0.0} cm from the closed-hand centre");
        }

        Assert.True(grip.GripToHandCentreUnity() <= 0.01f);
        var f1 = BlenderBoneGrip.ToUnityPoint(grip.ChildAHead);
        Assert.True(Math.Abs(f1[2] - u[2]) <= 0.002, "grip off the hand centreline");
    }

    [Theory]
    [InlineData("rest")]
    [InlineData("walk")]
    [InlineData("attack")]
    public void Bowstring_keeps_2cm_clear_of_body_coat_and_bow_arm_with_string_away_when_carried(string clip)
    {
        var doc = Json("string_clearance.json");
        var grip = RowanAttack.BowGrip;
        Assert.Equal(doc.GetProperty("roll_rest_deg").GetDouble(), grip.RollRestDeg, 3);
        Assert.Equal(doc.GetProperty("roll_draw_deg").GetDouble(), grip.RollDrawDeg, 3);
        var keys = F(doc.GetProperty("roll_keys"));
        Assert.Equal(keys, grip.RollKeys);
        var samples = doc.GetProperty("per_sample").GetProperty(clip).EnumerateArray().ToArray();
        Assert.Equal(61, samples.Length);
        foreach (var s in samples)
        {
            var f = (float)s[0].GetDouble();
            Assert.True(Math.Abs(s[5].GetDouble() - grip.RollDeg(clip == "attack", f)) < 0.05, $"{clip} f{f}: roll curve differs from Blender");
            // Only the draw hand is exempt, and only while it holds the string (nock f8 to release f14).
            Assert.Equal(clip == "attack" && f >= 8 && f <= 14, s[4].GetBoolean());
            Assert.True(s[1].GetDouble() >= 0.02, $"{clip} f{f}: string {s[1].GetDouble() * 100:0.0} cm from the body/bow hand/forearm");
            Assert.True(s[2].GetDouble() >= 0.02, $"{clip} f{f}: string {s[2].GetDouble() * 100:0.0} cm from the coat");
            Assert.True(s[3].GetDouble() >= 0.02, $"{clip} f{f}: string {s[3].GetDouble() * 100:0.0} cm from the bow-arm bones");
            if (clip != "attack" || f <= 2 || f >= 24)
            {
                Assert.True(s[6].GetDouble() > 0.5, $"{clip} f{f}: carried string not on the side away from the body");
            }
            else if (f >= 8 && f <= 17)
            {
                Assert.True(s[6].GetDouble() < 0, $"{clip} f{f}: string should face him from nock to release");
            }
        }
    }

    [Fact]
    public void Roll_eases_smoothly_and_only_spins_about_the_bow_long_axis()
    {
        var grip = RowanAttack.BowGrip;
        var prev = grip.RollDeg(true, 0f);
        for (var f = 0.25f; f <= 30f; f += 0.25f)
        {
            var r = grip.RollDeg(true, f);
            Assert.True(Math.Abs(r - prev) <= 12f, $"roll jumps {Math.Abs(r - prev):0.0} deg at f{f}");
            prev = r;
            var axis = Rotate(grip.UnityRotation(r), new[] { 0f, 0f, 1f });
            Assert.True(axis[2] > 0.999f);
        }

        Assert.Equal(grip.RollRestDeg, grip.RollDeg(false, 12f));
        Assert.Equal(grip.RollDrawDeg, grip.RollDeg(true, 14f));
    }

    [Fact]
    public void Finger_curl_is_keyed_and_rest_shows_the_keyed_carry_with_the_bow_on_the_hand_bone()
    {
        var root = FindRepoRoot();
        var pack = Path.Combine(root, "Assets", RowanMotion.ThemePackDir);
        foreach (var fbx in new[] { RowanMotion.WalkFileName, RowanAttack.FileName })
        {
            var text = Encoding.ASCII.GetString(File.ReadAllBytes(Path.Combine(pack, fbx)));
            foreach (var b in RowanMotion.LeftFingerBoneNames)
            {
                Assert.Contains(b, text);
            }

            Assert.Contains("AnimationCurveNode", text);
        }

        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "RowanActor.cs"));
        Assert.Contains("restAtAttackStart: true", actor);
        var driver = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlenderRigAttackDriver.cs"));
        Assert.Contains("AttachAtBoneGrip(view, root, prop.BoneGrip)", driver);
        Assert.Contains("t.SetParent(bone, false)", driver);
    }

    [Fact]
    public void Walk_track_is_narrowed_and_attack_steps_the_left_foot_forward_without_sliding()
    {
        var a = Json("anim.json");
        var src = File.ReadAllText(Path.Combine(Work, "anim.py"));
        Assert.Contains("NARROW=float(os.environ.get('NARROW','-2.9'))", src);
        var steps = a.GetProperty("attack_step").EnumerateArray().ToArray();
        Assert.Equal(31, steps.Length);
        var r0 = F(steps[0].GetProperty("R"));
        var l0 = F(steps[0].GetProperty("L"));
        var h0 = F(steps[0].GetProperty("hips"));
        // f0 and f30 are rest
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(l0[i], F(steps[30].GetProperty("L"))[i], 3);
            Assert.Equal(r0[i], F(steps[30].GetProperty("R"))[i], 3);
            Assert.Equal(h0[i], F(steps[30].GetProperty("hips"))[i], 3);
        }

        foreach (var s in steps)
        {
            var f = s.GetProperty("f").GetInt32();
            var r = F(s.GetProperty("R"));
            var l = F(s.GetProperty("L"));
            // planted right foot never slides
            Assert.True(Math.Sqrt(Math.Pow(r[0] - r0[0], 2) + Math.Pow(r[1] - r0[1], 2) + Math.Pow(r[2] - r0[2], 2)) < 0.002, $"f{f}: right foot slid");
            if (f >= 8 && f <= 19)
            {
                // planted forward (Blender -Y is toward the target) 25-35 cm before the f14 release, foot on the ground, weight shifted onto it
                var fwd = l0[1] - l[1];
                Assert.InRange(fwd, 0.25f, 0.35f);
                Assert.True(Math.Abs(l[2] - l0[2]) < 0.002, $"f{f}: front foot not on the ground");
            }

            if (f >= 10 && f <= 19)
            {
                var hips = F(s.GetProperty("hips"));
                Assert.True(h0[1] - hips[1] >= 0.08f, $"f{f}: weight not shifted onto the front foot");
            }
        }

        Assert.True(F(steps[5].GetProperty("L"))[2] - l0[2] > 0.04f, "left foot should lift early");
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
