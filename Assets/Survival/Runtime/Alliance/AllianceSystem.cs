using Survival.Domain.Catalog;
using Survival.Domain.Ids;

namespace Survival.Domain.Alliance
{
    /// <summary>Session-level alliance state. Membership comes from the alliance server (<see cref="AllianceService"/>).</summary>
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

        /// <summary>Server role 1-5 (R5 leader). 0 = not in an alliance.</summary>
        public int Role { get; private set; }

        /// <summary>Mirror the server's answer.</summary>
        public void Apply(AllianceService service)
        {
            Joined = service.InAlliance;
            Role = Joined ? service.MyRole : 0;
            RankId = SurvIds.AllianceRankMember;
        }
    }
}
