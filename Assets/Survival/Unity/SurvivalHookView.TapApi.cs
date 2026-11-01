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
            const float x0 = 0.018f;
            const float x1 = 0.168f;
            const float h = 0.076f;
            var frame = HookSkin.Panel(root, name, HookArtKeys.ButtonBlue, tint, 0.42f);
            frame.raycastTarget = true;
            var btn = frame.gameObject.AddComponent<Button>();
            btn.targetGraphic = frame;
            Box(frame.rectTransform, x0, 1f - yTop - h, x1, 1f - yTop);
            Ornaments.Bevel(frame.transform, 0.8f);
            Ornaments.Frame(frame.transform, Ornaments.Gold, 3f);
            Ornaments.Shine(frame.rectTransform, 3.4f);

            var icon = new GameObject("Icon", typeof(RectTransform)).GetComponent<RectTransform>();
            icon.SetParent(frame.transform, false);
            Box(icon, 0.12f, 0.30f, 0.88f, 0.98f);

            var plate = HookSkin.Panel(frame.transform, "Plate", HookArtKeys.Chip, SurvivalVisuals.PanelSoft, 0.4f);
            plate.raycastTarget = false;
            Box(plate.rectTransform, -0.06f, -0.06f, 1.06f, 0.30f);
            var text = HookSkin.Outlined(SurvivalVisuals.Text(plate.transform, "T", label, 15, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.4f);
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            HookSkin.Fit(text, 9, 15);
            SurvivalVisuals.Stretch(text.rectTransform);

            var badgeSprite = SurvivalArt.Get(HookArtKeys.BadgeFree);
            var badge = badgeSprite != null
                ? SurvivalVisuals.Image(frame.transform, "FreeBadge", Color.white, badgeSprite)
                : SurvivalVisuals.Image(frame.transform, "FreeBadge", TapFreeGreen);
            badge.preserveAspect = true;
            badge.raycastTarget = false;
            Box(badge.rectTransform, 0.48f, 0.72f, 1.14f, 1.10f);
            var badgeText = HookSkin.Outlined(SurvivalVisuals.Text(badge.transform, "T", S("hook.free", "FREE"), 14, TextAnchor.MiddleCenter, Color.white), 1.4f);
            badgeText.fontStyle = FontStyle.Bold;
            SurvivalVisuals.Stretch(badgeText.rectTransform);
            badge.gameObject.SetActive(false);

            var dot = SurvivalVisuals.Circle(frame.transform, "Dot", new Color(0.90f, 0.18f, 0.16f, 1f));
            dot.raycastTarget = false;
            Box(dot.rectTransform, 0.76f, 0.74f, 1.02f, 1.02f);
            var dotText = HookSkin.Outlined(SurvivalVisuals.Text(dot.transform, "T", "!", 15, TextAnchor.MiddleCenter, Color.white), 1.2f);
            dotText.fontStyle = FontStyle.Bold;
            SurvivalVisuals.Stretch(dotText.rectTransform);
            dot.gameObject.SetActive(false);

            parts = new TapHudParts(frame.rectTransform, icon, text, badge, dot, dotText);
            return btn;
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
