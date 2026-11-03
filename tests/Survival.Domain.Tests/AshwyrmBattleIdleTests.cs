using Survival.Domain.Enemies;
using Survival.Domain.Roster;
using Xunit;

public class AshwyrmBattleIdleTests
{
    [Fact]
    public void Stationary_ashwyrm_never_selects_the_walk_clip()
    {
        var ashwyrm = LockedBattleRoster.Ashwyrm;
        Assert.Equal(BlenderRigSpec.HoverIdlePoseName, ashwyrm.IdlePoseName);
        Assert.NotEqual(ashwyrm.WalkPoseName, ashwyrm.IdlePoseName);
        Assert.Equal(AshwyrmMotion.FlapPoseName, AshwyrmMotion.Spec.ClipPoseName);
        foreach (var attack in ashwyrm.Attacks)
        {
            Assert.NotEqual(AshwyrmMotion.WalkPoseName, attack.PoseName);
        }
    }

    [Fact]
    public void Ashwyrm_alternates_breath_and_gust()
    {
        var attacks = LockedBattleRoster.Ashwyrm.Attacks;
        Assert.Equal(2, attacks.Count);
        Assert.Equal(AshwyrmMotion.AttackPoseName, attacks[0].PoseName);
        Assert.Equal(AshwyrmMotion.FlapPoseName, attacks[1].PoseName);
    }

    [Fact]
    public void Other_fighters_still_idle_in_rest()
    {
        Assert.Equal(BlenderRigSpec.RestPoseName, LockedBattleRoster.Emberfang.IdlePoseName);
        Assert.Equal(BlenderRigSpec.RestPoseName, LockedBattleRoster.Vespera.IdlePoseName);
    }
}
