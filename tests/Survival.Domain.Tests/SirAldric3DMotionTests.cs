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
        // Lead is modest (−12) so the plant stays visible and step ≈ march 0.40 m.
        Assert.True(a.UpLegL.X < -10f);
        Assert.True(a.UpLegR.X > 7f);
        Assert.True(b.UpLegL.X > 7f);
        Assert.True(b.UpLegR.X < -10f);
        Assert.True(a.LegR.X > 8f);
        Assert.True(b.LegL.X > 8f);
        Assert.True(a.LeadLegTowardTop);
        Assert.True(b.LeadLegTowardTop);
        Assert.True(a.WalkArmPendulum);
        Assert.True(b.WalkArmPendulum);
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
        Assert.InRange(passL.LegL.X, 52f, 82f);
        Assert.InRange(passR.LegR.X, 52f, 82f);
        Assert.True(passL.LegL.X > passL.LegR.X + 25f);
        Assert.True(passR.LegR.X > passR.LegL.X + 25f);
        Assert.True(passL.HipDropOnPass);
        Assert.True(passR.HipDropOnPass);
        Assert.True(passL.Hips.Z >= 5f);
        Assert.True(passR.Hips.Z <= -5f);
        Assert.True(passL.StraightTrack);
        Assert.True(passR.StraightTrack);
        Assert.True(contactL.StraightTrack);
        Assert.True(contactR.StraightTrack);

        Assert.True(contactL.ShoulderHipCounter);
        Assert.True(contactR.ShoulderHipCounter);
        Assert.True(contactL.Hips.Y > 4f);
        Assert.True(contactL.Spine.Y < -4f);
        Assert.True(contactR.Hips.Y < -4f);
        Assert.True(contactR.Spine.Y > 4f);
        // Soft plant, not locked. Passing-foot heel lifts (foot +X) — gait-bar, not contact lock.
        Assert.InRange(contactL.LegL.X, 6f, 28f);
        Assert.True(passL.FootL.X > 10f);
        Assert.True(passR.FootR.X > 8f);

        Assert.True(passL.WalkArmPendulum);
        Assert.True(contactL.WalkArmPendulum);
        Assert.True(passL.SheathedOnCharacterRight);
        Assert.True(contactL.SheathedOnCharacterRight);

        // 14d9c17: pass thigh +X stacked on knee +X and the shin read as a back-kick
        // toward the camera. Passing thigh must be −X (toward TOP / +Z) so the tucked
        // foot steps forward, not further +X than the trail.
        Assert.True(passL.UpLegL.X < -8f);
        Assert.True(passR.UpLegR.X < -8f);
        Assert.True(passL.UpLegL.X < passL.UpLegR.X);
        Assert.True(passR.UpLegR.X < passR.UpLegL.X);
        Assert.True(passL.UpLegL.X < contactR.UpLegL.X);
        Assert.True(passR.UpLegR.X < contactL.UpLegR.X);
        // Passing thigh not abducted — foot tucks under the pelvis, not a side kick.
        Assert.True(passL.UpLegL.Z >= -2f);
        Assert.True(passR.UpLegR.Z <= 2f);
    }

    [Fact]
    public void Walk_swing_thigh_travels_toward_top_not_back()
    {
        // Left swing: contact R (trail +X) → pass L (−X) → contact L (lead −X).
        var trail = SirAldric3DMotion.Evaluate(SirAldric3DMotion.WalkPeriodSeconds * 0.75f);
        var pass = SirAldric3DMotion.Evaluate(0f);
        var lead = SirAldric3DMotion.Evaluate(SirAldric3DMotion.WalkPeriodSeconds * 0.25f);
        Assert.True(trail.UpLegL.X > 7f);
        Assert.True(pass.UpLegL.X < -8f);
        Assert.True(lead.UpLegL.X < -8f);
        Assert.True(pass.UpLegL.X < trail.UpLegL.X);
        for (var i = 0; i <= 16; i++)
        {
            var u = 0.75f + i * 0.50f / 16f;
            if (u >= 1f)
            {
                u -= 1f;
            }

            var pose = SirAldric3DMotion.Evaluate(u * SirAldric3DMotion.WalkPeriodSeconds);
            Assert.True(pose.UpLegL.X < trail.UpLegL.X + 0.5f);
        }

        // Right swing: contact L → pass R → contact R.
        trail = SirAldric3DMotion.Evaluate(SirAldric3DMotion.WalkPeriodSeconds * 0.25f);
        pass = SirAldric3DMotion.Evaluate(SirAldric3DMotion.WalkPeriodSeconds * 0.5f);
        lead = SirAldric3DMotion.Evaluate(SirAldric3DMotion.WalkPeriodSeconds * 0.75f);
        Assert.True(trail.UpLegR.X > 7f);
        Assert.True(pass.UpLegR.X < -8f);
        Assert.True(lead.UpLegR.X < -8f);
        Assert.True(pass.UpLegR.X < trail.UpLegR.X);
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
        Assert.True(new FileInfo(Path.Combine(dir, "walk_cycle_mixamo_style.bvh")).Length > 10_000);
        Assert.True(new FileInfo(Path.Combine(dir, "gait_bar_phases", "pass_l_rear.png")).Length > 10_000);
        Assert.True(new FileInfo(Path.Combine(dir, "gait_bar_phases", "contact_l_rear.png")).Length > 10_000);
        Assert.True(new FileInfo(Path.Combine(dir, "gait_bar_phases", "pass_r_rear.png")).Length > 10_000);
        Assert.True(new FileInfo(Path.Combine(dir, "gait_bar_phases", "contact_r_rear.png")).Length > 10_000);
    }

    [Fact]
    public void Walk_has_loose_contralateral_arm_pendulum_not_pinned()
    {
        float minL = 999f, maxL = -999f, minR = 999f, maxR = -999f;
        for (var i = 0; i < 20; i++)
        {
            var pose = SirAldric3DMotion.Evaluate(i * SirAldric3DMotion.WalkPeriodSeconds / 20f);
            Assert.True(pose.SheathedOnCharacterRight);
            Assert.True(pose.ForeR.X < 0f);
            minL = Math.Min(minL, pose.ArmL.X);
            maxL = Math.Max(maxL, pose.ArmL.X);
            minR = Math.Min(minR, pose.ArmR.X);
            maxR = Math.Max(maxR, pose.ArmR.X);
        }

        // Left arm must cross hang (0): back toward camera AND forward toward TOP.
        Assert.True(minL < -8f);
        Assert.True(maxL > 8f);
        Assert.True(minR < -8f);
        Assert.True(maxR > 4f);

        var contactL = SirAldric3DMotion.Evaluate(SirAldric3DMotion.WalkPeriodSeconds * 0.25f);
        var contactR = SirAldric3DMotion.Evaluate(SirAldric3DMotion.WalkPeriodSeconds * 0.75f);
        Assert.True(contactL.ArmL.X > 8f);
        Assert.True(contactL.ArmR.X < -8f);
        Assert.True(contactR.ArmL.X < -8f);
        Assert.True(contactR.ArmR.X > 4f);
        Assert.True(contactL.WalkArmPendulum);
        Assert.True(contactR.WalkArmPendulum);
        Assert.True(maxL - minL > 40f);
        Assert.True(maxR - minR > 40f);
    }

    [Fact]
    public void Walk_stays_on_a_straight_forward_track()
    {
        for (var i = 0; i < 20; i++)
        {
            var pose = SirAldric3DMotion.Evaluate(i * SirAldric3DMotion.WalkPeriodSeconds / 20f);
            Assert.True(pose.StraightTrack);
            Assert.InRange(pose.Hips.Y, -8f, 8f);
            Assert.InRange(pose.Hips.Z, -6f, 6f);
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
        Assert.Contains("sir_aldric_meshy.mesh.txt", src, StringComparison.Ordinal);
        Assert.Contains("SkinQuality.Bone4", src, StringComparison.Ordinal);
        Assert.Contains("Scabbard", src, StringComparison.Ordinal);
        Assert.DoesNotContain("BodyCap(\"Cape\"", src, StringComparison.Ordinal);
        Assert.DoesNotContain("(-0.16f, 0.04f, 0f)", src, StringComparison.Ordinal);
        var dir = Path.Combine(FindRepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a", "art", "heroes", "3d");
        var mesh = Path.Combine(dir, "sir_aldric_meshy.mesh.txt");
        var atlas = Path.Combine(dir, "sir_aldric_meshy_atlas.png");
        var loft = Path.Combine(dir, "sir_aldric_midpoly.mesh.txt");
        var fbx = Path.Combine(dir, "sir_aldric.fbx");
        var clean = Path.Combine(dir, "sir_aldric_path2_clean.fbx");
        Assert.True(new FileInfo(mesh).Length > 10_000);
        Assert.True(new FileInfo(atlas).Length > 50_000);
        Assert.True(new FileInfo(loft).Length > 10_000);
        Assert.True(new FileInfo(fbx).Length > 50_000);
        Assert.True(new FileInfo(clean).Length > 50_000);
        var meshText = File.ReadAllText(mesh);
        Assert.Contains("BONE Scabbard", meshText, StringComparison.Ordinal);
        Assert.Contains("FMT v4", meshText, StringComparison.Ordinal);
        Assert.Contains("blender", meshText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BONE Cape", meshText, StringComparison.Ordinal);
    }

    [Fact]
    public void Mixamo_import_rear_yaw_faces_world_plus_z_not_the_camera()
    {
        Assert.Equal(180f, SirAldric3DMotion.MixamoImportRearYawDegrees);
        var actor = Path.Combine(FindRepoRoot(), "Assets", "Survival", "Unity", "SirAldricMeshyAnimateActor.cs");
        var demo = Path.Combine(FindRepoRoot(), "Assets", "Survival", "Unity", "SirAldricDemo.cs");
        var actorSrc = File.ReadAllText(actor);
        var demoSrc = File.ReadAllText(demo);
        Assert.Contains("RearYawDegrees = SirAldric3DMotion.MixamoImportRearYawDegrees", actorSrc, StringComparison.Ordinal);
        Assert.Contains("FaceWorldTop", actorSrc, StringComparison.Ordinal);
        Assert.Contains("Quaternion.Euler(0f, RearYawDegrees, 0f)", actorSrc, StringComparison.Ordinal);
        Assert.Contains("new Vector3(0f, 2.80f, -5.40f)", demoSrc, StringComparison.Ordinal);
        Assert.Contains("new Vector3(0f, 0.90f, 0.50f)", demoSrc, StringComparison.Ordinal);
        Assert.Contains("EnemyTop", demoSrc, StringComparison.Ordinal);
        Assert.DoesNotContain("_clipPlayable", actorSrc, StringComparison.Ordinal);
    }

    [Fact]
    public void Humanoid_attack_draws_then_strikes_toward_world_top()
    {
        Assert.Equal(SirAldric3DMotion.AttackSeconds, SirAldricHumanoidAttack.Seconds);
        var draw = SirAldricHumanoidAttack.Evaluate(0.10f);
        var guard = SirAldricHumanoidAttack.Evaluate(SirAldricHumanoidAttack.Seconds * 0.30f);
        var strike = SirAldricHumanoidAttack.Evaluate(SirAldricHumanoidAttack.StrikePeakSeconds);
        var recover = SirAldricHumanoidAttack.Evaluate(SirAldricHumanoidAttack.Seconds - 0.02f);

        Assert.Equal(SirAldricHumanoidAttack.PhaseKind.Draw, draw.Phase);
        Assert.Equal(SirAldricHumanoidAttack.PhaseKind.Guard, guard.Phase);
        Assert.Equal(SirAldricHumanoidAttack.PhaseKind.Strike, strike.Phase);
        Assert.Equal(SirAldricHumanoidAttack.PhaseKind.Recover, recover.Phase);

        Assert.True(strike.SwordDrawn);
        Assert.True(strike.StrikeTowardTop);
        Assert.True(strike.HandZ > draw.HandZ + 0.45f);
        Assert.True(strike.HandZ > 0.50f);
        Assert.True(draw.HandZ < 0.10f);
        Assert.True(recover.HandZ < strike.HandZ);
        Assert.False(recover.StrikeTowardTop);
        Assert.Contains("GetBoneTransform", SirAldricHumanoidAttack.Authorship, StringComparison.Ordinal);
        Assert.Contains("FromToRotation", SirAldricHumanoidAttack.Authorship, StringComparison.Ordinal);
        Assert.DoesNotContain("Design PASS", SirAldricHumanoidAttack.Authorship, StringComparison.Ordinal);
    }

    [Fact]
    public void Meshy_animate_actor_plays_sep_walk_and_attack_on_same_humanoid()
    {
        var root = FindRepoRoot();
        var actor = Path.Combine(root, "Assets", "Survival", "Unity", "SirAldricMeshyAnimateActor.cs");
        var src = File.ReadAllText(actor);
        Assert.Contains("SirAldric_SEP_meshy_animate_walk.fbx", src, StringComparison.Ordinal);
        Assert.Contains("SirAldric_SEP_meshy_animate_attack.fbx", src, StringComparison.Ordinal);
        Assert.Contains("LeftoverFusedWalkFbx", src, StringComparison.Ordinal);
        Assert.Contains("LeftoverStandingSlashFbx", src, StringComparison.Ordinal);
        Assert.Contains("LoadAttackClip", src, StringComparison.Ordinal);
        Assert.Contains("AttackOnWalkHumanoid", src, StringComparison.Ordinal);
        Assert.Contains("WalkCyclesBeforeAttack", src, StringComparison.Ordinal);
        Assert.Contains("RearYawDegrees", src, StringComparison.Ordinal);
        Assert.Contains("Path A weight-paint is CANCELLED", src, StringComparison.Ordinal);
        Assert.Contains("not SoT", src, StringComparison.Ordinal);
        Assert.DoesNotContain("EnsureClipSword", src, StringComparison.Ordinal);
        Assert.DoesNotContain("PlaceClipSwordInHand", src, StringComparison.Ordinal);
        Assert.DoesNotContain("private static void AimChain", src, StringComparison.Ordinal);
        Assert.DoesNotContain("_mixer.ConnectInput(1", src, StringComparison.Ordinal);
        Assert.DoesNotContain("SirAldricMeshyAnimateAttack", src, StringComparison.Ordinal);

        var walk = Path.Combine(root, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "heroes", "3d", "SirAldric_SEP_meshy_animate_walk.fbx");
        var attack = Path.Combine(root, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "heroes", "3d", "SirAldric_SEP_meshy_animate_attack.fbx");
        Assert.True(new FileInfo(walk).Length > 1_000_000);
        Assert.True(new FileInfo(attack).Length > 1_000_000);
        var walkBytes = File.ReadAllBytes(walk);
        var atkBytes = File.ReadAllBytes(attack);
        Assert.True(IndexOfAscii(walkBytes, "mixamorig") >= 0);
        Assert.True(IndexOfAscii(atkBytes, "mixamorig") < 0);
        Assert.True(IndexOfAscii(atkBytes, "Spine02") >= 0);
        Assert.True(IndexOfAscii(walkBytes, "Scabbard") < 0);
        Assert.True(IndexOfAscii(atkBytes, "Scabbard") < 0);

        var walkMeta = File.ReadAllText(walk + ".meta");
        var atkMeta = File.ReadAllText(attack + ".meta");
        Assert.Contains("takeName: target_character|target_character|Walking", walkMeta, StringComparison.Ordinal);
        Assert.Contains("name: Walking", walkMeta, StringComparison.Ordinal);
        Assert.Contains("animationType: 3", walkMeta, StringComparison.Ordinal);
        Assert.Contains("takeName: target_character|rigify_clip|BaseLayer", atkMeta, StringComparison.Ordinal);
        Assert.Contains("name: Attack", atkMeta, StringComparison.Ordinal);
        Assert.Contains("firstFrame: 3", atkMeta, StringComparison.Ordinal);
        Assert.Contains("lastFrame: 92", atkMeta, StringComparison.Ordinal);
        Assert.Contains("animationType: 3", atkMeta, StringComparison.Ordinal);

        var capture = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "SirAldricGameViewCapture.cs"));
        Assert.Contains("Camera.Render", capture, StringComparison.Ordinal);
        Assert.Contains("1080", capture, StringComparison.Ordinal);
        Assert.Contains("aldric_sep_20260925", capture, StringComparison.Ordinal);
        Assert.Contains("sir_aldric_sep_walk_juice_gameview.mp4", capture, StringComparison.Ordinal);
        Assert.Contains("sir_aldric_sep_attack_juice_gameview.mp4", capture, StringComparison.Ordinal);
    }

    [Fact]
    public void Walk_wide_stance_is_authored_in_the_mixamo_clip_not_import_ik()
    {
        var root = FindRepoRoot();
        var hold = File.ReadAllText(Path.Combine(root, "Docs", "Survival", "previews", "facing_20260925", "MOTION_HOLD.md"));
        Assert.Contains("0.396", hold, StringComparison.Ordinal);
        Assert.Contains("0.719", hold, StringComparison.Ordinal);
        Assert.Contains("Wait Design walk iterate", hold, StringComparison.Ordinal);
        Assert.Contains("HOLD merge", hold, StringComparison.Ordinal);
        var meta = File.ReadAllText(Path.Combine(root, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "heroes", "3d", "sir_aldric_meshy_animate_walk.fbx.meta"));
        Assert.Contains("heightFromFeet: 1", meta, StringComparison.Ordinal);
        Assert.Contains("addHumanoidExtraRoot: 1", meta, StringComparison.Ordinal);
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "SirAldricMeshyAnimateActor.cs"));
        Assert.Contains("AttackAuthoredReason", actor, StringComparison.Ordinal);
        Assert.Contains("Path A weight-paint is CANCELLED", actor, StringComparison.Ordinal);
    }

    [Fact]
    public void Sep_meshy_clips_have_no_embedded_atlas_and_actor_does_not_invent_one()
    {
        var root = FindRepoRoot();
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "SirAldricMeshyAnimateActor.cs"));
        Assert.Contains("ExtractFbxPng", actor, StringComparison.Ordinal);
        Assert.Contains("RepairWalkingTake", actor, StringComparison.Ordinal);
        Assert.Contains("RepairAttackTake", actor, StringComparison.Ordinal);
        Assert.Contains("Atlas may be missing", actor, StringComparison.Ordinal);
        Assert.Contains("Do not bind the old fused-walk atlas", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("LoadSiblingPng(\"sir_aldric_meshy_atlas.png\")", actor, StringComparison.Ordinal);

        var walk = Path.Combine(root, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "heroes", "3d", "SirAldric_SEP_meshy_animate_walk.fbx");
        var bytes = File.ReadAllBytes(walk);
        var png = false;
        for (var i = 0; i < bytes.Length - 3; i++)
        {
            if (bytes[i] == 0x89 && bytes[i + 1] == 0x50 && bytes[i + 2] == 0x4E && bytes[i + 3] == 0x47)
            {
                png = true;
                break;
            }
        }

        Assert.False(png);
    }

    [Fact]
    public void Sep_walk_is_body_only_no_scabbard_and_clipsword_is_not_sot()
    {
        var root = FindRepoRoot();
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "SirAldricMeshyAnimateActor.cs"));
        Assert.Contains("Path A weight-paint is CANCELLED", actor, StringComparison.Ordinal);
        Assert.Contains("Body-only AccuRIG", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("EnsureClipSword", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("weight-paint", actor.Replace("Path A weight-paint is CANCELLED", ""), StringComparison.Ordinal);

        var walkBytes = File.ReadAllBytes(Path.Combine(
            root,
            "Assets",
            "ThemePack",
            "fantasy_kingdom_a",
            "art",
            "heroes",
            "3d",
            "SirAldric_SEP_meshy_animate_walk.fbx"));
        Assert.True(IndexOfAscii(walkBytes, "mixamorig:RightHand") >= 0);
        Assert.True(IndexOfAscii(walkBytes, "RightHandMiddle4") >= 0);
        Assert.True(IndexOfAscii(walkBytes, "Scabbard") < 0);
        Assert.True(IndexOfAscii(walkBytes, "Sheath") < 0);
        Assert.True(IndexOfAscii(walkBytes, "SirAldric_SEP_body_nosword") >= 0);
    }

    [Fact]
    public void Aldric_sep_20260925_playcam_proofs_are_on_disk()
    {
        var dir = Path.Combine(FindRepoRoot(), "Docs", "Survival", "previews", "aldric_sep_20260925");
        Assert.True(new FileInfo(Path.Combine(dir, "SEP_HOLD.md")).Length > 400);
        Assert.True(new FileInfo(Path.Combine(dir, "ANIMATE_HANDOFF.md")).Length > 200);
        Assert.True(new FileInfo(Path.Combine(dir, "sir_aldric_sep_rear_walk_playcam.png")).Length > 50_000);
        Assert.True(new FileInfo(Path.Combine(dir, "sir_aldric_sep_front_walk_playcam.png")).Length > 50_000);
        Assert.True(new FileInfo(Path.Combine(dir, "sir_aldric_sep_34_walk_playcam.png")).Length > 50_000);
        Assert.True(new FileInfo(Path.Combine(dir, "sir_aldric_sep_rear_attack_playcam.png")).Length > 50_000);
        Assert.True(new FileInfo(Path.Combine(dir, "sir_aldric_sep_front_attack_playcam.png")).Length > 50_000);
        Assert.True(new FileInfo(Path.Combine(dir, "sir_aldric_sep_34_attack_playcam.png")).Length > 50_000);
        Assert.True(new FileInfo(Path.Combine(dir, "sir_aldric_sep_walk_juice_playcam.mp4")).Length > 50_000);
        Assert.True(new FileInfo(Path.Combine(dir, "sir_aldric_sep_attack_juice_playcam.mp4")).Length > 50_000);
        var hold = File.ReadAllText(Path.Combine(dir, "SEP_HOLD.md"));
        Assert.Contains("HOLD merge", hold, StringComparison.Ordinal);
        Assert.Contains("Not Meshy website stills", hold, StringComparison.Ordinal);
        Assert.Contains("Not Unity Game-view", hold, StringComparison.Ordinal);
    }

    [Fact]
    public void Facing_20260925_rear_proofs_are_on_disk()
    {
        var dir = Path.Combine(FindRepoRoot(), "Docs", "Survival", "previews", "facing_20260925");
        Assert.True(new FileInfo(Path.Combine(dir, "FACING.md")).Length > 400);
        Assert.True(new FileInfo(Path.Combine(dir, "sir_aldric_rear_still_back_to_camera.png")).Length > 100_000);
        Assert.True(new FileInfo(Path.Combine(dir, "sir_aldric_rear_walk_pose.png")).Length > 100_000);
        Assert.True(new FileInfo(Path.Combine(dir, "sir_aldric_walk_toward_top_rear.mp4")).Length > 100_000);
        Assert.True(new FileInfo(Path.Combine(dir, "sir_aldric_attack_toward_top_rear.mp4")).Length > 100_000);
        Assert.True(new FileInfo(Path.Combine(dir, "sir_aldric_attack_slash_rear.png")).Length > 50_000);
    }

    [Fact]
    public void Play_placeholder_locked_rear_png_still_exists()
    {
        var root = FindRepoRoot();
        var packPng = Path.Combine(root, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "heroes", "SIR_ALDRIC_REAR_MASTER_LOCKED.png");
        Assert.True(File.Exists(packPng));
        Assert.True(new FileInfo(packPng).Length > 100_000);
    }

    private static int IndexOfAscii(byte[] hay, string needle)
    {
        var n = System.Text.Encoding.ASCII.GetBytes(needle);
        var last = hay.Length - n.Length;
        for (var i = 0; i <= last; i++)
        {
            var ok = true;
            for (var j = 0; j < n.Length; j++)
            {
                if (hay[i + j] != n[j])
                {
                    ok = false;
                    break;
                }
            }

            if (ok)
            {
                return i;
            }
        }

        return -1;
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
