using Survival.Domain.Catalog;
using Survival.Domain.Ids;

namespace Survival.Domain.Battles
{
    public sealed class BattleSystem
    {
        public BattleSystem(SurvivalCatalog catalog) => Ids = catalog.BattleIds;

        public System.Collections.Generic.IReadOnlyList<string> Ids { get; }
        public string? LastResultId { get; private set; }

        public string StubResolvePve()
        {
            LastResultId = SurvIds.BattleResultWin;
            return LastResultId;
        }
    }
}
