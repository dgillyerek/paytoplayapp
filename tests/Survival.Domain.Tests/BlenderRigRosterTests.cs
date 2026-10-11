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
    public void Vespera_is_derek_16_bone_generic_rig_with_rest_and_walk()
    {
        Assert.Equal(22, MixamoHumanoidBones.Count);
        Assert.Equal("mixamorig:Hips", MixamoHumanoidBones.Root);
        var spec = VesperaMotion.Spec;
        Assert.Equal(16, spec.BoneCount);
        Assert.Equal(16, spec.BoneNames.Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain("mixamorig:Hips", spec.BoneNames);
        Assert.False(spec.PreferHumanoid);
        Assert.Equal("rest", spec.PoseNames[0]);
        Assert.Equal("walk", spec.PoseNames[1]);
        Assert.Equal("Scene", spec.ClipTakeName);
        Assert.Equal(30, spec.ClipLastFrame);
        Assert.Equal("", spec.MetallicRoughnessFile);
        Assert.Contains(spec, BlenderRigRoster.All);
        var root = FindRepoRoot();
        var rest = File.ReadAllBytes(Path.Combine(root, "Assets", spec.RestThemePackRel));
        var walk = File.ReadAllBytes(Path.Combine(root, "Assets", spec.ClipThemePackRel));
        foreach (var bone in spec.BoneNames)
        {
            Assert.True(ContainsAscii(rest, bone), bone);
        }

        Assert.True(ContainsAscii(walk, "Scene"));
        Assert.True(ContainsBytes(rest, File.ReadAllBytes(Path.Combine(root, "Assets", spec.BaseColorThemePackRel))));
        Assert.True(ContainsBytes(rest, File.ReadAllBytes(Path.Combine(root, "Assets", spec.NormalThemePackRel))));
        Assert.Contains("animationType: 2", File.ReadAllText(Path.Combine(root, "Assets", spec.RestThemePackRel) + ".meta"), StringComparison.Ordinal);
        var scene = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Scenes", spec.SceneFileName));
        Assert.Contains("VesperaRoot", scene, StringComparison.Ordinal);
        Assert.Contains("202447e53edf4ca4a70c7de7ffd9cf1e", scene, StringComparison.Ordinal);
        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("Survival/Vespera Demo (rest + walk + attack, Game view 1080x1920)", editor, StringComparison.Ordinal);
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
        AssertImportSkipsReimportWhenMetaAlreadyMatches(player);

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

    [Fact]
    public void Vespera_saved_import_settings_already_match_so_play_mode_does_not_reimport()
    {
        var root = FindRepoRoot();
        var spec = VesperaMotion.Spec;
        Assert.False(spec.PreferHumanoid);
        var restMeta = File.ReadAllText(Path.Combine(root, "Assets", spec.RestThemePackRel) + ".meta");
        Assert.False(BlenderRigImportMeta.NeedsReimport(restMeta, false, false, string.Empty, string.Empty, 0));
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
            flapMeta.Replace("name: \"walk\"", "name: \"other\"", StringComparison.Ordinal),
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
