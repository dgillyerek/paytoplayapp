using Survival.Domain.Catalog;
using Survival.Domain.Ids;

namespace Survival.Domain.Ftue
{
    public sealed class FtueSystem
    {
        public FtueSystem(SurvivalCatalog catalog)
        {
            Steps = catalog.FtueSteps;
            CurrentStepId = SurvIds.FtueStepBoot;
        }

        public System.Collections.Generic.IReadOnlyList<string> Steps { get; }
        public string CurrentStepId { get; private set; }

        public void AdvanceTo(string stepId) => CurrentStepId = stepId;
    }
}
