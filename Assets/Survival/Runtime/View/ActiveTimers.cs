#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Survival.Domain.Campaign;
using Survival.Domain.Clock;
using Survival.Domain.World;

namespace Survival.Domain.View
{
    /// <summary>What kind of wait a timer row is (picks its icon).</summary>
    public enum TimerKind
    {
        Building = 0,
        HeroTraining,
        March
    }

    /// <summary>Where tapping a timer row goes.</summary>
    public enum TimerGoKind
    {
        /// <summary>Focus the plot in town and open its panel (Target = plot id).</summary>
        Building = 0,

        /// <summary>Heroes screen with this hero's detail (Target = hero id).</summary>
        HeroDetail,

        /// <summary>World map, glide to the march's target (Target = world entity id).</summary>
        WorldEntity
    }

    /// <summary>One row of the Active Timers panel. Times are clock seconds.</summary>
    public sealed class TimerRow
    {
        public TimerRow(string id, TimerKind kind, string target, double startedAt, double endsAt)
        {
            Id = id;
            Kind = kind;
            Target = target;
            StartedAt = startedAt;
            EndsAt = endsAt;
        }

        /// <summary>Stable id (rows rebuild only when the set of ids changes).</summary>
        public string Id { get; }
        public TimerKind Kind { get; }

        /// <summary>Plot id, hero id or world entity id, depending on <see cref="Kind"/>.</summary>
        public string Target { get; }
        public double StartedAt { get; }
        public double EndsAt { get; }

        public double Remaining(double now) => Math.Max(0, EndsAt - now);
        public bool IsReady(double now) => now >= EndsAt;

        public double Progress(double now)
        {
            var len = EndsAt - StartedAt;
            return len <= 0 ? 1 : Math.Min(1, Math.Max(0, (now - StartedAt) / len));
        }

        public TimerGoKind Go => ActiveTimers.GoFor(Kind);
    }

    /// <summary>
    /// The town's Active Timers panel (Derek 2026-11-03: "some kind of place where I can see what's current under timers
    /// so that I can click and go directly to that appropriate place"). Gathers every running wait the game has
    /// (building construction / upgrades on each builder, hero level-up training, the player's world marches),
    /// sorts by soonest finish, and says where each row goes on tap. The view builds rows once and only rebuilds
    /// them when <see cref="Signature"/> changes; on tick it updates countdown text and bars.
    /// </summary>
    public static class ActiveTimers
    {
        public static TimerGoKind GoFor(TimerKind kind) => kind switch
        {
            TimerKind.HeroTraining => TimerGoKind.HeroDetail,
            TimerKind.March => TimerGoKind.WorldEntity,
            _ => TimerGoKind.Building
        };

        /// <summary>Every running timer, soonest finish first (finished ones lead). Any source may be null.</summary>
        public static List<TimerRow> Gather(
            IEnumerable<GameTimer>? builds,
            IEnumerable<TrainingTimer>? training,
            IEnumerable<WorldMarch>? marches,
            double now)
        {
            var rows = new List<TimerRow>();
            if (builds != null)
            {
                foreach (var t in builds)
                {
                    rows.Add(new TimerRow(t.Id, TimerKind.Building, t.TargetId, t.StartedAt, t.EndsAt));
                }
            }

            if (training != null)
            {
                foreach (var t in training)
                {
                    rows.Add(new TimerRow("hero.train." + t.HeroId, TimerKind.HeroTraining, t.HeroId, t.StartedAt, t.EndsAt));
                }
            }

            if (marches != null)
            {
                foreach (var m in marches)
                {
                    // Only the player's own columns that are actually on the road (mock patrols and waiting legs are not timers).
                    if (!m.IsPlayer || m.IsWaiting(now) || now >= m.Arrival)
                    {
                        continue;
                    }

                    rows.Add(new TimerRow(m.Id + "." + m.Leg, TimerKind.March, m.TargetId, m.Start, m.Arrival));
                }
            }

            Sort(rows);
            return rows;
        }

        /// <summary>Soonest finish first; ties by id so the order never flickers.</summary>
        public static void Sort(List<TimerRow> rows) => rows.Sort((a, b) =>
        {
            var c = a.EndsAt.CompareTo(b.EndsAt);
            return c != 0 ? c : string.CompareOrdinal(a.Id, b.Id);
        });

        /// <summary>Ordered ids: the panel rebuilds its rows only when this changes (not every tick).</summary>
        public static string Signature(IReadOnlyList<TimerRow> rows)
        {
            var sb = new StringBuilder();
            foreach (var r in rows)
            {
                sb.Append(r.Id).Append('|');
            }

            return sb.ToString();
        }

        public static bool NeedsRebuild(string? lastSignature, IReadOnlyList<TimerRow> rows) =>
            !string.Equals(lastSignature, Signature(rows), StringComparison.Ordinal);

        public static int ReadyCount(IReadOnlyList<TimerRow> rows, double now)
        {
            var n = 0;
            foreach (var r in rows)
            {
                if (r.IsReady(now))
                {
                    n++;
                }
            }

            return n;
        }
    }
}
