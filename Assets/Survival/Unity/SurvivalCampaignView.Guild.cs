#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Survival.Domain.Clock;
using Survival.Domain.Guild;
using Survival.Domain.Ids;
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Guild help: a rail button (slot 3) with a red count of guildmates waiting for help. The sheet lets the player ask the
    /// guild to speed up their own running upgrades (each help that arrives takes time off) and tap HELP / HELP ALL on
    /// guildmates' requests for a little gold. Guildmates are simulated on this device (<see cref="SimulatedGuild"/>) behind
    /// <see cref="IGuildBackend"/>, the seam for a real guild server. Saved in guild_help.json.
    /// </summary>
    public sealed partial class SurvivalCampaignView
    {
        public const string GuildSheetName = "GuildHelpSheet";
        public const string GuildRailName = "GuildHelp";

        private static readonly string[] GuildBalanceIds = { SurvIds.ResSoft, SurvIds.ResSpeedupM };

        private GuildHelp? _guild;
        private Image? _guildBadge;
        private Text? _guildBadgeCount;
        private float _guildNextTick;

        /// <summary>Guild help (null until the Play HUD and the village are up).</summary>
        public GuildHelp? Guild => _guild;

        /// <summary>Swap in a real guild server here; defaults to on-device guildmates.</summary>
        public static Func<IGuildBackend> GuildBackendFactory = () => new SimulatedGuild();

        public static string GuildHelpPath => Path.Combine(Application.persistentDataPath, GuildHelp.FileName);

        partial void EngageAttach_Guild()
        {
            var timers = _session?.Hook?.Timers;
            if (timers == null)
            {
                return;
            }

            _guild = new GuildHelp(GuildBackendFactory(), timers);
            try
            {
                if (File.Exists(GuildHelpPath) && !_guild.Load(File.ReadAllText(GuildHelpPath)))
                {
                    Debug.LogWarning("Guild help: save unreadable, starting fresh. " + GuildHelpPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            _guild.Changed += SaveGuild;
            RailButton(3, GuildRailName, "\u2694", S("guild.rail", "GUILD"), new Color(0.16f, 0.48f, 0.36f, 1f), OpenGuild, out var badge, out var count);
            _guildBadge = badge;
            _guildBadgeCount = count;
        }

        partial void EngageTick_Guild()
        {
            if (_guild == null || Time.unscaledTime < _guildNextTick)
            {
                return;
            }

            _guildNextTick = Time.unscaledTime + 1f;
            var landed = _guild.Tick();
            RailBadge(_guildBadge, _guildBadgeCount, _guild.Helpable);
            if (landed > 0 && _sheet != null && _sheet.name == GuildSheetName)
            {
                OpenGuild();
            }
        }

        private void SaveGuild()
        {
            if (_guild == null)
            {
                return;
            }

            try
            {
                File.WriteAllText(GuildHelpPath, _guild.Save());
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private string GuildTimerName(GameTimer t)
        {
            var type = _session?.Hook?.Base.Find(t.TargetId)?.Def.BuildType;
            return type != null ? S("build.type." + type, type) : S("hook.plot." + t.TargetId, t.TargetId);
        }

        public void OpenGuild()
        {
            if (_guild == null || _overlay == null || Campaign == null)
            {
                return;
            }

            var g = _guild;
            var panel = OpenSheet(GuildSheetName, S("guild.title", "Guild Hall"), 0.86f);
            var sub = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Sub", S("guild.sub", "Help your guildmates, and they will help you"), 22, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.2f);
            HookSkin.Fit(sub, 12, 22);
            Box(sub.rectTransform, 0.05f, 0.85f, 0.95f, 0.89f);
            BalanceBar.Inside(panel, Campaign.Wallet, GuildBalanceIds, 0.15f, 0.80f, 0.85f, 0.845f);

            // Your builds: ask the guild for help.
            var mineHead = Ornaments.Ribbon(panel, S("guild.mine", "YOUR BUILDS"), RoyalBlue, out var mineRib);
            Box(mineRib.rectTransform, 0.20f, 0.74f, 0.80f, 0.79f);
            mineHead.fontSize = 22;
            var timers = new List<GameTimer>(g.Timers.Active);
            var timerTexts = new List<(Text Label, GameTimer T)>();
            if (timers.Count == 0)
            {
                var none = HookSkin.Outlined(SurvivalVisuals.Text(panel, "NoBuilds", S("guild.none", "Start an upgrade, then ask your guild to speed it up."), 18, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.1f);
                HookSkin.Fit(none, 11, 18);
                Box(none.rectTransform, 0.06f, 0.64f, 0.94f, 0.73f);
            }

            const float myTop = 0.735f, myH = 0.065f;
            for (var i = 0; i < timers.Count && i < 2; i++)
            {
                var t = timers[i];
                var y1 = myTop - (i * myH);
                var y0 = y1 - myH + 0.006f;
                var card = GuildCard(panel, "Mine_" + i, y0, y1, new Color(0.15f, 0.20f, 0.32f, 1f), Ornaments.Bronze);
                var name = HookSkin.Outlined(SurvivalVisuals.Text(card.transform, "Name", GuildTimerName(t), 22, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.3f);
                name.fontStyle = FontStyle.Bold;
                name.raycastTarget = false;
                HookSkin.Fit(name, 12, 22);
                Box(name.rectTransform, 0.04f, 0.50f, 0.60f, 0.96f);
                var info = HookSkin.Outlined(SurvivalVisuals.Text(card.transform, "Info", "", 17, TextAnchor.MiddleLeft, SurvivalVisuals.Reward), 1.1f);
                info.raycastTarget = false;
                HookSkin.Fit(info, 10, 17);
                Box(info.rectTransform, 0.04f, 0.04f, 0.64f, 0.50f);
                timerTexts.Add((info, t));

                var label = SheetButton(card.rectTransform, "Ask", g.CanAsk(t.Id) ? HookArtKeys.ButtonGreen : HookArtKeys.ButtonBlue, FreeGreen, 0.66f, 0.10f, 0.96f, 0.90f, 22, out var ask);
                if (g.CanAsk(t.Id))
                {
                    label.text = S("guild.ask", "ASK HELP");
                    StartCoroutine(Breathe(ask.GetComponent<RectTransform>()));
                    var id = t.Id;
                    Wire(ask, () =>
                    {
                        g.Ask(id);
                        OpenGuild();
                    });
                }
                else
                {
                    label.text = Fmt("guild.helps", "{0}/{1} helps", g.Received(t.Id), GuildHelp.MaxIncoming);
                    ask.interactable = false;
                }

                FitButtonLabel(label, 22);
            }

            // Guildmates' requests.
            var reqHead = Ornaments.Ribbon(panel, S("guild.theirs", "GUILDMATES NEED HELP"), new Color(0.16f, 0.48f, 0.36f, 1f), out var reqRib);
            Box(reqRib.rectTransform, 0.04f, 0.54f, 0.62f, 0.59f);
            reqHead.fontSize = 20;
            var allLabel = SheetButton(panel, "HelpAll", HookArtKeys.ButtonGold, Ornaments.Gold, 0.64f, 0.535f, 0.96f, 0.595f, 22, out var all);
            allLabel.text = S("guild.help_all", "HELP ALL");
            FitButtonLabel(allLabel, 22);
            all.interactable = g.Helpable > 0;
            if (g.Helpable > 0)
            {
                Ornaments.Shine(all.GetComponent<RectTransform>());
                Wire(all, () =>
                {
                    var (n, gold) = g.HelpAll(Campaign!.Wallet);
                    OpenGuild();
                    if (gold > 0)
                    {
                        Celebrate(Fmt("guild.thanks", "You helped {0} guildmates!", n), new Dictionary<string, int> { [SurvIds.ResSoft] = gold });
                    }
                });
            }

            var reqs = g.Requests();
            const float rTop = 0.525f, rBottom = 0.085f;
            var rh = (rTop - rBottom) / Math.Max(1, reqs.Count);
            for (var i = 0; i < reqs.Count; i++)
            {
                var r = reqs[i];
                var mine = g.HelpedByMe(r.Id);
                var y1 = rTop - (i * rh);
                var card = GuildCard(panel, "Req_" + i, y1 - rh + 0.005f, y1, mine ? new Color(0.14f, 0.17f, 0.22f, 1f) : new Color(0.16f, 0.30f, 0.24f, 1f), mine ? Ornaments.Bronze : new Color(0.55f, 0.95f, 0.45f, 1f));
                var who = HookSkin.Outlined(SurvivalVisuals.Text(card.transform, "Mate", r.Mate, 20, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.2f);
                who.fontStyle = FontStyle.Bold;
                who.raycastTarget = false;
                HookSkin.Fit(who, 11, 20);
                Box(who.rectTransform, 0.04f, 0.52f, 0.36f, 0.96f);
                var task = HookSkin.Outlined(SurvivalVisuals.Text(card.transform, "Task", S("guild.task." + r.Task, r.Task), 16, TextAnchor.MiddleLeft, SurvivalVisuals.Reward), 1.1f);
                task.raycastTarget = false;
                HookSkin.Fit(task, 10, 16);
                Box(task.rectTransform, 0.04f, 0.06f, 0.36f, 0.50f);

                var helps = r.Helps + (mine ? 1 : 0);
                var bar = HookSkin.Bar(card.transform, "Bar", out var fill);
                bar.raycastTarget = false;
                Box(bar.rectTransform, 0.38f, 0.25f, 0.66f, 0.75f);
                HookSkin.SetBar(fill, Mathf.Clamp01((float)helps / r.MaxHelps), HookArtKeys.BarFillGreen, FreeGreen);
                var n = HookSkin.Outlined(SurvivalVisuals.Text(bar.transform, "N", Math.Min(helps, r.MaxHelps) + " / " + r.MaxHelps, 15, TextAnchor.MiddleCenter, Color.white), 1.1f);
                n.raycastTarget = false;
                SurvivalVisuals.Stretch(n.rectTransform);

                var label = SheetButton(card.rectTransform, "Help", mine ? HookArtKeys.ButtonBlue : HookArtKeys.ButtonGreen, FreeGreen, 0.69f, 0.10f, 0.96f, 0.90f, 20, out var help);
                label.text = mine ? S("guild.helped", "\u2714 HELPED") : S("guild.help", "HELP");
                FitButtonLabel(label, 20);
                help.interactable = !mine;
                if (!mine)
                {
                    var id = r.Id;
                    Wire(help, () =>
                    {
                        g.Help(id, Campaign!.Wallet);
                        OpenGuild();
                    });
                }
            }

            var left = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Left", Fmt("guild.left", "+{0} gold per help \u00b7 {1} rewarded helps left today", GuildHelp.GoldPerHelp, g.RewardsLeftToday), 16, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.1f);
            left.raycastTarget = false;
            HookSkin.Fit(left, 10, 16);
            Box(left.rectTransform, 0.04f, 0.03f, 0.96f, 0.075f);

            void Refresh()
            {
                var now = g.Timers.Clock.Now;
                foreach (var (label, t) in timerTexts)
                {
                    label.text = Fmt("guild.timer", "{0} left \u00b7 each help \u2212{1}", RailTime(t.Remaining(now)), RailTime(GuildHelp.HelpSeconds(t)));
                }
            }

            Refresh();
            _sheetRefresh = Refresh;
        }

        private static Image GuildCard(RectTransform panel, string name, float y0, float y1, Color fill, Color frame)
        {
            var card = SurvivalVisuals.Image(panel, name, fill);
            card.raycastTarget = false;
            Box(card.rectTransform, 0.04f, y0, 0.96f, y1);
            Ornaments.Grain(card.transform, new Color(1f, 1f, 1f, 0.08f));
            Ornaments.Bevel(card.transform, 0.5f);
            Ornaments.Frame(card.transform, frame, 2f);
            return card;
        }
    }
}
