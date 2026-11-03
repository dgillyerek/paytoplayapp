#nullable enable
using Survival.Domain.Roster;
using Survival.Domain.Runner;
using Survival.Domain.Theme;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// The boss's slam swings, shown (Derek 2026-11-03: "if he swings his arms twice both should cause damage"). The run
    /// picks each slam's move from his rotation (<see cref="RunnerFoe.SlamSwing"/>) and the rig plays that exact clip, so
    /// every claw that lands in the sim is a claw on screen; each <see cref="RunnerEventKind.BossSwingHit"/> gets his claw
    /// impact: sparks and dust where it struck (a small shake), or just dust on the road when the hero had stepped out.
    /// </summary>
    public sealed partial class RunnerLevelView
    {
        /// <summary>The boss's slam take: the rotation move the run picked, from its first frame. False when he has none.</summary>
        private bool BossSlamTake(int foeId, RunnerRig rig)
        {
            var boss = _run?.Foe(foeId);
            if (boss == null || !boss.Boss || boss.SlamSwing < 0 || !rig.RotatesAttacks)
            {
                return false;
            }

            var move = LockedBattleRotation.AttackAt(rig.Data, boss.SlamSwing % LockedBattleRotation.Count(rig.Data));
            rig.Swing(move.PoseName, move.Seconds);
            return true;
        }

        private void BossSwingLanded(RunnerEvent e)
        {
            var at = new Vector3(e.X, e.Big ? 0.9f : 0.2f, e.Z);
            var dust = new Color(0.8f, 0.7f, 0.6f, 0.85f);
            AddFx(HookArtKeys.FxBurstRing, new Vector3(e.X, 0.2f, e.Z), e.Big ? 1.6f : 1.2f, 0.35f, dust, Vector3.zero, 1.6f, 0f);
            if (e.Big)
            {
                Sparks(at, new Color(1f, 0.6f, 0.3f, 1f), 8, 1.2f);
                Shake(0.25f);
            }
        }
    }
}
