#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Survival.Domain.Theme;
using Survival.Domain.TrophyHall;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Trophy Hall: a rail button (slot 1) with a red count of trophies ready to claim. The sheet lists one row per goal
    /// (buildings, hero stars, runner, heroes, wins); each row shows its current tier, a progress bar, the reward and a
    /// CLAIM button. Progress is read live from the game; claims are saved in trophy_hall.json.
    /// </summary>
    public sealed partial class SurvivalCampaignView
    {
        public const string TrophyHallSheetName = "TrophyHallSheet";
        public const string TrophyHallRailName = "TrophyHall";

        private static readonly string[] TrophyTierNames = { "I", "II", "III", "IV", "V" };

        private TrophyHall? _trophies;
        private Image? _trophyBadge;
        private Text? _trophyBadgeCount;

        /// <summary>The Trophy Hall (null until the Play HUD is up).</summary>
        public TrophyHall? Trophies => _trophies;

        public static string TrophyHallPath => Path.Combine(Application.persistentDataPath, TrophyHall.FileName);

        partial void EngageAttach_Trophy()
        {
            _trophies = new TrophyHall(TrophyProgress);
            try
            {
                if (File.Exists(TrophyHallPath) && !_trophies.Load(File.ReadAllText(TrophyHallPath)))
                {
                    Debug.LogWarning("Trophy Hall: save unreadable, starting fresh. " + TrophyHallPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            _trophies.Changed += SaveTrophies;
            RailButton(1, TrophyHallRailName, "\u2655", S("trophy.rail", "TROPHIES"), new Color(0.72f, 0.52f, 0.12f, 1f), OpenTrophyHall, out var badge, out var count);
            _trophyBadge = badge;
            _trophyBadgeCount = count;
        }

        partial void EngageTick_Trophy()
        {
            if (_trophies != null)
            {
                RailBadge(_trophyBadge, _trophyBadgeCount, _trophies.ReadyCount);
            }
        }

        /// <summary>Live counts for each trophy kind. The only place the Trophy Hall reads the game.</summary>
        private int TrophyProgress(string kind)
        {
            var c = Campaign;
            if (c == null)
            {
                return 0;
            }

            switch (kind)
            {
                case TrophyKinds.BuildingUpgrades:
                {
                    var n = 0;
                    var plots = _session?.Hook?.Base.Plots;
                    if (plots != null)
                    {
                        foreach (var p in plots)
                        {
                            n += Math.Max(0, p.Level - 1);
                        }
                    }

                    return n;
                }

                case TrophyKinds.HeroStars:
                {
                    var best = 0;
                    foreach (var h in c.Roster.All)
                    {
                        if (h.Owned)
                        {
                            best = Math.Max(best, h.Stars);
                        }
                    }

                    return best;
                }

                case TrophyKinds.RunnerCleared:
                    return Math.Max(0, c.RunnerUnlocked - 1);
                case TrophyKinds.HeroesOwned:
                {
                    var n = 0;
                    foreach (var _ in c.Roster.OwnedIds())
                    {
                        n++;
                    }

                    return n;
                }

                case TrophyKinds.BattlesWon:
                    return c.Wins;
                default:
                    return 0;
            }
        }

        private void SaveTrophies()
        {
            if (_trophies == null)
            {
                return;
            }

            try
            {
                File.WriteAllText(TrophyHallPath, _trophies.Save());
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void OpenTrophyHall()
        {
            if (_trophies == null || _overlay == null)
            {
                return;
            }

            var hall = _trophies;
            var panel = OpenSheet(TrophyHallSheetName, S("trophy.title", "Trophy Hall"), 0.80f);
            var sub = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Sub", Fmt("trophy.sub", "Earn trophies as your kingdom grows \u00b7 {0} won", hall.ClaimedCount), 22, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.2f);
            HookSkin.Fit(sub, 12, 22);
            Box(sub.rectTransform, 0.05f, 0.83f, 0.95f, 0.89f);

            var kinds = hall.Kinds();
            const float top = 0.815f, bottom = 0.04f;
            var h = (top - bottom) / Math.Max(1, kinds.Count);
            for (var i = 0; i < kinds.Count; i++)
            {
                var y1 = top - (i * h);
                TrophyRow(panel, hall, kinds[i], 0.04f, y1 - h + 0.008f, 0.96f, y1);
            }
        }

        private void TrophyRow(RectTransform panel, TrophyHall hall, string kind, float x0, float y0, float x1, float y1)
        {
            var next = hall.NextOf(kind);
            var (done, total) = hall.TiersOf(kind);
            var ready = next != null && hall.IsReady(next);
            var card = SurvivalVisuals.Image(panel, "Trophy_" + kind, ready ? new Color(0.22f, 0.36f, 0.20f, 1f) : new Color(0.15f, 0.17f, 0.26f, 1f));
            card.raycastTarget = false;
            Box(card.rectTransform, x0, y0, x1, y1);
            Ornaments.Grain(card.transform, new Color(1f, 1f, 1f, 0.08f));
            Ornaments.Bevel(card.transform, 0.6f);
            Ornaments.Frame(card.transform, next == null ? Ornaments.Gold : ready ? new Color(0.55f, 0.95f, 0.45f, 1f) : Ornaments.Bronze, 3f);

            // Crest with the trophy cup; tier pips under it (filled = claimed).
            var crest = SurvivalVisuals.Circle(card.transform, "Crest", next == null ? Ornaments.Gold : new Color(0.42f, 0.30f, 0.10f, 1f));
            crest.preserveAspect = true;
            crest.raycastTarget = false;
            Box(crest.rectTransform, 0.01f, 0.22f, 0.14f, 0.96f);
            var cup = HookSkin.Outlined(SurvivalVisuals.Text(crest.transform, "Cup", "\u2655", 40, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 2f);
            cup.raycastTarget = false;
            SurvivalVisuals.Stretch(cup.rectTransform);
            var pips = "";
            for (var t = 0; t < total; t++)
            {
                pips += t < done ? "\u2605" : "\u2606";
            }

            var pip = HookSkin.Outlined(SurvivalVisuals.Text(card.transform, "Pips", pips, 18, TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.30f, 1f)), 1.2f);
            pip.raycastTarget = false;
            Box(pip.rectTransform, 0.01f, 0.02f, 0.14f, 0.22f);

            var tier = Math.Min(done, TrophyTierNames.Length - 1);
            var name = HookSkin.Outlined(SurvivalVisuals.Text(card.transform, "Name", S("trophy.kind." + kind + ".name", kind) + (next != null ? " " + TrophyTierNames[tier] : ""), 24, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.4f);
            name.fontStyle = FontStyle.Bold;
            name.raycastTarget = false;
            HookSkin.Fit(name, 12, 24);
            Box(name.rectTransform, 0.16f, 0.62f, 0.62f, 0.96f);

            if (next == null)
            {
                var all = HookSkin.Outlined(SurvivalVisuals.Text(card.transform, "Done", S("trophy.complete", "ALL TROPHIES WON \u2714"), 26, TextAnchor.MiddleCenter, FreeGreen), 1.5f);
                all.raycastTarget = false;
                Box(all.rectTransform, 0.16f, 0.08f, 0.96f, 0.60f);
                return;
            }

            var cur = hall.Current(next);
            var desc = HookSkin.Outlined(SurvivalVisuals.Text(card.transform, "Desc", Fmt("trophy.kind." + kind + ".desc", "{0}", next.Target), 18, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.1f);
            desc.raycastTarget = false;
            HookSkin.Fit(desc, 11, 18);
            Box(desc.rectTransform, 0.16f, 0.38f, 0.62f, 0.62f);

            var bar = HookSkin.Bar(card.transform, "Bar", out var fill);
            bar.raycastTarget = false;
            Box(bar.rectTransform, 0.16f, 0.08f, 0.62f, 0.34f);
            HookSkin.SetBar(fill, next.Target <= 0 ? 1f : (float)cur / next.Target, ready ? HookArtKeys.BarFillGold : HookArtKeys.BarFillGreen, ready ? Ornaments.Gold : FreeGreen);
            var count = HookSkin.Outlined(SurvivalVisuals.Text(bar.transform, "N", cur + " / " + next.Target, 16, TextAnchor.MiddleCenter, Color.white), 1.2f);
            count.raycastTarget = false;
            SurvivalVisuals.Stretch(count.rectTransform);

            PackChips(card.rectTransform, new Dictionary<string, int>(next.Reward), 0.63f, 0.50f, 0.97f, 0.96f);
            var label = SheetButton(card.rectTransform, "Claim", ready ? HookArtKeys.ButtonGreen : HookArtKeys.ButtonBlue, ready ? FreeGreen : new Color(0.3f, 0.35f, 0.5f, 1f), 0.66f, 0.06f, 0.94f, 0.48f, 24, out var claim);
            label.text = ready ? S("trophy.claim", "CLAIM") : S("trophy.locked", "IN PROGRESS");
            FitButtonLabel(label, 24);
            claim.interactable = ready;
            if (ready)
            {
                Ornaments.Sparkles(card.rectTransform, 4, Ornaments.Gold);
                StartCoroutine(Breathe(claim.GetComponent<RectTransform>()));
                var id = next.Id;
                var title = Fmt("trophy.won", "Trophy won: {0}", name.text);
                Wire(claim, () =>
                {
                    var got = hall.Claim(id, Campaign!.Wallet);
                    if (got == null)
                    {
                        return;
                    }

                    OpenTrophyHall();
                    Celebrate(title, new Dictionary<string, int>(got));
                });
            }
        }
    }
}
