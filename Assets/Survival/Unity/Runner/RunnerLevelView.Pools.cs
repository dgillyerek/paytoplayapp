#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Survival.Domain.Runner;
using Survival.Domain.Theme;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Pools and the loading-screen prewarm. Nothing the road shows is built or destroyed mid-run: rigs (squad, enemies,
    /// captives) come off <see cref="RunnerRigPool"/> shelves, and shots, effect sprites and traps off the free lists here.
    /// They are filled by <c>PrewarmRoutine</c> (RunnerLevelView.Boot.cs) a few milliseconds per frame while the loading
    /// screen shows real progress, and the run starts only when it is done. The prewarm ends with one off-screen render
    /// of everything it built (the rigs in their poses, a shot of every look, every effect sprite, every trap), so the
    /// shaders, variants and materials the run needs are compiled and uploaded before the first frame the player sees.
    /// </summary>
    public sealed partial class RunnerLevelView
    {
        /// <summary>Effect sprites built before the run starts (a boss kill plus a box break plus sparks in one frame is ~60).</summary>
        private const int FxLaunch = 100;

        /// <summary>Effect sprites kept ready once the result-screen top-up has run.</summary>
        private const int FxFull = 200;

        /// <summary>Traps of one kind and width kept ready (each level has a handful at most).</summary>
        private const int HazardPrewarmCap = 6;

        private static readonly string[] ShotLooks = { "Lyra", "Rowan", "Oakenshield", "Bonequill", "" };

        private RunnerRigPool? _pool;

        /// <summary>Pooled shots, effects and traps live here (outside the road, which is rebuilt per level).</summary>
        private Transform? _spare;

        private readonly Stack<ShotView> _freeShots = new Stack<ShotView>();
        private readonly List<ShotView> _fadingShots = new List<ShotView>();
        private readonly Stack<Fx> _freeFx = new Stack<Fx>();
        private readonly Dictionary<(RunnerHazardKind, int), Stack<HazardView>> _freeHazards = new Dictionary<(RunnerHazardKind, int), Stack<HazardView>>();
        private readonly List<int> _scratchIds = new List<int>();
        private readonly HashSet<int> _liveIds = new HashSet<int>();
        private int _peakFoes;
        private int _peakSquad;
        private int _peakShots;

        /// <summary>
        /// Prewarm plans by level number. A plan runs the bots through the level (~0.1-0.25 s on a desktop), so the plan
        /// for the level after next is started on a worker thread as soon as a level is warmed (the sim is pure C#, no
        /// shared state) and is ready by the time Next asks for it.
        /// </summary>
        private static readonly Dictionary<int, Task<RunnerPrewarmSet>> Plans = new Dictionary<int, Task<RunnerPrewarmSet>>();

        private Task<RunnerPrewarmSet> PlanOf(RunnerLevelDef level)
        {
            if (Plans.TryGetValue(level.Number, out var task) && !task.IsFaulted && !task.IsCanceled)
            {
                return task;
            }

            var catalog = _catalog!;
#if UNITY_WEBGL && !UNITY_EDITOR
            // No worker threads on the web player.
            task = Task.FromResult(RunnerPrewarm.Sets(level, catalog, CaptiveScale));
#else
            task = Task.Run(() => RunnerPrewarm.Sets(level, catalog, CaptiveScale));
#endif
            Plans[level.Number] = task;
            return task;
        }

        /// <summary>
        /// One off-screen frame of everything just built, spread in front of the camera, so the GPU sees every
        /// material, shader variant (shadows, transparency, additive glow) and mesh before the run does.
        /// </summary>
        private void WarmRender()
        {
            if (_cam == null || _pool == null || _spare == null)
            {
                return;
            }

            var cam = _cam.transform;
            var origin = cam.position + (cam.forward * 9f);
            var right = cam.right;
            var up = cam.up;
            var shown = new List<GameObject>();
            var n = 0;
            void Place(Transform t)
            {
                // A loose grid in front of the camera: in frustum, so nothing is culled before it is drawn.
                var col = (n % 9) - 4;
                var row = n / 9 % 6;
                t.position = origin + (right * (col * 1.1f)) + (up * ((row * 1.2f) - 3f));
                n++;
            }

            foreach (var rig in _pool.Warming)
            {
                Place(rig.Slot);
            }

            var looks = 0;
            foreach (var s in _freeShots)
            {
                if (looks >= ShotLooks.Length)
                {
                    break;
                }

                Dress(s, ShotLooks[looks++]);
                s.Root.gameObject.SetActive(true);
                Place(s.Root);
                shown.Add(s.Root.gameObject);
            }

            var keys = new[] { HookArtKeys.FxBurstRing, HookArtKeys.FxGlow, HookArtKeys.FxSparkle, HookArtKeys.FxRays, HookArtKeys.FxConfetti, RunnerArtKeys.ChestOpen };
            var k = 0;
            foreach (var f in _freeFx)
            {
                if (k >= keys.Length)
                {
                    break;
                }

                f.Sprite!.sprite = SurvivalArt.Get(keys[k++]) ?? SurvivalArt.Circle();
                f.Sprite.color = new Color(1f, 1f, 1f, 0.5f);
                f.Go.SetActive(true);
                Place(f.Go.transform);
                shown.Add(f.Go);
            }

            foreach (var stack in _freeHazards.Values)
            {
                foreach (var v in stack)
                {
                    v.Root.gameObject.SetActive(true);
                    Place(v.Root);
                    shown.Add(v.Root.gameObject);
                    break;
                }
            }

            // The trap glow / warning materials in every alpha step their pulse can reach.
            for (var a = 0; a <= 10; a++)
            {
                Unlit(new Color(Warn.r, Warn.g, Warn.b, a / 10f * Warn.a));
            }

            var rt = RenderTexture.GetTemporary(256, 256, 24);
            var was = _cam.targetTexture;
            try
            {
                _cam.targetTexture = rt;
                _cam.Render();
            }
            finally
            {
                _cam.targetTexture = was;
                RenderTexture.ReleaseTemporary(rt);
            }

            foreach (var go in shown)
            {
                go.SetActive(false);
            }
        }

        // ---------- shots ----------

        private ShotView NewShot()
        {
            var go = new GameObject("Shot");
            go.transform.SetParent(_spare, false);
            var arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(arrow.GetComponent<Collider>());
            arrow.transform.SetParent(go.transform, false);
            arrow.transform.localScale = new Vector3(0.06f, 0.06f, 0.7f);
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(orb.GetComponent<Collider>());
            orb.transform.SetParent(go.transform, false);
            orb.transform.localScale = Vector3.one * 0.32f;
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.18f;
            trail.endWidth = 0f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var view = new ShotView { Root = go.transform, Arrow = arrow.GetComponent<Renderer>(), Orb = orb.GetComponent<Renderer>(), Trail = trail };
            view.Arrow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            view.Orb.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Dress(view, "");
            return view;
        }

        /// <summary>Re-dress a pooled shot as <paramref name="look"/>'s (only when the look changes).</summary>
        private static void Dress(ShotView v, string look)
        {
            if (v.Look == look)
            {
                return;
            }

            v.Look = look;
            ShotColors(look, out var core, out var glow);
            var arrow = look == "Rowan" || look == "Bonequill" || look == "Oakenshield";
            v.Head = arrow ? v.Arrow : v.Orb;
            v.Arrow!.enabled = arrow;
            v.Orb!.enabled = !arrow;
            v.Head!.sharedMaterial = Unlit(core);
            v.Trail!.startWidth = arrow ? 0.08f : 0.26f;
            v.Trail.sharedMaterial = Unlit(glow);
            v.Trail.startColor = glow;
            v.Trail.endColor = new Color(glow.r, glow.g, glow.b, 0f);
        }

        private ShotView TakeShot(RunnerShot s)
        {
            RunnerHitchLog.Note(RunnerHitchLog.Counter.Shot);
            var v = _freeShots.Count > 0 ? _freeShots.Pop() : NewShot();
            Dress(v, ShooterName(s));
            v.Head!.enabled = true;
            v.Root.position = new Vector3(s.X, ShotHeight, s.Z);
            v.Root.gameObject.SetActive(true);
            v.Trail!.Clear();
            v.Fade = 0f;
            return v;
        }

        /// <summary>The shot is spent: hide the head and let the trail finish (<see cref="TickShots"/>) rather than pop.</summary>
        private void ReleaseShot(ShotView v)
        {
            if (v.Root == null)
            {
                return;
            }

            if (v.Head != null)
            {
                v.Head.enabled = false;
            }

            v.Fade = 0.25f;
            _fadingShots.Add(v);
        }

        private void TickShots(float dt)
        {
            for (var i = _fadingShots.Count - 1; i >= 0; i--)
            {
                var v = _fadingShots[i];
                v.Fade -= dt;
                if (v.Fade > 0f)
                {
                    continue;
                }

                _fadingShots.RemoveAt(i);
                if (v.Root != null)
                {
                    v.Root.gameObject.SetActive(false);
                    _freeShots.Push(v);
                }
            }
        }

        // ---------- effect sprites ----------

        private Fx NewFx()
        {
            var go = new GameObject("Fx");
            go.transform.SetParent(_spare, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;
            return new Fx { Go = go, Sprite = sr };
        }

        private Fx TakeFx()
        {
            RunnerHitchLog.Note(RunnerHitchLog.Counter.Fx);
            Fx f;
            do
            {
                f = _freeFx.Count > 0 ? _freeFx.Pop() : NewFx();
            }
            while (f.Go == null);

            f.Go.SetActive(true);
            f.Age = 0f;
            return f;
        }

        private void ReleaseFx(Fx f)
        {
            if (f.Go == null)
            {
                return;
            }

            f.Go.SetActive(false);
            _freeFx.Push(f);
        }

        // ---------- traps ----------

        private static (RunnerHazardKind, int) HazardKey(RunnerHazardDef d) => (d.Kind, Mathf.RoundToInt(d.Width * 100f));

        private HazardView TakeHazard(RunnerHazard h)
        {
            RunnerHitchLog.Note(RunnerHitchLog.Counter.HazardSpawned);
            HazardView? v = null;
            if (_freeHazards.TryGetValue(HazardKey(h.Def), out var stack))
            {
                while (stack.Count > 0 && (v == null || v.Root == null))
                {
                    v = stack.Pop();
                }
            }

            if (v == null || v.Root == null)
            {
                v = BuildHazard(h.Def, _spare != null ? _spare : _actors!);
            }

            v.Root.position = new Vector3(h.X, 0f, h.Z);
            v.Last = RunnerHazardState.Idle;
            if (v.Body != null)
            {
                v.Body.localRotation = Quaternion.identity;
            }

            if (v.Kind == RunnerHazardKind.Log && v.Glow != null)
            {
                v.Glow.enabled = false;
            }

            if (v.Kind == RunnerHazardKind.Spikes)
            {
                foreach (var s in v.Parts)
                {
                    s.localScale = new Vector3(0.16f, 0.05f, 0.16f);
                }
            }

            v.Root.gameObject.SetActive(true);
            return v;
        }

        private void ReleaseHazard(HazardView v)
        {
            if (v.Root == null)
            {
                return;
            }

            v.Root.gameObject.SetActive(false);
            var key = HazardKey(v.Def);
            if (!_freeHazards.TryGetValue(key, out var stack))
            {
                stack = new Stack<HazardView>();
                _freeHazards[key] = stack;
            }

            stack.Push(v);
        }

        /// <summary>Every live, dying and fading thing back to its pool (a restart or a new level).</summary>
        private void ReturnAll()
        {
            foreach (var r in _members.Values)
            {
                ReturnRig(r);
            }

            foreach (var r in _foes.Values)
            {
                ReturnRig(r);
            }

            foreach (var r in _dying)
            {
                ReturnRig(r);
            }

            foreach (var b in _boxes.Values)
            {
                ReturnRig(b.Captive);
                b.Captive = null;
            }

            foreach (var s in _shots.Values)
            {
                ReleaseShot(s);
            }

            foreach (var s in _fadingShots)
            {
                if (s.Root != null)
                {
                    s.Root.gameObject.SetActive(false);
                    _freeShots.Push(s);
                }
            }

            _fadingShots.Clear();
            foreach (var f in _fx)
            {
                ReleaseFx(f);
            }

            _fx.Clear();
            foreach (var h in _hazards.Values)
            {
                ReleaseHazard(h);
            }
        }

        private void ReturnRig(RunnerRig? rig)
        {
            if (rig == null)
            {
                return;
            }

            if (_pool != null)
            {
                _pool.Return(rig);
            }
            else
            {
                rig.Destroy();
            }
        }

        /// <summary>Peaks for the run's hitch summary.</summary>
        private void TrackPeaks()
        {
            if (_run == null)
            {
                return;
            }

            _peakFoes = Mathf.Max(_peakFoes, _run.Foes.Count);
            _peakSquad = Mathf.Max(_peakSquad, _run.Squad.Count);
            _peakShots = Mathf.Max(_peakShots, _run.Shots.Count);
        }
    }
}
