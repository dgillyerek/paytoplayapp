using Survival.Domain.Enemies;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

public sealed class LockedBattleRosterTests
{
    [Fact]
    public void Roster_is_every_locked_merged_character_with_walk_and_attack_split_six_against_six()
    {
        Assert.Equal(new[] { "Emberfang", "Stormcrest", "Ironhowl", "Nightfang", "Lyra", "Oakenshield", "Vespera", "Ashwyrm", "Sir Aldric", "Blightroot", "Rowan", "Bonequill" }, LockedBattleRoster.All.Select(f => f.Name).ToArray());
        Assert.Equal(new[] { "Emberfang", "Stormcrest", "Lyra", "Oakenshield", "Sir Aldric", "Rowan" }, LockedBattleRoster.OnSide(BattleSide.Hero).Select(f => f.Name).ToArray());
        Assert.Equal(new[] { "Ironhowl", "Nightfang", "Vespera", "Ashwyrm", "Blightroot", "Bonequill" }, LockedBattleRoster.OnSide(BattleSide.Villain).Select(f => f.Name).ToArray());

        var root = FindRepoRoot();
        foreach (var f in LockedBattleRoster.All)
        {
            var folder = f.Side == BattleSide.Hero ? "Heroes" : "Enemies";
            Assert.True(File.Exists(Path.Combine(root, "Assets", "Survival", "Runtime", folder, f.FileKey + "Motion.cs")), f.Name);
            Assert.True(File.Exists(Path.Combine(root, "Assets", "Survival", "Unity", f.ActorClassName + ".cs")), f.Name);
            Assert.True(File.Exists(Path.Combine(root, "Assets", "Survival", "Scenes", f.FileKey + ".unity")), f.Name);
            Assert.True(f.DesignHeightMetres > 0.5f && f.DesignHeightMetres < 2.5f, f.Name);
            Assert.True(f.AttackRangeMetres > 1f && f.AttackRangeMetres < LockedBattleRoster.StartHalfGapMetres * 2f, f.Name);
            Assert.True(f.AttackSeconds > 0.5f, f.Name);
            Assert.True(f.WalkCarriesRoot ? f.StrideBone.Length > 0 : f.WalkSpeedMetresPerSecond > 0f, f.Name);
            Assert.Equal("rest", f.RestPoseName);
            Assert.Equal("attack", f.AttackPoseName);
        }

        Assert.Contains(LockedBattleRoster.Emberfang.WalkPoseName, EmberfangMotion.PoseNames);
        Assert.Contains(LockedBattleRoster.Emberfang.AttackPoseName, EmberfangMotion.PoseNames);
        Assert.Contains(LockedBattleRoster.Stormcrest.WalkPoseName, StormcrestMotion.Spec.PoseNames);
        Assert.Contains(LockedBattleRoster.Stormcrest.AttackPoseName, StormcrestMotion.Spec.PoseNames);
        Assert.Equal("trot", LockedBattleRoster.Nightfang.WalkPoseName);
        Assert.Contains(LockedBattleRoster.Nightfang.WalkPoseName, NightfangMotion.Spec.PoseNames);
        Assert.Contains(LockedBattleRoster.Nightfang.AttackPoseName, NightfangMotion.Spec.PoseNames);
        Assert.Contains(LockedBattleRoster.Ironhowl.WalkPoseName, IronhowlMotion.PoseNames);
        Assert.Contains(LockedBattleRoster.Ironhowl.AttackPoseName, IronhowlMotion.PoseNames);
        Assert.Contains(LockedBattleRoster.Lyra.WalkPoseName, LyraMotion.Spec.PoseNames);
        Assert.Contains(LockedBattleRoster.Lyra.AttackPoseName, LyraMotion.Spec.PoseNames);
        Assert.Contains(LockedBattleRoster.Oakenshield.WalkPoseName, OakenshieldMotion.Spec.PoseNames);
        Assert.Contains(LockedBattleRoster.Oakenshield.AttackPoseName, OakenshieldMotion.Spec.PoseNames);
        Assert.Contains(LockedBattleRoster.Vespera.WalkPoseName, VesperaMotion.Spec.PoseNames);
        Assert.Contains(LockedBattleRoster.Vespera.AttackPoseName, VesperaMotion.Spec.PoseNames);
        Assert.Contains(LockedBattleRoster.Ashwyrm.WalkPoseName, AshwyrmMotion.Spec.PoseNames);
        Assert.Contains(LockedBattleRoster.Ashwyrm.AttackPoseName, AshwyrmMotion.Spec.PoseNames);

        // Each added fighter uses its own attack clip length: Vespera's slowed conjure runs f0-36.
        Assert.Equal(VesperaAttack.ClipSeconds, LockedBattleRoster.Vespera.AttackSeconds);
        Assert.Equal(BlenderRigAttackSpec.ClipSeconds, LockedBattleRoster.Lyra.AttackSeconds);
        Assert.Equal(BlenderRigAttackSpec.ClipSeconds, LockedBattleRoster.Oakenshield.AttackSeconds);
        Assert.Equal(BlenderRigAttackSpec.ClipSeconds, LockedBattleRoster.Ashwyrm.AttackSeconds);

        // Lane 5: Sir Aldric plays Lite pack clips on his locked pilot actor; Blightroot's
        // creature-pack walk carries its hips like Ironhowl's.
        Assert.Equal("SirAldricMeshyAnimateActor", LockedBattleRoster.SirAldric.ActorClassName);
        Assert.Equal("BlightrootActor", LockedBattleRoster.Blightroot.ActorClassName);
        Assert.Equal("walk", LockedBattleRoster.SirAldric.WalkPoseName);
        Assert.True(SirAldric3DMotion.TryLiteSwordShield(SirAldricBattlePoses.RestClip, out var aldricRest) && aldricRest.Loop);
        Assert.True(SirAldric3DMotion.TryLiteSwordShield(SirAldricBattlePoses.AttackClip, out var aldricAttack) && !aldricAttack.Loop);
        Assert.False(LockedBattleRoster.SirAldric.WalkCarriesRoot);
        Assert.False(LockedBattleRoster.SirAldric.AttackCyclesItself);
        Assert.Equal(33f / 30f, LockedBattleRoster.SirAldric.AttackSeconds, 4);
        Assert.True(BlightrootMotion.TryGet(BlightrootBattlePoses.RestClip, out var bIdle) && bIdle.Loop);
        Assert.True(BlightrootMotion.TryGet(BlightrootBattlePoses.WalkClip, out var bWalk) && bWalk.Loop);
        Assert.True(BlightrootMotion.TryGet(BlightrootBattlePoses.AttackClip, out var bSwipe) && !bSwipe.Loop);
        Assert.Equal(bSwipe.LastFrame / 30f, LockedBattleRoster.Blightroot.AttackSeconds, 4);
        Assert.True(LockedBattleRoster.Blightroot.WalkCarriesRoot);
        Assert.Equal(BlightrootMotion.BoneRoot, LockedBattleRoster.Blightroot.StrideBone);
        Assert.False(LockedBattleRoster.Blightroot.AttackCyclesItself);
        // Stride matches the clip's measured hips travel (HipZMax is the per-loop travel).
        Assert.InRange(LockedBattleRoster.Blightroot.WalkRootStrideMetres, bWalk.HipZMax - 0.01f, bWalk.HipZMax + 0.01f);

        Assert.True(LockedBattleRoster.Ironhowl.WalkCarriesRoot);
        Assert.Equal(IronhowlMotion.BoneRoot, LockedBattleRoster.Ironhowl.StrideBone);
        Assert.False(LockedBattleRoster.Ironhowl.AttackCyclesItself);
        Assert.Equal(IronhowlMotion.AttackSeconds, LockedBattleRoster.Ironhowl.AttackSeconds);
        foreach (var f in new[] { LockedBattleRoster.Emberfang, LockedBattleRoster.Stormcrest, LockedBattleRoster.Nightfang, LockedBattleRoster.Lyra, LockedBattleRoster.Oakenshield, LockedBattleRoster.Vespera, LockedBattleRoster.Ashwyrm })
        {
            Assert.False(f.WalkCarriesRoot);
            Assert.True(f.AttackCyclesItself);
        }

        // Display names only: no space-less duplicate of Sir Aldric.
        var notInBattle = new[] { "SirAldric" };
        foreach (var name in notInBattle)
        {
            Assert.DoesNotContain(LockedBattleRoster.All, f => string.Equals(f.Name, name, StringComparison.Ordinal));
        }

        Assert.Empty(LockedBattleRoster.LeftOut);
    }

