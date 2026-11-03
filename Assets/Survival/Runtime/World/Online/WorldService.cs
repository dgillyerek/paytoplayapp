#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using Survival.Domain.Alliance;
using Survival.Domain.Serialization;
using static Survival.Domain.Alliance.AllianceService;

namespace Survival.Domain.World.Online
{
    /// <summary>A castle on the shared map (only ones inside your sight come back from the server).</summary>
    public sealed record WorldCastle(long Id, string Name, int Power, int X, int Y, bool Shielded, bool Mine, bool Ally, string Tag, int Color);

    /// <summary>food / wood / stone node or a PvE camp. Occupier is the player gathering there now (0 = free).</summary>
    public sealed record WorldTile(long Id, int X, int Y, string Kind, int Level, int Amount, long Occupier, bool OccupiedByMe, bool OccupiedByAlly)
    {
        public bool IsCamp => Kind == "camp";
    }

    public sealed record WorldMarchInfo(long Id, long PlayerId, string Kind, string State, float X, float Y, int FromX, int FromY, int ToX, int ToY,
        int Troops, int Carrying, string Resource, bool Mine, bool Ally, DateTime ArriveAt, DateTime? GatherUntil, DateTime? ReturnAt)
    {
        /// <summary>Server time when this march next changes state.</summary>
        public DateTime NextAt => State == "gathering" ? GatherUntil ?? ArriveAt : State == "returning" ? ReturnAt ?? ArriveAt : ArriveAt;
    }

    public sealed record WorldSight(int X, int Y, int R);

    public sealed record WorldTerritory(int X, int Y, int R, string Tag, int Color);

    public sealed record WorldReport(long Id, string Kind, string Opponent, int X, int Y, int MyTroops, int MyLoss, int TheirTroops, int TheirLoss,
        IReadOnlyDictionary<string, int> Loot, bool Claimed)
    {
        public bool Won => Kind is "attack_won" or "defend_won" or "camp_won" or "gather_done";
    }

