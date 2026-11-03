#nullable enable
using System.Collections.Generic;
using Survival.Domain.Ids;
using Survival.Domain.Theme;
using Survival.Domain.View;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Active Timers (Derek 2026-11-03: "some kind of place where I can see what's current under timers so that I can
    /// click and go directly to that appropriate place"). An hourglass on the town side rail, shown only while something
    /// is running, with a count badge. Tap opens a sheet listing every running timer (builders, hero training, world
    /// marches), soonest first, each with an icon, name, live countdown and bar; tap a row to go there.
    /// The sheet is built once; ticks only touch countdown text and bars, and rows rebuild only when the set changes.
    /// </summary>
    public sealed partial class SurvivalCampaignView
    {
        public const string TimersRailName = "Timers";
        public const string TimersSheetName = "ActiveTimersSheet";
        private const float TimersRailTop = 0.222f; // above Daily (slot 0): the busiest thing on the rail
        private const int TimersMaxRows = 8;

        private bool _timersRailBuilt;
        private Image? _timersDot;
        private Text? _timersDotText;
        private int _timersBadgeShown = -1;

        private RectTransform? _timersList;
        private Text? _timersEmpty;
        private string? _timersSig;
        private readonly List<(TimerRow Row, Text Clock, Image Fill, Text? Ready)> _timerRows = new();

        partial void EngageTick_Timers()
        {
            if (!_timersRailBuilt)
            {
                _timersRailBuilt = true;
                var root = _play!.Root!;
                var parts = SurvivalSideRail.Round(root, RailPrefix + TimersRailName, null, S("timers.rail", "TIMERS"), new Color(0.38f, 0.26f, 0.62f, 1f));
                var rt = parts.Root;
                HookSkin.Box(rt, 0.018f, 1f - (TimersRailTop + 0.070f), 0.150f, 1f - TimersRailTop);
                var icon = HookSkin.ResIcon(parts.Icon, "Hourglass", SurvIds.ResSpeedupM, SurvivalVisuals.Reward, "\u231b");
                icon.raycastTarget = false;
                SurvivalVisuals.Stretch(icon.rectTransform);
                parts.Button.onClick.AddListener(() =>
                {
                    Juice(rt);
                    OpenActiveTimers();
                });
                _timersDot = parts.Dot;
                _timersDotText = parts.DotText;
                _play.RegisterRail(RailPrefix + TimersRailName, TimersRailTop, rt);
            }

            var rows = GatherTimers();
            RailShown(TimersRailName, RailActionable.Timers(rows.Count));
            if (rows.Count != _timersBadgeShown)
            {
                _timersBadgeShown = rows.Count;
                RailBadge(_timersDot, _timersDotText, rows.Count);
            }
        }

        private double TimersNow => _session?.Hook != null ? _session.Hook.Clock.Now : Campaign!.Roster.Clock.Now;

        private List<TimerRow> GatherTimers() => ActiveTimers.Gather(
            _session?.Hook?.Timers.Active,
            Campaign?.Roster.Training,
            _play?.WorldView?.Sim?.Marches,
            TimersNow);

        public void OpenActiveTimers()
        {
            if (Campaign == null || _overlay == null)
            {
                return;
            }

            var panel = OpenSheet(TimersSheetName, S("timers.title", "Active Timers"), 0.74f);
            var listGo = new GameObject("List", typeof(RectTransform));
            listGo.transform.SetParent(panel, false);
            _timersList = (RectTransform)listGo.transform;
            HookSkin.Box(_timersList, 0.04f, 0.05f, 0.96f, 0.86f);
            _timersEmpty = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Empty", S("timers.empty", "Nothing running right now"), 24, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.8f);
            HookSkin.Box(_timersEmpty.rectTransform, 0.05f, 0.40f, 0.95f, 0.55f);
            _timersSig = null;
            TimersSheetTick();
            _sheetRefresh = TimersSheetTick;
        }

        private void TimersSheetTick()
        {
            if (_timersList == null)
            {
                return;
            }

            var rows = GatherTimers();
            if (ActiveTimers.NeedsRebuild(_timersSig, rows))
            {
                _timersSig = ActiveTimers.Signature(rows);
                BuildTimerRows(rows);
            }

            var now = TimersNow;
            foreach (var (row, clock, fill, ready) in _timerRows)
            {
                var done = row.IsReady(now);
                var txt = done ? S("timers.ready", "READY") : RailTime(row.Remaining(now));
                if (!string.Equals(clock.text, txt, System.StringComparison.Ordinal))
                {
                    clock.text = txt; // only when the shown seconds change
                }

                HookSkin.SetBar(fill, (float)row.Progress(now), done ? HookArtKeys.BarFillGreen : HookArtKeys.BarFillBlue, SurvivalVisuals.PinBuild);
                if (ready != null && ready.transform.parent.gameObject.activeSelf != done)
                {
                    ready.transform.parent.gameObject.SetActive(done);
                }
            }
        }

        private void BuildTimerRows(List<TimerRow> rows)
        {
            var list = _timersList!;
            for (var i = list.childCount - 1; i >= 0; i--)
            {
                Destroy(list.GetChild(i).gameObject);
            }

            _timerRows.Clear();
            if (_timersEmpty != null)
            {
                _timersEmpty.gameObject.SetActive(rows.Count == 0);
            }

            var h = 1f / TimersMaxRows;
            for (var i = 0; i < rows.Count && i < TimersMaxRows; i++)
            {
                var row = rows[i];
                var y1 = 1f - (i * h);
                var plate = HookSkin.Panel(list, "Row_" + row.Id, HookArtKeys.Plate, SurvivalVisuals.RowFill, 0.45f);
                plate.raycastTarget = true;
                HookSkin.Box(plate.rectTransform, 0f, y1 - h + 0.008f, 1f, y1 - 0.008f);
                var btn = plate.gameObject.AddComponent<Button>();
                btn.targetGraphic = plate;
                plate.gameObject.AddComponent<PressFx>();
                var go = row;
                btn.onClick.AddListener(() => GoToTimer(go));

                var icon = TimerIcon(plate.rectTransform, row);
                HookSkin.Box(icon.rectTransform, 0.02f, 0.10f, 0.14f, 0.90f);
                var name = HookSkin.Outlined(SurvivalVisuals.Text(plate.rectTransform, "Name", TimerLabel(row), 22, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.5f);
                name.fontStyle = FontStyle.Bold;
                HookSkin.Box(name.rectTransform, 0.17f, 0.50f, 0.70f, 0.94f);
                var clock = HookSkin.Outlined(SurvivalVisuals.Text(plate.rectTransform, "Clock", "", 22, TextAnchor.MiddleRight, SurvivalVisuals.Reward), 1.5f);
                clock.fontStyle = FontStyle.Bold;
                HookSkin.Box(clock.rectTransform, 0.70f, 0.50f, 0.96f, 0.94f);
                var bar = HookSkin.Bar(plate.rectTransform, "Bar", out var fill);
                HookSkin.Box(bar.rectTransform, 0.17f, 0.14f, 0.74f, 0.42f);

                // READY: green Claim pill (cream text on green, never dark text on gold); shown once the timer ends.
                var claim = HookSkin.Button(plate.rectTransform, "Claim", HookArtKeys.ButtonGreen, new Color(0.22f, 0.58f, 0.30f, 1f));
                HookSkin.Box(claim.GetComponent<RectTransform>(), 0.77f, 0.08f, 0.97f, 0.46f);
                claim.onClick.AddListener(() => GoToTimer(go));
                var claimText = HookSkin.Outlined(SurvivalVisuals.Text(claim.transform, "T", S("timers.claim", "CLAIM"), 18, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
                claimText.fontStyle = FontStyle.Bold;
                SurvivalVisuals.Stretch(claimText.rectTransform);
                claim.gameObject.SetActive(false);
                _timerRows.Add((row, clock, fill, claimText));
            }

            if (rows.Count > TimersMaxRows)
            {
                // More than fit: the last row says how many more (rare: builders + heroes + marches).
                var more = HookSkin.Outlined(SurvivalVisuals.Text(list, "More", Fmt("timers.more", "+{0} more", rows.Count - TimersMaxRows), 18, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
                HookSkin.Box(more.rectTransform, 0f, -0.06f, 1f, 0f);
            }
        }

        private Image TimerIcon(RectTransform parent, TimerRow row) => row.Kind switch
        {
            TimerKind.HeroTraining => HookSkin.Art(parent, "Icon", HookArtKeys.HeroTome, HeroGlow),
            TimerKind.March => HookSkin.Art(parent, "Icon", HookArtKeys.WarBanner, SurvivalVisuals.PinBuild),
            _ => HookSkin.Art(parent, "Icon", HookArtKeys.ForPlot(row.Target), SurvivalVisuals.PinBuild)
        };

        private string TimerLabel(TimerRow row)
        {
            switch (row.Kind)
            {
                case TimerKind.HeroTraining:
                    return Fmt("timers.training", "{0} training", HeroName(row.Target));
                case TimerKind.March:
                    var where = _play?.WorldView?.Sim != null && _play.WorldView.Sim.TryGet(row.Target, out var e) ? e.Name : row.Target;
                    return row.Id.EndsWith("." + Survival.Domain.World.MarchLeg.Return, System.StringComparison.Ordinal)
                        ? Fmt("timers.march_back", "Returning from {0}", where)
                        : Fmt("timers.march", "March to {0}", where);
                default:
                    return _hook != null ? _hook.TimerPlotLabel(row.Target) : row.Target;
            }
        }

        /// <summary>Row tap: close the sheet and go where the timer lives.</summary>
        private void GoToTimer(TimerRow row)
        {
            CloseSheet();
            switch (row.Go)
            {
                case TimerGoKind.HeroDetail:
                    _foesTab = false;
                    _selected = row.Target;
                    OpenHeroes();
                    break;
                case TimerGoKind.WorldEntity:
                    _play?.ShowScreen(PlayScreen.World);
                    _play?.WorldView?.FocusEntity(row.Target);
                    break;
                default:
                    _hook?.GoToPlot(row.Target);
                    break;
            }
        }
    }
}
