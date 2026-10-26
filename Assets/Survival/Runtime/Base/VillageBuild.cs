#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Economy;

namespace Survival.Domain.Base
{
    /// <summary>
    /// One thing the player can Build in the village (base_economy.json "buildables"): what it costs, how long it
    /// takes, and how many extra of it the Castle allows. <see cref="UnlockAtCastle"/> lists, per extra copy, the
    /// Castle level that allows it (e.g. [2, 4, 7, 11] = 1st extra Farm at Castle Lv 2, 2nd at Lv 4, ...).
    /// Each further copy of the same type costs <see cref="CostGrowth"/>x more and takes <see cref="TimeGrowth"/>x longer.
    /// </summary>
    public sealed record BuildableDef(
        string Type,
        string BuildingId,
        string? Produces,
        IReadOnlyDictionary<string, int> BuildCost,
        int BuildSeconds,
        IReadOnlyList<int> UnlockAtCastle,
        IReadOnlyList<BuildingLevelDef> Levels,
        double CostGrowth = 1.6,
        double TimeGrowth = 1.5)
    {
        /// <summary>How many of this type the player may have built at <paramref name="castleLevel"/>.</summary>
        public int MaxCount(int castleLevel)
        {
            var n = 0;
            foreach (var lv in UnlockAtCastle)
            {
                if (lv <= castleLevel)
                {
                    n++;
                }
            }

            return n;
        }

        /// <summary>Castle level that allows one more of this type than <paramref name="castleLevel"/> does, or null.</summary>
        public int? NextUnlockCastle(int castleLevel)
        {
            int? best = null;
            foreach (var lv in UnlockAtCastle)
            {
                if (lv > castleLevel && (best == null || lv < best))
                {
                    best = lv;
                }
            }

            return best;
        }

        /// <summary>Cost of the next copy when <paramref name="alreadyBuilt"/> exist (rounded to 10s).</summary>
        public IReadOnlyDictionary<string, int> CostFor(int alreadyBuilt)
        {
            var k = Math.Pow(CostGrowth, Math.Max(0, alreadyBuilt));
            var map = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var pair in BuildCost)
            {
                map[pair.Key] = (int)(Math.Round(pair.Value * k / 10.0) * 10);
            }

            return map;
        }

