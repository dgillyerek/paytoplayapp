#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Survival.Domain.Ids;
using Survival.Domain.Session;
using Survival.Domain.Theme;
using Survival.Domain.View;
using Survival.Domain.World;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Zoomed-out World view: the kingdom map (map/world_layout.json) with the painted crusade map and its six
    /// catalog nodes in the middle, player / rival / ally villages, enemy camps of four types and many levels,
    /// resource fields, props, and live marches (dashed path + walking troop marker) simulated by
    /// <see cref="WorldSim"/>. Tapping anything opens a small info card with its action
    /// (Attack / Gather / Scout / Reinforce / the node's March loop / Enter Village).
    /// </summary>
    public sealed class SurvivalWorldView : MonoBehaviour
    {
        private static readonly Color PlayerLine = new Color(0.45f, 0.76f, 1f, 0.95f);
        private static readonly Color AllyLine = new Color(0.52f, 0.95f, 0.52f, 0.9f);
        private static readonly Color RivalLine = new Color(1f, 0.42f, 0.36f, 0.9f);
        private static readonly Color Warn = new Color(1f, 0.55f, 0.45f, 1f);
        private static Sprite? _dash;

        private readonly Dictionary<string, EntityView> _entities = new(StringComparer.Ordinal);
        private readonly Dictionary<int, MarchView> _marches = new();
        private readonly Dictionary<string, int> _resourceLeft = new(StringComparer.Ordinal);
        private readonly List<RectTransform> _labels = new();
        private readonly HashSet<int> _alive = new();
        private readonly List<int> _dead = new();

        private SurvivalPlayView? _play;
        private SurvivalSession? _session;
        private RectTransform? _hud;
        private RectTransform? _paths;
        private RectTransform? _entityLayer;
        private RectTransform? _marchLayer;
        private RectTransform? _labelLayer;
        private RectTransform? _fx;
        private Image? _selection;
        private string? _selectedId;
        private Card? _card;
        private Action? _cardRefresh;

        public WorldLayout? Layout { get; private set; }
        public WorldSim? Sim { get; private set; }
        public RectTransform? Root { get; private set; }
        public SurvivalMapRig? Rig { get; private set; }

        /// <summary>The player tapped "Enter Village" on their own village card.</summary>
        public event Action? EnterVillage;

        /// <summary>
        /// Set by the Heroes / Battle slice: Attack on an enemy camp opens Battle Setup for (camp, display name)
        /// instead of sending a march. Null keeps the march.
        /// </summary>
        public Action<WorldEntity, string>? AttackCamp { get; set; }

        private sealed class EntityView
        {
            public WorldEntity Entity = null!;
            public RectTransform Root = null!;
            public Text Line2 = null!;
        }

        private sealed class MarchView
        {
            public RectTransform Marker = null!;
            public Image Art = null!;
            public RectTransform Path = null!;
            public Image PathImg = null!;
            public Text? Timer;
            public MarchLeg Leg;
        }

        private sealed class Card
        {
            public RectTransform Root = null!;
            public Image Icon = null!;
            public Text Title = null!;
            public Text Line1 = null!;
            public Text Line2 = null!;
            public Button Action = null!;
            public Image ActionImg = null!;
            public Text ActionText = null!;
            public Button Close = null!;
        }

        public static SurvivalWorldView Create(RectTransform parent, RectTransform hud, SurvivalPlayView play, SurvivalSession session, WorldLayout layout)
        {
            var root = new GameObject("WorldView", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            var view = root.AddComponent<SurvivalWorldView>();
            view.Build((RectTransform)root.transform, hud, play, session, layout);
            return view;
        }

        private double Now => _session?.Hook != null ? _session.Hook.Clock.Now : Time.unscaledTimeAsDouble;

        private string S(string key, string fallback) => _session != null ? _session.Pack.StringOr(key, fallback) : fallback;

        private string F(string key, string fallback, params object[] args) => string.Format(CultureInfo.InvariantCulture, S(key, fallback), args);

        private void Build(RectTransform root, RectTransform hud, SurvivalPlayView play, SurvivalSession session, WorldLayout baseLayout)
        {
            _play = play;
            _session = session;
            _hud = hud;
            Root = root;
            SurvivalVisuals.Stretch(root);

            var nodes = new List<(string, string, string, float, float)>();
            foreach (var n in session.World.Nodes)
            {
                nodes.Add((n.Id, n.NodeTypeId, session.Pack.StringOr("pin." + n.Id, n.Id), n.Pin.PinX, n.Pin.PinYFromTop));
            }

            var layout = baseLayout.WithLandmarks(nodes);
            Layout = layout;
            Sim = new WorldSim(layout, Now);
            Sim.Arrived += OnPlayerArrived;
            foreach (var e in layout.Entities)
            {
                if (e.Kind == WorldEntityKind.ResourceNode)
                {
                    _resourceLeft[e.Id] = e.Amount;
                }
            }

            var viewport = SurvivalMapRig.ViewportGraphic(root, "Viewport");
            viewport.color = new Color(0.30f, 0.36f, 0.18f, 1f);
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport.transform, false);

            SurvivalVillageView.Tiled(content, "Ground", WorldArtKeys.MapTile, 0f, 0f, layout.Width, layout.Height, new Color(0.35f, 0.42f, 0.2f, 1f));
            var heart = SurvivalArt.Get(WorldArtKeys.MapHeart) ?? SurvivalArt.Get("map.terrain");
            if (heart != null)
            {
                var h = SurvivalVisuals.Image(content, "CrusadeMap", Color.white, heart);
                Corner(h.rectTransform, layout.HeartX, layout.HeartY, layout.HeartWidth, layout.HeartHeight);
            }

            var player = layout.Player!;
            if (Sim.TryGet(SurvIds.WorldNodeHome01, out var home))
            {
                SurvivalVillageView.Road(content, "HomeRoad", new Vector2(player.X, player.Y), new Vector2(home.X, home.Y), 70f);
            }

            _paths = Layer(content, "Paths", layout);
            var props = Layer(content, "Props", layout);
            _entityLayer = Layer(content, "Entities", layout);
            _marchLayer = Layer(content, "Marches", layout);
            _labelLayer = Layer(content, "Labels", layout);
            _fx = Layer(content, "Fx", layout);

            var sortedProps = new List<WorldProp>(layout.Props);
            sortedProps.Sort((a, b) => b.Y.CompareTo(a.Y));
            foreach (var p in sortedProps)
            {
                var key = WorldArtKeys.MapProp(p.Type);
                if (!HookSkin.Has(key))
                {
                    continue;
                }

                var img = HookSkin.Art(props, "Prop_" + p.Type, key, Color.white);
                var size = (p.Type == "mountains" ? 440f : p.Type == "lake" ? 360f : 340f) * p.Scale;
                SurvivalVillageView.Point(img.rectTransform, p.X, p.Y, size, size, 0.3f);
            }

            _selection = SurvivalVisuals.Ring(_paths, "Selection", new Color(1f, 0.85f, 0.35f, 0f));
            _selection.raycastTarget = false;

            var sorted = new List<WorldEntity>(layout.Entities);
            sorted.Sort((a, b) => b.Y.CompareTo(a.Y));
            foreach (var e in sorted)
            {
                SpawnEntity(e);
            }

            var cam = new MapCamera(layout.Width, layout.Height, 1080f, 1920f, layout.MinZoom, layout.MaxZoom, layout.StartZoom);
            cam.CenterOn(player.X + 180f, player.Y - 120f);
            Rig = viewport.gameObject.AddComponent<SurvivalMapRig>();
            Rig.Init(viewport.rectTransform, content, cam);
            Rig.Moved += OnMoved;
            Rig.EmptyTap += CloseCard;
            OnMoved(cam);
            SyncMarches();
        }

        private static RectTransform Layer(RectTransform content, string name, WorldLayout layout)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(content, false);
            Corner(rt, 0f, 0f, layout.Width, layout.Height);
            return rt;
        }

        private static void Corner(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        // ---------- entities ----------

        private string ArtFor(WorldEntity e)
        {
            if (e.Kind != WorldEntityKind.Landmark)
            {
                return WorldArtKeys.For(e);
            }

            var key = "";
            if (_session != null && _session.World.TryGet(e.Id, out var node))
            {
                key = node.Pin.ContentKey;
            }

            // The small "_map" quarry chip reads poorly at map scale; prefer the full painting when bound.
            if (key.EndsWith("_map", StringComparison.Ordinal) && HookSkin.Has(key.Substring(0, key.Length - 4)))
            {
                key = key.Substring(0, key.Length - 4);
            }

            return key;
        }

        private static float SizeFor(WorldEntity e) => e.Kind switch
        {
            WorldEntityKind.PlayerVillage => 330f,
            WorldEntityKind.RivalVillage or WorldEntityKind.AllyVillage => 270f,
            WorldEntityKind.EnemyCamp => e.Type == "dragon" ? 330f : e.Level >= 10 ? 270f : 240f,
            WorldEntityKind.ResourceNode => 220f,
            _ => e.Type == SurvIds.WorldNodeTypeFight ? 320f : 260f
        };

        private string NameOf(WorldEntity e) => e.Kind switch
        {
            WorldEntityKind.EnemyCamp => S("world.camp." + e.Type, e.Type),
            WorldEntityKind.ResourceNode => S("world.node." + e.Type, e.Type),
            WorldEntityKind.Landmark => e.Name,
            WorldEntityKind.PlayerVillage => S("hud.player_name", e.Name),
            _ => e.Name
        };

        private string SubOf(WorldEntity e) => e.Kind switch
        {
            WorldEntityKind.EnemyCamp => F("world.card.level", "Lv {0}", e.Level),
            WorldEntityKind.ResourceNode => ChipWallet.FormatCompact(_resourceLeft.TryGetValue(e.Id, out var left) ? left : e.Amount),
            WorldEntityKind.Landmark => "",
            _ => F("world.card.power", "Power {0}", ChipWallet.FormatCompact(e.Power)) // plain text: the built-in font has no sword glyph
        };

        private void SpawnEntity(WorldEntity e)
        {
            var size = SizeFor(e);
            var art = HookSkin.Art(_entityLayer!, e.Id, ArtFor(e), LineFor(e));
            var rt = art.rectTransform;
            SurvivalVillageView.Point(rt, e.X, e.Y, size, size, 0.28f);
            art.raycastTarget = true;
            if (art.sprite != null && art.sprite.texture != null && art.sprite.texture.isReadable)
            {
                art.alphaHitTestMinimumThreshold = 0.08f;
            }

            if (e.Kind == WorldEntityKind.PlayerVillage)
            {
                var glow = HookSkin.Glow(_paths!, "PlayerGlow", new Color(1f, 0.86f, 0.42f, 0.55f));
                SurvivalVillageView.Point(glow.rectTransform, e.X, e.Y, size * 1.45f, size * 1.0f, 0.5f);
                var ring = SurvivalVisuals.Ring(_paths!, "PlayerRing", new Color(0.45f, 0.76f, 1f, 0.9f));
                SurvivalVillageView.Point(ring.rectTransform, e.X, e.Y, size * 1.05f, size * 0.55f, 0.5f);
                ring.preserveAspect = false;
            }

            var btn = art.gameObject.AddComponent<Button>();
            btn.targetGraphic = art;
            btn.transition = Selectable.Transition.None;
            var id = e.Id;
            btn.onClick.AddListener(() => OnTapEntity(id));

            // Name plate (counter-scaled with zoom so it stays legible zoomed out).
            var plateKey = e.Kind switch
            {
                WorldEntityKind.PlayerVillage => HookArtKeys.ButtonGold,
                WorldEntityKind.EnemyCamp => HookArtKeys.RibbonRed,
                WorldEntityKind.AllyVillage => HookArtKeys.ButtonGreen,
                _ => HookArtKeys.Chip
            };
            var plate = HookSkin.Panel(_labelLayer!, e.Id + "_plate", plateKey, SurvivalVisuals.PanelSoft, 0.38f);
            var hasSub = SubOf(e).Length > 0;
            SurvivalVillageView.Point(plate.rectTransform, e.X, e.Y - size * 0.30f, 290f, hasSub ? 78f : 52f, 1f);
            var ink = plateKey == HookArtKeys.ButtonGold ? HookSkin.Ink : SurvivalVisuals.Cream;
            var name = SurvivalVisuals.Text(plate.transform, "Name", NameOf(e), 24, TextAnchor.MiddleCenter, ink);
            name.fontStyle = FontStyle.Bold;
            if (plateKey != HookArtKeys.ButtonGold)
            {
                HookSkin.Outlined(name, 1.5f);
            }

            Box(name.rectTransform, 0.04f, hasSub ? 0.45f : 0f, 0.96f, 1f);
            var sub = HookSkin.Outlined(SurvivalVisuals.Text(plate.transform, "Sub", SubOf(e), 20, TextAnchor.MiddleCenter, plateKey == HookArtKeys.ButtonGold ? HookSkin.Ink : SurvivalVisuals.Reward), 1.2f);
            Box(sub.rectTransform, 0.04f, 0.04f, 0.96f, 0.5f);
            sub.gameObject.SetActive(hasSub);
            _labels.Add(plate.rectTransform);

            if (e.Kind == WorldEntityKind.PlayerVillage)
            {
                var you = HookSkin.Panel(plate.transform, "You", HookArtKeys.RibbonBlue, SurvivalVisuals.PinBuild, 0.3f);
                Box(you.rectTransform, 0.22f, 0.98f, 0.78f, 1.62f);
                var t = HookSkin.Outlined(SurvivalVisuals.Text(you.transform, "T", S("world.card.you", "Your village"), 20, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
                t.fontStyle = FontStyle.Bold;
                SurvivalVisuals.Stretch(t.rectTransform);
            }

            _entities[e.Id] = new EntityView { Entity = e, Root = rt, Line2 = sub };
        }

        private Color LineFor(WorldEntity e) => e.Kind switch
        {
            WorldEntityKind.PlayerVillage => PlayerLine,
            WorldEntityKind.AllyVillage => AllyLine,
            WorldEntityKind.EnemyCamp => RivalLine,
            WorldEntityKind.ResourceNode => SurvivalVisuals.Reward,
            _ => SurvivalVisuals.PinGather
        };

        private Color OwnerLine(string ownerId) =>
            Sim != null && Sim.TryGet(ownerId, out var o)
                ? o.Kind == WorldEntityKind.PlayerVillage ? PlayerLine : o.Kind == WorldEntityKind.AllyVillage ? AllyLine : RivalLine
                : RivalLine;

        private void OnMoved(MapCamera cam)
        {
            var k = Mathf.Clamp(0.8f / cam.Zoom, 0.75f, 1.6f);
            foreach (var l in _labels)
            {
                if (l != null)
                {
                    l.localScale = new Vector3(k, k, 1f);
                }
            }
        }

        // ---------- per frame ----------

        /// <summary>Advance the map simulation (called by the Play shell every frame, even while this view is hidden).</summary>
        public void TickSim() => Sim?.Tick(Now);

        private void Update()
        {
            if (Sim == null)
            {
                return;
            }

            Sim.Tick(Now);
            SyncMarches();
            AnimateSelection();
            _cardRefresh?.Invoke();
        }

        private void SyncMarches()
        {
            if (Sim == null || _marchLayer == null || _paths == null)
            {
                return;
            }

            var now = Sim.Now;
            _alive.Clear();
            foreach (var m in Sim.Marches)
            {
                if (m.IsWaiting(now))
                {
                    continue;
                }

                _alive.Add(m.Serial);
                if (!_marches.TryGetValue(m.Serial, out var v))
                {
                    v = MakeMarch(m);
                    _marches[m.Serial] = v;
                }

                var (x, y) = m.Position(now);
                var bob = Mathf.Sin(Time.unscaledTime * 9f + m.Serial) * 5f;
                v.Marker.anchoredPosition = new Vector2(x, y + bob);
                v.Marker.localScale = new Vector3(m.FacingLeft ? -1f : 1f, 1f, 1f);
                if (v.Timer != null)
                {
                    v.Timer.text = Clock(m.Remaining(now));
                    v.Timer.rectTransform.localScale = new Vector3(m.FacingLeft ? -1f : 1f, 1f, 1f);
                }

                SurvivalVillageView.Strip(v.Path, new Vector2(x, y), new Vector2(m.ToX, m.ToY), 12f);
                var c = v.PathImg.color;
                v.PathImg.color = new Color(c.r, c.g, c.b, m.Leg == MarchLeg.Return ? 0.45f : 0.9f);
            }

            _dead.Clear();
            foreach (var serial in _marches.Keys)
            {
                if (!_alive.Contains(serial))
                {
                    _dead.Add(serial);
                }
            }

            foreach (var serial in _dead)
            {
                var v = _marches[serial];
                Destroy(v.Marker.gameObject);
                Destroy(v.Path.gameObject);
                _marches.Remove(serial);
            }
        }

        private MarchView MakeMarch(WorldMarch m)
        {
            var color = OwnerLine(m.OwnerId);
            var path = SurvivalVisuals.Image(_paths!, "Path_" + m.Serial, color, DashSprite());
            path.type = Image.Type.Tiled;
            var key = m.IsPlayer || (Sim!.TryGet(m.OwnerId, out var o) && o.Kind == WorldEntityKind.AllyVillage) ? WorldArtKeys.MarchPlayer : WorldArtKeys.MarchRival;
            var art = HookSkin.Art(_marchLayer!, "March_" + m.Serial, key, color);
            if (!m.IsPlayer && key == WorldArtKeys.MarchPlayer)
            {
                art.color = new Color(0.82f, 1f, 0.82f, 1f); // allies: player troops with a green cast
            }

            var size = m.IsPlayer ? 170f : 140f;
            SurvivalVillageView.Point(art.rectTransform, m.FromX, m.FromY, size, size, 0.2f);
            art.raycastTarget = true;
            var btn = art.gameObject.AddComponent<Button>();
            btn.targetGraphic = art;
            btn.transition = Selectable.Transition.None;
            var serial = m.Serial;
            btn.onClick.AddListener(() => OnTapMarch(serial));
            var view = new MarchView { Marker = art.rectTransform, Art = art, Path = path.rectTransform, PathImg = path, Leg = m.Leg };
            if (m.IsPlayer)
            {
                var chip = HookSkin.Panel(art.transform, "Clock", HookArtKeys.Chip, SurvivalVisuals.PanelSoft, 0.3f);
                Box(chip.rectTransform, 0.08f, 0.98f, 0.92f, 1.26f);
                view.Timer = HookSkin.Outlined(SurvivalVisuals.Text(chip.transform, "T", "", 22, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
                view.Timer.fontStyle = FontStyle.Bold;
                SurvivalVisuals.Stretch(view.Timer.rectTransform);
            }

            return view;
        }

        private static Sprite DashSprite()
        {
            if (_dash != null)
            {
                return _dash;
            }

            const int w = 48;
            const int h = 12;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    // Rounded dash 26 px long, soft edge.
                    var cx = Mathf.Clamp(x, 6, 20);
                    var d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, (h - 1) * 0.5f));
                    var a = x <= 26 ? Mathf.Clamp01(5.2f - d) : 0f;
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            _dash = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            return _dash;
        }

        private void AnimateSelection()
        {
            if (_selection == null)
            {
                return;
            }

            if (_selectedId == null || !_entities.TryGetValue(_selectedId, out var v))
            {
                _selection.color = new Color(1f, 0.85f, 0.35f, 0f);
                return;
            }

            var e = v.Entity;
            var size = SizeFor(e) * (1.05f + 0.06f * Mathf.Sin(Time.unscaledTime * 5f));
            SurvivalVillageView.Point(_selection.rectTransform, e.X, e.Y, size, size * 0.55f, 0.5f);
            _selection.preserveAspect = false;
            _selection.color = new Color(1f, 0.85f, 0.35f, 0.65f + 0.3f * Mathf.Sin(Time.unscaledTime * 5f));
        }

        // ---------- taps + cards ----------

        public void CloseCard()
        {
            _cardRefresh = null;
            _selectedId = null;
            if (_card != null)
            {
                _card.Root.gameObject.SetActive(false);
            }
        }

        private Card EnsureCard()
        {
            if (_card != null)
            {
                _card.Root.SetAsLastSibling();
                _play?.Fx?.SetAsLastSibling();
                return _card;
            }

            var panel = HookSkin.Panel(_hud!, "WorldCard", HookArtKeys.Panel, SurvivalVisuals.InspectFill, 0.7f);
            panel.raycastTarget = true;
            if (_session != null && _session.Pack.Hud.TryGetValue("map_card", out var slot))
            {
                SurvivalVisuals.Place(panel.rectTransform, slot);
            }
            else
            {
                Box(panel.rectTransform, 0.03f, 0.235f, 0.97f, 0.415f);
            }

            var c = new Card { Root = panel.rectTransform };
            c.Icon = SurvivalVisuals.Image(panel.transform, "Icon", Color.white);
            c.Icon.preserveAspect = true;
            Box(c.Icon.rectTransform, 0.02f, 0.08f, 0.27f, 0.98f);
            c.Title = HookSkin.Outlined(SurvivalVisuals.Text(panel.transform, "Title", "", 34, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 2f);
            c.Title.fontStyle = FontStyle.Bold;
            Box(c.Title.rectTransform, 0.29f, 0.70f, 0.88f, 0.94f);
            c.Line1 = HookSkin.Outlined(SurvivalVisuals.Text(panel.transform, "Line1", "", 25, TextAnchor.MiddleLeft, SurvivalVisuals.Reward), 1.5f);
            Box(c.Line1.rectTransform, 0.29f, 0.52f, 0.97f, 0.70f);
            c.Line2 = HookSkin.Outlined(SurvivalVisuals.Text(panel.transform, "Line2", "", 22, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.5f);
            Box(c.Line2.rectTransform, 0.29f, 0.36f, 0.97f, 0.52f);
            c.Action = HookSkin.Button(panel.transform, "Action", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, 0.5f);
            c.ActionImg = c.Action.GetComponent<Image>();
            Box(c.ActionImg.rectTransform, 0.29f, 0.07f, 0.70f, 0.34f);
            c.ActionText = HookSkin.Outlined(SurvivalVisuals.Text(c.Action.transform, "T", "", 28, TextAnchor.MiddleCenter, HookSkin.Ink), 1.5f);
            c.ActionText.fontStyle = FontStyle.Bold;
            SurvivalVisuals.Stretch(c.ActionText.rectTransform);
            c.Close = HookSkin.Button(panel.transform, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.5f);
            Box(c.Close.GetComponent<RectTransform>(), 0.72f, 0.07f, 0.97f, 0.34f);
            var closeText = HookSkin.Outlined(SurvivalVisuals.Text(c.Close.transform, "T", S("world.action.close", "Close"), 26, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
            closeText.fontStyle = FontStyle.Bold;
            SurvivalVisuals.Stretch(closeText.rectTransform);
            c.Close.onClick.AddListener(CloseCard);
            _card = c;
            _play?.Fx?.SetAsLastSibling();
            return c;
        }

        private void ShowCard(string? artKey, string title, string line1, string line2, string? action, Action? onAction, bool actionEnabled = true)
        {
            var c = EnsureCard();
            c.Root.gameObject.SetActive(true);
            var sprite = artKey != null ? SurvivalArt.Get(artKey) : null;
            c.Icon.sprite = sprite ?? SurvivalArt.Circle();
            c.Icon.color = sprite != null ? Color.white : SurvivalVisuals.Gold;
            c.Title.text = title;
            c.Line1.text = line1;
            c.Line2.text = line2;
            c.Action.onClick.RemoveAllListeners();
            c.Action.gameObject.SetActive(action != null);
            if (action != null)
            {
                c.ActionText.text = action;
                HookSkin.SetButton(c.ActionImg, HookArtKeys.ButtonGold, actionEnabled ? SurvivalVisuals.MineFill : new Color(0.3f, 0.31f, 0.34f, 1f), actionEnabled, 0.5f);
                if (onAction != null)
                {
                    c.Action.onClick.AddListener(() => onAction());
                }
            }

            StartCoroutine(PopIn(c.Root));
        }

        private void OnTapEntity(string id)
        {
            if (Sim == null || !Sim.TryGet(id, out var e))
            {
                return;
            }

            _selectedId = id;
            _cardRefresh = null;
            if (_entities.TryGetValue(id, out var ev))
            {
                StartCoroutine(Punch(ev.Root, 0.12f, 10));
            }

            var art = ArtFor(e);
            var name = NameOf(e);
            switch (e.Kind)
            {
                case WorldEntityKind.PlayerVillage:
                    ShowCard(art, name, S("world.card.you", "Your village") + "  \u00b7  " + F("world.card.level", "Lv {0}", e.Level), F("world.card.power", "Power {0}", ChipWallet.FormatGrouped(e.Power)), S("world.action.enter", "Enter Village"), () =>
                    {
                        CloseCard();
                        EnterVillage?.Invoke();
                    });
                    break;
                case WorldEntityKind.RivalVillage:
                case WorldEntityKind.AllyVillage:
                {
                    var ally = e.Kind == WorldEntityKind.AllyVillage;
                    var tag = ally ? "reinforce" : "scout";
                    ShowMarchCard(e, art, name, (ally ? S("world.card.ally", "Guild ally") : S("world.card.rival", "Rival lord")) + "  \u00b7  " + F("world.card.level", "Lv {0}", e.Level), F("world.card.power", "Power {0}", ChipWallet.FormatGrouped(e.Power)), ally ? S("world.action.reinforce", "Reinforce") : S("world.action.scout", "Scout"), tag, null);
                    break;
                }

                case WorldEntityKind.EnemyCamp:
                    if (AttackCamp != null)
                    {
                        // Heroes battle: Attack opens Battle Setup against this camp's villains.
                        var camp = e;
                        var campName = name;
                        ShowCard(art, name, F("world.card.camp", "Enemy camp \u00b7 Lv {0}", e.Level), F("world.card.camp_loot", "Loot {0} Gold", ChipWallet.FormatCompact(e.Amount)), S("world.action.attack", "Attack"), () =>
                        {
                            CloseCard();
                            AttackCamp(camp, campName);
                        });
                        break;
                    }

                    ShowMarchCard(e, art, name, F("world.card.camp", "Enemy camp \u00b7 Lv {0}", e.Level), F("world.card.camp_power", "Troops {0}  \u00b7  Loot {1} Gold", ChipWallet.FormatCompact(e.Power), ChipWallet.FormatCompact(e.Amount)), S("world.action.attack", "Attack"), "attack", null);
                    break;
                case WorldEntityKind.ResourceNode:
                    ShowMarchCard(e, art, name, S("world.card.resource", "Resource field"), F("world.card.resource_amount", "{0} {1} to gather", ChipWallet.FormatGrouped(_resourceLeft.TryGetValue(e.Id, out var left) ? left : e.Amount), S(ResourceFor(e.Type) + ".name", e.Type)), S("world.action.gather", "Gather"), "gather", null);
                    break;
                default:
                    OnTapLandmark(e, art, name);
                    break;
            }
        }

        private void OnTapLandmark(WorldEntity e, string art, string name)
        {
            if (_session == null)
            {
                return;
            }

            _session.World.Select(e.Id);
            if (!_session.World.TryGetMarch(e.Id, out var loop))
            {
                ShowCard(art, name, S("inspect." + e.Type + ".title", ""), "", null, null);
                return;
            }

            var prefix = loop.NodeTypeId == SurvIds.WorldNodeTypeFight ? "inspect.fight" : loop.NodeTypeId == SurvIds.WorldNodeTypeExplore ? "inspect.explore" : "inspect.gather";
            var stat = S(prefix + (loop.NodeTypeId == SurvIds.WorldNodeTypeFight ? ".threat" : loop.NodeTypeId == SurvIds.WorldNodeTypeExplore ? ".relics" : ".available"), "") + "  " + ChipWallet.FormatGrouped(loop.Available);
            var march = S(prefix + ".march", "March") + "  " + WorldMarchLoop.FormatClock(loop.MarchSeconds);
            ShowMarchCard(e, art, S(prefix + ".title", name), stat, march, S(prefix + ".action", "March"), "node", loop);
        }

        private void ShowMarchCard(WorldEntity e, string art, string title, string line1, string line2, string action, string tag, WorldMarchLoop? loop)
        {
            void Refresh()
            {
                if (Sim == null || _card == null)
                {
                    return;
                }

                var mine = Sim.PlayerMarchTo(e.Id);
                var check = Sim.CanSend(e.Id);
                if (mine != null)
                {
                    _card.Line2.text = mine.Leg == MarchLeg.Outbound
                        ? F("world.card.march_eta", "Arrives in {0}", Clock(mine.Remaining(Sim.Now)))
                        : F("world.card.march_back", "Returning \u00b7 {0}", Clock(mine.Remaining(Sim.Now)));
                }
                else
                {
                    _card.Line2.text = line2;
                }

                var ok = check == MarchSendResult.Ok && (loop == null || !loop.MarchPending);
                _card.ActionText.text = mine != null ? F("world.marching", "Marching\u2026 {0}", Clock(mine.Remaining(Sim.Now))) : check == MarchSendResult.QueueFull ? S("world.queue_full", "All marches are out") : action;
                HookSkin.SetButton(_card.ActionImg, HookArtKeys.ButtonGold, ok ? SurvivalVisuals.MineFill : new Color(0.3f, 0.31f, 0.34f, 1f), ok, 0.5f);
            }

            ShowCard(art, title, line1, line2, action, () => Send(e, tag, loop));
            _cardRefresh = Refresh;
            Refresh();
        }

        private void Send(WorldEntity e, string tag, WorldMarchLoop? loop)
        {
            if (Sim == null || _session == null)
            {
                return;
            }

            var check = Sim.CanSend(e.Id);
            if (check != MarchSendResult.Ok)
            {
                Pop(e, check == MarchSendResult.QueueFull ? S("world.queue_full", "All marches are out") : S("world.already", "A march is already on the way"), Warn);
                return;
            }

            double? seconds = null;
            if (loop != null)
            {
                if (!loop.TryBegin(_session.Energy, SurvIds.WorldNodeHome01, out _))
                {
                    Pop(e, S("world.no_energy", "Not enough energy"), Warn);
                    return;
                }

                seconds = loop.MarchSeconds;
            }

            if (Sim.SendPlayerMarch(e.Id, tag, out _, seconds) == MarchSendResult.Ok)
            {
                SyncMarches();
                Pop(Sim.Player, S("world.action." + (tag == "node" ? "attack" : tag), "March") + "!", PlayerLine);
            }
        }

        private void OnTapMarch(int serial)
        {
            if (Sim == null)
            {
                return;
            }

            foreach (var m in Sim.Marches)
            {
                if (m.Serial != serial)
                {
                    continue;
                }

                var owner = Sim.TryGet(m.OwnerId, out var o) ? NameOf(o) : m.OwnerId;
                var target = Sim.TryGet(m.TargetId, out var t) ? NameOf(t) : m.TargetId;
                var key = m.IsPlayer ? WorldArtKeys.MarchPlayer : WorldArtKeys.MarchRival;
                _selectedId = null;
                ShowCard(key, F("world.card.march", "{0}'s march", owner), (m.Leg == MarchLeg.Outbound ? "\u2192 " : "\u2190 ") + target, "", null, null);
                var march = m;
                _cardRefresh = () =>
                {
                    if (_card != null && Sim != null)
                    {
                        _card.Line2.text = march.Leg == MarchLeg.Outbound
                            ? F("world.card.march_eta", "Arrives in {0}", Clock(march.Remaining(Sim.Now)))
                            : F("world.card.march_back", "Returning \u00b7 {0}", Clock(march.Remaining(Sim.Now)));
                    }
                };
                return;
            }
        }

        // ---------- arrivals ----------

        private void OnPlayerArrived(WorldMarch m)
        {
            if (_session == null || Sim == null || !Sim.TryGet(m.TargetId, out var e))
            {
                return;
            }

            switch (m.Tag)
            {
                case "attack":
                    Grant(ChipWallet.SoftChip, e.Amount);
                    Pop(e, F("world.victory", "Victory! +{0} Gold", ChipWallet.FormatGrouped(e.Amount)), SurvivalVisuals.Reward);
                    break;
                case "gather":
                {
                    var left = _resourceLeft.TryGetValue(e.Id, out var l) ? l : e.Amount;
                    var take = Mathf.Min(left, Mathf.Max(500, Mathf.RoundToInt(e.Amount / 5f / 100f) * 100));
                    _resourceLeft[e.Id] = Mathf.Max(0, left - take);
                    var res = ResourceFor(e.Type);
                    var chip = ChipWallet.ChipFor(res);
                    if (chip != null && take > 0)
                    {
                        Grant(chip, take);
                    }

                    Pop(e, F("world.gathered", "+{0} {1}", ChipWallet.FormatGrouped(take), S(res + ".name", e.Type)), SurvivalVisuals.Reward);
                    if (_entities.TryGetValue(e.Id, out var ev))
                    {
                        ev.Line2.text = SubOf(e);
                    }

                    break;
                }

                case "node":
                    ResolveNode(e);
                    break;
                default:
                    Pop(e, F("world.scouted", "Scouted {0}", NameOf(e)), PlayerLine);
                    break;
            }
        }

        private void ResolveNode(WorldEntity e)
        {
            if (_session == null || !_session.World.TryGetMarch(e.Id, out var loop))
            {
                return;
            }

            var result = loop.CompleteArrive();
            if (!result.Ok)
            {
                return;
            }

            if (loop.NodeTypeId == SurvIds.WorldNodeTypeFight)
            {
                _session.Battles.StubResolvePve();
            }

            Grant(result.ChipKey, result.Yield);
            _play?.RaiseMarchResolved(loop, result.Yield);
            var prefix = loop.NodeTypeId == SurvIds.WorldNodeTypeFight ? "inspect.fight" : loop.NodeTypeId == SurvIds.WorldNodeTypeExplore ? "inspect.explore" : "inspect.gather";
            Pop(e, string.Format(CultureInfo.InvariantCulture, S(prefix + ".reward_format", "+{0} {1}"), ChipWallet.FormatGrouped(result.Yield), S(prefix + ".resource", "")), SurvivalVisuals.Reward);
        }

        private void Grant(string chipKey, int amount)
        {
            if (_session == null || amount <= 0)
            {
                return;
            }

            _session.Chips.Add(chipKey, amount);
            if (_session.Hook == null)
            {
                _play?.RefreshChip(chipKey);
            }
        }

        private static string ResourceFor(string type) => type switch
        {
            "gold" => SurvIds.ResSoft,
            "wood" => SurvIds.ResWood,
            "food" => SurvIds.ResFood,
            _ => SurvIds.ResStone
        };

        // ---------- juice ----------

        private void Pop(WorldEntity at, string copy, Color color)
        {
            if (_fx == null || !gameObject.activeInHierarchy)
            {
                return;
            }

            var t = HookSkin.Outlined(SurvivalVisuals.Text(_fx, "Pop", copy, 40, TextAnchor.MiddleCenter, color), 2.5f);
            t.fontStyle = FontStyle.Bold;
            SurvivalVillageView.Point(t.rectTransform, at.X, at.Y + SizeFor(at) * 0.6f, 700f, 70f, 0.5f);
            StartCoroutine(Rise(t));
        }

        private IEnumerator Rise(Text t)
        {
            var start = t.rectTransform.anchoredPosition;
            var k = Rig?.Camera != null ? Mathf.Clamp(0.8f / Rig.Camera.Zoom, 0.8f, 1.8f) : 1f;
            const float seconds = 1.4f;
            for (var e = 0f; e < seconds && t != null; e += Time.unscaledDeltaTime)
            {
                var u = e / seconds;
                t.rectTransform.anchoredPosition = start + new Vector2(0f, 110f * u);
                var s = k * (u < 0.12f ? Mathf.Lerp(0.6f, 1.12f, u / 0.12f) : Mathf.Lerp(1.12f, 1f, Mathf.Clamp01((u - 0.12f) / 0.2f)));
                t.rectTransform.localScale = new Vector3(s, s, 1f);
                var c = t.color;
                t.color = new Color(c.r, c.g, c.b, u < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (u - 0.7f) / 0.3f));
                yield return null;
            }

            if (t != null)
            {
                Destroy(t.gameObject);
            }
        }

        private static IEnumerator PopIn(RectTransform rt)
        {
            for (var i = 1; i <= 10 && rt != null; i++)
            {
                var u = i / 10f;
                var k = u < 0.7f ? Mathf.Lerp(0.85f, 1.04f, u / 0.7f) : Mathf.Lerp(1.04f, 1f, (u - 0.7f) / 0.3f);
                rt.localScale = new Vector3(k, k, 1f);
                yield return null;
            }
        }

        private static IEnumerator Punch(RectTransform rt, float amount, int frames)
        {
            for (var i = 0; i < frames && rt != null; i++)
            {
                var k = 1f + amount * Mathf.Sin(i / (float)frames * Mathf.PI);
                rt.localScale = new Vector3(k, k, 1f);
                yield return null;
            }

            if (rt != null)
            {
                rt.localScale = Vector3.one;
            }
        }

        private static string Clock(double seconds)
        {
            var s = Mathf.Max(0, Mathf.CeilToInt((float)seconds));
            return (s / 60).ToString(CultureInfo.InvariantCulture) + ":" + (s % 60).ToString("00", CultureInfo.InvariantCulture);
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
