using System.Globalization;
using System.Text;
using Survival.Domain.Runner;
using Survival.Domain.Serialization;
using Xunit.Abstractions;

namespace Survival.Domain.Tests;

/// <summary>
/// The 50-level runner balance, as tests. BalanceReport (RunnerBalanceModel.cs) sims every level to find the power it
/// needs, plays each spend profile (free / light / mid / whale / stuck_39, runner_balance.json: what each buys from the
/// power-pack ladder) through the campaign economy, and sims
/// each level again with the squad that profile has when it gets there. These tests hold the design targets:
///   L1-30: a free player who keeps upgrading clears every level on schedule, with margin.
///   L31-50: the free player hits a wall at about L31 and from there falls further behind; payers get through.
/// And Golden/runner_balance.json pins the curve: any baseline change (unit stats, box counts, upgrade costs, rewards,
/// the ramp) that moves a level's power ratio more than 10%, or the free wall more than 2 levels, fails with a diff.
/// Regenerate after a deliberate change: UPDATE_GOLDEN=1 dotnet test tests/Survival.Domain.Tests --filter RunnerBalance
/// (docs/runner_balance.md).
/// </summary>
public sealed class RunnerBalanceTests
{
    private readonly ITestOutputHelper _out;

    public RunnerBalanceTests(ITestOutputHelper output)
    {
        _out = output;
    }

    private static BalanceReport R => BalanceReport.Get();

    private const int FreeEasyUntil = 30;

    private static double NominalDay(BalanceReport r, int level) => (level - 1) / (double)r.Def.SessionsPerDay;

    [Fact]
    public void Writes_the_balance_table_and_csv()
    {
        var r = R;
        var csv = Path.Combine(AppContext.BaseDirectory, BalanceReport.CsvName);
        File.WriteAllText(csv, r.Csv());
        _out.WriteLine("csv: " + csv);
        _out.WriteLine(r.Table());
        Assert.Equal(50, r.Levels);
        Assert.Equal(new[] { "free", "light", "mid", "whale", "stuck_39" }, r.Def.Profiles.Select(p => p.Id).ToArray());
        foreach (var p in r.Def.Profiles)
        {
            _out.WriteLine($"{p.Id,-9} ${p.UsdPerWeek(r.Src.Packs!):0.00}/week  wall L{r.Wall(p.Id)}  L50 day {r.Row(p.Id, 50).ClearedDay?.ToString("0.0", CultureInfo.InvariantCulture) ?? "never"}  spent ${r.Rows[p.Id][^1].UsdSpent:0.00}");
        }
        Assert.All(r.Rows.Values, rows => Assert.Equal(r.Levels, rows.Count));
    }

    [Fact]
    public void The_first_12_levels_still_need_no_upgrades()
    {
        // Untouched by the ramp: an untrained squad (scale 1) already wins each of them.
        for (var n = 1; n <= RunnerShipped.StartingLevels; n++)
        {
            Assert.True(R.RequiredScale[n] <= 1.0, $"L{n} needs scale {R.RequiredScale[n]:0.000}");
        }
    }

    [Fact]
    public void A_free_player_who_upgrades_clears_levels_1_to_30_on_schedule_with_margin()
    {
        var r = R;
        foreach (var row in r.Rows["free"].Take(FreeEasyUntil))
        {
            // Margin: at least 25% over the need through L25, 15% for the last few before the wall.
            var margin = row.Level <= 25 ? 1.25 : 1.15;
            Assert.True(row.Ratio >= margin, $"free L{row.Level} ratio {row.Ratio:0.00} < {margin}");
            Assert.True(row.SimWon, $"free L{row.Level} lost the sim with its at-pace squad");
            Assert.True(row.SimHeroHp >= 0.5, $"free L{row.Level} won with the hero at {row.SimHeroHp:P0}");
            Assert.NotNull(row.ClearedDay);
            Assert.True(row.ClearedDay <= NominalDay(r, row.Level) + 1e-6, $"free L{row.Level} cleared on day {row.ClearedDay:0.0}, schedule {NominalDay(r, row.Level):0.0}");
        }

        // It gets tighter towards L30: the last five are much closer than the first ten.
        var early = r.Rows["free"].Take(10).Average(x => x.Ratio);
        var late = r.Rows["free"].Skip(25).Take(5).Average(x => x.Ratio);
        Assert.True(late < 0.5 * early, $"ratio L1-10 avg {early:0.00}, L26-30 avg {late:0.00}");
    }