        public int SecondsFor(int alreadyBuilt) =>
            (int)Math.Round(BuildSeconds * Math.Pow(TimeGrowth, Math.Max(0, alreadyBuilt)));
    }

    public enum BuildBlock
    {
        None,
        Unknown,
        Locked,
        MaxCount,
        NoSpot,
        SpotTaken,
        QueueBusy,
        Cost
    }

    /// <summary>
    /// Build: place a new Farm / Lumber Mill / Quarry / Forge / Gold Mine on a free village spot. Spends the build cost, then
    /// runs the construction on the shared build queue (same timer id as an upgrade, so free finish / gem rush /
    /// speed-ups work unchanged). The new building opens at Lv 1 and upgrades like any other, through every level its
    /// data lists (<see cref="MaxLevel"/> for Farm / Lumber Mill / Quarry / Forge, 10 for the Gold Mine) and never past the Castle level.
    /// </summary>
    public sealed class VillageBuild
    {
        /// <summary>Top level of the Farm / Lumber Mill / Quarry / Forge (their data lists 6). A type's own top level is its level count.</summary>
        public const int MaxLevel = 6;

        private readonly BaseEconomy _base;
        private readonly Dictionary<string, BuildableDef> _types = new(StringComparer.Ordinal);
        private readonly List<BuildableDef> _order = new();
        private readonly List<string> _spots = new();
        private readonly Dictionary<string, string> _spotOfPlot = new(StringComparer.Ordinal);
        private int _next = 1;

        public VillageBuild(BaseEconomy economy, IReadOnlyList<BuildableDef> types, IReadOnlyList<string>? spotIds = null)
        {
            _base = economy ?? throw new ArgumentNullException(nameof(economy));
            foreach (var t in types)
            {
                if (t.Levels.Count == 0)
                {
                    throw new ArgumentException($"Buildable '{t.Type}' has no levels.");
                }

                _types[t.Type] = t;
                _order.Add(t);
            }

            if (spotIds != null)
            {
                UseSpots(spotIds);
            }
        }

        /// <summary>Build menu order.</summary>
        public IReadOnlyList<BuildableDef> Types => _order;

        public IReadOnlyList<string> Spots => _spots;

        /// <summary>plot id -> spot id for every building placed with Build.</summary>
        public IReadOnlyDictionary<string, string> Placed => _spotOfPlot;

        /// <summary>(new plot, spot id) right after a placement (construction timer running).</summary>
        public event Action<BasePlot, string>? Built;

        /// <summary>The village's free spots (from village_layout.json build_spots), in auto-place order.</summary>
        public void UseSpots(IReadOnlyList<string> spotIds)
        {
            _spots.Clear();
            _spots.AddRange(spotIds);
        }

        public BuildableDef? Type(string type) => _types.TryGetValue(type, out var t) ? t : null;

        public string? SpotOf(string plotId) => _spotOfPlot.TryGetValue(plotId, out var s) ? s : null;

        public string? PlotAt(string spotId)
        {
            foreach (var pair in _spotOfPlot)
            {
                if (string.Equals(pair.Value, spotId, StringComparison.Ordinal))
                {
                    return pair.Key;
                }
            }

            return null;
        }

        public bool IsFree(string spotId) => _spots.Contains(spotId) && PlotAt(spotId) == null;

        public IEnumerable<string> FreeSpots
        {
            get
            {
                foreach (var s in _spots)
                {
                    if (PlotAt(s) == null)
                    {
                        yield return s;
                    }
                }
            }
        }

        /// <summary>Where a placement goes if the player doesn't tap a spot.</summary>
        public string? NextFreeSpot
        {
            get
            {
                foreach (var s in FreeSpots)
                {
                    return s;
                }

                return null;
            }
        }

        /// <summary>Buildings of <paramref name="type"/> placed with Build (starting buildings don't count).</summary>
        public int CountOf(string type)
        {
            var n = 0;
            foreach (var p in _base.Plots)
            {
                if (string.Equals(p.Def.BuildType, type, StringComparison.Ordinal))
                {
                    n++;
                }
            }

            return n;
        }

        public int MaxCount(string type) => Type(type)?.MaxCount(_base.HqLevel) ?? 0;

        public IReadOnlyDictionary<string, int> CostOf(string type) =>
            Type(type)?.CostFor(CountOf(type)) ?? new Dictionary<string, int>();

        public int SecondsOf(string type) => Type(type)?.SecondsFor(CountOf(type)) ?? 0;

        public BuildBlock Check(string type, string? spotId = null)
        {
            var t = Type(type);
            if (t == null)
            {
                return BuildBlock.Unknown;
            }

            var max = t.MaxCount(_base.HqLevel);
            if (max <= 0)
            {
                return BuildBlock.Locked;
            }

            if (CountOf(type) >= max)
            {
                return BuildBlock.MaxCount;
            }

            if (spotId != null)
            {
                if (!_spots.Contains(spotId))
                {
                    return BuildBlock.NoSpot;
                }

                if (PlotAt(spotId) != null)
                {
                    return BuildBlock.SpotTaken;
                }
            }
            else if (NextFreeSpot == null)
            {
                return BuildBlock.NoSpot;
            }

            if (!_base.Timers.HasFreeSlot)
            {
                return BuildBlock.QueueBusy;
            }

            return _base.Wallet.CanAfford(CostOf(type)) ? BuildBlock.None : BuildBlock.Cost;
        }

        /// <summary>Place <paramref name="type"/> on <paramref name="spotId"/> (null = next free spot). Returns the new plot or null.</summary>
        public BasePlot? TryBuild(string type, string? spotId = null)
        {
            if (Check(type, spotId) != BuildBlock.None)
            {
                return null;
            }

            var t = Type(type)!;
            var spot = spotId ?? NextFreeSpot!;
            var cost = CostOf(type);
            var seconds = SecondsOf(type);
            if (!_base.Wallet.TrySpend(cost))
            {
                return null;
            }

            var def = DefFor(t, NewPlotId(type));
            if (!_base.Timers.TryStart(BaseEconomy.UpgradeTimerId(def.Id), def.Id, seconds, out _))
            {
                _base.Wallet.Grant(cost);
                return null;
            }

            var plot = _base.AddPlot(def, underConstruction: true);
            _spotOfPlot[def.Id] = spot;
            Built?.Invoke(plot, spot);
            return plot;
        }

        /// <summary>Save restore: put a built plot back on its spot at its level (no cost, no events from Build).</summary>
        public BasePlot? Restore(string plotId, string type, string spotId, int level, bool underConstruction)
        {
            var t = Type(type);
            if (t == null || _base.Find(plotId) != null || (PlotAt(spotId) != null))
            {
                return null;
            }

            var plot = _base.AddPlot(DefFor(t, plotId), underConstruction);
            _base.RestoreLevel(plotId, level, underConstruction);
            _spotOfPlot[plotId] = spotId;
            BumpNext(plotId);
            return plot;
        }

        private string NewPlotId(string type)
        {
            while (true)
            {
                var id = $"base.plot.{type}_b{_next:00}";
                _next++;
                if (_base.Find(id) == null)
                {
                    return id;
                }
            }
        }

        private void BumpNext(string plotId)
        {
            var i = plotId.LastIndexOf("_b", StringComparison.Ordinal);
            if (i >= 0 && int.TryParse(plotId.Substring(i + 2), out var n) && n >= _next)
            {
                _next = n + 1;
            }
        }

        private static BasePlotDef DefFor(BuildableDef t, string plotId)
        {
            var levels = new List<BuildingLevelDef>();
            for (var i = 0; i < t.Levels.Count; i++)
            {
                levels.Add(t.Levels[i]);
            }

            return new BasePlotDef(plotId, t.BuildingId, t.Produces, 1, 0, levels, t.Type);
        }
    }
}
