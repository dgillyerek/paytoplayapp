#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Survival.Domain.Alliance;
using Survival.Domain.Serialization;
using Survival.Domain.World;
using Survival.Domain.World.Online;
using Xunit;

namespace Survival.Domain.Tests
{
    /// <summary>World map fixes: troop slider, troops leaving home, server-owned counts, real loot, online castle spacing.</summary>
    public class WorldAuthorityTests
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

        private static string Me(int troops) => "{\"castle\":{\"x\":504,\"y\":496},\"shieldUntil\":\"2026-11-01T16:23:42.164Z\",\"troops\":" + troops +
            ",\"troopCap\":5000,\"heroPower\":100,\"marches\":[],\"unreadReports\":0,\"rules\":{\"maxMarches\":3,\"maxRange\":60}}";

        private static (WorldService, Canned) Online(int troops)
        {
            var t = new Canned();
            t.Map["POST /v1/signin"] = (200, "{\"token\":\"tok\",\"player\":{\"id\":\"5\",\"display_name\":\"WS Charlie\"}}");
            t.Map["GET /v1/me"] = (200, "{\"player\":{\"id\":\"5\"},\"alliance\":null,\"role\":0}");
            t.Map["GET /v1/world/me"] = (200, Me(troops));
            var net = new AllianceService(t);
            net.SignIn("device-xyz", "WS Charlie");
            var w = new WorldService(net);
            w.RefreshMe();
            return (w, t);
        }

        [Theory]
        [InlineData(0f, 1)]
        [InlineData(1f, 2000)]
        [InlineData(0.5f, 1001)]
        [InlineData(-3f, 1)]
        [InlineData(7f, 2000)]
        public void Slider_picks_between_one_and_everything_at_home(float t, int expected) =>
            Assert.Equal(expected, MarchTroops.FromSlider(t, 2000));

        [Fact]
        public void Slider_starts_at_half_and_round_trips()
        {
            Assert.Equal(1000, MarchTroops.Start(2000));
            Assert.Equal(1, MarchTroops.Start(1));
            Assert.Equal(0, MarchTroops.Start(0));
            foreach (var n in new[] { 1, 17, 999, 2000 })
            {
                Assert.Equal(n, MarchTroops.FromSlider(MarchTroops.ToSlider(n, 2000), 2000));
            }
        }

        [Fact]
        public void Cannot_send_more_than_home_or_zero()
        {
            Assert.False(MarchTroops.CanSend(0, 2000));
            Assert.False(MarchTroops.CanSend(2001, 2000));
            Assert.True(MarchTroops.CanSend(2000, 2000));
            Assert.Equal(2000, MarchTroops.Clamp(9999, 2000));
        }

        [Fact]
        public void Sent_troops_leave_home_and_the_count_comes_from_the_server()
        {
            var (w, t) = Online(2000);
            Assert.Equal(2000, w.Troops);
            Assert.Equal(5000, w.TroopCap);
            t.Map["POST /v1/world/march"] = (201, "{\"march\":{\"id\":\"9\"}}");
            t.Map["GET /v1/world/me"] = (200, Me(1300)); // the server took 700 out of the castle
            w.SendMarch("gather", 506, 499, 700);
            Assert.Contains(t.Calls, c => c.Path == "/v1/world/march" && c.Body!.Contains("\"troops\":700"));
            Assert.Equal(1300, w.Troops);
        }

        [Fact]
        public void A_refused_send_keeps_troops_home()
        {
            var (w, t) = Online(2000);
            t.Map["POST /v1/world/march"] = (400, "{\"error\":\"bad_troops\"}");
            var ok = true;
            w.SendMarch("gather", 506, 499, 5000, r => ok = r);
            Assert.False(ok);
            Assert.Equal(2000, w.Troops);
        }

        [Fact]
        public void Phone_troops_are_only_a_one_time_seed_and_stock_is_sent_for_raids()
        {
            var body = WorldService.ProfileBody(20000, 2000, 200, new Dictionary<string, int> { ["food"] = 12000, ["gold"] = -5 });
            Assert.Contains("\"troops\":2000", body);
            Assert.Contains("\"stock\":{\"food\":12000,\"gold\":0}", body);
        }

        [Fact]
        public void Claim_splits_won_loot_from_resources_raiders_took()
        {
            var o = (JsonNode.Obj)JsonRead.Parse("{\"claimed\":2,\"loot\":{\"food\":2000},\"lost\":{\"wood\":450},\"troopsLostDefending\":30}");
            var c = WorldService.ReadClaim(o);
            Assert.Equal(2000, c.Loot["food"]);
            Assert.Equal(450, c.Lost["wood"]);
            Assert.Equal(30, c.TroopsLostDefending);
        }

        [Fact]
        public void Online_castles_on_neighbouring_tiles_are_spaced_apart()
        {
            // five castles one tile (70 px) apart, plus a fixed map place in the middle of them
            var castles = Enumerable.Range(0, 5)
                .Select(i => new WorldEntity("online_" + i, i % 2 == 0 ? WorldEntityKind.RivalVillage : WorldEntityKind.AllyVillage, "castle", "Lord " + i, 0, 1000, 0, 2000f + i * 70f, 3000f + (i % 2) * 70f))
                .ToList();
            var fixedPlaces = new[] { new WorldEntity("home", WorldEntityKind.PlayerVillage, "village", "You", 1, 1000, 0, 2140f, 3000f) };
            Assert.NotEmpty(WorldSpacing.Overlaps(castles.Concat(fixedPlaces).ToList()));
            var spaced = WorldSpacing.Separate(castles, fixedPlaces);
            var bad = WorldSpacing.Overlaps(spaced.Concat(fixedPlaces).ToList());
            Assert.True(bad.Count == 0, "Still overlapping:\n" + string.Join("\n", bad));
            Assert.Equal(castles.Select(c => c.Id), spaced.Select(c => c.Id));
        }

        [Fact]
        public void Castles_already_apart_do_not_move()
        {
            var a = new WorldEntity("online_a", WorldEntityKind.RivalVillage, "castle", "A", 0, 1, 0, 0f, 0f);
            var b = a with { Id = "online_b", X = 2000f };
            var spaced = WorldSpacing.Separate(new[] { a, b }, Array.Empty<WorldEntity>());
            Assert.Equal(a, spaced[0]);
            Assert.Equal(b, spaced[1]);
        }
    }
}
