using Survival.Domain.Heroes;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Oakenshield fresh rig (2026-10-12): rest bind pose, walk and thorn spear attack, imported Generic (like the locked rigs) so
    /// Unity plays the exact Blender bone keys. Rejects OAKENSHIELD_rig.fbx. Does not bake axis conversion or rewrite the imported
    /// root. The mesh is sculpted square to the camera, so no spawn yaw.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class OakenshieldActor : MonoBehaviour, IBlenderRigPlayback
    {
        private BlenderRigPlayer? _player;

        /// <summary>Attack take playback rate (runner: one sped-up take per shot); 1 in the battle.</summary>
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

        /// <summary>Runner: legs walk (true) or stand under an upper-body attack.</summary>
        public void SetWalkUnderAttack(bool walking) => _player?.SetWalkUnderAttack(walking);

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

        public bool Built => _player != null && _player.Built;
        public string Pose => _player != null ? _player.Pose : OakenshieldMotion.RestPoseName;
        public float ClipLength => _player != null ? _player.ClipLength : 0f;
        public Bounds VisibleBounds => _player != null ? _player.VisibleBounds : new Bounds(Vector3.zero, Vector3.one);
        public BlenderRigAttackDriver? AttackDriver => _player?.AttackDriver;

        public void Build()
        {
            _player = new BlenderRigPlayer(OakenshieldMotion.Spec, transform, OakenshieldAttack.Spec);
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
