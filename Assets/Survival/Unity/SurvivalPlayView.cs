#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Survival.Domain.Base;
using Survival.Domain.Ids;
using Survival.Domain.Layout;
using Survival.Domain.Session;
using Survival.Domain.Theme;
using Survival.Domain.View;
using Survival.Domain.World;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Play shell with two screens and a toggle between them: the zoomed-in Village (Castle, Farms, Lumber Mills,
    /// Forge on plots, all upgradable) and the zoomed-out World (kingdom map with every player's village, enemy
    /// camps, resource fields, the six catalog nodes and live marches). Both scroll and zoom (SurvivalMapRig).
    /// The HUD (top bar, chips, banner, quest, shop, nav) stays on top of whichever map is showing.
    /// </summary>
    public sealed class SurvivalPlayView : MonoBehaviour
    {
        private const string FallbackWorldJson =
            "{\"world\":{\"width\":1080,\"height\":1920,\"min_zoom\":1,\"max_zoom\":1.6},\"heart\":{\"x\":0.5,\"y\":0.5,\"width\":1080,\"height\":1920}," +
            "\"villages\":[{\"id\":\"village.player\",\"name\":\"You\",\"kind\":\"player\",\"x\":0.31,\"y\":0.3}]}";

        public SurvivalSession? Session { get; set; }

        private RectTransform? _root;
        private RectTransform? _fx;
        private readonly Dictionary<string, Text> _chipValues = new();
        private readonly Dictionary<string, RectTransform> _navButtons = new();
        private readonly Dictionary<string, Action> _navHandlers = new();
        private RectTransform? _navBar;
        private ThemePackBinder? _navPack;
        private Text? _bannerText;
        private Image? _toggleIcon;
        private Text? _toggleText;
        private RectTransform? _toggleButton;
        private PlayScreen _shownFor = (PlayScreen)(-1);

        /// <summary>Raised after a march lands and its yield is in the wallet: (loop, yield).</summary>
        public event Action<WorldMarchLoop, int>? MarchResolved;

        /// <summary>Bottom-bar Heroes tap (see <see cref="OnHeroesTapped"/>). Battle Setup / Heroes screens hook here.</summary>
        public event Action? HeroesTapped;

        public event Action? AllianceTapped;

        public event Action? BagTapped;

        /// <summary>Bottom-bar model: Heroes, Alliance, Bag; add Battle with <see cref="AddNavButton"/>.</summary>
        public BottomNav NavModel { get; } = new BottomNav();

        public RectTransform? Root => _root;
        public RectTransform? Fx => _fx;

        /// <summary>World / Village switch (the HUD toggle button and Enter Village).</summary>
        public PlayScreenToggle Screen { get; } = new PlayScreenToggle(PlayScreen.Village);

        public SurvivalVillageView? Village { get; private set; }
        public SurvivalWorldView? WorldView { get; private set; }

        /// <summary>Value text of a HUD chip (res_soft, res_stone, ...), or null.</summary>
        public Text? ChipText(string key) => _chipValues.TryGetValue(key, out var t) ? t : null;

        /// <summary>Add one more HUD chip in its hud_layout slot (hook slice: res_hard gems).</summary>
        public Text? AddChip(string key, string label)
        {
            if (_root == null || Session == null || !Session.Pack.Hud.ContainsKey(key))
            {
                return null;
            }

            Chip(_root, Session.Pack.Hud, key, label);
            _fx?.SetAsLastSibling();
            return ChipText(key);
        }

        /// <summary>World view reports a resolved catalog-node march (quests listen to <see cref="MarchResolved"/>).</summary>
        public void RaiseMarchResolved(WorldMarchLoop loop, int yield)
        {
            RefreshChip(loop.ChipKey);
            MarchResolved?.Invoke(loop, yield);
        }

        public void ShowScreen(PlayScreen screen) => Screen.Set(screen);

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
            var hookOn = Session.Hook != null && hud.ContainsKey("quest_tracker");

            // Maps first (behind the HUD): the village needs the hook economy; the world always builds.
            var maps = new GameObject("Maps", typeof(RectTransform)).GetComponent<RectTransform>();
            maps.SetParent(_root, false);
            SurvivalVisuals.Stretch(maps);
            var packRoot = SurvivalArt.ResolvePackRoot(Session.Flavor);
            WorldView = SurvivalWorldView.Create(maps, _root, this, Session, LoadWorld(packRoot));
            WorldView.EnterVillage += () => Screen.Set(PlayScreen.Village);
            if (hookOn)
            {
                var village = LoadVillage(packRoot);
                if (village != null)
                {
                    Village = SurvivalVillageView.Create(maps, village, (k, f) => pack.StringOr(k, f));
                }
            }

            if (Village == null)
            {
                Screen.Set(PlayScreen.World);
                Screen.Tick(10f);
            }

            Screen.Changed += (_, to) => OnScreenChanged(to);

            _fx = SurvivalVisuals.Image(_root, "FxLayer", new Color(1f, 1f, 1f, 0f)).rectTransform;
            SurvivalVisuals.Stretch(_fx);

            Panel(_root, "TopBar", hud, "top_bar");
            var lord = SurvivalVisuals.Text(_root, "LordName", pack.StringOr("hud.player_name", ""), 28, TextAnchor.MiddleLeft, SurvivalVisuals.Cream);
            Place(lord.rectTransform, hud, "player_name");
            var power = SurvivalVisuals.Text(_root, "Power", pack.StringOr("hud.player_power", ""), 18, TextAnchor.MiddleLeft, SurvivalVisuals.Mute);
            Place(power.rectTransform, hud, "player_power");

            Chip(_root, hud, ChipWallet.SoftChip, pack.StringOr("hud.chip.gold_label", ""));
            Chip(_root, hud, ChipWallet.WoodChip, pack.StringOr("hud.chip.wood_label", ""));
            Chip(_root, hud, ChipWallet.StoneChip, pack.StringOr("hud.chip.stone_label", ""));
            Chip(_root, hud, ChipWallet.FoodChip, pack.StringOr("hud.chip.food_label", ""));

            var vip = SurvivalVisuals.Image(_root, "Vip", SurvivalVisuals.Panel);
            Place(vip.rectTransform, hud, "vip");
            var vipSkinned = HookSkin.TrySkin(vip, HookArtKeys.ButtonGold, 0.35f);
            var vipText = SurvivalVisuals.Text(vip.transform, "VipText", pack.StringOr("hud.vip", ""), 20, TextAnchor.MiddleCenter, vipSkinned ? HookSkin.Ink : SurvivalVisuals.Gold);
            vipText.fontStyle = vipSkinned ? FontStyle.Bold : FontStyle.Normal;

            var banner = SurvivalVisuals.Image(_root, "Banner", SurvivalVisuals.Panel);
            Place(banner.rectTransform, hud, "banner");
            HookSkin.TrySkin(banner, HookArtKeys.Header, 0.42f);
            _bannerText = SurvivalVisuals.Text(
                banner.transform,
                "BannerText",
                pack.StringOr(SurvIds.ThemeAMapLabel, pack.StringOr("hud.banner", "")),
                22,
                TextAnchor.MiddleCenter,
                SurvivalVisuals.Cream);

            // Hook slice: the live quest tracker takes the static queues mock's slot.
            if (!hookOn)
            {
                var queues = SurvivalVisuals.Image(_root, "Queues", SurvivalVisuals.Panel);
                Place(queues.rectTransform, hud, "queues");
                var qBody = pack.StringOr("hud.queues_body", "");
                SurvivalVisuals.Text(queues.transform, "QueuesText", qBody, 18, TextAnchor.UpperLeft, SurvivalVisuals.Cream).rectTransform.offsetMin = new Vector2(16f, 8f);
                StretchPad(queues.transform.Find("QueuesText") as RectTransform);
            }

            var events = SurvivalVisuals.Image(_root, "Events", SurvivalVisuals.Panel);
            Place(events.rectTransform, hud, "events");
            HookSkin.TrySkin(events, HookArtKeys.Panel, 0.5f);
            SurvivalVisuals.Text(events.transform, "EventsText", pack.StringOr("hud.events_body", ""), 18, TextAnchor.UpperLeft, SurvivalVisuals.Cream);

            var hero = SurvivalVisuals.Image(_root, "HeroCard", SurvivalVisuals.Panel);
            Place(hero.rectTransform, hud, "hero_card");
            HookSkin.TrySkin(hero, HookArtKeys.Panel, 0.45f);
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
            BuildToggle(_root, hud, pack);
            _fx!.SetAsLastSibling();

            if (hookOn)
            {
                var hookView = GetComponent<SurvivalHookView>() ?? gameObject.AddComponent<SurvivalHookView>();
                hookView.Build(this, Session);
            }

            // Heroes screen + Battle Setup (campaign.json); without the hook layer it still runs, minus celebrations.
            var campaignView = GetComponent<SurvivalCampaignView>() ?? gameObject.AddComponent<SurvivalCampaignView>();
            campaignView.Build(this, Session, hookOn ? GetComponent<SurvivalHookView>() : null);

            // Runner: Walk & Fight (RunnerLevel scene) behind a painted event button; pays through the campaign wallet.
            var runnerEntry = GetComponent<SurvivalRunnerEntry>() ?? gameObject.AddComponent<SurvivalRunnerEntry>();
            runnerEntry.Build(this, pack, hookOn ? GetComponent<SurvivalHookView>() : null, campaignView);

            ApplyScreen(1f);
        }

        private WorldLayout LoadWorld(string packRoot)
        {
            var path = Path.Combine(packRoot, "map", "world_layout.json");
            try
            {
                if (PlayerAssets.FileExists(path))
                {
                    return WorldLayout.Parse(PlayerAssets.ReadAllText(path));
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            return WorldLayout.Parse(FallbackWorldJson);
        }

        private static VillageLayout? LoadVillage(string packRoot)
        {
            var path = Path.Combine(packRoot, "map", "village_layout.json");
            try
            {
                return PlayerAssets.FileExists(path) ? VillageLayout.Parse(PlayerAssets.ReadAllText(path)) : null;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }

        // ---------- World / Village switch ----------

        private void Update()
        {
            // Marches keep moving (and paying out) while the player is in the village.
            WorldView?.TickSim();
            if (!Screen.InTransition && _shownFor == Screen.Current)
            {
                return;
            }

            Screen.Tick(Time.unscaledDeltaTime);
            ApplyScreen(Screen.Fade);
        }

        private void OnScreenChanged(PlayScreen to)
        {
            WorldView?.CloseCard();
            if (_toggleButton != null)
            {
                StartCoroutine(Punch(_toggleButton, 0.16f, 10));
            }

            ApplyScreen(0f);
        }

        /// <summary>Quick cross-fade: the incoming map fades in while settling from a slight zoom (in for Village, out for World).</summary>
        private void ApplyScreen(float fade)
        {
            var current = Screen.Current;
            Fade(Village?.Root, Village?.Rig, current == PlayScreen.Village, fade, 0.92f);
            Fade(WorldView?.Root, WorldView?.Rig, current == PlayScreen.World, fade, 1.10f);
            if (fade >= 1f)
            {
                _shownFor = current;
            }

            if (Session == null)
            {
                return;
            }

            var pack = Session.Pack;
            if (_bannerText != null)
            {
                _bannerText.text = current == PlayScreen.World
                    ? pack.StringOr("view.banner.world", pack.StringOr(SurvIds.ThemeAMapLabel, ""))
                    : pack.StringOr("view.banner.village", "");
            }

            if (_toggleText != null)
            {
                _toggleText.text = pack.StringOr(Screen.LabelKey, Screen.Destination.ToString());
            }

            if (_toggleIcon != null)
            {
                // Village icon = the player's own castle at its current stage.
                var key = Screen.Destination == PlayScreen.World
                    ? WorldArtKeys.IconWorld
                    : CastleTiers.For(Session?.Hook?.Base.HqLevel ?? 1, Village?.Layout?.CastleTiers).ArtKey;
                var sprite = SurvivalArt.Get(key);
                _toggleIcon.sprite = sprite ?? SurvivalArt.Circle();
                _toggleIcon.color = sprite != null ? Color.white : SurvivalVisuals.Gold;
            }

            if (_toggleButton != null)
            {
                _toggleButton.gameObject.SetActive(Village != null);
            }
        }

        private static void Fade(RectTransform? root, SurvivalMapRig? rig, bool incoming, float fade, float fromScale)
        {
            if (root == null)
            {
                return;
            }

            var group = root.GetComponent<CanvasGroup>();
            var alpha = incoming ? fade : 1f - fade;
            var visible = alpha > 0.001f;
            if (root.gameObject.activeSelf != visible)
            {
                root.gameObject.SetActive(visible);
            }

            if (group != null)
            {
                group.alpha = alpha;
                group.blocksRaycasts = incoming && fade >= 1f;
                group.interactable = incoming;
            }

            if (rig != null)
            {
                rig.InputEnabled = incoming;
            }

            var k = incoming ? Mathf.Lerp(fromScale, 1f, fade) : Mathf.Lerp(1f, 2f - fromScale, fade);
            root.localScale = new Vector3(k, k, 1f);
        }

        private void BuildToggle(RectTransform root, IReadOnlyDictionary<string, HudRect> hud, ThemePackBinder pack)
        {
            if (!hud.TryGetValue("view_toggle", out var slot))
            {
                return;
            }

            // Same painted frame as the bottom bar and the Build button (blue kit button if the pack lacks it).
            var btn = HookSkin.Button(root, "ViewToggle", HookSkin.Has(BottomNav.ButtonArt) ? BottomNav.ButtonArt : HookArtKeys.ButtonBlue, SurvivalVisuals.PinBuild, 0.42f);
            btn.transition = Selectable.Transition.None;
            btn.gameObject.AddComponent<PressSquash>();
            var rt = btn.GetComponent<RectTransform>();
            SurvivalVisuals.Place(rt, slot);
            var glow = HookSkin.Glow(rt, "Glow", new Color(0.55f, 0.8f, 1f, 0.45f));
            glow.rectTransform.anchorMin = new Vector2(-0.12f, -0.10f);
            glow.rectTransform.anchorMax = new Vector2(1.12f, 1.25f);
            glow.rectTransform.offsetMin = Vector2.zero;
            glow.rectTransform.offsetMax = Vector2.zero;
            glow.transform.SetAsFirstSibling();
            _toggleIcon = SurvivalVisuals.Image(rt, "Icon", Color.white);
            _toggleIcon.preserveAspect = true;
            _toggleIcon.rectTransform.anchorMin = new Vector2(0.10f, 0.30f);
            _toggleIcon.rectTransform.anchorMax = new Vector2(0.90f, 1.18f);
            _toggleIcon.rectTransform.offsetMin = Vector2.zero;
            _toggleIcon.rectTransform.offsetMax = Vector2.zero;
            _toggleText = HookSkin.Outlined(SurvivalVisuals.Text(rt, "T", pack.StringOr(Screen.LabelKey, ""), 28, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 2f);
            _toggleText.fontStyle = FontStyle.Bold;
            _toggleText.rectTransform.anchorMin = new Vector2(0f, 0.02f);
            _toggleText.rectTransform.anchorMax = new Vector2(1f, 0.32f);
            _toggleText.rectTransform.offsetMin = Vector2.zero;
            _toggleText.rectTransform.offsetMax = Vector2.zero;
            _toggleButton = rt;
            btn.onClick.AddListener(() => Screen.Toggle());
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

        private static void Panel(RectTransform root, string name, IReadOnlyDictionary<string, HudRect> hud, string key)
        {
            var img = SurvivalVisuals.Image(root, name, SurvivalVisuals.Panel);
            Place(img.rectTransform, hud, key);
            HookSkin.TrySkin(img, HookArtKeys.Panel, 0.5f);
        }

        private void Chip(RectTransform root, IReadOnlyDictionary<string, HudRect> hud, string key, string label)
        {
            var img = SurvivalVisuals.Image(root, key, SurvivalVisuals.PanelSoft);
            Place(img.rectTransform, hud, key);
            var resourceId = ChipWallet.ResourceFor(key);
            if (HookSkin.Has(HookArtKeys.ForResource(resourceId)) && HookSkin.TrySkin(img, HookArtKeys.Chip, 0.45f))
            {
                // Theme kit chip: painted resource icon on a gold-rimmed pill, bold count beside it.
                var icon = HookSkin.ResIcon(img.transform, "Icon", resourceId, SurvivalVisuals.Gold, "");
                icon.rectTransform.anchorMin = new Vector2(-0.06f, -0.12f);
                icon.rectTransform.anchorMax = new Vector2(0.40f, 1.12f);
                icon.rectTransform.offsetMin = Vector2.zero;
                icon.rectTransform.offsetMax = Vector2.zero;
                var count = HookSkin.Outlined(SurvivalVisuals.Text(img.transform, "V", ChipWallet.FormatCompact(Session!.Chips.Get(key)), 21, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
                count.fontStyle = FontStyle.Bold;
                count.rectTransform.anchorMin = new Vector2(0.36f, 0f);
                count.rectTransform.anchorMax = new Vector2(0.96f, 1f);
                count.rectTransform.offsetMin = Vector2.zero;
                count.rectTransform.offsetMax = Vector2.zero;
                _chipValues[key] = count;
                return;
            }

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

        public void RefreshChip(string key)
        {
            if (_chipValues.TryGetValue(key, out var text) && Session != null)
            {
                text.text = ChipWallet.FormatCompact(Session.Chips.Get(key));
            }
        }

        // ---------- bottom bar (Heroes / Alliance / Bag, room for Battle) ----------

        /// <summary>
        /// Builds the painted bottom bar from <see cref="NavModel"/>. Taps go button → <see cref="BottomNav.Tap"/>
        /// → the per-id handler (Heroes → <see cref="OnHeroesTapped"/>, ...). Village / World switching lives on the
        /// view toggle button, not in the bar.
        /// </summary>
        private void Nav(RectTransform root, IReadOnlyDictionary<string, HudRect> hud, ThemePackBinder pack)
        {
            _navPack = pack;
            var bar = SurvivalPaintedButton.Bar(root, "Nav");
            Place(bar.rectTransform, hud, "nav");
            _navBar = bar.rectTransform;
            _navHandlers[BottomNav.Heroes] = OnHeroesTapped;
            _navHandlers[BottomNav.Alliance] = OnAllianceTapped;
            _navHandlers[BottomNav.Bag] = OnBagTapped;
            NavModel.Tapped += id =>
            {
                if (_navHandlers.TryGetValue(id, out var handler))
                {
                    handler();
                }
            };
            NavModel.Changed += RelayoutNav;
            RelayoutNav();
        }

        /// <summary>
        /// Add one more bottom-bar button (Battle, ...) with its own tap handler. The bar re-spaces evenly.
        /// <paramref name="index"/> null appends; 0 puts it first. Returns the button, or null if the id exists.
        /// </summary>
        public RectTransform? AddNavButton(string id, string iconKey, Action onTap, int? index = null)
        {
            _navHandlers[id] = onTap;
            if (!NavModel.Add(new NavButtonDef(id, iconKey), index))
            {
                return null;
            }

            return _navButtons.TryGetValue(id, out var rt) ? rt : null;
        }

        /// <summary>The bottom-bar button for <paramref name="id"/> (badges, tutorials), or null.</summary>
        public RectTransform? NavButton(string id) => _navButtons.TryGetValue(id, out var rt) ? rt : null;

        /// <summary>Heroes bottom-bar entry point. The Heroes screen subscribes to <see cref="HeroesTapped"/>.</summary>
        public void OnHeroesTapped() => HeroesTapped?.Invoke();

        public void OnAllianceTapped() => AllianceTapped?.Invoke();

        public void OnBagTapped() => BagTapped?.Invoke();

        private void RelayoutNav()
        {
            if (_navBar == null)
            {
                return;
            }

            var keep = new HashSet<string>();
            for (var i = 0; i < NavModel.Buttons.Count; i++)
            {
                var def = NavModel.Buttons[i];
                keep.Add(def.Id);
                if (!_navButtons.TryGetValue(def.Id, out var rt))
                {
                    var label = _navPack != null ? _navPack.StringOr(def.Id, def.Id) : def.Id;
                    var btn = SurvivalPaintedButton.Create(_navBar, "Nav_" + def.Id, def.IconKey, label, out _, out _);
                    rt = btn.GetComponent<RectTransform>();
                    var id = def.Id;
                    btn.onClick.AddListener(() => NavModel.Tap(id));
                    _navButtons[def.Id] = rt;
                }

                var (x0, x1) = NavModel.Slot(i);
                rt.anchorMin = new Vector2(x0, 0.10f);
                rt.anchorMax = new Vector2(x1, 0.86f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            var gone = new List<string>();
            foreach (var kv in _navButtons)
            {
                if (!keep.Contains(kv.Key))
                {
                    Destroy(kv.Value.gameObject);
                    gone.Add(kv.Key);
                }
            }

            foreach (var id in gone)
            {
                _navButtons.Remove(id);
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
