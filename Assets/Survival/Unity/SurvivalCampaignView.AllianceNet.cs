#nullable enable
using System;
using System.Collections;
using System.Text;
using Survival.Domain.Alliance;
using Survival.Domain.Ids;
using Survival.Domain.Kids;
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Survival.Unity.Kids;

namespace Survival.Unity
{
    /// <summary>Sends alliance API calls with UnityWebRequest on the view's coroutine runner.</summary>
    public sealed class UnityAllianceTransport : IAllianceTransport
    {
        private readonly MonoBehaviour _runner;
        private readonly string _base;

        public UnityAllianceTransport(MonoBehaviour runner, string baseUrl)
        {
            _runner = runner;
            _base = baseUrl.TrimEnd('/');
        }

        public void Send(string method, string path, string? jsonBody, string? token, Action<int, string> done) =>
            _runner.StartCoroutine(Run(method, path, jsonBody, token, done));

        private IEnumerator Run(string method, string path, string? body, string? token, Action<int, string> done)
        {
            using var req = new UnityWebRequest(_base + path, method) { downloadHandler = new DownloadHandlerBuffer(), timeout = 10 };
            if (body != null)
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                req.SetRequestHeader("Content-Type", "application/json");
            }

            if (token != null)
            {
                req.SetRequestHeader("Authorization", "Bearer " + token);
            }

