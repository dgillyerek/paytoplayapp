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
        private float _stockSync;
        private readonly List<RectTransform> _onlineLabels = new();
        private int _sendTroops;
        private RectTransform? _troopSlider;
        private Slider? _troopSliderCtl;
        private Text? _troopSliderText;

        /// <summary>Cream #FFF4D6, the outlined text colour on ornate panels and gold.</summary>
        private static readonly Color CreamText = new Color(1f, 0.957f, 0.839f, 1f);
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
            _stockSync -= OnlinePollSeconds;
            if (_stockSync <= 0f && Sim != null)
            {
                // power + stored resources every minute (raiders loot real stock). The troop number only seeds a brand-new
                // castle once; after that the server owns the count and the phone just shows it.
                _stockSync = 60f;
                var power = Sim.Player.Power;
                _world.SyncStrength(power, Math.Max(100, power / 10), power / 100, Stock());
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

        private Dictionary<string, int>? Stock()
        {
            if (_session == null)
            {
                return null;
            }

            var d = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var k in new[] { "food", "wood", "stone", "gold" })
            {
                d[k] = _session.Chips.Wallet.Get(ResourceFor(k));
            }

            return d;
        }

        private WorldProjection Projection()
        {
            var c = _world!.Castle!.Value;
            return new WorldProjection(c.X, c.Y, Sim!.Player.X, Sim.Player.Y, PixelsPerTile);
        }

        private RectTransform? _onlineClearings;

        private void ClearOnline()
        {
            if (_onlineClearings != null)
            {
                Destroy(_onlineClearings.gameObject);
                _onlineClearings = null;
            }

            if (_onlineLayer != null)
            {
                Destroy(_onlineLayer.gameObject);
                _onlineLayer = null;
            }

            foreach (var l in _onlineLabels)
            {
                _labels.Remove(l);
            }

            _onlineLabels.Clear();

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

            DrawOnlineTilesAndCastles(map, proj);

            foreach (var m in map.Marches)
            {
                var (x, y) = proj.ToMap(m.X, m.Y);
                var dot = HookSkin.Art(_onlineLayer, "March_" + m.Id, m.Mine || m.Ally ? WorldArtKeys.MarchPlayer : WorldArtKeys.MarchRival, Color.white);
                dot.raycastTarget = false;
                SurvivalVillageView.Point(dot.rectTransform, x, y, 90f, 90f, 0.3f);
            }
        }

        /// <summary>
        /// Online tiles and other lords' castles use the same spacing rule as the six map places (#130): each is nudged off its tile just
        /// enough that no building or name plate overlaps another, and tapping the plate opens the same card as the castle.
        /// </summary>
        private void DrawOnlineTilesAndCastles(WorldSnapshot map, WorldProjection proj)
        {
            var places = new List<WorldEntity>();
            var tiles = new Dictionary<string, WorldTile>(StringComparer.Ordinal);
            foreach (var n in map.Tiles)
            {
                if (!map.Visible(n.X, n.Y))
                {
                    continue; // fog: the server already filters, this keeps stale tiles hidden too
                }

                var (x, y) = proj.ToMap(n.X, n.Y);
                var id = "tile_" + n.Id.ToString(CultureInfo.InvariantCulture);
                // empty name = unlabeled marker; spacing keeps it off every building and name plate
                places.Add(new WorldEntity(id, n.IsCamp ? WorldEntityKind.EnemyCamp : WorldEntityKind.ResourceNode, n.Kind, "", n.Level, 0, n.Amount, x, y));
                tiles[id] = n;
            }

            var byId = new Dictionary<string, WorldCastle>(StringComparer.Ordinal);
            foreach (var c in map.Castles)
            {
                if (c.Mine)
                {
                    continue; // the painted home village already sits here
                }

                var (x, y) = proj.ToMap(c.X, c.Y);
                var id = "online_" + c.Id.ToString(CultureInfo.InvariantCulture);
                var label = string.IsNullOrEmpty(c.Tag) ? c.Name : "[" + c.Tag + "] " + c.Name;
                places.Add(new WorldEntity(id, c.Ally ? WorldEntityKind.AllyVillage : WorldEntityKind.RivalVillage, "castle", label, 0, c.Power, 0, x, y));
                byId[id] = c;
            }

            var fixedPlaces = Layout != null ? Layout.Entities : (IReadOnlyList<WorldEntity>)Array.Empty<WorldEntity>();
            var k = Rig?.Camera != null ? WorldSpacing.PlateScale(Rig.Camera.Zoom) : 1f;
            if (_clearings != null)
            {
                _onlineClearings = new GameObject("OnlineClearings", typeof(RectTransform)).GetComponent<RectTransform>();
                _onlineClearings.SetParent(_clearings, false);
                _onlineClearings.anchorMin = Vector2.zero;
                _onlineClearings.anchorMax = Vector2.one;
                _onlineClearings.offsetMin = _onlineClearings.offsetMax = Vector2.zero;
            }

            foreach (var e in WorldSpacing.Separate(places, fixedPlaces, obstacles: Layout?.PropFootprints()))
            {
                if (Layout != null && Layout.OnHeart(e))
                {
                    AddClearing(e, _onlineClearings);
                }

                if (tiles.TryGetValue(e.Id, out var tile))
                {
                    var ts = WorldSpacing.SizeFor(e);
                    var img = HookSkin.Art(_onlineLayer!, e.Id, WorldArtKeys.OnlineTile(tile.Kind, tile.Level), SurvivalVisuals.Gold);
                    SurvivalVillageView.Point(img.rectTransform, e.X, e.Y, ts, ts, WorldSpacing.ArtPivotY);
                    var tb = img.gameObject.AddComponent<Button>();
                    tb.transition = Selectable.Transition.None;
                    tb.onClick.AddListener(() => ShowOnlineTile(tile));
                    continue;
                }

                var castle = byId[e.Id];
                Action open = () => ShowOnlineCastle(castle);
                var size = WorldSpacing.SizeFor(e);
                var art = HookSkin.Art(_onlineLayer!, e.Id, WorldArtKeys.Village(castle.Ally ? WorldEntityKind.AllyVillage : WorldEntityKind.RivalVillage), SurvivalVisuals.Gold);
                SurvivalVillageView.Point(art.rectTransform, e.X, e.Y, size, size, WorldSpacing.ArtPivotY);
                var btn = art.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => open());

                var plate = HookSkin.Panel(_onlineLayer!, e.Id + "_plate", castle.Ally ? HookArtKeys.ButtonGreen : HookArtKeys.Chip, SurvivalVisuals.PanelSoft, 0.38f);
                SurvivalVillageView.Point(plate.rectTransform, e.X, e.Y - size * WorldSpacing.PlateDrop, WorldSpacing.PlateWidth, WorldSpacing.PlateHeightWithSub, 1f);
                plate.rectTransform.localScale = new Vector3(k, k, 1f);
                var name = HookSkin.Outlined(SurvivalVisuals.Text(plate.transform, "Name", e.Name, 24, TextAnchor.MiddleCenter, CreamText), 1.5f);
                name.fontStyle = FontStyle.Bold;
                name.raycastTarget = false;
                Fill(name.rectTransform, 0.04f, 0.45f, 0.96f, 1f);
                var sub = HookSkin.Outlined(SurvivalVisuals.Text(plate.transform, "Sub", F("world.card.power", "Power {0}", ChipWallet.FormatCompact(e.Power)), 20, TextAnchor.MiddleCenter, SurvivalVisuals.Reward), 1.2f);
                sub.raycastTarget = false;
                Fill(sub.rectTransform, 0.04f, 0.04f, 0.96f, 0.5f);
                plate.raycastTarget = true;
                var plateBtn = plate.gameObject.AddComponent<Button>();
                plateBtn.targetGraphic = plate;
                plateBtn.transition = Selectable.Transition.None;
                plateBtn.onClick.AddListener(() => open());
                _labels.Add(plate.rectTransform);
                _onlineLabels.Add(plate.rectTransform);
            }
        }

        // ---------- troop slider (send card) ----------

        /// <summary>
        /// Ornate strip above the map card: "Send N of M at home" in cream, a gold bar slider from 1 to everything at home.
        /// Only shown for a new march; recall and ally cards hide it.
        /// </summary>
        private void ShowTroopSlider(bool show)
        {
            if (!show || _card == null || _world == null)
            {
                if (_troopSlider != null)
                {
                    _troopSlider.gameObject.SetActive(false);
                }

                return;
            }

            if (_troopSlider == null)
            {
                var strip = HookSkin.Panel(_card.Root, "TroopSlider", HookArtKeys.Panel, SurvivalVisuals.InspectFill, 0.5f);
                strip.raycastTarget = true;
                _troopSlider = strip.rectTransform;
                Fill(_troopSlider, 0f, 1.02f, 1f, 1.42f);
                Ornaments.Frame(strip.transform, Ornaments.Gold, 1.5f);
                _troopSliderText = HookSkin.Outlined(SurvivalVisuals.Text(strip.transform, "Count", "", 26, TextAnchor.MiddleCenter, CreamText), 1.8f);
                _troopSliderText.fontStyle = FontStyle.Bold;
                _troopSliderText.raycastTarget = false;
                Fill(_troopSliderText.rectTransform, 0.04f, 0.56f, 0.96f, 0.96f);

                var root = new GameObject("Slider", typeof(RectTransform)).GetComponent<RectTransform>();
                root.SetParent(strip.transform, false);
                Fill(root, 0.08f, 0.12f, 0.92f, 0.5f);
                var frame = HookSkin.Panel(root, "Frame", HookArtKeys.BarFrame, SurvivalVisuals.RowFill, 0.4f);
                SurvivalVisuals.Stretch(frame.rectTransform);
                var area = new GameObject("FillArea", typeof(RectTransform)).GetComponent<RectTransform>();
                area.SetParent(root, false);
                Fill(area, 0.02f, 0.18f, 0.98f, 0.82f);
                var fill = HookSkin.Panel(area, "Fill", HookArtKeys.BarFillGold, SurvivalVisuals.MineFill, 0.4f);
                fill.raycastTarget = false;
                var handleArea = new GameObject("HandleArea", typeof(RectTransform)).GetComponent<RectTransform>();
                handleArea.SetParent(root, false);
                Fill(handleArea, 0.02f, 0f, 0.98f, 1f);
                var handle = HookSkin.Art(handleArea, "Handle", HookArtKeys.ButtonGold, SurvivalVisuals.Gold);
                handle.rectTransform.sizeDelta = new Vector2(54f, 0f);
                handle.rectTransform.anchorMin = new Vector2(0f, -0.25f);
                handle.rectTransform.anchorMax = new Vector2(0f, 1.25f);

                var ctl = root.gameObject.AddComponent<Slider>();
                ctl.fillRect = fill.rectTransform;
                ctl.handleRect = handle.rectTransform;
                ctl.targetGraphic = handle;
                ctl.direction = Slider.Direction.LeftToRight;
                ctl.minValue = 0f;
                ctl.maxValue = 1f;
                ctl.onValueChanged.AddListener(v =>
                {
                    _sendTroops = MarchTroops.FromSlider(v, _world?.Troops ?? 0);
                    UpdateTroopSliderText();
                });
                _troopSliderCtl = ctl;
            }

            _troopSlider.gameObject.SetActive(true);
            var home = _world.Troops;
            _sendTroops = MarchTroops.Start(home);
            _troopSliderCtl!.interactable = home > 1;
            _troopSliderCtl.value = MarchTroops.ToSlider(_sendTroops, home); // the listener maps it straight back to the same count
            UpdateTroopSliderText();
        }

        private void UpdateTroopSliderText()
        {
            if (_troopSliderText != null && _world != null)
            {
                _troopSliderText.text = _world.Troops <= 0
                    ? S("world.online.no_troops", "No troops at home")
                    : F("world.online.send_count", "Send {0}  of  {1} at home", ChipWallet.FormatGrouped(_sendTroops), ChipWallet.FormatGrouped(_world.Troops));
            }
        }

        private string TroopsLine() => F("world.online.troops", "Troops at home {0}  \u00b7  Marches {1}/{2}",
            ChipWallet.FormatGrouped(_world!.Troops), _world.MyMarches.Count, _world.MaxMarches);

        private void ShowOnlineTile(WorldTile n)
        {
            var mine = FindMyMarch(n.X, n.Y);
            if (n.IsCamp)
            {
                ShowCard(WorldArtKeys.OnlineTile(n.Kind, n.Level), F("world.card.camp", "Enemy camp \u00b7 Lv {0}", n.Level),
                    S("world.online.camp_explain", "Beat the camp to win Gold and hero XP."), TroopsLine(),
                    mine != null ? S("world.online.recall", "Recall") : S("world.action.attack", "Attack"),
                    () => SendOrRecall(mine, "camp", n.X, n.Y), mine != null || _world!.Troops > 0);
                ShowTroopSlider(mine == null);
                return;
            }

            var who = n.OccupiedByMe ? S("world.online.yours", "You are gathering here") : n.OccupiedByAlly ? S("world.online.ally_here", "An ally is gathering here")
                : n.Occupier != 0 ? S("world.online.taken", "Occupied: winning the fight takes the field") : S("world.online.free", "Free to gather");
            ShowCard(WorldArtKeys.OnlineTile(n.Kind, n.Level), F("world.online.field", "{0} field \u00b7 Lv {1}", S(n.Kind + ".name", n.Kind), n.Level),
                who + "  \u00b7  " + ChipWallet.FormatGrouped(n.Amount) + " " + S("world.online.left", "left"), TroopsLine(),
                mine != null ? S("world.online.recall", "Recall") : S("world.action.gather", "Gather"),
                () => SendOrRecall(mine, "gather", n.X, n.Y), mine != null || (!n.OccupiedByAlly && _world!.Troops > 0));
            ShowTroopSlider(mine == null && !n.OccupiedByAlly);
        }

        private void ShowOnlineCastle(WorldCastle c)
        {
            var shielded = c.Shielded;
            ShowCard(WorldArtKeys.Village(c.Ally ? WorldEntityKind.AllyVillage : WorldEntityKind.RivalVillage), c.Name, (c.Ally ? S("world.card.ally", "Guild ally") : S("world.card.rival", "Rival lord")) + "  \u00b7  " +
                F("world.card.power", "Power {0}", ChipWallet.FormatGrouped(c.Power)),
                shielded ? S("world.online.shielded", "Shielded: cannot be attacked right now") : TroopsLine(),
                c.Ally ? null : S("world.action.attack", "Attack"), () => SendOrRecall(null, "attack", c.X, c.Y), !shielded && _world!.Troops > 0);
            ShowTroopSlider(!c.Ally && !shielded);
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

            // what the slider says, kept inside 1..troops at home (the server checks it again against its own count)
            var send = MarchTroops.Clamp(_sendTroops, _world.Troops);
            if (!MarchTroops.CanSend(send, _world.Troops))
            {
                Pop(Sim!.Player, S("world.online.no_troops", "No troops at home"), SurvivalVisuals.Cream);
                return;
            }

            _world.SendMarch(kind, x, y, send, after);
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
                    loot.Add((kv.Value < 0 ? "-" : "+") + ChipWallet.FormatGrouped(Math.Abs(kv.Value)) + " " + S(kv.Key + ".name", kv.Key));
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

                    // what raiders carried off your stores (the server already took it from your stock)
                    foreach (var kv in c.Lost)
                    {
                        Take(kv.Key == "gold" ? ChipWallet.SoftChip : ChipWallet.ChipFor(ResourceFor(kv.Key)), kv.Value);
                    }

                    _stockSync = 0f;
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
