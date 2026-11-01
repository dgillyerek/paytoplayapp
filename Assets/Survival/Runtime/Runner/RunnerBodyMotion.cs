#nullable enable
using System;
using System.Numerics;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;

namespace Survival.Domain.Runner
{
    /// <summary>
    /// How far a hit may move a runner rig's body (Derek 2026-10-23: "when blightroot gets hit he bounces back almost
    /// off screen"). The sim never pushes a foe back; the rig shows the battle's recoil step only as a small flinch,
    /// capped here for every enemy whatever its recoil profile, reaction or scale.
    /// </summary>
    public static class RunnerHitNudge
    {
        /// <summary>Most a hit moves a runner rig's body sideways or back, in metres.</summary>
        public const float MaxMetres = 0.05f;

        /// <summary>Factor that brings a ground nudge (<paramref name="x"/>, <paramref name="z"/>) to at most <see cref="MaxMetres"/>.</summary>
        public static float Scale(float x, float z)
        {
            var length = (float)Math.Sqrt((x * x) + (z * z));
            return length > MaxMetres ? MaxMetres / length : 1f;
        }
    }

    /// <summary>
    /// Rigs whose walk clip carries the body forward (Blightroot's mutant walk: 1.45 m of hips travel per loop, 1.9x
    /// scale on the road) are held in place by the runner, the way the battle pins its stride: the sim moves the slot,
    /// so the hips' own forward travel is cancelled every frame instead of drifting ahead and snapping back on each
    /// loop wrap and each walk / attack / rest change.
    /// </summary>
    public static class RunnerStrideHold
    {
        /// <summary>
        /// True for a rig the runner holds. Ironhowl is left out: he never walks in the runner (the boss holds at the
        /// arena line), and his slam's lunge is the readable, lane-aimed big attack.
        /// </summary>
        public static bool Applies(LockedBattleFighter fighter) =>
            fighter.WalkCarriesRoot && !string.Equals(fighter.Name, LockedBattleRoster.Ironhowl.Name, StringComparison.Ordinal);
    }

    /// <summary>
    /// Where a runner rig's host sits in its slot (Derek 2026-10-23: "blightroot when shot still bounces way back").
    /// The body turns (hit lean, death tip) about its pivot: the rest place of the stride bone (Blightroot's hips) or,
    /// for a rig without one, its mid height. The pivot stays where the sim puts the slot, give or take the hit
    /// nudge and the death sink, and a held rig's clip travel is cancelled whatever the host's turn (only its bob is
    /// kept, straight up). (The old hold was subtracted along the road while the host was tipped
    /// 80 degrees in its death fall: the walk's forward travel had turned upward, so the hold pushed the body up to
    /// 2.3 m back, sawing on every loop. The old lean also pivoted at the feet, swinging the hips back.)
    /// All vectors are in the slot's frame except <paramref name="pivotRest"/> and <paramref name="bone"/>, which are
    /// host-local (before the host's scale).
    /// </summary>
    public static class RunnerRootPlacement
    {
        public static Vector3 HostPosition(
            Vector3 baseLocal,
            Vector3 nudge,
            float sink,
            Quaternion rotation,
            Vector3 scale,
            Vector3 pivotRest,
            Vector3 bone,
            bool hold)
        {
            if (!hold)
            {
                bone = pivotRest;
            }

            // The held bone shows at its rest place; only its own rise and fall (the walk's bob) is kept, straight up.
            var bob = (bone.Y - pivotRest.Y) * scale.Y;
            return baseLocal + nudge + new Vector3(0f, bob - sink, 0f) + (pivotRest * scale) - Vector3.Transform(bone * scale, rotation);
        }

        /// <summary>Where host-local point <paramref name="point"/> shows in the slot.</summary>
        public static Vector3 Rendered(Vector3 hostPosition, Quaternion rotation, Vector3 scale, Vector3 point) =>
            hostPosition + Vector3.Transform(point * scale, rotation);
    }

    /// <summary>
    /// The runner's squad shooters (Lyra, Rowan, Oakenshield) keep walking while they fire (Derek 2026-10-23: "it
    /// looks weird when the heros are stepping foward and backwards while firing"): their attack takes move the
    /// hips 0.12 to 0.40 m forward and back, and played full-body over a sliding slot they read as steps. The take
    /// plays on the upper body only (spine and everything under it, plus any side branch such as a weapon bone);
    /// the hips and legs stay on the walk loop. Each take crossfades in and out over <see cref="CrossfadeSeconds"/>;
    /// its timing (and so its release frame) is untouched.
    /// </summary>
    public static class RunnerUpperBodyAttack
    {
        public const float CrossfadeSeconds = 0.1f;

