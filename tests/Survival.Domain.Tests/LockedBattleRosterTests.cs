using Survival.Domain.Enemies;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

public sealed class LockedBattleRosterTests
{
    [Fact]
    public void Roster_is_every_locked_merged_character_with_walk_and_attack_split_four_against_four()
    {
        Assert.Equal(new[] { "Emberfang", "Stormcrest", "Ironhowl", "Nightfang", "Lyra", "Oakenshield", "Vespera", "Ashwyrm" }, LockedBattleRoster.All.Select(f => f.Name).ToArray());
        Assert.Equal(new[] { "Emberfang", "Stormcrest", "Lyra", "Oakenshield" }, LockedBattleRoster.OnSide(BattleSide.Hero).Select(f => f.Name).ToArray());
        Assert.Equal(new[] { "Ironhowl", "Nightfang", "Vespera", "Ashwyrm" }, LockedBattleRoster.OnSide(BattleSide.Villain).Select(f => f.Name).ToArray());

        var root = FindRepoRoot();
        foreach (var f in LockedBattleRoster.All)
        {
            var folder = f.Side == BattleSide.Hero ? "Heroes" : "Enemies";
            Assert.True(File.Exists(Path.Combine(root, "Assets", "Survival", "Runtime", folder, f.Name + "Motion.cs")), f.Name);
            Assert.True(File.Exists(Path.Combine(root, "Assets", "Survival", "Unity", f.Name + "Actor.cs")), f.Name);
            Assert.True(File.Exists(Path.Combine(root, "Assets", "Survival", "Scenes", f.Name + ".unity")), f.Name);
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

        Assert.True(LockedBattleRoster.Ironhowl.WalkCarriesRoot);
        Assert.Equal(IronhowlMotion.BoneRoot, LockedBattleRoster.Ironhowl.StrideBone);
        Assert.False(LockedBattleRoster.Ironhowl.AttackCyclesItself);
        Assert.Equal(IronhowlMotion.AttackSeconds, LockedBattleRoster.Ironhowl.AttackSeconds);
        foreach (var f in new[] { LockedBattleRoster.Emberfang, LockedBattleRoster.Stormcrest, LockedBattleRoster.Nightfang, LockedBattleRoster.Lyra, LockedBattleRoster.Oakenshield, LockedBattleRoster.Vespera, LockedBattleRoster.Ashwyrm })
        {
            Assert.False(f.WalkCarriesRoot);
            Assert.True(f.AttackCyclesItself);
        }

        // Rowan (#64) and Bonequill (#63) are unmerged HOLD PRs; Blightroot and Sir Aldric have no Game-view PASS.
        var notInBattle = new[] { "Rowan", "Bonequill", "Blightroot", "Sir Aldric", "SirAldric" };
        foreach (var name in notInBattle)
        {
            Assert.DoesNotContain(LockedBattleRoster.All, f => string.Equals(f.Name, name, StringComparison.Ordinal));
        }

        Assert.Equal(new[] { "Blightroot", "Sir Aldric" }, LockedBattleRoster.LeftOut.Select(p => p.Key).ToArray());
    }

    [Fact]
    public void Battle_roster_is_exactly_the_eight_merged_fighters_and_never_Blightroot()
    {
        Assert.Equal(
            new[] { "Emberfang", "Stormcrest", "Ironhowl", "Nightfang", "Lyra", "Oakenshield", "Vespera", "Ashwyrm" },
            LockedBattleRoster.All.Select(f => f.Name).ToArray());
        Assert.Equal(8, LockedBattleRoster.All.Count);
        Assert.DoesNotContain(LockedBattleRoster.All, f => f.Name == "Blightroot");
        Assert.DoesNotContain(LockedBattleRoster.All, f => f.Name == "Rowan");
        Assert.DoesNotContain(LockedBattleRoster.All, f => f.Name == "Bonequill");
        Assert.DoesNotContain(LockedBattleRoster.All, f => f.Name == "Sir Aldric");
        Assert.Contains(LockedBattleRoster.LeftOut, p => p.Key == "Blightroot");
        var demo = File.ReadAllText(Path.Combine(FindRepoRoot(), "Assets", "Survival", "Unity", "LockedBattleDemo.cs"));
        Assert.Contains("StripLeftoverDemoActors", demo, StringComparison.Ordinal);
        Assert.Contains("BlightrootActor", demo, StringComparison.Ordinal);
        Assert.DoesNotContain("AddComponent<BlightrootActor>()", demo, StringComparison.Ordinal);
        var menu = File.ReadAllText(Path.Combine(FindRepoRoot(), "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("OpenSceneMode.Single", menu, StringComparison.Ordinal);
    }

    [Fact]
    public void Added_lanes_sit_outside_the_original_two_and_pair_one_hero_with_one_villain()
    {
        // #57 lanes stay exactly where they were.
        Assert.Equal(0, LockedBattleRoster.Emberfang.Lane);
        Assert.Equal(0, LockedBattleRoster.Ironhowl.Lane);
        Assert.Equal(1, LockedBattleRoster.Stormcrest.Lane);
        Assert.Equal(1, LockedBattleRoster.Nightfang.Lane);
        Assert.Equal(-1.15f, LockedBattleRoster.LaneX(0), 4);
        Assert.Equal(1.15f, LockedBattleRoster.LaneX(1), 4);
        Assert.Equal(-LockedBattleRoster.OuterLaneHalfSpacingMetres, LockedBattleRoster.LaneX(2), 4);
        Assert.Equal(LockedBattleRoster.OuterLaneHalfSpacingMetres, LockedBattleRoster.LaneX(3), 4);
        Assert.True(LockedBattleRoster.OuterLaneHalfSpacingMetres - LockedBattleRoster.LaneHalfSpacingMetres >= 1.2f);

        foreach (var lane in new[] { 0, 1, 2, 3 })
        {
            Assert.Single(LockedBattleRoster.All, f => f.Lane == lane && f.Side == BattleSide.Hero);
            Assert.Single(LockedBattleRoster.All, f => f.Lane == lane && f.Side == BattleSide.Villain);
        }

        Assert.Equal("Vespera", LockedBattleRoster.TargetOf(LockedBattleRoster.Lyra).Name);
        Assert.Equal("Lyra", LockedBattleRoster.TargetOf(LockedBattleRoster.Vespera).Name);
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
            Assert.Contains("AddComponent<" + f.Name + "Actor>()", demo, StringComparison.Ordinal);
        }

        Assert.Contains("LockedBattleRoster.All", demo, StringComparison.Ordinal);
        Assert.Contains("Restart", demo, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveAndReimport", demo, StringComparison.Ordinal);
        Assert.DoesNotContain("ModelImporter", demo, StringComparison.Ordinal);
        var camera = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "LockedBattleCamera.cs"));
        Assert.Contains("PortraitGameView.FovDegrees", camera, StringComparison.Ordinal);
        Assert.Contains("Rear battle cam", camera, StringComparison.Ordinal);
        Assert.Contains("Three-quarter", camera, StringComparison.Ordinal);
        Assert.Contains("Front", camera, StringComparison.Ordinal);
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
