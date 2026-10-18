using Survival.Domain.Heroes;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

/// <summary>
/// Sir Aldric attacks 2 and 3 (2026-10-18): the repo diagonal slash and the Mixamo "Sword And Shield Attack" shield bash
/// baked onto the lite sword and shield avatar (bash charge held in place, short slide only). HOLD merge until Derek Game-view PASS.
/// </summary>
public sealed class SirAldricExtraAttackTests
{
    private const string Design = "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/mixamo_attacks";

    [Fact]
    public void Slash_and_bash_are_lite_avatar_bakes_outside_the_17_clip_pack()
    {
        var root = ArcherExtraAttackTests.FindRepoRoot();
        var sums = File.ReadAllText(Path.Combine(root, Design, "CHECKSUMS.md5"));
        Assert.Equal(17, SirAldric3DMotion.LiteSwordShieldClips.Length);
        Assert.Equal(2, SirAldric3DMotion.BattleExtraClips.Length);
        foreach (var (name, last) in new[]
        {
            (SirAldricBattlePoses.Attack2Clip, SirAldricBattlePoses.Attack2LastFrame),
            (SirAldricBattlePoses.Attack3Clip, SirAldricBattlePoses.Attack3LastFrame)
        })
        {
            Assert.True(SirAldric3DMotion.TryLiteSwordShield(name, out var clip));
            Assert.False(clip.Loop);
            Assert.DoesNotContain(SirAldric3DMotion.LiteSwordShieldClips, c => c.ExactName == name);
            Assert.Equal(name, SirAldric3DMotion.LiteSwordShieldExactNameForFile(clip.FileName));
            var pack = Path.Combine(root, "Assets", clip.ThemePackRel);
            Assert.Equal(clip.Md5, ArcherExtraAttackTests.Md5(pack));
            Assert.Contains(clip.Md5 + "  ", sums, StringComparison.Ordinal);
            var meta = File.ReadAllText(pack + ".meta");
            Assert.Contains("name: " + name, meta, StringComparison.Ordinal);
            Assert.Contains("takeName: Scene", meta, StringComparison.Ordinal);
            Assert.Contains("lastFrame: " + last, meta, StringComparison.Ordinal);
            Assert.Contains("mirror: 0", meta, StringComparison.Ordinal);
            Assert.Contains("useFileScale: 0", meta, StringComparison.Ordinal);
            Assert.Contains(clip.Md5, meta, StringComparison.Ordinal);
            Assert.Equal(SirAldricBattlePoses.ClipFor(name == SirAldricBattlePoses.Attack2Clip ? SirAldricBattlePoses.Attack2 : SirAldricBattlePoses.Attack3), name);
        }

        Assert.Contains("8e72be0430c0f341f0b5e6db7bb3c8d9  SIRALDRIC_mixamo_sword_and_shield_attack_40f.fbx", sums, StringComparison.Ordinal);
        Assert.Equal(SirAldricBattlePoses.AttackClip, SirAldricBattlePoses.ClipFor(SirAldricBattlePoses.Attack));
        Assert.Equal(SirAldricBattlePoses.RestClip, SirAldricBattlePoses.ClipFor(SirAldricBattlePoses.Rest));
    }

    [Fact]
    public void Rotation_is_thrust_slash_bash_with_shield_shock_and_short_slides()
    {
        var a = LockedBattleRoster.SirAldric;
        var profile = LockedBattleHits.ProfileOf("Sir Aldric");
        Assert.Equal(3, LockedBattleRotation.Count(a));
        Assert.Equal(
            new[] { SirAldricBattlePoses.Attack, SirAldricBattlePoses.Attack2, SirAldricBattlePoses.Attack3 },
            new[] { 0, 1, 2 }.Select(i => LockedBattleRotation.AttackAt(a, i).PoseName).ToArray());
        Assert.Equal(33f / 30f, LockedBattleRotation.AttackAt(a, 0).Seconds, 4);
        Assert.Equal(24, LockedBattleRotation.ContactFrame(a, profile, 1));
        Assert.Equal(38, LockedBattleRotation.ContactFrame(a, profile, 2));
        Assert.Equal(BattleImpactKind.SwordSteel, LockedBattleRotation.Impact(a, profile, 1));
        Assert.Equal(BattleImpactKind.HeroShieldShock, LockedBattleRotation.Impact(a, profile, 2));
        Assert.Equal(SirAldricBattlePoses.LiteRightHand, LockedBattleRotation.StrikeBone(a, profile, 1));
        Assert.Equal(SirAldricBattlePoses.LiteLeftHand, LockedBattleRotation.StrikeBone(a, profile, 2));
        var bash = LockedBattleRotation.AttackAt(a, 2).ReachShift;
        Assert.InRange(bash.Metres, 0.2f, 0.5f);
        Assert.Equal(SirAldricBattlePoses.Attack3ContactFrame, bash.FullFrame);
        Assert.InRange(LockedBattleRotation.AttackAt(a, 1).ReachShift.Metres, 0.1f, 0.3f);
        Assert.True(SirAldricBattlePoses.IsAttackPose(SirAldricBattlePoses.Attack3));
        Assert.False(SirAldricBattlePoses.IsAttackPose(SirAldricBattlePoses.Walk));
    }
}
