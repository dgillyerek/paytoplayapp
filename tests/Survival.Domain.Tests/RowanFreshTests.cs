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
        Assert.Equal(4, RowanAttack.Spec.Tracks.Length);
        foreach (var track in RowanAttack.Spec.Tracks)
        {
            Assert.Equal(31, track.Frames.Length);
        }

        Assert.Single(RowanAttack.IdleProps);
        Assert.Equal("ROWAN_bow", RowanAttack.IdleProps[0].AttackTrackName);
        Assert.Equal("mixamorig:LeftHand", RowanAttack.IdleProps[0].Bone);
        Assert.Equal("ROWAN_bow", RowanAttack.Spec.Tracks[0].ObjectName);
        Assert.Equal("mixamorig:LeftHand", RowanAttack.Spec.Tracks[0].Bone);
        Assert.Single(RowanAttack.Spec.Tracks, t => t.PropFileName == "ROWAN_bow.fbx");
        Assert.DoesNotContain(RowanAttack.Spec.Tracks, t => t.Bone.Contains("Spine", StringComparison.Ordinal));
        Assert.DoesNotContain(RowanAttack.PropFileNames, n => n.Contains("quiver", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("mixamorig:RightHand", RowanAttack.Spec.Tracks[1].Bone);
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

        Assert.False(File.Exists(Path.Combine(pack, "ROWAN_quiver.fbx")));
        Assert.Equal(RowanCoat.FileMd5, Md5(Path.Combine(pack, RowanCoat.FileName)));
        Assert.Equal(RowanCoat.FileMd5, Md5(Path.Combine(design, RowanCoat.FileName)));
        var coatMeta = File.ReadAllText(Path.Combine(pack, RowanCoat.FileName + ".meta"));
        Assert.Contains("importAnimation: 0", coatMeta, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(pack, RowanCoat.TextureFolder, RowanCoat.BaseColorFile)));
        Assert.True(File.Exists(Path.Combine(pack, RowanCoat.TextureFolder, RowanCoat.NormalFile)));
        var scene = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Scenes", "Rowan.unity"));
        Assert.Contains("RowanRoot", scene, StringComparison.Ordinal);
        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("Survival/Rowan Demo (rest + walk + bow attack, Game view 1080x1920)", editor, StringComparison.Ordinal);
    }

    [Fact]
    public void Coat_tails_are_spring_chains_on_hips_never_the_legs()
    {
        Assert.Equal(1, RowanCoat.Anchors.GetLength(0));
        Assert.Equal("coat_Hips", RowanCoat.Anchors[0, 0]);
        Assert.Equal("mixamorig:Hips", RowanCoat.Anchors[0, 1]);
        Assert.Equal(7, RowanCoat.Chains.Length);
        var bones = 1;
        foreach (var chain in RowanCoat.Chains)
        {
            Assert.Equal("coat_Hips", chain.ParentBone);
            Assert.InRange(chain.Bones.Length, 2, 4);
            Assert.Equal(0.45f, chain.Damping);
            Assert.Equal(35f, chain.AngleLimitDegrees);
            Assert.True(chain.StiffnessAt(chain.JointCount - 1) < chain.StiffnessAt(1));
            Assert.All(chain.Bones, b => Assert.StartsWith("coat_", b, StringComparison.Ordinal));
            Assert.True(chain.DesignJoint(0).Z <= RowanCoat.SplitHeightMetres + 0.001f);
            bones += chain.Bones.Length;
        }

        Assert.Equal(RowanCoat.CoatBoneCount, bones);
        Assert.Contains(RowanCoat.Spec.Colliders, c => c.Bone == "mixamorig:LeftUpLeg");
        Assert.Contains(RowanCoat.Spec.Colliders, c => c.Bone == "mixamorig:RightLeg");
        Assert.Contains(RowanCoat.Spec.Colliders, c => c.Bone == "mixamorig:RightHand" && c.ExtendMetres > 0f);
        Assert.All(RowanCoat.Spec.ClothBoneNames, b => Assert.DoesNotContain(b, RowanMotion.BoneNames));
    }

    [Fact]
    public void Attack_arms_never_enter_chest_or_coat_and_bow_never_enters_the_fist()
    {
        var work = Path.Combine(FindRepoRoot(), RowanMotion.DesignDir, "work", "apose_20261014");
        foreach (var file in new[] { "pen.json", "pen_rest_walk.json" })
        {
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(work, file)));
            foreach (var clip in doc.RootElement.EnumerateObject().Where(p => !p.Name.StartsWith("_", StringComparison.Ordinal) && p.Value.ValueKind == System.Text.Json.JsonValueKind.Array))
            {
                var rows = clip.Value.EnumerateArray().ToArray();
                Assert.Equal(61, rows.Length); // every frame and half-frame 0..30
                foreach (var r in rows)
                {
                    foreach (var k in new[] { "L_inside", "R_inside", "L_in_coat", "R_in_coat" })
                    {
                        Assert.Equal(0, r.GetProperty(k).GetInt32());
                    }
                }
            }
        }

        using var sheet = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(work, "sheet.json")));
        foreach (var clip in sheet.RootElement.GetProperty("bow_checks_max").EnumerateObject())
        {
            foreach (var q in clip.Value.EnumerateObject())
            {
                Assert.Equal(0, q.Value.GetInt32());
            }
        }

        using var fist = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(work, "fist.json")));
        Assert.Equal(0, fist.RootElement.GetProperty("bow_in_hand_bind").GetInt32());
        Assert.Equal(0, fist.RootElement.GetProperty("hand_in_bow_bind").GetInt32());
        Assert.True(fist.RootElement.GetProperty("curled_verts").GetInt32() > 500);
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
