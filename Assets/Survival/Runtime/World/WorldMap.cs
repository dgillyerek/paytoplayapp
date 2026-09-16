using System;
using System.Collections.Generic;
using Survival.Domain.Catalog;
using Survival.Domain.Ids;
using Survival.Domain.Theme;

namespace Survival.Domain.World
{
    public sealed class WorldNodeState
    {
        public WorldNodeState(WorldNodeDef def, WorldPin pin)
        {
            Def = def ?? throw new ArgumentNullException(nameof(def));
            Pin = pin ?? throw new ArgumentNullException(nameof(pin));
        }

        public WorldNodeDef Def { get; }
        public WorldPin Pin { get; }
        public string Id => Def.Id;
        public string NodeTypeId => Def.NodeTypeId;
    }

    /// <summary>Crusade map graph for the SURV-P0 slice: six node types, six instance nodes.</summary>
    public sealed class WorldMap
    {
        private readonly Dictionary<string, WorldNodeState> _byId;
        private readonly List<WorldNodeState> _nodes;

        public WorldMap(SurvivalCatalog catalog, ThemePackBinder pack)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (pack == null)
            {
                throw new ArgumentNullException(nameof(pack));
            }

            MapId = SurvIds.WorldMapCampaign;
            _nodes = new List<WorldNodeState>(catalog.WorldNodes.Count);
            _byId = new Dictionary<string, WorldNodeState>(catalog.WorldNodes.Count, StringComparer.Ordinal);
            var pins = new Dictionary<string, WorldPin>(StringComparer.Ordinal);
            for (var i = 0; i < pack.Pins.Count; i++)
            {
                pins[pack.Pins[i].NodeId] = pack.Pins[i];
            }

            foreach (var def in catalog.WorldNodes)
            {
                if (!pins.TryGetValue(def.Id, out var pin))
                {
                    throw new InvalidOperationException($"ThemePack missing pin for '{def.Id}'.");
                }

                if (!string.Equals(pin.NodeTypeId, def.NodeTypeId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Pin type mismatch for '{def.Id}'.");
                }

                var state = new WorldNodeState(def, pin);
                _nodes.Add(state);
                _byId[def.Id] = state;
            }

            if (_nodes.Count != SurvIds.WorldNodeInstances.Count)
            {
                throw new InvalidOperationException("World map slice must include all six SURV-P0 instance nodes.");
            }
        }

        public string MapId { get; }

        public IReadOnlyList<WorldNodeState> Nodes => _nodes;

        public string? SelectedNodeId { get; private set; }

        public bool TryGet(string nodeId, out WorldNodeState node) => _byId.TryGetValue(nodeId, out node!);

        public bool Select(string nodeId)
        {
            if (!_byId.ContainsKey(nodeId))
            {
                return false;
            }

            SelectedNodeId = nodeId;
            return true;
        }
    }
}
