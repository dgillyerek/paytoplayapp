using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Survival.Domain.Campaign;
using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>One profile on one level: the model numbers plus what the runner sim said with that loadout.</summary>
public sealed record BalanceRow(
    string Profile,
    int Level,
    double Required,
    double Power,
    double Ratio,
    double TimeToKill,
    double TimeToOverrun,
    bool SimWon,
    double SimHeroHp,
    double SimSeconds,
    int? ClearedSession,
    double? ClearedDay,
    string Heroes,
    double GoldEarned,
    double UsdSpent);

/// <summary>
/// The runner balance report, computed once per test run (a few thousand sims on all cores, ~10-15 s):
/// required power per level from the sim, then every spend profile's timeline and a validation sim with its loadout.
/// </summary>
public sealed class BalanceReport
{
    public const string CsvName = "runner_balance.csv";

    private static readonly Lazy<BalanceReport> Shared = new(() => Build(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static BalanceReport Get() => Shared.Value;

    public required RunnerCatalog Runner { get; init; }
    public required RunnerBalanceDef Def { get; init; }
    /// <summary>Raw sim result per level (index = level number): the least uniform stat scale a dodging player wins with.</summary>
    public required double[] RequiredScale { get; init; }

    /// <summary>
    /// Required power per level: the scale squared (damage x HP). Not smoothed: the per-level wobble is the generated
    /// layout's real difficulty, and the economy clears levels in order, so reaching level n takes the max over 1..n anyway.
    /// </summary>
    public required double[] RequiredPower { get; init; }

    /// <summary>Per profile: (day, power on the reference level) at the end of chosen days of a full-length timeline.</summary>
    public required Dictionary<string, List<(int Day, double Power, string Heroes, double Usd)>> PowerByDay { get; init; }
    public required Dictionary<string, List<BalanceRow>> Rows { get; init; }
    public required Dictionary<string, List<RunnerLevelOutcome>> Timelines { get; init; }

    /// <summary>The economy of each profile's run (purchases, stuck buys).</summary>
    public required Dictionary<string, RunnerEconomy> Economies { get; init; }

    public required RunnerBalanceSources Src { get; init; }

    public int Levels => Runner.Levels.Count;

    public BalanceRow Row(string profile, int level) => Rows[profile][level - 1];

    /// <summary>The free wall: the first level the profile cannot clear on schedule (power ratio at its nominal session below 1).</summary>
    public int Wall(string profile) => Rows[profile].FirstOrDefault(r => r.Ratio < 1.0)?.Level ?? Levels + 1;

    /// <summary>The last level the profile ever clears within the modelled years.</summary>
    public int Furthest(string profile) => Rows[profile].LastOrDefault(r => r.ClearedSession != null)?.Level ?? 0;

    public static RunnerBalanceSources Sources(RunnerCatalog runner)
    {
        var dir = TestPaths.CatalogDir;
        return new RunnerBalanceSources(
            CampaignCatalog.Parse(File.ReadAllText(Path.Combine(dir, CampaignCatalog.FileName))),
            runner,
            File.ReadAllText(Path.Combine(dir, "base_economy.json")),
            File.ReadAllText(Path.Combine(dir, "gifts.json")),
            File.ReadAllText(Path.Combine(dir, "quests.json")),
            File.ReadAllText(Path.Combine(dir, "stores.json")),
            File.ReadAllText(Path.Combine(dir, "power_packs.json")));
    }

    public static RunnerBalanceDef LoadDef() => RunnerBalanceDef.Parse(File.ReadAllText(Path.Combine(TestPaths.CatalogDir, RunnerBalanceDef.FileName)));

    /// <summary>
    /// One run with a dodging player (0.3 s reaction), up to 10 minutes. (The reaction time barely matters here: it only
    /// decides boss dodges, and 0.2 / 0.3 / 0.4 s gave the same required power on every level.)
    /// </summary>
    public static RunnerRun Sim(RunnerCatalog c, RunnerLevelDef level, RunnerLoadout loadout, float reaction = 0.3f)
    {
        var run = new RunnerRun(c, level, loadout);
        while (!run.Over && run.Seconds < 600f)
        {
            run.SteerTo(RunnerBossQuakeTests.DodgingPlayer(run, reaction));
            run.Step(1f / 30f);
        }

        return run;
    }

    public static bool Wins(RunnerCatalog c, RunnerLevelDef level, RunnerLoadout loadout) => Sim(c, level, loadout).Outcome == RunnerOutcome.Won;

    /// <summary>
    /// The least uniform stat scale (every squad unit's damage and HP times s) a dodging player wins the level with:
    /// geometric bisection on [1/8, 64], 12 halvings (about 0.4% precision).
    /// </summary>
    public static double FindRequiredScale(RunnerCatalog c, RunnerLevelDef level)
    {
        double lo = 0.125, hi = 64;
        if (!Wins(c, level, RunnerBalance.Uniform(c, hi)))
        {
            return double.PositiveInfinity;
        }

        for (var k = 0; k < 12; k++)
        {
            var mid = Math.Sqrt(lo * hi);
            if (Wins(c, level, RunnerBalance.Uniform(c, mid)))
            {
                hi = mid;
            }
            else
            {
                lo = mid;
            }
        }

        return hi;
    }

    /// <param name="requiredScale">Reuse a required curve (same catalog) instead of re-simming it, e.g. to try an economy change.</param>
    public static BalanceReport Build(RunnerCatalog? catalog = null, RunnerBalanceDef? def = null, double[]? requiredScale = null)
    {
        var c = catalog ?? Tests.Runner.Catalog();
        var d = def ?? LoadDef();
        var n = c.Levels.Count;
        var req = requiredScale ?? new double[n + 1];
        if (requiredScale == null)
        {
            Parallel.For(1, n + 1, i => req[i] = FindRequiredScale(c, c.ByNumber(i)));
        }
        var reqPower = Enumerable.Range(0, n + 1).Select(i => i == 0 ? 0 : req[i] * req[i]).ToArray();
        var src = Sources(c);
        var byDay = new Dictionary<string, List<(int, double, string, double)>>();
        var rows = new Dictionary<string, List<BalanceRow>>();
        var timelines = new Dictionary<string, List<RunnerLevelOutcome>>();
        var economies = new Dictionary<string, RunnerEconomy>();
        foreach (var p in d.Profiles)
        {
            var eco = new RunnerEconomy(src, d, p, k => Math.Sqrt(reqPower[k]));
            var tl = eco.Run();
            var full = new RunnerEconomy(src, d with { StopWhenDone = false }, p, k => Math.Sqrt(reqPower[k]));
            full.Run();
            var refLevel = c.ByNumber(n);
            byDay[p.Id] = ReportDays.Where(day => day <= full.Daily.Count).Select(day =>
            {
                var snap = full.Daily[day - 1];
                var lo = full.LoadoutOf(snap);
                return (day, RunnerBalance.Strength(c, refLevel, lo).Power, HeroText(snap.Heroes), snap.UsdSpent);
            }).ToList();
            timelines[p.Id] = tl;
            economies[p.Id] = eco;
            var outRows = new BalanceRow[n];
            Parallel.For(0, n, i =>
            {
                var o = tl[i];
                var level = c.ByNumber(o.Number);
                var lo = eco.LoadoutOf(o.AtPace);
                var st = RunnerBalance.Strength(c, level, lo);
                var run = Sim(c, level, lo);
                outRows[i] = new BalanceRow(
                    p.Id,
                    o.Number,
                    reqPower[o.Number],
                    st.Power,
                    o.PowerRatio,
                    st.TimeToKill,
                    st.TimeToOverrun,
                    run.Outcome == RunnerOutcome.Won,
                    run.Hero.HpFraction,
                    run.Seconds,
                    o.ClearedSession,
                    o.ClearedDay,
                    HeroText(o.AtPace.Heroes),
                    o.AtPace.GoldEarned,
                    o.AtPace.UsdSpent);
            });
            rows[p.Id] = outRows.ToList();
        }

        return new BalanceReport { Runner = c, Def = d, RequiredScale = req, RequiredPower = reqPower, Rows = rows, Timelines = timelines, PowerByDay = byDay, Economies = economies, Src = src };
    }

    public static readonly int[] ReportDays = { 1, 3, 7, 10, 14, 21, 30, 45, 60, 90, 120, 180, 270, 365, 730, 1095 };

    private static string HeroText(IReadOnlyDictionary<string, (int Level, int Stars)> heroes) =>
        string.Join(" ", heroes.OrderBy(h => h.Key, StringComparer.Ordinal).Select(h => $"{h.Key.Replace("hero.", "")}:{h.Value.Level}/{h.Value.Stars}*"));

    private static string F(double v) => double.IsInfinity(v) ? "inf" : v.ToString("0.###", CultureInfo.InvariantCulture);

    public string Csv()
    {
        var sb = new StringBuilder("profile,level,raw_required_power,required_power,power,ratio,ttk_wave_s,tto_wave_s,sim_won,sim_hero_hp,sim_seconds,cleared_session,cleared_day,heroes,gold_earned,usd_spent\n");
        foreach (var p in Rows)
        {
            foreach (var r in p.Value)
            {
                sb.Append(string.Join(",", r.Profile, r.Level, F(RequiredScale[r.Level] * RequiredScale[r.Level]), F(r.Required), F(r.Power), F(r.Ratio), F(r.TimeToKill), F(r.TimeToOverrun), r.SimWon ? 1 : 0, F(r.SimHeroHp), F(r.SimSeconds),
                    r.ClearedSession?.ToString(CultureInfo.InvariantCulture) ?? "", r.ClearedDay == null ? "" : F(r.ClearedDay.Value), r.Heroes, F(r.GoldEarned), F(r.UsdSpent))).Append('\n');
            }
        }

        return sb.ToString();
    }

    public string Table()
    {
        var sb = new StringBuilder();
        foreach (var p in Rows)
        {
            sb.AppendLine($"== {p.Key}: wall at L{Wall(p.Key)}, furthest clear L{Furthest(p.Key)}");
            sb.AppendLine("   power on L" + Levels + " by day: " + string.Join("  ", PowerByDay[p.Key].Select(x => $"d{x.Day}={F(x.Power)} ({x.Heroes}, ${x.Usd:0})")));
            foreach (var r in p.Value)
            {
                sb.AppendLine($"L{r.Level,2} raw {F(RequiredScale[r.Level] * RequiredScale[r.Level]),8} need {F(r.Required),8} have {F(r.Power),8} ratio {r.Ratio,6:0.00} ttk {r.TimeToKill,6:0.0}s tto {r.TimeToOverrun,6:0.0}s sim {(r.SimWon ? "WIN " : "lose")} hp {r.SimHeroHp,4:0.00} t {r.SimSeconds,4:0} | clear day {(r.ClearedDay == null ? "never" : r.ClearedDay.Value.ToString("0.0", CultureInfo.InvariantCulture)),6} | {r.Heroes} | ${r.UsdSpent:0}");
            }
        }

        return sb.ToString();
    }
}
