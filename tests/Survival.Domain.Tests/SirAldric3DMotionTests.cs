using Survival.Domain.Heroes;
using Survival.Domain.Ids;

namespace Survival.Domain.Tests;

public sealed class SirAldric3DMotionTests
{
    [Fact]
    public void Loop_is_walk_then_attack_then_walk()
    {
        Assert.False(SirAldric3DMotion.IsAttacking(0f));
        Assert.False(SirAldric3DMotion.IsAttacking(SirAldric3DMotion.WalkBlockSeconds - 0.01f));
        Assert.True(SirAldric3DMotion.IsAttacking(SirAldric3DMotion.WalkBlockSeconds + 0.01f));
        Assert.False(SirAldric3DMotion.Evaluate(SirAldric3DMotion.LoopSeconds + 0.05f).Attacking);
    }

    [Fact]
    public void Walk_is_squared_rear_scabbard_character_right_opposite_stride()
    {
        for (var i = 0; i < 8; i++)
        {
            var pose = SirAldric3DMotion.Evaluate(i * SirAldric3DMotion.WalkPeriodSeconds * 0.25f);
            Assert.False(pose.Attacking);
            Assert.False(pose.SwordDrawn);
            Assert.True(pose.FacesTop);
            Assert.True(pose.SheathedOnCharacterRight);
        }

        var a = SirAldric3DMotion.Evaluate(SirAldric3DMotion.WalkPeriodSeconds * 0.25f);
        var b = SirAldric3DMotion.Evaluate(SirAldric3DMotion.WalkPeriodSeconds * 0.75f);
        // −X thigh = toward world +Z = TOP. At ¼ cycle left leads TOP; at ¾ right leads TOP.
        Assert.True(a.UpLegL.X < -16f);
        Assert.True(a.UpLegR.X > 16f);
        Assert.True(b.UpLegL.X > 16f);
        Assert.True(b.UpLegR.X < -16f);
        Assert.True(a.LegR.X > 8f);
        Assert.True(b.LegL.X > 8f);
        Assert.True(a.LeadLegTowardTop);
        Assert.True(b.LeadLegTowardTop);
        Assert.True(a.ArmsTowardTop);
        Assert.True(b.ArmsTowardTop);
        Assert.True(a.ShoulderHipCounter);
        Assert.True(b.ShoulderHipCounter);
    }

    [Fact]
    public void Walk_matches_gait_bar_pass_knee_hip_drop_and_counter_rotate()
    {
        var passL = SirAldric3DMotion.Evaluate(0f);
        var passR = SirAldric3DMotion.Evaluate(SirAldric3DMotion.WalkPeriodSeconds * 0.5f);
        var contactL = SirAldric3DMotion.Evaluate(SirAldric3DMotion.WalkPeriodSeconds * 0.25f);
        var contactR = SirAldric3DMotion.Evaluate(SirAldric3DMotion.WalkPeriodSeconds * 0.75f);

        Assert.False(passL.Attacking);
        Assert.True(passL.PassingKneeBent);
        Assert.True(passR.PassingKneeBent);
        Assert.InRange(passL.LegL.X, 52f, 78f);
        Assert.InRange(passR.LegR.X, 52f, 78f);
        Assert.True(passL.LegL.X > passL.LegR.X + 25f);
        Assert.True(passR.LegR.X > passR.LegL.X + 25f);
        Assert.True(passL.HipDropOnPass);
        Assert.True(passR.HipDropOnPass);
        Assert.True(passL.Hips.Z > 5f);
        Assert.True(passR.Hips.Z < -5f);

        Assert.True(contactL.ShoulderHipCounter);
        Assert.True(contactR.ShoulderHipCounter);
        Assert.True(contactL.Hips.Y > 4f);
        Assert.True(contactL.Spine.Y < -4f);
        Assert.True(contactR.Hips.Y < -4f);
        Assert.True(contactR.Spine.Y > 4f);
        // Soft plant, not locked; trailing heel lifts (foot +X).
        Assert.InRange(contactL.LegL.X, 8f, 28f);
        Assert.True(contactL.FootR.X > 10f);
        Assert.True(contactR.FootL.X > 10f);

        Assert.True(passL.ArmsTowardTop);
        Assert.True(passL.SheathedOnCharacterRight);
        Assert.True(contactL.ArmsTowardTop);
    }

