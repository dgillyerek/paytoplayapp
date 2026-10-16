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
