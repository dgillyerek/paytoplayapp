using Survival.Domain.Heroes;
using Survival.Domain.Roster;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Rowan Game view 1080×1920. Dropdown plays rest and walk.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public sealed class RowanDemo : BlenderRigDemo
    {
        public const string DropdownObjectName = "RowanClipDropdown";

        protected override BlenderRigSpec Spec => RowanMotion.Spec;

        protected override MonoBehaviour CreateActor(GameObject host)
        {
            var actor = host.AddComponent<RowanActor>();
            actor.Build();
            return actor;
        }
    }
}