            yield return req.SendWebRequest();
            var status = req.result == UnityWebRequest.Result.ConnectionError ? 0 : (int)req.responseCode;
            done(status, req.downloadHandler?.text ?? "");
        }
    }

    public sealed partial class SurvivalCampaignView
    {
        public const string TabChat = "chat";
        public const string TabGifts = "gifts";

        /// <summary>Public URL of the alliance Neon Function. Not a secret; override with PlayerPrefs "alliance.api" ("" = offline).</summary>
        public const string DefaultAllianceApi = "https://br-summer-firefly-b8or37b8-alliance.compute.c-14.us-east-1.aws.neon.tech";

        internal const string DeviceIdPref = "alliance.device";

        /// <summary>Shared alliance client (null until the view starts, or when the API is switched off).</summary>
        public static AllianceService? AllianceNet { get; private set; }

        /// <summary>Report / block and the server chat mode on the same sign-in (Kids, COPPA / Families).</summary>
        public static AllianceSafetyClient? AllianceSafety { get; private set; }

        /// <summary>Account deletion: drop the signed-in session so nothing more is sent until the app restarts.</summary>
        public static void ForgetAllianceNet()
        {
            AllianceNet = null;
            AllianceSafety = null;
        }

        private string _allianceSearch = "";
        private Text? _allyChatLog;
        private InputField? _allyChatInput;
        private Coroutine? _alliancePoll;

        private void EnsureAllianceNet()
        {
            if (AllianceNet != null)
            {
                return;
            }

            var api = PlayerPrefs.GetString("alliance.api", DefaultAllianceApi);
            if (string.IsNullOrWhiteSpace(api))
            {
                return;
            }

            // Sign-in sends a persistent device id: for under-13 / unknown age it waits for verifiable parental consent
            // (KidSafety.Consent; the default provider never grants it). Until then the guild runs offline (SimulatedGuild).
            if (!KidSafety.AllowsOnlineSignIn)
            {
                return;
            }

            var device = PlayerPrefs.GetString(DeviceIdPref, "");
            if (device.Length < 16)
            {
                device = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(DeviceIdPref, device);
                PlayerPrefs.Save();
            }

            AllianceNet = new AllianceService(new UnityAllianceTransport(this, api));
            AllianceSafety = new AllianceSafetyClient(AllianceNet);
            AllianceNet.Changed += () => _session?.Alliance?.Apply(AllianceNet);
            // Restricted (child / unknown age) players never send a typed name: a generated one, e.g. "Swift Ranger 417".
            var policy = KidSafety.Policy;
            var safeName = ChatSafety.SafeName(device);
            var name = ChatSafety.CanTypeName(policy) ? S("alliance.default_name", "Survivor") : safeName;
            AllianceNet.SignIn(device, name, ok =>
            {
                if (ok)
                {
                    // Age band first (the server's restricted flag reads it), then the chat mode / safe name.
                    var band = Survival.Domain.Iap.AgeBands.Id(KidSafety.AgeBand);
                    AllianceNet.SetAgeBand(band, _ => AllianceSafety?.ApplyPolicy(KidSafety.Policy, safeName));
                    AllianceNet.RefreshHelp();
                    AllianceNet.RefreshGifts();
                }
            });
            _alliancePoll ??= StartCoroutine(AlliancePoll());
        }

        /// <summary>Help requests and gifts every 20 s (feeds the Guild Help rail icon); chat every 4 s while the chat tab is open.</summary>
        private IEnumerator AlliancePoll()
        {
            var slow = 0f;
            while (true)
            {
                yield return new WaitForSecondsRealtime(4f);
                var net = AllianceNet;
                if (net is not { InAlliance: true })
                {
                    continue;
                }

                var chatOpen = _allianceTab == TabChat && GameObject.Find("AllianceScreen") != null;
                if (chatOpen)
                {
                    var before = net.Chat.Count;
                    net.PollChat(ok => { if (ok && net.Chat.Count != before) RefreshAllianceChat(); });
                }

                slow += 4f;
                if (slow >= 20f)
                {
                    slow = 0f;
                    net.RefreshHelp();
                    net.RefreshGifts();
                }
            }
        }

        private void AllianceCloseButton(RectTransform panel)
        {
            var close = SheetButton(panel, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.30f, 0.025f, 0.70f, 0.082f, 26, out var closeBtn);
            close.text = S("hook.close", "Close");
            Wire(closeBtn, CloseScreen);
        }

        private void AllianceError(RectTransform panel)
        {
            var err = AllianceNet?.LastError;
            if (!string.IsNullOrEmpty(err))
            {
                Label(panel, "Error", S("alliance.error." + err, err!.Replace('_', ' ')), 18, TextAnchor.MiddleCenter, new Color(1f, 0.72f, 0.62f, 1f), 0.04f, 0.085f, 0.96f, 0.115f);
            }
        }

        private InputField AllianceInput(RectTransform parent, string name, string hint, int limit, float x0, float y0, float x1, float y1)
        {
            var card = OrnateCard(parent, name, x0, y0, x1, y1, false);
            var text = Label(card, "Text", "", 22, TextAnchor.MiddleLeft, SurvivalVisuals.Cream, 0.04f, 0f, 0.96f, 1f);
            text.raycastTarget = true;
            var ph = Label(card, "Hint", hint, 20, TextAnchor.MiddleLeft, new Color(1f, 0.957f, 0.839f, 0.55f), 0.04f, 0f, 0.96f, 1f);
            var field = card.gameObject.AddComponent<InputField>();
            field.textComponent = text;
            field.placeholder = ph;
            field.characterLimit = limit;
            return field;
        }

        /// <summary>Not in an alliance yet: search and join one, or found your own.</summary>
        private void OpenAllianceFind()
        {
            var net = AllianceNet!;
            var panel = OpenScreen("AllianceScreen", S("alliance.find.title", "FIND AN ALLIANCE"));
            _allyPanel = null;
            _allyContent = null;
            var crest = HookSkin.Art(panel, "Crest", HookArtKeys.GuildCrest, Ornaments.Gold);
            crest.raycastTarget = false;
            HookSkin.Box(crest.rectTransform, 0.36f, 0.80f, 0.64f, 0.94f);
            HookSkin.Fit(Label(panel, "Explain", S("alliance.find.explain", "Join an alliance to chat, help each other's builds and earn gifts."), 20, TextAnchor.MiddleCenter, SurvivalVisuals.Cream, 0.04f, 0.755f, 0.96f, 0.795f), 12, 20);

            var search = AllianceInput(panel, "Search", S("alliance.find.search_hint", "Search by name or tag"), 30, 0.04f, 0.69f, 0.70f, 0.745f);
            search.text = _allianceSearch;
            var go = SheetButton(panel, "SearchGo", HookArtKeys.ButtonBlue, SurvivalVisuals.PinBuild, 0.72f, 0.69f, 0.96f, 0.745f, 22, out var goBtn);
            go.text = S("alliance.find.search", "SEARCH");
            Wire(goBtn, () =>
            {
                _allianceSearch = search.text;
                net.Search(_allianceSearch, _ => OpenAllianceFind());
            });

            const float top = 0.675f, rowH = 0.075f;
            for (var i = 0; i < Math.Min(6, net.Listings.Count); i++)
            {
                var l = net.Listings[i];
                var y1 = top - i * rowH;
                var row = OrnateCard(panel, "Listing_" + i, 0.04f, y1 - rowH + 0.006f, 0.96f, y1, l.Open);
                var banner = HookSkin.Art(row, "Banner", HookArtKeys.WarBanner, Ornaments.Gold);
                banner.raycastTarget = false;
                HookSkin.Box(banner.rectTransform, 0.02f, 0.1f, 0.12f, 0.9f);
                HookSkin.Fit(Label(row, "Name", "[" + l.Tag + "] " + l.Name, 23, TextAnchor.MiddleLeft, SurvivalVisuals.Cream, 0.14f, 0.5f, 0.70f, 0.96f), 13, 23);
                Label(row, "Info", Fmt("alliance.find.row", "{0}/{1} members  \u00b7  power {2:N0}", l.Members, l.MaxMembers, l.Power), 17, TextAnchor.MiddleLeft, AllyCaption, 0.14f, 0.05f, 0.70f, 0.5f);
                var join = SheetButton(row, "Join", l.Open ? HookArtKeys.ButtonGreen : HookArtKeys.ButtonBlue, SurvivalVisuals.MineFill, 0.72f, 0.18f, 0.97f, 0.82f, 22, out var joinBtn);
                join.text = l.Open ? S("alliance.find.join", "JOIN") : S("alliance.find.closed", "INVITE ONLY");
                var id = l.Id;
                if (l.Open)
                {
                    Wire(joinBtn, () => net.Join(id, _ => OpenAlliance(AllianceOverview.TabMembers)));
                }
            }

            if (net.Listings.Count == 0)
            {
                Label(panel, "Empty", S("alliance.find.empty", "Tap SEARCH to see alliances."), 20, TextAnchor.MiddleCenter, AllyCaption, 0.04f, 0.50f, 0.96f, 0.56f);
            }

            // Found your own: name + 2-4 letter tag. Not for child mode (a typed alliance name is free text other players see).
            if (KidSafety.Policy.CanCreateAlliance)
            {
                DrawAllianceCreate(panel);
            }

            AllianceError(panel);
            AllianceCloseButton(panel);
            if (net.Listings.Count == 0 && net.LastError == null)
            {
                net.Search("", ok => { if (ok && net.Listings.Count > 0 && GameObject.Find("AllianceScreen") != null) OpenAllianceFind(); });
            }
        }

        private void DrawAllianceCreate(RectTransform panel)
        {
            var net = AllianceNet!;
            var create = OrnateCard(panel, "CreateCard", 0.04f, 0.12f, 0.96f, 0.215f, true);
            Label(create, "Title", S("alliance.create.title", "Found your own"), 20, TextAnchor.MiddleLeft, SurvivalVisuals.Cream, 0.03f, 0.62f, 0.96f, 0.98f);
            var name = AllianceInput(create, "Name", S("alliance.create.name", "Alliance name"), 24, 0.03f, 0.08f, 0.50f, 0.6f);
            var tag = AllianceInput(create, "Tag", S("alliance.create.tag", "TAG"), 4, 0.52f, 0.08f, 0.68f, 0.6f);
            var found = SheetButton(create, "Found", HookArtKeys.ButtonGreen, SurvivalVisuals.MineFill, 0.70f, 0.08f, 0.97f, 0.6f, 20, out var foundBtn);
            found.text = S("alliance.create.go", "FOUND");
            Wire(foundBtn, () => net.Create(name.text.Trim(), tag.text.Trim().ToUpperInvariant(), true, ok =>
            {
                if (ok)
                {
                    OpenAlliance(AllianceOverview.TabMembers);
                }
                else
                {
                    OpenAllianceFind();
                }
            }));
        }

        private void DrawAllianceOnlineTab(RectTransform panel, AllianceService net, string tab)
        {
            switch (tab)
            {
                case TabChat:
                    DrawAllianceChat(panel, net);
                    break;
                case TabGifts:
                    DrawAllianceGifts(panel, net);
                    break;
                default:
                    DrawAllianceHelp(panel, net);
                    break;
            }

            AllianceError(panel);
        }

        private string? PresetText(string id)
        {
            foreach (var p in KidSafety.Config.ChatPresets)
            {
                if (p.Id == id)
                {
                    return S("kids.chat.preset." + id, p.Text);
                }
            }

            return null;
        }

        private static string AllianceChatText(SurvivalCampaignView v, AllianceService net)
        {
            var lines = new StringBuilder();
            var policy = KidSafety.Policy;
            var blocked = AllianceSafety?.Blocked ?? new System.Collections.Generic.HashSet<long>();
            for (var i = Math.Max(0, net.Chat.Count - 14); i < net.Chat.Count; i++)
            {
                var m = net.Chat[i];
                var body = ChatSafety.Visible(policy, m.PlayerId, m.Body, blocked, v.PresetText);
                if (body == null)
                {
                    continue;
                }

                lines.Append("<b>").Append(m.Name).Append(":</b> ").Append(body.Replace("<", "\u2039")).Append('\n');
            }

            return lines.Length == 0 ? v.S("alliance.chat.empty", "Say hello to your alliance!") : lines.ToString();
        }

        /// <summary>New chat lines: update the log text in place so the input keeps its focus and half-typed text.</summary>
        private void RefreshAllianceChat()
        {
            var net = AllianceNet;
            if (net == null || !AllianceShellAlive() || _allianceTab != TabChat)
            {
                return;
            }

            if (_allyChatLog == null)
            {
                OpenAlliance(TabChat);
                return;
            }

            var text = AllianceChatText(this, net);
            if (_allyChatLog.text != text)
            {
                _allyChatLog.text = text;
            }
        }

        private void DrawAllianceChat(RectTransform panel, AllianceService net)
        {
            var policy = KidSafety.Policy;
            if (!ChatSafety.CanRead(policy))
            {
                // Child mode, chat off (child_safety.json child.chat): nothing is shown or sent. Neutral copy, no upsell.
                var off = OrnateCard(panel, "ChatOff", 0.04f, 0.40f, 0.96f, 0.55f, false);
                HookSkin.Fit(Label(off, "Text", S("kids.chat.off", "Chat is turned off. You can still help your alliance and send gifts."), 21, TextAnchor.MiddleCenter, SurvivalVisuals.Cream, 0.04f, 0.05f, 0.96f, 0.95f), 12, 21);
                return;
            }

            if (!ChatSafety.CanSendFreeText(policy))
            {
                DrawAlliancePresetChat(panel, net);
                return;
            }

            var card = OrnateCard(panel, "ChatCard", 0.04f, 0.19f, 0.96f, 0.67f, false);
            var log = Label(card, "Log", AllianceChatText(this, net), 19, TextAnchor.LowerLeft, SurvivalVisuals.Cream, 0.03f, 0.02f, 0.97f, 0.98f);
            log.supportRichText = true;
            log.horizontalOverflow = HorizontalWrapMode.Wrap;
            log.verticalOverflow = VerticalWrapMode.Truncate;
            _allyChatLog = log;

            var input = AllianceInput(panel, "ChatInput", S("alliance.chat.hint", "Write a message"), 280, 0.04f, 0.12f, 0.56f, 0.18f);
            _allyChatInput = input;
            ChatSafetyButton(panel, 0.78f, 0.96f);
            var send = SheetButton(panel, "Send", HookArtKeys.ButtonGreen, SurvivalVisuals.MineFill, 0.58f, 0.12f, 0.76f, 0.18f, 22, out var sendBtn);
            send.text = S("alliance.chat.send", "SEND");
            Wire(sendBtn, () =>
            {
                var body = input.text.Trim();
                if (body.Length > 0)
                {
                    net.SendChat(body, ok =>
                    {
                        if (ok && input != null && input.text.Trim() == body)
                        {
                            input.text = "";
                        }

                        RefreshAllianceChat();
                    });
                }
            });
            if (net.Chat.Count == 0)
            {
                net.PollChat(ok => { if (ok && net.Chat.Count > 0) RefreshAllianceChat(); });
            }
        }

        /// <summary>Presets-only chat (child mode): fixed phrases as buttons, no text box. The server only accepts preset ids.</summary>
        private void DrawAlliancePresetChat(RectTransform panel, AllianceService net)
        {
            var card = OrnateCard(panel, "ChatCard", 0.04f, 0.28f, 0.96f, 0.67f, false);
            var log = Label(card, "Log", AllianceChatText(this, net), 19, TextAnchor.LowerLeft, SurvivalVisuals.Cream, 0.03f, 0.02f, 0.97f, 0.98f);
            log.supportRichText = true;
            log.horizontalOverflow = HorizontalWrapMode.Wrap;
            log.verticalOverflow = VerticalWrapMode.Truncate;
            _allyChatLog = log;
            _allyChatInput = null;

            var presets = KidSafety.Config.ChatPresets;
            for (var i = 0; i < Math.Min(8, presets.Count); i++)
            {
                var p = presets[i];
                var x0 = 0.04f + i % 4 * 0.23f;
                var y1 = i < 4 ? 0.272f : 0.232f;
                var b = SheetButton(panel, "Preset_" + p.Id, HookArtKeys.ButtonBlue, SurvivalVisuals.PinBuild, x0, y1 - 0.036f, x0 + 0.22f, y1, 16, out var btn);
                b.text = S("kids.chat.preset." + p.Id, p.Text);
                HookSkin.Fit(b, 10, 16);
                var id = p.Id;
                Wire(btn, () => AllianceSafety?.SendPreset(id, _ => RefreshAllianceChat()));
            }

            ChatSafetyButton(panel, 0.30f, 0.70f);
            if (net.Chat.Count == 0)
            {
                net.PollChat(ok => { if (ok && net.Chat.Count > 0) RefreshAllianceChat(); });
            }
        }

        private void ChatSafetyButton(RectTransform panel, float x0, float x1)
        {
            var t = SheetButton(panel, "ChatSafety", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, x0, 0.12f, x1, 0.18f, 18, out var btn);
            t.text = S("kids.chat.report_block", "Report / Block");
            HookSkin.Fit(t, 10, 18);
            Wire(btn, OpenChatSafety);
        }

        /// <summary>Report or block anyone from the last messages. Blocked players' messages are hidden (server and client).</summary>
        private void OpenChatSafety()
        {
            var net = AllianceNet;
            var safety = AllianceSafety;
            if (net == null || safety == null)
            {
                return;
            }

            var panel = OpenScreen("AllianceScreen", S("kids.chat.safety.title", "REPORT OR BLOCK"));
            Label(panel, "Explain", S("kids.chat.safety.explain", "If someone is mean or asks for personal info, report or block them. A grown-up can help too."), 19, TextAnchor.MiddleCenter, SurvivalVisuals.Cream, 0.04f, 0.80f, 0.96f, 0.86f);
            var shown = 0;
            const float top = 0.78f, rowH = 0.085f;
            for (var i = net.Chat.Count - 1; i >= 0 && shown < 7; i--)
            {
                var m = net.Chat[i];
                if (m.PlayerId == net.PlayerId)
                {
                    continue;
                }

                var y1 = top - shown++ * rowH;
                var row = OrnateCard(panel, "Msg_" + m.Id, 0.04f, y1 - rowH + 0.006f, 0.96f, y1, false);
                var isBlocked = safety.Blocked.Contains(m.PlayerId);
                var text = isBlocked ? S("kids.chat.safety.blocked", "(blocked)") : ChatSafety.Visible(KidSafety.Policy, m.PlayerId, m.Body, safety.Blocked, PresetText) ?? "\u2026";
                HookSkin.Fit(Label(row, "Line", m.Name + ": " + text.Replace("<", "\u2039"), 18, TextAnchor.MiddleLeft, SurvivalVisuals.Cream, 0.03f, 0.05f, 0.56f, 0.95f), 10, 18);
                var rep = SheetButton(row, "Report", HookArtKeys.ButtonBlue, SurvivalVisuals.PinBuild, 0.58f, 0.15f, 0.77f, 0.85f, 16, out var repBtn);
                rep.text = S("kids.chat.report", "REPORT");
                var blk = SheetButton(row, "Block", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.78f, 0.15f, 0.97f, 0.85f, 16, out var blkBtn);
                blk.text = isBlocked ? S("kids.chat.unblock", "UNBLOCK") : S("kids.chat.block", "BLOCK");
                var msgId = m.Id;
                var who = m.PlayerId;
                Wire(repBtn, () => safety.Report(msgId, "other", ok => { rep.text = ok ? S("kids.chat.reported", "SENT") : rep.text; }));
                Wire(blkBtn, () =>
                {
                    if (safety.Blocked.Contains(who))
                    {
                        safety.Unblock(who, _ => OpenChatSafety());
                    }
                    else
                    {
                        safety.Block(who, _ => OpenChatSafety());
                    }
                });
            }

            if (shown == 0)
            {
                Label(panel, "Empty", S("kids.chat.safety.empty", "No messages from others yet."), 20, TextAnchor.MiddleCenter, AllyCaption, 0.04f, 0.50f, 0.96f, 0.56f);
            }

            var back = SheetButton(panel, "Back", HookArtKeys.ButtonBlue, SurvivalVisuals.PinBuild, 0.30f, 0.10f, 0.70f, 0.16f, 22, out var backBtn);
            back.text = S("kids.chat.safety.back", "Back to chat");
            Wire(backBtn, () => OpenAlliance(TabChat));
            AllianceCloseButton(panel);
        }

        private void DrawAllianceHelp(RectTransform panel, AllianceService net)
        {
            var gold = Campaign?.Wallet.Get(SurvIds.ResSoft) ?? 0;
            Label(panel, "Balance", Fmt("alliance.help.balance", "Gold {0:N0}  \u00b7  +100 per help, 30 rewarded helps a day", gold), 18, TextAnchor.MiddleCenter, AllyCaption, 0.04f, 0.63f, 0.96f, 0.67f);
            var open = 0;
            const float top = 0.62f, rowH = 0.07f;
            foreach (var r in net.Requests)
            {
                if (r.PlayerId == net.PlayerId || open >= 6)
                {
                    continue;
                }

                var y1 = top - open++ * rowH;
                var row = OrnateCard(panel, "Req_" + r.Id, 0.04f, y1 - rowH + 0.006f, 0.96f, y1, !r.HelpedByMe);
                var hand = HookSkin.Art(row, "Hand", HookArtKeys.HelpHand, Ornaments.Gold);
                hand.raycastTarget = false;
                HookSkin.Box(hand.rectTransform, 0.02f, 0.1f, 0.12f, 0.9f);
                var task = OnlineGuild.TaskOf(r.TaskKey);
                HookSkin.Fit(Label(row, "Who", Fmt("alliance.help.row", "{0}: {1}", r.Name, S("build.type." + task, task)), 21, TextAnchor.MiddleLeft, SurvivalVisuals.Cream, 0.14f, 0.5f, 0.72f, 0.96f), 12, 21);
                Label(row, "Count", Fmt("alliance.help.count", "{0}/{1} helps", r.Helps, r.MaxHelps), 17, TextAnchor.MiddleLeft, AllyCaption, 0.14f, 0.05f, 0.72f, 0.5f);
                if (!r.HelpedByMe)
                {
                    var help = SheetButton(row, "Help", HookArtKeys.ButtonGreen, SurvivalVisuals.MineFill, 0.74f, 0.15f, 0.97f, 0.85f, 22, out var helpBtn);
                    help.text = S("alliance.help.go", "HELP");
                    var id = r.Id;
                    Wire(helpBtn, () => net.GiveHelp(new[] { id }, (_, g) =>
                    {
                        Campaign?.Wallet.Add(SurvIds.ResSoft, g);
                        OpenAlliance(AllianceOverview.TabHelp);
                    }));
                }
            }

            if (open == 0)
            {
                Label(panel, "Empty", S("alliance.help.empty", "No one needs help right now. Ask for help from a running upgrade."), 20, TextAnchor.MiddleCenter, AllyCaption, 0.06f, 0.40f, 0.94f, 0.50f).horizontalOverflow = HorizontalWrapMode.Wrap;
            }

            if (open > 1)
            {
                var all = SheetButton(panel, "HelpAll", HookArtKeys.ButtonGreen, SurvivalVisuals.MineFill, 0.25f, 0.12f, 0.75f, 0.18f, 24, out var allBtn);
                all.text = S("alliance.help.all", "HELP ALL");
                Wire(allBtn, () =>
                {
                    var ids = new System.Collections.Generic.List<long>();
                    foreach (var r in net.Requests)
                    {
                        if (r.PlayerId != net.PlayerId && !r.HelpedByMe)
                        {
                            ids.Add(r.Id);
                        }
                    }

                    net.GiveHelp(ids, (_, g) =>
                    {
                        Campaign?.Wallet.Add(SurvIds.ResSoft, g);
                        OpenAlliance(AllianceOverview.TabHelp);
                    });
                });
            }
        }

        private void DrawAllianceGifts(RectTransform panel, AllianceService net)
        {
            var gems = Campaign?.Wallet.Get(SurvIds.ResHard) ?? 0;
            Label(panel, "Balance", Fmt("alliance.gifts.balance", "Gems {0:N0}", gems), 20, TextAnchor.MiddleCenter, AllyCaption, 0.04f, 0.63f, 0.96f, 0.67f);
            var card = OrnateCard(panel, "GiftCard", 0.08f, 0.30f, 0.92f, 0.61f, net.Gifts.Count > 0);
            var chest = HookSkin.Art(card, "Chest", HookArtKeys.ChestOpen, Ornaments.Gold);
            chest.raycastTarget = false;
            HookSkin.Box(chest.rectTransform, 0.30f, 0.40f, 0.70f, 0.96f);
            var total = 0;
            foreach (var g in net.Gifts)
            {
                total += g.Gems;
            }

            HookSkin.Fit(Label(card, "Body", net.Gifts.Count == 0
                ? S("alliance.gifts.none", "No gifts yet. You earn one when a request you helped fills up.")
                : Fmt("alliance.gifts.some", "{0} gifts waiting: {1} gems", net.Gifts.Count, total), 21, TextAnchor.MiddleCenter, SurvivalVisuals.Cream, 0.05f, 0.05f, 0.95f, 0.38f), 12, 21).horizontalOverflow = HorizontalWrapMode.Wrap;
            if (net.Gifts.Count > 0)
            {
                var claim = SheetButton(panel, "Claim", HookArtKeys.ButtonGreen, SurvivalVisuals.MineFill, 0.25f, 0.18f, 0.75f, 0.25f, 26, out var claimBtn);
                claim.text = S("alliance.gifts.claim", "CLAIM ALL");
                Wire(claimBtn, () => net.ClaimGifts(g =>
                {
                    Campaign?.Wallet.Add(SurvIds.ResHard, g);
                    OpenAlliance(TabGifts);
                }));
            }
        }
    }
}
