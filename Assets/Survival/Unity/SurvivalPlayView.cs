using System.Collections;
using System.Collections.Generic;
using Survival.Domain.Ids;
using Survival.Domain.Layout;
using Survival.Domain.Session;
using Survival.Domain.Theme;
using Survival.Domain.World;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>Play shell: crusade map + first playable Gather/Quarry loop (tap → inspect → Mine → march → +Stone).</summary>
    public sealed class SurvivalPlayView : MonoBehaviour
    {
        public SurvivalSession? Session { get; set; }

        private RectTransform? _root;
        private RectTransform? _fx;
        private readonly Dictionary<string, Vector2> _pinCenters = new();
        private readonly Dictionary<string, Text> _chipValues = new();
        private GameObject? _inspect;
        private bool _busy;

        public void Build()
        {
            if (Session == null)
            {
                return;
            }

            SurvivalVisuals.EnsureEventSystem();
            _root = SurvivalVisuals.Canvas(transform, "SurvivalPlayCanvas", 40);
            var pack = Session.Pack;
            var hud = pack.Hud;

            var terrain = SurvivalVisuals.Image(_root, "Terrain", Color.white, SurvivalArt.Get("map.terrain") ?? SurvivalArt.Get("terrain"));
            SurvivalVisuals.Stretch(terrain.rectTransform);
            terrain.preserveAspect = false;

            foreach (var node in Session.World.Nodes)
            {
                SpawnNode(_root, node);
            }

            _fx = SurvivalVisuals.Image(_root, "FxLayer", new Color(1f, 1f, 1f, 0f)).rectTransform;
            SurvivalVisuals.Stretch(_fx);

            Panel(_root, "TopBar", hud, "top_bar");
            var lord = SurvivalVisuals.Text(_root, "LordName", pack.StringOr("hud.player_name", ""), 28, TextAnchor.MiddleLeft, SurvivalVisuals.Cream);
            Place(lord.rectTransform, hud, "player_name");
            var power = SurvivalVisuals.Text(_root, "Power", pack.StringOr("hud.player_power", ""), 18, TextAnchor.MiddleLeft, SurvivalVisuals.Mute);
            Place(power.rectTransform, hud, "player_power");

            Chip(_root, hud, ChipWallet.SoftChip, pack.StringOr("hud.chip.gold_label", ""));
            Chip(_root, hud, "res_wood", pack.StringOr("hud.chip.wood_label", ""));
            Chip(_root, hud, ChipWallet.StoneChip, pack.StringOr("hud.chip.stone_label", ""));
            Chip(_root, hud, "res_food", pack.StringOr("hud.chip.food_label", ""));

            var vip = SurvivalVisuals.Image(_root, "Vip", SurvivalVisuals.Panel);
            Place(vip.rectTransform, hud, "vip");
            SurvivalVisuals.Text(vip.transform, "VipText", pack.StringOr("hud.vip", ""), 20, TextAnchor.MiddleCenter, SurvivalVisuals.Gold);

            var banner = SurvivalVisuals.Image(_root, "Banner", SurvivalVisuals.Panel);
            Place(banner.rectTransform, hud, "banner");
            SurvivalVisuals.Text(
                banner.transform,
                "BannerText",
                pack.StringOr(SurvIds.ThemeAMapLabel, pack.StringOr("hud.banner", "")),
                22,
                TextAnchor.MiddleCenter,
                SurvivalVisuals.Cream);

            var queues = SurvivalVisuals.Image(_root, "Queues", SurvivalVisuals.Panel);
            Place(queues.rectTransform, hud, "queues");
            var qBody = pack.StringOr("hud.queues_body", "");
            SurvivalVisuals.Text(queues.transform, "QueuesText", qBody, 18, TextAnchor.UpperLeft, SurvivalVisuals.Cream).rectTransform.offsetMin = new Vector2(16f, 8f);
            StretchPad(queues.transform.Find("QueuesText") as RectTransform);

            var events = SurvivalVisuals.Image(_root, "Events", SurvivalVisuals.Panel);
            Place(events.rectTransform, hud, "events");
            SurvivalVisuals.Text(events.transform, "EventsText", pack.StringOr("hud.events_body", ""), 18, TextAnchor.UpperLeft, SurvivalVisuals.Cream);

            var hero = SurvivalVisuals.Image(_root, "HeroCard", SurvivalVisuals.Panel);
            Place(hero.rectTransform, hud, "hero_card");
            var portrait = SurvivalVisuals.Image(hero.transform, "Portrait", Color.white, SurvivalArt.Get(SurvIds.ThemeAHeroKnight01));
            var prt = portrait.rectTransform;
            prt.anchorMin = new Vector2(0.04f, 0.08f);
            prt.anchorMax = new Vector2(0.28f, 0.92f);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            SurvivalVisuals.Text(hero.transform, "HeroName", pack.StringOr("hud.hero_name", ""), 22, TextAnchor.MiddleLeft, SurvivalVisuals.Cream).rectTransform.anchorMin = new Vector2(0.32f, 0.45f);
            var heroName = hero.transform.Find("HeroName") as RectTransform;
            if (heroName != null)
            {
                heroName.anchorMax = new Vector2(0.98f, 0.92f);
                heroName.offsetMin = Vector2.zero;
                heroName.offsetMax = Vector2.zero;
            }

            var heroStatus = SurvivalVisuals.Text(hero.transform, "HeroStatus", pack.StringOr("hud.hero_status", ""), 16, TextAnchor.MiddleLeft, new Color(0.45f, 0.85f, 0.55f));
            heroStatus.rectTransform.anchorMin = new Vector2(0.32f, 0.08f);
            heroStatus.rectTransform.anchorMax = new Vector2(0.98f, 0.48f);
            heroStatus.rectTransform.offsetMin = Vector2.zero;
            heroStatus.rectTransform.offsetMax = Vector2.zero;

            Nav(_root, hud, pack);
            _fx!.SetAsLastSibling();
        }

        private void SpawnNode(RectTransform root, WorldNodeState node)
        {
            var pin = node.Pin;
            var x = pin.PinX;
            var y = 1f - pin.PinYFromTop;
            _pinCenters[node.Id] = new Vector2(x, y);
            var color = PinColor(node.NodeTypeId);
            GameObject? quarryGo = null;

            if (pin.SpriteSize > 0.01f)
            {
                var art = SurvivalArt.Get(pin.ContentKey);
                var go = new GameObject(node.Id);
                go.transform.SetParent(root, false);
                var img = go.AddComponent<Image>();
                img.sprite = art ?? SurvivalArt.White();
                img.color = art != null ? Color.white : color;
                img.preserveAspect = true;
                img.raycastTarget = true;
                var rt = img.rectTransform;
                var w = pin.SpriteSize;
                var aspect = art != null && art.rect.height > 1f ? art.rect.width / art.rect.height : 139f / 111f;
                var h = w * (PlayHudLayout.ReferenceWidth / PlayHudLayout.ReferenceHeight) * (1f / aspect);
                rt.anchorMin = new Vector2(x - w * 0.5f, y - h * 0.62f);
                rt.anchorMax = new Vector2(x + w * 0.5f, y + h * 0.38f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                quarryGo = go;
            }

            var label = SurvivalVisuals.Image(root, node.Id + "_label", SurvivalVisuals.Panel);
            var lr = label.rectTransform;
            lr.anchorMin = new Vector2(Mathf.Clamp01(x - 0.09f), Mathf.Clamp01(y - 0.055f));
            lr.anchorMax = new Vector2(Mathf.Clamp01(x + 0.09f), Mathf.Clamp01(y - 0.018f));
            lr.offsetMin = Vector2.zero;
            lr.offsetMax = Vector2.zero;
            var copy = Session!.Pack.StringOr("pin." + node.Id, node.NodeTypeId);
            SurvivalVisuals.Text(label.transform, "Label", copy, 14, TextAnchor.MiddleCenter, SurvivalVisuals.Cream);

            var dot = SurvivalVisuals.Circle(root, node.Id + "_pin", color);
            SurvivalVisuals.AnchorBox(dot.rectTransform, x, y + 0.016f, 0.028f, 0.016f);

            var hitGo = quarryGo ?? MakeHit(root, node.Id + "_hit", x, y, pin.SpriteSize > 0.01f ? 0.16f : 0.14f, 0.08f);
            var btn = hitGo.GetComponent<Button>() ?? hitGo.AddComponent<Button>();
            var graphic = hitGo.GetComponent<Image>();
            if (graphic != null)
            {
                btn.targetGraphic = graphic;
                graphic.raycastTarget = true;
            }

            var captured = node.Id;
            var capturedGo = quarryGo ?? hitGo;
            btn.onClick.AddListener(() => OnTapNode(capturedGo, captured));
            if (quarryGo != null)
            {
                label.raycastTarget = true;
                var labelBtn = label.gameObject.AddComponent<Button>();
                labelBtn.targetGraphic = label;
                labelBtn.onClick.AddListener(() => OnTapNode(capturedGo, captured));
                dot.raycastTarget = true;
                var pinBtn = dot.gameObject.AddComponent<Button>();
                pinBtn.targetGraphic = dot;
                pinBtn.onClick.AddListener(() => OnTapNode(capturedGo, captured));
            }
        }

        private static GameObject MakeHit(RectTransform root, string name, float x, float y, float w, float h)
        {
            var img = SurvivalVisuals.Image(root, name, new Color(1f, 1f, 1f, 0f));
            img.raycastTarget = true;
            SurvivalVisuals.AnchorBox(img.rectTransform, x, y, w, h);
            return img.gameObject;
        }

        private void OnTapNode(GameObject nodeGo, string nodeId)
        {
            if (_busy || Session == null)
            {
                return;
            }

            Session.World.Select(nodeId);
            StopAllCoroutines();
            CloseInspect();
            StartCoroutine(TapJuice(nodeGo, nodeId));
        }

        private IEnumerator TapJuice(GameObject nodeGo, string nodeId)
        {
            if (_pinCenters.TryGetValue(nodeId, out var center))
            {
                StartCoroutine(PressRing(center));
            }

            yield return Punch(nodeGo.transform, 0.14f, 8);

            if (Session != null
                && string.Equals(nodeId, SurvIds.WorldNodeGather01, System.StringComparison.Ordinal)
                && Session.World.Gather != null)
            {
                OpenGatherInspect();
            }
        }

        private IEnumerator PressRing(Vector2 center)
        {
            if (_fx == null)
            {
                yield break;
            }

            var ring = SurvivalVisuals.Ring(_fx, "TapRing", new Color(1f, 0.84f, 0.28f, 0.95f));
            SurvivalVisuals.AnchorBox(ring.rectTransform, center.x, center.y, 0.07f, 0.04f);
            ring.rectTransform.localScale = Vector3.one * 0.55f;
            for (var i = 0; i < 12; i++)
            {
                var t = i / 11f;
                ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.55f, t);
                var c = ring.color;
                c.a = Mathf.Lerp(0.95f, 0f, t);
                ring.color = c;
                yield return null;
            }

            Destroy(ring.gameObject);
        }

        private void OpenGatherInspect()
        {
            if (_root == null || Session?.World.Gather == null)
            {
                return;
            }

            CloseInspect();
            var pack = Session.Pack;
            var gather = Session.World.Gather;
            var blocker = SurvivalVisuals.Image(_root, "GatherInspect", new Color(0f, 0f, 0f, 0.12f));
            SurvivalVisuals.Stretch(blocker.rectTransform);
            blocker.raycastTarget = true;
            _inspect = blocker.gameObject;

            var panel = SurvivalVisuals.Framed(blocker.transform, "Panel", SurvivalVisuals.InspectFill, SurvivalVisuals.GoldLine, 3f);
            var pr = panel.rectTransform;
            pr.anchorMin = new Vector2(0.09f, 0.40f);
            pr.anchorMax = new Vector2(0.91f, 0.695f);
            pr.offsetMin = Vector2.zero;
            pr.offsetMax = Vector2.zero;

            var title = SurvivalVisuals.Text(panel.transform, "Title", pack.StringOr("inspect.gather.title", ""), 34, TextAnchor.MiddleLeft, SurvivalVisuals.Cream);
            title.rectTransform.anchorMin = new Vector2(0.06f, 0.78f);
            title.rectTransform.anchorMax = new Vector2(0.78f, 0.96f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            var close = SurvivalVisuals.Framed(panel.transform, "Close", SurvivalVisuals.InspectFill, SurvivalVisuals.GoldLine, 2f);
            var crt = close.rectTransform;
            crt.anchorMin = new Vector2(0.86f, 0.80f);
            crt.anchorMax = new Vector2(0.955f, 0.95f);
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;
            close.raycastTarget = true;
            var closeBtn = close.gameObject.AddComponent<Button>();
            closeBtn.targetGraphic = close;
            SurvivalVisuals.Text(close.transform, "X", "×", 28, TextAnchor.MiddleCenter, SurvivalVisuals.Gold);
            closeBtn.onClick.AddListener(CloseInspect);

            InspectRow(
                panel.transform,
                "Avail",
                0.52f,
                0.74f,
                pack.StringOr("inspect.gather.available", ""),
                pack.StringOr("inspect.gather.resource", "") + " " + ChipWallet.FormatGrouped(gather.Available));
            InspectRow(
                panel.transform,
                "March",
                0.30f,
                0.50f,
                pack.StringOr("inspect.gather.march", ""),
                GatherLoop.FormatClock(gather.MarchSeconds));

            var mine = SurvivalVisuals.Framed(panel.transform, "Mine", SurvivalVisuals.MineFill, SurvivalVisuals.GoldLine, 3f);
            var mrt = mine.rectTransform;
            mrt.anchorMin = new Vector2(0.06f, 0.07f);
            mrt.anchorMax = new Vector2(0.94f, 0.26f);
            mrt.offsetMin = Vector2.zero;
            mrt.offsetMax = Vector2.zero;
            mine.raycastTarget = true;
            var fill = mine.transform.Find("Fill") as RectTransform;
            if (fill != null)
            {
                var fillImg = fill.GetComponent<Image>();
                if (fillImg != null)
                {
                    fillImg.raycastTarget = true;
                }
            }

            var mineBtn = mine.gameObject.AddComponent<Button>();
            mineBtn.targetGraphic = mine;
            SurvivalVisuals.Text(mine.transform, "MineLabel", pack.StringOr("inspect.gather.action", ""), 32, TextAnchor.MiddleCenter, SurvivalVisuals.Gold);
            mineBtn.onClick.AddListener(() => OnMine(mine.gameObject));
        }

        private static void InspectRow(Transform panel, string name, float yMin, float yMax, string left, string right)
        {
            var row = SurvivalVisuals.Image(panel, name, SurvivalVisuals.RowFill);
            var rt = row.rectTransform;
            rt.anchorMin = new Vector2(0.06f, yMin);
            rt.anchorMax = new Vector2(0.94f, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var l = SurvivalVisuals.Text(row.transform, "L", left, 22, TextAnchor.MiddleLeft, SurvivalVisuals.Mute);
            l.rectTransform.anchorMin = new Vector2(0.04f, 0f);
            l.rectTransform.anchorMax = new Vector2(0.55f, 1f);
            l.rectTransform.offsetMin = Vector2.zero;
            l.rectTransform.offsetMax = Vector2.zero;
            var r = SurvivalVisuals.Text(row.transform, "R", right, 22, TextAnchor.MiddleRight, SurvivalVisuals.Cream);
            r.rectTransform.anchorMin = new Vector2(0.45f, 0f);
            r.rectTransform.anchorMax = new Vector2(0.96f, 1f);
            r.rectTransform.offsetMin = Vector2.zero;
            r.rectTransform.offsetMax = Vector2.zero;
        }

        private void OnMine(GameObject mineBtn)
        {
            if (_busy || Session?.World.Gather == null)
            {
                return;
            }

            if (!Session.World.Gather.TryBeginMine(Session.Energy, SurvIds.WorldNodeHome01, out _))
            {
                return;
            }

            _busy = true;
            StartCoroutine(MineLoop(mineBtn));
        }

        private IEnumerator MineLoop(GameObject mineBtn)
        {
            yield return Punch(mineBtn.transform, 0.08f, 6);
            CloseInspect();

            if (Session?.World.Gather == null || _fx == null)
            {
                _busy = false;
                yield break;
            }

            var from = PinOf(SurvIds.WorldNodeHome01);
            var to = PinOf(SurvIds.WorldNodeGather01);
            var marker = SurvivalVisuals.Circle(_fx, "MarchMarker", new Color(1f, 0.82f, 0.28f, 1f));
            var glow = SurvivalVisuals.Ring(marker.transform, "Glow", new Color(1f, 0.78f, 0.22f, 0.75f));
            SurvivalVisuals.Stretch(glow.rectTransform);
            glow.rectTransform.offsetMin = new Vector2(-10f, -10f);
            glow.rectTransform.offsetMax = new Vector2(10f, 10f);

            yield return March(marker.rectTransform, from, to, 1.45f);

            var result = Session.World.Gather.CompleteArrive();
            if (result.Ok)
            {
                Session.Chips.Add(ChipWallet.StoneChip, result.Yield);
                RefreshChip(ChipWallet.StoneChip);
                yield return RewardPop(to, result.Yield);
            }

            yield return March(marker.rectTransform, to, from, 0.95f);
            Destroy(marker.gameObject);
            _busy = false;
        }

        private IEnumerator RewardPop(Vector2 at, int yield)
        {
            if (_fx == null || Session == null)
            {
                yield break;
            }

            var pack = Session.Pack;
            var label = pack.StringOr("inspect.gather.reward_format", "+{0} {1}");
            var copy = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                label,
                yield,
                pack.StringOr("inspect.gather.resource", ""));
            var pop = SurvivalVisuals.Text(_fx, "RewardPop", copy, 44, TextAnchor.MiddleCenter, SurvivalVisuals.Reward);
            SurvivalVisuals.AnchorBox(pop.rectTransform, at.x, at.y + 0.04f, 0.42f, 0.06f);
            pop.rectTransform.localScale = Vector3.one * 0.7f;

            for (var i = 0; i < 10; i++)
            {
                var spark = SurvivalVisuals.Circle(_fx, "Spark" + i, new Color(1f, 0.86f, 0.35f, 0.95f));
                var ang = i / 10f * Mathf.PI * 2f;
                var ox = Mathf.Cos(ang) * 0.04f;
                var oy = Mathf.Sin(ang) * 0.022f;
                SurvivalVisuals.AnchorBox(spark.rectTransform, at.x + ox, at.y + oy, 0.018f, 0.01f);
                StartCoroutine(FadeSpark(spark, at + new Vector2(ox * 1.6f, oy * 1.6f + 0.02f)));
            }

            for (var i = 0; i < 18; i++)
            {
                var t = i / 17f;
                pop.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.08f, t);
                var n = Mathf.Lerp(at.y + 0.04f, at.y + 0.08f, t);
                SurvivalVisuals.AnchorBox(pop.rectTransform, at.x, n, 0.42f, 0.06f);
                var c = pop.color;
                c.a = t < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f);
                pop.color = c;
                yield return null;
            }

            Destroy(pop.gameObject);
        }

        private IEnumerator FadeSpark(Image spark, Vector2 dest)
        {
            var start = spark.rectTransform.anchorMin + (spark.rectTransform.anchorMax - spark.rectTransform.anchorMin) * 0.5f;
            for (var i = 0; i < 14; i++)
            {
                var t = i / 13f;
                var p = Vector2.Lerp(start, dest, t);
                SurvivalVisuals.AnchorBox(spark.rectTransform, p.x, p.y, 0.016f * (1f - t * 0.4f), 0.009f * (1f - t * 0.4f));
                var c = spark.color;
                c.a = 1f - t;
                spark.color = c;
                yield return null;
            }

            Destroy(spark.gameObject);
        }

        private IEnumerator March(RectTransform marker, Vector2 from, Vector2 to, float seconds)
        {
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / seconds);
                var ease = t * t * (3f - 2f * t);
                var p = Vector2.Lerp(from, to, ease);
                SurvivalVisuals.AnchorBox(marker, p.x, p.y + 0.012f, 0.038f, 0.022f);
                marker.localScale = Vector3.one * (0.9f + 0.12f * Mathf.Sin(elapsed * 10f));
                yield return null;
            }

            SurvivalVisuals.AnchorBox(marker, to.x, to.y + 0.012f, 0.038f, 0.022f);
        }

        private Vector2 PinOf(string nodeId) =>
            _pinCenters.TryGetValue(nodeId, out var p) ? p : new Vector2(0.5f, 0.5f);

        private void CloseInspect()
        {
            if (_inspect != null)
            {
                Destroy(_inspect);
                _inspect = null;
            }
        }

        private static IEnumerator Punch(Transform t, float amount, int frames)
        {
            var rt = t as RectTransform;
            if (rt == null)
            {
                yield break;
            }

            var start = Vector3.one;
            for (var i = 0; i < frames; i++)
            {
                var k = 1f + amount * Mathf.Sin(i / (float)frames * Mathf.PI);
                rt.localScale = start * k;
                yield return null;
            }

            rt.localScale = start;
        }

        private static Color PinColor(string nodeTypeId) => nodeTypeId switch
        {
            SurvIds.WorldNodeTypeHome => SurvivalVisuals.PinHome,
            SurvIds.WorldNodeTypeGather => SurvivalVisuals.PinGather,
            SurvIds.WorldNodeTypeBuild => SurvivalVisuals.PinBuild,
            SurvIds.WorldNodeTypeGuild => SurvivalVisuals.PinGuild,
            SurvIds.WorldNodeTypeFight => SurvivalVisuals.PinFight,
            _ => SurvivalVisuals.PinExplore
        };

        private static void Panel(RectTransform root, string name, IReadOnlyDictionary<string, HudRect> hud, string key)
        {
            var img = SurvivalVisuals.Image(root, name, SurvivalVisuals.Panel);
            Place(img.rectTransform, hud, key);
        }

        private void Chip(RectTransform root, IReadOnlyDictionary<string, HudRect> hud, string key, string label)
        {
            var img = SurvivalVisuals.Image(root, key, SurvivalVisuals.PanelSoft);
            Place(img.rectTransform, hud, key);
            var lab = SurvivalVisuals.Text(img.transform, "L", label, 14, TextAnchor.UpperCenter, SurvivalVisuals.Mute);
            StretchPad(lab.rectTransform);
            lab.rectTransform.anchorMax = new Vector2(1f, 1f);
            lab.rectTransform.anchorMin = new Vector2(0f, 0.45f);
            var value = ChipWallet.FormatCompact(Session!.Chips.Get(key));
            var val = SurvivalVisuals.Text(img.transform, "V", value, 18, TextAnchor.LowerCenter, SurvivalVisuals.Cream);
            val.rectTransform.anchorMin = Vector2.zero;
            val.rectTransform.anchorMax = new Vector2(1f, 0.55f);
            val.rectTransform.offsetMin = Vector2.zero;
            val.rectTransform.offsetMax = Vector2.zero;
            _chipValues[key] = val;
        }

        private void RefreshChip(string key)
        {
            if (_chipValues.TryGetValue(key, out var text) && Session != null)
            {
                text.text = ChipWallet.FormatCompact(Session.Chips.Get(key));
            }
        }

        private void Nav(RectTransform root, IReadOnlyDictionary<string, HudRect> hud, ThemePackBinder pack)
        {
            var bar = SurvivalVisuals.Image(root, "Nav", SurvivalVisuals.Panel);
            Place(bar.rectTransform, hud, "nav");
            var tabs = new[]
            {
                ("nav.base", SurvIds.SysBase, false),
                ("nav.heroes", SurvIds.SysHeroes, false),
                ("nav.alliance", SurvIds.SysAlliance, false),
                ("nav.world", SurvIds.SysWorld, true),
                ("nav.bag", SurvIds.SysMeta, false)
            };
            for (var i = 0; i < tabs.Length; i++)
            {
                var (key, _, active) = tabs[i];
                var btn = SurvivalVisuals.Button(bar.transform, key, active ? SurvivalVisuals.WorldTab : SurvivalVisuals.PanelSoft);
                var rt = btn.GetComponent<RectTransform>();
                var x0 = i / 5f;
                var x1 = (i + 1) / 5f;
                rt.anchorMin = new Vector2(x0 + 0.01f, 0.12f);
                rt.anchorMax = new Vector2(x1 - 0.01f, 0.88f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                SurvivalVisuals.Text(btn.transform, "T", pack.StringOr(key, key), 20, TextAnchor.MiddleCenter, active ? SurvivalVisuals.Gold : SurvivalVisuals.Cream);
            }
        }

        private static void Place(RectTransform rt, IReadOnlyDictionary<string, HudRect> hud, string key)
        {
            if (hud.TryGetValue(key, out var rect))
            {
                SurvivalVisuals.Place(rt, rect);
            }
        }

        private static void StretchPad(RectTransform? rt)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(14f, 10f);
            rt.offsetMax = new Vector2(-14f, -10f);
        }
    }
}