        /// <summary>Roster names that play the layered attack in the runner.</summary>
        public static readonly string[] Shooters = { "Lyra", "Rowan", "Oakenshield" };

        public static bool Applies(string rosterName) => Array.IndexOf(Shooters, rosterName) >= 0;

        /// <summary>Smoothstep of a 0..1 progress.</summary>
        public static float Ease(float u)
        {
            if (u <= 0f)
            {
                return 0f;
            }

            if (u >= 1f)
            {
                return 1f;
            }

            return u * u * (3f - (2f * u));
        }

        /// <summary>Weight of a take <paramref name="age"/> seconds after it started.</summary>
        public static float FadeIn(float age) => Ease(age / CrossfadeSeconds);

        /// <summary>Weight of a layer that was at <paramref name="from"/> when it began fading out <paramref name="age"/> seconds ago.</summary>
        public static float FadeOut(float from, float age) => from * (1f - Ease(age / CrossfadeSeconds));

        /// <summary>
        /// True when the transform at <paramref name="path"/> (relative to the animator, '/'-separated) takes the attack.
        /// The hips, their ancestors and every hips branch other than the spine (the legs) stay on the walk; the spine
        /// and everything under it take the attack, as does a side branch beside the hips (a weapon bone).
        /// </summary>
        public static bool IsUpperBody(string path, string hipsPath, string spineName)
        {
            if (path.Length == 0 || hipsPath.Length == 0)
            {
                return false;
            }

            if (string.Equals(path, hipsPath, StringComparison.Ordinal)
                || hipsPath.StartsWith(path + "/", StringComparison.Ordinal))
            {
                return false;
            }

            if (!path.StartsWith(hipsPath + "/", StringComparison.Ordinal))
            {
                return true;
            }

            var below = path.Substring(hipsPath.Length + 1).Split('/');
            return string.Equals(below[0], spineName, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Derek 2026-10-24: "just keep the heros body in firing position constantly. moving back and forth is wild." In the
    /// runner the squad shooters (Lyra, Rowan, Oakenshield) hold one aim pose on the upper body the whole run: the
    /// attack take pinned at <see cref="HoldFrameFor"/> (the frame before its release: bow at full draw, bolt charged,
    /// thorn cocked) at full weight, while the legs keep walking (or stand at the boss's line). A shot no longer
    /// restarts or crossfades the take, and going back to the walk does not fade it out; the sim still fires on its
    /// own timer and the scene draws the shot from the shooter. Their body also takes no hit step, hit lean, hit
    /// flinch pose or steering roll (the hit flash stays). The battle never turns this on.
    /// </summary>
    public static class RunnerAimHold
    {
        /// <summary>True for a roster the runner holds in its aim pose (the upper-body shooters).</summary>
        public static bool Applies(string rosterName) => RunnerUpperBodyAttack.Applies(rosterName);

        /// <summary>The attack-take frame the upper body holds (one before the take's release), or null for a roster that does not hold.</summary>
        /// <remarks>
        /// Null for every roster now (Derek 2026-10-27: "not all heros have a firing animation in the runner. some of them
        /// are just walking"). The frozen aim frame read as walking, so each shot plays the hero's real attack take on the
        /// upper body again (legs keep walking, release frame on the sim's Fired); the body still takes no hit step or lean
        /// (<see cref="BodyReactionScale"/>, <see cref="LeanScale"/>), which was the "moving back and forth" of 2026-10-24.
        /// <see cref="HeldFrameWas"/> keeps the old frame for reference.
        /// </remarks>
        public static float? HoldFrameFor(string rosterName) => null;

        /// <summary>The frame the 2026-10-24 hold froze on (one before release), kept so a later hold can come back as a switch.</summary>
        public static float? HeldFrameWas(string rosterName) => rosterName switch
        {
            "Lyra" => LyraAttack.ReleaseFrame - 1,
            "Rowan" => RowanAttack.ReleaseFrame - 1,
            "Oakenshield" => OakenshieldAttack.ReleaseFrame - 1,
            _ => null,
        };

        /// <summary>How much of the hit reaction (root step, root lean, flinch pose) a runner rig's body shows: none for a held shooter.</summary>
        public static float BodyReactionScale(string rosterName) => Applies(rosterName) ? 0f : 1f;

        /// <summary>How much of the steering roll a runner rig shows: none for a held shooter (the aim stays level).</summary>
        public static float LeanScale(string rosterName) => Applies(rosterName) ? 0f : 1f;
    }

    /// <summary>What a pose request does to a rig's held aim (<see cref="RunnerAimHoldLayer"/>).</summary>
    public enum RunnerAimHoldPose
    {
        Attack,
        Walk,
        Rest,
    }

    /// <summary>
    /// The held aim's upper-body layer state (<see cref="RunnerAimHold"/>), as the rig player drives it: any pose
    /// (walk, rest or a new take) enters the hold once; after that nothing restarts, crossfades or fades it, so the
    /// upper body's weight and sampled frame never change across shots, walk returns or attack speed changes.
    /// </summary>
    public sealed class RunnerAimHoldLayer
    {
        public RunnerAimHoldLayer(float holdFrame)
        {
            HoldFrame = holdFrame;
        }

        public float HoldFrame { get; }

        /// <summary>True once the hold is on (the take is built and pinned).</summary>
        public bool Active { get; private set; }

        /// <summary>Legs on the walk loop (true) or standing (the squad halted at the boss's line).</summary>
        public bool LegsWalk { get; private set; } = true;

        /// <summary>Upper-body weight of the held take: full from the first frame, never eased.</summary>
        public float Weight => Active ? 1f : 0f;

        /// <summary>The take frame sampled on the upper body: always <see cref="HoldFrame"/>.</summary>
        public float Frame => HoldFrame;

        /// <summary>
        /// A pose request. Returns true only when the held take has to be built (the first request, or after
        /// <see cref="Reset"/>); every later request leaves the upper body exactly as it is.
        /// </summary>
        public bool Play(RunnerAimHoldPose pose)
        {
            var build = !Active;
            Active = true;
            if (pose == RunnerAimHoldPose.Walk)
            {
                LegsWalk = true;
            }
            else if (pose == RunnerAimHoldPose.Rest)
            {
                LegsWalk = false;
            }

            return build;
        }

        /// <summary>The legs walk or stand under the hold (the upper body is untouched).</summary>
        public void SetLegsWalk(bool walking) => LegsWalk = walking;

        /// <summary>The rig left the layered path (another clip played full-body): the next request builds the hold again.</summary>
        public void Reset() => Active = false;
    }

    /// <summary>One bone of a <see cref="RunnerShotFlick"/>: a pitch about the road's right axis, + = forward/down (toward the shot), - = up/back (recoil).</summary>
    public readonly struct RunnerShotFlickBone
    {
        public RunnerShotFlickBone(string bone, float pitchDegrees)
        {
            Bone = bone;
            PitchDegrees = pitchDegrees;
        }

        public string Bone { get; }

        public float PitchDegrees { get; }
    }

    /// <summary>
    /// Derek 2026-10-24: "add is some sort of firing motion for heroes, just make it subtle", then "add a better firing
    /// animation. for a staff it should just shift backwards almost like firing a shotgun". On top of the held aim pose
    /// (<see cref="RunnerAimHold"/>) each shot adds a readable, controlled recoil: a rotation-only snap on the arm / hand /
    /// shoulder / upper-spine bones that peaks fast (<see cref="RiseSeconds"/>) and eases back to the hold over about
    /// 0.3 s, started on the sim's Fired (the frame the scene draws the shot). Never the hips, root or legs: the body does
    /// not step forward or back and the legs keep walking. Runner only; the battle never uses it.
    /// </summary>
    public static class RunnerShotFlick
    {
        /// <summary>No shot's recoil may turn a rig's kicked bones more than this in total at the peak.</summary>
        public const float MaxDegrees = 20f;

        /// <summary>Seconds from the shot to the peak of the recoil (a shotgun-fast snap).</summary>
        public const float RiseSeconds = 0.05f;

        /// <summary>Oakenshield: a sharp snap of the throwing arm toward the thorn, the shoulder and upper back following through.</summary>
        public static readonly RunnerShotFlickBone[] Oakenshield =
        {
            new RunnerShotFlickBone("mixamorig:Spine2", 2f),
            new RunnerShotFlickBone("mixamorig:RightShoulder", 3f),
            new RunnerShotFlickBone("mixamorig:RightForeArm", 7f),
            new RunnerShotFlickBone("mixamorig:RightHand", 5f),
        };

        /// <summary>
        /// Lyra: a shotgun recoil. The spine chain rocks her chest and shoulders back about 13 degrees, carrying both hands
        /// and the staff with it (her two-handed grip stays on the shaft), and the staff hand kicks the staff back toward
        /// her shoulder.
        /// </summary>
        public static readonly RunnerShotFlickBone[] Lyra =
        {
            new RunnerShotFlickBone("mixamorig:Spine", -3f),
            new RunnerShotFlickBone("mixamorig:Spine1", -5f),
            new RunnerShotFlickBone("mixamorig:Spine2", -5f),
            new RunnerShotFlickBone("mixamorig:RightHand", -3.5f),
        };

        /// <summary>The bones of Lyra's upper-body rock (the spine chain), whose sum is her recoil's lean back.</summary>
        public static readonly string[] LyraSpine = { "mixamorig:Spine", "mixamorig:Spine1", "mixamorig:Spine2" };

        /// <summary>Rowan: the bow arm kicks up off the shot and the string hand flicks back off the release.</summary>
        public static readonly RunnerShotFlickBone[] Rowan =
        {
            new RunnerShotFlickBone("mixamorig:LeftForeArm", -5f),
            new RunnerShotFlickBone("mixamorig:LeftHand", -4f),
            new RunnerShotFlickBone("mixamorig:RightForeArm", -4f),
            new RunnerShotFlickBone("mixamorig:RightHand", -6f),
        };

        /// <summary>Bones that may never take the kick (the root, hips and legs: no step, no forward/back move).</summary>
        public static readonly string[] Forbidden =
        {
            "root", "Armature", "mixamorig:Hips",
            "mixamorig:LeftUpLeg", "mixamorig:LeftLeg", "mixamorig:LeftFoot", "mixamorig:LeftToeBase",
            "mixamorig:RightUpLeg", "mixamorig:RightLeg", "mixamorig:RightFoot", "mixamorig:RightToeBase",
        };

        /// <summary>The kicked bones (parent before child) for a roster, or an empty list for one that does not kick.</summary>
        public static RunnerShotFlickBone[] BonesFor(string rosterName) => rosterName switch
        {
            "Lyra" => Lyra,
            "Rowan" => Rowan,
            "Oakenshield" => Oakenshield,
            _ => Array.Empty<RunnerShotFlickBone>(),
        };

        /// <summary>How long one recoil lasts, shot to back on the hold (the snap plus about 0.3 s of ease back).</summary>
        public static float SecondsFor(string rosterName) => rosterName switch
        {
            "Lyra" => 0.35f,
            "Rowan" => 0.32f,
            "Oakenshield" => 0.3f,
            _ => 0f,
        };

        /// <summary>
        /// Kick weight 0..1 <paramref name="age"/> seconds after a shot: a smoothstep up to 1 over <see cref="RiseSeconds"/>,
        /// then an ease-out (quadratic) back to exactly 0 at <paramref name="seconds"/>.
        /// </summary>
        public static float Weight(float age, float seconds)
        {
            if (age < 0f || seconds <= RiseSeconds || age >= seconds)
            {
                return 0f;
            }

            if (age < RiseSeconds)
            {
                return RunnerUpperBodyAttack.Ease(age / RiseSeconds);
            }

            var u = 1f - ((age - RiseSeconds) / (seconds - RiseSeconds));
            return u * u;
        }
    }

    /// <summary>
    /// One rig's running kick. A new shot starts a new kick; one still fading keeps fading, and the rig shows the larger of
    /// the two, so a fast fire rate never pops the arm or stacks past one kick's peak.
    /// </summary>
    public sealed class RunnerShotFlickState
    {
        private float _age = 10f;
        private float _previousAge = 10f;

        public RunnerShotFlickState(float seconds)
        {
            Seconds = seconds;
        }

        public float Seconds { get; }

        /// <summary>The sim fired (the shot leaves this frame).</summary>
        public void Fire()
        {
            _previousAge = _age;
            _age = 0f;
        }

        public void Advance(float dt)
        {
            _age += dt;
            _previousAge += dt;
        }

        public void Reset()
        {
            _age = 10f;
            _previousAge = 10f;
        }

        /// <summary>The kick's weight now, 0..1 (0 = exactly the held pose).</summary>
        public float Weight => Math.Max(RunnerShotFlick.Weight(_age, Seconds), RunnerShotFlick.Weight(_previousAge, Seconds));
    }
}
