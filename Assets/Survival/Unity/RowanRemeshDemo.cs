using Survival.Domain.Heroes;
using Survival.Domain.Roster;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Male Rowan remesh Game view 1080×1920. Dropdown plays rest and walk.
    /// Opens on the rest bind pose. Does not replace the tip Rowan demo.
    /// </summary>
    public sealed class RowanRemeshDemo : BlenderRigDemo
    {
        public const string DropdownObjectName = "RowanRemeshClipDropdown";

        protected override BlenderRigSpec Spec => RowanRemeshMotion.Spec;

        protected override MonoBehaviour CreateActor(GameObject host)
        {
            var actor = host.AddComponent<RowanRemeshActor>();
            actor.Build();
            return actor;
        }
    }
}
