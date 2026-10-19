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
    /// World + quest "hook" slice on top of the Play shell: Castle / Farm / Lumber Mill cards (tap to collect,
    /// timed upgrades, gem rush, free finish), the starter quest tracker with chest claim, the free gift,
    /// the sandbox Gem Shop with its pulsing FREE badge, and the chest-burst celebration. Juice on every tap.
    /// Debug (Editor / dev builds): G = free gift now, T = skip 5 min (also on-screen buttons).
    /// </summary>
    public sealed class SurvivalHookView : MonoBehaviour
    {
        private const float Aspect = 1080f / 1920f;
        private const float DebugSkipSeconds = 300f;

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
        private GameObject? _sheet;
        private Action? _sheetRefresh;
        private bool _celebrating;
        private int _lastDebugFrame = -1;
        private float _uiTick;

        private sealed class PlotCard
        {
            public string PlotId = "";
            public RectTransform Root = null!;
            public Text Level = null!;
            public Image Bubble = null!;
            public Text BubbleText = null!;
            public Image BarFill = null!;
            public Text Status = null!;
            public Image Action = null!;
            public Text ActionText = null!;
            public Image Badge = null!;
            public Text BadgeText = null!;
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
            BuildBaseStrip(root);
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
                _hook.Wallet.Changed -= OnWalletChanged;
                _hook.Gifts.Offered -= OnGiftOffered;
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
        }

        // ---------- build ----------

        private void BuildBaseStrip(RectTransform root)
        {
            if (_hook == null || _pack == null || !_pack.Hud.TryGetValue("base_strip", out var stripRect))
            {
                return;
            }

            var strip = SurvivalVisuals.Image(root, "BaseStrip", new Color(0f, 0f, 0f, 0f));
            SurvivalVisuals.Place(strip.rectTransform, stripRect);
            var plots = _hook.Base.Plots;
            for (var i = 0; i < plots.Count; i++)
            {
                var plot = plots[i];
                var x0 = i / (float)plots.Count;
                var x1 = (i + 1) / (float)plots.Count;
                _cards[plot.Id] = BuildCard(strip.rectTransform, plot, x0, x1);
            }
        }

        private PlotCard BuildCard(RectTransform strip, BasePlot plot, float x0, float x1)
        {
            var plotId = plot.Id;
            var frame = SurvivalVisuals.Framed(strip, "Plot_" + plotId, SurvivalVisuals.InspectFill, SurvivalVisuals.GoldLine, 2f);
            frame.raycastTarget = true;
            var body = frame.gameObject.AddComponent<Button>();
            body.targetGraphic = frame;
            var rt = frame.rectTransform;
            rt.anchorMin = new Vector2(x0 + 0.006f, 0f);
            rt.anchorMax = new Vector2(x1 - 0.006f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var card = new PlotCard { PlotId = plotId, Root = rt };
            var title = SurvivalVisuals.Text(rt, "Title", S("hook.plot." + plotId, plotId), 20, TextAnchor.MiddleLeft, SurvivalVisuals.Cream);
            Box(title.rectTransform, 0.05f, 0.74f, 0.70f, 0.97f);
            card.Level = SurvivalVisuals.Text(rt, "Level", "", 18, TextAnchor.MiddleRight, SurvivalVisuals.Gold);
            Box(card.Level.rectTransform, 0.60f, 0.74f, 0.95f, 0.97f);

            Image icon;
            if (plot.Def.Produces == null)
            {
                icon = SurvivalVisuals.Image(rt, "Icon", Color.white, SurvivalArt.Get(SurvIds.ThemeABuildingHqKeep));
                icon.preserveAspect = true;
            }
            else
            {
                icon = SurvivalVisuals.Circle(rt, "Icon", ResColor(plot.Def.Produces));
                var glyph = SurvivalVisuals.Text(icon.transform, "Glyph", ResGlyph(plot.Def.Produces), 26, TextAnchor.MiddleCenter, SurvivalVisuals.Panel);
                SurvivalVisuals.Stretch(glyph.rectTransform);
            }

            Box(icon.rectTransform, 0.04f, 0.31f, 0.33f, 0.74f);

            card.Status = SurvivalVisuals.Text(rt, "Status", "", 15, TextAnchor.MiddleLeft, SurvivalVisuals.Mute);
            card.Status.horizontalOverflow = HorizontalWrapMode.Wrap;
            Box(card.Status.rectTransform, 0.36f, 0.45f, 0.97f, 0.74f);
            var barBack = SurvivalVisuals.Image(rt, "BarBack", new Color(0f, 0f, 0f, 0.55f));
            Box(barBack.rectTransform, 0.36f, 0.33f, 0.95f, 0.43f);
            card.BarFill = SurvivalVisuals.Image(barBack.transform, "BarFill", SurvivalVisuals.PinBuild);
            Box(card.BarFill.rectTransform, 0f, 0f, 0f, 1f);

            var action = SurvivalVisuals.Button(rt, "Action", SurvivalVisuals.MineFill);
            card.Action = action.GetComponent<Image>();
            Box(card.Action.rectTransform, 0.05f, 0.05f, 0.95f, 0.29f);
            card.ActionText = SurvivalVisuals.Text(action.transform, "T", "", 18, TextAnchor.MiddleCenter, SurvivalVisuals.Panel);
            SurvivalVisuals.Stretch(card.ActionText.rectTransform);

            card.Bubble = SurvivalVisuals.Circle(rt, "Bubble", SurvivalVisuals.Cream);
            Box(card.Bubble.rectTransform, 0.02f, 0.58f, 0.36f, 1.18f);
            var bubbleRing = SurvivalVisuals.Ring(card.Bubble.transform, "Ring", SurvivalVisuals.Gold);
            SurvivalVisuals.Stretch(bubbleRing.rectTransform);
            card.BubbleText = SurvivalVisuals.Text(card.Bubble.transform, "T", "", 17, TextAnchor.MiddleCenter, SurvivalVisuals.Panel);
            SurvivalVisuals.Stretch(card.BubbleText.rectTransform);
            card.Bubble.gameObject.SetActive(false);

            card.Badge = SurvivalVisuals.Circle(rt, "Badge", FreeGreen);
            Box(card.Badge.rectTransform, 0.78f, 0.82f, 1.04f, 1.20f);
            card.BadgeText = SurvivalVisuals.Text(card.Badge.transform, "T", "", 14, TextAnchor.MiddleCenter, Color.white);
            SurvivalVisuals.Stretch(card.BadgeText.rectTransform);
            card.Badge.gameObject.SetActive(false);

            Wire(body, () => OnTapPlot(plotId));
            Wire(action, () => OnPlotAction(plotId));
            return card;
        }

        private void BuildQuestTracker(RectTransform root)
        {
            if (_pack == null || !_pack.Hud.TryGetValue("quest_tracker", out var questRect))
            {
                return;
            }

            var frame = SurvivalVisuals.Framed(root, "QuestTracker", SurvivalVisuals.Panel, SurvivalVisuals.GoldLine, 2f);
            frame.raycastTarget = true;
            var btn = frame.gameObject.AddComponent<Button>();
            btn.targetGraphic = frame;
            SurvivalVisuals.Place(frame.rectTransform, questRect);

            var q = new QuestTracker { Root = frame.rectTransform };
            q.Title = SurvivalVisuals.Text(frame.transform, "Title", "", 15, TextAnchor.MiddleLeft, SurvivalVisuals.Gold);
            Box(q.Title.rectTransform, 0.05f, 0.70f, 0.72f, 0.96f);
            q.Body = SurvivalVisuals.Text(frame.transform, "Body", "", 17, TextAnchor.UpperLeft, SurvivalVisuals.Cream);
            q.Body.horizontalOverflow = HorizontalWrapMode.Wrap;
            Box(q.Body.rectTransform, 0.05f, 0.30f, 0.72f, 0.72f);
            q.BarBack = SurvivalVisuals.Image(frame.transform, "BarBack", new Color(0f, 0f, 0f, 0.55f));
            Box(q.BarBack.rectTransform, 0.05f, 0.10f, 0.55f, 0.24f);
            q.BarFill = SurvivalVisuals.Image(q.BarBack.transform, "BarFill", SurvivalVisuals.Reward);
            Box(q.BarFill.rectTransform, 0f, 0f, 0f, 1f);
            q.Count = SurvivalVisuals.Text(frame.transform, "Count", "", 15, TextAnchor.MiddleLeft, SurvivalVisuals.Cream);
            Box(q.Count.rectTransform, 0.58f, 0.06f, 0.74f, 0.28f);

            var chestHost = SurvivalVisuals.Image(frame.transform, "Chest", new Color(0f, 0f, 0f, 0f));
            Box(chestHost.rectTransform, 0.74f, 0.12f, 0.98f, 0.88f);
            BuildChestArt(chestHost.rectTransform, out _);
            var claim = SurvivalVisuals.Text(chestHost.transform, "Claim", S("hook.claim", "Claim"), 15, TextAnchor.LowerCenter, SurvivalVisuals.Reward);
            Box(claim.rectTransform, -0.2f, -0.28f, 1.2f, 0.05f);
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

            var frame = SurvivalVisuals.Framed(root, "GemShopButton", SurvivalVisuals.AttackFill, SurvivalVisuals.GoldLine, 2f);
            frame.raycastTarget = true;
            var btn = frame.gameObject.AddComponent<Button>();
            btn.targetGraphic = frame;
            SurvivalVisuals.Place(frame.rectTransform, shopRect);
            var storeArt = SurvivalArt.Get(SurvIds.ThemeAStoreIcon);
            var icon = storeArt != null
                ? SurvivalVisuals.Image(frame.transform, "Icon", Color.white, storeArt)
                : SurvivalVisuals.Circle(frame.transform, "Icon", ResColor(SurvIds.ResHard));
            icon.preserveAspect = true;
            Box(icon.rectTransform, 0.03f, 0.10f, 0.26f, 0.90f);
            var label = SurvivalVisuals.Text(frame.transform, "T", S("store.gem_shop.button", "Gem Shop"), 20, TextAnchor.MiddleLeft, SurvivalVisuals.Cream);
            Box(label.rectTransform, 0.29f, 0f, 1f, 1f);

            _freeBadge = SurvivalVisuals.Image(frame.transform, "FreeBadge", FreeGreen);
            Box(_freeBadge.rectTransform, 0.66f, 0.55f, 1.04f, 1.30f);
            var badgeText = SurvivalVisuals.Text(_freeBadge.transform, "T", S("hook.free", "FREE"), 16, TextAnchor.MiddleCenter, Color.white);
            SurvivalVisuals.Stretch(badgeText.rectTransform);
            _freeBadge.gameObject.SetActive(false);

            _shopButton = frame.rectTransform;
            Wire(btn, OpenShop);
        }

        private void BuildDebugButtons(RectTransform root)
        {
            DebugButton(root, "debug_gift", S("debug.gift", "DEBUG: Gift now (G)"), DebugGiftNow);
            DebugButton(root, "debug_skip", S("debug.skip", "DEBUG: +5 min (T)"), DebugSkip);
        }

        private void DebugButton(RectTransform root, string slot, string label, Action onTap)
        {
            if (_pack == null || !_pack.Hud.TryGetValue(slot, out var slotRect))
            {
                return;
            }

            var btn = SurvivalVisuals.Button(root, "Debug_" + slot, new Color(0.35f, 0.12f, 0.12f, 0.85f));
            SurvivalVisuals.Place(btn.GetComponent<RectTransform>(), slotRect);
            var t = SurvivalVisuals.Text(btn.transform, "T", label, 16, TextAnchor.MiddleCenter, SurvivalVisuals.Cream);
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

                card.Level.text = Fmt("hook.level", "Lv {0}", plot.Level);
                var timerId = BaseEconomy.UpgradeTimerId(plot.Id);
                var timer = _hook.Timers.Find(timerId);
                if (timer != null)
                {
                    var remaining = timer.Remaining(now);
                    var done = remaining <= 0;
                    var free = _hook.Timers.CanFinishFree(timerId);
                    card.Status.text = done
                        ? S("hook.finish", "Finish!")
                        : S("hook.upgrading", "Upgrading") + "  " + TimerQueue.FormatClock(remaining);
                    card.Status.color = SurvivalVisuals.Cream;
                    SetBar(card.BarFill, (float)timer.Progress(now), done || free ? FreeGreen : SurvivalVisuals.PinBuild);
                    card.ActionText.text = done
                        ? S("hook.finish", "Finish!")
                        : free ? S("hook.speedup.free", "Finish FREE") : S("hook.speedup.title", "Speed Up");
                    card.Action.color = done || free ? FreeGreen : SurvivalVisuals.PinBuild;
                    card.Badge.gameObject.SetActive(done || free);
                    card.BadgeText.text = done ? "!" : S("hook.free", "FREE");
                    card.Bubble.gameObject.SetActive(false);
                    continue;
                }

                card.Badge.gameObject.SetActive(false);
                if (plot.IsProducer)
                {
                    var res = plot.Def.Produces!;
                    card.Status.text = ResName(res) + "  " + ChipWallet.FormatGrouped(plot.Collectable) + " / " + ChipWallet.FormatGrouped(plot.Current.Capacity);
                    card.Status.color = SurvivalVisuals.Mute;
                    SetBar(card.BarFill, (float)plot.Fill, ResColor(res));
                    card.Bubble.gameObject.SetActive(plot.CanCollect);
                    card.BubbleText.text = "+" + ChipWallet.FormatCompact(plot.Collectable);
                }
                else
                {
                    card.Status.text = plot.Next == null ? "" : CostLine(plot.UpgradeCost);
                    card.Status.color = SurvivalVisuals.Mute;
                    SetBar(card.BarFill, 0f, SurvivalVisuals.PinBuild);
                    card.Bubble.gameObject.SetActive(false);
                }

                var block = _hook.Base.CheckUpgrade(plot.Id);
                card.ActionText.text = BlockLabel(block, plot.Next?.Level ?? 0, S("hook.upgrade", "Upgrade"));
                card.Action.color = block == UpgradeBlock.None ? SurvivalVisuals.MineFill : Disabled;
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
                return;
            }

            _quest.Title.text = S("quest.tracker.title", "Quest") + "  " + (quests.Index + 1) + "/" + quests.Chain.Steps.Count;
            _quest.Body.text = S(step.Id + ".title", step.Id);
            _quest.BarBack.gameObject.SetActive(true);
            var frac = step.Count <= 0 ? 1f : Mathf.Clamp01(quests.Progress / (float)step.Count);
            SetBar(_quest.BarFill, frac, quests.IsComplete ? FreeGreen : SurvivalVisuals.Reward);
            _quest.Count.text = quests.IsComplete ? "\u2713" : quests.Progress + "/" + step.Count;
            _quest.Chest.gameObject.SetActive(quests.IsComplete);

            if (quests.IsComplete && !_questWasComplete)
            {
                StartCoroutine(Punch(_quest.Root, 0.18f, 12));
                StartCoroutine(SparkBurst(NormCenterOf(_quest.Root), SurvivalVisuals.Reward, 12, 0.07f));
            }

            _questWasComplete = quests.IsComplete;
        }

        private void RefreshShop()
        {
            if (_hook == null || _freeBadge == null)
            {
                return;
            }

            _freeBadge.gameObject.SetActive(_hook.FreeAvailable(SurvIds.StoreGemShop));
        }

        private void AnimateIdle(float t)
        {
            foreach (var card in _cards.Values)
            {
                if (card.Bubble.gameObject.activeSelf)
                {
                    var bob = 1f + 0.07f * Mathf.Sin(t * 5f + card.PlotId.Length);
                    card.Bubble.rectTransform.localScale = new Vector3(bob, bob, 1f);
                }

                if (card.Badge.gameObject.activeSelf)
                {
                    var throb = 1f + 0.12f * Mathf.Abs(Mathf.Sin(t * 4f));
                    card.Badge.rectTransform.localScale = new Vector3(throb, throb, 1f);
                }
            }

            if (_freeBadge != null && _freeBadge.gameObject.activeSelf)
            {
                var pulse = 1f + 0.14f * Mathf.Abs(Mathf.Sin(t * 4.5f));
                _freeBadge.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
                _freeBadge.color = Color.Lerp(FreeGreen, new Color(0.65f, 1f, 0.55f, 1f), 0.5f + 0.5f * Mathf.Sin(t * 9f));
            }

            if (_quest != null && _quest.Chest.gameObject.activeSelf)
            {
                var hop = 1f + 0.10f * Mathf.Abs(Mathf.Sin(t * 4f));
                _quest.Chest.localScale = new Vector3(hop, hop, 1f);
                _quest.Chest.localRotation = Quaternion.Euler(0f, 0f, 7f * Mathf.Sin(t * 8f));
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

            if (!plot.IsProducer)
            {
                return;
            }

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
                    var finished = _hook.FinishFree(plotId);
                    if (finished > 0)
                    {
                        LevelUp(card, finished);
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
                StartCoroutine(SparkBurst(NormCenterOf(card.Action.rectTransform), SurvivalVisuals.PinBuild, 8, 0.05f));
                RefreshCards();
                return;
            }

            var nextLevel = _hook.Base.Find(plotId)?.Next?.Level ?? 0;
            Toast(NormCenterOf(card.Root), BlockLabel(block, nextLevel, S("hook.need_more", "Not enough resources")), Warn);
            StartCoroutine(Shake(card.Root));
        }

        private void LevelUp(PlotCard card, int level)
        {
            var at = NormCenterOf(card.Root);
            StartCoroutine(Punch(card.Root, 0.25f, 16));
            StartCoroutine(RingBurst(at, SurvivalVisuals.Reward, 0.36f));
            StartCoroutine(SparkBurst(at, SurvivalVisuals.Reward, 18, 0.10f));
            FloatText(at + new Vector2(0f, 0.05f), Fmt("hook.level_up", "Level {0}!", level), SurvivalVisuals.Reward, 46);
            RefreshAll();
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

        private RectTransform? OpenSheet(string name, float width, float height)
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

            var panel = SurvivalVisuals.Framed(dimBtn.transform, "Panel", SurvivalVisuals.InspectFill, SurvivalVisuals.GoldLine, 3f);
            panel.raycastTarget = true;
            SurvivalVisuals.AnchorBox(panel.rectTransform, 0.5f, 0.52f, width, height);
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

        private Text SheetButton(RectTransform panel, string name, Color fill, float x0, float y0, float x1, float y1, int size, Color ink, out Button button)
        {
            button = SurvivalVisuals.Button(panel, name, fill);
            Box(button.GetComponent<RectTransform>(), x0, y0, x1, y1);
            var label = SurvivalVisuals.Text(button.transform, "T", "", size, TextAnchor.MiddleCenter, ink);
            SurvivalVisuals.Stretch(label.rectTransform);
            return label;
        }

        private void OpenSpeedUp(string plotId)
        {
            if (_hook == null)
            {
                return;
            }

            var panel = OpenSheet("SpeedUpSheet", 0.82f, 0.30f);
            if (panel == null)
            {
                return;
            }

            var hook = _hook;
            var timerId = BaseEconomy.UpgradeTimerId(plotId);
            var title = SurvivalVisuals.Text(panel, "Title", S("hook.speedup.title", "Speed Up") + " \u00b7 " + S("hook.plot." + plotId, plotId), 30, TextAnchor.MiddleCenter, SurvivalVisuals.Gold);
            Box(title.rectTransform, 0f, 0.84f, 1f, 0.98f);
            var clock = SurvivalVisuals.Text(panel, "Clock", "", 44, TextAnchor.MiddleCenter, SurvivalVisuals.Cream);
            Box(clock.rectTransform, 0f, 0.66f, 1f, 0.84f);
            var barBack = SurvivalVisuals.Image(panel, "BarBack", new Color(0f, 0f, 0f, 0.6f));
            Box(barBack.rectTransform, 0.08f, 0.60f, 0.92f, 0.65f);
            var barFill = SurvivalVisuals.Image(barBack.transform, "Fill", SurvivalVisuals.PinBuild);
            Box(barFill.rectTransform, 0f, 0f, 0f, 1f);

            var rushText = SheetButton(panel, "Rush", SurvivalVisuals.MineFill, 0.08f, 0.40f, 0.92f, 0.56f, 24, SurvivalVisuals.Panel, out var rush);
            var itemsText = SheetButton(panel, "Items", SurvivalVisuals.PinBuild, 0.08f, 0.22f, 0.92f, 0.37f, 22, Color.white, out var items);
            var hint = SurvivalVisuals.Text(panel, "Hint", S("hook.speedup.free_hint", "Under 5 min left finishes free"), 17, TextAnchor.MiddleCenter, SurvivalVisuals.Mute);
            Box(hint.rectTransform, 0f, 0.12f, 1f, 0.21f);
            var closeText = SheetButton(panel, "Close", SurvivalVisuals.PanelSoft, 0.35f, 0.02f, 0.65f, 0.11f, 18, SurvivalVisuals.Cream, out var close);
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
                SetBar(barFill, (float)timer.Progress(nowSec), isFree ? FreeGreen : SurvivalVisuals.PinBuild);
                var cost = hook.Timers.RushCost(timerId);
                rushText.text = isFree
                    ? S("hook.speedup.free", "Finish FREE")
                    : Fmt("hook.speedup.rush", "Finish now  \u00b7  {0} Gems", cost);
                rushImg.color = isFree
                    ? FreeGreen
                    : hook.Wallet.CanAfford(SurvIds.ResHard, cost) ? SurvivalVisuals.MineFill : Disabled;
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
            LevelUp(card, level);
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

            var panel = OpenSheet("GemShopSheet", 0.90f, 0.50f);
            if (panel == null)
            {
                return;
            }

            var hook = _hook;
            var title = SurvivalVisuals.Text(panel, "Title", S("store.gem_shop.title", "Gem Shop"), 34, TextAnchor.MiddleCenter, SurvivalVisuals.Gold);
            Box(title.rectTransform, 0f, 0.90f, 1f, 0.985f);
            var balance = SurvivalVisuals.Text(panel, "Balance", "", 20, TextAnchor.MiddleCenter, ResColor(SurvIds.ResHard));
            Box(balance.rectTransform, 0f, 0.85f, 1f, 0.90f);
            var sandbox = SurvivalVisuals.Text(panel, "Sandbox", S("store.sandbox", "SANDBOX \u00b7 no real charge"), 15, TextAnchor.MiddleCenter, Warn);
            Box(sandbox.rectTransform, 0f, 0.81f, 1f, 0.85f);

            var refreshers = new List<Action>();
            var rows = store.Offers.Count;
            var rowH = 0.67f / Mathf.Max(1, rows);
            for (var i = 0; i < rows; i++)
            {
                var top = 0.79f - i * rowH;
                refreshers.Add(BuildOfferRow(panel, store.Offers[i], top - rowH + 0.015f, top));
            }

            var closeText = SheetButton(panel, "Close", SurvivalVisuals.PanelSoft, 0.35f, 0.02f, 0.65f, 0.09f, 18, SurvivalVisuals.Cream, out var close);
            closeText.text = S("hook.close", "Close");
            Wire(close, CloseSheet);

            _sheetRefresh = () =>
            {
                balance.text = ChipWallet.FormatGrouped(hook.Wallet.Get(SurvIds.ResHard)) + " " + ResName(SurvIds.ResHard);
                foreach (var refresh in refreshers)
                {
                    refresh();
                }
            };
            _sheetRefresh();
        }

        private Action BuildOfferRow(RectTransform panel, OfferDef offer, float yMin, float yMax)
        {
            var row = SurvivalVisuals.Framed(panel, "Offer_" + offer.Id, SurvivalVisuals.RowFill, SurvivalVisuals.GoldLine, 1.5f);
            Box(row.rectTransform, 0.04f, yMin, 0.96f, yMax);
            var isFree = string.Equals(offer.Kind, OfferKinds.Free, StringComparison.Ordinal);
            var iconRes = isFree ? SurvIds.ResSoft : FirstKey(offer.Grants) ?? SurvIds.ResHard;

            var icon = SurvivalVisuals.Circle(row.transform, "Icon", isFree ? FreeGreen : ResColor(iconRes));
            Box(icon.rectTransform, 0.02f, 0.12f, 0.16f, 0.88f);
            var glyph = SurvivalVisuals.Text(icon.transform, "G", isFree ? "\u2605" : ResGlyph(iconRes), 24, TextAnchor.MiddleCenter, SurvivalVisuals.Panel);
            SurvivalVisuals.Stretch(glyph.rectTransform);

            var name = SurvivalVisuals.Text(row.transform, "Name", S(offer.Id + ".title", offer.Id), 22, TextAnchor.MiddleLeft, SurvivalVisuals.Cream);
            Box(name.rectTransform, 0.19f, 0.50f, 0.62f, 0.94f);
            var sub = SurvivalVisuals.Text(row.transform, "Sub", isFree ? "" : GrantLine(offer.Grants), 17, TextAnchor.MiddleLeft, SurvivalVisuals.Mute);
            Box(sub.rectTransform, 0.19f, 0.08f, 0.62f, 0.50f);

            if (!string.IsNullOrEmpty(offer.Tag))
            {
                var tag = SurvivalVisuals.Image(row.transform, "Tag", new Color(0.85f, 0.25f, 0.30f, 1f));
                Box(tag.rectTransform, 0.44f, 0.62f, 0.64f, 0.98f);
                var tagText = SurvivalVisuals.Text(tag.transform, "T", S("offer.tag." + offer.Tag, offer.Tag ?? ""), 13, TextAnchor.MiddleCenter, Color.white);
                SurvivalVisuals.Stretch(tagText.rectTransform);
            }

            var buyText = SheetButton((RectTransform)row.transform, "Buy", SurvivalVisuals.MineFill, 0.65f, 0.14f, 0.98f, 0.86f, 20, SurvivalVisuals.Panel, out var buy);
            var buyRt = buy.GetComponent<RectTransform>();
            var buyImg = buy.GetComponent<Image>();
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
                    buyImg.color = ready
                        ? Color.Lerp(FreeGreen, Color.white, 0.25f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)))
                        : Disabled;
                    return;
                }

                if (string.Equals(offer.Kind, OfferKinds.Sku, StringComparison.Ordinal))
                {
                    var product = _hook.Store.ProductFor(offer.Sku);
                    buyText.text = product != null && product.PriceLabel.Length > 0 ? product.PriceLabel : "\u2014";
                    buyImg.color = SurvivalVisuals.MineFill;
                    return;
                }

                buyText.text = CostLine(offer.Price);
                buyImg.color = _hook.Store.CanAfford(offer) ? SurvivalVisuals.MineFill : Disabled;
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

            var rays = new GameObject("Rays", typeof(RectTransform)).GetComponent<RectTransform>();
            rays.SetParent(host, false);
            SurvivalVisuals.AnchorBox(rays, 0.5f, 0.55f, 0f, 0f);
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

            var glow = SurvivalVisuals.Circle(host, "Glow", new Color(1f, 0.85f, 0.4f, 0f));
            SurvivalVisuals.AnchorBox(glow.rectTransform, 0.5f, 0.55f, 0.5f, 0.5f * Aspect);

            var chestRt = new GameObject("Chest", typeof(RectTransform)).GetComponent<RectTransform>();
            chestRt.SetParent(host, false);
            const float chestW = 0.34f;
            SurvivalVisuals.AnchorBox(chestRt, 0.5f, 0.66f, chestW, chestW * Aspect);
            BuildChestArt(chestRt, out var lid);

            var titleText = SurvivalVisuals.Text(host, "Title", title, 56, TextAnchor.MiddleCenter, SurvivalVisuals.Reward);
            SurvivalVisuals.AnchorBox(titleText.rectTransform, 0.5f, 0.74f, 0.9f, 0.06f);
            titleText.rectTransform.localScale = Vector3.zero;
            var titleOutline = titleText.gameObject.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0.25f, 0.12f, 0.02f, 1f);
            titleOutline.effectDistance = new Vector2(3f, -3f);

            // A: drop in with overshoot.
            var elapsed = 0f;
            while (elapsed < 0.32f)
            {
                elapsed += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(elapsed / 0.32f);
                dimImg.color = new Color(Dim.r, Dim.g, Dim.b, Mathf.Lerp(0f, Dim.a, u));
                var back = 1f + 2.2f * Mathf.Pow(u - 1f, 3f) + 1.2f * Mathf.Pow(u - 1f, 2f);
                SurvivalVisuals.AnchorBox(chestRt, 0.5f, Mathf.Lerp(0.66f, 0.52f, u), chestW, chestW * Aspect);
                chestRt.localScale = Vector3.one * Mathf.Max(0f, back);
                yield return null;
            }

            chestRt.localScale = Vector3.one;

            // B: shake.
            elapsed = 0f;
            while (elapsed < 0.42f)
            {
                elapsed += Time.unscaledDeltaTime;
                chestRt.localRotation = Quaternion.Euler(0f, 0f, 7f * Mathf.Sin(elapsed * 48f) * (1f - elapsed / 0.42f));
                chestRt.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(elapsed * 30f));
                yield return null;
            }

            chestRt.localRotation = Quaternion.identity;
            chestRt.localScale = Vector3.one;

            // C: burst: lid flies, rays, glow, ring, confetti, reward rows.
            var center = new Vector2(0.5f, 0.56f);
            StartCoroutine(RingBurst(center, SurvivalVisuals.Reward, 0.6f));
            StartCoroutine(SparkBurst(center, SurvivalVisuals.Reward, 22, 0.16f));
            StartCoroutine(Confetti(host, center, 70));
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

                glow.color = new Color(1f, 0.85f, 0.4f, Mathf.Lerp(0.95f, 0.55f, u));
                glow.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1.6f, u);
                titleText.rectTransform.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u * 1.4f));
                for (var k = 0; k < rewardRows.Count; k++)
                {
                    var rowRt = rewardRows[k].Rt;
                    var y = 0.38f - k * 0.05f;
                    SurvivalVisuals.AnchorBox(rowRt, 0.5f, Mathf.Lerp(0.54f, y, u), 0.6f, 0.045f);
                    rowRt.localScale = Vector3.one * (u < 0.8f ? Mathf.Lerp(0f, 1.15f, u / 0.8f) : Mathf.Lerp(1.15f, 1f, (u - 0.8f) / 0.2f));
                }

                SetRayAlpha(rays, Mathf.Lerp(0f, 0.22f, u));
                yield return null;
            }

            var hint = SurvivalVisuals.Text(host, "Hint", S("hook.continue", "Tap to continue"), 22, TextAnchor.MiddleCenter, SurvivalVisuals.Cream);
            SurvivalVisuals.AnchorBox(hint.rectTransform, 0.5f, 0.17f, 0.8f, 0.04f);
            tapped = false;
            elapsed = 0f;
            while (!tapped && elapsed < 3.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                rays.localRotation = Quaternion.Euler(0f, 0f, -elapsed * 22f);
                var blink = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(elapsed * 3f));
                hint.color = new Color(SurvivalVisuals.Cream.r, SurvivalVisuals.Cream.g, SurvivalVisuals.Cream.b, blink);
                glow.rectTransform.localScale = Vector3.one * (1.6f + 0.08f * Mathf.Sin(elapsed * 5f));
                yield return null;
            }

            // D: rewards fly to the HUD, overlay fades.
            foreach (var row in rewardRows)
            {
                StartCoroutine(FlyReward(row.Resource, NormCenterOf(row.Rt), 7));
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
                titleText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, u);
                var tc = titleText.color;
                titleText.color = new Color(tc.r, tc.g, tc.b, 1f - u);
                glow.color = new Color(1f, 0.85f, 0.4f, Mathf.Lerp(0.55f, 0f, u));
                SetRayAlpha(rays, Mathf.Lerp(0.22f, 0f, u));
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

        private RectTransform RewardRow(Transform host, string res, int amount)
        {
            var row = new GameObject("Reward_" + res, typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(host, false);
            var icon = SurvivalVisuals.Circle(row, "Icon", ResColor(res));
            Box(icon.rectTransform, 0.10f, 0f, 0.26f, 1f);
            var glyph = SurvivalVisuals.Text(icon.transform, "G", ResGlyph(res), 22, TextAnchor.MiddleCenter, SurvivalVisuals.Panel);
            SurvivalVisuals.Stretch(glyph.rectTransform);
            var label = SurvivalVisuals.Text(row, "T", "+" + ChipWallet.FormatGrouped(amount) + " " + ResName(res), 36, TextAnchor.MiddleLeft, SurvivalVisuals.Cream);
            Box(label.rectTransform, 0.30f, 0f, 1f, 1f);
            var labelOutline = label.gameObject.AddComponent<Outline>();
            labelOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            labelOutline.effectDistance = new Vector2(2f, -2f);
            row.localScale = Vector3.zero;
            return row;
        }

        /// <summary>Chest drawn from UI quads. <paramref name="lid"/> pivots at its bottom-left hinge.</summary>
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

        private IEnumerator Confetti(Transform host, Vector2 origin, int count)
        {
            var bits = new List<Confetto>();
            for (var i = 0; i < count; i++)
            {
                var img = SurvivalVisuals.Image(host, "Confetti" + i, ConfettiColors[i % ConfettiColors.Length]);
                var rt = img.rectTransform;
                rt.anchorMin = origin;
                rt.anchorMax = origin;
                rt.sizeDelta = new Vector2(UnityEngine.Random.Range(10f, 18f), UnityEngine.Random.Range(18f, 30f));
                bits.Add(new Confetto
                {
                    Rt = rt,
                    Img = img,
                    Pos = origin,
                    Vel = new Vector2(UnityEngine.Random.Range(-0.55f, 0.55f), UnityEngine.Random.Range(0.45f, 1.25f)),
                    Spin = UnityEngine.Random.Range(-540f, 540f),
                    Life = UnityEngine.Random.Range(1.4f, 2.4f)
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

                    bit.Vel += new Vector2(-bit.Vel.x * 0.9f * dt, -1.35f * dt);
                    bit.Pos += new Vector2(bit.Vel.x * dt, bit.Vel.y * dt * 0.75f);
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
            var flying = new List<RectTransform?>();
            for (var i = 0; i < coins; i++)
            {
                var coin = SurvivalVisuals.Circle(_overlay, "Coin", color);
                var rim = SurvivalVisuals.Ring(coin.transform, "Rim", Color.white);
                SurvivalVisuals.Stretch(rim.rectTransform);
                SurvivalVisuals.AnchorBox(coin.rectTransform, from.x, from.y, 0.045f, 0.045f * Aspect);
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
                    var size = 0.045f * Mathf.Lerp(1.15f, 0.65f, u);
                    SurvivalVisuals.AnchorBox(coinRt, p.x, p.y, size, size * Aspect);
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

        private IEnumerator RingBurst(Vector2 at, Color color, float size)
        {
            if (_overlay == null)
            {
                yield break;
            }

            var ring = SurvivalVisuals.Ring(_overlay, "TapRing", color);
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
            for (var i = 0; i < count; i++)
            {
                var ang = i / (float)count * Mathf.PI * 2f + UnityEngine.Random.Range(-0.2f, 0.2f);
                sparks.Add(SurvivalVisuals.Circle(_overlay, "Spark", color));
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
                    var s = 0.018f * (1f - u * 0.6f);
                    SurvivalVisuals.AnchorBox(sparks[i].rectTransform, p.x, p.y, s, s * Aspect);
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
            var kb = Keyboard.current;
            if (kb != null && kb.enabled)
            {
                gift |= kb.gKey.wasPressedThisFrame;
                skip |= kb.tKey.wasPressedThisFrame;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            gift |= Input.GetKeyDown(KeyCode.G);
            skip |= Input.GetKeyDown(KeyCode.T);
#endif
            FireDebug(gift, skip);
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

            FireDebug(ev.keyCode == KeyCode.G, ev.keyCode == KeyCode.T);
        }

        private void FireDebug(bool gift, bool skip)
        {
            if ((!gift && !skip) || _lastDebugFrame == Time.frameCount)
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

        private string Fmt(string key, string fallback, object arg) =>
            string.Format(CultureInfo.InvariantCulture, S(key, fallback), arg);

        private string ResName(string resourceId) => S(resourceId + ".name", resourceId);

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

        private static Color ResColor(string resourceId) => resourceId switch
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

        private static string ResGlyph(string resourceId) => resourceId switch
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
