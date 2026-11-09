#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Survival.Domain.Serialization;

namespace Survival.Domain.Alliance
{
    /// <summary>One HTTP call. Unity sends it with UnityWebRequest; tests answer it in memory. Status 0 = no network.</summary>
    public interface IAllianceTransport
    {
        void Send(string method, string path, string? jsonBody, string? token, Action<int, string> done);
    }

    public sealed record AllianceMember(long Id, string Name, int Power, int Role, bool Online)
    {
        /// <summary>R5 leader, R4 officer, R1-R3 members.</summary>
        public string RoleKey => Role >= 5 ? "leader" : Role == 4 ? "officer" : "member";
    }

    public sealed record AllianceInfo(long Id, string Name, string Tag, string Banner, long LeaderId, bool Open, int MaxMembers, IReadOnlyList<AllianceMember> Members);

    public sealed record AllianceListing(long Id, string Name, string Tag, string Banner, bool Open, int Members, int MaxMembers, long Power);

    public sealed record ChatMessage(long Id, long PlayerId, string Name, string Body, string CreatedAt);

    public sealed record HelpRequestInfo(long Id, long PlayerId, string Name, string TaskKey, int Helps, int MaxHelps, int SecondsPerHelp, bool HelpedByMe);

    public sealed record MyHelpRequest(long Id, string TaskKey, int Helps, int MaxHelps, int SecondsSaved, bool Open);

    public sealed record AllianceGift(long Id, string Source, int Gems);

    /// <summary>
    /// Client for the alliance server (Backend/alliance, a Neon Function). The server owns every rule: membership, roles,
    /// help caps, gifts. This class only keeps the last answer it got so the UI can draw without waiting. With no network
    /// (or no base URL) <see cref="Online"/> stays false and the game keeps its on-device guild.
    /// </summary>
    public sealed class AllianceService
    {
        private readonly IAllianceTransport _http;
        private string? _token;
        private long _lastChatId;

        public AllianceService(IAllianceTransport http) => _http = http ?? throw new ArgumentNullException(nameof(http));

        public bool Online { get; private set; }
        public long PlayerId { get; private set; }
        public string PlayerName { get; private set; } = "";
        public AllianceInfo? Alliance { get; private set; }
        public int MyRole { get; private set; }
        public IReadOnlyList<AllianceListing> Listings { get; private set; } = Array.Empty<AllianceListing>();
        public List<ChatMessage> Chat { get; } = new();
        public IReadOnlyList<HelpRequestInfo> Requests { get; private set; } = Array.Empty<HelpRequestInfo>();
        public IReadOnlyList<MyHelpRequest> Mine { get; private set; } = Array.Empty<MyHelpRequest>();
        public IReadOnlyList<AllianceGift> Gifts { get; private set; } = Array.Empty<AllianceGift>();
        public string? LastError { get; private set; }
        public bool InAlliance => Online && Alliance != null;

        /// <summary>
        /// The signed-in bearer token (null until SignIn succeeds). Shared with the purchase verifier so both use the one
        /// player account: signing in twice would rotate the token and log the other client out.
        /// </summary>
        public string? SessionToken => _token;

        /// <summary>The transport this client sends with (shared by the purchase verifier).</summary>
        public IAllianceTransport Transport => _http;

        /// <summary>Fires after any answer changes the cached state.</summary>
        public event Action? Changed;

        public void SignIn(string deviceId, string name, Action<bool>? done = null) =>
            Call("POST", "/v1/signin", Body("deviceId", deviceId, "name", name), o =>
            {
                _token = Str(o, "token");
                var p = (JsonNode.Obj)o.Fields["player"];
                PlayerId = Long(p, "id");
                PlayerName = Str(p, "display_name");
                Online = true;
                RefreshMe(done);
            }, done, auth: false);

        /// <summary>Account age band ("under13" | "13plus" | "unknown"; Survival.Domain.Iap.AgeBands). Last value the server confirmed.</summary>
        public string AgeBand { get; private set; } = "unknown";

        /// <summary>Store the account's age band on the server (COPPA / Play Families). Only the band is sent, never a birth date.</summary>
        public void SetAgeBand(string ageBand, Action<bool>? done = null) =>
            Call("POST", "/v1/account/age_band", Body("ageBand", ageBand), o =>
            {
                AgeBand = Str(o, "ageBand");
                Fire(done);
            }, done);

