using Survival.Domain.Heroes;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Survival.Unity
{
    /// <summary>
    /// Four Scene-view empties Derek can drag. Play reads their world
    /// positions — no code change to retarget the slash. Gizmos + labels
    /// stay visible with Gizmos on. Not Mixamo DIAG. HOLD merge.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(100)]
    public sealed class SirAldricStrikePath : MonoBehaviour
    {
        public const string RootName = SirAldric3DMotion.StrikePathRootName;

        [SerializeField] private Transform draw = null!;
        [SerializeField] private Transform raise = null!;
        [SerializeField] private Transform forward = null!;
        [SerializeField] private Transform foot = null!;

        public Transform Draw => draw;
        public Transform Raise => raise;
        public Transform Forward => forward;
        public Transform Foot => foot;

        public Transform Marker(int index)
        {
            return index switch
            {
                0 => draw,
                1 => raise,
                2 => forward,
                _ => foot,
            };
        }

        public Vector3 MarkerWorld(int index)
        {
            var t = Marker(index);
            if (t != null)
            {
                return t.position;
            }

            SirAldric3DMotion.StrikeMarkerDefaultWorld(index, out var x, out var y, out var z);
            return new Vector3(x, y, z);
        }

        public Vector3 SampleWorld(float attackNormalized01)
        {
            var p0 = MarkerWorld(0);
            var p1 = MarkerWorld(1);
            var p2 = MarkerWorld(2);
            var p3 = MarkerWorld(3);
            SirAldric3DMotion.SampleStrikePath(
                attackNormalized01,
                p0.x, p0.y, p0.z,
                p1.x, p1.y, p1.z,
                p2.x, p2.y, p2.z,
                p3.x, p3.y, p3.z,
                out var x, out var y, out var z);
            return new Vector3(x, y, z);
        }

        public static SirAldricStrikePath? FindInScene()
        {
            var found = FindFirstObjectByType<SirAldricStrikePath>();
            if (found != null)
            {
                return found;
            }

            var go = GameObject.Find(RootName);
            return go != null ? go.GetComponent<SirAldricStrikePath>() : null;
        }

        public void Bind(Transform drawMarker, Transform raiseMarker, Transform forwardMarker, Transform footMarker)
        {
            draw = drawMarker;
            raise = raiseMarker;
            forward = forwardMarker;
            foot = footMarker;
        }

        private void OnDrawGizmos()
        {
            var colors = new[]
            {
                new Color(0.95f, 0.78f, 0.22f, 0.95f),
                new Color(0.35f, 0.82f, 0.95f, 0.95f),
                new Color(0.95f, 0.48f, 0.18f, 0.95f),
                new Color(0.92f, 0.22f, 0.28f, 0.95f),
            };

            for (var i = 0; i < SirAldric3DMotion.StrikeMarkerCount; i++)
            {
                var p = MarkerWorld(i);
                Gizmos.color = colors[i];
                Gizmos.DrawSphere(p, 0.09f);
                Gizmos.DrawWireSphere(p, 0.11f);
#if UNITY_EDITOR
                Handles.color = colors[i];
                Handles.Label(p + (Vector3.up * 0.16f), SirAldric3DMotion.StrikeMarkerName(i));
#endif
            }

            Gizmos.color = new Color(1f, 0.92f, 0.45f, 0.85f);
            Vector3? prev = null;
            for (var i = 0; i <= 24; i++)
            {
                var p = SampleWorld(i / 24f);
                if (prev.HasValue)
                {
                    Gizmos.DrawLine(prev.Value, p);
                }

                prev = p;
            }
        }
    }
}
