using Survival.Domain.Heroes;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Male Rowan remesh rest bind and walk clip. Same player path as the tip Rowan actor.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class RowanRemeshActor : MonoBehaviour, IBlenderRigPlayback
    {
        private BlenderRigPlayer? _player;

        public bool Built => _player != null && _player.Built;
        public string Pose => _player != null ? _player.Pose : RowanRemeshMotion.RestPoseName;
        public float ClipLength => _player != null ? _player.ClipLength : 0f;
        public Bounds VisibleBounds => _player != null ? _player.VisibleBounds : new Bounds(Vector3.zero, Vector3.one);

        public void Build()
        {
            _player = new BlenderRigPlayer(RowanRemeshMotion.Spec, transform, RowanAttack.Spec);
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
