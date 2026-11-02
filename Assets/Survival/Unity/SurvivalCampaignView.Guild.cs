#nullable enable
using System;
using Survival.Domain.Alliance;
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

        /// <summary>The alliance server when reachable (falls back to on-device guildmates while offline or guildless).</summary>
        public static Func<IGuildBackend> GuildBackendFactory = () => AllianceNet != null ? new OnlineGuild(AllianceNet) : new SimulatedGuild();

        public static string GuildHelpPath => Path.Combine(Application.persistentDataPath, GuildHelp.FileName);

        partial void EngageAttach_Guild()
        {
            var timers = _session?.Hook?.Timers;
            if (timers == null)
            {
                return;
            }

            EnsureAllianceNet();
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
            if (_guild == null)
            {
                return;
            }

            RailShown(GuildRailName, Survival.Domain.View.RailActionable.Guild(_guild));
            if (Time.unscaledTime < _guildNextTick)
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

            // Hero art: guild crest on the left, the helping hand on the right, the explanation and balances between.
            var crestGlow = HookSkin.Glow(panel, "CrestGlow", new Color(0.35f, 0.55f, 1f, 0.7f));
            Box(crestGlow.rectTransform, 0.00f, 0.78f, 0.26f, 0.94f);
            GuildArt(panel, "Crest", HookArtKeys.GuildCrest, 0.03f, 0.795f, 0.23f, 0.925f);
            var handGlow = HookSkin.Glow(panel, "HandGlow", new Color(0.45f, 0.95f, 0.5f, 0.6f));
            Box(handGlow.rectTransform, 0.74f, 0.78f, 1.00f, 0.94f);
            GuildArt(panel, "Hand", HookArtKeys.HelpHand, 0.77f, 0.795f, 0.97f, 0.925f);
            var sub = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Sub", S("guild.explain", "Guildmates speed up your upgrades. Help theirs to earn gold."), 20, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.4f);
            sub.fontStyle = FontStyle.Bold;
            sub.horizontalOverflow = HorizontalWrapMode.Wrap;
            HookSkin.Fit(sub, 12, 20);
            Box(sub.rectTransform, 0.24f, 0.85f, 0.76f, 0.915f);
            BalanceBar.Inside(panel, Campaign.Wallet, GuildBalanceIds, 0.25f, 0.80f, 0.75f, 0.845f);

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
                var card = GuildCard(panel, "Mine_" + i, y0, y1, new Color(0.11f, 0.17f, 0.38f, 1f), Ornaments.Gold);
                GuildSlot(card.rectTransform, "Icon", HookArtKeys.ForResource(SurvIds.ResSpeedupM), 0.015f, 0.08f, 0.115f, 0.92f);
                var name = HookSkin.Outlined(SurvivalVisuals.Text(card.transform, "Name", GuildTimerName(t), 22, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.3f);
                name.fontStyle = FontStyle.Bold;
                name.raycastTarget = false;
                HookSkin.Fit(name, 12, 22);
                Box(name.rectTransform, 0.135f, 0.50f, 0.64f, 0.96f);
                var info = HookSkin.Outlined(SurvivalVisuals.Text(card.transform, "Info", "", 17, TextAnchor.MiddleLeft, SurvivalVisuals.Reward), 1.1f);
                info.raycastTarget = false;
                HookSkin.Fit(info, 10, 17);
                Box(info.rectTransform, 0.135f, 0.04f, 0.64f, 0.50f);
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
            var allLabel = SheetButton(panel, "HelpAll", HookArtKeys.ButtonGreen, FreeGreen, 0.64f, 0.535f, 0.96f, 0.595f, 22, out var all);
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
                var card = GuildCard(panel, "Req_" + i, y1 - rh + 0.005f, y1, mine ? new Color(0.10f, 0.13f, 0.26f, 1f) : new Color(0.11f, 0.17f, 0.38f, 1f), mine ? Ornaments.Bronze : Ornaments.Gold);
                GuildSlot(card.rectTransform, "Avatar", MatePortrait(r.Mate), 0.015f, 0.08f, 0.115f, 0.92f);
                var who = HookSkin.Outlined(SurvivalVisuals.Text(card.transform, "Mate", r.Mate, 20, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.2f);
                who.fontStyle = FontStyle.Bold;
                who.raycastTarget = false;
                HookSkin.Fit(who, 11, 20);
                Box(who.rectTransform, 0.135f, 0.52f, 0.40f, 0.96f);
                var task = HookSkin.Outlined(SurvivalVisuals.Text(card.transform, "Task", S("guild.task." + r.Task, r.Task), 16, TextAnchor.MiddleLeft, SurvivalVisuals.Reward), 1.1f);
                task.raycastTarget = false;
                HookSkin.Fit(task, 10, 16);
                Box(task.rectTransform, 0.135f, 0.06f, 0.40f, 0.50f);

                var helps = r.Helps + (mine ? 1 : 0);
                var bar = HookSkin.Bar(card.transform, "Bar", out var fill);
                bar.raycastTarget = false;
                Box(bar.rectTransform, 0.42f, 0.22f, 0.67f, 0.78f);
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

            var coin = HookSkin.ResIcon(panel, "LeftCoin", SurvIds.ResSoft, Ornaments.Gold, "G");
            Box(coin.rectTransform, 0.05f, 0.03f, 0.11f, 0.078f);
            var left = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Left", Fmt("guild.left", "+{0} gold per help \u00b7 {1} rewarded helps left today", GuildHelp.GoldPerHelp, g.RewardsLeftToday), 16, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.1f);
            left.raycastTarget = false;
            HookSkin.Fit(left, 10, 16);
            Box(left.rectTransform, 0.12f, 0.03f, 0.96f, 0.075f);

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

        private static readonly string[] MatePortraits = { "sir_aldric", "rowan", "oakenshield", "stormcrest", "emberfang", "ironhowl", "nightfang", "vespera", "ashwyrm", "blightroot", "bonequill" };

        /// <summary>A stable hero portrait per guildmate name, so the same mate always has the same face.</summary>
        private static string MatePortrait(string mate)
        {
            var h = 17;
            foreach (var ch in mate)
            {
                h = unchecked((h * 31) + ch);
            }

            return "theme_a.hero.portrait." + MatePortraits[(h & 0x7fffffff) % MatePortraits.Length];
        }

        private static void GuildArt(RectTransform parent, string name, string key, float x0, float y0, float x1, float y1)
        {
            var art = HookSkin.Art(parent, name, key, Ornaments.Gold);
            art.raycastTarget = false;
            Box(art.rectTransform, x0, y0, x1, y1);
        }

        /// <summary>Painted art (portrait or icon) in a gold-framed dark well at the left of a card.</summary>
        private static void GuildSlot(RectTransform card, string name, string key, float x0, float y0, float x1, float y1)
        {
            var slot = SurvivalVisuals.Image(card, name, new Color(0.06f, 0.09f, 0.22f, 1f));
            slot.raycastTarget = false;
            Box(slot.rectTransform, x0, y0, x1, y1);
            slot.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            Ornaments.Frame(slot.transform, Ornaments.Gold, 1.5f);
            GuildArt(slot.rectTransform, "Art", key, 0.06f, 0.06f, 0.94f, 0.94f);
        }

        /// <summary>Orb-style card: navy plate, bevel, frame.</summary>
        private static Image GuildCard(RectTransform panel, string name, float y0, float y1, Color fill, Color frame)
        {
            var card = HookSkin.Panel(panel, name, HookArtKeys.Plate, fill, 0.35f);
            card.raycastTarget = false;
            Box(card.rectTransform, 0.04f, y0, 0.96f, y1);
            Ornaments.Bevel(card.transform, 0.7f);
            Ornaments.Frame(card.transform, frame, 2f);
            return card;
        }
    }
}
