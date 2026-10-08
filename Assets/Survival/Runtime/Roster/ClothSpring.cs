using System;
using System.Collections.Generic;
using System.Numerics;

namespace Survival.Domain.Roster
{
    public enum ClothColliderKind
    {
        Capsule,
        Sphere
    }

    /// <summary>
    /// One Design cloth bone chain (Design clothsplit NOTE.md "Cloth chains" table).
    /// Bones are bound by name. They hang from <see cref="ParentBone"/> and are never keyed:
    /// they move only through <see cref="ClothSpringSolver"/> at runtime.
    /// </summary>
    public sealed class ClothChainSpec
    {
        public ClothChainSpec(
            string name,
            string sector,
            string[] bones,
            string parentBone,
            float lengthMetres,
            int vertexCount,
            float stiffness,
            float damping,
            float drag,
            float gravity,
            float radiusMetres,
            float angleLimitDegrees,
            float[] designJoints)
        {
            Name = name;
            Sector = sector;
            Bones = bones;
            ParentBone = parentBone;
            LengthMetres = lengthMetres;
            VertexCount = vertexCount;
            Stiffness = stiffness;
            Damping = damping;
            Drag = drag;
            Gravity = gravity;
            RadiusMetres = radiusMetres;
            AngleLimitDegrees = angleLimitDegrees;
            DesignJoints = designJoints;
        }

        public string Name { get; }
        /// <summary>F = front, L = character-left, B = back (NOTE.md sector letters).</summary>
        public string Sector { get; }
        public string[] Bones { get; }
        public string ParentBone { get; }
        public float LengthMetres { get; }
        public int VertexCount { get; }
        public float Stiffness { get; }
        public float Damping { get; }
        public float Drag { get; }
        public float Gravity { get; }
        public float RadiusMetres { get; }
        public float AngleLimitDegrees { get; }
        /// <summary>Design-space (Blender, metres, Z up) joints: each bone head, then the last bone's tail.</summary>
        public float[] DesignJoints { get; }

        public int JointCount => Bones.Length + 1;
        public bool IsFrontStrip => Sector == "F" || Sector == "FL" || Sector == "FR";

        public Vector3 DesignJoint(int index) =>
            new Vector3(DesignJoints[index * 3], DesignJoints[(index * 3) + 1], DesignJoints[(index * 3) + 2]);

        public float DesignSegmentLength(int index) => Vector3.Distance(DesignJoint(index), DesignJoint(index + 1));
    }

    /// <summary>Leg/hip collider from the NOTE.md suggestions. Capsule runs from Bone's head to TailBone's head.</summary>
    public sealed class ClothColliderSpec
    {
        public ClothColliderSpec(string name, ClothColliderKind kind, string bone, string tailBone, float radiusMetres)
        {
            Name = name;
            Kind = kind;
            Bone = bone;
            TailBone = tailBone;
            RadiusMetres = radiusMetres;
        }

        public string Name { get; }
        public ClothColliderKind Kind { get; }
        public string Bone { get; }
        public string TailBone { get; }
        public float RadiusMetres { get; }
    }

    /// <summary>
    /// Design cloth-rule "split" pack: body mesh on the 22 mixamorig bones, plus a separate cloth mesh
    /// on its own chains parented to mixamorig:Spine. Walk/attack are baked on body bones only.
    /// Cloth bones come after Spine's children in the FBX, so all binding is by name, never by index.
    /// Derek's cloth rule: cloth moves only via runtime spring bones, never baked.
    /// </summary>
    public sealed class ClothSplitSpec
    {
        public const string DesignFolder = "clothsplit_20261007";

        public ClothSplitSpec(
            string character,
            string designDir,
            int rigBoneCount,
            float splitHeightMetres,
            string restFileName,
            string walkFileName,
            string attackFileName,
            string blendFileName,
            string restMd5,
            string walkMd5,
            string attackMd5,
            string blendMd5,
            ClothChainSpec[] chains,
            ClothColliderSpec[] colliders)
        {
            Character = character;
            DesignDir = designDir;
            RigBoneCount = rigBoneCount;
            SplitHeightMetres = splitHeightMetres;
            RestFileName = restFileName;
            WalkFileName = walkFileName;
            AttackFileName = attackFileName;
            BlendFileName = blendFileName;
            RestMd5 = restMd5;
            WalkMd5 = walkMd5;
            AttackMd5 = attackMd5;
            BlendMd5 = blendMd5;
            Chains = chains;
            Colliders = colliders;
            var names = new List<string>();
            foreach (var chain in chains)
            {
                names.AddRange(chain.Bones);
            }

            ClothBoneNames = names.ToArray();
            _cloth = new HashSet<string>(names, StringComparer.Ordinal);
        }

