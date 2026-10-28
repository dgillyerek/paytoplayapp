using Survival.Domain.Theme;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>Derek 2026-10-28: hero health bars move to a corner section in the runner and the battle.</summary>
public sealed class HeroHpPanelLayoutTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(5, 5)]
    [InlineData(9, 5)]
    public void One_row_per_hero_up_to_five(int heroes, int rows) => Assert.Equal(rows, HeroHpPanelLayout.Rows(heroes));

    [Fact]
    public void Height_grows_by_a_row_and_a_gap_and_fits_the_corner()
    {
        Assert.Equal(0f, HeroHpPanelLayout.Height(0));
        Assert.Equal(HeroHpPanelLayout.RowHeight + HeroHpPanelLayout.RowGap, HeroHpPanelLayout.Height(2) - HeroHpPanelLayout.Height(1), 3);
        Assert.Equal(HeroHpPanelLayout.Height(5), HeroHpPanelLayout.Height(8));
        // A full panel stays under the runner's steer hint (0.215 of the 1920 reference height).
        Assert.True(HeroHpPanelLayout.ClearAboveShare(1920f) <= 0.215f);
        Assert.True(HeroHpPanelLayout.RowTop(4) + HeroHpPanelLayout.RowHeight + HeroHpPanelLayout.Padding <= HeroHpPanelLayout.Height(5) + 1e-3f);
    }

    [Fact]
    public void Width_is_a_corner_share_of_a_phone_and_capped_on_wide_screens()
    {
        Assert.InRange(HeroHpPanelLayout.Width(886f), HeroHpPanelLayout.MinWidth, 0.45f * 886f);
        Assert.Equal(HeroHpPanelLayout.MaxWidth, HeroHpPanelLayout.Width(3000f));
    }

    [Fact]
    public void Trail_holds_after_a_hit_drains_after_and_snaps_up_on_heal()
    {
        Assert.Equal(0.8f, HeroHpPanelLayout.Trail(0.8f, 0.5f, 0.1f, 0.016f));
        var drained = HeroHpPanelLayout.Trail(0.8f, 0.5f, 1f, 0.1f);
        Assert.True(drained < 0.8f && drained >= 0.5f);
        Assert.Equal(0.5f, HeroHpPanelLayout.Trail(0.52f, 0.5f, 5f, 1f));
        Assert.Equal(0.9f, HeroHpPanelLayout.Trail(0.5f, 0.9f, 0f, 0f));
    }

    [Fact]
    public void Fallen_rows_dim_and_initials_fall_back()
    {
        Assert.Equal(1f, HeroHpPanelLayout.RowAlpha(true));
        Assert.True(HeroHpPanelLayout.RowAlpha(false) < 0.6f);
        Assert.Equal("S", HeroHpPanelLayout.Initial("Sir Aldric"));
        Assert.Equal("?", HeroHpPanelLayout.Initial(" "));
        Assert.Equal(1f, HeroHpPanelLayout.Flash(0f));
        Assert.Equal(0f, HeroHpPanelLayout.Flash(1f));
    }
}
