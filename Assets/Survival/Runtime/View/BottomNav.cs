#nullable enable
using System;
using System.Collections.Generic;

namespace Survival.Domain.View
{
    /// <summary>One bottom-bar button: stable id (also the label string key), painted icon key.</summary>
    public sealed record NavButtonDef(string Id, string IconKey);

    /// <summary>
    /// The village/world bottom bar: Heroes, Alliance, Bag by default, evenly spaced, with room to insert more
    /// (Battle goes in via <see cref="Add"/>). Pure model: the Unity view lays buttons out from <see cref="Slot"/>
    /// and routes taps through <see cref="Tap"/>, so every screen wires one entry point per button.
    /// </summary>
    public sealed class BottomNav
    {
        public const string Heroes = "nav.heroes";
        public const string Alliance = "nav.alliance";
        public const string Bag = "nav.bag";
        public const string Battle = "nav.battle";

        public const string IconHeroes = "theme_a.ui.icon_heroes";
        public const string IconAlliance = "theme_a.ui.icon_alliance";
        public const string IconBag = "theme_a.ui.icon_bag";
        public const string IconBattle = "theme_a.ui.icon_battle";
        public const string IconBuild = "theme_a.ui.icon_build";
        public const string BarArt = "theme_a.ui.nav_bar";
        public const string ButtonArt = "theme_a.ui.nav_button";

        /// <summary>Horizontal padding inside the bar and the gap between buttons (fractions of the bar width).</summary>
        public const float Pad = 0.035f;
        public const float Gap = 0.03f;

        /// <summary>Widest a button gets, so three buttons leave visible room for a fourth (Battle).</summary>
        public const float MaxWidth = 0.22f;

        private readonly List<NavButtonDef> _buttons = new();

        public BottomNav()
        {
            _buttons.Add(new NavButtonDef(Heroes, IconHeroes));
            _buttons.Add(new NavButtonDef(Alliance, IconAlliance));
            _buttons.Add(new NavButtonDef(Bag, IconBag));
        }

        public IReadOnlyList<NavButtonDef> Buttons => _buttons;

        /// <summary>Raised with the button id on every tap (after the per-id handlers in the view).</summary>
        public event Action<string>? Tapped;

        /// <summary>Raised when buttons are added or removed, so the view re-lays the bar.</summary>
        public event Action? Changed;

        public int IndexOf(string id) => _buttons.FindIndex(b => b.Id == id);

        public bool Contains(string id) => IndexOf(id) >= 0;

        /// <summary>Insert a button (index clamps; null appends). Re-adding an existing id is a no-op.</summary>
        public bool Add(NavButtonDef def, int? index = null)
        {
            if (Contains(def.Id))
            {
                return false;
            }

            var at = index is int i ? Math.Max(0, Math.Min(i, _buttons.Count)) : _buttons.Count;
            _buttons.Insert(at, def);
            Changed?.Invoke();
            return true;
        }

        public bool Remove(string id)
        {
            var i = IndexOf(id);
            if (i < 0)
            {
                return false;
            }

            _buttons.RemoveAt(i);
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Even layout: n equal buttons centred in the bar with equal gaps, each no wider than
        /// <see cref="MaxWidth"/>. Returns (xMin, xMax) as fractions of the bar width.
        /// </summary>
        public (float Min, float Max) Slot(int index) => Slot(index, _buttons.Count);

        public static (float Min, float Max) Slot(int index, int count)
        {
            if (count <= 0)
            {
                return (0f, 0f);
            }

            var avail = 1f - 2f * Pad;
            var w = Math.Min(MaxWidth, (avail - Gap * (count - 1)) / count);
            var total = w * count + Gap * (count - 1);
            var x0 = (1f - total) / 2f + index * (w + Gap);
            return (x0, x0 + w);
        }

        /// <summary>Report a tap on button <paramref name="id"/> (unknown ids are ignored).</summary>
        public bool Tap(string id)
        {
            if (!Contains(id))
            {
                return false;
            }

            Tapped?.Invoke(id);
            return true;
        }
    }
}
