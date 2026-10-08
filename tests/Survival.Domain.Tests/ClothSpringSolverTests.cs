using System.Numerics;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

/// <summary>Pure-math checks of the runtime cloth spring solver (Unity is not needed for these).</summary>
public sealed class ClothSpringSolverTests
{
    private static readonly Vector3 Down = new(0, -1, 0);

    // 3-bone strip hanging 0.45 m from (0, 1, 0.2), NOTE.md front-strip values.
    private static ClothChainSpec Strip(float limit = 60f) => new(
        "strip", "F", new[] { "s_01", "s_02", "s_03" }, "mixamorig:Spine", 0.45f, 100,
        0.35f, 0.4f, 0.45f, 0.4f, 0.05f, limit,
        new[] { 0f, 1f, 0.2f, 0f, 0.85f, 0.2f, 0f, 0.7f, 0.2f, 0f, 0.55f, 0.2f });

    private static Vector3[] Rest(ClothChainSpec c) => Enumerable.Range(0, c.JointCount).Select(c.DesignJoint).ToArray();

    private static Vector3[] Rotated(Vector3[] joints, float degrees)
    {
        var q = Quaternion.CreateFromAxisAngle(Vector3.UnitY, degrees * MathF.PI / 180f);
        return joints.Select(j => Vector3.Transform(j, q)).ToArray();
    }

    [Fact]
    public void Reset_snaps_to_the_animated_rest_pose_exactly()
    {
        var c = Strip();
        var s = new ClothSpringSolver(c, 1f);
        s.Bind(Rest(c), Array.Empty<ClothCollider>());
        var moved = Rotated(Rest(c), 30f);
        s.Reset(moved);
        for (var i = 0; i < s.Count; i++)
        {
            Assert.Equal(moved[i], s[i]);
        }
    }

    [Fact]
    public void Settles_without_jitter_at_rest_and_keeps_segment_lengths()
    {
        var c = Strip();
        var s = new ClothSpringSolver(c, 1f);
        var rest = Rest(c);
        s.Bind(rest, Array.Empty<ClothCollider>());
        var last = new Vector3[s.Count];
        var maxMove = 0f;
        for (var frame = 0; frame < 180; frame++)
        {
            for (var i = 0; i < s.Count; i++)
            {
                last[i] = s[i];
            }

            s.Step(1f / 60f, rest, Array.Empty<ClothCollider>(), Down, Vector3.Zero);
            if (frame >= 60)
            {
                for (var i = 0; i < s.Count; i++)
                {
                    maxMove = Math.Max(maxMove, Vector3.Distance(last[i], s[i]));
                }
            }
        }

        Assert.True(maxMove < 1e-5f, "per-frame movement at rest " + maxMove);
        for (var i = 1; i < s.Count; i++)
        {
            Assert.InRange(Vector3.Distance(s[i - 1], s[i]), 0.1499f, 0.1501f);
            Assert.True(Vector3.Distance(s[i], rest[i]) < 0.03f, "rest offset " + Vector3.Distance(s[i], rest[i]));
        }
    }

    [Fact]
    public void Swings_when_the_parent_moves_then_settles_inside_a_half_second_gap()
    {
        var c = Strip();
        var s = new ClothSpringSolver(c, 1f);
        var rest = Rest(c);
        s.Bind(rest, Array.Empty<ClothCollider>());
        var swing = 0f;
        for (var frame = 0; frame <= 30; frame++)
        {
            var target = Rotated(rest, 40f * MathF.Sin(frame / 30f * MathF.PI));
            s.Step(1f / 60f, target, Array.Empty<ClothCollider>(), Down, Vector3.Zero);
            swing = Math.Max(swing, Vector3.Distance(s[s.Count - 1], target[s.Count - 1]));
        }

        Assert.True(swing > 0.01f, "tip should lag the animated pose, swing " + swing);
        var last = new Vector3[s.Count];
        var gapMove = 0f;
        for (var frame = 0; frame < 30; frame++)
        {
            for (var i = 0; i < s.Count; i++)
            {
                last[i] = s[i];
            }

            s.Step(1f / 60f, rest, Array.Empty<ClothCollider>(), Down, Vector3.Zero);
            if (frame >= 24)
            {
                gapMove = Math.Max(gapMove, Enumerable.Range(1, s.Count - 1).Max(i => Vector3.Distance(last[i], s[i])));
            }
        }

        Assert.True(gapMove < 5e-4f, "still moving at the end of the 0.5 s rest gap: " + gapMove);
    }

    [Fact]
    public void Frame_rate_does_not_change_the_settled_pose()
    {
        var c = Strip();
        var results = new List<Vector3>();
        foreach (var fps in new[] { 30, 60, 144 })
        {
            var s = new ClothSpringSolver(c, 1f);
            var rest = Rest(c);
            s.Bind(rest, Array.Empty<ClothCollider>());
            var target = Rotated(rest, 25f);
            for (var t = 0f; t < 3f; t += 1f / fps)
            {
                s.Step(1f / fps, target, Array.Empty<ClothCollider>(), Down, Vector3.Zero);
            }

            results.Add(s[s.Count - 1]);
        }

        Assert.True(Vector3.Distance(results[0], results[1]) < 0.005f);
        Assert.True(Vector3.Distance(results[2], results[1]) < 0.005f);
    }

