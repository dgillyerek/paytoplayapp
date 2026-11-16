using System;
using System.Collections.Generic;
using Survival.Domain.Enemies;
using Survival.Domain.Heroes;

namespace Survival.Domain.Roster
{
    /// <summary>
    /// Everything a plain Blender-rig fighter needs to spawn: its motion spec, attack take, idle props and spawn yaw.
    /// One row per fighter that plays through the shared <c>BlenderRigActor</c> (the battle and the runner both read it).
    /// Fighters with their own actor (Sir Aldric, Blightroot, Emberfang, Ironhowl, Vorgath, Rowan, Bonequill) are not here.
    /// </summary>
    public sealed class BlenderRigCast
    {
        public BlenderRigCast(
            BlenderRigSpec motion,
            BlenderRigAttackSpec attack,
            BlenderRigIdlePropSpec[]? idleProps = null,
            float facingYawDegrees = 0f,
            bool runnerPacedAttack = true)
        {
            Motion = motion;
            Attack = attack ?? throw new ArgumentNullException(nameof(attack));
            IdleProps = idleProps;
            FacingYawDegrees = facingYawDegrees;
            RunnerPacedAttack = runnerPacedAttack;
        }

        public BlenderRigSpec Motion { get; }

        public BlenderRigAttackSpec Attack { get; }

        public BlenderRigIdlePropSpec[]? IdleProps { get; }

        /// <summary>Spawn yaw on the actor transform, set before the build so bounds and attack calibration use it.</summary>
        public float FacingYawDegrees { get; }

        /// <summary>The runner paces this fighter's attack take (one take per shot). Stormcrest keeps its natural rate.</summary>
        public bool RunnerPacedAttack { get; }

        /// <summary>The Unity actor class every row spawns.</summary>
        public const string ActorClassName = "BlenderRigActor";

        // Names only (no spec references), so the roster can ask during its own static initialisation.
        private static readonly string[] CastNames =
        {
            FighterNames.Lyra, FighterNames.Oakenshield, FighterNames.Stormcrest,
            FighterNames.Vespera, FighterNames.Nightfang, FighterNames.Ashwyrm,
        };

        /// <summary>True when <paramref name="rosterName"/> plays through the shared actor.</summary>
        public static bool IsCast(string rosterName) => Array.IndexOf(CastNames, rosterName) >= 0;

        /// <summary>Roster name (same as the motion spec's name).</summary>
        public string Name => Motion.Name;

        public static IReadOnlyList<BlenderRigCast> All { get; } = new[]
        {
            new BlenderRigCast(LyraMotion.Spec, LyraAttack.Spec, LyraMotion.IdleProps, LyraMotion.FacingYawDegrees),
            new BlenderRigCast(OakenshieldMotion.Spec, OakenshieldAttack.Spec),
            new BlenderRigCast(StormcrestMotion.Spec, StormcrestAttack.Spec, runnerPacedAttack: false),
            new BlenderRigCast(VesperaMotion.Spec, VesperaAttack.Spec),
            new BlenderRigCast(NightfangMotion.Spec, NightfangAttack.Spec),
            new BlenderRigCast(AshwyrmMotion.Spec, AshwyrmAttack.Spec),
        };

        public static bool TryFind(string rosterName, out BlenderRigCast cast)
        {
            foreach (var c in All)
            {
                if (string.Equals(c.Name, rosterName, StringComparison.Ordinal))
                {
                    cast = c;
                    return true;
                }
            }

            cast = null!;
            return false;
        }
    }
}
