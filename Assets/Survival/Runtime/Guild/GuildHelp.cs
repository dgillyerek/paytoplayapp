#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Campaign;
using Survival.Domain.Clock;
using Survival.Domain.Economy;
using Survival.Domain.Ids;
using Survival.Domain.Serialization;

namespace Survival.Domain.Guild
{
    /// <summary>One guildmate's open "help me" request (a building or training they are waiting on).</summary>
    public sealed class GuildRequest
    {
        public GuildRequest(string id, string mate, string task, int helps, int maxHelps)
        {
            Id = id;
            Mate = mate;
            Task = task;
            Helps = helps;
            MaxHelps = maxHelps;
        }

        public string Id { get; }
        public string Mate { get; }

        /// <summary>String-table key suffix for what they are building (guild.task.*).</summary>
        public string Task { get; }

        /// <summary>Helps from other guildmates so far (not counting the player).</summary>
        public int Helps { get; }
        public int MaxHelps { get; }
    }

    /// <summary>
    /// The seam for a real guild server. Today <see cref="SimulatedGuild"/> fakes guildmates on this device;
    /// a backend client can implement this without touching <see cref="GuildHelp"/> or the UI.
    /// </summary>
    public interface IGuildBackend
    {
        /// <summary>Guildmates' requests open right now.</summary>
        IReadOnlyList<GuildRequest> OpenRequests(double now);

        /// <summary>Send the player's help to a request. False if it is gone.</summary>
        bool SendHelp(string requestId, double now);

        /// <summary>How many guildmate helps the player's request (posted at <paramref name="askedAt"/>) has received by now.</summary>
        int HelpsReceived(string timerId, double askedAt, double now);
    }

    /// <summary>
    /// Offline guildmates: every 30 minutes one of eight mates posts a request that stays open for 3 hours;
    /// the player's own requests get one help every 90 seconds, up to <see cref="GuildHelp.MaxIncoming"/>.
    /// Deterministic from the clock so it needs no save of its own.
    /// </summary>
    public sealed class SimulatedGuild : IGuildBackend
    {
        public const double PostEvery = 1800;
        public const int OpenSlots = 6;
        public const double IncomingEvery = 90;

        public static readonly string[] Mates = { "Sir Bram", "Lady Wren", "Odo the Bold", "Mira", "Garrick", "Elsbeth", "Tamsin", "Old Hugh" };
        public static readonly string[] Tasks = { "farm", "lumber", "quarry", "forge", "mine", "hero" };

        public IReadOnlyList<GuildRequest> OpenRequests(double now)
        {
            var list = new List<GuildRequest>();
            var slot = (long)Math.Floor(now / PostEvery);
            for (var i = 0; i < OpenSlots; i++)
            {
                var s = slot - i;
                var age = now - (s * PostEvery);
                var max = 8 + (int)(Mod(s * 3, 5));
                var helps = Math.Min(max - 1, (int)(age / 600) + (int)Mod(s, 3));
                list.Add(new GuildRequest("sim." + s, Mates[Mod(s, Mates.Length)], Tasks[Mod(s * 7, Tasks.Length)], helps, max));
            }

            return list;
        }

        public bool SendHelp(string requestId, double now)
        {
            foreach (var r in OpenRequests(now))
            {
                if (r.Id == requestId)
                {
                    return true;
                }
            }

            return false;
        }

        public int HelpsReceived(string timerId, double askedAt, double now) =>
            Math.Max(0, Math.Min(GuildHelp.MaxIncoming, (int)Math.Floor((now - askedAt) / IncomingEvery)));

        private static int Mod(long a, int m) => (int)(((a % m) + m) % m);
    }

    /// <summary>
    /// Guild help taps. The player taps HELP on guildmates' requests (small gold reward, capped per day) and asks the guild
    /// to help their own running timers; each help that arrives takes <see cref="HelpSeconds"/> off that timer.
    /// Saved in <see cref="FileName"/>.
    /// </summary>
    public sealed class GuildHelp
    {
        public const string FileName = "guild_help.json";
        public const int MaxIncoming = 10;
        public const int GoldPerHelp = 100;
        public const int RewardedHelpsPerDay = 30;
        public const double MinHelpSeconds = 60;
        public const double HelpFraction = 0.01;

        private readonly IGuildBackend _backend;
        private readonly HashSet<string> _helped = new(StringComparer.Ordinal);
        private readonly Dictionary<string, (double At, int Applied)> _asks = new(StringComparer.Ordinal);
        private long _day = -1;
        private int _dayHelps;

        public GuildHelp(IGuildBackend backend, TimerQueue timers)
        {
            _backend = backend;
            Timers = timers;
        }

        public TimerQueue Timers { get; }
        private double Now => Timers.Clock.Now;

        public event Action? Changed;

        /// <summary>Seconds one guild help takes off a timer: 1% of its length, at least a minute.</summary>
        public static double HelpSeconds(GameTimer t) => Math.Max(MinHelpSeconds, t.TotalSeconds * HelpFraction);

        public IReadOnlyList<GuildRequest> Requests() => _backend.OpenRequests(Now);

        public bool HelpedByMe(string requestId) => _helped.Contains(requestId);

