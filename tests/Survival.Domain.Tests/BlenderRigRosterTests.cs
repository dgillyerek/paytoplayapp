using System.Security.Cryptography;
using System.Text;
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
        AssertHumanoidDemo(
            RowanMotion.Spec,
            "ROWAN_basecolor_0",
            "ROWAN_normal_2",
            "834d72d2cb424b758ab79f8936200656",
            "Survival/Rowan Demo (rest + walk, Game view 1080x1920)");
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

    [Fact]
    public void Rowan_walk_scene_opens_on_walk_and_leaves_the_rest_demo()
    {
        var root = FindRepoRoot();
        var restScene = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Scenes", "Rowan.unity"));
        var walkScene = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Scenes", "RowanWalk.unity"));
        var walkDemo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "RowanWalkDemo.cs"));
        var shared = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlenderRigDemo.cs"));
        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));

        Assert.Contains("834d72d2cb424b758ab79f8936200656", restScene, StringComparison.Ordinal);
        Assert.Contains("Survival.Unity.RowanDemo", restScene, StringComparison.Ordinal);
        Assert.DoesNotContain("RowanWalkDemo", restScene, StringComparison.Ordinal);
        Assert.Contains("RowanRoot", walkScene, StringComparison.Ordinal);
        Assert.Contains("field of view: 54", walkScene, StringComparison.Ordinal);
        Assert.Contains("c3e91a04b7d64f18a2e5c6b8d0f14a27", walkScene, StringComparison.Ordinal);
        Assert.Contains("Survival.Unity.RowanWalkDemo", walkScene, StringComparison.Ordinal);
        Assert.Contains("RowanMotion.WalkPoseName", walkDemo, StringComparison.Ordinal);
        Assert.Contains("PlayNamedPose(OpeningPose)", shared, StringComparison.Ordinal);
        Assert.Contains("SetValueWithoutNotify(0)", shared, StringComparison.Ordinal);
        Assert.Contains("Survival/Rowan Walk Demo (walk, Game view 1080x1920)", editor, StringComparison.Ordinal);
        Assert.Contains("RowanWalk.unity", editor, StringComparison.Ordinal);
        Assert.Contains("Survival/Rowan Demo (rest + walk, Game view 1080x1920)", editor, StringComparison.Ordinal);
    }

    [Fact]
    public void Rowan_remesh_demo_plays_the_uploaded_rest_and_fixed_walk()
    {
        var root = FindRepoRoot();
        var rest = Path.Combine(root, "Assets", RowanRemeshMotion.RestThemePackRel);
        var walk = Path.Combine(root, "Assets", RowanRemeshMotion.WalkThemePackRel);
        var albedo = Path.Combine(root, "Assets", RowanRemeshMotion.Spec.BaseColorThemePackRel);
        var normal = Path.Combine(root, "Assets", RowanRemeshMotion.Spec.NormalThemePackRel);
        Assert.Equal("486d9cf097f6d0fc0dd8ae6713b5f726", Md5(rest));
        // Design walkfix_male_20261007 replaces the folded ab148b67 walk in the ThemePack (Game-view walk fix).
        Assert.Equal("3bd5f6d392ff61c8f4ae1ac23a39fae3", Md5(walk));
        var walkFixDir = Path.Combine(root, "design", "survival-theme-a-fantasy", "heroes", "anim", "rowan", "walkfix_male_20261007");
        Assert.Equal("3bd5f6d392ff61c8f4ae1ac23a39fae3", Md5(Path.Combine(walkFixDir, "ROWAN_male_blenderig_walk.fbx")));
        Assert.Contains("3bd5f6d392ff61c8f4ae1ac23a39fae3  ./ROWAN_male_blenderig_walk.fbx", File.ReadAllText(Path.Combine(walkFixDir, "CHECKSUMS.md5")));
        Assert.Equal("ROWAN_male_blenderig.fbx", RowanRemeshMotion.RestFileName);
        Assert.Equal("ROWAN_male_blenderig_walk.fbx", RowanRemeshMotion.WalkFileName);
        Assert.Equal(30, RowanRemeshMotion.WalkLastFrame);
        var designDir = Path.Combine(
            root,
            "design",
            "survival-theme-a-fantasy",
            "heroes",
            "anim",
            "rowan",
            "blender_rig_male_20261007");
        Assert.Equal("486d9cf097f6d0fc0dd8ae6713b5f726", Md5(Path.Combine(designDir, "ROWAN_male_blenderig.fbx")));
        Assert.Equal("ab148b67d09b4f39f2923f4db8549d66", Md5(Path.Combine(designDir, "ROWAN_male_blenderig_walk.fbx")));
        Assert.Equal("2104c645a29ff97cafdaaaa145fe96ea", Md5(Path.Combine(designDir, "ROWAN_male_blenderig_sheet.jpg")));
        Assert.Equal("feb555f5d48ed4c2f950c948fb57b85f", Md5(Path.Combine(root, "Assets", RowanMotion.RestThemePackRel)));
        Assert.Equal("1653b1bd3e15819d073e030e803fe203", Md5(Path.Combine(root, "Assets", RowanMotion.WalkThemePackRel)));
        var femaleRig = Path.Combine(root, "design", "survival-theme-a-fantasy", "heroes", "anim", "rowan", "blender_rig");
        Assert.Equal("feb555f5d48ed4c2f950c948fb57b85f", Md5(Path.Combine(femaleRig, "ROWAN_blenderig.fbx")));
        Assert.Equal("1653b1bd3e15819d073e030e803fe203", Md5(Path.Combine(femaleRig, "ROWAN_blenderig_walk.fbx")));
        Assert.Equal("e831cbb7dedc8b4f5f8cf104e56bd2d5", Md5(Path.Combine(femaleRig, "ROWAN_blenderig.blend")));
        var oldRemesh = Path.Combine(root, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "heroes", "3d", "rowan_remesh");
        Assert.Equal("e1f38c54af17e181abda2df8cdc46492", Md5(Path.Combine(oldRemesh, "ROWAN_remesh_blenderig.fbx")));
        Assert.Equal("b1b5aa6b587cbfa1e83729157605fd96", Md5(Path.Combine(oldRemesh, "ROWAN_remesh_blenderig_walk.fbx")));
        var restBytes = File.ReadAllBytes(rest);
        var walkBytes = File.ReadAllBytes(walk);
        Assert.True(ContainsAscii(restBytes, "mixamorig:Hips"));
        Assert.True(ContainsAscii(restBytes, "ROWAN_male_basecolor_0"));
        Assert.True(ContainsAscii(restBytes, "ROWAN_male_normal_2"));
        Assert.True(ContainsBytes(restBytes, File.ReadAllBytes(albedo)));
        Assert.True(ContainsBytes(restBytes, File.ReadAllBytes(normal)));
        var restMeta = File.ReadAllText(rest + ".meta");
        Assert.Contains("animationType: 3", restMeta, StringComparison.Ordinal);
        Assert.Contains("bakeAxisConversion: 0", restMeta, StringComparison.Ordinal);
        Assert.Contains("useFileScale: 0", restMeta, StringComparison.Ordinal);
        Assert.Contains("materialImportMode: 2", restMeta, StringComparison.Ordinal);
        var walkMeta = File.ReadAllText(walk + ".meta");
        Assert.Contains("name: \"walk\"", walkMeta, StringComparison.Ordinal);
        Assert.Contains("takeName: Scene", walkMeta, StringComparison.Ordinal);
        Assert.Contains("firstFrame: 0", walkMeta, StringComparison.Ordinal);
        Assert.Contains("lastFrame: 30", walkMeta, StringComparison.Ordinal);
        Assert.Contains("bakeAxisConversion: 0", walkMeta, StringComparison.Ordinal);
        Assert.Contains("animationType: 3", walkMeta, StringComparison.Ordinal);
        Assert.True(ContainsAscii(walkBytes, "Scene"));
        Assert.False(ContainsAscii(walkBytes, "mixamo.com"));
        Assert.Equal("rest", RowanRemeshMotion.PoseNames[0]);
        Assert.Equal("walk", RowanRemeshMotion.PoseNames[1]);
        Assert.Equal(1, BlenderRigRoster.All.Count);
        Assert.DoesNotContain(RowanRemeshMotion.Spec, BlenderRigRoster.All);

        var scene = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Scenes", "RowanRemesh.unity"));
        var tip = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Scenes", "Rowan.unity"));
        var walkScene = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Scenes", "RowanWalk.unity"));
        Assert.Contains("RowanRemeshRoot", scene, StringComparison.Ordinal);
        Assert.Contains("field of view: 54", scene, StringComparison.Ordinal);
        Assert.Contains("4e7b2c91a6d84f0e8b3a5c1d9f246073", scene, StringComparison.Ordinal);
        Assert.Contains("Survival.Unity.RowanRemeshDemo", scene, StringComparison.Ordinal);
        Assert.DoesNotContain("RowanRemeshDemo", tip, StringComparison.Ordinal);
        Assert.DoesNotContain("RowanRemeshDemo", walkScene, StringComparison.Ordinal);
        var demo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "RowanRemeshDemo.cs"));
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "RowanRemeshActor.cs"));
        Assert.Contains("RowanRemeshClipDropdown", demo, StringComparison.Ordinal);
        Assert.Contains("RowanRemeshMotion.Spec", demo, StringComparison.Ordinal);
        Assert.DoesNotContain("OpeningPose", demo, StringComparison.Ordinal);
        Assert.Contains("PlayPose", actor, StringComparison.Ordinal);
        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("Survival/Rowan Remesh Demo (rest + walk + attack, Game view 1080x1920)", editor, StringComparison.Ordinal);
        Assert.Contains("RowanRemesh.unity", editor, StringComparison.Ordinal);
        Assert.Contains("Survival/Rowan Demo (rest + walk, Game view 1080x1920)", editor, StringComparison.Ordinal);
        Assert.Contains("Survival/Rowan Walk Demo (walk, Game view 1080x1920)", editor, StringComparison.Ordinal);
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
        Assert.Equal(1, BlenderRigRoster.All.Count);
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
        Assert.Equal(1, BlenderRigRoster.All.Count);
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

    private static string Md5(string path)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(md5.ComputeHash(stream)).ToLowerInvariant();
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
