#nullable enable
using System;
using System.Collections.Generic;

namespace Survival.Domain.Alliance
{
    /// <summary>One simulated guildmate on the Alliance screen.</summary>
    public sealed record Guildmate(string Name, string Role, int Power, bool Online);

    /// <summary>
    /// The Alliance screen's guild (Derek 2026-10-31: "these buttons do nothing"): a name, a level and guildmates simulated
    /// on this device until a guild server exists. Deterministic from a seed so the list is the same every launch. The
    /// Help tab is where guild help (PR #115, Survival.Domain.Guild) plugs in.
    /// </summary>
    public sealed class AllianceOverview
    {
        public const string TabMembers = "members";
        public const string TabHelp = "help";

        private static readonly string[] FirstNames =
        {
            "Aldwin", "Brenna", "Cedric", "Daria", "Eldon", "Fiona", "Garrick", "Helena", "Ivo", "Jessa", "Kael", "Lorna",
        };

        private static readonly string[] Titles = { "the Bold", "Ironhand", "of Ashvale", "Swift", "the Wise", "Stormborn" };

        public AllianceOverview(string guildName, int level, IReadOnlyList<Guildmate> members)
        {
            GuildName = guildName;
            Level = level;
            Members = members;
        }

        public string GuildName { get; }
        public int Level { get; }
        public IReadOnlyList<Guildmate> Members { get; }
        public int OnlineCount
        {
            get
            {
                var n = 0;
                foreach (var m in Members)
                {
                    n += m.Online ? 1 : 0;
                }

                return n;
            }
        }

        /// <summary>The on-device guild: a leader, two officers and members, strongest first. Same seed, same guild.</summary>
        public static AllianceOverview Simulated(int seed = 7, int count = 10, string guildName = "Knights of the Ember Crown")
        {
            var rng = new Random(seed);
            var list = new List<Guildmate>();
            for (var i = 0; i < count; i++)
            {
                var name = FirstNames[(i + seed) % FirstNames.Length] + " " + Titles[rng.Next(Titles.Length)];
                var role = i == 0 ? "leader" : i <= 2 ? "officer" : "member";
                var power = 48000 - (i * 3600) + rng.Next(0, 2500);
                list.Add(new Guildmate(name, role, Math.Max(1000, power), rng.Next(100) < 55));
            }

            list.Sort((a, b) => b.Power.CompareTo(a.Power));
            return new AllianceOverview(guildName, 4, list);
        }
    }
}
