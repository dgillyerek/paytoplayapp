using Survival.Domain.Catalog;
using Survival.Domain.Ids;

namespace Survival.Domain.Alliance
{
    public sealed class AllianceSystem
    {
        public AllianceSystem(SurvivalCatalog catalog)
        {
            Ids = catalog.AllianceIds;
            RankId = SurvIds.AllianceRankMember;
        }

        public System.Collections.Generic.IReadOnlyList<string> Ids { get; }
        public string RankId { get; private set; }
        public bool Joined { get; private set; }

        public void StubJoin()
        {
            Joined = true;
            RankId = SurvIds.AllianceRankMember;
        }
    }
}