    [Fact]
    public void The_free_player_hits_the_wall_at_31_and_falls_further_behind_through_50()
    {
        var r = R;
        var wall = r.Wall("free");
        Assert.InRange(wall, 30, 33);
        var late = r.Rows["free"].Skip(FreeEasyUntil).ToList();
        Assert.All(late, row => Assert.True(row.Ratio < 1.0, $"free L{row.Level} ratio {row.Ratio:0.00} on schedule"));
        Assert.True(late.Count(x => !x.SimWon) >= 18, "free at-pace sims won: " + string.Join(",", late.Where(x => x.SimWon).Select(x => "L" + x.Level)));

        // A ramp, not a cliff: the gap opens level by level (L41-50 worse than L31-40), ending near half the need.
        var a = late.Take(10).Average(x => x.Ratio);
        var b = late.Skip(10).Average(x => x.Ratio);
        Assert.True(b < a, $"free ratio L31-40 {a:0.00}, L41-50 {b:0.00}");
        Assert.InRange(late[^1].Ratio, 0.3, 0.7);

        // "Nearly impossible": grinding for free, L40 takes months and L50 more than a year.
        Assert.True((r.Row("free", 40).ClearedDay ?? double.MaxValue) >= 60, $"free L40 day {r.Row("free", 40).ClearedDay}");
        Assert.True((r.Row("free", 50).ClearedDay ?? double.MaxValue) >= 365, $"free L50 day {r.Row("free", 50).ClearedDay}");
    }

    [Fact]
    public void Paying_moves_the_wall_and_gets_through_the_last_20_much_sooner()
    {
        var r = R;
        double Day(string p, int n) => r.Row(p, n).ClearedDay ?? double.PositiveInfinity;
        var ladder = new[] { "free", "light", "mid", "whale" };
        for (var n = 1; n <= r.Levels; n++)
        {
            for (var i = 1; i < ladder.Length; i++)
            {
                Assert.True(Day(ladder[i], n) <= Day(ladder[i - 1], n) + 1e-6, $"L{n}: {ladder[i]} day {Day(ladder[i], n)} after {ladder[i - 1]} {Day(ladder[i - 1], n)}");
            }
        }

        // What they spend a week from the ladder: light $4.99, mid $19.99, whale two $39.99 packs.
        var packs = r.Src.Packs!;
        Assert.Equal(4.99, r.Def.Profile("light").UsdPerWeek(packs), 2);
        Assert.Equal(19.99, r.Def.Profile("mid").UsdPerWeek(packs), 2);
        Assert.True(r.Def.Profile("whale").UsdPerWeek(packs) >= 2 * 39.99 - 1e-6);

        // Each step up the ladder moves the at-pace wall: light a few levels, mid further, the whale past L50.
        Assert.InRange(r.Wall("light"), r.Wall("free") + 2, r.Wall("free") + 5);
        Assert.InRange(r.Wall("mid"), r.Wall("light") + 2, 45);
        Assert.Equal(r.Levels + 1, r.Wall("whale"));

        // The whale clears L50 within a month (no later than about the free schedule plus two weeks), far sooner than free.
        Assert.True(Day("whale", 50) <= 30, $"whale L50 day {Day("whale", 50)}");
        Assert.True(Day("whale", 50) * 20 <= Day("free", 50), $"whale L50 day {Day("whale", 50)}, free {Day("free", 50)}");

        // Mid within about two months, light within about three, both much sooner than free.
        Assert.True(Day("mid", 50) <= 60, $"mid L50 day {Day("mid", 50)}");
        Assert.True(Day("light", 50) <= 120, $"light L50 day {Day("light", 50)}");
        Assert.True(Day("light", 50) * 4 <= Day("free", 50), $"light L50 day {Day("light", 50)}, free {Day("free", 50)}");
    }

