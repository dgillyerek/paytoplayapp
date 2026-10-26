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
    /// <paramref name="BuildType"/> is set for buildings the player placed with Build (farm, lumber, quarry, forge, mine).
    /// </summary>
    public sealed record BasePlotDef(
        string Id,
        string BuildingId,
        string? Produces,
        int StartLevel,
        double StartFill,
        IReadOnlyList<BuildingLevelDef> Levels,
        string? BuildType = null);

    /// <summary>A finished build / upgrade whose congratulations reward waits for its tap.</summary>
    public sealed record PendingCompletion(string PlotId, int Level, bool WasBuild, IReadOnlyDictionary<string, int> Rewards);

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
        /// <summary>When the plot last produced (clock seconds).</summary>
        public double LastTick { get; internal set; }

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

        /// <summary>
        /// Save restore: a producer's stock (e.g. Gold Mine gold not yet collected) and when it last produced. The next
        /// Tick produces from <paramref name="lastTick"/> to now, so a building keeps filling (up to its cap) while the
        /// game is closed. Clamped to 0..capacity; a future time is treated as now.
        /// </summary>
        public bool RestoreStock(string plotId, double stored, double lastTick)
        {
            var p = Find(plotId);
            if (p == null || !p.IsProducer)
            {
                return false;
            }

            p.Stored = Math.Max(0, Math.Min(p.Current.Capacity, stored));
            p.LastTick = Math.Min(lastTick, Clock.Now);
            return true;
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
            return Apply(p, Clock.Now);
        }

        /// <summary>
        /// (plot, new level, was a construction) for each build / upgrade that finished on its own because its timer
        /// ran out (<see cref="CompleteDueUpgrades"/>); <see cref="LevelReached"/> / <see cref="Constructed"/> fire too.
        /// </summary>
        public event Action<BasePlot, int, bool>? AutoCompleted;

        /// <summary>
        /// Every build / upgrade whose timer has run out completes now, no tap needed: the level applies (a construction
        /// opens at Lv 1) and production carries on at the new rate. Timers that ran out while the game was closed finish
        /// in the order they ended, each plot producing at its old rate up to its own end time. Returns the finished plots.
        /// </summary>
        public IReadOnlyList<BasePlot> CompleteDueUpgrades()
        {
            var now = Clock.Now;
            List<(BasePlot Plot, double EndsAt)>? due = null;
            foreach (var p in _plots)
            {
                var t = Timers.Find(UpgradeTimerId(p.Id));
                if (t != null && t.IsDone(now))
                {
                    (due ??= new List<(BasePlot, double)>()).Add((p, t.EndsAt));
                }
            }

            if (due == null)
            {
                return Array.Empty<BasePlot>();
            }

            due.Sort((a, b) => a.EndsAt.CompareTo(b.EndsAt));
            var done = new List<BasePlot>(due.Count);
            foreach (var (p, endsAt) in due)
            {
                if (!Timers.TryComplete(UpgradeTimerId(p.Id)))
                {
                    continue;
                }

                var at = Math.Min(now, endsAt);
                Advance(p, at);
                var wasBuild = p.UnderConstruction;
                var level = Apply(p, at);
                done.Add(p);
                AutoCompleted?.Invoke(p, level, wasBuild);
            }

            Tick();
            return done;
        }

        /// <summary>Produce at the plot's current rate up to <paramref name="until"/> (never backwards).</summary>
        private static void Advance(BasePlot p, double until)
        {
            var dt = until - p.LastTick;
            if (dt <= 0)
            {
                return;
            }

            p.LastTick = until;
            if (p.IsProducer)
            {
                p.Stored = Math.Min(p.Current.Capacity, p.Stored + p.Current.PerHour / 3600.0 * dt);
            }
        }

        private int Apply(BasePlot p, double at)
        {
            if (p.UnderConstruction)
            {
                p.UnderConstruction = false;
                p.LastTick = at;
                p.Stored = 0;
                AddPending(p.Id, p.Level, true, CompletionRewardsFor(p, true));
                Constructed?.Invoke(p);
                LevelReached?.Invoke(p, p.Level);
                return p.Level;
            }

            p.Level = Math.Min(p.Level + 1, p.Def.Levels.Count);
            AddPending(p.Id, p.Level, false, CompletionRewardsFor(p, false));
            LevelReached?.Invoke(p, p.Level);
            return p.Level;
        }

        // ---------- completion rewards (the congratulations tap) ----------

        /// <summary>Gold for a finished non-producer step: this share of the step's gold cost (at least <see cref="MinCompletionGold"/>).</summary>
        public const double CompletionGoldShare = 0.25;

        public const int MinCompletionGold = 50;

        private readonly Dictionary<string, PendingCompletion> _pending = new();

        /// <summary>Finished builds / upgrades whose congratulations reward is waiting for its tap (the bubble on the building).</summary>
        public IReadOnlyCollection<PendingCompletion> Pending => _pending.Values;

        public PendingCompletion? PendingFor(string plotId) => _pending.TryGetValue(plotId, out var c) ? c : null;

        /// <summary>A completion reward is now waiting on this plot.</summary>
        public event Action<PendingCompletion>? CompletionPending;

        /// <summary>
        /// The congratulations reward for a finished step: producers give one hour of the new level's output; the Castle
        /// and other non-producers give gold, a quarter of what the step cost (constructions: at least 50).
        /// </summary>
        public static IReadOnlyDictionary<string, int> CompletionRewardsFor(BasePlot p, bool wasBuild)
        {
            if (!string.IsNullOrEmpty(p.Def.Produces) && p.Current.PerHour > 0)
            {
                return new Dictionary<string, int> { [p.Def.Produces!] = Math.Max(1, p.Current.PerHour) };
            }

            var gold = 0;
            if (!wasBuild && p.Level >= 2 && p.Def.Levels[p.Level - 2].UpgradeCost.TryGetValue(SurvIds.ResSoft, out var paid))
            {
                gold = (int)(Math.Round(paid * CompletionGoldShare / 10.0, MidpointRounding.AwayFromZero) * 10);
            }

            return new Dictionary<string, int> { [SurvIds.ResSoft] = Math.Max(MinCompletionGold, gold) };
        }

        /// <summary>Tap the congratulations bubble: the waiting reward goes into the wallet. Null if nothing waits.</summary>
        public PendingCompletion? ClaimCompletion(string plotId)
        {
            if (!_pending.TryGetValue(plotId, out var c))
            {
                return null;
            }

            _pending.Remove(plotId);
            Wallet.Grant(c.Rewards);
            return c;
        }

        /// <summary>Save restore of a waiting reward (no event).</summary>
        public void RestorePending(string plotId, int level, bool wasBuild, IReadOnlyDictionary<string, int> rewards)
        {
            if (Find(plotId) != null && rewards.Count > 0)
            {
                _pending[plotId] = new PendingCompletion(plotId, level, wasBuild, rewards);
            }
        }

        private void AddPending(string plotId, int level, bool wasBuild, IReadOnlyDictionary<string, int> rewards)
        {
            // Two steps finishing before a tap stack into one bubble.
            if (_pending.TryGetValue(plotId, out var prior))
            {
                var sum = new Dictionary<string, int>(prior.Rewards.Count + rewards.Count);
                foreach (var kv in prior.Rewards)
                {
                    sum[kv.Key] = kv.Value;
                }

                foreach (var kv in rewards)
                {
                    sum[kv.Key] = (sum.TryGetValue(kv.Key, out var v) ? v : 0) + kv.Value;
                }

                rewards = sum;
                wasBuild |= prior.WasBuild;
            }

            var c = new PendingCompletion(plotId, level, wasBuild, rewards);
            _pending[plotId] = c;
            CompletionPending?.Invoke(c);
        }
    }
}
