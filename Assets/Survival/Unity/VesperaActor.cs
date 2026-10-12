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

        public bool Built => _player != null && _player.Built;
        public string Pose => _player != null ? _player.Pose : VesperaMotion.RestPoseName;
        public float ClipLength => _player != null ? _player.ClipLength : 0f;
        public Bounds VisibleBounds => _player != null ? _player.VisibleBounds : new Bounds(Vector3.zero, Vector3.one);

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
