using System.Security.Cryptography;
using System.Text.Json;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

/// <summary>Oakenshield Design cloth-rule split (2026-10-07) swapped into the ThemePack. HOLD merge until Derek Game-view PASS.</summary>
public sealed class OakenshieldClothSplitTests
{
    private static readonly string[] Kinds = { "rest", "walk", "attack" };

    private static string PackFile(string kind) => kind switch
    {
        "rest" => OakenshieldMotion.RestFileName,
        "walk" => OakenshieldMotion.WalkFileName,
        _ => OakenshieldMotion.AttackFileName
    };

    private static string DesignFile(string kind) => kind switch
    {
        "rest" => OakenshieldClothSplit.RestFileName,
        "walk" => OakenshieldClothSplit.WalkFileName,
        _ => OakenshieldClothSplit.AttackFileName
    };

    private static string Md5Of(string kind) => kind switch
    {
        "rest" => OakenshieldClothSplit.RestMd5,
        "walk" => OakenshieldClothSplit.WalkMd5,
        _ => OakenshieldClothSplit.AttackMd5
    };

    [Fact]
    public void Oakenshield_themepack_rest_walk_attack_are_the_clothsplit_fbxs()
    {
        var root = FindRepoRoot();
        var pack = Path.Combine(root, "Assets", OakenshieldMotion.ThemePackDir);
        var design = Path.Combine(root, OakenshieldClothSplit.DesignDir.Replace('/', Path.DirectorySeparatorChar));
        Assert.Equal("6d754014dd09a23ad616378a70826b14", OakenshieldClothSplit.RestMd5);
        Assert.Equal("57fba709bf3c367dd9ac0b4bc552be26", OakenshieldClothSplit.WalkMd5);
        Assert.Equal("769543a3e39321857dc50e734aadffd2", OakenshieldClothSplit.AttackMd5);
        Assert.Equal("9bf48c3405516ffe8740e7f9ae3e7550", OakenshieldClothSplit.BlendMd5);
        foreach (var kind in Kinds)
        {
            Assert.Equal(Md5Of(kind), Md5(Path.Combine(pack, PackFile(kind))));
            Assert.Equal(Md5Of(kind), Md5(Path.Combine(design, DesignFile(kind))));
            Assert.True(File.Exists(Path.Combine(pack, PackFile(kind) + ".meta")), PackFile(kind) + ".meta keeps its GUID");
        }

        Assert.Equal(OakenshieldClothSplit.BlendMd5, Md5(Path.Combine(design, OakenshieldClothSplit.BlendFileName)));
        Assert.Equal(OakenshieldAttack.FileName, OakenshieldMotion.AttackFileName);
        Assert.Equal(OakenshieldClothSplit.AttackMd5, OakenshieldAttack.FileMd5);
        Assert.NotEqual(OakenshieldClothSplit.PreviousRestMd5, OakenshieldClothSplit.RestMd5);
        foreach (var file in new[] { "NOTE.md", "CHECKSUMS.md5", Path.Combine("work", "build_meta.json"), Path.Combine("work", "qc.json") })
        {
            Assert.True(File.Exists(Path.Combine(design, file)), file);
        }

        var verified = 0;
        foreach (var line in File.ReadAllLines(Path.Combine(design, "CHECKSUMS.md5")))
        {
            var parts = line.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || parts[0].Length != 32)
            {
                continue;
            }

            var local = Path.Combine(design, parts[1].TrimStart('*').Trim().Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(local))
            {
                Assert.Equal(parts[0], Md5(local));
                verified++;
            }
        }

