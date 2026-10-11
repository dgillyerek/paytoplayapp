using System;
using System.Collections.Generic;
using Survival.Domain.Enemies;
using Survival.Domain.Heroes;

namespace Survival.Domain.Roster
{
    /// <summary>Which side of the locked battle a fighter is on.</summary>
    public enum BattleSide
    {
        Hero = 0,
        Villain = 1
    }

    /// <summary>
    /// One locked, merged character in the battle demo. Sizes and speeds are Design metres,
    /// measured from the locked rest and walk FBXs on main. Pose names are the ones each
    /// character's own actor already plays, so the battle reuses the locked clips as-is.
    /// </summary>
    public readonly struct LockedBattleFighter
    {
        public LockedBattleFighter(
            string name,
            BattleSide side,
            string mergedPr,
            string walkPoseName,
            string attackPoseName,
            string attackLabel,
            float designHeightMetres,
            float walkSpeedMetresPerSecond,
            float walkRootStrideMetres,
            string strideBone,
            float attackRangeMetres,
            float attackSeconds,
            bool attackCyclesItself,
            int lane)
        {
            Name = name;
            Side = side;
            MergedPr = mergedPr;
            WalkPoseName = walkPoseName;
            AttackPoseName = attackPoseName;
            AttackLabel = attackLabel;
            DesignHeightMetres = designHeightMetres;
            WalkSpeedMetresPerSecond = walkSpeedMetresPerSecond;
            WalkRootStrideMetres = walkRootStrideMetres;
            StrideBone = strideBone;
            AttackRangeMetres = attackRangeMetres;
            AttackSeconds = attackSeconds;
            AttackCyclesItself = attackCyclesItself;
            Lane = lane;
        }

        public string Name { get; }
        public BattleSide Side { get; }

        /// <summary>The merged PR or commit on main that carries Derek's Game-view PASS.</summary>
        public string MergedPr { get; }

        public string RestPoseName => BlenderRigSpec.RestPoseName;
        public string WalkPoseName { get; }
        public string AttackPoseName { get; }
        public string AttackLabel { get; }

        /// <summary>Rest-pose mesh height in Design metres. The battle scales each body to this.</summary>
        public float DesignHeightMetres { get; }

        /// <summary>Ground speed for in-place walk clips, from the locked clip's foot travel.</summary>
        public float WalkSpeedMetresPerSecond { get; }

        /// <summary>
        /// Forward distance the walk clip itself carries the body each loop. Zero for in-place clips.
        /// Ironhowl's Mixamo walk moves its hips this far and snaps back on every loop.
        /// </summary>
        public float WalkRootStrideMetres { get; }

        /// <summary>Bone that carries the walk's own travel. Empty for in-place clips.</summary>
        public string StrideBone { get; }

        /// <summary>Distance between the two bodies' origins at which this fighter stops and attacks.</summary>
        public float AttackRangeMetres { get; }

        /// <summary>Length of one attack clip.</summary>
        public float AttackSeconds { get; }

        /// <summary>
        /// True when the actor already plays attack once, holds rest for
        /// <see cref="BlenderRigAttackSpec.RestGapSeconds"/>, and repeats (Theme A attack add-on).
        /// False when the battle has to put rest between attacks itself.
        /// </summary>
        public bool AttackCyclesItself { get; }

        /// <summary>0 = left lane, 1 = right lane, seen from behind the heroes.</summary>
        public int Lane { get; }

        public bool WalkCarriesRoot => WalkRootStrideMetres > 0f;

        public float AttackCycleSeconds => AttackSeconds + BlenderRigAttackSpec.RestGapSeconds;
    }

    /// <summary>
    /// Locked battle demo roster: characters on main whose animations are locked with a
    /// Derek Game-view PASS and that have both a walk and an attack clip. Ashwyrm qualifies
    /// since #56 but is held out until Derek picks its lane (see <see cref="LeftOut"/>).
    /// Heroes come from Runtime/Heroes, villains from Runtime/Enemies (Theme A: heroes of the
    /// kingdom against the darkness). Pure presentation, no combat rules.
    /// </summary>
    public static class LockedBattleRoster
    {
        public const string SceneFileName = "BattleLocked.unity";
        public const string ScenePath = "Assets/Survival/Scenes/" + SceneFileName;
        public const string MenuPath = "Survival/Locked Battle (heroes vs villains, Game view 1080x1920)";
        public const string Title = "LOCKED BATTLE";

        /// <summary>Heroes start at -Z facing +Z. Villains start at +Z facing -Z.</summary>
        public const float StartHalfGapMetres = 2.5f;

        /// <summary>Lane centres sit this far either side of the battle centre line.</summary>
        public const float LaneHalfSpacingMetres = 1.15f;

        /// <summary>Everyone holds rest this long before walking in.</summary>
        public const float OpeningHoldSeconds = 0.75f;

        public static readonly LockedBattleFighter Emberfang = new LockedBattleFighter(
            "Emberfang",
            BattleSide.Hero,
            "#49",
            EmberfangMotion.WalkPoseName,
            EmberfangMotion.AttackPoseName,
            "fire breath fireball",
            0.976f,
            0.33f,
            0f,
            string.Empty,
            3.2f,
            BlenderRigAttackSpec.ClipSeconds,
            true,
            0);