    [Fact]
    public void Leg_capsule_pushes_the_strip_out_instead_of_poking_through()
    {
        var c = Strip();
        var s = new ClothSpringSolver(c, 1f);
        var rest = Rest(c);
        // Thigh capsule starts behind the strip (z = 0) and swings forward through it.
        var restLeg = new ClothCollider(new Vector3(0, 0.95f, 0f), new Vector3(0, 0.5f, 0f), 0.07f);
        s.Bind(rest, new[] { restLeg });
        var worst = 1f;
        for (var frame = 0; frame <= 40; frame++)
        {
            var z = Math.Min(0.3f, frame * 0.01f);
            var leg = new ClothCollider(new Vector3(0, 0.95f, 0f), new Vector3(0, 0.5f, z), 0.07f);
            s.Step(1f / 60f, rest, new[] { leg }, Down, Vector3.Zero);
            for (var i = 1; i < s.Count; i++)
            {
                var a = i == 1 ? Vector3.Lerp(s[0], s[1], 0.5f) : s[i - 1];
                var d = ClothSpringSolver.SegmentSegmentDistance(a, s[i], leg.A, leg.B, out _, out _);
                worst = Math.Min(worst, d - s.Allowed(i - 1, 0));
            }
        }

        Assert.True(worst > -0.01f, "strip passes through the leg by " + (-worst));
    }

    [Fact]
    public void A_strip_that_overlaps_a_collider_at_rest_is_not_pushed_off_its_rest_pose()
    {
        var c = Strip();
        var s = new ClothSpringSolver(c, 1f);
        var rest = Rest(c);
        var leg = new ClothCollider(new Vector3(0, 0.95f, 0.15f), new Vector3(0, 0.5f, 0.15f), 0.07f);
        s.Bind(rest, new[] { leg });
        for (var frame = 0; frame < 120; frame++)
        {
            s.Step(1f / 60f, rest, new[] { leg }, Down, Vector3.Zero);
        }

        for (var i = 1; i < s.Count; i++)
        {
            Assert.True(Vector3.Distance(s[i], rest[i]) < 0.03f, "rest overlap pushed joint " + i);
        }
    }

    [Fact]
    public void Angle_limit_caps_each_joint()
    {
        var c = Strip(limit: 20f);
        var s = new ClothSpringSolver(c, 1f);
        var rest = Rest(c);
        s.Bind(rest, Array.Empty<ClothCollider>());
        for (var frame = 0; frame < 20; frame++)
        {
            s.Step(1f / 60f, rest, Array.Empty<ClothCollider>(), Down, new Vector3(0, 0, 0.2f));
            for (var i = 1; i < s.Count; i++)
            {
                var a = Vector3.Normalize(rest[i] - rest[i - 1]);
                var b = Vector3.Normalize(s[i] - s[i - 1]);
                Assert.True(MathF.Acos(Math.Clamp(Vector3.Dot(a, b), -1f, 1f)) * 180f / MathF.PI < 20.5f);
            }
        }
    }

    [Fact]
    public void A_long_hitch_resets_instead_of_exploding()
    {
        var c = Strip();
        var s = new ClothSpringSolver(c, 1f);
        var rest = Rest(c);
        s.Bind(rest, Array.Empty<ClothCollider>());
        var moved = Rotated(rest, 90f);
        s.Step(1.5f, moved, Array.Empty<ClothCollider>(), Down, Vector3.Zero);
        for (var i = 0; i < s.Count; i++)
        {
            Assert.Equal(moved[i], s[i]);
        }
    }

    [Fact]
    public void Humanoid_mapping_must_be_by_name_and_never_use_a_cloth_bone()
    {
        var good = MixamoHumanMap.Required.Concat(new[] { new KeyValuePair<string, string>("Chest", "mixamorig:Spine1") }).ToList();
        Func<string, bool> cloth = n => n.StartsWith("skirt_", StringComparison.Ordinal);
        Assert.Null(MixamoHumanMap.Validate(good, cloth));

        var clothMapped = good.Where(p => p.Key != "LeftUpperLeg").Append(new KeyValuePair<string, string>("LeftUpperLeg", "skirt_L_01"));
        Assert.Contains("cloth bone", MixamoHumanMap.Validate(clothMapped, cloth));

        var swapped = good.Where(p => p.Key != "LeftLowerLeg").Append(new KeyValuePair<string, string>("LeftLowerLeg", "mixamorig:LeftFoot"));
        Assert.NotNull(MixamoHumanMap.Validate(swapped, cloth));

        Assert.Equal("skirt_F_01", ClothSplitSpec.Leaf("VESPERA_rig/mixamorig:Hips/mixamorig:Spine/skirt_F_01"));
    }
}