        /// <summary>Requests the player can still help (the red badge).</summary>
        public int Helpable
        {
            get
            {
                var n = 0;
                foreach (var r in Requests())
                {
                    if (!_helped.Contains(r.Id))
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        /// <summary>Rewarded helps left today (UTC day).</summary>
        public int RewardsLeftToday
        {
            get
            {
                RollDay();
                return Math.Max(0, RewardedHelpsPerDay - _dayHelps);
            }
        }

        /// <summary>Help one request. Returns gold granted (0 once today's cap is used up), or -1 if it can't be helped.</summary>
        public int Help(string requestId, Wallet wallet)
        {
            var g = HelpOne(requestId, wallet);
            if (g >= 0)
            {
                Changed?.Invoke();
            }

            return g;
        }

        /// <summary>Help every open request at once. Returns how many were helped and the gold granted.</summary>
        public (int Helped, int Gold) HelpAll(Wallet wallet)
        {
            int n = 0, gold = 0;
            foreach (var r in Requests())
            {
                var g = HelpOne(r.Id, wallet);
                if (g >= 0)
                {
                    n++;
                    gold += g;
                }
            }

            if (n > 0)
            {
                Changed?.Invoke();
            }

            return (n, gold);
        }

        private int HelpOne(string requestId, Wallet wallet)
        {
            if (_helped.Contains(requestId) || !_backend.SendHelp(requestId, Now))
            {
                return -1;
            }

            _helped.Add(requestId);
            RollDay();
            if (_dayHelps >= RewardedHelpsPerDay)
            {
                return 0;
            }

            _dayHelps++;
            wallet.Grant(new Dictionary<string, int> { [SurvIds.ResSoft] = GoldPerHelp });
            return GoldPerHelp;
        }

        /// <summary>The player's running timers that can still ask for help.</summary>
        public bool CanAsk(string timerId)
        {
            var t = Timers.Find(timerId);
            return t != null && !t.IsDone(Now) && !_asks.ContainsKey(timerId);
        }

        public bool Asked(string timerId) => _asks.ContainsKey(timerId);

        /// <summary>Helps received so far on an asked timer (0..MaxIncoming).</summary>
        public int Received(string timerId) => _asks.TryGetValue(timerId, out var a) ? a.Applied : 0;

        /// <summary>Running timers that have not asked yet.</summary>
        public int AskableCount
        {
            get
            {
                var n = 0;
                foreach (var t in Timers.Active)
                {
                    if (CanAsk(t.Id))
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        public bool Ask(string timerId)
        {
            if (!CanAsk(timerId))
            {
                return false;
            }

            _asks[timerId] = (Now, 0);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Ask for help on every running timer. Returns how many asked.</summary>
        public int AskAll()
        {
            var n = 0;
            foreach (var t in new List<GameTimer>(Timers.Active))
            {
                if (CanAsk(t.Id))
                {
                    _asks[t.Id] = (Now, 0);
                    n++;
                }
            }

            if (n > 0)
            {
                Changed?.Invoke();
            }

            return n;
        }

        /// <summary>Apply guild helps that have arrived to the player's timers. Returns how many helps landed.</summary>
        public int Tick()
        {
            var landed = 0;
            var changed = false;
            foreach (var id in new List<string>(_asks.Keys))
            {
                var a = _asks[id];
                var t = Timers.Find(id);
                if (t == null || t.IsDone(Now))
                {
                    _asks.Remove(id);
                    changed = true;
                    continue;
                }

                var got = Math.Min(MaxIncoming, _backend.HelpsReceived(id, a.At, Now));
                if (got <= a.Applied)
                {
                    continue;
                }

                var each = HelpSeconds(t);
                for (var i = a.Applied; i < got && !t.IsDone(Now); i++)
                {
                    t.Shorten(each);
                    landed++;
                }

                _asks[id] = (a.At, got);
                changed = true;
            }

            PruneHelped();
            if (changed)
            {
                Changed?.Invoke();
            }

            return landed;
        }

        private void PruneHelped()
        {
            if (_helped.Count <= 32)
            {
                return;
            }

            var open = new HashSet<string>(StringComparer.Ordinal);
            foreach (var r in Requests())
            {
                open.Add(r.Id);
            }

            _helped.IntersectWith(open);
        }

        private void RollDay()
        {
            var d = (long)Math.Floor(Now / 86400);
            if (d != _day)
            {
                _day = d;
                _dayHelps = 0;
            }
        }

        public string Save()
        {
            var w = new JsonWriter();
            w.Begin();
            w.Field("version", 1);
            w.Field("day", (double)_day);
            w.Field("day_helps", _dayHelps);
            w.Key("helped");
            w.BeginArray();
            var ids = new List<string>(_helped);
            ids.Sort(StringComparer.Ordinal);
            foreach (var id in ids)
            {
                w.Value(id);
            }

            w.EndArray();
            w.Key("asks");
            w.BeginArray();
            var keys = new List<string>(_asks.Keys);
            keys.Sort(StringComparer.Ordinal);
            foreach (var k in keys)
            {
                w.Begin();
                w.Field("timer", k);
                w.Field("at", _asks[k].At);
                w.Field("applied", _asks[k].Applied);
                w.End();
            }

            w.EndArray();
            w.End();
            return w.ToString();
        }

        public bool Load(string json)
        {
            try
            {
                if (JsonRead.Parse(json) is not JsonNode.Obj o)
                {
                    return false;
                }

                _helped.Clear();
                _asks.Clear();
                _day = (long)CampaignCatalog.Num(o, "day", -1);
                _dayHelps = CampaignCatalog.Int(o, "day_helps", 0);
                foreach (var n in CampaignCatalog.Items(o, "helped"))
                {
                    if (n is JsonNode.Str s)
                    {
                        _helped.Add(s.Value);
                    }
                }

                foreach (var n in CampaignCatalog.Items(o, "asks"))
                {
                    if (n is JsonNode.Obj a)
                    {
                        var id = CampaignCatalog.Str(a, "timer", "");
                        if (id.Length > 0)
                        {
                            _asks[id] = (CampaignCatalog.Num(a, "at", 0), Math.Max(0, CampaignCatalog.Int(a, "applied", 0)));
                        }
                    }
                }

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
