#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Layout;
using Survival.Domain.Taps;
using Survival.Domain.Theme;
using Survival.Domain.View;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>The visual bits of one round rail icon. Badge (FREE), Dot (red count / "!"), Label (name or timer).</summary>
    public sealed class RailButtonParts
    {
        public RailButtonParts(RectTransform root, Button button, RectTransform icon, Text label, Image free, Text freeText, Image dot, Text dotText)
        {
            Root = root;
            Button = button;
            Icon = icon;
            Label = label;
            Free = free;
            FreeText = freeText;
            Dot = dot;
            DotText = dotText;
        }

        public RectTransform Root { get; }
        public Button Button { get; }
        public RectTransform Icon { get; }
        public Text Label { get; }
        public Image Free { get; }
        public Text FreeText { get; }
        public Image Dot { get; }
        public Text DotText { get; }

        /// <summary>Red dot with a count ("3") or "!"; null or empty hides it.</summary>
        public void SetDot(string? text)
        {
            Dot.gameObject.SetActive(!string.IsNullOrEmpty(text));
            DotText.text = text ?? "";
        }

        /// <summary>Label under the icon: the feature name, or a countdown like "2h 16m".</summary>
        public void SetLabel(string text) => Label.text = text;
    }

    /// <summary>
    /// Slim left rail of round ornate icons (hud slot <c>side_rail</c>) laid out from <see cref="SideRail"/>.
    /// Anything can join: <see cref="SurvivalPlayView.AddRailButton"/> builds a standard icon,
    /// <see cref="SurvivalPlayView.RegisterRail"/> adopts a button the feature built itself.
    /// </summary>
    public sealed class SurvivalSideRail
    {
        private static readonly Color DotRed = new Color(0.90f, 0.18f, 0.16f, 1f);
        private static readonly Color FreeGreen = new Color(0.30f, 0.82f, 0.38f, 1f);

        private readonly RectTransform _root;
        private readonly Dictionary<string, RectTransform> _buttons = new();
        private readonly Func<string, string, string> _s;
        private RailButtonParts? _more;

        public SurvivalSideRail(RectTransform parent, HudRect slot, Func<string, string, string> strings)
        {
            _s = strings;
            _root = new GameObject("SideRail", typeof(RectTransform)).GetComponent<RectTransform>();
            _root.SetParent(parent, false);
            SurvivalVisuals.Place(_root, slot);
            Model.Changed += Relayout;
        }

        public SideRail Model { get; } = new SideRail();

        public RectTransform Root => _root;

        public RectTransform? Button(string id) => _buttons.TryGetValue(id, out var rt) ? rt : null;

        public bool Register(string id, float order, RectTransform button, bool pinned)
        {
            if (id == SideRail.MoreId)
            {
                return false;
            }

            button.SetParent(_root, false);
            _buttons[id] = button;
            return Model.Register(new RailEntryDef(id, order, pinned));
        }

        public void Unregister(string id)
        {
            if (_buttons.TryGetValue(id, out var rt))
            {
                _buttons.Remove(id);
                if (rt != null)
                {
                    UnityEngine.Object.Destroy(rt.gameObject);
                }
            }

            Model.Unregister(id);
        }

        private void Relayout()
        {
            var visible = Model.Visible();
            var shown = new HashSet<string>(visible);
            foreach (var kv in _buttons)
            {
                if (kv.Value != null)
                {
                    kv.Value.gameObject.SetActive(shown.Contains(kv.Key));
                }
            }

            if (Model.NeedsMore && _more == null)
            {
                _more = Round(_root, "Rail_more", null, _s("rail.more", "More"), new Color(0.32f, 0.36f, 0.62f, 1f));
                var dots = SurvivalVisuals.Text(_more.Icon, "Dots", "•••", 34, TextAnchor.MiddleCenter, HookSkin.Cream);
                HookSkin.Outlined(dots, 1.6f).fontStyle = FontStyle.Bold;
                SurvivalVisuals.Stretch(dots.rectTransform);
                dots.raycastTarget = false;
                _more.Button.onClick.AddListener(Model.Toggle);
            }

            if (_more != null)
            {
                _more.Root.gameObject.SetActive(Model.NeedsMore);
                _more.SetLabel(Model.Expanded ? _s("rail.less", "Less") : _s("rail.more", "More"));
                _more.SetDot(Model.Expanded ? null : Model.Overflow.ToString());
            }

            for (var i = 0; i < visible.Count; i++)
            {
                var rt = visible[i] == SideRail.MoreId ? _more?.Root : Button(visible[i]);
                if (rt == null)
                {
                    continue;
                }

                var (x0, yTop, x1, yBottom) = SideRail.Slot(i);
                rt.anchorMin = new Vector2(x0, 1f - yBottom);
                rt.anchorMax = new Vector2(x1, 1f - yTop);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                rt.SetAsLastSibling();
            }
        }

        /// <summary>
        /// One round ornate icon: gold ring, tinted bevelled disc with gloss, icon art, cream outlined label below,
        /// FREE badge and red dot on the rim. Stays round on any screen shape (aspect fitter).
        /// </summary>
        internal static readonly Color RimGold = new Color(0.98f, 0.80f, 0.32f, 1f);
        internal static readonly Color RimLight = new Color(1f, 0.95f, 0.70f, 1f);
        internal static readonly Color RimShade = new Color(0.86f, 0.62f, 0.18f, 1f);
        internal static readonly Color Navy = new Color(0.07f, 0.12f, 0.34f, 1f);

        public static RailButtonParts Round(Transform parent, string name, string? iconKey, string label, Color tint)
        {
            var root = SurvivalVisuals.Image(parent, name, new Color(1f, 1f, 1f, 0f));
            root.raycastTarget = true;
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = root;
            button.transition = Selectable.Transition.None;
            root.gameObject.AddComponent<PressSquash>();

            var host = new GameObject("DiscHost", typeof(RectTransform)).GetComponent<RectTransform>();
            host.SetParent(root.transform, false);
            Box(host, 0.04f, 0.26f, 0.96f, 1f);
            // Clean ornate rim: thin bright bevelled gold, rivets, gloss, on a deep royal navy disc. No brown.
            var ring = SurvivalVisuals.Circle(host, "Ring", RimGold);
            ring.raycastTarget = false;
            var fit = ring.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = 1f;
            var hi = SurvivalVisuals.Ring(ring.transform, "RimHi", RimLight);
            hi.raycastTarget = false;
            SurvivalVisuals.Stretch(hi.rectTransform);
            var disc = SurvivalVisuals.Circle(ring.transform, "Disc", Navy);
            disc.raycastTarget = false;
            Box(disc.rectTransform, 0.055f, 0.055f, 0.945f, 0.945f);
            var lip = SurvivalVisuals.Ring(disc.transform, "RimLip", RimShade);
            lip.raycastTarget = false;
            SurvivalVisuals.Stretch(lip.rectTransform);
            var accent = SurvivalVisuals.Circle(disc.transform, "Accent", new Color(tint.r, tint.g, tint.b, 0.22f));
            accent.raycastTarget = false;
            Box(accent.rectTransform, 0.14f, 0.14f, 0.86f, 0.86f);
            for (var i = 0; i < 4; i++)
            {
                var ang = (45f + (i * 90f)) * Mathf.Deg2Rad;
                var cx = 0.5f + (0.475f * Mathf.Cos(ang));
                var cy = 0.5f + (0.475f * Mathf.Sin(ang));
                var rivet = SurvivalVisuals.Circle(ring.transform, "Rivet" + i, RimLight);
                rivet.raycastTarget = false;
                Box(rivet.rectTransform, cx - 0.035f, cy - 0.035f, cx + 0.035f, cy + 0.035f);
            }

            var icon = new GameObject("Icon", typeof(RectTransform)).GetComponent<RectTransform>();
            icon.SetParent(disc.transform, false);
            Box(icon, 0.12f, 0.12f, 0.88f, 0.88f);
            if (!string.IsNullOrEmpty(iconKey))
            {
                var art = SurvivalArt.Get(iconKey!);
                if (art != null)
                {
                    var img = SurvivalVisuals.Image(icon, "Art", Color.white, art);
                    img.preserveAspect = true;
                    img.raycastTarget = false;
                    SurvivalVisuals.Stretch(img.rectTransform);
                }
            }

            var gloss = SurvivalVisuals.Circle(disc.transform, "Gloss", new Color(1f, 1f, 1f, 0.16f));
            gloss.raycastTarget = false;
            Box(gloss.rectTransform, 0.18f, 0.55f, 0.82f, 0.94f);

            // Small cream label with a dark outline, same for every icon (names and timers).
            var text = HookSkin.Outlined(SurvivalVisuals.Text(root.transform, "Label", label, 15, TextAnchor.MiddleCenter, HookSkin.Cream), 2f);
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            HookSkin.Fit(text, 10, 15);
            Box(text.rectTransform, 0f, 0f, 1f, 0.24f);

            var badgeSprite = SurvivalArt.Get(HookArtKeys.BadgeFree);
            var badge = badgeSprite != null
                ? SurvivalVisuals.Image(ring.transform, "FreeBadge", Color.white, badgeSprite)
                : SurvivalVisuals.Image(ring.transform, "FreeBadge", FreeGreen);
            badge.preserveAspect = true;
            badge.raycastTarget = false;
            Box(badge.rectTransform, 0.42f, 0.74f, 1.16f, 1.06f);
            var badgeText = HookSkin.Outlined(SurvivalVisuals.Text(badge.transform, "T", "FREE", 13, TextAnchor.MiddleCenter, HookSkin.Cream), 1.3f);
            badgeText.fontStyle = FontStyle.Bold;
            SurvivalVisuals.Stretch(badgeText.rectTransform);
            badge.gameObject.SetActive(false);

            var dot = SurvivalVisuals.Circle(ring.transform, "Dot", DotRed);
            dot.raycastTarget = false;
            Box(dot.rectTransform, 0.70f, 0.70f, 1.02f, 1.02f);
            var dotText = HookSkin.Outlined(SurvivalVisuals.Text(dot.transform, "T", "!", 15, TextAnchor.MiddleCenter, HookSkin.Cream), 1.2f);
            dotText.fontStyle = FontStyle.Bold;
            SurvivalVisuals.Stretch(dotText.rectTransform);
            dot.gameObject.SetActive(false);

            return new RailButtonParts(root.rectTransform, button, icon, text, badge, badgeText, dot, dotText);
        }

        private static void Box(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