        /// <summary>Last HTTP response body (for cloud-save blob extraction).</summary>
        public string? LastRawBody { get; private set; }

        /// <summary>Cloud save GET /v1/save. done(version, blobJson or null, consentAt, ageBand, error).</summary>
        public void GetSave(Action<long, string?, string?, string, string?> done) =>
            Call("GET", "/v1/save", null, o =>
            {
                var version = Long(o, "version");
                var blob = SliceJsonObject(LastRawBody, "blob");
                if (blob == null && o.Fields.TryGetValue("blob", out var bn) && bn is JsonNode.Null)
                {
                    blob = null;
                }

                var consent = o.Fields.TryGetValue("consentAt", out var c) && c is JsonNode.Str cs ? cs.Value : null;
                if (consent == null && o.Fields.TryGetValue("consentAt", out var cn) && cn is JsonNode.Null)
                {
                    consent = null;
                }

                var band = Str(o, "ageBand");
                if (!string.IsNullOrEmpty(band))
                {
                    AgeBand = band;
                }

                done(version, blob, consent, string.IsNullOrEmpty(AgeBand) ? "unknown" : AgeBand, null);
            }, _ => done(0, null, null, AgeBand, LastError));

        /// <summary>Cloud save PUT /v1/save. <paramref name="blobJson"/> is the document object JSON.</summary>
        public void PutSave(long baseVersion, string blobJson, bool consentGranted, Action<long, string?, bool, string?, string?> done)
        {
            var body = "{\"baseVersion\":" + baseVersion.ToString(CultureInfo.InvariantCulture)
                + ",\"blob\":" + blobJson
                + (consentGranted ? ",\"consentGranted\":true" : "")
                + "}";
            Call("PUT", "/v1/save", body, o =>
            {
                var version = Long(o, "version");
                var merged = Bool(o, "merged");
                var consent = o.Fields.TryGetValue("consentAt", out var c) && c is JsonNode.Str cs ? cs.Value : null;
                var blob = SliceJsonObject(LastRawBody, "blob");
                done(version, blob, merged, consent, null);
            }, _ => done(0, null, false, null, LastError));
        }

        public void RefreshMe(Action<bool>? done = null) => Call("GET", "/v1/me", null, o =>
        {
            Alliance = o.Fields.TryGetValue("alliance", out var a) && a is JsonNode.Obj ao ? ReadAlliance(ao) : null;
            MyRole = Int(o, "role");
            if (Alliance == null)
            {
                Chat.Clear();
                _lastChatId = 0;
                Requests = Array.Empty<HelpRequestInfo>();
            }

            Fire(done);
        }, done);

        public void Search(string query, Action<bool>? done = null) =>
            Call("GET", "/v1/alliances?q=" + Uri.EscapeDataString(query ?? ""), null, o =>
            {
                var list = new List<AllianceListing>();
                foreach (var n in Arr(o, "alliances"))
                {
                    list.Add(new AllianceListing(Long(n, "id"), Str(n, "name"), Str(n, "tag"), Str(n, "banner"), Bool(n, "is_open"), Int(n, "members"), Int(n, "max_members"), Long(n, "power")));
                }

                Listings = list;
                Fire(done);
            }, done);

        public void Create(string name, string tag, bool open, Action<bool>? done = null) =>
            Call("POST", "/v1/alliances", "{\"name\":" + Q(name) + ",\"tag\":" + Q(tag) + ",\"open\":" + (open ? "true" : "false") + "}", _ => RefreshMe(done), done);

        public void Join(long allianceId, Action<bool>? done = null) =>
            Call("POST", "/v1/alliances/join", "{\"allianceId\":" + allianceId + "}", _ => RefreshMe(done), done);

        public void Leave(Action<bool>? done = null) => Call("POST", "/v1/alliances/leave", "{}", _ => RefreshMe(done), done);

        public void SetRole(long playerId, int role, Action<bool>? done = null) =>
            Call("POST", "/v1/alliances/role", "{\"playerId\":" + playerId + ",\"role\":" + role + "}", _ => RefreshMe(done), done);

