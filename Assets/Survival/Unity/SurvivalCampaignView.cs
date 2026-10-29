#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Survival.Domain.Campaign;
using Survival.Domain.Clock;
using Survival.Domain.Ids;
using Survival.Domain.Layout;
using Survival.Domain.Session;
using Survival.Domain.Store;
using Survival.Domain.Theme;
using Survival.Domain.View;
using Survival.Domain.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Heroes and battles on the Play shell.
    /// Heroes screen (bottom-bar Heroes button, or the HUD hero card): every battle character with its painted portrait
    /// (heroes framed in Sir Aldric's #1b3060 blue, villains on a Foes tab in purple). Owned heroes train a level for gold +
    /// food on a timer (free in the last minutes, gems to rush, speed-ups), star up with shards (higher level cap, +15%
    /// stats per star), and show HP / ATK / Power rising; locked heroes are recruited for gems or shards from a sandbox
    /// store sheet styled like the Gem Shop. Unlocks and level-ups get the chest-burst celebration.
    /// Battle Setup (the painted Battle button, or Attack on a World camp): pick a stage, see its villains in their lanes,
    /// put owned heroes in lanes, compare power, Fight. Fight hands the lineup to BattleLocked (loaded additively over
    /// Play, which hides meanwhile); the configured battle plays it out on the locked rigs with the three-attack rotation
    /// and hit reactions, then Play comes back with a VICTORY / DEFEAT result screen and the rewards (gold, food, shards,
    /// first-clear gems, bounty progress). Everything is saved to survival_save.json.
    /// </summary>
    public sealed class SurvivalCampaignView : MonoBehaviour
    {
        public const string BattleSceneName = "BattleLocked";
        private const int LoadingTipCount = 6;
        public const string BattleButtonName = "BattleButton";
        public const string HeroesScreenName = "HeroesScreen";
        public const string SetupScreenName = "BattleSetupScreen";
        public const string ResultScreenName = "BattleResultScreen";
        public const string PowerPacksSheetName = "PowerPacksSheet";
        public const string RewardTrackSheetName = "RewardTrackSheet";
        public const string BattleSlot = "battle_button";
        public const string HeroFreeSlot = "hero_free";
        public const string HeroFreeName = "HeroFreeBadge";

        private const float TickSeconds = 0.25f;
        private const float SaveDelaySeconds = 1.5f;
        private const float PanelAspect = 1037f / 1766f;

        private static readonly Color Dim = new Color(0.02f, 0.03f, 0.05f, 0.82f);
        private static readonly Color HeroFrame = Rgb(CampaignArtKeys.HeroFrameRgb);
        private static readonly Color VillainFrame = Rgb(CampaignArtKeys.VillainFrameRgb);
        private static readonly Color HeroGlow = new Color(0.40f, 0.62f, 1f, 1f);
        private static readonly Color VillainGlow = new Color(0.72f, 0.42f, 1f, 1f);
        private static readonly Color Disabled = new Color(0.30f, 0.31f, 0.34f, 1f);
        private static readonly Color FreeGreen = new Color(0.30f, 0.82f, 0.38f, 1f);
        private static readonly Color Warn = new Color(1f, 0.55f, 0.45f, 1f);
        private static readonly Color StarOn = new Color(1f, 0.82f, 0.25f, 1f);
        private static readonly Color StarOff = new Color(0.35f, 0.33f, 0.30f, 0.9f);
        private static readonly Color ShardPurple = new Color(0.72f, 0.52f, 1f, 1f);

        private SurvivalPlayView? _play;
        private SurvivalSession? _session;
        private SurvivalHookView? _hook;
        private RectTransform? _overlay;
        private GameObject? _screen;
        private GameObject? _sheet;
        private Action? _refresh;
        private Action? _sheetRefresh;
        private Image? _battleBadge;
        private Image? _heroesBadge;
        private RectTransform? _heroFree;
        private Text? _heroFreeCount;
        private Image? _heroFreeCountDot;
        private bool _bulkFree;
        private float _tick;
        private float _saveIn = -1f;
        private bool _inBattle;
        private int _battleTrips;
        private bool _foesTab;
        private string _selected = "";
        private BattleSetup? _setup;
        private readonly List<string> _lastLineupHeroes = new List<string>();
        private readonly List<(string Title, Dictionary<string, int> Rows)> _deferred = new List<(string, Dictionary<string, int>)>();

        public CampaignSession? Campaign { get; private set; }

        /// <summary>The power-up ladder (power_packs.json + store.power_packs); null when the file or the hook store is missing.</summary>
        public PowerPackShop? PowerPacks { get; private set; }

        /// <summary>The Kingdom Path reward track (reward_track.json + store.reward_track); null when the file or the hook store is missing.</summary>
        public RewardTrack? Track { get; private set; }

        private int _trackPage = -1;

        public static string SavePath => Path.Combine(Application.persistentDataPath, CampaignSave.FileName);

        /// <summary>The static handoff survives "enter play mode" without a domain reload; start every session clean.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetHandoff() => BattleHandoff.Reset();

        public void Build(SurvivalPlayView play, SurvivalSession session, SurvivalHookView? hook)
        {
            if (Campaign != null || play.Root == null)
            {
                return;
            }

            var path = Path.Combine(SurvivalBootstrap.ResolveCatalogDir(), CampaignCatalog.FileName);
            if (!PlayerAssets.FileExists(path))
            {
                Debug.LogWarning("Heroes / battles: " + path + " missing; the Heroes and Battle screens stay off.");
                return;
            }

            CampaignCatalog catalog;
            try
            {
                catalog = CampaignCatalog.Parse(PlayerAssets.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return;
            }

            _play = play;
            _session = session;
            _hook = hook;
            var clock = session.Hook != null ? session.Hook.Clock : new WallClock();
            Campaign = new CampaignSession(catalog, session.Chips.Wallet, clock, session.Hook?.Timers.Rules);
            LoadSave();
            PowerPacks = LoadPowerPacks(session, Campaign);
            Track = LoadRewardTrack(session, Campaign);
            Campaign.Changed += MarkDirty;
            session.Chips.Wallet.Changed += (_, __, ___) => MarkDirty();
            Campaign.Roster.LevelledUp += OnLevelledUp;
            Campaign.Roster.Unlocked += OnUnlocked;
            Campaign.Roster.StarredUp += OnStarredUp;
            Campaign.Tick();

            _overlay = SurvivalVisuals.Canvas(transform, "SurvivalCampaignOverlay", 55);
            WireHeroesEntry(play, play.Root);
            BuildBattleEntry(play, play.Root, session.Pack.Hud);
            BuildHeroFree(play.Root, session.Pack.Hud);
            if (play.WorldView != null)
            {
                play.WorldView.AttackCamp = OnAttackCamp;
            }

            RefreshBadges();
        }

        // ---------- save ----------

        private void LoadSave()
        {
            if (Campaign == null)
            {
                return;
            }

            try
            {
                if (File.Exists(SavePath) && !Campaign.Load(File.ReadAllText(SavePath)))
                {
                    Debug.LogWarning("Heroes / battles: save file unreadable, starting fresh. " + SavePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void MarkDirty()
        {
            if (_saveIn < 0f)
            {
                _saveIn = SaveDelaySeconds;
            }
        }

        public void SaveNow()
        {
            _saveIn = -1f;
            if (Campaign == null)
            {
                return;
            }

            try
            {
                var tmp = SavePath + ".tmp";
                File.WriteAllText(tmp, Campaign.Save());
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                }

                File.Move(tmp, SavePath);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveNow();
            }
        }

        private void OnApplicationQuit() => SaveNow();

        private void OnDestroy()
        {
            if (Campaign != null && _saveIn >= 0f)
            {
                SaveNow();
            }
        }

        // ---------- tick ----------

        private void Update()
        {
            if (Campaign == null)
            {
                return;
            }

            var dt = Time.unscaledDeltaTime;
            if (_saveIn >= 0f)
            {
                _saveIn -= dt;
                if (_saveIn < 0f)
                {
                    SaveNow();
                }
            }

            _tick += dt;
            if (_tick < TickSeconds)
            {
                return;
            }

            _tick = 0f;
            Campaign.Tick();
            _refresh?.Invoke();
            _sheetRefresh?.Invoke();
            RefreshBadges();
        }

        // ---------- entry points ----------

        /// <summary>Heroes: the painted bottom-bar Heroes button (SurvivalPlayView.HeroesTapped) and the HUD hero card.</summary>
        private void WireHeroesEntry(SurvivalPlayView play, RectTransform root)
        {
            play.HeroesTapped += OpenHeroes;
            var nav = play.NavButton(BottomNav.Heroes);
            if (nav != null)
            {
                _heroesBadge = Badge(nav, "CampaignBadge");
            }

            var card = FindDeep(root, "HeroCard");
            if (card != null && card.GetComponent<Button>() == null && card.GetComponent<Image>() is { } cardImg)
            {
                cardImg.raycastTarget = true;
                var b = card.gameObject.AddComponent<Button>();
                b.targetGraphic = cardImg;
                b.onClick.AddListener(OpenHeroes);
            }
        }

        /// <summary>
        /// Battle: a painted Battle button in the bottom bar (the bar re-spaces to Heroes, Battle, Alliance, Bag). If the
        /// bar is missing, a standalone painted button goes in the hud_layout "battle_button" slot instead.
        /// </summary>
        private void BuildBattleEntry(SurvivalPlayView play, RectTransform root, IReadOnlyDictionary<string, HudRect> hud)
        {
            var nav = play.AddNavButton(BottomNav.Battle, BottomNav.IconBattle, OnBattleTapped, 1) ?? play.NavButton(BottomNav.Battle);
            if (nav != null)
            {
                _battleBadge = Badge(nav, "CampaignBadge");
                return;
            }

            BuildBattleButton(root, hud);
        }

        private void OnBattleTapped() => OpenSetup(Campaign?.NextStage);

        private void BuildBattleButton(RectTransform root, IReadOnlyDictionary<string, HudRect> hud)
        {
            var btn = SurvivalVisuals.Button(root, BattleButtonName, new Color(1f, 1f, 1f, 0f));
            var rt = btn.GetComponent<RectTransform>();
            if (hud.TryGetValue(BattleSlot, out var slot))
            {
                SurvivalVisuals.Place(rt, slot);
            }
            else
            {
                Box(rt, 0.80f, 1f - 0.438f, 0.975f, 1f - 0.338f);
            }

            btn.transition = Selectable.Transition.None;
            var glow = HookSkin.Glow(btn.transform, "Glow", new Color(1f, 0.78f, 0.35f, 0.55f));
            Box(glow.rectTransform, -0.2f, -0.1f, 1.2f, 1.3f);
            var icon = SurvivalArt.Get(CampaignArtKeys.IconBattle);
            if (icon != null)
            {
                var art = SurvivalVisuals.Image(btn.transform, "Icon", Color.white, icon);
                art.preserveAspect = true;
                Box(art.rectTransform, 0.06f, 0.24f, 0.94f, 1.02f);
            }
            else
            {
                // Code-drawn fallback: a hero-blue disc with a crossed-swords glyph.
                var disc = SurvivalVisuals.Circle(btn.transform, "Icon", HeroFrame);
                Box(disc.rectTransform, 0.1f, 0.26f, 0.9f, 1f);
                var g = SurvivalVisuals.Text(disc.transform, "G", "\u2694", 64, TextAnchor.MiddleCenter, SurvivalVisuals.Gold);
                SurvivalVisuals.Stretch(g.rectTransform);
            }

            var plate = SurvivalVisuals.Image(btn.transform, "Label", new Color(0.55f, 0.12f, 0.12f, 1f));
            HookSkin.TrySkin(plate, HookArtKeys.RibbonRed, 0.5f);
            Box(plate.rectTransform, -0.12f, -0.02f, 1.12f, 0.30f);
            var label = HookSkin.Outlined(SurvivalVisuals.Text(plate.transform, "T", S("campaign.battle", "BATTLE"), 26, TextAnchor.MiddleCenter, Color.white), 2f);
            label.fontStyle = FontStyle.Bold;
            Box(label.rectTransform, 0f, 0.2f, 1f, 0.95f);
            _battleBadge = Badge(btn.transform, "Badge");
            btn.onClick.AddListener(() =>
            {
                Juice(rt);
                OnBattleTapped();
            });
            StartCoroutine(Breathe(rt));
        }

        private static IEnumerator Breathe(RectTransform rt)
        {
            var t = 0f;
            while (rt != null)
            {
                t += Time.unscaledDeltaTime;
                var k = 1f + 0.035f * Mathf.Sin(t * 3.2f);
                rt.localScale = new Vector3(k, k, 1f);
                yield return null;
            }
        }

        private Image Badge(Transform parent, string name)
        {
            var dot = SurvivalVisuals.Circle(parent, name, new Color(0.90f, 0.18f, 0.20f, 1f));
            dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(0.92f, 0.88f);
            dot.rectTransform.sizeDelta = new Vector2(34f, 34f);
            var t = HookSkin.Outlined(SurvivalVisuals.Text(dot.transform, "T", "!", 24, TextAnchor.MiddleCenter, Color.white), 1.2f);
            t.fontStyle = FontStyle.Bold;
            SurvivalVisuals.Stretch(t.rectTransform);
            dot.gameObject.SetActive(false);
            return dot;
        }

        private void RefreshBadges()
        {
            if (Campaign == null)
            {
                return;
            }

            if (_battleBadge != null)
            {
                _battleBadge.gameObject.SetActive(Campaign.BountyReady);
            }

            if (_heroesBadge != null)
            {
                _heroesBadge.gameObject.SetActive(AnyHeroAction() || (Track != null && Track.ClaimableCount() > 0));
            }

            if (_heroFree != null)
            {
                var n = Campaign.Roster.FreeFinishable().Count;
                var show = n > 0 && !_inBattle;
                if (_heroFree.gameObject.activeSelf != show)
                {
                    _heroFree.gameObject.SetActive(show);
                    if (show)
                    {
                        StartCoroutine(PopIn(_heroFree));
                    }
                }

                _heroFreeCountDot!.gameObject.SetActive(n > 1);
                _heroFreeCount!.text = "x" + n.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Village / Play HUD: painted hero helmet with a FREE ribbon, shown only while at least one hero training is in its
        /// free-finish window (a count when there are more). Tap: finish them all free and celebrate. Distinct from the
        /// Gem Shop's FREE gift badge: hero helmet, hero-blue glow, its own slot.
        /// </summary>
        private void BuildHeroFree(RectTransform root, IReadOnlyDictionary<string, HudRect> hud)
        {
            var btn = SurvivalVisuals.Button(root, HeroFreeName, new Color(1f, 1f, 1f, 0f));
            btn.transition = Selectable.Transition.None;
            var rt = btn.GetComponent<RectTransform>();
            if (hud.TryGetValue(HeroFreeSlot, out var slot))
            {
                SurvivalVisuals.Place(rt, slot);
            }
            else
            {
                Box(rt, 0.80f, 1f - 0.538f, 0.975f, 1f - 0.448f);
            }

            var glow = HookSkin.Glow(rt, "Glow", new Color(HeroGlow.r, HeroGlow.g, HeroGlow.b, 0.8f));
            glow.raycastTarget = false;
            Box(glow.rectTransform, -0.25f, -0.2f, 1.25f, 1.25f);
            var disc = SurvivalVisuals.Circle(rt, "Disc", HeroFrame);
            disc.raycastTarget = false;
            Box(disc.rectTransform, 0.08f, 0.18f, 0.92f, 1.02f);
            var ring = SurvivalVisuals.Ring(disc.transform, "Ring", SurvivalVisuals.GoldLine);
            ring.raycastTarget = false;
            SurvivalVisuals.Stretch(ring.rectTransform);
            var helm = HookSkin.Art(rt, "Helm", BottomNav.IconHeroes, SurvivalVisuals.Cream);
            helm.preserveAspect = true;
            helm.raycastTarget = false;
            Box(helm.rectTransform, 0.12f, 0.22f, 0.88f, 1.06f);
            var ribbon = SurvivalArt.Get(HookArtKeys.BadgeFree);
            var free = ribbon != null ? SurvivalVisuals.Image(rt, "Free", Color.white, ribbon) : SurvivalVisuals.Image(rt, "Free", FreeGreen);
            free.preserveAspect = true;
            free.raycastTarget = false;
            Box(free.rectTransform, 0.02f, -0.10f, 0.98f, 0.36f);
            var freeText = HookSkin.Outlined(SurvivalVisuals.Text(free.transform, "T", S("hook.free", "FREE"), 24, TextAnchor.MiddleCenter, Color.white), 1.6f);
            freeText.fontStyle = FontStyle.Bold;
            freeText.raycastTarget = false;
            SurvivalVisuals.Stretch(freeText.rectTransform);
            _heroFreeCountDot = SurvivalVisuals.Circle(rt, "Count", new Color(0.90f, 0.18f, 0.20f, 1f));
            _heroFreeCountDot.raycastTarget = false;
            _heroFreeCountDot.rectTransform.anchorMin = _heroFreeCountDot.rectTransform.anchorMax = new Vector2(0.9f, 0.92f);
            _heroFreeCountDot.rectTransform.sizeDelta = new Vector2(46f, 46f);
            _heroFreeCount = HookSkin.Outlined(SurvivalVisuals.Text(_heroFreeCountDot.transform, "T", "", 22, TextAnchor.MiddleCenter, Color.white), 1.2f);
            _heroFreeCount.fontStyle = FontStyle.Bold;
            SurvivalVisuals.Stretch(_heroFreeCount.rectTransform);
            _heroFree = rt;
            Wire(btn, OnHeroFreeTapped);
            StartCoroutine(Breathe(rt));
            rt.gameObject.SetActive(false);
        }

        /// <summary>Free-finish every hero training in its free window, then one celebration for them all.</summary>
        public void OnHeroFreeTapped()
        {
            if (Campaign == null)
            {
                return;
            }

            _bulkFree = true;
            IReadOnlyList<(string HeroId, int Level)> done;
            try
            {
                done = Campaign.Roster.FinishAllFree();
            }
            finally
            {
                _bulkFree = false;
            }

            if (done.Count == 0)
            {
                RefreshBadges();
                return;
            }

            if (_heroFree != null)
            {
                _hook?.Sparks(_heroFree, HeroGlow, 18);
            }

            var title = done.Count == 1
                ? Fmt("campaign.levelled", "{0} reached Lv {1}!", HeroName(done[0].HeroId), done[0].Level)
                : Fmt("campaign.levelled_many", "{0} heroes levelled up!", done.Count);
            Celebrate(title, new Dictionary<string, int>());
            Campaign.Touch();
            SaveNow();
            _refresh?.Invoke();
            RefreshBadges();
        }

        /// <summary>Something to tap on the Heroes screen: an affordable level-up, a free finish, a star up or a shard unlock.</summary>
        private bool AnyHeroAction()
        {
            var r = Campaign!.Roster;
            foreach (var def in Campaign.Catalog.OnSide(CampaignSide.Hero))
            {
                if (r.CanLevelUp(def.Id) == LevelUpCheck.Ok || r.CanFinishFree(def.Id) || r.CanStarUp(def.Id) || r.CanUnlockWithShards(def.Id) == UnlockCheck.Ok)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnAttackCamp(WorldEntity camp, string name)
        {
            if (Campaign == null)
            {
                return;
            }

            OpenSetup(Campaign.Catalog.CampStage(camp.Id, camp.Type, camp.Level, camp.Amount, name));
        }

        // ---------- roster events ----------

        private void OnLevelledUp(string heroId, int level)
        {
            if (_bulkFree)
            {
                return;
            }

            Celebrate(Fmt("campaign.levelled", "{0} reached Lv {1}!", HeroName(heroId), level), new Dictionary<string, int>());
            MarkDirty();
        }

        private void OnUnlocked(string heroId)
        {
            Celebrate(Fmt("campaign.unlocked", "{0} joins your heroes!", HeroName(heroId)), new Dictionary<string, int>());
            SaveNow();
        }

        private void OnStarredUp(string heroId, int stars)
        {
            Celebrate(Fmt("campaign.starred", "{0} \u2605 {1}!", HeroName(heroId), stars), new Dictionary<string, int>());
            SaveNow();
        }

        private void Celebrate(string title, Dictionary<string, int> rows)
        {
            if (_inBattle)
            {
                _deferred.Add((title, rows));
                return;
            }

            _hook?.Celebrate(title, rows);
        }

        // ---------- screens ----------

        private RectTransform OpenScreen(string name, string title)
        {
            CloseSheet();
            CloseScreen();
            var dim = SurvivalVisuals.Button(_overlay!, name, Dim);
            dim.transition = Selectable.Transition.None;
            SurvivalVisuals.Stretch(dim.GetComponent<RectTransform>());
            _screen = dim.gameObject;
            var panel = HookSkin.Panel(dim.transform, "Panel", HookArtKeys.Panel, SurvivalVisuals.InspectFill, 1f);
            panel.raycastTarget = true;
            Box(panel.rectTransform, 0.02f, 0.035f, 0.98f, 0.955f);
            HookSkin.Header(panel.rectTransform, title, 700f, 118f, 40);
            StartCoroutine(PopIn(panel.rectTransform));
            return panel.rectTransform;
        }

        public void CloseScreen()
        {
            _refresh = null;
            _setup = null;
            if (_screen != null)
            {
                Destroy(_screen);
                _screen = null;
            }
        }

        private RectTransform OpenSheet(string name, string title, float height)
        {
            CloseSheet();
            var dim = SurvivalVisuals.Button(_overlay!, name, Dim);
            dim.transition = Selectable.Transition.None;
            SurvivalVisuals.Stretch(dim.GetComponent<RectTransform>());
            dim.onClick.AddListener(CloseSheet);
            _sheet = dim.gameObject;
            var panel = HookSkin.Panel(dim.transform, "Panel", HookArtKeys.Panel, SurvivalVisuals.InspectFill, 1f);
            panel.raycastTarget = true;
            SurvivalVisuals.AnchorBox(panel.rectTransform, 0.5f, 0.5f, 0.9f, height);
            HookSkin.Header(panel.rectTransform, title, 640f, 110f, 34);
            StartCoroutine(PopIn(panel.rectTransform));
            return panel.rectTransform;
        }

        private void CloseSheet()
        {
            _sheetRefresh = null;
            if (_sheet != null)
            {
                Destroy(_sheet);
                _sheet = null;
            }
        }

        // ---------- Heroes ----------

        public void OpenHeroes()
        {
            if (Campaign == null || _overlay == null)
            {
                return;
            }

            // The banner follows the tab: OpenHeroes runs on open and on every tab tap, so the title is rebuilt each time.
            var panel = OpenScreen(HeroesScreenName, S(HeroesScreen.TitleKey(_foesTab), HeroesScreen.TitleFallback(_foesTab)));
            var roster = Campaign.Roster;
            var defs = new List<CampaignHeroDef>(Campaign.Catalog.OnSide(_foesTab ? CampaignSide.Villain : CampaignSide.Hero));
            var sel = _selected.Length > 0 ? Campaign.Catalog.Hero(_selected) : null;
            if (sel == null || (sel.Side == CampaignSide.Villain) != _foesTab)
            {
                _selected = defs.Count > 0 ? defs[0].Id : "";
            }

            Tab(panel, "TabHeroes", S("campaign.tab.heroes", "Heroes"), HeroesScreen.TabHighlighted(false, _foesTab), 0.04f, 0.27f, () => { _foesTab = false; _selected = ""; OpenHeroes(); });
            Tab(panel, "TabFoes", S("campaign.tab.foes", "Foes"), HeroesScreen.TabHighlighted(true, _foesTab), 0.29f, 0.52f, () => { _foesTab = true; _selected = ""; OpenHeroes(); });
            var queue = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Queue", "", 22, TextAnchor.MiddleRight, SurvivalVisuals.Cream), 1.5f);
            Box(queue.rectTransform, 0.54f, 0.895f, 0.96f, 0.935f);

            var refreshers = new List<Action>();
            for (var i = 0; i < defs.Count && i < 6; i++)
            {
                var col = i % 3;
                var row = i / 3;
                var x0 = 0.035f + col * 0.315f;
                var y1 = 0.885f - row * 0.228f;
                refreshers.Add(HeroCard(panel, defs[i], x0, y1 - 0.215f, x0 + 0.30f, y1));
            }

            var detail = SurvivalVisuals.Image(panel, "Detail", new Color(0f, 0f, 0f, 0.28f));
            HookSkin.TrySkin(detail, HookArtKeys.Plate, 0.45f);
            Box(detail.rectTransform, 0.025f, 0.095f, 0.975f, 0.43f);
            var def = Campaign.Catalog.Hero(_selected);
            if (def != null)
            {
                refreshers.Add(def.Side == CampaignSide.Hero ? HeroDetail(detail.rectTransform, def) : FoeDetail(detail.rectTransform, def));
            }

            var closeX0 = 0.30f;
            var closeX1 = 0.70f;
            if (PowerPacks != null)
            {
                closeX0 = 0.52f;
                closeX1 = 0.92f;
                var powerX1 = Track != null ? 0.335f : 0.48f;
                var power = SheetButton(panel, "PowerUp", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, 0.03f, 0.025f, powerX1, 0.082f, 24, out var powerBtn);
                power.text = S("store.power_packs.button", "POWER UP");
                Wire(powerBtn, OpenPowerPacks);
            }

            if (Track != null)
            {
                var pathX0 = PowerPacks != null ? 0.35f : 0.03f;
                var path = SheetButton(panel, "KingdomPath", HookArtKeys.ButtonBlue, HeroFrame, pathX0, 0.025f, pathX0 + 0.305f, 0.082f, 24, out var pathBtn);
                path.text = S("reward_track.button", "KINGDOM PATH");
                Wire(pathBtn, OpenRewardTrack);
                var free = FreeTag(pathBtn.transform, "FreeBadge");
                var freeRt = free.rectTransform;
                refreshers.Add(() =>
                {
                    var show = Track.ClaimableCount() > 0;
                    if (free.gameObject.activeSelf != show)
                    {
                        free.gameObject.SetActive(show);
                        if (show)
                        {
                            StartCoroutine(Breathe(freeRt));
                        }
                    }
                });
                closeX0 = PowerPacks != null ? 0.67f : 0.35f;
                closeX1 = 0.97f;
            }

            var close = SheetButton(panel, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, closeX0, 0.025f, closeX1, 0.082f, 26, out var closeBtn);
            close.text = S("hook.close", "Close");
            Wire(closeBtn, CloseScreen);

            _refresh = () =>
            {
                queue.text = Fmt("campaign.training_slots", "Training {0}/{1}", roster.Training.Count, Campaign.Catalog.TrainingSlots);
                foreach (var r in refreshers)
                {
                    r();
                }
            };
            _refresh();
        }

        private void Tab(RectTransform panel, string name, string label, bool on, float x0, float x1, Action tap)
        {
            var key = on ? HookArtKeys.ButtonGold : HookArtKeys.ButtonBlue;
            var text = SheetButton(panel, name, key, on ? SurvivalVisuals.MineFill : SurvivalVisuals.PanelSoft, x0, 0.893f, x1, 0.937f, 24, out var btn);
            text.text = label;
            Wire(btn, tap);
        }

        /// <summary>Roster card: framed portrait, name plate with level and power, stars, lock + price or training bar.</summary>
        private Action HeroCard(RectTransform panel, CampaignHeroDef def, float x0, float y0, float x1, float y1)
        {
            var roster = Campaign!.Roster;
            var hero = def.Side == CampaignSide.Hero;
            var btn = SurvivalVisuals.Button(panel, "Card_" + def.Id, new Color(1f, 1f, 1f, 0f));
            btn.transition = Selectable.Transition.None;
            var rt = btn.GetComponent<RectTransform>();
            Box(rt, x0, y0, x1, y1);
            if (string.Equals(def.Id, _selected, StringComparison.Ordinal))
            {
                var glow = HookSkin.Glow(rt, "Glow", hero ? new Color(0.5f, 0.7f, 1f, 0.7f) : new Color(0.75f, 0.45f, 1f, 0.7f));
                Box(glow.rectTransform, -0.08f, -0.06f, 1.08f, 1.06f);
            }

            var frame = Portrait(rt, def, 0f, 0.22f, 1f, 1f, out var art);
            var plate = SurvivalVisuals.Image(rt, "Plate", hero ? HeroFrame : VillainFrame);
            Box(plate.rectTransform, 0f, 0f, 1f, 0.24f);
            var name = HookSkin.Outlined(SurvivalVisuals.Text(plate.transform, "Name", HeroName(def.Id), 24, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
            name.fontStyle = FontStyle.Bold;
            Box(name.rectTransform, 0.02f, 0.40f, 0.98f, 0.98f);
            var sub = HookSkin.Outlined(SurvivalVisuals.Text(plate.transform, "Sub", "", 19, TextAnchor.MiddleCenter, SurvivalVisuals.Reward), 1.2f);
            Box(sub.rectTransform, 0.02f, 0.02f, 0.98f, 0.46f);
            var stars = Stars(frame, 0.08f, 0.02f, 0.92f, 0.16f);
            var lockTag = SurvivalVisuals.Image(frame, "Lock", new Color(0f, 0f, 0f, 0.62f));
            Box(lockTag.rectTransform, 0f, 0.36f, 1f, 0.62f);
            var lockText = HookSkin.Outlined(SurvivalVisuals.Text(lockTag.transform, "T", "", 22, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
            lockText.fontStyle = FontStyle.Bold;
            SurvivalVisuals.Stretch(lockText.rectTransform);
            var barFrame = HookSkin.Bar(frame, "Training", out var barFill);
            Box(barFrame.rectTransform, 0.06f, 0.19f, 0.94f, 0.29f);
            var dot = Badge(rt, "Badge");
            Wire(btn, () =>
            {
                _selected = def.Id;
                OpenHeroes();
            });

            return () =>
            {
                if (!hero)
                {
                    sub.text = Title(def.Role);
                    SetStars(stars, 0);
                    lockTag.gameObject.SetActive(false);
                    barFrame.gameObject.SetActive(false);
                    dot.gameObject.SetActive(false);
                    return;
                }

                var p = roster.Get(def.Id);
                var owned = p != null && p.Owned;
                art.color = owned ? Color.white : new Color(0.42f, 0.42f, 0.48f, 1f);
                sub.text = owned
                    ? Fmt("campaign.card.level", "Lv {0}  \u00b7  \u2694 {1}", p!.Level, ChipWallet.FormatCompact(roster.Stats(def.Id).Power))
                    : Fmt("campaign.card.shards", "{0}/{1} shards", p?.Shards ?? 0, def.UnlockShards);
                SetStars(stars, owned ? p!.Stars : 0);
                lockTag.gameObject.SetActive(!owned);
                lockText.text = "\U0001F512 " + ChipWallet.FormatGrouped(def.UnlockGems) + " \u25c6";
                var training = roster.TrainingOf(def.Id);
                barFrame.gameObject.SetActive(training != null);
                if (training != null)
                {
                    HookSkin.SetBar(barFill, (float)training.Progress(roster.Clock.Now), HookArtKeys.BarFillBlue, HeroGlow);
                }

                dot.gameObject.SetActive(roster.CanFinishFree(def.Id) || roster.CanStarUp(def.Id) || roster.CanUnlockWithShards(def.Id) == UnlockCheck.Ok || roster.CanLevelUp(def.Id) == LevelUpCheck.Ok);
            };
        }

        private Action HeroDetail(RectTransform d, CampaignHeroDef def)
        {
            var roster = Campaign!.Roster;
            var wallet = Campaign.Wallet;
            Portrait(d, def, 0.025f, 0.24f, 0.345f, 0.975f, out _);
            var stars = Stars(d, 0.04f, 0.04f, 0.33f, 0.20f);
            var name = HookSkin.Outlined(SurvivalVisuals.Text(d, "Name", HeroName(def.Id), 36, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 2f);
            name.fontStyle = FontStyle.Bold;
            Box(name.rectTransform, 0.37f, 0.86f, 0.98f, 0.98f);
            var role = HookSkin.Outlined(SurvivalVisuals.Text(d, "Role", Title(def.Rarity) + "  \u00b7  " + Title(def.Role), 22, TextAnchor.MiddleLeft, RarityColor(def.Rarity)), 1.5f);
            Box(role.rectTransform, 0.37f, 0.78f, 0.74f, 0.86f);
            // Hero Tome (power packs): one level now, up to the star cap.
            var tomeText = SheetButton(d, "Tome", HookArtKeys.ButtonGreen, FreeGreen, 0.76f, 0.775f, 0.975f, 0.865f, 18, out var tomeBtn);
            var tomeImg = tomeBtn.GetComponent<Image>();
            var tomeRt = tomeBtn.GetComponent<RectTransform>();
            Wire(tomeBtn, () =>
            {
                if (wallet.Get(SurvIds.ResHeroXp) <= 0)
                {
                    OpenPowerPacks();
                    return;
                }

                var check = roster.TryUseTome(def.Id);
                if (check == LevelUpCheck.Ok)
                {
                    _hook?.Rise(tomeRt, S("hero.tome.used", "Tome used: level up!"), SurvivalVisuals.Reward);
                    _hook?.Sparks(tomeRt, HeroGlow, 12);
                    Campaign.Touch();
                    SaveNow();
                }
                else
                {
                    _hook?.Say(tomeRt, LevelReason(check), Warn);
                }

                _refresh?.Invoke();
            });
            var hp = StatLine(d, "Hp", 0.69f);
            var atk = StatLine(d, "Atk", 0.60f);
            var pow = StatLine(d, "Power", 0.51f);

            // Level row: Level Up (cost + time), or the training bar with Finish FREE / Speed up / Rush.
            var levelText = SheetButton(d, "LevelUp", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, 0.37f, 0.27f, 0.975f, 0.46f, 22, out var levelBtn);
            var levelImg = levelBtn.GetComponent<Image>();
            var levelRt = levelBtn.GetComponent<RectTransform>();
            var trainFrame = HookSkin.Bar(d, "TrainBar", out var trainFill);
            Box(trainFrame.rectTransform, 0.37f, 0.31f, 0.66f, 0.42f);
            var trainText = HookSkin.Outlined(SurvivalVisuals.Text(trainFrame.transform, "T", "", 20, TextAnchor.MiddleCenter, Color.white), 1.5f);
            SurvivalVisuals.Stretch(trainText.rectTransform);
            var rushText = SheetButton(d, "Rush", HookArtKeys.ButtonGreen, FreeGreen, 0.68f, 0.27f, 0.975f, 0.46f, 22, out var rushBtn);
            var rushImg = rushBtn.GetComponent<Image>();
            var rushRt = rushBtn.GetComponent<RectTransform>();

            // Star row: shard bar + Star Up (owned) / Recruit (locked).
            var shardIcon = HookSkin.ResIcon(d, "ShardIcon", RewardBundle.ShardKey(def.Id), ShardPurple, "\u2726");
            Box(shardIcon.rectTransform, 0.36f, 0.05f, 0.43f, 0.22f);
            var shardFrame = HookSkin.Bar(d, "ShardBar", out var shardFill);
            Box(shardFrame.rectTransform, 0.44f, 0.08f, 0.66f, 0.19f);
            var shardText = HookSkin.Outlined(SurvivalVisuals.Text(shardFrame.transform, "T", "", 20, TextAnchor.MiddleCenter, Color.white), 1.5f);
            SurvivalVisuals.Stretch(shardText.rectTransform);
            var starText = SheetButton(d, "StarUp", HookArtKeys.ButtonBlue, HeroFrame, 0.68f, 0.04f, 0.975f, 0.23f, 24, out var starBtn);
            var starImg = starBtn.GetComponent<Image>();
            var starRt = starBtn.GetComponent<RectTransform>();

            Wire(levelBtn, () =>
            {
                var check = roster.TryStartLevelUp(def.Id);
                if (check == LevelUpCheck.Ok)
                {
                    _hook?.Rise(levelRt, S("campaign.training", "Training!"), SurvivalVisuals.Reward);
                    _hook?.Sparks(levelRt, HeroGlow, 10);
                    Campaign.Touch();
                }
                else
                {
                    _hook?.Say(levelRt, LevelReason(check), Warn);
                }

                _refresh?.Invoke();
            });
            Wire(rushBtn, () =>
            {
                if (roster.TrainingOf(def.Id) == null)
                {
                    return;
                }

                var free = roster.CanFinishFree(def.Id) || roster.RushCost(def.Id) == 0;
                if (!free && wallet.Get(SurvIds.ResSpeedupM) > 0)
                {
                    var used = roster.ApplySpeedups(def.Id);
                    _hook?.Say(rushRt, Fmt("campaign.speedups_used", "-{0} min", used), SurvivalVisuals.PinBuild);
                }
                else if (roster.TryRush(def.Id, out var gems))
                {
                    if (gems > 0)
                    {
                        _hook?.Rise(rushRt, "-" + gems + " \u25c6", new Color(0.42f, 0.86f, 1f, 1f));
                    }
                }
                else
                {
                    _hook?.Say(rushRt, S("campaign.need_gems", "Not enough gems"), Warn);
                    _hook?.OpenGemShop();
                }

                Campaign.Touch();
                _refresh?.Invoke();
            });
            Wire(starBtn, () =>
            {
                if (!roster.IsOwned(def.Id))
                {
                    OpenRecruit(def);
                    return;
                }

                if (roster.TryStarUp(def.Id))
                {
                    _hook?.Sparks(starRt, StarOn, 20);
                }
                else
                {
                    _hook?.Say(starRt, roster.ShardsToStar(def.Id) == 0 ? S("campaign.max_stars", "MAX \u2605") : S("campaign.need_shards", "Win battles for shards"), Warn);
                }

                _refresh?.Invoke();
            });

            return () =>
            {
                var p = roster.Get(def.Id)!;
                var now = roster.Stats(def.Id);
                var training = roster.TrainingOf(def.Id);
                var check = roster.CanLevelUp(def.Id);
                var preview = p.Owned && check != LevelUpCheck.AtStarCap && check != LevelUpCheck.MaxLevel;
                var next = roster.NextLevelStats(def.Id);
                SetStat(hp, S("campaign.stat.hp", "HP"), now.Hp, preview ? next.Hp : 0);
                SetStat(atk, S("campaign.stat.atk", "ATK"), now.Atk, preview ? next.Atk : 0);
                SetStat(pow, S("campaign.stat.power", "POWER"), now.Power, preview ? next.Power : 0);
                SetStars(stars, p.Owned ? p.Stars : 0);
                name.text = HeroName(def.Id) + (p.Owned ? "   " + Fmt("campaign.level", "Lv {0}/{1}", p.Level, roster.LevelCap(def.Id)) : "");
                var tomes = wallet.Get(SurvIds.ResHeroXp);
                tomeBtn.gameObject.SetActive(p.Owned && (tomes > 0 || PowerPacks != null));
                tomeText.text = Fmt("hero.tome.use", "Tome ({0})", tomes);
                var tomeOk = tomes <= 0 || roster.CanUseTome(def.Id) == LevelUpCheck.Ok;
                HookSkin.SetButton(tomeImg, HookArtKeys.ButtonGreen, tomeOk ? FreeGreen : Disabled, tomeOk, 0.5f);

                levelBtn.gameObject.SetActive(p.Owned && training == null);
                trainFrame.gameObject.SetActive(training != null);
                // Inside the free window the finish moves to the Village FREE hero badge; the timer stays, Rush hides.
                var inFree = training != null && (roster.CanFinishFree(def.Id) || roster.RushCost(def.Id) == 0);
                rushBtn.gameObject.SetActive(training != null && !inFree);
                if (p.Owned && training == null)
                {
                    var cost = roster.LevelUpCost(def.Id);
                    var ok = check == LevelUpCheck.Ok;
                    levelText.text = check == LevelUpCheck.AtStarCap || check == LevelUpCheck.MaxLevel || check == LevelUpCheck.NoSlot
                        ? LevelReason(check)
                        : Fmt("campaign.level_up", "LEVEL UP  {0} Gold \u00b7 {1} Food \u00b7 {2}", ChipWallet.FormatCompact(Cost(cost, SurvIds.ResSoft)), ChipWallet.FormatCompact(Cost(cost, SurvIds.ResFood)), TimerQueue.FormatClock(roster.LevelUpSeconds(def.Id)));
                    HookSkin.SetButton(levelImg, HookArtKeys.ButtonGold, ok ? SurvivalVisuals.MineFill : Disabled, ok, 0.5f);
                    levelText.color = ok ? HookSkin.InkFor(HookArtKeys.ButtonGold) : SurvivalVisuals.Cream;
                }

                if (training != null)
                {
                    var left = training.Remaining(roster.Clock.Now);
                    HookSkin.SetBar(trainFill, (float)training.Progress(roster.Clock.Now), HookArtKeys.BarFillBlue, HeroGlow);
                    trainText.text = Fmt("campaign.training_to", "Lv {0} in {1}", p.Level + 1, TimerQueue.FormatClock(left));
                    var free = roster.CanFinishFree(def.Id) || roster.RushCost(def.Id) == 0;
                    var speed = !free && wallet.Get(SurvIds.ResSpeedupM) > 0;
                    rushText.text = free ? S("hook.free", "FREE") : speed ? Fmt("campaign.speedup", "SPEED UP {0}m", wallet.Get(SurvIds.ResSpeedupM)) : Fmt("campaign.rush", "RUSH {0} \u25c6", roster.RushCost(def.Id));
                    var key = free ? HookArtKeys.ButtonGreen : HookArtKeys.ButtonGold;
                    HookSkin.SetButton(rushImg, key, free ? FreeGreen : SurvivalVisuals.MineFill, true, 0.5f);
                    rushText.color = HookSkin.InkFor(key);
                }

                if (p.Owned)
                {
                    var need = roster.ShardsToStar(def.Id);
                    shardText.text = need > 0 ? p.Shards + "/" + need : p.Shards.ToString(CultureInfo.InvariantCulture);
                    HookSkin.SetBar(shardFill, need > 0 ? Mathf.Clamp01(p.Shards / (float)need) : 1f, HookArtKeys.BarFillGold, ShardPurple);
                    starText.text = need > 0 ? S("campaign.star_up", "STAR UP \u2605") : S("campaign.max_stars", "MAX \u2605");
                    var can = roster.CanStarUp(def.Id);
                    HookSkin.SetButton(starImg, HookArtKeys.ButtonBlue, can ? HeroFrame : Disabled, can, 0.5f);
                    starText.color = HookSkin.InkFor(HookArtKeys.ButtonBlue);
                }
                else
                {
                    shardText.text = p.Shards + "/" + def.UnlockShards;
                    HookSkin.SetBar(shardFill, def.UnlockShards > 0 ? Mathf.Clamp01(p.Shards / (float)def.UnlockShards) : 0f, HookArtKeys.BarFillGold, ShardPurple);
                    starText.text = S("campaign.recruit", "RECRUIT");
                    HookSkin.SetButton(starImg, HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, true, 0.5f);
                    starText.color = HookSkin.InkFor(HookArtKeys.ButtonGold);
                }
            };
        }

        private static int Cost(IReadOnlyDictionary<string, int> cost, string id) => cost.TryGetValue(id, out var v) ? v : 0;

        private Action FoeDetail(RectTransform d, CampaignHeroDef def)
        {
            Portrait(d, def, 0.025f, 0.24f, 0.345f, 0.975f, out _);
            var name = HookSkin.Outlined(SurvivalVisuals.Text(d, "Name", HeroName(def.Id), 36, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 2f);
            name.fontStyle = FontStyle.Bold;
            Box(name.rectTransform, 0.37f, 0.86f, 0.98f, 0.98f);
            var role = HookSkin.Outlined(SurvivalVisuals.Text(d, "Role", Title(def.Rarity) + "  \u00b7  " + Title(def.Role), 22, TextAnchor.MiddleLeft, VillainGlow), 1.5f);
            Box(role.rectTransform, 0.37f, 0.78f, 0.98f, 0.86f);
            var st = HeroStats.Of(def, Campaign!.Catalog.Curve, 1, 1);
            SetStat(StatLine(d, "Hp", 0.69f), S("campaign.stat.hp", "HP"), st.Hp, 0);
            SetStat(StatLine(d, "Atk", 0.60f), S("campaign.stat.atk", "ATK"), st.Atk, 0);
            SetStat(StatLine(d, "Power", 0.51f), S("campaign.stat.power", "POWER"), st.Power, 0);
            var seen = new List<string>();
            foreach (var s in Campaign.Catalog.Stages)
            {
                foreach (var e in s.Enemies)
                {
                    if (e.HeroId == def.Id && !seen.Contains(s.Name))
                    {
                        seen.Add(s.Name);
                    }
                }
            }

            var where = HookSkin.Outlined(SurvivalVisuals.Text(d, "Where", Fmt("campaign.foe.where", "Fights in: {0}", seen.Count > 0 ? string.Join(", ", seen) : S("campaign.foe.camps", "World camps")), 22, TextAnchor.UpperLeft, SurvivalVisuals.Cream), 1.2f);
            where.horizontalOverflow = HorizontalWrapMode.Wrap;
            Box(where.rectTransform, 0.37f, 0.06f, 0.97f, 0.44f);
            return () => { };
        }

        private Text StatLine(RectTransform d, string name, float y)
        {
            var t = HookSkin.Outlined(SurvivalVisuals.Text(d, name, "", 28, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.5f);
            t.supportRichText = true;
            Box(t.rectTransform, 0.37f, y, 0.98f, y + 0.09f);
            return t;
        }

        private static void SetStat(Text t, string label, int now, int next)
        {
            var arrow = next > now ? "  <color=#7ee08a>\u2192 " + ChipWallet.FormatGrouped(next) + "  (+" + ChipWallet.FormatGrouped(next - now) + ")</color>" : "";
            t.text = "<color=#e6d7a0>" + label + "</color>  <b>" + ChipWallet.FormatGrouped(now) + "</b>" + arrow;
        }

        private string LevelReason(LevelUpCheck check) => check switch
        {
            LevelUpCheck.AtStarCap => S("campaign.reason.star_cap", "STAR UP TO KEEP LEVELLING"),
            LevelUpCheck.MaxLevel => S("campaign.reason.max", "MAX LEVEL"),
            LevelUpCheck.NoSlot => S("campaign.reason.slot", "Both training slots busy"),
            LevelUpCheck.CantAfford => S("campaign.reason.afford", "Need more gold / food"),
            LevelUpCheck.NotOwned => S("campaign.reason.locked", "Recruit first"),
            _ => S("campaign.reason.busy", "Training\u2026")
        };

        /// <summary>Sandbox recruit sheet in the Gem Shop's style: unlock now for gems, or for shards once collected.</summary>
        private void OpenRecruit(CampaignHeroDef def)
        {
            if (Campaign == null)
            {
                return;
            }

            var roster = Campaign.Roster;
            var panel = OpenSheet("RecruitSheet", Fmt("campaign.recruit_title", "Recruit {0}", HeroName(def.Id)), 0.50f);
            Portrait(panel, def, 0.05f, 0.50f, 0.36f, 0.84f, out _);
            var blurb = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Blurb", Title(def.Rarity) + " " + Title(def.Role), 26, TextAnchor.MiddleLeft, RarityColor(def.Rarity)), 1.5f);
            Box(blurb.rectTransform, 0.40f, 0.74f, 0.96f, 0.82f);
            var st = HeroStats.Of(def, Campaign.Catalog.Curve, 1, 1);
            var stats = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Stats", Fmt("campaign.recruit_stats", "HP {0}  \u00b7  ATK {1}\nPOWER {2}", ChipWallet.FormatGrouped(st.Hp), st.Atk, ChipWallet.FormatGrouped(st.Power)), 24, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.5f);
            Box(stats.rectTransform, 0.40f, 0.56f, 0.96f, 0.73f);
            var sandbox = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Sandbox", S("store.sandbox", "SANDBOX \u00b7 no real charge"), 16, TextAnchor.MiddleCenter, Warn), 1.5f);
            Box(sandbox.rectTransform, 0f, 0.47f, 1f, 0.50f);

            var gemRow = HookSkin.Panel(panel, "GemRow", HookArtKeys.Plate, SurvivalVisuals.RowFill, 0.45f);
            Box(gemRow.rectTransform, 0.04f, 0.31f, 0.96f, 0.46f);
            var gemIcon = HookSkin.ResIcon(gemRow.transform, "Icon", SurvIds.ResHard, new Color(0.42f, 0.86f, 1f, 1f), "\u25c6");
            Box(gemIcon.rectTransform, 0.01f, 0.05f, 0.18f, 0.95f);
            var gemLine = HookSkin.Outlined(SurvivalVisuals.Text(gemRow.transform, "T", S("campaign.recruit_now", "Recruit now"), 24, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.5f);
            Box(gemLine.rectTransform, 0.20f, 0f, 0.62f, 1f);
            var gemText = SheetButton(gemRow.rectTransform, "BuyGems", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, 0.64f, 0.14f, 0.98f, 0.86f, 24, out var gemBtn);
            gemText.text = ChipWallet.FormatGrouped(def.UnlockGems) + " \u25c6";
            var gemRt = gemBtn.GetComponent<RectTransform>();

            var shardRow = HookSkin.Panel(panel, "ShardRow", HookArtKeys.Plate, SurvivalVisuals.RowFill, 0.45f);
            Box(shardRow.rectTransform, 0.04f, 0.15f, 0.96f, 0.30f);
            var shardIcon = HookSkin.ResIcon(shardRow.transform, "Icon", RewardBundle.ShardKey(def.Id), ShardPurple, "\u2726");
            Box(shardIcon.rectTransform, 0.01f, 0.05f, 0.18f, 0.95f);
            var shardLine = HookSkin.Outlined(SurvivalVisuals.Text(shardRow.transform, "T", "", 24, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.5f);
            Box(shardLine.rectTransform, 0.20f, 0f, 0.62f, 1f);
            var shardText = SheetButton(shardRow.rectTransform, "UseShards", HookArtKeys.ButtonGreen, FreeGreen, 0.64f, 0.14f, 0.98f, 0.86f, 24, out var shardBtn);
            shardText.text = S("campaign.unlock", "Unlock");
            var shardImg = shardBtn.GetComponent<Image>();
            var shardRt = shardBtn.GetComponent<RectTransform>();

            var getGems = SheetButton(panel, "GetGems", HookArtKeys.ButtonBlue, HeroFrame, 0.06f, 0.03f, 0.48f, 0.12f, 22, out var getBtn);
            getGems.text = S("campaign.get_gems", "Get Gems");
            Wire(getBtn, () => _hook?.OpenGemShop());
            var close = SheetButton(panel, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.52f, 0.03f, 0.94f, 0.12f, 22, out var closeBtn);
            close.text = S("hook.close", "Close");
            Wire(closeBtn, CloseSheet);

            Wire(gemBtn, () =>
            {
                if (roster.TryUnlockWithGems(def.Id) == UnlockCheck.Ok)
                {
                    CloseSheet();
                    _selected = def.Id;
                    OpenHeroes();
                }
                else
                {
                    _hook?.Say(gemRt, S("campaign.need_gems", "Not enough gems"), Warn);
                    _hook?.OpenGemShop();
                }
            });
            Wire(shardBtn, () =>
            {
                if (roster.TryUnlockWithShards(def.Id) == UnlockCheck.Ok)
                {
                    CloseSheet();
                    _selected = def.Id;
                    OpenHeroes();
                }
                else
                {
                    _hook?.Say(shardRt, S("campaign.need_shards", "Win battles for shards"), Warn);
                }
            });

            _sheetRefresh = () =>
            {
                var p = roster.Get(def.Id);
                shardLine.text = Fmt("campaign.recruit_shards", "{0}/{1} shards", p?.Shards ?? 0, def.UnlockShards);
                var ok = roster.CanUnlockWithShards(def.Id) == UnlockCheck.Ok;
                HookSkin.SetButton(shardImg, HookArtKeys.ButtonGreen, ok ? FreeGreen : Disabled, ok, 0.5f);
            };
            _sheetRefresh();
        }

        // ---------- Power Packs ----------

        private static PowerPackShop? LoadPowerPacks(SurvivalSession session, CampaignSession campaign)
        {
            var path = Path.Combine(SurvivalBootstrap.ResolveCatalogDir(), PowerPackCatalog.FileName);
            if (session.Hook == null || !PlayerAssets.FileExists(path))
            {
                return null;
            }

            try
            {
                var shop = new PowerPackShop(PowerPackCatalog.Parse(PlayerAssets.ReadAllText(path)), session.Hook.Store, campaign);
                return shop.Store.Find(shop.Catalog.StoreId) != null && ReferenceEquals(shop.Store.Wallet, campaign.Wallet) ? shop : null;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }

        /// <summary>
        /// The power-up ladder sheet ($0.99 .. $39.99 + the one-time starter): title, contents, "+X% value" against the $0.99
        /// pack, price from the theme's products.json. Sandbox purchases only (no real billing in this build).
        /// </summary>
        public void OpenPowerPacks()
        {
            if (Campaign == null || PowerPacks == null || _overlay == null)
            {
                return;
            }

            var shop = PowerPacks;
            var panel = OpenSheet(PowerPacksSheetName, S("store.power_packs.title", "Power Packs"), 0.88f);
            var have = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Have", "", 20, TextAnchor.MiddleCenter, SurvivalVisuals.Reward), 1.5f);
            Box(have.rectTransform, 0.03f, 0.845f, 0.97f, 0.875f);
            var sandbox = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Sandbox", S("store.sandbox", "SANDBOX \u00b7 no real charge"), 16, TextAnchor.MiddleCenter, Warn), 1.5f);
            Box(sandbox.rectTransform, 0f, 0.82f, 1f, 0.845f);

            var rows = shop.Available();
            var top = 0.81f;
            var h = Mathf.Min(0.095f, (top - 0.10f) / Mathf.Max(1, rows.Count));
            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var y1 = top - (i * h);
                var row = HookSkin.Panel(panel, "Pack_" + r.Offer.Id, HookArtKeys.Plate, SurvivalVisuals.RowFill, 0.45f);
                Box(row.rectTransform, 0.03f, y1 - h + 0.006f, 0.97f, y1);
                var title = HookSkin.Outlined(SurvivalVisuals.Text(row.transform, "Name", S(r.Offer.Id + ".title", r.Offer.Id), 22, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.5f);
                title.fontStyle = FontStyle.Bold;
                Box(title.rectTransform, 0.03f, 0.52f, 0.48f, 0.96f);
                var tagLine = r.Offer.Tag is { Length: > 0 } tag ? S("offer.tag." + tag, tag.ToUpperInvariant()) : "";
                var bonus = r.BonusLabel.Length > 0 ? Fmt("store.power_packs.value", "{0} value", r.BonusLabel) : "";
                var bonusText = HookSkin.Outlined(SurvivalVisuals.Text(row.transform, "Bonus", (bonus + (bonus.Length > 0 && tagLine.Length > 0 ? "  \u00b7  " : "") + tagLine), 18, TextAnchor.MiddleRight, FreeGreen), 1.5f);
                bonusText.fontStyle = FontStyle.Bold;
                Box(bonusText.rectTransform, 0.48f, 0.52f, 0.66f, 0.96f);
                var contents = HookSkin.Outlined(SurvivalVisuals.Text(row.transform, "Contents", PackLine(r.Grants), 15, TextAnchor.MiddleLeft, SurvivalVisuals.Reward), 1.2f);
                Box(contents.rectTransform, 0.03f, 0.04f, 0.66f, 0.52f);
                var price = SheetButton((RectTransform)row.transform, "Buy", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, 0.68f, 0.14f, 0.98f, 0.86f, 22, out var buyBtn);
                price.text = shop.PriceLabel(r);
                var buyRt = buyBtn.GetComponent<RectTransform>();
                var offerId = r.Offer.Id;
                var grants = new Dictionary<string, int>(r.Offer.Grants);
                var name = title.text;
                Wire(buyBtn, () => shop.Buy(offerId, outcome =>
                {
                    if (outcome.Ok)
                    {
                        _hook?.Celebrate(name, grants);
                        SaveNow();
                        OpenPowerPacks();
                        _refresh?.Invoke();
                    }
                    else
                    {
                        _hook?.Say(buyRt, S("store.power_packs.failed", "Purchase failed"), Warn);
                    }
                }));
            }

            var close = SheetButton(panel, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.30f, 0.02f, 0.70f, 0.085f, 24, out var closeBtn);
            close.text = S("hook.close", "Close");
            Wire(closeBtn, CloseSheet);

            _sheetRefresh = () =>
            {
                have.text = ChipWallet.FormatGrouped(Campaign.Wallet.Get(SurvIds.ResHeroXp)) + " " + S(SurvIds.ResHeroXp + ".name", "Hero Tome") + "   \u00b7   "
                    + ChipWallet.FormatGrouped(Campaign.Wallet.Get(SurvIds.ResRunnerBoost)) + " " + S(SurvIds.ResRunnerBoost + ".name", "War Banner");
            };
            _sheetRefresh();
        }

        // ---------- Kingdom Path (reward track) ----------

        private static RewardTrack? LoadRewardTrack(SurvivalSession session, CampaignSession campaign)
        {
            var path = Path.Combine(SurvivalBootstrap.ResolveCatalogDir(), RewardTrackCatalog.FileName);
            if (session.Hook == null || !PlayerAssets.FileExists(path))
            {
                return null;
            }

            try
            {
                var track = new RewardTrack(RewardTrackCatalog.Parse(PlayerAssets.ReadAllText(path)), session.Hook.Store, campaign);
                return track.Store.Find(SurvIds.StoreRewardTrack) != null && ReferenceEquals(track.Store.Wallet, campaign.Wallet) ? track : null;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }

        /// <summary>A green "FREE" pill on a store button (hidden until something free is claimable).</summary>
        private static Image FreeTag(Transform parent, string name)
        {
            var pill = SurvivalVisuals.Image(parent, name, FreeGreen);
            HookSkin.TrySkin(pill, HookArtKeys.ButtonGreen, 0.5f);
            pill.rectTransform.anchorMin = pill.rectTransform.anchorMax = new Vector2(0.86f, 0.92f);
            pill.rectTransform.sizeDelta = new Vector2(78f, 34f);
            var t = HookSkin.Outlined(SurvivalVisuals.Text(pill.transform, "T", "FREE", 20, TextAnchor.MiddleCenter, Color.white), 1.5f);
            t.fontStyle = FontStyle.Bold;
            SurvivalVisuals.Stretch(t.rectTransform);
            pill.raycastTarget = false;
            pill.gameObject.SetActive(false);
            return pill;
        }

        private const int TrackRowsPerPage = 6;

        /// <summary>
        /// The Kingdom Path sheet: three columns per milestone (FREE, PREMIUM $2.99, ROYAL $3.99 which opens both paid
        /// columns; a Premium owner sees the $0.99 upgrade). Reached cells pulse CLAIM, locked ones show a lock and the price,
        /// buying late opens every reached row. Sandbox purchases only (no real billing in this build).
        /// </summary>
        public void OpenRewardTrack()
        {
            if (Campaign == null || Track == null || _overlay == null)
            {
                return;
            }

            var track = Track;
            var rows = track.Catalog.Rows;
            var pages = Mathf.Max(1, (rows.Count + TrackRowsPerPage - 1) / TrackRowsPerPage);
            if (_trackPage < 0)
            {
                var next = 0;
                while (next < rows.Count && track.Progress >= rows[next].Level)
                {
                    next++;
                }

                _trackPage = Mathf.Clamp((next == 0 ? 0 : next - 1) / TrackRowsPerPage, 0, pages - 1);
            }

            _trackPage = Mathf.Clamp(_trackPage, 0, pages - 1);
            var panel = OpenSheet(RewardTrackSheetName, S("reward_track.title", track.Catalog.Title), 0.90f);
            var progress = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Progress", Fmt("reward_track.progress", "Runner levels beaten: {0}", track.Progress), 20, TextAnchor.MiddleCenter, SurvivalVisuals.Reward), 1.5f);
            Box(progress.rectTransform, 0.03f, 0.85f, 0.97f, 0.88f);
            var sandbox = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Sandbox", S("store.sandbox", "SANDBOX \u00b7 no real charge"), 16, TextAnchor.MiddleCenter, Warn), 1.5f);
            Box(sandbox.rectTransform, 0f, 0.828f, 1f, 0.85f);

            // Header: the three column titles; the paid ones carry their buy button.
            var colX = new[] { 0.17f, 0.445f, 0.72f };
            const float colW = 0.265f;
            HeaderCell(panel, "HeadFree", colX[0], colW, S("reward_track.free", "FREE"), FreeGreen, null, null, null);
            HeaderCell(panel, "HeadPremium", colX[1], colW, S("reward_track.premium", "PREMIUM"), SurvivalVisuals.Gold,
                track.PremiumOfferNow is { } p ? track.PriceLabel(p) : null, track.Owns(TrackColumn.Premium) ? S("reward_track.owned", "OWNED") : null,
                () => BuyTrack(track.BuyPremium, S("reward_track.premium", "PREMIUM")));
            HeaderCell(panel, "HeadRoyal", colX[2], colW, S("reward_track.royal", "ROYAL"), RoyalBlue,
                track.RoyalOfferNow is { } r ? track.PriceLabel(r) + (track.Tier == TrackTier.Premium ? " " + S("reward_track.upgrade", "UPGRADE") : "") : null,
                track.Owns(TrackColumn.Royal) ? S("reward_track.owned", "OWNED") : null,
                () => BuyTrack(track.BuyRoyal, S("reward_track.royal", "ROYAL")));
            var pitch = track.Tier == TrackTier.None ? S("reward_track.pitch_royal", "Royal opens PREMIUM + ROYAL") : "";
            var pitchText = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Pitch", pitch, 15, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.2f);
            Box(pitchText.rectTransform, colX[1], 0.675f, colX[2] + colW, 0.70f);

            var top = 0.67f;
            var h = (top - 0.17f) / TrackRowsPerPage;
            for (var i = 0; i < TrackRowsPerPage; i++)
            {
                var idx = (_trackPage * TrackRowsPerPage) + i;
                if (idx >= rows.Count)
                {
                    break;
                }

                var row = rows[idx];
                var y1 = top - (i * h);
                var reached = track.Progress >= row.Level;
                var plate = HookSkin.Panel(panel, "Milestone_" + row.Level, HookArtKeys.Plate, reached ? SurvivalVisuals.RowFill : HeroFrame, 0.45f);
                Box(plate.rectTransform, 0.03f, y1 - h + 0.006f, 0.165f, y1);
                var lvl = HookSkin.Outlined(SurvivalVisuals.Text(plate.transform, "L", Fmt("reward_track.level", "LV\n{0}", row.Level), 22, TextAnchor.MiddleCenter, reached ? SurvivalVisuals.Gold : SurvivalVisuals.Mute), 1.5f);
                lvl.fontStyle = FontStyle.Bold;
                SurvivalVisuals.Stretch(lvl.rectTransform);
                for (var c = 0; c < 3; c++)
                {
                    TrackCell(panel, track, row, (TrackColumn)c, colX[c], y1 - h + 0.006f, colX[c] + colW, y1);
                }
            }

            var prev = SheetButton(panel, "Prev", HookArtKeys.ButtonBlue, HeroFrame, 0.03f, 0.10f, 0.20f, 0.155f, 22, out var prevBtn);
            prev.text = "\u25C0";
            Wire(prevBtn, () =>
            {
                _trackPage = Mathf.Max(0, _trackPage - 1);
                OpenRewardTrack();
            });
            var pageText = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Page", (_trackPage + 1) + " / " + pages, 18, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.2f);
            Box(pageText.rectTransform, 0.20f, 0.10f, 0.30f, 0.155f);
            var nextB = SheetButton(panel, "Next", HookArtKeys.ButtonBlue, HeroFrame, 0.30f, 0.10f, 0.47f, 0.155f, 22, out var nextBtn);
            nextB.text = "\u25B6";
            Wire(nextBtn, () =>
            {
                _trackPage = Mathf.Min(pages - 1, _trackPage + 1);
                OpenRewardTrack();
            });
            var n = track.ClaimableCount();
            var all = SheetButton(panel, "ClaimAll", n > 0 ? HookArtKeys.ButtonGreen : HookArtKeys.ButtonBlue, n > 0 ? FreeGreen : Disabled, 0.50f, 0.10f, 0.97f, 0.155f, 22, out var allBtn);
            all.text = n > 0 ? Fmt("reward_track.claim_all", "CLAIM ALL ({0})", n) : S("reward_track.nothing", "Nothing to claim");
            if (n > 0)
            {
                StartCoroutine(Breathe(allBtn.GetComponent<RectTransform>()));
            }

            Wire(allBtn, () =>
            {
                var got = track.ClaimAll();
                if (got.Count > 0)
                {
                    Celebrate(S("reward_track.claimed", "Rewards claimed!"), TrackRewards(got));
                    SaveNow();
                }

                OpenRewardTrack();
                _refresh?.Invoke();
            });

            var close = SheetButton(panel, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.30f, 0.02f, 0.70f, 0.085f, 24, out var closeBtn);
            close.text = S("hook.close", "Close");
            Wire(closeBtn, () =>
            {
                _trackPage = -1;
                CloseSheet();
            });
        }

        private static readonly Color RoyalBlue = new Color(0.20f, 0.36f, 0.85f, 1f);

        private void HeaderCell(RectTransform panel, string name, float x0, float w, string title, Color color, string? price, string? owned, Action? buy)
        {
            var head = HookSkin.Panel(panel, name, HookArtKeys.Plate, new Color(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f, 1f), 0.45f);
            Box(head.rectTransform, x0, 0.705f, x0 + w - 0.01f, 0.825f);
            var t = HookSkin.Outlined(SurvivalVisuals.Text(head.transform, "T", title, 24, TextAnchor.MiddleCenter, color), 2f);
            t.fontStyle = FontStyle.Bold;
            Box(t.rectTransform, 0f, 0.52f, 1f, 1f);
            if (price != null && buy != null)
            {
                var label = SheetButton((RectTransform)head.transform, "Buy", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, 0.06f, 0.06f, 0.94f, 0.50f, 20, out var btn);
                label.text = price;
                Wire(btn, buy);
            }
            else
            {
                var sub = HookSkin.Outlined(SurvivalVisuals.Text(head.transform, "Sub", owned ?? S("reward_track.everyone", "EVERYONE"), 18, TextAnchor.MiddleCenter, owned != null ? FreeGreen : SurvivalVisuals.Cream), 1.5f);
                sub.fontStyle = FontStyle.Bold;
                Box(sub.rectTransform, 0f, 0.06f, 1f, 0.50f);
            }
        }

        private void TrackCell(RectTransform panel, RewardTrack track, TrackRow row, TrackColumn col, float x0, float y0, float x1, float y1)
        {
            var state = track.State(row, col);
            var fill = state switch
            {
                TrackCellState.Claimable => new Color(0.16f, 0.42f, 0.20f, 1f),
                TrackCellState.Claimed => new Color(0.16f, 0.17f, 0.20f, 1f),
                TrackCellState.Locked => new Color(0.12f, 0.14f, 0.24f, 1f),
                _ => SurvivalVisuals.RowFill
            };
            var cell = HookSkin.Panel(panel, "Cell_" + row.Level + "_" + col, HookArtKeys.Plate, fill, 0.45f);
            Box(cell.rectTransform, x0, y0, x1 - 0.01f, y1);
            if (state == TrackCellState.Claimed || state == TrackCellState.Locked)
            {
                cell.color = new Color(cell.color.r * 0.75f, cell.color.g * 0.75f, cell.color.b * 0.75f, 1f);
            }

            var line = HookSkin.Outlined(SurvivalVisuals.Text(cell.transform, "Reward", TrackLine(row.Rewards(col)), 17, TextAnchor.MiddleCenter,
                state == TrackCellState.Claimed ? SurvivalVisuals.Mute : SurvivalVisuals.Reward), 1.2f);
            Box(line.rectTransform, 0.03f, 0.48f, 0.97f, 0.98f);
            switch (state)
            {
                case TrackCellState.Claimable:
                {
                    var label = SheetButton((RectTransform)cell.transform, "Claim", HookArtKeys.ButtonGreen, FreeGreen, 0.10f, 0.06f, 0.90f, 0.46f, 18, out var btn);
                    label.text = S("reward_track.claim", "CLAIM");
                    var rt = btn.GetComponent<RectTransform>();
                    StartCoroutine(Breathe(rt));
                    var level = row.Level;
                    Wire(btn, () =>
                    {
                        if (track.Claim(level, col) is { } got)
                        {
                            Juice(rt);
                            Celebrate(Fmt("reward_track.cell_claimed", "Level {0} reward!", level), TrackRewards(got));
                            SaveNow();
                        }

                        OpenRewardTrack();
                        _refresh?.Invoke();
                    });
                    break;
                }

                case TrackCellState.Locked:
                {
                    var offer = col == TrackColumn.Premium ? track.PremiumOfferNow ?? track.RoyalOfferNow : track.RoyalOfferNow;
                    var label = SheetButton((RectTransform)cell.transform, "Unlock", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, 0.10f, 0.06f, 0.90f, 0.46f, 17, out var btn);
                    label.text = S("reward_track.unlock", "UNLOCK") + " " + (offer != null ? track.PriceLabel(offer) : "");
                    Wire(btn, () => BuyTrack(col == TrackColumn.Premium && track.PremiumOfferNow != null ? track.BuyPremium : track.BuyRoyal,
                        col == TrackColumn.Premium ? S("reward_track.premium", "PREMIUM") : S("reward_track.royal", "ROYAL")));
                    break;
                }

                default:
                {
                    var mark = HookSkin.Outlined(SurvivalVisuals.Text(cell.transform, "State",
                        state == TrackCellState.Claimed ? "\u2713 " + S("reward_track.done", "CLAIMED") : Fmt("reward_track.at", "at LV {0}", row.Level),
                        16, TextAnchor.MiddleCenter, state == TrackCellState.Claimed ? FreeGreen : SurvivalVisuals.Mute), 1.2f);
                    mark.fontStyle = FontStyle.Bold;
                    Box(mark.rectTransform, 0.03f, 0.06f, 0.97f, 0.46f);
                    break;
                }
            }
        }

        private void BuyTrack(Action<Action<PurchaseOutcome>> buy, string name)
        {
            if (Track == null)
            {
                return;
            }

            var opened = Track.LockedReached(TrackColumn.Premium) + Track.LockedReached(TrackColumn.Royal);
            buy(outcome =>
            {
                if (outcome.Ok)
                {
                    var now = Track.LockedReached(TrackColumn.Premium) + Track.LockedReached(TrackColumn.Royal);
                    Celebrate(Fmt("reward_track.bought", "{0} unlocked! {1} rewards ready", name, Mathf.Max(0, opened - now)), new Dictionary<string, int>());
                    SaveNow();
                    OpenRewardTrack();
                    _refresh?.Invoke();
                }
                else
                {
                    _hook?.Say(_overlay!, S("store.power_packs.failed", "Purchase failed"), Warn);
                }
            });
        }

        /// <summary>Wallet rewards as they are; "shard:&lt;hero&gt;" as "&lt;Hero&gt; Shards".</summary>
        private Dictionary<string, int> TrackRewards(IReadOnlyDictionary<string, int> got)
        {
            var rows = new Dictionary<string, int>();
            foreach (var g in got)
            {
                var key = g.Key.StartsWith(RewardTrackCatalog.ShardPrefix, StringComparison.Ordinal)
                    ? HeroName(g.Key.Substring(RewardTrackCatalog.ShardPrefix.Length)) + " " + S("reward_track.shards", "Shards")
                    : g.Key;
                rows[key] = (rows.TryGetValue(key, out var v) ? v : 0) + g.Value;
            }

            return rows;
        }

        private string TrackLine(IReadOnlyDictionary<string, int> grants)
        {
            var parts = new List<string>();
            foreach (var pair in grants)
            {
                var name = pair.Key.StartsWith(RewardTrackCatalog.ShardPrefix, StringComparison.Ordinal)
                    ? HeroName(pair.Key.Substring(RewardTrackCatalog.ShardPrefix.Length)) + " " + S("reward_track.shards", "Shards")
                    : S(pair.Key + ".name", pair.Key);
                parts.Add("+" + ChipWallet.FormatCompact(pair.Value) + " " + name);
            }

            return string.Join("\n", parts);
        }

        private string PackLine(IReadOnlyDictionary<string, int> grants)
        {
            var parts = new List<string>();
            foreach (var pair in grants)
            {
                parts.Add("+" + ChipWallet.FormatCompact(pair.Value) + " " + S(pair.Key + ".name", pair.Key));
            }

            return string.Join("  ", parts);
        }

        // ---------- Battle Setup ----------

        public void OpenSetup(StageDef? stage)
        {
            if (Campaign == null || _overlay == null || stage == null || _inBattle)
            {
                return;
            }

            var panel = OpenScreen(SetupScreenName, S("campaign.setup.title", "BATTLE"));
            var setup = Campaign.Setup(stage);
            _setup = setup;

            // Stage strip, or the World camp's name plate.
            if (stage.CampId != null)
            {
                var plate = HookSkin.Panel(panel, "CampPlate", HookArtKeys.Plate, SurvivalVisuals.RowFill, 0.45f);
                Box(plate.rectTransform, 0.06f, 0.865f, 0.94f, 0.935f);
                var t = HookSkin.Outlined(SurvivalVisuals.Text(plate.transform, "T", Fmt("campaign.setup.camp", "World camp: {0}", stage.Name), 28, TextAnchor.MiddleCenter, SurvivalVisuals.Reward), 1.5f);
                t.fontStyle = FontStyle.Bold;
                SurvivalVisuals.Stretch(t.rectTransform);
            }
            else
            {
                var stages = Campaign.Catalog.Stages;
                var w = 0.95f / Mathf.Max(1, stages.Count);
                for (var i = 0; i < stages.Count; i++)
                {
                    var s = stages[i];
                    var on = s.Id == stage.Id;
                    var open = Campaign.IsUnlocked(s.Id);
                    var key = on ? HookArtKeys.ButtonGold : HookArtKeys.ButtonBlue;
                    var x0 = 0.025f + i * w;
                    var label = SheetButton(panel, "Stage_" + s.Id, key, on ? SurvivalVisuals.MineFill : HeroFrame, x0 + 0.004f, 0.862f, x0 + w - 0.004f, 0.938f, 19, out var sb);
                    label.text = (i + 1) + (Campaign.IsCleared(s.Id) ? " \u2713" : "") + "\n" + s.Name;
                    label.color = on ? HookSkin.InkFor(HookArtKeys.ButtonGold) : open ? Color.white : SurvivalVisuals.Mute;
                    if (!open)
                    {
                        sb.GetComponent<Image>().color = Disabled;
                    }

                    var target = s;
                    var sbRt = sb.GetComponent<RectTransform>();
                    Wire(sb, () =>
                    {
                        if (Campaign.IsUnlocked(target.Id))
                        {
                            OpenSetup(target);
                        }
                        else
                        {
                            _hook?.Say(sbRt, S("campaign.stage_locked", "Clear the stage before first"), Warn);
                        }
                    });
                }
            }

            // Bounty strip.
            var bounty = HookSkin.Panel(panel, "Bounty", HookArtKeys.Plate, SurvivalVisuals.RowFill, 0.45f);
            Box(bounty.rectTransform, 0.04f, 0.795f, 0.96f, 0.852f);
            var bountyText = HookSkin.Outlined(SurvivalVisuals.Text(bounty.transform, "T", "", 22, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.5f);
            Box(bountyText.rectTransform, 0.03f, 0f, 0.72f, 1f);
            var claimText = SheetButton(bounty.rectTransform, "Claim", HookArtKeys.ButtonGreen, FreeGreen, 0.74f, 0.1f, 0.99f, 0.9f, 20, out var claimBtn);
            claimText.text = S("hook.claim", "Claim");
            var claimRt = claimBtn.GetComponent<RectTransform>();
            Wire(claimBtn, () =>
            {
                var b = Campaign.CurrentBounty;
                if (b == null || !Campaign.BountyReady)
                {
                    return;
                }

                var rows = new Dictionary<string, int>();
                foreach (var pair in b.Rewards)
                {
                    rows[pair.Key] = pair.Value;
                }

                _hook?.Sparks(claimRt, FreeGreen, 14);
                if (_hook != null)
                {
                    _hook.Celebrate(S("campaign.bounty_done", "Bounty complete!"), rows, () => Campaign.ClaimBounty());
                }
                else
                {
                    Campaign.ClaimBounty();
                }

                SaveNow();
                _refresh?.Invoke();
            });

            // Lanes: villains on top, heroes below.
            var n = setup.LaneCount;
            var refreshers = new List<Action>();
            var vs = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Vs", "VS", 56, TextAnchor.MiddleCenter, SurvivalVisuals.Reward), 3f);
            vs.fontStyle = FontStyle.Bold;
            Box(vs.rectTransform, 0.3f, 0.528f, 0.7f, 0.578f);
            for (var lane = 0; lane < n; lane++)
            {
                var cx = 0.5f + (lane - ((n - 1) * 0.5f)) * 0.315f;
                var x0 = cx - 0.135f;
                var x1 = cx + 0.135f;
                var enemy = setup.EnemyAt(lane);
                var edef = enemy != null ? Campaign.Catalog.Hero(enemy.HeroId) : null;
                if (edef != null && enemy != null)
                {
                    Portrait(panel, edef, x0, 0.62f, x1, 0.62f + 0.27f * PanelAspect, out _);
                    var es = setup.EnemyStats(lane);
                    var el = HookSkin.Outlined(SurvivalVisuals.Text(panel, "EnemyLabel" + lane, HeroName(edef.Id) + "  Lv " + enemy.Level + "\n\u2694 " + ChipWallet.FormatGrouped(es.Power), 21, TextAnchor.UpperCenter, new Color(1f, 0.78f, 0.82f, 1f)), 1.5f);
                    Box(el.rectTransform, x0 - 0.02f, 0.578f, x1 + 0.02f, 0.62f);
                }

                var laneTag = HookSkin.Outlined(SurvivalVisuals.Text(panel, "LaneTag" + lane, Fmt("campaign.lane", "LANE {0}", lane + 1), 18, TextAnchor.MiddleCenter, SurvivalVisuals.Mute), 1.2f);
                Box(laneTag.rectTransform, x0, 0.515f, x1, 0.535f);
                refreshers.Add(HeroSlot(panel, lane, x0, x1));
            }

            // Power comparison.
            var mine = HookSkin.Outlined(SurvivalVisuals.Text(panel, "MyPower", "", 26, TextAnchor.MiddleLeft, HeroGlow), 1.5f);
            mine.fontStyle = FontStyle.Bold;
            Box(mine.rectTransform, 0.05f, 0.262f, 0.42f, 0.312f);
            var theirs = HookSkin.Outlined(SurvivalVisuals.Text(panel, "EnemyPower", "", 26, TextAnchor.MiddleRight, VillainGlow), 1.5f);
            theirs.fontStyle = FontStyle.Bold;
            Box(theirs.rectTransform, 0.58f, 0.262f, 0.95f, 0.312f);
            var odds = SurvivalVisuals.Image(panel, "Odds", FreeGreen);
            Box(odds.rectTransform, 0.40f, 0.266f, 0.60f, 0.308f);
            var oddsText = HookSkin.Outlined(SurvivalVisuals.Text(odds.transform, "T", "", 22, TextAnchor.MiddleCenter, Color.white), 1.5f);
            oddsText.fontStyle = FontStyle.Bold;
            SurvivalVisuals.Stretch(oddsText.rectTransform);
            var balance = SurvivalVisuals.Image(panel, "Balance", VillainFrame);
            Box(balance.rectTransform, 0.05f, 0.246f, 0.95f, 0.258f);
            var balanceFill = SurvivalVisuals.Image(balance.transform, "Fill", HeroGlow);

            // Hero pool.
            var poolLabel = HookSkin.Outlined(SurvivalVisuals.Text(panel, "PoolLabel", S("campaign.pool", "YOUR HEROES \u00b7 tap to place"), 20, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.2f);
            Box(poolLabel.rectTransform, 0.05f, 0.205f, 0.70f, 0.235f);
            var autoText = SheetButton(panel, "Auto", HookArtKeys.ButtonBlue, HeroFrame, 0.74f, 0.200f, 0.95f, 0.240f, 20, out var autoBtn);
            autoText.text = S("campaign.auto", "AUTO");
            Wire(autoBtn, () => setup.AutoFill());
            var owned = new List<string>(Campaign.Roster.OwnedIds());
            for (var i = 0; i < owned.Count && i < 6; i++)
            {
                var px = 0.04f + i * 0.155f;
                refreshers.Add(PoolCard(panel, owned[i], px, 0.10f, px + 0.145f, 0.10f + 0.145f * PanelAspect));
            }

            var close = SheetButton(panel, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.04f, 0.022f, 0.32f, 0.085f, 24, out var closeBtn);
            close.text = S("hook.close", "Close");
            Wire(closeBtn, CloseScreen);
            var fightText = SheetButton(panel, "Fight", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, 0.36f, 0.018f, 0.96f, 0.090f, 40, out var fightBtn);
            fightText.text = S("campaign.fight", "FIGHT!");
            var fightImg = fightBtn.GetComponent<Image>();
            var fightRt = fightBtn.GetComponent<RectTransform>();
            Wire(fightBtn, () =>
            {
                var check = setup.CanFight;
                if (check != SetupCheck.Ok)
                {
                    _hook?.Say(fightRt, check == SetupCheck.NoHeroes ? S("campaign.need_heroes", "Place at least one hero") : S("campaign.stage_locked", "Clear the stage before first"), Warn);
                    return;
                }

                StartCoroutine(RunBattle(setup.Build()));
            });

            _refresh = () =>
            {
                var b = Campaign.CurrentBounty;
                bountyText.text = b == null
                    ? S("campaign.bounty_all", "All bounties claimed!")
                    : Fmt("campaign.bounty", "BOUNTY: {0}  {1}/{2}", BountyName(b), Campaign.BountyProgress(b), b.Count);
                claimBtn.gameObject.SetActive(Campaign.BountyReady);
                mine.text = Fmt("campaign.my_power", "YOU \u2694 {0}", ChipWallet.FormatGrouped(setup.HeroPower));
                theirs.text = Fmt("campaign.enemy_power", "{0} \u2694 FOES", ChipWallet.FormatGrouped(setup.EnemyPower));
                var o = setup.Odds;
                oddsText.text = OddsName(o);
                odds.color = OddsColor(o);
                var total = Mathf.Max(1, setup.HeroPower + setup.EnemyPower);
                Box(balanceFill.rectTransform, 0f, 0f, setup.HeroPower / (float)total, 1f);
                var ok = setup.CanFight == SetupCheck.Ok;
                HookSkin.SetButton(fightImg, HookArtKeys.ButtonGold, ok ? SurvivalVisuals.MineFill : Disabled, ok, 0.5f);
                fightText.color = ok ? HookSkin.InkFor(HookArtKeys.ButtonGold) : SurvivalVisuals.Cream;
                foreach (var r in refreshers)
                {
                    r();
                }
            };
            setup.Changed += () =>
            {
                if (ReferenceEquals(_setup, setup))
                {
                    _refresh?.Invoke();
                }
            };
            _refresh();
        }

        /// <summary>One hero lane slot: the placed hero's framed portrait (tap to take it out) or an empty "+" slot.</summary>
        private Action HeroSlot(RectTransform panel, int lane, float x0, float x1)
        {
            var y0 = 0.345f;
            var y1 = y0 + 0.27f * PanelAspect;
            var btn = SurvivalVisuals.Button(panel, "Lane" + lane, new Color(1f, 1f, 1f, 0f));
            btn.transition = Selectable.Transition.None;
            var rt = btn.GetComponent<RectTransform>();
            Box(rt, x0, y0, x1, y1);
            var empty = SurvivalVisuals.Image(rt, "Empty", new Color(HeroFrame.r, HeroFrame.g, HeroFrame.b, 0.55f));
            SurvivalVisuals.Stretch(empty.rectTransform);
            var ring = SurvivalVisuals.Image(empty.transform, "Line", new Color(HeroGlow.r, HeroGlow.g, HeroGlow.b, 0.35f));
            SurvivalVisuals.Stretch(ring.rectTransform);
            ring.rectTransform.offsetMin = new Vector2(8f, 8f);
            ring.rectTransform.offsetMax = new Vector2(-8f, -8f);
            var plus = HookSkin.Outlined(SurvivalVisuals.Text(empty.transform, "T", "+\n" + S("campaign.slot_empty", "Tap a hero"), 26, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
            SurvivalVisuals.Stretch(plus.rectTransform);
            var label = HookSkin.Outlined(SurvivalVisuals.Text(panel, "HeroLabel" + lane, "", 21, TextAnchor.UpperCenter, new Color(0.78f, 0.88f, 1f, 1f)), 1.5f);
            Box(label.rectTransform, x0 - 0.02f, 0.30f, x1 + 0.02f, y0);
            RectTransform? shown = null;
            var shownId = "";
            Wire(btn, () =>
            {
                if (_setup != null && _setup.HeroAt(lane) != null)
                {
                    _setup.Clear(lane);
                }
            });

            return () =>
            {
                var id = _setup?.HeroAt(lane) ?? "";
                if (id != shownId)
                {
                    if (shown != null)
                    {
                        Destroy(shown.gameObject);
                        shown = null;
                    }

                    shownId = id;
                    var def = id.Length > 0 ? Campaign!.Catalog.Hero(id) : null;
                    if (def != null)
                    {
                        shown = Portrait(rt, def, 0f, 0f, 1f, 1f, out var art);
                        art.raycastTarget = false;
                        StartCoroutine(PopIn(shown));
                        _hook?.Sparks(rt, HeroGlow, 8);
                    }
                }

                empty.gameObject.SetActive(id.Length == 0);
                if (id.Length > 0)
                {
                    var p = Campaign!.Roster.Get(id)!;
                    label.text = HeroName(id) + "  Lv " + p.Level + "\n\u2694 " + ChipWallet.FormatGrouped(Campaign.Roster.Stats(id).Power);
                }
                else
                {
                    label.text = "";
                }
            };
        }

        private Action PoolCard(RectTransform panel, string heroId, float x0, float y0, float x1, float y1)
        {
            var def = Campaign!.Catalog.Hero(heroId)!;
            var btn = SurvivalVisuals.Button(panel, "Pool_" + heroId, new Color(1f, 1f, 1f, 0f));
            btn.transition = Selectable.Transition.None;
            var rt = btn.GetComponent<RectTransform>();
            Box(rt, x0, y0, x1, y1);
            Portrait(rt, def, 0f, 0f, 1f, 1f, out var art);
            var tag = SurvivalVisuals.Circle(rt, "Lane", SurvivalVisuals.MineFill);
            tag.rectTransform.anchorMin = tag.rectTransform.anchorMax = new Vector2(0.84f, 0.84f);
            tag.rectTransform.sizeDelta = new Vector2(44f, 44f);
            var tagText = SurvivalVisuals.Text(tag.transform, "T", "", 24, TextAnchor.MiddleCenter, HookSkin.InkFor(HookArtKeys.ButtonGold));
            tagText.fontStyle = FontStyle.Bold;
            SurvivalVisuals.Stretch(tagText.rectTransform);
            var lv = HookSkin.Outlined(SurvivalVisuals.Text(rt, "Lv", "", 18, TextAnchor.LowerCenter, SurvivalVisuals.Cream), 1.5f);
            Box(lv.rectTransform, 0f, 0.02f, 1f, 0.3f);
            Wire(btn, () => _setup?.Toggle(heroId));
            return () =>
            {
                var lane = _setup?.LaneOf(heroId) ?? -1;
                tag.gameObject.SetActive(lane >= 0);
                tagText.text = (lane + 1).ToString(CultureInfo.InvariantCulture);
                art.color = lane >= 0 ? new Color(0.55f, 0.55f, 0.6f, 1f) : Color.white;
                lv.text = "Lv " + (Campaign.Roster.Get(heroId)?.Level ?? 1);
            };
        }

        // ---------- the battle round trip ----------

        private IEnumerator RunBattle(BattleLineup lineup)
        {
            if (_inBattle || Campaign == null)
            {
                yield break;
            }

            if (!Application.CanStreamedLevelBeLoaded(BattleSceneName))
            {
                Debug.LogWarning("Battle: scene '" + BattleSceneName + "' is not in Build Settings.");
                if (_screen != null && _screen.transform is RectTransform screenRt)
                {
                    _hook?.Say(screenRt, S("campaign.no_scene", "BattleLocked scene missing from Build Settings"), Warn);
                }

                yield break;
            }

            _inBattle = true;
            _lastLineupHeroes.Clear();
            foreach (var h in lineup.Heroes)
            {
                _lastLineupHeroes.Add(h.CharacterId);
            }

            SaveNow();
            CloseSheet();
            CloseScreen();
            var stage = lineup.Stage;
            var playScene = gameObject.scene;
            _battleTrips++;

            // Root cause of "Display 1 No cameras rendering": Play's cameras went off before BattleLocked's camera
            // existed (the additive load spans several frames), and on the way back BattleLocked's camera was
            // unloaded before Play's came back on. The loading screen's floor camera lives outside both scenes and
            // renders the whole time; Play is only hidden once the screen covers the view, and the screen only
            // fades once the destination camera has drawn a frame.
            var into = BattleLoadingScreen.Show(BattleLoadingInfo.Into(lineup, S("battle.loading.into", "To battle!"), LoadingTip(stage.Id)), S("battle.loading.vs", "VS"));
            yield return into.WaitCovered();
            var hidden = HidePlay(playScene);
            var render = RenderState.Capture();
            BattleReport? report = null;
            void OnDone(BattleReport r) => report = r;
            BattleHandoff.Completed += OnDone;
            BattleHandoff.Begin(lineup);
            var load = SceneManager.LoadSceneAsync(BattleSceneName, LoadSceneMode.Additive);
            while (load != null && !load.isDone)
            {
                into.Progress(load.progress);
                yield return null;
            }

            into.Progress(1f);
            var battle = SceneManager.GetSceneByName(BattleSceneName);
            if (battle.IsValid() && battle.isLoaded)
            {
                SceneManager.SetActiveScene(battle);
                into.RevealOnFirstFrameOf(battle);
            }
            else
            {
                // Load failed: bring Play straight back under the screen.
                render.Restore();
                ShowPlay(hidden);
                into.RevealOnFirstFrameOf(playScene);
            }

            while (report == null && battle.IsValid() && battle.isLoaded)
            {
                yield return null;
            }

            BattleHandoff.Completed -= OnDone;
            var finished = report ?? BattleHandoff.Result;
            var back = BattleLoadingScreen.Show(BattleLoadingInfo.Back(ReturnHeadline(finished), stage.Name, LoadingTip(stage.Id)), S("battle.loading.vs", "VS"));
            yield return back.WaitCovered();
            if (playScene.IsValid())
            {
                SceneManager.SetActiveScene(playScene);
            }

            if (battle.IsValid() && battle.isLoaded)
            {
                var unload = SceneManager.UnloadSceneAsync(battle);
                while (unload != null && !unload.isDone)
                {
                    back.Progress(unload.progress);
                    yield return null;
                }
            }

            back.Progress(1f);
            render.Restore();
            ShowPlay(hidden);
            back.RevealOnFirstFrameOf(playScene);
            var taken = BattleHandoff.TakeResult();
            _inBattle = false;
            var result = report ?? taken ?? new BattleReport(stage.Id, stage.CampId, false, 0, 0, 0, false, true);
            ShowResult(result, stage);
            foreach (var (title, rows) in _deferred)
            {
                _hook?.Celebrate(title, rows);
            }

            _deferred.Clear();
        }

        private string LoadingTip(string stageId)
        {
            var tips = new List<string>();
            for (var i = 1; i <= LoadingTipCount; i++)
            {
                var t = S("battle.tip." + i, "");
                if (t.Length > 0)
                {
                    tips.Add(t);
                }
            }

            if (tips.Count == 0)
            {
                tips.Add("Level up heroes in the Heroes screen to raise their power.");
            }

            return BattleTransition.Tip(tips, BattleTransition.StableSeed(stageId) + _battleTrips);
        }

        private string ReturnHeadline(BattleReport? r) =>
            r == null ? S("battle.loading.back", "Back to the village")
            : r.Retreated ? S("battle.loading.retreat", "Retreat!")
            : r.Won ? S("battle.loading.victory", "Victory!")
            : S("battle.loading.defeat", "Defeat");

        /// <summary>Play stays loaded (village, world and timers keep running); its canvases, cameras and listeners switch off.</summary>
        private static List<Behaviour> HidePlay(Scene scene)
        {
            var hidden = new List<Behaviour>();
            if (!scene.IsValid())
            {
                return hidden;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var c in root.GetComponentsInChildren<Canvas>(false))
                {
                    if (c.enabled && c.isRootCanvas)
                    {
                        c.enabled = false;
                        hidden.Add(c);
                    }
                }

                foreach (var cam in root.GetComponentsInChildren<Camera>(false))
                {
                    if (cam.enabled)
                    {
                        cam.enabled = false;
                        hidden.Add(cam);
                    }
                }

                foreach (var ear in root.GetComponentsInChildren<AudioListener>(false))
                {
                    if (ear.enabled)
                    {
                        ear.enabled = false;
                        hidden.Add(ear);
                    }
                }
            }

            return hidden;
        }

        private static void ShowPlay(List<Behaviour> hidden)
        {
            foreach (var b in hidden)
            {
                if (b != null)
                {
                    b.enabled = true;
                }
            }
        }

        /// <summary>The battle sets ambient light and fog at boot; put Play's back afterwards.</summary>
        private sealed class RenderState
        {
            private UnityEngine.Rendering.AmbientMode _ambientMode;
            private Color _ambient;
            private bool _fog;
            private FogMode _fogMode;
            private Color _fogColor;
            private float _fogStart;
            private float _fogEnd;
            private float _fogDensity;
            private Material? _skybox;

            public static RenderState Capture() => new RenderState
            {
                _ambientMode = RenderSettings.ambientMode,
                _ambient = RenderSettings.ambientLight,
                _fog = RenderSettings.fog,
                _fogMode = RenderSettings.fogMode,
                _fogColor = RenderSettings.fogColor,
                _fogStart = RenderSettings.fogStartDistance,
                _fogEnd = RenderSettings.fogEndDistance,
                _fogDensity = RenderSettings.fogDensity,
                _skybox = RenderSettings.skybox
            };

            public void Restore()
            {
                RenderSettings.ambientMode = _ambientMode;
                RenderSettings.ambientLight = _ambient;
                RenderSettings.fog = _fog;
                RenderSettings.fogMode = _fogMode;
                RenderSettings.fogColor = _fogColor;
                RenderSettings.fogStartDistance = _fogStart;
                RenderSettings.fogEndDistance = _fogEnd;
                RenderSettings.fogDensity = _fogDensity;
                RenderSettings.skybox = _skybox;
            }
        }

        // ---------- result ----------

        private void ShowResult(BattleReport result, StageDef stage)
        {
            if (Campaign == null || _overlay == null)
            {
                return;
            }

            var firstClear = result.Won && stage.CampId == null && !Campaign.IsCleared(stage.Id);
            var preview = BattleRewards.For(stage, result.Won, firstClear, Campaign.Catalog.LossGoldShare).AsRows();
            _hook?.HoldChips(preview);
            var bundle = Campaign.ApplyResult(result, stage);
            var rows = bundle.AsRows();
            if (result.Won)
            {
                _session?.Hook?.Quests.Report("quest.objective.win_battle", stage.Id, 1);
            }

            SaveNow();

            var panel = OpenScreen(ResultScreenName, result.Won ? S("campaign.victory", "VICTORY!") : S("campaign.defeat", "DEFEAT"));
            var glow = HookSkin.Glow(panel, "Glow", result.Won ? new Color(1f, 0.85f, 0.4f, 0.5f) : new Color(0.6f, 0.3f, 0.3f, 0.35f));
            Box(glow.rectTransform, 0.1f, 0.62f, 0.9f, 0.95f);
            for (var i = 0; i < _lastLineupHeroes.Count; i++)
            {
                var def = Campaign.Catalog.Hero(_lastLineupHeroes[i]);
                if (def == null)
                {
                    continue;
                }

                var cx = 0.5f + (i - ((_lastLineupHeroes.Count - 1) * 0.5f)) * 0.27f;
                Portrait(panel, def, cx - 0.12f, 0.70f, cx + 0.12f, 0.70f + 0.24f * PanelAspect, out var art);
                if (!result.Won)
                {
                    art.color = new Color(0.6f, 0.6f, 0.65f, 1f);
                }
            }

            var title = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Stage", stage.Name, 34, TextAnchor.MiddleCenter, SurvivalVisuals.Reward), 2f);
            title.fontStyle = FontStyle.Bold;
            Box(title.rectTransform, 0.05f, 0.635f, 0.95f, 0.685f);
            var extra = (result.TimedOut ? "  \u00b7  " + S("campaign.timeout", "time up") : "") + (result.Retreated ? "  \u00b7  " + S("campaign.retreated", "retreated") : "");
            var summary = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Summary", Fmt("campaign.summary", "Foes defeated {0}  \u00b7  Heroes standing {1}  \u00b7  {2}", result.EnemiesDefeated, result.HeroesStanding, TimerQueue.FormatClock(result.Seconds)) + extra, 22, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
            Box(summary.rectTransform, 0.03f, 0.595f, 0.97f, 0.635f);
            if (bundle.FirstClear)
            {
                var first = SurvivalVisuals.Image(panel, "FirstClear", new Color(0.85f, 0.25f, 0.30f, 1f));
                HookSkin.TrySkin(first, HookArtKeys.RibbonRed, 0.5f);
                Box(first.rectTransform, 0.28f, 0.545f, 0.72f, 0.592f);
                var ft = HookSkin.Outlined(SurvivalVisuals.Text(first.transform, "T", S("campaign.first_clear", "FIRST CLEAR!"), 24, TextAnchor.MiddleCenter, Color.white), 1.5f);
                ft.fontStyle = FontStyle.Bold;
                Box(ft.rectTransform, 0.1f, 0.2f, 0.9f, 0.95f);
            }

            var y = 0.53f;
            foreach (var pair in rows)
            {
                var row = HookSkin.Panel(panel, "Reward_" + pair.Key, HookArtKeys.Chip, SurvivalVisuals.RowFill, 0.6f);
                Box(row.rectTransform, 0.22f, y - 0.05f, 0.78f, y);
                var icon = HookSkin.ResIcon(row.transform, "Icon", pair.Key, ShardPurple, "\u2726");
                Box(icon.rectTransform, 0.02f, -0.3f, 0.2f, 1.3f);
                var t = HookSkin.Outlined(SurvivalVisuals.Text(row.transform, "T", "+" + ChipWallet.FormatGrouped(pair.Value) + "  " + RowName(pair.Key), 26, TextAnchor.MiddleLeft, SurvivalVisuals.Reward), 1.5f);
                t.fontStyle = FontStyle.Bold;
                Box(t.rectTransform, 0.24f, 0f, 1f, 1f);
                y -= 0.058f;
            }

            if (rows.Count == 0)
            {
                var none = HookSkin.Outlined(SurvivalVisuals.Text(panel, "NoRewards", S("campaign.no_rewards", "No rewards this time"), 24, TextAnchor.MiddleCenter, SurvivalVisuals.Mute), 1.5f);
                Box(none.rectTransform, 0.1f, y - 0.05f, 0.9f, y);
                y -= 0.058f;
            }

            var b = Campaign.CurrentBounty;
            if (b != null)
            {
                var ready = Campaign.BountyReady;
                var bl = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Bounty", Fmt("campaign.bounty", "BOUNTY: {0}  {1}/{2}", BountyName(b), Campaign.BountyProgress(b), b.Count) + (ready ? "  \u2714 " + S("campaign.bounty_ready", "claim it in Battle") : ""), 22, TextAnchor.MiddleCenter, ready ? FreeGreen : SurvivalVisuals.Cream), 1.5f);
                Box(bl.rectTransform, 0.04f, y - 0.045f, 0.96f, y);
            }

            if (!result.Won)
            {
                var tip = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Tip", S("campaign.tip", "Train and star up your heroes, then try again!"), 22, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
                Box(tip.rectTransform, 0.04f, 0.19f, 0.96f, 0.23f);
            }

            var celebrateTitle = result.Won ? S("campaign.victory", "VICTORY!") : S("campaign.defeat", "DEFEAT");
            var collect = SheetButton(panel, "Collect", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, result.Won ? 0.25f : 0.52f, 0.06f, result.Won ? 0.75f : 0.96f, 0.15f, 34, out var collectBtn);
            collect.text = result.Won ? S("campaign.collect", "COLLECT") : S("campaign.continue", "CONTINUE");
            FitButtonLabel(collect, 34);
            Wire(collectBtn, () =>
            {
                CloseScreen();
                CelebrateOrRelease(celebrateTitle, rows);
            });
            if (!result.Won)
            {
                var up = SheetButton(panel, "Upgrade", HookArtKeys.ButtonBlue, HeroFrame, 0.04f, 0.06f, 0.48f, 0.15f, 28, out var upBtn);
                up.text = S("campaign.upgrade", "UPGRADE HEROES");
                FitButtonLabel(up, 28);
                Wire(upBtn, () =>
                {
                    CelebrateOrRelease(celebrateTitle, rows);
                    _foesTab = false;
                    OpenHeroes();
                });
            }

            RefreshBadges();
        }

        /// <summary>Rewards are already in the wallet (the chips were held); the celebration counts them up.</summary>
        private void CelebrateOrRelease(string title, Dictionary<string, int> rows)
        {
            if (_hook == null)
            {
                return;
            }

            if (rows.Count > 0)
            {
                _hook.Celebrate(title, rows);
            }
            else
            {
                _hook.ReleaseChips(rows);
            }
        }

        // ---------- widgets ----------

        /// <summary>
        /// Framed portrait: Sir Aldric blue (#1b3060) for heroes, purple for villains, a gold inner line and the painted
        /// portrait from the pack, or a code-drawn fallback (tinted plate with initials).
        /// </summary>
        private RectTransform Portrait(RectTransform parent, CampaignHeroDef def, float x0, float y0, float x1, float y1, out Image art)
        {
            var hero = def.Side == CampaignSide.Hero;
            var frame = SurvivalVisuals.Image(parent, "Portrait_" + def.Id, hero ? HeroFrame : VillainFrame);
            frame.raycastTarget = false;
            Box(frame.rectTransform, x0, y0, x1, y1);
            var trim = SurvivalVisuals.Image(frame.transform, "Trim", SurvivalVisuals.GoldLine);
            trim.raycastTarget = false;
            SurvivalVisuals.Stretch(trim.rectTransform);
            trim.rectTransform.offsetMin = new Vector2(6f, 6f);
            trim.rectTransform.offsetMax = new Vector2(-6f, -6f);
            var sprite = SurvivalArt.Get(def.PortraitKey);
            art = sprite != null
                ? SurvivalVisuals.Image(trim.transform, "Art", Color.white, sprite)
                : SurvivalVisuals.Image(trim.transform, "Art", hero ? new Color(0.22f, 0.36f, 0.66f, 1f) : new Color(0.36f, 0.18f, 0.48f, 1f));
            art.raycastTarget = false;
            SurvivalVisuals.Stretch(art.rectTransform);
            art.rectTransform.offsetMin = new Vector2(3f, 3f);
            art.rectTransform.offsetMax = new Vector2(-3f, -3f);
            if (sprite == null)
            {
                var initials = HookSkin.Outlined(SurvivalVisuals.Text(art.transform, "Initials", Initials(HeroName(def.Id)), 64, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 2f);
                initials.fontStyle = FontStyle.Bold;
                initials.raycastTarget = false;
                SurvivalVisuals.Stretch(initials.rectTransform);
            }

            return frame.rectTransform;
        }

        private static Text[] Stars(RectTransform parent, float x0, float y0, float x1, float y1)
        {
            var host = new GameObject("Stars", typeof(RectTransform)).GetComponent<RectTransform>();
            host.SetParent(parent, false);
            Box(host, x0, y0, x1, y1);
            var stars = new Text[5];
            for (var i = 0; i < 5; i++)
            {
                var t = HookSkin.Outlined(SurvivalVisuals.Text(host, "Star" + i, "\u2605", 30, TextAnchor.MiddleCenter, StarOff), 1.5f);
                t.raycastTarget = false;
                Box(t.rectTransform, i / 5f, 0f, (i + 1) / 5f, 1f);
                stars[i] = t;
            }

            return stars;
        }

        private static void SetStars(Text[] stars, int count)
        {
            for (var i = 0; i < stars.Length; i++)
            {
                stars[i].color = i < count ? StarOn : StarOff;
                stars[i].gameObject.SetActive(count > 0);
            }
        }

        /// <summary>
        /// Shrink a sheet button's label to fit inside the painted rim (best fit, never above its design size), so long
        /// labels like "UPGRADE HEROES" don't spill past a half-width button on narrow phones.
        /// </summary>
        private static void FitButtonLabel(Text label, int size)
        {
            HookSkin.Fit(label, Mathf.Max(12, Mathf.RoundToInt(size * 0.5f)), size);
            HookSkin.PadLabel(label.rectTransform, 0.07f, 0.12f);
        }

        private static Text SheetButton(RectTransform parent, string name, string key, Color fallback, float x0, float y0, float x1, float y1, int size, out Button button)
        {
            button = HookSkin.Button(parent, name, key, fallback, 0.5f);
            Box(button.GetComponent<RectTransform>(), x0, y0, x1, y1);
            var label = HookSkin.Outlined(SurvivalVisuals.Text(button.transform, "T", "", size, TextAnchor.MiddleCenter, HookSkin.InkFor(key)), 1.5f);
            label.fontStyle = FontStyle.Bold;
            label.raycastTarget = false;
            SurvivalVisuals.Stretch(label.rectTransform);
            return label;
        }

        private void Wire(Button button, Action onTap)
        {
            button.onClick.AddListener(() =>
            {
                if (button.transform is RectTransform pressed)
                {
                    Juice(pressed);
                }

                onTap();
            });
        }

        private void Juice(RectTransform rt)
        {
            if (_hook != null)
            {
                _hook.Juice(rt);
            }
        }

        private static IEnumerator PopIn(RectTransform rt)
        {
            const int frames = 12;
            for (var i = 1; i <= frames; i++)
            {
                if (rt == null)
                {
                    yield break;
                }

                var u = i / (float)frames;
                var k = u < 0.7f ? Mathf.Lerp(0.6f, 1.06f, u / 0.7f) : Mathf.Lerp(1.06f, 1f, (u - 0.7f) / 0.3f);
                rt.localScale = new Vector3(k, k, 1f);
                yield return null;
            }
        }

        private static Transform? FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(t.name, name, StringComparison.Ordinal))
                {
                    return t;
                }
            }

            return null;
        }

        private static void Box(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Color Rgb(IReadOnlyList<float> rgb) => new Color(rgb[0], rgb[1], rgb[2], 1f);

        private static string Initials(string name)
        {
            var parts = name.Split(' ');
            return parts.Length > 1 ? parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1) : name.Substring(0, Math.Min(2, name.Length));
        }

        private static string Title(string s) => string.IsNullOrEmpty(s) ? "" : char.ToUpperInvariant(s[0]) + s.Substring(1);

        private static Color RarityColor(string rarity) => rarity switch
        {
            "legendary" => new Color(1f, 0.72f, 0.28f, 1f),
            "epic" => new Color(0.80f, 0.58f, 1f, 1f),
            _ => new Color(0.55f, 0.78f, 1f, 1f)
        };

        private string OddsName(BattleOdds o) => o switch
        {
            BattleOdds.Easy => S("campaign.odds.easy", "EASY"),
            BattleOdds.Even => S("campaign.odds.even", "EVEN"),
            BattleOdds.Hard => S("campaign.odds.hard", "HARD"),
            _ => S("campaign.odds.very_hard", "VERY HARD")
        };

        private static Color OddsColor(BattleOdds o) => o switch
        {
            BattleOdds.Easy => new Color(0.30f, 0.78f, 0.38f, 1f),
            BattleOdds.Even => new Color(0.86f, 0.68f, 0.22f, 1f),
            BattleOdds.Hard => new Color(0.90f, 0.42f, 0.22f, 1f),
            _ => new Color(0.80f, 0.20f, 0.24f, 1f)
        };

        private string BountyName(BountyDef b) => b.Kind switch
        {
            "win" => Fmt("campaign.bounty.win", "Win {0} battles", b.Count),
            "clear" => Fmt("campaign.bounty.clear", "Clear {0}", Campaign?.Catalog.Stage(b.Target)?.Name ?? b.Target),
            "hero_level" => Fmt("campaign.bounty.level", "Train a hero to Lv {0}", b.Count),
            _ => b.Id
        };

        private string RowName(string key) =>
            key.StartsWith("shard.", StringComparison.Ordinal)
                ? Fmt("campaign.shards_of", "{0} shards", HeroName(key.Substring(6)))
                : S(key + ".name", key);

        private string HeroName(string id)
        {
            var def = Campaign?.Catalog.Hero(id);
            return S(id + ".name", def != null ? def.Fighter : id);
        }

        private string S(string key, string fallback) => _session != null ? _session.Pack.StringOr(key, fallback) : fallback;

        private string Fmt(string key, string fallback, params object[] args) => string.Format(CultureInfo.InvariantCulture, S(key, fallback), args);
    }
}
