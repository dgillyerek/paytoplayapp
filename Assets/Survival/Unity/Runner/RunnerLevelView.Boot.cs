#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Survival.Domain.Runner;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Survival.Unity
{
    /// <summary>
    /// The runner's boot and level switch, spread over frames so the loading screen is live.
    /// Before this, <c>Start</c> built every pooled object in one call (10.5 s on a laptop for level 1): the frame never
    /// ended, so the loading screen's bar and fade could not move ("it just sits there"). Now each step does about
    /// <see cref="RunnerBootProgress.FrameBudgetMs"/> of work per frame (a rig is one unit and takes one frame by itself),
    /// reports to a <see cref="RunnerBootProgress"/> the loading screen reads, and the run starts only when the last step is
    /// done. There is a minimum time (<see cref="RunnerBootProgress.MinSeconds"/>) and a hard timeout
    /// (<see cref="RunnerBootProgress.TimeoutSeconds"/>): past it the remaining pools are skipped, a pool miss builds
    /// that rig live (logged) and the run starts anyway. The loading screen's own 20 s self-clear stays as the last resort.
    /// What the launch plan leaves out (the margin on every peak, the next level's extras) is topped up on the result screen
    /// by <see cref="TopUpRoutine"/>, never mid-run: building a rig is itself a frame-long hitch.
    /// </summary>
    public sealed partial class RunnerLevelView
    {
        private sealed class BootTimes
        {
            public double Plan;
            public double Rigs;
            public double Shots;
            public double Fx;
            public double Traps;
            public double Shaders;
            public double Hold;
            public int RigCount;
            public int ShotCount;
            public int FxCount;
            public int TrapCount;
            public int Frames;
            public double WorstFrame;
            public double SlowestRig;
            public string SlowestRoster = "";
        }

        private readonly struct RigJob
        {
            public RigJob(string roster, float scale, bool warm)
            {
                Roster = roster;
                Scale = scale;
                Warm = warm;
            }

            public string Roster { get; }
            public float Scale { get; }
            public bool Warm { get; }
        }

        private Coroutine? _topUp;
        private bool _switching;

        private static double MsSince(long start) => (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;

        private static double ElapsedSeconds(long start) => MsSince(start) / 1000.0;

        private void EnsurePools()
        {
            _pool ??= new RunnerRigPool(transform);
            if (_spare == null)
            {
                _spare = new GameObject("RunnerSpare").transform;
                _spare.SetParent(transform, false);
            }
        }

        private void Publish(RunnerBootProgress p, BattleLoadingScreen? screen)
        {
            if (screen == null)
            {
                return;
            }

            screen.Progress(RunnerBootProgress.Bar(1f, p.Fraction));
            screen.SetStatus(RunnerBoot.Status(p));
        }

        /// <summary>True once the hard timeout has passed: the caller stops building and starts the run with what it has.</summary>
        private bool TimeUp(long bootStart, RunnerBootProgress p)
        {
            if (p.TimedOut)
            {
                return true;
            }

            if (RunnerBootProgress.Decide(ElapsedSeconds(bootStart), false) != RunnerBootVerdict.TimedOut)
            {
                return false;
            }

            p.MarkTimedOut();
            Debug.LogWarning(RunnerHitchLog.Prefix + " prewarm TIMED OUT after " + RunnerBootProgress.TimeoutSeconds
                + " s at step " + p.Step + " " + p.StepDone + "/" + p.StepTotal + ": starting the run with what is built (anything missing builds live and is logged as a pool miss).");
            return true;
        }

        /// <summary>Rigs still to build for <paramref name="plan"/> (squad, foe and captive entries of one roster and size share a shelf).</summary>
        private List<RigJob> RigJobs(IReadOnlyList<RunnerPrewarmEntry> plan)
        {
            var want = new Dictionary<(string, int), (string Roster, float Scale, int Count)>();
            var order = new List<(string, int)>();
            foreach (var e in plan)
            {
                var key = (e.Roster, RunnerRigPool.ScaleKey(e.Scale));
                if (want.TryGetValue(key, out var have))
                {
                    want[key] = (e.Roster, e.Scale, have.Count + e.Count);
                }
                else
                {
                    want[key] = (e.Roster, e.Scale, e.Count);
                    order.Add(key);
                }
            }

            var jobs = new List<RigJob>();
            foreach (var key in order)
            {
                var w = want[key];
                var owned = _pool!.Owned(w.Roster, w.Scale);
                for (var i = owned; i < w.Count; i++)
                {
                    // The first rig of a roster nobody has shown yet is the one the shader warm-up draws.
                    jobs.Add(new RigJob(w.Roster, w.Scale, owned == 0 && i == 0));
                }
            }

            return jobs;
        }

        /// <summary>Traps still to build for <paramref name="level"/>, up to <see cref="HazardPrewarmCap"/> per kind and width.</summary>
        private List<(RunnerHazardDef Def, (RunnerHazardKind, int) Key)> HazardJobs(RunnerLevelDef level)
        {
            var counts = new Dictionary<(RunnerHazardKind, int), (RunnerHazardDef Def, int Count)>();
            foreach (var r in level.Rows)
            {
                if (r.Kind == RunnerRowKind.Hazard && r.Hazard != null)
                {
                    var key = HazardKey(r.Hazard);
                    counts[key] = counts.TryGetValue(key, out var have) ? (have.Def, have.Count + 1) : (r.Hazard, 1);
                }
            }

            var jobs = new List<(RunnerHazardDef, (RunnerHazardKind, int))>();
            foreach (var pair in counts)
            {
                var have = _freeHazards.TryGetValue(pair.Key, out var stack) ? stack.Count : 0;
                for (var i = have; i < Mathf.Min(pair.Value.Count, HazardPrewarmCap); i++)
                {
                    jobs.Add((pair.Value.Def, pair.Key));
                }
            }

            return jobs;
        }

        private void PushHazard(RunnerHazardDef def, (RunnerHazardKind, int) key)
        {
            if (!_freeHazards.TryGetValue(key, out var stack))
            {
                stack = new Stack<HazardView>();
                _freeHazards[key] = stack;
            }

            var v = BuildHazard(def, _spare!);
            v.Root.gameObject.SetActive(false);
            stack.Push(v);
        }

        /// <summary>
        /// Build, a few milliseconds a frame, what <paramref name="level"/> shows at launch (<see cref="RunnerPrewarmSet.Launch"/>:
        /// the bots' peaks, not the margin plan), draw it once off-screen to compile the shaders, hold the minimum
        /// time, then start the run. Reports to <paramref name="p"/> (and <paramref name="screen"/> when this view owns the
        /// loading screen). Each step catches its own failures: one bad rig must not strand the player under the screen.
        /// </summary>
        private IEnumerator PrewarmRoutine(RunnerLevelDef level, RunnerBootProgress p, BattleLoadingScreen? screen, long bootStart)
        {
            var t = new BootTimes();
            var frame = Stopwatch.GetTimestamp();
            RunnerHitchLog.Loading(true);
            RunnerHitchLog.Transition("prewarm L" + level.Number + " begin");
            EnsurePools();
            var next = _catalog!.Next(level);

            // 1. The plan (computed on a worker thread; usually ready already for levels after the first).
            p.Begin(RunnerBootStep.Plan, 1);
            p.Expect(RunnerBootProgress.PlanUnits);
            var planTask = PlanOf(level);
            var planStart = Stopwatch.GetTimestamp();
            while (!planTask.IsCompleted && !TimeUp(bootStart, p))
            {
                Publish(p, screen);
                yield return null;
                t.Frames++;
                frame = Stopwatch.GetTimestamp();
            }

            t.Plan = MsSince(planStart);
            var set = new RunnerPrewarmSet(new List<RunnerPrewarmEntry>(), 0, new List<RunnerPrewarmEntry>(), 0);
            if (planTask.IsCompleted)
            {
                if (planTask.IsFaulted)
                {
                    Debug.LogError("Runner: prewarm plan failed for L" + level.Number + "; nothing is prewarmed (rigs build live).");
                    Debug.LogException(planTask.Exception!);
                }
                else
                {
                    set = planTask.Result;
                }
            }

            p.Did(RunnerBootProgress.PlanUnits);

            // The next two levels' plans, on workers, for the result-screen top-up and Next.
            if (next != null)
            {
                PlanOf(next);
                if (_catalog.Next(next) is { } later)
                {
                    PlanOf(later);
                }
            }

            var rigJobs = RigJobs(set.Launch);
            var hazards = HazardJobs(level);
            var shotsWanted = Mathf.Max(0, set.LaunchShots - (_freeShots.Count + _fadingShots.Count));
            var fxWanted = Mathf.Max(0, FxLaunch - _freeFx.Count);
            p.Expect(rigJobs.Count + (shotsWanted * RunnerBootProgress.ShotUnits) + (fxWanted * RunnerBootProgress.FxUnits)
                + ((hazards.Count + 2) * RunnerBootProgress.TrapUnits) + RunnerBootProgress.ShaderUnits + RunnerBootProgress.StartUnits);

            // 2. Rigs: one per unit; a rig usually fills the whole frame budget by itself.
            p.Begin(RunnerBootStep.Rigs, rigJobs.Count);
            for (var i = 0; i < rigJobs.Count; i++)
            {
                if (TimeUp(bootStart, p))
                {
                    break;
                }

                var job = rigJobs[i];
                var started = Stopwatch.GetTimestamp();
                try
                {
                    _pool!.PrewarmOne(job.Roster, job.Scale, job.Warm);
                }
                catch (Exception ex)
                {
                    Debug.LogError("Runner: prewarm of a " + job.Roster + " rig failed; it will build live if needed.");
                    Debug.LogException(ex);
                }

                var ms = MsSince(started);
                t.Rigs += ms;
                t.RigCount++;
                if (ms > t.SlowestRig)
                {
                    t.SlowestRig = ms;
                    t.SlowestRoster = job.Roster;
                }

                p.Did(1);
                if (!RunnerBootProgress.HasFrameBudget(MsSince(frame)))
                {
                    t.WorstFrame = Math.Max(t.WorstFrame, MsSince(frame));
                    Publish(p, screen);
                    yield return null;
                    t.Frames++;
                    frame = Stopwatch.GetTimestamp();
                }
            }

            // 3. Shots.
            p.Begin(RunnerBootStep.Shots, shotsWanted);
            var phase = Stopwatch.GetTimestamp();
            for (var i = 0; i < shotsWanted && !TimeUp(bootStart, p); i++)
            {
                try
                {
                    var s = NewShot();
                    s.Root.gameObject.SetActive(false);
                    _freeShots.Push(s);
                    t.ShotCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    break;
                }

                p.Did(RunnerBootProgress.ShotUnits);
                if (!RunnerBootProgress.HasFrameBudget(MsSince(frame)))
                {
                    t.Shots += MsSince(phase);
                    t.WorstFrame = Math.Max(t.WorstFrame, MsSince(frame));
                    Publish(p, screen);
                    yield return null;
                    t.Frames++;
                    frame = Stopwatch.GetTimestamp();
                    phase = frame;
                }
            }

            t.Shots += MsSince(phase);

            // 4. Effect sprites.
            p.Begin(RunnerBootStep.Effects, fxWanted);
            phase = Stopwatch.GetTimestamp();
            for (var i = 0; i < fxWanted && !TimeUp(bootStart, p); i++)
            {
                try
                {
                    var f = NewFx();
                    f.Go.SetActive(false);
                    _freeFx.Push(f);
                    t.FxCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    break;
                }

                p.Did(RunnerBootProgress.FxUnits);
                if (!RunnerBootProgress.HasFrameBudget(MsSince(frame)))
                {
                    t.Fx += MsSince(phase);
                    t.WorstFrame = Math.Max(t.WorstFrame, MsSince(frame));
                    Publish(p, screen);
                    yield return null;
                    t.Frames++;
                    frame = Stopwatch.GetTimestamp();
                    phase = frame;
                }
            }

            t.Fx += MsSince(phase);

            // 5. Traps and the boss's telegraph marks.
            p.Begin(RunnerBootStep.Traps, hazards.Count + 2);
            phase = Stopwatch.GetTimestamp();
            for (var i = 0; i < hazards.Count + 2 && !TimeUp(bootStart, p); i++)
            {
                try
                {
                    if (i < hazards.Count)
                    {
                        PushHazard(hazards[i].Def, hazards[i].Key);
                        t.TrapCount++;
                    }
                    else if (i == hazards.Count)
                    {
                        BuildSlamMark();
                    }
                    else
                    {
                        BuildQuakeMark();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                p.Did(RunnerBootProgress.TrapUnits);
                if (!RunnerBootProgress.HasFrameBudget(MsSince(frame)))
                {
                    t.Traps += MsSince(phase);
                    t.WorstFrame = Math.Max(t.WorstFrame, MsSince(frame));
                    Publish(p, screen);
                    yield return null;
                    t.Frames++;
                    frame = Stopwatch.GetTimestamp();
                    phase = frame;
                }
            }

            t.Traps += MsSince(phase);

            // 6. Shader warm-up: one off-screen render of one of everything. Show the step first, then do the frame-long work.
            p.Begin(RunnerBootStep.Shaders, 1);
            t.WorstFrame = Math.Max(t.WorstFrame, MsSince(frame));
            Publish(p, screen);
            yield return null;
            t.Frames++;
            phase = Stopwatch.GetTimestamp();
            using (RunnerHitchLog.Heavy("shader warm-up L" + level.Number))
            {
                try
                {
                    if (!p.TimedOut)
                    {
                        WarmRender();
                    }

                    _pool!.FinishWarm();
                }
                catch (Exception ex)
                {
                    Debug.LogError("Runner: the shader warm-up failed; shaders compile on first use instead.");
                    Debug.LogException(ex);
                }
            }

            t.Shaders = MsSince(phase);
            t.WorstFrame = Math.Max(t.WorstFrame, t.Shaders);
            p.Did(RunnerBootProgress.ShaderUnits);

            // 7. Hold the minimum time so the screen is readable, then start the run.
            p.Begin(RunnerBootStep.Start, 1);
            phase = Stopwatch.GetTimestamp();
            while (!p.TimedOut && RunnerBootProgress.Decide(ElapsedSeconds(bootStart), true) == RunnerBootVerdict.Hold)
            {
                Publish(p, screen);
                yield return null;
                t.Frames++;
            }

            t.Hold = MsSince(phase);
            RunnerHitchLog.Loading(false);
            RunnerHitchLog.Transition("prewarm L" + level.Number + " done");
            LogPrewarm(level, t, MsSince(bootStart), p.TimedOut);
            StartRunGuarded();
            p.Did(RunnerBootProgress.StartUnits);
            p.Finish();
            Publish(p, screen);
        }

        private void LogPrewarm(RunnerLevelDef level, BootTimes t, double wallMs, bool timedOut)
        {
            Debug.Log(RunnerHitchLog.Prefix + " prewarm L" + level.Number + ": " + t.RigCount + " rigs (" + (_pool?.Built ?? 0) + " owned), "
                + (_freeShots.Count + _fadingShots.Count) + " shots, " + _freeFx.Count + " fx, " + t.TrapCount + " traps in " + Mathf.RoundToInt((float)wallMs)
                + " ms over " + t.Frames + " loading frames (worst frame's own work " + Mathf.RoundToInt((float)t.WorstFrame) + " ms)"
                + " | step ms: plan-wait " + Mathf.RoundToInt((float)t.Plan) + ", rigs " + Mathf.RoundToInt((float)t.Rigs)
                + " (slowest " + t.SlowestRoster + " " + Mathf.RoundToInt((float)t.SlowestRig) + "), shots " + Mathf.RoundToInt((float)t.Shots)
                + ", fx " + Mathf.RoundToInt((float)t.Fx) + ", traps " + Mathf.RoundToInt((float)t.Traps) + ", shader warm " + Mathf.RoundToInt((float)t.Shaders)
                + ", min-time hold " + Mathf.RoundToInt((float)t.Hold)
                + " | rig asset cache " + RigAssetCache.Hits + " hits / " + RigAssetCache.Misses + " misses"
                + (timedOut ? " | TIMED OUT" : "") + " (under the loading screen)");
        }

        /// <summary>StartRun, with the old boot's failure rule: log, drop the run and, from Play, go back to the village.</summary>
        private void StartRunGuarded()
        {
            try
            {
                StartRun();
            }
            catch (Exception ex)
            {
                // A broken boot must never strand the player: log it and, from Play, go straight back to the village
                // (the loading screen covers the way back).
                Debug.LogError("Runner: boot failed; " + (RunnerHandoff.FromPlay ? "back to the village." : "nothing to run."));
                Debug.LogException(ex);
                _run = null;
                if (RunnerHandoff.FromPlay && !RunnerHandoff.Done)
                {
                    RunnerHandoff.Finish();
                }
            }
        }

        // ---------- level switch ----------

        /// <summary>
        /// Next / the level list: cover the road with the loading screen, swap the world, build what the new level adds
        /// (almost nothing after a top-up), reveal when its first frame is drawn.
        /// </summary>
        private IEnumerator PlayLevelRoutine(int number)
        {
            _switching = true;
            StopTopUp();
            BattleLoadingScreen? screen = null;
            try
            {
                var target = _catalog!.ByNumber(number);
                try
                {
                    var tips = new List<string>();
                    for (var i = 1; i <= RunnerLoading.TipCount; i++)
                    {
                        var tip = S(RunnerLoading.TipPrefix + i, "");
                        if (tip.Length > 0)
                        {
                            tips.Add(tip);
                        }
                    }

                    if (tips.Count == 0)
                    {
                        tips.Add("Steer through boxes to grow your squad.");
                    }

                    var name = S(target.Title, target.DisplayName.Length > 0 ? target.DisplayName : "Runner");
                    var info = RunnerLoading.Into(_catalog, target, name, S(RunnerLoading.IntoKey, "Onto the road!"),
                        Survival.Domain.Campaign.BattleTransition.Tip(tips, Survival.Domain.Campaign.BattleTransition.StableSeed(target.Id)));
                    screen = BattleLoadingScreen.Show(info, S("battle.loading.vs", "VS"));
                }
                catch (Exception ex)
                {
                    // No screen: switch anyway (the boot below still spreads its work over frames).
                    Debug.LogException(ex);
                }

                if (screen != null)
                {
                    yield return screen.WaitCovered();
                }

                var bootStart = Stopwatch.GetTimestamp();
                var p = RunnerBoot.Begin();
                p.Begin(RunnerBootStep.Plan, 1);
                var swapped = false;
                using (RunnerHitchLog.Heavy("loading-screen swap to L" + number))
                {
                    swapped = SwapLevel(number);
                }

                if (swapped)
                {
                    yield return PrewarmRoutine(_level!, p, screen, bootStart);
                }
                else
                {
                    p.Finish();
                }

                if (screen != null)
                {
                    screen.RevealOnFirstFrameOf(gameObject.scene);
                }
            }
            finally
            {
                _switching = false;
                RunnerHitchLog.Loading(false);
            }
        }

        /// <summary>Tear the old road down and build the new one (a synchronous slice, logged as a heavy section).</summary>
        private bool SwapLevel(int number)
        {
            try
            {
                _run = null;
                ClearActors();
                HideResult();
                HideLevelList();
                if (_hud != null)
                {
                    Destroy(_hud.gameObject);
                    _hud = null;
                }

                if (_world != null)
                {
                    Destroy(_world.gameObject);
                }

                _level = _catalog!.ByNumber(number);
                _world = new GameObject("RunnerWorld").transform;
                _world.SetParent(transform, false);
                _actors = new GameObject("RunnerActors").transform;
                _actors.SetParent(_world, false);
                BuildGround(_level);
                BuildHud();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("Runner: switching to level " + number + " failed.");
                Debug.LogException(ex);
                _run = null;
                if (RunnerHandoff.FromPlay && !RunnerHandoff.Done)
                {
                    RunnerHandoff.Finish();
                }

                return false;
            }
        }

        // ---------- result-screen top-up ----------

        private void StopTopUp()
        {
            if (_topUp != null)
            {
                StopCoroutine(_topUp);
                _topUp = null;
            }
        }

        /// <summary>
        /// While the result screen is up (nothing is running, so a frame-long rig build costs nothing): finish this level's
        /// margin plan and build what the next level adds (its launch plan), so the Next press builds almost nothing.
        /// A new run (Retry) or a level switch stops it between units.
        /// </summary>
        private IEnumerator TopUpRoutine(RunnerLevelDef level)
        {
            // Let the result screen draw first.
            yield return null;
            yield return null;
            if (_catalog == null || _pool == null)
            {
                yield break;
            }

            var next = _catalog.Next(level);
            var mine = PlanOf(level);
            var theirs = next != null ? PlanOf(next) : null;
            var waited = 0f;
            while ((!mine.IsCompleted || (theirs != null && !theirs.IsCompleted)) && waited < 5f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!mine.IsCompleted || mine.IsFaulted)
            {
                yield break;
            }

            var plan = mine.Result.Full;
            var shots = mine.Result.FullShots;
            if (theirs != null && theirs.IsCompleted && !theirs.IsFaulted)
            {
                plan = RunnerPrewarm.Union(plan, theirs.Result.Launch);
                shots = Mathf.Max(shots, theirs.Result.LaunchShots);
            }

            var started = Stopwatch.GetTimestamp();
            var frame = started;
            var frames = 0;
            var rigs = 0;
            var shotsBuilt = 0;
            var fxBuilt = 0;
            var trapsBuilt = 0;
            var jobs = RigJobs(plan);
            RunnerHitchLog.Transition("top-up begin");
            foreach (var job in jobs)
            {
                try
                {
                    _pool.PrewarmOne(job.Roster, job.Scale, false);
                    rigs++;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                if (!RunnerBootProgress.HasFrameBudget(MsSince(frame)))
                {
                    yield return null;
                    frames++;
                    frame = Stopwatch.GetTimestamp();
                }
            }

            while (_freeShots.Count + _fadingShots.Count < shots)
            {
                var s = NewShot();
                s.Root.gameObject.SetActive(false);
                _freeShots.Push(s);
                shotsBuilt++;
                if (!RunnerBootProgress.HasFrameBudget(MsSince(frame)))
                {
                    yield return null;
                    frames++;
                    frame = Stopwatch.GetTimestamp();
                }
            }

            while (_freeFx.Count < FxFull)
            {
                var f = NewFx();
                f.Go.SetActive(false);
                _freeFx.Push(f);
                fxBuilt++;
                if (!RunnerBootProgress.HasFrameBudget(MsSince(frame)))
                {
                    yield return null;
                    frames++;
                    frame = Stopwatch.GetTimestamp();
                }
            }

            var hazards = HazardJobs(level);
            if (next != null)
            {
                hazards.AddRange(HazardJobs(next));
            }

            foreach (var h in hazards)
            {
                // The next level may repeat a kind this level already filled: re-check before each build.
                if (_freeHazards.TryGetValue(h.Key, out var have) && have.Count >= HazardPrewarmCap)
                {
                    continue;
                }

                PushHazard(h.Def, h.Key);
                trapsBuilt++;
                if (!RunnerBootProgress.HasFrameBudget(MsSince(frame)))
                {
                    yield return null;
                    frames++;
                    frame = Stopwatch.GetTimestamp();
                }
            }

            _pool.FinishWarm();
            _topUp = null;
            RunnerHitchLog.Transition("top-up done");
            Debug.Log(RunnerHitchLog.Prefix + " top-up after L" + level.Number + ": +" + rigs + " rigs, +" + shotsBuilt + " shots, +" + fxBuilt + " fx, +" + trapsBuilt
                + " traps in " + Mathf.RoundToInt((float)MsSince(started)) + " ms over " + frames + " frames (result screen; next level: " + (next != null ? "L" + next.Number : "none") + ")");
        }

        // ---------- hitch-log scene description ----------

        /// <summary>One line of scene state for the hitch log: the boss, in the words the telegraphs use.</summary>
        private string DescribeBoss()
        {
            var run = _run;
            if (run == null)
            {
                return "none (no run: booting or switching levels)";
            }

            var runState = " | run " + run.Outcome + " " + run.Seconds.ToString("0.0") + " s"
                + (run.Over ? ", over (end delay " + _endDelay.ToString("0.00") + ", result " + (_resultShown ? "shown" : "not yet") + ")" : ", running");

            RunnerFoe? boss = null;
            foreach (var f in run.Foes)
            {
                if (f.Boss)
                {
                    boss = f;
                    break;
                }
            }

            if (boss == null)
            {
                return (run.BossFight ? "fight over, boss gone" : "none on the road") + runState;
            }

            string state;
            if (boss.Windup > 0f && boss.Move == RunnerBossMove.Quake)
            {
                state = "quake telegraph " + boss.Windup.ToString("0.00") + "/" + boss.WindupTotal.ToString("0.00") + " s";
            }
            else if (boss.Windup > 0f && boss.Move == RunnerBossMove.Slam)
            {
                state = "slam telegraph " + boss.Windup.ToString("0.00") + "/" + boss.WindupTotal.ToString("0.00") + " s";
            }
            else if (boss.Actions > 0 && boss.SinceAttack < 1.5f)
            {
                state = "recovering " + boss.SinceAttack.ToString("0.0") + " s since the attack";
            }
            else
            {
                state = "idle";
            }

            return state + " (hp " + boss.Hp + "/" + boss.MaxHp + ", actions " + boss.Actions + ", quakes " + boss.Quakes + ", engaged " + (boss.Engaged ? "yes" : "no") + ")" + runState;
        }
    }
}