        private readonly HashSet<string> _cloth;

        public string Character { get; }
        public string DesignDir { get; }
        public int RigBoneCount { get; }
        public float SplitHeightMetres { get; }
        public string RestFileName { get; }
        public string WalkFileName { get; }
        public string AttackFileName { get; }
        public string BlendFileName { get; }
        public string RestMd5 { get; }
        public string WalkMd5 { get; }
        public string AttackMd5 { get; }
        public string BlendMd5 { get; }
        public ClothChainSpec[] Chains { get; }
        public ClothColliderSpec[] Colliders { get; }
        public string[] ClothBoneNames { get; }

        public bool IsClothBone(string boneName) => _cloth.Contains(boneName);

        /// <summary>Leaf name of a Unity transform path ("a/b/skirt_F_01" -> "skirt_F_01").</summary>
        public static string Leaf(string path)
        {
            var slash = path.LastIndexOf('/');
            return slash < 0 ? path : path.Substring(slash + 1);
        }

        /// <summary>NOTE.md collider suggestion shared by the three packs: thigh/shin capsules and a Hips sphere.</summary>
        public static ClothColliderSpec[] MixamoLegColliders(float thigh, float shin, float hips) => new[]
        {
            new ClothColliderSpec("LeftThigh", ClothColliderKind.Capsule, "mixamorig:LeftUpLeg", "mixamorig:LeftLeg", thigh),
            new ClothColliderSpec("LeftShin", ClothColliderKind.Capsule, "mixamorig:LeftLeg", "mixamorig:LeftFoot", shin),
            new ClothColliderSpec("RightThigh", ClothColliderKind.Capsule, "mixamorig:RightUpLeg", "mixamorig:RightLeg", thigh),
            new ClothColliderSpec("RightShin", ClothColliderKind.Capsule, "mixamorig:RightLeg", "mixamorig:RightFoot", shin),
            new ClothColliderSpec("Hips", ClothColliderKind.Sphere, "mixamorig:Hips", "mixamorig:Hips", hips)
        };
    }

    /// <summary>
    /// By-name humanoid mapping for the 22 mixamorig body bones (Unity HumanTrait names).
    /// Used to reject an auto-generated avatar that mapped a cloth bone or the wrong body bone.
    /// </summary>
    public static class MixamoHumanMap
    {
        /// <summary>Human bones that must map to exactly this mixamorig bone.</summary>
        public static readonly KeyValuePair<string, string>[] Required =
        {
            Pair("Hips", "mixamorig:Hips"),
            Pair("Spine", "mixamorig:Spine"),
            Pair("Neck", "mixamorig:Neck"),
            Pair("Head", "mixamorig:Head"),
            Pair("LeftUpperArm", "mixamorig:LeftArm"),
            Pair("LeftLowerArm", "mixamorig:LeftForeArm"),
            Pair("LeftHand", "mixamorig:LeftHand"),
            Pair("RightUpperArm", "mixamorig:RightArm"),
            Pair("RightLowerArm", "mixamorig:RightForeArm"),
            Pair("RightHand", "mixamorig:RightHand"),
            Pair("LeftUpperLeg", "mixamorig:LeftUpLeg"),
            Pair("LeftLowerLeg", "mixamorig:LeftLeg"),
            Pair("LeftFoot", "mixamorig:LeftFoot"),
            Pair("RightUpperLeg", "mixamorig:RightUpLeg"),
            Pair("RightLowerLeg", "mixamorig:RightLeg"),
            Pair("RightFoot", "mixamorig:RightFoot")
        };

        /// <summary>Optional human bones: when mapped, they must map to one of these mixamorig bones.</summary>
        public static readonly KeyValuePair<string, string[]>[] Optional =
        {
            new KeyValuePair<string, string[]>("Chest", new[] { "mixamorig:Spine1", "mixamorig:Spine2" }),
            new KeyValuePair<string, string[]>("UpperChest", new[] { "mixamorig:Spine2" }),
            new KeyValuePair<string, string[]>("LeftShoulder", new[] { "mixamorig:LeftShoulder" }),
            new KeyValuePair<string, string[]>("RightShoulder", new[] { "mixamorig:RightShoulder" }),
            new KeyValuePair<string, string[]>("LeftToes", new[] { "mixamorig:LeftToeBase" }),
            new KeyValuePair<string, string[]>("RightToes", new[] { "mixamorig:RightToeBase" })
        };

