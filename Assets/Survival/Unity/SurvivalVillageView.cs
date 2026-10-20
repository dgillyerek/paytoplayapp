#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using Survival.Domain.Base;
using Survival.Domain.Theme;
using Survival.Domain.View;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Zoomed-in Village view: a scrollable lawn (map/village_layout.json) with dirt roads, props, one host per
    /// base plot (the hook view fills each with its building, badge, bubble and timer) and empty plots that
    /// open as the Castle levels up. The Castle host grows with its painted tier.
    /// </summary>
    public sealed class SurvivalVillageView : MonoBehaviour
    {
        private readonly Dictionary<string, RectTransform> _hosts = new(StringComparer.Ordinal);
        private readonly List<(VillageEmptySpot Spot, Text Sign, Image Art)> _empty = new();
        private readonly List<RectTransform> _labels = new();
        private int _shownHq = -1;

        public VillageLayout? Layout { get; private set; }
        public RectTransform? Root { get; private set; }
        public RectTransform? Content { get; private set; }
        public SurvivalMapRig? Rig { get; private set; }

        /// <summary>Content-space layer above buildings for pops and bursts that should scroll with the map.</summary>
        public RectTransform? Fx { get; private set; }

        public IReadOnlyDictionary<string, RectTransform> Hosts => _hosts;

        public RectTransform? Host(string plotId) => _hosts.TryGetValue(plotId, out var h) ? h : null;

        public static SurvivalVillageView Create(RectTransform parent, VillageLayout layout, Func<string, string, string> strings)
        {
            var root = new GameObject("VillageView", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            var view = root.AddComponent<SurvivalVillageView>();
            view.Build((RectTransform)root.transform, layout, strings);
            return view;
        }

        private void Build(RectTransform root, VillageLayout layout, Func<string, string, string> strings)
        {
            Layout = layout;
            Root = root;
            SurvivalVisuals.Stretch(root);
            var viewport = SurvivalMapRig.ViewportGraphic(root, "Viewport");
            viewport.color = new Color(0.33f, 0.47f, 0.20f, 1f); // lawn green behind the tiles while they load
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport.transform, false);
            Content = content;

            Tiled(content, "Lawn", WorldArtKeys.VillageLawn, 0f, 0f, layout.Width, layout.Height, new Color(0.38f, 0.55f, 0.24f, 1f));
            foreach (var road in layout.Roads)
            {
                var (x0, y0) = layout.ToMap(road.X0, road.Y0FromTop);
                var (x1, y1) = layout.ToMap(road.X1, road.Y1FromTop);
                Road(content, "Road", new Vector2(x0, y0), new Vector2(x1, y1), road.Width);
            }

            // Draw north first so southern things overlap them (simple painter's order on y).
            var items = new List<(float YTop, Action Make)>();
            foreach (var prop in layout.Props)
            {
                var p = prop;
                items.Add((p.YFromTop, () => Prop(content, p)));
            }

            foreach (var spot in layout.EmptyPlots)
            {
                var s = spot;
                items.Add((s.YFromTop, () => Empty(content, s, strings)));
            }

            foreach (var spot in layout.Plots)
            {
                var s = spot;
                items.Add((s.YFromTop, () => _hosts[s.PlotId] = MakeHost(content, s)));
            }

            items.Sort((a, b) => a.YTop.CompareTo(b.YTop));
            foreach (var item in items)
            {
                item.Make();
            }

            Fx = new GameObject("Fx", typeof(RectTransform)).GetComponent<RectTransform>();
            Fx.SetParent(content, false);
            Fx.anchorMin = Vector2.zero;
            Fx.anchorMax = Vector2.zero;
            Fx.pivot = Vector2.zero;
            Fx.sizeDelta = new Vector2(layout.Width, layout.Height);

            var cam = new MapCamera(layout.Width, layout.Height, 1080f, 1920f, layout.MinZoom, layout.MaxZoom, layout.StartZoom);
            var castle = layout.Plots.Count > 0 ? layout.Plots[0] : null;
            var (fx, fy) = castle != null ? layout.ToMap(castle.X, castle.YFromTop + 0.14f) : (layout.Width * 0.5f, layout.Height * 0.5f);
            cam.CenterOn(fx, fy);
            Rig = viewport.gameObject.AddComponent<SurvivalMapRig>();
            Rig.Init(viewport.rectTransform, content, cam);
            Rig.Moved += OnMoved;
            OnMoved(cam);
        }

        /// <summary>Unlock signs on empty plots follow the castle level.</summary>
        public void RefreshUnlocks(int hqLevel, Func<string, string, string> strings)
        {
            if (hqLevel == _shownHq)
            {
                return;
            }

            _shownHq = hqLevel;
            foreach (var (spot, sign, art) in _empty)
            {
                var open = hqLevel >= spot.UnlockCastleLevel;
                sign.text = open
                    ? strings("village.slot.open", "Build soon")
                    : string.Format(CultureInfo.InvariantCulture, strings("village.slot.locked", "Castle Lv {0}"), spot.UnlockCastleLevel);
                art.color = open ? Color.white : new Color(0.78f, 0.78f, 0.78f, 1f);
            }
        }

        /// <summary>Resize a plot host's footprint (Castle tiers): size is the fraction of map width at scale 1.</summary>
        public void SetFootprint(string plotId, float scale)
        {
            var host = Host(plotId);
            var spot = Layout?.SpotFor(plotId);
            if (host == null || spot == null || Layout == null)
            {
                return;
            }

            var px = spot.Size * Layout.Width * scale;
            host.sizeDelta = new Vector2(px, px);
        }

        public float FootprintScaleOf(string plotId)
        {
            var host = Host(plotId);
            var spot = Layout?.SpotFor(plotId);
            if (host == null || spot == null || Layout == null || spot.Size <= 0f)
            {
                return 1f;
            }

            return host.sizeDelta.x / (spot.Size * Layout.Width);
        }

        /// <summary>Counter-scale name plates a little so they stay readable when zoomed out.</summary>
        public void RegisterLabel(RectTransform label)
        {
            _labels.Add(label);
            if (Rig?.Camera != null)
            {
                OnMoved(Rig.Camera);
            }
        }

        private void OnMoved(MapCamera cam)
        {
            var k = Mathf.Clamp(0.95f / cam.Zoom, 0.85f, 1.45f);
            for (var i = _labels.Count - 1; i >= 0; i--)
            {
                if (_labels[i] == null)
                {
                    _labels.RemoveAt(i);
                    continue;
                }

                _labels[i].localScale = new Vector3(k, k, 1f);
            }
        }

        private RectTransform MakeHost(RectTransform content, VillagePlotSpot spot)
        {
            var host = new GameObject("Plot_" + spot.PlotId, typeof(RectTransform)).GetComponent<RectTransform>();
            host.SetParent(content, false);
            var (x, y) = Layout!.ToMap(spot.X, spot.YFromTop);
            host.anchorMin = Vector2.zero;
            host.anchorMax = Vector2.zero;
            host.pivot = new Vector2(0.5f, 0.32f); // the building's base sits on the plot point; growth goes up
            host.anchoredPosition = new Vector2(x, y);
            var px = spot.Size * Layout.Width;
            host.sizeDelta = new Vector2(px, px);
            return host;
        }

        private void Empty(RectTransform content, VillageEmptySpot spot, Func<string, string, string> strings)
        {
            var (x, y) = Layout!.ToMap(spot.X, spot.YFromTop);
            var px = spot.Size * Layout.Width;
            var art = HookSkin.Art(content, "Slot_" + spot.Id, WorldArtKeys.PlotEmpty, new Color(0.45f, 0.33f, 0.2f, 1f));
            Point(art.rectTransform, x, y, px, px, 0.4f);
            var plate = HookSkin.Panel(art.transform, "Sign", HookArtKeys.Chip, SurvivalVisuals.PanelSoft, 0.4f);
            Box(plate.rectTransform, 0.08f, -0.08f, 0.92f, 0.10f);
            var sign = HookSkin.Outlined(SurvivalVisuals.Text(plate.transform, "T", "", 26, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
            sign.fontStyle = FontStyle.Bold;
            SurvivalVisuals.Stretch(sign.rectTransform);
            _empty.Add((spot, sign, art));
            RegisterLabel(plate.rectTransform);
            sign.text = string.Format(CultureInfo.InvariantCulture, strings("village.slot.locked", "Castle Lv {0}"), spot.UnlockCastleLevel);
        }

        private void Prop(RectTransform content, VillageProp prop)
        {
            var key = HookSkin.Has(WorldArtKeys.VillageProp(prop.Type)) ? WorldArtKeys.VillageProp(prop.Type) : WorldArtKeys.MapProp(prop.Type);
            if (!HookSkin.Has(key))
            {
                return;
            }

            var img = HookSkin.Art(content, "Prop_" + prop.Type, key, Color.white);
            var (x, y) = Layout!.ToMap(prop.X, prop.YFromTop);
            var size = (prop.Type == "bushes" ? 200f : 380f) * prop.Scale;
            Point(img.rectTransform, x, y, size, size, 0.35f);
        }

        internal static Image Tiled(RectTransform content, string name, string key, float x, float y, float w, float h, Color fallback)
        {
            var sprite = SurvivalArt.Get(key);
            var img = SurvivalVisuals.Image(content, name, sprite != null ? Color.white : fallback, sprite);
            if (sprite != null)
            {
                img.type = Image.Type.Tiled;
            }

            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            return img;
        }

        /// <summary>A dirt road strip: tiled dirt rotated along the segment, soft-edged by a darker under-strip.</summary>
        internal static Image Road(RectTransform content, string name, Vector2 a, Vector2 b, float width)
        {
            var edge = SurvivalVisuals.Image(content, name + "Edge", new Color(0.24f, 0.18f, 0.10f, 0.35f));
            Strip(edge.rectTransform, a, b, width * 1.18f);
            var road = Tiled(content, name, WorldArtKeys.RoadTile, 0f, 0f, 1f, 1f, new Color(0.55f, 0.43f, 0.28f, 1f));
            Strip(road.rectTransform, a, b, width);
            return road;
        }

        internal static void Strip(RectTransform rt, Vector2 a, Vector2 b, float width)
        {
            var d = b - a;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = a;
            rt.sizeDelta = new Vector2(d.magnitude + width * 0.5f, width);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        /// <summary>Place a rect at a content point (bottom-left origin) with a pivot height (0 = feet on the point).</summary>
        internal static void Point(RectTransform rt, float x, float y, float w, float h, float pivotY)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, pivotY);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void Box(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
