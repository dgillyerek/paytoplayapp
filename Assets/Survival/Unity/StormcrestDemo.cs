using Survival.Domain.Heroes;
using Survival.Domain.Roster;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Stormcrest Game view 1080×1920. Dropdown plays rest, wing flap, walk, and attack.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public sealed class StormcrestDemo : BlenderRigDemo
    {
        public const string DropdownObjectName = "StormcrestClipDropdown";

        protected override BlenderRigSpec Spec => StormcrestMotion.Spec;

        protected override MonoBehaviour CreateActor(GameObject host)
        {
            var actor = host.AddComponent<StormcrestActor>();
            actor.Build();
            return actor;
        }
    }
}