        /// <summary>Returns null when the (humanName, boneName) mapping is a correct by-name mixamorig mapping.</summary>
        public static string? Validate(IEnumerable<KeyValuePair<string, string>> human, Func<string, bool> isClothBone)
        {
            var seen = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in human)
            {
                if (isClothBone(pair.Value))
                {
                    return "cloth bone " + pair.Value + " mapped as human " + pair.Key;
                }

                seen[pair.Key] = pair.Value;
            }

            foreach (var req in Required)
            {
                if (!seen.TryGetValue(req.Key, out var bone))
                {
                    return "human " + req.Key + " not mapped";
                }

                if (!string.Equals(bone, req.Value, StringComparison.Ordinal))
                {
                    return "human " + req.Key + " mapped to " + bone + ", want " + req.Value;
                }
            }

            foreach (var opt in Optional)
            {
                if (seen.TryGetValue(opt.Key, out var bone) && Array.IndexOf(opt.Value, bone) < 0)
                {
                    return "human " + opt.Key + " mapped to " + bone;
                }
            }

            return null;
        }

        private static KeyValuePair<string, string> Pair(string human, string bone) => new KeyValuePair<string, string>(human, bone);
    }

    /// <summary>Collider in world space for one solver step. Sphere when A == B.</summary>
    public readonly struct ClothCollider
    {
        public ClothCollider(Vector3 a, Vector3 b, float radius)
        {
            A = a;
            B = b;
            Radius = radius;
        }

        public Vector3 A { get; }
        public Vector3 B { get; }
        public float Radius { get; }
    }

    /// <summary>
    /// Damped Verlet spring chain (DynamicBone-style), pure math so it runs in dotnet tests.
    /// Particle 0 is the chain root (bone _01 head) and follows the animated parent exactly.
    /// Particles 1..n are the other bone heads and the last bone's tail.
    /// NOTE.md values (0-1) map as:
    ///   stiffness -> pull toward the animated rest pose, stiffness * <see cref="ElasticityPerStep"/> per 60 Hz step,
    ///                plus a max deviation of 2 * (1 - stiffness) * segment length;
    ///   damping   -> velocity loss per 60 Hz step (velocity *= 1 - damping);
    ///   drag      -> lag behind the character's own world movement (particles inherit 1 - drag of the root move);
    ///   gravity   -> gravity * <see cref="GravityStepMetres"/> downward per 60 Hz step squared;
    ///   radius    -> particle/segment collision radius;
    ///   angle     -> per-joint cap vs the animated rest direction (60 deg front strips, 75 deg back/side).
    /// Collision is rest-relative: a segment is never pushed further from a collider than it sat in the rest pose,
    /// so the authored rest look is untouched while legs cannot push in deeper than rest.
    /// Fixed 60 Hz sub-steps (max 4 per frame); a frame longer than <see cref="ResetAfterSeconds"/> resets instead.
    /// </summary>
    public sealed class ClothSpringSolver
    {
        public const float StepSeconds = 1f / 60f;
        public const int MaxSubsteps = 4;
        public const float ResetAfterSeconds = 0.25f;
        public const float ElasticityPerStep = 0.3f;
        public const float GravityStepMetres = 0.005f;
        public const int ConstraintIterations = 2;

        private readonly ClothChainSpec _chain;
        private readonly float _scale;
        private readonly Vector3[] _p;
        private readonly Vector3[] _prev;
        private readonly Vector3[] _target;
        private readonly Vector3[] _lastTarget;
        private readonly Vector3[] _stepTarget;
        private readonly float[] _restLength;
        private float[,] _allowed = new float[0, 0];
        private ClothCollider[] _lastColliders = Array.Empty<ClothCollider>();
        private ClothCollider[] _stepColliders = Array.Empty<ClothCollider>();
        private bool _ready;

        public ClothSpringSolver(ClothChainSpec chain, float designToWorld)
        {
            _chain = chain;
            _scale = designToWorld;
            var n = chain.JointCount;
            _p = new Vector3[n];
            _prev = new Vector3[n];
            _target = new Vector3[n];
            _lastTarget = new Vector3[n];
            _stepTarget = new Vector3[n];
            _restLength = new float[n - 1];
        }

        public ClothChainSpec Chain => _chain;
        public int Count => _p.Length;
        public bool Ready => _ready;
        public Vector3 this[int index] => _p[index];

        /// <summary>Allowed (rest-relative) clearance of segment i from collider j, world units.</summary>
        public float Allowed(int segment, int collider) => _allowed[segment, collider];

        /// <summary>Capture rest segment lengths and rest-relative collider clearance, then reset.</summary>
        public void Bind(Vector3[] restTargets, ClothCollider[] restColliders)
        {
            Check(restTargets);
            for (var i = 0; i < _restLength.Length; i++)
            {
                _restLength[i] = Vector3.Distance(restTargets[i], restTargets[i + 1]);
            }

            var radius = _chain.RadiusMetres * _scale;
            _allowed = new float[_restLength.Length, restColliders.Length];
            for (var i = 0; i < _restLength.Length; i++)
            {
                for (var j = 0; j < restColliders.Length; j++)
                {
                    var c = restColliders[j];
                    var rest = SegmentSegmentDistance(restTargets[i], restTargets[i + 1], c.A, c.B, out _, out _);
                    _allowed[i, j] = Math.Max(0f, Math.Min(c.Radius + radius, rest));
                }
            }

            _lastColliders = (ClothCollider[])restColliders.Clone();
            _stepColliders = new ClothCollider[restColliders.Length];
            Reset(restTargets);
        }

        /// <summary>Snap every particle to the animated rest pose with zero velocity (pose switch, hitch).</summary>
        public void Reset(Vector3[] targets)
        {
            Check(targets);
            for (var i = 0; i < _p.Length; i++)
            {
                _p[i] = targets[i];
                _prev[i] = targets[i];
                _target[i] = targets[i];
                _lastTarget[i] = targets[i];
            }

            _ready = true;
        }

        public void Step(float dt, Vector3[] targets, ClothCollider[] colliders, Vector3 down, Vector3 rootDelta)
        {
            Check(targets);
            if (!_ready || dt > ResetAfterSeconds || colliders.Length != _lastColliders.Length)
            {
                Reset(targets);
                _lastColliders = (ClothCollider[])colliders.Clone();
                _stepColliders = new ClothCollider[colliders.Length];
                return;
            }

            if (dt <= 0f)
            {
                return;
            }

            for (var i = 0; i < _p.Length; i++)
            {
                _lastTarget[i] = _target[i];
                _target[i] = targets[i];
            }

            var steps = (int)Math.Ceiling(dt / StepSeconds - 1e-4);
            steps = Math.Max(1, Math.Min(MaxSubsteps, steps));
            var h = dt / steps;
            var k = h / StepSeconds;
            var retention = (float)Math.Pow(1.0 - _chain.Damping, k);
            var elastic = 1f - (float)Math.Pow(1.0 - (_chain.Stiffness * ElasticityPerStep), k);
            var gravity = down * (_chain.Gravity * GravityStepMetres * _scale * k * k);
            var inherit = rootDelta * ((1f - _chain.Drag) / steps);
            var limit = _chain.AngleLimitDegrees * (float)Math.PI / 180f;
            var maxDevScale = 2f * (1f - _chain.Stiffness);

            for (var s = 1; s <= steps; s++)
            {
                var a = (float)s / steps;
                for (var i = 0; i < _p.Length; i++)
                {
                    _stepTarget[i] = Vector3.Lerp(_lastTarget[i], _target[i], a);
                }

                for (var j = 0; j < colliders.Length; j++)
                {
                    var from = _lastColliders[j];
                    var to = colliders[j];
                    _stepColliders[j] = new ClothCollider(Vector3.Lerp(from.A, to.A, a), Vector3.Lerp(from.B, to.B, a), to.Radius);
                }

                _p[0] = _stepTarget[0];
                _prev[0] = _stepTarget[0];
                for (var i = 1; i < _p.Length; i++)
                {
                    var v = (_p[i] - _prev[i]) * retention;
                    _prev[i] = _p[i] + inherit;
                    var p = _p[i] + v + gravity + inherit;
                    p += (_stepTarget[i] - p) * elastic;
                    var dev = p - _stepTarget[i];
                    var maxDev = _restLength[i - 1] * maxDevScale;
                    var devLen = dev.Length();
                    if (devLen > maxDev && devLen > 1e-9f)
                    {
                        p = _stepTarget[i] + (dev * (maxDev / devLen));
                    }

                    _p[i] = p;
                }

                for (var iter = 0; iter < ConstraintIterations; iter++)
                {
                    for (var i = 1; i < _p.Length; i++)
                    {
                        LimitAngle(i, limit);
                        Collide(i);
                        KeepLength(i);
                    }
                }
            }

            Array.Copy(colliders, _lastColliders, colliders.Length);
        }

        private void LimitAngle(int i, float limit)
        {
            var rest = _stepTarget[i] - _stepTarget[i - 1];
            var dir = _p[i] - _p[i - 1];
            var restLen = rest.Length();
            var dirLen = dir.Length();
            if (restLen < 1e-9f || dirLen < 1e-9f)
            {
                return;
            }

            var r = rest / restLen;
            var d = dir / dirLen;
            var cos = Math.Max(-1f, Math.Min(1f, Vector3.Dot(r, d)));
            var angle = (float)Math.Acos(cos);
            if (angle <= limit)
            {
                return;
            }

            var axis = Vector3.Cross(r, d);
            var axisLen = axis.Length();
            if (axisLen < 1e-9f)
            {
                _p[i] = _p[i - 1] + (r * dirLen);
                return;
            }

            var q = Quaternion.CreateFromAxisAngle(axis / axisLen, limit);
            _p[i] = _p[i - 1] + (Vector3.Transform(r, q) * dirLen);
        }

        private void Collide(int i)
        {
            for (var j = 0; j < _stepColliders.Length; j++)
            {
                var allowed = _allowed[i - 1, j];
                if (allowed <= 0f)
                {
                    continue;
                }

                var c = _stepColliders[j];
                var dist = SegmentSegmentDistance(_p[i - 1], _p[i], c.A, c.B, out var onSegment, out var t);
                if (dist >= allowed)
                {
                    continue;
                }

                var onAxis = ClosestOnSegment(c.A, c.B, onSegment);
                var normal = onSegment - onAxis;
                var nLen = normal.Length();
                if (nLen < 1e-9f)
                {
                    normal = _p[i] - onAxis;
                    nLen = normal.Length();
                    if (nLen < 1e-9f)
                    {
                        continue;
                    }
                }

                normal /= nLen;
                var push = (allowed - dist) / Math.Max(t, 0.5f);
                _p[i] += normal * push;
            }
        }

        private void KeepLength(int i)
        {
            var dir = _p[i] - _p[i - 1];
            var len = dir.Length();
            if (len < 1e-9f)
            {
                _p[i] = _p[i - 1] + (_stepTarget[i] - _stepTarget[i - 1]);
                return;
            }

            _p[i] = _p[i - 1] + (dir * (_restLength[i - 1] / len));
        }

        private void Check(Vector3[] targets)
        {
            if (targets.Length != _p.Length)
            {
                throw new ArgumentException(_chain.Name + " expects " + _p.Length + " joints, got " + targets.Length);
            }
        }

        public static Vector3 ClosestOnSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            var ab = b - a;
            var len2 = Vector3.Dot(ab, ab);
            if (len2 < 1e-12f)
            {
                return a;
            }

            var t = Math.Max(0f, Math.Min(1f, Vector3.Dot(p - a, ab) / len2));
            return a + (ab * t);
        }

        /// <summary>Closest distance between segment PQ and segment AB. Returns the point on PQ and its parameter t (0 at P).</summary>
        public static float SegmentSegmentDistance(Vector3 p, Vector3 q, Vector3 a, Vector3 b, out Vector3 onPq, out float t)
        {
            var d1 = q - p;
            var d2 = b - a;
            var r = p - a;
            var aa = Vector3.Dot(d1, d1);
            var ee = Vector3.Dot(d2, d2);
            var ff = Vector3.Dot(d2, r);
            float s;
            float u;
            if (aa <= 1e-12f && ee <= 1e-12f)
            {
                onPq = p;
                t = 0f;
                return Vector3.Distance(p, a);
            }

            if (aa <= 1e-12f)
            {
                s = 0f;
                u = Clamp01(ff / ee);
            }
            else
            {
                var cc = Vector3.Dot(d1, r);
                if (ee <= 1e-12f)
                {
                    u = 0f;
                    s = Clamp01(-cc / aa);
                }
                else
                {
                    var bb = Vector3.Dot(d1, d2);
                    var denom = (aa * ee) - (bb * bb);
                    s = denom > 1e-12f ? Clamp01(((bb * ff) - (cc * ee)) / denom) : 0f;
                    u = ((bb * s) + ff) / ee;
                    if (u < 0f)
                    {
                        u = 0f;
                        s = Clamp01(-cc / aa);
                    }
                    else if (u > 1f)
                    {
                        u = 1f;
                        s = Clamp01((bb - cc) / aa);
                    }
                }
            }

            onPq = p + (d1 * s);
            t = s;
            return Vector3.Distance(onPq, a + (d2 * u));
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
