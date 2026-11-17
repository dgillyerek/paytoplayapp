#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Survival.Domain.Campaign;
using Survival.Domain.Clock;
using Survival.Domain.Ids;
using Survival.Domain.Layout;
using Survival.Domain.Session;
using Survival.Domain.Store;
using Survival.Domain.Theme;
using Survival.Domain.View;
using Survival.Domain.World;
using UnityEngine;
using Survival.Unity.Kids;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>Split out of <see cref="SurvivalCampaignView"/>; draws into the view's overlay and uses its helpers.</summary>
    internal sealed class RewardTrackScreen
    {
        private readonly SurvivalCampaignView _v;
        private int _trackPage = -1;

        internal RewardTrackScreen(SurvivalCampaignView view)
        {
            _v = view;
        }

        private const int TrackRowsPerPage = 6;

        /// <summary>
        /// The Kingdom Path sheet: three columns per milestone (FREE, PREMIUM $2.99, ROYAL $3.99 which opens both paid
        /// columns; a Premium owner sees the $0.99 upgrade). Reached cells pulse CLAIM, locked ones show a lock and the price,
        /// buying late opens every reached row. Sandbox purchases only (no real billing in this build).
        /// </summary>
        internal void Open()
        {
            if (_v.Campaign == null || _v.Track == null || _v._overlay == null)
            {
                return;
            }

            var track = _v.Track;
            var rows = track.Catalog.Rows;
            var pages = Mathf.Max(1, (rows.Count + TrackRowsPerPage - 1) / TrackRowsPerPage);
            if (_trackPage < 0)
            {
                var next = 0;
                while (next < rows.Count && track.Progress >= rows[next].Level)
                {
                    next++;
                }

                _trackPage = Mathf.Clamp((next == 0 ? 0 : next - 1) / TrackRowsPerPage, 0, pages - 1);
            }

            _trackPage = Mathf.Clamp(_trackPage, 0, pages - 1);
            var panel = _v.OpenSheet(SurvivalCampaignView.RewardTrackSheetName, _v.S("reward_track.title", track.Catalog.Title), 0.90f);
            var progress = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Progress", _v.Fmt("reward_track.progress", "Runner levels beaten: {0}", track.Progress), 20, TextAnchor.MiddleCenter, SurvivalVisuals.Reward), 1.5f);
            HookSkin.Box(progress.rectTransform, 0.03f, 0.85f, 0.97f, 0.88f);
            var sandbox = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Sandbox", _v.S("store.sandbox", "SANDBOX \u00b7 no real charge"), 16, TextAnchor.MiddleCenter, SurvivalCampaignView.Warn), 1.5f);
            HookSkin.Box(sandbox.rectTransform, 0f, 0.828f, 1f, 0.85f);

            // Header: the three column titles; the paid ones carry their buy button.
            var colX = new[] { 0.17f, 0.445f, 0.72f };
            const float colW = 0.265f;
            TrackBackdrop(panel, colX, colW);
            HeaderCell(panel, "HeadFree", colX[0], colW, _v.S("reward_track.free", "FREE"), SurvivalCampaignView.FreeGreen, null, null, null);
            HeaderCell(panel, "HeadPremium", colX[1], colW, _v.S("reward_track.premium", "PREMIUM"), SurvivalVisuals.Gold,
                track.PremiumOfferNow is { } p ? track.PriceLabel(p) : null, track.Owns(TrackColumn.Premium) ? _v.S("reward_track.owned", "OWNED") : null,
                () => BuyTrack(track.BuyPremium, _v.S("reward_track.premium", "PREMIUM")));
            HeaderCell(panel, "HeadRoyal", colX[2], colW, _v.S("reward_track.royal", "ROYAL"), SurvivalCampaignView.RoyalBlue,
                track.RoyalOfferNow is { } r ? track.PriceLabel(r) + (track.Tier == TrackTier.Premium ? " " + _v.S("reward_track.upgrade", "UPGRADE") : "") : null,
                track.Owns(TrackColumn.Royal) ? _v.S("reward_track.owned", "OWNED") : null,
                () => BuyTrack(track.BuyRoyal, _v.S("reward_track.royal", "ROYAL")));
            var pitch = track.Tier == TrackTier.None ? _v.S("reward_track.pitch_royal", "Royal opens PREMIUM + ROYAL") : "";
            var pitchText = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Pitch", pitch, 15, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.2f);
            HookSkin.Box(pitchText.rectTransform, colX[1], 0.675f, colX[2] + colW, 0.70f);

            var top = 0.67f;
            var h = (top - 0.17f) / TrackRowsPerPage;
            TrackSpine(panel, track, rows, top, h);
            for (var i = 0; i < TrackRowsPerPage; i++)
            {
                var idx = (_trackPage * TrackRowsPerPage) + i;
                if (idx >= rows.Count)
                {
                    break;
                }

                var row = rows[idx];
                var y1 = top - (i * h);
                var reached = track.Progress >= row.Level;
                var plate = TrackMedallion(panel, row.Level, reached, 0.03f, y1 - h + 0.006f, 0.165f, y1);
                var lvl = HookSkin.Outlined(SurvivalVisuals.Text(plate, "L", _v.Fmt("reward_track.level", "LV\n{0}", row.Level), 22, TextAnchor.MiddleCenter, reached ? SurvivalVisuals.Gold : SurvivalVisuals.Mute), 1.5f);
                lvl.fontStyle = FontStyle.Bold;
                lvl.raycastTarget = false;
                SurvivalVisuals.Stretch(lvl.rectTransform);
                for (var c = 0; c < 3; c++)
                {
                    TrackCell(panel, track, row, (TrackColumn)c, colX[c], y1 - h + 0.006f, colX[c] + colW, y1);
                }
            }

            var prev = SurvivalCampaignView.SheetButton(panel, "Prev", HookArtKeys.ButtonBlue, SurvivalCampaignView.HeroFrame, 0.03f, 0.10f, 0.20f, 0.155f, 22, out var prevBtn);
            prev.text = "\u25C0";
            _v.Wire(prevBtn, () =>
            {
                _trackPage = Mathf.Max(0, _trackPage - 1);
                Open();
            });
            var pageText = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Page", (_trackPage + 1) + " / " + pages, 18, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.2f);
            HookSkin.Box(pageText.rectTransform, 0.20f, 0.10f, 0.30f, 0.155f);
            var nextB = SurvivalCampaignView.SheetButton(panel, "Next", HookArtKeys.ButtonBlue, SurvivalCampaignView.HeroFrame, 0.30f, 0.10f, 0.47f, 0.155f, 22, out var nextBtn);
            nextB.text = "\u25B6";
            _v.Wire(nextBtn, () =>
            {
                _trackPage = Mathf.Min(pages - 1, _trackPage + 1);
                Open();
            });
            var n = track.ClaimableCount();
            var all = SurvivalCampaignView.SheetButton(panel, "ClaimAll", n > 0 ? HookArtKeys.ButtonGreen : HookArtKeys.ButtonBlue, n > 0 ? SurvivalCampaignView.FreeGreen : SurvivalCampaignView.Disabled, 0.50f, 0.10f, 0.97f, 0.155f, 22, out var allBtn);
            all.text = n > 0 ? _v.Fmt("reward_track.claim_all", "CLAIM ALL ({0})", n) : _v.S("reward_track.nothing", "Nothing to claim");
            if (n > 0)
            {
                _v.StartCoroutine(SurvivalCampaignView.Breathe(allBtn.GetComponent<RectTransform>()));
            }

            _v.Wire(allBtn, () =>
            {
                var got = track.ClaimAll();
                if (got.Count > 0)
                {
                    _v.Celebrate(_v.S("reward_track.claimed", "Rewards claimed!"), TrackRewards(got));
                    _v.SaveNow();
                }

                Open();
                _v._refresh?.Invoke();
            });

            var close = SurvivalCampaignView.SheetButton(panel, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.30f, 0.02f, 0.70f, 0.085f, 24, out var closeBtn);
            close.text = _v.S("hook.close", "Close");
            _v.Wire(closeBtn, () =>
            {
                _trackPage = -1;
                _v.CloseSheet();
            });

            // Gold filigree over the whole sheet, last so it sits on top (no raycasts).
            var frame = TrackDeco(panel, "GoldFrame", "frame_gold", 0f, 0f, 1f, 1f, Color.white, 0.55f);
            if (frame == null)
            {
                Ornaments.Frame(panel, Ornaments.Gold, 4f);
            }
        }


        private static readonly string[] TrackColArt = { "free", "premium", "royal" };

        private static Color TrackRarity(TrackColumn col) => col switch
        {
            TrackColumn.Royal => new Color(0.86f, 0.42f, 1f, 1f),
            TrackColumn.Premium => new Color(0.40f, 0.70f, 1f, 1f),
            _ => new Color(1f, 0.86f, 0.62f, 1f)
        };

        /// <summary>Atlas sprite on a box (sliced when the atlas gives a border); null when the pack has no atlas.</summary>
        private static Image? TrackDeco(Transform parent, string name, string art, float x0, float y0, float x1, float y1, Color tint, float borderScale = 0.5f)
        {
            var sprite = SurvivalArt.Get(HookArtKeys.Track(art));
            if (sprite == null)
            {
                return null;
            }

            var img = SurvivalVisuals.Image(parent, name, tint, sprite);
            img.raycastTarget = false;
            HookSkin.ApplySliced(img, borderScale);
            HookSkin.Box(img.rectTransform, x0, y0, x1, y1);
            return img;
        }

        /// <summary>Atlas sprite of a fixed reference size centred on an anchor point.</summary>
        private static Image? TrackSprite(Transform parent, string name, string art, float ax, float ay, float w, float h, Color tint)
        {
            var sprite = SurvivalArt.Get(HookArtKeys.Track(art));
            if (sprite == null)
            {
                return null;
            }

            var img = SurvivalVisuals.Image(parent, name, tint, sprite);
            img.raycastTarget = false;
            img.preserveAspect = true;
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(ax, ay);
            img.rectTransform.sizeDelta = new Vector2(w, h);
            return img;
        }

        /// <summary>Parchment sheet, stone milestone lane, column skins and the crowned crest over the title.</summary>
        private void TrackBackdrop(RectTransform panel, float[] colX, float colW)
        {
            var parchment = TrackDeco(panel, "Parchment", "parchment", 0.012f, 0.008f, 0.988f, 0.992f, Color.white);
            if (parchment != null)
            {
                parchment.rectTransform.SetSiblingIndex(0);
            }
            else
            {
                Ornaments.Grain(panel, new Color(1f, 0.92f, 0.75f, 0.25f)).rectTransform.SetSiblingIndex(0);
            }

            TrackDeco(panel, "StoneLane", "stone", 0.025f, 0.165f, 0.165f, 0.675f, new Color(1f, 1f, 1f, 0.92f));
            for (var c = 0; c < 3; c++)
            {
                TrackDeco(panel, "Column_" + TrackColArt[c], "col_" + TrackColArt[c], colX[c] - 0.006f, 0.165f, colX[c] + colW - 0.004f, 0.832f, new Color(1f, 1f, 1f, 0.95f), 0.5f);
            }

            var crest = TrackSprite(panel, "KingdomCrest", "crest_kingdom", 0.5f, 1f, 150f, 150f, Color.white);
            if (crest != null)
            {
                crest.rectTransform.anchoredPosition = new Vector2(0f, 62f);
                var glow = HookSkin.Glow(crest.transform, "Glow", new Color(1f, 0.82f, 0.4f, 0.55f));
                glow.raycastTarget = false;
                glow.rectTransform.SetAsFirstSibling();
                SurvivalVisuals.Stretch(glow.rectTransform);
                glow.rectTransform.offsetMin = new Vector2(-40f, -40f);
                glow.rectTransform.offsetMax = new Vector2(40f, 40f);
            }
        }

        /// <summary>The road the milestones sit on: a dim stone path, gold-lit up to the player's progress.</summary>
        private void TrackSpine(RectTransform panel, RewardTrack track, IReadOnlyList<TrackRow> rows, float top, float h)
        {
            const float x0 = 0.072f;
            const float x1 = 0.123f;
            var first = _trackPage * TrackRowsPerPage;
            var shown = Mathf.Min(TrackRowsPerPage, rows.Count - first);
            if (shown <= 0)
            {
                return;
            }

            var yTop = top - (h * 0.5f);
            var yBot = top - ((shown - 0.5f) * h);
            TrackDeco(panel, "Road", "road", x0, yBot, x1, yTop, new Color(0.62f, 0.60f, 0.58f, 1f));
            var lit = 0;
            while (lit < shown && track.Progress >= rows[first + lit].Level)
            {
                lit++;
            }

            if (lit > 0)
            {
                var litBot = lit >= shown ? yBot : top - ((lit - 0.5f) * h);
                TrackDeco(panel, "RoadLit", "road", x0, litBot, x1, yTop, new Color(1f, 0.86f, 0.42f, 1f));
            }
        }

        /// <summary>Level medallion (gold when reached, iron when not); returns the host for the level text.</summary>
        private RectTransform TrackMedallion(RectTransform panel, int level, bool reached, float x0, float y0, float x1, float y1)
        {
            var host = new GameObject("Milestone_" + level, typeof(RectTransform)).GetComponent<RectTransform>();
            host.SetParent(panel, false);
            HookSkin.Box(host, x0, y0, x1, y1);
            var med = TrackSprite(host, "Medallion", reached ? "medallion_on" : "medallion_off", 0.5f, 0.5f, 118f, 118f, Color.white);
            if (med == null)
            {
                var plate = HookSkin.Panel(host, "Plate", HookArtKeys.Plate, reached ? SurvivalVisuals.RowFill : SurvivalCampaignView.HeroFrame, 0.45f);
                plate.raycastTarget = false;
                SurvivalVisuals.Stretch(plate.rectTransform);
                return host;
            }

            if (reached)
            {
                var glow = HookSkin.Glow(host, "Glow", new Color(1f, 0.80f, 0.35f, 0.5f));
                glow.raycastTarget = false;
                glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                glow.rectTransform.sizeDelta = new Vector2(170f, 170f);
                glow.rectTransform.SetAsFirstSibling();
            }

            return med.rectTransform;
        }

        private void HeaderCell(RectTransform panel, string name, float x0, float w, string title, Color color, string? price, string? owned, Action? buy)
        {
            var col = name == "HeadRoyal" ? 2 : name == "HeadPremium" ? 1 : 0;
            var head = HookSkin.Panel(panel, name, HookArtKeys.Plate, new Color(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f, 1f), 0.45f);
            HookSkin.Box(head.rectTransform, x0, 0.705f, x0 + w - 0.01f, 0.825f);
            if (SurvivalArt.Get(HookArtKeys.Track("cell_" + TrackColArt[col])) is { } skin)
            {
                head.sprite = skin;
                head.color = Color.white;
                HookSkin.ApplySliced(head, 0.5f);
            }

            // Column ribbon with the title, and its crest (the Royal one is crowned).
            var ribbon = TrackDeco(head.transform, "Ribbon", col == 2 ? "ribbon_crimson" : col == 1 ? "ribbon_blue" : "ribbon_stone", -0.06f, 0.50f, 1.06f, 1.02f, Color.white, 0.4f);
            var crest = TrackSprite(head.transform, "Crest", "crest_" + TrackColArt[col], 0.06f, 0.78f, 62f, 62f, Color.white);
            if (col == 2 && crest != null)
            {
                Ornaments.Sparkles(crest.rectTransform, 3, new Color(1f, 0.9f, 0.6f, 1f));
            }

            var t = HookSkin.Outlined(SurvivalVisuals.Text(ribbon != null ? ribbon.transform : head.transform, "T", title, 24, TextAnchor.MiddleCenter, ribbon != null ? SurvivalVisuals.Cream : color), 2f);
            t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false;
            if (ribbon != null)
            {
                HookSkin.Box(t.rectTransform, 0.12f, 0.12f, 0.88f, 0.92f);
            }
            else
            {
                HookSkin.Box(t.rectTransform, 0f, 0.52f, 1f, 1f);
            }

            if (price != null && buy != null)
            {
                var label = SurvivalCampaignView.SheetButton((RectTransform)head.transform, "Buy", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, 0.06f, 0.06f, 0.94f, 0.50f, 20, out var btn);
                label.text = price;
                _v.Wire(btn, buy);
                Ornaments.Shine(btn.GetComponent<RectTransform>(), 3.2f);
            }
            else
            {
                var sub = HookSkin.Outlined(SurvivalVisuals.Text(head.transform, "Sub", owned ?? _v.S("reward_track.everyone", "EVERYONE"), 18, TextAnchor.MiddleCenter, owned != null ? SurvivalCampaignView.FreeGreen : SurvivalVisuals.Cream), 1.5f);
                sub.fontStyle = FontStyle.Bold;
                HookSkin.Box(sub.rectTransform, 0f, 0.06f, 1f, 0.50f);
            }
        }

        private void TrackCell(RectTransform panel, RewardTrack track, TrackRow row, TrackColumn col, float x0, float y0, float x1, float y1)
        {
            var state = track.State(row, col);
            var fill = state switch
            {
                TrackCellState.Claimable => new Color(0.16f, 0.42f, 0.20f, 1f),
                TrackCellState.Claimed => new Color(0.16f, 0.17f, 0.20f, 1f),
                TrackCellState.Locked => new Color(0.12f, 0.14f, 0.24f, 1f),
                _ => SurvivalVisuals.RowFill
            };
            var cell = HookSkin.Panel(panel, "Cell_" + row.Level + "_" + col, HookArtKeys.Plate, fill, 0.45f);
            HookSkin.Box(cell.rectTransform, x0 + 0.004f, y0 + 0.003f, x1 - 0.014f, y1 - 0.003f);
            var art = state == TrackCellState.Claimed ? "cell_claimed" : "cell_" + TrackColArt[(int)col];
            if (SurvivalArt.Get(HookArtKeys.Track(art)) is { } skin)
            {
                cell.sprite = skin;
                cell.color = state == TrackCellState.Locked ? new Color(0.62f, 0.62f, 0.68f, 1f) : Color.white;
                HookSkin.ApplySliced(cell, 0.45f);
            }
            else if (state == TrackCellState.Claimed || state == TrackCellState.Locked)
            {
                cell.color = new Color(cell.color.r * 0.75f, cell.color.g * 0.75f, cell.color.b * 0.75f, 1f);
            }

            var crt = cell.rectTransform;
            TrackRewardIcons(crt, row.Rewards(col), col, state);

            switch (state)
            {
                case TrackCellState.Claimable:
                {
                    // Claimables shine: rarity glow pulse, light sweep, twinkles.
                    Ornaments.Shine(crt, 2.4f);
                    Ornaments.Sparkles(crt, 4, new Color(1f, 0.95f, 0.7f, 1f));
                    var label = SurvivalCampaignView.SheetButton(crt, "Claim", HookArtKeys.ButtonGreen, SurvivalCampaignView.FreeGreen, 0.10f, 0.05f, 0.90f, 0.40f, 18, out var btn);
                    label.text = _v.S("reward_track.claim", "CLAIM");
                    var rt = btn.GetComponent<RectTransform>();
                    _v.StartCoroutine(SurvivalCampaignView.Breathe(rt));
                    var level = row.Level;
                    _v.Wire(btn, () =>
                    {
                        if (track.Claim(level, col) is { } got)
                        {
                            _v.Juice(rt);
                            ClaimBurst(crt, TrackRarity(col));
                            _v.Celebrate(_v.Fmt("reward_track.cell_claimed", "Level {0} reward!", level), TrackRewards(got));
                            _v.SaveNow();
                        }

                        Open();
                        _v._refresh?.Invoke();
                    });
                    break;
                }

                case TrackCellState.Locked:
                {
                    // Chained shut with a padlock until the column is bought.
                    var veil = SurvivalVisuals.Image(crt, "Veil", new Color(0.03f, 0.04f, 0.10f, 0.30f));
                    veil.raycastTarget = false;
                    SurvivalVisuals.Stretch(veil.rectTransform);
                    TrackDeco(crt, "Chains", "chains", 0.02f, 0.30f, 0.98f, 0.98f, new Color(1f, 1f, 1f, 0.9f));
                    TrackSprite(crt, "Padlock", "padlock", 0.87f, 0.80f, 40f, 40f, Color.white);
                    var offer = col == TrackColumn.Premium ? track.PremiumOfferNow ?? track.RoyalOfferNow : track.RoyalOfferNow;
                    var label = SurvivalCampaignView.SheetButton(crt, "Unlock", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, 0.10f, 0.05f, 0.90f, 0.40f, 17, out var btn);
                    label.text = _v.S("reward_track.unlock", "UNLOCK") + " " + (offer != null ? track.PriceLabel(offer) : "");
                    _v.Wire(btn, () => BuyTrack(col == TrackColumn.Premium && track.PremiumOfferNow != null ? track.BuyPremium : track.BuyRoyal,
                        col == TrackColumn.Premium ? _v.S("reward_track.premium", "PREMIUM") : _v.S("reward_track.royal", "ROYAL")));
                    break;
                }

                default:
                {
                    if (state == TrackCellState.Claimed)
                    {
                        TrackSprite(crt, "Check", "check", 0.86f, 0.80f, 34f, 34f, Color.white);
                    }

                    var mark = HookSkin.Outlined(SurvivalVisuals.Text(crt, "State",
                        state == TrackCellState.Claimed ? _v.S("reward_track.done", "CLAIMED") : _v.Fmt("reward_track.at", "at LV {0}", row.Level),
                        16, TextAnchor.MiddleCenter, state == TrackCellState.Claimed ? SurvivalCampaignView.FreeGreen : SurvivalVisuals.Mute), 1.2f);
                    mark.fontStyle = FontStyle.Bold;
                    mark.raycastTarget = false;
                    HookSkin.Box(mark.rectTransform, 0.03f, 0.05f, 0.97f, 0.40f);
                    break;
                }
            }
        }

        /// <summary>Atlas sprite name for a reward id (painted/procedural ResIcon is the fallback).</summary>
        private static string? TrackIconArt(string id) => id switch
        {
            "res.soft" => "icon_coin",
            "res.food" => "icon_wheat",
            "res.hard" => "icon_gem",
            "res.hero_xp" => "icon_tome",
            "res.runner_boost" => "icon_banner",
            "res.speedup_m" => "icon_hourglass",
            _ => null
        };

        /// <summary>Up to two reward icons with rarity glow and a compact amount; hero shards show the hero's face in a crystal.</summary>
        private void TrackRewardIcons(RectTransform cell, IReadOnlyDictionary<string, int> grants, TrackColumn col, TrackCellState state)
        {
            var n = Mathf.Min(2, grants.Count);
            var i = 0;
            var dim = state == TrackCellState.Claimed ? new Color(0.6f, 0.6f, 0.6f, 0.8f) : Color.white;
            foreach (var pair in grants)
            {
                if (i >= n)
                {
                    break;
                }

                var cx = n == 1 ? 0.5f : (i == 0 ? 0.28f : 0.72f);
                var slot = new GameObject("Reward" + i, typeof(RectTransform)).GetComponent<RectTransform>();
                slot.SetParent(cell, false);
                HookSkin.Box(slot, cx - 0.22f, 0.40f, cx + 0.22f, 0.97f);
                if (state != TrackCellState.Claimed)
                {
                    var rarity = TrackRarity(col);
                    var glow = HookSkin.Glow(slot, "Glow", new Color(rarity.r, rarity.g, rarity.b, state == TrackCellState.Claimable ? 0.85f : 0.45f));
                    glow.raycastTarget = false;
                    glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(0.5f, 0.56f);
                    glow.rectTransform.sizeDelta = new Vector2(96f, 96f);
                    if (state == TrackCellState.Claimable)
                    {
                        _v.StartCoroutine(SurvivalCampaignView.Breathe(glow.rectTransform));
                    }
                }

                var shard = pair.Key.StartsWith(RewardTrackCatalog.ShardPrefix, StringComparison.Ordinal) ? pair.Key.Substring(RewardTrackCatalog.ShardPrefix.Length) : null;
                Image? icon;
                if (shard != null)
                {
                    icon = TrackShard(slot, shard, dim);
                }
                else if (TrackIconArt(pair.Key) is { } artName && TrackSprite(slot, "Icon", artName, 0.5f, 0.58f, 58f, 58f, dim) is { } atlasIcon)
                {
                    icon = atlasIcon;
                }
                else
                {
                    icon = HookSkin.ResIcon(slot, "Icon", pair.Key, SurvivalVisuals.Gold, "+");
                    icon.raycastTarget = false;
                    icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.5f, 0.58f);
                    icon.rectTransform.sizeDelta = new Vector2(54f, 54f);
                }

                var amount = HookSkin.Outlined(SurvivalVisuals.Text(slot, "Amount", "x" + ChipWallet.FormatCompact(pair.Value), 17, TextAnchor.MiddleCenter,
                    state == TrackCellState.Claimed ? SurvivalVisuals.Mute : SurvivalVisuals.Reward), 1.4f);
                amount.fontStyle = FontStyle.Bold;
                amount.raycastTarget = false;
                HookSkin.Box(amount.rectTransform, -0.2f, 0f, 1.2f, 0.30f);
                i++;
            }

            if (grants.Count > n)
            {
                var more = HookSkin.Outlined(SurvivalVisuals.Text(cell, "More", "+" + (grants.Count - n), 14, TextAnchor.UpperRight, SurvivalVisuals.Cream), 1.2f);
                more.raycastTarget = false;
                HookSkin.Box(more.rectTransform, 0.6f, 0.80f, 0.95f, 0.97f);
            }
        }

        /// <summary>Hero shard: the hero's portrait masked into a crystal frame.</summary>
        private Image? TrackShard(RectTransform slot, string heroId, Color tint)
        {
            var mask = TrackSprite(slot, "ShardFace", "shard_mask", 0.5f, 0.58f, 60f, 60f, Color.white);
            if (mask == null)
            {
                return HookSkin.ResIcon(slot, "Icon", "shard." + heroId, SurvivalVisuals.Gold, "+");
            }

            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var def = _v.Campaign?.Catalog.Hero(heroId);
            if (def != null && SurvivalArt.Get(def.PortraitKey) is { } face)
            {
                var img = SurvivalVisuals.Image(mask.transform, "Face", tint, face);
                img.raycastTarget = false;
                img.preserveAspect = true;
                SurvivalVisuals.Stretch(img.rectTransform);
            }

            return TrackSprite(slot, "ShardFrame", "shard_frame", 0.5f, 0.58f, 60f, 60f, tint);
        }

        /// <summary>Claim celebration on the overlay: rotating light rays, a ring of sparkles, all fading out.</summary>
        private void ClaimBurst(RectTransform cell, Color tint)
        {
            if (_v._overlay == null || SurvivalArt.Get(HookArtKeys.Track("burst")) is not { } rays)
            {
                return;
            }

            var host = new GameObject("TrackClaimBurst", typeof(RectTransform)).GetComponent<RectTransform>();
            host.SetParent(_v._overlay, false);
            host.position = cell.position;
            host.sizeDelta = new Vector2(10f, 10f);
            var burst = SurvivalVisuals.Image(host, "Rays", new Color(tint.r, tint.g, tint.b, 0.9f), rays);
            burst.raycastTarget = false;
            burst.rectTransform.sizeDelta = new Vector2(320f, 320f);
            var sparks = new List<RectTransform>();
            for (var k = 0; k < 8 && SurvivalArt.Get(HookArtKeys.Track("sparkle")) is { } sp; k++)
            {
                var s = SurvivalVisuals.Image(host, "Spark" + k, Color.white, sp);
                s.raycastTarget = false;
                s.rectTransform.sizeDelta = new Vector2(34f, 34f);
                sparks.Add(s.rectTransform);
            }

            _v.StartCoroutine(BurstRoutine(host, burst, sparks));
        }

        private static IEnumerator BurstRoutine(RectTransform host, Image burst, List<RectTransform> sparks)
        {
            const float dur = 0.9f;
            var t = 0f;
            while (host != null && t < dur)
            {
                t += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(t / dur);
                var ease = 1f - ((1f - u) * (1f - u));
                burst.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.3f, ease);
                burst.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -120f * u);
                var c = burst.color;
                burst.color = new Color(c.r, c.g, c.b, 0.9f * (1f - u));
                for (var k = 0; k < sparks.Count; k++)
                {
                    var a = (k / (float)sparks.Count * Mathf.PI * 2f) + u;
                    sparks[k].anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (40f + 150f * ease);
                    sparks[k].localScale = Vector3.one * (1f - (u * 0.7f));
                }

                yield return null;
            }

            if (host != null)
            {
                UnityEngine.Object.Destroy(host.gameObject);
            }
        }

        private void BuyTrack(Action<Action<PurchaseOutcome>> buy, string name)
        {
            if (_v.Track == null)
            {
                return;
            }

            var opened = _v.Track.LockedReached(TrackColumn.Premium) + _v.Track.LockedReached(TrackColumn.Royal);
            buy(outcome =>
            {
                if (outcome.Ok)
                {
                    var now = _v.Track.LockedReached(TrackColumn.Premium) + _v.Track.LockedReached(TrackColumn.Royal);
                    _v.Celebrate(_v.Fmt("reward_track.bought", "{0} unlocked! {1} rewards ready", name, Mathf.Max(0, opened - now)), new Dictionary<string, int>());
                    _v.SaveNow();
                    Open();
                    _v._refresh?.Invoke();
                }
                else
                {
                    _v._hook?.Say(_v._overlay!, _v.S("store.power_packs.failed", "Purchase failed"), SurvivalCampaignView.Warn);
                }
            });
        }

        /// <summary>Wallet rewards as they are; "shard:&lt;hero&gt;" as "&lt;Hero&gt; Shards".</summary>
        private Dictionary<string, int> TrackRewards(IReadOnlyDictionary<string, int> got)
        {
            var rows = new Dictionary<string, int>();
            foreach (var g in got)
            {
                var key = g.Key.StartsWith(RewardTrackCatalog.ShardPrefix, StringComparison.Ordinal)
                    ? _v.HeroName(g.Key.Substring(RewardTrackCatalog.ShardPrefix.Length)) + " " + _v.S("reward_track.shards", "Shards")
                    : g.Key;
                rows[key] = (rows.TryGetValue(key, out var v) ? v : 0) + g.Value;
            }

            return rows;
        }
    }
}
