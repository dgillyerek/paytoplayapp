#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Survival.Domain.Clock;
using Survival.Domain.Ids;
using Survival.Domain.Runner;
using Survival.Domain.Session;
using Survival.Domain.Taps;
using Survival.Domain.Theme;
using Survival.Domain.View;
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

            // Tavern board header: the painted board with what to do here.
            var board = HookSkin.Art(panel, "TavernBoard", HookArtKeys.TavernBoard, new Color(0.36f, 0.22f, 0.10f, 1f));
            board.raycastTarget = false;
            SurvivalHookView.TapBox(board.rectTransform, 0.03f, 0.84f, 0.97f, 0.975f);
            var explain = HookSkin.Outlined(SurvivalVisuals.Text(board.transform, "Explain", _view.TapFmt("bounty.explain", "Accept up to {0} posters, do the task before time runs out, then claim the reward.", BountyConfig.Default.MaxActive), 18, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.4f);
            explain.raycastTarget = false;
            HookSkin.Fit(explain, 11, 18);
            SurvivalHookView.TapBox(explain.rectTransform, 0.20f, 0.14f, 0.72f, 0.86f);

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

                header.text = string.Format(_view.TapS("bounty.info", "Active {0}/{1}  \u00b7  new posters in {2}"), _board.ActiveCount, _board.Config.MaxActive, TimerQueue.FormatClock(_board.SecondsUntilRefresh));
                buyText.text = _view.TapFmt("bounty.repost", "New posters now  \u00b7  {0} Gems", _board.Config.RefreshGems);
                var can = wallet.CanAfford(SurvIds.ResHard, _board.Config.RefreshGems);
                HookSkin.SetButton(buyImg, HookArtKeys.ButtonGold, can ? SurvivalVisuals.MineFill : SurvivalHookView.TapDisabled, can, 0.5f);
            });
        }

        private static string TargetArt(string kind) => kind switch
        {
            ActivityKinds.RunnerKills => BottomNav.IconBattle,
            ActivityKinds.RunnerWin => RunnerArtKeys.EventIcon,
            ActivityKinds.Harvest => HookArtKeys.ForResource(SurvIds.ResFood),
            ActivityKinds.Upgrade => BottomNav.IconBuild,
            _ => BottomNav.IconHeroes,
        };

        /// <summary>Takes the player to where the bounty's task is done.</summary>
        private void GoDo(string kind)
        {
            _view.TapCloseSheet();
            switch (kind)
            {
                case ActivityKinds.RunnerKills:
                case ActivityKinds.RunnerWin:
                    FindFirstObjectByType<SurvivalRunnerEntry>()?.OnRunnerTapped();
                    break;
                case ActivityKinds.HeroLevel:
                    FindFirstObjectByType<SurvivalCampaignView>()?.OpenHeroes();
                    break;
                // Harvest and upgrades happen in the village behind the sheet.
            }
        }

        private Action BuildPoster(RectTransform panel, int slot)
        {
            var col = slot % 2;
            var row = slot / 2;
            var x0 = 0.04f + col * 0.47f;
            var y1 = 0.825f - row * 0.31f;
            var card = HookSkin.Art(panel, "Poster" + slot, HookArtKeys.WantedPoster, Parchment);
            card.raycastTarget = false;
            SurvivalHookView.TapBox(card.rectTransform, x0, y1 - 0.30f, x0 + 0.45f, y1);
            var rt = card.rectTransform;
            var star = Ornaments.Star(rt, new Color(1f, 0.82f, 0.25f, 1f));
            SurvivalHookView.TapBox(star.rectTransform, 0.78f, 0.80f, 0.95f, 0.96f);

            Text Line(string name, int size, float y0, float y1b, FontStyle style)
            {
                var t = SurvivalVisuals.Text(rt, name, "", size, TextAnchor.MiddleCenter, Ink);
                t.fontStyle = style;
                t.raycastTarget = false;
                HookSkin.Fit(t, 10, size);
                SurvivalHookView.TapBox(t.rectTransform, 0.10f, y0, 0.90f, y1b);
                return t;
            }

            var wanted = Line("Wanted", 20, 0.86f, 0.96f, FontStyle.Bold);
            wanted.text = _view.TapS("bounty.wanted", "WANTED");
            var target = HookSkin.Art(rt, "Target", BottomNav.IconBattle, Ink);
            target.preserveAspect = true;
            target.raycastTarget = false;
            SurvivalHookView.TapBox(target.rectTransform, 0.32f, 0.64f, 0.68f, 0.86f);
            var giver = Line("Giver", 14, 0.57f, 0.64f, FontStyle.Italic);
            var task = Line("Task", 17, 0.47f, 0.57f, FontStyle.Bold);
            var rewardRow = new GameObject("Reward", typeof(RectTransform)).GetComponent<RectTransform>();
            rewardRow.SetParent(rt, false);
            SurvivalHookView.TapBox(rewardRow, 0.12f, 0.385f, 0.88f, 0.47f);
            var rewardKey = "";
            var bar = HookSkin.Bar(rt, "Bar", out var fill);
            SurvivalHookView.TapBox(bar.rectTransform, 0.12f, 0.30f, 0.88f, 0.38f);
            var status = Line("Status", 13, 0.22f, 0.30f, FontStyle.Normal);
            var btnText = _view.TapSheetButton(rt, "Act", HookArtKeys.ButtonGreen, SurvivalHookView.TapFreeGreen, 0.14f, 0.05f, 0.86f, 0.21f, 16, out var btn);
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
                    case BountyState.Active:
                        GoDo(b.Kind);
                        return;
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
                target.sprite = SurvivalArt.Get(TargetArt(b.Kind)) ?? target.sprite;
                var rk = RewardText(b.Reward);
                if (rk != rewardKey)
                {
                    rewardKey = rk;
                    for (var c = rewardRow.childCount - 1; c >= 0; c--)
                    {
                        Destroy(rewardRow.GetChild(c).gameObject);
                    }

                    var n = Math.Max(1, b.Reward.Count);
                    var k = 0;
                    foreach (var kv in b.Reward)
                    {
                        var cx = (float)k / n;
                        var ic = HookSkin.ResIcon(rewardRow, "Ic" + k, kv.Key, SurvivalVisuals.MineFill, SurvivalHookView.TapResGlyph(kv.Key));
                        ic.raycastTarget = false;
                        SurvivalHookView.TapBox(ic.rectTransform, cx, 0f, cx + (0.38f / n), 1f);
                        var amt = HookSkin.Outlined(SurvivalVisuals.Text(rewardRow, "N" + k, ChipWallet.FormatCompact(kv.Value), 15, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.2f);
                        amt.raycastTarget = false;
                        SurvivalHookView.TapBox(amt.rectTransform, cx + (0.38f / n), 0f, cx + (1f / n), 1f);
                        k++;
                    }
                }
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
                        status.text = _view.TapFmt2("bounty.active", "{0}/{1}", b.Progress, b.Target) + "  \u00b7  " + _view.TapFmt("bounty.time_left", "{0} left", TimerQueue.FormatClock(b.EndsAt - now));
                        btnText.text = _view.TapS("bounty.go", "Go \u25b6");
                        key = HookArtKeys.ButtonBlue;
                        on = true;
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
                btnText.color = SurvivalVisuals.Cream;
                HookSkin.Outlined(btnText, 1.2f);
            };
        }
    }
}
