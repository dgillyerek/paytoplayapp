#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Survival.Domain.Campaign;
using Survival.Domain.Clock;
using Survival.Domain.Economy;
using Survival.Domain.Flavor;
using Survival.Domain.Roster;
using Survival.Domain.Runner;
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Survival.Unity
{
    /// <summary>
    /// Runner: Walk &amp; Fight (RunnerLevel.unity). A "runner army" slice on the locked battle rigs: the squad walks down
    /// a painted lane on its own, the camera rides behind and above, and the player only steers left / right (drag,
    /// A / D or the arrows). Everyone auto-attacks what is in front of them; bonus boxes must be shot down to be claimed.
    /// Rules live in <see cref="RunnerRun"/> (plain C#, tested); this view builds the world, drives the run every frame
    /// and turns its events into rigs, shots, hit numbers, bursts and shake. From Play (Runner button) the rewards go
    /// back through <see cref="RunnerHandoff"/> to the village celebration; opened on its own (menu entry) a win is paid
    /// straight into survival_save.json when one exists.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed partial class RunnerLevelView : MonoBehaviour
    {
        public const string SceneName = "RunnerLevel";
        public const string ScenePath = "Assets/Survival/Runner/RunnerLevel.unity";

        // Behind / above, a quarter turn off the centre line: the squad reads as 3D bodies in 3/4 from behind, the
        // foes coming down the road in 3/4 from the front (about 26 degrees down).
        // Framed on the middle of the squad (not the leader) so a 10-strong formation's back row stays in shot.
        private const float CameraHeight = 7.6f;
        private const float CameraBack = 10.0f;
        private const float CameraSide = -1.2f;
        private const float CameraLookAhead = 5.0f;
        private const float CameraLookHeight = 0.9f;

        /// <summary>The unit a follower / hero box pays stands on its lid at this share of battle size.</summary>
        private const float CaptiveScale = 0.72f;
        private const float KeySteerSpeed = 7.5f;

        /// <summary>How far ahead of the hero a held key can push the steer point (m).</summary>
        private const float KeyLeadMetres = 1.2f;
        private const float DragMetresPerScreen = 9.5f;
        private const float ShotHeight = 1.15f;
        private const float BossScale = 1.5f;

        [SerializeField] private string levelId = "";

        private ThemePackBinder? _pack;
        private RunnerCatalog? _catalog;
        private RunnerLevelDef? _level;
        private RunnerRun? _run;
        private Camera? _cam;
        private Transform? _world;
        private Transform? _actors;
        private readonly Dictionary<int, RunnerRig> _members = new Dictionary<int, RunnerRig>();
        private readonly Dictionary<int, RunnerRig> _foes = new Dictionary<int, RunnerRig>();
        private readonly Dictionary<int, BoxView> _boxes = new Dictionary<int, BoxView>();
        private readonly Dictionary<int, RunnerBoxDef> _boxDefs = new Dictionary<int, RunnerBoxDef>();
        private readonly Dictionary<int, ShotView> _shots = new Dictionary<int, ShotView>();
        private readonly List<RunnerRig> _dying = new List<RunnerRig>();
        private readonly List<Fx> _fx = new List<Fx>();
        private readonly HashSet<int> _killed = new HashSet<int>();
        private float _shake;
        private float _dragAccum;
        private bool _keyHeld;
        private bool _steered;
        private bool _booted;
        private bool _resultShown;

        /// <summary>Highest runner level open (from Play's save via the handoff, or the save file when opened on its own).</summary>
        private int _unlocked = 1;
        private float _endDelay = -1f;
        private int _attempt;

        public RunnerRun? Run => _run;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => RunnerHandoff.Reset();

        private void Start() => Boot();

        private void Boot()
        {
            if (_booted || !Application.isPlaying)
            {
                return;
            }

            _booted = true;
            SurvivalVisuals.EnsureEventSystem();
            _pack = LoadPack();
            if (_pack != null)
            {
                SurvivalArt.EnsureLoaded(AppFlavorConfig.FantasyKingdomA, _pack);
            }

            var path = Path.Combine(SurvivalBootstrap.ResolveCatalogDir(), RunnerCatalog.FileName);
            try
            {
                _catalog = RunnerCatalog.Parse(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Debug.LogError("Runner: could not read " + path + ". " + ex.Message);
                return;
            }

            _unlocked = RunnerHandoff.FromPlay ? RunnerHandoff.Unlocked : StandaloneUnlocked();
            var wanted = RunnerHandoff.PendingLevel ?? (levelId.Length > 0 ? levelId : null);
            _level = _catalog.Level(wanted) ?? _catalog.ByNumber(_unlocked);
            try
            {
                // Camera first, so the scene always has a working camera for the loading screen to reveal.
                BuildCamera();
                _world = new GameObject("RunnerWorld").transform;
                _world.SetParent(transform, false);
                BuildLights();
                BuildGround(_level);
                BuildHud();
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

        private static ThemePackBinder? LoadPack()
        {
            try
            {
                var root = SurvivalArt.ResolvePackRoot(AppFlavorConfig.FantasyKingdomA);
                return ThemePackBinder.Parse(
                    File.ReadAllText(Path.Combine(root, "pack.json")),
                    File.ReadAllText(Path.Combine(root, "strings", "en.json")),
                    File.ReadAllText(Path.Combine(root, "map", "node_pins.json")),
                    File.ReadAllText(Path.Combine(root, "ui", "hud_layout.json")));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Runner: Theme A pack unreadable, code-drawn UI only. " + ex.Message);
                return null;
            }
        }

        private string S(string key, string fallback) => _pack != null ? _pack.StringOr(key, fallback) : fallback;

        private string Fmt(string key, string fallback, params object[] args) => string.Format(S(key, fallback), args);

        // ---------- run lifecycle ----------

        private void StartRun()
        {
            if (_catalog == null || _level == null)
            {
                return;
            }

            ClearActors();
            _attempt++;
            _run = new RunnerRun(_catalog, _level);
            _resultShown = false;
            _endDelay = -1f;
            _killed.Clear();
            HideResult();
            Sync();
            SnapCamera();
            ShowHint(true);
        }

        public void Retry() => StartRun();

        /// <summary>
        /// Switch to runner level <paramref name="number"/> in place (result screen Next / level list): a new road, HUD and
        /// run. Only open levels; banked runs from Play still pay once on the way back.
        /// </summary>
        public void PlayLevel(int number)
        {
            if (_catalog == null || !RunnerProgress.IsOpen(_unlocked, number))
            {
                return;
            }

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

            _level = _catalog.ByNumber(number);
            _world = new GameObject("RunnerWorld").transform;
            _world.SetParent(transform, false);
            _actors = null;
            BuildGround(_level);
            BuildHud();
            StartRun();
        }

        /// <summary>The next level after this one, when it is open.</summary>
        private RunnerLevelDef? OpenNext => _catalog != null && _level != null && _catalog.Next(_level) is { } next && RunnerProgress.IsOpen(_unlocked, next.Number) ? next : null;

        /// <summary>Back to Play (from the village) or a fresh run (opened on its own).</summary>
        public void Leave()
        {
            if (RunnerHandoff.FromPlay)
            {
                RunnerHandoff.Finish();
                return;
            }

            StartRun();
        }

        private void ClearActors()
        {
            if (_actors != null)
            {
                Destroy(_actors.gameObject);
            }

            _actors = new GameObject("RunnerActors").transform;
            _actors.SetParent(_world, false);
            _members.Clear();
            _foes.Clear();
            _boxes.Clear();
            _boxDefs.Clear();
            _shots.Clear();
            _dying.Clear();
            foreach (var f in _fx)
            {
                if (f.Go != null)
                {
                    Destroy(f.Go);
                }
            }

            _fx.Clear();
            ClearFloaters();
        }

        private void Update()
        {
            if (_run == null)
            {
                return;
            }

            var dt = Mathf.Min(Time.deltaTime, 0.1f);
            if (!_run.Over)
            {
                ReadSteer(dt);
                var events = _run.Step(dt);
                Sync();
                foreach (var e in events)
                {
                    OnEvent(e);
                }
            }
            else if (!_resultShown)
            {
                if (_endDelay < 0f)
                {
                    _endDelay = 1.1f;
                }

                _endDelay -= dt;
                if (_endDelay <= 0f)
                {
                    Finish();
                }
            }

            AnimateSquad();
            AnimateFoes();
            TickFx(dt);
            TickHud(dt);
        }

        private void LateUpdate()
        {
            var dt = Time.deltaTime;
            foreach (var r in _members.Values)
            {
                r.Late(dt);
            }

            foreach (var r in _foes.Values)
            {
                r.Late(dt);
            }

            foreach (var b in _boxes.Values)
            {
                b.Captive?.Late(dt);
            }

            for (var i = _dying.Count - 1; i >= 0; i--)
            {
                var r = _dying[i];
                r.Late(dt);
                if (r.Gone)
                {
                    r.Destroy();
                    _dying.RemoveAt(i);
                }
            }

            FollowCamera(dt);
            Billboards();
            PlaceWorldUi();
        }

        private void Finish()
        {
            if (_run == null || _level == null)
            {
                return;
            }

            _resultShown = true;
            var result = _run.Result;
            var bundle = RunnerRewards.For(_level, result);
            _unlocked = RunnerProgress.AfterRun(_unlocked, result, _catalog?.Levels.Count ?? 1);
            var saved = false;
            if (RunnerHandoff.FromPlay)
            {
                RunnerHandoff.Bank(result, bundle);
            }
            else
            {
                saved = PayStandalone(bundle);
            }

            ShowResult(result, bundle, saved);
        }

        /// <summary>
        /// Opened on its own (no Play behind it): pay into survival_save.json the way Play would, but only when the
        /// save exists, so a fresh install's starting wallet is never overwritten by a runner-only save.
        /// </summary>
        private static int StandaloneUnlocked()
        {
            try
            {
                var savePath = SurvivalCampaignView.SavePath;
                var catalogPath = Path.Combine(SurvivalBootstrap.ResolveCatalogDir(), CampaignCatalog.FileName);
                if (!File.Exists(savePath) || !File.Exists(catalogPath))
                {
                    return 1;
                }

                var campaign = new CampaignSession(CampaignCatalog.Parse(File.ReadAllText(catalogPath)), new Wallet(), new WallClock());
                return campaign.Load(File.ReadAllText(savePath)) ? campaign.RunnerUnlocked : 1;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Runner: save unreadable, Level 1 only. " + ex.Message);
                return 1;
            }
        }

        private static bool PayStandalone(RewardBundle bundle)
        {
            try
            {
                var savePath = SurvivalCampaignView.SavePath;
                var catalogPath = Path.Combine(SurvivalBootstrap.ResolveCatalogDir(), CampaignCatalog.FileName);
                if (!File.Exists(savePath) || !File.Exists(catalogPath))
                {
                    return false;
                }

                var campaign = new CampaignSession(CampaignCatalog.Parse(File.ReadAllText(catalogPath)), new Wallet(), new WallClock());
                if (!campaign.Load(File.ReadAllText(savePath)))
                {
                    return false;
                }

                RunnerRewards.Grant(bundle, campaign);
                File.WriteAllText(savePath, campaign.Save());
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Runner: rewards not saved. " + ex.Message);
                return false;
            }
        }

        // ---------- steering ----------

        private void ReadSteer(float dt)
        {
            if (_run == null)
            {
                return;
            }

            var dir = 0f;
            var kb = Keyboard.current;
            if (kb != null && kb.enabled)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir += 1f;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (dir == 0f)
            {
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) dir -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dir += 1f;
            }
#endif
            if (dir != 0f)
            {
                // Keys slide the steer point at a steady pace, kept a short lead ahead of the hero; the steering spring
                // eases the hero after it.
                var lead = KeyLeadMetres;
                var next = Mathf.Clamp(_run.SteerTarget + (Mathf.Clamp(dir, -1f, 1f) * KeySteerSpeed * dt), _run.Hero.X - lead, _run.Hero.X + lead);
                _run.SteerTo(next);
                _keyHeld = true;
                Steered();
            }
            else if (_keyHeld)
            {
                // Let go: settle just past where the hero is heading, so it eases to a stop instead of snapping.
                _keyHeld = false;
                _run.SteerTo(_run.Hero.X + (_run.Hero.VX * RunnerRules.SteerSmoothSeconds * 0.5f));
            }

            if (Mathf.Abs(_dragAccum) > 1e-4f)
            {
                _run.SteerBy(_dragAccum);
                _dragAccum = 0f;
                Steered();
            }
        }

        /// <summary>Drag pad: horizontal screen drag (mouse or touch) moves the steer point.</summary>
        internal void Drag(float screenDx)
        {
            if (_run == null || _run.Over)
            {
                return;
            }

            _dragAccum += screenDx / Mathf.Max(1f, Screen.width) * DragMetresPerScreen;
        }

        private void Steered()
        {
            if (!_steered)
            {
                _steered = true;
                ShowHint(false);
            }
        }

        // ---------- sync the scene to the run ----------

        private void Sync()
        {
            if (_run == null || _actors == null)
            {
                return;
            }

            foreach (var m in _run.Squad)
            {
                if (!_members.ContainsKey(m.Id))
                {
                    var rig = RunnerRig.Spawn(m.Unit.Roster, _actors, (m.IsHero ? "Hero_" : "Follower_") + m.Unit.Id + "_" + m.Id);
                    if (rig != null)
                    {
                        rig.Slot.position = new Vector3(m.X, 0f, m.Z);
                        rig.Slot.rotation = Quaternion.identity;
                        rig.Walk();
                        _members[m.Id] = rig;
                        if (m.IsHero)
                        {
                            AddSideBar(m.Id, true, rig, false);
                        }
                    }
                }
            }

            RemoveMissing(_members, id => _run.Member(id) != null);
            foreach (var f in _run.Foes)
            {
                if (!_foes.ContainsKey(f.Id))
                {
                    var rig = RunnerRig.Spawn(f.Unit.Roster, _actors, "Foe_" + f.Unit.Id + "_" + f.Id, f.Boss ? BossScale : 1f);
                    if (rig != null)
                    {
                        rig.Slot.position = new Vector3(f.X, 0f, f.Z);
                        rig.Slot.rotation = Quaternion.Euler(0f, 180f, 0f);
                        rig.Rest();
                        _foes[f.Id] = rig;
                        AddSideBar(f.Id, false, rig, f.Boss);
                    }
                }
            }

            RemoveMissing(_foes, id => _run.Foe(id) != null);
            foreach (var b in _run.Boxes)
            {
                if (!_boxes.ContainsKey(b.Id))
                {
                    var view = BuildBox(b);
                    _boxes[b.Id] = view;
                    AddBar(b.Id, true, view.Top, false);
                }
            }

            foreach (var id in new List<int>(_boxes.Keys))
            {
                if (_run.Box(id) == null)
                {
                    var v = _boxes[id];
                    _boxes.Remove(id);
                    RemoveBar(id);
                    v.Captive?.Destroy();
                    if (v.Root != null)
                    {
                        Destroy(v.Root.gameObject);
                    }
                }
            }

            foreach (var s in _run.Shots)
            {
                if (!_shots.ContainsKey(s.Id))
                {
                    _shots[s.Id] = BuildShot(s);
                }

                var view = _shots[s.Id];
                var to = new Vector3(s.X, ShotHeight, s.Z);
                var dir = to - view.Root.position;
                view.Root.position = to;
                if (dir.sqrMagnitude > 1e-6f)
                {
                    view.Root.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                }
            }

            foreach (var id in new List<int>(_shots.Keys))
            {
                var live = false;
                foreach (var s in _run.Shots)
                {
                    if (s.Id == id)
                    {
                        live = true;
                        break;
                    }
                }

                if (!live)
                {
                    var v = _shots[id];
                    _shots.Remove(id);
                    if (v.Root != null)
                    {
                        // Let the trail finish rather than popping.
                        v.Root.gameObject.AddComponent<RunnerAutoDestroy>().Seconds = 0.25f;
                        if (v.Head != null)
                        {
                            v.Head.enabled = false;
                        }
                    }
                }
            }

            // Positions: the run owns them; rigs follow exactly.
            foreach (var m in _run.Squad)
            {
                if (_members.TryGetValue(m.Id, out var rig))
                {
                    rig.Slot.position = new Vector3(m.X, 0f, m.Z);
                }
            }

            foreach (var f in _run.Foes)
            {
                if (_foes.TryGetValue(f.Id, out var rig))
                {
                    rig.Slot.position = new Vector3(f.X, 0f, f.Z);
                }
            }
        }

        private void RemoveMissing(Dictionary<int, RunnerRig> rigs, Func<int, bool> alive)
        {
            foreach (var id in new List<int>(rigs.Keys))
            {
                if (alive(id))
                {
                    continue;
                }

                var rig = rigs[id];
                rigs.Remove(id);
                RemoveBar(id);
                if (_killed.Remove(id))
                {
                    rig.Die(rig.Slot.forward * -1f);
                    _dying.Add(rig);
                }
                else
                {
                    // Left behind (passed): fade out by sinking too, without the death flash.
                    rig.Die(Vector3.zero);
                    _dying.Add(rig);
                }
            }
        }

        private void AnimateSquad()
        {
            if (_run == null)
            {
                return;
            }

            foreach (var m in _run.Squad)
            {
                if (!_members.TryGetValue(m.Id, out var rig))
                {
                    continue;
                }

                rig.LeanWith(_run.Over ? 0f : m.VX);
                if (_run.Over && _run.Outcome == RunnerOutcome.Won)
                {
                    rig.Rest();
                    rig.Face(Vector3.forward);
                    continue;
                }

                if (m.Attacking)
                {
                    // Shots fly straight down the road, so shooters face straight down it (plus the rig's own aim
                    // offset, e.g. Rowan's bow); a melee swing turns to what it hit.
                    var target = !m.Unit.Ranged && m.TargetId != 0 ? TargetPosition(m.TargetId) : null;
                    rig.Face(target != null ? target.Value - rig.Slot.position : Vector3.forward, rig.Data.AimYawDegrees);
                    continue;
                }

                // The squad never stops for the waves (it walks through what it has not killed); it only stands at the
                // boss's arena line.
                rig.Face(Vector3.forward);
                if (_run.Halted)
                {
                    rig.Rest();
                }
                else
                {
                    rig.Walk();
                }
            }
        }

        private void AnimateFoes()
        {
            if (_run == null)
            {
                return;
            }

            foreach (var f in _run.Foes)
            {
                if (!_foes.TryGetValue(f.Id, out var rig))
                {
                    continue;
                }

                // Faces whoever it last went for (it turns round on a squad that walked past it), else the hero.
                var at = (f.TargetId != 0 ? _run.Member(f.TargetId) : null) ?? _run.Hero;
                rig.Face(new Vector3(at.X - f.X, 0f, at.Z - f.Z));
                if (f.Attacking)
                {
                    continue;
                }

                if (f.Walking)
                {
                    rig.Walk();
                }
                else
                {
                    rig.Rest();
                }
            }
        }

        private Vector3? TargetPosition(int id)
        {
            if (_run == null)
            {
                return null;
            }

            var f = _run.Foe(id);
            if (f != null)
            {
                return new Vector3(f.X, 0f, f.Z);
            }

            var b = _run.Box(id);
            return b != null ? new Vector3(b.X, 0f, b.Z) : (Vector3?)null;
        }

        // ---------- events → juice ----------

        private void OnEvent(RunnerEvent e)
        {
            var at = new Vector3(e.X, 0f, e.Z);
            switch (e.Kind)
            {
                case RunnerEventKind.AttackStarted:
                {
                    // One take per shot: the sim starts it and fires at its release frame, so the bolt leaves on the draw.
                    if (_members.TryGetValue(e.Id, out var m))
                    {
                        m.StartTake(e.Amount);
                    }

                    break;
                }

                case RunnerEventKind.Fired:
                {
                    if (_members.TryGetValue(e.Id, out var m))
                    {
                        if (!m.HasAttackSpeed)
                        {
                            m.Attack();
                        }

                        if (e.Amount <= 0f && e.Other != 0)
                        {
                            Slash(at + (Vector3.up * 0.9f), m.Data.Name);
                        }
                    }
                    else if (_foes.TryGetValue(e.Id, out var f))
                    {
                        f.Attack();
                    }

                    break;
                }

                case RunnerEventKind.Hit:
                {
                    var attacker = _run?.Member(e.Other);
                    var from = attacker != null ? new Vector3(attacker.X, 0f, attacker.Z) : at + Vector3.back;
                    if (e.OnBox)
                    {
                        if (_boxes.TryGetValue(e.Id, out var box))
                        {
                            box.Punch = 1f;
                            box.Captive?.Hit(null, Vector3.forward, 0.35f);
                        }

                        HitNumber(at + (Vector3.up * BoxView.Size), (int)e.Amount, e.Big, true);
                    }
                    else if (_foes.TryGetValue(e.Id, out var rig))
                    {
                        rig.Hit(attacker != null ? RunnerRig.Find(attacker.Unit.Roster) : null, at - from, e.Big ? 1f : 0.6f);
                        HitNumber(at + (Vector3.up * rig.Height), (int)e.Amount, e.Big, false);
                    }

                    if (e.Big)
                    {
                        Shake(0.18f);
                    }

                    break;
                }

                case RunnerEventKind.FoeKilled:
                    _killed.Add(e.Id);
                    Burst(at + (Vector3.up * 0.8f), new Color(1f, 0.55f, 0.25f, 1f), e.Big ? 2.4f : 1.1f, false);
                    if (e.Big)
                    {
                        Shake(0.55f);
                    }

                    break;

                case RunnerEventKind.BoxBroken:
                    BoxBurst(e);
                    Shake(0.3f);
                    break;

                case RunnerEventKind.BoxMissed:
                    Floater(at + (Vector3.up * BoxView.Size), "✗", new Color(0.8f, 0.8f, 0.8f, 1f), 34);
                    break;

                case RunnerEventKind.FollowerJoined:
                    break;

                case RunnerEventKind.FollowerLost:
                {
                    _killed.Add(e.Id);
                    Floater(at + (Vector3.up * 2f), S("runner.follower_lost", "-1"), new Color(1f, 0.35f, 0.3f, 1f), 44);
                    Shake(0.22f);
                    break;
                }

                case RunnerEventKind.HeroHit:
                {
                    if (_run != null && _members.TryGetValue(_run.Hero.Id, out var hero))
                    {
                        var foe = _run.Foe(e.Other);
                        hero.Hit(foe != null ? RunnerRig.Find(foe.Unit.Roster) : null, foe != null ? at - new Vector3(foe.X, 0f, foe.Z) : Vector3.back, 1f);
                        HitNumber(at + (Vector3.up * hero.Height), (int)e.Amount, true, false, true);
                    }

                    Shake(0.4f);
                    break;
                }

                case RunnerEventKind.Upgrade:
                    break;

                case RunnerEventKind.BossEngaged:
                    Banner(S("runner.boss_incoming", "Ironhowl blocks the road!"), new Color(1f, 0.45f, 0.35f, 1f));
                    Shake(0.35f);
                    break;

                case RunnerEventKind.Won:
                    Burst(at + (Vector3.up * 1.2f), new Color(1f, 0.85f, 0.35f, 1f), 3.2f, true);
                    break;

                case RunnerEventKind.Lost:
                    if (_run != null && _members.TryGetValue(_run.Hero.Id, out var dead))
                    {
                        _members.Remove(_run.Hero.Id);
                        dead.Die(Vector3.back);
                        _dying.Add(dead);
                    }

                    Shake(0.6f);
                    break;
            }
        }

        private void Shake(float amount) => _shake = Mathf.Max(_shake, amount);

        // ---------- camera ----------

        private Vector3 _camFocus;

        private void BuildCamera()
        {
            // The scene's own Main Camera (so the first frame already has one); else make one.
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                var cam = root.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    _cam = cam;
                    break;
                }
            }

            if (_cam == null)
            {
                var go = new GameObject("RunnerCamera");
                go.transform.SetParent(transform, false);
                _cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }

            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = LockedBattleGround.Horizon;
            _cam.fieldOfView = 52f;
            _cam.nearClipPlane = 0.2f;
            _cam.farClipPlane = 120f;
            _cam.enabled = true;
        }

        private void SnapCamera()
        {
            _camFocus = Focus();
            PlaceCamera(Vector3.zero);
        }

        private Vector3 Focus()
        {
            if (_run == null)
            {
                return Vector3.zero;
            }

            // Ride a little toward the steer side, never the full lane, so the squad stays centred; halfway between
            // the leader and the back row.
            var back = _run.Distance;
            foreach (var m in _run.Squad)
            {
                back = Mathf.Min(back, m.Z);
            }

            return new Vector3(_run.Hero.X * 0.45f, 0f, (_run.Distance + back) * 0.5f);
        }

        private void FollowCamera(float dt)
        {
            if (_cam == null || _run == null)
            {
                return;
            }

            _camFocus = Vector3.Lerp(_camFocus, Focus(), 1f - Mathf.Exp(-dt * 6f));
            _shake = Mathf.MoveTowards(_shake, 0f, dt * 1.6f);
            var jitter = _shake > 1e-3f
                ? new Vector3(Mathf.PerlinNoise(Time.time * 31f, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, Time.time * 29f) - 0.5f, 0f) * (_shake * 0.9f)
                : Vector3.zero;
            PlaceCamera(jitter);
        }

        private void PlaceCamera(Vector3 jitter)
        {
            if (_cam == null)
            {
                return;
            }

            var boss = _run != null && _run.BossFight && !_run.Over;
            var back = CameraBack + (boss ? 1.4f : 0f);
            var pos = _camFocus + new Vector3(CameraSide, CameraHeight + (boss ? 0.8f : 0f), -back);
            var look = _camFocus + new Vector3(0f, CameraLookHeight + (boss ? 0.6f : 0f), CameraLookAhead);
            _cam.transform.position = pos;
            _cam.transform.rotation = Quaternion.LookRotation(look - pos, Vector3.up);
            _cam.transform.position += _cam.transform.rotation * jitter;
        }

        // ---------- world ----------

        private void BuildLights()
        {
            var key = new GameObject("RunnerKeyLight");
            key.transform.SetParent(transform, false);
            key.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
            var light = key.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.95f, 0.86f, 1f);
            light.shadows = LightShadows.Soft;
            var fill = new GameObject("RunnerFillLight");
            fill.transform.SetParent(transform, false);
            fill.transform.rotation = Quaternion.Euler(28f, 150f, 0f);
            var f = fill.AddComponent<Light>();
            f.type = LightType.Directional;
            f.intensity = 0.35f;
            f.color = new Color(0.75f, 0.82f, 1f, 1f);
            f.shadows = LightShadows.None;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.78f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.46f, 0.38f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.2f, 0.14f, 1f);
        }

        /// <summary>Map grass either side of a worn dirt road (the battle's painted ground tiles), with painted decor billboards.</summary>
        private void BuildGround(RunnerLevelDef level)
        {
            var roadEnd = level.Length;
            var length = roadEnd + 60f;
            var mid = (roadEnd * 0.5f) + 10f;
            var grassTex = LockedBattleGround.Load(LockedBattleGroundSpec.GrassTileRel);
            var grass = LockedBattleGround.Quad(_world!, "RunnerGrass", 70f, length, 0f, 0f, mid);
            var grassMat = LockedBattleGround.Lit(grassTex, new Color(0.39f, 0.41f, 0.20f, 1f), false);
            LockedBattleGround.Tile(grassMat, 70f / LockedBattleGroundSpec.GrassTileMetres, length / LockedBattleGroundSpec.GrassTileMetres);
            grass.sharedMaterial = grassMat;
            var dirtTex = LockedBattleGround.Load(LockedBattleGroundSpec.DirtTrackRel);
            var road = LockedBattleGround.Quad(_world!, "RunnerRoad", (level.HalfWidth * 2f) + 1.4f, length, 0f, 0.004f, mid);
            var dirtMat = LockedBattleGround.Lit(dirtTex, new Color(0.58f, 0.52f, 0.38f, 0.9f), true);
            // One worn track across the whole road (the tile is a single track with soft alpha edges; two side by side read as twin paths).
            LockedBattleGround.Tile(dirtMat, 1f, length / 8f);
            road.sharedMaterial = dirtMat;
            road.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Finish line: a gold strip across the road at the end.
            var finish = LockedBattleGround.Quad(_world!, "RunnerFinish", (level.HalfWidth * 2f) + 1.4f, 0.6f, 0f, 0.008f, level.Length);
            finish.sharedMaterial = LockedBattleGround.Lit(null, new Color(0.95f, 0.78f, 0.3f, 1f), false);

            var props = new[] { "theme_a.village.prop.tree", "theme_a.village.prop.bushes", "theme_a.village.prop.rocks", "theme_a.village.prop.tree" };
            var rnd = new System.Random(level.Id.GetHashCode() & 0x7fffffff);
            for (var z = -6f; z < roadEnd + 40f; z += 2.6f + ((float)rnd.NextDouble() * 2.2f))
            {
                for (var side = -1; side <= 1; side += 2)
                {
                    var key = props[rnd.Next(props.Length)];
                    var x = side * (level.HalfWidth + 1.6f + ((float)rnd.NextDouble() * 7f));
                    var size = key.EndsWith("tree", StringComparison.Ordinal) ? 2.6f + ((float)rnd.NextDouble() * 1.4f) : 1.2f + ((float)rnd.NextDouble() * 0.8f);
                    Billboard(_world!, "Decor", key, new Vector3(x, 0f, z), size, new Color(0.25f, 0.45f, 0.2f, 1f));
                }
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = LockedBattleGround.Horizon;
            RenderSettings.fogStartDistance = 26f;
            RenderSettings.fogEndDistance = 60f;
        }

        private readonly List<Transform> _billboards = new List<Transform>();

        /// <summary>A painted sprite standing on the ground and turned to the camera (code-drawn disc when the art is missing).</summary>
        private Transform Billboard(Transform parent, string name, string key, Vector3 foot, float width, Color fallback)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = foot;
            var sr = new GameObject("Sprite").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(go.transform, false);
            var sprite = SurvivalArt.Get(key) ?? SurvivalArt.Circle();
            sr.sprite = sprite;
            if (SurvivalArt.Get(key) == null)
            {
                sr.color = fallback;
            }

            var w = Mathf.Max(0.01f, sprite.bounds.size.x);
            var s = width / w;
            sr.transform.localScale = new Vector3(s, s, s);
            sr.transform.localPosition = new Vector3(0f, sprite.bounds.size.y * s * 0.5f, 0f);
            _billboards.Add(go.transform);
            return go.transform;
        }

        private void Billboards()
        {
            if (_cam == null)
            {
                return;
            }

            var yaw = Quaternion.Euler(0f, _cam.transform.eulerAngles.y, 0f);
            for (var i = _billboards.Count - 1; i >= 0; i--)
            {
                var t = _billboards[i];
                if (t == null)
                {
                    _billboards.RemoveAt(i);
                    continue;
                }

                t.rotation = yaw;
            }

            foreach (var b in _boxes.Values)
            {
                b.Punch = Mathf.MoveTowards(b.Punch, 0f, Time.deltaTime * 5f);
                var k = 1f + (0.12f * Mathf.Sin(b.Punch * Mathf.PI));
                b.Root.localScale = new Vector3(k, 2f - k, 1f);
                if (b.Sprite != null)
                {
                    b.Sprite.color = Color.Lerp(Color.white, new Color(1.6f, 1.6f, 1.6f, 1f), b.Punch);
                }
            }
        }

        // ---------- boxes, shots, bursts ----------

        private sealed class BoxView
        {
            public const float Size = 1.7f;
            public Transform Root = null!;
            public SpriteRenderer? Sprite;
            public float Punch;

            /// <summary>The 3D unit the box pays (follower and hero boxes), standing on the lid until the box breaks.</summary>
            public RunnerRig? Captive;

            /// <summary>Top of the box and whatever stands on it, metres (the HP bar sits above it).</summary>
            public float Top = Size * 0.95f;
        }

        private BoxView BuildBox(RunnerBox b)
        {
            var root = Billboard(_actors!, "Box", b.Def.ArtKey, new Vector3(b.X, 0f, b.Z), BoxView.Size, new Color(0.6f, 0.42f, 0.2f, 1f));
            LockedBattleGround.AddContactShadow(root, BoxView.Size, BoxView.Size * 0.7f);
            _boxDefs[b.Id] = b.Def;
            var view = new BoxView { Root = root, Sprite = root.GetComponentInChildren<SpriteRenderer>() };
            if (b.Def.GivesUnits && _catalog != null && _actors != null)
            {
                // The real battle rig of what's inside (not its portrait): rest pose on the lid, facing the squad.
                var captive = RunnerRig.Spawn(_catalog.Unit(b.Def.Unit).Roster, _actors, "Captive_" + b.Def.Unit + "_" + b.Id, CaptiveScale);
                if (captive != null)
                {
                    captive.Slot.position = new Vector3(b.X, BoxView.Size * 0.5f, b.Z + 0.15f);
                    captive.Slot.rotation = Quaternion.Euler(0f, 180f, 0f);
                    captive.Rest();
                    view.Captive = captive;
                    view.Top = (BoxView.Size * 0.5f) + captive.Height + 0.1f;
                }
            }

            return view;
        }

        private sealed class ShotView
        {
            public Transform Root = null!;
            public Renderer? Head;
        }

        private ShotView BuildShot(RunnerShot s)
        {
            var name = ShooterName(s);
            ShotColors(name, out var core, out var glow);
            var arrow = name == "Rowan" || name == "Bonequill" || name == "Oakenshield";
            var go = new GameObject("Shot_" + name);
            go.transform.SetParent(_actors, false);
            go.transform.position = new Vector3(s.X, ShotHeight, s.Z);
            var head = GameObject.CreatePrimitive(arrow ? PrimitiveType.Cube : PrimitiveType.Sphere);
            Destroy(head.GetComponent<Collider>());
            head.transform.SetParent(go.transform, false);
            head.transform.localScale = arrow ? new Vector3(0.06f, 0.06f, 0.7f) : Vector3.one * 0.32f;
            var r = head.GetComponent<Renderer>();
            r.sharedMaterial = Unlit(core);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.18f;
            trail.startWidth = arrow ? 0.08f : 0.26f;
            trail.endWidth = 0f;
            trail.sharedMaterial = Unlit(glow);
            trail.startColor = glow;
            trail.endColor = new Color(glow.r, glow.g, glow.b, 0f);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return new ShotView { Root = go.transform, Head = r };
        }

        private string ShooterName(RunnerShot s)
        {
            if (_run == null)
            {
                return "";
            }

            if (s.ByFoe)
            {
                // The shooter may be dead already; the shot keeps its look.
                foreach (var pair in _foes)
                {
                    if (pair.Key == s.Shooter)
                    {
                        return pair.Value.Data.Name;
                    }
                }

                return "Bonequill";
            }

            return _run.Member(s.Shooter)?.Unit.Roster ?? "";
        }

        private static void ShotColors(string name, out Color core, out Color glow)
        {
            switch (name)
            {
                case "Lyra":
                    core = new Color(0.85f, 0.75f, 1f, 1f);
                    glow = new Color(0.55f, 0.4f, 1f, 0.9f);
                    return;
                case "Rowan":
                    core = new Color(0.95f, 0.88f, 0.6f, 1f);
                    glow = new Color(1f, 0.85f, 0.4f, 0.7f);
                    return;
                case "Oakenshield":
                    core = new Color(0.55f, 0.85f, 0.35f, 1f);
                    glow = new Color(0.4f, 0.9f, 0.3f, 0.7f);
                    return;
                case "Bonequill":
                    core = new Color(0.92f, 0.9f, 0.82f, 1f);
                    glow = new Color(0.6f, 0.95f, 0.7f, 0.7f);
                    return;
                default:
                    core = Color.white;
                    glow = new Color(0.6f, 0.8f, 1f, 0.8f);
                    return;
            }
        }

        private static readonly Dictionary<Color, Material> Unlits = new Dictionary<Color, Material>();

        private static Material Unlit(Color c)
        {
            if (Unlits.TryGetValue(c, out var m) && m != null)
            {
                return m;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            m = new Material(shader);
            if (m.HasProperty("_BaseColor"))
            {
                m.SetColor("_BaseColor", c);
            }

            m.color = c;
            if (c.a < 0.99f)
            {
                LockedBattleGround.MakeTransparent(m);
            }

            Unlits[c] = m;
            return m;
        }

        private sealed class Fx
        {
            public GameObject Go = null!;
            public SpriteRenderer? Sprite;
            public float Age;
            public float Life;
            public Vector3 Velocity;
            public float Grow;
            public float Spin;
            public Color Color;
            public float Size;
        }

        private void AddFx(string key, Vector3 at, float size, float life, Color color, Vector3 velocity, float grow, float spin)
        {
            var sprite = SurvivalArt.Get(key) ?? SurvivalArt.Circle();
            var go = new GameObject("Fx");
            go.transform.SetParent(_actors, false);
            go.transform.position = at;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = 10;
            var w = Mathf.Max(0.01f, sprite.bounds.size.x);
            go.transform.localScale = Vector3.one * (size / w);
            _fx.Add(new Fx { Go = go, Sprite = sr, Life = life, Velocity = velocity, Grow = grow, Spin = spin, Color = color, Size = size / w });
        }

        private void TickFx(float dt)
        {
            if (_cam == null)
            {
                return;
            }

            var face = _cam.transform.rotation;
            for (var i = _fx.Count - 1; i >= 0; i--)
            {
                var f = _fx[i];
                f.Age += dt;
                if (f.Go == null || f.Age >= f.Life)
                {
                    if (f.Go != null)
                    {
                        Destroy(f.Go);
                    }

                    _fx.RemoveAt(i);
                    continue;
                }

                var u = f.Age / f.Life;
                f.Velocity += Vector3.down * (f.Grow > 0f ? 0f : 9f * dt);
                f.Go.transform.position += f.Velocity * dt;
                f.Go.transform.rotation = face * Quaternion.Euler(0f, 0f, f.Spin * f.Age);
                f.Go.transform.localScale = Vector3.one * (f.Size * (1f + (f.Grow * u)));
                if (f.Sprite != null)
                {
                    var c = f.Color;
                    c.a *= 1f - (u * u);
                    f.Sprite.color = c;
                }
            }
        }

        /// <summary>Kill / win burst: ring, glow and sparks; the win adds the celebration rays and confetti.</summary>
        private void Burst(Vector3 at, Color color, float size, bool celebrate)
        {
            AddFx(HookArtKeys.FxBurstRing, at, size, 0.45f, color, Vector3.zero, 1.6f, 0f);
            AddFx(HookArtKeys.FxGlow, at, size * 0.8f, 0.35f, new Color(1f, 0.9f, 0.6f, 0.8f), Vector3.zero, 0.8f, 0f);
            Sparks(at, color, celebrate ? 18 : 8, size);
            if (celebrate)
            {
                AddFx(HookArtKeys.FxRays, at + (Vector3.up * 0.5f), size * 1.8f, 1.6f, new Color(1f, 0.92f, 0.55f, 0.85f), Vector3.zero, 0.5f, 40f);
                for (var i = 0; i < 16; i++)
                {
                    var v = new Vector3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(4f, 8f), UnityEngine.Random.Range(-1f, 2f));
                    AddFx(HookArtKeys.FxConfetti, at, 0.35f, 1.6f, Color.white, v, 0f, UnityEngine.Random.Range(-400f, 400f));
                }
            }
        }

        private void Sparks(Vector3 at, Color color, int count, float size)
        {
            for (var i = 0; i < count; i++)
            {
                var a = (i / (float)count) * Mathf.PI * 2f;
                var v = new Vector3(Mathf.Cos(a) * 2.6f, UnityEngine.Random.Range(2.5f, 5f), Mathf.Sin(a) * 1.2f) * (0.6f + (size * 0.2f));
                AddFx(HookArtKeys.FxSparkle, at, 0.32f, 0.7f, color, v, 0f, 300f);
            }
        }

        /// <summary>The box break: the hero chest opens (or the crate bursts), celebration rays, sparks and the reward call-out.</summary>
        private void BoxBurst(RunnerEvent e)
        {
            var at = new Vector3(e.X, 0.9f, e.Z);
            _boxDefs.TryGetValue(e.Id, out var box);
            var gold = new Color(1f, 0.85f, 0.35f, 1f);
            if (box != null && box.GivesUnits)
            {
                AddFx(RunnerArtKeys.ChestOpen, at, BoxView.Size * 1.1f, 0.9f, Color.white, Vector3.up * 0.6f, 0.2f, 0f);
            }

            AddFx(HookArtKeys.FxRays, at, 3.4f, 0.9f, new Color(1f, 0.92f, 0.55f, 0.8f), Vector3.zero, 0.6f, 60f);
            AddFx(HookArtKeys.FxBurstRing, at, 2.2f, 0.5f, gold, Vector3.zero, 1.8f, 0f);
            Sparks(at, gold, 14, 2f);
            if (box == null)
            {
                return;
            }

            var call = box.Kind switch
            {
                RunnerBoxKind.Weapon => Fmt("runner.upgrade.weapon", "Weapon up! +{0}% damage", Mathf.RoundToInt(box.Amount * 100f)),
                RunnerBoxKind.Rate => Fmt("runner.upgrade.rate", "Faster attacks! +{0}% speed", Mathf.RoundToInt(box.Amount * 100f)),
                RunnerBoxKind.Hero => Fmt("runner.box.hero", "{0} joins!", HeroName(box.Unit)),
                _ => Fmt("runner.joined", "+{0} {1}", box.Count, HeroName(box.Unit))
            };
            Banner(call, box.Kind == RunnerBoxKind.Rate ? new Color(0.55f, 0.9f, 1f, 1f) : gold);
        }

        private void Slash(Vector3 at, string who)
        {
            ShotColors(who, out _, out var glow);
            AddFx(HookArtKeys.FxBurstRing, at, 1.1f, 0.25f, new Color(0.85f, 0.92f, 1f, 0.9f), Vector3.zero, 1.2f, 0f);
            AddFx(HookArtKeys.FxSparkle, at, 0.6f, 0.25f, glow, Vector3.zero, 0.5f, 600f);
        }

        private string HeroName(string unitId)
        {
            var roster = _catalog != null && _catalog.Units.TryGetValue(unitId, out var u) ? u.Roster : unitId;
            var key = "hero." + roster.ToLowerInvariant().Replace(' ', '_') + ".name";
            return S(key, roster);
        }
    }

    /// <summary>Full-screen drag pad under the HUD: horizontal drags steer (mouse or touch, either input backend).</summary>
    public sealed class RunnerDragPad : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        internal RunnerLevelView? View;

        public void OnPointerDown(PointerEventData eventData)
        {
        }

        public void OnDrag(PointerEventData eventData) => View?.Drag(eventData.delta.x);
    }

    public sealed class RunnerAutoDestroy : MonoBehaviour
    {
        public float Seconds = 0.3f;

        private void Update()
        {
            Seconds -= Time.deltaTime;
            if (Seconds <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
