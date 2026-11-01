#nullable enable
using System;
using Survival.Domain.Alliance;
using Survival.Domain.Ids;
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Bottom-bar Alliance and Bag screens (Derek 2026-10-31: "these buttons do nothing"). Ornate v1 cards: gold frame,
    /// bevel, grain, cream outlined text. Alliance shows the simulated guild with a Help tab (guild help from PR #115 slots
    /// in there); Bag lists the wallet's items with Use buttons where a use exists.
    /// </summary>
    public sealed partial class SurvivalCampaignView
    {
        private AllianceOverview? _alliance;
        private string _allianceTab = AllianceOverview.TabMembers;

        private void WireAllianceBag(SurvivalPlayView play)
        {
            play.AllianceTapped += () => OpenAlliance(AllianceOverview.TabMembers);
            play.BagTapped += OpenBag;
        }

        private static readonly Color AllyCaption = new Color(1f, 0.86f, 0.48f, 1f);
        private static readonly Color AllyCrestGlow = new Color(0.35f, 0.55f, 1f, 0.75f);
        private static readonly Color AllyOnline = new Color(0.45f, 0.9f, 0.45f, 1f);
        private static readonly string[] AllyPortraits = { "sir_aldric", "rowan", "oakenshield", "stormcrest", "emberfang", "ironhowl", "nightfang", "vespera", "ashwyrm", "blightroot", "bonequill" };

        /// <summary>Orb-style card: navy plate, bevel, gold frame, gold rivets in the corners.</summary>
        private RectTransform OrnateCard(RectTransform parent, string name, float x0, float y0, float x1, float y1, bool lit)
        {
            var card = HookSkin.Panel(parent, name, HookArtKeys.Plate, new Color(0.11f, 0.17f, 0.38f, 1f), 0.35f);
            card.raycastTarget = false;
            Box(card.rectTransform, x0, y0, x1, y1);
            Ornaments.Bevel(card.transform, lit ? 0.9f : 0.6f);
            Ornaments.Frame(card.transform, Ornaments.Gold, 2f);
            return card.rectTransform;
        }

        private Text Label(RectTransform parent, string name, string text, int size, TextAnchor anchor, Color color, float x0, float y0, float x1, float y1)
        {
            var t = HookSkin.Outlined(SurvivalVisuals.Text(parent, name, text, size, anchor, color), 1.5f);
            t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false;
            Box(t.rectTransform, x0, y0, x1, y1);
            return t;
        }

        /// <summary>Painted art in a small gold-framed dark well (item slots, portraits).</summary>
        private static Image ArtSlot(RectTransform parent, string name, string key, Color fallback, float x0, float y0, float x1, float y1)
        {
            var slot = SurvivalVisuals.Image(parent, name, new Color(0.06f, 0.09f, 0.22f, 1f));
            slot.raycastTarget = false;
            Box(slot.rectTransform, x0, y0, x1, y1);
            Ornaments.Frame(slot.transform, Ornaments.Gold, 1.5f);
            var art = HookSkin.Art(slot.transform, "Art", key, fallback);
            art.raycastTarget = false;
            Box(art.rectTransform, 0.06f, 0.06f, 0.94f, 0.94f);
            return art;
        }

        private static Color RoleColor(string role) => role switch
        {
            "leader" => new Color(0.66f, 0.16f, 0.16f, 1f),
            "officer" => new Color(0.16f, 0.32f, 0.66f, 1f),
            _ => new Color(0.16f, 0.47f, 0.27f, 1f),
        };

        private void OpenAlliance(string tab)
        {
            EnsureAllianceNet();
            var net = AllianceNet;
            if (net is { Online: true, InAlliance: false })
            {
                OpenAllianceFind();
                return;
            }

            _alliance = net is { InAlliance: true } ? AllianceOverview.From(net.Alliance!) : _alliance ?? AllianceOverview.Simulated();
            _allianceTab = tab;
            var a = _alliance;
            var panel = OpenScreen("AllianceScreen", S("alliance.title", "ALLIANCE"));

            // Hero art: the painted guild crest with a soft royal glow, guild name and stats beside it.
            var glow = HookSkin.Glow(panel, "CrestGlow", AllyCrestGlow);
            Box(glow.rectTransform, 0.00f, 0.74f, 0.42f, 0.95f);
            var crest = HookSkin.Art(panel, "Crest", HookArtKeys.GuildCrest, Ornaments.Gold);
            crest.raycastTarget = false;
            Box(crest.rectTransform, 0.05f, 0.765f, 0.37f, 0.93f);
            Ornaments.Sparkles(crest.rectTransform, 3, new Color(1f, 0.9f, 0.6f, 1f));
            HookSkin.Fit(Label(panel, "Name", a.GuildName, 30, TextAnchor.MiddleLeft, SurvivalVisuals.Cream, 0.39f, 0.855f, 0.96f, 0.925f), 16, 30).horizontalOverflow = HorizontalWrapMode.Wrap;
            Label(panel, "Sub", Fmt("alliance.summary2", "Level {0}  \u00b7  {1} members", a.Level, a.Members.Count), 20, TextAnchor.MiddleLeft, AllyCaption, 0.39f, 0.815f, 0.96f, 0.855f);
            Label(panel, "Online", Fmt("alliance.online", "{0} online now", a.OnlineCount), 20, TextAnchor.MiddleLeft, AllyOnline, 0.39f, 0.775f, 0.96f, 0.815f);
            var explain = Label(panel, "Explain", S("alliance.explain", "Your guild. Guildmates help speed up your builds."), 19, TextAnchor.MiddleCenter, SurvivalVisuals.Cream, 0.04f, 0.735f, 0.96f, 0.772f);
            HookSkin.Fit(explain, 12, 19);

            var members = tab == AllianceOverview.TabMembers;
            if (net is { InAlliance: true })
            {
                AllyTab(panel, "TabMembers", S("alliance.tab.members", "MEMBERS"), members, 0.03f, 0.25f, () => OpenAlliance(AllianceOverview.TabMembers));
                AllyTab(panel, "TabChat", S("alliance.tab.chat", "CHAT"), tab == TabChat, 0.27f, 0.49f, () => OpenAlliance(TabChat));
                AllyTab(panel, "TabHelp", S("alliance.tab.help", "HELP"), tab == AllianceOverview.TabHelp, 0.51f, 0.73f, () => OpenAlliance(AllianceOverview.TabHelp));
                AllyTab(panel, "TabGifts", Fmt("alliance.tab.gifts", "GIFTS {0}", net.Gifts.Count == 0 ? "" : "(" + net.Gifts.Count + ")"), tab == TabGifts, 0.75f, 0.97f, () => OpenAlliance(TabGifts));
                if (!members)
                {
                    DrawAllianceOnlineTab(panel, net, tab);
                    AllianceCloseButton(panel);
                    return;
                }
            }
            else
            {
                AllyTab(panel, "TabMembers", S("alliance.tab.members", "MEMBERS"), members, 0.05f, 0.49f, () => OpenAlliance(AllianceOverview.TabMembers));
                AllyTab(panel, "TabHelp", S("alliance.tab.help", "HELP"), !members, 0.51f, 0.95f, () => OpenAlliance(AllianceOverview.TabHelp));
            }

            if (members)
            {
                const float top = 0.675f, rowH = 0.057f;
                for (var i = 0; i < a.Members.Count; i++)
                {
                    var m = a.Members[i];
                    var y1 = top - i * rowH;
                    var row = OrnateCard(panel, "Member_" + i, 0.04f, y1 - rowH + 0.005f, 0.96f, y1, m.Role == "leader");
                    ArtSlot(row, "Portrait", "theme_a.hero.portrait." + AllyPortraits[i % AllyPortraits.Length], SurvivalVisuals.PanelSoft, 0.02f, 0.08f, 0.135f, 0.92f);
                    var dot = SurvivalVisuals.Image(row, "Online", m.Online ? AllyOnline : new Color(0.45f, 0.45f, 0.45f, 1f));
                    dot.raycastTarget = false;
                    Box(dot.rectTransform, 0.115f, 0.08f, 0.145f, 0.32f);
                    Ornaments.Frame(dot.transform, Ornaments.Gold, 1f);
                    HookSkin.Fit(Label(row, "Name", m.Name, 24, TextAnchor.MiddleLeft, SurvivalVisuals.Cream, 0.165f, 0.48f, 0.66f, 0.96f), 14, 24);

                    // Role badge: coloured pill, gold rim, cream outlined text.
                    var badge = SurvivalVisuals.Image(row, "RoleBadge", RoleColor(m.Role));
                    badge.raycastTarget = false;
                    Box(badge.rectTransform, 0.165f, 0.08f, 0.38f, 0.44f);
                    Ornaments.Bevel(badge.transform, 0.6f);
                    Ornaments.Frame(badge.transform, Ornaments.Gold, 1f);
                    var role = Label(badge.rectTransform, "Role", S("alliance.role." + m.Role, m.Role.ToUpperInvariant()), 15, TextAnchor.MiddleCenter, SurvivalVisuals.Cream, 0f, 0f, 1f, 1f);
                    HookSkin.Fit(role, 9, 15);

                    var banner = HookSkin.Art(row, "PowerIcon", HookArtKeys.WarBanner, Ornaments.Gold);
                    banner.raycastTarget = false;
                    Box(banner.rectTransform, 0.62f, 0.1f, 0.71f, 0.9f);
                    Label(row, "Power", m.Power.ToString("N0"), 24, TextAnchor.MiddleRight, SurvivalVisuals.Cream, 0.70f, 0.1f, 0.97f, 0.9f);
                }
            }
            else
            {
                var help = OrnateCard(panel, "HelpCard", 0.06f, 0.36f, 0.94f, 0.67f, true);
                var hand = HookSkin.Art(help, "HelpHand", HookArtKeys.HelpHand, Ornaments.Gold);
                hand.raycastTarget = false;
                Box(hand.rectTransform, 0.30f, 0.45f, 0.70f, 0.97f);
                Label(help, "Title", S("alliance.help.title", "Guild Help"), 30, TextAnchor.MiddleCenter, SurvivalVisuals.Cream, 0.05f, 0.30f, 0.95f, 0.45f);
                var body = Label(help, "Body", S("alliance.help.body", "Ask guildmates to speed up your upgrades, and help theirs for rewards. Connect to the internet to join a real alliance."),
                    21, TextAnchor.MiddleCenter, SurvivalVisuals.Cream, 0.07f, 0.04f, 0.93f, 0.30f);
                HookSkin.Fit(body, 12, 21);
            }

            var close = SheetButton(panel, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.30f, 0.025f, 0.70f, 0.082f, 26, out var closeBtn);
            close.text = S("hook.close", "Close");
            Wire(closeBtn, CloseScreen);
        }

        private void AllyTab(RectTransform panel, string name, string label, bool on, float x0, float x1, Action tap)
        {
            var text = SheetButton(panel, name, on ? HookArtKeys.ButtonGreen : HookArtKeys.ButtonBlue, on ? SurvivalVisuals.MineFill : SurvivalVisuals.PanelSoft, x0, 0.685f, x1, 0.728f, 24, out var btn);
            text.text = label;
            Wire(btn, tap);
        }

        private (string Button, string Hint) BagLine(BagItem item, Survival.Domain.Clock.GameTimer? running) => item.ResourceId switch
        {
            SurvIds.ResSpeedupM => (S("bag.use", "USE"), running != null
                ? Fmt("bag.hint.speedup_on", "Speeds up: {0} ({1} left)", BagTimerName(running), Survival.Domain.Clock.TimerQueue.FormatClock(running.Remaining(_session!.Hook!.Timers.Clock.Now)))
                : S("bag.hint.speedup_idle", "Start an upgrade to use these")),
            SurvIds.ResHeroXp => (S("bag.go.heroes", "HEROES \u25b6"), S("bag.hint.hero_xp", "Level up your heroes")),
            SurvIds.ResEnergy => (S("bag.go.road", "ROAD \u25b6"), S("bag.hint.energy", "Spend it on the Road")),
            SurvIds.ResRunnerBoost => (S("bag.go.road", "ROAD \u25b6"), S("bag.hint.runner_boost", "Extra power for one run")),
            SurvIds.ResAllianceGift => ("", S("bag.hint.alliance_gift", "Gifts from your guildmates")),
            _ => ("", ""),
        };

        private string BagTimerName(Survival.Domain.Clock.GameTimer t)
        {
            var type = _session?.Hook?.Base.Find(t.TargetId)?.Def.BuildType;
            return type != null ? S("build.type." + type, type) : S("hook.plot." + t.TargetId, t.TargetId);
        }

        private void OpenBag()
        {
            if (Campaign == null)
            {
                return;
            }

            var panel = OpenScreen("BagScreen", S("bag.title", "BAG"));

            // Hero art: the open treasure chest with a warm glow, and what this screen is for.
            var glow = HookSkin.Glow(panel, "ChestGlow", new Color(1f, 0.78f, 0.42f, 0.6f));
            Box(glow.rectTransform, 0.25f, 0.74f, 0.75f, 0.95f);
            var chest = HookSkin.Art(panel, "Chest", HookArtKeys.ChestOpen, Ornaments.Gold);
            chest.raycastTarget = false;
            Box(chest.rectTransform, 0.32f, 0.77f, 0.68f, 0.93f);
            Ornaments.Sparkles(chest.rectTransform, 4, new Color(1f, 0.92f, 0.6f, 1f));
            var explain = Label(panel, "Explain", S("bag.explain", "Everything you own. Tap a button to use it."), 20, TextAnchor.MiddleCenter, SurvivalVisuals.Cream, 0.04f, 0.725f, 0.96f, 0.765f);
            HookSkin.Fit(explain, 12, 20);

            var timers = _session?.Hook?.Timers;
            var running = SoonestRunning(timers);
            var items = BagContents.From(Campaign.Wallet);
            const float top = 0.715f, rowH = 0.122f;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var y1 = top - i * rowH;
                var row = OrnateCard(panel, "Item_" + item.ResourceId, 0.04f, y1 - rowH + 0.008f, 0.96f, y1, item.Count > 0);
                var icon = ArtSlot(row, "Icon", HookArtKeys.ForResource(item.ResourceId), SurvivalVisuals.Reward, 0.025f, 0.10f, 0.205f, 0.90f);
                if (item.Count == 0)
                {
                    icon.color = new Color(1f, 1f, 1f, 0.4f);
                }

                var line = BagLine(item, running);
                HookSkin.Fit(Label(row, "Name", S(item.ResourceId + ".name", item.ResourceId), 26, TextAnchor.MiddleLeft, SurvivalVisuals.Cream, 0.235f, 0.58f, 0.66f, 0.94f), 14, 26);
                Label(row, "Count", Fmt("bag.count", "x{0:N0}", item.Count), 24, TextAnchor.MiddleLeft, AllyCaption, 0.235f, 0.32f, 0.66f, 0.60f);
                HookSkin.Fit(Label(row, "Hint", line.Hint, 17, TextAnchor.MiddleLeft, new Color(0.80f, 0.86f, 1f, 1f), 0.235f, 0.05f, 0.70f, 0.32f), 10, 17);
                if (BagContents.CanUse(item, running != null))
                {
                    var go = item.Use != BagUse.SpeedUpTimer;
                    var use = SheetButton(row, "Use", go ? HookArtKeys.ButtonBlue : HookArtKeys.ButtonGreen, go ? SurvivalVisuals.PinBuild : SurvivalVisuals.MineFill, 0.69f, 0.22f, 0.97f, 0.78f, 24, out var useBtn);
                    use.text = line.Button;
                    HookSkin.Fit(use, 14, 24);
                    var captured = item;
                    Wire(useBtn, () => UseBagItem(captured));
                }
            }

            var close = SheetButton(panel, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.30f, 0.025f, 0.70f, 0.082f, 26, out var closeBtn);
            close.text = S("hook.close", "Close");
            Wire(closeBtn, CloseScreen);
        }

        private static Survival.Domain.Clock.GameTimer? SoonestRunning(Survival.Domain.Clock.TimerQueue? timers)
        {
            if (timers == null)
            {
                return null;
            }

            Survival.Domain.Clock.GameTimer? best = null;
            foreach (var t in timers.Active)
            {
                if (!t.IsDone(timers.Clock.Now) && (best == null || t.EndsAt < best.EndsAt))
                {
                    best = t;
                }
            }

            return best;
        }

        private void UseBagItem(BagItem item)
        {
            switch (item.Use)
            {
                case BagUse.SpeedUpTimer:
                    var timers = _session?.Hook?.Timers;
                    var t = SoonestRunning(timers);
                    if (t != null && Campaign != null)
                    {
                        timers!.ApplySpeedups(t.Id, Campaign.Wallet);
                    }

                    OpenBag();
                    break;
                case BagUse.OpenHeroes:
                    OpenHeroes();
                    break;
                case BagUse.OpenRoad:
                    CloseScreen();
                    FindFirstObjectByType<SurvivalRunnerEntry>()?.OnRunnerTapped();
                    break;
            }
        }
    }
}
