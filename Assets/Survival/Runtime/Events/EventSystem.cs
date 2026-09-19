using Survival.Domain.Catalog;

namespace Survival.Domain.Events
{
    public sealed class EventSystem
    {
        public EventSystem(SurvivalCatalog catalog) => Events = catalog.Events;

        public System.Collections.Generic.IReadOnlyList<EventDef> Events { get; }
    }
}