    [Fact]
    public void Gait_bar_refs_are_on_disk()
    {
        var root = FindRepoRoot();
        var dir = Path.Combine(
            root,
            "design",
            "survival-theme-a-fantasy",
            "heroes",
            "anim",
            "sir_aldric",
            "UNITY_3D_HANDOFF",
            "refs");
        Assert.True(new FileInfo(Path.Combine(dir, "WALK_GAIT_BAR.md")).Length > 400);
        Assert.True(new FileInfo(Path.Combine(dir, "WALK_GAIT_BAR_skeleton_sample.mp4")).Length > 100_000);
    }

    [Fact]
    public void Walk_arms_and_sword_hang_toward_top_not_camera()
    {
        for (var i = 0; i < 8; i++)
        {
            var pose = SirAldric3DMotion.Evaluate(i * SirAldric3DMotion.WalkPeriodSeconds * 0.25f);
            Assert.True(pose.ArmsTowardTop);
            Assert.True(pose.ArmR.X < 0f);
            Assert.True(pose.ArmL.X < 0f);
            Assert.True(pose.ForeR.X < 0f);
        }
    }

    [Fact]
    public void RootZ_increases_during_the_loop_toward_top()
    {
        var a = SirAldric3DMotion.Evaluate(0.05f);
        var b = SirAldric3DMotion.Evaluate(SirAldric3DMotion.WalkBlockSeconds - 0.05f);
        var c = SirAldric3DMotion.Evaluate(SirAldric3DMotion.LoopSeconds - 0.05f);
        Assert.True(b.RootZ > a.RootZ + 0.3f);
        Assert.True(c.RootZ > b.RootZ);
        Assert.InRange(c.RootZ, 0.8f, 3.2f);
    }

    [Fact]
    public void Attack_strikes_toward_top_then_resheaths_on_character_right()
    {
        var strike = SirAldric3DMotion.Evaluate(SirAldric3DMotion.WalkBlockSeconds + SirAldric3DMotion.AttackSeconds * 0.48f);
        Assert.True(strike.Attacking);
        Assert.True(strike.SwordDrawn);
        Assert.True(strike.FacesTop);
        Assert.True(strike.StrikeTowardTop);
        Assert.True(strike.ArmsTowardTop);
        Assert.InRange(strike.ArmR.X, -175f, -80f);

        var end = SirAldric3DMotion.Evaluate(SirAldric3DMotion.WalkBlockSeconds + SirAldric3DMotion.AttackSeconds - 0.02f);
        Assert.True(end.Attacking);
        Assert.False(end.SwordDrawn);
        Assert.True(end.SheathedOnCharacterRight);
    }

    [Fact]
    public void Look_targets_00_01_02_are_on_disk()
    {
        var root = FindRepoRoot();
        var dir = Path.Combine(
            root,
            "design",
            "survival-theme-a-fantasy",
            "heroes",
            "anim",
            "sir_aldric",
            "UNITY_3D_HANDOFF",
            "look_targets");
        Assert.True(new FileInfo(Path.Combine(dir, "00_fullbody_LOCKED.png")).Length > 100_000);
        Assert.True(new FileInfo(Path.Combine(dir, "01_rear_LOCKED.png")).Length > 100_000);
        Assert.True(new FileInfo(Path.Combine(dir, "02_aldric_turnaround_orthos.png")).Length > 100_000);
    }

    [Fact]
    public void Actor_has_single_character_right_scabbard_and_no_back_sheath_mesh()
    {
        var path = Path.Combine(FindRepoRoot(), "Assets", "Survival", "Unity", "SirAldric3DActor.cs");
        var src = File.ReadAllText(path);
        Assert.Contains("TrimCap(\"Scabbard\"", src, StringComparison.Ordinal);
        Assert.DoesNotContain("BodyCap(\"Cape\"", src, StringComparison.Ordinal);
        Assert.DoesNotContain("(-0.16f, 0.04f, 0f)", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Play_placeholder_locked_rear_png_still_exists()
    {
        var root = FindRepoRoot();
        var packPng = Path.Combine(root, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "heroes", "SIR_ALDRIC_REAR_MASTER_LOCKED.png");
        Assert.True(File.Exists(packPng));
        Assert.True(new FileInfo(packPng).Length > 100_000);
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
