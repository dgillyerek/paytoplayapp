using System.Globalization;
using System.Text;
using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>
/// How evenly a level's enemies reach the squad (Derek 2026-11-20: "too much downtime ... big clumps that die too easily,
/// then a long lull"). Layout numbers in metres come straight from the generated rows; time numbers come from the real
/// <see cref="RunnerRun"/> sim with the free player's squad on schedule (its golden power ratio, at least 1.2x what the
/// level needs, so it wins).
/// </summary>
public static class RunnerSpawnSpacing
{
    /// <summary>The road is "empty" while no live enemy is within this many metres ahead of the hero (about the squad's reach).</summary>
    public const float FightAhead = 25f;

    /// <summary>The clump in metres: the most enemies whose rows stand inside one window this long.</summary>
    public const float ClumpMetres = 4f;

    /// <summary>The clump in seconds: the most kills inside one window this long.</summary>
    public const float ClumpSeconds = 2f;

    public sealed record Stats(int Level, int Enemies, double AvgGapM, double MaxGapM, int MaxClumpM, double ArrAvgS, double ArrMaxS, int ArrClumpS, double LongestEmptyS, double EmptyShare, int KillBurst, double Seconds, bool Won);

    /// <summary>
    /// Where each enemy reaches the fight, in road metres the squad has walked: its row, less the ground it covers itself
    /// while the squad closes the last <see cref="RunnerSpreadDef.FightLine"/> m (a wolf meets the squad well before an
    /// archer that stands level with it). Dividing by the level's speed gives seconds.
    /// </summary>
    public static IEnumerable<double> Arrivals(RunnerCatalog c, RunnerLevelDef level) =>
        level.Rows.Where(r => r.Kind == RunnerRowKind.Enemy)
            .SelectMany(r => Enumerable.Repeat(r.At - (RunnerSpreadDef.FightLine * level.Speed / (level.Speed + c.Units[r.Unit].Speed)), Math.Max(1, r.Count)))
            .Select(z => (double)z).OrderBy(z => z);

    private static (double Avg, double Max, int Clump) Gaps(IReadOnlyList<double> xs, double clump)
    {
        if (xs.Count < 2)
        {
            return (0, 0, xs.Count);
        }

        // Gaps between distinct arrivals (same-spot enemies are one clump, not a zero gap); the clump is the most
        // arrivals inside any one window of width clump.
        var gaps = new List<double>();
        var best = 1;
        var lo = 0;
        for (var i = 0; i < xs.Count; i++)
        {
            while (xs[i] - xs[lo] > clump)
            {
                lo++;
            }

            best = Math.Max(best, i - lo + 1);
            if (i > 0 && xs[i] - xs[i - 1] > 1e-3)
            {
                gaps.Add(xs[i] - xs[i - 1]);
            }
        }

        return (gaps.Count == 0 ? 0 : gaps.Average(), gaps.Count == 0 ? 0 : gaps.Max(), best);
    }

    public static Stats Measure(RunnerCatalog c, int number, double scale)
    {
        var level = c.ByNumber(number);
        var ats = Arrivals(c, level).ToList();
        var lay = Gaps(ats, ClumpMetres);
        var laySec = Gaps(ats.Select(z => z / level.Speed).ToList(), ClumpSeconds);

        var run = new RunnerRun(c, level, RunnerBalance.Uniform(c, scale));
        // Kill times (bursts), and the stretches with no live enemy within FightAhead m of the hero (the empty road).
        var times = new List<double>();
        double empty = 0, longest = 0, emptySum = 0, first = -1, last = 0;
        const float dt = 1f / 30f;
        while (!run.Over && run.Seconds < 600f && !run.BossFight)
        {
            run.SteerTo(RunnerBossQuakeTests.DodgingPlayer(run, 0.3f));
            foreach (var e in run.Step(dt))
            {
                if (e.Kind == RunnerEventKind.FoeKilled || e.Kind == RunnerEventKind.FoePassed)
                {
                    times.Add(run.Seconds);
                }
            }

            var near = run.Foes.Any(f => !f.Boss && f.Hp > 0 && f.Z - run.Distance <= FightAhead && f.Z - run.Distance >= -3f);
            if (near)
            {
                if (first < 0)
                {
                    first = run.Seconds;
                }

                // Only a stretch that ends with another enemy counts: the walk from the last enemy to the finish gate or
                // the boss arena is the level's run-out, not a lull between waves.
                emptySum += empty;
                longest = Math.Max(longest, empty);
                empty = 0;
                last = run.Seconds;
            }
            else if (first >= 0)
            {
                empty += dt;
            }
        }

        times.Sort();
        var tg = Gaps(times, ClumpSeconds);
        var span = Math.Max(1e-3, last - Math.Max(0, first));
        return new Stats(number, ats.Count, lay.Avg, lay.Max, lay.Clump, laySec.Avg, laySec.Max, laySec.Clump, longest, emptySum / span, tg.Clump, run.Seconds, run.Outcome == RunnerOutcome.Won || run.BossFight);
    }

