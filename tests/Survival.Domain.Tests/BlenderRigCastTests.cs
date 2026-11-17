using Survival.Domain.Enemies;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;
using Survival.Domain.Runner;

/// <summary>The shared Blender-rig actor's cast table: one row per plain rig fighter, read by the battle and the runner.</summary>
public sealed class BlenderRigCastTests
{
    /// <summary>Asserts <paramref name="name"/> spawns through the shared actor with exactly this motion and attack.</summary>
    internal static BlenderRigCast AssertCast(string name, BlenderRigSpec motion, BlenderRigAttackSpec? attack = null)
    {
        Assert.True(BlenderRigCast.TryFind(name, out var cast), name + " has no BlenderRigCast row");
        Assert.Equal(motion.Name, cast.Motion.Name);
        if (attack != null)
        {
            Assert.Same(attack, cast.Attack);
        }

        return cast;
    }

    [Fact]
    public void Every_plain_rig_fighter_has_one_row_with_its_own_specs()
    {
        AssertCast("Lyra", LyraMotion.Spec, LyraAttack.Spec);
        AssertCast("Oakenshield", OakenshieldMotion.Spec, OakenshieldAttack.Spec);
        AssertCast("Stormcrest", StormcrestMotion.Spec, StormcrestAttack.Spec);
        AssertCast("Vespera", VesperaMotion.Spec, VesperaAttack.Spec);
        AssertCast("Nightfang", NightfangMotion.Spec, NightfangAttack.Spec);
        AssertCast("Ashwyrm", AshwyrmMotion.Spec, AshwyrmAttack.Spec);
        Assert.Equal(6, BlenderRigCast.All.Count);
        Assert.Equal(BlenderRigCast.All.Count, BlenderRigCast.All.Select(c => c.Name).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Every_row_is_a_locked_battle_fighter()
    {
        foreach (var cast in BlenderRigCast.All)
        {
            Assert.True(LockedBattleRoster.TryFind(cast.Name, out _), cast.Name + " is not on the locked battle roster");
        }
    }

    [Fact]
    public void Fighters_with_their_own_actor_are_not_in_the_table()
    {
        foreach (var own in new[] { "Sir Aldric", "Blightroot", "Emberfang", "Ironhowl", "Vorgath", "Rowan", "Bonequill" })
        {
            Assert.False(BlenderRigCast.TryFind(own, out _), own);
        }
    }

    [Fact]
    public void Only_lyra_carries_idle_props_and_a_spawn_yaw()
    {
        var lyra = AssertCast("Lyra", LyraMotion.Spec);
        Assert.Same(LyraMotion.IdleProps, lyra.IdleProps);
        Assert.Equal(LyraMotion.FacingYawDegrees, lyra.FacingYawDegrees);
        foreach (var cast in BlenderRigCast.All.Where(c => c.Name != "Lyra"))
        {
            Assert.Null(cast.IdleProps);
            Assert.Equal(0f, cast.FacingYawDegrees);
        }
    }

    [Fact]
    public void Runner_paces_every_attack_but_stormcrest()
    {
        foreach (var cast in BlenderRigCast.All)
        {
            Assert.Equal(cast.Name != "Stormcrest", cast.RunnerPacedAttack);
        }
    }

    [Fact]
    public void Runner_upper_body_shooters_in_the_table_are_the_squad_and_walking_foes()
    {
        var upper = BlenderRigCast.All
            .Where(c => RunnerUpperBodyAttack.Applies(c.Name) || RunnerWalkingShooterFoes.Applies(c.Name))
            .Select(c => c.Name)
            .OrderBy(n => n, StringComparer.Ordinal);
        Assert.Equal(new[] { "Lyra", "Oakenshield", "Vespera" }, upper);
    }

    [Fact]
    public void Battle_and_runner_spawn_the_table_through_the_shared_actor()
    {
        var unity = Path.Combine(TestRepo.Root, "Assets", "Survival", "Unity");
        foreach (var file in new[] { "LockedBattleDemo.cs", Path.Combine("Runner", "RunnerRig.cs") })
        {
            var src = File.ReadAllText(Path.Combine(unity, file));
            Assert.Contains("BlenderRigCast.TryFind(data.Name, out var cast)", src, StringComparison.Ordinal);
            Assert.Contains("AddComponent<BlenderRigActor>()", src, StringComparison.Ordinal);
            foreach (var cast in BlenderRigCast.All)
            {
                Assert.DoesNotContain("case \"" + cast.Name + "\":", src, StringComparison.Ordinal);
            }
        }
    }
}
