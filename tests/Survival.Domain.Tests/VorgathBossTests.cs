using Survival.Domain.Campaign;
using Survival.Domain.Enemies;
using Survival.Domain.Roster;
using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>
/// Vorgath is the milestone end boss. The runner already ends every 5th level on a boss (Ironhowl); every 10th of
/// those is Vorgath. The campaign already marks every 5th stage as a boss; the chapter finale (stage 10) is Vorgath.
/// His runner HP, damage, cycle and throw clock match Ironhowl so the free-pace sim does not move.
/// </summary>
public sealed class VorgathBossTests
{
    [Fact]
    public void Runner_milestone_levels_end_on_vorgath_and_the_other_boss_levels_stay_ironhowl()
    {
        var c = Runner.Catalog();
        var p = c.Progression!;
        Assert.Equal(5, p.BossEvery);
        Assert.Equal(10, RunnerProgressionDef.MilestoneEvery);
        for (var n = 1; n <= c.Levels.Count; n++)
        {
            var level = c.ByNumber(n);
            var milestone = n % 10 == 0;
            Assert.Equal(n % 5 == 0, level.HasBoss);
            Assert.Equal(milestone, p.IsMilestoneBossLevel(n));
            if (!level.HasBoss)
            {
                Assert.DoesNotContain(level.Rows, r => r.Kind == RunnerRowKind.Boss);
                continue;
            }

            var boss = level.Rows.Single(r => r.Kind == RunnerRowKind.Boss);
            Assert.Equal(milestone ? "vorgath" : "ironhowl", boss.Unit);
            Assert.Equal(level.Rows.Max(r => r.At), boss.At);
        }
    }

    [Fact]
    public void Vorgath_runner_stats_match_ironhowl_so_the_pace_sim_does_not_move()
    {
        var c = Runner.Catalog();
        var iron = c.Unit("ironhowl");
        var vorgath = c.Unit("vorgath");
        Assert.Equal(iron.Hp, vorgath.Hp);
        Assert.Equal(iron.Damage, vorgath.Damage);
        Assert.Equal(iron.Cycle, vorgath.Cycle);
        Assert.Equal(iron.Range, vorgath.Range);
        Assert.Equal(iron.Radius, vorgath.Radius);
        Assert.Equal(iron.Scale, vorgath.Scale);
        Assert.Equal(iron.Throws, vorgath.Throws);
        Assert.False(vorgath.Ranged);
        Assert.Equal(0f, vorgath.Speed);

        var moves = RunnerRun.BossMoves("Vorgath");
        var ironMoves = RunnerRun.BossMoves("Ironhowl");
        Assert.NotNull(moves);
        Assert.Equal(LockedBattleRoster.Ironhowl.Name, moves!.Value.Name);
        Assert.Equal(LockedBattleRotation.Count(ironMoves!.Value), LockedBattleRotation.Count(moves.Value));
        Assert.True(LockedBattleRoster.VorgathBattleScale > 1f);
    }

    [Fact]
    public void Vorgath_battle_attacks_are_his_slam_and_spit()
    {
        var f = LockedBattleRoster.Vorgath;
        Assert.DoesNotContain(LockedBattleRoster.All, x => x.Name == f.Name);
        Assert.True(LockedBattleRoster.TryFind(f.Name, out _));
        Assert.Equal(2, LockedBattleRotation.Count(f));
        var slam = LockedBattleRotation.AttackAt(f, 0);
        var spit = LockedBattleRotation.AttackAt(f, 1);
        Assert.Equal(VorgathMotion.SlamPoseName, slam.PoseName);
        Assert.Equal(VorgathMotion.SlamContactFrame, slam.MeleeContactFrame);
        Assert.Equal(VorgathMotion.SlamSeconds, slam.Seconds, 4);
        Assert.Equal(BattleImpactKind.GroundSlam, slam.Impact);
        Assert.Equal(VorgathMotion.SpitPoseName, spit.PoseName);
        Assert.Equal(VorgathMotion.SpitPeakFrame, spit.MeleeContactFrame);
        Assert.Equal(BattleImpactKind.VoidFireBreath, spit.Impact);
        Assert.InRange(VorgathMotion.SpitFireStartSeconds, 1.7f, VorgathMotion.SpitPeakSeconds);
        Assert.True(VorgathMotion.SpitFireStopSeconds > VorgathMotion.SpitFireStartSeconds);
        var profile = LockedBattleHits.ProfileOf(f.Name);
        Assert.Equal(VorgathMotion.SlamContactFrame, profile.MeleeContactFrame);
        Assert.Equal("Bone.004", LockedBattleReactions.RigFor(f).Head[1]);
    }

    [Fact]
    public void Campaign_chapter_finales_are_vorgath_and_elites_keep_the_chapter_boss()
    {
        var c = Camp.Catalog();
        var def = c.BattleCampaign!;
        Assert.Equal(5, def.BossEvery);
        Assert.Equal(10, def.StagesPerChapter);
        for (var ch = 1; ch <= def.Chapters.Count; ch++)
        {
            var stages = c.StagesIn(ch);
            var chapterBoss = def.Chapters[ch - 1].Boss;
            for (var i = 0; i < stages.Count; i++)
            {
                var n = ((ch - 1) * def.StagesPerChapter) + i + 1;
                var stage = stages[i];
                var finale = n % def.StagesPerChapter == 0;
                Assert.Equal(finale, def.IsFinaleBoss(n));
                if (finale)
                {
                    Assert.True(stage.Boss);
                    Assert.Contains(stage.Enemies, e => e.HeroId == BattleCampaignDef.VorgathId);
                    Assert.Contains(stage.Enemies, e => e.HeroId == chapterBoss);
                    var vorgath = stage.Enemies.Single(e => e.HeroId == BattleCampaignDef.VorgathId);
                    Assert.True(vorgath.Level >= stage.Enemies.Where(e => e.HeroId != BattleCampaignDef.VorgathId).Max(e => e.Level));
                }
                else if (stage.Boss)
                {
                    Assert.Contains(stage.Enemies, e => e.HeroId == chapterBoss);
                    Assert.DoesNotContain(stage.Enemies, e => e.HeroId == BattleCampaignDef.VorgathId);
                }
                else
                {
                    Assert.DoesNotContain(stage.Enemies, e => e.HeroId == BattleCampaignDef.VorgathId);
                }
            }
        }
    }
}
