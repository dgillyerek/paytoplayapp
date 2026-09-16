using Survival.Domain.Catalog;
using Survival.Domain.Ids;

namespace Survival.Domain.Heroes
{
    public sealed class HeroSystem
    {
        public HeroSystem(SurvivalCatalog catalog)
        {
            Roster = catalog.Heroes;
            SelectedSlotId = SurvIds.HeroSlot01;
        }

        public System.Collections.Generic.IReadOnlyList<HeroDef> Roster { get; }
        public string SelectedSlotId { get; }
    }
}
