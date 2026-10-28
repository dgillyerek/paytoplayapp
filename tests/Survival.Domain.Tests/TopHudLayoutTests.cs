using System.Linq;
using Survival.Domain.Runner;
using Survival.Domain.Theme;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>Derek 2026-10-28: hero HP top-left, a smaller title banner with the distance beside it; nothing on top may collide.</summary>
public sealed class TopHudLayoutTests
{
    public static TheoryData<float, int> Screens => new()
    {
        { 1080f, 1 }, { 1080f, 5 }, // 1080 x 1920
        { 1920f * 9f / 19.5f, 1 }, { 1920f * 9f / 19.5f, 5 }, // 9 : 19.5
    };

    private static TopHudLayout.Box[] RunnerBoxes() =>
        TopHudLayout.Runner(RunnerHudChips.DamageLeft, RunnerHudChips.Width, RunnerHudChips.SpeedLeft).ToArray();

    [Theory]
    [MemberData(nameof(Screens))]
    public void Runner_top_elements_and_hero_panel_do_not_overlap(float width, int heroes) =>
        AssertApart(RunnerBoxes().Append(TopHudLayout.Panel(width, 1920f, heroes, heroes == 5 ? HeroHpPanelLayout.MaxUnitRows : 1)).ToArray());

    [Theory]
    [MemberData(nameof(Screens))]
    public void Battle_top_elements_and_hero_panel_do_not_overlap(float width, int heroes) =>
        AssertApart(TopHudLayout.Battle().Append(TopHudLayout.Panel(width, 1920f, heroes)).ToArray());

    [Fact]
    public void Everything_is_on_screen_and_the_title_shares_its_row_with_the_distance()
    {
        foreach (var b in RunnerBoxes().Concat(TopHudLayout.Battle()))
        {
            Assert.True(b.X0 >= 0f && b.X1 <= 1f && b.Y0 >= 0f && b.Y1 <= 1f && b.X0 < b.X1 && b.Y0 < b.Y1, b.ToString());
        }

        var t = TopHudLayout.RunnerTitle;
        var d = TopHudLayout.Distance;
        Assert.True(d.Y0 < t.Y1 && t.Y0 < d.Y1, "distance on the title's row");
        Assert.True(t.X1 - t.X0 < 0.5f, "title banner smaller than before (0.68 wide)");
        Assert.True(TopHudLayout.Hint.Y1 < 0.5f);
    }

    private static void AssertApart(TopHudLayout.Box[] boxes)
    {
        for (var i = 0; i < boxes.Length; i++)
        {
            for (var j = i + 1; j < boxes.Length; j++)
            {
                // The boss mark deliberately caps the distance bar's end.
                if ((boxes[i].Name, boxes[j].Name) is ("Distance", "BossMark"))
                {
                    continue;
                }

                Assert.False(boxes[i].Overlaps(boxes[j]), $"{boxes[i]} overlaps {boxes[j]}");
            }
        }
    }
}
