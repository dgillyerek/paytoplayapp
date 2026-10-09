using Survival.Domain.Heroes;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Rowan Meshy compare actor: Meshy auto-rig played as-is (idle rest, walk, Archery Shot attack).
    /// The bow is skinned into the mesh, so there is no bow prop; it warps in the attack (known).
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class RowanMeshyActor : MonoBehaviour, IBlenderRigPlayback
    {
        private MeshyRigPlayer? _player;

        public bool Built => _player != null && _player.Built;
        public string Pose => _player != null ? _player.Pose : Survival.Domain.Roster.MeshyRigSpec.RestPoseName;
        public float ClipLength => _player != null ? _player.ClipLength : 0f;
        public Bounds VisibleBounds => _player != null ? _player.VisibleBounds : new Bounds(Vector3.up, Vector3.one);

        public void Build()
        {
            _player = new MeshyRigPlayer(RowanMeshyMotion.Spec, transform);
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