    [Fact]
    public void Kill_time_against_overrun_time_tracks_the_pressure()
    {
        // TTK: seconds the squad needs to kill one wave; TTO: seconds that wave needs to kill the squad.
        // For the free player the pressure (TTK / TTO) climbs from easy (well under 1) to well past 1 after the wall.
        var rows = R.Rows["free"];
        Assert.All(rows, x => Assert.True(x.TimeToKill > 0 && x.TimeToOverrun > 0 && !double.IsInfinity(x.TimeToKill) && !double.IsInfinity(x.TimeToOverrun), $"L{x.Level}"));
        Assert.All(rows.Take(20), x => Assert.True(x.TimeToKill < x.TimeToOverrun, $"free L{x.Level} ttk {x.TimeToKill:0.0} tto {x.TimeToOverrun:0.0}"));
        Assert.All(rows.Skip(FreeEasyUntil), x => Assert.True(x.TimeToKill > 2 * x.TimeToOverrun, $"free L{x.Level} ttk {x.TimeToKill:0.0} tto {x.TimeToOverrun:0.0}"));
    }

    [Fact]
    public void No_level_needs_much_more_than_the_one_before()
    {
        var r = R;
        for (var n = 2; n <= r.Levels; n++)
        {
            var jump = r.RequiredPower[n] / r.RequiredPower[n - 1];
            Assert.True(jump <= 1.6, $"L{n} needs {jump:0.00}x L{n - 1}");
        }
    }

    [Fact]
    public void The_curve_matches_the_golden_snapshot()
    {
        var path = BalanceGolden.SourcePath();
        var now = BalanceGolden.Of(R);
        if (Environment.GetEnvironmentVariable("UPDATE_GOLDEN") == "1")
        {
            File.WriteAllText(path, BalanceGolden.Write(now));
            _out.WriteLine("wrote " + path);
            return;
        }

        Assert.True(File.Exists(path), $"no golden at {path}; run with UPDATE_GOLDEN=1 to create it");
        var was = BalanceGolden.Read(File.ReadAllText(path));
        var diff = BalanceGolden.Diff(was, now);
        _out.WriteLine(diff.Text);
        Assert.True(diff.Flagged.Count == 0, "The runner balance moved significantly:\n" + diff.Text + "\nIf that was deliberate, regenerate: UPDATE_GOLDEN=1 dotnet test tests/Survival.Domain.Tests --filter RunnerBalance");
    }

    [Fact]
    public void The_golden_diff_flags_a_10_percent_shift_and_a_3_level_wall_move_but_not_noise()
    {
        var g = BalanceGolden.Of(R);
        Assert.Empty(BalanceGolden.Diff(g, g).Flagged);

        var free = g.Profiles["free"];
        var nudged = g with { Profiles = new Dictionary<string, BalanceGolden.Curve>(g.Profiles) { ["free"] = free with { Ratio = free.Ratio.Select((x, i) => i == 9 ? x * 1.05 : x).ToArray() } } };
        Assert.Empty(BalanceGolden.Diff(g, nudged).Flagged);

        var moved = g with { Profiles = new Dictionary<string, BalanceGolden.Curve>(g.Profiles) { ["free"] = free with { Ratio = free.Ratio.Select((x, i) => i == 9 ? x * 1.15 : x).ToArray(), Wall = free.Wall + 3 } } };
        var d = BalanceGolden.Diff(g, moved);
        Assert.Contains(d.Flagged, l => l.Contains("free L10 ratio"));
        Assert.Contains(d.Flagged, l => l.Contains("free wall"));
    }
}

/// <summary>A deliberate baseline change shows up: Lyra (the lead) hitting 30% harder moves the curve past the golden tolerances.</summary>
public sealed class RunnerBalanceSensitivityTests
{
    private readonly ITestOutputHelper _out;

    public RunnerBalanceSensitivityTests(ITestOutputHelper output)
    {
        _out = output;
    }

    [Fact]
    public void A_30_percent_stronger_lead_hero_is_flagged_as_a_significant_change()
    {
        var json = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Path.Combine(TestPaths.CatalogDir, RunnerCatalog.FileName)))!;
        var lyra = json["units"]!["lyra"]!;
        lyra["damage"] = (int)Math.Round((double)lyra["damage"]! * 1.3);
        var stronger = BalanceReport.Build(RunnerCatalog.Parse(json.ToJsonString()));
        var d = BalanceGolden.Diff(BalanceGolden.Of(BalanceReport.Get()), BalanceGolden.Of(stronger));
        _out.WriteLine(d.Text);
        Assert.NotEmpty(d.Flagged);
    }

    [Fact]
    public void Halving_the_gold_that_goes_to_heroes_is_flagged_too()
    {
        var r = BalanceReport.Get();
        var poorer = BalanceReport.Build(r.Runner, r.Def with { HeroGoldShare = r.Def.HeroGoldShare / 2 }, r.RequiredScale);
        var d = BalanceGolden.Diff(BalanceGolden.Of(r), BalanceGolden.Of(poorer));
        _out.WriteLine(d.Text);
        Assert.NotEmpty(d.Flagged);
    }
}