    [Fact]
    public void Battle_roster_is_locked_at_exactly_twelve_fighters_and_only_the_lane_five_Blightroot_survives_the_cleanup()
    {
        Assert.Equal(
            new[] { "Emberfang", "Stormcrest", "Ironhowl", "Nightfang", "Lyra", "Oakenshield", "Vespera", "Ashwyrm", "Sir Aldric", "Blightroot", "Rowan", "Bonequill" },
            LockedBattleRoster.All.Select(f => f.Name).ToArray());
        Assert.Equal(12, LockedBattleRoster.All.Count);
        Assert.Equal(6, LockedBattleRoster.OnSide(BattleSide.Hero).Count());
        Assert.Equal(6, LockedBattleRoster.OnSide(BattleSide.Villain).Count());
        Assert.Single(LockedBattleRoster.All, f => f.Name == "Blightroot");
        Assert.Single(LockedBattleRoster.All, f => f.Name == "Sir Aldric");
        Assert.Single(LockedBattleRoster.All, f => f.Name == "Rowan");
        Assert.Single(LockedBattleRoster.All, f => f.Name == "Bonequill");
        var demo = File.ReadAllText(Path.Combine(FindRepoRoot(), "Assets", "Survival", "Unity", "LockedBattleDemo.cs"));
        // Stray demo Blightroots (an open Blightroot scene) are still stripped; the battle's own
        // lane-5 Blightroot under its root is the one exception, and it spawns after the strip.
        Assert.Contains("StripLeftoverDemoActors", demo, StringComparison.Ordinal);
        Assert.Contains("FindObjectsByType<BlightrootActor>", demo, StringComparison.Ordinal);
        Assert.Contains("FindObjectsByType<BlightrootDemo>", demo, StringComparison.Ordinal);
        Assert.Contains("!IsOwnLaneFighter(actor.transform)", demo, StringComparison.Ordinal);
        Assert.Contains("actor.IsChildOf(transform)", demo, StringComparison.Ordinal);
        Assert.Contains("pivot.AddComponent<BlightrootActor>()", demo, StringComparison.Ordinal);
        Assert.True(demo.IndexOf("StripLeftoverDemoActors();", StringComparison.Ordinal) < demo.IndexOf("var fighter = Spawn(data);", StringComparison.Ordinal));
        var menu = File.ReadAllText(Path.Combine(FindRepoRoot(), "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("OpenSceneMode.Single", menu, StringComparison.Ordinal);
    }

    [Fact]
    public void Added_lanes_sit_outside_the_original_two_and_pair_one_hero_with_one_villain()
    {
        // #57 matchups stay in their lanes; the lanes spread out for the 2x dragons (2026-10-18).
        Assert.Equal(0, LockedBattleRoster.Emberfang.Lane);
        Assert.Equal(0, LockedBattleRoster.Ironhowl.Lane);
        Assert.Equal(1, LockedBattleRoster.Stormcrest.Lane);
        Assert.Equal(1, LockedBattleRoster.Nightfang.Lane);
        Assert.Equal(-1.35f, LockedBattleRoster.LaneX(0), 4);
        Assert.Equal(1.35f, LockedBattleRoster.LaneX(1), 4);
        Assert.Equal(-LockedBattleRoster.OuterLaneHalfSpacingMetres, LockedBattleRoster.LaneX(2), 4);
        Assert.Equal(LockedBattleRoster.RightOuterLaneHalfSpacingMetres, LockedBattleRoster.LaneX(3), 4);
        Assert.True(LockedBattleRoster.OuterLaneHalfSpacingMetres - LockedBattleRoster.LaneHalfSpacingMetres >= 1.2f);

        foreach (var lane in new[] { 0, 1, 2, 3, 4, 5 })
        {
            Assert.Single(LockedBattleRoster.All, f => f.Lane == lane && f.Side == BattleSide.Hero);
            Assert.Single(LockedBattleRoster.All, f => f.Lane == lane && f.Side == BattleSide.Villain);
        }

        Assert.Equal("Vespera", LockedBattleRoster.TargetOf(LockedBattleRoster.Lyra).Name);
        Assert.Equal("Lyra", LockedBattleRoster.TargetOf(LockedBattleRoster.Vespera).Name);
        Assert.Equal("Ashwyrm", LockedBattleRoster.TargetOf(LockedBattleRoster.Oakenshield).Name);
        Assert.Equal("Oakenshield", LockedBattleRoster.TargetOf(LockedBattleRoster.Ashwyrm).Name);

        // Lane 5 sits outside lane 2 on the left and pairs Sir Aldric with Blightroot.
        Assert.Equal(4, LockedBattleRoster.SirAldric.Lane);
        Assert.Equal(4, LockedBattleRoster.Blightroot.Lane);
        Assert.Equal(-LockedBattleRoster.FifthLaneHalfSpacingMetres, LockedBattleRoster.LaneX(4), 4);
        Assert.True(LockedBattleRoster.FifthLaneHalfSpacingMetres - LockedBattleRoster.OuterLaneHalfSpacingMetres >= 1.2f);
        Assert.Equal(6, LockedBattleRoster.LaneCount);
        Assert.Equal("Blightroot", LockedBattleRoster.TargetOf(LockedBattleRoster.SirAldric).Name);
        Assert.Equal("Sir Aldric", LockedBattleRoster.TargetOf(LockedBattleRoster.Blightroot).Name);

        // Lane 6 sits outside lane 3 on the right, clear of 2x Ashwyrm's wingspan, and pairs the archers.
        Assert.Equal(5, LockedBattleRoster.Rowan.Lane);
        Assert.Equal(5, LockedBattleRoster.Bonequill.Lane);
        Assert.Equal(LockedBattleRoster.SixthLaneHalfSpacingMetres, LockedBattleRoster.LaneX(5), 4);
        Assert.True(LockedBattleRoster.SixthLaneHalfSpacingMetres - 0.47f > LockedBattleRoster.RightOuterLaneHalfSpacingMetres + 0.95f * LockedBattleRoster.AshwyrmBattleScale * 0.85f);
        Assert.Equal("Bonequill", LockedBattleRoster.TargetOf(LockedBattleRoster.Rowan).Name);
        Assert.Equal("Rowan", LockedBattleRoster.TargetOf(LockedBattleRoster.Bonequill).Name);
        Assert.Equal("Ashwyrm", LockedBattleRoster.TargetOf(LockedBattleRoster.Oakenshield).Name);
        Assert.Equal("Oakenshield", LockedBattleRoster.TargetOf(LockedBattleRoster.Ashwyrm).Name);
    }

    [Fact]
    public void Heroes_and_villains_start_on_opposite_ends_facing_each_other_and_lock_their_lane_opponent()
    {
        foreach (var f in LockedBattleRoster.All)
        {
            LockedBattleRoster.StartPoint(f, out var x, out var z);
            Assert.Equal(LockedBattleRoster.LaneX(f.Lane), x);
            Assert.Equal(f.Side == BattleSide.Hero ? -LockedBattleRoster.StartHalfGapMetres : LockedBattleRoster.StartHalfGapMetres, z);
            Assert.Equal(f.Side == BattleSide.Hero ? 0f : 180f, LockedBattleRoster.StartYawDegrees(f));
            var target = LockedBattleRoster.TargetOf(f);
            Assert.NotEqual(f.Side, target.Side);
            Assert.Equal(f.Lane, target.Lane);
        }

        Assert.Equal("Ironhowl", LockedBattleRoster.TargetOf(LockedBattleRoster.Emberfang).Name);
        Assert.Equal("Emberfang", LockedBattleRoster.TargetOf(LockedBattleRoster.Ironhowl).Name);
        Assert.Equal("Nightfang", LockedBattleRoster.TargetOf(LockedBattleRoster.Stormcrest).Name);
        Assert.Equal("Stormcrest", LockedBattleRoster.TargetOf(LockedBattleRoster.Nightfang).Name);
    }

    [Fact]
    public void Approach_stops_at_range_root_walk_loops_are_caught_and_ironhowl_rests_between_attacks()
    {
        Assert.Equal(0.1f, LockedBattleRoster.ApproachStep(5f, 3f, 0.5f, 0.2f), 4);
        Assert.Equal(0.25f, LockedBattleRoster.ApproachStep(3.25f, 3f, 2f, 0.5f), 4);
        Assert.Equal(0f, LockedBattleRoster.ApproachStep(3f, 3f, 2f, 0.5f));
        Assert.Equal(0f, LockedBattleRoster.ApproachStep(2f, 3f, 2f, 0.5f));
        Assert.Equal(0f, LockedBattleRoster.ApproachStep(5f, 3f, 0f, 0.5f));

        var stride = LockedBattleRoster.Ironhowl.WalkRootStrideMetres;
        Assert.Equal(0f, LockedBattleRoster.LoopSnapBack(0.5f, 0.56f, stride));
        Assert.Equal(0f, LockedBattleRoster.LoopSnapBack(0.5f, 0.45f, stride));
        Assert.Equal(1.85f, LockedBattleRoster.LoopSnapBack(1.9f, 0.05f, stride), 4);
        Assert.Equal(0f, LockedBattleRoster.LoopSnapBack(1.9f, 0.05f, 0f));

        var iron = LockedBattleRoster.Ironhowl;
        Assert.True(LockedBattleRoster.InAttackClip(iron, 0f));
        Assert.True(LockedBattleRoster.InAttackClip(iron, iron.AttackSeconds - 0.01f));
        Assert.False(LockedBattleRoster.InAttackClip(iron, iron.AttackSeconds + 0.01f));
        Assert.False(LockedBattleRoster.InAttackClip(iron, iron.AttackCycleSeconds - 0.01f));
        Assert.True(LockedBattleRoster.InAttackClip(iron, iron.AttackCycleSeconds + 0.01f));
        Assert.False(LockedBattleRoster.InAttackClip(iron, -0.1f));
        Assert.Equal(BlenderRigAttackSpec.RestGapSeconds, iron.AttackCycleSeconds - iron.AttackSeconds, 4);
    }

    [Fact]
    public void Battle_scene_menu_and_build_list_are_wired_and_reuse_the_locked_actors()
    {
        var root = FindRepoRoot();
        var scenePath = Path.Combine(root, LockedBattleRoster.ScenePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(scenePath));
        var scene = File.ReadAllText(scenePath);
        var demoMeta = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "LockedBattleDemo.cs.meta"));
        var guid = demoMeta.Split('\n').First(l => l.StartsWith("guid: ", StringComparison.Ordinal)).Substring(6).Trim();
        Assert.Contains("BattleLockedRoot", scene, StringComparison.Ordinal);
        Assert.Contains("guid: " + guid, scene, StringComparison.Ordinal);
        Assert.Contains("Survival.Unity.LockedBattleDemo", scene, StringComparison.Ordinal);
        Assert.Contains("field of view: 54", scene, StringComparison.Ordinal);

        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("[MenuItem(\"" + LockedBattleRoster.MenuPath + "\")]", editor, StringComparison.Ordinal);
        Assert.Contains(LockedBattleRoster.ScenePath, editor, StringComparison.Ordinal);
        var build = File.ReadAllText(Path.Combine(root, "ProjectSettings", "EditorBuildSettings.asset"));
        Assert.Contains("path: " + LockedBattleRoster.ScenePath, build, StringComparison.Ordinal);
        var sceneGuid = File.ReadAllText(scenePath + ".meta").Split('\n').First(l => l.StartsWith("guid: ", StringComparison.Ordinal)).Substring(6).Trim();
        Assert.Contains("guid: " + sceneGuid, build, StringComparison.Ordinal);

        var demo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "LockedBattleDemo.cs"));
        foreach (var f in LockedBattleRoster.All)
        {
            Assert.Contains("AddComponent<" + f.ActorClassName + ">()", demo, StringComparison.Ordinal);
        }

        Assert.Contains("LockedBattleRoster.All", demo, StringComparison.Ordinal);
        Assert.Contains("Restart", demo, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveAndReimport", demo, StringComparison.Ordinal);
        Assert.DoesNotContain("ModelImporter", demo, StringComparison.Ordinal);
        var camera = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "LockedBattleCamera.cs"));
        Assert.Contains("PortraitGameView.FovDegrees", camera, StringComparison.Ordinal);

        // Free world-view camera (Derek 2026-10-16): mouse orbit, wheel zoom, middle-drag pan,
        // starting on the old three-quarter framing. No view buttons, no bottom pose ticker.
        Assert.Contains("Mouse.current", camera, StringComparison.Ordinal);
        Assert.Contains("mouse.scroll", camera, StringComparison.Ordinal);
        Assert.Contains("middleButton", camera, StringComparison.Ordinal);
        Assert.Contains("DefaultYawDegrees = 135f", camera, StringComparison.Ordinal);
        Assert.Contains("DefaultPitchDegrees = 30f", camera, StringComparison.Ordinal);
        Assert.Contains("Mathf.Clamp(_pitch, MinPitchDegrees, MaxPitchDegrees)", camera, StringComparison.Ordinal);
        Assert.Contains("MinEyeHeightMetres", camera, StringComparison.Ordinal);
        Assert.DoesNotContain("Rear battle cam", camera, StringComparison.Ordinal);
        Assert.DoesNotContain("SetMode", camera, StringComparison.Ordinal);
        Assert.DoesNotContain("Dropdown", demo, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdatePhaseText", demo, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Phase\"", demo, StringComparison.Ordinal);
    }

    [Fact]
    public void Locked_battle_files_match_their_locked_checksums_and_do_not_reimport_on_play()
    {
        var root = FindRepoRoot();
        var assets = Path.Combine(root, "Assets");
        Assert.Equal(StormcrestMotion.RestMd5, Md5(Path.Combine(assets, StormcrestMotion.RestThemePackRel)));
        Assert.Equal(StormcrestMotion.WalkMd5, Md5(Path.Combine(assets, StormcrestMotion.WalkThemePackRel)));
        Assert.Equal(StormcrestMotion.WingFlapMd5, Md5(Path.Combine(assets, StormcrestMotion.WingFlapThemePackRel)));
        Assert.Equal(StormcrestAttack.FileMd5, Md5(Path.Combine(assets, StormcrestMotion.ThemePackDir, StormcrestAttack.FileName)));
        Assert.Equal(NightfangAttack.FileMd5, Md5(Path.Combine(assets, NightfangMotion.ThemePackDir, NightfangAttack.FileName)));
        Assert.Equal(EmberfangAttack.FileMd5, Md5(Path.Combine(assets, EmberfangMotion.ThemePackDir, EmberfangAttack.FileName)));
        Assert.Equal(IronhowlMotion.BodyMd5, Md5(Path.Combine(assets, IronhowlMotion.BodyThemePackRel)));
        Assert.Equal(IronhowlMotion.WalkMd5, Md5(Path.Combine(assets, IronhowlMotion.WalkThemePackRel)));
        Assert.Equal(IronhowlMotion.AttackMd5, Md5(Path.Combine(assets, IronhowlMotion.AttackThemePackRel)));
        Assert.Equal(LyraAttack.FileMd5, Md5(Path.Combine(assets, LyraMotion.ThemePackDir, LyraAttack.FileName)));
        Assert.Equal(OakenshieldMotion.RestMd5, Md5(Path.Combine(assets, OakenshieldMotion.RestThemePackRel)));
        Assert.Equal(OakenshieldMotion.WalkMd5, Md5(Path.Combine(assets, OakenshieldMotion.WalkThemePackRel)));
        Assert.Equal(OakenshieldAttack.FileMd5, Md5(Path.Combine(assets, OakenshieldMotion.ThemePackDir, OakenshieldAttack.FileName)));
        Assert.Equal(VesperaAttack.FileMd5, Md5(Path.Combine(assets, VesperaMotion.ThemePackDir, VesperaAttack.FileName)));
        Assert.Equal(AshwyrmAttack.FileMd5, Md5(Path.Combine(assets, AshwyrmMotion.ThemePackDir, AshwyrmAttack.FileName)));
        Assert.Equal(BlightrootMotion.BodyMd5, Md5(Path.Combine(assets, BlightrootMotion.BodyThemePackRel)));
        foreach (var name in new[] { BlightrootBattlePoses.RestClip, BlightrootBattlePoses.WalkClip, BlightrootBattlePoses.AttackClip })
        {
            Assert.True(BlightrootMotion.TryGet(name, out var clip));
            Assert.Equal(clip.Md5, Md5(Path.Combine(assets, clip.ThemePackRel)));
        }

        foreach (var name in new[] { SirAldricBattlePoses.RestClip, SirAldricBattlePoses.AttackClip })
        {
            Assert.True(SirAldric3DMotion.TryLiteSwordShield(name, out var clip));
            Assert.Equal(clip.Md5, Md5(Path.Combine(assets, clip.ThemePackRel)));
        }

        foreach (var spec in new[] { NightfangMotion.Spec, StormcrestMotion.Spec })
        {
            var rest = File.ReadAllText(Path.Combine(assets, spec.RestThemePackRel) + ".meta");
            Assert.False(BlenderRigImportMeta.NeedsReimport(rest, spec.PreferHumanoid, false, string.Empty, string.Empty, 0), spec.Name);
            var clip = File.ReadAllText(Path.Combine(assets, spec.ClipThemePackRel) + ".meta");
            Assert.False(BlenderRigImportMeta.NeedsReimport(clip, false, true, spec.ClipPoseName, spec.ClipTakeName, spec.ClipLastFrame), spec.Name);
            foreach (var extra in spec.ExtraClips)
            {
                var meta = File.ReadAllText(Path.Combine(assets, spec.ThemePackDir, extra.FileName) + ".meta");
                Assert.False(BlenderRigImportMeta.NeedsReimport(meta, false, true, extra.PoseName, extra.TakeName, extra.LastFrame), spec.Name + " " + extra.PoseName);
            }
        }
    }

    private static string Md5(string path)
    {
        var hash = System.Security.Cryptography.MD5.HashData(File.ReadAllBytes(path));
        return Convert.ToHexString(hash).ToLowerInvariant();
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
