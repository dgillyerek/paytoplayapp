using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>
/// What the "every shipped level" play tests run with. Levels 1-12 are the original campaign curve and are played with
/// an untrained squad, as before. Levels 13-50 ramp past what an untrained squad can survive (that is the point:
/// RunnerBalanceTests), so the play tests give them a trained squad with a margin over what the balance model says
/// the level needs. The mechanics checks (shots fire, bosses slam, nobody gets pushed back) then cover all 50.
/// </summary>
internal static class RunnerShipped
{
    /// <summary>The levels whose difficulty is unchanged from the 12-level campaign (the ramp starts after these).</summary>
    public const int StartingLevels = 12;

    /// <summary>Trained-squad margin over the level's required scale (damage and HP each).</summary>
    public const double Margin = 1.3;

    public static RunnerLoadout Squad(RunnerCatalog c, RunnerLevelDef level) =>
        level.Number <= StartingLevels
            ? RunnerLoadout.None
            : RunnerBalance.Uniform(c, Margin * BalanceReport.Get().RequiredScale[level.Number]);
}