/// <summary>Golden/runner_balance.json: the numbers the balance is pinned to, and the diff against them.</summary>
public static class BalanceGolden
{
    /// <summary>A level's power ratio (or required power) moving by more than this fraction is significant.</summary>
    public const double RatioTolerance = 0.10;

    /// <summary>A profile's wall (first level it cannot clear on schedule) moving by more than this many levels is significant.</summary>
    public const int WallTolerance = 2;

    /// <summary>The days a profile takes to first clear L50 moving by more than this fraction is significant.</summary>
    public const double DaysTolerance = 0.25;

    public sealed record Curve(int Wall, int Furthest, double[] Ratio, double?[] ClearedDay);

    public sealed record Snapshot(double[] RequiredPower, Dictionary<string, Curve> Profiles);

    public sealed record Result(List<string> Flagged, string Text);

    public static Snapshot Of(BalanceReport r) => new(
        r.RequiredPower.Skip(1).Select(x => Math.Round(x, 4)).ToArray(),
        r.Rows.ToDictionary(
            p => p.Key,
            p => new Curve(r.Wall(p.Key), r.Furthest(p.Key), p.Value.Select(x => Math.Round(x.Ratio, 4)).ToArray(), p.Value.Select(x => x.ClearedDay == null ? (double?)null : Math.Round(x.ClearedDay.Value, 2)).ToArray())));

    public static string SourcePath()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
        {
            var p = Path.Combine(d.FullName, "tests", "Survival.Domain.Tests", "Golden");
            if (Directory.Exists(p))
            {
                return Path.Combine(p, "runner_balance.json");
            }
        }

