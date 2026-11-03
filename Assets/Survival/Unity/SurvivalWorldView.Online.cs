#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using Survival.Domain.Alliance;
using Survival.Domain.Ids;
using Survival.Domain.Theme;
using Survival.Domain.World;
using Survival.Domain.World.Online;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Shared online world map (Phase 2). When the alliance backend is signed in, the scripted rival columns stop
    /// (Sim.MockColumns = false) and real castles, nodes, camps, marches and alliance territory from /v1/world/map are drawn
    /// over the painted map, around the player's own village. Offline (no API / no sign-in) the mock columns come back.
    /// The server settles marches lazily, so polling every 10 s is all the client does.
    /// </summary>
    public sealed partial class SurvivalWorldView
    {
        private const float OnlinePollSeconds = 10f;
        private const float PixelsPerTile = 70f;
        private const int ViewRadius = 18;

        private static readonly Color[] AllianceTints =
        {
            new Color(0.35f, 0.55f, 1f, 0.22f), new Color(0.95f, 0.35f, 0.3f, 0.22f), new Color(0.45f, 0.9f, 0.45f, 0.22f),
            new Color(0.95f, 0.75f, 0.25f, 0.22f), new Color(0.75f, 0.45f, 0.95f, 0.22f), new Color(0.3f, 0.85f, 0.9f, 0.22f),
        };

        private WorldService? _world;
        private RectTransform? _onlineLayer;
        private float _onlinePoll;
        private bool _strengthSent;
        private RectTransform? _reportsScreen;

        public WorldService? World => _world;

        private void TickOnline()
        {
            var net = SurvivalCampaignView.AllianceNet;
            if (net == null || !net.Online)
            {
                if (Sim != null && !Sim.MockColumns)
                {
                    Sim.MockColumns = true; // offline fallback: the scripted columns again
                    ClearOnline();
                }

                return;
            }

            _world ??= new WorldService(net);
            _onlinePoll -= Time.unscaledDeltaTime;
            if (_onlinePoll > 0f)
            {
                return;
            }

            _onlinePoll = OnlinePollSeconds;
            if (!_strengthSent && Sim != null)
            {
                _strengthSent = true;
                var power = Sim.Player.Power;
                _world.SyncStrength(power, Math.Max(100, power / 10), power / 100);
            }

            _world.RefreshMe(ok =>
            {
                if (!ok || _world.Castle is not { } c)
                {
                    return;
                }

                _world.RefreshMap(c.X, c.Y, ViewRadius, mapOk =>
                {
                    if (mapOk && Sim != null)
                    {
                        Sim.MockColumns = false;
                        DrawOnline();
                    }
                });
            });
        }

        private WorldProjection Projection()
        {
            var c = _world!.Castle!.Value;
            return new WorldProjection(c.X, c.Y, Sim!.Player.X, Sim.Player.Y, PixelsPerTile);
        }

        private void ClearOnline()
        {
            if (_onlineLayer != null)
            {
                Destroy(_onlineLayer.gameObject);
                _onlineLayer = null;
            }

            if (_reportsButton != null && _world is not { Online: true })
            {
                Destroy(_reportsButton.gameObject);
                _reportsButton = null;
            }
        }

        private Button? _reportsButton;

        /// <summary>Painted scroll button (top right) with the unread count; opens the reports screen.</summary>
        private void EnsureReportsButton()
        {
            if (_reportsButton == null)
            {
                _reportsButton = HookSkin.Button((RectTransform)transform, "WorldReportsButton", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill);
                Fill((RectTransform)_reportsButton.transform, 0.78f, 0.80f, 0.97f, 0.86f);
                Line((RectTransform)_reportsButton.transform, "", 22, 0f, 1f, SurvivalVisuals.Cream);
                _reportsButton.onClick.AddListener(OpenReports);
            }

            _reportsButton.GetComponentInChildren<Text>().text = F("world.reports.button", "Reports ({0})", _world!.UnreadReports);
        }

        private void DrawOnline()
        {
            if (_world?.Map is not { } map || _marchLayer == null)
            {
                return;
            }

            EnsureReportsButton();

            ClearOnline();
            _onlineLayer = new GameObject("Online", typeof(RectTransform)).GetComponent<RectTransform>();
            _onlineLayer.SetParent(_marchLayer.parent, false);
            _onlineLayer.SetSiblingIndex(_marchLayer.GetSiblingIndex());
            _onlineLayer.anchorMin = Vector2.zero;
            _onlineLayer.anchorMax = Vector2.one;
            _onlineLayer.offsetMin = _onlineLayer.offsetMax = Vector2.zero;
            var proj = Projection();

            foreach (var t in map.Territory)
            {
                var (x, y) = proj.ToMap(t.X, t.Y);
                var disc = SurvivalVisuals.Image(_onlineLayer, "Territory_" + t.Tag, AllianceTints[Math.Abs(t.Color) % AllianceTints.Length], SurvivalArt.Circle());
                disc.raycastTarget = false;
                var d = t.R * 2f * PixelsPerTile;
                SurvivalVillageView.Point(disc.rectTransform, x, y, d, d, 0.5f);
            }

            foreach (var n in map.Tiles)
            {
                if (!map.Visible(n.X, n.Y))
                {
                    continue; // fog: the server already filters, this keeps stale tiles hidden too
                }

                var key = n.IsCamp ? WorldArtKeys.MapProp("camp") : WorldArtKeys.MapProp(n.Kind);
                var tile = n;
                Marker(proj, n.X, n.Y, key, n.IsCamp ? 150f : 130f, null, () => ShowOnlineTile(tile));
            }

            foreach (var c in map.Castles)
            {
                if (c.Mine)
                {
                    continue; // the painted home village already sits here
                }

                var castle = c;
                var label = string.IsNullOrEmpty(c.Tag) ? c.Name : "[" + c.Tag + "] " + c.Name;
                Marker(proj, c.X, c.Y, WorldArtKeys.MapProp("castle"), 190f, label, () => ShowOnlineCastle(castle));
            }

            foreach (var m in map.Marches)
            {
                var (x, y) = proj.ToMap(m.X, m.Y);
                var dot = HookSkin.Art(_onlineLayer, "March_" + m.Id, m.Mine || m.Ally ? WorldArtKeys.MarchPlayer : WorldArtKeys.MarchRival, Color.white);
                dot.raycastTarget = false;
                SurvivalVillageView.Point(dot.rectTransform, x, y, 90f, 90f, 0.3f);
            }
        }

        private void Marker(WorldProjection proj, int tx, int ty, string key, float size, string? label, Action onTap)
        {
            var (x, y) = proj.ToMap(tx, ty);
            var img = HookSkin.Art(_onlineLayer!, "Tile_" + tx + "_" + ty, key, SurvivalVisuals.Gold);
            SurvivalVillageView.Point(img.rectTransform, x, y, size, size, 0.3f);
            var btn = img.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(() => onTap());
            if (label != null)
            {
                // one short cream line per castle; nodes and camps stay unlabeled so the map stays clean
                var t = HookSkin.Outlined(SurvivalVisuals.Text(_onlineLayer!, "Name", label, 24, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.6f);
                t.fontStyle = FontStyle.Bold;
                t.raycastTarget = false;
                SurvivalVillageView.Point(t.rectTransform, x, y - size * 0.15f, 420f, 34f, 1f);
            }
        }

        private string TroopsLine() => F("world.online.troops", "Troops at home {0}  \u00b7  Marches {1}/{2}",
            ChipWallet.FormatGrouped(_world!.Troops), _world.MyMarches.Count, _world.MaxMarches);

        private void ShowOnlineTile(WorldTile n)
        {
            var mine = FindMyMarch(n.X, n.Y);
            if (n.IsCamp)
            {
                ShowCard(WorldArtKeys.MapProp("camp"), F("world.card.camp", "Enemy camp \u00b7 Lv {0}", n.Level),
                    S("world.online.camp_explain", "Beat the camp to win Gold and hero XP."), TroopsLine(),
                    mine != null ? S("world.online.recall", "Recall") : S("world.action.attack", "Attack"),
                    () => SendOrRecall(mine, "camp", n.X, n.Y));
                return;
            }

            var who = n.OccupiedByMe ? S("world.online.yours", "You are gathering here") : n.OccupiedByAlly ? S("world.online.ally_here", "An ally is gathering here")
                : n.Occupier != 0 ? S("world.online.taken", "Occupied: winning the fight takes the field") : S("world.online.free", "Free to gather");
            ShowCard(WorldArtKeys.MapProp(n.Kind), F("world.online.field", "{0} field \u00b7 Lv {1}", S(n.Kind + ".name", n.Kind), n.Level),
                who + "  \u00b7  " + ChipWallet.FormatGrouped(n.Amount) + " " + S("world.online.left", "left"), TroopsLine(),
                mine != null ? S("world.online.recall", "Recall") : S("world.action.gather", "Gather"),
                () => SendOrRecall(mine, "gather", n.X, n.Y), mine != null || !n.OccupiedByAlly);
        }

        private void ShowOnlineCastle(WorldCastle c)
        {
            var shielded = c.Shielded;
            ShowCard(WorldArtKeys.MapProp("castle"), c.Name, (c.Ally ? S("world.card.ally", "Guild ally") : S("world.card.rival", "Rival lord")) + "  \u00b7  " +
                F("world.card.power", "Power {0}", ChipWallet.FormatGrouped(c.Power)),
                shielded ? S("world.online.shielded", "Shielded: cannot be attacked right now") : TroopsLine(),
                c.Ally ? null : S("world.action.attack", "Attack"), () => SendOrRecall(null, "attack", c.X, c.Y), !shielded);
        }

        private WorldMarchInfo? FindMyMarch(int x, int y)
        {
            foreach (var m in _world!.MyMarches)
            {
                if (m.ToX == x && m.ToY == y)
                {
                    return m;
                }
            }

            return null;
        }

        private void SendOrRecall(WorldMarchInfo? mine, string kind, int x, int y)
        {
            if (_world == null)
            {
                return;
            }

            Action<bool> after = ok =>
            {
                Pop(Sim!.Player, ok ? S("world.online.sent", "March!") : S("world.online.err." + _world.LastError, _world.LastError ?? "Try again"), ok ? PlayerLine : SurvivalVisuals.Cream);
                _onlinePoll = 0f;
                _card?.Root.gameObject.SetActive(false);
            };
            if (mine != null)
            {
                _world.Recall(mine.Id, after);
                return;
            }

            // half the troops at home, at least one: the sheet always shows the balance first
            _world.SendMarch(kind, x, y, Math.Max(1, _world.Troops / 2), after);
        }

        /// <summary>Battle reports with one Claim button that moves all unclaimed loot into the wallet.</summary>
        public void OpenReports()
        {
            if (_world == null)
            {
                return;
            }

            _world.RefreshReports(ok => { if (ok) DrawReports(); });
        }

        private void DrawReports()
        {
            if (_reportsScreen != null)
            {
                Destroy(_reportsScreen.gameObject);
            }

            var root = (RectTransform)transform;
            var panel = HookSkin.Panel(root, "WorldReports", HookArtKeys.Plate, new Color(0.11f, 0.17f, 0.38f, 1f), 0.35f);
            _reportsScreen = panel.rectTransform;
            Fill(_reportsScreen, 0.05f, 0.12f, 0.95f, 0.88f);
            Ornaments.Frame(panel.transform, Ornaments.Gold, 2f);
            Line(_reportsScreen, S("world.reports.title", "Battle Reports"), 34, 0.9f, 0.98f, HookSkin.ParchmentTitle);
            Line(_reportsScreen, S("world.reports.explain", "What happened while you were away. Claim adds the loot to your wallet."), 18, 0.84f, 0.9f, SurvivalVisuals.Cream);
            var y = 0.82f;
            foreach (var r in _world!.Reports)
            {
                if (y < 0.2f)
                {
                    break;
                }

                var loot = new List<string>();
                foreach (var kv in r.Loot)
                {
                    loot.Add("+" + ChipWallet.FormatGrouped(kv.Value) + " " + S(kv.Key + ".name", kv.Key));
                }

                var head = S("world.reports." + r.Kind, r.Kind) + (string.IsNullOrEmpty(r.Opponent) ? "" : "  \u00b7  " + r.Opponent);
                var body = F("world.reports.losses", "Lost {0}  \u00b7  They lost {1}", r.MyLoss, r.TheirLoss) + (loot.Count > 0 ? "  \u00b7  " + string.Join("  ", loot) : "");
                Line(_reportsScreen, head, 22, y - 0.04f, y, r.Won ? WonLine : SurvivalVisuals.Cream);
                Line(_reportsScreen, body, 18, y - 0.08f, y - 0.04f, SurvivalVisuals.Cream);
                y -= 0.1f;
            }

            var claim = HookSkin.Button(_reportsScreen, "Claim", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill);
            Fill((RectTransform)claim.transform, 0.3f, 0.04f, 0.7f, 0.12f);
            Line((RectTransform)claim.transform, S("world.reports.claim", "Claim all"), 26, 0f, 1f, SurvivalVisuals.Cream);
            claim.onClick.AddListener(() => _world.ClaimReports(c =>
            {
                if (c != null)
                {
                    foreach (var kv in c.Loot)
                    {
                        Grant(kv.Key == "gold" ? ChipWallet.SoftChip : ChipWallet.ChipFor(ResourceFor(kv.Key)), kv.Value);
                    }
                }

                if (_reportsScreen != null)
                {
                    Destroy(_reportsScreen.gameObject);
                }
            }));
            var close = HookSkin.Button(_reportsScreen, "Close", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill);
            Fill((RectTransform)close.transform, 0.86f, 0.92f, 0.98f, 0.99f);
            Line((RectTransform)close.transform, "X", 26, 0f, 1f, SurvivalVisuals.Cream);
            close.onClick.AddListener(() => Destroy(_reportsScreen!.gameObject));
        }

        private static readonly Color WonLine = new Color(0.55f, 0.95f, 0.55f, 1f);

        private static void Fill(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void Line(RectTransform parent, string text, int size, float y0, float y1, Color color)
        {
            var t = HookSkin.Outlined(SurvivalVisuals.Text(parent, "Line", text, size, TextAnchor.MiddleCenter, color), 1.5f);
            t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false;
            Fill(t.rectTransform, 0.04f, y0, 0.96f, y1);
        }
    }
}
