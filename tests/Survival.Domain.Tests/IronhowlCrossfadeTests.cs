using Survival.Domain.Enemies;

namespace Survival.Domain.Tests;

public sealed class IronhowlCrossfadeTests
{
    // Measured on the locked FBXs (IRONHOWL_rig_rest / IRONHOWL_rig_attack) at every frame and half-frame:
    // biggest single-bone gap between rest and attack frame 0 (LeftForeArm), and the attack clip's own
    // fastest half-frame step (RightForeArm). /workspace/battle_scene/ironhowl_blend/sim.py.
    private const float RestToAttackGapDegrees = 108.7f;
    private const float ClipOwnMaxHalfFrameDegrees = 15.7f;

    [Fact]
    public void Crossfade_is_a_quarter_second_smootherstep_with_flat_ends()
    {
        Assert.InRange(IronhowlMotion.CrossfadeSeconds, 0.15f, 0.25f);
        Assert.Equal(0f, IronhowlMotion.CrossfadeWeight(0f));
        Assert.Equal(0f, IronhowlMotion.CrossfadeWeight(-1f));
        Assert.Equal(1f, IronhowlMotion.CrossfadeWeight(IronhowlMotion.CrossfadeSeconds));
        Assert.Equal(1f, IronhowlMotion.CrossfadeWeight(5f));
        Assert.Equal(0.5f, IronhowlMotion.CrossfadeWeight(IronhowlMotion.CrossfadeSeconds * 0.5f), 4);

        // Monotonic, sampled at every half-frame (60 Hz) and finer.
        var last = 0f;
        for (var t = 0f; t <= IronhowlMotion.CrossfadeSeconds + 0.02f; t += 1f / 240f)
        {
            var w = IronhowlMotion.CrossfadeWeight(t);
            Assert.True(w >= last - 1e-6f);
            last = w;
        }

        // Zero slope at both ends: the blend adds no velocity jump when it starts or finishes.
        var h = 1e-3f;
        Assert.True(IronhowlMotion.CrossfadeWeight(h) / h < 0.01f);
        var end = IronhowlMotion.CrossfadeSeconds;
        Assert.True((1f - IronhowlMotion.CrossfadeWeight(end - h)) / h < 0.01f);
    }

    [Fact]
    public void No_half_frame_step_during_the_blend_is_faster_than_the_attack_clip_itself()
    {
        // Worst case per half-frame (1/60 s) for the biggest bone gap, using the weight's steepest step.
        var maxStep = 0f;
        for (var i = 0; i * (1f / 60f) <= IronhowlMotion.CrossfadeSeconds + 1e-4f; i++)
        {
            var a = IronhowlMotion.CrossfadeWeight(i / 60f);
            var b = IronhowlMotion.CrossfadeWeight((i + 1) / 60f);
            maxStep = Math.Max(maxStep, (b - a) * RestToAttackGapDegrees);
        }

        Assert.True(maxStep <= ClipOwnMaxHalfFrameDegrees, "blend step " + maxStep);
        Assert.True(RestToAttackGapDegrees / ClipOwnMaxHalfFrameDegrees > 1f, "a hard cut would pop");
    }

    [Fact]
    public void Actor_blends_through_a_mixer_and_freezes_the_outgoing_pose()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Assets", "ThemePack", "fantasy_kingdom_a", "pack.json")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        var actor = File.ReadAllText(Path.Combine(root!.FullName, "Assets", "Survival", "Unity", "IronhowlActor.cs"));
        Assert.Contains("AnimationMixerPlayable.Create(_graph, 2)", actor, StringComparison.Ordinal);
        Assert.Contains("IronhowlMotion.CrossfadeWeight(_fade)", actor, StringComparison.Ordinal);
        Assert.Contains("_from.SetSpeed(0d)", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("_output.SetSourcePlayable(_playable)", actor, StringComparison.Ordinal);
    }
}
