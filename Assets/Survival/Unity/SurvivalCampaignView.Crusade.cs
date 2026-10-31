#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Survival.Domain.Crusade;
using Survival.Domain.Ids;
using Survival.Domain.Store;
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Crusade Banner season pass: a rail button (slot 2) with a red count of rewards ready. The sheet shows the season
    /// timer, the player's balances, the season XP bar and five tiers per page, each with a FREE and a ROYAL BANNER reward.
    /// Royal needs the sandbox SKU sku.pass_season ($9.99 from products.json). XP comes from runner levels, finished
    /// upgrades (buildings and hero levels) and quest steps. Saved in crusade_banner.json.
    /// </summary>
    public sealed partial class SurvivalCampaignView
    {
        public const string CrusadeSheetName = "CrusadeBannerSheet";
        public const string CrusadeRailName = "CrusadeBanner";
        private const int CrusadeRowsPerPage = 5;

        private static readonly IReadOnlyList<string> CrusadeBalanceIds = new[] { SurvIds.ResHard, SurvIds.ResSoft, SurvIds.ResSpeedupM, SurvIds.ResHeroXp };

        private CrusadeBanner? _crusade;
        private StoreSystem? _crusadeStore;
        private Image? _crusadeBadge;
        private Text? _crusadeBadgeCount;
        private int _crusadePage = -1;

        /// <summary>The season pass (null until the Play HUD is up).</summary>
        public CrusadeBanner? Crusade => _crusade;

        public static string CrusadePath => Path.Combine(Application.persistentDataPath, CrusadeBanner.FileName);

        partial void EngageAttach_Crusade()
        {
            var campaign = Campaign!;
            _crusade = new CrusadeBanner(campaign.Clock);
            try
            {
                if (File.Exists(CrusadePath) && !_crusade.Load(File.ReadAllText(CrusadePath)))
                {
                    Debug.LogWarning("Crusade Banner: save unreadable, starting fresh. " + CrusadePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            var hook = _session?.Hook;
            if (hook != null)
            {
                _crusadeStore = CrusadeBanner.MakeStore(hook.Store.ProductFor(CrusadeBanner.Sku), hook.Store.Purchases, campaign.Wallet);
                hook.Base.LevelReached += (_, __) => _crusade.AddXp(CrusadeXpSource.Upgrade);
                hook.Quests.StepCompleted += _ => _crusade.AddXp(CrusadeXpSource.Quest);
            }

            campaign.Roster.LevelledUp += (_, __) => _crusade.AddXp(CrusadeXpSource.Upgrade);
            if (_crusade.RunnerSeen < 0)
            {
                _crusade.RunnerSeen = campaign.RunnerUnlocked;
            }

            _crusade.Changed += SaveCrusade;
            RailButton(2, CrusadeRailName, "\u2694", S("crusade.rail", "CRUSADE"), new Color(0.20f, 0.30f, 0.66f, 1f), OpenCrusade, out var badge, out var count);
            _crusadeBadge = badge;
            _crusadeBadgeCount = count;
            SaveCrusade();
        }

        partial void EngageTick_Crusade()
        {
            var c = _crusade;
            if (c == null || Campaign == null)
            {
                return;
            }

            c.RollSeason();
            var runner = Campaign.RunnerUnlocked;
            if (runner > c.RunnerSeen)
            {
                var gained = runner - c.RunnerSeen;
                c.RunnerSeen = runner;
                c.AddXp(CrusadeXpSource.RunnerLevel, gained);
            }

            RailBadge(_crusadeBadge, _crusadeBadgeCount, c.ReadyCount);
        }

        private void SaveCrusade()
        {
            if (_crusade == null)
            {
                return;
            }

            try
            {
                File.WriteAllText(CrusadePath, _crusade.Save());
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void OpenCrusade()
        {
            if (_crusade == null || _overlay == null || Campaign == null)
            {
                return;
            }

            var c = _crusade;
            c.RollSeason();
            var pages = (c.Tiers.Count + CrusadeRowsPerPage - 1) / CrusadeRowsPerPage;
            if (_crusadePage < 0)
            {
                var first = 1;
                while (first <= c.Tiers.Count && c.IsClaimed(first, false) && (!c.HasPremium || c.IsClaimed(first, true)))
                {
                    first++;
                }

                _crusadePage = Mathf.Clamp((Math.Min(first, Math.Max(1, c.TierReached + 1)) - 1) / CrusadeRowsPerPage, 0, pages - 1);
            }

            _crusadePage = Mathf.Clamp(_crusadePage, 0, pages - 1);
            var panel = OpenSheet(CrusadeSheetName, S("crusade.title", "Crusade Banner"), 0.90f);

            var season = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Season", "", 22, TextAnchor.MiddleCenter, SurvivalVisuals.Reward), 1.4f);
            Box(season.rectTransform, 0.03f, 0.855f, 0.97f, 0.885f);
            var sandbox = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Sandbox", S("store.sandbox", "SANDBOX \u00b7 no real charge"), 15, TextAnchor.MiddleCenter, Warn), 1.2f);
            Box(sandbox.rectTransform, 0f, 0.835f, 1f, 0.855f);
            BalanceBar.Inside(panel, Campaign.Wallet, CrusadeBalanceIds, 0.05f, 0.78f, 0.95f, 0.83f);

            // Season XP bar.
            var tierCrest = SurvivalVisuals.Circle(panel, "TierCrest", RoyalBlue);
            tierCrest.preserveAspect = true;
            tierCrest.raycastTarget = false;
            Box(tierCrest.rectTransform, 0.04f, 0.695f, 0.16f, 0.775f);
            Ornaments.Sparkles(tierCrest.rectTransform, 2, Ornaments.Gold);
            var tierNum = HookSkin.Outlined(SurvivalVisuals.Text(tierCrest.transform, "N", c.TierReached.ToString(), 30, TextAnchor.MiddleCenter, Color.white), 2f);
            tierNum.raycastTarget = false;
            SurvivalVisuals.Stretch(tierNum.rectTransform);
            var bar = HookSkin.Bar(panel, "XpBar", out var fill);
            Box(bar.rectTransform, 0.18f, 0.712f, 0.96f, 0.758f);
            HookSkin.SetBar(fill, (float)c.XpIntoTier / CrusadeBanner.XpPerTier, HookArtKeys.BarFillBlue, RoyalBlue);
            var xp = HookSkin.Outlined(SurvivalVisuals.Text(bar.transform, "Xp", c.TierReached >= c.Tiers.Count ? S("crusade.maxed", "BANNER COMPLETE") : Fmt("crusade.xp", "{0} / {1} XP to tier {2}", c.XpIntoTier, CrusadeBanner.XpPerTier, c.TierReached + 1), 17, TextAnchor.MiddleCenter, Color.white), 1.2f);
            xp.raycastTarget = false;
            SurvivalVisuals.Stretch(xp.rectTransform);
            var tips = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Tips", Fmt("crusade.tips", "Season XP: runner level +{0} \u00b7 upgrade +{1} \u00b7 quest +{2}", CrusadeBanner.XpFor(CrusadeXpSource.RunnerLevel), CrusadeBanner.XpFor(CrusadeXpSource.Upgrade), CrusadeBanner.XpFor(CrusadeXpSource.Quest)), 15, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.1f);
            tips.raycastTarget = false;
            HookSkin.Fit(tips, 10, 15);
            Box(tips.rectTransform, 0.03f, 0.668f, 0.97f, 0.695f);

            // Column heads: FREE, and ROYAL BANNER with its buy button.
            const float freeX0 = 0.17f, freeX1 = 0.55f, royX0 = 0.57f, royX1 = 0.96f;
            var freeHead = Ornaments.Ribbon(panel, S("crusade.free", "FREE"), new Color(0.18f, 0.50f, 0.24f, 1f), out var freeRib);
            Box(freeRib.rectTransform, freeX0, 0.60f, freeX1, 0.66f);
            freeHead.fontSize = 22;
            var royHead = Ornaments.Ribbon(panel, S("crusade.royal", "ROYAL BANNER"), new Color(0.50f, 0.18f, 0.60f, 1f), out var royRib);
            Box(royRib.rectTransform, royX0, 0.60f, royX1, 0.66f);
            royHead.fontSize = 22;

            var buyLabel = SheetButton(panel, "BuyRoyal", c.HasPremium ? HookArtKeys.ButtonBlue : HookArtKeys.ButtonGold, Ornaments.Gold, royX0 + 0.02f, 0.545f, royX1 - 0.02f, 0.598f, 22, out var buy);
            if (c.HasPremium)
            {
                buyLabel.text = S("crusade.owned", "\u2714 BANNER RAISED");
                buy.interactable = false;
            }
            else
            {
                var price = _crusadeStore?.ProductFor(CrusadeBanner.Sku)?.PriceLabel ?? CrusadeBanner.FallbackPrice;
                buyLabel.text = Fmt("crusade.buy", "UNLOCK {0}", price);
                Ornaments.Shine(buy.GetComponent<RectTransform>());
                Wire(buy, BuyCrusade);
            }

            FitButtonLabel(buyLabel, 22);

            // Five tiers per page.
            var first0 = _crusadePage * CrusadeRowsPerPage;
            const float rowsTop = 0.535f, rowsBottom = 0.11f;
            var rh = (rowsTop - rowsBottom) / CrusadeRowsPerPage;
            for (var r = 0; r < CrusadeRowsPerPage && first0 + r < c.Tiers.Count; r++)
            {
                var tier = c.Tiers[first0 + r];
                var y1 = rowsTop - (r * rh);
                var y0 = y1 - rh + 0.006f;
                var reached = tier.Number <= c.TierReached;
                var num = SurvivalVisuals.Circle(panel, "Tier" + tier.Number, reached ? Ornaments.Gold : new Color(0.25f, 0.27f, 0.34f, 1f));
                num.preserveAspect = true;
                num.raycastTarget = false;
                Box(num.rectTransform, 0.04f, y0 + 0.01f, 0.15f, y1 - 0.01f);
                var nt = HookSkin.Outlined(SurvivalVisuals.Text(num.transform, "N", tier.Number.ToString(), 26, TextAnchor.MiddleCenter, reached ? HookSkin.Ink : SurvivalVisuals.Cream), 1.2f);
                nt.raycastTarget = false;
                SurvivalVisuals.Stretch(nt.rectTransform);
                CrusadeCell(panel, c, tier.Number, false, tier.Free, freeX0, y0, freeX1, y1);
                CrusadeCell(panel, c, tier.Number, true, tier.Premium, royX0, y0, royX1, y1);
            }

            // Paging.
            var prevLabel = SheetButton(panel, "Prev", HookArtKeys.ButtonBlue, RoyalBlue, 0.06f, 0.03f, 0.26f, 0.095f, 26, out var prev);
            prevLabel.text = "\u25c0";
            prev.interactable = _crusadePage > 0;
            Wire(prev, () => { _crusadePage--; OpenCrusade(); });
            var nextLabel = SheetButton(panel, "Next", HookArtKeys.ButtonBlue, RoyalBlue, 0.74f, 0.03f, 0.94f, 0.095f, 26, out var next);
            nextLabel.text = "\u25b6";
            next.interactable = _crusadePage < pages - 1;
            Wire(next, () => { _crusadePage++; OpenCrusade(); });
            var pageText = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Page", Fmt("crusade.page", "Tiers {0}-{1}", first0 + 1, Math.Min(c.Tiers.Count, first0 + CrusadeRowsPerPage)), 20, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.2f);
            Box(pageText.rectTransform, 0.28f, 0.03f, 0.72f, 0.095f);

            void Refresh()
            {
                season.text = Fmt("crusade.season", "Season {0} \u00b7 ends in {1}", c.Season, RailTime(c.SecondsLeft));
                if (c.CurrentSeason != c.Season)
                {
                    OpenCrusade();
                }
            }

            Refresh();
            _sheetRefresh = Refresh;
        }

        private void CrusadeCell(RectTransform panel, CrusadeBanner c, int tier, bool premium, IReadOnlyDictionary<string, int> reward, float x0, float y0, float x1, float y1)
        {
            var claimed = c.IsClaimed(tier, premium);
            var ready = c.CanClaim(tier, premium);
            var locked = premium && !c.HasPremium;
            var fill = ready ? new Color(0.22f, 0.40f, 0.22f, 1f) : premium ? new Color(0.28f, 0.14f, 0.34f, 1f) : new Color(0.14f, 0.20f, 0.32f, 1f);
            var cell = SurvivalVisuals.Image(panel, (premium ? "Royal" : "Free") + tier, fill);
            cell.raycastTarget = false;
            Box(cell.rectTransform, x0, y0, x1, y1);
            Ornaments.Bevel(cell.transform, 0.5f);
            Ornaments.Frame(cell.transform, premium ? new Color(0.86f, 0.42f, 1f, 1f) : Ornaments.Bronze, 2f);
            PackChips(cell.rectTransform, new Dictionary<string, int>(reward), 0.03f, 0.08f, ready ? 0.62f : 0.80f, 0.92f);

            if (claimed)
            {
                var tick = HookSkin.Outlined(SurvivalVisuals.Text(cell.transform, "Tick", "\u2714", 34, TextAnchor.MiddleCenter, FreeGreen), 1.5f);
                tick.raycastTarget = false;
                Box(tick.rectTransform, 0.80f, 0f, 1f, 1f);
                var dim = SurvivalVisuals.Image(cell.transform, "Dim", new Color(0f, 0f, 0f, 0.35f));
                dim.raycastTarget = false;
                SurvivalVisuals.Stretch(dim.rectTransform);
                return;
            }

            if (locked)
            {
                var lk = HookSkin.Outlined(SurvivalVisuals.Text(cell.transform, "Lock", S("crusade.locked", "LOCKED"), 16, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.2f);
                lk.raycastTarget = false;
                Box(lk.rectTransform, 0.80f, 0f, 1f, 1f);
                return;
            }

            if (!ready)
            {
                return;
            }

            var label = SheetButton(cell.rectTransform, "Claim", HookArtKeys.ButtonGreen, FreeGreen, 0.63f, 0.12f, 0.97f, 0.88f, 20, out var btn);
            label.text = S("crusade.claim", "CLAIM");
            FitButtonLabel(label, 20);
            StartCoroutine(Breathe(btn.GetComponent<RectTransform>()));
            Wire(btn, () =>
            {
                var got = c.Claim(tier, premium, Campaign!.Wallet);
                if (got == null)
                {
                    return;
                }

                OpenCrusade();
                Celebrate(Fmt("crusade.claimed", "Tier {0} reward!", tier), new Dictionary<string, int>(got));
            });
        }

        private void BuyCrusade()
        {
            var c = _crusade;
            if (c == null || _crusadeStore == null)
            {
                _hook?.Say(_overlay!, S("store.power_packs.failed", "Purchase failed"), Warn);
                return;
            }

            _crusadeStore.BuySku(CrusadeBanner.OfferId, outcome =>
            {
                if (outcome.Ok)
                {
                    c.UnlockPremium();
                    var ready = 0;
                    for (var t = 1; t <= c.TierReached; t++)
                    {
                        if (c.CanClaim(t, true))
                        {
                            ready++;
                        }
                    }

                    OpenCrusade();
                    Celebrate(Fmt("crusade.bought", "Royal Banner raised! {0} rewards ready", ready), new Dictionary<string, int>());
                }
                else
                {
                    _hook?.Say(_overlay!, S("store.power_packs.failed", "Purchase failed"), Warn);
                }
            });
        }
    }
}
