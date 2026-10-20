using Survival.Domain.Theme;
using Survival.Domain.View;

namespace Survival.Domain.Tests;

public sealed class BottomNavTests
{
    [Fact]
    public void Default_bar_is_heroes_alliance_bag_with_no_base_or_world()
    {
        var nav = new BottomNav();
        Assert.Equal(new[] { BottomNav.Heroes, BottomNav.Alliance, BottomNav.Bag }, nav.Buttons.Select(b => b.Id));
        Assert.False(nav.Contains("nav.base"));
        Assert.False(nav.Contains("nav.world"));
    }

    [Fact]
    public void Buttons_are_evenly_spaced_equal_width_and_inside_the_bar()
    {
        for (var n = 1; n <= 5; n++)
        {
            var slots = Enumerable.Range(0, n).Select(i => BottomNav.Slot(i, n)).ToList();
            var w = slots[0].Max - slots[0].Min;
            Assert.True(w > 0.1f && w <= BottomNav.MaxWidth + 1e-5f, $"n={n} w={w}");
            Assert.True(slots[0].Min >= BottomNav.Pad - 1e-5f && slots[^1].Max <= 1f - BottomNav.Pad + 1e-5f);
            Assert.Equal(1f - slots[^1].Max, slots[0].Min, 4); // centred
            for (var i = 1; i < n; i++)
            {
                Assert.Equal(w, slots[i].Max - slots[i].Min, 4);
                Assert.Equal(BottomNav.Gap, slots[i].Min - slots[i - 1].Max, 4);
            }
        }
    }

    [Fact]
    public void Three_buttons_leave_room_for_a_battle_button()
    {
        var used = BottomNav.Slot(2, 3).Max - BottomNav.Slot(0, 3).Min;
        Assert.True(1f - used > BottomNav.MaxWidth, "free width for a fourth button");
    }

    [Fact]
    public void Adding_battle_relayouts_and_taps_route_by_id()
    {
        var nav = new BottomNav();
        var changed = 0;
        var taps = new List<string>();
        nav.Changed += () => changed++;
        nav.Tapped += taps.Add;
        Assert.True(nav.Add(new NavButtonDef(BottomNav.Battle, BottomNav.IconBattle), 0));
        Assert.False(nav.Add(new NavButtonDef(BottomNav.Battle, BottomNav.IconBattle)));
        Assert.Equal(1, changed);
        Assert.Equal(BottomNav.Battle, nav.Buttons[0].Id);
        Assert.Equal(4, nav.Buttons.Count);
        Assert.True(nav.Tap(BottomNav.Heroes));
        Assert.False(nav.Tap("nav.world"));
        Assert.Equal(new[] { BottomNav.Heroes }, taps);
        Assert.True(nav.Remove(BottomNav.Battle));
        Assert.Equal(3, nav.Buttons.Count);
    }

    [Fact]
    public void Bar_icons_have_pack_art_strings_and_hud_slots()
    {
        var pack = TestPaths.LoadPack();
        foreach (var key in new[] { BottomNav.BarArt, BottomNav.ButtonArt, BottomNav.IconHeroes, BottomNav.IconAlliance, BottomNav.IconBag, BottomNav.IconBattle, BottomNav.IconBuild })
        {
            Assert.True(pack.TryArt(key, out var rel), key);
            Assert.True(File.Exists(Path.Combine(Layouts.RepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a", rel)), rel);
            var (_, _, colorType) = Layouts.PngHeader(Path.Combine(Layouts.RepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a", rel));
            Assert.Equal(6, colorType); // RGBA cut-out
        }

        foreach (var id in new[] { BottomNav.Heroes, BottomNav.Alliance, BottomNav.Bag, BottomNav.Battle })
        {
            Assert.True(pack.Strings.ContainsKey(id), id);
        }

        foreach (var slot in new[] { "nav", "build_button", "debug_gift", "debug_skip", "view_toggle" })
        {
            Assert.True(pack.Hud.ContainsKey(slot), slot);
        }

        // Debug chips shrank into the corner above the hero card.
        var gift = pack.Hud["debug_gift"];
        Assert.True(gift.XMax - gift.XMin < 0.12f && gift.YMin >= pack.Hud["hero_card"].YMax - 1e-4f);
    }
}
