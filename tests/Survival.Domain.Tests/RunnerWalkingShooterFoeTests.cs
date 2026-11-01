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

    [Fact]
    public void Vespera_without_a_hips_bone_keeps_her_legs_and_rig_on_the_walk_and_casts_with_the_spine()
    {
        // Her edited rig has no Hips: Spine1 and both legs are roots under VESPERA_rig, which stands in for the hips.
        const string hips = "VESPERA_rig";
        const string spine = "mixamorig:Spine1";
        Assert.False(RunnerUpperBodyAttack.IsUpperBody("VESPERA_rig", hips, spine));
        Assert.False(RunnerUpperBodyAttack.IsUpperBody("VESPERA_rig/mixamorig:LeftUpLeg", hips, spine));
        Assert.False(RunnerUpperBodyAttack.IsUpperBody("VESPERA_rig/mixamorig:RightUpLeg/mixamorig:RightLeg/mixamorig:RightFoot", hips, spine));
        Assert.True(RunnerUpperBodyAttack.IsUpperBody("VESPERA_rig/mixamorig:Spine1", hips, spine));
        Assert.True(RunnerUpperBodyAttack.IsUpperBody("VESPERA_rig/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightArm/mixamorig:RightForeArm/mixamorig:RightHand", hips, spine));
    }

    [Fact]
    public void The_upper_body_mask_falls_back_to_the_legs_parent_when_a_rig_has_no_hips()
    {
        var src = System.IO.File.ReadAllText(System.IO.Path.Combine(Layouts.RepoRoot(), "Assets", "Survival", "Unity", "BlenderRigPlayer.cs"));
        Assert.Contains("t.name.EndsWith(\"LeftUpLeg\", System.StringComparison.Ordinal) && t.parent != null && t.parent != root", src);
        Assert.Contains("_spine ??= spineLike;", src);
    }
}

