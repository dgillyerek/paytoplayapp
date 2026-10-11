using System.Text;
using Survival.Domain.Enemies;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

public sealed class BlenderRigRosterTests
{
    [Fact]
    public void Mixamo_rig_exports_are_rejected_and_blenderig_files_are_kept()
    {
        Assert.True(BlenderRigSpec.IsRejectedMixamoRigFile("ROWAN_rig.fbx"));
        Assert.True(BlenderRigSpec.IsRejectedMixamoRigFile("Assets/ThemePack/EMBERFANG_rig.fbx"));
        Assert.False(BlenderRigSpec.IsRejectedMixamoRigFile("ROWAN_blenderig.fbx"));
        Assert.False(BlenderRigSpec.IsRejectedMixamoRigFile("ROWAN_blenderig_walk.fbx"));
        Assert.False(BlenderRigSpec.IsRejectedMixamoRigFile("NIGHTFANG_blenderig_trot.fbx"));
        Assert.False(BlenderRigSpec.IsRejectedMixamoRigFile("ASHWYRM_blenderig.fbx"));
        Assert.False(BlenderRigSpec.IsRejectedMixamoRigFile("ASHWYRM_blenderig_wingflap.fbx"));
    }

    [Fact]
    public void Ashwyrm_is_a_30_bone_generic_dragon_with_rest_and_wing_flap()
    {
        Assert.Equal(new[] { AshwyrmMotion.Spec, NightfangMotion.Spec, StormcrestMotion.Spec }, BlenderRigRoster.All);
        Assert.Equal(30, AshwyrmMotion.BoneCount);
        Assert.Equal(30, AshwyrmMotion.BoneNames.Length);
        Assert.Equal(AshwyrmMotion.BoneNames.Length, AshwyrmMotion.BoneNames.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("root", AshwyrmMotion.BoneRoot);
        Assert.Equal("spine", AshwyrmMotion.BoneNames[1]);
        Assert.Equal("wing_root.L", AshwyrmMotion.BoneNames[9]);
        Assert.Equal("wing_inner.L", AshwyrmMotion.BoneNames[15]);
        Assert.Equal("tail_06", AshwyrmMotion.BoneNames[25]);
        Assert.Equal("upperarm.L", AshwyrmMotion.BoneNames[26]);
        Assert.Equal("forearm.R", AshwyrmMotion.BoneNames[29]);
        Assert.DoesNotContain("hips", AshwyrmMotion.BoneNames);
        Assert.DoesNotContain("wing_forearm.L", AshwyrmMotion.BoneNames);
        Assert.Equal("rest", AshwyrmMotion.PoseNames[0]);
        Assert.Equal("wing flap", AshwyrmMotion.PoseNames[1]);
        Assert.Equal("walk", AshwyrmMotion.PoseNames[2]);
        Assert.Equal(AshwyrmMotion.PoseNames, AshwyrmMotion.Spec.PoseNames);
        Assert.Equal("walk", AshwyrmMotion.Spec.ExtraClips[0].PoseName);
        Assert.Equal("ASHWYRM_blenderig_walk.fbx", AshwyrmMotion.Spec.ExtraClips[0].FileName);
        Assert.Equal("Scene", AshwyrmMotion.WalkTakeName);
        Assert.Equal(1f, AshwyrmMotion.WalkSeconds);
        Assert.Equal(30, AshwyrmMotion.WalkLastFrame);
        Assert.Equal("Scene", AshwyrmMotion.FlapTakeName);
        Assert.Equal(1f, AshwyrmMotion.FlapSeconds);
        Assert.Equal(30, AshwyrmMotion.FlapLastFrame);
        Assert.Equal(BlenderRigAvatar.CustomGeneric, AshwyrmMotion.Spec.Avatar);
        Assert.False(AshwyrmMotion.Spec.PreferHumanoid);
        Assert.Equal("ASHWYRM_rig.fbx", AshwyrmMotion.RejectedMixamoFileName);
        AssertGenericDemo(
            AshwyrmMotion.Spec,
            "ASHWYRM_basecolor_0",
            "ASHWYRM_normal_2",
            "wing_inner.L",
            "11f3e71f3fc54d8baee7d5ea145bd473",
            "Survival/Ashwyrm Demo (rest + wing flap + walk + attack, Game view 1080x1920)");

        var root = FindRepoRoot();
        var rest = File.ReadAllBytes(Path.Combine(root, "Assets", AshwyrmMotion.RestThemePackRel));
        var flap = File.ReadAllBytes(Path.Combine(root, "Assets", AshwyrmMotion.FlapThemePackRel));
        foreach (var bone in AshwyrmMotion.BoneNames)
        {
            Assert.True(ContainsAscii(rest, bone));
            Assert.True(ContainsAscii(flap, bone));
        }

        Assert.False(ContainsAscii(rest, "wing_forearm"));
        Assert.False(ContainsAscii(flap, "wing_forearm"));
        Assert.False(ContainsAscii(rest, "spine_01"));
        Assert.False(ContainsAscii(flap, "spine_01"));
        var motion = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Runtime", "Enemies", "AshwyrmMotion.cs"));
        Assert.DoesNotContain("wing_forearm", motion, StringComparison.Ordinal);
        Assert.Contains("upperarm.L", motion, StringComparison.Ordinal);
        Assert.Contains("forearm.L", motion, StringComparison.Ordinal);
        Assert.Contains("tail_06", motion, StringComparison.Ordinal);

        var walk = File.ReadAllBytes(Path.Combine(root, "Assets", AshwyrmMotion.WalkThemePackRel));
        foreach (var bone in AshwyrmMotion.BoneNames)
        {
            Assert.True(ContainsAscii(walk, bone));
        }

        Assert.False(ContainsAscii(walk, "wing_forearm"));
        Assert.False(ContainsAscii(walk, "mixamorig"));
        Assert.True(ContainsAscii(walk, "Scene"));
        var walkMeta = File.ReadAllText(Path.Combine(root, "Assets", AshwyrmMotion.WalkThemePackRel) + ".meta");
        Assert.Contains("name: \"walk\"", walkMeta, StringComparison.Ordinal);
        Assert.Contains("takeName: Scene", walkMeta, StringComparison.Ordinal);
        Assert.Contains("lastFrame: 30", walkMeta, StringComparison.Ordinal);
        Assert.Contains("animationType: 2", walkMeta, StringComparison.Ordinal);
        Assert.Contains("bakeAxisConversion: 0", walkMeta, StringComparison.Ordinal);
        Assert.Contains("useFileScale: 0", walkMeta, StringComparison.Ordinal);
        Assert.DoesNotContain("animationType: 3", walkMeta, StringComparison.Ordinal);
        Assert.DoesNotContain("mixamo.com", walkMeta, StringComparison.Ordinal);
        var player = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlenderRigPlayer.cs"));
        Assert.Contains("ExtraClips", player, StringComparison.Ordinal);
        Assert.Contains("AnimationClipPlayable.Create", player, StringComparison.Ordinal);
        var demo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlenderRigDemo.cs"));
        Assert.Contains("spec.PoseNames", demo, StringComparison.Ordinal);
        Assert.Contains("PlayNamedPose", demo, StringComparison.Ordinal);
    }

    [Fact]
    public void Player_binds_embedded_maps_and_does_not_rewrite_the_rig()
    {
        var root = FindRepoRoot();
        var player = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlenderRigPlayer.cs"));
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "AshwyrmActor.cs"));
        var demo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "AshwyrmDemo.cs"));
        var shared = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlenderRigDemo.cs"));
        Assert.Contains("bakeAxisConversion = false", player, StringComparison.Ordinal);
        Assert.DoesNotContain("bakeAxisConversion = true", player, StringComparison.Ordinal);
        Assert.DoesNotContain("localRotation =", player, StringComparison.Ordinal);
        Assert.DoesNotContain("localScale =", player, StringComparison.Ordinal);
        Assert.Contains("ImportViaMaterialDescription", player, StringComparison.Ordinal);
        Assert.Contains("ModelImporterAnimationType.Human", player, StringComparison.Ordinal);
        Assert.Contains("ModelImporterAnimationType.Generic", player, StringComparison.Ordinal);
        Assert.Contains("isHuman", player, StringComparison.Ordinal);
        Assert.Contains("_BaseMap", player, StringComparison.Ordinal);
        Assert.Contains("_BumpMap", player, StringComparison.Ordinal);
        Assert.Contains("IsRejectedMixamoRigFile", player, StringComparison.Ordinal);
        Assert.Contains("PlayPose", actor, StringComparison.Ordinal);
        Assert.Contains("AshwyrmMotion.Spec", actor, StringComparison.Ordinal);
        Assert.Contains("AshwyrmClipDropdown", demo, StringComparison.Ordinal);
        Assert.Contains("AshwyrmMotion.Spec", demo, StringComparison.Ordinal);
        Assert.Contains("SetValueWithoutNotify(0)", shared, StringComparison.Ordinal);
        Assert.Contains("DropdownObjectName", shared, StringComparison.Ordinal);
        AssertImportSkipsReimportWhenMetaAlreadyMatches(player);

        var scene = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Scenes", "Ashwyrm.unity"));
        Assert.Contains("AshwyrmRoot", scene, StringComparison.Ordinal);
        Assert.Contains("11f3e71f3fc54d8baee7d5ea145bd473", scene, StringComparison.Ordinal);
        Assert.Contains("field of view: 54", scene, StringComparison.Ordinal);

        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("Survival/Ashwyrm Demo (rest + wing flap + walk + attack, Game view 1080x1920)", editor, StringComparison.Ordinal);
        Assert.Contains("Ashwyrm.unity", editor, StringComparison.Ordinal);
        Assert.DoesNotContain("Rowan.unity", editor, StringComparison.Ordinal);
    }

    [Fact]
    public void Nightfang_is_a_30_bone_generic_quadruped_with_rest_and_trot()
    {
        Assert.Equal(30, NightfangMotion.BoneCount);
        Assert.Equal(30, NightfangMotion.BoneNames.Length);
        Assert.Equal("root", NightfangMotion.BoneRoot);
        Assert.Equal("toe_h.L", NightfangMotion.BoneNames[6]);
        Assert.Equal("snout", NightfangMotion.BoneNames[29]);
        Assert.Equal("trot", NightfangMotion.PoseNames[1]);
        Assert.Equal("Scene", NightfangMotion.TrotTakeName);
        Assert.Equal(1f, NightfangMotion.TrotSeconds);
        Assert.Equal(30, NightfangMotion.TrotLastFrame);
        Assert.Equal(BlenderRigAvatar.CustomGeneric, NightfangMotion.Spec.Avatar);
        AssertGenericDemo(
            NightfangMotion.Spec,
            "NIGHTFANG_basecolor_0",
            "NIGHTFANG_normal_2",
            "toe_h.L",
            "5a612f20bf58466c8634f45803244d4f",
            "Survival/Nightfang Demo (rest + trot + attack, Game view 1080x1920)");
    }

    [Fact]
    public void Nightfang_player_binds_embedded_maps_and_does_not_rewrite_the_rig()
    {
        var root = FindRepoRoot();
        var player = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlenderRigPlayer.cs"));
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "NightfangActor.cs"));
        var demo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "NightfangDemo.cs"));
        var shared = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlenderRigDemo.cs"));
        Assert.Contains("bakeAxisConversion = false", player, StringComparison.Ordinal);
        Assert.DoesNotContain("bakeAxisConversion = true", player, StringComparison.Ordinal);
        Assert.DoesNotContain("localRotation =", player, StringComparison.Ordinal);
        Assert.DoesNotContain("localScale =", player, StringComparison.Ordinal);
        Assert.Contains("ImportViaMaterialDescription", player, StringComparison.Ordinal);
        Assert.Contains("ModelImporterAnimationType.Human", player, StringComparison.Ordinal);
        Assert.Contains("ModelImporterAnimationType.Generic", player, StringComparison.Ordinal);
        Assert.Contains("isHuman", player, StringComparison.Ordinal);
        Assert.Contains("_BaseMap", player, StringComparison.Ordinal);
        Assert.Contains("_BumpMap", player, StringComparison.Ordinal);
        Assert.Contains("IsRejectedMixamoRigFile", player, StringComparison.Ordinal);
        Assert.Contains("PlayPose", actor, StringComparison.Ordinal);
        Assert.Contains("NightfangMotion.Spec", actor, StringComparison.Ordinal);
        Assert.Contains("NightfangClipDropdown", demo, StringComparison.Ordinal);
        Assert.Contains("NightfangMotion.Spec", demo, StringComparison.Ordinal);
        Assert.Contains("SetValueWithoutNotify(0)", shared, StringComparison.Ordinal);
        Assert.Contains("DropdownObjectName", shared, StringComparison.Ordinal);

        var scene = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Scenes", "Nightfang.unity"));
        Assert.Contains("NightfangRoot", scene, StringComparison.Ordinal);
        Assert.Contains("5a612f20bf58466c8634f45803244d4f", scene, StringComparison.Ordinal);
        Assert.Contains("field of view: 54", scene, StringComparison.Ordinal);

        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("Survival/Nightfang Demo (rest + trot + attack, Game view 1080x1920)", editor, StringComparison.Ordinal);
        Assert.Contains("Nightfang.unity", editor, StringComparison.Ordinal);
    }

    [Fact]
    public void Stormcrest_is_a_45_bone_generic_with_rest_wingflap_walk_and_attack()
    {
        var spec = StormcrestMotion.Spec;
        Assert.Equal(45, spec.BoneCount);
        Assert.Equal(StormcrestMotion.BoneNames, spec.BoneNames);
        Assert.Equal(45, spec.BoneNames.Length);
        Assert.Equal(spec.BoneNames.Length, spec.BoneNames.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("root", spec.BoneRoot);
        Assert.Equal("root", spec.BoneNames[0]);
        Assert.Contains("wing_arm.L", spec.BoneNames);
        Assert.Contains("wing_feather_b.R", spec.BoneNames);
        Assert.Contains("hind_toe.R", spec.BoneNames);
        Assert.DoesNotContain(spec.BoneNames, name => name.Contains("mixamorig", StringComparison.Ordinal));
        Assert.Equal(new[] { "rest", "wing flap", "walk", "attack" }, spec.PoseNames);
        Assert.Equal(StormcrestMotion.PoseNames, spec.PoseNames);
        Assert.Equal("wing flap", spec.ClipPoseName);
        Assert.Equal("STORMCREST_blenderig_wingflap.fbx", spec.ClipFileName);
        Assert.Equal("Scene", spec.ClipTakeName);
        Assert.Equal(1f, spec.ClipSeconds);
        Assert.Equal(30, spec.ClipLastFrame);
        Assert.Equal(30f, spec.ClipFrameRate);
        Assert.Equal("walk", spec.ExtraClips[0].PoseName);
        Assert.Equal("STORMCREST_blenderig_walk.fbx", spec.ExtraClips[0].FileName);
        Assert.Equal("attack", spec.ExtraClips[1].PoseName);
        Assert.Equal("", spec.MetallicRoughnessFile);
        Assert.False(spec.PreferHumanoid);
        Assert.Equal(BlenderRigAvatar.CustomGeneric, spec.Avatar);
        Assert.True(BlenderRigSpec.IsRejectedMixamoRigFile(spec.RejectedMixamoFileName));
        Assert.Contains(spec, BlenderRigRoster.All);

        var root = FindRepoRoot();
        var rest = Path.Combine(root, "Assets", spec.RestThemePackRel);
        var flap = Path.Combine(root, "Assets", spec.ClipThemePackRel);
        var walk = Path.Combine(root, "Assets", spec.ThemePackDir, StormcrestMotion.WalkFileName);
        var attack = Path.Combine(root, "Assets", spec.ThemePackDir, StormcrestAttack.FileName);
        var rejected = Path.Combine(root, "Assets", spec.ThemePackDir, spec.RejectedMixamoFileName);
        var albedo = Path.Combine(root, "Assets", spec.BaseColorThemePackRel);
        var normal = Path.Combine(root, "Assets", spec.NormalThemePackRel);
        Assert.True(File.Exists(rest));
        Assert.True(File.Exists(flap));
        Assert.True(File.Exists(walk));
        Assert.True(File.Exists(attack));
        Assert.False(File.Exists(rejected));
        Assert.True(File.Exists(albedo));
        Assert.True(File.Exists(normal));
        var restBytes = File.ReadAllBytes(rest);
        var flapBytes = File.ReadAllBytes(flap);
        var walkBytes = File.ReadAllBytes(walk);
        Assert.True(ContainsAscii(restBytes, "root"));
        Assert.True(ContainsAscii(restBytes, "wing_arm.L"));
        Assert.True(ContainsAscii(restBytes, "STORMCREST_basecolor_0"));
        Assert.True(ContainsAscii(restBytes, "STORMCREST_normal_2"));
        Assert.False(ContainsAscii(restBytes, "mixamorig"));
        Assert.False(ContainsAscii(flapBytes, "mixamorig"));
        Assert.False(ContainsAscii(walkBytes, "mixamorig"));
        Assert.True(ContainsAscii(flapBytes, "Scene"));
        Assert.True(ContainsAscii(walkBytes, "Scene"));
        Assert.False(ContainsAscii(walkBytes, "mixamo.com"));
        Assert.True(ContainsBytes(restBytes, File.ReadAllBytes(albedo)));
        Assert.True(ContainsBytes(restBytes, File.ReadAllBytes(normal)));

        AssertGenericClipMeta(rest + ".meta", expectClip: false, poseName: "");
        AssertGenericClipMeta(flap + ".meta", expectClip: true, poseName: "wing flap");
        AssertGenericClipMeta(walk + ".meta", expectClip: true, poseName: "walk");
        AssertGenericClipMeta(attack + ".meta", expectClip: true, poseName: "attack");

        var scene = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Scenes", spec.SceneFileName));
        Assert.Contains("StormcrestRoot", scene, StringComparison.Ordinal);
        Assert.Contains("cd5dad8493e64f1bad48ecb1bf8586ed", scene, StringComparison.Ordinal);
        Assert.Contains("field of view: 54", scene, StringComparison.Ordinal);
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "StormcrestActor.cs"));
        var demo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "StormcrestDemo.cs"));
        Assert.Contains("StormcrestMotion.Spec", actor, StringComparison.Ordinal);
        Assert.Contains("PlayPose", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("mixamorig", actor, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("StormcrestClipDropdown", demo, StringComparison.Ordinal);
        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("Survival/Stormcrest Demo (rest + wing flap + walk + attack, Game view 1080x1920)", editor, StringComparison.Ordinal);
        var build = File.ReadAllText(Path.Combine(root, "ProjectSettings", "EditorBuildSettings.asset"));
        Assert.Contains("Stormcrest.unity", build, StringComparison.Ordinal);
    }

    [Fact]
    public void Stormcrest_player_binds_embedded_maps_and_does_not_rewrite_the_rig()
    {
        var root = FindRepoRoot();
        var player = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlenderRigPlayer.cs"));
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "StormcrestActor.cs"));
        var demo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "StormcrestDemo.cs"));
        var shared = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlenderRigDemo.cs"));
        Assert.Contains("bakeAxisConversion = false", player, StringComparison.Ordinal);
        Assert.DoesNotContain("bakeAxisConversion = true", player, StringComparison.Ordinal);
        Assert.DoesNotContain("localRotation =", player, StringComparison.Ordinal);
        Assert.DoesNotContain("localScale =", player, StringComparison.Ordinal);
        Assert.Contains("ImportViaMaterialDescription", player, StringComparison.Ordinal);
        Assert.Contains("ModelImporterAnimationType.Human", player, StringComparison.Ordinal);
        Assert.Contains("ModelImporterAnimationType.Generic", player, StringComparison.Ordinal);
        Assert.Contains("isHuman", player, StringComparison.Ordinal);
        Assert.Contains("_BaseMap", player, StringComparison.Ordinal);
        Assert.Contains("_BumpMap", player, StringComparison.Ordinal);
        Assert.Contains("IsRejectedMixamoRigFile", player, StringComparison.Ordinal);
        Assert.Contains("PlayPose", actor, StringComparison.Ordinal);
        Assert.Contains("StormcrestMotion.Spec", actor, StringComparison.Ordinal);
        Assert.Contains("StormcrestClipDropdown", demo, StringComparison.Ordinal);
        Assert.Contains("StormcrestMotion.Spec", demo, StringComparison.Ordinal);
        Assert.Contains("SetValueWithoutNotify(0)", shared, StringComparison.Ordinal);
        Assert.Contains("DropdownObjectName", shared, StringComparison.Ordinal);

        var scene = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Scenes", "Stormcrest.unity"));
        Assert.Contains("StormcrestRoot", scene, StringComparison.Ordinal);
        Assert.Contains("cd5dad8493e64f1bad48ecb1bf8586ed", scene, StringComparison.Ordinal);
        Assert.Contains("field of view: 54", scene, StringComparison.Ordinal);

        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("Survival/Stormcrest Demo (rest + wing flap + walk + attack, Game view 1080x1920)", editor, StringComparison.Ordinal);
        Assert.Contains("Stormcrest.unity", editor, StringComparison.Ordinal);
    }

    private static void AssertGenericClipMeta(string metaPath, bool expectClip, string poseName)
    {
        var meta = File.ReadAllText(metaPath);
        Assert.Contains("animationType: 2", meta, StringComparison.Ordinal);
        Assert.Contains("autoGenerateAvatarMappingIfUnspecified: 0", meta, StringComparison.Ordinal);
        Assert.Contains("bakeAxisConversion: 0", meta, StringComparison.Ordinal);
        Assert.Contains("useFileScale: 0", meta, StringComparison.Ordinal);
        Assert.Contains("materialImportMode: 2", meta, StringComparison.Ordinal);
        Assert.DoesNotContain("animationType: 3", meta, StringComparison.Ordinal);
        Assert.DoesNotContain("mixamo.com", meta, StringComparison.Ordinal);
        if (expectClip)
        {
            Assert.Contains("name: \"" + poseName + "\"", meta, StringComparison.Ordinal);
            Assert.Contains("takeName: Scene", meta, StringComparison.Ordinal);
            Assert.Contains("lastFrame: 30", meta, StringComparison.Ordinal);
            Assert.Contains("loopTime: 1", meta, StringComparison.Ordinal);
        }
        else
        {
            Assert.Contains("clipAnimations: []", meta, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Ashwyrm_saved_import_settings_already_match_so_play_mode_does_not_reimport()
    {
        var root = FindRepoRoot();
        var spec = AshwyrmMotion.Spec;
        var restMeta = File.ReadAllText(Path.Combine(root, "Assets", spec.RestThemePackRel) + ".meta");
        Assert.False(BlenderRigImportMeta.NeedsReimport(restMeta, spec.PreferHumanoid, false, string.Empty, string.Empty, 0));
        var flapMeta = File.ReadAllText(Path.Combine(root, "Assets", spec.ClipThemePackRel) + ".meta");
        Assert.False(BlenderRigImportMeta.NeedsReimport(
            flapMeta, false, true, spec.ClipPoseName, spec.ClipTakeName, spec.ClipLastFrame));
        foreach (var extra in spec.ExtraClips)
        {
            var meta = File.ReadAllText(Path.Combine(root, "Assets", spec.ThemePackDir, extra.FileName) + ".meta");
            Assert.False(BlenderRigImportMeta.NeedsReimport(meta, false, true, extra.PoseName, extra.TakeName, extra.LastFrame));
        }

        Assert.True(BlenderRigImportMeta.NeedsReimport(
            restMeta.Replace("useFileScale: 0", "useFileScale: 1", StringComparison.Ordinal),
            false, false, string.Empty, string.Empty, 0));
        Assert.True(BlenderRigImportMeta.NeedsReimport(
            restMeta.Replace("animationType: 2", "animationType: 3", StringComparison.Ordinal),
            false, false, string.Empty, string.Empty, 0));
        Assert.True(BlenderRigImportMeta.NeedsReimport(
            restMeta.Replace("globalScale: 1", "globalScale: 0.01", StringComparison.Ordinal),
            false, false, string.Empty, string.Empty, 0));
        Assert.True(BlenderRigImportMeta.NeedsReimport(
            flapMeta.Replace("name: \"wing flap\"", "name: \"other\"", StringComparison.Ordinal),
            false, true, spec.ClipPoseName, spec.ClipTakeName, spec.ClipLastFrame));

        var bare =
            "animationType: 2\n" +
            "avatarSetup: 1\n" +
            "autoGenerateAvatarMappingIfUnspecified: 0\n" +
            "materialImportMode: 2\n" +
            "useFileScale: 0\n" +
            "globalScale: 1\n" +
            "optimizeBones: 0\n" +
            "bakeAxisConversion: 0\n" +
            "importAnimation: 1\n" +
            "clipAnimations: []\n";
        Assert.False(BlenderRigImportMeta.NeedsReimport(bare, false, false, string.Empty, string.Empty, 0));
        Assert.True(BlenderRigImportMeta.NeedsReimport(bare, true, false, string.Empty, string.Empty, 0));
        Assert.True(BlenderRigImportMeta.NeedsReimport(bare, false, true, "walk", "Scene", 30));
        var human = bare
            .Replace("animationType: 2", "animationType: 3", StringComparison.Ordinal)
            .Replace("autoGenerateAvatarMappingIfUnspecified: 0", "autoGenerateAvatarMappingIfUnspecified: 1", StringComparison.Ordinal);
        Assert.False(BlenderRigImportMeta.NeedsReimport(human, true, false, string.Empty, string.Empty, 0));
    }

    private static void AssertImportSkipsReimportWhenMetaAlreadyMatches(string player)
    {
        var ensureAt = player.IndexOf("private void EnsureImport", StringComparison.Ordinal);
        var pinAt = player.IndexOf("private bool PinClip", StringComparison.Ordinal);
        Assert.True(ensureAt >= 0 && pinAt > ensureAt);
        var ensure = player.Substring(ensureAt, pinAt - ensureAt);
        var gate = ensure.IndexOf("SavedSettingsMatch", StringComparison.Ordinal);
        var save = ensure.IndexOf("SaveAndReimport", StringComparison.Ordinal);
        Assert.True(gate >= 0 && save > gate);
        Assert.Contains("return;", ensure.Substring(gate, save - gate), StringComparison.Ordinal);
        Assert.Contains("BlenderRigImportMeta.NeedsReimport", player, StringComparison.Ordinal);
    }

    private static void AssertGenericDemo(
        BlenderRigSpec spec,
        string albedoStem,
        string normalStem,
        string distinctiveBone,
        string demoGuid,
        string menu)
    {
        Assert.Equal("rest", spec.PoseNames[0]);
        Assert.Equal("", spec.MetallicRoughnessFile);
        Assert.False(spec.PreferHumanoid);
        Assert.Equal(BlenderRigAvatar.CustomGeneric, spec.Avatar);
        Assert.True(BlenderRigSpec.IsRejectedMixamoRigFile(spec.RejectedMixamoFileName));
        Assert.Contains(spec, BlenderRigRoster.All);
        Assert.Contains(distinctiveBone, spec.BoneNames);

        var root = FindRepoRoot();
        var rest = Path.Combine(root, "Assets", spec.RestThemePackRel);
        var clip = Path.Combine(root, "Assets", spec.ClipThemePackRel);
        var rejected = Path.Combine(root, "Assets", spec.ThemePackDir, spec.RejectedMixamoFileName);
        var albedo = Path.Combine(root, "Assets", spec.BaseColorThemePackRel);
        var normal = Path.Combine(root, "Assets", spec.NormalThemePackRel);
        Assert.True(File.Exists(rest));
        Assert.True(File.Exists(clip));
        Assert.False(File.Exists(rejected));
        Assert.True(File.Exists(albedo));
        Assert.True(File.Exists(normal));
        var restBytes = File.ReadAllBytes(rest);
        var clipBytes = File.ReadAllBytes(clip);
        Assert.True(ContainsAscii(restBytes, spec.BoneRoot));
        Assert.True(ContainsAscii(restBytes, distinctiveBone));
        Assert.True(ContainsAscii(restBytes, albedoStem));
        Assert.True(ContainsAscii(restBytes, normalStem));
        Assert.False(ContainsAscii(restBytes, "mixamorig"));
        Assert.False(ContainsAscii(clipBytes, "mixamorig"));
        Assert.True(ContainsAscii(clipBytes, "Scene"));
        Assert.True(ContainsBytes(restBytes, File.ReadAllBytes(albedo)));
        Assert.True(ContainsBytes(restBytes, File.ReadAllBytes(normal)));
        var restMeta = File.ReadAllText(rest + ".meta");
        Assert.Contains("animationType: 2", restMeta, StringComparison.Ordinal);
        Assert.Contains("autoGenerateAvatarMappingIfUnspecified: 0", restMeta, StringComparison.Ordinal);
        Assert.Contains("clipAnimations: []", restMeta, StringComparison.Ordinal);
        Assert.Contains("bakeAxisConversion: 0", restMeta, StringComparison.Ordinal);
        Assert.Contains("useFileScale: 0", restMeta, StringComparison.Ordinal);
        Assert.Contains("materialImportMode: 2", restMeta, StringComparison.Ordinal);
        Assert.DoesNotContain("animationType: 3", restMeta, StringComparison.Ordinal);
        var clipMeta = File.ReadAllText(clip + ".meta");
        Assert.Contains("name: \"" + spec.ClipPoseName + "\"", clipMeta, StringComparison.Ordinal);
        Assert.Contains("takeName: " + spec.ClipTakeName, clipMeta, StringComparison.Ordinal);
        Assert.Contains("lastFrame: " + spec.ClipLastFrame, clipMeta, StringComparison.Ordinal);
        Assert.Contains("animationType: 2", clipMeta, StringComparison.Ordinal);
        Assert.Contains("bakeAxisConversion: 0", clipMeta, StringComparison.Ordinal);
        Assert.DoesNotContain("animationType: 3", clipMeta, StringComparison.Ordinal);
        Assert.DoesNotContain("mixamo.com", clipMeta, StringComparison.Ordinal);

        var scene = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Scenes", spec.SceneFileName));
        Assert.Contains(spec.Name + "Root", scene, StringComparison.Ordinal);
        Assert.Contains(demoGuid, scene, StringComparison.Ordinal);
        Assert.Contains("field of view: 54", scene, StringComparison.Ordinal);
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", spec.Name + "Actor.cs"));
        var demo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", spec.Name + "Demo.cs"));
        Assert.Contains("Survival.Domain.Enemies", actor, StringComparison.Ordinal);
        Assert.Contains(spec.Name + "Motion.Spec", actor, StringComparison.Ordinal);
        Assert.Contains("PlayPose", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("Mixamorig", actor, StringComparison.Ordinal);
        Assert.Contains(spec.DropdownObjectName, demo, StringComparison.Ordinal);
        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains(menu, editor, StringComparison.Ordinal);
        var build = File.ReadAllText(Path.Combine(root, "ProjectSettings", "EditorBuildSettings.asset"));
        Assert.Contains(spec.SceneFileName, build, StringComparison.Ordinal);
    }

    private static bool ContainsAscii(byte[] bytes, string needle)
    {
        return ContainsBytes(bytes, Encoding.ASCII.GetBytes(needle));
    }

    private static bool ContainsBytes(byte[] haystack, byte[] needle)
    {
        if (needle.Length == 0 || haystack.Length < needle.Length)
        {
            return false;
        }

        for (var i = 0; i <= haystack.Length - needle.Length; i++)
        {
            var hit = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    hit = false;
                    break;
                }
            }

            if (hit)
            {
                return true;
            }
        }

        return false;
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
