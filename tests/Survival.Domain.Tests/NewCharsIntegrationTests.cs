using Survival.Domain.Campaign;
using Survival.Domain.Roster;
using Survival.Domain.Runner;
using Survival.Domain.Theme;
using Survival.Domain.World;

namespace Survival.Domain.Tests;

/// <summary>
/// The preview_new_chars characters in the game (Derek 2026-11-15: "incorporate them into the game where it makes sense").
/// Seraphine and Kaelen are campaign heroes (shards from chapters and Malzareth's camp); Malzareth is a campaign villain
/// (Duskcrown's boss, his own world camps) and a runner heavy. His runner stats copy Blightroot's so the free pace holds.
/// </summary>
public sealed class NewCharsIntegrationTests
{
    [Fact]
    public void Each_new_character_is_a_findable_battle_fighter_with_a_hit_profile_and_shipped_files()
    {
        foreach (var spec in NewCharMotion.All)
        {
            Assert.True(LockedBattleRoster.TryFind(spec.Name, out var f), spec.Name);
            Assert.Equal(spec.Side, f.Side);
            Assert.Contains(f, LockedBattleRoster.Extras);
            Assert.DoesNotContain(f, LockedBattleRoster.All);
            _ = LockedBattleHits.ProfileOf(spec.Name);
            Assert.NotNull(RunnerAttackTiming.ClipFor(spec.Name));
            foreach (var rel in new[] { spec.RigRestRel, spec.AtlasRel }.Concat(spec.PoseNames.Where(p => p != NewCharMotion.RestPoseName).Select(spec.ClipRel)))
            {
                Assert.True(File.Exists(Path.Combine(Layouts.RepoRoot(), "Assets", rel)), rel);
            }

            foreach (var a in spec.Attacks)
            {
                Assert.InRange(a.ContactSeconds, 0.1f, a.Seconds - 0.05f);
            }
        }

        Assert.Equal(BattleSide.Hero, NewCharMotion.Seraphine.Side);
        Assert.Equal(BattleSide.Hero, NewCharMotion.Kaelen.Side);
        Assert.Equal(BattleSide.Villain, NewCharMotion.Malzareth.Side);
    }

    [Fact]
    public void Campaign_has_the_new_heroes_unowned_and_malzareth_as_duskcrowns_boss()
    {
        var c = Camp.Catalog();
        foreach (var id in new[] { "hero.seraphine", "hero.kaelen" })
        {
            var h = c.Hero(id)!;
            Assert.Equal(CampaignSide.Hero, h.Side);
            Assert.False(h.StartOwned);
        }

        Assert.Equal(CampaignSide.Villain, c.Hero("foe.malzareth")!.Side);
        var chapters = c.BattleCampaign!.Chapters;
        var duskcrown = chapters.Single(ch => ch.Name == "Duskcrown");
        Assert.Equal("foe.malzareth", duskcrown.Boss);
        Assert.Equal("hero.kaelen", duskcrown.ShardHero);
        Assert.Contains(chapters, ch => ch.ShardHero == "hero.seraphine");

        var camp = c.CampStage("camp.malzareth_01", "malzareth", 10, 4000, "Malzareth's Camp");
        Assert.Equal("foe.malzareth", camp.Enemies[0].HeroId);
        Assert.Equal("hero.kaelen", camp.ShardHero);
        Assert.True(camp.Shards > 0);
    }

    [Fact]
    public void World_map_places_malzareths_camps_with_bound_art_and_name()
    {
        var layout = WorldLayout.Parse(File.ReadAllText(Path.Combine(TestPaths.PackDir, "map", "world_layout.json")));
        var camps = layout.Entities.Where(e => e.Kind == WorldEntityKind.EnemyCamp && e.Type == "malzareth").ToList();
        Assert.NotEmpty(camps);
        var pack = TestPaths.LoadPack();
        Assert.True(pack.TryArt(WorldArtKeys.Camp("malzareth"), out _));
        Assert.True(pack.Strings.ContainsKey("world.camp.malzareth"));
        foreach (var key in new[] { "hero.seraphine.name", "hero.kaelen.name", "foe.malzareth.name" })
        {
            Assert.True(pack.Strings.ContainsKey(key), key);
        }

        foreach (var name in new[] { "seraphine", "kaelen", "malzareth", "vorgath" })
        {
            Assert.True(pack.TryArt("theme_a.hero.portrait." + name, out var rel), name);
            Assert.True(File.Exists(Path.Combine(Layouts.RepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a", rel)), rel);
        }
    }

    [Fact]
    public void Malzareth_runner_unit_copies_blightroot_so_the_pace_sim_does_not_move()
    {
        var c = Runner.Catalog();
        var b = c.Unit("blightroot");
        var m = c.Unit("malzareth");
        Assert.Equal("Malzareth", m.Roster);
        Assert.Equal(b.Hp, m.Hp);
        Assert.Equal(b.Damage, m.Damage);
        Assert.Equal(b.Cycle, m.Cycle);
        Assert.Equal(b.Range, m.Range);
        Assert.Equal(b.Speed, m.Speed);
        Assert.Equal(b.Ranged, m.Ranged);
        Assert.Equal(b.Radius, m.Radius, 3);
    }

    [Fact]
    public void Malzareth_anchors_some_runner_waves_from_level_11_and_never_earlier()
    {
        var c = Runner.Catalog();
        var with = new List<int>();
        for (var n = 1; n <= c.Levels.Count; n++)
        {
            if (c.ByNumber(n).Rows.Any(r => r.Unit == "malzareth"))
            {
                with.Add(n);
            }
        }

        Assert.NotEmpty(with);
        Assert.True(with.Min() >= RunnerProgression.MalzarethFrom, string.Join(",", with));
    }

    [Fact]
    public void Melee_heroes_seraphine_and_kaelen_stay_off_the_ranged_runner_squad()
    {
        var c = Runner.Catalog();
        Assert.DoesNotContain(c.Units.Values, u => u.Roster is "Seraphine" or "Kaelen");
    }
}
