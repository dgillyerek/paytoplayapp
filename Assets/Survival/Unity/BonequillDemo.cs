using Survival.Domain.Enemies;
using Survival.Domain.Roster;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Bonequill Game view 1080×1920. Dropdown plays rest and walk.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public sealed class BonequillDemo : BlenderRigDemo
    {
        public const string DropdownObjectName = "BonequillClipDropdown";

        protected override BlenderRigSpec Spec => BonequillMotion.Spec;

        protected override MonoBehaviour CreateActor(GameObject host)
        {
            var actor = host.AddComponent<BonequillActor>();
            actor.Build();
            return actor;
        }
    }
}
