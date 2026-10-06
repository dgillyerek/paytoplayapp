using Survival.Domain.Enemies;
using Survival.Domain.Roster;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Nightfang Game view 1080×1920. Dropdown plays rest and trot.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public sealed class NightfangDemo : BlenderRigDemo
    {
        public const string DropdownObjectName = "NightfangClipDropdown";

        protected override BlenderRigSpec Spec => NightfangMotion.Spec;

        protected override MonoBehaviour CreateActor(GameObject host)
        {
            var actor = host.AddComponent<NightfangActor>();
            actor.Build();
            return actor;
        }
    }
}
