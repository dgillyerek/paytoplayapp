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

        private RectTransform OrnateCard(RectTransform parent, string name, float x0, float y0, float x1, float y1, bool lit)
        {
            var card = HookSkin.Panel(parent, name, HookArtKeys.Chip, new Color(0.16f, 0.11f, 0.07f, 0.95f), 0.4f);
            Box(card.rectTransform, x0, y0, x1, y1);
            Ornaments.Grain(card.transform, new Color(1f, 1f, 1f, 0.8f));
            Ornaments.Bevel(card.transform, lit ? 1f : 0.7f);
            Ornaments.Frame(card.transform, Ornaments.Gold);
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

        private void OpenAlliance(string tab)
        {
            _alliance ??= AllianceOverview.Simulated();
            _allianceTab = tab;
            var a = _alliance;
            var panel = OpenScreen("AllianceScreen", S("alliance.title", "ALLIANCE"));
            var head = OrnateCard(panel, "Guild", 0.04f, 0.895f, 0.96f, 0.945f, true);
            Label(head, "Name", a.GuildName, 28, TextAnchor.MiddleLeft, SurvivalVisuals.Cream, 0.04f, 0.5f, 0.96f, 0.96f);
            Label(head, "Sub", Fmt("alliance.summary", "Level {0}  ·  {1} members  ·  {2} online", a.Level, a.Members.Count, a.OnlineCount),
                20, TextAnchor.MiddleLeft, SurvivalVisuals.Reward, 0.04f, 0.06f, 0.96f, 0.5f);
            var members = tab == AllianceOverview.TabMembers;
            Tab(panel, "TabMembers", S("alliance.tab.members", "MEMBERS"), members, 0.04f, 0.49f, () => OpenAlliance(AllianceOverview.TabMembers));
            Tab(panel, "TabHelp", S("alliance.tab.help", "HELP"), !members, 0.51f, 0.96f, () => OpenAlliance(AllianceOverview.TabHelp));
            if (members)
            {
                const float top = 0.83f, rowH = 0.071f;
                for (var i = 0; i < a.Members.Count; i++)
                {
                    var m = a.Members[i];
                    var y1 = top - i * rowH;
                    var row = OrnateCard(panel, "Member_" + i, 0.04f, y1 - rowH + 0.006f, 0.96f, y1, m.Role == "leader");
                    var dot = SurvivalVisuals.Image(row, "Online", m.Online ? new Color(0.45f, 0.9f, 0.45f, 1f) : new Color(0.45f, 0.45f, 0.45f, 1f));
                    Box(dot.rectTransform, 0.025f, 0.38f, 0.045f, 0.62f);
                    Label(row, "Name", m.Name, 24, TextAnchor.MiddleLeft, SurvivalVisuals.Cream, 0.065f, 0.45f, 0.70f, 0.95f);
                    Label(row, "Role", S("alliance.role." + m.Role, m.Role.ToUpperInvariant()), 18, TextAnchor.MiddleLeft, SurvivalVisuals.Reward, 0.065f, 0.05f, 0.70f, 0.5f);
                    Label(row, "Power", Fmt("alliance.power", "⚔ {0:N0}", m.Power), 22, TextAnchor.MiddleRight, SurvivalVisuals.Cream, 0.66f, 0.1f, 0.97f, 0.9f);
                }
            }
            else
            {
                var help = OrnateCard(panel, "HelpSoon", 0.08f, 0.55f, 0.92f, 0.80f, false);
                Label(help, "Title", S("alliance.help.title", "Guild Help"), 30, TextAnchor.MiddleCenter, SurvivalVisuals.Cream, 0.05f, 0.62f, 0.95f, 0.92f);
                var body = Label(help, "Body", S("alliance.help.body", "Ask guildmates to speed up your upgrades, and help theirs for rewards. Coming soon."),
                    22, TextAnchor.MiddleCenter, SurvivalVisuals.Cream, 0.07f, 0.08f, 0.93f, 0.6f);
                body.fontStyle = FontStyle.Normal;
            }

            var close = SheetButton(panel, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.30f, 0.025f, 0.70f, 0.082f, 26, out var closeBtn);
            close.text = S("hook.close", "Close");
            Wire(closeBtn, CloseScreen);
        }

        private void OpenBag()
        {
            if (Campaign == null)
            {
                return;
            }

            var panel = OpenScreen("BagScreen", S("bag.title", "BAG"));
            var timers = _session?.Hook?.Timers;
            var items = BagContents.From(Campaign.Wallet);
            const float top = 0.93f, rowH = 0.115f;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var y1 = top - i * rowH;
                var row = OrnateCard(panel, "Item_" + item.ResourceId, 0.04f, y1 - rowH + 0.01f, 0.96f, y1, item.Count > 0);
                var icon = HookSkin.ResIcon(row, "Icon", item.ResourceId, SurvivalVisuals.Reward, "\u2726");
                icon.raycastTarget = false;
                Box(icon.rectTransform, 0.02f, 0.06f, 0.20f, 0.94f);
                if (item.Count == 0)
                {
                    icon.color = new Color(1f, 1f, 1f, 0.4f);
                }

                Label(row, "Name", S(item.ResourceId + ".name", item.ResourceId), 26, TextAnchor.MiddleLeft, SurvivalVisuals.Cream, 0.23f, 0.5f, 0.66f, 0.92f);
                Label(row, "Count", Fmt("bag.count", "x{0:N0}", item.Count), 24, TextAnchor.MiddleLeft, SurvivalVisuals.Reward, 0.23f, 0.08f, 0.66f, 0.5f);
                var running = SoonestRunning(timers) != null;
                if (BagContents.CanUse(item, running))
                {
                    var use = SheetButton(row, "Use", HookArtKeys.ButtonGreen, SurvivalVisuals.MineFill, 0.69f, 0.2f, 0.97f, 0.8f, 24, out var useBtn);
                    use.text = S("bag.use", "USE");
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
            }
        }
    }
}
