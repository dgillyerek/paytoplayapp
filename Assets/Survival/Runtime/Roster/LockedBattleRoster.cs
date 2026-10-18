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
            int lane,
            string actorClassName = "",
            float aimYawDegrees = 0f,
            IReadOnlyList<LockedBattleAttack>? attacks = null)
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
            AimYawDegrees = aimYawDegrees;
            ActorClassName = actorClassName.Length > 0 ? actorClassName : FileKeyOf(name) + "Actor";
            Attacks = attacks != null && attacks.Count > 0
                ? attacks
                : new[] { new LockedBattleAttack(attackLabel, attackPoseName, attackSeconds) };
        }

        /// <summary>
        /// Attack rotation (<see cref="LockedBattleRotation"/>). Attack 1 is the locked attack
        /// (<see cref="AttackPoseName"/>, <see cref="AttackSeconds"/>); humanoids add 2 and 3 as
        /// their premade Mixamo clips land. Creatures (on hold) keep their single attack.
        /// </summary>
        public IReadOnlyList<LockedBattleAttack> Attacks { get; }

        /// <summary>Name without spaces, as used by the Motion, Actor and scene file names ("Sir Aldric" → "SirAldric").</summary>
        public string FileKey => FileKeyOf(Name);

        /// <summary>
        /// Unity component the battle adds for this fighter. "{FileKey}Actor" for everyone except
        /// Sir Aldric, whose locked actor is SirAldricMeshyAnimateActor.
        /// </summary>
        public string ActorClassName { get; }

        /// <summary>
        /// Extra yaw (degrees, + turns toward +X) on top of facing the target while attacking. Zero
        /// for everyone but Rowan, whose locked arrow flies 15.25 deg left of his forward from a
        /// release point right of his centre; 12.33 deg puts that line through Bonequill's centre at
        /// the 4.2 m shooting range.
        /// </summary>
        public float AimYawDegrees { get; }

        private static string FileKeyOf(string name) => name.Replace(" ", string.Empty);

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

        /// <summary>
        /// Seen from behind the heroes: 0 = left inner lane, 1 = right inner lane (the original
        /// #57 matchups), 2 = left outer lane, 3 = right outer lane, 4 = fifth lane, outside lane 2
        /// on the left (Sir Aldric vs Blightroot, added 2026-10-15), 5 = sixth lane, outside lane 3
        /// on the right (Rowan vs Bonequill archers, added 2026-10-16).
        /// </summary>
        public int Lane { get; }

        public bool WalkCarriesRoot => WalkRootStrideMetres > 0f;

        public float AttackCycleSeconds => AttackSeconds + BlenderRigAttackSpec.RestGapSeconds;
    }

    /// <summary>Sir Aldric's battle poses on SirAldricMeshyAnimateActor.</summary>
    public static class SirAldricBattlePoses
    {
        public const string Rest = "rest";
        public const string Walk = "walk";
        public const string Attack = "attack";

        /// <summary>Lite pack clip played for rest (loops).</summary>
        public const string RestClip = "Sword And Shield idle";

        /// <summary>Lite pack clip played for attack: in place, frames 0-32 move, then it holds the idle pose.</summary>
        public const string AttackClip = "Sword And Shield attack (4)";

        /// <summary>Last moving frame of <see cref="AttackClip"/> (it holds the idle pose to frame 100).</summary>
        public const int AttackLastFrame = 32;

        public const float AttackSeconds = (AttackLastFrame + 1) / 30f;
    }

    /// <summary>Blightroot's battle poses on BlightrootActor (exact creature-pack Mixamo names).</summary>
    public static class BlightrootBattlePoses
    {
        public const string Rest = "rest";
        public const string Walk = "walk";
        public const string Attack = "attack";
        public const string RestClip = "mutant idle";
        public const string WalkClip = "mutant walking";
        public const string AttackClip = "mutant swiping";

        /// <summary>Attack 2 (rotation): creature-pack "mutant punch", a right hook, frames 0-33.</summary>
        public const string Attack2 = "attack2";
        public const string Attack2Clip = "mutant punch";
        public const int Attack2LastFrame = 33;

        /// <summary>
        /// Right fist furthest forward (0.70 m ahead of the start hips) on frame 10, measured on the
        /// creature-pack FBX.
        /// </summary>
        public const int Attack2ContactFrame = 10;

        /// <summary>Attack 3 (rotation): creature-pack "mutant jump attack", a leap and two-claw slam, frames 0-111.</summary>
        public const string Attack3 = "attack3";
        public const string Attack3Clip = "mutant jump attack";
        public const int Attack3LastFrame = 111;

        /// <summary>
        /// Both claws hit the ground (0.2-0.3 m up, 1.9 m ahead of the start hips with the hips
        /// carried 1.3 m) on frame 50.
        /// </summary>
        public const int Attack3ContactFrame = 50;

        public static bool IsAttack(string pose)
        {
            return pose == Attack || pose == Attack2 || pose == Attack3;
        }

        /// <summary>
        /// Rotation: swipe (locked attack 1), punch, jump smash. The punch reaches 0.70 m and the
        /// in-place slam 0.6 m, short of Sir Aldric's front face 1.15 m away at his 1.4 m stop,
        /// so each slides the actor root forward into the strike and back out (the leap's own
        /// 1.3 m carry is held on the spot so Blightroot lands where it took off).
        /// </summary>
        public static readonly LockedBattleAttack[] Attacks =
        {
            new LockedBattleAttack("rot claw swipe", Attack, AttackSeconds),
            new LockedBattleAttack(
                "rot punch",
                Attack2,
                Attack2LastFrame / 30f,
                Attack2ContactFrame,
                "mixamorig:RightHand",
                0.04f,
                false,
                new LockedBattleReachShift(0.45f, 2f, 10f, 14f, 30f)),
            new LockedBattleAttack(
                "rot jump smash",
                Attack3,
                Attack3LastFrame / 30f,
                Attack3ContactFrame,
                "mixamorig:LeftHand",
                0.09f,
                true,
                new LockedBattleReachShift(0.55f, 14f, 49f, 72f, 108f))
        };

        /// <summary>Hips travel of one "mutant walking" loop (measured on the FBX, 1.4526 m).</summary>
        public const float WalkStrideMetres = 1.453f;

        /// <summary>"mutant swiping" runs frames 0-80 in Unity.</summary>
        public const float AttackSeconds = 80f / 30f;

        public static string ClipFor(string pose)
        {
            switch (pose)
            {
                case Walk:
                    return WalkClip;
                case Attack:
                    return AttackClip;
                case Attack2:
                    return Attack2Clip;
                case Attack3:
                    return Attack3Clip;
                default:
                    return RestClip;
            }
        }
    }

    /// <summary>
    /// Locked battle demo roster: characters on main whose animations are locked with a
    /// Derek Game-view PASS and that have both a walk and an attack clip, plus the fifth lane
    /// Derek approved on 2026-10-15 (Sir Aldric vs Blightroot): five heroes against five
    /// villains, one matchup per lane (see <see cref="LeftOut"/> for who is not in).
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

        /// <summary>
        /// Outer lanes (2 and 3) sit this far either side of the centre line: 1.3 m outside the
        /// inner lanes, so the widest added body (Ashwyrm, 1.9 m wingspan) clears its neighbours
        /// and the inner lanes stay exactly where #57 put them.
        /// </summary>
        public const float OuterLaneHalfSpacingMetres = 2.45f;

        /// <summary>
        /// Fifth lane (4) sits this far left of the centre line: 1.4 m outside lane 2. Vespera
        /// (lane 2 villain) is slim, so Blightroot (1.65 m T-pose span, about 1.0 m with arms
        /// down at idle) clears her. The right side is not used because Ashwyrm's 1.9 m wingspan
        /// already reaches 3.4 m out. Lanes 0-3 do not move.
        /// </summary>
        public const float FifthLaneHalfSpacingMetres = 3.85f;

        /// <summary>
        /// Sixth lane (5) sits this far right of the centre line: 1.9 m outside lane 3, because
        /// Ashwyrm (lane 3 villain) spans 1.9 m wing to wing and reaches 3.4 m out. Rowan and
        /// Bonequill are 0.9 m across in A-pose, so their inner edge (3.9 m) clears it.
        /// </summary>
        public const float SixthLaneHalfSpacingMetres = 4.35f;

        /// <summary>Lanes 0-5. The ground lays one dirt track per lane.</summary>
        public const int LaneCount = 6;

        /// <summary>
        /// Archers shoot from here: the starts are 5 m apart, so they take a step or two in and
        /// loose. Both arrows (Rowan 14 m/s, Bonequill 14 m/s) fly well past 4.2 m, so they reach.
        /// </summary>
        public const float ArcherRangeMetres = 4.2f;

        /// <summary>Everyone holds rest this long before walking in.</summary>
        public const float OpeningHoldSeconds = 0.75f;

        /// <summary>
        /// Ironhowl's rotation (2026-10-18; his own clips, not Blightroot's mutant moves): claw swipe
        /// (locked), overhead smash (Mixamo "Standing Melee Attack Downward": purple ground-slam crack
        /// ring and a heavy shake), claw combo (Mixamo "Standing Melee Combo Attack Ver. 2": four hits,
        /// purple claw sparks on each). Both hold the hips' travel in place and slide 0.3 m in.
        /// </summary>
        public static readonly LockedBattleAttack[] IronhowlAttacks =
        {
            new LockedBattleAttack("claw swipe", IronhowlMotion.AttackPoseName, IronhowlMotion.AttackSeconds),
            new LockedBattleAttack(
                "overhead smash",
                IronhowlMotion.Attack2PoseName,
                IronhowlMotion.Attack2LastFrame / IronhowlMotion.FrameRate,
                IronhowlMotion.Attack2ContactFrame,
                "mixamorig:RightHand",
                0.09f,
                true,
                new LockedBattleReachShift(0.3f, 8f, 26f, 40f, 64f),
                BattleImpactKind.GroundSlam),
            new LockedBattleAttack(
                "claw combo",
                IronhowlMotion.Attack3PoseName,
                IronhowlMotion.Attack3LastFrame / IronhowlMotion.FrameRate,
                IronhowlMotion.Attack3HitFrames[0],
                IronhowlMotion.Attack3HitBones[0],
                0.04f,
                true,
                new LockedBattleReachShift(0.3f, 16f, 28f, 100f, 122f),
                BattleImpactKind.ShadowRake,
                new[]
                {
                    new LockedBattleHitMark(IronhowlMotion.Attack3HitFrames[1], IronhowlMotion.Attack3HitBones[1]),
                    new LockedBattleHitMark(IronhowlMotion.Attack3HitFrames[2], IronhowlMotion.Attack3HitBones[2]),
                    new LockedBattleHitMark(IronhowlMotion.Attack3HitFrames[3], IronhowlMotion.Attack3HitBones[3])
                })
        };

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
            0,
            "",
            0f,
            IronhowlAttacks);

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

        // Added lanes (2026-10-14). Walk speeds come from each locked walk clip's foot travel
        // (two strides of the foot's forward sweep per 30-frame loop, the same measure that gives
        // Emberfang 0.33 m/s); heights are the rest-mesh heights of the locked FBXs.

        /// <summary>
        /// Lyra's rotation: arcane bolt (locked, projectile), staff sweep (attack 2) and arcane nova (attack 3)
        /// from the retargeted Mixamo clips. The extra two land on their own contact frames in hero blue: the
        /// sweep throws a blue arc onto the target from range, the nova bursts a blue ring on it.
        /// </summary>
        public static readonly LockedBattleAttack[] LyraAttacks =
        {
            new LockedBattleAttack("staff cast arcane bolt", LyraMotion.AttackPoseName, BlenderRigAttackSpec.ClipSeconds),
            new LockedBattleAttack(
                "staff sweep",
                LyraMotion.Attack2PoseName,
                LyraMotion.Attack2LastFrame / LyraMotion.WalkFrameRate,
                LyraMotion.Attack2ContactFrame,
                "mixamorig:RightHand",
                0.03f,
                false,
                default,
                BattleImpactKind.HeroStaffSweep),
            new LockedBattleAttack(
                "arcane nova",
                LyraMotion.Attack3PoseName,
                LyraMotion.Attack3LastFrame / LyraMotion.WalkFrameRate,
                LyraMotion.Attack3ContactFrame,
                "mixamorig:RightHand",
                0.06f,
                false,
                default,
                BattleImpactKind.HeroArcaneNova)
        };

        public static readonly LockedBattleFighter Lyra = new LockedBattleFighter(
            "Lyra",
            BattleSide.Hero,
            "#59",
            LyraMotion.WalkPoseName,
            LyraMotion.AttackPoseName,
            "staff cast arcane bolt",
            1.885f,
            0.88f,
            0f,
            string.Empty,
            3.2f,
            BlenderRigAttackSpec.ClipSeconds,
            true,
            2,
            "",
            0f,
            LyraAttacks);

        /// <summary>
        /// Oakenshield's rotation (2026-10-18): the locked thorn spear throw, then the root snare (Mixamo): both palms drive down at
        /// f52 and hero-blue roots burst up out of the ground under the target. Attack 2 (a forward spear thrust) is still to come.
        /// </summary>
        public static readonly LockedBattleAttack[] OakenshieldAttacks =
        {
            new LockedBattleAttack("thorn spear throw", OakenshieldMotion.AttackPoseName, BlenderRigAttackSpec.ClipSeconds),
            new LockedBattleAttack(
                "root snare",
                OakenshieldMotion.Attack3PoseName,
                OakenshieldMotion.Attack3LastFrame / OakenshieldMotion.WalkFrameRate,
                OakenshieldMotion.Attack3ContactFrame,
                "mixamorig:LeftHand",
                0.05f,
                false,
                default,
                BattleImpactKind.HeroRootSnare)
        };

        public static readonly LockedBattleFighter Oakenshield = new LockedBattleFighter(
            "Oakenshield",
            BattleSide.Hero,
            "#60",
            OakenshieldMotion.WalkPoseName,
            OakenshieldMotion.AttackPoseName,
            "thorn spear throw",
            1.800f,
            1.15f,
            0f,
            string.Empty,
            3.0f,
            BlenderRigAttackSpec.ClipSeconds,
            true,
            3,
            "",
            0f,
            OakenshieldAttacks);

        /// <summary>
        /// Vespera's rotation (2026-10-18): the locked rave orb conjure, the shadow lash (Mixamo, purple lash streaks at f50) and the
        /// hex rain (Mixamo, three purple hexes falling 0.3 m apart from f84). Both Mixamo wind-ups play 1.5x slower, as her conjure.
        /// </summary>
        public static readonly LockedBattleAttack[] VesperaAttacks =
        {
            new LockedBattleAttack("rave orb conjure shadow bolt", VesperaMotion.AttackPoseName, VesperaAttack.ClipSeconds),
            new LockedBattleAttack(
                "shadow lash",
                VesperaMotion.Attack2PoseName,
                VesperaMotion.Attack2LastFrame / VesperaMotion.WalkFrameRate,
                VesperaMotion.Attack2ContactFrame,
                "mixamorig:RightHand",
                0.03f,
                false,
                default,
                BattleImpactKind.VillainShadowLash),
            new LockedBattleAttack(
                "hex rain",
                VesperaMotion.Attack3PoseName,
                VesperaMotion.Attack3LastFrame / VesperaMotion.WalkFrameRate,
                VesperaMotion.Attack3ContactFrame,
                "mixamorig:LeftHand",
                0.04f,
                false,
                default,
                BattleImpactKind.VillainHexRain,
                new[]
                {
                    new LockedBattleHitMark(VesperaMotion.Attack3ContactFrame + 4),
                    new LockedBattleHitMark(VesperaMotion.Attack3ContactFrame + 8)
                },
                0,
                0f,
                0.3f)
        };

        public static readonly LockedBattleFighter Vespera = new LockedBattleFighter(
            "Vespera",
            BattleSide.Villain,
            "#58",
            VesperaMotion.WalkPoseName,
            VesperaMotion.AttackPoseName,
            "rave orb conjure shadow bolt",
            1.909f,
            0.63f,
            0f,
            string.Empty,
            3.0f,
            VesperaAttack.ClipSeconds,
            true,
            2,
            "",
            0f,
            VesperaAttacks);

        public static readonly LockedBattleFighter Ashwyrm = new LockedBattleFighter(
            "Ashwyrm",
            BattleSide.Villain,
            "#56",
            AshwyrmMotion.WalkPoseName,
            AshwyrmMotion.AttackPoseName,
            "void-fire breath fireball",
            1.343f,
            0.52f,
            0f,
            string.Empty,
            3.2f,
            BlenderRigAttackSpec.ClipSeconds,
            true,
            3);

        // Fifth lane (2026-10-15, Derek approved). Sir Aldric plays the Lite Sword And Shield Pack
        // idle and "Sword And Shield attack (4)" (an in-place thrust that starts and ends on the
        // idle pose, frames 0-32) and the holefixed Standard Walk, which is in place: 2 × 0.861 m
        // foot sweep per 35-frame loop = 1.48 m/s. His blade tip reaches 0.94 m ahead on the
        // contact frame (the blade angles across the thrust), so he stops 1.4 m out, where the
        // tip meets Blightroot's 0.38 m front face. Blightroot plays the creature pack "mutant
        // idle", "mutant walking" (carries the hips 1.453 m per 43-frame loop, like Ironhowl) and
        // "mutant swiping" (80 frames, lunges and comes back to its start pose by the last frame).

        public static readonly LockedBattleFighter SirAldric = new LockedBattleFighter(
            "Sir Aldric",
            BattleSide.Hero,
            "lane 5 (Derek 2026-10-15)",
            SirAldricBattlePoses.Walk,
            SirAldricBattlePoses.Attack,
            "sword and shield thrust",
            1.800f,
            1.48f,
            0f,
            string.Empty,
            1.4f,
            SirAldricBattlePoses.AttackSeconds,
            false,
            4,
            "SirAldricMeshyAnimateActor");

        public static readonly LockedBattleFighter Blightroot = new LockedBattleFighter(
            "Blightroot",
            BattleSide.Villain,
            "lane 5 (Derek 2026-10-15)",
            BlightrootBattlePoses.Walk,
            BlightrootBattlePoses.Attack,
            "rot claw swipe",
            1.900f,
            0f,
            BlightrootBattlePoses.WalkStrideMetres,
            BlightrootMotion.BoneRoot,
            1.6f,
            BlightrootBattlePoses.AttackSeconds,
            false,
            4,
            "",
            0f,
            BlightrootBattlePoses.Attacks);

        // Sixth lane (2026-10-16, Derek: the characters not in yet). Both are Theme A Blender rigs
        // (#64, #63) with rest, walk and bow attack; their actors cycle attack + rest themselves.
        // Walk speeds from the locked walk clips: two left-foot sweeps per 1 s loop (Rowan
        // 0.515 m, Bonequill 0.549 m). Heights from the rest FBXs (1.80 m each).

        /// <summary>
        /// Rowan's rotation (2026-10-18): the locked bow shot, then the Mixamo double shot and volley. Their arrows land through
        /// explicit hit frames (loose + 9 frames of flight at 14 m/s over the 4.2 m range) with a hero-blue flight streak from the
        /// bow hand; the volley's three arrows arc up and land 0.35 m apart.
        /// </summary>
        public static readonly LockedBattleAttack[] RowanAttacks =
        {
            new LockedBattleAttack("bow shot", RowanMotion.AttackPoseName, BlenderRigAttackSpec.ClipSeconds),
            new LockedBattleAttack(
                "double shot",
                RowanMotion.Attack2PoseName,
                RowanMotion.Attack2LastFrame / RowanMotion.WalkFrameRate,
                RowanMotion.Attack2ReleaseFrames[0] + RowanMotion.ShotFlightFrames,
                "mixamorig:LeftHand",
                0.02f,
                false,
                default,
                BattleImpactKind.RangerArrow,
                new[] { new LockedBattleHitMark(RowanMotion.Attack2ReleaseFrames[1] + RowanMotion.ShotFlightFrames) },
                RowanMotion.ShotFlightFrames),
            new LockedBattleAttack(
                "volley",
                RowanMotion.Attack3PoseName,
                RowanMotion.Attack3LastFrame / RowanMotion.WalkFrameRate,
                RowanMotion.Attack3ReleaseFrame + RowanMotion.ShotFlightFrames,
                "mixamorig:LeftHand",
                0.03f,
                false,
                default,
                BattleImpactKind.RangerArrow,
                new[]
                {
                    new LockedBattleHitMark(RowanMotion.Attack3ReleaseFrame + RowanMotion.ShotFlightFrames + 2),
                    new LockedBattleHitMark(RowanMotion.Attack3ReleaseFrame + RowanMotion.ShotFlightFrames + 4)
                },
                RowanMotion.ShotFlightFrames,
                0.6f,
                0.35f)
        };

        public static readonly LockedBattleFighter Rowan = new LockedBattleFighter(
            "Rowan",
            BattleSide.Hero,
            "#64",
            RowanMotion.WalkPoseName,
            RowanMotion.AttackPoseName,
            "bow shot",
            1.800f,
            1.03f,
            0f,
            string.Empty,
            ArcherRangeMetres,
            BlenderRigAttackSpec.ClipSeconds,
            true,
            5,
            "",
            12.33f,
            RowanAttacks);

        /// <summary>
        /// Bonequill's rotation (2026-10-18): the locked bone arrow shot, the rapid quiver shot and the bone volley (Mixamo). Arrows
        /// land 9 frames after each loose with a purple flight streak from the bow hand; the volley's three arc up and land 0.35 m apart.
        /// </summary>
        public static readonly LockedBattleAttack[] BonequillAttacks =
        {
            new LockedBattleAttack("bone arrow shot", BonequillMotion.AttackPoseName, BlenderRigAttackSpec.ClipSeconds),
            new LockedBattleAttack(
                "rapid quiver shot",
                BonequillMotion.Attack2PoseName,
                BonequillMotion.Attack2LastFrame / BonequillMotion.WalkFrameRate,
                BonequillMotion.Attack2ReleaseFrame + BonequillMotion.ShotFlightFrames,
                "mixamorig:LeftHand",
                0.02f,
                false,
                default,
                BattleImpactKind.BoneShard,
                null,
                BonequillMotion.ShotFlightFrames),
            new LockedBattleAttack(
                "bone volley",
                BonequillMotion.Attack3PoseName,
                BonequillMotion.Attack3LastFrame / BonequillMotion.WalkFrameRate,
                BonequillMotion.Attack3ReleaseFrame + BonequillMotion.ShotFlightFrames,
                "mixamorig:LeftHand",
                0.03f,
                false,
                default,
                BattleImpactKind.BoneShard,
                new[]
                {
                    new LockedBattleHitMark(BonequillMotion.Attack3ReleaseFrame + BonequillMotion.ShotFlightFrames + 2),
                    new LockedBattleHitMark(BonequillMotion.Attack3ReleaseFrame + BonequillMotion.ShotFlightFrames + 4)
                },
                BonequillMotion.ShotFlightFrames,
                0.6f,
                0.35f)
        };

        public static readonly LockedBattleFighter Bonequill = new LockedBattleFighter(
            "Bonequill",
            BattleSide.Villain,
            "#63",
            BonequillMotion.WalkPoseName,
            BonequillMotion.AttackPoseName,
            "bone arrow shot",
            1.800f,
            1.10f,
            0f,
            string.Empty,
            ArcherRangeMetres,
            BlenderRigAttackSpec.ClipSeconds,
            true,
            5,
            "",
            0f,
            BonequillAttacks);

        public static IReadOnlyList<LockedBattleFighter> All { get; } = new[]
        {
            Emberfang,
            Stormcrest,
            Ironhowl,
            Nightfang,
            Lyra,
            Oakenshield,
            Vespera,
            Ashwyrm,
            SirAldric,
            Blightroot,
            Rowan,
            Bonequill
        };

        /// <summary>
        /// On main but not in the battle, with the reason. Unmerged PRs are not listed. Empty since
        /// the fifth lane brought in Blightroot and Sir Aldric.
        /// </summary>
        public static IReadOnlyList<KeyValuePair<string, string>> LeftOut { get; } = Array.Empty<KeyValuePair<string, string>>();

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
            x = LaneX(fighter.Lane);
            z = fighter.Side == BattleSide.Hero ? -StartHalfGapMetres : StartHalfGapMetres;
        }

        /// <summary>Lane centre across the field (x), Design metres.</summary>
        public static float LaneX(int lane)
        {
            switch (lane)
            {
                case 0:
                    return -LaneHalfSpacingMetres;
                case 1:
                    return LaneHalfSpacingMetres;
                case 2:
                    return -OuterLaneHalfSpacingMetres;
                case 4:
                    return -FifthLaneHalfSpacingMetres;
                case 5:
                    return SixthLaneHalfSpacingMetres;
                default:
                    return OuterLaneHalfSpacingMetres;
            }
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
            // Rotation-aware: with one attack this is the old attack + rest gap loop.
            return LockedBattleRotation.InAttackClip(fighter, secondsSinceFirstAttack);
        }
    }
}
