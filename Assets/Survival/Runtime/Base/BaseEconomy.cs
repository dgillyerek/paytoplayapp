#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Clock;
using Survival.Domain.Economy;
using Survival.Domain.Ids;

namespace Survival.Domain.Base
{
    public sealed record BuildingLevelDef(
        int Level,
        int PerHour,
        int Capacity,
        int MinCollect,
        IReadOnlyDictionary<string, int> UpgradeCost,
        int UpgradeSeconds);

    /// <summary>
    /// A placed building on the base: an instance of a SURV-P0 building id (Farm = building.economy producing res.food).
    /// <paramref name="BuildType"/> is set for buildings the player placed with Build (farm, lumber, quarry, forge).
    /// </summary>
    public sealed record BasePlotDef(
        string Id,
        string BuildingId,
        string? Produces,
        int StartLevel,
        double StartFill,
        IReadOnlyList<BuildingLevelDef> Levels,
        string? BuildType = null);

    public sealed class BasePlot
    {
        internal BasePlot(BasePlotDef def, double now)
        {
            Def = def;
            Level = Math.Max(1, Math.Min(def.StartLevel, def.Levels.Count));
            LastTick = now;
            Stored = Current.Capacity * Math.Max(0, Math.Min(1, def.StartFill));
        }

        public BasePlotDef Def { get; }
        public string Id => Def.Id;
        public int Level { get; internal set; }

        /// <summary>Placed with Build and still on its construction timer: no production, no upgrades.</summary>
        public bool UnderConstruction { get; internal set; }
        public double Stored { get; internal set; }
        internal double LastTick { get; set; }

        public BuildingLevelDef Current => Def.Levels[Level - 1];
        public BuildingLevelDef? Next => Level < Def.Levels.Count ? Def.Levels[Level] : null;
        public bool IsProducer => !UnderConstruction && !string.IsNullOrEmpty(Def.Produces) && Current.PerHour > 0;
        public int Collectable => (int)Math.Floor(Stored + 1e-9);
        public bool CanCollect => IsProducer && Collectable >= Math.Max(1, Current.MinCollect);
        public bool IsFull => IsProducer && Stored >= Current.Capacity - 1e-9;

        /// <summary>0..1 storage fill, for the bubble.</summary>
        public double Fill => Current.Capacity <= 0 ? 0 : Math.Min(1, Stored / Current.Capacity);

        /// <summary>Upgrade cost read off the CURRENT level row (cost to go Lv n → n+1).</summary>
        public IReadOnlyDictionary<string, int> UpgradeCost => Current.UpgradeCost;
    }

    public enum UpgradeBlock
    {
        None,
        MaxLevel,
        NeedsCastle,
        QueueBusy,
        AlreadyUpgrading,
        Cost
    }

    /// <summary>Castle / Farm / Lumber Mill: production into storage, tap-to-collect, and timed upgrades through the shared queue.</summary>
    public sealed class BaseEconomy
    {
        private readonly List<BasePlot> _plots = new();

        public BaseEconomy(IReadOnlyList<BasePlotDef> plots, IGameClock clock, Wallet wallet, TimerQueue timers)
        {
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Timers = timers ?? throw new ArgumentNullException(nameof(timers));
            var now = clock.Now;
            foreach (var def in plots)
            {
                if (def.Levels.Count == 0)
                {
                    throw new ArgumentException($"Plot '{def.Id}' has no levels.");
                }

                _plots.Add(new BasePlot(def, now));
            }
        }

        public IGameClock Clock { get; }
        public Wallet Wallet { get; }
        public TimerQueue Timers { get; }
        public IReadOnlyList<BasePlot> Plots => _plots;

        /// <summary>(plot, resource, amount) after a tap-collect.</summary>
        public event Action<BasePlot, string, int>? Collected;

        /// <summary>(plot, new level) after an upgrade finishes (also level 1 when a construction finishes).</summary>
        public event Action<BasePlot, int>? LevelReached;

        /// <summary>A Build construction finished: the new building is standing at Lv 1.</summary>
        public event Action<BasePlot>? Constructed;

        /// <summary>A plot was added at runtime (Build placement or save restore).</summary>
        public event Action<BasePlot>? PlotAdded;

        /// <summary>
        /// Add a plot at runtime. <paramref name="underConstruction"/> plots wait on
        /// <see cref="UpgradeTimerId"/> (the caller starts it); finishing it leaves them at Lv 1.
        /// </summary>
        public BasePlot AddPlot(BasePlotDef def, bool underConstruction)
        {
            if (def.Levels.Count == 0)
            {
                throw new ArgumentException($"Plot '{def.Id}' has no levels.");
            }

            if (Find(def.Id) != null)
            {
                throw new ArgumentException($"Plot '{def.Id}' already exists.");
            }

            var p = new BasePlot(def, Clock.Now) { UnderConstruction = underConstruction };
            if (underConstruction)
            {
                p.Stored = 0;
            }

            _plots.Add(p);
            PlotAdded?.Invoke(p);
            return p;
        }

