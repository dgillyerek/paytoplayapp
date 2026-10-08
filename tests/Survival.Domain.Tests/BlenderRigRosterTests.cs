using System.Text;
using Survival.Domain.Enemies;
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
    public void Vespera_is_a_22_bone_mixamorig_with_rest_and_walk()
    {
        Assert.Equal(22, MixamoHumanoidBones.Count);
        Assert.Equal(22, MixamoHumanoidBones.Names.Length);
        Assert.Equal(MixamoHumanoidBones.Names.Length, MixamoHumanoidBones.Names.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("mixamorig:Hips", MixamoHumanoidBones.Root);
        AssertHumanoidDemo(
            VesperaMotion.Spec,
            "VESPERA_basecolor_0",
            "VESPERA_normal_2",
            "202447e53edf4ca4a70c7de7ffd9cf1e",
            "Survival/Vespera Demo (rest + walk + attack, Game view 1080x1920)");
    }


    [Fact]
    public void Player_binds_embedded_maps_and_does_not_rewrite_the_rig()
    {
        var root = FindRepoRoot();
        var player = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlenderRigPlayer.cs"));
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "VesperaActor.cs"));
        var demo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "VesperaDemo.cs"));
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
        Assert.Contains("VesperaMotion.Spec", actor, StringComparison.Ordinal);
        Assert.Contains("VesperaClipDropdown", demo, StringComparison.Ordinal);
        Assert.Contains("VesperaMotion.Spec", demo, StringComparison.Ordinal);
        Assert.Contains("SetValueWithoutNotify(0)", shared, StringComparison.Ordinal);
        Assert.Contains("DropdownObjectName", shared, StringComparison.Ordinal);

        var scene = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Scenes", "Vespera.unity"));
        Assert.Contains("VesperaRoot", scene, StringComparison.Ordinal);
        Assert.Contains("202447e53edf4ca4a70c7de7ffd9cf1e", scene, StringComparison.Ordinal);
        Assert.Contains("field of view: 54", scene, StringComparison.Ordinal);

        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("Survival/Vespera Demo (rest + walk + attack, Game view 1080x1920)", editor, StringComparison.Ordinal);
        Assert.Contains("Vespera.unity", editor, StringComparison.Ordinal);
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
