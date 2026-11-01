#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Economy;
using Survival.Domain.Ids;

namespace Survival.Domain.Clock
{
    /// <summary>Speed-up economics: gem rush price and the free-finish window.</summary>
    public sealed class RushRules
    {
        public RushRules(int freeFinishSeconds = 300, double gemsPerMinute = 1.0, int minGems = 1)
        {
            FreeFinishSeconds = Math.Max(0, freeFinishSeconds);
            GemsPerMinute = Math.Max(0, gemsPerMinute);
            MinGems = Math.Max(0, minGems);
        }

        public int FreeFinishSeconds { get; }
        public double GemsPerMinute { get; }
        public int MinGems { get; }

        public bool IsFree(double remainingSeconds) => remainingSeconds <= FreeFinishSeconds;

        /// <summary>
        /// The free window for one timer: its last <see cref="FreeFinishSeconds"/>, but never more than the last half of it,
        /// so a short build / training still shows a running countdown before it can be finished for free.
        /// </summary>
        public double FreeWindow(double durationSeconds) => Math.Min(FreeFinishSeconds, Math.Max(0, durationSeconds) * 0.5);

        public bool IsFree(double remainingSeconds, double durationSeconds) => remainingSeconds <= FreeWindow(durationSeconds);

        /// <summary>Gems to finish a timer of <paramref name="durationSeconds"/> now. 0 when done or inside its free window.</summary>
        public int GemCost(double remainingSeconds, double durationSeconds)
        {
            if (remainingSeconds <= 0 || IsFree(remainingSeconds, durationSeconds))
            {
                return 0;
            }

            var raw = (int)Math.Ceiling(remainingSeconds / 60.0 * GemsPerMinute - 1e-9);
            return Math.Max(MinGems, raw);
        }

        /// <summary>Gems to finish now. 0 when done or inside the free window.</summary>
        public int GemCost(double remainingSeconds)
        {
            if (remainingSeconds <= 0 || IsFree(remainingSeconds))
            {
                return 0;
            }

            var raw = (int)Math.Ceiling(remainingSeconds / 60.0 * GemsPerMinute - 1e-9);
            return Math.Max(MinGems, raw);
        }
    }

    public sealed class GameTimer
    {
        internal GameTimer(string id, string targetId, double startedAt, double durationSeconds)
        {
            Id = id;
            TargetId = targetId;
            StartedAt = startedAt;
            TotalSeconds = DurationSeconds = durationSeconds;
        }

        public string Id { get; }
        public string TargetId { get; }
        public double StartedAt { get; }
        public double DurationSeconds { get; private set; }

        /// <summary>Length when started; speed-ups shorten <see cref="DurationSeconds"/> but not this (the free window is half of it).</summary>
        public double TotalSeconds { get; }
        public double EndsAt => StartedAt + DurationSeconds;

        public double Remaining(double now) => Math.Max(0, EndsAt - now);

        public bool IsDone(double now) => now >= EndsAt;

        /// <summary>0..1 fill for progress bars.</summary>
        public double Progress(double now) =>
            DurationSeconds <= 0 ? 1 : Math.Min(1, Math.Max(0, (now - StartedAt) / DurationSeconds));

        internal void Shorten(double seconds) => DurationSeconds = Math.Max(0, DurationSeconds - seconds);

        internal void EndAt(double now) => DurationSeconds = Math.Max(0, now - StartedAt);
    }

    /// <summary>Build/upgrade queue with a fixed number of slots, gem rush, speed-up items and free finish.</summary>
    public sealed class TimerQueue
    {
        private readonly List<GameTimer> _timers = new();

        public TimerQueue(IGameClock clock, RushRules rules, int slots = 1)
        {
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            BaseSlots = Math.Max(1, slots);
        }

        public IGameClock Clock { get; }
        public RushRules Rules { get; }
        /// <summary>Permanent builders (catalog build slots).</summary>
        public int BaseSlots { get; }

        /// <summary>
        /// Clock time (epoch seconds) the hired temporary builder leaves; 0 = never hired. While <see cref="Clock"/> is
        /// before it there is one extra slot. A job the temporary builder started keeps running past the expiry and
        /// finishes normally; only new jobs need a free slot.
        /// </summary>
        public double ExtraSlotUntil { get; set; }

        public bool ExtraSlotActive => Clock.Now < ExtraSlotUntil;

