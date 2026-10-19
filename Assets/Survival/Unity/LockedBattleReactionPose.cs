using System.Collections.Generic;
using Survival.Domain.Roster;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Procedural hit reaction for one fighter: a short additive bend on the upper body, neck/head, wing roots and tail
    /// (<see cref="LockedBattleReactions.RigFor"/>) on top of whatever pose the actor's clip left this frame. Applied in the
    /// battle's LateUpdate after the clips have posed the bones; the clip value is put back first whenever nothing
    /// re-posed a bone (a held rest pose), so the bend never accumulates. Never touches a clip, an import, or a bone the legs
    /// hang from.
    /// </summary>
    public sealed class LockedBattleReactionPose
    {
        private const int SpineGroup = 0;
        private const int HeadGroup = 1;
        private const int WingGroup = 2;
        private const int TailGroup = 3;

        private readonly List<Bone> _bones = new List<Bone>();
        private readonly int[] _groupCounts = new int[4];

        public int BoneCount => _bones.Count;

        public static LockedBattleReactionPose Build(Transform root, LockedBattleReactionRig rig)
        {
            var pose = new LockedBattleReactionPose();
            pose.Add(root, rig.Spine, SpineGroup);
            pose.Add(root, rig.Head, HeadGroup);
            pose.Add(root, rig.Wings, WingGroup);
            pose.Add(root, rig.Tail, TailGroup);
            return pose;
        }

        private void Add(Transform root, string[] names, int group)
        {
            // Every match: Sir Aldric carries three rigs with the same Mixamo bone names, one per clip set. The bend is
            // shared over the names found, so each rig gets the full amount.
            var all = root.GetComponentsInChildren<Transform>(true);
            var count = 0;
            foreach (var name in names)
            {
                var found = false;
                foreach (var t in all)
                {
                    if (string.Equals(t.name, name, System.StringComparison.Ordinal))
                    {
                        _bones.Add(new Bone(t, group));
                        found = true;
                    }
                }

                if (found)
                {
                    count++;
                }
            }

            _groupCounts[group] = count;
        }

        /// <summary>
        /// Bends the bones for the current reaction (weight <paramref name="weight"/>, away direction in world space) plus the
        /// fading previous one (a combo hit that landed before the last reaction settled).
        /// </summary>
        public void Apply(
            Transform slot,
            LockedBattleReaction reaction,
            Vector3 away,
            float weight,
            LockedBattleReaction previous,
            Vector3 previousAway,
            float previousWeight)
        {
            foreach (var b in _bones)
            {
                b.Restore();
            }

            if (weight <= 1e-4f && previousWeight <= 1e-4f)
            {
                return;
            }

            foreach (var b in _bones)
            {
                var delta = Delta(b, slot, reaction, away, weight) * Delta(b, slot, previous, previousAway, previousWeight);
                b.Set(delta * b.T.rotation);
            }
        }

        /// <summary>Puts every bone back on its clip value (the fighter is reset or the battle stops).</summary>
        public void Clear()
        {
            foreach (var b in _bones)
            {
                b.Restore();
            }
        }

        private Quaternion Delta(Bone b, Transform slot, LockedBattleReaction r, Vector3 away, float w)
        {
            if (w <= 1e-4f)
            {
                return Quaternion.identity;
            }

            away.y = 0f;
            away = away.sqrMagnitude > 1e-8f ? away.normalized : -slot.forward;
            var share = 1f / Mathf.Max(1, _groupCounts[b.Group]);
            switch (b.Group)
            {
                case SpineGroup:
                    return Lean(Vector3.up, away, r.SpineDegrees * share * w);
                case HeadGroup:
                    return Lean(Vector3.up, away, r.HeadDegrees * share * w);
                case WingGroup:
                {
                    var tip = b.T.childCount > 0 ? b.T.GetChild(0).position : b.T.position;
                    var side = Vector3.Dot(tip - slot.position, slot.right) >= 0f ? slot.right : -slot.right;

                    // Gust: swept back with the wind. Otherwise: a quick raise of the wing (the flinch).
                    var toward = r.Kind == BattleReactionKind.GustLean ? away : Vector3.up;
                    return Lean(side, toward, r.WingDegrees * w);
                }

                default:
                {
                    // Tail: a sideways flick away from the hit side.
                    var sign = Vector3.Dot(away, slot.right) >= 0f ? 1f : -1f;
                    return Quaternion.AngleAxis(sign * r.TailDegrees * share * w, Vector3.up);
                }
            }
        }

        /// <summary>Rotation that tips <paramref name="axis"/> toward <paramref name="toward"/> by <paramref name="degrees"/>.</summary>
        private static Quaternion Lean(Vector3 axis, Vector3 toward, float degrees)
        {
            var tipped = axis + (toward * Mathf.Tan(Mathf.Clamp(degrees, -60f, 60f) * Mathf.Deg2Rad));
            return Quaternion.FromToRotation(axis, tipped.normalized);
        }

        private sealed class Bone
        {
            private Quaternion _base;
            private Quaternion _applied;
            private bool _has;

            public Bone(Transform t, int group)
            {
                T = t;
                Group = group;
            }

            public Transform T { get; }
            public int Group { get; }

            /// <summary>Back to the clip value when nothing re-posed the bone since the bend was set.</summary>
            public void Restore()
            {
                if (_has && T != null && Quaternion.Angle(T.localRotation, _applied) < 0.01f)
                {
                    T.localRotation = _base;
                }

                _has = false;
            }

            public void Set(Quaternion world)
            {
                _base = T.localRotation;
                T.rotation = world;
                _applied = T.localRotation;
                _has = true;
            }
        }
    }
}
