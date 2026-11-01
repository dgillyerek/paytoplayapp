#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Session;
using Survival.Domain.Taps;
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// What the tap features (Crystal Orb, Bounty Board, Wandering Merchant, Wishing Well) borrow from the village
    /// HUD: its sheets, buttons, toasts, sparks and the chest-burst celebration. Each feature is its own component
    /// that attaches on <see cref="HudBuilt"/>, so the features stay out of this view's code.
    /// </summary>
    public sealed partial class SurvivalHookView
    {
        /// <summary>Raised once the village HUD is built; tap features attach here.</summary>
        public static event Action<SurvivalHookView, RectTransform>? HudBuilt;

        internal static readonly Color TapFreeGreen = new Color(0.30f, 0.82f, 0.38f, 1f);
        internal static readonly Color TapWarn = new Color(1f, 0.55f, 0.45f, 1f);
        internal static readonly Color TapDisabled = new Color(0.30f, 0.31f, 0.34f, 1f);

        private bool _tapFeedWired;

        internal HookSession? TapHook => _hook;

        internal RectTransform? TapOverlay => _overlay;

        private void RaiseHudBuilt(RectTransform root)
        {
            WireTapFeed();
            HudBuilt?.Invoke(this, root);
        }

        /// <summary>Harvest taps and building level-ups feed <see cref="ActivityFeed"/>.</summary>
        private void WireTapFeed()
        {
            if (_hook == null || _tapFeedWired)
            {
                return;
            }

            _tapFeedWired = true;
            _hook.Base.Collected += (_, __, ___) => ActivityFeed.Report(ActivityKinds.Harvest, 1);
            _hook.Base.LevelReached += (_, level) =>
            {
                if (level > 1)
                {
                    ActivityFeed.Report(ActivityKinds.Upgrade, 1);
                }
            };
        }

        internal string TapS(string key, string fallback) => S(key, fallback);

        internal string TapFmt(string key, string fallback, object arg) => Fmt(key, fallback, arg);

        internal string TapFmt2(string key, string fallback, object a, object b) => Fmt2(key, fallback, a, b);

        internal string TapResName(string resourceId) => ResName(resourceId);

        internal static Color TapResColor(string resourceId) => ResColor(resourceId);

        internal static string TapResGlyph(string resourceId) => ResGlyph(resourceId);

        internal RectTransform? TapOpenSheet(string name, float width, float height, string title) => OpenSheet(name, width, height, title);

        internal void TapCloseSheet() => CloseSheet();

        internal void TapSheetRefresh(Action? refresh)
        {
            _sheetRefresh = refresh;
            refresh?.Invoke();
        }

        internal bool TapSheetOpen => _sheet != null;

        internal Text TapSheetButton(RectTransform panel, string name, string key, Color fallback, float x0, float y0, float x1, float y1, int size, out Button button) =>
            SheetButton(panel, name, key, fallback, x0, y0, x1, y1, size, out button);

        internal void TapWire(Button button, Action onTap) => Wire(button, onTap);

        internal static void TapBox(RectTransform rt, float x0, float y0, float x1, float y1) => Box(rt, x0, y0, x1, y1);

        internal void TapToast(RectTransform at, string copy, Color color) => Toast(NormCenterOf(at), copy, color);

        internal void TapRise(RectTransform at, string copy, Color color) => FloatText(NormCenterOf(at), copy, color);

        internal void TapShake(RectTransform rt) => StartCoroutine(Shake(rt));

        internal void TapBurst(RectTransform at, Color color, int count = 14)
        {
            var c = NormCenterOf(at);
            StartCoroutine(SparkBurst(c, color, count, 0.07f));
            StartCoroutine(RingBurst(c, color, 0.16f));
        }

        /// <summary>Pop of resource icons flying from <paramref name="from"/> to the HUD chips.</summary>
        internal void TapFly(RectTransform from, string resourceId, int coins = 6) => StartCoroutine(FlyReward(resourceId, NormCenterOf(from), coins));

        /// <summary>
        /// Round HUD button on the left edge (bottom-up anchors): gold filigree, bevel, shine, an icon slot, a label
        /// plate, a FREE badge and a red count dot. Returns the button; the parts come back through <paramref name="parts"/>.
        /// </summary>
        internal Button TapHudButton(RectTransform root, string name, float yTop, string label, Color tint, out TapHudParts parts)
        {
            // Round ornate icon. With a side rail it joins the rail (yTop is the order hint, so features keep
            // their relative order); without one it sits on the left edge at yTop like before.
            var round = SurvivalSideRail.Round(root, name, null, label, tint);
            Box(round.Root, 0.018f, 1f - yTop - 0.076f, 0.168f, 1f - yTop);
            _play?.RegisterRail(name, yTop, round.Root);
            round.FreeText.text = S("hook.free", "FREE");
            parts = new TapHudParts(round.Root, round.Icon, round.Label, round.Free, round.Dot, round.DotText);
            return round.Button;
        }

        /// <summary>Juice for a tapped HUD button: punch plus a ring.</summary>
        internal void TapPunch(RectTransform rt) => Juice(rt);
    }

    internal sealed class TapHudParts
    {
        public TapHudParts(RectTransform root, RectTransform icon, Text label, Image free, Image dot, Text dotText)
        {
            Root = root;
            Icon = icon;
            Label = label;
            Free = free;
            Dot = dot;
            DotText = dotText;
        }

        public RectTransform Root { get; }
        public RectTransform Icon { get; }
        public Text Label { get; }
        public Image Free { get; }
        public Image Dot { get; }
        public Text DotText { get; }

        /// <summary>FREE badge (pulsing) or a red "!" dot, or nothing.</summary>
        public void Show(bool free, bool attention)
        {
            Free.gameObject.SetActive(free);
            Dot.gameObject.SetActive(!free && attention);
            if (free)
            {
                var pulse = 1f + 0.08f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f));
                Free.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
            }
        }
    }
}
