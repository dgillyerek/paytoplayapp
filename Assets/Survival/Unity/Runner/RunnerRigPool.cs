#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Built rigs kept for reuse, one shelf per (roster, extra scale). Building a locked battle rig is the most expensive
    /// thing the runner does (FBX instance, materials, avatar, playable graph, reaction bones, and on the first swing the
    /// attack props): it happens here, under the loading screen, for as many of each as the level can ever show at
    /// once (<see cref="Survival.Domain.Runner.RunnerPrewarm"/>). On the road a rig is taken off its shelf and returned
    /// when it dies, drops behind, or the run restarts; a miss (a shelf ran dry) still builds, and the hitch log says so.
    /// </summary>
    public sealed class RunnerRigPool
    {
        private readonly Transform _shelf;
        private readonly Dictionary<(string, int), Stack<RunnerRig>> _free = new Dictionary<(string, int), Stack<RunnerRig>>();
        private readonly Dictionary<(string, int), int> _owned = new Dictionary<(string, int), int>();
        private readonly List<RunnerRig> _warming = new List<RunnerRig>();

        public RunnerRigPool(Transform parent)
        {
            var shelf = new GameObject("RunnerRigPool");
            shelf.transform.SetParent(parent, false);
            // Far below the road: parked rigs have their renderers off anyway; this keeps their bounds out of every cull.
            shelf.transform.position = new Vector3(0f, -500f, 0f);
            _shelf = shelf.transform;
        }

        public Transform Shelf => _shelf;

        /// <summary>Rigs built since the pool was made (prewarm plus misses).</summary>
        public int Built { get; private set; }

        /// <summary>Rigs built on the road because a shelf was empty.</summary>
        public int Misses { get; private set; }

        public static int ScaleKey(float extraScale) => Mathf.RoundToInt(extraScale * 1000f);

        public int Owned(string roster, float extraScale) =>
            _owned.TryGetValue((roster, ScaleKey(extraScale)), out var n) ? n : 0;

        /// <summary>A rig of <paramref name="roster"/> at <paramref name="extraScale"/> placed under <paramref name="parent"/>.</summary>
        public RunnerRig? Take(string roster, Transform parent, string name, float extraScale = 1f)
        {
            var key = (roster, ScaleKey(extraScale));
            if (_free.TryGetValue(key, out var stack))
            {
                while (stack.Count > 0)
                {
                    var rig = stack.Pop();
                    if (rig.Slot == null)
                    {
                        Forget(key);
                        continue;
                    }

                    rig.Unpark(parent, name);
                    RunnerHitchLog.Note(RunnerHitchLog.Counter.RigReused);
                    return rig;
                }
            }

            Misses++;
            RunnerHitchLog.NoteRigBuilt(roster, prewarm: false);
            return Build(key, roster, parent, name, extraScale);
        }

        /// <summary>Back on its shelf (parked); a rig not from this pool is destroyed.</summary>
        public void Return(RunnerRig? rig)
        {
            if (rig == null || rig.Slot == null)
            {
                return;
            }

            if (!_owned.ContainsKey(rig.PoolKey))
            {
                rig.Destroy();
                return;
            }

            rig.Park(_shelf);
            if (!_free.TryGetValue(rig.PoolKey, out var stack))
            {
                stack = new Stack<RunnerRig>();
                _free[rig.PoolKey] = stack;
            }

            if (!stack.Contains(rig))
            {
                stack.Push(rig);
            }
        }

        /// <summary>Build (parked) until this pool owns at least <paramref name="count"/>; returns the rigs built now.</summary>
        public List<RunnerRig> Prewarm(string roster, float extraScale, int count)
        {
            var made = new List<RunnerRig>();
            var key = (roster, ScaleKey(extraScale));
            var tries = 0;
            while (Owned(roster, extraScale) < count && tries++ < count + 2)
            {
                RunnerHitchLog.NoteRigBuilt(roster, prewarm: true);
                var rig = Build(key, roster, _shelf, roster + "_Pooled", extraScale);
                if (rig == null)
                {
                    break;
                }

                rig.WarmPoses();
                made.Add(rig);
                _warming.Add(rig);
            }

            return made;
        }

        /// <summary>
        /// Build one more rig of <paramref name="roster"/> (parked at once, unless <paramref name="keepForWarm"/>: the
        /// shader warm-up draws one of each kind, then <see cref="FinishWarm"/> shelves it). One rig per call, so the boot
        /// can build under a frame budget and keep its loading bar moving. Null when the roster has no rig.
        /// </summary>
        public RunnerRig? PrewarmOne(string roster, float extraScale, bool keepForWarm)
        {
            RunnerHitchLog.NoteRigBuilt(roster, prewarm: true);
            var rig = Build((roster, ScaleKey(extraScale)), roster, _shelf, roster + "_Pooled", extraScale);
            if (rig == null)
            {
                return null;
            }

            rig.WarmPoses();
            if (keepForWarm)
            {
                _warming.Add(rig);
            }
            else
            {
                Return(rig);
            }

            return rig;
        }

        /// <summary>Prewarmed rigs not yet on a shelf: still drawn, so the caller can show them once to warm shaders.</summary>
        public IReadOnlyList<RunnerRig> Warming => _warming;

        /// <summary>Shelve every prewarmed rig (after the warm-up frames).</summary>
        public void FinishWarm()
        {
            foreach (var rig in _warming)
            {
                Return(rig);
            }

            _warming.Clear();
        }

        private RunnerRig? Build((string, int) key, string roster, Transform parent, string name, float extraScale)
        {
            var rig = RunnerRig.Spawn(roster, parent, name, extraScale);
            if (rig == null)
            {
                return null;
            }

            Built++;
            rig.PoolKey = key;
            _owned[key] = (_owned.TryGetValue(key, out var n) ? n : 0) + 1;
            return rig;
        }

        private void Forget((string, int) key)
        {
            if (_owned.TryGetValue(key, out var n))
            {
                _owned[key] = Mathf.Max(0, n - 1);
            }
        }
    }
}
