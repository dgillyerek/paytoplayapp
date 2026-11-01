using Survival.Domain.Enemies;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Design no-cape pack (blender_rig_nocape_20261009): 22 mixamorig bones, no cloth chains, no spring bones or leg colliders.
    /// Vespera rest bind pose and walk clip. Mixamorig humanoid when the avatar
    /// validates, otherwise Generic. Rejects VESPERA_rig.fbx. Does not bake axis
    /// conversion or rewrite the imported root.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class VesperaActor : MonoBehaviour, IBlenderRigPlayback
    {
        private BlenderRigPlayer? _player;

        /// <summary>Attack take playback rate (runner: one take per shot, paced to the enemy's fire interval); 1 in the battle.</summary>
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

        /// <summary>Runner only: a ranged foe's attack plays on the upper body over its walk (see <see cref="BlenderRigPlayer.UpperBodyAttack"/>).</summary>
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

        public bool Built => _player != null && _player.Built;
        public string Pose => _player != null ? _player.Pose : VesperaMotion.RestPoseName;
        public float ClipLength => _player != null ? _player.ClipLength : 0f;
        public Bounds VisibleBounds => _player != null ? _player.VisibleBounds : new Bounds(Vector3.zero, Vector3.one);
        public BlenderRigAttackDriver? AttackDriver => _player?.AttackDriver;

        public void Build()
        {
            _player = new BlenderRigPlayer(VesperaMotion.Spec, transform, VesperaAttack.Spec);
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