        public static readonly LockedBattleFighter Stormcrest = new LockedBattleFighter(
            "Stormcrest",
            BattleSide.Hero,
            "#53",
            StormcrestMotion.WalkPoseName,
            StormcrestMotion.AttackPoseName,
            "wing buffet storm bolt",
            1.158f,
            0.20f,
            0f,
            string.Empty,
            3.0f,
            BlenderRigAttackSpec.ClipSeconds,
            true,
            1);

        public static readonly LockedBattleFighter Ironhowl = new LockedBattleFighter(
            "Ironhowl",
            BattleSide.Villain,
            "ccd19f8",
            IronhowlMotion.WalkPoseName,
            IronhowlMotion.AttackPoseName,
            "claw swipe",
            1.900f,
            0f,
            1.917f,
            IronhowlMotion.BoneRoot,
            1.9f,
            IronhowlMotion.AttackSeconds,
            false,
            0);

        public static readonly LockedBattleFighter Nightfang = new LockedBattleFighter(
            "Nightfang",
            BattleSide.Villain,
            "#52",
            NightfangMotion.TrotPoseName,
            NightfangMotion.AttackPoseName,
            "lunge bite + claw rake",
            1.090f,
            0.45f,
            0f,
            string.Empty,
            2.1f,
            BlenderRigAttackSpec.ClipSeconds,
            true,
            1);

        public static IReadOnlyList<LockedBattleFighter> All { get; } = new[]
        {
            Emberfang,
            Stormcrest,
            Ironhowl,
            Nightfang
        };

        /// <summary>
        /// On main but not in the battle, with the reason. Unmerged PRs are not listed.
        /// </summary>
        public static IReadOnlyList<KeyValuePair<string, string>> LeftOut { get; } = new[]
        {
            new KeyValuePair<string, string>("Ashwyrm", "merged with walk and attack (#56), not placed yet: waiting on Derek's lane decision."),
            new KeyValuePair<string, string>("Blightroot", "creature pack is still HOLD, no Game-view PASS."),
            new KeyValuePair<string, string>("Sir Aldric", "walk and attack are not Design or Game-view PASS yet.")
        };

        public static IEnumerable<LockedBattleFighter> OnSide(BattleSide side)
        {
            foreach (var fighter in All)
            {
                if (fighter.Side == side)
                {
                    yield return fighter;
                }
            }
        }

        /// <summary>Start point on the ground plane (x, z). Heroes at -Z, villains at +Z.</summary>
        public static void StartPoint(LockedBattleFighter fighter, out float x, out float z)
        {
            x = fighter.Lane == 0 ? -LaneHalfSpacingMetres : LaneHalfSpacingMetres;
            z = fighter.Side == BattleSide.Hero ? -StartHalfGapMetres : StartHalfGapMetres;
        }

        /// <summary>Start yaw in degrees. Every locked rig faces +Z at yaw 0.</summary>
        public static float StartYawDegrees(LockedBattleFighter fighter)
        {
            return fighter.Side == BattleSide.Hero ? 0f : 180f;
        }

        /// <summary>
        /// Opponent each fighter locks onto for the whole fight: the nearest one at the start,
        /// ties going to the same lane.
        /// </summary>
        public static LockedBattleFighter TargetOf(LockedBattleFighter fighter)
        {
            StartPoint(fighter, out var fx, out var fz);
            var found = false;
            var best = fighter;
            var bestDistance = float.MaxValue;
            foreach (var other in All)
            {
                if (other.Side == fighter.Side)
                {
                    continue;
                }

                StartPoint(other, out var ox, out var oz);
                var d = ((ox - fx) * (ox - fx)) + ((oz - fz) * (oz - fz));
                var better = d < bestDistance - 1e-4f
                    || (Math.Abs(d - bestDistance) <= 1e-4f && other.Lane == fighter.Lane);
                if (!found || better)
                {
                    found = true;
                    best = other;
                    bestDistance = d;
                }
            }

            return best;
        }

        /// <summary>
        /// How far to move toward the target this frame. Stops exactly at attack range and never backs off.
        /// </summary>
        public static float ApproachStep(float distance, float attackRange, float speed, float deltaSeconds)
        {
            var room = distance - attackRange;
            if (room <= 0f || speed <= 0f || deltaSeconds <= 0f)
            {
                return 0f;
            }

            return Math.Min(room, speed * deltaSeconds);
        }

        /// <summary>
        /// For a walk clip that carries its own root forward: how far the body jumped back when the
        /// clip looped this frame (forward offsets in metres). Zero when it did not loop.
        /// </summary>
        public static float LoopSnapBack(float previousForward, float forward, float stride)
        {
            if (stride <= 0f)
            {
                return 0f;
            }

            var drop = previousForward - forward;
            return drop > stride * 0.5f ? drop : 0f;
        }

        /// <summary>
        /// For actors whose attack clip just loops: true while the attack clip should play,
        /// false during the rest gap that follows it.
        /// </summary>
        public static bool InAttackClip(LockedBattleFighter fighter, float secondsSinceFirstAttack)
        {
            if (secondsSinceFirstAttack < 0f)
            {
                return false;
            }

            var cycle = fighter.AttackCycleSeconds;
            if (cycle <= 0f)
            {
                return true;
            }

            var t = secondsSinceFirstAttack % cycle;
            return t < fighter.AttackSeconds;
        }
    }
}
