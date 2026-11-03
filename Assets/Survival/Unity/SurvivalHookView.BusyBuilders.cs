#nullable enable
using System.Collections.Generic;
using Survival.Domain.Base;
using Survival.Domain.Clock;
using Survival.Domain.Theme;
using Survival.Domain.View;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Hire-builder mode (Derek 2026-11-03): "show the building that's going so we can click on it and bring up its build
    /// screen for skipping or buying the finish". A strip of tappable rows, one per busy builder; a tap closes the sheet
    /// and opens that building (speed-up / finish-now), same deep link as Active Timers. Rows rebuild only when the set of
    /// jobs changes; each tick only the clock text and bar move.
    /// </summary>
    public sealed partial class SurvivalHookView
    {
        private const int BusyMaxRows = 3;

        /// <summary>Build the strip in <paramref name="area"/> of <paramref name="panel"/>; returns its per-tick update.</summary>
        private System.Action BusyBuilderStrip(RectTransform panel, float x0, float y0, float x1, float y1)
        {
            var go = new GameObject("BusyBuilders", typeof(RectTransform));
            go.transform.SetParent(panel, false);
            var area = (RectTransform)go.transform;
            HookSkin.Box(area, x0, y0, x1, y1);
            string? sig = null;
            var live = new List<(TimerRow row, Text clock, Image fill)>();

            return () =>
            {
                if (_hook == null || area == null)
                {
                    return;
                }

                var hook = _hook;
                var now = hook.Clock.Now;
                var rows = ActiveTimers.BusyBuilders(hook.Timers.Active, now);
                if (ActiveTimers.NeedsRebuild(sig, rows))
                {
                    sig = ActiveTimers.Signature(rows);
                    for (var i = area.childCount - 1; i >= 0; i--)
                    {
                        Destroy(area.GetChild(i).gameObject);
                    }

                    live.Clear();
                    var n = Mathf.Min(rows.Count, BusyMaxRows);
                    var h = 1f / Mathf.Max(1, n);
                    for (var i = 0; i < n; i++)
                    {
                        var row = rows[i];
                        var top = 1f - (i * h);
                        var plate = HookSkin.Panel(area, "Busy_" + row.Target, HookArtKeys.Plate, SurvivalVisuals.RowFill, 0.45f);
                        plate.raycastTarget = true;
                        HookSkin.Box(plate.rectTransform, 0f, top - h + 0.03f, 1f, top - 0.03f);
                        var btn = plate.gameObject.AddComponent<Button>();
                        btn.targetGraphic = plate;
                        plate.gameObject.AddComponent<PressFx>();
                        var target = row.Target;
                        btn.onClick.AddListener(() =>
                        {
                            CloseSheet();
                            GoToPlot(target);
                        });

                        var art = hook.Base.Find(target) is { } p ? BuildingVisual.For(p, Tiers).ArtKey : HookArtKeys.ForPlot(target);
                        var icon = HookSkin.Art(plate.rectTransform, "Icon", art, SurvivalVisuals.PinBuild);
                        HookSkin.Box(icon.rectTransform, 0.02f, 0.06f, 0.14f, 0.94f);
                        var name = HookSkin.Outlined(SurvivalVisuals.Text(plate.rectTransform, "Name", TimerPlotLabel(target), 20, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.5f);
                        name.fontStyle = FontStyle.Bold;
                        HookSkin.Box(name.rectTransform, 0.16f, 0.50f, 0.66f, 0.95f);
                        var clock = HookSkin.Outlined(SurvivalVisuals.Text(plate.rectTransform, "Clock", "", 20, TextAnchor.MiddleRight, SurvivalVisuals.Reward), 1.5f);
                        clock.fontStyle = FontStyle.Bold;
                        HookSkin.Box(clock.rectTransform, 0.66f, 0.50f, 0.96f, 0.95f);
                        var bar = HookSkin.Bar(plate.rectTransform, "Bar", out var fill);
                        HookSkin.Box(bar.rectTransform, 0.16f, 0.14f, 0.74f, 0.42f);
                        var hint = HookSkin.Outlined(SurvivalVisuals.Text(plate.rectTransform, "Hint", S("builder.busy.tap", "Speed up \u203a"), 16, TextAnchor.MiddleRight, SurvivalVisuals.Cream), 1.2f);
                        HookSkin.Box(hint.rectTransform, 0.75f, 0.06f, 0.96f, 0.48f);
                        live.Add((row, clock, fill));
                    }
                }

                foreach (var (row, clock, fill) in live)
                {
                    var done = row.IsReady(now);
                    var txt = done ? S("timers.ready", "READY") : TimerQueue.FormatClock(row.Remaining(now));
                    if (!string.Equals(clock.text, txt, System.StringComparison.Ordinal))
                    {
                        clock.text = txt;
                    }

                    HookSkin.SetBar(fill, (float)row.Progress(now), done ? HookArtKeys.BarFillGreen : HookArtKeys.BarFillBlue, SurvivalVisuals.PinBuild);
                }
            };
        }

        /// <summary>Show / hide the strip without rebuilding it.</summary>
        private static void ShowStrip(RectTransform panel, bool on)
        {
            var t = panel.Find("BusyBuilders");
            if (t != null && t.gameObject.activeSelf != on)
            {
                t.gameObject.SetActive(on);
            }
        }
    }
}