        public double ExtraSlotRemaining => Math.Max(0, ExtraSlotUntil - Clock.Now);

        public int Slots => BaseSlots + (ExtraSlotActive ? 1 : 0);
        public IReadOnlyList<GameTimer> Active => _timers;
        public bool HasFreeSlot => _timers.Count < Slots;

        public GameTimer? Find(string id)
        {
            foreach (var t in _timers)
            {
                if (string.Equals(t.Id, id, StringComparison.Ordinal))
                {
                    return t;
                }
            }

            return null;
        }

        public bool TryStart(string id, string targetId, double seconds, out GameTimer? timer)
        {
            timer = null;
            if (!HasFreeSlot || Find(id) != null)
            {
                return false;
            }

            timer = new GameTimer(id, targetId, Clock.Now, Math.Max(0, seconds));
            _timers.Add(timer);
            return true;
        }

        /// <summary>Save restore: put a timer back exactly as it was (wall-clock start + duration), slots permitting.</summary>
        public bool Restore(string id, string targetId, double startedAt, double durationSeconds)
        {
            // A job the temporary builder started may still be running after it left: let it back in.
            var cap = BaseSlots + (ExtraSlotUntil > 0 ? 1 : 0);
            if (_timers.Count >= Math.Max(Slots, cap) || Find(id) != null)
            {
                return false;
            }

            _timers.Add(new GameTimer(id, targetId, startedAt, Math.Max(0, durationSeconds)));
            return true;
        }

        public double Remaining(string id) => Find(id)?.Remaining(Clock.Now) ?? 0;

        public bool IsDone(string id)
        {
            var t = Find(id);
            return t != null && t.IsDone(Clock.Now);
        }

        public int RushCost(string id)
        {
            var t = Find(id);
            return t == null ? 0 : Rules.GemCost(t.Remaining(Clock.Now), t.TotalSeconds);
        }

        public bool CanFinishFree(string id)
        {
            var t = Find(id);
            return t != null && !t.IsDone(Clock.Now) && Rules.IsFree(t.Remaining(Clock.Now), t.TotalSeconds);
        }

        /// <summary>Inside the free window: end the timer now for 0 gems.</summary>
        public bool TryFinishFree(string id)
        {
            if (!CanFinishFree(id))
            {
                return false;
            }

            Find(id)!.EndAt(Clock.Now);
            return true;
        }

        /// <summary>Spend gems (res.hard) to end the timer now. Free window costs 0.</summary>
        public bool TryRush(string id, Wallet wallet, out int gemsSpent)
        {
            gemsSpent = 0;
            var t = Find(id);
            if (t == null || t.IsDone(Clock.Now))
            {
                return false;
            }

            var cost = Rules.GemCost(t.Remaining(Clock.Now), t.TotalSeconds);
            if (cost > 0 && !wallet.TrySpend(SurvIds.ResHard, cost))
            {
                return false;
            }

            gemsSpent = cost;
            t.EndAt(Clock.Now);
            return true;
        }

        /// <summary>Burn speed-up minutes (res.speedup_m) against the timer; never wastes more than needed.</summary>
        public int ApplySpeedups(string id, Wallet wallet)
        {
            var t = Find(id);
            if (t == null || t.IsDone(Clock.Now))
            {
                return 0;
            }

            var needMinutes = (int)Math.Ceiling(t.Remaining(Clock.Now) / 60.0 - 1e-9);
            var used = Math.Min(needMinutes, wallet.Get(SurvIds.ResSpeedupM));
            if (used <= 0)
            {
                return 0;
            }

            wallet.TrySpend(SurvIds.ResSpeedupM, used);
            t.Shorten(used * 60.0);
            return used;
        }

        /// <summary>Remove a finished timer. Returns false if missing or still running.</summary>
        public bool TryComplete(string id)
        {
            var t = Find(id);
            if (t == null || !t.IsDone(Clock.Now))
            {
                return false;
            }

            _timers.Remove(t);
            return true;
        }

        public static string FormatClock(double seconds)
        {
            var s = (int)Math.Ceiling(Math.Max(0, seconds) - 1e-9);
            var h = s / 3600;
            var m = (s % 3600) / 60;
            var sec = s % 60;
            return h > 0 ? $"{h}h {m:00}m" : $"{m}:{sec:00}";
        }
    }
}
