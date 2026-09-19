using System;
using System.Collections.Generic;
using Survival.Domain.Catalog;
using Survival.Domain.Ids;

namespace Survival.Domain.Base
{
    public sealed class BuildQueueItem
    {
        public BuildQueueItem(string queueId, string buildingId, int remainingSeconds)
        {
            QueueId = queueId;
            BuildingId = buildingId;
            RemainingSeconds = remainingSeconds;
        }

        public string QueueId { get; }
        public string BuildingId { get; }
        public int RemainingSeconds { get; set; }
    }

    public sealed class BaseSystem
    {
        private readonly List<BuildQueueItem> _queue = new();

        public BaseSystem(SurvivalCatalog catalog)
        {
            Buildings = catalog.Buildings;
        }

        public IReadOnlyList<BuildingDef> Buildings { get; }
        public IReadOnlyList<BuildQueueItem> Queue => _queue;

        public void EnqueueBuild(string buildingId, int seconds)
        {
            _queue.Add(new BuildQueueItem(SurvIds.BaseQueueBuild, buildingId, seconds));
        }
    }
}
