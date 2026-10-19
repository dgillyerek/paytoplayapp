using Survival.Domain.Heroes;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

public sealed class OakenshieldFreshTests
{
    [Fact]
    public void Fresh_rig_is_20_bone_generic_with_rest_walk_attack()
    {
        Assert.Equal(20, OakenshieldMotion.BoneNames.Length);
        Assert.Equal(OakenshieldMotion.BoneCount, OakenshieldMotion.Spec.BoneCount);
        Assert.Equal(MixamoHumanoidBones.Names.Where(n => !n.EndsWith("ToeBase", StringComparison.Ordinal)).ToArray(), OakenshieldMotion.BoneNames);
        Assert.Equal(BlenderRigAvatar.CustomGeneric, OakenshieldMotion.Spec.Avatar);
        Assert.Equal(new[] { "rest", "walk", "attack", "attack2", "attack3" }, OakenshieldMotion.PoseNames);
        Assert.Equal(31, OakenshieldAttack.Spec.Tracks[0].Frames.Length);
        Assert.Equal(31, OakenshieldAttack.Spec.Tracks[1].Frames.Length);
        Assert.Equal(14, OakenshieldAttack.ReleaseFrame);
    }

    [Fact]
    public void ThemePack_and_design_files_match_and_import_generic()
    {
        var root = FindRepoRoot();
        var pack = Path.Combine(root, "Assets", OakenshieldMotion.ThemePackDir);
        var design = Path.Combine(root, OakenshieldMotion.DesignDir);
        foreach (var (file, md5, clip) in new[]
        {
            (OakenshieldMotion.RestFileName, OakenshieldMotion.RestMd5, ""),
            (OakenshieldMotion.WalkFileName, OakenshieldMotion.WalkMd5, "walk"),
            (OakenshieldAttack.FileName, OakenshieldAttack.FileMd5, "attack")
        })
        {
            Assert.Equal(md5, Md5(Path.Combine(pack, file)));
            Assert.Equal(md5, Md5(Path.Combine(design, file)));
            var meta = File.ReadAllText(Path.Combine(pack, file + ".meta"));
            Assert.Contains("animationType: 2", meta, StringComparison.Ordinal);
            Assert.DoesNotContain("animationType: 3", meta, StringComparison.Ordinal);
            Assert.Contains("bakeAxisConversion: 0", meta, StringComparison.Ordinal);
            if (clip.Length > 0)
            {
                Assert.Contains("name: \"" + clip + "\"", meta, StringComparison.Ordinal);
                Assert.Contains("lastFrame: 30", meta, StringComparison.Ordinal);
            }
        }

        foreach (var map in new[] { OakenshieldMotion.BaseColorFile, OakenshieldMotion.NormalFile, OakenshieldMotion.MetallicRoughnessFile })
        {
            Assert.True(File.Exists(Path.Combine(pack, OakenshieldMotion.TextureFolder, map)), map);
        }

        Assert.Equal(OakenshieldAttack.PropFileMd5s[0], Md5(Path.Combine(pack, OakenshieldAttack.PropFileNames[0])));
        var scene = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Scenes", "Oakenshield.unity"));
        Assert.Contains("OakenshieldRoot", scene, StringComparison.Ordinal);
        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("Survival/Oakenshield Demo (rest + walk + attack, Game view 1080x1920)", editor, StringComparison.Ordinal);
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
