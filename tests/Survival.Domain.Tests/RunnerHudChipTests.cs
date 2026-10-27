using Survival.Domain.Runner;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>The runner HUD's upgrade badges keep their label inside them (Derek 2026-10-27: "+65% DMG" ran past the badge).</summary>
public sealed class RunnerHudChipTests
{
    [Theory]
    [InlineData("+100% DMG")]
    [InlineData("+100% SPD")]
    [InlineData("+65% DMG")]
    public void The_longest_label_fits_inside_its_badge_on_the_narrowest_phone(string label)
    {
        var room = RunnerHudChips.LabelRoom(RunnerHudChips.NarrowestAspect);
        Assert.True(RunnerHudChips.LabelWidth(label, RunnerHudChips.MaxFont) <= room, $"{label}: {RunnerHudChips.LabelWidth(label, RunnerHudChips.MaxFont):0} > {room:0}");
    }

    [Fact]
    public void The_badges_stand_side_by_side_clear_of_each_other_and_the_kills_panel()
    {
        Assert.True(RunnerHudChips.DamageLeft >= 0f);
        Assert.True(RunnerHudChips.DamageLeft + RunnerHudChips.Width < RunnerHudChips.SpeedLeft);
        Assert.True(RunnerHudChips.SpeedLeft + RunnerHudChips.Width < RunnerHudChips.KillsLeft);
        Assert.True(RunnerHudChips.TextFrom < RunnerHudChips.TextTo && RunnerHudChips.TextTo < 1f);
        Assert.True(RunnerHudChips.MinFont < RunnerHudChips.MaxFont);
    }
}
