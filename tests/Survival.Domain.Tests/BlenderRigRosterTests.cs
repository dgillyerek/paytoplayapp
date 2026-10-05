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
    }

    [Fact]
    public void Rowan_is_a_22_bone_mixamorig_with_rest_and_walk()
    {
        Assert.Equal(22, MixamoHumanoidBones.Count);
        Assert.Equal(22, MixamoHumanoidBones.Names.Length);
        Assert.Equal(MixamoHumanoidBones.Names.Length, MixamoHumanoidBones.Names.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("mixamorig:Hips", MixamoHumanoidBones.Root);
        Assert.Equal(22, RowanMotion.BoneCount);
        Assert.Equal(RowanMotion.BoneNames, MixamoHumanoidBones.Names);
        Assert.Equal("rest", RowanMotion.PoseNames[0]);
        Assert.Equal("walk", RowanMotion.PoseNames[1]);
        Assert.Equal("Scene", RowanMotion.WalkTakeName);
        Assert.NotEqual("mixamo.com", RowanMotion.WalkTakeName);
        Assert.Equal(1f, RowanMotion.WalkSeconds);
        Assert.Equal(30f, RowanMotion.WalkFrameRate);
        Assert.Equal(30, RowanMotion.WalkLastFrame);
        Assert.Equal("ROWAN_rig.fbx", RowanMotion.RejectedMixamoFileName);
        Assert.Equal("ROWAN_basecolor_0.jpg", RowanMotion.BaseColorFile);
        Assert.Equal("ROWAN_normal_2.jpg", RowanMotion.NormalFile);
        Assert.Equal("", RowanMotion.Spec.MetallicRoughnessFile);
        Assert.True(RowanMotion.Spec.PreferHumanoid);
        Assert.Equal("RowanClipDropdown", RowanMotion.Spec.DropdownObjectName);
        Assert.Contains(RowanMotion.Spec, BlenderRigRoster.All);
    }

    [Fact]
    public void Rowan_fbx_pair_embeds_the_maps_and_skips_the_mixamo_rig()
    {
        var root = FindRepoRoot();
        var rest = Path.Combine(root, "Assets", RowanMotion.RestThemePackRel);
        var walk = Path.Combine(root, "Assets", RowanMotion.WalkThemePackRel);
        var rejected = Path.Combine(root, "Assets", RowanMotion.ThemePackDir, RowanMotion.RejectedMixamoFileName);
        var albedo = Path.Combine(root, "Assets", RowanMotion.Spec.BaseColorThemePackRel);
        var normal = Path.Combine(root, "Assets", RowanMotion.Spec.NormalThemePackRel);
        Assert.True(File.Exists(rest));
        Assert.True(File.Exists(walk));
        Assert.False(File.Exists(rejected));
        Assert.True(File.Exists(albedo));
        Assert.True(File.Exists(normal));

        var restBytes = File.ReadAllBytes(rest);
        var walkBytes = File.ReadAllBytes(walk);
        Assert.True(ContainsAscii(restBytes, "mixamorig:Hips"));
        Assert.True(ContainsAscii(restBytes, "mixamorig:LeftToeBase"));
        Assert.True(ContainsAscii(restBytes, "ROWAN_basecolor_0"));
        Assert.True(ContainsAscii(restBytes, "ROWAN_normal_2"));
        Assert.True(ContainsAscii(walkBytes, "mixamorig:Hips"));
        Assert.True(ContainsAscii(walkBytes, "Scene"));
        Assert.False(ContainsAscii(walkBytes, "mixamo.com"));

        var albedoBytes = File.ReadAllBytes(albedo);
        var normalBytes = File.ReadAllBytes(normal);
        Assert.Equal(0xFF, albedoBytes[0]);
        Assert.Equal(0xD8, albedoBytes[1]);
        Assert.Equal(0xFF, normalBytes[0]);
        Assert.Equal(0xD8, normalBytes[1]);
        Assert.True(ContainsBytes(restBytes, albedoBytes));
        Assert.True(ContainsBytes(restBytes, normalBytes));

        var restMeta = File.ReadAllText(rest + ".meta");
        Assert.Contains("clipAnimations: []", restMeta, StringComparison.Ordinal);
        Assert.Contains("animationType: 3", restMeta, StringComparison.Ordinal);
        Assert.Contains("autoGenerateAvatarMappingIfUnspecified: 1", restMeta, StringComparison.Ordinal);
        Assert.Contains("bakeAxisConversion: 0", restMeta, StringComparison.Ordinal);
        Assert.Contains("useFileScale: 0", restMeta, StringComparison.Ordinal);
        Assert.Contains("materialImportMode: 2", restMeta, StringComparison.Ordinal);

        var walkMeta = File.ReadAllText(walk + ".meta");
        Assert.Contains("name: \"walk\"", walkMeta, StringComparison.Ordinal);
        Assert.Contains("takeName: Scene", walkMeta, StringComparison.Ordinal);
        Assert.Contains("lastFrame: 30", walkMeta, StringComparison.Ordinal);
        Assert.Contains("mirror: 0", walkMeta, StringComparison.Ordinal);
        Assert.Contains("bakeAxisConversion: 0", walkMeta, StringComparison.Ordinal);
        Assert.Contains("animationType: 3", walkMeta, StringComparison.Ordinal);
        Assert.Contains("materialImportMode: 2", walkMeta, StringComparison.Ordinal);
        Assert.DoesNotContain("mixamo.com", walkMeta, StringComparison.Ordinal);
    }

    [Fact]
    public void Lyra_is_a_22_bone_mixamorig_with_rest_and_walk()
    {
        Assert.Equal(22, LyraMotion.BoneCount);
        Assert.Equal(LyraMotion.BoneNames, MixamoHumanoidBones.Names);
        Assert.Equal("rest", LyraMotion.PoseNames[0]);
        Assert.Equal("walk", LyraMotion.PoseNames[1]);
        Assert.Equal("Scene", LyraMotion.WalkTakeName);
        Assert.Equal(1f, LyraMotion.WalkSeconds);
        Assert.Equal(30, LyraMotion.WalkLastFrame);
        Assert.Equal("LYRA_rig.fbx", LyraMotion.RejectedMixamoFileName);
        Assert.Equal("LYRA_basecolor_0.jpg", LyraMotion.BaseColorFile);
        Assert.Equal("LYRA_normal_2.jpg", LyraMotion.NormalFile);
        Assert.Equal("", LyraMotion.Spec.MetallicRoughnessFile);
        Assert.True(LyraMotion.Spec.PreferHumanoid);
        Assert.Equal("LyraClipDropdown", LyraMotion.Spec.DropdownObjectName);
        Assert.Contains(LyraMotion.Spec, BlenderRigRoster.All);
        AssertMixamoPair(LyraMotion.Spec, "LYRA_basecolor_0", "LYRA_normal_2");

        var root = FindRepoRoot();
        var scene = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Scenes", "Lyra.unity"));
        Assert.Contains("LyraRoot", scene, StringComparison.Ordinal);
        Assert.Contains("ce82c9521cb54da4bf669276f7f7c7cb", scene, StringComparison.Ordinal);
        Assert.Contains("field of view: 54", scene, StringComparison.Ordinal);
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "LyraActor.cs"));
        var demo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "LyraDemo.cs"));
        Assert.Contains("LyraMotion.Spec", actor, StringComparison.Ordinal);
        Assert.Contains("PlayPose", actor, StringComparison.Ordinal);
        Assert.Contains("LyraClipDropdown", demo, StringComparison.Ordinal);
        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("Survival/Lyra Demo (rest + walk, Game view 1080x1920)", editor, StringComparison.Ordinal);
    }

    [Fact]
    public void Vespera_is_a_22_bone_mixamorig_with_rest_and_walk()
    {
        AssertHumanoidDemo(
            VesperaMotion.Spec,
            "VESPERA_basecolor_0",
            "VESPERA_normal_2",
            "202447e53edf4ca4a70c7de7ffd9cf1e",
            "Survival/Vespera Demo (rest + walk, Game view 1080x1920)");
    }

    [Fact]
    public void Stormcrest_is_a_22_bone_mixamorig_with_rest_and_walk()
    {
        AssertHumanoidDemo(
            StormcrestMotion.Spec,
            "STORMCREST_basecolor_0",
            "STORMCREST_normal_2",
            "cd5dad8493e64f1bad48ecb1bf8586ed",
            "Survival/Stormcrest Demo (rest + walk, Game view 1080x1920)");
    }

    [Fact]
    public void Oakenshield_is_a_22_bone_mixamorig_with_rest_and_walk()
    {
        AssertHumanoidDemo(
            OakenshieldMotion.Spec,
            "OAKENSHIELD_basecolor_0",
            "OAKENSHIELD_normal_2",
            "b3654b0602e44b2790cc360faf2a7562",
            "Survival/Oakenshield Demo (rest + walk, Game view 1080x1920)");
    }

    [Fact]
    public void Bonequill_is_a_22_bone_mixamorig_with_rest_and_walk()
    {
        AssertHumanoidDemo(
            BonequillMotion.Spec,
            "BONEQUILL_basecolor_0",
            "BONEQUILL_normal_2",
            "e5bda4227e47499985f4e0ecd1e4eb3c",
            "Survival/Bonequill Demo (rest + walk, Game view 1080x1920)");
    }

    [Fact]
    public void Nightfang_is_a_33_bone_generic_quadruped_with_rest_and_trot()
    {
        Assert.Equal(33, NightfangMotion.BoneCount);
        Assert.Equal(33, NightfangMotion.BoneNames.Length);
        Assert.Equal("root", NightfangMotion.BoneRoot);
        Assert.Equal("toe_h.L", NightfangMotion.BoneNames[28]);
        Assert.Equal("rest", NightfangMotion.PoseNames[0]);
        Assert.Equal("trot", NightfangMotion.PoseNames[1]);
        Assert.Equal("Scene", NightfangMotion.TrotTakeName);
        Assert.Equal(1f, NightfangMotion.TrotSeconds);
        Assert.Equal(30, NightfangMotion.TrotLastFrame);
        Assert.Equal(BlenderRigAvatar.CustomGeneric, NightfangMotion.Spec.Avatar);
        Assert.False(NightfangMotion.Spec.PreferHumanoid);
        Assert.Equal("NIGHTFANG_rig.fbx", NightfangMotion.RejectedMixamoFileName);
        AssertGenericDemo(
            NightfangMotion.Spec,
            "NIGHTFANG_basecolor_0",
            "NIGHTFANG_normal_2",
            "toe_h.L",
            "5a612f20bf58466c8634f45803244d4f",
            "Survival/Nightfang Demo (rest + trot, Game view 1080x1920)");
    }

    [Fact]
    public void Ashwyrm_rest_is_a_generic_dragon_until_wingflap_is_wired()
    {
        var root = FindRepoRoot();
        var dir = Path.Combine(root, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "enemies", "3d", "ashwyrm");
        var rest = Path.Combine(dir, "ASHWYRM_blenderig.fbx");
        var rejected = Path.Combine(dir, "ASHWYRM_rig.fbx");
        var albedo = Path.Combine(dir, "ASHWYRM_blenderig.fbm", "ASHWYRM_basecolor_0.jpg");
        var normal = Path.Combine(dir, "ASHWYRM_blenderig.fbm", "ASHWYRM_normal_2.jpg");
        Assert.True(File.Exists(rest));
        Assert.False(File.Exists(rejected));
        Assert.True(File.Exists(albedo));
        Assert.True(File.Exists(normal));
        Assert.False(File.Exists(Path.Combine(root, "Assets", "Survival", "Scenes", "Ashwyrm.unity")));
        var restBytes = File.ReadAllBytes(rest);
        Assert.True(ContainsAscii(restBytes, "wing_root.L"));
        Assert.True(ContainsAscii(restBytes, "ASHWYRM_body"));
        Assert.False(ContainsAscii(restBytes, "mixamorig"));
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
    }

    private static void AssertHumanoidDemo(BlenderRigSpec spec, string albedoStem, string normalStem, string demoGuid, string menu)
    {
        Assert.Equal(22, spec.BoneCount);
        Assert.Equal(MixamoHumanoidBones.Names, spec.BoneNames);
        Assert.Equal("rest", spec.PoseNames[0]);
        Assert.Equal("walk", spec.PoseNames[1]);
        Assert.Equal("Scene", spec.ClipTakeName);
        Assert.Equal(1f, spec.ClipSeconds);
        Assert.Equal(30, spec.ClipLastFrame);
        Assert.Equal("", spec.MetallicRoughnessFile);
        Assert.True(spec.PreferHumanoid);
        Assert.True(BlenderRigSpec.IsRejectedMixamoRigFile(spec.RejectedMixamoFileName));
        Assert.Contains(spec, BlenderRigRoster.All);
        AssertMixamoPair(spec, albedoStem, normalStem);

        var root = FindRepoRoot();
        var scene = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Scenes", spec.SceneFileName));
        Assert.Contains(spec.Name + "Root", scene, StringComparison.Ordinal);
        Assert.Contains(demoGuid, scene, StringComparison.Ordinal);
        Assert.Contains("field of view: 54", scene, StringComparison.Ordinal);
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", spec.Name + "Actor.cs"));
        var demo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", spec.Name + "Demo.cs"));
        Assert.Contains(spec.Name + "Motion.Spec", actor, StringComparison.Ordinal);
        Assert.Contains("PlayPose", actor, StringComparison.Ordinal);
        Assert.Contains(spec.DropdownObjectName, demo, StringComparison.Ordinal);
        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains(menu, editor, StringComparison.Ordinal);
    }

    private static void AssertMixamoPair(BlenderRigSpec spec, string albedoStem, string normalStem)
    {
        var root = FindRepoRoot();
        var rest = Path.Combine(root, "Assets", spec.RestThemePackRel);
        var walk = Path.Combine(root, "Assets", spec.ClipThemePackRel);
        var rejected = Path.Combine(root, "Assets", spec.ThemePackDir, spec.RejectedMixamoFileName);
        var albedo = Path.Combine(root, "Assets", spec.BaseColorThemePackRel);
        var normal = Path.Combine(root, "Assets", spec.NormalThemePackRel);
        Assert.True(File.Exists(rest));
        Assert.True(File.Exists(walk));
        Assert.False(File.Exists(rejected));
        Assert.True(File.Exists(albedo));
        Assert.True(File.Exists(normal));
        var restBytes = File.ReadAllBytes(rest);
        var walkBytes = File.ReadAllBytes(walk);
        Assert.True(ContainsAscii(restBytes, "mixamorig:Hips"));
        Assert.True(ContainsAscii(restBytes, albedoStem));
        Assert.True(ContainsAscii(restBytes, normalStem));
        Assert.True(ContainsAscii(walkBytes, "mixamorig:Hips"));
        Assert.True(ContainsAscii(walkBytes, "Scene"));
        Assert.False(ContainsAscii(walkBytes, "mixamo.com"));
        Assert.True(ContainsBytes(restBytes, File.ReadAllBytes(albedo)));
        Assert.True(ContainsBytes(restBytes, File.ReadAllBytes(normal)));
        var restMeta = File.ReadAllText(rest + ".meta");
        Assert.Contains("animationType: 3", restMeta, StringComparison.Ordinal);
        Assert.Contains("bakeAxisConversion: 0", restMeta, StringComparison.Ordinal);
        Assert.Contains("useFileScale: 0", restMeta, StringComparison.Ordinal);
        Assert.Contains("materialImportMode: 2", restMeta, StringComparison.Ordinal);
        var walkMeta = File.ReadAllText(walk + ".meta");
        Assert.Contains("name: \"" + spec.ClipPoseName + "\"", walkMeta, StringComparison.Ordinal);
        Assert.Contains("takeName: " + spec.ClipTakeName, walkMeta, StringComparison.Ordinal);
        Assert.Contains("lastFrame: " + spec.ClipLastFrame, walkMeta, StringComparison.Ordinal);
        Assert.Contains("bakeAxisConversion: 0", walkMeta, StringComparison.Ordinal);
        Assert.Contains("animationType: 3", walkMeta, StringComparison.Ordinal);
        Assert.DoesNotContain("mixamo.com", walkMeta, StringComparison.Ordinal);
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

    [Fact]
    public void Player_binds_embedded_maps_and_does_not_rewrite_the_rig()
    {
        var root = FindRepoRoot();
        var player = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlenderRigPlayer.cs"));
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "RowanActor.cs"));
        var demo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "RowanDemo.cs"));
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
        Assert.Contains("RowanMotion.Spec", actor, StringComparison.Ordinal);
        Assert.Contains("RowanClipDropdown", demo, StringComparison.Ordinal);
        Assert.Contains("RowanMotion.Spec", demo, StringComparison.Ordinal);
        Assert.Contains("SetValueWithoutNotify(0)", shared, StringComparison.Ordinal);
        Assert.Contains("DropdownObjectName", shared, StringComparison.Ordinal);

        var scene = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Scenes", "Rowan.unity"));
        Assert.Contains("RowanRoot", scene, StringComparison.Ordinal);
        Assert.Contains("834d72d2cb424b758ab79f8936200656", scene, StringComparison.Ordinal);
        Assert.Contains("field of view: 54", scene, StringComparison.Ordinal);

        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("Survival/Rowan Demo (rest + walk, Game view 1080x1920)", editor, StringComparison.Ordinal);
        Assert.Contains("Rowan.unity", editor, StringComparison.Ordinal);
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
