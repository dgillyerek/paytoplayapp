#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Survival.Domain.View
{
    /// <summary>One side-rail icon: stable id, sort order (lower sits higher), pinned entries never fold into "More".</summary>
    public sealed record RailEntryDef(string Id, float Order, bool Pinned = false);

    /// <summary>
    /// The slim left rail of round icons for timed and event features (Runner, Events, Oracle, Bounty, Merchant...).
    /// Collapsed it shows at most <see cref="MaxCollapsed"/> slots: if more are shown the last slot becomes "More",
    /// which expands the rail into extra columns. Pure model: the Unity view places icons from <see cref="Layout"/>.
    /// </summary>
    public sealed class SideRail
    {
        public const string MoreId = "rail.more";
        public const int MaxCollapsed = 5;

        private readonly List<RailEntryDef> _entries = new();
        private readonly HashSet<string> _hidden = new();
        private bool _expanded;

        /// <summary>Raised whenever the visible set or order changes.</summary>
        public event Action? Changed;

        public IReadOnlyList<RailEntryDef> Entries => _entries;

        public bool Contains(string id) => _entries.Any(e => e.Id == id);

        public bool Expanded
        {
            get => _expanded;
            set
            {
                if (_expanded != value)
                {
                    _expanded = value;
                    Changed?.Invoke();
                }
            }
        }

        public void Toggle() => Expanded = !Expanded;

        /// <summary>Add an entry; re-registering an id replaces its order. Returns false for the reserved More id.</summary>
        public bool Register(RailEntryDef def)
        {
            if (string.IsNullOrEmpty(def.Id) || def.Id == MoreId)
            {
                return false;
            }

            _entries.RemoveAll(e => e.Id == def.Id);
            _entries.Add(def);
            Changed?.Invoke();
            return true;
        }

        public bool Unregister(string id)
        {
            var removed = _entries.RemoveAll(e => e.Id == id) > 0;
            _hidden.Remove(id);
            if (removed)
            {
                Changed?.Invoke();
            }

            return removed;
        }

        /// <summary>Hide or show an entry without unregistering it (a merchant who has left, an event that ended).</summary>
        public void SetShown(string id, bool shown)
        {
            var changed = shown ? _hidden.Remove(id) : _hidden.Add(id);
            if (changed && Contains(id))
            {
                Changed?.Invoke();
            }
        }

        /// <summary>All shown entries in display order: pinned first, then by order, ties by registration order.</summary>
        public IReadOnlyList<string> Ordered()
        {
            return _entries
                .Select((e, i) => (e, i))
                .Where(t => !_hidden.Contains(t.e.Id))
                .OrderBy(t => t.e.Pinned ? 0 : 1)
                .ThenBy(t => t.e.Order)
                .ThenBy(t => t.i)
                .Select(t => t.e.Id)
                .ToList();
        }

        /// <summary>True when there are more shown entries than fit collapsed.</summary>
        public bool NeedsMore => Ordered().Count > MaxCollapsed;

        /// <summary>How many shown entries sit behind "More" while collapsed.</summary>
        public int Overflow => Math.Max(0, Ordered().Count - (MaxCollapsed - 1));

        /// <summary>
        /// Ids to draw, in slot order. Includes <see cref="MoreId"/> last when needed. Collapsed: first
        /// MaxCollapsed-1 plus More; expanded: all plus More (so it can fold back).
        /// </summary>
        public IReadOnlyList<string> Visible()
        {
            var all = Ordered();
            if (all.Count <= MaxCollapsed)
            {
                return all;
            }

            var list = _expanded ? all.ToList() : all.Take(MaxCollapsed - 1).ToList();
            list.Add(MoreId);
            return list;
        }

        /// <summary>Column and row for slot <paramref name="index"/>: rows fill top-down, then wrap to a new column.</summary>
        public static (int Column, int Row) Layout(int index) => (index / MaxCollapsed, index % MaxCollapsed);

        /// <summary>Slot rect inside the rail as fractions (x across one column width, y from the top of the rail).</summary>
        public static (float XMin, float YTop, float XMax, float YBottom) Slot(int index)
        {
            var (col, row) = Layout(index);
            var h = 1f / MaxCollapsed;
            return (col * 1.08f, row * h, col * 1.08f + 1f, (row + 1) * h);
        }
    }
}
