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
            _crest = Ornaments.Crest(parts.Icon, new Color(0.62f, 0.45f, 1f, 1f));
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

        private void OpenSheet()
        {
            var panel = _view.TapOpenSheet("CrystalOrbSheet", 0.86f, 0.46f, _view.TapS("orb.title", "Oracle's Crystal Orb"));
            if (panel == null)
            {
                return;
            }

            var wallet = _hook.Wallet;
            BalanceBar.Above(panel, wallet, new[] { SurvIds.ResHard });
            var crest = Ornaments.Crest(panel, new Color(0.62f, 0.45f, 1f, 1f));
            SurvivalHookView.TapBox(crest.rectTransform, 0.36f, 0.62f, 0.64f, 0.86f);
            Ornaments.Sparkles(crest.rectTransform, 5, new Color(0.85f, 0.75f, 1f, 1f));

            var line = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Task", "", 26, TextAnchor.MiddleCenter, SurvivalVisuals.Cream));
            line.fontStyle = FontStyle.Bold;
            SurvivalHookView.TapBox(line.rectTransform, 0.05f, 0.52f, 0.95f, 0.61f);
            var bar = HookSkin.Bar(panel, "Bar", out var fill);
            SurvivalHookView.TapBox(bar.rectTransform, 0.10f, 0.46f, 0.90f, 0.51f);
            var hint = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Hint", "", 18, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.4f);
            SurvivalHookView.TapBox(hint.rectTransform, 0.05f, 0.39f, 0.95f, 0.45f);

            var mainText = _view.TapSheetButton(panel, "Main", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, 0.10f, 0.24f, 0.90f, 0.37f, 24, out var main);
            var rerollText = _view.TapSheetButton(panel, "Reroll", HookArtKeys.ButtonBlue, SurvivalVisuals.PinBuild, 0.10f, 0.12f, 0.90f, 0.225f, 20, out var reroll);
            var closeText = _view.TapSheetButton(panel, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.33f, 0.025f, 0.67f, 0.10f, 19, out var close);
            closeText.text = _view.TapS("hook.close", "Close");
            var mainRt = main.GetComponent<RectTransform>();
            var mainImg = main.GetComponent<Image>();
            var rerollRt = reroll.GetComponent<RectTransform>();

            _view.TapWire(main, () =>
            {
                switch (_orb.State)
                {
                    case OrbState.Glowing:
                        _orb.Reveal();
                        _view.TapBurst(crest.rectTransform, new Color(0.70f, 0.55f, 1f, 1f), 18);
                        break;
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
                            _view.TapBurst(crest.rectTransform, new Color(0.70f, 0.55f, 1f, 1f), 18);
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
                    _view.TapBurst(crest.rectTransform, new Color(0.70f, 0.55f, 1f, 1f), 12);
                }

                Save();
                Refresh();
            });
            _view.TapWire(close, _view.TapCloseSheet);

            _view.TapSheetRefresh(() =>
            {
                var task = _orb.Task;
                var state = _orb.State;
                line.text = task != null
                    ? TaskText(task)
                    : state == OrbState.Glowing
                        ? _view.TapS("orb.glowing", "The orb glows with a new vision\u2026")
                        : _view.TapS("orb.resting", "The orb is gathering power.");
                bar.gameObject.SetActive(task != null);
                if (task != null)
                {
                    HookSkin.SetBar(fill, task.Fraction, task.Done ? HookArtKeys.BarFillGreen : HookArtKeys.BarFillBlue, task.Done ? SurvivalHookView.TapFreeGreen : SurvivalVisuals.PinBuild);
                }

                hint.text = task != null
                    ? _view.TapFmt2("orb.progress", "{0} / {1}  \u00b7  finish it for a Mystery Chest", task.Progress, task.Target)
                    : _view.TapFmt("orb.next_in", "Next vision in {0}", TimerQueue.FormatClock(_orb.SecondsUntilNext));

                string key;
                bool enabled;
                switch (state)
                {
                    case OrbState.Glowing:
                        mainText.text = _view.TapS("orb.reveal", "Reveal the vision  \u00b7  FREE");
                        key = HookArtKeys.ButtonGreen;
                        enabled = true;
                        break;
                    case OrbState.Complete:
                        mainText.text = _view.TapS("orb.claim", "Open Mystery Chest");
                        key = HookArtKeys.ButtonGreen;
                        enabled = true;
                        var pulse = 1f + 0.05f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));
                        mainRt.localScale = new Vector3(pulse, pulse, 1f);
                        break;
                    case OrbState.Resting:
                        mainText.text = _view.TapFmt("orb.hasten", "See the next vision now  \u00b7  {0} Gems", _orb.Config.HastenGems);
                        key = HookArtKeys.ButtonGold;
                        enabled = wallet.CanAfford(SurvIds.ResHard, _orb.Config.HastenGems);
                        break;
                    default:
                        mainText.text = _view.TapS("orb.in_progress", "Keep going!");
                        key = HookArtKeys.ButtonBlue;
                        enabled = false;
                        break;
                }

                HookSkin.SetButton(mainImg, key, enabled ? SurvivalVisuals.MineFill : SurvivalHookView.TapDisabled, enabled, 0.5f);
                mainText.color = HookSkin.InkFor(key);
                reroll.gameObject.SetActive(state == OrbState.Active);
                var free = _orb.FreeRerollsLeft;
                rerollText.text = free > 0
                    ? _view.TapFmt("orb.reroll_free", "New task  \u00b7  FREE ({0} left today)", free)
                    : _view.TapFmt("orb.reroll_gems", "New task  \u00b7  {0} Gems", _orb.Config.RerollGems);
            });
        }
    }
}
