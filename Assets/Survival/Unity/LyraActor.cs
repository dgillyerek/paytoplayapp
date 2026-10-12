using Survival.Domain.Heroes;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Lyra rest bind pose, walk and attack clips, imported Generic (like the locked rigs) so Unity
    /// plays the exact Blender bone keys; Humanoid retargeting dropped the attack's body turn and step. Rejects LYRA_rig.fbx. Does not bake axis
    /// conversion or rewrite the imported root; the actor transform carries
    /// LyraMotion.FacingYawDegrees so she faces the camera.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class LyraActor : MonoBehaviour, IBlenderRigPlayback
    {
        private BlenderRigPlayer? _player;

        public bool Built => _player != null && _player.Built;
        public string Pose => _player != null ? _player.Pose : LyraMotion.RestPoseName;
        public float ClipLength => _player != null ? _player.ClipLength : 0f;
        public Bounds VisibleBounds => _player != null ? _player.VisibleBounds : new Bounds(Vector3.zero, Vector3.one);

        public void Build()
        {
            // Square her to the camera (see LyraMotion.FacingYawDegrees); before the build so bounds and attack calibration use it.
            transform.localRotation = Quaternion.Euler(0f, LyraMotion.FacingYawDegrees, 0f);
            _player = new BlenderRigPlayer(LyraMotion.Spec, transform, LyraAttack.Spec, LyraMotion.IdleProps);
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
