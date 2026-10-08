using System.Collections.Generic;
using Survival.Domain.Roster;
using UnityEngine;
using NVector3 = System.Numerics.Vector3;

namespace Survival.Unity
{
    /// <summary>
    /// Runtime spring bones for a Design cloth-split rig (Derek's cloth rule: cloth moves only here, never baked).
    /// Binds every chain bone and leg collider BY NAME (cloth bones are interleaved after Spine's children in the FBX,
    /// so index order is meaningless). Each LateUpdate, after the actor evaluated the body animation in Update:
    /// restore the authored rest local pose of every cloth bone (drops any constant FBX keys), read the animated-rest
    /// joints, step <see cref="ClothSpringSolver"/> (60 Hz sub-steps), then rotate each bone toward its particle.
    /// <see cref="ResetToRest"/> snaps all chains with zero velocity on the next LateUpdate (dropdown pose switch).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ClothSpringRig : MonoBehaviour
    {
        private sealed class Chain
        {
            public Chain(ClothChainSpec spec, Transform[] bones)
            {
                Spec = spec;
                Bones = bones;
                RestLocalRotation = new Quaternion[bones.Length];
                RestLocalPosition = new Vector3[bones.Length];
                Targets = new NVector3[bones.Length + 1];
            }

            public ClothChainSpec Spec { get; }
            public Transform[] Bones { get; }
            public Quaternion[] RestLocalRotation { get; }
            public Vector3[] RestLocalPosition { get; }
            public Vector3 TailLocal { get; set; }
            public NVector3[] Targets { get; }
            public ClothSpringSolver? Solver { get; set; }
        }

        private sealed class ColliderBinding
        {
            public ColliderBinding(ClothColliderSpec spec, Transform head, Transform tail)
            {
                Spec = spec;
                Head = head;
                Tail = tail;
            }

            public ClothColliderSpec Spec { get; }
            public Transform Head { get; }
            public Transform Tail { get; }
        }

        private readonly List<Chain> _chains = new List<Chain>();
        private readonly List<ColliderBinding> _colliders = new List<ColliderBinding>();
        private readonly List<string> _missing = new List<string>();
        private ClothCollider[] _colliderFrame = System.Array.Empty<ClothCollider>();
        private ClothSplitSpec? _spec;
        private Transform? _actor;
        private Vector3 _lastActorPosition;
        private float _designToWorld = 1f;
        private bool _bound;
        private bool _resetPending = true;

        public bool Bound => _bound;
        public int ChainCount => _chains.Count;
        public int ColliderCount => _colliders.Count;
        public float DesignToWorld => _designToWorld;
        public IReadOnlyList<string> MissingBones => _missing;

        /// <summary>Bind by name while the rig is in its bind (rest) pose, right after Instantiate.</summary>
        public bool Bind(ClothSplitSpec spec, Transform actor)
        {
            _spec = spec;
            _actor = actor;
            _chains.Clear();
            _colliders.Clear();
            _missing.Clear();
            var byName = new Dictionary<string, Transform>(System.StringComparer.Ordinal);
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (byName.ContainsKey(t.name))
                {
                    Debug.LogWarning(spec.Character + " cloth: duplicate transform name " + t.name + ". Using the first.");
                    continue;
                }

                byName[t.name] = t;
            }

            foreach (var chainSpec in spec.Chains)
            {
                var bones = new Transform[chainSpec.Bones.Length];
                var ok = byName.ContainsKey(chainSpec.ParentBone);
                if (!ok)
                {
                    _missing.Add(chainSpec.ParentBone);
                }

                for (var i = 0; i < bones.Length; i++)
                {
                    if (byName.TryGetValue(chainSpec.Bones[i], out var bone))
                    {
                        bones[i] = bone;
                    }
                    else
                    {
                        _missing.Add(chainSpec.Bones[i]);
                        ok = false;
                    }
                }

                if (ok && bones[0].parent != byName[chainSpec.ParentBone])
                {
                    Debug.LogWarning(spec.Character + " cloth: " + chainSpec.Bones[0] + " is not parented to " + chainSpec.ParentBone + ".");
                }

                if (ok)
                {
                    _chains.Add(new Chain(chainSpec, bones));
                }
            }

            foreach (var colliderSpec in spec.Colliders)
            {
                if (byName.TryGetValue(colliderSpec.Bone, out var head) && byName.TryGetValue(colliderSpec.TailBone, out var tail))
                {
                    _colliders.Add(new ColliderBinding(colliderSpec, head, tail));
                }
                else
                {
                    _missing.Add(colliderSpec.Bone + "/" + colliderSpec.TailBone);
                }
            }

            if (_missing.Count > 0)
            {
                Debug.LogError(spec.Character + " cloth: missing bones by name: " + string.Join(", ", _missing));
            }

            _designToWorld = MeasureDesignToWorld();
            foreach (var chain in _chains)
            {
                for (var i = 0; i < chain.Bones.Length; i++)
                {
                    chain.RestLocalRotation[i] = chain.Bones[i].localRotation;
                    chain.RestLocalPosition[i] = chain.Bones[i].localPosition;
                }

                chain.TailLocal = MeasureTailLocal(chain);
            }

            _colliderFrame = new ClothCollider[_colliders.Count];
            ReadColliders();
            foreach (var chain in _chains)
            {
                ReadTargets(chain);
                chain.Solver = new ClothSpringSolver(chain.Spec, _designToWorld);
                chain.Solver.Bind(chain.Targets, _colliderFrame);
            }

            _lastActorPosition = actor.position;
            _bound = _chains.Count > 0;
            _resetPending = true;
            Debug.Log(
                spec.Character + " cloth springs: " + _chains.Count + "/" + spec.Chains.Length + " chains, " +
                _colliders.Count + " colliders, design->world " + _designToWorld.ToString("0.000") + ".");
            return _bound && _missing.Count == 0;
        }