    public sealed record WorldSnapshot(int CenterX, int CenterY, int Radius, int MyX, int MyY, IReadOnlyList<WorldSight> Sight,
        IReadOnlyList<WorldCastle> Castles, IReadOnlyList<WorldTile> Tiles, IReadOnlyList<WorldMarchInfo> Marches, IReadOnlyList<WorldTerritory> Territory)
    {
        /// <summary>Fog of war: true when (x, y) is inside any sight circle (my castle, alliance castles, my marches).</summary>
        public bool Visible(float x, float y)
        {
            foreach (var s in Sight)
            {
                var dx = s.X - x;
                var dy = s.Y - y;
                if (dx * dx + dy * dy <= s.R * s.R)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Loot = what you won; Lost = what raiders took from your stores (positive numbers to take out of the wallet).</summary>
    public sealed record WorldClaim(int Claimed, IReadOnlyDictionary<string, int> Loot, IReadOnlyDictionary<string, int> Lost, int TroopsLostDefending);

    /// <summary>
    /// Shared world map over the alliance sign-in. The server settles marches lazily, so every read here is
    /// authoritative; when the network is down <see cref="Online"/> goes false and the view keeps its scripted map.
    /// </summary>
    public sealed class WorldService
    {
        private readonly AllianceService _net;

        public WorldService(AllianceService net) => _net = net ?? throw new ArgumentNullException(nameof(net));

        public bool Online => _net.Online && Castle != null;
        public (int X, int Y)? Castle { get; private set; }
        public DateTime ShieldUntil { get; private set; }
        /// <summary>Troops at home, as counted by the server (the server owns this number; the phone only shows it).</summary>
        public int Troops { get; private set; }

        /// <summary>Most troops you can have (home + marching); wounded troops heal back toward it.</summary>
        public int TroopCap { get; private set; }
        public int UnreadReports { get; private set; }
        public int MaxMarches { get; private set; } = 3;
        public int MaxRange { get; private set; } = 60;
        public IReadOnlyList<WorldMarchInfo> MyMarches { get; private set; } = Array.Empty<WorldMarchInfo>();
        public WorldSnapshot? Map { get; private set; }
        public IReadOnlyList<WorldReport> Reports { get; private set; } = Array.Empty<WorldReport>();
        public string? LastError => _net.LastError;

        public event Action? Changed;

        public bool Shielded(DateTime nowUtc) => ShieldUntil > nowUtc;

        /// <summary>
        /// Tell the server how strong you are and what sits in your stores (raiders loot from that). <paramref name="seedTroops"/>
        /// only counts the very first time; after that the server owns the troop count and ignores it.
        /// </summary>
        public void SyncStrength(int power, int seedTroops, int heroPower, IReadOnlyDictionary<string, int>? stock = null, Action<bool>? done = null) =>
            _net.Request("POST", "/v1/profile", ProfileBody(power, seedTroops, heroPower, stock), _ => done?.Invoke(true), done);

        internal static string ProfileBody(int power, int seedTroops, int heroPower, IReadOnlyDictionary<string, int>? stock)
        {
            var body = "{\"power\":" + Clamp(power) + ",\"troops\":" + Clamp(seedTroops) + ",\"heroPower\":" + Clamp(heroPower);
            if (stock != null)
            {
                var parts = new List<string>();
                foreach (var kv in stock)
                {
                    parts.Add(Q(kv.Key) + ":" + Clamp(kv.Value));
                }

                body += ",\"stock\":{" + string.Join(",", parts) + "}";
            }

            return body + "}";
        }

        public void RefreshMe(Action<bool>? done = null) => _net.Request("GET", "/v1/world/me", null, o =>
        {
            if (o.Fields.TryGetValue("castle", out var c) && c is JsonNode.Obj co)
            {
                Castle = (Int(co, "x"), Int(co, "y"));
            }

            ShieldUntil = Time(o, "shieldUntil") ?? DateTime.MinValue;
            Troops = Int(o, "troops");
            TroopCap = Int(o, "troopCap");
            UnreadReports = Int(o, "unreadReports");
            if (o.Fields.TryGetValue("rules", out var r) && r is JsonNode.Obj ro)
            {
                MaxMarches = Math.Max(1, Int(ro, "maxMarches"));
                MaxRange = Math.Max(1, Int(ro, "maxRange"));
            }

            var list = new List<WorldMarchInfo>();
            foreach (var m in Arr(o, "marches"))
            {
                list.Add(ReadMarch(m, true));
            }

            MyMarches = list;
            Fire(done);
        }, done);

        public void RefreshMap(int x, int y, int radius, Action<bool>? done = null) =>
            _net.Request("GET", "/v1/world/map?x=" + x.ToString(CultureInfo.InvariantCulture) + "&y=" + y.ToString(CultureInfo.InvariantCulture) + "&r=" + radius.ToString(CultureInfo.InvariantCulture), null, o =>
            {
                Map = ReadSnapshot(o);
                Fire(done);
            }, done);

        /// <summary>kind: gather (resource tile), camp (PvE camp) or attack (another lord's castle).</summary>
        /// <remarks>
        /// The troops leave home the moment the server accepts the march (shown here right away, then re-read from the
        /// server). Asking for more than the server's count fails with bad_troops and nothing changes.
        /// </remarks>
        public void SendMarch(string kind, int x, int y, int troops, Action<bool>? done = null) =>
            _net.Request("POST", "/v1/world/march", "{\"kind\":" + Q(kind) + ",\"x\":" + x + ",\"y\":" + y + ",\"troops\":" + Clamp(troops) + "}", _ =>
            {
                Troops = Math.Max(0, Troops - troops);
                RefreshMe(done);
            }, done);

        public void Recall(long marchId, Action<bool>? done = null) =>
            _net.Request("POST", "/v1/world/recall", "{\"marchId\":" + marchId.ToString(CultureInfo.InvariantCulture) + "}", _ => RefreshMe(done), done);

        public void RefreshReports(Action<bool>? done = null) => _net.Request("GET", "/v1/world/reports", null, o =>
        {
            var list = new List<WorldReport>();
            foreach (var r in Arr(o, "reports"))
            {
                list.Add(new WorldReport(Long(r, "id"), Str(r, "kind"), Str(r, "opponent"), Int(r, "x"), Int(r, "y"), Int(r, "my_troops"), Int(r, "my_loss"),
                    Int(r, "their_troops"), Int(r, "their_loss"), Loot(r, "loot"), Bool(r, "claimed")));
            }

            Reports = list;
            Fire(done);
        }, done);

        /// <summary>Marks every report read and returns the loot to add to the wallet (server pays each report once).</summary>
        public void ClaimReports(Action<WorldClaim?>? done = null) => _net.Request("POST", "/v1/world/reports/claim", "{}", o =>
        {
            UnreadReports = 0;
            var claim = ReadClaim(o);
            Changed?.Invoke();
            done?.Invoke(claim);
        }, _ => done?.Invoke(null));

        internal static WorldClaim ReadClaim(JsonNode.Obj o) => new(Int(o, "claimed"), Loot(o, "loot"), Loot(o, "lost"), Int(o, "troopsLostDefending"));

        internal static WorldSnapshot ReadSnapshot(JsonNode.Obj o)
        {
            var center = o.Fields.TryGetValue("center", out var c) && c is JsonNode.Obj co ? co : new JsonNode.Obj();
            var me = o.Fields.TryGetValue("me", out var m) && m is JsonNode.Obj mo ? mo : new JsonNode.Obj();
            var sight = new List<WorldSight>();
            foreach (var s in Arr(o, "sight"))
            {
                sight.Add(new WorldSight(Int(s, "x"), Int(s, "y"), Int(s, "r")));
            }

            var castles = new List<WorldCastle>();
            foreach (var k in Arr(o, "castles"))
            {
                castles.Add(new WorldCastle(Long(k, "id"), Str(k, "name"), Int(k, "power"), Int(k, "x"), Int(k, "y"), Bool(k, "shielded"), Bool(k, "mine"), Bool(k, "ally"), Str(k, "tag"), IntOr(k, "color", -1)));
            }

            var tiles = new List<WorldTile>();
            foreach (var n in Arr(o, "nodes"))
            {
                tiles.Add(new WorldTile(Long(n, "id"), Int(n, "x"), Int(n, "y"), Str(n, "kind"), Int(n, "level"), Int(n, "amount"), Long(n, "occupier"), Bool(n, "occupiedByMe"), Bool(n, "occupiedByAlly")));
            }

            var marches = new List<WorldMarchInfo>();
            foreach (var mm in Arr(o, "marches"))
            {
                marches.Add(ReadMarch(mm, Bool(mm, "mine")));
            }

            var terr = new List<WorldTerritory>();
            foreach (var t in Arr(o, "territory"))
            {
                terr.Add(new WorldTerritory(Int(t, "x"), Int(t, "y"), Int(t, "r"), Str(t, "tag"), Int(t, "color")));
            }

            return new WorldSnapshot(Int(center, "x"), Int(center, "y"), Int(center, "r"), Int(me, "x"), Int(me, "y"), sight, castles, tiles, marches, terr);
        }

        private static WorldMarchInfo ReadMarch(JsonNode.Obj m, bool mine) =>
            new(Long(m, "id"), Long(m, "playerId"), Str(m, "kind"), Str(m, "state"), Float(m, "x"), Float(m, "y"), Int(m, "fromX"), Int(m, "fromY"), Int(m, "toX"), Int(m, "toY"),
                Int(m, "troops"), Int(m, "carrying"), Str(m, "resource"), mine, mine || Bool(m, "ally"), Time(m, "arriveAt") ?? DateTime.MinValue, Time(m, "gatherUntil"), Time(m, "returnAt"));

        private static Dictionary<string, int> Loot(JsonNode.Obj o, string key)
        {
            var d = new Dictionary<string, int>(StringComparer.Ordinal);
            if (o.Fields.TryGetValue(key, out var n) && n is JsonNode.Obj lo)
            {
                foreach (var kv in lo.Fields)
                {
                    if (kv.Value is JsonNode.Num v)
                    {
                        d[kv.Key] = (int)v.Value;
                    }
                }
            }

            return d;
        }

        private static float Float(JsonNode.Obj o, string key) => o.Fields.TryGetValue(key, out var n) && n is JsonNode.Num v ? (float)v.Value : 0f;

        private static int IntOr(JsonNode.Obj o, string key, int fallback) => o.Fields.ContainsKey(key) ? Int(o, key) : fallback;

        private static DateTime? Time(JsonNode.Obj o, string key) =>
            o.Fields.TryGetValue(key, out var n) && n is JsonNode.Str s && DateTime.TryParse(s.Value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var t) ? t : null;

        private static string Clamp(int v) => Math.Max(0, v).ToString(CultureInfo.InvariantCulture);

        private void Fire(Action<bool>? done)
        {
            Changed?.Invoke();
            done?.Invoke(true);
        }
    }

    /// <summary>
    /// Maps server tiles onto the painted kingdom map: your castle tile sits on your village, and each tile is
    /// <see cref="PixelsPerTile"/> map pixels. Pure math so it is unit-tested.
    /// </summary>
    public readonly struct WorldProjection
    {
        public WorldProjection(int originTileX, int originTileY, float originMapX, float originMapY, float pixelsPerTile)
        {
            OriginTileX = originTileX;
            OriginTileY = originTileY;
            OriginMapX = originMapX;
            OriginMapY = originMapY;
            PixelsPerTile = pixelsPerTile;
        }

        public int OriginTileX { get; }
        public int OriginTileY { get; }
        public float OriginMapX { get; }
        public float OriginMapY { get; }
        public float PixelsPerTile { get; }

        public (float X, float Y) ToMap(float tileX, float tileY) => (OriginMapX + (tileX - OriginTileX) * PixelsPerTile, OriginMapY + (tileY - OriginTileY) * PixelsPerTile);

        public (int X, int Y) ToTile(float mapX, float mapY) =>
            ((int)Math.Round(OriginTileX + (mapX - OriginMapX) / PixelsPerTile), (int)Math.Round(OriginTileY + (mapY - OriginMapY) / PixelsPerTile));
    }
}
