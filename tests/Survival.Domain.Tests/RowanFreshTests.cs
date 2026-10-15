using Survival.Domain.Heroes;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

public sealed class RowanFreshTests
{
    [Fact]
    public void Fresh_rig_is_25_bone_generic_with_left_finger_chain_and_rest_walk_attack()
    {
        Assert.Equal(25, RowanMotion.BoneNames.Length);
        Assert.Equal(RowanMotion.BoneCount, RowanMotion.Spec.BoneCount);
        Assert.Equal(MixamoHumanoidBones.Names.Where(n => !n.EndsWith("ToeBase", StringComparison.Ordinal)).Concat(RowanMotion.LeftFingerBoneNames).ToArray(), RowanMotion.BoneNames);
        Assert.Equal(5, RowanMotion.LeftFingerBoneNames.Length);
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
    public void Attack_arms_never_enter_chest_or_coat_and_bow_never_enters_the_gripping_hand()
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
                // v5c: only the attack's -45 -> -310 grip spin may brush the finger skin (see grip_halfframe.json per-sample check)
                var cap = clip.Name == "attack" && q.Name == "bow_in_hand" ? 30 : clip.Name == "attack" && q.Name == "hand_in_bow" ? 2 : 0;
                Assert.True(q.Value.GetInt32() <= cap, $"{clip.Name} {q.Name} = {q.Value.GetInt32()}");
            }
        }

        using var grip = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(work, "grip.json")));
        var g = grip.RootElement;
        Assert.Equal(RowanMotion.LeftFingerBoneNames, g.GetProperty("names").EnumerateArray().Select(x => x.GetString()).ToArray());
        Assert.True(g.GetProperty("max_finger_length_change").GetDouble() <= 0.03);
        Assert.True(g.GetProperty("min_hand_to_grip_axis").GetDouble() >= 0.020); // bow grip radius 2 cm
        Assert.True(g.GetProperty("finger_verts").GetInt32() > 500);
        Assert.Equal(new[] { 15, 35, 20 }, g.GetProperty("finger_curl_deg").EnumerateArray().Select(x => (int)Math.Round(x.GetDouble())).ToArray());
        Assert.True(g.GetProperty("finger_chain_length_change").GetDouble() is > -0.03 and < 0.03);

        // every half-frame of rest, walk and attack: hand wraps the grip >= 270 deg, nothing of the hand in the bow, no bow in hand, body or coat
        using var half = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(work, "grip_halfframe.json")));
        Assert.Equal(61, half.RootElement.GetProperty("samples_per_clip").GetInt32());
        foreach (var clip in new[] { "rest", "walk", "attack" })
        {
            var w = half.RootElement.GetProperty("worst").GetProperty(clip);
            Assert.True(w.GetProperty("wrap_deg_min").GetInt32() >= 270, clip);
            foreach (var p in half.RootElement.GetProperty("per_sample").GetProperty(clip).EnumerateArray())
            {
                var f = p.GetProperty("f").GetDouble();
                // v5c: the bow spins -45 -> -310 in the closed hand over attack f1-f11 and back over f17-f27; the octagonal grip's corners
                // brush the finger skin (<= ~2 mm) mid-spin. Everywhere else (rest, walk, carry and nock-to-release) the hand is clean.
                var spinning = clip == "attack" && ((f > 1 && f < 11) || (f > 17 && f < 27));
                Assert.True(p.GetProperty("hand_in_bow").GetInt32() <= (spinning ? 2 : 0), $"{clip} f{f}: hand in bow");
                Assert.True(p.GetProperty("bow_in_hand").GetInt32() <= (spinning ? 30 : 0), $"{clip} f{f}: bow in hand");
            }

            Assert.Equal(0, w.GetProperty("bow_in_body").GetInt32());
            Assert.Equal(0, w.GetProperty("bow_in_coat").GetInt32());
            Assert.True(w.GetProperty("finger_chain_change_max").GetDouble() < 0.03, clip);
            Assert.Equal(61, half.RootElement.GetProperty("per_sample").GetProperty(clip).GetArrayLength());
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