        throw new DirectoryNotFoundException("tests/Survival.Domain.Tests/Golden above " + AppContext.BaseDirectory);
    }

    private static string N(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);

    public static string Write(Snapshot s)
    {
        var sb = new StringBuilder();
        sb.Append("{\n  \"_doc\": \"Pinned runner balance (RunnerBalanceTests). Index i = level i+1. ratio = profile power / required power when it reaches the level on schedule; cleared_day = day it first clears (null = not within the modelled years). Regenerate after a deliberate change: UPDATE_GOLDEN=1 dotnet test tests/Survival.Domain.Tests --filter RunnerBalance\",\n");
        sb.Append($"  \"tolerance\": {{ \"ratio\": {N(RatioTolerance)}, \"wall_levels\": {WallTolerance}, \"l50_days\": {N(DaysTolerance)} }},\n");
        sb.Append("  \"required_power\": [").Append(string.Join(", ", s.RequiredPower.Select(N))).Append("],\n");
        sb.Append("  \"profiles\": {\n");
        var i = 0;
        foreach (var p in s.Profiles)
        {
            sb.Append($"    \"{p.Key}\": {{\n      \"wall\": {p.Value.Wall},\n      \"furthest\": {p.Value.Furthest},\n");
            sb.Append("      \"ratio\": [").Append(string.Join(", ", p.Value.Ratio.Select(N))).Append("],\n");
            sb.Append("      \"cleared_day\": [").Append(string.Join(", ", p.Value.ClearedDay.Select(x => x == null ? "null" : N(x.Value)))).Append("]\n");
            sb.Append(++i < s.Profiles.Count ? "    },\n" : "    }\n");
        }

        sb.Append("  }\n}\n");
        return sb.ToString();
    }

    public static Snapshot Read(string json)
    {
        var o = (JsonNode.Obj)JsonRead.Parse(json);
        double[] Nums(JsonNode n) => ((JsonNode.Arr)n).Items.Select(x => ((JsonNode.Num)x).Value).Select(x => (double)x).ToArray();
        var profiles = new Dictionary<string, Curve>();
        foreach (var p in ((JsonNode.Obj)o.Fields["profiles"]).Fields)
        {
            var po = (JsonNode.Obj)p.Value;
            profiles[p.Key] = new Curve(
                (int)((JsonNode.Num)po.Fields["wall"]).Value,
                (int)((JsonNode.Num)po.Fields["furthest"]).Value,
                Nums(po.Fields["ratio"]),
                ((JsonNode.Arr)po.Fields["cleared_day"]).Items.Select(x => x is JsonNode.Num v ? (double?)v.Value : null).ToArray());
        }

        return new Snapshot(Nums(o.Fields["required_power"]), profiles);
    }

    /// <summary>Everything that moved, biggest first; <see cref="Result.Flagged"/> holds the moves past the tolerances.</summary>
    public static Result Diff(Snapshot was, Snapshot now)
    {
        var flagged = new List<string>();
        var minor = new List<(double Size, string Line)>();
        var major = new List<(double Size, string Line)>();

        void Rel(string what, double a, double b, double tol)
        {
            if (a == b)
            {
                return;
            }

            var rel = a == 0 ? double.PositiveInfinity : (b - a) / a;
            var line = $"{what} {N(a)} -> {N(b)} ({rel:+0.0%;-0.0%})";
            (Math.Abs(rel) > tol ? major : minor).Add((Math.Abs(rel), line));
        }

        if (was.RequiredPower.Length != now.RequiredPower.Length)
        {
            major.Add((double.MaxValue, $"level count {was.RequiredPower.Length} -> {now.RequiredPower.Length}"));
        }

        for (var i = 0; i < Math.Min(was.RequiredPower.Length, now.RequiredPower.Length); i++)
        {
            Rel($"required power L{i + 1}", was.RequiredPower[i], now.RequiredPower[i], RatioTolerance);
        }

        foreach (var p in was.Profiles)
        {
            if (!now.Profiles.TryGetValue(p.Key, out var c))
            {
                major.Add((double.MaxValue, $"profile {p.Key} is gone"));
                continue;
            }

            var w = p.Value;
            if (w.Wall != c.Wall)
            {
                (Math.Abs(c.Wall - w.Wall) > WallTolerance ? major : minor).Add((1000 + Math.Abs(c.Wall - w.Wall), $"{p.Key} wall L{w.Wall} -> L{c.Wall}"));
            }

            if (w.Furthest != c.Furthest)
            {
                major.Add((1000, $"{p.Key} furthest clear L{w.Furthest} -> L{c.Furthest}"));
            }

            for (var i = 0; i < Math.Min(w.Ratio.Length, c.Ratio.Length); i++)
            {
                Rel($"{p.Key} L{i + 1} ratio", w.Ratio[i], c.Ratio[i], RatioTolerance);
            }

            double? a = w.ClearedDay.LastOrDefault(), b = c.ClearedDay.LastOrDefault();
            if (a != b)
            {
                if (a == null || b == null)
                {
                    major.Add((1000, $"{p.Key} L{w.ClearedDay.Length} first clear day {a?.ToString("0.0", CultureInfo.InvariantCulture) ?? "never"} -> {b?.ToString("0.0", CultureInfo.InvariantCulture) ?? "never"}"));
                }
                else
                {
                    Rel($"{p.Key} L{w.ClearedDay.Length} first clear day", a.Value, b.Value, DaysTolerance);
                }
            }
        }

        flagged.AddRange(major.OrderByDescending(x => x.Size).Select(x => x.Line));
        var text = new StringBuilder();
        text.AppendLine(major.Count == 0 ? "Runner balance: no significant change." : $"Runner balance: {major.Count} significant change(s) (ratio/required > {RatioTolerance:P0}, wall > {WallTolerance} levels, L50 days > {DaysTolerance:P0}):");
        foreach (var l in flagged.Take(40))
        {
            text.AppendLine("  ! " + l);
        }

        if (flagged.Count > 40)
        {
            text.AppendLine($"  ! ... and {flagged.Count - 40} more (the CSV next to the test dll, {BalanceReport.CsvName}, has every number)");
        }

        if (minor.Count > 0)
        {
            text.AppendLine($"  within tolerance ({minor.Count} moved), largest:");
            foreach (var l in minor.OrderByDescending(x => x.Size).Take(10))
            {
                text.AppendLine("    " + l.Line);
            }
        }

        return new Result(flagged, text.ToString());
    }
}
