using Survival.Domain.Heroes;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Stormcrest rest bind pose plus wing flap, walk, and attack. Custom Generic
    /// on Derek's skeleton. Rejects STORMCREST_rig.fbx. Does not bake axis
    /// conversion or rewrite the imported root.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class StormcrestActor : MonoBehaviour, IBlenderRigPlayback
    {
        private BlenderRigPlayer? _player;

        public bool Built => _player != null && _player.Built;
        public string Pose => _player != null ? _player.Pose : StormcrestMotion.RestPoseName;
        public float ClipLength => _player != null ? _player.ClipLength : 0f;
        public Bounds VisibleBounds => _player != null ? _player.VisibleBounds : new Bounds(Vector3.zero, Vector3.one);
        public BlenderRigAttackDriver? AttackDriver => _player?.AttackDriver;

        public void Build()
        {
            _player = new BlenderRigPlayer(StormcrestMotion.Spec, transform, StormcrestAttack.Spec);
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
