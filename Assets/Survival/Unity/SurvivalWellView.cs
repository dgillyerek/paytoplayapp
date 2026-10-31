#nullable enable
using System;
using Survival.Domain.Clock;
using Survival.Domain.Ids;
using Survival.Domain.Session;
using Survival.Domain.Taps;
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Wishing Well on the village HUD (left edge). FREE badge while today's free toss is waiting. The sheet shows
    /// the BalanceBar (gold + gems), a big coin over the well, the price of the next toss and a "make a wish" button;
    /// each toss plays the coin-drop sparkle and the reward celebration (bigger for the jackpot). Saved in PlayerPrefs.
    /// </summary>
    public sealed class SurvivalWellView : MonoBehaviour
    {
        private const float YTop = 0.570f;
        private static readonly Color Water = new Color(0.35f, 0.70f, 1f, 1f);

        private SurvivalHookView _view = null!;
        private HookSession _hook = null!;
        private WishingWell _well = null!;
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
            if (view.TapHook == null || view.GetComponent<SurvivalWellView>() != null)
            {
                return;
            }

            view.gameObject.AddComponent<SurvivalWellView>().Init(view, root);
        }

        private void Init(SurvivalHookView view, RectTransform root)
        {
            _view = view;
            _hook = view.TapHook!;
            var clock = _hook.Clock;
            _well = new WishingWell(WellConfig.Default, () => clock.Now, Environment.TickCount);
            _well.Apply(PlayerPrefs.GetString(WishingWell.PrefsKey, ""));

            var btn = view.TapHudButton(root, "WishingWellButton", YTop, view.TapS("well.button", "Well"), new Color(0.14f, 0.34f, 0.55f, 1f), out var parts);
            _button = parts;
            var icon = Ornaments.Icon(parts.Icon, "Coin", "coin");
            SurvivalVisuals.Stretch(icon.rectTransform);
            view.TapWire(btn, () =>
            {
                view.TapPunch(parts.Root);
                OpenSheet();
            });
        }

        private void OnDestroy()
        {
            if (_well != null)
            {
                Save();
            }
        }

        private void Save()
        {
            PlayerPrefs.SetString(WishingWell.PrefsKey, _well.Capture());
            PlayerPrefs.Save();
        }

        private void Update()
        {
            if (_well == null || _button == null)
            {
                return;
            }

            _tick -= Time.unscaledDeltaTime;
            if (_tick > 0f)
            {
                return;
            }

            _tick = 0.5f;
            var free = _well.FreeAvailable;
            _button.Show(free, false);
            _button.Label.text = free ? _view.TapS("well.button.free", "Free wish!") : _view.TapS("well.button", "Well");
        }

        private string CostText(WellCost c) => c.Kind switch
        {
            WellCostKind.Free => _view.TapS("well.toss_free", "Make a wish  \u00b7  FREE"),
            WellCostKind.Gold => _view.TapFmt("well.toss_gold", "Make a wish  \u00b7  {0} Gold", c.Amount.ToString("N0")),
            WellCostKind.Gems => _view.TapFmt("well.toss_gems", "Make a wish  \u00b7  {0} Gems", c.Amount),
            _ => _view.TapS("well.sold_out", "The well rests until tomorrow"),
        };

        private void OpenSheet()
        {
            var panel = _view.TapOpenSheet("WishingWellSheet", 0.84f, 0.48f, _view.TapS("well.title", "Wishing Well"));
            if (panel == null)
            {
                return;
            }

            var wallet = _hook.Wallet;
            BalanceBar.Above(panel, wallet, new[] { SurvIds.ResSoft, SurvIds.ResHard });
            var glow = HookSkin.Glow(panel, "WellGlow", new Color(Water.r, Water.g, Water.b, 0.55f));
            SurvivalHookView.TapBox(glow.rectTransform, 0.25f, 0.50f, 0.75f, 0.90f);
            var coin = Ornaments.Icon(panel, "WellCoin", "coin");
            SurvivalHookView.TapBox(coin.rectTransform, 0.40f, 0.60f, 0.60f, 0.82f);
            Ornaments.Sparkles(coin.rectTransform, 4, Water);

            var line = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Line", "", 20, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.4f);
            SurvivalHookView.TapBox(line.rectTransform, 0.05f, 0.40f, 0.95f, 0.50f);
            var tossText = _view.TapSheetButton(panel, "Toss", HookArtKeys.ButtonGreen, SurvivalHookView.TapFreeGreen, 0.10f, 0.20f, 0.90f, 0.36f, 23, out var toss);
            var closeText = _view.TapSheetButton(panel, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.33f, 0.04f, 0.67f, 0.15f, 19, out var close);
            closeText.text = _view.TapS("hook.close", "Close");
            var tossImg = toss.GetComponent<Image>();
            var tossRt = toss.GetComponent<RectTransform>();

            _view.TapWire(toss, () =>
            {
                var result = _well.Toss(wallet);
                if (result == null)
                {
                    _view.TapShake(tossRt);
                    if (_well.NextCost.Kind != WellCostKind.SoldOut)
                    {
                        _view.TapToast(tossRt, _view.TapS("hook.need_more", "Not enough resources"), SurvivalHookView.TapWarn);
                    }

                    return;
                }

                Save();
                _view.TapBurst(coin.rectTransform, Water, result.Jackpot ? 30 : 16);
                _view.TapCloseSheet();
                var title = result.Jackpot ? _view.TapS("well.jackpot", "JACKPOT! The well overflows!") : _view.TapS("well.granted", "Your wish is granted!");
                _view.Celebrate(title, result.Rewards, () => wallet.Grant(result.Rewards));
            });
            _view.TapWire(close, _view.TapCloseSheet);

            _view.TapSheetRefresh(() =>
            {
                var cost = _well.NextCost;
                tossText.text = CostText(cost);
                line.text = cost.Kind == WellCostKind.Free
                    ? _view.TapS("well.free_line", "Your daily wish is free!")
                    : _view.TapFmt("well.reset_line", "Free wish again in {0}", TimerQueue.FormatClock(_well.SecondsUntilReset));
                var on = cost.Kind != WellCostKind.SoldOut && (cost.Resource == null || wallet.CanAfford(cost.Resource, cost.Amount));
                var key = cost.Kind == WellCostKind.Free ? HookArtKeys.ButtonGreen : HookArtKeys.ButtonGold;
                HookSkin.SetButton(tossImg, key, on ? SurvivalVisuals.MineFill : SurvivalHookView.TapDisabled, on, 0.5f);
                tossText.color = HookSkin.InkFor(key);
                var bob = Mathf.Sin(Time.unscaledTime * 2.2f) * 4f;
                coin.rectTransform.anchoredPosition = new Vector2(0f, bob);
            });
        }
    }
}
