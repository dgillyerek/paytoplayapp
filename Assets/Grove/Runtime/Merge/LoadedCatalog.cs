using System.Collections.Generic;
using Grove.Domain.Energy;
using Grove.Domain.Orders;
using Grove.Domain.Producer;

namespace Grove.Domain.Merge
{
    /// <summary>Parsed production data: items, 3-merge recipes, garden crate, energy, orders 1–3.</summary>
    public sealed record LoadedCatalog(
        ItemCatalog Items,
        MergeCatalog Recipes,
        ProducerDefinition? GardenCrate,
        EnergyConfig Energy,
        IReadOnlyList<OrderSpec> Orders);
}
