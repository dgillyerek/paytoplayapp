using Survival.Domain.Enemies;
using Survival.Domain.Roster;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Vespera Game view 1080×1920. Dropdown plays rest and walk.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public sealed class VesperaDemo : BlenderRigDemo
    {
        public const string DropdownObjectName = "VesperaClipDropdown";

        protected override BlenderRigSpec Spec => VesperaMotion.Spec;

        protected override MonoBehaviour CreateActor(GameObject host)
        {
            var actor = host.AddComponent<VesperaActor>();
            actor.Build();
            return actor;
        }
    }
}
