using Survival.Domain.Heroes;

namespace Survival.Unity
{
    /// <summary>
    /// Same Rowan actor and portrait stage as <see cref="RowanDemo"/>.
    /// Opens already playing the walk clip.
    /// </summary>
    public sealed class RowanWalkDemo : RowanDemo
    {
        protected override string OpeningPose => RowanMotion.WalkPoseName;
    }
}
