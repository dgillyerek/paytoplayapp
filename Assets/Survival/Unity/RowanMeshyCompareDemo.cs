using Survival.Domain.Heroes;
using Survival.Domain.Roster;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Rowan Meshy compare Game view 1080×1920: rest / walk / attack dropdown, in place, rear battle
    /// camera, attack aimed toward the top of the screen. Separate from the Blender-rig Rowan (#50).
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public sealed class RowanMeshyCompareDemo : BlenderRigDemo
    {
        public const string DropdownObjectName = "RowanMeshyCompareClipDropdown";

        protected override BlenderRigSpec Spec => RowanMeshyMotion.DemoSpec;

        protected override string Title => RowanMeshyMotion.Title;

        protected override float StartYawDegrees => RowanMeshyMotion.CameraYawDegrees;

        protected override float StartPitchDegrees => RowanMeshyMotion.CameraPitchDegrees;

        protected override MonoBehaviour CreateActor(GameObject host)
        {
            var actor = host.AddComponent<RowanMeshyActor>();
            actor.Build();
            return actor;
        }
    }
}
