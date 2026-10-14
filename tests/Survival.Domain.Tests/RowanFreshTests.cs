using Survival.Domain.Heroes;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

public sealed class RowanFreshTests
{
    [Fact]
    public void Fresh_rig_is_20_bone_generic_with_rest_walk_attack()
    {
        Assert.Equal(20, RowanMotion.BoneNames.Length);
        Assert.Equal(RowanMotion.BoneCount, RowanMotion.Spec.BoneCount);
        Assert.Equal(MixamoHumanoidBones.Names.Where(n => !n.EndsWith("ToeBase", StringComparison.Ordinal)).ToArray(), RowanMotion.BoneNames);
        Assert.Equal(BlenderRigAvatar.CustomGeneric, RowanMotion.Spec.Avatar);
        Assert.Equal(new[] { "rest", "walk", "attack" }, RowanMotion.PoseNames);
        Assert.Equal(5, RowanAttack.Spec.Tracks.Length);
        foreach (var track in RowanAttack.Spec.Tracks)
        {
            Assert.Equal(31, track.Frames.Length);
        }

        Assert.Single(RowanAttack.IdleProps);
        Assert.Equal("ROWAN_quiver", RowanAttack.IdleProps[0].AttackTrackName);
        Assert.Equal("mixamorig:Spine2", RowanAttack.IdleProps[0].Bone);
        Assert.Equal("mixamorig:LeftHand", RowanAttack.Spec.Tracks[1].Bone);
        Assert.Equal(14, RowanAttack.ReleaseFrame);
    }

    [Fact]
    public void ThemePack_and_design_files_match_and_import_generic()
    {
        var root = FindRepoRoot();
        var pack = Path.Combine(root, "Assets", RowanMotion.ThemePackDir);
        var design = Path.Combine(root, RowanMotion.DesignDir, "export");
        foreach (var (file, md5, clip) in new[]
        {
            (RowanMotion.RestFileName, RowanMotion.RestMd5, ""),
            (RowanMotion.WalkFileName, RowanMotion.WalkMd5, "walk"),
            (RowanAttack.FileName, RowanAttack.FileMd5, "attack")
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

        foreach (var map in new[] { RowanMotion.BaseColorFile, RowanMotion.NormalFile, RowanMotion.MetallicRoughnessFile })
        {
            Assert.True(File.Exists(Path.Combine(pack, RowanMotion.TextureFolder, map)), map);
        }

        for (var i = 0; i < RowanAttack.PropFileNames.Length; i++)
        {
            Assert.Equal(RowanAttack.PropFileMd5s[i], Md5(Path.Combine(pack, RowanAttack.PropFileNames[i])));
            Assert.Equal(RowanAttack.PropFileMd5s[i], Md5(Path.Combine(root, RowanMotion.DesignDir, "props", RowanAttack.PropFileNames[i])));
        }
        var scene = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Scenes", "Rowan.unity"));
        Assert.Contains("RowanRoot", scene, StringComparison.Ordinal);
        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("Survival/Rowan Demo (rest + walk + bow attack, Game view 1080x1920)", editor, StringComparison.Ordinal);
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
