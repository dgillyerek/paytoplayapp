#nullable enable
using System.Collections.Generic;

namespace Survival.Domain.Runner
{
    /// <summary>
    /// Followers summed by unit type for the runner's HP panel (Derek 2026-10-28: "new rows with the cumulative health of
    /// the secondary units split up by unit type"). Heroes have their own rows and are left out.
    /// </summary>
    public static class RunnerSquadHp
    {
        public readonly struct TypeHp
        {
            public readonly string Roster;
            public readonly int Count, Hp, MaxHp;

            public TypeHp(string roster, int count, int hp, int maxHp)
            {
                Roster = roster;
                Count = count;
                Hp = hp;
                MaxHp = maxHp;
            }

            public float Fraction => MaxHp > 0 ? System.Math.Max(0f, Hp / (float)MaxHp) : 0f;
        }

        /// <summary>
        /// Living followers grouped by roster, in the order each type first appears in <paramref name="squad"/>: count,
        /// summed current HP and summed max HP. Fills <paramref name="into"/> (cleared first) so a per-frame caller allocates nothing.
        /// </summary>
        public static void ByType(IReadOnlyList<RunnerMember> squad, List<TypeHp> into)
        {
            into.Clear();
            for (var k = 0; k < squad.Count; k++)
            {
                var m = squad[k];
                if (m.IsHero || m.Hp <= 0)
                {
                    continue;
                }

                Accumulate(into, m.Unit.Roster, m.Hp, m.MaxHp);
            }
        }

        /// <summary>Adds one living follower of <paramref name="roster"/> to its type's sum (a new type goes last).</summary>
        public static void Accumulate(List<TypeHp> into, string roster, int hp, int maxHp)
        {
            for (var i = 0; i < into.Count; i++)
            {
                var t = into[i];
                if (t.Roster == roster)
                {
                    into[i] = new TypeHp(roster, t.Count + 1, t.Hp + hp, t.MaxHp + maxHp);
                    return;
                }
            }

            into.Add(new TypeHp(roster, 1, hp, maxHp));
        }
    }
}