        Assert.True(verified >= 4, "CHECKSUMS.md5 verified " + verified);
    }

    [Fact]
    public void Oakenshield_bones_bind_by_name_with_every_body_and_cloth_chain_bone_present()
    {
        var root = FindRepoRoot();
        var pack = Path.Combine(root, "Assets", OakenshieldMotion.ThemePackDir);
        var spec = OakenshieldClothSplit.Spec;
        Assert.Equal(36, spec.RigBoneCount);
        Assert.Equal(spec.RigBoneCount - MixamoHumanoidBones.Count, spec.ClothBoneNames.Length);
        Assert.Equal(spec.ClothBoneNames.Length, spec.ClothBoneNames.Distinct(StringComparer.Ordinal).Count());
        foreach (var body in MixamoHumanoidBones.Names)
        {
            Assert.False(spec.IsClothBone(body), body);
        }

        foreach (var kind in Kinds)
        {
            var rig = FbxRig.Read(Path.Combine(pack, PackFile(kind)));
            Assert.Equal(spec.RigBoneCount, rig.Limbs.Count);
            Assert.Equal(rig.Limbs.Count, rig.Limbs.Distinct(StringComparer.Ordinal).Count());
            foreach (var body in MixamoHumanoidBones.Names)
            {
                Assert.Contains(body, rig.Limbs);
            }

            // Cloth chains sit after Spine's children, so the first 22 bones are NOT the body: index binding is wrong.
            Assert.NotEqual(MixamoHumanoidBones.Names, rig.Limbs.Take(MixamoHumanoidBones.Count).ToArray());
            var bodyOrder = rig.Limbs.Where(n => !spec.IsClothBone(n)).ToArray();
            Assert.Equal(MixamoHumanoidBones.Names, bodyOrder);
            foreach (var chain in spec.Chains)
            {
                for (var i = 0; i < chain.Bones.Length; i++)
                {
                    Assert.Contains(chain.Bones[i], rig.Limbs);
                    Assert.Equal(i == 0 ? chain.ParentBone : chain.Bones[i - 1], rig.Parent[chain.Bones[i]]);
                }
            }

            Assert.Equal(spec.ClothBoneNames.OrderBy(n => n, StringComparer.Ordinal), rig.Limbs.Where(spec.IsClothBone).OrderBy(n => n, StringComparer.Ordinal));
        }

        foreach (var collider in spec.Colliders)
        {
            Assert.Contains(collider.Bone, MixamoHumanoidBones.Names);
            Assert.Contains(collider.TailBone, MixamoHumanoidBones.Names);
        }

        foreach (var bone in OakenshieldAttack.Spec.CalibrationBones)
        {
            Assert.Contains(bone, MixamoHumanoidBones.Names);
        }

        foreach (var track in OakenshieldAttack.Spec.Tracks.Where(t => t.Kind == AttackTrackKind.Held))
        {
            Assert.Contains(track.Bone, MixamoHumanoidBones.Names);
        }
    }

    [Fact]
    public void Oakenshield_walk_and_attack_carry_no_cloth_motion_only_body_motion()
    {
        var root = FindRepoRoot();
        var pack = Path.Combine(root, "Assets", OakenshieldMotion.ThemePackDir);
        var spec = OakenshieldClothSplit.Spec;
        var rest = FbxRig.Read(Path.Combine(pack, PackFile("rest")));
        foreach (var bone in spec.ClothBoneNames)
        {
            Assert.False(rest.Curves.ContainsKey(bone), bone + " keyed in rest");
        }

        foreach (var kind in new[] { "walk", "attack" })
        {
            var rig = FbxRig.Read(Path.Combine(pack, PackFile(kind)));
            Assert.Equal(new[] { "Scene" }, rig.Stacks.ToArray());
            foreach (var bone in spec.ClothBoneNames)
            {
                // Design's full-bone bake writes constant rest keys; no cloth bone moves (deg / cm). Unity masks them out on import.
                Assert.True(rig.MaxRange(bone) < 1e-3, bone + " moves in " + kind + ": " + rig.MaxRange(bone));
            }

            var bodyMotion = MixamoHumanoidBones.Names.Max(b => rig.MaxRange(b, "Lcl Rotation"));
            Assert.True(bodyMotion > 5, kind + " body rotation range " + bodyMotion);
        }

        Assert.True(FbxRig.Read(Path.Combine(pack, PackFile("walk"))).MaxRange("mixamorig:LeftLeg", "Lcl Rotation") > 10);
        var player = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlenderRigPlayer.cs"));
        Assert.Contains("ConfigureClipFromMask", player, StringComparison.Ordinal);
        Assert.Contains("HasClothCurves", player, StringComparison.Ordinal);
    }

    [Fact]
    public void Oakenshield_spring_and_collider_config_per_chain_matches_note()
    {
        var root = FindRepoRoot();
        var design = Path.Combine(root, OakenshieldClothSplit.DesignDir.Replace('/', Path.DirectorySeparatorChar));
        var spec = OakenshieldClothSplit.Spec;
        var rows = File.ReadAllLines(Path.Combine(design, "NOTE.md"))
            .Select(l => l.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToArray())
            .Where(c => c.Length == 11 && c[0] != "chain" && !c[0].StartsWith("---", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(rows.Length, spec.Chains.Length);
        Assert.Equal(5, spec.Chains.Length);
        using var meta = JsonDocument.Parse(File.ReadAllText(Path.Combine(design, "work", "build_meta.json")));
        var metaChains = meta.RootElement.GetProperty("chains").EnumerateArray().ToDictionary(e => e.GetProperty("prefix").GetString()!, e => e);
        foreach (var row in rows)
        {
            var chain = spec.Chains.Single(c => c.Name == row[0]);
            Assert.Equal(row[1], chain.Sector);
            Assert.Equal(row[2].Split(',').Select(b => b.Trim()).ToArray(), chain.Bones);
            Assert.Equal(row[3], chain.ParentBone);
            Assert.Equal("mixamorig:Spine", chain.ParentBone);
            Assert.Equal(float.Parse(row[4], System.Globalization.CultureInfo.InvariantCulture), chain.LengthMetres, 3);
            Assert.Equal(float.Parse(row[6], System.Globalization.CultureInfo.InvariantCulture), chain.Stiffness, 3);
            Assert.Equal(float.Parse(row[7], System.Globalization.CultureInfo.InvariantCulture), chain.Damping, 3);
            Assert.Equal(float.Parse(row[8], System.Globalization.CultureInfo.InvariantCulture), chain.Drag, 3);
            Assert.Equal(float.Parse(row[9], System.Globalization.CultureInfo.InvariantCulture), chain.Gravity, 3);
            Assert.Equal(float.Parse(row[10], System.Globalization.CultureInfo.InvariantCulture), chain.RadiusMetres, 3);
            Assert.Equal(chain.IsFrontStrip ? 60f : 75f, chain.AngleLimitDegrees);
            Assert.Equal(chain.JointCount * 3, chain.DesignJoints.Length);
            var joints = metaChains[chain.Name].GetProperty("joints").EnumerateArray().ToArray();
            Assert.Equal(chain.JointCount, joints.Length);
            for (var i = 0; i < joints.Length; i++)
            {
                var j = joints[i].EnumerateArray().Select(v => (float)v.GetDouble()).ToArray();
                Assert.True(Math.Abs(j[0] - chain.DesignJoint(i).X) < 1e-4f, chain.Name + " joint " + i);
                Assert.True(Math.Abs(j[1] - chain.DesignJoint(i).Y) < 1e-4f, chain.Name + " joint " + i);
                Assert.True(Math.Abs(j[2] - chain.DesignJoint(i).Z) < 1e-4f, chain.Name + " joint " + i);
            }
        }

        var colliders = spec.Colliders.ToDictionary(c => c.Name);
        Assert.Equal(5, colliders.Count);
        Assert.Equal(ClothColliderKind.Capsule, colliders["LeftThigh"].Kind);
        Assert.Equal(("mixamorig:LeftUpLeg", "mixamorig:LeftLeg", 0.07f), (colliders["LeftThigh"].Bone, colliders["LeftThigh"].TailBone, colliders["LeftThigh"].RadiusMetres));
        Assert.Equal(("mixamorig:LeftLeg", "mixamorig:LeftFoot", 0.055f), (colliders["LeftShin"].Bone, colliders["LeftShin"].TailBone, colliders["LeftShin"].RadiusMetres));
        Assert.Equal(("mixamorig:RightUpLeg", "mixamorig:RightLeg", 0.07f), (colliders["RightThigh"].Bone, colliders["RightThigh"].TailBone, colliders["RightThigh"].RadiusMetres));
        Assert.Equal(("mixamorig:RightLeg", "mixamorig:RightFoot", 0.055f), (colliders["RightShin"].Bone, colliders["RightShin"].TailBone, colliders["RightShin"].RadiusMetres));
        Assert.Equal(ClothColliderKind.Sphere, colliders["Hips"].Kind);
        Assert.Equal(0.15f, colliders["Hips"].RadiusMetres);
        var note = File.ReadAllText(Path.Combine(design, "NOTE.md"));
        Assert.Contains("r ≈ 0.07 thigh, 0.055 shin) and a sphere on Hips (r ≈ 0.15)", note, StringComparison.Ordinal);
        Assert.Contains("about 60° per joint for the front strips and about 75°", note, StringComparison.Ordinal);
    }

    [Fact]
    public void Oakenshield_every_chain_settles_at_rest_and_resets_cleanly()
    {
        var spec = OakenshieldClothSplit.Spec;
        var down = new System.Numerics.Vector3(0, 0, -1);
        foreach (var chain in spec.Chains)
        {
            var rest = Enumerable.Range(0, chain.JointCount).Select(chain.DesignJoint).ToArray();
            var solver = new ClothSpringSolver(chain, 1f);
            solver.Bind(rest, Array.Empty<ClothCollider>());
            var spin = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitZ, 0.3f);
            for (var f = 0; f < 30; f++)
            {
                var a = System.Numerics.Quaternion.Slerp(System.Numerics.Quaternion.Identity, spin, MathF.Sin(f / 30f * MathF.PI));
                solver.Step(1f / 60f, rest.Select(p => System.Numerics.Vector3.Transform(p, a)).ToArray(), Array.Empty<ClothCollider>(), down, System.Numerics.Vector3.Zero);
            }

            var before = new System.Numerics.Vector3[solver.Count];
            var move = 0f;
            for (var f = 0; f < 120; f++)
            {
                for (var i = 0; i < solver.Count; i++)
                {
                    before[i] = solver[i];
                }

                solver.Step(1f / 60f, rest, Array.Empty<ClothCollider>(), down, System.Numerics.Vector3.Zero);
                if (f >= 60)
                {
                    move = Math.Max(move, Enumerable.Range(0, solver.Count).Max(i => System.Numerics.Vector3.Distance(before[i], solver[i])));
                }
            }

            Assert.True(move < 1e-4f, chain.Name + " jitters at rest: " + move);
            solver.Reset(rest);
            for (var i = 0; i < solver.Count; i++)
            {
                Assert.Equal(rest[i], solver[i]);
            }
        }
    }

    [Fact]
    public void Oakenshield_spring_rig_is_wired_into_the_demo()
    {
        var root = FindRepoRoot();
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "OakenshieldActor.cs"));
        Assert.Contains("OakenshieldClothSplit.Spec", actor, StringComparison.Ordinal);
        var player = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlenderRigPlayer.cs"));
        Assert.Contains("AddComponent<ClothSpringRig>()", player, StringComparison.Ordinal);
        Assert.Contains("_cloth?.ResetToRest()", player, StringComparison.Ordinal);
        Assert.Contains("HumanMappedByName", player, StringComparison.Ordinal);
        var rig = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "ClothSpringRig.cs"));
        Assert.Contains("private void LateUpdate()", rig, StringComparison.Ordinal);
        Assert.Contains("ClothSpringSolver", rig, StringComparison.Ordinal);
        Assert.Contains("RestLocalRotation", rig, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(root, "Assets", "Survival", "Unity", "ClothSpringRig.cs.meta")));
        Assert.True(File.Exists(Path.Combine(root, "Assets", "Survival", "Runtime", "Roster", "ClothSpring.cs.meta")));
    }

    private static string Md5(string path)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(md5.ComputeHash(stream)).ToLowerInvariant();
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "Assets")) && Directory.Exists(Path.Combine(dir.FullName, "tests")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
