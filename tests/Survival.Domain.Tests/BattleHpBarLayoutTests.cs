using Survival.Domain.Campaign;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

/// <summary>Side HP bars in configured battles: beside the body, outer / back side, clear of wings, out of the lane gap.</summary>
public sealed class BattleHpBarLayoutTests
{
    [Fact]
    public void Heroes_get_it_on_their_left_and_foes_on_their_right_both_behind_the_body()
    {
        var hero = BattleHpBarLayout.Offset(CampaignSide.Hero, 0.4f, 1f, BattleLaneLayout.LaneWidth(1f));
        var foe = BattleHpBarLayout.Offset(CampaignSide.Villain, 0.4f, 1f, BattleLaneLayout.LaneWidth(1f));
        Assert.True(hero.Right < 0f);
        Assert.True(foe.Right > 0f);
        Assert.True(hero.Forward < 0f, "the bar must sit back, away from the lane between the sides");
        Assert.True(foe.Forward < 0f);
        Assert.Equal(0.4f + BattleHpBarLayout.GapMetres, -hero.Right, 3);
    }

    [Fact]
    public void Bar_clears_the_body_and_the_dragons_wings()
    {
        foreach (var f in LockedBattleRoster.All)
        {
            var scale = LockedBattleRoster.BattleScaleOf(f);
            var half = BattleHpBarLayout.HalfWidthOf(f.Name, f.DesignHeightMetres, scale);
            var lane = BattleLaneLayout.LaneWidth(scale);
            var (right, forward) = BattleHpBarLayout.Offset(CampaignSide.Hero, half, scale, lane);
            var clearance = System.MathF.Sqrt((right * right) + (forward * forward));
            Assert.True(clearance >= half + (BattleHpBarLayout.GapMetres * scale) - 0.001f, f.Name + " bar overlaps its body");
            Assert.True(System.MathF.Abs(right) <= (lane * 0.5f) - BattleHpBarLayout.LaneMarginMetres + 0.001f, f.Name + " bar leaves its lane");
        }

        // 2x Ashwyrm spans about 3.2 m wing to wing.
        Assert.True(BattleHpBarLayout.HalfWidthOf("Ashwyrm", 1f, LockedBattleRoster.AshwyrmBattleScale) >= 1.6f);
    }

    [Fact]
    public void Height_follows_the_body_so_the_2x_dragons_get_twice_the_bar()
    {
        var one = BattleHpBarLayout.Span(1.2f, 1f);
        var two = BattleHpBarLayout.Span(1.2f, 2f);
        Assert.Equal((one.Top - one.Bottom) * 2f, two.Top - two.Bottom, 3);
        Assert.True(one.Bottom > 0f && one.Top < 1.2f);
    }

    [Fact]
    public void On_screen_height_stays_readable_at_any_zoom()
    {
        Assert.Equal(BattleHpBarLayout.MinHeightPx, BattleHpBarLayout.HeightPx(5f));
        Assert.Equal(BattleHpBarLayout.MaxHeightPx, BattleHpBarLayout.HeightPx(5000f));
        Assert.Equal(200f, BattleHpBarLayout.HeightPx(200f));
    }
}
