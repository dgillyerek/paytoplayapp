using Survival.Domain.Enemies;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

public sealed class BonequillFreshTests
{
    [Fact]
    public void Fresh_rig_is_20_bone_generic_with_rest_walk_attack()
    {
        Assert.Equal(20, BonequillMotion.BoneNames.Length);
        Assert.Equal(BonequillMotion.BoneCount, BonequillMotion.Spec.BoneCount);
        Assert.Equal(MixamoHumanoidBones.Names.Where(n => !n.EndsWith("ToeBase", StringComparison.Ordinal)).ToArray(), BonequillMotion.BoneNames);
        Assert.Equal(BlenderRigAvatar.CustomGeneric, BonequillMotion.Spec.Avatar);
        Assert.Equal(new[] { "rest", "walk", "attack" }, BonequillMotion.PoseNames);
        Assert.Equal(4, BonequillAttack.Spec.Tracks.Length);
        foreach (var track in BonequillAttack.Spec.Tracks)
        {
            Assert.Equal(31, track.Frames.Length);
        }

        Assert.Single(BonequillAttack.IdleProps);
        Assert.Equal("BONEQUILL_bow", BonequillAttack.IdleProps[0].AttackTrackName);
        Assert.Equal("mixamorig:LeftHand", BonequillAttack.IdleProps[0].Bone);
        Assert.Equal(14, BonequillAttack.ReleaseFrame);
    }

    [Fact]
    public void ThemePack_and_design_files_match_and_import_generic()
    {
        var root = FindRepoRoot();
        var pack = Path.Combine(root, "Assets", BonequillMotion.ThemePackDir);
        var design = Path.Combine(root, BonequillMotion.DesignDir, "export");
        foreach (var (file, md5, clip) in new[]
        {
            (BonequillMotion.RestFileName, BonequillMotion.RestMd5, ""),
            (BonequillMotion.WalkFileName, BonequillMotion.WalkMd5, "walk"),
            (BonequillAttack.FileName, BonequillAttack.FileMd5, "attack")
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

        foreach (var map in new[] { BonequillMotion.BaseColorFile, BonequillMotion.NormalFile, BonequillMotion.MetallicRoughnessFile })
        {
            Assert.True(File.Exists(Path.Combine(pack, BonequillMotion.TextureFolder, map)), map);
        }

        for (var i = 0; i < BonequillAttack.PropFileNames.Length; i++)
        {
            Assert.Equal(BonequillAttack.PropFileMd5s[i], Md5(Path.Combine(pack, BonequillAttack.PropFileNames[i])));
        }

        Assert.Equal(BonequillCape.FileMd5, Md5(Path.Combine(pack, BonequillCape.FileName)));
        Assert.Equal(BonequillCape.FileMd5, Md5(Path.Combine(design, BonequillCape.FileName)));
        var capeMeta = File.ReadAllText(Path.Combine(pack, BonequillCape.FileName + ".meta"));
        Assert.Contains("importAnimation: 0", capeMeta, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(pack, BonequillCape.TextureFolder, BonequillCape.BaseColorFile)));
        Assert.True(File.Exists(Path.Combine(pack, BonequillCape.TextureFolder, BonequillCape.NormalFile)));
        var scene = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Scenes", "Bonequill.unity"));
        Assert.Contains("BonequillRoot", scene, StringComparison.Ordinal);
        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("Survival/Bonequill Demo (rest + walk + attack, Game view 1080x1920)", editor, StringComparison.Ordinal);
    }

    [Fact]
    public void Cape_is_spring_only_on_upper_body_anchors_with_leg_colliders()
    {
        var anchors = new[] { "cape_Spine2", "cape_LeftShoulder", "cape_RightShoulder" };
        Assert.Equal(7, BonequillCape.Chains.Length);
        var bones = 3;
        foreach (var chain in BonequillCape.Chains)
        {
            Assert.Contains(chain.ParentBone, anchors);
            Assert.Equal(3, chain.Bones.Length);
            Assert.Equal(chain.JointCount * 3, chain.DesignJoints.Length);
            Assert.DoesNotContain(chain.Bones, b => b.StartsWith("mixamorig:", StringComparison.Ordinal));
            Assert.True(chain.Gravity <= 0.2f, chain.Name);
            bones += chain.Bones.Length;
        }

        Assert.Equal(BonequillCape.CapeBoneCount, bones);
        Assert.Equal(5, BonequillCape.Spec.Colliders.Length);
        Assert.Contains(BonequillCape.Spec.Colliders, c => c.Bone == "mixamorig:LeftLeg");
        for (var i = 0; i < BonequillCape.Anchors.GetLength(0); i++)
        {
            Assert.Contains(BonequillCape.Anchors[i, 1], BonequillMotion.BoneNames);
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
