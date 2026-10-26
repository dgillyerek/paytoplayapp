using Survival.Domain.Enemies;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

/// <summary>
/// Ironhowl's authored attacks (ground pound, backhand sweep, shoulder charge; Derek 2026-10-24: "he just punches").
/// They are keyposed on his own skeleton and written into a copy of the overhead smash FBX, so they have to keep that
/// file's layout: the same animated node paths (mixamorig:Hips root, no Armature wrapper that would stop them
/// binding), the mixamo.com take and a stop on their last frame.
/// </summary>
public sealed class IronhowlAuthoredAttackTests
{
    private const long TicksPerFrame = 46186158000L / 30L;

    private static readonly (string File, string Md5, string Pose, int Last, int Contact)[] Clips =
    {
        (IronhowlMotion.Attack4FileName, IronhowlMotion.Attack4Md5, IronhowlMotion.Attack4PoseName, IronhowlMotion.Attack4LastFrame, IronhowlMotion.Attack4ContactFrame),
        (IronhowlMotion.Attack5FileName, IronhowlMotion.Attack5Md5, IronhowlMotion.Attack5PoseName, IronhowlMotion.Attack5LastFrame, IronhowlMotion.Attack5ContactFrame),
        (IronhowlMotion.Attack6FileName, IronhowlMotion.Attack6Md5, IronhowlMotion.Attack6PoseName, IronhowlMotion.Attack6LastFrame, IronhowlMotion.Attack6ContactFrame)
    };

    [Fact]
    public void Authored_attacks_bind_like_the_mixamo_clips_and_are_pinned_generic()
    {
        var root = FindRepoRoot();
        var smash = IronhowlRestBindingTests.AnimatedPaths(File.ReadAllBytes(Path.Combine(root, "Assets", IronhowlMotion.Attack2ThemePackRel)));
        var walk = IronhowlRestBindingTests.AnimatedPaths(File.ReadAllBytes(Path.Combine(root, "Assets", IronhowlMotion.WalkThemePackRel)));
        var walkBones = walk.Select(p => p.Split('|')[0]).ToHashSet(StringComparer.Ordinal);
        var sums = File.ReadAllText(Path.Combine(root, IronhowlMotion.DesignAuthoredAttacksDir, "CHECKSUMS.md5"));
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "IronhowlActor.cs"));

        foreach (var (file, md5, pose, last, contact) in Clips)
        {
            var pack = Path.Combine(root, "Assets", IronhowlMotion.ThemePackDir, file);
            Assert.Equal(md5, Md5(pack));
            Assert.Equal(md5, Md5(Path.Combine(root, IronhowlMotion.DesignAuthoredAttacksDir, file)));
            Assert.Contains(md5 + "  " + file, sums, StringComparison.Ordinal);

            var bytes = File.ReadAllBytes(pack);
            var paths = IronhowlRestBindingTests.AnimatedPaths(bytes);
            Assert.DoesNotContain(paths, p => p.Contains("Armature", StringComparison.Ordinal));
            Assert.True(paths.SetEquals(smash), file + " paths differ from the overhead smash");
            Assert.All(paths, p => Assert.Contains(p.Split('|')[0], walkBones));

            var doc = IronhowlRestBindingTests.Fbx.Parse(bytes);
            var stack = doc.Find("AnimationStack").Single();
            Assert.Equal(IronhowlMotion.TakeName, stack.PropString(1).Split('\0')[0]);
            var stop = stack.Find("P").Single(p => p.PropString(0) == "LocalStop");
            Assert.Equal(last * TicksPerFrame, stop.PropLong(4));
            Assert.InRange(contact, 8, last - 12);

            var meta = File.ReadAllText(pack + ".meta");
            Assert.Contains("name: \"" + pose + "\"", meta, StringComparison.Ordinal);
            Assert.Contains("takeName: mixamo.com", meta, StringComparison.Ordinal);
            Assert.Contains("lastFrame: " + last, meta, StringComparison.Ordinal);
            Assert.Contains("animationType: 2", meta, StringComparison.Ordinal);
            Assert.Contains("mirror: 0", meta, StringComparison.Ordinal);
            Assert.Contains("IronhowlMotion.Attack" + pose.Substring(6) + "ThemePackRel, IronhowlMotion.Attack" + pose.Substring(6) + "PoseName", actor, StringComparison.Ordinal);
        }

        // Each has its own guid.
        var guids = Clips.Select(c => File.ReadLines(Path.Combine(root, "Assets", IronhowlMotion.ThemePackDir, c.File + ".meta")).Single(l => l.StartsWith("guid:", StringComparison.Ordinal))).ToArray();
        Assert.Equal(guids.Length, guids.Distinct().Count());
    }

    [Fact]
    public void Authored_attacks_land_their_hits_on_their_contact_frames()
    {
        var iron = LockedBattleRoster.Ironhowl;
        var profile = LockedBattleHits.Ironhowl;
        var expected = new[]
        {
            ("ground pound", "mixamorig:RightHand", BattleImpactKind.StompQuake),
            ("backhand sweep", "mixamorig:LeftHand", BattleImpactKind.IronClaw),
            ("shoulder charge", "mixamorig:LeftArm", BattleImpactKind.IronClaw)
        };

        for (var i = 0; i < Clips.Length; i++)
        {
            var index = 3 + i;
            var attack = LockedBattleRotation.AttackAt(iron, index);
            Assert.Equal(Clips[i].Pose, attack.PoseName);
            Assert.Equal(expected[i].Item1, attack.Label);
            Assert.Equal(Clips[i].Last / IronhowlMotion.FrameRate, attack.Seconds, 4);
            Assert.Equal(Clips[i].Contact, attack.MeleeContactFrame);
            Assert.Equal(expected[i].Item2, LockedBattleRotation.HitStrikeBone(iron, profile, index, 0));
            Assert.Equal(expected[i].Item3, LockedBattleRotation.Impact(iron, profile, index));
            Assert.Equal(1, LockedBattleRotation.HitCount(iron, profile, index));

            // Feet are pinned in the clip: the hips' own travel plays as authored.
            Assert.False(attack.InPlace);
        }

        Assert.True(LockedBattleRotation.ShakeMetres(iron, profile, 3) >= LockedBattleReactions.HeavyShakeMetres);
    }

    private static string Md5(string path)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(md5.ComputeHash(stream)).ToLowerInvariant();
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets", "Survival")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("repo root");
    }
}