    /// <summary>The free player's stat scale on Level <paramref name="number"/> when it gets there on schedule (power = scale²), at least 1.2x what the level needs so the run is won.</summary>
    public static double FreeScale(BalanceReport r, int number) =>
        Math.Sqrt(Math.Max(r.Row("free", number).Power, 1.2 * r.RequiredPower[number]));

    public static string Table(BalanceReport r, IEnumerable<int> levels)
    {
        var sb = new StringBuilder("arrivals (layout: when each enemy reaches the squad, metres / seconds of walking) | sim (free squad on schedule: empty road = no live enemy within 25 m, between the first and the last enemy)\nlevel enemies | avg_m max_m most_in_4m | avg_s max_s most_in_2s | longest_empty_s empty_share most_kills_in_2s run_s\n");
        foreach (var n in levels)
        {
            var s = Measure(r.Runner, n, FreeScale(r, n));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "L{0,-3} {1,4} | {2,5:0.0} {3,5:0.0} {4,3} | {5,5:0.00} {6,5:0.0} {7,3} | {8,5:0.0} {9,5:0.00} {10,3} {11,5:0} {12}",
                s.Level, s.Enemies, s.AvgGapM, s.MaxGapM, s.MaxClumpM, s.ArrAvgS, s.ArrMaxS, s.ArrClumpS, s.LongestEmptyS, s.EmptyShare, s.KillBurst, s.Seconds, s.Won ? "" : "(lost)"));
        }

        return sb.ToString();
    }
}

public sealed class RunnerSpawnSpacingProbe
{
    private readonly Xunit.Abstractions.ITestOutputHelper _out;

    public RunnerSpawnSpacingProbe(Xunit.Abstractions.ITestOutputHelper o) => _out = o;

    [Fact]
    public void Writes_the_spawn_spacing_table()
    {
        // RUNNER_LEVELS_JSON: measure another runner_levels.json (e.g. main's, for a before / after) with today's code.
        var other = Environment.GetEnvironmentVariable("RUNNER_LEVELS_JSON");
        var r = string.IsNullOrEmpty(other) ? BalanceReport.Get() : BalanceReport.Build(RunnerCatalog.Parse(File.ReadAllText(other)));
        var t = RunnerSpawnSpacing.Table(r, new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 15, 20, 25, 31, 40, 50 });
        var sb = new StringBuilder(t).AppendLine("level required_power free_ratio free_cleared_day");
        for (var n = 1; n <= r.Levels; n++)
        {
            var row = r.Row("free", n);
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "L{0} {1:0.####} {2:0.###} {3}", n, r.RequiredPower[n], row.Ratio, row.ClearedDay?.ToString("0.##", CultureInfo.InvariantCulture) ?? "never"));
        }

        sb.AppendLine("free wall L" + r.Wall("free"));
        _out.WriteLine(sb.ToString());
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "runner_spawn_spacing.txt"), sb.ToString());
    }
}

public sealed class RunnerSpawnSpacingTests
{
    /// <summary>Longest wait, in seconds of walking, between one enemy reaching the squad and the next on Levels 13+ (main: 5.7-9.1 s).</summary>
    public const double MaxGapSeconds = 7.5;

    /// <summary>Most enemies reaching the squad inside any 2 s on Levels 13+ (main: 16-27; the no-boss finale's push and archer line is the 22).</summary>
    public const int MaxClump = 22;

    /// <summary>Levels 1-12 (the milder early spread, progression spread "early"): longest wait between enemies (main: 6.5-9.1 s).</summary>
    public const double EarlyMaxGapSeconds = 99.0;

    /// <summary>Levels 1-12: most enemies inside any 2 s (main: 7-18).</summary>
    public const int EarlyMaxClump = 999;

    private static (double MaxGap, int Clump) Spacing(RunnerCatalog c, int n)
    {
        var level = c.ByNumber(n);
        var s = RunnerSpawnSpacing.Arrivals(c, level).Select(z => z / level.Speed).ToList();
        var maxGap = 0.0;
        var clump = 1;
        var lo = 0;
        for (var i = 1; i < s.Count; i++)
        {
            maxGap = Math.Max(maxGap, s[i] - s[i - 1]);
            while (s[i] - s[lo] > RunnerSpawnSpacing.ClumpSeconds)
            {
                lo++;
            }

            clump = Math.Max(clump, i - lo + 1);
        }

        return (maxGap, clump);
    }

    [Fact]
    public void Generated_levels_have_no_long_lulls_or_big_clumps()
    {
        var c = Runner.Catalog();
        var spread = c.Progression!.Spread!;
        Assert.Equal(1, spread.From);
        Assert.Equal(12, spread.EarlyUntil);
        for (var n = 1; n <= c.Levels.Count; n++)
        {
            var (maxGap, clump) = Spacing(c, n);
            var early = n <= spread.EarlyUntil;
            Assert.True(maxGap <= (early ? EarlyMaxGapSeconds : MaxGapSeconds), $"L{n}: {maxGap:0.0} s between enemies");
            Assert.True(clump <= (early ? EarlyMaxClump : MaxClump), $"L{n}: {clump} enemies inside 2 s");
        }
    }
}
