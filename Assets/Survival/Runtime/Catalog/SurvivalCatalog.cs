using System;
using System.Collections.Generic;
using Survival.Domain.Ids;

namespace Survival.Domain.Catalog
{
    public sealed record BuildingDef(string Id, IReadOnlyList<string> Requires);

    public sealed record ResearchDef(string Id, string Branch, IReadOnlyList<string> Requires);

    public sealed record HeroDef(
        string SlotId,
        string ArchetypeId,
        string RarityId,
        IReadOnlyDictionary<string, int> Stats);

    public sealed record WorldNodeTypeDef(string Id, string DesignRole);

    public sealed record WorldNodeDef(string Id, string NodeTypeId);

    public sealed record QueueRow(string QueueId, string ActionId, string TargetNodeId, int RemainingSeconds);

    public sealed record EventDef(string TypeId);

    public sealed record SkuDef(string Id);

    public sealed record EnergyConfig(
        string MeterId,
        int Cap,
        int RegenSecondsPerPoint,
        IReadOnlyDictionary<string, int> ActionCosts)
    {
        public static EnergyConfig SliceDefault { get; } = new(
            SurvIds.EnergyMeterMain,
            Cap: 120,
            RegenSecondsPerPoint: 90,
            ActionCosts: new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [SurvIds.EnergyActionMarch] = 5,
                [SurvIds.EnergyActionBattle] = 10,
                [SurvIds.EnergyActionGather] = 4,
                [SurvIds.EnergyActionBuildRush] = 8
            });
    }

    public sealed class SurvivalCatalog
    {
        public SurvivalCatalog(
            string systemsApi,
            IReadOnlyList<string> modules,
            IReadOnlyList<string> resources,
            IReadOnlyList<BuildingDef> buildings,
            IReadOnlyList<ResearchDef> research,
            IReadOnlyList<HeroDef> heroes,
            IReadOnlyList<string> allianceIds,
            IReadOnlyList<WorldNodeTypeDef> worldNodeTypes,
            IReadOnlyList<WorldNodeDef> worldNodes,
            IReadOnlyList<string> battleIds,
            EnergyConfig energy,
            IReadOnlyList<EventDef> events,
            IReadOnlyList<SkuDef> skus,
            IReadOnlyList<string> ftueSteps)
        {
            SystemsApi = systemsApi ?? throw new ArgumentNullException(nameof(systemsApi));
            Modules = modules ?? throw new ArgumentNullException(nameof(modules));
            Resources = resources ?? throw new ArgumentNullException(nameof(resources));
            Buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
            Research = research ?? throw new ArgumentNullException(nameof(research));
            Heroes = heroes ?? throw new ArgumentNullException(nameof(heroes));
            AllianceIds = allianceIds ?? throw new ArgumentNullException(nameof(allianceIds));
            WorldNodeTypes = worldNodeTypes ?? throw new ArgumentNullException(nameof(worldNodeTypes));
            WorldNodes = worldNodes ?? throw new ArgumentNullException(nameof(worldNodes));
            BattleIds = battleIds ?? throw new ArgumentNullException(nameof(battleIds));
            Energy = energy ?? throw new ArgumentNullException(nameof(energy));
            Events = events ?? throw new ArgumentNullException(nameof(events));
            Skus = skus ?? throw new ArgumentNullException(nameof(skus));
            FtueSteps = ftueSteps ?? throw new ArgumentNullException(nameof(ftueSteps));
        }

        public string SystemsApi { get; }
        public IReadOnlyList<string> Modules { get; }
        public IReadOnlyList<string> Resources { get; }
        public IReadOnlyList<BuildingDef> Buildings { get; }
        public IReadOnlyList<ResearchDef> Research { get; }
        public IReadOnlyList<HeroDef> Heroes { get; }
        public IReadOnlyList<string> AllianceIds { get; }
        public IReadOnlyList<WorldNodeTypeDef> WorldNodeTypes { get; }
        public IReadOnlyList<WorldNodeDef> WorldNodes { get; }
        public IReadOnlyList<string> BattleIds { get; }
        public EnergyConfig Energy { get; }
        public IReadOnlyList<EventDef> Events { get; }
        public IReadOnlyList<SkuDef> Skus { get; }
        public IReadOnlyList<string> FtueSteps { get; }
    }
}
