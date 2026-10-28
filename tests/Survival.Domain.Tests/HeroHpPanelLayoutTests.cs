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
        // ~80-85% of the first cut (62 px rows), and a full panel ends well above mid-screen.
        Assert.InRange(HeroHpPanelLayout.RowHeight / 62f, 0.85f, 0.95f);
        // Narrower than the 0.38-share / 360 cut (Derek: "less wide but a little taller").
        Assert.InRange(HeroHpPanelLayout.Width(886f) / 336.7f, 0.70f, 0.76f);
        Assert.True(TopHudLayout.PanelTop - (HeroHpPanelLayout.Height(5) / 1920f) > 0.70f);
        Assert.True(HeroHpPanelLayout.RowTop(4) + HeroHpPanelLayout.RowHeight + HeroHpPanelLayout.Padding <= HeroHpPanelLayout.Height(5) + 1e-3f);
    }

    [Fact]
    public void Width_is_a_corner_share_of_a_phone_and_capped_on_wide_screens()
    {
        Assert.InRange(HeroHpPanelLayout.Width(886f), HeroHpPanelLayout.MinWidth, 0.40f * 886f);
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

    [Fact]
    public void Panel_hangs_under_the_top_row_inside_the_safe_area()
    {
        Assert.Equal(HeroHpPanelLayout.Margin, HeroHpPanelLayout.TopOffset(1920f, 500f));
        Assert.Equal((1f - TopHudLayout.PanelTop) * 1920f, HeroHpPanelLayout.TopOffset(1920f, 0f), 3);
        Assert.True(TopHudLayout.PanelTop <= TopHudLayout.RowY0);
    }

    [Fact]
    public void Follower_type_rows_sit_under_a_divider_and_are_a_little_shorter()
    {
        Assert.True(HeroHpPanelLayout.UnitRowHeight < HeroHpPanelLayout.RowHeight);
        Assert.Equal(HeroHpPanelLayout.Height(5), HeroHpPanelLayout.Height(5, 0));
        Assert.Equal(HeroHpPanelLayout.UnitRowHeight + HeroHpPanelLayout.RowGap + HeroHpPanelLayout.DividerHeight, HeroHpPanelLayout.Height(2, 1) - HeroHpPanelLayout.Height(2), 3);
        Assert.Equal(HeroHpPanelLayout.Height(5, 4), HeroHpPanelLayout.Height(5, 9));
        Assert.True(HeroHpPanelLayout.UnitRowTop(5, 0) >= HeroHpPanelLayout.RowTop(4) + HeroHpPanelLayout.RowHeight + HeroHpPanelLayout.DividerHeight);
        Assert.Equal(HeroHpPanelLayout.Height(5, 4), HeroHpPanelLayout.UnitRowTop(5, 3) + HeroHpPanelLayout.UnitRowHeight + HeroHpPanelLayout.Padding, 3);
    }
}
