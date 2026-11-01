#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Survival.Domain.Runner;
using Survival.Domain.Theme;
using Survival.Domain.View;
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
                RailShown(TrophyHallRailName, Survival.Domain.View.RailActionable.Trophy(_trophies.ReadyCount));
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

        /// <summary>Painted cup for a tier: first tier bronze, second silver, third and up gold.</summary>
        internal static string TrophyArtFor(int tierIndex) => tierIndex <= 0 ? HookArtKeys.TrophyBronze : tierIndex == 1 ? HookArtKeys.TrophySilver : HookArtKeys.TrophyGold;

        private static Color TrophyMetal(int tierIndex) => tierIndex <= 0 ? new Color(0.85f, 0.55f, 0.30f, 1f) : tierIndex == 1 ? new Color(0.85f, 0.88f, 0.95f, 1f) : Ornaments.Gold;

        private static string TrophyIconFor(string kind)
        {
            switch (kind)
            {
                case TrophyKinds.BuildingUpgrades: return BottomNav.IconBuild;
                case TrophyKinds.HeroStars: return WorldArtKeys.IconUpgrade;
                case TrophyKinds.RunnerCleared: return RunnerArtKeys.EventIcon;
                case TrophyKinds.HeroesOwned: return BottomNav.IconHeroes;
                default: return BottomNav.IconBattle;
            }
        }

        /// <summary>Where the player does the work for a trophy kind.</summary>
        private void TrophyGo(string kind)
        {
            CloseSheet();
            switch (kind)
            {
                case TrophyKinds.RunnerCleared:
                    FindFirstObjectByType<SurvivalRunnerEntry>()?.OnRunnerTapped();
                    break;
                case TrophyKinds.BattlesWon:
                    OnBattleTapped();
                    break;
                case TrophyKinds.HeroStars:
                case TrophyKinds.HeroesOwned:
                    OpenHeroes();
                    break;
                // Buildings: the town is right behind the sheet.
            }
        }

        public void OpenTrophyHall()
        {
            if (_trophies == null || _overlay == null || Campaign == null)
            {
                return;
            }

            var hall = _trophies;
            var panel = OpenSheet(TrophyHallSheetName, S("trophy.title", "Trophy Hall"), 0.84f);
            BalanceBar.Above(panel, Campaign.Wallet, BalanceBarIds.PowerPacks);

            // Header: the painted gold cup, how many are won, and what this screen is for.
            var head = HookSkin.Panel(panel, "TrophyHead", HookArtKeys.Plate, new Color(0.16f, 0.20f, 0.36f, 1f), 0.8f);
            head.raycastTarget = false;
            Box(head.rectTransform, 0.04f, 0.80f, 0.96f, 0.905f);
            Ornaments.Bevel(head.transform, 0.6f);
            Ornaments.Frame(head.transform, Ornaments.Gold, 2f);
            var cup = HookSkin.Art(head.transform, "Cup", HookArtKeys.TrophyGold, Ornaments.Gold);
            cup.preserveAspect = true;
            cup.raycastTarget = false;
            Box(cup.rectTransform, 0.01f, 0.04f, 0.17f, 0.96f);
            var won = HookSkin.Outlined(SurvivalVisuals.Text(head.transform, "Won", Fmt("trophy.won_count", "{0} trophies won", hall.ClaimedCount), 30, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.6f);
            won.fontStyle = FontStyle.Bold;
            won.raycastTarget = false;
            HookSkin.Fit(won, 14, 30);
            Box(won.rectTransform, 0.19f, 0.52f, 0.98f, 0.96f);
            var sub = HookSkin.Outlined(SurvivalVisuals.Text(head.transform, "Sub", S("trophy.explain", "Win bronze, then silver, then gold. Tap GO to play, CLAIM to collect."), 18, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.1f);
            sub.raycastTarget = false;
            HookSkin.Fit(sub, 11, 18);
            Box(sub.rectTransform, 0.19f, 0.04f, 0.95f, 0.52f);

            var kinds = hall.Kinds();
            const float top = 0.79f, bottom = 0.03f;
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
            var tierIdx = next == null ? Math.Max(0, total - 1) : done;
            var metal = TrophyMetal(tierIdx);
            var card = HookSkin.Panel(panel, "Trophy_" + kind, HookArtKeys.Plate, ready ? new Color(0.20f, 0.34f, 0.22f, 1f) : new Color(0.16f, 0.19f, 0.32f, 1f), 0.7f);
            card.raycastTarget = false;
            Box(card.rectTransform, x0, y0, x1, y1);
            Ornaments.Bevel(card.transform, 0.6f);
            Ornaments.Frame(card.transform, ready ? new Color(0.55f, 0.95f, 0.45f, 1f) : Ornaments.Gold, 2f);

            // Painted cup for the tier being worked on, with the goal's icon as a badge.
            var art = HookSkin.Art(card.transform, "Cup", TrophyArtFor(tierIdx), metal);
            art.preserveAspect = true;
            art.raycastTarget = false;
            Box(art.rectTransform, 0.01f, 0.06f, 0.15f, 0.94f);
            if (next != null && !ready)
            {
                art.color = new Color(0.75f, 0.75f, 0.75f, 1f);
            }

            var badge = HookSkin.Art(card.transform, "Kind", TrophyIconFor(kind), metal);
            badge.preserveAspect = true;
            badge.raycastTarget = false;
            Box(badge.rectTransform, 0.10f, 0.04f, 0.18f, 0.40f);

            var name = HookSkin.Outlined(SurvivalVisuals.Text(card.transform, "Name", S("trophy.kind." + kind + ".name", kind), 24, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.4f);
            name.fontStyle = FontStyle.Bold;
            name.raycastTarget = false;
            HookSkin.Fit(name, 12, 24);
            Box(name.rectTransform, 0.19f, 0.66f, 0.62f, 0.96f);

            var tierText = next == null
                ? S("trophy.tier.max", "All tiers won")
                : Fmt("trophy.tier", "Tier {0} of {1} \u00b7 {2}", done + 1, total, S("trophy.metal." + Math.Min(tierIdx, 2), tierIdx <= 0 ? "Bronze" : tierIdx == 1 ? "Silver" : "Gold"));
            var tier = HookSkin.Outlined(SurvivalVisuals.Text(card.transform, "Tier", tierText, 17, TextAnchor.MiddleLeft, metal), 1.2f);
            tier.fontStyle = FontStyle.Bold;
            tier.raycastTarget = false;
            HookSkin.Fit(tier, 10, 17);
            Box(tier.rectTransform, 0.19f, 0.46f, 0.62f, 0.68f);

            if (next == null)
            {
                var all = HookSkin.Outlined(SurvivalVisuals.Text(card.transform, "Done", S("trophy.complete", "ALL TROPHIES WON \u2714"), 24, TextAnchor.MiddleCenter, FreeGreen), 1.5f);
                all.raycastTarget = false;
                Box(all.rectTransform, 0.19f, 0.06f, 0.96f, 0.46f);
                return;
            }

            var cur = hall.Current(next);
            var desc = HookSkin.Outlined(SurvivalVisuals.Text(card.transform, "Desc", Fmt("trophy.kind." + kind + ".desc", "{0}", next.Target), 16, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.1f);
            desc.raycastTarget = false;
            HookSkin.Fit(desc, 10, 16);
            Box(desc.rectTransform, 0.19f, 0.28f, 0.62f, 0.47f);

            var bar = HookSkin.Bar(card.transform, "Bar", out var fill);
            bar.raycastTarget = false;
            Box(bar.rectTransform, 0.19f, 0.05f, 0.62f, 0.28f);
            HookSkin.SetBar(fill, next.Target <= 0 ? 1f : (float)cur / next.Target, ready ? HookArtKeys.BarFillGold : HookArtKeys.BarFillGreen, ready ? Ornaments.Gold : FreeGreen);
            var count = HookSkin.Outlined(SurvivalVisuals.Text(bar.transform, "N", Math.Min(cur, next.Target) + " / " + next.Target, 16, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.2f);
            count.raycastTarget = false;
            SurvivalVisuals.Stretch(count.rectTransform);

            PackChips(card.rectTransform, new Dictionary<string, int>(next.Reward), 0.64f, 0.52f, 0.97f, 0.95f);
            var label = SheetButton(card.rectTransform, ready ? "Claim" : "Go", ready ? HookArtKeys.ButtonGreen : HookArtKeys.ButtonBlue, ready ? FreeGreen : new Color(0.25f, 0.45f, 0.85f, 1f), 0.66f, 0.06f, 0.95f, 0.48f, 24, out var btn);
            label.text = ready ? S("trophy.claim", "CLAIM") : S("trophy.go", "GO \u25b6");
            FitButtonLabel(label, 24);
            if (!ready)
            {
                Wire(btn, () => TrophyGo(kind));
                return;
            }

            Ornaments.Sparkles(card.rectTransform, 4, Ornaments.Gold);
            StartCoroutine(Breathe(btn.GetComponent<RectTransform>()));
            var id = next.Id;
            var title = Fmt("trophy.won", "Trophy won: {0}", name.text);
            Wire(btn, () =>
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
