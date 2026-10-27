#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Roster;
using Survival.Domain.Runner;

namespace Survival.Domain.Audio
{
    /// <summary>
    /// Which cues a game moment plays, read from the theme's <see cref="ThemeAudioConfig"/>. Presentation only:
    /// nothing here feeds back into the runner, the battle or the economy.
    /// </summary>
    public static class AudioEventMap
    {
        /// <summary>Runner events that are deliberately silent (spawns, misses, the take wind-up before a Fired).</summary>
        public static readonly RunnerEventKind[] SilentRunnerEvents =
        {
            RunnerEventKind.FoeSpawned,
            RunnerEventKind.BoxSpawned,
            RunnerEventKind.FoePassed,
            RunnerEventKind.BoxMissed,
            RunnerEventKind.HazardSpawned,
            RunnerEventKind.BarrierSpawned,
            RunnerEventKind.AttackStarted
        };

        private static readonly IReadOnlyList<string> None = Array.Empty<string>();

        public static IReadOnlyList<string> Runner(ThemeAudioConfig config, RunnerEvent e)
        {
            switch (e.Kind)
            {
                case RunnerEventKind.Fired:
                    return config.RunnerShots.TryGetValue(e.Unit ?? "", out var shot) ? new[] { shot } : None;
                case RunnerEventKind.HazardHit:
                    return config.RunnerHazards.TryGetValue(e.Unit ?? "", out var trap) ? new[] { trap } : None;
                case RunnerEventKind.Hit when e.OnBox:
                    return Lookup(config, "Hit.box", "Hit");
                case RunnerEventKind.FoeKilled when e.Big:
                    return Lookup(config, "FoeKilled.big", "FoeKilled");
                case RunnerEventKind.BarrierHit:
                case RunnerEventKind.BarrierBroken:
                    // Unit = the barrier kind (palisade / stone_wall / barricade): each material has its own crack and crash.
                    return Lookup(config, e.Kind + "." + (e.Unit ?? ""), e.Kind.ToString());
                default:
                    return Lookup(config, e.Kind.ToString(), "");
            }
        }

        /// <summary>Cue when <paramref name="fighterName"/>'s shot or spell leaves its hand ("" for melee fighters).</summary>
        public static string BattleLaunch(ThemeAudioConfig config, string fighterName) =>
            config.BattleShots.TryGetValue(fighterName ?? "", out var cue) ? cue : "";

        /// <summary>Cue where a battle hit lands: a boss attack by its label, otherwise by the hit's impact kind.</summary>
        public static string BattleImpact(ThemeAudioConfig config, string attackerName, string attackLabel, BattleImpactKind kind)
        {
            if (IsBoss(config, attackerName) && config.BossAttacks.TryGetValue(attackLabel ?? "", out var boss))
            {
                return boss;
            }

            return config.BattleImpacts.TryGetValue(kind.ToString(), out var cue) ? cue : "";
        }

        public static string BattleDefeat(ThemeAudioConfig config, string fighterName, bool isHero)
        {
            if (isHero)
            {
                return AudioCues.HeroDeath;
            }

            return IsBoss(config, fighterName) ? AudioCues.EnemyDeathBig : AudioCues.EnemyDeath;
        }

        public static bool IsBoss(ThemeAudioConfig config, string fighterName)
        {
            foreach (var b in config.BattleBosses)
            {
                if (string.Equals(b, fighterName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Collect cue for a building that produces <paramref name="resourceId"/>.</summary>
        public static string Collect(ThemeAudioConfig config, string resourceId) =>
            config.EconomyCollect.TryGetValue(resourceId ?? "", out var cue) ? cue : AudioCues.CollectResource;

        private static IReadOnlyList<string> Lookup(ThemeAudioConfig config, string key, string fallback)
        {
            if (config.RunnerEvents.TryGetValue(key, out var list))
            {
                return list;
            }

            return fallback.Length > 0 && config.RunnerEvents.TryGetValue(fallback, out var f) ? f : None;
        }
    }
}
