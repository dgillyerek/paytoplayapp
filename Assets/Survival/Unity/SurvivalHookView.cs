#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Survival.Domain.Base;
using Survival.Domain.Clock;
using Survival.Domain.Gift;
using Survival.Domain.Ids;
using Survival.Domain.Session;
using Survival.Domain.Store;
using Survival.Domain.Theme;
using Survival.Domain.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// World + quest "hook" slice on top of the Play shell: the Village view's buildings (Castle, Farms, Lumber
    /// Mills, Forge on their plots: tap to collect, tap for the building card with timed upgrades, gem rush, free
    /// finish; the Castle swaps to the next, bigger painted stage every level, the others show a numbered level badge),
    /// the starter quest tracker with chest claim, the free gift, the sandbox Gem Shop with its pulsing FREE badge,
    /// and the chest-burst celebration. Juice on every tap.
    /// Build (SurvivalHookView.Build.cs): the painted Build button opens the build menu; pick a type, tap a glowing
    /// free spot (or let it auto-place) and the construction runs on the same timer / speed-up flow as upgrades.
    /// The village (levels, placed buildings, timers) is saved to PlayerPrefs.
    /// Debug (Editor / dev builds): G = free gift now, T = skip 5 min (also on-screen buttons), R = reset the village save.
    /// </summary>
    public sealed partial class SurvivalHookView : MonoBehaviour
    {
        private const float Aspect = 1080f / 1920f;
        private const float DebugSkipSeconds = 300f;
        private const float CoinSize = 0.06f;

        private static readonly string[] ChipKeys =
        {
            ChipWallet.SoftChip, ChipWallet.WoodChip, ChipWallet.StoneChip, ChipWallet.FoodChip, ChipWallet.HardChip
        };

        private static readonly Color FreeGreen = new Color(0.30f, 0.82f, 0.38f, 1f);
        private static readonly Color Dim = new Color(0.02f, 0.03f, 0.05f, 0.78f);
        private static readonly Color Disabled = new Color(0.30f, 0.31f, 0.34f, 1f);
        private static readonly Color Warn = new Color(1f, 0.55f, 0.45f, 1f);
        private static readonly Color ChestWood = new Color(0.47f, 0.27f, 0.12f, 1f);
        private static readonly Color ChestLid = new Color(0.58f, 0.34f, 0.15f, 1f);

        private static readonly Color[] ConfettiColors =
        {
            new Color(0.98f, 0.82f, 0.25f), new Color(0.95f, 0.36f, 0.42f), new Color(0.40f, 0.80f, 1f),
            new Color(0.45f, 0.90f, 0.45f), new Color(0.80f, 0.50f, 1f), new Color(1f, 1f, 1f)
        };

        private SurvivalPlayView? _play;
        private HookSession? _hook;
        private ThemePackBinder? _pack;
        private RectTransform? _overlay;

        private readonly Dictionary<string, PlotCard> _cards = new();
        private readonly Dictionary<string, int> _shown = new();
        private readonly HashSet<string> _held = new();
        private readonly Dictionary<string, Coroutine> _counting = new();
        private readonly Queue<IEnumerator> _celebrations = new();

        private QuestTracker? _quest;
        private bool _questWasComplete;
        private RectTransform? _shopButton;
        private Image? _freeBadge;
        private Image? _freeGlow;
        private Image? _questPreview;
        private GameObject? _sheet;
        private Action? _sheetRefresh;
        private bool _celebrating;
        private int _lastDebugFrame = -1;
        private float _uiTick;

        /// <summary>One village building: art on its plot host plus its signs (level badge, name plate, timer, bubble).</summary>
        private sealed class PlotCard
        {
            public string PlotId = "";
            public bool IsCastle;
            public int Tier;
            public RectTransform Root = null!;
            public Image Art = null!;
            public Text Name = null!;
            public Image? LevelBadge;
            public Text? LevelText;
            public Image Bubble = null!;
            public Text BubbleText = null!;
            public RectTransform TimerRoot = null!;
            public Image BarFill = null!;
            public Text Status = null!;
            public Image Badge = null!;
            public Text BadgeText = null!;
            public Image Arrow = null!;
            public Image? Hammer;
        }

        private sealed class QuestTracker
        {
            public RectTransform Root = null!;
            public Text Title = null!;
            public Text Body = null!;
            public Image BarBack = null!;
            public Image BarFill = null!;
            public Text Count = null!;
            public RectTransform Chest = null!;
        }

        private sealed class Confetto
        {
            public RectTransform Rt = null!;
            public Image Img = null!;
            public Vector2 Pos;
            public Vector2 Vel;
            public float Spin;
            public float Life;
        }

        public void Build(SurvivalPlayView play, SurvivalSession session)
        {
            if (session.Hook == null || play.Root == null || _hook != null)
            {
                return;
            }

            _play = play;
            _hook = session.Hook;
            _pack = session.Pack;
            _overlay = SurvivalVisuals.Canvas(transform, "SurvivalHookOverlay", 60);

            play.AddChip(ChipWallet.HardChip, S("hud.chip.gems_label", "Gems"));
            foreach (var key in ChipKeys)
            {
                _shown[key] = session.Chips.Get(key);
            }

            var root = play.Root;
            SetupBuild(play);
            BuildVillage(play.Village);
            BuildBuildButton(root);
            BuildQuestTracker(root);
            BuildShopButton(root);
            if (Application.isEditor || Debug.isDebugBuild)
            {
                BuildDebugButtons(root);
            }

            play.Fx?.SetAsLastSibling();
            _hook.Wallet.Changed += OnWalletChanged;
            _hook.Gifts.Offered += OnGiftOffered;
            play.MarchResolved += OnMarchResolved;
            _questWasComplete = _hook.Quests.IsComplete;
            RefreshAll();
        }

        private void OnDestroy()
        {
            if (_hook != null)
            {
                SaveVillage();
                _hook.Wallet.Changed -= OnWalletChanged;
                _hook.Gifts.Offered -= OnGiftOffered;
                _hook.Build.Built -= OnBuilt;
            }

            if (_play != null)
            {
                _play.MarchResolved -= OnMarchResolved;
            }
        }

        private void Update()
        {
            if (_hook == null)
            {
                return;
            }

            _hook.Tick();
            PollDebugKeys();
            _uiTick -= Time.unscaledDeltaTime;
            if (_uiTick <= 0f)
            {
                _uiTick = 0.1f;
                RefreshAll();
            }

            AnimateIdle(Time.unscaledTime);
            TickBuild();
        }

        // ---------- build ----------

        private void BuildVillage(SurvivalVillageView? village)
        {
            if (_hook == null || village == null)
            {
                return;
            }

            foreach (var plot in _hook.Base.Plots)
            {
                var spot = _hook.Build.SpotOf(plot.Id);
                var host = village.Host(plot.Id) ?? (spot != null ? village.AddHost(plot.Id, spot) : null);
                if (host != null)
                {
                    _cards[plot.Id] = BuildCard(village, host, plot);
                }
            }
        }

        private IReadOnlyList<CastleTier> Tiers => _play?.Village?.Layout?.CastleTiers ?? CastleTiers.Default;

        private PlotCard BuildCard(SurvivalVillageView village, RectTransform host, BasePlot plot)
        {
            var plotId = plot.Id;
            var visual = BuildingVisual.For(plot, Tiers);
            var card = new PlotCard { PlotId = plotId, Root = host, IsCastle = !visual.ShowBadge, Tier = visual.Tier };
            if (card.IsCastle)
            {
                village.SetFootprint(plotId, visual.Scale);
            }

            // Painted building fills its plot footprint; transparent pixels let drags and taps fall through to the map.
            var glow = HookSkin.Glow(host, "Glow", new Color(1f, 0.86f, 0.45f, 0f));
            Box(glow.rectTransform, -0.1f, -0.05f, 1.1f, 1.05f);
            var art = HookSkin.Art(host, "Building", visual.ArtKey, plot.Def.Produces != null ? ResColor(plot.Def.Produces) : SurvivalVisuals.PinBuild);
            SurvivalVisuals.Stretch(art.rectTransform);
            art.raycastTarget = true;
            if (art.sprite != null && art.sprite.texture != null && art.sprite.texture.isReadable)
            {
                art.alphaHitTestMinimumThreshold = 0.08f;
            }

            card.Art = art;
            var body = art.gameObject.AddComponent<Button>();
            body.targetGraphic = art;
            body.transition = Selectable.Transition.None;

            // Name plate under the building (the Castle's carries its level; the rest wear a badge).
            var plate = HookSkin.Panel(host, "Plate", HookArtKeys.Chip, SurvivalVisuals.PanelSoft, 0.4f);
            plate.rectTransform.anchorMin = new Vector2(0.5f, 0.05f);
            plate.rectTransform.anchorMax = new Vector2(0.5f, 0.05f);
            plate.rectTransform.pivot = new Vector2(0.5f, 1f);
            plate.rectTransform.sizeDelta = new Vector2(card.IsCastle ? 330f : 260f, 58f);
            plate.rectTransform.anchoredPosition = Vector2.zero;
            card.Name = HookSkin.Outlined(SurvivalVisuals.Text(plate.transform, "Name", PlotName(plotId), 26, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
            card.Name.fontStyle = FontStyle.Bold;
            SurvivalVisuals.Stretch(card.Name.rectTransform);
            village.RegisterLabel(plate.rectTransform);

            if (visual.ShowBadge)
            {
                var badgeSprite = SurvivalArt.Get(LevelBadge.ArtKey);
                card.LevelBadge = badgeSprite != null
                    ? SurvivalVisuals.Image(host, "LevelBadge", Color.white, badgeSprite)
                    : SurvivalVisuals.Circle(host, "LevelBadge", SurvivalVisuals.PinBuild);
                card.LevelBadge.preserveAspect = true;
                Box(card.LevelBadge.rectTransform, 0.0f, 0.62f, 0.26f, 0.92f);
                card.LevelText = HookSkin.Outlined(SurvivalVisuals.Text(card.LevelBadge.transform, "N", visual.BadgeText, 40, TextAnchor.MiddleCenter, Color.white), 2.5f);
                card.LevelText.fontStyle = FontStyle.Bold;
                Box(card.LevelText.rectTransform, 0f, 0.0f, 1f, 0.86f);
            }

            // Upgrade timer under the plate.
            var timer = new GameObject("Timer", typeof(RectTransform)).GetComponent<RectTransform>();
            timer.SetParent(host, false);
            timer.anchorMin = new Vector2(0.5f, 0.05f);
            timer.anchorMax = new Vector2(0.5f, 0.05f);
            timer.pivot = new Vector2(0.5f, 1f);
            timer.sizeDelta = new Vector2(280f, 64f);
            timer.anchoredPosition = new Vector2(0f, -60f);
            var bar = HookSkin.Bar(timer, "Bar", out card.BarFill);
            Box(bar.rectTransform, 0f, 0f, 1f, 0.5f);
            card.Status = HookSkin.Outlined(SurvivalVisuals.Text(timer, "Status", "", 22, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
            card.Status.fontStyle = FontStyle.Bold;
            Box(card.Status.rectTransform, -0.2f, 0.45f, 1.2f, 1.1f);
            card.TimerRoot = timer;
            village.RegisterLabel(timer);

            // Collect bubble: pill with the resource icon above the roof.
            var bubbleSprite = SurvivalArt.Get(HookArtKeys.Chip);
            card.Bubble = bubbleSprite != null
                ? SurvivalVisuals.Image(host, "Bubble", Color.white, bubbleSprite)
                : SurvivalVisuals.Circle(host, "Bubble", SurvivalVisuals.Cream);
            HookSkin.ApplySliced(card.Bubble, 0.4f);
            Box(card.Bubble.rectTransform, 0.22f, 0.88f, 0.78f, 1.08f);
            card.Bubble.raycastTarget = true;
            var bubbleBtn = card.Bubble.gameObject.AddComponent<Button>();
            bubbleBtn.targetGraphic = card.Bubble;
            if (plot.Def.Produces != null)
            {
                var bubbleIcon = HookSkin.ResIcon(card.Bubble.transform, "Icon", plot.Def.Produces, ResColor(plot.Def.Produces), ResGlyph(plot.Def.Produces));
                Box(bubbleIcon.rectTransform, -0.08f, -0.25f, 0.40f, 1.25f);
            }

            card.BubbleText = HookSkin.Outlined(SurvivalVisuals.Text(card.Bubble.transform, "T", "", 26, TextAnchor.MiddleCenter, bubbleSprite != null ? SurvivalVisuals.Reward : SurvivalVisuals.Panel), 1.5f);
            Box(card.BubbleText.rectTransform, 0.32f, 0f, 1f, 1f);
            card.Bubble.gameObject.SetActive(false);

            var badgeFree = SurvivalArt.Get(HookArtKeys.BadgeFree);
            card.Badge = badgeFree != null
                ? SurvivalVisuals.Image(host, "Badge", Color.white, badgeFree)
                : SurvivalVisuals.Circle(host, "Badge", FreeGreen);
            card.Badge.preserveAspect = true;
            Box(card.Badge.rectTransform, 0.72f, 0.66f, 1.0f, 0.98f);
            card.BadgeText = HookSkin.Outlined(SurvivalVisuals.Text(card.Badge.transform, "T", "", 22, TextAnchor.MiddleCenter, Color.white), 1.5f);
            SurvivalVisuals.Stretch(card.BadgeText.rectTransform);
            card.Badge.gameObject.SetActive(false);

            card.Arrow = HookSkin.Art(host, "UpgradeArrow", WorldArtKeys.IconUpgrade, FreeGreen);
            Box(card.Arrow.rectTransform, 0.76f, 0.66f, 0.98f, 0.90f);
            card.Arrow.gameObject.SetActive(false);

            // Under construction: a hammer bobs over the half-built (faded) building.
            card.Hammer = HookSkin.Art(host, "Hammer", Survival.Domain.View.BottomNav.IconBuild, SurvivalVisuals.Gold);
            Box(card.Hammer.rectTransform, 0.34f, 0.62f, 0.66f, 0.98f);
            card.Hammer.raycastTarget = false;
            card.Hammer.gameObject.SetActive(plot.UnderConstruction);

            Wire(body, () => OnTapPlot(plotId));
            Wire(bubbleBtn, () => OnTapPlot(plotId));
            return card;
        }

        private void BuildQuestTracker(RectTransform root)
        {
            if (_pack == null || !_pack.Hud.TryGetValue("quest_tracker", out var questRect))
            {
                return;
            }

            var scroll = HookSkin.Has(HookArtKeys.Scroll);
            var frame = HookSkin.Panel(root, "QuestTracker", HookArtKeys.Scroll, SurvivalVisuals.Panel, 0.8f);
            frame.raycastTarget = true;
            var btn = frame.gameObject.AddComponent<Button>();
            btn.targetGraphic = frame;
            SurvivalVisuals.Place(frame.rectTransform, questRect);

            var titleInk = scroll ? HookSkin.ParchmentTitle : SurvivalVisuals.Gold;
            var bodyInk = scroll ? HookSkin.ParchmentInk : SurvivalVisuals.Cream;
            var q = new QuestTracker { Root = frame.rectTransform };
            q.Title = SurvivalVisuals.Text(frame.transform, "Title", "", 17, TextAnchor.MiddleLeft, titleInk);
            q.Title.fontStyle = FontStyle.Bold;
            Box(q.Title.rectTransform, 0.11f, 0.68f, 0.74f, 0.90f);
            q.Body = SurvivalVisuals.Text(frame.transform, "Body", "", 17, TextAnchor.UpperLeft, bodyInk);
            q.Body.horizontalOverflow = HorizontalWrapMode.Wrap;
            Box(q.Body.rectTransform, 0.11f, 0.34f, 0.74f, 0.68f);
            var bar = HookSkin.Bar(frame.transform, "Bar", out q.BarFill);
            q.BarBack = bar;
            Box(bar.rectTransform, 0.10f, 0.13f, 0.56f, 0.30f);
            q.Count = SurvivalVisuals.Text(frame.transform, "Count", "", 16, TextAnchor.MiddleLeft, bodyInk);
            q.Count.fontStyle = FontStyle.Bold;
            Box(q.Count.rectTransform, 0.58f, 0.10f, 0.74f, 0.32f);

            // Reward chest preview always sits on the scroll; it glows and hops once the step is done.
            var preview = HookSkin.Art(frame.transform, "ChestPreview", HookArtKeys.ChestClosed, ChestWood);
            Box(preview.rectTransform, 0.74f, 0.16f, 0.92f, 0.84f);
            preview.color = new Color(1f, 1f, 1f, 0.55f);
            _questPreview = preview;

            var chestHost = SurvivalVisuals.Image(frame.transform, "Chest", new Color(0f, 0f, 0f, 0f));
            Box(chestHost.rectTransform, 0.70f, 0.06f, 0.98f, 0.96f);
            var chestGlow = HookSkin.Glow(chestHost.transform, "Glow", new Color(1f, 0.85f, 0.4f, 0.9f));
            Box(chestGlow.rectTransform, -0.35f, -0.35f, 1.35f, 1.35f);
            if (HookSkin.Has(HookArtKeys.ChestClosed))
            {
                var chest = HookSkin.Art(chestHost.transform, "Art", HookArtKeys.ChestClosed, ChestWood);
                SurvivalVisuals.Stretch(chest.rectTransform);
            }
            else
            {
                BuildChestArt(chestHost.rectTransform, out _);
            }

            var claim = HookSkin.Outlined(SurvivalVisuals.Text(chestHost.transform, "Claim", S("hook.claim", "Claim"), 17, TextAnchor.LowerCenter, SurvivalVisuals.Reward));
            Box(claim.rectTransform, -0.2f, -0.30f, 1.2f, 0.05f);
            q.Chest = chestHost.rectTransform;
            q.Chest.gameObject.SetActive(false);

            _quest = q;
            Wire(btn, OnTapQuest);
        }

        private void BuildShopButton(RectTransform root)
        {
            if (_pack == null || !_pack.Hud.TryGetValue("gem_shop", out var shopRect))
            {
                return;
            }

            var frame = HookSkin.Panel(root, "GemShopButton", HookArtKeys.ButtonBlue, SurvivalVisuals.AttackFill, 0.42f);
            frame.raycastTarget = true;
            var btn = frame.gameObject.AddComponent<Button>();
            btn.targetGraphic = frame;
            SurvivalVisuals.Place(frame.rectTransform, shopRect);
            var storeArt = SurvivalArt.Get(SurvIds.ThemeAStoreIcon);
            var icon = storeArt != null
                ? SurvivalVisuals.Image(frame.transform, "Icon", Color.white, storeArt)
                : SurvivalVisuals.Circle(frame.transform, "Icon", ResColor(SurvIds.ResHard));
            icon.preserveAspect = true;
            Box(icon.rectTransform, -0.04f, -0.25f, 0.32f, 1.25f);
            var label = HookSkin.Outlined(SurvivalVisuals.Text(frame.transform, "T", S("store.gem_shop.button", "Gem Shop"), 21, TextAnchor.MiddleLeft, SurvivalVisuals.Cream));
            label.fontStyle = FontStyle.Bold;
            Box(label.rectTransform, 0.31f, 0f, 1f, 1f);

            _freeGlow = HookSkin.Glow(frame.transform, "FreeGlow", new Color(0.55f, 1f, 0.45f, 0.8f));
            Box(_freeGlow.rectTransform, 0.60f, 0.05f, 1.16f, 1.80f);
            var badgeSprite = SurvivalArt.Get(HookArtKeys.BadgeFree);
            _freeBadge = badgeSprite != null
                ? SurvivalVisuals.Image(frame.transform, "FreeBadge", Color.white, badgeSprite)
                : SurvivalVisuals.Image(frame.transform, "FreeBadge", FreeGreen);
            _freeBadge.preserveAspect = true;
            Box(_freeBadge.rectTransform, 0.68f, 0.30f, 1.08f, 1.55f);
            var badgeText = HookSkin.Outlined(SurvivalVisuals.Text(_freeBadge.transform, "T", S("hook.free", "FREE"), 17, TextAnchor.MiddleCenter, Color.white), 1.5f);
            badgeText.fontStyle = FontStyle.Bold;
            SurvivalVisuals.Stretch(badgeText.rectTransform);
            _freeBadge.gameObject.SetActive(false);
            _freeGlow.gameObject.SetActive(false);

            _shopButton = frame.rectTransform;
            Wire(btn, OpenShop);
        }

        private void BuildDebugButtons(RectTransform root)
        {
            // Small corner chips; the G / T keys do the same.
            DebugButton(root, "debug_gift", S("debug.gift_short", "G: Gift"), DebugGiftNow);
            DebugButton(root, "debug_skip", S("debug.skip_short", "T: +5m"), DebugSkip);
        }

        private void DebugButton(RectTransform root, string slot, string label, Action onTap)
        {
            if (_pack == null || !_pack.Hud.TryGetValue(slot, out var slotRect))
            {
                return;
            }

            var btn = HookSkin.Button(root, "Debug_" + slot, HookArtKeys.ButtonRed, new Color(0.35f, 0.12f, 0.12f, 0.85f), 0.34f);
            SurvivalVisuals.Place(btn.GetComponent<RectTransform>(), slotRect);
            var t = HookSkin.Outlined(SurvivalVisuals.Text(btn.transform, "T", label, 13, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
            SurvivalVisuals.Stretch(t.rectTransform);
            Wire(btn, onTap);
        }

        // ---------- refresh ----------

        private void RefreshAll()
        {
            RefreshCards();
            RefreshQuest();
            RefreshShop();
            _sheetRefresh?.Invoke();
            SaveIfChanged();
        }

        private void RefreshCards()
        {
            if (_hook == null)
            {
                return;
            }

            var now = _hook.Clock.Now;
            foreach (var card in _cards.Values)
            {
                var plot = _hook.Base.Find(card.PlotId);
                if (plot == null)
                {
                    continue;
                }

                if (card.IsCastle)
                {
                    card.Name.text = PlotName(plot.Id) + "  " + Fmt("hook.level", "Lv {0}", plot.Level);
                }
                else if (card.LevelText != null)
                {
                    card.LevelText.text = LevelBadge.Text(plot.Level);
                }

                if (!card.IsCastle)
                {
                    card.Art.color = plot.UnderConstruction ? new Color(1f, 1f, 1f, 0.55f) : Color.white;
                    card.LevelBadge?.gameObject.SetActive(!plot.UnderConstruction);
                }

                card.Hammer?.gameObject.SetActive(plot.UnderConstruction);
                var timerId = BaseEconomy.UpgradeTimerId(plot.Id);
                var timer = _hook.Timers.Find(timerId);
                if (timer != null)
                {
                    var remaining = timer.Remaining(now);
                    var done = remaining <= 0;
                    var free = _hook.Timers.CanFinishFree(timerId);
                    card.TimerRoot.gameObject.SetActive(true);
                    card.Status.text = done
                        ? S("hook.finish", "Finish!")
                        : plot.UnderConstruction ? S("build.building", "Building") + " " + TimerQueue.FormatClock(remaining) : TimerQueue.FormatClock(remaining);
                    HookSkin.SetBar(card.BarFill, (float)timer.Progress(now), done || free ? HookArtKeys.BarFillGreen : HookArtKeys.BarFillBlue, done || free ? FreeGreen : SurvivalVisuals.PinBuild);
                    card.Badge.gameObject.SetActive(done || free);
                    card.BadgeText.text = done ? "!" : S("hook.free", "FREE");
                    card.Bubble.gameObject.SetActive(false);
                    card.Arrow.gameObject.SetActive(false);
                    continue;
                }

                card.TimerRoot.gameObject.SetActive(false);
                card.Badge.gameObject.SetActive(false);
                card.Bubble.gameObject.SetActive(plot.CanCollect);
                if (plot.IsProducer)
                {
                    card.BubbleText.text = "+" + ChipWallet.FormatCompact(plot.Collectable);
                }

                card.Arrow.gameObject.SetActive(!plot.CanCollect && _hook.Base.CheckUpgrade(plot.Id) == UpgradeBlock.None);
            }
        }

        private string BlockLabel(UpgradeBlock block, int nextLevel, string otherwise) => block switch
        {
            UpgradeBlock.MaxLevel => S("hook.max_level", "Max level"),
            UpgradeBlock.NeedsCastle => Fmt("hook.needs_castle", "Needs Castle Lv {0}", nextLevel),
            UpgradeBlock.QueueBusy => S("hook.queue_busy", "Builder busy"),
            _ => otherwise
        };

        private void RefreshQuest()
        {
            if (_hook == null || _quest == null)
            {
                return;
            }

            var quests = _hook.Quests;
            var step = quests.Current;
            if (step == null)
            {
                _quest.Title.text = S("quest.tracker.title", "Quest");
                _quest.Body.text = S("quest.tracker.done", "All starter quests done!");
                _quest.BarBack.gameObject.SetActive(false);
                _quest.Count.text = "";
                _quest.Chest.gameObject.SetActive(false);
                _questPreview?.gameObject.SetActive(false);
                return;
            }

            _quest.Title.text = S("quest.tracker.title", "Quest") + "  " + (quests.Index + 1) + "/" + quests.Chain.Steps.Count;
            _quest.Body.text = S(step.Id + ".title", step.Id);
            _quest.BarBack.gameObject.SetActive(true);
            var frac = step.Count <= 0 ? 1f : Mathf.Clamp01(quests.Progress / (float)step.Count);
            HookSkin.SetBar(_quest.BarFill, frac, quests.IsComplete ? HookArtKeys.BarFillGreen : HookArtKeys.BarFillGold, quests.IsComplete ? FreeGreen : SurvivalVisuals.Reward);
            _quest.Count.text = quests.IsComplete ? "\u2713" : quests.Progress + "/" + step.Count;
            _quest.Chest.gameObject.SetActive(quests.IsComplete);
            _questPreview?.gameObject.SetActive(!quests.IsComplete);

            if (quests.IsComplete && !_questWasComplete)
            {
                StartCoroutine(Punch(_quest.Root, 0.18f, 12));
                StartCoroutine(SparkBurst(NormCenterOf(_quest.Chest), SurvivalVisuals.Reward, 16, 0.08f));
                StartCoroutine(RingBurst(NormCenterOf(_quest.Chest), SurvivalVisuals.Reward, 0.16f));
            }

            _questWasComplete = quests.IsComplete;
        }

        private void RefreshShop()
        {
            if (_hook == null || _freeBadge == null)
            {
                return;
            }

            var free = _hook.FreeAvailable(SurvIds.StoreGemShop);
            _freeBadge.gameObject.SetActive(free);
            _freeGlow?.gameObject.SetActive(free);
        }

        private void AnimateIdle(float t)
        {
            foreach (var card in _cards.Values)
            {
                if (card.Bubble.gameObject.activeSelf)
                {
                    var bob = 1f + 0.07f * Mathf.Sin(t * 5f + card.PlotId.Length);
                    card.Bubble.rectTransform.localScale = new Vector3(bob, bob, 1f);
                    card.Bubble.rectTransform.anchoredPosition = new Vector2(0f, 6f * Mathf.Sin(t * 3.2f + card.PlotId.Length));
                }

                if (card.Badge.gameObject.activeSelf)
                {
                    var throb = 1f + 0.12f * Mathf.Abs(Mathf.Sin(t * 4f));
                    card.Badge.rectTransform.localScale = new Vector3(throb, throb, 1f);
                    card.Badge.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 6f * Mathf.Sin(t * 2.5f));
                }

                if (card.Arrow.gameObject.activeSelf)
                {
                    card.Arrow.rectTransform.anchoredPosition = new Vector2(0f, 10f * Mathf.Abs(Mathf.Sin(t * 3.4f + card.PlotId.Length)));
                }
            }

            if (_freeBadge != null && _freeBadge.gameObject.activeSelf)
            {
                var pulse = 1f + 0.14f * Mathf.Abs(Mathf.Sin(t * 4.5f));
                _freeBadge.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
                _freeBadge.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 8f * Mathf.Sin(t * 2f));
                if (_freeGlow != null)
                {
                    var g = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(t * 4.5f));
                    _freeGlow.color = new Color(0.55f, 1f, 0.45f, g);
                    _freeGlow.rectTransform.localScale = Vector3.one * (0.9f + 0.25f * g);
                }

                if (!HookSkin.Has(HookArtKeys.BadgeFree))
                {
                    _freeBadge.color = Color.Lerp(FreeGreen, new Color(0.65f, 1f, 0.55f, 1f), 0.5f + 0.5f * Mathf.Sin(t * 9f));
                }
            }

            if (_quest != null && _quest.Chest.gameObject.activeSelf)
            {
                var hop = 1f + 0.10f * Mathf.Abs(Mathf.Sin(t * 4f));
                _quest.Chest.localScale = new Vector3(hop, hop, 1f);
                _quest.Chest.localRotation = Quaternion.Euler(0f, 0f, 7f * Mathf.Sin(t * 8f));
                var glow = _quest.Chest.Find("Glow");
                if (glow != null)
                {
                    glow.localRotation = Quaternion.Euler(0f, 0f, -t * 40f);
                    glow.localScale = Vector3.one * (0.9f + 0.15f * Mathf.Sin(t * 5f));
                }
            }
        }

        // ---------- actions ----------

        private void OnTapPlot(string plotId)
        {
            if (_hook == null || !_cards.TryGetValue(plotId, out var card))
            {
                return;
            }

            var plot = _hook.Base.Find(plotId);
            if (plot == null)
            {
                return;
            }

            if (_hook.Base.IsUpgrading(plotId))
            {
                OnPlotAction(plotId);
                return;
            }

            if (!plot.CanCollect)
            {
                OpenBuilding(plotId);
                return;
            }

            Collect(card, plot);
        }

        private void Collect(PlotCard card, BasePlot plot)
        {
            if (_hook == null || !plot.IsProducer)
            {
                return;
            }

            var plotId = plot.Id;
            var res = plot.Def.Produces!;
            var from = NormCenterOf(card.Bubble.gameObject.activeSelf ? card.Bubble.rectTransform : card.Root);
            _held.Add(res);
            var got = _hook.Base.Collect(plotId);
            if (got <= 0)
            {
                _held.Remove(res);
                Toast(NormCenterOf(card.Root), S("hook.filling", "Filling\u2026"), SurvivalVisuals.Mute);
                return;
            }

            FloatText(from, "+" + ChipWallet.FormatGrouped(got) + " " + ResName(res), ResColor(res));
            StartCoroutine(SparkBurst(from, ResColor(res), 10, 0.05f));
            StartCoroutine(FlyReward(res, from, 6));
            RefreshCards();
            RefreshQuest();
        }

        private void OnPlotAction(string plotId)
        {
            if (_hook == null || !_cards.TryGetValue(plotId, out var card))
            {
                return;
            }

            var timerId = BaseEconomy.UpgradeTimerId(plotId);
            if (_hook.Timers.Find(timerId) != null)
            {
                if (_hook.Timers.IsDone(timerId) || _hook.Timers.CanFinishFree(timerId))
                {
                    var wasBuild = _hook.Base.Find(plotId)?.UnderConstruction == true;
                    var finished = _hook.FinishFree(plotId);
                    if (finished > 0)
                    {
                        Finished(card, finished, wasBuild);
                    }
                }
                else
                {
                    OpenSpeedUp(plotId);
                }

                return;
            }

            var block = _hook.Base.CheckUpgrade(plotId);
            if (block == UpgradeBlock.None && _hook.Base.TryStartUpgrade(plotId))
            {
                Toast(NormCenterOf(card.Root), S("hook.upgrading", "Upgrading"), SurvivalVisuals.PinBuild);
                StartCoroutine(SparkBurst(NormCenterOf(card.Root), SurvivalVisuals.PinBuild, 10, 0.06f));
                RefreshCards();
                return;
            }

            var nextLevel = _hook.Base.Find(plotId)?.Next?.Level ?? 0;
            Toast(NormCenterOf(card.Root), BlockLabel(block, nextLevel, S("hook.need_more", "Not enough resources")), Warn);
            StartCoroutine(Shake(card.Root));
        }

        private void LevelUp(PlotCard card, int level)
        {
            var at = NormCenterOf(card.Art.rectTransform);
            StartCoroutine(Punch(card.Root, 0.25f, 16));
            StartCoroutine(LevelBurst(at));
            StartCoroutine(RingBurst(at, SurvivalVisuals.Reward, 0.36f));
            StartCoroutine(RingBurst(at, Color.white, 0.22f));
            StartCoroutine(SparkBurst(at, SurvivalVisuals.Reward, 22, 0.13f));
            FloatText(at + new Vector2(0f, 0.06f), Fmt("hook.level_up", "Level {0}!", level), SurvivalVisuals.Reward, 52);
            if (card.IsCastle && _hook?.Base.Find(card.PlotId) is BasePlot plot)
            {
                var visual = BuildingVisual.For(plot, Tiers);
                if (visual.Tier != card.Tier)
                {
                    card.Tier = visual.Tier;
                    StartCoroutine(GrowCastle(card, visual));
                }
            }
            else if (card.LevelBadge != null)
            {
                StartCoroutine(Punch(card.LevelBadge.rectTransform, 0.5f, 18));
            }

            RefreshAll();
        }

        /// <summary>Castle tier-up: flash, swap to the bigger painting, grow the footprint with an overshoot, confetti.</summary>
        private IEnumerator GrowCastle(PlotCard card, BuildingVisual visual)
        {
            var village = _play?.Village;
            var from = village != null ? village.FootprintScaleOf(card.PlotId) : 1f;
            var glow = card.Root.Find("Glow")?.GetComponent<Image>();
            const float flash = 0.22f;
            for (var e = 0f; e < flash; e += Time.unscaledDeltaTime)
            {
                var u = e / flash;
                card.Art.color = Color.Lerp(Color.white, new Color(1f, 0.95f, 0.75f, 1f), u);
                if (glow != null)
                {
                    glow.color = new Color(1f, 0.86f, 0.45f, 0.9f * u);
                }

                yield return null;
            }

            var sprite = SurvivalArt.Get(visual.ArtKey);
            if (sprite != null)
            {
                card.Art.sprite = sprite;
            }

            var at = NormCenterOf(card.Art.rectTransform);
            StartCoroutine(LevelBurst(at));
            StartCoroutine(RingBurst(at, SurvivalVisuals.Reward, 0.6f));
            StartCoroutine(SparkBurst(at, Color.white, 28, 0.2f));
            if (_overlay != null)
            {
                StartCoroutine(Confetti(_overlay, at, 40, new Vector2(0f, 0.5f)));
            }

            FloatText(at + new Vector2(0f, 0.12f), Fmt2("village.building.castle", "Castle stage {0} of {1}", visual.Tier, Tiers.Count), SurvivalVisuals.Reward, 44);
            const float grow = 0.7f;
            for (var e = 0f; e < grow; e += Time.unscaledDeltaTime)
            {
                var u = Mathf.Clamp01(e / grow);
                var back = 1f + 2.4f * Mathf.Pow(u - 1f, 3f) + 1.4f * Mathf.Pow(u - 1f, 2f); // ease-out-back
                village?.SetFootprint(card.PlotId, Mathf.LerpUnclamped(from, visual.Scale, back));
                card.Art.color = Color.Lerp(new Color(1f, 0.95f, 0.75f, 1f), Color.white, u);
                if (glow != null)
                {
                    glow.color = new Color(1f, 0.86f, 0.45f, 0.9f * (1f - u));
                }

                yield return null;
            }

            village?.SetFootprint(card.PlotId, visual.Scale);
            card.Art.color = Color.white;
            if (glow != null)
            {
                glow.color = new Color(1f, 0.86f, 0.45f, 0f);
            }
        }

        /// <summary>Building card: what it does, its next level (cost, time), Upgrade / Speed Up / Collect.</summary>
        private void OpenBuilding(string plotId)
        {
            if (_hook == null || _hook.Base.Find(plotId) is not BasePlot plot)
            {
                return;
            }

            var hook = _hook;
            var name = PlotName(plotId);
            var panel = OpenSheet("BuildingSheet", 0.86f, 0.44f, name);
            if (panel == null)
            {
                return;
            }

            var isCastle = !LevelBadge.Shows(plot.Def);
            var art = SurvivalVisuals.Image(panel, "Building", Color.white);
            art.preserveAspect = true;
            Box(art.rectTransform, 0.04f, 0.47f, 0.38f, 0.88f);
            var badge = SurvivalVisuals.Image(panel, "Badge", Color.white, SurvivalArt.Get(LevelBadge.ArtKey) ?? SurvivalArt.Circle());
            badge.preserveAspect = true;
            Box(badge.rectTransform, 0.02f, 0.74f, 0.13f, 0.88f);
            var badgeText = HookSkin.Outlined(SurvivalVisuals.Text(badge.transform, "N", "", 34, TextAnchor.MiddleCenter, Color.white), 2f);
            badgeText.fontStyle = FontStyle.Bold;
            Box(badgeText.rectTransform, 0f, 0f, 1f, 0.86f);

            var level = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Level", "", 34, TextAnchor.MiddleLeft, SurvivalVisuals.Reward), 2f);
            level.fontStyle = FontStyle.Bold;
            Box(level.rectTransform, 0.41f, 0.78f, 0.96f, 0.87f);
            var what = HookSkin.Outlined(SurvivalVisuals.Text(panel, "What", "", 23, TextAnchor.UpperLeft, SurvivalVisuals.Cream), 1.5f);
            what.horizontalOverflow = HorizontalWrapMode.Wrap;
            Box(what.rectTransform, 0.41f, 0.62f, 0.96f, 0.77f);
            var next = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Next", "", 22, TextAnchor.UpperLeft, SurvivalVisuals.Cream), 1.5f);
            next.horizontalOverflow = HorizontalWrapMode.Wrap;
            Box(next.rectTransform, 0.41f, 0.45f, 0.96f, 0.62f);
            Image? nextTier = null;
            if (isCastle)
            {
                nextTier = SurvivalVisuals.Image(panel, "NextTier", new Color(1f, 1f, 1f, 0.9f));
                nextTier.preserveAspect = true;
                Box(nextTier.rectTransform, 0.80f, 0.42f, 0.97f, 0.62f);
            }

            var upText = SheetButton(panel, "Upgrade", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, 0.06f, 0.24f, 0.94f, 0.40f, 28, out var up);
            var collectText = SheetButton(panel, "Collect", HookArtKeys.ButtonGreen, FreeGreen, 0.06f, 0.12f, 0.49f, 0.225f, 22, out var collect);
            var closeText = SheetButton(panel, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.51f, 0.12f, 0.94f, 0.225f, 22, out var close);
            closeText.text = S("hook.close", "Close");
            var upImg = up.GetComponent<Image>();
            var upRt = up.GetComponent<RectTransform>();

            Wire(up, () =>
            {
                if (hook.Timers.Find(BaseEconomy.UpgradeTimerId(plotId)) != null)
                {
                    CloseSheet();
                    OnPlotAction(plotId);
                    return;
                }

                if (hook.Base.CheckUpgrade(plotId) == UpgradeBlock.None)
                {
                    CloseSheet();
                }
                else
                {
                    StartCoroutine(Shake(upRt));
                }

                OnPlotAction(plotId);
            });
            Wire(collect, () =>
            {
                if (_cards.TryGetValue(plotId, out var card) && hook.Base.Find(plotId) is BasePlot p)
                {
                    Collect(card, p);
                }
            });
            Wire(close, CloseSheet);

            _sheetRefresh = () =>
            {
                if (hook.Base.Find(plotId) is not BasePlot p)
                {
                    return;
                }

                var visual = BuildingVisual.For(p, Tiers);
                var sprite = SurvivalArt.Get(visual.ArtKey);
                art.sprite = sprite ?? SurvivalArt.Circle();
                badge.gameObject.SetActive(visual.ShowBadge);
                badgeText.text = visual.BadgeText;
                level.text = Fmt("hook.level", "Lv {0}", p.Level);
                if (isCastle)
                {
                    var nextAt = CastleTiers.NextTierLevel(p.Level, Tiers);
                    what.text = Fmt2("village.building.castle", "Castle stage {0} of {1}", visual.Tier, Tiers.Count) + "\n" +
                        (nextAt is int n ? Fmt("village.building.next_tier", "Next stage at Lv {0}", n) : S("village.building.top_tier", "Grand Citadel reached"));
                    if (nextTier != null)
                    {
                        var nt = nextAt is int lv ? SurvivalArt.Get(CastleTiers.For(lv, Tiers).ArtKey) : null;
                        nextTier.gameObject.SetActive(nt != null);
                        nextTier.sprite = nt ?? SurvivalArt.Circle();
                    }
                }
                else if (p.Def.Produces != null)
                {
                    what.text = string.Format(CultureInfo.InvariantCulture, S("village.building.produces", "Produces {0}/h \u00b7 holds {1}"), ChipWallet.FormatCompact(p.Current.PerHour), ChipWallet.FormatCompact(p.Current.Capacity))
                        + "  " + ResName(p.Def.Produces);
                }
                else
                {
                    what.text = Fmt("village.building.forge", "Forges arms: troop attack +{0}%", p.Level * 5);
                }

                var timerId = BaseEconomy.UpgradeTimerId(plotId);
                var timer = hook.Timers.Find(timerId);
                var block = hook.Base.CheckUpgrade(plotId);
                if (timer != null)
                {
                    var left = timer.Remaining(hook.Clock.Now);
                    next.text = S("hook.upgrading", "Upgrading") + "  " + TimerQueue.FormatClock(left);
                    var free = left <= 0 || hook.Timers.CanFinishFree(timerId);
                    upText.text = left <= 0 ? S("hook.finish", "Finish!") : free ? S("hook.speedup.free", "Finish FREE") : S("hook.speedup.title", "Speed Up");
                    var key = free ? HookArtKeys.ButtonGreen : HookArtKeys.ButtonBlue;
                    HookSkin.SetButton(upImg, key, free ? FreeGreen : SurvivalVisuals.PinBuild, true, 0.5f);
                    upText.color = HookSkin.InkFor(key);
                }
                else if (p.Next == null)
                {
                    next.text = S("village.building.max", "Max level reached");
                    upText.text = S("hook.max_level", "Max level");
                    HookSkin.SetButton(upImg, HookArtKeys.ButtonGold, Disabled, false, 0.5f);
                    upText.color = SurvivalVisuals.Cream;
                }
                else
                {
                    next.text = string.Format(CultureInfo.InvariantCulture, S("village.building.next", "Next: Lv {0}  \u00b7  {1}"), p.Next.Level, TimerQueue.FormatClock(p.Current.UpgradeSeconds))
                        + "\n" + CostLine(p.UpgradeCost);
                    var ok = block == UpgradeBlock.None;
                    upText.text = BlockLabel(block, p.Next.Level, S("hook.upgrade", "Upgrade"));
                    HookSkin.SetButton(upImg, HookArtKeys.ButtonGold, ok ? SurvivalVisuals.MineFill : Disabled, true, 0.5f);
                    upText.color = ok ? HookSkin.InkFor(HookArtKeys.ButtonGold) : SurvivalVisuals.Cream;
                }

                collect.gameObject.SetActive(p.CanCollect);
                collectText.text = Fmt("village.building.collect", "Collect +{0}", ChipWallet.FormatCompact(p.Collectable));
                var closeRt = close.GetComponent<RectTransform>();
                closeRt.anchorMin = new Vector2(p.CanCollect ? 0.51f : 0.25f, 0.12f);
                closeRt.anchorMax = new Vector2(p.CanCollect ? 0.94f : 0.75f, 0.225f);
            };
            _sheetRefresh();
        }

        private void OnTapQuest()
        {
            if (_hook == null || _quest == null)
            {
                return;
            }

            var step = _hook.Quests.Current;
            if (step == null || !_hook.Quests.IsComplete)
            {
                return;
            }

            Hold(step.Rewards);
            var rewards = _hook.Quests.Claim(_hook.Wallet);
            if (rewards == null)
            {
                Release(step.Rewards);
                return;
            }

            _questWasComplete = _hook.Quests.IsComplete;
            QueueCelebration(S("quest.claim.title", "Quest complete!"), rewards);
            RefreshQuest();
        }

        private void OnMarchResolved(WorldMarchLoop loop, int yield)
        {
            _hook?.ReportMarch(loop.NodeId, loop.NodeTypeId);
            RefreshQuest();
        }

        private void OnGiftOffered(PendingGift gift)
        {
            if (_shopButton == null)
            {
                return;
            }

            StartCoroutine(Punch(_shopButton, 0.22f, 14));
            Toast(NormCenterOf(_shopButton) + new Vector2(0f, -0.045f), S("gift.claim.title", "Free Gift!"), FreeGreen);
        }

        private void DebugGiftNow()
        {
            if (_hook == null)
            {
                return;
            }

            if (!_hook.Gifts.DebugOfferNow())
            {
                Toast(new Vector2(0.5f, 0.5f), S("offer.free_gift.ready", "Ready to claim!"), FreeGreen);
            }

            RefreshShop();
        }

        private void DebugSkip()
        {
            if (_hook == null)
            {
                return;
            }

            _hook.Clock.Skip(DebugSkipSeconds);
            _hook.Tick();
            Toast(new Vector2(0.5f, 0.5f), "+5 min", SurvivalVisuals.PinBuild);
            RefreshAll();
        }

        // ---------- sheets ----------

        private RectTransform? OpenSheet(string name, float width, float height, string title)
        {
            CloseSheet();
            if (_overlay == null)
            {
                return null;
            }

            var dimBtn = SurvivalVisuals.Button(_overlay, name, Dim);
            SurvivalVisuals.Stretch(dimBtn.GetComponent<RectTransform>());
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(CloseSheet);
            _sheet = dimBtn.gameObject;

            var panel = HookSkin.Panel(dimBtn.transform, "Panel", HookArtKeys.Panel, SurvivalVisuals.InspectFill, 1f);
            panel.raycastTarget = true;
            SurvivalVisuals.AnchorBox(panel.rectTransform, 0.5f, 0.52f, width, height);
            var widthPx = width * 1080f;
            HookSkin.Header(panel.rectTransform, title, Mathf.Min(widthPx * 0.82f, 700f), 118f, 34);
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

        private Text SheetButton(RectTransform panel, string name, string key, Color fallback, float x0, float y0, float x1, float y1, int size, out Button button)
        {
            button = HookSkin.Button(panel, name, key, fallback, 0.5f);
            Box(button.GetComponent<RectTransform>(), x0, y0, x1, y1);
            var label = HookSkin.Outlined(SurvivalVisuals.Text(button.transform, "T", "", size, TextAnchor.MiddleCenter, HookSkin.InkFor(key)), 1.5f);
            label.fontStyle = FontStyle.Bold;
            SurvivalVisuals.Stretch(label.rectTransform);
            return label;
        }

        private void OpenSpeedUp(string plotId)
        {
            if (_hook == null)
            {
                return;
            }

            var panel = OpenSheet("SpeedUpSheet", 0.84f, 0.36f, S("hook.speedup.title", "Speed Up"));
            if (panel == null)
            {
                return;
            }

            var hook = _hook;
            var timerId = BaseEconomy.UpgradeTimerId(plotId);
            var speedPlot = hook.Base.Find(plotId);
            var building = HookSkin.Art(panel, "Building", speedPlot != null ? BuildingVisual.For(speedPlot, Tiers).ArtKey : HookArtKeys.ForPlot(plotId), SurvivalVisuals.PinBuild);
            Box(building.rectTransform, 0.05f, 0.60f, 0.30f, 0.88f);
            var plotName = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Plot", PlotName(plotId), 26, TextAnchor.MiddleLeft, SurvivalVisuals.Cream));
            Box(plotName.rectTransform, 0.33f, 0.78f, 0.95f, 0.87f);
            var hourglass = HookSkin.ResIcon(panel, "Hourglass", SurvIds.ResSpeedupM, ResColor(SurvIds.ResSpeedupM), ResGlyph(SurvIds.ResSpeedupM));
            Box(hourglass.rectTransform, 0.32f, 0.645f, 0.43f, 0.775f);
            var clock = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Clock", "", 46, TextAnchor.MiddleLeft, SurvivalVisuals.Reward), 2.5f);
            clock.fontStyle = FontStyle.Bold;
            Box(clock.rectTransform, 0.45f, 0.64f, 0.95f, 0.78f);
            var bar = HookSkin.Bar(panel, "Bar", out var barFill);
            Box(bar.rectTransform, 0.07f, 0.53f, 0.93f, 0.60f);

            var rushText = SheetButton(panel, "Rush", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, 0.08f, 0.35f, 0.92f, 0.50f, 26, out var rush);
            var gem = HookSkin.ResIcon(rush.transform, "Gem", SurvIds.ResHard, ResColor(SurvIds.ResHard), ResGlyph(SurvIds.ResHard));
            Box(gem.rectTransform, 0.03f, 0.05f, 0.15f, 0.95f);
            var itemsText = SheetButton(panel, "Items", HookArtKeys.ButtonBlue, SurvivalVisuals.PinBuild, 0.08f, 0.19f, 0.92f, 0.33f, 22, out var items);
            var itemIcon = HookSkin.ResIcon(items.transform, "Icon", SurvIds.ResSpeedupM, ResColor(SurvIds.ResSpeedupM), ResGlyph(SurvIds.ResSpeedupM));
            Box(itemIcon.rectTransform, 0.03f, 0.05f, 0.15f, 0.95f);
            var hint = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Hint", S("hook.speedup.free_hint", "Under 5 min left finishes free"), 19, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
            Box(hint.rectTransform, 0f, 0.115f, 1f, 0.18f);
            var closeText = SheetButton(panel, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.33f, 0.03f, 0.67f, 0.115f, 20, out var close);
            closeText.text = S("hook.close", "Close");

            var rushRt = rush.GetComponent<RectTransform>();
            var itemsRt = items.GetComponent<RectTransform>();
            var rushImg = rush.GetComponent<Image>();
            Wire(rush, () => OnRush(plotId, rushRt));
            Wire(items, () =>
            {
                var used = hook.Timers.ApplySpeedups(timerId, hook.Wallet);
                if (used > 0)
                {
                    FloatText(NormCenterOf(itemsRt), "-" + used + " min", SurvivalVisuals.PinBuild);
                    StartCoroutine(SparkBurst(NormCenterOf(bar.rectTransform), SurvivalVisuals.PinBuild, 14, 0.08f));
                }

                RefreshAll();
            });
            Wire(close, CloseSheet);

            _sheetRefresh = () =>
            {
                var timer = hook.Timers.Find(timerId);
                if (timer == null)
                {
                    CloseSheet();
                    return;
                }

                var nowSec = hook.Clock.Now;
                var left = timer.Remaining(nowSec);
                var isFree = hook.Timers.Rules.IsFree(left);
                clock.text = TimerQueue.FormatClock(left);
                clock.color = isFree ? FreeGreen : SurvivalVisuals.Reward;
                HookSkin.SetBar(barFill, (float)timer.Progress(nowSec), isFree ? HookArtKeys.BarFillGreen : HookArtKeys.BarFillBlue, isFree ? FreeGreen : SurvivalVisuals.PinBuild);
                var cost = hook.Timers.RushCost(timerId);
                rushText.text = isFree
                    ? S("hook.speedup.free", "Finish FREE")
                    : Fmt("hook.speedup.rush", "Finish now  \u00b7  {0} Gems", cost);
                var afford = hook.Wallet.CanAfford(SurvIds.ResHard, cost);
                var rushKey = isFree ? HookArtKeys.ButtonGreen : HookArtKeys.ButtonGold;
                HookSkin.SetButton(rushImg, rushKey, isFree ? FreeGreen : afford ? SurvivalVisuals.MineFill : Disabled, isFree || afford, 0.5f);
                rushText.color = HookSkin.InkFor(rushKey);
                gem.gameObject.SetActive(!isFree);
                if (isFree)
                {
                    var pulse = 1f + 0.05f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));
                    rushRt.localScale = new Vector3(pulse, pulse, 1f);
                }

                var have = hook.Wallet.Get(SurvIds.ResSpeedupM);
                itemsText.text = Fmt("hook.speedup.items", "Use speed-ups ({0} min)", have);
                items.gameObject.SetActive(have > 0 && !isFree);
            };
            _sheetRefresh();
        }

        private void OnRush(string plotId, RectTransform button)
        {
            if (_hook == null || !_cards.TryGetValue(plotId, out var card))
            {
                return;
            }

            var timerId = BaseEconomy.UpgradeTimerId(plotId);
            var wasBuild = _hook.Base.Find(plotId)?.UnderConstruction == true;
            var level = _hook.Timers.IsDone(timerId) || _hook.Timers.CanFinishFree(timerId)
                ? _hook.FinishFree(plotId)
                : _hook.Rush(plotId, out _);
            if (level <= 0)
            {
                StartCoroutine(Shake(button));
                Toast(NormCenterOf(button), S("hook.need_more", "Not enough resources"), Warn);
                OpenShop();
                return;
            }

            CloseSheet();
            Finished(card, level, wasBuild);
        }

        private void OpenShop()
        {
            if (_hook == null)
            {
                return;
            }

            var store = _hook.Store.Find(SurvIds.StoreGemShop);
            if (store == null)
            {
                return;
            }

            var panel = OpenSheet("GemShopSheet", 0.92f, 0.56f, S("store.gem_shop.title", "Gem Shop"));
            if (panel == null)
            {
                return;
            }

            var hook = _hook;
            var balanceChip = HookSkin.Panel(panel, "BalanceChip", HookArtKeys.Chip, SurvivalVisuals.PanelSoft, 0.5f);
            Box(balanceChip.rectTransform, 0.30f, 0.855f, 0.70f, 0.905f);
            var balanceIcon = HookSkin.ResIcon(balanceChip.transform, "Gem", SurvIds.ResHard, ResColor(SurvIds.ResHard), ResGlyph(SurvIds.ResHard));
            Box(balanceIcon.rectTransform, 0.02f, -0.2f, 0.22f, 1.2f);
            var balance = HookSkin.Outlined(SurvivalVisuals.Text(balanceChip.transform, "Balance", "", 22, TextAnchor.MiddleCenter, ResColor(SurvIds.ResHard)), 1.5f);
            Box(balance.rectTransform, 0.20f, 0f, 0.98f, 1f);
            var sandbox = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Sandbox", S("store.sandbox", "SANDBOX \u00b7 no real charge"), 15, TextAnchor.MiddleCenter, Warn), 1.5f);
            Box(sandbox.rectTransform, 0f, 0.82f, 1f, 0.85f);

            var refreshers = new List<Action>();
            var rows = store.Offers.Count;
            var rowH = 0.70f / Mathf.Max(1, rows);
            var gemTier = 0;
            for (var i = 0; i < rows; i++)
            {
                var offer = store.Offers[i];
                var top = 0.81f - i * rowH;
                var isGemPack = !string.Equals(offer.Kind, OfferKinds.Free, StringComparison.Ordinal)
                    && string.Equals(FirstKey(offer.Grants), SurvIds.ResHard, StringComparison.Ordinal);
                // The tagged (BEST VALUE) gem pack always gets the biggest pile.
                var tierKey = isGemPack ? HookArtKeys.ForGemTier(string.IsNullOrEmpty(offer.Tag) ? gemTier : 2) : null;
                if (isGemPack)
                {
                    gemTier++;
                }

                refreshers.Add(BuildOfferRow(panel, offer, top - rowH + 0.012f, top, tierKey));
            }

            var closeText = SheetButton(panel, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.35f, 0.025f, 0.65f, 0.085f, 19, out var close);
            closeText.text = S("hook.close", "Close");
            Wire(close, CloseSheet);

            _sheetRefresh = () =>
            {
                balance.text = ChipWallet.FormatGrouped(hook.Wallet.Get(SurvIds.ResHard));
                foreach (var refresh in refreshers)
                {
                    refresh();
                }
            };
            _sheetRefresh();
        }

        private Action BuildOfferRow(RectTransform panel, OfferDef offer, float yMin, float yMax, string? tierKey)
        {
            var isFree = string.Equals(offer.Kind, OfferKinds.Free, StringComparison.Ordinal);
            var tagged = !string.IsNullOrEmpty(offer.Tag);
            if (tagged)
            {
                // Warm halo behind the BEST VALUE row.
                var halo = HookSkin.Glow(panel, "Halo_" + offer.Id, new Color(1f, 0.82f, 0.35f, 0.45f));
                halo.preserveAspect = false;
                var pad = (yMax - yMin) * 0.25f;
                Box(halo.rectTransform, 0.0f, yMin - pad, 1.0f, yMax + pad);
            }

            var row = HookSkin.Panel(panel, "Offer_" + offer.Id, HookArtKeys.Plate, SurvivalVisuals.RowFill, 0.45f);
            Box(row.rectTransform, 0.04f, yMin, 0.96f, yMax);

            var iconRes = isFree ? SurvIds.ResSoft : FirstKey(offer.Grants) ?? SurvIds.ResHard;
            Image icon;
            if (isFree && HookSkin.Has(HookArtKeys.Gift))
            {
                icon = HookSkin.Art(row.transform, "Icon", HookArtKeys.Gift, FreeGreen);
            }
            else if (tierKey != null && HookSkin.Has(tierKey))
            {
                icon = HookSkin.Art(row.transform, "Icon", tierKey, ResColor(iconRes));
            }
            else if (isFree)
            {
                icon = SurvivalVisuals.Circle(row.transform, "Icon", FreeGreen);
                var star = SurvivalVisuals.Text(icon.transform, "G", "\u2605", 24, TextAnchor.MiddleCenter, SurvivalVisuals.Panel);
                SurvivalVisuals.Stretch(star.rectTransform);
            }
            else
            {
                icon = HookSkin.ResIcon(row.transform, "Icon", iconRes, ResColor(iconRes), ResGlyph(iconRes));
            }

            Box(icon.rectTransform, 0.01f, -0.04f, 0.22f, 1.04f);

            var name = HookSkin.Outlined(SurvivalVisuals.Text(row.transform, "Name", S(offer.Id + ".title", offer.Id), 24, TextAnchor.MiddleLeft, SurvivalVisuals.Cream));
            name.fontStyle = FontStyle.Bold;
            Box(name.rectTransform, 0.24f, 0.50f, 0.64f, 0.92f);
            var sub = HookSkin.Outlined(SurvivalVisuals.Text(row.transform, "Sub", isFree ? "" : GrantLine(offer.Grants), 19, TextAnchor.MiddleLeft, isFree ? FreeGreen : ResColor(iconRes)), 1.5f);
            Box(sub.rectTransform, 0.24f, 0.10f, 0.64f, 0.50f);

            if (tagged)
            {
                var ribbonSprite = SurvivalArt.Get(HookArtKeys.RibbonRed);
                var tag = ribbonSprite != null
                    ? SurvivalVisuals.Image(row.transform, "Tag", Color.white, ribbonSprite)
                    : SurvivalVisuals.Image(row.transform, "Tag", new Color(0.85f, 0.25f, 0.30f, 1f));
                Box(tag.rectTransform, 0.24f, 0.72f, 0.76f, 1.24f);
                var tagText = HookSkin.Outlined(SurvivalVisuals.Text(tag.transform, "T", S("offer.tag." + offer.Tag, offer.Tag ?? ""), 18, TextAnchor.MiddleCenter, Color.white), 1.5f);
                tagText.fontStyle = FontStyle.Bold;
                Box(tagText.rectTransform, 0.15f, 0.30f, 0.85f, 0.95f);
            }

            var buyKey = isFree ? HookArtKeys.ButtonGreen : HookArtKeys.ButtonGold;
            var buyText = SheetButton((RectTransform)row.transform, "Buy", buyKey, SurvivalVisuals.MineFill, 0.66f, 0.16f, 0.97f, 0.84f, 22, out var buy);
            var buyRt = buy.GetComponent<RectTransform>();
            var buyImg = buy.GetComponent<Image>();
            Image? freeBadge = null;
            if (isFree)
            {
                freeBadge = HookSkin.Art(buy.transform, "Badge", HookArtKeys.BadgeFree, FreeGreen);
                Box(freeBadge.rectTransform, 0.76f, 0.40f, 1.14f, 1.45f);
                var freeText = HookSkin.Outlined(SurvivalVisuals.Text(freeBadge.transform, "T", S("hook.free", "FREE"), 14, TextAnchor.MiddleCenter, Color.white), 1.5f);
                freeText.fontStyle = FontStyle.Bold;
                SurvivalVisuals.Stretch(freeText.rectTransform);
            }

            Wire(buy, () => OnBuy(offer, buyRt));

            return () =>
            {
                if (_hook == null)
                {
                    return;
                }

                if (isFree)
                {
                    var ready = _hook.FreeAvailable(SurvIds.StoreGemShop);
                    sub.text = ready
                        ? S("offer.free_gift.ready", "Ready to claim!")
                        : _hook.Gifts.CapReached
                            ? S("offer.free_gift.cap", "Come back tomorrow")
                            : Fmt("offer.free_gift.next", "Next gift in {0}", TimerQueue.FormatClock(_hook.Gifts.SecondsUntilNext));
                    buyText.text = ready ? S("hook.claim", "Claim") : S("hook.free", "FREE");
                    HookSkin.SetButton(buyImg, HookArtKeys.ButtonGreen, ready ? FreeGreen : Disabled, ready, 0.5f);
                    if (ready)
                    {
                        var pulse = 1f + 0.06f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));
                        buyRt.localScale = new Vector3(pulse, pulse, 1f);
                        if (!HookSkin.Has(HookArtKeys.ButtonGreen))
                        {
                            buyImg.color = Color.Lerp(FreeGreen, Color.white, 0.25f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)));
                        }
                    }

                    if (freeBadge != null)
                    {
                        freeBadge.gameObject.SetActive(ready);
                        freeBadge.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 10f * Mathf.Sin(Time.unscaledTime * 3f));
                    }

                    return;
                }

                if (string.Equals(offer.Kind, OfferKinds.Sku, StringComparison.Ordinal))
                {
                    var product = _hook.Store.ProductFor(offer.Sku);
                    buyText.text = product != null && product.PriceLabel.Length > 0 ? product.PriceLabel : "\u2014";
                    HookSkin.SetButton(buyImg, buyKey, SurvivalVisuals.MineFill, true, 0.5f);
                    return;
                }

                buyText.text = CostLine(offer.Price);
                var afford = _hook.Store.CanAfford(offer);
                HookSkin.SetButton(buyImg, buyKey, afford ? SurvivalVisuals.MineFill : Disabled, afford, 0.5f);
            };
        }

        private void OnBuy(OfferDef offer, RectTransform button)
        {
            if (_hook == null)
            {
                return;
            }

            if (string.Equals(offer.Kind, OfferKinds.Free, StringComparison.Ordinal))
            {
                var pending = _hook.Gifts.Pending;
                if (pending == null)
                {
                    StartCoroutine(Shake(button));
                    return;
                }

                _held.Add(pending.Resource);
                var gift = _hook.ClaimFreeGift();
                if (gift == null)
                {
                    Release(pending.Resource);
                    return;
                }

                QueueCelebration(S("gift.claim.title", "Free Gift!"), new Dictionary<string, int> { [gift.Resource] = gift.Amount });
                RefreshAll();
                return;
            }

            if (string.Equals(offer.Kind, OfferKinds.Sku, StringComparison.Ordinal))
            {
                Hold(offer.Grants);
                _hook.Store.BuySku(offer.Id, outcome =>
                {
                    if (outcome.Ok)
                    {
                        QueueCelebration(S(offer.Id + ".title", offer.Id), offer.Grants);
                    }
                    else
                    {
                        Release(offer.Grants);
                        StartCoroutine(Shake(button));
                    }

                    RefreshAll();
                });
                return;
            }

            Hold(offer.Grants);
            if (!_hook.Store.TryBuyWithCurrency(offer.Id))
            {
                Release(offer.Grants);
                StartCoroutine(Shake(button));
                Toast(NormCenterOf(button), S("hook.need_more", "Not enough resources"), Warn);
                return;
            }

            QueueCelebration(S(offer.Id + ".title", offer.Id), offer.Grants);
            RefreshAll();
        }

        // ---------- public API: Heroes and Battle Setup screens (SurvivalCampaignView) ----------

        /// <summary>
        /// The chest-burst celebration for another screen: holds the HUD chips for the reward keys, runs
        /// <paramref name="grant"/> (which pays the wallet), then queues the celebration; the chips count up as the
        /// rewards fly in. Keys are resource ids or "shard.{hero id}".
        /// </summary>
        public void Celebrate(string title, IReadOnlyDictionary<string, int> rewards, Action? grant = null)
        {
            Hold(rewards);
            grant?.Invoke();
            QueueCelebration(title, rewards);
        }

        public bool Celebrating => _celebrating;

        /// <summary>Keep the HUD chips for these keys still while another screen pays the wallet; Celebrate counts them up.</summary>
        public void HoldChips(IReadOnlyDictionary<string, int> rewards) => Hold(rewards);

        /// <summary>Let held chips count up to the wallet without a celebration.</summary>
        public void ReleaseChips(IReadOnlyDictionary<string, int> rewards) => Release(rewards);

        /// <summary>Tap juice on a button of another screen: punch plus a ring.</summary>
        public void Juice(RectTransform pressed)
        {
            StartCoroutine(Punch(pressed, 0.12f, 8));
            StartCoroutine(RingBurst(NormCenterOf(pressed), SurvivalVisuals.Cream, 0.10f));
        }

        /// <summary>Spark burst and ring on a rect (level up, star up, unlock).</summary>
        public void Sparks(RectTransform at, Color color, int count = 16)
        {
            var c = NormCenterOf(at);
            StartCoroutine(SparkBurst(c, color, count, 0.08f));
            StartCoroutine(RingBurst(c, color, 0.3f));
        }

        public void Say(RectTransform at, string copy, Color color) => Toast(NormCenterOf(at), copy, color);

        public void Rise(RectTransform at, string copy, Color color) => FloatText(NormCenterOf(at), copy, color);

        /// <summary>Opens the sandbox Gem Shop sheet (above the Heroes screen).</summary>
        public void OpenGemShop() => OpenShop();

        // ---------- celebration ----------

        private void QueueCelebration(string title, IReadOnlyDictionary<string, int> rewards)
        {
            _celebrations.Enqueue(Celebration(title, rewards));
            if (!_celebrating)
            {
                StartCoroutine(RunCelebrations());
            }
        }

        private IEnumerator RunCelebrations()
        {
            _celebrating = true;
            while (_celebrations.Count > 0)
            {
                yield return StartCoroutine(_celebrations.Dequeue());
            }

            _celebrating = false;
        }

        private IEnumerator Celebration(string title, IReadOnlyDictionary<string, int> rewards)
        {
            if (_overlay == null)
            {
                Release(rewards);
                yield break;
            }

            var dim = SurvivalVisuals.Button(_overlay, "Celebration", new Color(Dim.r, Dim.g, Dim.b, 0f));
            dim.transition = Selectable.Transition.None;
            SurvivalVisuals.Stretch(dim.GetComponent<RectTransform>());
            var dimImg = dim.GetComponent<Image>();
            var tapped = false;
            dim.onClick.AddListener(() => tapped = true);
            var host = dim.transform;
            const float cy = 0.55f;

            // Rays: two painted ray wheels turning against each other (procedural quads if the pack has none).
            var rays = new GameObject("Rays", typeof(RectTransform)).GetComponent<RectTransform>();
            rays.SetParent(host, false);
            SurvivalVisuals.AnchorBox(rays, 0.5f, cy, 0f, 0f);
            RectTransform? rays2 = null;
            var raySprite = SurvivalArt.Get(HookArtKeys.FxRays);
            if (raySprite != null)
            {
                var wheel = SurvivalVisuals.Image(rays, "Wheel", new Color(1f, 0.88f, 0.5f, 0f), raySprite);
                wheel.rectTransform.sizeDelta = new Vector2(1500f, 1500f);
                var inner = SurvivalVisuals.Image(host, "Rays2", new Color(1f, 0.95f, 0.75f, 0f), raySprite);
                rays2 = inner.rectTransform;
                SurvivalVisuals.AnchorBox(rays2, 0.5f, cy, 0f, 0f);
                rays2.sizeDelta = new Vector2(950f, 950f);
            }
            else
            {
                for (var r = 0; r < 12; r++)
                {
                    var ray = SurvivalVisuals.Image(rays, "Ray" + r, new Color(1f, 0.86f, 0.40f, 0f));
                    var rayRt = ray.rectTransform;
                    rayRt.anchorMin = new Vector2(0.5f, 0.5f);
                    rayRt.anchorMax = new Vector2(0.5f, 0.5f);
                    rayRt.pivot = new Vector2(0.5f, 0f);
                    rayRt.sizeDelta = new Vector2(70f, 900f);
                    rayRt.localRotation = Quaternion.Euler(0f, 0f, r * 30f);
                }
            }

            var glow = HookSkin.Glow(host, "Glow", new Color(1f, 0.85f, 0.4f, 0f));
            SurvivalVisuals.AnchorBox(glow.rectTransform, 0.5f, cy, 0.9f, 0.9f * Aspect);

            var chestRt = new GameObject("Chest", typeof(RectTransform)).GetComponent<RectTransform>();
            chestRt.SetParent(host, false);
            const float chestW = 0.46f;
            SurvivalVisuals.AnchorBox(chestRt, 0.5f, 0.66f, chestW, chestW * Aspect);
            Image? chestImg = null;
            RectTransform? lid = null;
            var openSprite = SurvivalArt.Get(HookArtKeys.ChestOpen);
            if (HookSkin.Has(HookArtKeys.ChestClosed))
            {
                chestImg = HookSkin.Art(chestRt, "Art", HookArtKeys.ChestClosed, ChestWood);
                SurvivalVisuals.Stretch(chestImg.rectTransform);
            }
            else
            {
                BuildChestArt(chestRt, out lid);
            }

            var titleHost = new GameObject("TitleHost", typeof(RectTransform)).GetComponent<RectTransform>();
            titleHost.SetParent(host, false);
            SurvivalVisuals.AnchorBox(titleHost, 0.5f, 0.765f, 0.94f, 0.085f);
            var ribbon = SurvivalArt.Get(HookArtKeys.RibbonBlue);
            if (ribbon != null)
            {
                var band = SurvivalVisuals.Image(titleHost, "Ribbon", Color.white, ribbon);
                SurvivalVisuals.Stretch(band.rectTransform);
            }

            var titleText = SurvivalVisuals.Text(titleHost, "Title", title, 50, TextAnchor.MiddleCenter, SurvivalVisuals.Reward);
            Box(titleText.rectTransform, 0.12f, 0.22f, 0.88f, 0.98f);
            titleText.fontStyle = FontStyle.Bold;
            HookSkin.Outlined(titleText, 3f);
            titleHost.localScale = Vector3.zero;

            // A: drop in with overshoot.
            var elapsed = 0f;
            while (elapsed < 0.32f)
            {
                elapsed += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(elapsed / 0.32f);
                dimImg.color = new Color(Dim.r, Dim.g, Dim.b, Mathf.Lerp(0f, Dim.a, u));
                var back = 1f + 2.2f * Mathf.Pow(u - 1f, 3f) + 1.2f * Mathf.Pow(u - 1f, 2f);
                SurvivalVisuals.AnchorBox(chestRt, 0.5f, Mathf.Lerp(0.70f, 0.53f, u), chestW, chestW * Aspect);
                chestRt.localScale = Vector3.one * Mathf.Max(0f, back);
                glow.color = new Color(1f, 0.85f, 0.4f, 0.35f * u);
                yield return null;
            }

            chestRt.localScale = Vector3.one;

            // B: shake, building glow.
            elapsed = 0f;
            while (elapsed < 0.48f)
            {
                elapsed += Time.unscaledDeltaTime;
                var k = elapsed / 0.48f;
                chestRt.localRotation = Quaternion.Euler(0f, 0f, 8f * Mathf.Sin(elapsed * 52f) * (0.4f + k));
                chestRt.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(elapsed * 34f));
                glow.color = new Color(1f, 0.85f, 0.4f, Mathf.Lerp(0.35f, 0.9f, k));
                glow.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 0.8f, k);
                yield return null;
            }

            chestRt.localRotation = Quaternion.identity;
            chestRt.localScale = Vector3.one;

            // C: burst: chest opens, white flash, rays, rings, sparkles, three confetti cannons, reward rows.
            if (chestImg != null && openSprite != null)
            {
                chestImg.sprite = openSprite;
            }

            var center = new Vector2(0.5f, 0.54f);
            var flash = SurvivalVisuals.Image(host, "Flash", new Color(1f, 1f, 0.92f, 0.85f));
            SurvivalVisuals.Stretch(flash.rectTransform);
            StartCoroutine(RingBurst(center, SurvivalVisuals.Reward, 0.7f));
            StartCoroutine(RingBurst(center, Color.white, 0.45f));
            StartCoroutine(SparkBurst(center, SurvivalVisuals.Reward, 28, 0.22f));
            StartCoroutine(Confetti(host, center, 110, Vector2.zero));
            StartCoroutine(Confetti(host, new Vector2(0.02f, 0.30f), 45, new Vector2(0.75f, 0.35f)));
            StartCoroutine(Confetti(host, new Vector2(0.98f, 0.30f), 45, new Vector2(-0.75f, 0.35f)));
            var sparkles = Twinkles(host, center, 10);
            var rewardRows = new List<RewardRowView>();
            foreach (var pair in rewards)
            {
                if (rewardRows.Count >= 4)
                {
                    Release(pair.Key);
                    continue;
                }

                rewardRows.Add(new RewardRowView(pair.Key, RewardRow(host, pair.Key, pair.Value)));
            }

            elapsed = 0f;
            while (elapsed < 0.55f)
            {
                elapsed += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(elapsed / 0.55f);
                if (lid != null)
                {
                    lid.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 62f, u));
                    lid.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, 60f, u));
                }

                flash.color = new Color(1f, 1f, 0.92f, Mathf.Lerp(0.85f, 0f, Mathf.Clamp01(u * 3f)));
                var pop = u < 0.3f ? Mathf.Lerp(1.25f, 0.95f, u / 0.3f) : Mathf.Lerp(0.95f, 1f, (u - 0.3f) / 0.7f);
                chestRt.localScale = Vector3.one * pop;
                glow.color = new Color(1f, 0.85f, 0.4f, Mathf.Lerp(1f, 0.7f, u));
                glow.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.25f, u);
                var ts = u < 0.7f ? Mathf.Lerp(0f, 1.15f, u / 0.7f) : Mathf.Lerp(1.15f, 1f, (u - 0.7f) / 0.3f);
                titleHost.localScale = Vector3.one * ts;
                for (var k = 0; k < rewardRows.Count; k++)
                {
                    var rowRt = rewardRows[k].Rt;
                    var y = 0.36f - k * 0.058f;
                    SurvivalVisuals.AnchorBox(rowRt, 0.5f, Mathf.Lerp(0.54f, y, u), 0.66f, 0.05f);
                    rowRt.localScale = Vector3.one * (u < 0.8f ? Mathf.Lerp(0f, 1.15f, u / 0.8f) : Mathf.Lerp(1.15f, 1f, (u - 0.8f) / 0.2f));
                }

                SetRayAlpha(rays, Mathf.Lerp(0f, raySprite != null ? 0.75f : 0.22f, u));
                if (rays2 != null)
                {
                    SetImageAlpha(rays2, Mathf.Lerp(0f, 0.55f, u));
                }

                yield return null;
            }

            Destroy(flash.gameObject);
            var hint = HookSkin.Outlined(SurvivalVisuals.Text(host, "Hint", S("hook.continue", "Tap to continue"), 24, TextAnchor.MiddleCenter, SurvivalVisuals.Cream));
            SurvivalVisuals.AnchorBox(hint.rectTransform, 0.5f, 0.12f, 0.8f, 0.04f);
            tapped = false;
            elapsed = 0f;
            while (!tapped && elapsed < 3.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                rays.localRotation = Quaternion.Euler(0f, 0f, -elapsed * 18f);
                if (rays2 != null)
                {
                    rays2.localRotation = Quaternion.Euler(0f, 0f, elapsed * 30f);
                }

                var blink = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(elapsed * 3f));
                hint.color = new Color(SurvivalVisuals.Cream.r, SurvivalVisuals.Cream.g, SurvivalVisuals.Cream.b, blink);
                glow.rectTransform.localScale = Vector3.one * (1.25f + 0.06f * Mathf.Sin(elapsed * 5f));
                chestRt.anchoredPosition = new Vector2(0f, 6f * Mathf.Sin(elapsed * 3.5f));
                AnimateTwinkles(sparkles, elapsed);
                yield return null;
            }

            // D: rewards fly to the HUD, overlay fades.
            foreach (var row in rewardRows)
            {
                StartCoroutine(FlyReward(row.Resource, NormCenterOf(row.Rt), 12));
                row.Rt.gameObject.SetActive(false);
            }

            hint.gameObject.SetActive(false);
            elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(elapsed / 0.3f);
                dimImg.color = new Color(Dim.r, Dim.g, Dim.b, Mathf.Lerp(Dim.a, 0f, u));
                chestRt.localScale = Vector3.one * Mathf.Lerp(1f, 0f, u);
                titleHost.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, u);
                var tc = titleText.color;
                titleText.color = new Color(tc.r, tc.g, tc.b, 1f - u);
                glow.color = new Color(1f, 0.85f, 0.4f, Mathf.Lerp(0.7f, 0f, u));
                SetRayAlpha(rays, Mathf.Lerp(raySprite != null ? 0.75f : 0.22f, 0f, u));
                if (rays2 != null)
                {
                    SetImageAlpha(rays2, Mathf.Lerp(0.55f, 0f, u));
                }

                foreach (var s in sparkles)
                {
                    SetImageAlpha(s, 1f - u);
                }

                yield return null;
            }

            Destroy(dim.gameObject);
        }

        private sealed class RewardRowView
        {
            public RewardRowView(string resource, RectTransform rt)
            {
                Resource = resource;
                Rt = rt;
            }

            public string Resource { get; }
            public RectTransform Rt { get; }
        }

        private static void SetRayAlpha(RectTransform rays, float alpha)
        {
            for (var i = 0; i < rays.childCount; i++)
            {
                var img = rays.GetChild(i).GetComponent<Image>();
                if (img != null)
                {
                    var c = img.color;
                    img.color = new Color(c.r, c.g, c.b, alpha);
                }
            }
        }

        private static void SetImageAlpha(RectTransform rt, float alpha)
        {
            var img = rt.GetComponent<Image>();
            if (img != null)
            {
                var c = img.color;
                img.color = new Color(c.r, c.g, c.b, alpha);
            }
        }

        /// <summary>Twinkling sparkle stars scattered around the chest.</summary>
        private static List<RectTransform> Twinkles(Transform host, Vector2 around, int count)
        {
            var list = new List<RectTransform>();
            var sprite = SurvivalArt.Get(HookArtKeys.FxSparkle);
            for (var i = 0; i < count; i++)
            {
                var img = sprite != null
                    ? SurvivalVisuals.Image(host, "Twinkle" + i, new Color(1f, 0.95f, 0.75f, 1f), sprite)
                    : SurvivalVisuals.Circle(host, "Twinkle" + i, new Color(1f, 0.95f, 0.75f, 1f));
                var ang = i / (float)count * Mathf.PI * 2f + 0.4f;
                var dist = 0.20f + 0.08f * ((i * 37) % 5) / 4f;
                var p = around + new Vector2(Mathf.Cos(ang) * dist, Mathf.Sin(ang) * dist * Aspect * 1.3f);
                var size = 0.05f + 0.03f * (i % 3);
                SurvivalVisuals.AnchorBox(img.rectTransform, p.x, p.y, size, size * Aspect);
                list.Add(img.rectTransform);
            }

            return list;
        }

        private static void AnimateTwinkles(List<RectTransform> sparkles, float t)
        {
            for (var i = 0; i < sparkles.Count; i++)
            {
                var s = Mathf.Max(0f, Mathf.Sin(t * 4.2f + i * 1.7f));
                sparkles[i].localScale = Vector3.one * (0.3f + 0.9f * s);
                sparkles[i].localRotation = Quaternion.Euler(0f, 0f, t * 90f + i * 20f);
            }
        }

        private RectTransform RewardRow(Transform host, string res, int amount)
        {
            var chipSprite = SurvivalArt.Get(HookArtKeys.Chip);
            var row = chipSprite != null
                ? SurvivalVisuals.Image(host, "Reward_" + res, Color.white, chipSprite).rectTransform
                : new GameObject("Reward_" + res, typeof(RectTransform)).GetComponent<RectTransform>();
            if (chipSprite != null)
            {
                row.SetParent(host, false);
                HookSkin.ApplySliced(row.GetComponent<Image>(), 0.6f);
            }
            else
            {
                row.SetParent(host, false);
            }

            var icon = HookSkin.ResIcon(row, "Icon", res, ResColor(res), ResGlyph(res));
            Box(icon.rectTransform, 0.02f, -0.35f, 0.24f, 1.35f);
            var label = HookSkin.Outlined(SurvivalVisuals.Text(row, "T", "+" + ChipWallet.FormatGrouped(amount) + " " + ResName(res), 36, TextAnchor.MiddleLeft, SurvivalVisuals.Reward), 2.5f);
            label.fontStyle = FontStyle.Bold;
            Box(label.rectTransform, 0.27f, 0f, 1f, 1f);
            row.localScale = Vector3.zero;
            return row;
        }

        /// <summary>Fallback chest drawn from UI quads. <paramref name="lid"/> pivots at its bottom-left hinge.</summary>
        private static void BuildChestArt(RectTransform host, out RectTransform? lid)
        {
            var body = SurvivalVisuals.Image(host, "Body", ChestWood);
            Box(body.rectTransform, 0.05f, 0f, 0.95f, 0.60f);
            var band = SurvivalVisuals.Image(body.transform, "Band", SurvivalVisuals.Gold);
            Box(band.rectTransform, 0f, 0.62f, 1f, 0.74f);
            var lockImg = SurvivalVisuals.Image(body.transform, "Lock", SurvivalVisuals.Reward);
            Box(lockImg.rectTransform, 0.42f, 0.40f, 0.58f, 0.86f);
            var lidImg = SurvivalVisuals.Image(host, "Lid", ChestLid);
            lidImg.rectTransform.pivot = new Vector2(0f, 0f);
            Box(lidImg.rectTransform, 0f, 0.58f, 1f, 0.92f);
            var lidBand = SurvivalVisuals.Image(lidImg.transform, "Band", SurvivalVisuals.Gold);
            Box(lidBand.rectTransform, 0f, 0f, 1f, 0.22f);
            lid = lidImg.rectTransform;
        }

        private IEnumerator Confetti(Transform host, Vector2 origin, int count, Vector2 bias)
        {
            var bits = new List<Confetto>();
            var sprite = SurvivalArt.Get(HookArtKeys.FxConfetti);
            for (var i = 0; i < count; i++)
            {
                var img = SurvivalVisuals.Image(host, "Confetti" + i, ConfettiColors[i % ConfettiColors.Length], sprite);
                var rt = img.rectTransform;
                rt.anchorMin = origin;
                rt.anchorMax = origin;
                var len = UnityEngine.Random.Range(22f, 40f);
                rt.sizeDelta = i % 4 == 0 ? new Vector2(16f, 16f) : new Vector2(len * 0.45f, len);
                var spread = bias == Vector2.zero
                    ? new Vector2(UnityEngine.Random.Range(-0.7f, 0.7f), UnityEngine.Random.Range(0.5f, 1.45f))
                    : bias + new Vector2(UnityEngine.Random.Range(-0.25f, 0.25f), UnityEngine.Random.Range(0.3f, 1.0f));
                bits.Add(new Confetto
                {
                    Rt = rt,
                    Img = img,
                    Pos = origin,
                    Vel = spread,
                    Spin = UnityEngine.Random.Range(-620f, 620f),
                    Life = UnityEngine.Random.Range(1.6f, 2.8f)
                });
            }

            var age = 0f;
            while (bits.Count > 0)
            {
                var dt = Time.unscaledDeltaTime;
                age += dt;
                for (var i = bits.Count - 1; i >= 0; i--)
                {
                    var bit = bits[i];
                    if (bit.Rt == null)
                    {
                        bits.RemoveAt(i);
                        continue;
                    }

                    bit.Vel += new Vector2(-bit.Vel.x * 1.1f * dt, -1.35f * dt);
                    var flutter = Mathf.Sin(age * 7f + i) * 0.04f * Mathf.Clamp01(age);
                    bit.Pos += new Vector2((bit.Vel.x + flutter) * dt, bit.Vel.y * dt * 0.75f);
                    bit.Rt.anchorMin = bit.Pos;
                    bit.Rt.anchorMax = bit.Pos;
                    bit.Rt.localRotation = Quaternion.Euler(0f, 0f, bit.Spin * age);
                    bit.Rt.localScale = new Vector3(Mathf.Cos(age * 9f + i), 1f, 1f);
                    var fade = Mathf.Clamp01((bit.Life - age) / 0.4f);
                    var c = bit.Img.color;
                    bit.Img.color = new Color(c.r, c.g, c.b, fade);
                    if (age >= bit.Life)
                    {
                        Destroy(bit.Rt.gameObject);
                        bits.RemoveAt(i);
                    }
                }

                yield return null;
            }
        }

        // ---------- juice ----------

        private void Wire(Button button, Action onTap)
        {
            button.onClick.AddListener(() =>
            {
                if (button.transform is RectTransform pressed)
                {
                    StartCoroutine(Punch(pressed, 0.12f, 8));
                    StartCoroutine(RingBurst(NormCenterOf(pressed), SurvivalVisuals.Cream, 0.10f));
                }

                onTap();
            });
        }

        private void Hold(IReadOnlyDictionary<string, int> rewards)
        {
            foreach (var key in rewards.Keys)
            {
                _held.Add(key);
            }
        }

        private void Release(IReadOnlyDictionary<string, int> rewards)
        {
            foreach (var key in rewards.Keys)
            {
                Release(key);
            }
        }

        private void Release(string resourceId)
        {
            _held.Remove(resourceId);
            var chip = ChipWallet.ChipFor(resourceId);
            if (chip != null && _hook != null)
            {
                CountTo(chip, _hook.Wallet.Get(resourceId));
            }
        }

        private void OnWalletChanged(string resourceId, int amount, int delta)
        {
            if (_held.Contains(resourceId))
            {
                return;
            }

            var chip = ChipWallet.ChipFor(resourceId);
            if (chip != null)
            {
                CountTo(chip, amount);
            }
        }

        private void CountTo(string chip, int target)
        {
            if (_counting.TryGetValue(chip, out var running) && running != null)
            {
                StopCoroutine(running);
            }

            var from = _shown.TryGetValue(chip, out var shownNow) ? shownNow : target;
            if (from == target)
            {
                _counting.Remove(chip);
                var text = _play?.ChipText(chip);
                if (text != null)
                {
                    text.text = ChipWallet.FormatCompact(target);
                }

                return;
            }

            _counting[chip] = StartCoroutine(CountUp(chip, from, target));
        }

        private IEnumerator CountUp(string chip, int from, int to)
        {
            var text = _play?.ChipText(chip);
            if (text == null)
            {
                _shown[chip] = to;
                yield break;
            }

            var gain = to > from;
            if (gain && text.transform.parent is RectTransform chipHost)
            {
                StartCoroutine(Punch(chipHost, 0.16f, 10));
            }

            const int frames = 22;
            for (var i = 1; i <= frames; i++)
            {
                var u = i / (float)frames;
                var ease = 1f - (1f - u) * (1f - u);
                var value = Mathf.RoundToInt(Mathf.Lerp(from, to, ease));
                _shown[chip] = value;
                text.text = ChipWallet.FormatCompact(value);
                text.color = gain
                    ? Color.Lerp(SurvivalVisuals.Reward, SurvivalVisuals.Cream, u)
                    : Color.Lerp(Warn, SurvivalVisuals.Cream, u);
                yield return null;
            }

            _shown[chip] = to;
            text.text = ChipWallet.FormatCompact(to);
            text.color = SurvivalVisuals.Cream;
            _counting.Remove(chip);
        }

        /// <summary>Coins arc from <paramref name="from"/> to the resource's HUD chip; the chip counts up as the first lands.</summary>
        private IEnumerator FlyReward(string resourceId, Vector2 from, int coins)
        {
            if (_overlay == null)
            {
                Release(resourceId);
                yield break;
            }

            var target = FlyTarget(resourceId);
            var color = ResColor(resourceId);
            var iconSprite = SurvivalArt.Get(HookArtKeys.ForResource(resourceId));
            var flying = new List<RectTransform?>();
            for (var i = 0; i < coins; i++)
            {
                Image coin;
                if (iconSprite != null)
                {
                    coin = SurvivalVisuals.Image(_overlay, "Coin", Color.white, iconSprite);
                    coin.preserveAspect = true;
                }
                else
                {
                    coin = SurvivalVisuals.Circle(_overlay, "Coin", color);
                    var rim = SurvivalVisuals.Ring(coin.transform, "Rim", Color.white);
                    SurvivalVisuals.Stretch(rim.rectTransform);
                }

                SurvivalVisuals.AnchorBox(coin.rectTransform, from.x, from.y, CoinSize, CoinSize * Aspect);
                flying.Add(coin.rectTransform);
            }

            const float flight = 0.55f;
            const float stagger = 0.05f;
            var total = flight + stagger * coins;
            var elapsed = 0f;
            var released = false;
            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                for (var i = 0; i < flying.Count; i++)
                {
                    var coinRt = flying[i];
                    if (coinRt == null)
                    {
                        continue;
                    }

                    var u = Mathf.Clamp01((elapsed - i * stagger) / flight);
                    var spread = new Vector2(Mathf.Cos(i * 2.4f) * 0.06f, Mathf.Sin(i * 2.4f) * 0.035f);
                    var start = from + spread * Mathf.Clamp01(u * 4f);
                    var control = Vector2.Lerp(start, target, 0.35f) + new Vector2(0f, 0.12f);
                    var e = u * u;
                    var p = (1f - e) * (1f - e) * start + 2f * (1f - e) * e * control + e * e * target;
                    var size = CoinSize * Mathf.Lerp(1.2f, 0.6f, u);
                    SurvivalVisuals.AnchorBox(coinRt, p.x, p.y, size, size * Aspect);
                    coinRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(elapsed * 9f + i) * 18f);
                    if (u >= 1f)
                    {
                        Destroy(coinRt.gameObject);
                        flying[i] = null;
                        if (!released)
                        {
                            released = true;
                            Release(resourceId);
                        }
                    }
                }

                yield return null;
            }

            foreach (var leftover in flying)
            {
                if (leftover != null)
                {
                    Destroy(leftover.gameObject);
                }
            }

            if (!released)
            {
                Release(resourceId);
            }

            StartCoroutine(RingBurst(target, color, 0.08f));
        }

        private Vector2 FlyTarget(string resourceId)
        {
            var chip = ChipWallet.ChipFor(resourceId);
            var text = chip == null ? null : _play?.ChipText(chip);
            if (text != null && text.transform.parent is RectTransform chipHost)
            {
                return NormCenterOf(chipHost);
            }

            if (string.Equals(resourceId, SurvIds.ResSpeedupM, StringComparison.Ordinal)
                && _cards.TryGetValue(SurvIds.BasePlotHq01, out var castle))
            {
                return NormCenterOf(castle.Root);
            }

            return new Vector2(0.5f, 0.95f);
        }

        /// <summary>Level-up: a gold ray wheel and glow flare behind the card, then fade.</summary>
        private IEnumerator LevelBurst(Vector2 at)
        {
            if (_overlay == null)
            {
                yield break;
            }

            var raySprite = SurvivalArt.Get(HookArtKeys.FxRays);
            var glow = HookSkin.Glow(_overlay, "LevelGlow", new Color(1f, 0.85f, 0.4f, 0f));
            SurvivalVisuals.AnchorBox(glow.rectTransform, at.x, at.y, 0.6f, 0.6f * Aspect);
            Image? rays = null;
            if (raySprite != null)
            {
                rays = SurvivalVisuals.Image(_overlay, "LevelRays", new Color(1f, 0.9f, 0.55f, 0f), raySprite);
                rays.preserveAspect = true;
                SurvivalVisuals.AnchorBox(rays.rectTransform, at.x, at.y, 0.7f, 0.7f * Aspect);
            }

            const float seconds = 0.9f;
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(elapsed / seconds);
                var a = u < 0.2f ? u / 0.2f : 1f - (u - 0.2f) / 0.8f;
                glow.color = new Color(1f, 0.85f, 0.4f, 0.9f * a);
                glow.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.3f, u);
                if (rays != null)
                {
                    rays.color = new Color(1f, 0.9f, 0.55f, 0.8f * a);
                    rays.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.2f, u);
                    rays.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -u * 70f);
                }

                yield return null;
            }

            Destroy(glow.gameObject);
            if (rays != null)
            {
                Destroy(rays.gameObject);
            }
        }

        private IEnumerator RingBurst(Vector2 at, Color color, float size)
        {
            if (_overlay == null)
            {
                yield break;
            }

            var ringSprite = SurvivalArt.Get(HookArtKeys.FxBurstRing);
            var ring = ringSprite != null
                ? SurvivalVisuals.Image(_overlay, "TapRing", color, ringSprite)
                : SurvivalVisuals.Ring(_overlay, "TapRing", color);
            ring.preserveAspect = true;
            SurvivalVisuals.AnchorBox(ring.rectTransform, at.x, at.y, size, size * Aspect);
            const int frames = 14;
            for (var i = 0; i < frames; i++)
            {
                var u = i / (frames - 1f);
                ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.8f, u);
                ring.color = new Color(color.r, color.g, color.b, 1f - u);
                yield return null;
            }

            Destroy(ring.gameObject);
        }

        private IEnumerator SparkBurst(Vector2 at, Color color, int count, float radius)
        {
            if (_overlay == null)
            {
                yield break;
            }

            var sparks = new List<Image>();
            var dirs = new List<Vector2>();
            var sparkSprite = SurvivalArt.Get(HookArtKeys.FxSparkle);
            var sparkScale = sparkSprite != null ? 2.4f : 1f;
            for (var i = 0; i < count; i++)
            {
                var ang = i / (float)count * Mathf.PI * 2f + UnityEngine.Random.Range(-0.2f, 0.2f);
                var spark = sparkSprite != null
                    ? SurvivalVisuals.Image(_overlay, "Spark", color, sparkSprite)
                    : SurvivalVisuals.Circle(_overlay, "Spark", color);
                spark.preserveAspect = true;
                sparks.Add(spark);
                // Square-ish spread in pixels: y in normalized height is scaled by the aspect.
                dirs.Add(new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * Aspect));
            }

            const int frames = 18;
            for (var f = 0; f < frames; f++)
            {
                var u = f / (frames - 1f);
                var dist = radius * (1f - (1f - u) * (1f - u));
                for (var i = 0; i < sparks.Count; i++)
                {
                    var p = at + dirs[i] * dist;
                    var s = 0.018f * sparkScale * (1f - u * 0.6f);
                    SurvivalVisuals.AnchorBox(sparks[i].rectTransform, p.x, p.y, s, s * Aspect);
                    sparks[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, u * 120f);
                    sparks[i].color = new Color(color.r, color.g, color.b, 1f - u);
                }

                yield return null;
            }

            foreach (var spark in sparks)
            {
                Destroy(spark.gameObject);
            }
        }

        private void FloatText(Vector2 at, string copy, Color color, int size = 34)
        {
            if (_overlay == null)
            {
                return;
            }

            var text = SurvivalVisuals.Text(_overlay, "Float", copy, size, TextAnchor.MiddleCenter, color);
            var floatOutline = text.gameObject.AddComponent<Outline>();
            floatOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            floatOutline.effectDistance = new Vector2(2f, -2f);
            StartCoroutine(Rise(text, at, 0.07f, 0.9f));
        }

        private void Toast(Vector2 at, string copy, Color color) => FloatText(at, copy, color, 26);

        private static IEnumerator Rise(Text text, Vector2 at, float rise, float seconds)
        {
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(elapsed / seconds);
                SurvivalVisuals.AnchorBox(text.rectTransform, at.x, at.y + rise * u, 0.7f, 0.05f);
                var k = u < 0.15f ? Mathf.Lerp(0.6f, 1.15f, u / 0.15f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((u - 0.15f) / 0.2f));
                text.rectTransform.localScale = new Vector3(k, k, 1f);
                var c = text.color;
                text.color = new Color(c.r, c.g, c.b, u < 0.65f ? 1f : Mathf.Lerp(1f, 0f, (u - 0.65f) / 0.35f));
                yield return null;
            }

            Destroy(text.gameObject);
        }

        private static IEnumerator Punch(RectTransform rt, float amount, int frames)
        {
            for (var i = 0; i < frames; i++)
            {
                if (rt == null)
                {
                    yield break;
                }

                var k = 1f + amount * Mathf.Sin(i / (float)frames * Mathf.PI);
                rt.localScale = new Vector3(k, k, 1f);
                yield return null;
            }

            if (rt != null)
            {
                rt.localScale = Vector3.one;
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

        private static IEnumerator Shake(RectTransform rt)
        {
            const int frames = 14;
            for (var i = 0; i < frames; i++)
            {
                if (rt == null)
                {
                    yield break;
                }

                rt.localRotation = Quaternion.Euler(0f, 0f, 5f * Mathf.Sin(i * 1.9f) * (1f - i / (float)frames));
                yield return null;
            }

            if (rt != null)
            {
                rt.localRotation = Quaternion.identity;
            }
        }

        // ---------- debug input (Input System + legacy + IMGUI) ----------

        private static bool DebugAllowed => Application.isEditor || Debug.isDebugBuild;

        private void PollDebugKeys()
        {
            if (!DebugAllowed)
            {
                return;
            }

            var gift = false;
            var skip = false;
            var reset = false;
            var kb = Keyboard.current;
            if (kb != null && kb.enabled)
            {
                gift |= kb.gKey.wasPressedThisFrame;
                skip |= kb.tKey.wasPressedThisFrame;
                reset |= kb.rKey.wasPressedThisFrame;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            gift |= Input.GetKeyDown(KeyCode.G);
            skip |= Input.GetKeyDown(KeyCode.T);
            reset |= Input.GetKeyDown(KeyCode.R);
#endif
            FireDebug(gift, skip, reset);
        }

        private void OnGUI()
        {
            if (!DebugAllowed)
            {
                return;
            }

            var ev = Event.current;
            if (ev == null || ev.type != EventType.KeyDown)
            {
                return;
            }

            FireDebug(ev.keyCode == KeyCode.G, ev.keyCode == KeyCode.T, ev.keyCode == KeyCode.R);
        }

        private void FireDebug(bool gift, bool skip, bool reset = false)
        {
            if ((!gift && !skip && !reset) || _lastDebugFrame == Time.frameCount)
            {
                return;
            }

            _lastDebugFrame = Time.frameCount;
            if (gift)
            {
                DebugGiftNow();
            }

            if (skip)
            {
                DebugSkip();
            }

            if (reset)
            {
                DebugResetVillage();
            }
        }

        // ---------- helpers ----------

        private Vector2 NormCenterOf(RectTransform rt)
        {
            var space = _overlay != null ? _overlay : _play?.Root;
            if (space == null || rt == null)
            {
                return new Vector2(0.5f, 0.5f);
            }

            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            var mid = (corners[0] + corners[2]) * 0.5f;
            var local = space.InverseTransformPoint(mid);
            var rect = space.rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return new Vector2(0.5f, 0.5f);
            }

            return new Vector2((local.x - rect.xMin) / rect.width, (local.y - rect.yMin) / rect.height);
        }

        private static void Box(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetBar(Image fill, float fraction, Color color)
        {
            var rt = fill.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            fill.color = color;
        }

        private string S(string key, string fallback) => _pack != null ? _pack.StringOr(key, fallback) : fallback;

        private string Fmt2(string key, string fallback, object a, object b) =>
            string.Format(CultureInfo.InvariantCulture, S(key, fallback), a, b);

        private string Fmt(string key, string fallback, object arg) =>
            string.Format(CultureInfo.InvariantCulture, S(key, fallback), arg);

        private string ResName(string resourceId) =>
            resourceId.StartsWith(ShardPrefix, StringComparison.Ordinal)
                ? string.Format(CultureInfo.InvariantCulture, S("campaign.shards_of", "{0} shards"), S(resourceId.Substring(ShardPrefix.Length) + ".name", resourceId.Substring(ShardPrefix.Length)))
                : S(resourceId + ".name", resourceId);

        private const string ShardPrefix = "shard.";

        private string CostLine(IReadOnlyDictionary<string, int> costs)
        {
            var parts = new List<string>();
            foreach (var pair in costs)
            {
                parts.Add(ChipWallet.FormatCompact(pair.Value) + " " + ResName(pair.Key));
            }

            return string.Join("  ", parts);
        }

        private string GrantLine(IReadOnlyDictionary<string, int> grants)
        {
            var parts = new List<string>();
            foreach (var pair in grants)
            {
                parts.Add("+" + ChipWallet.FormatGrouped(pair.Value) + " " + ResName(pair.Key));
            }

            return string.Join("  ", parts);
        }

        private static string? FirstKey(IReadOnlyDictionary<string, int> map)
        {
            foreach (var key in map.Keys)
            {
                return key;
            }

            return null;
        }

        private static Color ResColor(string resourceId) => resourceId.StartsWith(ShardPrefix, StringComparison.Ordinal) ? new Color(0.72f, 0.52f, 1f, 1f) : resourceId switch
        {
            SurvIds.ResSoft => new Color(0.97f, 0.78f, 0.25f, 1f),
            SurvIds.ResStone => new Color(0.72f, 0.74f, 0.78f, 1f),
            SurvIds.ResWood => new Color(0.70f, 0.47f, 0.24f, 1f),
            SurvIds.ResFood => new Color(0.56f, 0.82f, 0.32f, 1f),
            SurvIds.ResHard => new Color(0.42f, 0.86f, 1f, 1f),
            SurvIds.ResSpeedupM => new Color(0.48f, 0.66f, 1f, 1f),
            SurvIds.ResEnergy => new Color(1f, 0.88f, 0.30f, 1f),
            _ => SurvivalVisuals.Cream
        };

        private static string ResGlyph(string resourceId) => resourceId.StartsWith(ShardPrefix, StringComparison.Ordinal) ? "\u2726" : resourceId switch
        {
            SurvIds.ResSoft => "G",
            SurvIds.ResStone => "S",
            SurvIds.ResWood => "W",
            SurvIds.ResFood => "F",
            SurvIds.ResHard => "\u25c6",
            SurvIds.ResSpeedupM => "\u00bb",
            SurvIds.ResEnergy => "E",
            _ => "?"
        };
    }
}
