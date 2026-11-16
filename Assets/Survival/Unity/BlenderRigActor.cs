using Survival.Domain.Roster;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// The shared actor for every plain Blender-rig fighter (<see cref="BlenderRigCast"/>: Lyra, Oakenshield, Stormcrest,
    /// Vespera, Nightfang, Ashwyrm). Rest bind pose plus the spec's walk and attack clips. Rejects the Mixamo rig file,
    /// does not bake axis conversion or rewrite the imported root; the actor transform carries the cast's spawn yaw.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class BlenderRigActor : MonoBehaviour, IBlenderRigPlayback
    {
        private BlenderRigPlayer? _player;

        /// <summary>The cast this actor was built from, or null before <see cref="Build"/>.</summary>
        public BlenderRigCast? Cast { get; private set; }

        /// <summary>Attack take playback rate (runner: one take per shot); 1 in the battle.</summary>
        public float AttackSpeed
        {
            get => _player?.AttackSpeed ?? 1f;
            set
            {
                if (_player != null)
                {
                    _player.AttackSpeed = value;
                }
            }
        }

        /// <summary>Runner only: the attack plays on the upper body over the walk (see <see cref="BlenderRigPlayer.UpperBodyAttack"/>).</summary>
        public bool UpperBodyAttack
        {
            get => _player != null && _player.UpperBodyAttack;
            set
            {
                if (_player != null)
                {
                    _player.UpperBodyAttack = value;
                }
            }
        }

        /// <summary>Runner only: the take frame the upper body holds the whole run (see <see cref="BlenderRigPlayer.AimHoldFrame"/>), or null.</summary>
        public float? AimHoldFrame
        {
            get => _player?.AimHoldFrame;
            set
            {
                if (_player != null)
                {
                    _player.AimHoldFrame = value;
                }
            }
        }

        /// <summary>Runner: legs walk (true) or stand under an upper-body attack.</summary>
        public void SetWalkUnderAttack(bool walking) => _player?.SetWalkUnderAttack(walking);

        public bool Built => _player != null && _player.Built;
        public string Pose => _player != null ? _player.Pose : BlenderRigSpec.RestPoseName;
        public float ClipLength => _player != null ? _player.ClipLength : 0f;
        public Bounds VisibleBounds => _player != null ? _player.VisibleBounds : new Bounds(Vector3.zero, Vector3.one);
        public BlenderRigAttackDriver? AttackDriver => _player?.AttackDriver;

        public void Build(BlenderRigCast cast)
        {
            Cast = cast;
            if (cast.FacingYawDegrees != 0f)
            {
                transform.localRotation = Quaternion.Euler(0f, cast.FacingYawDegrees, 0f);
            }

            _player = new BlenderRigPlayer(cast.Motion, transform, cast.Attack, cast.IdleProps);
            _player.Build();
        }

        public bool PlayPose(string poseName)
        {
            return _player != null && _player.PlayPose(poseName);
        }

        private void Update()
        {
            _player?.Tick();
        }

        private void OnDestroy()
        {
            _player?.Dispose();
        }
    }
}
