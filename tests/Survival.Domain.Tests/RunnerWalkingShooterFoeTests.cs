using Survival.Domain.Runner;
using Xunit;

namespace Survival.Domain.Tests;

public class RunnerWalkingShooterFoeTests
{
    [Fact]
    public void Vespera_and_Bonequill_walk_while_they_shoot()
    {
        Assert.True(RunnerWalkingShooterFoes.Applies("Vespera"));
        Assert.True(RunnerWalkingShooterFoes.Applies("Bonequill"));
        Assert.False(RunnerWalkingShooterFoes.Applies("Lyra"));
        Assert.Equal(RunnerFoeBody.AttackWalking, RunnerWalkingShooterFoes.BodyFor(walking: true, attacking: true, inTake: false, layered: true));
        Assert.Equal(RunnerFoeBody.AttackStanding, RunnerWalkingShooterFoes.BodyFor(walking: false, attacking: true, inTake: true, layered: true));
    }

    [Fact]
    public void A_moving_foe_between_swings_walks_instead_of_sliding()
    {
        Assert.Equal(RunnerFoeBody.Walk, RunnerWalkingShooterFoes.BodyFor(walking: true, attacking: true, inTake: false, layered: false));
        Assert.Equal(RunnerFoeBody.Attack, RunnerWalkingShooterFoes.BodyFor(walking: true, attacking: true, inTake: true, layered: false));
        Assert.Equal(RunnerFoeBody.Attack, RunnerWalkingShooterFoes.BodyFor(walking: false, attacking: true, inTake: false, layered: false));
        Assert.Equal(RunnerFoeBody.Rest, RunnerWalkingShooterFoes.BodyFor(walking: false, attacking: false, inTake: false, layered: false));
    }
}
