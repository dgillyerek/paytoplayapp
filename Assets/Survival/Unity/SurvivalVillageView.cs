#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Base;
using Survival.Domain.Theme;
using Survival.Domain.View;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Zoomed-in Village view: a scrollable lawn (map/village_layout.json) of grass and props - no roads - with one host per
    /// base plot (the hook view fills each with its building, badge, bubble and timer) and free build spots that light
    /// up while the player is placing a new building (Build). The Castle host grows with its painted stage.
    /// </summary>
    public sealed class SurvivalVillageView : MonoBehaviour
    {
        private readonly Dictionary<string, RectTransform> _hosts = new(StringComparer.Ordinal);
        private readonly Dictionary<string, (VillageBuildSpot Spot, RectTransform Marker, Image Glow)> _spots = new(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _hostBase = new(StringComparer.Ordinal);
        private readonly List<RectTransform> _labels = new();
        private bool _placing;

        /// <summary>A highlighted free spot was tapped while placing (spot id).</summary>
        public event Action<string>? SpotTapped;

        public bool Placing => _placing;

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
            // Draw north first so southern things overlap them (simple painter's order on y).
            var items = new List<(float YTop, Action Make)>();
            foreach (var prop in layout.Props)
            {
                var p = prop;
                items.Add((p.YFromTop, () => Prop(content, p)));
            }

            foreach (var spot in layout.BuildSpots)
            {
                var s = spot;
                items.Add((s.YFromTop, () => SpotMarker(content, s)));
            }

            foreach (var spot in layout.Plots)
            {
                var s = spot;
                items.Add((s.YFromTop, () => _hosts[s.PlotId] = MakeHost(content, s.PlotId, s.X, s.YFromTop, s.Size)));
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
            // Start a little south of the castle (400 map px) so the castle and the first buildings frame the screen.
            var (fx, fy) = castle != null ? layout.ToMap(castle.X, castle.YFromTop + 400f / layout.Height) : (layout.Width * 0.5f, layout.Height * 0.5f);
            cam.CenterOn(fx, fy);
            Rig = viewport.gameObject.AddComponent<SurvivalMapRig>();
            Rig.Init(viewport.rectTransform, content, cam);
            Rig.Moved += OnMoved;
            OnMoved(cam);
        }

        /// <summary>
        /// Placement mode: light up <paramref name="freeSpots"/> (tap one to build there); null turns it off.
        /// Returns the first lit spot so the camera can glide to it.
        /// </summary>
        public RectTransform? ShowPlacement(IEnumerable<string>? freeSpots)
        {
            _placing = freeSpots != null;
            var free = freeSpots != null ? new HashSet<string>(freeSpots, StringComparer.Ordinal) : new HashSet<string>(StringComparer.Ordinal);
            RectTransform? first = null;
            foreach (var spot in Layout?.BuildSpots ?? Array.Empty<VillageBuildSpot>())
            {
                if (!_spots.TryGetValue(spot.Id, out var m))
                {
                    continue;
                }

                var on = free.Contains(spot.Id);
                m.Marker.gameObject.SetActive(on);
                if (on && first == null)
                {
                    first = m.Marker;
                }
            }

            return first;
        }

        /// <summary>Pulse the lit spots (call every frame while placing).</summary>
        public void TickPlacement(float time)
        {
            if (!_placing)
            {
                return;
            }

            var a = 0.55f + 0.35f * Mathf.Sin(time * 5f);
            var k = 1f + 0.05f * Mathf.Sin(time * 5f);
            foreach (var m in _spots.Values)
            {
                if (m.Marker.gameObject.activeSelf)
                {
                    m.Glow.color = new Color(1f, 0.86f, 0.35f, a);
                    m.Marker.localScale = new Vector3(k, k, 1f);
                }
            }
        }

        /// <summary>Map point of a build spot (content space), for camera glides and pops.</summary>
        public Vector2? SpotPoint(string spotId)
        {
            var spot = Layout?.BuildSpot(spotId);
            if (spot == null || Layout == null)
            {
                return null;
            }

            var (x, y) = Layout.ToMap(spot.X, spot.YFromTop);
            return new Vector2(x, y);
        }

        /// <summary>Host for a building placed with Build on <paramref name="spotId"/>, drawn in y order with the rest.</summary>
        public RectTransform? AddHost(string plotId, string spotId)
        {
            if (_hosts.TryGetValue(plotId, out var existing))
            {
                return existing;
            }

            var spot = Layout?.BuildSpot(spotId);
            if (spot == null || Content == null)
            {
                return null;
            }

            var host = MakeHost(Content, plotId, spot.X, spot.YFromTop, spot.Size * 1.06f);
            _hosts[plotId] = host;
            SortInto(host);
            if (_spots.TryGetValue(spotId, out var m))
            {
                m.Marker.gameObject.SetActive(false);
            }

            return host;
        }

        /// <summary>Move <paramref name="rt"/> among the content's children so south (lower y) draws over north.</summary>
        private void SortInto(RectTransform rt)
        {
            if (Content == null)
            {
                return;
            }

            var y = rt.anchoredPosition.y;
            var index = Content.childCount - 1;
            for (var i = 1; i < Content.childCount; i++)
            {
                var c = Content.GetChild(i) as RectTransform;
                if (c == null || c == rt || c == Fx)
                {
                    continue;
                }

                if (c.anchoredPosition.y < y)
                {
                    index = i;
                    break;
                }
            }

            rt.SetSiblingIndex(index);
            Fx?.SetAsLastSibling();
        }

        /// <summary>Resize a plot host's footprint (Castle tiers): size is the fraction of map width at scale 1.</summary>
        public void SetFootprint(string plotId, float scale)
        {
            var host = Host(plotId);
            if (host == null || !_hostBase.TryGetValue(plotId, out var basePx))
            {
                return;
            }

            var px = basePx * scale;
            host.sizeDelta = new Vector2(px, px);
        }

        public float FootprintScaleOf(string plotId)
        {
            var host = Host(plotId);
            if (host == null || !_hostBase.TryGetValue(plotId, out var basePx) || basePx <= 0f)
            {
                return 1f;
            }

            return host.sizeDelta.x / basePx;
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

        private RectTransform MakeHost(RectTransform content, string plotId, float nx, float ny, float size)
        {
            var host = new GameObject("Plot_" + plotId, typeof(RectTransform)).GetComponent<RectTransform>();
            host.SetParent(content, false);
            var (x, y) = Layout!.ToMap(nx, ny);
            host.anchorMin = Vector2.zero;
            host.anchorMax = Vector2.zero;
            host.pivot = new Vector2(0.5f, 0.32f); // the building's base sits on the plot point; growth goes up
            host.anchoredPosition = new Vector2(x, y);
            var px = size * Layout.Width;
            host.sizeDelta = new Vector2(px, px);
            _hostBase[plotId] = px;
            return host;
        }

        /// <summary>A free build spot: dirt plot + gold glow + hammer, hidden until the player is placing.</summary>
        private void SpotMarker(RectTransform content, VillageBuildSpot spot)
        {
            var (x, y) = Layout!.ToMap(spot.X, spot.YFromTop);
            var px = spot.Size * Layout.Width;
            var marker = new GameObject("Spot_" + spot.Id, typeof(RectTransform)).GetComponent<RectTransform>();
            marker.SetParent(content, false);
            Point(marker, x, y, px, px * 0.75f, 0.5f);
            var glow = HookSkin.Glow(marker, "Glow", new Color(1f, 0.86f, 0.35f, 0.7f));
            Box(glow.rectTransform, -0.18f, -0.25f, 1.18f, 1.25f);
            var dirt = HookSkin.Art(marker, "Dirt", WorldArtKeys.PlotEmpty, new Color(0.45f, 0.33f, 0.2f, 0.9f));
            Box(dirt.rectTransform, 0f, 0f, 1f, 1f);
            var hammer = HookSkin.Art(marker, "Hammer", BottomNav.IconBuild, SurvivalVisuals.Gold);
            Box(hammer.rectTransform, 0.30f, 0.30f, 0.70f, 0.95f);
            var hit = SurvivalVisuals.Button(marker, "Hit", new Color(1f, 1f, 1f, 0f));
            Box((RectTransform)hit.transform, -0.05f, -0.1f, 1.05f, 1.2f);
            var id = spot.Id;
            hit.onClick.AddListener(() =>
            {
                if (_placing)
                {
                    SpotTapped?.Invoke(id);
                }
            });
            marker.gameObject.SetActive(false);
            _spots[spot.Id] = (spot, marker, glow);
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
            var size = PropSize(prop.Type) * prop.Scale;
            Point(img.rectTransform, x, y, size, size, 0.35f);
        }

        /// <summary>Base draw size (content px at scale 1) per decor type.</summary>
        internal static float PropSize(string type) => type switch
        {
            "bushes" => 200f,
            "tree" => 280f,
            "rocks" => 260f,
            "field" => 340f,
            _ => 380f
        };

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