        /// <summary>Save restore: set a plot's level (clamped) without events.</summary>
        public bool RestoreLevel(string plotId, int level, bool underConstruction = false)
        {
            var p = Find(plotId);
            if (p == null)
            {
                return false;
            }

            p.Level = Math.Max(1, Math.Min(level, p.Def.Levels.Count));
            p.UnderConstruction = underConstruction;
            p.Stored = Math.Min(p.Stored, p.Current.Capacity);
            return true;
        }

        public BasePlot? Find(string plotId)
        {
            foreach (var p in _plots)
            {
                if (string.Equals(p.Id, plotId, StringComparison.Ordinal))
                {
                    return p;
                }
            }

            return null;
        }

        public BasePlot? Hq
        {
            get
            {
                foreach (var p in _plots)
                {
                    if (string.Equals(p.Def.BuildingId, SurvIds.BuildingHq, StringComparison.Ordinal))
                    {
                        return p;
                    }
                }

                return null;
            }
        }

        public int HqLevel => Hq?.Level ?? 1;

        /// <summary>Highest level of any plot built from <paramref name="buildingId"/>.</summary>
        public int LevelOf(string buildingId)
        {
            var best = 0;
            foreach (var p in _plots)
            {
                if (!p.UnderConstruction && string.Equals(p.Def.BuildingId, buildingId, StringComparison.Ordinal) && p.Level > best)
                {
                    best = p.Level;
                }
            }

            return best;
        }

        public static string UpgradeTimerId(string plotId) => "base.timer." + plotId;

        public void Tick()
        {
            var now = Clock.Now;
            foreach (var p in _plots)
            {
                var dt = now - p.LastTick;
                p.LastTick = now;
                if (dt <= 0 || !p.IsProducer)
                {
                    continue;
                }

                p.Stored = Math.Min(p.Current.Capacity, p.Stored + p.Current.PerHour / 3600.0 * dt);
            }
        }

        /// <summary>Tap-to-collect. Returns the amount moved into the wallet (0 below the minimum).</summary>
        public int Collect(string plotId)
        {
            Tick();
            var p = Find(plotId);
            if (p == null || !p.CanCollect)
            {
                return 0;
            }

            var amount = p.Collectable;
            p.Stored -= amount;
            Wallet.Add(p.Def.Produces!, amount);
            Collected?.Invoke(p, p.Def.Produces!, amount);
            return amount;
        }

        public bool IsUpgrading(string plotId) => Timers.Find(UpgradeTimerId(plotId)) != null;

        public UpgradeBlock CheckUpgrade(string plotId)
        {
            var p = Find(plotId);
            if (p == null || p.Next == null)
            {
                return UpgradeBlock.MaxLevel;
            }

            if (IsUpgrading(plotId) || p.UnderConstruction)
            {
                return UpgradeBlock.AlreadyUpgrading;
            }

            var isHq = string.Equals(p.Def.BuildingId, SurvIds.BuildingHq, StringComparison.Ordinal);
            if (!isHq && p.Next.Level > HqLevel)
            {
                return UpgradeBlock.NeedsCastle;
            }

            if (!Timers.HasFreeSlot)
            {
                return UpgradeBlock.QueueBusy;
            }

            return Wallet.CanAfford(p.UpgradeCost) ? UpgradeBlock.None : UpgradeBlock.Cost;
        }

        public bool TryStartUpgrade(string plotId)
        {
            if (CheckUpgrade(plotId) != UpgradeBlock.None)
            {
                return false;
            }

            var p = Find(plotId)!;
            if (!Wallet.TrySpend(p.UpgradeCost))
            {
                return false;
            }

            if (!Timers.TryStart(UpgradeTimerId(plotId), plotId, p.Current.UpgradeSeconds, out _))
            {
                Wallet.Grant(p.UpgradeCost);
                return false;
            }

            return true;
        }

        /// <summary>Finish a done upgrade timer: level up (a construction just opens at Lv 1). Returns the new level, or 0.</summary>
        public int TryFinishUpgrade(string plotId)
        {
            var p = Find(plotId);
            if (p == null || !Timers.TryComplete(UpgradeTimerId(plotId)))
            {
                return 0;
            }

            Tick();
            if (p.UnderConstruction)
            {
                p.UnderConstruction = false;
                p.LastTick = Clock.Now;
                p.Stored = 0;
                Constructed?.Invoke(p);
                LevelReached?.Invoke(p, p.Level);
                return p.Level;
            }

            p.Level = Math.Min(p.Level + 1, p.Def.Levels.Count);
            LevelReached?.Invoke(p, p.Level);
            return p.Level;
        }
    }
}
