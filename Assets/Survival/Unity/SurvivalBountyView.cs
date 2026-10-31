#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Survival.Domain.Clock;
using Survival.Domain.Ids;
using Survival.Domain.Session;
using Survival.Domain.Taps;
using Survival.Domain.Theme;
using Survival.Domain.World;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Tavern Bounty Board on the village HUD (left edge). The sheet pins four parchment posters; each one shows who
    /// asks, the task, the reward and a button (Accept / progress + time left / Claim / Expired). Rare posters carry a
    /// gold star and pay double. A gem button re-posts the board early. Saved in PlayerPrefs.
    /// </summary>
    public sealed class SurvivalBountyView : MonoBehaviour
    {
        private const float YTop = 0.390f;
        private static readonly Color Parchment = new Color(0.86f, 0.78f, 0.60f, 1f);
        private static readonly Color Ink = new Color(0.24f, 0.16f, 0.08f, 1f);

        private SurvivalHookView _view = null!;
        private HookSession _hook = null!;
        private BountyBoard _board = null!;
        private TapHudParts? _button;
        private float _tick;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {
            SurvivalHookView.HudBuilt -= Attach;
            SurvivalHookView.HudBuilt += Attach;
        }

        private static void Attach(SurvivalHookView view, RectTransform root)
        {
            if (view.TapHook == null || view.GetComponent<SurvivalBountyView>() != null)
            {
                return;
            }

            view.gameObject.AddComponent<SurvivalBountyView>().Init(view, root);
        }

        private void Init(SurvivalHookView view, RectTransform root)
        {
            _view = view;
            _hook = view.TapHook!;
            var clock = _hook.Clock;
            _board = new BountyBoard(BountyConfig.Default, () => clock.Now, Environment.TickCount);
            _board.Apply(PlayerPrefs.GetString(BountyBoard.PrefsKey, ""));
            ActivityFeed.Reported += OnActivity;

            var btn = view.TapHudButton(root, "BountyBoardButton", YTop, view.TapS("bounty.button", "Bounties"), new Color(0.42f, 0.26f, 0.12f, 1f), out var parts);
            _button = parts;
            var icon = Ornaments.Icon(parts.Icon, "Scroll", "banner");
            SurvivalVisuals.Stretch(icon.rectTransform);
            view.TapWire(btn, () =>
            {
                view.TapPunch(parts.Root);
                OpenSheet();
            });
            Save();
        }

        private void OnDestroy()
        {
            ActivityFeed.Reported -= OnActivity;
            if (_board != null)
            {
                Save();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _board != null)
            {
                Save();
            }
        }

        private void Save()
        {
            PlayerPrefs.SetString(BountyBoard.PrefsKey, _board.Capture());
            PlayerPrefs.Save();
        }

        private void OnActivity(string kind, int amount)
        {
            if (_board.Report(kind, amount) > 0 && _button != null)
            {
                _view.TapToast(_button.Root, _view.TapS("bounty.done_toast", "Bounty complete!"), SurvivalHookView.TapFreeGreen);
            }

            Save();
        }

        private void Update()
        {
            if (_board == null)
            {
                return;
            }

            _tick -= Time.unscaledDeltaTime;
            if (_tick > 0f)
            {
                return;
            }

            _tick = 0.5f;
            if (_board.Tick())
            {
                Save();
            }

            var ready = _board.ReadyCount;
            var fresh = _board.ActiveCount == 0 && HasOffer();
            _button?.Show(false, ready > 0 || fresh);
            if (_button != null)
            {
                _button.DotText.text = ready > 0 ? ready.ToString() : "!";
                _button.Label.text = ready > 0 ? _view.TapS("bounty.button.claim", "Claim!") : _view.TapS("bounty.button", "Bounties");
            }
        }

        private bool HasOffer()
        {
            foreach (var b in _board.Slots)
            {
                if (b.StateAt(_board.Now) == BountyState.Offered)
                {
                    return true;
                }
            }

            return false;
        }

        private string TaskText(Bounty b)
        {
            var key = "bounty.task." + b.Kind;
            return b.Kind switch
            {
                ActivityKinds.RunnerKills => _view.TapFmt(key, "Slay {0} foes on the road", b.Target),
                ActivityKinds.RunnerWin => _view.TapFmt(key, "Win {0} runner levels", b.Target),
                ActivityKinds.Harvest => _view.TapFmt(key, "Gather {0} harvests", b.Target),
                ActivityKinds.Upgrade => _view.TapFmt(key, "Upgrade {0} buildings", b.Target),
                ActivityKinds.HeroLevel => _view.TapFmt(key, "Level up heroes {0} times", b.Target),
                _ => b.Kind,
            };
        }

        private string RewardText(IReadOnlyDictionary<string, int> bag)
        {
            var sb = new StringBuilder();
            foreach (var kv in bag)
            {
                if (sb.Length > 0)
                {
                    sb.Append("   ");
                }

                sb.Append(SurvivalHookView.TapResGlyph(kv.Key)).Append(' ').Append(ChipWallet.FormatCompact(kv.Value));
            }

            return sb.ToString();
        }

        private void OpenSheet()
        {
            var panel = _view.TapOpenSheet("BountyBoardSheet", 0.92f, 0.62f, _view.TapS("bounty.title", "Tavern Bounty Board"));
            if (panel == null)
            {
                return;
            }

            var wallet = _hook.Wallet;
            BalanceBar.Above(panel, wallet, new[] { SurvIds.ResHard });

            var refresh = new List<Action>();
            for (var i = 0; i < BountyConfig.Default.Slots; i++)
            {
                refresh.Add(BuildPoster(panel, i));
            }

            var header = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Info", "", 18, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.4f);
            SurvivalHookView.TapBox(header.rectTransform, 0.04f, 0.15f, 0.96f, 0.20f);
            var buyText = _view.TapSheetButton(panel, "Repost", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, 0.08f, 0.03f, 0.60f, 0.13f, 19, out var buy);
            var closeText = _view.TapSheetButton(panel, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.64f, 0.03f, 0.92f, 0.13f, 19, out var close);
            closeText.text = _view.TapS("hook.close", "Close");
            var buyRt = buy.GetComponent<RectTransform>();
            var buyImg = buy.GetComponent<Image>();
            _view.TapWire(buy, () =>
            {
                if (_board.BuyRefresh(wallet) != BountyResult.Ok)
                {
                    _view.TapShake(buyRt);
                    _view.TapToast(buyRt, _view.TapS("hook.need_more", "Not enough resources"), SurvivalHookView.TapWarn);
                    return;
                }

                _view.TapBurst(buyRt, SurvivalVisuals.MineFill, 14);
                Save();
            });
            _view.TapWire(close, _view.TapCloseSheet);

            _view.TapSheetRefresh(() =>
            {
                foreach (var r in refresh)
                {
                    r();
                }

                header.text = _view.TapFmt2("bounty.info", "Active {0}/2  \u00b7  new posters in {1}", _board.ActiveCount, TimerQueue.FormatClock(_board.SecondsUntilRefresh));
                buyText.text = _view.TapFmt("bounty.repost", "New posters now  \u00b7  {0} Gems", _board.Config.RefreshGems);
                var can = wallet.CanAfford(SurvIds.ResHard, _board.Config.RefreshGems);
                HookSkin.SetButton(buyImg, HookArtKeys.ButtonGold, can ? SurvivalVisuals.MineFill : SurvivalHookView.TapDisabled, can, 0.5f);
            });
        }

        private Action BuildPoster(RectTransform panel, int slot)
        {
            var col = slot % 2;
            var row = slot / 2;
            var x0 = 0.04f + col * 0.47f;
            var y1 = 0.86f - row * 0.33f;
            var card = HookSkin.Panel(panel, "Poster" + slot, HookArtKeys.Scroll, Parchment, 0.6f);
            SurvivalHookView.TapBox(card.rectTransform, x0, y1 - 0.31f, x0 + 0.45f, y1);
            var rt = card.rectTransform;
            var star = Ornaments.Star(rt, new Color(1f, 0.82f, 0.25f, 1f));
            SurvivalHookView.TapBox(star.rectTransform, 0.82f, 0.80f, 0.97f, 0.98f);

            Text Line(string name, int size, float y0, float y1b, FontStyle style)
            {
                var t = SurvivalVisuals.Text(rt, name, "", size, TextAnchor.MiddleCenter, Ink);
                t.fontStyle = style;
                SurvivalHookView.TapBox(t.rectTransform, 0.06f, y0, 0.94f, y1b);
                return t;
            }

            var giver = Line("Giver", 15, 0.80f, 0.95f, FontStyle.Italic);
            var task = Line("Task", 18, 0.60f, 0.80f, FontStyle.Bold);
            var reward = Line("Reward", 16, 0.45f, 0.60f, FontStyle.Normal);
            var bar = HookSkin.Bar(rt, "Bar", out var fill);
            SurvivalHookView.TapBox(bar.rectTransform, 0.10f, 0.35f, 0.90f, 0.43f);
            var status = Line("Status", 14, 0.25f, 0.35f, FontStyle.Normal);
            var btnText = _view.TapSheetButton(rt, "Act", HookArtKeys.ButtonGreen, SurvivalHookView.TapFreeGreen, 0.12f, 0.04f, 0.88f, 0.24f, 16, out var btn);
            var btnImg = btn.GetComponent<Image>();
            var btnRt = btn.GetComponent<RectTransform>();
            var wallet = _hook.Wallet;

            _view.TapWire(btn, () =>
            {
                if (slot >= _board.Slots.Count)
                {
                    return;
                }

                var b = _board.Slots[slot];
                switch (b.StateAt(_board.Now))
                {
                    case BountyState.Offered:
                        var r = _board.Accept(slot);
                        if (r == BountyResult.TooManyActive)
                        {
                            _view.TapShake(btnRt);
                            _view.TapToast(btnRt, _view.TapS("bounty.too_many", "Finish an active bounty first"), SurvivalHookView.TapWarn);
                        }
                        else
                        {
                            _view.TapBurst(rt, Parchment, 10);
                        }

                        break;
                    case BountyState.Done:
                        var bag = _board.Claim(slot);
                        if (bag != null)
                        {
                            _view.TapCloseSheet();
                            _view.Celebrate(_view.TapS("bounty.claimed", "Bounty paid!"), bag, () => wallet.Grant(bag));
                        }

                        break;
                }

                Save();
            });

            return () =>
            {
                if (slot >= _board.Slots.Count)
                {
                    return;
                }

                var b = _board.Slots[slot];
                var now = _board.Now;
                var state = b.StateAt(now);
                star.gameObject.SetActive(b.Rare);
                giver.text = _view.TapS("bounty.giver." + b.Giver, b.Giver) + (b.Rare ? "  \u00b7  " + _view.TapS("bounty.rare", "RARE") : "");
                task.text = TaskText(b);
                reward.text = RewardText(b.Reward);
                bar.gameObject.SetActive(state == BountyState.Active || state == BountyState.Done);
                HookSkin.SetBar(fill, b.Fraction, state == BountyState.Done ? HookArtKeys.BarFillGreen : HookArtKeys.BarFillGold, SurvivalVisuals.MineFill);
                string key;
                bool on;
                switch (state)
                {
                    case BountyState.Offered:
                        status.text = _view.TapFmt("bounty.limit", "Time limit {0}", TimerQueue.FormatClock(b.LimitSeconds));
                        btnText.text = _view.TapS("bounty.accept", "Accept");
                        key = HookArtKeys.ButtonBlue;
                        on = true;
                        break;
                    case BountyState.Active:
                        status.text = _view.TapFmt2("bounty.active", "{0}/{1}", b.Progress, b.Target) + "   \u23f3 " + TimerQueue.FormatClock(b.EndsAt - now);
                        btnText.text = _view.TapS("bounty.in_progress", "In progress");
                        key = HookArtKeys.ButtonBlue;
                        on = false;
                        break;
                    case BountyState.Done:
                        status.text = _view.TapS("bounty.done", "Done!");
                        btnText.text = _view.TapS("bounty.claim", "Claim");
                        key = HookArtKeys.ButtonGreen;
                        on = true;
                        break;
                    case BountyState.Expired:
                        status.text = _view.TapS("bounty.expired", "Too late \u2014 the poster faded");
                        btnText.text = _view.TapS("bounty.expired_button", "Expired");
                        key = HookArtKeys.ButtonRed;
                        on = false;
                        break;
                    default:
                        status.text = _view.TapS("bounty.paid", "Paid \u2014 new poster soon");
                        btnText.text = _view.TapS("bounty.claimed_button", "Claimed");
                        key = HookArtKeys.ButtonBlue;
                        on = false;
                        break;
                }

                HookSkin.SetButton(btnImg, key, on ? SurvivalVisuals.PinBuild : SurvivalHookView.TapDisabled, on, 0.5f);
                btnText.color = HookSkin.InkFor(key);
            };
        }
    }
}
