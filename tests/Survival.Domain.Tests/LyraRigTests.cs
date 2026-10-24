using System.Text;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

public sealed class LyraRigTests
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
    public void Lyra_is_a_20_bone_mixamorig_with_rest_and_walk()
    {
        Assert.Equal(22, MixamoHumanoidBones.Count);
        Assert.Equal(22, MixamoHumanoidBones.Names.Length);
        Assert.Equal(MixamoHumanoidBones.Names.Length, MixamoHumanoidBones.Names.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("mixamorig:Hips", MixamoHumanoidBones.Root);
        AssertMixamorigDemo(
            LyraMotion.Spec,
            "LYRA_tighten_basecolor_0",
            "LYRA_tighten_normal_2",
            "ce82c9521cb54da4bf669276f7f7c7cb",
            "Survival/Lyra Demo (rest + walk + attack, Game view 1080x1920)");
        Assert.Equal("LYRA_tighten_metal_rough_1.png", LyraMotion.Spec.MetallicRoughnessFile);
        // Derek removed both ToeBase bones in his rig edit (2026-10-08 22:45); the toes are weighted to the Foot.
        Assert.Equal(20, LyraMotion.BoneNames.Length);
        Assert.Equal(MixamoHumanoidBones.Names.Where(n => !n.EndsWith("ToeBase", StringComparison.Ordinal)).ToArray(), LyraMotion.BoneNames);
    }


    [Fact]
    public void Player_binds_embedded_maps_and_does_not_rewrite_the_rig()
    {
        var root = FindRepoRoot();
        var player = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlenderRigPlayer.cs"));
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "LyraActor.cs"));
        var demo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "LyraDemo.cs"));
        var shared = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlenderRigDemo.cs"));
        Assert.Contains("bakeAxisConversion = false", player, StringComparison.Ordinal);
        Assert.DoesNotContain("bakeAxisConversion = true", player, StringComparison.Ordinal);
        // The one pose write is the runner upper-body layer carrying the take's hips turn onto the spine each frame
        // (BlenderRigPlayer.CarryHipsTurn); the rig itself is never rewritten.
        Assert.DoesNotContain("localRotation =", player.Replace("_spine.localRotation = ", "", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.DoesNotContain("localScale =", player, StringComparison.Ordinal);
        Assert.Contains("ImportViaMaterialDescription", player, StringComparison.Ordinal);
        Assert.Contains("ModelImporterAnimationType.Human", player, StringComparison.Ordinal);
        Assert.Contains("ModelImporterAnimationType.Generic", player, StringComparison.Ordinal);
        Assert.Contains("isHuman", player, StringComparison.Ordinal);
        Assert.Contains("_BaseMap", player, StringComparison.Ordinal);
        Assert.Contains("_BumpMap", player, StringComparison.Ordinal);
        Assert.Contains("IsRejectedMixamoRigFile", player, StringComparison.Ordinal);
        Assert.Contains("PlayPose", actor, StringComparison.Ordinal);
        Assert.Contains("LyraMotion.Spec", actor, StringComparison.Ordinal);
        Assert.Contains("LyraClipDropdown", demo, StringComparison.Ordinal);
        Assert.Contains("LyraMotion.Spec", demo, StringComparison.Ordinal);
        Assert.Contains("SetValueWithoutNotify(0)", shared, StringComparison.Ordinal);
        Assert.Contains("DropdownObjectName", shared, StringComparison.Ordinal);
        AssertImportSkipsReimportWhenMetaAlreadyMatches(player);

        Assert.False(File.Exists(Path.Combine(root, "Assets", "Survival", "Scenes", "Lyra.unity")), "one-character demo scenes were removed 2026-10-19");

        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.DoesNotContain("Survival/Lyra Demo (rest + walk + attack, Game view 1080x1920)", editor, StringComparison.Ordinal);
        Assert.DoesNotContain("Lyra.unity", editor, StringComparison.Ordinal);
    }


    private static void AssertMixamorigDemo(BlenderRigSpec spec, string albedoStem, string normalStem, string demoGuid, string menu)
    {
        Assert.Equal(spec.BoneNames.Length, spec.BoneCount);
        Assert.All(spec.BoneNames, n => Assert.Contains(n, MixamoHumanoidBones.Names));
        Assert.Equal("rest", spec.PoseNames[0]);
        Assert.Equal("walk", spec.PoseNames[1]);
        Assert.Equal("Scene", spec.ClipTakeName);
        Assert.Equal(1f, spec.ClipSeconds);
        Assert.Equal(30, spec.ClipLastFrame);
        // Generic like the locked rigs: Humanoid retargeting stripped the attack's body turn and step (root motion off).
        Assert.False(spec.PreferHumanoid);
        Assert.Equal(BlenderRigAvatar.CustomGeneric, spec.Avatar);
        Assert.True(BlenderRigSpec.IsRejectedMixamoRigFile(spec.RejectedMixamoFileName));
        AssertMixamoPair(spec, albedoStem, normalStem);

        var root = FindRepoRoot();
        Assert.False(File.Exists(Path.Combine(root, "Assets", "Survival", "Scenes", spec.SceneFileName)), "one-character demo scenes were removed 2026-10-19");
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", spec.Name + "Actor.cs"));
        var demo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", spec.Name + "Demo.cs"));
        Assert.Contains(spec.Name + "Motion.Spec", actor, StringComparison.Ordinal);
        Assert.Contains("PlayPose", actor, StringComparison.Ordinal);
        Assert.Contains(spec.DropdownObjectName, demo, StringComparison.Ordinal);
        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.DoesNotContain(menu, editor, StringComparison.Ordinal);
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
        Assert.Contains("animationType: 2", restMeta, StringComparison.Ordinal);
        Assert.Contains("bakeAxisConversion: 0", restMeta, StringComparison.Ordinal);
        Assert.Contains("useFileScale: 0", restMeta, StringComparison.Ordinal);
        Assert.Contains("materialImportMode: 2", restMeta, StringComparison.Ordinal);
        var walkMeta = File.ReadAllText(walk + ".meta");
        Assert.Contains("name: \"" + spec.ClipPoseName + "\"", walkMeta, StringComparison.Ordinal);
        Assert.Contains("takeName: " + spec.ClipTakeName, walkMeta, StringComparison.Ordinal);
        Assert.Contains("lastFrame: " + spec.ClipLastFrame, walkMeta, StringComparison.Ordinal);
        Assert.Contains("bakeAxisConversion: 0", walkMeta, StringComparison.Ordinal);
        Assert.Contains("animationType: 2", walkMeta, StringComparison.Ordinal);
        Assert.DoesNotContain("mixamo.com", walkMeta, StringComparison.Ordinal);
    }

    [Fact]
    public void Import_meta_check_matches_what_EnsureImport_writes()
    {
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

    [Fact]
    public void Lyra_imports_generic_and_saved_settings_already_match_so_play_mode_does_not_reimport()
    {
        var root = FindRepoRoot();
        var spec = LyraMotion.Spec;
        var restMeta = File.ReadAllText(Path.Combine(root, "Assets", spec.RestThemePackRel) + ".meta");
        Assert.Contains("animationType: 2", restMeta, StringComparison.Ordinal);
        Assert.False(BlenderRigImportMeta.NeedsReimport(restMeta, spec.PreferHumanoid, false, string.Empty, string.Empty, 0));
        var walkMeta = File.ReadAllText(Path.Combine(root, "Assets", spec.ClipThemePackRel) + ".meta");
        Assert.Contains("animationType: 2", walkMeta, StringComparison.Ordinal);
        Assert.False(BlenderRigImportMeta.NeedsReimport(
            walkMeta, spec.PreferHumanoid, true, spec.ClipPoseName, spec.ClipTakeName, spec.ClipLastFrame));
        foreach (var extra in spec.ExtraClips)
        {
            var meta = File.ReadAllText(Path.Combine(root, "Assets", spec.ThemePackDir, extra.FileName) + ".meta");
            Assert.Contains("animationType: 2", meta, StringComparison.Ordinal);
            Assert.False(BlenderRigImportMeta.NeedsReimport(meta, spec.PreferHumanoid, true, extra.PoseName, extra.TakeName, extra.LastFrame));
        }
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

        Assert.False(File.Exists(Path.Combine(root, "Assets", "Survival", "Scenes", spec.SceneFileName)), "one-character demo scenes were removed 2026-10-19");
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", spec.Name + "Actor.cs"));
        var demo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", spec.Name + "Demo.cs"));
        Assert.Contains("Survival.Domain.Enemies", actor, StringComparison.Ordinal);
        Assert.Contains(spec.Name + "Motion.Spec", actor, StringComparison.Ordinal);
        Assert.Contains("PlayPose", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("Mixamorig", actor, StringComparison.Ordinal);
        Assert.Contains(spec.DropdownObjectName, demo, StringComparison.Ordinal);
        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.DoesNotContain(menu, editor, StringComparison.Ordinal);
        var build = File.ReadAllText(Path.Combine(root, "ProjectSettings", "EditorBuildSettings.asset"));
        Assert.DoesNotContain(spec.SceneFileName, build, StringComparison.Ordinal);
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
