#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Survival.Domain.Runner
{
    /// <summary>
    /// Derek 2026-10-27: "the runner characters shouldn't be available until they've been purchased in the main level". A
    /// level's lead and every chest that hands out a named hero unit only use heroes the player owns in the main game (the
    /// shared save's <see cref="Survival.Domain.Campaign.HeroRoster"/>). A chest naming an unowned hero hands out an owned hero's unit
    /// instead (same count, same HP, same place), so the level keeps its shape; recruiting the hero in the Kingdom brings
    /// its chests back. No selection screen exists in the runner, so locked heroes are simply left out.
    /// </summary>
    public static class RunnerOwnership
    {
        /// <summary>Owned hero units in the order a stand-in is picked (the free starters first).</summary>
        private static readonly string[] StandInOrder = { "rowan", "lyra", "oakenshield" };

        /// <summary>The hero units <paramref name="loadout"/> lets into the squad.</summary>
        public static IReadOnlyList<string> FieldedUnits(RunnerCatalog c, RunnerLoadout loadout) =>
            c.Units.Values.Where(u => u.HeroId.Length > 0 && loadout.Fields(u)).Select(u => u.Id).ToList();

        /// <summary>
        /// <paramref name="level"/> with every unowned hero unit swapped for an owned one. Unchanged when there is no gate,
        /// everything named is owned, or nothing at all is owned (a broken save: keep the shipped default rather than an
        /// empty squad).
        /// </summary>
        public static RunnerLevelDef Gate(RunnerCatalog c, RunnerLevelDef level, RunnerLoadout loadout)
        {
            if (loadout.OwnedHeroes == null)
            {
                return level;
            }

            var owned = FieldedUnits(c, loadout);
            if (owned.Count == 0)
            {
                return level;
            }

            string StandIn(string unitId)
            {
                var u = c.Unit(unitId);
                if (loadout.Fields(u))
                {
                    return unitId;
                }

                foreach (var id in StandInOrder)
                {
                    if (owned.Contains(id) && id != unitId)
                    {
                        return id;
                    }
                }

                return owned[0];
            }

            var changed = false;
            var rows = new List<RunnerRowDef>(level.Rows.Count);
            foreach (var r in level.Rows)
            {
                if (r.Box is { GivesUnits: true } box && box.Unit.Length > 0)
                {
                    var to = StandIn(box.Unit);
                    if (to != box.Unit)
                    {
                        rows.Add(r with { Box = box with { Unit = to } });
                        changed = true;
                        continue;
                    }
                }

                rows.Add(r);
            }

            var hero = StandIn(level.Hero);
            changed |= hero != level.Hero;
            return changed ? level with { Hero = hero, Rows = rows } : level;
        }
    }
}
