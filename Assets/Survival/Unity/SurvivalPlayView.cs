using Survival.Domain.Ids;
using Survival.Domain.Layout;
using Survival.Domain.Session;
using Survival.Domain.World;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>Play shell: plain terrain + six SURV-P0 world nodes + HUD chrome from the locked Play mock.</summary>
    public sealed class SurvivalPlayView : MonoBehaviour
    {
        public SurvivalSession? Session { get; set; }

        public void Build()
        {
            if (Session == null)
            {
                return;
            }

            SurvivalVisuals.EnsureEventSystem();
            var root = SurvivalVisuals.Canvas(transform, "SurvivalPlayCanvas", 40);
            var pack = Session.Pack;
            var hud = pack.Hud;

            var terrain = SurvivalVisuals.Image(root, "Terrain", Color.white, SurvivalArt.Get("map.terrain") ?? SurvivalArt.Get("terrain"));
            SurvivalVisuals.Stretch(terrain.rectTransform);
            terrain.preserveAspect = false;

            foreach (var node in Session.World.Nodes)
            {
                SpawnNode(root, node);
            }

            Panel(root, "TopBar", hud, "top_bar");
            var lord = SurvivalVisuals.Text(root, "LordName", pack.StringOr("hud.player_name", ""), 28, TextAnchor.MiddleLeft, SurvivalVisuals.Cream);
            Place(lord.rectTransform, hud, "player_name");
            var power = SurvivalVisuals.Text(root, "Power", pack.StringOr("hud.player_power", ""), 18, TextAnchor.MiddleLeft, SurvivalVisuals.Mute);
            Place(power.rectTransform, hud, "player_power");

            Chip(root, hud, "res_soft", pack.StringOr("hud.chip.gold_label", ""), pack.StringOr("hud.chip.gold_value", ""));
            Chip(root, hud, "res_wood", pack.StringOr("hud.chip.wood_label", ""), pack.StringOr("hud.chip.wood_value", ""));
            Chip(root, hud, "res_stone", pack.StringOr("hud.chip.stone_label", ""), pack.StringOr("hud.chip.stone_value", ""));
            Chip(root, hud, "res_food", pack.StringOr("hud.chip.food_label", ""), pack.StringOr("hud.chip.food_value", ""));

            var vip = SurvivalVisuals.Image(root, "Vip", SurvivalVisuals.Panel);
            Place(vip.rectTransform, hud, "vip");
            SurvivalVisuals.Text(vip.transform, "VipText", pack.StringOr("hud.vip", ""), 20, TextAnchor.MiddleCenter, SurvivalVisuals.Gold);

            var banner = SurvivalVisuals.Image(root, "Banner", SurvivalVisuals.Panel);
            Place(banner.rectTransform, hud, "banner");
            SurvivalVisuals.Text(
                banner.transform,
                "BannerText",
                pack.StringOr(SurvIds.ThemeAMapLabel, pack.StringOr("hud.banner", "")),
                22,
                TextAnchor.MiddleCenter,
                SurvivalVisuals.Cream);

            var queues = SurvivalVisuals.Image(root, "Queues", SurvivalVisuals.Panel);
            Place(queues.rectTransform, hud, "queues");
            var qBody = pack.StringOr("hud.queues_body", "");
            SurvivalVisuals.Text(queues.transform, "QueuesText", qBody, 18, TextAnchor.UpperLeft, SurvivalVisuals.Cream).rectTransform.offsetMin = new Vector2(16f, 8f);
            StretchPad(queues.transform.Find("QueuesText") as RectTransform);

            var events = SurvivalVisuals.Image(root, "Events", SurvivalVisuals.Panel);
            Place(events.rectTransform, hud, "events");
            SurvivalVisuals.Text(events.transform, "EventsText", pack.StringOr("hud.events_body", ""), 18, TextAnchor.UpperLeft, SurvivalVisuals.Cream);

            var hero = SurvivalVisuals.Image(root, "HeroCard", SurvivalVisuals.Panel);
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

            Nav(root, hud, pack);
        }

        private void SpawnNode(RectTransform root, WorldNodeState node)
        {
            var pin = node.Pin;
            var size = pin.SpriteSize > 0.01f ? pin.SpriteSize : 0.16f;
            var art = SurvivalArt.Get(pin.ContentKey);
            var go = new GameObject(node.Id);
            go.transform.SetParent(root, false);
            var btn = go.AddComponent<Button>();
            var img = go.AddComponent<Image>();
            img.sprite = art ?? SurvivalArt.White();
            img.color = art != null ? Color.white : PinColor(node.NodeTypeId);
            img.preserveAspect = true;
            img.raycastTarget = true;
            btn.targetGraphic = img;
            var rt = img.rectTransform;
            var x = pin.PinX;
            var y = 1f - pin.PinYFromTop;
            rt.anchorMin = new Vector2(x - size * 0.5f, y - size * 0.28f);
            rt.anchorMax = new Vector2(x + size * 0.5f, y + size * 0.72f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var captured = node.Id;
            btn.onClick.AddListener(() => OnTapNode(go, captured));

            var label = SurvivalVisuals.Image(root, node.Id + "_label", SurvivalVisuals.Panel);
            var lr = label.rectTransform;
            lr.anchorMin = new Vector2(Mathf.Clamp01(x - 0.09f), Mathf.Clamp01(y - 0.055f));
            lr.anchorMax = new Vector2(Mathf.Clamp01(x + 0.09f), Mathf.Clamp01(y - 0.018f));
            lr.offsetMin = Vector2.zero;
            lr.offsetMax = Vector2.zero;
            var copy = Session!.Pack.StringOr("pin." + node.Id, node.NodeTypeId);
            SurvivalVisuals.Text(label.transform, "Label", copy, 14, TextAnchor.MiddleCenter, SurvivalVisuals.Cream);

            var dot = SurvivalVisuals.Image(root, node.Id + "_pin", PinColor(node.NodeTypeId));
            var dr = dot.rectTransform;
            dr.anchorMin = new Vector2(x - 0.012f, y + 0.008f);
            dr.anchorMax = new Vector2(x + 0.012f, y + 0.028f);
            dr.offsetMin = Vector2.zero;
            dr.offsetMax = Vector2.zero;
        }

        private void OnTapNode(GameObject nodeGo, string nodeId)
        {
            Session?.World.Select(nodeId);
            StopAllCoroutines();
            StartCoroutine(Punch(nodeGo.transform));
        }

        private System.Collections.IEnumerator Punch(Transform t)
        {
            var rt = t as RectTransform;
            if (rt == null)
            {
                yield break;
            }

            var start = Vector3.one;
            for (var i = 0; i < 8; i++)
            {
                var k = 1f + 0.12f * Mathf.Sin(i / 8f * Mathf.PI);
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

        private static void Panel(RectTransform root, string name, System.Collections.Generic.IReadOnlyDictionary<string, HudRect> hud, string key)
        {
            var img = SurvivalVisuals.Image(root, name, SurvivalVisuals.Panel);
            Place(img.rectTransform, hud, key);
        }

        private static void Chip(RectTransform root, System.Collections.Generic.IReadOnlyDictionary<string, HudRect> hud, string key, string label, string value)
        {
            var img = SurvivalVisuals.Image(root, key, SurvivalVisuals.PanelSoft);
            Place(img.rectTransform, hud, key);
            var lab = SurvivalVisuals.Text(img.transform, "L", label, 14, TextAnchor.UpperCenter, SurvivalVisuals.Mute);
            StretchPad(lab.rectTransform);
            lab.rectTransform.anchorMax = new Vector2(1f, 1f);
            lab.rectTransform.anchorMin = new Vector2(0f, 0.45f);
            var val = SurvivalVisuals.Text(img.transform, "V", value, 18, TextAnchor.LowerCenter, SurvivalVisuals.Cream);
            val.rectTransform.anchorMin = Vector2.zero;
            val.rectTransform.anchorMax = new Vector2(1f, 0.55f);
            val.rectTransform.offsetMin = Vector2.zero;
            val.rectTransform.offsetMax = Vector2.zero;
        }

        private void Nav(RectTransform root, System.Collections.Generic.IReadOnlyDictionary<string, HudRect> hud, Domain.Theme.ThemePackBinder pack)
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

        private static void Place(RectTransform rt, System.Collections.Generic.IReadOnlyDictionary<string, HudRect> hud, string key)
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
