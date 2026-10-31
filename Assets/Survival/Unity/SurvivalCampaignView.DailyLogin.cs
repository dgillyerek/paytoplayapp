#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Survival.Domain.DailyLogin;
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Daily login chest: a rail button (slot 0, red "1" when today's box is ready) and a 7-day calendar sheet. Today's
    /// box glows with a FREE tag; tapping CLAIM grants it with the chest-burst celebration. Day 7 is the big chest.
    /// Opens by itself once per launch when a box is waiting. Saved in daily_login.json next to the campaign save.
    /// </summary>
    public sealed partial class SurvivalCampaignView
    {
        public const string DailyLoginSheetName = "DailyLoginSheet";
        public const string DailyLoginRailName = "DailyLogin";

        private DailyLogin? _daily;
        private Image? _dailyBadge;
        private Text? _dailyBadgeCount;
        private bool _dailyAutoShown;
        private float _dailyAutoDelay = 2.5f;

        /// <summary>The daily login calendar (null until the Play HUD is up).</summary>
        public DailyLogin? Daily => _daily;

        public static string DailyLoginPath => Path.Combine(Application.persistentDataPath, DailyLogin.FileName);

        partial void EngageAttach_Daily()
        {
            _daily = new DailyLogin(Campaign!.Clock, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalSeconds);
            try
            {
                if (File.Exists(DailyLoginPath) && !_daily.Load(File.ReadAllText(DailyLoginPath)))
                {
                    Debug.LogWarning("Daily login: save unreadable, starting a fresh week. " + DailyLoginPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            _daily.Changed += SaveDaily;
            RailButton(0, DailyLoginRailName, "\u2756", S("daily.rail", "DAILY"), new Color(0.62f, 0.16f, 0.14f, 1f), OpenDailyLogin, out var badge, out var count);
            _dailyBadge = badge;
            _dailyBadgeCount = count;
        }

        partial void EngageTick_Daily()
        {
            if (_daily == null)
            {
                return;
            }

            RailBadge(_dailyBadge, _dailyBadgeCount, _daily.CanClaim ? 1 : 0);
            if (!_dailyAutoShown && _daily.CanClaim)
            {
                _dailyAutoDelay -= 0.25f;
                if (_dailyAutoDelay <= 0f && !EngageBusy)
                {
                    _dailyAutoShown = true;
                    OpenDailyLogin();
                }
            }
        }

        private void SaveDaily()
        {
            if (_daily == null)
            {
                return;
            }

            try
            {
                File.WriteAllText(DailyLoginPath, _daily.Save());
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void OpenDailyLogin()
        {
            if (_daily == null || _overlay == null)
            {
                return;
            }

            _dailyAutoShown = true;
            var daily = _daily;
            var panel = OpenSheet(DailyLoginSheetName, S("daily.title", "Royal Daily Chest"), 0.74f);
            var sub = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Sub", S("daily.sub", "Return every day for a bigger gift. Day 7 is the Royal Chest!"), 22, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.2f);
            HookSkin.Fit(sub, 12, 22);
            Box(sub.rectTransform, 0.05f, 0.80f, 0.95f, 0.87f);

            // Days 1-4 on the top row, 5-6 below, the big day-7 chest beside them.
            var w = 0.225f;
            for (var i = 0; i < DailyLogin.Days; i++)
            {
                float x0, y0, x1, y1;
                if (i < 4)
                {
                    x0 = 0.04f + (i * (w + 0.008f));
                    x1 = x0 + w;
                    y0 = 0.53f;
                    y1 = 0.79f;
                }
                else if (i < 6)
                {
                    x0 = 0.04f + ((i - 4) * (w + 0.008f));
                    x1 = x0 + w;
                    y0 = 0.25f;
                    y1 = 0.51f;
                }
                else
                {
                    x0 = 0.04f + (2 * (w + 0.008f));
                    x1 = 0.96f;
                    y0 = 0.25f;
                    y1 = 0.51f;
                }

                DailyBox(panel, daily, i, x0, y0, x1, y1);
            }

            var claimLabel = SheetButton(panel, "Claim", HookArtKeys.ButtonGreen, FreeGreen, 0.22f, 0.07f, 0.78f, 0.17f, 34, out var claim);
            var timer = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Timer", "", 22, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.2f);
            Box(timer.rectTransform, 0.05f, 0.175f, 0.95f, 0.235f);
            Wire(claim, () =>
            {
                var day = daily.NextDay;
                var got = daily.Claim(Campaign!.Wallet);
                if (got == null)
                {
                    return;
                }

                CloseSheet();
                Celebrate(day == DailyLogin.Days ? S("daily.big", "ROYAL CHEST!") : Fmt("daily.claimed", "Day {0} gift claimed!", day), new Dictionary<string, int>(got));
            });

            void Refresh()
            {
                var ready = daily.CanClaim;
                claim.interactable = ready;
                if (claim.targetGraphic is Image bg)
                {
                    HookSkin.SetButton(bg, HookArtKeys.ButtonGreen, FreeGreen, ready);
                }

                claimLabel.text = ready ? S("daily.claim", "CLAIM FREE") : S("daily.come_back", "COME BACK TOMORROW");
                FitButtonLabel(claimLabel, 34);
                timer.text = ready ? "" : Fmt("daily.next_in", "Next gift in {0}", RailTime(daily.SecondsUntilNext));
            }

            Refresh();
            _sheetRefresh = Refresh;
        }

        private void DailyBox(RectTransform panel, DailyLogin daily, int i, float x0, float y0, float x1, float y1)
        {
            var state = daily.State(i);
            var big = i == DailyLogin.Days - 1;
            var fill = state == LoginDayState.Ready ? new Color(0.20f, 0.42f, 0.22f, 1f)
                : state == LoginDayState.Claimed ? new Color(0.16f, 0.17f, 0.20f, 1f)
                : big ? new Color(0.36f, 0.16f, 0.42f, 1f) : new Color(0.14f, 0.20f, 0.36f, 1f);
            var card = SurvivalVisuals.Image(panel, "Day" + (i + 1), fill);
            card.raycastTarget = false;
            Box(card.rectTransform, x0, y0, x1, y1);
            Ornaments.Grain(card.transform, new Color(1f, 1f, 1f, 0.10f));
            Ornaments.Bevel(card.transform, 0.7f);
            Ornaments.Frame(card.transform, big ? new Color(0.86f, 0.42f, 1f, 1f) : Ornaments.Gold, 4f);

            var head = Ornaments.Ribbon(card.transform, big ? S("daily.day7", "DAY 7 \u00b7 ROYAL CHEST") : Fmt("daily.day", "DAY {0}", i + 1), big ? new Color(0.50f, 0.18f, 0.60f, 1f) : new Color(0.55f, 0.12f, 0.12f, 1f), out var ribbon);
            Box(ribbon.rectTransform, -0.04f, 0.76f, 1.04f, 1.02f);
            head.fontSize = big ? 20 : 17;

            var chest = HookSkin.Outlined(SurvivalVisuals.Text(card.transform, "Chest", big ? "\u2756\u2756\u2756" : "\u2756", big ? 46 : 36, TextAnchor.MiddleCenter, big ? new Color(1f, 0.82f, 0.30f, 1f) : SurvivalVisuals.Cream), 2f);
            chest.raycastTarget = false;
            Box(chest.rectTransform, 0f, 0.46f, 1f, 0.76f);
            PackChips(card.rectTransform, new Dictionary<string, int>(daily.Rewards[i]), 0.03f, 0.04f, 0.97f, 0.46f);

            if (state == LoginDayState.Claimed)
            {
                var dim = SurvivalVisuals.Image(card.transform, "Dim", new Color(0f, 0f, 0f, 0.45f));
                dim.raycastTarget = false;
                SurvivalVisuals.Stretch(dim.rectTransform);
                var tick = HookSkin.Outlined(SurvivalVisuals.Text(card.transform, "Tick", "\u2714", 64, TextAnchor.MiddleCenter, FreeGreen), 2f);
                tick.raycastTarget = false;
                SurvivalVisuals.Stretch(tick.rectTransform);
            }
            else if (state == LoginDayState.Ready)
            {
                FreeTag(card.transform, "Free").gameObject.SetActive(true);
                Ornaments.Shine(card.rectTransform);
                Ornaments.Sparkles(card.rectTransform, 5, Ornaments.Gold);
                StartCoroutine(Breathe(card.rectTransform));
            }
        }
    }
}