        /// <summary>Snap every chain to the animated rest pose with zero velocity on the next LateUpdate.</summary>
        public void ResetToRest()
        {
            _resetPending = true;
        }

        private void OnEnable()
        {
            _resetPending = true;
        }

        private void LateUpdate()
        {
            if (!_bound || _actor == null)
            {
                return;
            }

            // Cloth is never baked: start every frame from the authored rest locals under the animated Spine.
            foreach (var chain in _chains)
            {
                for (var i = 0; i < chain.Bones.Length; i++)
                {
                    chain.Bones[i].localRotation = chain.RestLocalRotation[i];
                    chain.Bones[i].localPosition = chain.RestLocalPosition[i];
                }
            }

            ReadColliders();
            var actorPosition = _actor.position;
            var rootDelta = ToN(actorPosition - _lastActorPosition);
            _lastActorPosition = actorPosition;
            var dt = Time.unscaledDeltaTime;
            var down = ToN(Vector3.down);
            foreach (var chain in _chains)
            {
                ReadTargets(chain);
                if (chain.Solver == null)
                {
                    continue;
                }

                if (_resetPending)
                {
                    chain.Solver.Reset(chain.Targets);
                }
                else
                {
                    chain.Solver.Step(dt, chain.Targets, _colliderFrame, down, rootDelta);
                }

                Apply(chain);
            }

            _resetPending = false;
        }

        private void Apply(Chain chain)
        {
            var solver = chain.Solver!;
            for (var i = 0; i < chain.Bones.Length; i++)
            {
                var bone = chain.Bones[i];
                var child = i + 1 < chain.Bones.Length ? chain.Bones[i + 1].position : bone.TransformPoint(chain.TailLocal);
                var from = child - bone.position;
                var to = ToU(solver[i + 1]) - bone.position;
                if (from.sqrMagnitude < 1e-12f || to.sqrMagnitude < 1e-12f)
                {
                    continue;
                }

                bone.rotation = Quaternion.FromToRotation(from, to) * bone.rotation;
            }
        }

        private void ReadTargets(Chain chain)
        {
            for (var i = 0; i < chain.Bones.Length; i++)
            {
                chain.Targets[i] = ToN(chain.Bones[i].position);
            }

            var last = chain.Bones[chain.Bones.Length - 1];
            chain.Targets[chain.Bones.Length] = ToN(last.TransformPoint(chain.TailLocal));
        }

        private void ReadColliders()
        {
            for (var i = 0; i < _colliders.Count; i++)
            {
                var c = _colliders[i];
                var radius = c.Spec.RadiusMetres * _designToWorld;
                var tail = c.Spec.Kind == ClothColliderKind.Sphere ? c.Head.position : c.Tail.position;
                _colliderFrame[i] = new ClothCollider(ToN(c.Head.position), ToN(tail), radius);
            }
        }

        /// <summary>World units per Design metre, from bone-head spacing vs Design joints (FBX unit scale varies).</summary>
        private float MeasureDesignToWorld()
        {
            var world = 0f;
            var design = 0f;
            foreach (var chain in _chains)
            {
                for (var i = 0; i + 1 < chain.Bones.Length; i++)
                {
                    world += Vector3.Distance(chain.Bones[i].position, chain.Bones[i + 1].position);
                    design += chain.Spec.DesignSegmentLength(i);
                }
            }

            if (design < 1e-6f || world < 1e-9f)
            {
                Debug.LogWarning((_spec?.Character ?? "Cloth") + " cloth: could not measure design->world scale. Using 1.");
                return 1f;
            }

            return world / design;
        }

        /// <summary>
        /// Last bone's tail in its local space: along the bone's local +Y (Blender FBX primary bone axis) when that axis
        /// matches the chain direction, else extrapolated from the previous segment. Length from the Design joints.
        /// </summary>
        private Vector3 MeasureTailLocal(Chain chain)
        {
            var n = chain.Bones.Length;
            var last = chain.Bones[n - 1];
            var length = chain.Spec.DesignSegmentLength(n - 1) * _designToWorld;
            var dir = last.up;
            if (n >= 2)
            {
                var prev = chain.Bones[n - 2];
                var seg = last.position - prev.position;
                if (seg.sqrMagnitude > 1e-12f && Vector3.Dot(prev.up, seg.normalized) < 0.9f)
                {
                    Debug.LogWarning(chain.Spec.Name + ": bone +Y does not follow the chain. Extrapolating the tail.");
                    dir = seg.normalized;
                }
            }

            return last.InverseTransformPoint(last.position + (dir * length));
        }

        private static NVector3 ToN(Vector3 v) => new NVector3(v.x, v.y, v.z);

        private static Vector3 ToU(NVector3 v) => new Vector3(v.X, v.Y, v.Z);

        private void OnDrawGizmosSelected()
        {
            if (!_bound)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.8f);
            foreach (var c in _colliderFrame)
            {
                var a = ToU(c.A);
                var b = ToU(c.B);
                Gizmos.DrawWireSphere(a, c.Radius);
                Gizmos.DrawWireSphere(b, c.Radius);
                Gizmos.DrawLine(a, b);
            }

            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
            foreach (var chain in _chains)
            {
                if (chain.Solver == null)
                {
                    continue;
                }

                for (var i = 1; i < chain.Solver.Count; i++)
                {
                    Gizmos.DrawLine(ToU(chain.Solver[i - 1]), ToU(chain.Solver[i]));
                }
            }
        }
    }
}
