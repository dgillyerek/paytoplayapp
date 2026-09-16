using System;
using System.Globalization;
using Survival.Domain.Catalog;
using Survival.Domain.Energy;
using Survival.Domain.Ids;

namespace Survival.Domain.World
{
    public readonly struct MarchResolveResult
    {
        public MarchResolveResult(
            bool ok,
            string nodeId,
            string marchActionId,
            string resolveActionId,
            string energyActionId,
            string chipKey,
            int yield,
            int availableAfter)
        {
            Ok = ok;
            NodeId = nodeId;
            MarchActionId = marchActionId;
            ResolveActionId = resolveActionId;
            EnergyActionId = energyActionId;
            ChipKey = chipKey;
            Yield = yield;
            AvailableAfter = availableAfter;
        }

        public bool Ok { get; }
        public string NodeId { get; }
        public string MarchActionId { get; }
        public string ResolveActionId { get; }
        public string EnergyActionId { get; }
        public string ChipKey { get; }
        public int Yield { get; }
        public int AvailableAfter { get; }

        public static MarchResolveResult Fail { get; } = new(false, "", "", "", "", "", 0, 0);
    }

    /// <summary>
    /// Shared World march → resolve for playable nodes (Gather stock, Fight/Explore attempts).
    /// March is always <see cref="SurvIds.WorldActionMarch"/>.
    /// </summary>
    public sealed class WorldMarchLoop
    {
        public WorldMarchLoop(
            WorldNodeDef def,
            string resolveActionId,
            string energyActionId,
            string chipKey,
            bool consumeYieldFromStock)
        {
            NodeId = def.Id;
            NodeTypeId = def.NodeTypeId;
            Available = def.Available;
            YieldPerAction = def.YieldPerAction;
            MarchSeconds = def.MarchSeconds;
            ResolveActionId = resolveActionId;
            EnergyActionId = energyActionId;
            ChipKey = chipKey;
            ConsumeYieldFromStock = consumeYieldFromStock;
        }

        public static WorldMarchLoop? TryCreate(WorldNodeDef def)
        {
            if (string.Equals(def.NodeTypeId, SurvIds.WorldNodeTypeGather, StringComparison.Ordinal))
            {
                return new WorldMarchLoop(
                    def,
                    SurvIds.WorldActionGather,
                    SurvIds.EnergyActionGather,
                    ChipWallet.StoneChip,
                    consumeYieldFromStock: true);
            }

            if (string.Equals(def.NodeTypeId, SurvIds.WorldNodeTypeFight, StringComparison.Ordinal))
            {
                return new WorldMarchLoop(
                    def,
                    SurvIds.BattleActionStart,
                    SurvIds.EnergyActionBattle,
                    ChipWallet.SoftChip,
                    consumeYieldFromStock: false);
            }

            if (string.Equals(def.NodeTypeId, SurvIds.WorldNodeTypeExplore, StringComparison.Ordinal))
            {
                return new WorldMarchLoop(
                    def,
                    SurvIds.WorldActionScout,
                    SurvIds.EnergyActionMarch,
                    ChipWallet.SoftChip,
                    consumeYieldFromStock: false);
            }

            return null;
        }

        public string NodeId { get; }
        public string NodeTypeId { get; }
        public int Available { get; private set; }
        public int YieldPerAction { get; }
        public int MarchSeconds { get; }
        public string ResolveActionId { get; }
        public string EnergyActionId { get; }
        public string ChipKey { get; }
        public bool ConsumeYieldFromStock { get; }
        public bool MarchPending { get; private set; }
        public string? FromNodeId { get; private set; }

        public bool TryBegin(EnergySystem energy, string fromNodeId, out MarchResolveResult result)
        {
            result = MarchResolveResult.Fail;
            if (MarchPending || YieldPerAction <= 0)
            {
                return false;
            }

            if (ConsumeYieldFromStock)
            {
                if (Available < YieldPerAction)
                {
                    return false;
                }
            }
            else if (Available <= 0)
            {
                return false;
            }

            if (!energy.TrySpend(EnergyActionId))
            {
                return false;
            }

            MarchPending = true;
            FromNodeId = fromNodeId;
            result = Snapshot(Available);
            return true;
        }

        public MarchResolveResult CompleteArrive()
        {
            if (!MarchPending)
            {
                return MarchResolveResult.Fail;
            }

            Available -= ConsumeYieldFromStock ? YieldPerAction : 1;
            if (Available < 0)
            {
                Available = 0;
            }

            MarchPending = false;
            return Snapshot(Available);
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

        public static int RemainingSeconds(int marchSeconds, float elapsedSeconds)
        {
            if (marchSeconds < 0)
            {
                marchSeconds = 0;
            }

            if (elapsedSeconds < 0f)
            {
                elapsedSeconds = 0f;
            }

            var left = marchSeconds - (int)Math.Floor(elapsedSeconds);
            return left > 0 ? left : 0;
        }

        public static bool TimedLegComplete(int marchSeconds, float elapsedSeconds) =>
            elapsedSeconds + 0.0001f >= marchSeconds;

        private MarchResolveResult Snapshot(int availableAfter) =>
            new(
                true,
                NodeId,
                SurvIds.WorldActionMarch,
                ResolveActionId,
                EnergyActionId,
                ChipKey,
                YieldPerAction,
                availableAfter);
    }
}
