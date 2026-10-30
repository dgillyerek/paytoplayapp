using System.Text;
using Survival.Domain.Runner;
using Xunit.Abstractions;

namespace Survival.Domain.Tests;

/// <summary>
/// How much the in-run boosts swing a level (Derek 2026-10-28: "the levels seem to be too heavily relied upon the initial
/// upgrades or extra characters"): the same level with only its first N boost crates / hero chests kept.
/// A probe, run on demand: RUNNER_SWING=1 dotnet test --filter RunnerBoostSwingProbe (writes runner_swing.txt in the temp dir).
/// </summary>
public sealed class RunnerBoostSwingProbe(ITestOutputHelper output)
{
    public static readonly int[] Levels = { 10, 15, 25, 31 };
    public static readonly int[] Keep = { 0, 2, 99 };

    public static bool IsBoost(RunnerRowDef r) => r.Kind == RunnerRowKind.Box && r.Box != null && r.Box.Kind != RunnerBoxKind.Heal;

    public static RunnerLevelDef KeepFirst(RunnerLevelDef level, int n)
    {
        var seen = 0;
        var rows = level.Rows.Where(r => !IsBoost(r) || seen++ < n).ToList();
        return level with { Rows = rows };
    }

    [Fact]
    public void Report()
    {
        if (Environment.GetEnvironmentVariable("RUNNER_SWING") != "1")
        {
            return;
        }

        var c = Runner.Catalog();
        var lines = new string[Levels.Length];
        Parallel.For(0, Levels.Length, i =>
        {
            var level = c.ByNumber(Levels[i]);
            var reqAll = BalanceReport.FindRequiredScale(c, level);
            var lo = RunnerBalance.Uniform(c, reqAll * 1.15);
            var sb = new StringBuilder($"L{Levels[i],2} boosts {level.Rows.Count(IsBoost),2}");
            foreach (var n in Keep)
            {
                var v = KeepFirst(level, n);
                var req = BalanceReport.FindRequiredScale(c, v);
                var run = BalanceReport.Sim(c, v, lo);
                sb.Append($" | keep{(n == 99 ? "All" : n.ToString())}: needPow {req * req / (reqAll * reqAll):0.00}x {(run.Outcome == RunnerOutcome.Won ? "win" : "LOSE")} t{run.Seconds:0} hpLost{1 - run.Hero.HpFraction:0.00} dmg{run.DamageMul:0.00} rate{run.RateMul:0.00} fol{run.FollowersPeak}");
            }

            lines[i] = sb.ToString();
        });
        var text = string.Join("\n", lines);
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "runner_swing.txt"), text);
        output.WriteLine(text);
    }
}
