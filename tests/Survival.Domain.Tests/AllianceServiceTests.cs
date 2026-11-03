#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Alliance;
using Survival.Domain.Guild;
using Xunit;

namespace Survival.Domain.Tests
{
    /// <summary>Contract tests: responses below are the exact shapes the deployed alliance server returned (smoke test 2026-11-01).</summary>
    public class AllianceServiceTests
    {
        private sealed class Canned : IAllianceTransport
        {
            public readonly Dictionary<string, (int, string)> Map = new();
            public readonly List<(string Method, string Path, string? Body, string? Token)> Calls = new();

            public void Send(string method, string path, string? jsonBody, string? token, Action<int, string> done)
            {
                Calls.Add((method, path, jsonBody, token));
                var key = method + " " + path.Split('?')[0];
                done(Map.TryGetValue(key, out var r) ? r.Item1 : 0, Map.TryGetValue(key, out r) ? r.Item2 : "");
            }
        }

        private const string SignIn = "{\"token\":\"tok123\",\"player\":{\"id\":\"2\",\"display_name\":\"SmokeB\",\"power\":0,\"castle_x\":10,\"castle_y\":20}}";
        private const string Me = "{\"player\":{\"id\":\"2\"},\"alliance\":{\"id\":\"1\",\"name\":\"Smoke Test Keep\",\"tag\":\"SMK\",\"banner\":\"banner_01\",\"leader_id\":\"1\",\"is_open\":true,\"max_members\":50,\"members\":[{\"id\":\"1\",\"name\":\"SmokeA\",\"power\":0,\"role\":5,\"online\":true},{\"id\":\"2\",\"name\":\"SmokeB\",\"power\":0,\"role\":1,\"online\":true}]},\"role\":1}";
        private const string Help = "{\"requests\":[{\"id\":\"1\",\"player_id\":\"1\",\"name\":\"SmokeA\",\"task_key\":\"farm.2\",\"helps\":0,\"max_helps\":10,\"seconds_per_help\":90,\"helped_by_me\":false}]}";
        private const string Mine = "{\"requests\":[{\"id\":\"3\",\"task_key\":\"forge.1\",\"helps\":4,\"max_helps\":10,\"seconds_saved\":240,\"open\":true}]}";

        private static (AllianceService, Canned) Online()
        {
            var t = new Canned();
            t.Map["POST /v1/signin"] = (200, SignIn);
            t.Map["GET /v1/me"] = (200, Me);
            t.Map["GET /v1/help"] = (200, Help);
            t.Map["GET /v1/help/mine"] = (200, Mine);
            t.Map["POST /v1/help/give"] = (200, "{\"helped\":1,\"gold\":100}");
            var s = new AllianceService(t);
            s.SignIn("device-xyz", "SmokeB");
            s.RefreshHelp();
            return (s, t);
        }

        [Fact]
        public void SignInReadsStringIdsAndAlliance()
        {
            var (s, t) = Online();
            Assert.True(s.Online);
            Assert.True(s.InAlliance);
            Assert.Equal(2, s.PlayerId);
            Assert.Equal("SMK", s.Alliance!.Tag);
            Assert.Equal("leader", s.Alliance.Members[0].RoleKey);
            Assert.Equal("tok123", t.Calls[1].Token);
            Assert.Null(t.Calls[0].Token);
        }

        [Fact]
        public void NoNetworkStaysOfflineAndGuildFallsBackToSimulated()
        {
            var s = new AllianceService(new Canned());
            s.SignIn("device-xyz", "x");
            Assert.False(s.Online);
            Assert.Equal("offline", s.LastError);
            var g = new OnlineGuild(s);
            Assert.False(g.Live);
            Assert.Equal(new SimulatedGuild().OpenRequests(5000).Count, g.OpenRequests(5000).Count);
        }

        [Fact]
        public void OnlineGuildShowsRealRequestsAndSendsHelp()
        {
            var (s, t) = Online();
            var g = new OnlineGuild(s);
            var reqs = g.OpenRequests(0);
            Assert.Single(reqs);
            Assert.Equal("SmokeA", reqs[0].Mate);
            Assert.Equal("farm", reqs[0].Task);
            Assert.True(g.SendHelp("net.1", 0));
            Assert.Contains(t.Calls, c => c.Path == "/v1/help/give" && c.Body == "{\"requestIds\":[1]}");
            Assert.False(g.SendHelp("sim.4", 0));
            Assert.Equal(4, g.HelpsReceived("forge.1", 0, 0));
        }

        [Fact]
        public void AskPostsRequestWithHelpSeconds()
        {
            var (s, t) = Online();
            new OnlineGuild(s).Asked("farm.2", 72.4);
            Assert.Contains(t.Calls, c => c.Path == "/v1/help/request" && c.Body == "{\"taskKey\":\"farm.2\",\"secondsPerHelp\":72}");
        }

        [Fact]
        public void ServerErrorCodeIsKeptAnd401SignsOut()
        {
            var (s, t) = Online();
            t.Map["POST /v1/alliances/join"] = (409, "{\"error\":\"already_in_alliance\"}");
            var ok = true;
            s.Join(5, r => ok = r);
            Assert.False(ok);
            Assert.Equal("already_in_alliance", s.LastError);
            t.Map["GET /v1/me"] = (401, "{\"error\":\"unauthorized\"}");
            s.RefreshMe();
            Assert.False(s.Online);
            Assert.False(s.InAlliance);
        }

        [Fact]
        public void ChatPollAppendsOnlyNewLines()
        {
            var (s, t) = Online();
            t.Map["GET /v1/chat"] = (200, "{\"messages\":[{\"id\":\"1\",\"player_id\":\"1\",\"name\":\"SmokeA\",\"body\":\"Welcome to the keep!\",\"created_at\":\"x\"}]}");
            s.PollChat();
            s.PollChat();
            Assert.Single(s.Chat);
            Assert.EndsWith("after=1", t.Calls[^1].Path);
        }

        [Fact]
        public void JsonQuoteEscapes() => Assert.Equal("\"a\\\"b\\\\\\u000a\"", AllianceService.Q("a\"b\\\n"));
    }
}
