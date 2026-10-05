using Survival.Domain.Enemies;
using Survival.Domain.Roster;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Ashwyrm Game view 1080×1920. Dropdown plays rest and wing flap.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public sealed class AshwyrmDemo : BlenderRigDemo
    {
        public const string DropdownObjectName = "AshwyrmClipDropdown";

        protected override BlenderRigSpec Spec => AshwyrmMotion.Spec;

        protected override MonoBehaviour CreateActor(GameObject host)
        {
            var actor = host.AddComponent<AshwyrmActor>();
            actor.Build();
            return actor;
        }
    }
}
