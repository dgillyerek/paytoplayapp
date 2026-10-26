using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek 2026-10-24: "the powerup notification banner is too in the way maybe make it just a quick pop-up from the chests
/// where it's granted". Pick-ups show a short world pop-up at the box, never the screen banner.
/// </summary>
public sealed class RunnerPickupPopupTests
{
    private const float Dt = 1f / 30f;

    [Fact]
    public void Every_pickup_in_a_run_pops_up_at_its_box_with_a_short_label_and_no_banner()
    {
        var c = Runner.Catalog();
        var popped = 0;
        foreach (var level in c.Levels.Take(6))
        {
            var run = new RunnerRun(c, level);
            var defs = new Dictionary<int, RunnerBoxDef>();
            while (!run.Over && run.Seconds < 400f)
            {
                run.SteerTo(Runner.AimLikeAPlayer(run));
                foreach (var e in run.Step(Dt))
                {
                    if (e.Kind == RunnerEventKind.BoxSpawned && run.Box(e.Id) is { } b)
                    {
                        defs[e.Id] = b.Def;
                    }
                    else if (e.Kind == RunnerEventKind.BoxBroken && defs.TryGetValue(e.Id, out var def))
                    {
                        var p = RunnerPickupPopup.For(def, e.X, e.Z);
                        Assert.Equal(e.X, p.X);
                        Assert.Equal(e.Z, p.Z);
                        Assert.False(p.ShowsBanner);
                        Assert.InRange(p.Life, 0.8f, 1.0f);
                        Assert.True(p.Fallback.Length <= 12, p.Fallback);
                        Assert.StartsWith("+", p.Fallback);
                        Assert.NotEmpty(p.IconKey);
                        popped++;
                    }
                }
            }
        }

        Assert.True(popped > 5, "pick-ups seen: " + popped);
        Assert.False(RunnerPickupPopup.UsesBanner(RunnerBannerCause.BoxPickup));
        Assert.False(RunnerPickupPopup.UsesBanner(RunnerBannerCause.SquadFull));
        Assert.True(RunnerPickupPopup.UsesBanner(RunnerBannerCause.BossIncoming));
        Assert.True(RunnerPickupPopup.UsesBanner(RunnerBannerCause.BossQuake));
    }

    [Theory]
    [InlineData(RunnerBoxKind.Weapon, "runner.box.weapon", "25")]
    [InlineData(RunnerBoxKind.Rate, "runner.box.rate", "25")]
    [InlineData(RunnerBoxKind.Range, "runner.box.range", "25")]
    [InlineData(RunnerBoxKind.Heal, "runner.box.heal", "25")]
    [InlineData(RunnerBoxKind.Hero, "runner.popup.hero", "lyra")]
    [InlineData(RunnerBoxKind.Followers, "runner.joined", "3")]
    public void Each_kind_has_a_short_label(RunnerBoxKind kind, string key, string arg0)
    {
        var p = RunnerPickupPopup.For(new RunnerBoxDef(100, kind, "lyra", 3, 0.25f), 1.5f, 40f);
        Assert.Equal(key, p.Key);
        Assert.Equal(arg0, p.Arg0);
        Assert.Equal(1.5f, p.X);
        Assert.Equal(40f, p.Z);
        var strings = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(Layouts.RepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a", "strings", "en.json"))).RootElement;
        var text = strings.GetProperty(key).GetString()!;
        Assert.True(text.Length <= 12, text);
    }

    [Fact]
    public void A_popup_rises_and_fades_inside_a_second()
    {
        Assert.Equal(1f, RunnerPickupPopup.Alpha(0f));
        Assert.Equal(1f, RunnerPickupPopup.Alpha(0.4f));
        Assert.True(RunnerPickupPopup.Alpha(0.8f) < 0.4f);
        Assert.Equal(0f, RunnerPickupPopup.Alpha(RunnerPickupPopup.LifeSeconds));
        Assert.True(RunnerPickupPopup.Height(0.85f) > RunnerPickupPopup.Height(0f));
        Assert.InRange(RunnerPickupPopup.Height(0.85f), 1.0f, 3.0f);
        var full = RunnerPickupPopup.SquadFull(1.1f, 0f, 2f);
        Assert.Equal("1.10", full.Arg0);
        Assert.False(full.ShowsBanner);
    }

    [Fact]
    public void The_scene_pops_pickups_up_and_keeps_only_the_boss_banners()
    {
        var root = Layouts.RepoRoot();
        var view = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "Runner", "RunnerLevelView.cs"));
        Assert.Contains("Popup(RunnerPickupPopup.For(box, e.X, e.Z), RewardColor(box.Kind, gold));", view);
        Assert.Contains("Popup(RunnerPickupPopup.SquadFull(e.Amount,", view);
        Assert.DoesNotContain("Banner(call", view);
        Assert.DoesNotContain("Banner(Fmt(\"runner.squad_full\"", view);
        Assert.Contains("Banner(S(\"runner.boss_incoming\"", view);
        var quake = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "Runner", "RunnerLevelView.BossQuake.cs"));
        Assert.Contains("Banner(S(\"runner.quake_banner\"", quake);
        var all = string.Join("\n", Directory.GetFiles(Path.Combine(root, "Assets", "Survival", "Unity", "Runner"), "RunnerLevelView*.cs").Select(File.ReadAllText));
        // Only the two boss warnings still raise the banner.
        Assert.Equal(2, CountBannerCalls(all));
    }

    private static int CountBannerCalls(string all) =>
        System.Text.RegularExpressions.Regex.Matches(all, @"(?<!void )\bBanner\(").Count;
}
