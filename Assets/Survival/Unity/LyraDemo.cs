using Survival.Domain.Heroes;
using Survival.Domain.Roster;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Lyra Game view 1080×1920. Dropdown plays rest and walk.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public sealed class LyraDemo : BlenderRigDemo
    {
        public const string DropdownObjectName = "LyraClipDropdown";

        protected override BlenderRigSpec Spec => LyraMotion.Spec;

        protected override MonoBehaviour CreateActor(GameObject host)
        {
            var actor = host.AddComponent<LyraActor>();
            actor.Build();
            return actor;
        }
    }
}
