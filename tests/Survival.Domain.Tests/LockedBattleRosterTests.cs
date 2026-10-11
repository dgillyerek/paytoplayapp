using Survival.Domain.Enemies;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

public sealed class LockedBattleRosterTests
{
    [Fact]
    public void Roster_is_the_four_locked_merged_characters_with_walk_and_attack_split_two_against_two()
    {
        Assert.Equal(new[] { "Emberfang", "Stormcrest", "Ironhowl", "Nightfang" }, LockedBattleRoster.All.Select(f => f.Name).ToArray());
        Assert.Equal(new[] { "Emberfang", "Stormcrest" }, LockedBattleRoster.OnSide(BattleSide.Hero).Select(f => f.Name).ToArray());
        Assert.Equal(new[] { "Ironhowl", "Nightfang" }, LockedBattleRoster.OnSide(BattleSide.Villain).Select(f => f.Name).ToArray());

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

        Assert.True(LockedBattleRoster.Ironhowl.WalkCarriesRoot);
        Assert.Equal(IronhowlMotion.BoneRoot, LockedBattleRoster.Ironhowl.StrideBone);
        Assert.False(LockedBattleRoster.Ironhowl.AttackCyclesItself);
        Assert.Equal(IronhowlMotion.AttackSeconds, LockedBattleRoster.Ironhowl.AttackSeconds);
        foreach (var f in new[] { LockedBattleRoster.Emberfang, LockedBattleRoster.Stormcrest, LockedBattleRoster.Nightfang })
        {
            Assert.False(f.WalkCarriesRoot);
            Assert.True(f.AttackCyclesItself);
        }

        var notInBattle = new[] { "Ashwyrm", "Lyra", "Rowan", "Bonequill", "Vespera", "Oakenshield", "Blightroot", "Sir Aldric", "SirAldric" };
        foreach (var name in notInBattle)
        {
            Assert.DoesNotContain(LockedBattleRoster.All, f => string.Equals(f.Name, name, StringComparison.Ordinal));
        }

        Assert.Equal(new[] { "Ashwyrm", "Blightroot", "Sir Aldric" }, LockedBattleRoster.LeftOut.Select(p => p.Key).ToArray());
    }

    [Fact]
    public void Ashwyrm_is_merged_with_walk_and_attack_but_not_placed_until_Derek_picks_its_lane()
    {
        // #56 merged Ashwyrm's attack. It qualifies now, it just has no lane yet.
        Assert.Contains(AshwyrmMotion.WalkPoseName, AshwyrmMotion.Spec.PoseNames);
        Assert.Contains(BlenderRigAttackSpec.PoseName, AshwyrmMotion.Spec.PoseNames);
        Assert.Equal(BlenderRigAttackSpec.PoseName, AshwyrmMotion.AttackPoseName);

        var reason = LockedBattleRoster.LeftOut.Single(p => p.Key == "Ashwyrm").Value;
        Assert.Contains("merged", reason, StringComparison.Ordinal);
        Assert.Contains("attack", reason, StringComparison.Ordinal);
        Assert.Contains("lane", reason, StringComparison.Ordinal);
        Assert.DoesNotContain("unmerged", reason, StringComparison.Ordinal);
        Assert.DoesNotContain(LockedBattleRoster.All, f => f.Name == "Ashwyrm");
    }

    [Fact]
    public void Heroes_and_villains_start_on_opposite_ends_facing_each_other_and_lock_their_lane_opponent()
    {
        foreach (var f in LockedBattleRoster.All)
        {
            LockedBattleRoster.StartPoint(f, out var x, out var z);
            Assert.Equal(f.Lane == 0 ? -LockedBattleRoster.LaneHalfSpacingMetres : LockedBattleRoster.LaneHalfSpacingMetres, x);
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
