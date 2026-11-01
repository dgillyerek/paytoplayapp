#nullable enable
using System;
using Survival.Domain.Clock;
using Survival.Domain.Ids;
using Survival.Domain.Session;
using Survival.Domain.Taps;
using Survival.Domain.Theme;
using Survival.Domain.View;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Oracle's Crystal Orb on the village HUD (left edge). Glowing orb + FREE badge when a vision is ready; tap to
    /// reveal a random task; the sheet shows progress, re-roll (free a few times a day, then gems), "call the next
    /// vision now" for gems, and the mystery chest claim with the chest-burst celebration. Saved in PlayerPrefs.
    /// </summary>
    public sealed class SurvivalOrbView : MonoBehaviour
    {
        private const float YTop = 0.300f;

        private SurvivalHookView _view = null!;
        private HookSession _hook = null!;
        private CrystalOrb _orb = null!;
        private TapHudParts? _button;
        private Image? _crest;
        private float _tick;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {
            SurvivalHookView.HudBuilt -= Attach;
            SurvivalHookView.HudBuilt += Attach;
        }

        private static void Attach(SurvivalHookView view, RectTransform root)
        {
            if (view.TapHook == null || view.GetComponent<SurvivalOrbView>() != null)
            {
                return;
            }

            view.gameObject.AddComponent<SurvivalOrbView>().Init(view, root);
        }

        private void Init(SurvivalHookView view, RectTransform root)
        {
            _view = view;
            _hook = view.TapHook!;
            var clock = _hook.Clock;
            _orb = new CrystalOrb(OrbConfig.Default, () => clock.Now, Environment.TickCount);
            _orb.Apply(PlayerPrefs.GetString(CrystalOrb.PrefsKey, ""));
            ActivityFeed.Reported += OnActivity;

            var btn = view.TapHudButton(root, "CrystalOrbButton", YTop, view.TapS("orb.button", "Oracle"), new Color(0.30f, 0.18f, 0.52f, 1f), out var parts);
            _button = parts;
            _crest = HookSkin.Art(parts.Icon, "OrbArt", HookArtKeys.OrbCrystal, new Color(0.62f, 0.45f, 1f, 1f));
            _crest.raycastTarget = false;
            SurvivalVisuals.Stretch(_crest.rectTransform);
            view.TapWire(btn, OnTap);
            Refresh();
        }

        private void OnDestroy()
        {
            ActivityFeed.Reported -= OnActivity;
            if (_orb != null)
            {
                Save();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _orb != null)
            {
                Save();
            }
        }

        private void Save()
        {
            PlayerPrefs.SetString(CrystalOrb.PrefsKey, _orb.Capture());
            PlayerPrefs.Save();
        }

        private void OnActivity(string kind, int amount)
        {
            if (_orb.Report(kind, amount) && _button != null)
            {
                _view.TapToast(_button.Root, _view.TapS("orb.done_toast", "Vision fulfilled!"), SurvivalHookView.TapFreeGreen);
            }

            Save();
        }

        private void Update()
        {
            if (_orb == null)
            {
                return;
            }

            _tick -= Time.unscaledDeltaTime;
            if (_tick <= 0f)
            {
                _tick = 0.25f;
                Refresh();
            }

            if (_crest != null)
            {
                var glow = _orb.State == OrbState.Glowing || _orb.State == OrbState.Complete;
                var s = glow ? 1f + 0.07f * Mathf.Sin(Time.unscaledTime * 4f) : 1f;
                _crest.rectTransform.localScale = new Vector3(s, s, 1f);
            }

            _button?.Show(_orb.State == OrbState.Glowing, _orb.State == OrbState.Complete);
            _view.TapRailShown("CrystalOrbButton", Survival.Domain.View.RailActionable.Orb(_orb));
        }

        private void Refresh()
        {
            if (_button == null)
            {
                return;
            }

            var task = _orb.Task;
            _button.Label.text = _orb.State switch
            {
                OrbState.Glowing => _view.TapS("orb.button.reveal", "Tap the Orb!"),
                OrbState.Complete => _view.TapS("orb.button.claim", "Claim!"),
                OrbState.Active when task != null => task.Progress + "/" + task.Target,
                _ => TimerQueue.FormatClock(_orb.SecondsUntilNext),
            };
        }

        private void OnTap()
        {
            _view.TapPunch(_button!.Root);
            if (_orb.Reveal())
            {
                _view.TapBurst(_button.Root, new Color(0.70f, 0.55f, 1f, 1f), 18);
                Save();
            }

            OpenSheet();
        }

        private string TaskText(OrbTask task)
        {
            var key = "orb.task." + task.Kind;
            return task.Kind switch
            {
                ActivityKinds.RunnerKills => _view.TapFmt(key, "Slay {0} foes on the road", task.Target),
                ActivityKinds.RunnerWin => _view.TapFmt(key, "Win {0} runner levels", task.Target),
                ActivityKinds.Harvest => _view.TapFmt(key, "Gather {0} harvests in town", task.Target),
                ActivityKinds.Upgrade => _view.TapFmt(key, "Upgrade {0} building", task.Target),
                ActivityKinds.HeroLevel => _view.TapFmt(key, "Level up heroes {0} times", task.Target),
                _ => task.Kind,
            };
        }

        private static Image Card(RectTransform panel, string name, float y0, float y1)
        {
            var card = HookSkin.Panel(panel, name, HookArtKeys.Plate, new Color(0.11f, 0.17f, 0.38f, 1f), 0.35f);
            card.raycastTarget = false;
            SurvivalHookView.TapBox(card.rectTransform, 0.06f, y0, 0.94f, y1);
            Ornaments.Bevel(card.transform, 0.7f);
            Ornaments.Frame(card.transform, Ornaments.Gold, 2f);
            return card;
        }

        private static Text Label(Transform parent, string name, int size, TextAnchor anchor, Color color, float x0, float y0, float x1, float y1)
        {
            var t = HookSkin.Outlined(SurvivalVisuals.Text(parent, name, "", size, anchor, color));
            t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false;
            SurvivalHookView.TapBox(t.rectTransform, x0, y0, x1, y1);
            return t;
        }

        private static readonly Color Caption = new Color(1f, 0.86f, 0.48f, 1f);
        private static readonly Color OrbGlow = new Color(0.62f, 0.45f, 1f, 1f);

        /// <summary>Where a task gets done: the runner for road tasks, heroes for hero levels, the town otherwise.</summary>
        private (string Label, Action Go) Destination(OrbTask task)
        {
            switch (task.Kind)
            {
                case ActivityKinds.RunnerKills:
                case ActivityKinds.RunnerWin:
                    return (_view.TapS("orb.go.road", "Go to the Road"), () =>
                    {
                        _view.TapCloseSheet();
                        FindFirstObjectByType<SurvivalRunnerEntry>()?.OnRunnerTapped();
                    });
                case ActivityKinds.HeroLevel:
                    return (_view.TapS("orb.go.heroes", "Go to Heroes"), () =>
                    {
                        _view.TapCloseSheet();
                        FindFirstObjectByType<SurvivalCampaignView>()?.OpenHeroes();
                    });
                case ActivityKinds.Upgrade:
                    return (_view.TapS("orb.go.build", "Go to Town & Build"), _view.TapCloseSheet);
                default:
                    return (_view.TapS("orb.go.town", "Go to Town"), _view.TapCloseSheet);
            }
        }

        private void OpenSheet()
        {
            var panel = _view.TapOpenSheet("CrystalOrbSheet", 0.88f, 0.66f, _view.TapS("orb.title", "Oracle's Crystal Orb"));
            if (panel == null)
            {
                return;
            }

            var wallet = _hook.Wallet;
            BalanceBar.Above(panel, wallet, new[] { SurvIds.ResHard });

            // Painted crystal ball on its stand, with a soft violet glow behind it.
            var glow = HookSkin.Glow(panel, "OrbGlow", OrbGlow);
            SurvivalHookView.TapBox(glow.rectTransform, 0.24f, 0.70f, 0.76f, 0.99f);
            var orbArt = HookSkin.Art(panel, "OrbArt", HookArtKeys.OrbCrystal, OrbGlow);
            orbArt.raycastTarget = false;
            SurvivalHookView.TapBox(orbArt.rectTransform, 0.33f, 0.735f, 0.67f, 0.975f);
            Ornaments.Sparkles(orbArt.rectTransform, 5, new Color(0.85f, 0.75f, 1f, 1f));

            var explain = Label(panel, "Explain", 18, TextAnchor.MiddleCenter, SurvivalVisuals.Cream, 0.05f, 0.655f, 0.95f, 0.735f);
            explain.text = _view.TapS("orb.explain", "The Oracle gives you a task.\nFinish it to win the reward!");

            // Task card: icon, caption, task line, chunky bar with the count on it.
            var taskCard = Card(panel, "TaskCard", 0.475f, 0.645f);
            var taskIcon = new GameObject("TaskIcon", typeof(RectTransform)).GetComponent<RectTransform>();
            taskIcon.SetParent(taskCard.transform, false);
            SurvivalHookView.TapBox(taskIcon, 0.02f, 0.10f, 0.20f, 0.90f);
            Image? taskArt = null;
            string? taskArtKey = null;
            var taskCaption = Label(taskCard.transform, "Caption", 14, TextAnchor.MiddleLeft, Caption, 0.22f, 0.68f, 0.98f, 0.92f);
            taskCaption.text = _view.TapS("orb.caption.task", "TASK");
            var line = Label(taskCard.transform, "Task", 21, TextAnchor.MiddleLeft, SurvivalVisuals.Cream, 0.22f, 0.40f, 0.98f, 0.70f);
            HookSkin.Fit(line, 14, 21);
            var bar = HookSkin.Bar(taskCard.transform, "Bar", out var fill);
            SurvivalHookView.TapBox(bar.rectTransform, 0.22f, 0.10f, 0.96f, 0.36f);
            var count = Label(bar.transform, "Count", 16, TextAnchor.MiddleCenter, SurvivalVisuals.Cream, 0f, 0f, 1f, 1f);

            // Reward card: the mystery chest and what it is.
            var rewardCard = Card(panel, "RewardCard", 0.335f, 0.465f);
            var chestArt = HookSkin.Art(rewardCard.transform, "Chest", HookArtKeys.ChestMystery, OrbGlow);
            chestArt.raycastTarget = false;
            SurvivalHookView.TapBox(chestArt.rectTransform, 0.02f, 0.04f, 0.22f, 0.96f);
            var rewardCaption = Label(rewardCard.transform, "Caption", 14, TextAnchor.MiddleLeft, Caption, 0.25f, 0.58f, 0.98f, 0.88f);
            rewardCaption.text = _view.TapS("orb.caption.reward", "REWARD");
            var rewardName = Label(rewardCard.transform, "Reward", 23, TextAnchor.MiddleLeft, SurvivalVisuals.Cream, 0.25f, 0.14f, 0.98f, 0.58f);
            rewardName.text = _view.TapS("orb.reward.name", "Mystery Chest");

            var timer = Label(panel, "Timer", 17, TextAnchor.MiddleCenter, new Color(0.80f, 0.86f, 1f, 1f), 0.05f, 0.29f, 0.95f, 0.33f);

            var mainText = _view.TapSheetButton(panel, "Main", HookArtKeys.ButtonGreen, SurvivalVisuals.MineFill, 0.10f, 0.175f, 0.90f, 0.28f, 25, out var main);
            var rerollText = _view.TapSheetButton(panel, "Reroll", HookArtKeys.ButtonBlue, SurvivalVisuals.PinBuild, 0.10f, 0.09f, 0.90f, 0.165f, 19, out var reroll);
            var closeText = _view.TapSheetButton(panel, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.34f, 0.015f, 0.66f, 0.08f, 18, out var close);
            closeText.text = _view.TapS("hook.close", "Close");
            var mainRt = main.GetComponent<RectTransform>();
            var mainImg = main.GetComponent<Image>();
            var rerollRt = reroll.GetComponent<RectTransform>();
            var mainGem = HookSkin.ResIcon(main.transform, "Gem", SurvIds.ResHard, SurvivalVisuals.PinBuild, "G");
            mainGem.raycastTarget = false;
            SurvivalHookView.TapBox(mainGem.rectTransform, 0.80f, 0.15f, 0.92f, 0.85f);
            var rerollGem = HookSkin.ResIcon(reroll.transform, "Gem", SurvIds.ResHard, SurvivalVisuals.PinBuild, "G");
            rerollGem.raycastTarget = false;
            SurvivalHookView.TapBox(rerollGem.rectTransform, 0.80f, 0.12f, 0.92f, 0.88f);

            _view.TapWire(main, () =>
            {
                switch (_orb.State)
                {
                    case OrbState.Glowing:
                        _orb.Reveal();
                        _view.TapBurst(orbArt.rectTransform, OrbGlow, 18);
                        break;
                    case OrbState.Active when _orb.Task != null:
                        Save();
                        Destination(_orb.Task).Go();
                        return;
                    case OrbState.Complete:
                        var chest = _orb.Claim();
                        if (chest != null)
                        {
                            _view.TapCloseSheet();
                            _view.Celebrate(_view.TapS("orb.chest.title", "Mystery Chest!"), chest, () => wallet.Grant(chest));
                        }

                        break;
                    case OrbState.Resting:
                        if (_orb.Hasten(wallet) != OrbSpend.Ok)
                        {
                            _view.TapShake(mainRt);
                            _view.TapToast(mainRt, _view.TapS("hook.need_more", "Not enough resources"), SurvivalHookView.TapWarn);
                        }
                        else
                        {
                            _view.TapBurst(orbArt.rectTransform, OrbGlow, 18);
                        }

                        break;
                }

                Save();
                Refresh();
            });
            _view.TapWire(reroll, () =>
            {
                var r = _orb.Reroll(wallet);
                if (r == OrbSpend.NotEnoughGems)
                {
                    _view.TapShake(rerollRt);
                    _view.TapToast(rerollRt, _view.TapS("hook.need_more", "Not enough resources"), SurvivalHookView.TapWarn);
                }
                else if (r == OrbSpend.Ok)
                {
                    _view.TapBurst(orbArt.rectTransform, OrbGlow, 12);
                }

                Save();
                Refresh();
            });
            _view.TapWire(close, _view.TapCloseSheet);

            _view.TapSheetRefresh(() =>
            {
                var task = _orb.Task;
                var state = _orb.State;

                var artKey = task == null ? null : task.Kind switch
                {
                    ActivityKinds.RunnerKills or ActivityKinds.RunnerWin => BottomNav.IconBattle,
                    ActivityKinds.HeroLevel => BottomNav.IconHeroes,
                    ActivityKinds.Upgrade => WorldArtKeys.IconUpgrade,
                    _ => HookArtKeys.ForResource(SurvIds.ResFood),
                };
                if (artKey != taskArtKey)
                {
                    taskArtKey = artKey;
                    if (taskArt != null)
                    {
                        Destroy(taskArt.gameObject);
                    }

                    taskArt = artKey != null ? HookSkin.Art(taskIcon, "Art", artKey, SurvivalVisuals.PinBuild) : HookSkin.Art(taskIcon, "Art", HookArtKeys.OrbCrystal, OrbGlow);
                    taskArt.raycastTarget = false;
                    SurvivalVisuals.Stretch(taskArt.rectTransform);
                }

                line.text = task != null
                    ? TaskText(task)
                    : state == OrbState.Glowing
                        ? _view.TapS("orb.glowing", "A new task is ready. Tap Reveal!")
                        : _view.TapS("orb.resting", "The Oracle is resting.");
                bar.gameObject.SetActive(task != null);
                if (task != null)
                {
                    HookSkin.SetBar(fill, task.Fraction, task.Done ? HookArtKeys.BarFillGreen : HookArtKeys.BarFillBlue, task.Done ? SurvivalHookView.TapFreeGreen : SurvivalVisuals.PinBuild);
                    count.text = task.Progress + " / " + task.Target;
                }

                timer.text = state == OrbState.Resting
                    ? _view.TapFmt("orb.next_in", "Next task in {0}", TimerQueue.FormatClock(_orb.SecondsUntilNext))
                    : _view.TapS("orb.every", "A new task every 3 hours");

                string key;
                bool enabled;
                var gemOnMain = false;
                mainRt.localScale = Vector3.one;
                switch (state)
                {
                    case OrbState.Glowing:
                        mainText.text = _view.TapS("orb.reveal", "Reveal the task  \u00b7  FREE");
                        key = HookArtKeys.ButtonGreen;
                        enabled = true;
                        break;
                    case OrbState.Complete:
                        mainText.text = _view.TapS("orb.claim", "Claim Mystery Chest");
                        key = HookArtKeys.ButtonGreen;
                        enabled = true;
                        var pulse = 1f + 0.05f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));
                        mainRt.localScale = new Vector3(pulse, pulse, 1f);
                        break;
                    case OrbState.Resting:
                        mainText.text = _view.TapFmt("orb.hasten", "New task now  \u00b7  {0}", _orb.Config.HastenGems);
                        key = HookArtKeys.ButtonBlue;
                        enabled = wallet.CanAfford(SurvIds.ResHard, _orb.Config.HastenGems);
                        gemOnMain = true;
                        break;
                    default:
                        mainText.text = task != null ? Destination(task).Label + "  \u25b6" : "";
                        key = HookArtKeys.ButtonGreen;
                        enabled = true;
                        break;
                }

                HookSkin.SetButton(mainImg, key, enabled ? SurvivalVisuals.MineFill : SurvivalHookView.TapDisabled, enabled, 0.5f);
                mainText.color = HookSkin.InkFor(key);
                mainGem.gameObject.SetActive(gemOnMain);
                reroll.gameObject.SetActive(state == OrbState.Active);
                var free = _orb.FreeRerollsLeft;
                rerollGem.gameObject.SetActive(free == 0);
                rerollText.text = free > 0
                    ? _view.TapFmt("orb.reroll_free", "New task  \u00b7  Free x{0}", free)
                    : _view.TapFmt("orb.reroll_gems", "New task  \u00b7  {0}", _orb.Config.RerollGems);
            });
        }
    }
}
