using System;
using System.Globalization;
using Survival.Domain.Catalog;
using Survival.Domain.Energy;
using Survival.Domain.Ids;

namespace Survival.Domain.World
{
    public readonly struct GatherMineResult
    {
        public GatherMineResult(
            bool ok,
            string nodeId,
            string marchActionId,
            string gatherActionId,
            string energyActionId,
            int yield,
            int availableAfter)
        {
            Ok = ok;
            NodeId = nodeId;
            MarchActionId = marchActionId;
            GatherActionId = gatherActionId;
            EnergyActionId = energyActionId;
            Yield = yield;
            AvailableAfter = availableAfter;
        }

        public bool Ok { get; }
        public string NodeId { get; }
        public string MarchActionId { get; }
        public string GatherActionId { get; }
        public string EnergyActionId { get; }
        public int Yield { get; }
        public int AvailableAfter { get; }

        public static GatherMineResult Fail { get; } = new(false, "", "", "", "", 0, 0);
    }

    /// <summary>
    /// Slice gather at <see cref="SurvIds.WorldNodeGather01"/>: march (world.action.march)
    /// then gather (world.action.gather), spending energy.action.gather.
    /// </summary>
    public sealed class GatherLoop
    {
        public GatherLoop(WorldNodeDef def)
        {
            NodeId = def.Id;
            NodeTypeId = def.NodeTypeId;
            Available = def.Available;
            YieldPerAction = def.YieldPerAction;
            MarchSeconds = def.MarchSeconds;
        }

        public string NodeId { get; }
        public string NodeTypeId { get; }
        public int Available { get; private set; }
        public int YieldPerAction { get; }
        public int MarchSeconds { get; }
        public bool MarchPending { get; private set; }
        public string? FromNodeId { get; private set; }

        public bool TryBeginMine(EnergySystem energy, string fromNodeId, out GatherMineResult result)
        {
            result = GatherMineResult.Fail;
            if (MarchPending
                || !string.Equals(NodeTypeId, SurvIds.WorldNodeTypeGather, StringComparison.Ordinal)
                || YieldPerAction <= 0
                || Available < YieldPerAction)
            {
                return false;
            }

            if (!energy.TrySpend(SurvIds.EnergyActionGather))
            {
                return false;
            }

            MarchPending = true;
            FromNodeId = fromNodeId;
            result = new GatherMineResult(
                true,
                NodeId,
                SurvIds.WorldActionMarch,
                SurvIds.WorldActionGather,
                SurvIds.EnergyActionGather,
                YieldPerAction,
                Available);
            return true;
        }

        public GatherMineResult CompleteArrive()
        {
            if (!MarchPending)
            {
                return GatherMineResult.Fail;
            }

            Available -= YieldPerAction;
            MarchPending = false;
            return new GatherMineResult(
                true,
                NodeId,
                SurvIds.WorldActionMarch,
                SurvIds.WorldActionGather,
                SurvIds.EnergyActionGather,
                YieldPerAction,
                Available);
        }

        public static string FormatClock(int seconds)
        {
            if (seconds < 0)
            {
                seconds = 0;
            }

            var m = seconds / 60;
            var s = seconds % 60;
            return m.ToString(CultureInfo.InvariantCulture) + "m " + s.ToString(CultureInfo.InvariantCulture) + "s";
        }
    }
}
