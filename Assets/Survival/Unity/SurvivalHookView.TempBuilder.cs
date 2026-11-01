#nullable enable
using Survival.Domain.Base;
using Survival.Domain.Clock;
using Survival.Domain.Ids;
using Survival.Domain.Theme;
using Survival.Domain.View;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// The temporary builder (Derek 2026-10-30): tapping Upgrade / Build while every builder is busy opens a sheet
    /// offering one more builder for 12 hours ($1.99, sandbox). Buying starts the job that asked for it at once.
    /// While hired, the sheet's timer and the building sheet's upgrade line show how long it stays.
    /// </summary>
    public sealed partial class SurvivalHookView
    {
        /// <summary>Open the offer for <paramref name="plotId"/>'s upgrade, or for placing <paramref name="buildType"/>.</summary>
        private void OpenTempBuilder(string? plotId, string? buildType = null, string? spotId = null)
        {
            if (_hook == null)
            {
                return;
            }

            var hook = _hook;
            var panel = OpenSheet("TempBuilderSheet", 0.84f, 0.40f, S("builder.temp.title", "All Builders Busy"));
            if (panel == null)
            {
                return;
            }

            BalanceBar.Above(panel, hook.Wallet, BalanceBarIds.Building);
            var art = HookSkin.Art(panel, "Building", plotId != null && hook.Base.Find(plotId) is { } p ? BuildingVisual.For(p, Tiers).ArtKey : HookArtKeys.ForPlot(plotId ?? ""), SurvivalVisuals.PinBuild);
            Box(art.rectTransform, 0.05f, 0.58f, 0.30f, 0.88f);
            var head = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Head", S("builder.temp.head", "Hire an extra builder"), 28, TextAnchor.MiddleLeft, SurvivalVisuals.Reward), 2f);
            head.fontStyle = FontStyle.Bold;
            Box(head.rectTransform, 0.33f, 0.76f, 0.95f, 0.87f);
            var body = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Body", S("builder.temp.body", "One more builder works for you for 12 hours. This job starts right away."), 20, TextAnchor.UpperLeft, SurvivalVisuals.Cream), 1.5f);
            Box(body.rectTransform, 0.33f, 0.56f, 0.95f, 0.75f);

            var price = hook.TempBuilderProduct?.PriceLabel ?? "$1.99";
            var buyText = SheetButton(panel, "Buy", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, 0.12f, 0.33f, 0.88f, 0.49f, 28, out var buy);
            buyText.text = Fmt("builder.temp.buy", "Hire for 12h  \u00b7  {0}", price);
            var sandbox = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Sandbox", S("store.sandbox", "SANDBOX \u00b7 no real charge"), 15, TextAnchor.MiddleCenter, Warn), 1.5f);
            Box(sandbox.rectTransform, 0f, 0.26f, 1f, 0.32f);
            var closeText = SheetButton(panel, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.33f, 0.05f, 0.67f, 0.16f, 20, out var close);
            closeText.text = S("hook.close", "Close");
            var buyRt = buy.GetComponent<RectTransform>();

            Wire(close, CloseSheet);
            Wire(buy, () =>
            {
                void Done(bool ok)
                {
                    if (!ok)
                    {
                        SurvivalAudio.Play(Survival.Domain.Audio.AudioCues.UiDeny);
                        Toast(NormCenterOf(buyRt), S("builder.temp.failed", "Purchase cancelled"), Warn);
                        return;
                    }

                    SurvivalAudio.Play(Survival.Domain.Audio.AudioCues.UpgradeStart);
                    CloseSheet();
                    Toast(new Vector2(0.5f, 0.55f), S("builder.temp.hired", "Extra builder hired for 12h!"), SurvivalVisuals.PinBuild);
                    RefreshAll();
                }

                if (plotId != null)
                {
                    hook.HireTempBuilderAndUpgrade(plotId, Done);
                }
                else if (buildType != null)
                {
                    hook.HireTempBuilderAndBuild(buildType, spotId, plot => Done(plot != null));
                }
            });
        }

        /// <summary>"Extra builder 11:42:05" while the temporary builder is hired, else empty.</summary>
        private string TempBuilderLine()
        {
            if (_hook == null || !_hook.Timers.ExtraSlotActive)
            {
                return "";
            }

            return Fmt("builder.temp.left", "Extra builder  {0}", TimerQueue.FormatClock(_hook.Timers.ExtraSlotRemaining));
        }
    }
}
