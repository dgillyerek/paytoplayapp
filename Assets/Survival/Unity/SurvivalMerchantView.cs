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
    /// Wandering Merchant on the village HUD (left edge). While the caravan is in town the button shows the minutes
    /// left (and a FREE badge if one of the offers is a gift); otherwise it shows when he is due back. The sheet lists
    /// three one-off offers with the BalanceBar on top; the bundle goes through the store's purchase service (sandbox
    /// in dev, labelled). Saved in PlayerPrefs.
    /// </summary>
    public sealed class SurvivalMerchantView : MonoBehaviour
    {
        private const float YTop = 0.480f;
        private static readonly Color CardFill = new Color(0.16f, 0.12f, 0.22f, 0.96f);

        private SurvivalHookView _view = null!;
        private HookSession _hook = null!;
        private WanderingMerchant _merchant = null!;
        private TapHudParts? _button;
        private bool _wasHere;
        private bool _buying;
        private float _tick;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {
            SurvivalHookView.HudBuilt -= Attach;
            SurvivalHookView.HudBuilt += Attach;
        }

        private static void Attach(SurvivalHookView view, RectTransform root)
        {
            if (view.TapHook == null || view.GetComponent<SurvivalMerchantView>() != null)
            {
                return;
            }

            view.gameObject.AddComponent<SurvivalMerchantView>().Init(view, root);
        }

        private void Init(SurvivalHookView view, RectTransform root)
        {
            _view = view;
            _hook = view.TapHook!;
            var clock = _hook.Clock;
            _merchant = new WanderingMerchant(MerchantConfig.Default, () => clock.Now, Environment.TickCount);
            _merchant.Apply(PlayerPrefs.GetString(WanderingMerchant.PrefsKey, ""));
            _wasHere = _merchant.IsHere;

            var btn = view.TapHudButton(root, "MerchantButton", YTop, view.TapS("merchant.button", "Merchant"), new Color(0.48f, 0.20f, 0.30f, 1f), out var parts);
            _button = parts;
            var icon = Ornaments.Icon(parts.Icon, "Coin", "coin");
            SurvivalVisuals.Stretch(icon.rectTransform);
            view.TapWire(btn, () =>
            {
                view.TapPunch(parts.Root);
                if (!_merchant.IsHere)
                {
                    view.TapToast(parts.Root, view.TapFmt("merchant.away_toast", "The merchant returns in {0}", TimerQueue.FormatClock(_merchant.SecondsUntilArrival)), SurvivalVisuals.Cream);
                    return;
                }

                OpenSheet();
            });
            Save();
        }

        private void OnDestroy()
        {
            if (_merchant != null)
            {
                Save();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _merchant != null)
            {
                Save();
            }
        }

        private void Save()
        {
            PlayerPrefs.SetString(WanderingMerchant.PrefsKey, _merchant.Capture());
            PlayerPrefs.Save();
        }

        private void Update()
        {
            if (_merchant == null || _button == null)
            {
                return;
            }

            _tick -= Time.unscaledDeltaTime;
            if (_tick > 0f)
            {
                return;
            }

            _tick = 0.5f;
            if (_merchant.Tick())
            {
                Save();
            }

            var here = _merchant.IsHere;
            if (here && !_wasHere)
            {
                _view.TapToast(_button.Root, _view.TapS("merchant.arrived", "A merchant caravan has arrived!"), SurvivalVisuals.MineFill);
                _view.TapBurst(_button.Root, SurvivalVisuals.MineFill, 14);
            }

            if (!here && _wasHere && _view.TapSheetOpen)
            {
                _view.TapCloseSheet();
            }

            _wasHere = here;
            _button.Show(_merchant.HasFreeWaiting, here && !_merchant.HasFreeWaiting && AnyLeft());
            _button.Label.text = here
                ? TimerQueue.FormatClock(_merchant.SecondsLeft)
                : _view.TapS("merchant.button", "Merchant");
        }

        private bool AnyLeft()
        {
            foreach (var o in _merchant.Offers)
            {
                if (!o.Bought)
                {
                    return true;
                }
            }

            return false;
        }

        private string Bag(IReadOnlyDictionary<string, int> bag)
        {
            var sb = new StringBuilder();
            foreach (var kv in bag)
            {
                if (sb.Length > 0)
                {
                    sb.Append("  +  ");
                }

                sb.Append(SurvivalHookView.TapResGlyph(kv.Key)).Append(' ').Append(ChipWallet.FormatCompact(kv.Value)).Append(' ').Append(_view.TapResName(kv.Key));
            }

            return sb.ToString();
        }

        private void OpenSheet()
        {
            var panel = _view.TapOpenSheet("MerchantSheet", 0.90f, 0.60f, _view.TapS("merchant.title", "Wandering Merchant"));
            if (panel == null)
            {
                return;
            }

            var wallet = _hook.Wallet;
            BalanceBar.Above(panel, wallet, new[] { SurvIds.ResSoft, SurvIds.ResWood, SurvIds.ResFood, SurvIds.ResHard });
            var leave = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Leaves", "", 19, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.4f);
            SurvivalHookView.TapBox(leave.rectTransform, 0.04f, 0.81f, 0.96f, 0.87f);
            if (_hook.Store.Purchases.IsSandbox)
            {
                var sandbox = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Sandbox", _view.TapS("store.sandbox", "SANDBOX \u00b7 no real charge"), 15, TextAnchor.MiddleCenter, SurvivalHookView.TapWarn), 1.5f);
                SurvivalHookView.TapBox(sandbox.rectTransform, 0f, 0.775f, 1f, 0.81f);
            }

            var rows = new List<Action>();
            for (var i = 0; i < 3; i++)
            {
                rows.Add(BuildRow(panel, i, 0.76f - i * 0.21f));
            }

            var closeText = _view.TapSheetButton(panel, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.33f, 0.03f, 0.67f, 0.10f, 19, out var close);
            closeText.text = _view.TapS("hook.close", "Close");
            _view.TapWire(close, _view.TapCloseSheet);
            _view.TapSheetRefresh(() =>
            {
                leave.text = _view.TapFmt("merchant.leaves", "Packing up in {0}", TimerQueue.FormatClock(_merchant.SecondsLeft));
                foreach (var r in rows)
                {
                    r();
                }
            });
        }

        private Action BuildRow(RectTransform panel, int index, float yTop)
        {
            var card = HookSkin.Panel(panel, "Offer" + index, HookArtKeys.Panel, CardFill, 0.6f);
            SurvivalHookView.TapBox(card.rectTransform, 0.04f, yTop - 0.19f, 0.96f, yTop);
            var rt = card.rectTransform;
            Ornaments.Frame(rt, new Color(0.85f, 0.68f, 0.30f, 1f));
            var title = HookSkin.Outlined(SurvivalVisuals.Text(rt, "Title", "", 19, TextAnchor.MiddleLeft, SurvivalVisuals.Cream));
            title.fontStyle = FontStyle.Bold;
            SurvivalHookView.TapBox(title.rectTransform, 0.05f, 0.55f, 0.62f, 0.92f);
            var gets = HookSkin.Fit(HookSkin.Outlined(SurvivalVisuals.Text(rt, "Gets", "", 17, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.4f), 11, 17);
            SurvivalHookView.TapBox(gets.rectTransform, 0.05f, 0.08f, 0.62f, 0.55f);
            var priceText = _view.TapSheetButton(rt, "Buy", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, 0.64f, 0.16f, 0.96f, 0.84f, 17, out var buy);
            HookSkin.Fit(priceText, 11, 17);
            var buyImg = buy.GetComponent<Image>();
            var buyRt = buy.GetComponent<RectTransform>();
            var wallet = _hook.Wallet;

            _view.TapWire(buy, () => OnBuy(index, buyRt));

            return () =>
            {
                if (index >= _merchant.Offers.Count)
                {
                    card.gameObject.SetActive(false);
                    return;
                }

                var offer = _merchant.Offers[index];
                var deal = offer.Deal;
                title.text = _view.TapS("merchant.deal." + deal.Id, deal.Id);
                gets.text = Bag(deal.Grants);
                string key;
                bool on;
                if (offer.Bought)
                {
                    priceText.text = _view.TapS("merchant.sold", "Sold");
                    key = HookArtKeys.ButtonBlue;
                    on = false;
                }
                else if (offer.IsFree)
                {
                    priceText.text = _view.TapS("hook.free", "FREE");
                    key = HookArtKeys.ButtonGreen;
                    on = true;
                    var s = 1f + 0.05f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));
                    buyRt.localScale = new Vector3(s, s, 1f);
                }
                else if (deal.Kind == MerchantDealKinds.Sku)
                {
                    var product = _hook.Store.ProductFor(deal.Sku);
                    priceText.text = product != null && product.PriceLabel.Length > 0 ? product.PriceLabel : "\u2014";
                    key = HookArtKeys.ButtonGreen;
                    on = product != null && !_buying;
                }
                else
                {
                    priceText.text = Bag(deal.Price);
                    key = HookArtKeys.ButtonGold;
                    on = wallet.CanAfford(deal.Price);
                }

                HookSkin.SetButton(buyImg, key, on ? SurvivalVisuals.MineFill : SurvivalHookView.TapDisabled, on, 0.5f);
                priceText.color = HookSkin.InkFor(key);
            };
        }

        private void OnBuy(int index, RectTransform button)
        {
            if (_buying || _merchant.Check(index) != MerchantBuy.Ok)
            {
                _view.TapShake(button);
                return;
            }

            var deal = _merchant.Offers[index].Deal;
            var wallet = _hook.Wallet;
            if (deal.Kind == MerchantDealKinds.Sku)
            {
                var product = _hook.Store.ProductFor(deal.Sku);
                if (product == null)
                {
                    _view.TapShake(button);
                    return;
                }

                _buying = true;
                _hook.Store.Purchases.Purchase(product.ProductId, outcome =>
                {
                    _buying = false;
                    if (outcome.Ok && _merchant.MarkBought(index))
                    {
                        Save();
                        _view.TapCloseSheet();
                        _view.Celebrate(_view.TapS("merchant.thanks", "Pleasure doing business!"), deal.Grants, () => wallet.Grant(deal.Grants));
                    }
                    else if (!outcome.Ok)
                    {
                        _view.TapToast(button, _view.TapS("merchant.purchase_failed", "Purchase cancelled"), SurvivalHookView.TapWarn);
                    }
                });
                return;
            }

            var r = _merchant.TryBuy(index, wallet, out var grants);
            if (r != MerchantBuy.Ok || grants == null)
            {
                _view.TapShake(button);
                _view.TapToast(button, _view.TapS("hook.need_more", "Not enough resources"), SurvivalHookView.TapWarn);
                return;
            }

            Save();
            _view.TapCloseSheet();
            _view.Celebrate(_view.TapS("merchant.thanks", "Pleasure doing business!"), grants, () => wallet.Grant(grants));
        }
    }
}
