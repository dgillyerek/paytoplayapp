using Grove.Domain.Board;

namespace Grove.Domain.Merge
{
    /// <summary>Starter Grove chain used by DEV-001: pebble → sprout → sapling → tree → grove.</summary>
    public static class GroveCatalog
    {
        public static readonly PieceId Pebble = new("pebble");
        public static readonly PieceId Sprout = new("sprout");
        public static readonly PieceId Sapling = new("sapling");
        public static readonly PieceId Tree = new("tree");
        public static readonly PieceId Grove = new("grove");

        public static MergeCatalog CreateDefault() => new(
        [
            new MergeRecipe(Pebble, MergeRecipe.DefaultInputCount, Sprout, MergeRecipe.DefaultOutputCount),
            new MergeRecipe(Sprout, MergeRecipe.DefaultInputCount, Sapling, MergeRecipe.DefaultOutputCount),
            new MergeRecipe(Sapling, MergeRecipe.DefaultInputCount, Tree, MergeRecipe.DefaultOutputCount),
            new MergeRecipe(Tree, MergeRecipe.DefaultInputCount, Grove, MergeRecipe.DefaultOutputCount)
        ]);
    }
}
