using System.Globalization;
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
    public void Meshy_animate_actor_binds_albedo_and_keeps_walking_clip()
    {
        var root = FindRepoRoot();
        var actor = Path.Combine(root, "Assets", "Survival", "Unity", "SirAldricMeshyAnimateActor.cs");
        var src = File.ReadAllText(actor);
        Assert.Contains("SetTexture(\"_BaseMap\"", src, StringComparison.Ordinal);
        Assert.Contains("SetTexture(\"_MainTex\"", src, StringComparison.Ordinal);
        Assert.Contains("ExtractFbxPng", src, StringComparison.Ordinal);
        Assert.Contains("RepairWalkingTake", src, StringComparison.Ordinal);
        Assert.Contains("takeName", src, StringComparison.Ordinal);
        var leftover = Path.Combine(root, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "heroes", "3d", "sir_aldric_meshy_animate_walk.fbx");
        var atlas = Path.Combine(root, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "heroes", "3d", "sir_aldric_meshy_atlas.png");
        Assert.True(new FileInfo(leftover).Length > 1_000_000);
        Assert.True(new FileInfo(atlas).Length > 100_000);
    }

    [Fact]
    public void Pilot_mixamo_sep_walk_slash_are_sot_and_clipsword_is_not()
    {
        var root = FindRepoRoot();
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "SirAldricMeshyAnimateActor.cs"));
        Assert.Contains("SirAldric_body_holefixed_walk.fbx", actor, StringComparison.Ordinal);
        Assert.Contains("SirAldric_body_holefixed_slash.fbx", actor, StringComparison.Ordinal);
        Assert.Contains("SirAldric_body_holefixed_mid280k.fbx", actor, StringComparison.Ordinal);
        Assert.Contains("SirAldric_PILOT_sword.fbx", actor, StringComparison.Ordinal);
        Assert.Contains("SirAldricPilotMixamo", actor, StringComparison.Ordinal);
        Assert.Contains("StripEmbeddedClipMeshes", actor, StringComparison.Ordinal);
        Assert.Contains("visible=Mixamo", actor, StringComparison.Ordinal);
        Assert.Contains("updateWhenOffscreen", actor, StringComparison.Ordinal);
        Assert.Contains("NormalizeMixamoCmRoot", actor, StringComparison.Ordinal);
        Assert.Contains("PILOT skin FAIL", actor, StringComparison.Ordinal);
        Assert.Contains("ModelImporterAnimationType.Generic", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("ModelImporterAnimationType.Human", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("visible=AccuRIG", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("SirAldric_PILOT_accurig_humanoid.fbx", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("SirAldric_PILOT_walk_accurig.fbx", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("SirAldric_PILOT_attack_library.fbx", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildHumanAvatar", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("AvatarBuilder", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("SirAldricPilotWalk", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("SirAldricPilotAttack", actor, StringComparison.Ordinal);
        Assert.Contains("applyRootMotion = false", actor, StringComparison.Ordinal);
        Assert.Contains("AlwaysAnimate", actor, StringComparison.Ordinal);
        Assert.Contains("RepairAttackTake", actor, StringComparison.Ordinal);
        Assert.Contains("EnableKeyword(\"_BASEMAP\")", actor, StringComparison.Ordinal);
        Assert.Contains("AttachHeldSword", actor, StringComparison.Ordinal);
        Assert.Contains("AnimationMixerPlayable", actor, StringComparison.Ordinal);
        Assert.Contains("WalkToAttackBlendSeconds", actor, StringComparison.Ordinal);
        Assert.Contains("SwordBladeMeters", actor, StringComparison.Ordinal);
        Assert.Contains("useFileScale = false", actor, StringComparison.Ordinal);
        Assert.Contains("ApplyAttackWindupLift", actor, StringComparison.Ordinal);
        Assert.Contains("AttackSlashReach", actor, StringComparison.Ordinal);
        Assert.Contains("RightForeArm", actor, StringComparison.Ordinal);
        Assert.Contains("max-reach", actor, StringComparison.Ordinal);
        Assert.Contains("HeldSwordRestEulerX", actor, StringComparison.Ordinal);
        Assert.Contains("FromToRotation", actor, StringComparison.Ordinal);
        Assert.Contains("SnapHeldSwordToArmAxis", actor, StringComparison.Ordinal);
        Assert.Contains("SwordRestLocal", actor, StringComparison.Ordinal);
        Assert.Contains("ApplySheathedSword", actor, StringComparison.Ordinal);
        Assert.Contains("BindSwordTo", actor, StringComparison.Ordinal);
        Assert.Contains("mixamorig:Hips", actor, StringComparison.Ordinal);
        Assert.Contains("mixamorig:RightUpLeg", actor, StringComparison.Ordinal);
        Assert.Contains("if (k <= 0.001f)", actor, StringComparison.Ordinal);
        Assert.Contains("AimArmAlong(_rightArm", actor, StringComparison.Ordinal);
        Assert.Contains("HipSheathSocket", actor, StringComparison.Ordinal);
        Assert.Contains("Quaternion.Normalize", actor, StringComparison.Ordinal);
        Assert.Contains("HipSheathBladeLocal", actor, StringComparison.Ordinal);
        Assert.Contains("CharacterRight", actor, StringComparison.Ordinal);
        Assert.Contains("SnapSwordHiltTo", actor, StringComparison.Ordinal);
        Assert.Contains("HideEmbeddedSwords", actor, StringComparison.Ordinal);
        Assert.Contains("BladeLocalAxis", actor, StringComparison.Ordinal);
        Assert.Contains("7187204", actor, StringComparison.Ordinal);
        Assert.Contains("52aba6b", actor, StringComparison.Ordinal);
        Assert.Contains("0fc0930", actor, StringComparison.Ordinal);
        Assert.Contains("BindSwordTo(_rightHand", actor, StringComparison.Ordinal);
        Assert.Contains("Vector3.right", actor, StringComparison.Ordinal);
        Assert.Contains("strike points along AttackSlashReach", actor, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("iQ1s3nN1330", actor, StringComparison.Ordinal);
        Assert.Contains("AimArmAlong", actor, StringComparison.Ordinal);
        Assert.Contains("AttackSlashGuardReach", actor, StringComparison.Ordinal);
        Assert.Contains("AttackSlashSpineYawDegrees", actor, StringComparison.Ordinal);
        Assert.Contains("mixamorig:LeftArm", actor, StringComparison.Ordinal);
        Assert.Contains("backswing", actor, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("879a6f3", actor, StringComparison.Ordinal);
        Assert.Contains("beginCameraRendering", actor, StringComparison.Ordinal);
        Assert.Contains("LateUpdate", actor, StringComparison.Ordinal);
        Assert.Contains("DefaultExecutionOrder(200)", actor, StringComparison.Ordinal);
        Assert.Contains("MeasureLocalBladeAxis", actor, StringComparison.Ordinal);
        Assert.Contains("_swordLocalBlade", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("FromToRotation(restBlade.normalized, desired)", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("Slerp(restWorld, aimWorld", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("FromToRotation(Vector3.forward, desired)", actor, StringComparison.Ordinal);
        Assert.Contains("mixamorig:RightHand", actor, StringComparison.Ordinal);
        Assert.Contains("CacheAttackBones", actor, StringComparison.Ordinal);
        Assert.Contains("never a whole-body X-flip", actor, StringComparison.Ordinal);
        Assert.Contains("Do not scale.x = -1", actor, StringComparison.Ordinal);
        Assert.Contains("Mathf.Abs(s.x)", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("MixamoRearMirrorX", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("AttackWindupShoulderX", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("AttackWindupArmX", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("AttackWindupHandX", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("AttackWindupSwordZ", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("AttackWindupLiftMaxDegrees", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("AttackWindupTipRight", actor, StringComparison.Ordinal);
        var motionSrc = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Runtime", "Heroes", "SirAldric3DMotion.cs"));
        Assert.DoesNotContain("MixamoRearMirrorX", motionSrc, StringComparison.Ordinal);
        Assert.DoesNotContain("AttackWindupShoulderX", motionSrc, StringComparison.Ordinal);
        Assert.DoesNotContain("SetSourcePlayable(_attackPlayable)", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("EnsureClipSword", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("private static void AimChain", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("Path A weight-paint", actor.Replace("Path A / ClipSword", ""), StringComparison.Ordinal);

        var pilot = Path.Combine(root, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "heroes", "3d", "pilot");
        Assert.True(new FileInfo(Path.Combine(pilot, "SirAldric_body_holefixed_mid280k.fbx")).Length > 1_000_000);
        Assert.True(new FileInfo(Path.Combine(pilot, "SirAldric_body_holefixed_walk.fbx")).Length > 1_000_000);
        Assert.True(new FileInfo(Path.Combine(pilot, "SirAldric_body_holefixed_slash.fbx")).Length > 1_000_000);
        Assert.True(new FileInfo(Path.Combine(pilot, "SirAldric_PILOT_sword.fbx")).Length > 10_000);
        Assert.True(new FileInfo(Path.Combine(pilot, "mixamo_tex", "Meshy_AI_Lionheart_Sentinel_0929004215_texture.png")).Length > 100_000);
        var walkMeta = File.ReadAllText(Path.Combine(pilot, "SirAldric_body_holefixed_walk.fbx.meta"));
        var attackMeta = File.ReadAllText(Path.Combine(pilot, "SirAldric_body_holefixed_slash.fbx.meta"));
        Assert.Contains("takeName: mixamo.com", walkMeta, StringComparison.Ordinal);
        Assert.Contains("takeName: mixamo.com", attackMeta, StringComparison.Ordinal);
        Assert.Contains("avatarSetup: 1", walkMeta, StringComparison.Ordinal);
        Assert.Contains("avatarSetup: 1", attackMeta, StringComparison.Ordinal);
        Assert.Contains("animationType: 2", walkMeta, StringComparison.Ordinal);
        Assert.Contains("animationType: 2", attackMeta, StringComparison.Ordinal);
        Assert.Contains("useFileScale: 0", walkMeta, StringComparison.Ordinal);
        Assert.Contains("useFileScale: 0", attackMeta, StringComparison.Ordinal);
        Assert.Contains("firstFrame: 8", attackMeta, StringComparison.Ordinal);
        Assert.Contains("lastFrame: 40", attackMeta, StringComparison.Ordinal);
        Assert.Equal(8, SirAldric3DMotion.MixamoSlashFirstFrame);
        Assert.Equal(40, SirAldric3DMotion.MixamoSlashLastFrame);
        Assert.DoesNotContain("animationType: 3", walkMeta, StringComparison.Ordinal);
        Assert.DoesNotContain("animationType: 3", attackMeta, StringComparison.Ordinal);
        Assert.DoesNotContain("addHumanoidExtraRoot", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("legTwist", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("armTwist", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("foreArmTwist", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("upperLegTwist", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("feetSpacing", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("hasTranslationDoF", actor, StringComparison.Ordinal);
        var post = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "SirAldricPilotFbxImport.cs"));
        Assert.Contains("OnPreprocessModel", post, StringComparison.Ordinal);
        Assert.Contains("CreateFromThisModel", post, StringComparison.Ordinal);
        Assert.Contains("SirAldric_body_holefixed_walk.fbx", post, StringComparison.Ordinal);
        Assert.Contains("SirAldric_body_holefixed_slash.fbx", post, StringComparison.Ordinal);
        Assert.Contains("ModelImporterAnimationType.Generic", post, StringComparison.Ordinal);
        Assert.Contains("useFileScale = false", post, StringComparison.Ordinal);
        Assert.Contains("MixamoSlashFirstFrame", post, StringComparison.Ordinal);
        Assert.DoesNotContain("ModelImporterAnimationType.Human", post, StringComparison.Ordinal);
        Assert.DoesNotContain("addHumanoidExtraRoot", post, StringComparison.Ordinal);
        Assert.DoesNotContain("addHumanoidExtraRoot", walkMeta, StringComparison.Ordinal);
        Assert.DoesNotContain("addHumanoidExtraRoot", attackMeta, StringComparison.Ordinal);
        Assert.Equal(1.92f, SirAldric3DMotion.PlayCamRearY);
        Assert.Equal(-3.08f, SirAldric3DMotion.PlayCamRearZ);
        Assert.Equal(1.12f, SirAldric3DMotion.PlayCamLookY);
        Assert.Equal(42f, SirAldric3DMotion.PlayCamFovDegrees);
        Assert.Equal(180f, SirAldric3DMotion.MixamoImportRearYawDegrees);
        Assert.Equal(0.42f, SirAldric3DMotion.AttackSlashReadyRight);
        Assert.Equal(-0.48f, SirAldric3DMotion.AttackSlashReadyUp);
        Assert.Equal(-0.22f, SirAldric3DMotion.AttackSlashReadyFront);
        Assert.Equal(0.55f, SirAldric3DMotion.AttackSlashBackRight);
        Assert.Equal(0.05f, SirAldric3DMotion.AttackSlashBackUp);
        Assert.Equal(-0.78f, SirAldric3DMotion.AttackSlashBackFront);
        Assert.Equal(0.22f, SirAldric3DMotion.AttackSlashBackU);
        Assert.Equal(0.68f, SirAldric3DMotion.AttackSlashUrRight);
        Assert.Equal(0.82f, SirAldric3DMotion.AttackSlashUrUp);
        Assert.Equal(-0.12f, SirAldric3DMotion.AttackSlashUrFront);
        Assert.Equal(0.06f, SirAldric3DMotion.AttackSlashFrontUp);
        Assert.Equal(0.72f, SirAldric3DMotion.AttackSlashFrontZ);
        Assert.Equal(-0.76f, SirAldric3DMotion.AttackSlashLlRight);
        Assert.Equal(-0.80f, SirAldric3DMotion.AttackSlashLlUp);
        Assert.Equal(0.40f, SirAldric3DMotion.AttackSlashFrontU);
        Assert.Equal(0.58f, SirAldric3DMotion.AttackSlashFrontHoldU);
        Assert.Equal(0.84f, SirAldric3DMotion.AttackSlashLlU);
        Assert.True(SirAldric3DMotion.AttackSlashFrontHoldU > SirAldric3DMotion.AttackSlashFrontU);
        Assert.True(SirAldric3DMotion.AttackSlashBackFront < 0f);
        Assert.Equal(90f, SirAldric3DMotion.HeldSwordRestEulerX);
        Assert.Equal(0.32f, SirAldric3DMotion.HipSheathOutboard);
        Assert.Equal(0.02f, SirAldric3DMotion.HipSheathUp);
        Assert.Equal(0.06f, SirAldric3DMotion.HipSheathBack);
        Assert.Equal(0.40f, SirAldric3DMotion.HeldSwordSheathRight);
        Assert.Equal(1.00f, SirAldric3DMotion.HeldSwordSheathDown);
        Assert.Equal(0.00f, SirAldric3DMotion.HeldSwordSheathBack);
        SirAldric3DMotion.HipSheathBladeLocal(out var shR, out var shU, out var shF);
        Assert.True(shR > 0.25f && shU < -0.70f && Math.Abs(shF) < 0.20f, $"sheath local ({shR},{shU},{shF}) must be character-right + down, not world-left");
        SirAldric3DMotion.PlayCamOrbitEye(0f, 0f, out var eyeX, out var eyeY, out var eyeZ);
        Assert.Equal(SirAldric3DMotion.PlayCamRearX, eyeX, 3);
        Assert.Equal(SirAldric3DMotion.PlayCamRearY, eyeY, 3);
        Assert.Equal(SirAldric3DMotion.PlayCamRearZ, eyeZ, 3);
        Assert.InRange(SirAldric3DMotion.PlayCamThreeQuarterYawDegrees(), -80f, -20f);
        Assert.Equal(1f, SirAldric3DMotion.AttackWindupWeight(0f), 3);
        Assert.Equal(1f, SirAldric3DMotion.AttackWindupWeight(0.72f), 3);
        Assert.Equal(1f, SirAldric3DMotion.AttackWindupWeight(1f), 3);
        SirAldric3DMotion.AttackSlashReach(0f, out var readyX, out var readyY, out var readyZ);
        Assert.True(readyX > 0.25f && readyY < 0f && readyZ <= 0f, $"ready ({readyX},{readyY},{readyZ}) must be low character-right / back (video t≈0)");
        SirAldric3DMotion.AttackSlashReach(SirAldric3DMotion.AttackSlashBackU, out var backX, out var backY, out var backZ);
        Assert.True(backX > 0.20f && backZ < -0.45f, $"backswing ({backX},{backY},{backZ}) must put the blade behind the body (video t≈5s)");
        SirAldric3DMotion.AttackSlashReach(SirAldric3DMotion.AttackSlashFrontU, out var urX, out var urY, out var urZ);
        Assert.True(urX > 0.40f && urY > 0.45f, $"high ({urX},{urY},{urZ}) must be high character-right");
        SirAldric3DMotion.AttackSlashReach(SirAldric3DMotion.AttackSlashFrontHoldU, out var cutX, out var cutY, out var cutZ);
        Assert.True(cutZ > 0.40f, $"contact ({cutX},{cutY},{cutZ}) must be in front of the body");
        SirAldric3DMotion.AttackSlashReach(1f, out var llX, out var llY, out var llZ);
        Assert.True(llX < -0.45f && llY < -0.45f, $"finish ({llX},{llY},{llZ}) must be low character-left");
        SirAldric3DMotion.AttackSlashGuardReach(out var gX, out var gY, out var gZ);
        Assert.True(gZ > 0.70f && gY > 0f, $"guard ({gX},{gY},{gZ}) must point forward");
        Assert.InRange(SirAldric3DMotion.AttackSlashSpineYawDegrees(0f), 15f, 30f);
        Assert.InRange(SirAldric3DMotion.AttackSlashSpineYawDegrees(1f), -25f, -8f);
        _ = readyZ;
        _ = backY;
        _ = urZ;
        _ = cutX;
        _ = cutY;
        _ = llZ;
        _ = gX;
        var blender = File.ReadAllText(Path.Combine(root, "scripts", "blender", "render_aldric_pilot_playcam.py"));
        Assert.Contains("SLASH_READY_UP = -0.48", blender, StringComparison.Ordinal);
        Assert.Contains("SLASH_BACK_FRONT = -0.78", blender, StringComparison.Ordinal);
        Assert.Contains("SLASH_BACK_U = 0.22", blender, StringComparison.Ordinal);
        Assert.Contains("SLASH_FRONT_UP = 0.06", blender, StringComparison.Ordinal);
        Assert.Contains("SLASH_FRONT_HOLD_U = 0.58", blender, StringComparison.Ordinal);
        Assert.Contains("SLASH_FRONT_Z = 0.72", blender, StringComparison.Ordinal);
        Assert.Contains("SLASH_UR_UP = 0.82", blender, StringComparison.Ordinal);
        Assert.Contains("iQ1s3nN1330", blender, StringComparison.Ordinal);
        Assert.Contains("hip_sheath_", blender, StringComparison.Ordinal);
        Assert.Contains("apply_sheathed_sword", blender, StringComparison.Ordinal);
        Assert.Contains("mixamorig:Hips", blender, StringComparison.Ordinal);
        Assert.Contains("bind_sword_to", blender, StringComparison.Ordinal);
        Assert.Contains("SHEATH_BACK = 0.00", blender, StringComparison.Ordinal);
        Assert.Contains("SHEATH_RIGHT = 0.40", blender, StringComparison.Ordinal);
        Assert.Contains("forearm–hand", blender, StringComparison.Ordinal);
        Assert.DoesNotContain("rest_world.slerp", blender, StringComparison.Ordinal);
        Assert.Contains("not Unity Game-view", blender, StringComparison.Ordinal);
        var demo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "SirAldricDemo.cs"));
        Assert.Contains("ApplyPlayCam", demo, StringComparison.Ordinal);
        Assert.Contains("usePhysicalProperties = false", demo, StringComparison.Ordinal);
        Assert.Contains("LateUpdate", demo, StringComparison.Ordinal);
        Assert.Contains("ready→backswing→LL", demo, StringComparison.Ordinal);
        Assert.Contains("iQ1s3nN1330", demo, StringComparison.Ordinal);
        Assert.Contains("orbit", demo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PlayCamOrbitEye", demo, StringComparison.Ordinal);
        Assert.Contains("Alpha1", demo, StringComparison.Ordinal);
        Assert.Contains("Keyboard.current", demo, StringComparison.Ordinal);
        Assert.Contains("digit1Key", demo, StringComparison.Ordinal);
        Assert.Contains("OnGUI", demo, StringComparison.Ordinal);
        Assert.Contains("DefaultExecutionOrder(500)", demo, StringComparison.Ordinal);
        Assert.Contains("Quaternion.Normalize", demo, StringComparison.Ordinal);
        Assert.Contains("LookRotation", demo, StringComparison.Ordinal);
        Assert.DoesNotContain("cam.transform.LookAt", demo, StringComparison.Ordinal);
        Assert.DoesNotContain("ev.repeat", demo, StringComparison.Ordinal);
        Assert.Contains("ev.keyCode == KeyCode.None", demo, StringComparison.Ordinal);
        Assert.Contains("GUI.Button", demo, StringComparison.Ordinal);
        Assert.Contains("IgnoreFocus", demo, StringComparison.Ordinal);
        Assert.Contains("AllDeviceInputAlwaysGoesToGameView", demo, StringComparison.Ordinal);
        Assert.Contains("InputSystemUIInputModule", demo, StringComparison.Ordinal);
        Assert.Contains("InputActionMap", demo, StringComparison.Ordinal);
        Assert.Contains("SirAldricOrbitPad", demo, StringComparison.Ordinal);
        Assert.Contains("ForcePreset", demo, StringComparison.Ordinal);
        Assert.Contains("GetAllCameras", demo, StringComparison.Ordinal);
        var orbitHook = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "SirAldricPlayOrbitHook.cs"));
        Assert.Contains("EditorApplication.update", orbitHook, StringComparison.Ordinal);
        Assert.Contains("ForcePreset", orbitHook, StringComparison.Ordinal);
        SirAldric3DMotion.PlayCamOrbitEye(180f, 0f, out var frontX, out var frontY, out var frontZ);
        Assert.True(frontZ > SirAldric3DMotion.PlayCamLookZ, "yaw 180 must sit in front of LookAt");
        Assert.True(Math.Abs(frontZ - eyeZ) > 4f, "front eye must leave the rear socket");
        SirAldric3DMotion.PlayCamOrbitEye(SirAldric3DMotion.PlayCamThreeQuarterYawDegrees(), 0f, out var tqX, out var tqY, out var tqZ);
        Assert.True(Math.Abs(tqX) > 1f, "3/4 yaw must leave the rear X=0 line");
        _ = frontX;
        _ = frontY;
        _ = tqY;
        _ = tqZ;

        var dir = Path.Combine(root, "Docs", "Survival", "previews", "aldric_pilot_20260928");
        Assert.True(new FileInfo(Path.Combine(dir, "PILOT_HOLD.md")).Length > 400);
        var hold = File.ReadAllText(Path.Combine(dir, "PILOT_HOLD.md"));
        Assert.Contains("HOLD merge", hold, StringComparison.Ordinal);
        Assert.Contains("Not Unity Game-view", hold, StringComparison.Ordinal);
        Assert.Contains("Mixamo", hold, StringComparison.Ordinal);
        Assert.Contains("a9f8aff", hold, StringComparison.Ordinal);
        Assert.Contains("cdfbea5", hold, StringComparison.Ordinal);
        Assert.Contains("879a6f3", hold, StringComparison.Ordinal);
        Assert.Contains("8098f64", hold, StringComparison.Ordinal);
        Assert.Contains("7187204", hold, StringComparison.Ordinal);
        Assert.Contains("52aba6b", hold, StringComparison.Ordinal);
        Assert.Contains("0fc0930", hold, StringComparison.Ordinal);
        Assert.Contains("98dabbb", hold, StringComparison.Ordinal);
        Assert.Contains("3d0efcd", hold, StringComparison.Ordinal);
        Assert.Contains("backswing", hold, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("iQ1s3nN1330", hold, StringComparison.Ordinal);
        Assert.Contains("https://www.youtube.com/watch?v=iQ1s3nN1330", hold, StringComparison.Ordinal);
        Assert.Contains("CS1061", hold, StringComparison.Ordinal);
        Assert.Contains("QuaternionToEuler", hold, StringComparison.Ordinal);
        Assert.Contains("HipSheathSocket", hold, StringComparison.Ordinal);
        Assert.Contains("orbit", hold, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("mixamorig:Hips", hold, StringComparison.Ordinal);
        Assert.Contains("RightHand", hold, StringComparison.Ordinal);
        Assert.Contains("not a whole-body X-flip", hold, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("b0a9509", hold, StringComparison.Ordinal);
        Assert.Contains("b2ce8f7", hold, StringComparison.Ordinal);
        Assert.Contains("19b16aa", hold, StringComparison.Ordinal);
        Assert.Contains("horizontal thrust", hold, StringComparison.OrdinalIgnoreCase);
        Assert.True(new FileInfo(Path.Combine(dir, "sir_aldric_pilot_rear_walk_playcam.png")).Length > 50_000);
        Assert.True(new FileInfo(Path.Combine(dir, "sir_aldric_pilot_front_walk_playcam.png")).Length > 50_000);
        Assert.True(new FileInfo(Path.Combine(dir, "sir_aldric_pilot_34_walk_playcam.png")).Length > 50_000);
        Assert.True(new FileInfo(Path.Combine(dir, "sir_aldric_pilot_rear_strike_start_playcam.png")).Length > 50_000);
        Assert.True(new FileInfo(Path.Combine(dir, "sir_aldric_pilot_rear_strike_mid_playcam.png")).Length > 50_000);
        Assert.True(new FileInfo(Path.Combine(dir, "sir_aldric_pilot_rear_strike_playcam.png")).Length > 50_000);
        Assert.True(new FileInfo(Path.Combine(dir, "sir_aldric_pilot_front_strike_playcam.png")).Length > 50_000);
        Assert.True(new FileInfo(Path.Combine(dir, "sir_aldric_pilot_34_strike_playcam.png")).Length > 50_000);
        var walkDump = File.ReadAllText(Path.Combine(dir, "sir_aldric_pilot_dump_walk_f18.json"));
        Assert.Contains("\"sword_parent\": \"mixamorig:Hips\"", walkDump, StringComparison.Ordinal);
        Assert.Contains("not Unity Game-view", walkDump, StringComparison.Ordinal);
        var walkTip = ReadJsonVec3(walkDump, "tip_unity");
        Assert.True(walkTip[1] < -0.70f, $"walk tip_unity=({walkTip[0]},{walkTip[1]},{walkTip[2]}) must point down, not a RH grip");
        Assert.True(Math.Abs(walkTip[2]) < 0.25f, $"walk tip Z {walkTip[2]} must not aim across the back (Derek a9f8aff Game-view)");
        var gripToRhIdx = walkDump.IndexOf("\"grip_to_rh\":", StringComparison.Ordinal);
        Assert.True(gripToRhIdx >= 0, "walk dump missing grip_to_rh");
        var colon = walkDump.IndexOf(':', gripToRhIdx);
        var comma = walkDump.IndexOfAny(new[] { ',', '\n' }, colon + 1);
        var gripToRh = float.Parse(walkDump.Substring(colon + 1, comma - colon - 1).Trim(), CultureInfo.InvariantCulture);
        Assert.True(gripToRh > 0.05f, $"walk grip_to_rh={gripToRh} must sit outside the RH palm");
        var midDump = File.ReadAllText(Path.Combine(dir, "sir_aldric_pilot_dump_slash_f20.json"));
        Assert.Contains("\"sword_parent\": \"mixamorig:RightHand\"", midDump, StringComparison.Ordinal);
        Assert.Contains("not Unity Game-view", midDump, StringComparison.Ordinal);
        // Rematch dumps are optional / not slash SoT. Do not lock mid to the
        // superseded +Z thrust plateau. Game-view must match iQ1s3nN1330.
        _ = ReadJsonVec3(midDump, "tip_unity");
        _ = ReadJsonVec3(midDump, "desired_unity");
        var startTip = ReadJsonVec3(File.ReadAllText(Path.Combine(dir, "sir_aldric_pilot_dump_slash_f8.json")), "tip_unity");
        Assert.True(startTip[1] > 0.25f, $"start tip Y {startTip[1]} should be up");
        var finishTip = ReadJsonVec3(File.ReadAllText(Path.Combine(dir, "sir_aldric_pilot_dump_slash_f32.json")), "tip_unity");
        Assert.True(finishTip[1] < 0f, $"finish tip Y {finishTip[1]} should be down");
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

    private static float[] ReadJsonVec3(string json, string key)
    {
        var needle = "\"" + key + "\"";
        var i = json.IndexOf(needle, StringComparison.Ordinal);
        Assert.True(i >= 0, "missing JSON key " + key);
        var lb = json.IndexOf('[', i);
        Assert.True(lb > i, "missing JSON array for " + key);
        var rb = json.IndexOf(']', lb);
        Assert.True(rb > lb, "unterminated JSON array for " + key);
        var parts = json.Substring(lb + 1, rb - lb - 1).Split(',');
        Assert.True(parts.Length >= 3, key + " needs 3 floats");
        return new[]
        {
            float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
            float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
            float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
        };
    }
}
