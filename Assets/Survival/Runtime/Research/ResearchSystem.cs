using Survival.Domain.Catalog;

namespace Survival.Domain.Research
{
    public sealed class ResearchSystem
    {
        public ResearchSystem(SurvivalCatalog catalog) => Nodes = catalog.Research;

        public System.Collections.Generic.IReadOnlyList<ResearchDef> Nodes { get; }
    }
}
