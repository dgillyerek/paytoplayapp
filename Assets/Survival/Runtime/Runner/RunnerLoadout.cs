#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Survival.Domain.Campaign;

namespace Survival.Domain.Runner
{
    /// <summary>How much harder one runner unit hits and how much more it takes than its runner_levels.json numbers (1 = untrained).</summary>
    public sealed record RunnerUnitBoost(float DamageMul, float HpMul)
    {
        public static readonly RunnerUnitBoost None = new(1f, 1f);
    }

    /// <summary>
    /// The player's trained heroes carried into a run. Every runner unit names its campaign hero ("hero": "hero.lyra"); the
    /// unit's damage scales with that hero's attack and its HP (the lead's HP and every follower's 25) with that hero's HP,
    /// both relative to the hero at level 1, one star: <c>mul = HeroStats.Of(level, stars) / HeroStats.Of(1, 1)</c>. So a
    /// level 1 one-star roster plays exactly the JSON numbers, and training a hero in the village makes its runner unit
    /// stronger by the same share it makes the battle hero stronger.
    /// </summary>
    public sealed class RunnerLoadout
    {
        public static readonly RunnerLoadout None = new(new Dictionary<string, RunnerUnitBoost>());

        private readonly Dictionary<string, RunnerUnitBoost> _byUnit;

        public RunnerLoadout(IReadOnlyDictionary<string, RunnerUnitBoost> byUnit, IReadOnlyCollection<string>? ownedHeroes = null)
        {
            OwnedHeroes = ownedHeroes == null ? null : new HashSet<string>(ownedHeroes, StringComparer.Ordinal);
            _byUnit = new Dictionary<string, RunnerUnitBoost>(StringComparer.Ordinal);
            foreach (var pair in byUnit)
            {
                _byUnit[pair.Key] = pair.Value;
            }
        }

        public IReadOnlyDictionary<string, RunnerUnitBoost> ByUnit => _byUnit;

        /// <summary>
        /// Campaign heroes the player owns in the main game (Derek 2026-10-27: "the runner characters shouldn't be available
        /// until they've been purchased in the main level"). Null: no gate (tests, the balance sim's scale probes).
        /// </summary>
        public IReadOnlyCollection<string>? OwnedHeroes { get; }

        /// <summary>True when <paramref name="u"/> may appear in the squad: not a hero unit, no gate, or its hero is owned.</summary>
        public bool Fields(RunnerUnitDef u) => u.HeroId.Length == 0 || OwnedHeroes == null || OwnedHeroes.Contains(u.HeroId);

        /// <summary>This loadout's boosts, gated on <paramref name="ownedHeroes"/>.</summary>
        public RunnerLoadout OwningOnly(IReadOnlyCollection<string>? ownedHeroes) => new(_byUnit, ownedHeroes);

        public RunnerUnitBoost For(string unitId) => _byUnit.TryGetValue(unitId, out var b) ? b : RunnerUnitBoost.None;

        /// <summary>Damage / HP multipliers of <paramref name="def"/> at <paramref name="level"/> and <paramref name="stars"/> vs level 1, one star.</summary>
        public static RunnerUnitBoost Boost(CampaignHeroDef def, LevelCurve curve, int level, int stars)
        {
            var at = HeroStats.Of(def, curve, Math.Max(1, level), Math.Max(1, stars));
            var one = HeroStats.Of(def, curve, 1, 1);
            return new RunnerUnitBoost(one.Atk > 0 ? at.Atk / (float)one.Atk : 1f, one.Hp > 0 ? at.Hp / (float)one.Hp : 1f);
        }

        /// <summary>
        /// The loadout for <paramref name="levelAndStars"/> (campaign hero id to (level, stars); a hero it doesn't name counts
        /// as level 1, one star).
        /// </summary>
        public static RunnerLoadout From(RunnerCatalog runner, CampaignCatalog campaign, Func<string, (int Level, int Stars)?> levelAndStars)
        {
            var map = new Dictionary<string, RunnerUnitBoost>(StringComparer.Ordinal);
            foreach (var u in runner.Units.Values)
            {
                if (u.HeroId.Length == 0)
                {
                    continue;
                }

                var def = campaign.Hero(u.HeroId);
                var ls = levelAndStars(u.HeroId);
                if (def != null && ls != null)
                {
                    map[u.Id] = Boost(def, campaign.Curve, ls.Value.Level, ls.Value.Stars);
                }
            }

            return new RunnerLoadout(map);
        }

        /// <summary>
        /// This loadout with every hero unit's damage x <paramref name="damageMul"/> and HP x <paramref name="hpMul"/> (a War
        /// Banner charge, power_packs.json runner_boost). Units without a hero are not boosted.
        /// </summary>
        public RunnerLoadout Boosted(RunnerCatalog runner, double damageMul, double hpMul)
        {
            var map = new Dictionary<string, RunnerUnitBoost>(StringComparer.Ordinal);
            foreach (var u in runner.Units.Values)
            {
                if (u.HeroId.Length == 0)
                {
                    continue;
                }

                var b = For(u.Id);
                map[u.Id] = new RunnerUnitBoost((float)(b.DamageMul * damageMul), (float)(b.HpMul * hpMul));
            }

            return new RunnerLoadout(map, OwnedHeroes);
        }

        /// <summary>The loadout of the heroes in <paramref name="roster"/>, gated on the ones it owns (an unowned hero never fields).</summary>
        public static RunnerLoadout From(RunnerCatalog runner, HeroRoster roster) =>
            From(runner, roster.Catalog, id => roster.Get(id) is { } p ? (p.Level, p.Stars) : null).OwningOnly(roster.OwnedIds().ToList());
    }
}
