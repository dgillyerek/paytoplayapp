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
            var panel = OpenSheet(DailyLoginSheetName, S("daily.title", "Royal Daily Chest"), 0.86f);
            // One navy backdrop over the panel body so the art's grey-blue gradient band never shows (Derek 2026-10-31).
            var backdrop = HookSkin.Panel(panel, "Backdrop", HookArtKeys.Plate, DailyNavyDeep, 0.8f);
            backdrop.raycastTarget = false;
            Box(backdrop.rectTransform, 0.02f, 0.012f, 0.98f, 0.925f);
            Ornaments.Frame(backdrop.transform, Ornaments.Gold, 1.5f);

            var sub = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Sub", S("daily.sub", "Come back every day. Day 7 is the Royal Chest!"), 22, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
            sub.fontStyle = FontStyle.Bold;
            HookSkin.Fit(sub, 12, 22);
            Box(sub.rectTransform, 0.05f, 0.865f, 0.95f, 0.905f);

            // Days 1-3 and 4-6 in two rows of three, the day-7 hero card across the bottom, then CLAIM: no dead space.
            const float gap = 0.022f;
            var w = (0.92f - (2 * gap)) / 3f;
            for (var i = 0; i < DailyLogin.Days - 1; i++)
            {
                var x0 = 0.04f + ((i % 3) * (w + gap));
                var y1 = i < 3 ? 0.845f : 0.575f;
                DailyBox(panel, daily, i, x0, y1 - 0.25f, x0 + w, y1);
            }

            DailyBox(panel, daily, DailyLogin.Days - 1, 0.04f, 0.13f, 0.96f, 0.305f);

            var claimLabel = SheetButton(panel, "Claim", HookArtKeys.ButtonGreen, FreeGreen, 0.12f, 0.025f, 0.88f, 0.105f, 34, out var claim);
            Ornaments.Dress(claim.targetGraphic, Ornaments.Gold, button: true);
            // Cream text with a heavy dark outline, drawn above the button's gloss so it never reads as dark-on-green.
            claimLabel.color = SurvivalVisuals.Cream;
            HookSkin.Outlined(claimLabel, 3f);
            claimLabel.transform.SetAsLastSibling();
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

                claimLabel.text = ready ? Fmt("daily.claim_day", "CLAIM DAY {0}", daily.NextDay) : Fmt("daily.next_in", "Next gift in {0}", RailTime(daily.SecondsUntilNext));
                FitButtonLabel(claimLabel, 34);
            }

            Refresh();
            _sheetRefresh = Refresh;
        }

        /// <summary>
        /// One day card in the v1 ornate style (Derek 2026-10-31, "this screen is ugly"): royal navy plate (v2, no brown) dressed with gloss,
        /// bevel, gold trim and rivets; a ribbon; painted art that grows richer toward day 7; big reward icons with cream
        /// outlined amounts. Claimed days dim behind a check, today glows with a TODAY tag, day 7 is the wide hero card.
        /// </summary>
        private void DailyBox(RectTransform panel, DailyLogin daily, int i, float x0, float y0, float x1, float y1)
        {
            var state = daily.State(i);
            var big = i == DailyLogin.Days - 1;
            var claimed = state == LoginDayState.Claimed;
            var ready = state == LoginDayState.Ready;
            var fill = big ? DailyRoyalPurple : ready ? DailyTodayBlue : DailyNavy;
            if (ready)
            {
                var glow = HookSkin.Glow(panel, "Glow" + (i + 1), new Color(1f, 0.82f, 0.35f, 0.75f));
                glow.raycastTarget = false;
                Box(glow.rectTransform, x0 - 0.02f, y0 - 0.012f, x1 + 0.02f, y1 + 0.012f);
            }

            // Royal navy plate with bevel, gloss and gold trim, like the Trophy Hall and Crystal Orb cards.
            var card = HookSkin.Panel(panel, "Day" + (i + 1), HookArtKeys.Plate, fill, 0.7f);
            card.raycastTarget = false;
            Box(card.rectTransform, x0, y0, x1, y1);
            Ornaments.Bevel(card.transform, 0.6f);
            Ornaments.Dress(card, Ornaments.Gold, rivet: 10f);
            if (ready || big)
            {
                Ornaments.Frame(card.transform, ready ? new Color(1f, 0.86f, 0.40f, 1f) : Ornaments.Gold, ready ? 4f : 3f);
            }

            var head = Ornaments.Ribbon(card.transform, big ? S("daily.day7", "DAY 7 \u00b7 ROYAL CHEST") : Fmt("daily.day", "DAY {0}", i + 1), big ? new Color(0.24f, 0.34f, 0.70f, 1f) : new Color(0.62f, 0.14f, 0.12f, 1f), out var ribbon);
            Box(ribbon.rectTransform, big ? 0.22f : 0.02f, big ? 0.84f : 0.86f, big ? 0.78f : 0.98f, big ? 1.06f : 1.04f);
            head.fontSize = big ? 22 : 19;

            var rewards = new List<KeyValuePair<string, int>>(daily.Rewards[i]);
            var art = HookSkin.Art(card.transform, "Chest", DailyLogin.ArtKey(i), SurvivalVisuals.Reward);
            art.raycastTarget = false;
            art.preserveAspect = true;
            if (big)
            {
                Box(art.rectTransform, 0.02f, 0.04f, 0.36f, 0.86f);
                for (var k = 0; k < rewards.Count; k++)
                {
                    var cx = 0.39f + ((k % 2) * 0.30f);
                    var cy1 = 0.80f - ((k / 2) * 0.38f);
                    RewardIcon(card.rectTransform, rewards[k], cx, cy1 - 0.36f, cx + 0.29f, cy1, horizontal: true, dim: false);
                }
            }
            else
            {
                // Art grows with the day: day 1 is a small gift, day 6 a brimming chest.
                var grow = 0.30f + (0.035f * i);
                Box(art.rectTransform, 0.5f - (grow * 0.85f), 0.84f - grow - 0.02f, 0.5f + (grow * 0.85f), 0.84f);
                var n = Math.Max(1, rewards.Count);
                for (var k = 0; k < rewards.Count; k++)
                {
                    var cx0 = 0.04f + (k * 0.92f / n);
                    RewardIcon(card.rectTransform, rewards[k], cx0, 0.04f, cx0 + (0.92f / n), 0.40f, horizontal: false, dim: claimed);
                }
            }

            if (claimed)
            {
                art.color = new Color(1f, 1f, 1f, 0.55f);
                var dim = SurvivalVisuals.Image(card.transform, "Dim", new Color(0f, 0f, 0f, 0.42f));
                dim.raycastTarget = false;
                SurvivalVisuals.Stretch(dim.rectTransform);
                var tick = HookSkin.Outlined(SurvivalVisuals.Text(card.transform, "Tick", "\u2714", 64, TextAnchor.MiddleCenter, FreeGreen), 2.4f);
                tick.raycastTarget = false;
                Box(tick.rectTransform, 0f, 0.35f, 1f, 0.85f);
            }
            else if (ready)
            {
                var tag = FreeTag(card.transform, "Today");
                tag.GetComponentInChildren<Text>().text = S("daily.today", "TODAY");
                // Below the ribbon on the right, so it never collides with the DAY label.
                tag.rectTransform.anchorMin = tag.rectTransform.anchorMax = new Vector2(0.80f, 0.78f);
                tag.rectTransform.sizeDelta = new Vector2(92f, 32f);
                tag.gameObject.SetActive(true);
                Ornaments.Shine(card.rectTransform);
                StartCoroutine(Breathe(card.rectTransform));
            }
        }

        private static readonly Color DailyNavyDeep = new Color(0.07f, 0.10f, 0.24f, 1f);
        private static readonly Color DailyNavy = new Color(0.12f, 0.17f, 0.36f, 1f);
        private static readonly Color DailyTodayBlue = new Color(0.20f, 0.33f, 0.66f, 1f);
        private static readonly Color DailyRoyalPurple = new Color(0.30f, 0.14f, 0.44f, 1f);

        /// <summary>A large painted reward icon with its amount in cream outlined text (no dark box behind it).</summary>
        private void RewardIcon(RectTransform card, KeyValuePair<string, int> reward, float x0, float y0, float x1, float y1, bool horizontal, bool dim)
        {
            var icon = HookSkin.ResIcon(card, "Icon_" + reward.Key, reward.Key, SurvivalVisuals.Reward, "\u2726");
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            var amount = HookSkin.Outlined(SurvivalVisuals.Text(card, "Amount_" + reward.Key, ChipAmount(reward.Key, reward.Value), horizontal ? 28 : 22, horizontal ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.8f);
            amount.fontStyle = FontStyle.Bold;
            amount.raycastTarget = false;
            HookSkin.Fit(amount, 12, horizontal ? 28 : 22);
            if (horizontal)
            {
                var mid = x0 + ((x1 - x0) * 0.42f);
                Box(icon.rectTransform, x0, y0, mid, y1);
                Box(amount.rectTransform, mid + 0.01f, y0, x1, y1);
            }
            else
            {
                var mid = y0 + ((y1 - y0) * 0.38f);
                Box(icon.rectTransform, x0, mid, x1, y1);
                Box(amount.rectTransform, x0, y0, x1, mid);
            }

            if (dim)
            {
                icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, 0.6f);
            }
        }
    }
}
