using System.Globalization;
using Survival.Domain.Ota;
using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>
/// The free-pace sim behind remote balance (Docs/OTA.md). A phone cannot run it, so publishing runs it:
/// scripts/ota/check_remote_balance.sh sets OTA_CANDIDATE_CONFIG (and OTA_ATTESTATION_OUT) and runs
/// <see cref="Candidate_config_keeps_the_locked_free_pace"/>, which applies the candidate's balance patches to the
/// shipped catalog files, re-runs the RunnerFreePaceTests model, fails when the free wall or any checkpoint day moves
/// beyond <see cref="FreePaceLock.Tolerance"/>, and otherwise writes the free_pace_check the phone's
/// <see cref="FreePaceGuard"/> requires.
/// </summary>
public sealed class RemoteBalancePaceTests
{
    private const string AppId = "com.brightmoatstudios.kingdomdefense";

    private static (int Wall, Dictionary<int, double?> Days, List<string> Off) Simulate(RemoteConfig config)
    {
        var overlay = RemoteBalance.Unchecked(config);
        string Read(string name)
        {
            var path = Path.Combine(TestPaths.CatalogDir, name);
            return overlay.Apply(path, File.ReadAllText(path));
        }

        var runnerPatched = config.BalancePatches.ContainsKey(RunnerCatalog.FileName);
        var catalog = RunnerCatalog.Parse(Read(RunnerCatalog.FileName));
        var baseline = BalanceReport.Get();

        // Only runner_levels.json changes the fights; any other patch reuses the shipped required-power curve (fast).
        var report = BalanceReport.Build(catalog, BalanceReport.LoadDef(), runnerPatched ? null : baseline.RequiredScale, Read);
        var days = FreePaceLock.ClearedDay.Keys.ToDictionary(l => l, l => report.Row("free", l).ClearedDay);
        var oneSession = 1.0 / report.Def.SessionsPerDay + 1e-6;
        var off = FreePaceLock.Drift(report.Wall("free"), l => days[l], oneSession);
        return (report.Wall("free"), days, off);
    }

    [Fact]
    public void The_shipped_default_config_keeps_the_locked_free_pace()
    {
        var c = RemoteConfigParser.Parse(OtaTests.Baked, AppId).Config!;
        if (!c.HasBalancePatches)
        {
            Assert.Equal(FreePaceVerdict.NoPatches, FreePaceGuard.Check(c, out _));
            return;
        }

        var (_, _, off) = Simulate(c);
        Assert.True(off.Count == 0, "Baked remote balance moves the free pace:\n" + string.Join("\n", off));
    }

    [Fact]
    public void A_pace_moving_remote_patch_is_caught_by_the_sim()
    {
        // Ten times the runner's per-level reward growth: free players get far more gold, so they out-level the wall.
        var patch = "{ \"runner_levels.json\": { \"progression\": { \"reward_step\": 3.0 } } }";
        var c = RemoteConfigParser.Parse(OtaTests.ConfigWithPatch(patch, null), AppId).Config!;
        var (_, _, off) = Simulate(c);
        Assert.NotEmpty(off);
    }

    [Fact]
    public void Candidate_config_keeps_the_locked_free_pace()
    {
        var path = Environment.GetEnvironmentVariable("OTA_CANDIDATE_CONFIG");
        if (string.IsNullOrEmpty(path))
        {
            return; // Only scripts/ota/check_remote_balance.sh sets it.
        }

        var parsed = RemoteConfigParser.Parse(File.ReadAllText(path), AppId);
        Assert.True(parsed.Ok, "Candidate remote config is invalid:\n" + string.Join("\n", parsed.Errors));
        var c = parsed.Config!;
        var sha = FreePaceGuard.HashPatches(c);
        if (c.FreePaceCheck != null && c.FreePaceCheck.BalanceSha256 == sha)
        {
            // Already stamped (publish_config re-checks): the phone's guard must accept exactly this file.
            Assert.Equal(FreePaceVerdict.Accepted, FreePaceGuard.Check(c, out var why));
            Assert.Equal("", why);
        }

        var (wall, days, off) = c.HasBalancePatches ? Simulate(c) : (FreePaceLock.Wall, FreePaceLock.ClearedDay.ToDictionary(d => d.Key, d => (double?)d.Value), new List<string>());
        Assert.True(off.Count == 0, "Candidate balance moves the locked free pace (re-tune enemy strength instead):\n" + string.Join("\n", off));

        var outPath = Environment.GetEnvironmentVariable("OTA_ATTESTATION_OUT");
        if (!string.IsNullOrEmpty(outPath))
        {
            var dayJson = string.Join(", ", days.OrderBy(d => d.Key).Select(d => "\"" + d.Key + "\": " + d.Value!.Value.ToString("R", CultureInfo.InvariantCulture)));
            File.WriteAllText(outPath,
                "{ \"balance_sha256\": \"" + sha + "\", \"wall\": " + wall + ", \"cleared_day\": { " + dayJson + " }, \"checked_at\": \"" +
                DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) + "\", \"checked_by\": \"RemoteBalancePaceTests\" }\n");
        }
    }
}
