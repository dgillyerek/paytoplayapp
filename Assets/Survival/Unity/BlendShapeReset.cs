using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Clips sampled with <see cref="AnimationClip.SampleAnimation"/> only write the curves they own. Vorgath's slam keys
    /// the "Fist" blend shape and his other clips don't, so a slam cut short would leave the fists clenched. Zeroing the
    /// weights before each sample makes every clip start from the open-hand mesh.
    /// </summary>
    public static class BlendShapeReset
    {
        public static SkinnedMeshRenderer[] Find(GameObject model)
        {
            var found = new System.Collections.Generic.List<SkinnedMeshRenderer>();
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh != null && smr.sharedMesh.blendShapeCount > 0)
                {
                    found.Add(smr);
                }
            }

            return found.ToArray();
        }

        public static void Zero(SkinnedMeshRenderer[] renderers)
        {
            foreach (var smr in renderers)
            {
                var count = smr.sharedMesh.blendShapeCount;
                for (var i = 0; i < count; i++)
                {
                    smr.SetBlendShapeWeight(i, 0f);
                }
            }
        }
    }
}
