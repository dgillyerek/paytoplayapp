#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Alliance;
using Survival.Domain.World;
using Survival.Domain.World.Online;
using Xunit;

namespace Survival.Domain.Tests
{
    /// <summary>Contract tests: the JSON below is what the deployed world API returned in the 2026-11-01 smoke test.</summary>
    public class WorldServiceTests
    {
        private sealed class Canned : IAllianceTransport
        {
            public readonly Dictionary<string, (int, string)> Map = new();
            public readonly List<(string Method, string Path, string? Body)> Calls = new();

            public void Send(string method, string path, string? jsonBody, string? token, Action<int, string> done)
            {
                Calls.Add((method, path, jsonBody));
                var key = method + " " + path.Split('?')[0];
                done(Map.TryGetValue(key, out var r) ? r.Item1 : 0, Map.TryGetValue(key, out r) ? r.Item2 : "");
            }
        }

        private const string MapJson = "{\"center\":{\"x\":504,\"y\":496,\"r\":6},\"me\":{\"x\":504,\"y\":496},\"sight\":[{\"x\":504,\"y\":496,\"r\":15}],\"castles\":[{\"id\":\"4\",\"name\":\"WS Bravo\",\"power\":10000,\"x\":499,\"y\":502,\"shielded\":false,\"mine\":false,\"ally\":false,\"tag\":\"WSM\",\"color\":2},{\"id\":\"3\",\"name\":\"WS Alpha\",\"power\":30000,\"x\":510,\"y\":502,\"shielded\":true,\"mine\":false,\"ally\":false,\"tag\":\"WSM\",\"color\":2},{\"id\":\"5\",\"name\":\"WS Charlie\",\"power\":20000,\"x\":504,\"y\":496,\"shielded\":false,\"mine\":true,\"ally\":true,\"tag\":null,\"color\":-1}],\"nodes\":[{\"id\":\"11\",\"x\":498,\"y\":498,\"kind\":\"camp\",\"level\":2,\"amount\":0,\"occupier\":null,\"occupiedByMe\":false,\"occupiedByAlly\":false},{\"id\":\"18\",\"x\":499,\"y\":490,\"kind\":\"camp\",\"level\":2,\"amount\":0,\"occupier\":null,\"occupiedByMe\":false,\"occupiedByAlly\":false}],\"marches\":[],\"territory\":[{\"x\":499,\"y\":502,\"r\":6,\"tag\":\"WSM\",\"color\":2},{\"x\":510,\"y\":502,\"r\":6,\"tag\":\"WSM\",\"color\":2}]}";
        private const string MeJson = "{\"castle\":{\"x\":504,\"y\":496},\"shieldUntil\":\"2026-11-01T16:23:42.164Z\",\"troops\":2000,\"heroPower\":100,\"marches\":[],\"unreadReports\":4,\"rules\":{\"size\":1000,\"sight\":15,\"marchSight\":6,\"territory\":6,\"maxView\":25,\"maxRange\":60,\"secPerTile\":4,\"minMarchSec\":20,\"maxMarches\":3,\"newShieldHours\":24,\"defeatShieldHours\":4,\"gatherPerSecPer20Troops\":1,\"loadPerTroop\":10,\"nodeAmountPerLevel\":10000,\"nodeRespawnMin\":30,\"campRespawnMin\":15,\"defenderLossCap\":0.1,\"attackerLossCap\":0.25,\"gatherLossCap\":0.2}}";
        private const string ReportsJson = "{\"reports\":[{\"id\":\"8\",\"kind\":\"gather_done\",\"opponent_id\":null,\"opponent\":null,\"x\":506,\"y\":499,\"my_troops\":828,\"my_loss\":0,\"their_troops\":0,\"their_loss\":0,\"loot\":{\"stone\":318},\"claimed\":false,\"created_at\":\"2026-11-01T16:24:47.119Z\"},{\"id\":\"6\",\"kind\":\"shielded\",\"opponent_id\":\"3\",\"opponent\":\"WS Alpha\",\"x\":510,\"y\":502,\"my_troops\":500,\"my_loss\":0,\"their_troops\":0,\"their_loss\":0,\"loot\":{},\"claimed\":false,\"created_at\":\"2026-11-01T16:24:18.724Z\"}]}";

