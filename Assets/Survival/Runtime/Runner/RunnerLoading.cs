#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Campaign;

namespace Survival.Domain.Runner
{
    /// <summary>
    /// What the shared painted loading screen (#76's BattleLoadingScreen) shows on the Play ⇄ runner round trip.
    /// Going in, it shows the level name, the hero versus the level's enemies (first appearance order, boss last).
    /// Coming back, it shows how the last run went.
    /// </summary>
    public static class RunnerLoading
    {
        /// <summary>The loading screen shows at most this many faces per side.</summary>
        public const int MaxFaces = 5;

        public const int TipCount = 4;

        public const string IntoKey = "runner.loading.into";
        public const string BackKey = "runner.loading.back";
        public const string VictoryKey = "runner.loading.victory";
        public const string DefeatKey = "runner.loading.defeat";
        public const string TipPrefix = "runner.tip.";

        /// <summary>Into the runner: the level's hero against its enemies, the boss always shown (last).</summary>
        public static BattleLoadingInfo Into(RunnerCatalog catalog, RunnerLevelDef level, string levelName, string headline, string tip)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            var heroes = new List<string> { RunnerArtKeys.Portrait(catalog.Unit(level.Hero).Roster) };
            var foes = new List<string>();
            string? boss = null;
            foreach (var r in level.Rows)
            {
                if (r.Kind == RunnerRowKind.Boss)
                {
                    boss = RunnerArtKeys.Portrait(catalog.Unit(r.Unit).Roster);
                }
                else if (r.Kind == RunnerRowKind.Enemy)
                {
                    var key = RunnerArtKeys.Portrait(catalog.Unit(r.Unit).Roster);
                    if (!foes.Contains(key))
                    {
                        foes.Add(key);
                    }
                }
            }

            if (boss != null)
            {
                foes.Remove(boss);
                if (foes.Count >= MaxFaces)
                {
                    foes.RemoveRange(MaxFaces - 1, foes.Count - (MaxFaces - 1));
                }

                foes.Add(boss);
            }
            else if (foes.Count > MaxFaces)
            {
                foes.RemoveRange(MaxFaces, foes.Count - MaxFaces);
            }

            return new BattleLoadingInfo(headline, levelName, heroes, foes, tip, false);
        }

        /// <summary>The string key for the way-back headline: the last run's outcome, or plain "back" with none.</summary>
        public static string BackHeadlineKey(IReadOnlyList<RunnerResult> runs)
        {
            if (runs == null || runs.Count == 0)
            {
                return BackKey;
            }

            return runs[runs.Count - 1].Won ? VictoryKey : DefeatKey;
        }

        /// <summary>Back to the village: no VS row, the headline says how it went.</summary>
        public static BattleLoadingInfo Back(string headline, string levelName, string tip) =>
            BattleLoadingInfo.Back(headline, levelName, tip);
    }
}
