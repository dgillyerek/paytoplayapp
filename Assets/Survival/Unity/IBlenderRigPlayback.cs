using UnityEngine;

namespace Survival.Unity
{
    /// <summary>What every Blender-rig actor exposes to the battle and the runner.</summary>
    public interface IBlenderRigPlayback
    {
        bool Built { get; }
        string Pose { get; }
        float ClipLength { get; }
        Bounds VisibleBounds { get; }
        bool PlayPose(string poseName);
    }
}