        /// <summary>Poll for new chat lines (call every few seconds while the chat tab is open).</summary>
        public void PollChat(Action<bool>? done = null) => Call("GET", "/v1/chat?after=" + _lastChatId, null, o =>
        {
            foreach (var n in Arr(o, "messages"))
            {
                var m = new ChatMessage(Long(n, "id"), Long(n, "player_id"), Str(n, "name"), Str(n, "body"), Str(n, "created_at"));
                if (m.Id > _lastChatId)
                {
                    Chat.Add(m);
                    _lastChatId = m.Id;
                }
            }

            if (Chat.Count > 200)
            {
                Chat.RemoveRange(0, Chat.Count - 200);
            }

            Fire(done);
        }, done);

        public void SendChat(string body, Action<bool>? done = null) =>
            Call("POST", "/v1/chat", Body("body", body), _ => PollChat(done), done);

        public void RefreshHelp(Action<bool>? done = null) => Call("GET", "/v1/help", null, o =>
        {
            var list = new List<HelpRequestInfo>();
            foreach (var n in Arr(o, "requests"))
            {
                list.Add(new HelpRequestInfo(Long(n, "id"), Long(n, "player_id"), Str(n, "name"), Str(n, "task_key"), Int(n, "helps"), Int(n, "max_helps"), Int(n, "seconds_per_help"), Bool(n, "helped_by_me")));
            }

            Requests = list;
            Call("GET", "/v1/help/mine", null, m =>
            {
                var mine = new List<MyHelpRequest>();
                foreach (var n in Arr(m, "requests"))
                {
                    mine.Add(new MyHelpRequest(Long(n, "id"), Str(n, "task_key"), Int(n, "helps"), Int(n, "max_helps"), Int(n, "seconds_saved"), Bool(n, "open")));
                }

                Mine = mine;
                Fire(done);
            }, done);
        }, done);

        /// <summary>Ask the alliance to help one of the player's timers. The server clamps the seconds per help.</summary>
        public void RequestHelp(string taskKey, int secondsPerHelp, Action<bool>? done = null) =>
            Call("POST", "/v1/help/request", "{\"taskKey\":" + Q(taskKey) + ",\"secondsPerHelp\":" + secondsPerHelp + "}", _ => RefreshHelp(done), done);

        /// <summary>Help guildmates. <paramref name="done"/> gets the gold the server says this earned (0 past the daily cap).</summary>
        public void GiveHelp(IReadOnlyList<long> requestIds, Action<int, int>? done = null)
        {
            var sb = new StringBuilder("{\"requestIds\":[");
            for (var i = 0; i < requestIds.Count; i++)
            {
                sb.Append(i == 0 ? "" : ",").Append(requestIds[i].ToString(CultureInfo.InvariantCulture));
            }

            Call("POST", "/v1/help/give", sb.Append("]}").ToString(), o =>
            {
                done?.Invoke(Int(o, "helped"), Int(o, "gold"));
                RefreshHelp();
            }, ok => { if (!ok) done?.Invoke(0, 0); });
        }

        public void RefreshGifts(Action<bool>? done = null) => Call("GET", "/v1/gifts", null, o =>
        {
            var list = new List<AllianceGift>();
            foreach (var n in Arr(o, "gifts"))
            {
                list.Add(new AllianceGift(Long(n, "id"), Str(n, "source"), Int(n, "gems")));
            }

            Gifts = list;
            Fire(done);
        }, done);

        /// <summary>Claim every gift; <paramref name="done"/> gets the gems the server granted.</summary>
        public void ClaimGifts(Action<int>? done = null) => Call("POST", "/v1/gifts/claim", "{}", o =>
        {
            Gifts = Array.Empty<AllianceGift>();
            done?.Invoke(Int(o, "gems"));
            Fire(null);
        }, ok => { if (!ok) done?.Invoke(0); });

        private void Fire(Action<bool>? done)
        {
            Changed?.Invoke();
            done?.Invoke(true);
        }

        /// <summary>Authenticated call for sibling services (World map) that share this sign-in.</summary>
        internal void Request(string method, string path, string? body, Action<JsonNode.Obj> ok, Action<bool>? fail) => Call(method, path, body, ok, fail);

