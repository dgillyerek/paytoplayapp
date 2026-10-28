#nullable enable
using System.Collections.Generic;
using Survival.Domain.Campaign;
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Hero HP corner panel shared by the runner and the locked battle: one compact row per hero in the bottom-left of the
    /// safe area (framed portrait or initial, name / level, horizontal HP bar with a damage trail, dimmed once down).
    /// Replaces the world-space bars over heroes; enemy, boss and box bars are unchanged. Layout numbers live in
    /// <see cref="HeroHpPanelLayout"/>. Never blocks touches (the runner's drag pad sits under it).
    /// </summary>
    internal sealed class HeroHpPanel
    {
        private sealed class Row
        {
            public int Key;
            public RectTransform Rect = null!;
            public CanvasGroup Group = null!;
            public Image Frame = null!;
            public Image Fill = null!;
            public Image Trail = null!;
            public Text Hp = null!;
            public float Fraction = 1f;
            public float Trailed = 1f;
            public float SinceHit = 10f;
            public bool Alive = true;
            public int ShownHp = int.MinValue;
        }

        private static readonly Color RowFill = new Color(0.06f, 0.08f, 0.16f, 0.86f);
        private static readonly Color TrailColor = new Color(0.95f, 0.30f, 0.25f, 0.95f);

        private readonly RectTransform _safe;
        private readonly RectTransform _panel;
        private readonly List<Row> _rows = new List<Row>();
        private Rect _lastSafe;
        private Vector2Int _lastScreen;

        private HeroHpPanel(RectTransform safe, RectTransform panel)
        {
            _safe = safe;
            _panel = panel;
        }

        public int RowCount => _rows.Count;

        /// <summary>Builds the (empty) panel on a HUD canvas.</summary>
        public static HeroHpPanel Build(RectTransform canvas)
        {
            var safeGo = new GameObject("HeroHpSafeArea", typeof(RectTransform));
            safeGo.transform.SetParent(canvas, false);
            var safe = (RectTransform)safeGo.transform;
            var group = safeGo.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var panel = HookSkin.Panel(safe, "HeroHpPanel", HookArtKeys.Panel, SurvivalVisuals.PanelSoft, 0.4f);
            panel.raycastTarget = false;
            var rt = panel.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            var hp = new HeroHpPanel(safe, rt);
            hp.FitSafeArea();
            panel.gameObject.SetActive(false);
            return hp;
        }

        /// <summary>Adds a row for a hero (no-op past <see cref="HeroHpPanelLayout.MaxRows"/> or for a known key).</summary>
        public void Add(int key, string name, string tag, string portraitKey)
        {
            if (_rows.Count >= HeroHpPanelLayout.MaxRows || Find(key) != null)
            {
                return;
            }

            var row = new Row { Key = key };
            var bg = HookSkin.Panel(_panel, "Hero" + _rows.Count, HookArtKeys.Chip, RowFill, 0.35f);
            row.Rect = bg.rectTransform;
            row.Rect.anchorMin = new Vector2(0f, 1f);
            row.Rect.anchorMax = new Vector2(1f, 1f);
            row.Rect.pivot = new Vector2(0.5f, 1f);
            row.Group = bg.gameObject.AddComponent<CanvasGroup>();
            row.Frame = bg;

            const float h = HeroHpPanelLayout.RowHeight;
            const float portrait = h - 10f;
            var heroFrame = Rgb(CampaignArtKeys.HeroFrameRgb, SurvivalVisuals.Gold);
            var ring = SurvivalVisuals.Circle(bg.transform, "Portrait", SurvivalVisuals.Gold);
            Place(ring.rectTransform, 6f, portrait, portrait);
            var inner = SurvivalVisuals.Circle(ring.transform, "Back", heroFrame);
            Inset(inner.rectTransform, 3f);
            var sprite = string.IsNullOrEmpty(portraitKey) ? null : SurvivalArt.Get(portraitKey);
            if (sprite != null)
            {
                var face = SurvivalVisuals.Image(inner.transform, "Face", Color.white, sprite);
                face.preserveAspect = true;
                Inset(face.rectTransform, 2f);
            }
            else
            {
                var initial = HookSkin.Outlined(SurvivalVisuals.Text(inner.transform, "Initial", HeroHpPanelLayout.Initial(name), HeroHpPanelLayout.InitialFont, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
                initial.fontStyle = FontStyle.Bold;
                SurvivalVisuals.Stretch(initial.rectTransform);
            }

            var left = 6f + portrait + 8f;
            var title = HookSkin.Outlined(SurvivalVisuals.Text(bg.transform, "Name", name, HeroHpPanelLayout.NameFont, TextAnchor.LowerLeft, SurvivalVisuals.Cream), 1.5f);
            title.fontStyle = FontStyle.Bold;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            Band(title.rectTransform, left, string.IsNullOrEmpty(tag) ? 8f : 46f, 0.50f, 0.96f);
            var level = HookSkin.Outlined(SurvivalVisuals.Text(bg.transform, "Tag", tag, HeroHpPanelLayout.TagFont, TextAnchor.LowerRight, SurvivalVisuals.Gold), 1.5f);
            Band(level.rectTransform, left, 8f, 0.50f, 0.96f);

            var bar = HookSkin.Bar(bg.transform, "Hp", out var fill);
            Band(bar.rectTransform, left, 8f, 0.10f, 0.46f);
            row.Fill = fill;
            row.Trail = SurvivalVisuals.Image(bar.transform, "Trail", TrailColor, fill.sprite);
            row.Trail.transform.SetSiblingIndex(fill.transform.GetSiblingIndex());
            var trt = row.Trail.rectTransform;
            var frt = fill.rectTransform;
            trt.anchorMin = frt.anchorMin;
            trt.anchorMax = frt.anchorMax;
            trt.offsetMin = frt.offsetMin;
            trt.offsetMax = frt.offsetMax;
            row.Trail.type = fill.type;
            row.Trail.fillMethod = fill.fillMethod;
            row.Trail.fillOrigin = fill.fillOrigin;
            row.Hp = HookSkin.Outlined(SurvivalVisuals.Text(bar.transform, "Value", "", HeroHpPanelLayout.HpFont, TextAnchor.MiddleCenter, Color.white), 1.2f);
            SurvivalVisuals.Stretch(row.Hp.rectTransform);

            foreach (var g in bg.GetComponentsInChildren<Graphic>(true))
            {
                g.raycastTarget = false;
            }

            _rows.Add(row);
            Paint(row);
            Relayout();
        }

        /// <summary>Feeds a hero's HP; a drop starts the trail hold and the frame flash.</summary>
        public void Set(int key, int hp, int maxHp)
        {
            var row = Find(key);
            if (row == null)
            {
                return;
            }

            var fraction = maxHp > 0 ? Mathf.Clamp01(hp / (float)maxHp) : 0f;
            if (fraction < row.Fraction - 1e-4f)
            {
                row.SinceHit = 0f;
            }

            row.Fraction = fraction;
            row.Alive = hp > 0;
            if (hp != row.ShownHp)
            {
                row.ShownHp = hp;
                row.Hp.text = Mathf.Max(0, hp) + " / " + Mathf.Max(0, maxHp);
            }
        }

        /// <summary>Per frame: safe area, trail drain, hit flash, dimming.</summary>
        public void Tick(float dt)
        {
            FitSafeArea();
            foreach (var row in _rows)
            {
                row.SinceHit += dt;
                row.Trailed = HeroHpPanelLayout.Trail(row.Trailed, row.Fraction, row.SinceHit, dt);
                Paint(row);
            }
        }

        private void Paint(Row row)
        {
            var (r, g, b) = HeroHpPanelLayout.FillRgb(row.Fraction);
            var key = row.Fraction > 0.5f ? HookArtKeys.BarFillGreen : HookArtKeys.BarFillGold;
            HookSkin.SetBar(row.Fill, row.Fraction, key, new Color(r, g, b, 1f));
            if (row.Fraction <= 0.25f && row.Fill.type == Image.Type.Filled)
            {
                // Low: tint the gold art red (there is no red fill sprite in the kit).
                row.Fill.color = new Color(1f, 0.45f, 0.42f, 1f);
            }

            SetTrail(row.Trail, row.Trailed);
            var flash = HeroHpPanelLayout.Flash(row.SinceHit);
            row.Frame.color = Color.Lerp(row.Frame.sprite != null ? Color.white : RowFill, new Color(1f, 0.45f, 0.4f, 1f), flash * 0.6f);
            row.Group.alpha = HeroHpPanelLayout.RowAlpha(row.Alive);
        }

        private static void SetTrail(Image trail, float fraction)
        {
            if (trail.type == Image.Type.Filled)
            {
                trail.fillAmount = fraction;
                return;
            }

            var rt = trail.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private Row? Find(int key)
        {
            foreach (var r in _rows)
            {
                if (r.Key == key)
                {
                    return r;
                }
            }

            return null;
        }

        private void Relayout()
        {
            var n = HeroHpPanelLayout.Rows(_rows.Count);
            _panel.gameObject.SetActive(n > 0);
            var width = HeroHpPanelLayout.Width(_safe.rect.width);
            _panel.sizeDelta = new Vector2(width, HeroHpPanelLayout.Height(n));
            // Under the HUD's top row (TopHudLayout.PanelTop), measured from the safe area's top edge.
            var canvas = _safe.parent as RectTransform;
            var canvasHeight = canvas != null ? canvas.rect.height : 1920f;
            var inset = canvasHeight * (1f - _safe.anchorMax.y);
            _panel.anchoredPosition = new Vector2(HeroHpPanelLayout.Margin, -HeroHpPanelLayout.TopOffset(canvasHeight, inset));
            for (var i = 0; i < _rows.Count; i++)
            {
                var rt = _rows[i].Rect;
                rt.sizeDelta = new Vector2(-2f * HeroHpPanelLayout.Padding, HeroHpPanelLayout.RowHeight);
                rt.anchoredPosition = new Vector2(0f, -HeroHpPanelLayout.RowTop(i));
            }
        }

        /// <summary>Keeps the panel inside Screen.safeArea (notches, home indicator).</summary>
        private void FitSafeArea()
        {
            var screen = new Vector2Int(Screen.width, Screen.height);
            var safe = Screen.safeArea;
            if (safe == _lastSafe && screen == _lastScreen)
            {
                return;
            }

            _lastSafe = safe;
            _lastScreen = screen;
            var w = Mathf.Max(1f, screen.x);
            var h = Mathf.Max(1f, screen.y);
            _safe.anchorMin = new Vector2(safe.xMin / w, safe.yMin / h);
            _safe.anchorMax = new Vector2(safe.xMax / w, safe.yMax / h);
            _safe.offsetMin = Vector2.zero;
            _safe.offsetMax = Vector2.zero;
            Relayout();
        }

        private static void Place(RectTransform rt, float left, float width, float height)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(left, 0f);
        }

        private static void Inset(RectTransform rt, float px)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(px, px);
            rt.offsetMax = new Vector2(-px, -px);
        }

        private static void Band(RectTransform rt, float left, float right, float yMin, float yMax)
        {
            rt.anchorMin = new Vector2(0f, yMin);
            rt.anchorMax = new Vector2(1f, yMax);
            rt.offsetMin = new Vector2(left, 0f);
            rt.offsetMax = new Vector2(-right, 0f);
        }

        private static Color Rgb(float[] rgb, Color fallback) =>
            rgb != null && rgb.Length >= 3 ? new Color(rgb[0], rgb[1], rgb[2], 1f) : fallback;
    }
}
