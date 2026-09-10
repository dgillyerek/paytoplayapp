using Grove.Domain.Board;
using Grove.Domain.Merge;

namespace Grove.Domain.Tests;

public sealed class CatalogDataTests
{
    [Fact]
    public void Production_json_loads_wildflower_t1_to_t5()
    {
        var catalog = CatalogLoader.LoadDefault();
        Assert.Equal(8, catalog.Items.MaxTier("wildflower"));
        Assert.True(catalog.Items.MaxTier("herb") >= 1);
        Assert.True(catalog.Recipes.TryGet(GroveCatalog.WildflowerT1, out var t1));
        Assert.True(t1.IsStandardThreeToOne);
        Assert.Equal(GroveCatalog.WildflowerT2, t1.Output);
        Assert.True(catalog.Recipes.TryGet(GroveCatalog.WildflowerT4, out var t4));
        Assert.Equal(GroveCatalog.WildflowerT5, t4.Output);
        Assert.True(catalog.Items.TryGet(GroveCatalog.WildflowerT5, out _));
        Assert.True(catalog.Recipes.TryGet(new PieceId("wildflower_t7"), out var t7));
        Assert.Equal(new PieceId("wildflower_t8"), t7.Output);
    }

    [Fact]
    public void Qa_can_change_a_recipe_in_json_without_touching_merge_session()
    {
        const string items = """
            { "items": [
              { "id": "qa_t1", "displayName": "QA T1", "chain": "qa", "tier": 1 },
              { "id": "qa_t2", "displayName": "QA T2", "chain": "qa", "tier": 2 }
            ] }
            """;
        const string recipes = """
            { "recipes": [
              { "input": "qa_t1", "inputCount": 3, "output": "qa_t2", "outputCount": 1 }
            ] }
            """;

        var loaded = CatalogLoader.FromJson(items, recipes);
        var session = new MergeSession(new BoardGrid(), loaded.Recipes);
        var a = new GridPos(0, 0);
        var b = new GridPos(1, 0);
        session.Board.Place(a, new PieceStack(new PieceId("qa_t1"), 1));
        session.Board.Place(b, new PieceStack(new PieceId("qa_t1"), 2));

        var result = session.TryDrag(a, b);

        var applied = Assert.IsType<DragResult.Applied>(result);
        Assert.Equal(new PieceId("qa_t2"), applied.Merge!.Produced);
        Assert.Equal("qa", loaded.Items.Require(new PieceId("qa_t2")).Chain);
    }

    [Fact]
    public void Production_recipes_are_three_to_one()
    {
        var catalog = CatalogLoader.LoadDefault();
        Assert.NotEmpty(catalog.Recipes.Recipes);
        foreach (var recipe in catalog.Recipes.Recipes)
        {
            Assert.True(recipe.IsStandardThreeToOne, recipe.Input.Value);
        }
    }

    [Fact]
    public void Pebble_chain_still_present_for_dev001()
    {
        var catalog = GroveCatalog.CreateDefault();
        Assert.True(catalog.TryGet(GroveCatalog.Pebble, out var recipe));
        Assert.Equal(GroveCatalog.Sprout, recipe.Output);
    }
}
