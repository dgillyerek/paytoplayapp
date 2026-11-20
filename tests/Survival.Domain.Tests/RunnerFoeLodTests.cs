using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>Derek 2026-11-20: ~1.4x the enemies on the road. Far, walking enemies sample their loop at half rate (view only).</summary>
public sealed class RunnerFoeLodTests
{
    [Fact]
    public void Only_far_walking_non_boss_enemies_sample_every_other_frame()
    {
        Assert.Equal(2, RunnerFoeLod.EvaluateEvery(RunnerFoeLod.FarAhead + 0.1f, attacking: false, boss: false));
        Assert.Equal(1, RunnerFoeLod.EvaluateEvery(RunnerFoeLod.FarAhead, attacking: false, boss: false));
        Assert.Equal(1, RunnerFoeLod.EvaluateEvery(5f, attacking: false, boss: false));
        Assert.Equal(1, RunnerFoeLod.EvaluateEvery(-2f, attacking: false, boss: false));
        Assert.Equal(1, RunnerFoeLod.EvaluateEvery(40f, attacking: true, boss: false));
        Assert.Equal(1, RunnerFoeLod.EvaluateEvery(40f, attacking: false, boss: true));
    }
}
