using System.Collections.Generic;
using Grove.Domain.Energy;
using Grove.Domain.Orders;
using Grove.Domain.Producer;

namespace Grove.Domain.Merge
{
    /// <summary>Parsed production data: items, 3-merge recipes, garden crate, energy, scripted orders, presentation copy.</summary>
    public sealed record LoadedCatalog(
        ItemCatalog Items,
        MergeCatalog Recipes,
        ProducerDefinition? GardenCrate,
        EnergyConfig Energy,
        IReadOnlyList<OrderSpec> Orders,
        PresentationCopy Copy);
}