        private static (WorldService, Canned) Online()
        {
            var t = new Canned();
            t.Map["POST /v1/signin"] = (200, "{\"token\":\"tok\",\"player\":{\"id\":\"5\",\"display_name\":\"WS Charlie\"}}");
            t.Map["GET /v1/me"] = (200, "{\"player\":{\"id\":\"5\"},\"alliance\":null,\"role\":0}");
            t.Map["GET /v1/world/map"] = (200, MapJson);
            t.Map["GET /v1/world/me"] = (200, MeJson);
            t.Map["GET /v1/world/reports"] = (200, ReportsJson);
            var net = new AllianceService(t);
            net.SignIn("device-xyz", "WS Charlie");
            return (new WorldService(net), t);
        }

        [Fact]
        public void Me_reads_castle_shield_and_rules()
        {
            var (w, _) = Online();
            Assert.False(w.Online);
            w.RefreshMe();
            Assert.True(w.Online);
            Assert.Equal((504, 496), w.Castle);
            Assert.Equal(2000, w.Troops);
            Assert.Equal(3, w.MaxMarches);
            Assert.Equal(4, w.UnreadReports);
            Assert.False(w.Shielded(new DateTime(2026, 11, 1, 17, 0, 0, DateTimeKind.Utc)));
        }

        [Fact]
        public void Map_reads_castles_alliance_colors_territory_and_fog()
        {
            var (w, t) = Online();
            w.RefreshMap(504, 496, 6);
            Assert.Contains(t.Calls, c => c.Path == "/v1/world/map?x=504&y=496&r=6");
            var m = w.Map!;
            Assert.Equal(3, m.Castles.Count);
            Assert.Contains(m.Castles, c => c.Mine && c.Name == "WS Charlie" && c.Color == -1);
            Assert.Contains(m.Castles, c => c.Tag == "WSM" && c.Color == 2 && c.Shielded);
            Assert.Equal(2, m.Territory.Count);
            Assert.All(m.Tiles, n => Assert.True(n.IsCamp));
            Assert.True(m.Visible(510, 502));
            Assert.False(m.Visible(530, 530));
        }

        [Fact]
        public void Reports_read_loot_and_kinds()
        {
            var (w, _) = Online();
            w.RefreshReports();
            Assert.Equal("gather_done", w.Reports[0].Kind);
            Assert.True(w.Reports[0].Won);
            Assert.Equal(318, w.Reports[0].Loot["stone"]);
            Assert.Equal("WS Alpha", w.Reports[1].Opponent);
            Assert.False(w.Reports[1].Won);
        }

        [Fact]
        public void Send_march_posts_kind_tile_and_troops()
        {
            var (w, t) = Online();
            t.Map["POST /v1/world/march"] = (201, "{\"march\":{\"id\":\"9\"}}");
            var ok = false;
            w.SendMarch("gather", 506, 499, 1000, r => ok = r);
            Assert.True(ok);
            Assert.Contains(t.Calls, c => c.Path == "/v1/world/march" && c.Body == "{\"kind\":\"gather\",\"x\":506,\"y\":499,\"troops\":1000}");
        }

        [Fact]
        public void Server_error_is_reported_and_offline_keeps_mock()
        {
            var (w, t) = Online();
            t.Map["POST /v1/world/march"] = (409, "{\"error\":\"too_many_marches\"}");
            var ok = true;
            w.SendMarch("camp", 1, 1, 5, r => ok = r);
            Assert.False(ok);
            Assert.Equal("too_many_marches", w.LastError);
            t.Map.Remove("GET /v1/world/me");
            w.RefreshMe();
            Assert.False(w.Online);
        }

        [Fact]
        public void Claim_returns_loot_once()
        {
            var (w, t) = Online();
            t.Map["POST /v1/world/reports/claim"] = (200, "{\"claimed\":3,\"loot\":{\"gold\":200,\"heroXp\":50,\"stone\":990},\"troopsLostDefending\":0}");
            WorldClaim? got = null;
            w.ClaimReports(c => got = c);
            Assert.Equal(990, got!.Loot["stone"]);
            Assert.Equal(0, w.UnreadReports);
        }

        [Fact]
        public void Projection_round_trips_tiles()
        {
            var p = new WorldProjection(504, 496, 1000f, 800f, 40f);
            Assert.Equal((1240f, 1040f), p.ToMap(510, 502));
            Assert.Equal((510, 502), p.ToTile(1240f, 1040f));
        }
    }
}
