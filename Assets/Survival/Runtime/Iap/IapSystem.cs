using Survival.Domain.Catalog;

namespace Survival.Domain.Iap
{
    /// <summary>SKU class table only. Live StoreKit / Play Billing is out of scope for this scaffold.</summary>
    public sealed class IapSystem
    {
        public IapSystem(SurvivalCatalog catalog) => Skus = catalog.Skus;

        public System.Collections.Generic.IReadOnlyList<SkuDef> Skus { get; }
    }
}