        private void Call(string method, string path, string? body, Action<JsonNode.Obj> ok, Action<bool>? fail, bool auth = true)
        {
            if (auth && _token == null)
            {
                LastError = "not_signed_in";
                fail?.Invoke(false);
                return;
            }

            _http.Send(method, path, body, auth ? _token : null, (status, text) =>
            {
                LastRawBody = text;
                JsonNode.Obj? obj = null;
                try
                {
                    obj = JsonRead.Parse(string.IsNullOrEmpty(text) ? "{}" : text) as JsonNode.Obj;
                }
                catch (FormatException)
                {
                }

                if (status == 0)
                {
                    Online = false;
                }

                if (status < 200 || status >= 300 || obj == null)
                {
                    LastError = obj != null && obj.Fields.TryGetValue("error", out var e) && e is JsonNode.Str s ? s.Value : status == 0 ? "offline" : "http_" + status;
                    if (status == 401)
                    {
                        _token = null;
                        Online = false;
                    }

                    fail?.Invoke(false);
                    return;
                }

                LastError = null;
                ok(obj);
            });
        }

        private static string Body(string k1, string v1, string? k2 = null, string? v2 = null) =>
            "{" + Q(k1) + ":" + Q(v1) + (k2 == null ? "" : "," + Q(k2) + ":" + Q(v2 ?? "")) + "}";

        /// <summary>JSON string literal.</summary>
        public static string Q(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (var c in s ?? "")
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    default:
                        if (c < 0x20)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            return sb.Append('"').ToString();
        }

        private static AllianceInfo ReadAlliance(JsonNode.Obj a)
        {
            var members = new List<AllianceMember>();
            foreach (var m in Arr(a, "members"))
            {
                members.Add(new AllianceMember(Long(m, "id"), Str(m, "name"), Int(m, "power"), Int(m, "role"), Bool(m, "online")));
            }

            return new AllianceInfo(Long(a, "id"), Str(a, "name"), Str(a, "tag"), Str(a, "banner"), Long(a, "leader_id"), Bool(a, "is_open"), Int(a, "max_members"), members);
        }

        internal static IEnumerable<JsonNode.Obj> Arr(JsonNode.Obj o, string key)
        {
            if (o.Fields.TryGetValue(key, out var n) && n is JsonNode.Arr arr)
            {
                foreach (var i in arr.Items)
                {
                    if (i is JsonNode.Obj io)
                    {
                        yield return io;
                    }
                }
            }
        }

        internal static string Str(JsonNode.Obj o, string key) => o.Fields.TryGetValue(key, out var n) && n is JsonNode.Str s ? s.Value : "";

        internal static bool Bool(JsonNode.Obj o, string key) => o.Fields.TryGetValue(key, out var n) && n is JsonNode.Bool b && b.Value;

        /// <summary>Postgres bigint ids and sums arrive as JSON strings; ints arrive as numbers. Accept both.</summary>
        internal static long Long(JsonNode.Obj o, string key)
        {
            if (!o.Fields.TryGetValue(key, out var n))
            {
                return 0;
            }

            return n switch
            {
                JsonNode.Num num => (long)num.Value,
                JsonNode.Str s when long.TryParse(s.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) => v,
                _ => 0,
            };
        }

        internal static int Int(JsonNode.Obj o, string key) => (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, Long(o, key)));
        /// <summary>Extract a top-level JSON object value by key from a raw response body.</summary>
        internal static string? SliceJsonObject(string? json, string key)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            var needle = "\"" + key + "\"";
            var i = json.IndexOf(needle, StringComparison.Ordinal);
            if (i < 0)
            {
                return null;
            }

            i += needle.Length;
            while (i < json.Length && char.IsWhiteSpace(json[i]))
            {
                i++;
            }

            if (i >= json.Length || json[i] != ':')
            {
                return null;
            }

            i++;
            while (i < json.Length && char.IsWhiteSpace(json[i]))
            {
                i++;
            }

            if (i >= json.Length)
            {
                return null;
            }

            if (json[i] == 'n') // null
            {
                return null;
            }

            if (json[i] != '{')
            {
                return null;
            }

            var start = i;
            var depth = 0;
            for (var j = i; j < json.Length; j++)
            {
                var c = json[j];
                if (c == '"')
                {
                    j++;
                    while (j < json.Length)
                    {
                        if (json[j] == '\\')
                        {
                            j += 2;
                            continue;
                        }

                        if (json[j] == '"')
                        {
                            break;
                        }

                        j++;
                    }

                    continue;
                }

                if (c == '{')
                {
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return json.Substring(start, j - start + 1);
                    }
                }
            }

            return null;
        }

    }
}
