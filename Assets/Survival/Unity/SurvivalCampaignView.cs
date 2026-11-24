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
using Survival.Unity.Kids;
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
    public sealed partial class SurvivalCampaignView : MonoBehaviour
    {
        public const string BattleSceneName = "BattleLocked";
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
        internal const float PanelAspect = 1037f / 1766f;

        private static readonly Color Dim = new Color(0.02f, 0.03f, 0.05f, 0.82f);
        internal static readonly Color HeroFrame = Rgb(CampaignArtKeys.HeroFrameRgb);
        internal static readonly Color VillainFrame = Rgb(CampaignArtKeys.VillainFrameRgb);
        internal static readonly Color HeroGlow = new Color(0.40f, 0.62f, 1f, 1f);
        internal static readonly Color VillainGlow = new Color(0.72f, 0.42f, 1f, 1f);
        internal static readonly Color Disabled = new Color(0.30f, 0.31f, 0.34f, 1f);
        internal static readonly Color FreeGreen = new Color(0.30f, 0.82f, 0.38f, 1f);
        private static readonly Color ShortRed = new Color(1f, 0.30f, 0.26f, 1f);
        internal static readonly Color Warn = new Color(1f, 0.55f, 0.45f, 1f);
        private static readonly Color StarOn = new Color(1f, 0.82f, 0.25f, 1f);
        private static readonly Color StarOff = new Color(0.35f, 0.33f, 0.30f, 0.9f);
        internal static readonly Color ShardPurple = new Color(0.72f, 0.52f, 1f, 1f);

        private SurvivalPlayView? _play;
        internal SurvivalSession? _session;
        internal SurvivalHookView? _hook;
        internal RectTransform? _overlay;
        internal GameObject? _screen;
        private GameObject? _sheet;
        internal Action? _refresh;
        private Action? _sheetRefresh;
        private Image? _battleBadge;
        private Image? _heroesBadge;
        private RectTransform? _heroFree;
        private Text? _heroFreeCount;
        private Image? _heroFreeCountDot;
        private bool _bulkFree;
        private float _tick;
        private float _saveIn = -1f;
        internal bool _inBattle;
        internal bool _foesTab;
        private string _selected = "";
        internal BattleSetup? _setup;
        internal readonly List<(string Title, Dictionary<string, int> Rows)> _deferred = new List<(string, Dictionary<string, int>)>();

        public CampaignSession? Campaign { get; private set; }

        /// <summary>The power-up ladder (power_packs.json + store.power_packs); null when the file or the hook store is missing.</summary>
        public PowerPackShop? PowerPacks { get; private set; }

        /// <summary>The Kingdom Path reward track (reward_track.json + store.reward_track); null when the file or the hook store is missing.</summary>
        public RewardTrack? Track { get; private set; }

        private RewardTrackScreen? _rewardTrack;

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
            Campaign.CastleLevel = () => _session?.Hook?.Base.HqLevel ?? 1;
            Campaign.Changed += MarkDirty;
            session.Chips.Wallet.Changed += (_, __, ___) => MarkDirty();
            Campaign.Roster.LevelledUp += OnLevelledUp;
            Campaign.Roster.LevelledUp += (_, __) => Survival.Domain.Taps.ActivityFeed.Report(Survival.Domain.Taps.ActivityKinds.HeroLevel, 1);
            Campaign.Roster.Unlocked += OnUnlocked;
            Campaign.Roster.StarredUp += OnStarredUp;
            Campaign.Tick();

            _overlay = SurvivalVisuals.Canvas(transform, "SurvivalCampaignOverlay", 55);
            WireHeroesEntry(play, play.Root);
            WireAllianceBag(play);
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
                _heroesBadge = UpgradeBadge(nav, "CampaignBadge");
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
                HookSkin.Box(rt, 0.80f, 1f - 0.438f, 0.975f, 1f - 0.338f);
            }

            btn.transition = Selectable.Transition.None;
            var glow = HookSkin.Glow(btn.transform, "Glow", new Color(1f, 0.78f, 0.35f, 0.55f));
            HookSkin.Box(glow.rectTransform, -0.2f, -0.1f, 1.2f, 1.3f);
            var icon = SurvivalArt.Get(CampaignArtKeys.IconBattle);
            if (icon != null)
            {
                var art = SurvivalVisuals.Image(btn.transform, "Icon", Color.white, icon);
                art.preserveAspect = true;
                HookSkin.Box(art.rectTransform, 0.06f, 0.24f, 0.94f, 1.02f);
            }
            else
            {
                // Code-drawn fallback: a hero-blue disc with a crossed-swords glyph.
                var disc = SurvivalVisuals.Circle(btn.transform, "Icon", HeroFrame);
                HookSkin.Box(disc.rectTransform, 0.1f, 0.26f, 0.9f, 1f);
                var g = SurvivalVisuals.Text(disc.transform, "G", "\u2694", 64, TextAnchor.MiddleCenter, SurvivalVisuals.Gold);
                SurvivalVisuals.Stretch(g.rectTransform);
            }

            var plate = SurvivalVisuals.Image(btn.transform, "Label", new Color(0.55f, 0.12f, 0.12f, 1f));
            HookSkin.TrySkin(plate, HookArtKeys.RibbonRed, 0.5f);
            HookSkin.Box(plate.rectTransform, -0.12f, -0.02f, 1.12f, 0.30f);
            var label = HookSkin.Outlined(SurvivalVisuals.Text(plate.transform, "T", S("campaign.battle", "BATTLE"), 26, TextAnchor.MiddleCenter, Color.white), 2f);
            label.fontStyle = FontStyle.Bold;
            HookSkin.Box(label.rectTransform, 0f, 0.2f, 1f, 0.95f);
            _battleBadge = Badge(btn.transform, "Badge");
            btn.onClick.AddListener(() =>
            {
                Juice(rt);
                OnBattleTapped();
            });
            StartCoroutine(Breathe(rt));
        }

        internal static IEnumerator Breathe(RectTransform rt)
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

        /// <summary>
        /// Hero "upgrade ready" marker (Derek 2026-10-31: the red "!" dot read as an error): the painted gold-chevron
        /// shield on the card's top-right corner, gently bobbing and pulsing. Falls back to the red dot if the art is missing.
        /// </summary>
        private Image UpgradeBadge(Transform parent, string name)
        {
            var sprite = SurvivalArt.Get(HookArtKeys.BadgeUpgrade);
            if (sprite == null)
            {
                return Badge(parent, name);
            }

            var badge = SurvivalVisuals.Image(parent, name, Color.white, sprite);
            badge.preserveAspect = true;
            badge.raycastTarget = false;
            var brt = badge.rectTransform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.88f, 0.80f);
            brt.sizeDelta = new Vector2(44f, 44f);
            badge.gameObject.SetActive(false);
            StartCoroutine(Bob(brt));
            return badge;
        }

        private static IEnumerator Bob(RectTransform rt)
        {
            var t = 0f;
            while (rt != null)
            {
                if (rt.gameObject.activeInHierarchy)
                {
                    t += Time.unscaledDeltaTime;
                    var k = 1f + 0.07f * Mathf.Sin(t * 4f);
                    rt.localScale = new Vector3(k, k, 1f);
                    rt.anchoredPosition = new Vector2(0f, 3f * Mathf.Sin(t * 4f));
                }

                yield return null;
            }
        }

        internal void RefreshBadges()
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
                _heroesBadge.gameObject.SetActive(AnyHeroAction());
            }

            if (_heroFree != null)
            {
                var n = Campaign.Roster.FreeFinishable().Count;
                // Hidden behind any open campaign screen (the Heroes panel shows the countdown itself).
                var show = HeroRoster.ShowFreeBadge(n, _inBattle, _screen != null);
                if (_heroFree.gameObject.activeSelf != show)
                {
                    _heroFree.gameObject.SetActive(show);
                    if (show)
                    {
                        StartCoroutine(HookSkin.PopIn(_heroFree));
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
                HookSkin.Box(rt, 0.80f, 1f - 0.538f, 0.975f, 1f - 0.448f);
            }

            var glow = HookSkin.Glow(rt, "Glow", new Color(HeroGlow.r, HeroGlow.g, HeroGlow.b, 0.8f));
            glow.raycastTarget = false;
            HookSkin.Box(glow.rectTransform, -0.25f, -0.2f, 1.25f, 1.25f);
            var disc = SurvivalVisuals.Circle(rt, "Disc", HeroFrame);
            disc.raycastTarget = false;
            HookSkin.Box(disc.rectTransform, 0.08f, 0.18f, 0.92f, 1.02f);
            var ring = SurvivalVisuals.Ring(disc.transform, "Ring", SurvivalVisuals.GoldLine);
            ring.raycastTarget = false;
            SurvivalVisuals.Stretch(ring.rectTransform);
            var helm = HookSkin.Art(rt, "Helm", BottomNav.IconHeroes, SurvivalVisuals.Cream);
            helm.preserveAspect = true;
            helm.raycastTarget = false;
            HookSkin.Box(helm.rectTransform, 0.12f, 0.22f, 0.88f, 1.06f);
            var ribbon = SurvivalArt.Get(HookArtKeys.BadgeFree);
            var free = ribbon != null ? SurvivalVisuals.Image(rt, "Free", Color.white, ribbon) : SurvivalVisuals.Image(rt, "Free", FreeGreen);
            free.preserveAspect = true;
            free.raycastTarget = false;
            HookSkin.Box(free.rectTransform, 0.02f, -0.10f, 0.98f, 0.36f);
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

        internal void Celebrate(string title, Dictionary<string, int> rows)
        {
            if (_inBattle)
            {
                _deferred.Add((title, rows));
                return;
            }

            _hook?.Celebrate(title, rows);
        }

        // ---------- screens ----------

        internal RectTransform OpenScreen(string name, string title)
        {
            CloseSheet();
            CloseScreen();
            var dim = SurvivalVisuals.Button(_overlay!, name, Dim);
            dim.transition = Selectable.Transition.None;
            SurvivalVisuals.Stretch(dim.GetComponent<RectTransform>());
            _screen = dim.gameObject;
            RefreshBadges(); // hide the village FREE hero badge behind the screen right away
            var panel = HookSkin.Panel(dim.transform, "Panel", HookArtKeys.Panel, SurvivalVisuals.InspectFill, 1f);
            panel.raycastTarget = true;
            HookSkin.Box(panel.rectTransform, 0.02f, 0.035f, 0.98f, 0.955f);
            HookSkin.Header(panel.rectTransform, title, 700f, 118f, 40);
            StartCoroutine(HookSkin.PopIn(panel.rectTransform));
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

        internal RectTransform OpenSheet(string name, string title, float height)
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
            StartCoroutine(HookSkin.PopIn(panel.rectTransform));
            return panel.rectTransform;
        }

        internal void CloseSheet()
        {
            _sheetRefresh = null;
            if (_sheet != null)
            {
                Destroy(_sheet);
                _sheet = null;
            }
        }

        // ---------- Heroes ----------

        // Heroes screen pieces kept while it is open, so taps only swap what changed (Derek 2026-11-02: tapping a hero
        // rebuilt the whole screen). Shell = panel, banner, balances, bottom buttons; body = tabs + cards + detail.
        private Text? _heroesTitle;
        private RectTransform? _heroesBody;
        private RectTransform? _heroesDetail;
        private RectTransform? _heroesDetailContent;
        private bool _heroesShownFoes;
        private readonly Dictionary<string, GameObject> _heroesGlows = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private readonly List<Action> _heroesBodyRefreshers = new List<Action>();
        private Action? _heroesDetailRefresh;

        public void OpenHeroes()
        {
            if (Campaign == null || _overlay == null)
            {
                return;
            }

            var defs = new List<CampaignHeroDef>(Campaign.Catalog.OnSide(_foesTab ? CampaignSide.Villain : CampaignSide.Hero));
            var sel = _selected.Length > 0 ? Campaign.Catalog.Hero(_selected) : null;
            if (sel == null || (sel.Side == CampaignSide.Villain) != _foesTab)
            {
                _selected = defs.Count > 0 ? defs[0].Id : "";
            }

            var open = _screen != null && _screen.name == HeroesScreenName && _heroesBody != null;
            var step = HeroesScreen.Plan(open, _heroesShownFoes, _foesTab);
            if (step == HeroesRebuild.Shell)
            {
                BuildHeroesShell();
            }

            if (step != HeroesRebuild.DetailOnly)
            {
                BuildHeroesBody(defs);
            }
            else
            {
                ShowHeroDetail();
            }

            _refresh?.Invoke();
        }

        /// <summary>Tap on a roster card: move the glow and swap the detail pane; nothing else is rebuilt.</summary>
        private void SelectHero(string id)
        {
            _selected = id;
            ShowHeroDetail();
            _refresh?.Invoke();
        }

        private void BuildHeroesShell()
        {
            var panel = OpenScreen(HeroesScreenName, S(HeroesScreen.TitleKey(_foesTab), HeroesScreen.TitleFallback(_foesTab)));
            _heroesTitle = panel.Find("Header")?.GetComponentInChildren<Text>(true);
            var roster = Campaign!.Roster;

            // What you have, right where you spend it: gold + food (Level Up), gems / speed-ups (Rush), tomes.
            BalanceBar.Inside(panel, Campaign.Wallet, BalanceBarIds.Heroes, 0.035f, 0.893f, 0.965f, 0.940f);
            var queue = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Queue", "", 22, TextAnchor.MiddleRight, SurvivalVisuals.Cream), 1.5f);
            HookSkin.Box(queue.rectTransform, 0.54f, 0.845f, 0.96f, 0.885f);

            var body = new GameObject("HeroesBody", typeof(RectTransform)).GetComponent<RectTransform>();
            body.SetParent(panel, false);
            SurvivalVisuals.Stretch(body);
            _heroesBody = body;

            var detail = SurvivalVisuals.Image(panel, "Detail", new Color(0f, 0f, 0f, 0.28f));
            HookSkin.TrySkin(detail, HookArtKeys.Plate, 0.45f);
            HookSkin.Box(detail.rectTransform, 0.025f, 0.095f, 0.975f, 0.43f);
            Ornaments.Grain(detail.transform, new Color(1f, 1f, 1f, 0.8f));
            Ornaments.Bevel(detail.transform);
            Ornaments.Frame(detail.transform, Ornaments.Gold, 3f);
            _heroesDetail = detail.rectTransform;
            _heroesDetailContent = null;

            var closeX0 = 0.30f;
            var closeX1 = 0.70f;
            if (PowerPacks != null)
            {
                closeX0 = 0.52f;
                closeX1 = 0.92f;
                var powerX1 = 0.48f;
                var power = SheetButton(panel, "PowerUp", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, 0.03f, 0.025f, powerX1, 0.082f, 24, out var powerBtn);
                power.text = S("store.power_packs.button", "POWER UP");
                FitButtonLabel(power, 26);
                Wire(powerBtn, OpenPowerPacks);
            }

            var close = SheetButton(panel, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, closeX0, 0.025f, closeX1, 0.082f, 26, out var closeBtn);
            close.text = S("hook.close", "Close");
            FitButtonLabel(close, 26);
            Wire(closeBtn, CloseScreen);

            _refresh = () =>
            {
                if (_heroesBody == null)
                {
                    return;
                }

                queue.text = Fmt("campaign.training_slots", "Training {0}/{1}", roster.Training.Count, Campaign.Catalog.TrainingSlots);
                foreach (var r in _heroesBodyRefreshers)
                {
                    r();
                }

                _heroesDetailRefresh?.Invoke();
            };
        }

        /// <summary>Tabs + roster cards for the shown side; on a tab switch only this and the detail pane are redrawn.</summary>
        private void BuildHeroesBody(List<CampaignHeroDef> defs)
        {
            var body = _heroesBody!;
            for (var c = body.childCount - 1; c >= 0; c--)
            {
                Destroy(body.GetChild(c).gameObject);
            }

            _heroesGlows.Clear();
            _heroesBodyRefreshers.Clear();
            _heroesShownFoes = _foesTab;
            if (_heroesTitle != null)
            {
                // The banner follows the tab: same art and font, only the words change.
                _heroesTitle.text = S(HeroesScreen.TitleKey(_foesTab), HeroesScreen.TitleFallback(_foesTab));
            }

            Tab(body, "TabHeroes", S("campaign.tab.heroes", "Heroes"), HeroesScreen.TabHighlighted(false, _foesTab), 0.04f, 0.27f, () => { if (_foesTab) { _foesTab = false; _selected = ""; OpenHeroes(); } });
            Tab(body, "TabFoes", S("campaign.tab.foes", "Foes"), HeroesScreen.TabHighlighted(true, _foesTab), 0.29f, 0.52f, () => { if (!_foesTab) { _foesTab = true; _selected = ""; OpenHeroes(); } });

            for (var i = 0; i < defs.Count && i < 6; i++)
            {
                var col = i % 3;
                var row = i / 3;
                var x0 = 0.035f + col * 0.315f;
                var y1 = 0.837f - row * 0.204f;
                _heroesBodyRefreshers.Add(HeroCard(body, defs[i], x0, y1 - 0.195f, x0 + 0.30f, y1));
            }

            ShowHeroDetail();
        }

        /// <summary>Moves the selection glow and redraws only the detail pane's contents (its plate and frame stay).</summary>
        private void ShowHeroDetail()
        {
            foreach (var kv in _heroesGlows)
            {
                if (kv.Value != null)
                {
                    kv.Value.SetActive(string.Equals(kv.Key, _selected, StringComparison.Ordinal));
                }
            }

            if (_heroesDetail == null)
            {
                return;
            }

            if (_heroesDetailContent != null)
            {
                Destroy(_heroesDetailContent.gameObject);
            }

            var content = new GameObject("DetailContent", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(_heroesDetail, false);
            SurvivalVisuals.Stretch(content);
            _heroesDetailContent = content;
            _heroesDetailRefresh = null;
            var def = Campaign!.Catalog.Hero(_selected);
            if (def != null)
            {
                _heroesDetailRefresh = def.Side == CampaignSide.Hero ? HeroDetail(content, def) : FoeDetail(content, def);
            }
        }

        private void Tab(RectTransform panel, string name, string label, bool on, float x0, float x1, Action tap)
        {
            var key = on ? HookArtKeys.ButtonGold : HookArtKeys.ButtonBlue;
            var text = SheetButton(panel, name, key, on ? SurvivalVisuals.MineFill : SurvivalVisuals.PanelSoft, x0, 0.845f, x1, 0.887f, 24, out var btn);
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
            HookSkin.Box(rt, x0, y0, x1, y1);
            {
                // Built for every card, shown only on the selected one, so a tap just moves it.
                var glow = HookSkin.Glow(rt, "Glow", hero ? new Color(0.5f, 0.7f, 1f, 0.7f) : new Color(0.75f, 0.45f, 1f, 0.7f));
                HookSkin.Box(glow.rectTransform, -0.08f, -0.06f, 1.08f, 1.06f);
                glow.gameObject.SetActive(string.Equals(def.Id, _selected, StringComparison.Ordinal));
                _heroesGlows[def.Id] = glow.gameObject;
            }

            var frame = Portrait(rt, def, 0f, 0.22f, 1f, 1f, out var art);
            var plate = SurvivalVisuals.Image(rt, "Plate", hero ? HeroFrame : VillainFrame);
            HookSkin.Box(plate.rectTransform, 0f, 0f, 1f, 0.24f);
            var name = HookSkin.Outlined(SurvivalVisuals.Text(plate.transform, "Name", HeroName(def.Id), 24, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
            name.fontStyle = FontStyle.Bold;
            HookSkin.Box(name.rectTransform, 0.02f, 0.40f, 0.98f, 0.98f);
            var sub = HookSkin.Outlined(SurvivalVisuals.Text(plate.transform, "Sub", "", 19, TextAnchor.MiddleCenter, SurvivalVisuals.Reward), 1.2f);
            HookSkin.Box(sub.rectTransform, 0.02f, 0.02f, 0.98f, 0.46f);
            var stars = Stars(frame, 0.08f, 0.02f, 0.92f, 0.16f);
            var lockTag = SurvivalVisuals.Image(frame, "Lock", new Color(0f, 0f, 0f, 0.62f));
            HookSkin.Box(lockTag.rectTransform, 0f, 0.36f, 1f, 0.62f);
            var lockText = HookSkin.Outlined(SurvivalVisuals.Text(lockTag.transform, "T", "", 22, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
            lockText.fontStyle = FontStyle.Bold;
            SurvivalVisuals.Stretch(lockText.rectTransform);
            var barFrame = HookSkin.Bar(frame, "Training", out var barFill);
            HookSkin.Box(barFrame.rectTransform, 0.06f, 0.19f, 0.94f, 0.29f);
            var dot = UpgradeBadge(rt, "Badge");
            Wire(btn, () =>
            {
                SelectHero(def.Id);
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
            HookSkin.Box(name.rectTransform, 0.37f, 0.86f, 0.98f, 0.98f);
            var role = HookSkin.Outlined(SurvivalVisuals.Text(d, "Role", Title(def.Rarity) + "  \u00b7  " + Title(def.Role), 22, TextAnchor.MiddleLeft, RarityColor(def.Rarity)), 1.5f);
            HookSkin.Box(role.rectTransform, 0.37f, 0.78f, 0.74f, 0.86f);
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
            // Two rows: LEVEL UP on top, cost + time underneath, each best-fit inside the rim.
            HookSkin.Fit(levelText, 16, 28);
            HookSkin.PadLabel(levelText.rectTransform, 0.05f, 0.0f);
            levelText.rectTransform.anchorMin = new Vector2(0.05f, 0.50f);
            levelText.rectTransform.anchorMax = new Vector2(0.95f, 0.92f);
            // Cost row: gold coin, wheat and hourglass icons, each with its amount.
            var levelCost = new GameObject("Cost", typeof(RectTransform)).GetComponent<RectTransform>();
            levelCost.SetParent(levelBtn.transform, false);
            HookSkin.Box(levelCost, 0.06f, 0.08f, 0.94f, 0.50f);
            var costIds = new[] { SurvIds.ResSoft, SurvIds.ResFood, SurvIds.ResSpeedupM };
            var costTexts = new Text[3];
            for (var ci = 0; ci < 3; ci++)
            {
                var cx = ci / 3f;
                var cIcon = HookSkin.ResIcon(levelCost, "Icon" + ci, costIds[ci], SurvivalVisuals.Reward, "\u2726");
                cIcon.raycastTarget = false;
                HookSkin.Box(cIcon.rectTransform, cx + 0.01f, 0.02f, cx + 0.09f, 0.98f);
                var ct = HookSkin.Outlined(SurvivalVisuals.Text(levelCost, "Amount" + ci, "", 18, TextAnchor.MiddleLeft, HookSkin.InkFor(HookArtKeys.ButtonGold)), 1.2f);
                ct.fontStyle = FontStyle.Bold;
                ct.raycastTarget = false;
                HookSkin.Fit(ct, 10, 18);
                HookSkin.Box(ct.rectTransform, cx + 0.10f, 0f, cx + 0.33f, 1f);
                costTexts[ci] = ct;
            }
            Ornaments.Shine(levelBtn.GetComponent<RectTransform>(), 3.4f);
            var levelImg = levelBtn.GetComponent<Image>();
            var levelRt = levelBtn.GetComponent<RectTransform>();
            var trainFrame = HookSkin.Bar(d, "TrainBar", out var trainFill);
            HookSkin.Box(trainFrame.rectTransform, 0.37f, 0.31f, 0.66f, 0.42f);
            var trainText = HookSkin.Outlined(SurvivalVisuals.Text(trainFrame.transform, "T", "", 20, TextAnchor.MiddleCenter, Color.white), 1.5f);
            SurvivalVisuals.Stretch(trainText.rectTransform);
            var rushText = SheetButton(d, "Rush", HookArtKeys.ButtonGreen, FreeGreen, 0.68f, 0.27f, 0.975f, 0.46f, 22, out var rushBtn);
            FitButtonLabel(rushText, 22);
            HookSkin.Fit(trainText, 12, 20);
            var rushImg = rushBtn.GetComponent<Image>();
            var rushRt = rushBtn.GetComponent<RectTransform>();

            // Star row: shard bar + Star Up (owned) / Recruit (locked).
            var shardIcon = HookSkin.ResIcon(d, "ShardIcon", RewardBundle.ShardKey(def.Id), ShardPurple, "\u2726");
            HookSkin.Box(shardIcon.rectTransform, 0.36f, 0.05f, 0.43f, 0.22f);
            var shardFrame = HookSkin.Bar(d, "ShardBar", out var shardFill);
            HookSkin.Box(shardFrame.rectTransform, 0.44f, 0.08f, 0.66f, 0.19f);
            var shardText = HookSkin.Outlined(SurvivalVisuals.Text(shardFrame.transform, "T", "", 20, TextAnchor.MiddleCenter, Color.white), 1.5f);
            SurvivalVisuals.Stretch(shardText.rectTransform);
            var starText = SheetButton(d, "StarUp", HookArtKeys.ButtonBlue, HeroFrame, 0.68f, 0.04f, 0.975f, 0.23f, 24, out var starBtn);
            FitButtonLabel(starText, 24);
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
                    var blocked = check == LevelUpCheck.AtStarCap || check == LevelUpCheck.MaxLevel || check == LevelUpCheck.NoSlot;
                    levelText.text = blocked ? LevelReason(check) : S("campaign.level_up_title", "LEVEL UP");
                    levelText.rectTransform.anchorMin = new Vector2(0.05f, blocked ? 0.12f : 0.50f);
                    levelCost.gameObject.SetActive(!blocked);
                    costTexts[0].text = ChipWallet.FormatGrouped(Cost(cost, SurvIds.ResSoft));
                    costTexts[1].text = ChipWallet.FormatGrouped(Cost(cost, SurvIds.ResFood));
                    costTexts[2].text = TimerQueue.FormatClock(roster.LevelUpSeconds(def.Id));
                    HookSkin.SetButton(levelImg, HookArtKeys.ButtonGold, ok ? SurvivalVisuals.MineFill : Disabled, ok, 0.5f);
                    levelText.color = ok ? HookSkin.InkFor(HookArtKeys.ButtonGold) : SurvivalVisuals.Cream;
                    foreach (var ct in costTexts)
                    {
                        ct.color = levelText.color;
                    }

                    // The resource you're short on goes red, so it's clear what's missing.
                    if (!blocked)
                    {
                        for (var ci = 0; ci < 2; ci++)
                        {
                            if (!wallet.CanAfford(costIds[ci], Cost(cost, costIds[ci])))
                            {
                                costTexts[ci].color = ShortRed;
                            }
                        }
                    }
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
            HookSkin.Box(name.rectTransform, 0.37f, 0.86f, 0.98f, 0.98f);
            var role = HookSkin.Outlined(SurvivalVisuals.Text(d, "Role", Title(def.Rarity) + "  \u00b7  " + Title(def.Role), 22, TextAnchor.MiddleLeft, VillainGlow), 1.5f);
            HookSkin.Box(role.rectTransform, 0.37f, 0.78f, 0.98f, 0.86f);
            var st = HeroStats.Of(def, Campaign!.Catalog.Curve, 1, 1);
            SetStat(StatLine(d, "Hp", 0.69f), S("campaign.stat.hp", "HP"), st.Hp, 0);
            SetStat(StatLine(d, "Atk", 0.60f), S("campaign.stat.atk", "ATK"), st.Atk, 0);
            SetStat(StatLine(d, "Power", 0.51f), S("campaign.stat.power", "POWER"), st.Power, 0);
            // Prologue stages by name, campaign chapters once each (126 stages would not fit).
            var seen = new List<string>();
            foreach (var s in Campaign.Catalog.Stages)
            {
                foreach (var e in s.Enemies)
                {
                    var label = s.InCampaign ? Fmt("campaign.foe.chapter", "Ch {0}", s.Chapter) : s.Name;
                    if (e.HeroId == def.Id && !seen.Contains(label))
                    {
                        seen.Add(label);
                    }
                }
            }

            if (seen.Count > 8)
            {
                seen.RemoveRange(7, seen.Count - 7);
                seen.Add("\u2026");
            }

            var where = HookSkin.Outlined(SurvivalVisuals.Text(d, "Where", Fmt("campaign.foe.where", "Fights in: {0}", seen.Count > 0 ? string.Join(", ", seen) : S("campaign.foe.camps", "World camps")), 22, TextAnchor.UpperLeft, SurvivalVisuals.Cream), 1.2f);
            where.horizontalOverflow = HorizontalWrapMode.Wrap;
            HookSkin.Box(where.rectTransform, 0.37f, 0.06f, 0.97f, 0.44f);
            return () => { };
        }

        private Text StatLine(RectTransform d, string name, float y)
        {
            var t = HookSkin.Outlined(SurvivalVisuals.Text(d, name, "", 28, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.5f);
            t.supportRichText = true;
            HookSkin.Box(t.rectTransform, 0.37f, y, 0.98f, y + 0.09f);
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
            BalanceBar.Above(panel, Campaign.Wallet, BalanceBarIds.Recruit);
            Portrait(panel, def, 0.05f, 0.50f, 0.36f, 0.84f, out _);
            var blurb = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Blurb", Title(def.Rarity) + " " + Title(def.Role), 26, TextAnchor.MiddleLeft, RarityColor(def.Rarity)), 1.5f);
            HookSkin.Box(blurb.rectTransform, 0.40f, 0.74f, 0.96f, 0.82f);
            var st = HeroStats.Of(def, Campaign.Catalog.Curve, 1, 1);
            var stats = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Stats", Fmt("campaign.recruit_stats", "HP {0}  \u00b7  ATK {1}\nPOWER {2}", ChipWallet.FormatGrouped(st.Hp), st.Atk, ChipWallet.FormatGrouped(st.Power)), 24, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.5f);
            HookSkin.Box(stats.rectTransform, 0.40f, 0.56f, 0.96f, 0.73f);
            var sandbox = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Sandbox", S("store.sandbox", "SANDBOX \u00b7 no real charge"), 16, TextAnchor.MiddleCenter, Warn), 1.5f);
            HookSkin.Box(sandbox.rectTransform, 0f, 0.47f, 1f, 0.50f);

            var gemRow = HookSkin.Panel(panel, "GemRow", HookArtKeys.Plate, SurvivalVisuals.RowFill, 0.45f);
            HookSkin.Box(gemRow.rectTransform, 0.04f, 0.31f, 0.96f, 0.46f);
            var gemIcon = HookSkin.ResIcon(gemRow.transform, "Icon", SurvIds.ResHard, new Color(0.42f, 0.86f, 1f, 1f), "\u25c6");
            HookSkin.Box(gemIcon.rectTransform, 0.01f, 0.05f, 0.18f, 0.95f);
            var gemLine = HookSkin.Outlined(SurvivalVisuals.Text(gemRow.transform, "T", S("campaign.recruit_now", "Recruit now"), 24, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.5f);
            HookSkin.Box(gemLine.rectTransform, 0.20f, 0f, 0.62f, 1f);
            var gemText = SheetButton(gemRow.rectTransform, "BuyGems", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, 0.64f, 0.14f, 0.98f, 0.86f, 24, out var gemBtn);
            gemText.text = ChipWallet.FormatGrouped(def.UnlockGems) + " \u25c6";
            var gemRt = gemBtn.GetComponent<RectTransform>();

            var shardRow = HookSkin.Panel(panel, "ShardRow", HookArtKeys.Plate, SurvivalVisuals.RowFill, 0.45f);
            HookSkin.Box(shardRow.rectTransform, 0.04f, 0.15f, 0.96f, 0.30f);
            var shardIcon = HookSkin.ResIcon(shardRow.transform, "Icon", RewardBundle.ShardKey(def.Id), ShardPurple, "\u2726");
            HookSkin.Box(shardIcon.rectTransform, 0.01f, 0.05f, 0.18f, 0.95f);
            var shardLine = HookSkin.Outlined(SurvivalVisuals.Text(shardRow.transform, "T", "", 24, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.5f);
            HookSkin.Box(shardLine.rectTransform, 0.20f, 0f, 0.62f, 1f);
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
            Ornaments.Grain(panel, new Color(1f, 1f, 1f, 0.9f));
            Ornaments.Frame(panel, Ornaments.Gold, 4f);
            var crestTop = Ornaments.Crest(panel, new Color(0.30f, 0.55f, 1f, 1f));
            HookSkin.Box(crestTop.rectTransform, 0.44f, 0.955f, 0.56f, 1.035f);
            BalanceBar.Inside(panel, Campaign.Wallet, BalanceBarIds.PowerPacks, 0.05f, 0.843f, 0.95f, 0.885f);
            var sandbox = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Sandbox", S("store.sandbox", "SANDBOX \u00b7 no real charge"), 16, TextAnchor.MiddleCenter, Warn), 1.5f);
            HookSkin.Box(sandbox.rectTransform, 0f, 0.82f, 1f, 0.845f);

            var rows = shop.Available();
            var top = 0.81f;
            var h = Mathf.Min(0.095f, (top - 0.10f) / Mathf.Max(1, rows.Count));
            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var y1 = top - (i * h);
                // Child mode: no BEST VALUE / POPULAR / ONE TIME ribbons or sparkles (Families: no pressure to buy).
                var tag = Survival.Domain.Kids.NoPressure.ShowSalesTag(KidSafety.Policy, r.Offer.Tag) ? r.Offer.Tag ?? "" : "";
                var hot = tag == "popular" || tag == "best_value";
                var accent = tag == "best_value" ? SurvivalVisuals.MineFill : tag == "popular" ? HeroGlow : FreeGreen;
                if (hot)
                {
                    // Highlighted cards: soft glow and a gold border behind the plate.
                    var glow = HookSkin.Glow(panel, "PackGlow_" + r.Offer.Id, new Color(accent.r, accent.g, accent.b, 0.55f));
                    glow.raycastTarget = false;
                    HookSkin.Box(glow.rectTransform, 0.015f, y1 - h - 0.002f, 0.985f, y1 + 0.008f);
                    var border = SurvivalVisuals.Image(panel, "PackBorder_" + r.Offer.Id, SurvivalVisuals.GoldLine);
                    border.raycastTarget = false;
                    HookSkin.Box(border.rectTransform, 0.026f, y1 - h + 0.003f, 0.974f, y1 + 0.003f);
                }

                var row = HookSkin.Panel(panel, "Pack_" + r.Offer.Id, HookArtKeys.Plate, hot ? new Color(0.16f, 0.24f, 0.52f, 1f) : SurvivalVisuals.RowFill, 0.45f);
                HookSkin.Box(row.rectTransform, 0.03f, y1 - h + 0.006f, 0.97f, y1);
                Ornaments.Grain(row.transform, new Color(1f, 1f, 1f, 0.85f));
                Ornaments.Bevel(row.transform);
                Ornaments.Frame(row.transform, hot ? Ornaments.Gold : Ornaments.Bronze, 1f);
                // Pack art: satchel/chest/hoard by ladder position; the best-value pack gets the open hoard.
                var artKey = tag == "best_value" ? HookArtKeys.ChestOpen : tag == "starter" ? HookArtKeys.Gift : i <= 1 ? HookArtKeys.GemsTier1 : i == 2 ? HookArtKeys.GemsTier2 : i == 3 ? HookArtKeys.ChestClosed : HookArtKeys.GemsTier3;
                if (hot)
                {
                    var halo = HookSkin.Glow(row.transform, "ArtGlow", new Color(accent.r, accent.g, accent.b, 0.6f));
                    halo.raycastTarget = false;
                    HookSkin.Box(halo.rectTransform, 0.0f, -0.05f, 0.17f, 0.85f);
                }

                var packArt = HookSkin.Art(row.transform, "PackArt", artKey, SurvivalVisuals.MineFill);
                packArt.raycastTarget = false;
                HookSkin.Box(packArt.rectTransform, 0.015f, 0.05f, 0.155f, 0.68f);
                var title = HookSkin.Outlined(SurvivalVisuals.Text(row.transform, "Name", S(r.Offer.Id + ".title", r.Offer.Id), 24, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.5f);
                title.fontStyle = FontStyle.Bold;
                HookSkin.Fit(title, 14, 24);
                HookSkin.Box(title.rectTransform, 0.20f, 0.60f, 0.60f, 0.96f);
                if (tag.Length > 0)
                {
                    // Corner ribbon: ONE TIME / POPULAR / BEST VALUE.
                    var label = tag == "starter" ? S("store.power_packs.one_time", "ONE TIME") : S("offer.tag." + tag, tag.Replace('_', ' ').ToUpperInvariant());
                    Ornaments.Ribbon(row.transform, label, tag == "starter" ? new Color(0.80f, 0.22f, 0.24f, 1f) : accent, out var ribbon);
                    HookSkin.Box(ribbon.rectTransform, -0.015f, 0.68f, 0.195f, 1.06f);
                }

                if (hot)
                {
                    Ornaments.Sparkles((RectTransform)row.transform, tag == "best_value" ? 6 : 4, new Color(1f, 0.93f, 0.70f, 1f));
                }

                if (r.BonusLabel.Length > 0)
                {
                    // Big "+X%" with VALUE under it.
                    var pct = HookSkin.Outlined(SurvivalVisuals.Text(row.transform, "Bonus", r.BonusLabel, 34, TextAnchor.MiddleCenter, FreeGreen), 2f);
                    pct.fontStyle = FontStyle.Bold;
                    HookSkin.Fit(pct, 16, 34);
                    HookSkin.Box(pct.rectTransform, 0.61f, 0.36f, 0.75f, 0.96f);
                    var val = HookSkin.Outlined(SurvivalVisuals.Text(row.transform, "BonusSub", S("store.power_packs.value_word", "VALUE"), 15, TextAnchor.MiddleCenter, FreeGreen), 1.2f);
                    val.fontStyle = FontStyle.Bold;
                    HookSkin.Fit(val, 9, 15);
                    HookSkin.Box(val.rectTransform, 0.61f, 0.08f, 0.75f, 0.38f);
                }

                PackChips((RectTransform)row.transform, r.Grants, 0.17f, 0.05f, 0.60f, 0.58f);
                var price = SheetButton((RectTransform)row.transform, "Buy", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, 0.77f, 0.14f, 0.98f, 0.86f, 26, out var buyBtn);
                price.text = shop.PriceLabel(r);
                price.color = Color.white; // crisp: white on gold with the dark outline
                HookSkin.Fit(price, 14, 26);
                HookSkin.PadLabel(price.rectTransform, 0.08f, 0.16f);
                var buyRt = buyBtn.GetComponent<RectTransform>();
                Ornaments.Shine(buyRt, hot ? 2.4f : 3.6f);
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

        internal static readonly Color RoyalBlue = new Color(0.20f, 0.36f, 0.85f, 1f);
        /// <summary>The Kingdom Path sheet (<see cref="RewardTrackScreen"/>).</summary>
        public void OpenRewardTrack() => (_rewardTrack ??= new RewardTrackScreen(this)).Open();

        /// <summary>Reward chips (icon + amount) in up to two rows across the box, nothing clipped.</summary>
        private void PackChips(RectTransform parent, IReadOnlyDictionary<string, int> grants, float x0, float y0, float x1, float y1)
        {
            var n = grants.Count;
            if (n == 0)
            {
                return;
            }

            var rowsN = n > 3 ? 2 : 1;
            var cols = (n + rowsN - 1) / rowsN;
            var w = (x1 - x0) / cols;
            var hh = (y1 - y0) / rowsN;
            var i = 0;
            foreach (var pair in grants)
            {
                var c = i % cols;
                var rr = i / cols;
                var cx0 = x0 + (c * w);
                var cy1 = y1 - (rr * hh);
                var chip = SurvivalVisuals.Image(parent, "Chip_" + pair.Key, Color.white, Ornaments.Get("plate"));
                chip.type = Image.Type.Sliced;
                chip.raycastTarget = false;
                HookSkin.Box(chip.rectTransform, cx0 + 0.004f, cy1 - hh + 0.04f, cx0 + w - 0.004f, cy1 - 0.02f);
                var icon = HookSkin.ResIcon(chip.transform, "Icon", pair.Key, SurvivalVisuals.Reward, "\u2726");
                icon.raycastTarget = false;
                HookSkin.Box(icon.rectTransform, 0.02f, 0.06f, 0.30f, 0.94f);
                var amount = HookSkin.Outlined(SurvivalVisuals.Text(chip.transform, "T", "+" + ChipAmount(pair.Key, pair.Value), 18, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.2f);
                amount.fontStyle = FontStyle.Bold;
                amount.raycastTarget = false;
                HookSkin.Fit(amount, 9, 18);
                HookSkin.Box(amount.rectTransform, 0.32f, 0.04f, 0.98f, 0.96f);
                i++;
            }
        }

        private static string ChipAmount(string resourceId, int value) =>
            string.Equals(resourceId, SurvIds.ResSpeedupM, StringComparison.Ordinal) ? FormatMinutes(value) : ChipWallet.FormatCompact(value);

        /// <summary>Speed-up minutes as hours: 45 -> 45m, 960 -> 16h, 2000 -> 33h20m.</summary>
        public static string FormatMinutes(int minutes)
        {
            if (minutes < 60)
            {
                return minutes.ToString(CultureInfo.InvariantCulture) + "m";
            }

            var hrs = minutes / 60;
            var m = minutes % 60;
            return hrs.ToString(CultureInfo.InvariantCulture) + "h" + (m > 0 ? m.ToString(CultureInfo.InvariantCulture) + "m" : "");
        }

        private BattleFlowScreen? _battleFlow;

        /// <summary>Battle Setup for <paramref name="stage"/>, then the battle and its result (<see cref="BattleFlowScreen"/>).</summary>
        public void OpenSetup(StageDef? stage) => (_battleFlow ??= new BattleFlowScreen(this)).OpenSetup(stage);

        // ---------- widgets ----------

        /// <summary>
        /// Framed portrait: Sir Aldric blue (#1b3060) for heroes, purple for villains, a gold inner line and the painted
        /// portrait from the pack cropped and clipped inside the trim, or a code-drawn fallback (tinted plate with initials).
        /// </summary>
        internal RectTransform Portrait(RectTransform parent, CampaignHeroDef def, float x0, float y0, float x1, float y1, out Image art)
        {
            var hero = def.Side == CampaignSide.Hero;
            var frame = SurvivalVisuals.Image(parent, "Portrait_" + def.Id, hero ? HeroFrame : VillainFrame);
            frame.raycastTarget = false;
            HookSkin.Box(frame.rectTransform, x0, y0, x1, y1);
            var trim = SurvivalVisuals.Image(frame.transform, "Trim", SurvivalVisuals.GoldLine);
            trim.raycastTarget = false;
            SurvivalVisuals.Stretch(trim.rectTransform);
            trim.rectTransform.offsetMin = new Vector2(6f, 6f);
            trim.rectTransform.offsetMax = new Vector2(-6f, -6f);
            var sprite = SurvivalArt.Get(def.PortraitKey);
            if (sprite != null)
            {
                // Crop to fill, never squash, and clip to the gold. Nightfang's ears sit on the top
                // edge of a square painting, so any leak past the trim shows as a head poking out of
                // the box on the Foes pane. Parent the window onto the canvas first, then add the
                // mask — a clipper enabled off-canvas can miss the Canvas clip list. Stencil Mask
                // plus RectMask2D so cover-sized art cannot paint over the filigree.
                var windowGo = new GameObject("Window", typeof(RectTransform));
                var window = windowGo.GetComponent<RectTransform>();
                window.SetParent(trim.transform, false);
                SurvivalVisuals.Stretch(window);
                window.offsetMin = new Vector2(3f, 3f);
                window.offsetMax = new Vector2(-3f, -3f);
                var hole = windowGo.AddComponent<Image>();
                hole.sprite = SurvivalArt.White();
                hole.color = Color.white;
                hole.raycastTarget = false;
                windowGo.AddComponent<RectMask2D>();
                var mask = windowGo.AddComponent<Mask>();
                mask.showMaskGraphic = false;
                art = SurvivalVisuals.Image(window, "Art", Color.white, sprite);
                art.raycastTarget = false;
                art.maskable = true;
                var fit = art.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = sprite.rect.height > 0 ? sprite.rect.width / sprite.rect.height : 1f;
            }
            else
            {
                art = SurvivalVisuals.Image(trim.transform, "Art", hero ? new Color(0.22f, 0.36f, 0.66f, 1f) : new Color(0.36f, 0.18f, 0.48f, 1f));
                art.raycastTarget = false;
                SurvivalVisuals.Stretch(art.rectTransform);
                art.rectTransform.offsetMin = new Vector2(3f, 3f);
                art.rectTransform.offsetMax = new Vector2(-3f, -3f);
            }

            // Detail pass: inner-shadow bevel over the art, rarity-tinted filigree frame, gem crest on top.
            Ornaments.Bevel(trim.transform, 0.9f);
            var rarity = Ornaments.Rarity(def.Rarity);
            Ornaments.Frame(frame.transform, Color.Lerp(Ornaments.Gold, rarity, 0.45f), 2f);
            var crest = Ornaments.Crest(frame.transform, rarity);
            HookSkin.Box(crest.rectTransform, 0.40f, 0.90f, 0.60f, 1.06f);
            if (sprite == null)
            {
                var initials = HookSkin.Outlined(SurvivalVisuals.Text(art.transform, "Initials", Initials(HeroName(def.Id)), 64, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 2f);
                initials.fontStyle = FontStyle.Bold;
                initials.raycastTarget = false;
                SurvivalVisuals.Stretch(initials.rectTransform);
            }

            return frame.rectTransform;
        }

        /// <summary>
        /// Five star images (sprite art, not a font glyph). The row is drawn once and only touched again when the
        /// star count changes: the Heroes refresh runs every tick, and the old "\u2605" text glyphs flickered on the
        /// dim unearned stars whenever the dynamic font atlas rebuilt under them (Derek 2026-11-03).
        /// </summary>
        private sealed class StarRow
        {
            public readonly Image[] Stars = new Image[5];
            public int Shown = -1;
        }

        private static StarRow Stars(RectTransform parent, float x0, float y0, float x1, float y1)
        {
            var host = new GameObject("Stars", typeof(RectTransform)).GetComponent<RectTransform>();
            host.SetParent(parent, false);
            HookSkin.Box(host, x0, y0, x1, y1);
            var plate = Ornaments.Plate(host, "StarPlate");
            plate.rectTransform.anchorMin = new Vector2(-0.04f, -0.08f);
            plate.rectTransform.anchorMax = new Vector2(1.04f, 1.08f);
            plate.rectTransform.offsetMin = plate.rectTransform.offsetMax = Vector2.zero;
            var row = new StarRow();
            for (var i = 0; i < row.Stars.Length; i++)
            {
                var img = Ornaments.Star(host, StarOff);
                img.name = "Star" + i;
                img.raycastTarget = false;
                img.preserveAspect = true;
                HookSkin.Box(img.rectTransform, i / 5f + 0.02f, 0.08f, (i + 1) / 5f - 0.02f, 0.92f);
                row.Stars[i] = img;
            }

            return row;
        }

        private static void SetStars(StarRow row, int count)
        {
            if (!HeroesScreen.StarsNeedRedraw(row.Shown, count))
            {
                return;
            }

            row.Shown = count;
            for (var i = 0; i < row.Stars.Length; i++)
            {
                row.Stars[i].color = i < count ? StarOn : StarOff;
                row.Stars[i].gameObject.SetActive(count > 0);
            }
        }

        /// <summary>
        /// Shrink a sheet button's label to fit inside the painted rim (best fit, never above its design size), so long
        /// labels like "UPGRADE HEROES" don't spill past a half-width button on narrow phones.
        /// </summary>
        internal static void FitButtonLabel(Text label, int size)
        {
            HookSkin.Fit(label, Mathf.Max(12, Mathf.RoundToInt(size * 0.5f)), size);
            HookSkin.PadLabel(label.rectTransform, 0.07f, 0.12f);
        }

        internal static Text SheetButton(RectTransform parent, string name, string key, Color fallback, float x0, float y0, float x1, float y1, int size, out Button button)
        {
            button = HookSkin.Button(parent, name, key, fallback, 0.5f);
            HookSkin.Box(button.GetComponent<RectTransform>(), x0, y0, x1, y1);
            var label = HookSkin.Outlined(SurvivalVisuals.Text(button.transform, "T", "", size, TextAnchor.MiddleCenter, HookSkin.InkFor(key)), 1.5f);
            label.fontStyle = FontStyle.Bold;
            label.raycastTarget = false;
            SurvivalVisuals.Stretch(label.rectTransform);
            return label;
        }

        internal void Wire(Button button, Action onTap)
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

        internal void Juice(RectTransform rt)
        {
            if (_hook != null)
            {
                _hook.Juice(rt);
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

        private static Color Rgb(IReadOnlyList<float> rgb) => new Color(rgb[0], rgb[1], rgb[2], 1f);

        private static string Initials(string name)
        {
            var parts = name.Split(' ');
            return parts.Length > 1 ? parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1) : name.Substring(0, Math.Min(2, name.Length));
        }

        internal static string Title(string s) => string.IsNullOrEmpty(s) ? "" : char.ToUpperInvariant(s[0]) + s.Substring(1);

        private static Color RarityColor(string rarity) => rarity switch
        {
            "legendary" => new Color(1f, 0.72f, 0.28f, 1f),
            "epic" => new Color(0.80f, 0.58f, 1f, 1f),
            _ => new Color(0.55f, 0.78f, 1f, 1f)
        };

        internal string HeroName(string id)
        {
            var def = Campaign?.Catalog.Hero(id);
            return S(id + ".name", def != null ? def.Fighter : id);
        }

        internal string S(string key, string fallback) => _session != null ? _session.Pack.StringOr(key, fallback) : fallback;

        internal string Fmt(string key, string fallback, params object[] args) => string.Format(CultureInfo.InvariantCulture, S(key, fallback), args);
    }
}
