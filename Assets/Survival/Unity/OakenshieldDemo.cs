using Survival.Domain.Heroes;
using Survival.Domain.Roster;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Oakenshield Game view 1080×1920. Dropdown plays rest and walk.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public sealed class OakenshieldDemo : BlenderRigDemo
    {
        public const string DropdownObjectName = "OakenshieldClipDropdown";

        protected override BlenderRigSpec Spec => OakenshieldMotion.Spec;

        protected override MonoBehaviour CreateActor(GameObject host)
        {
            var actor = host.AddComponent<OakenshieldActor>();
            actor.Build();
            return actor;
        }
    }
}
