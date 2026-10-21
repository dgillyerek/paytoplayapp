using System.Collections.Generic;
using Survival.Domain.Campaign;
using Survival.Domain.Roster;
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Configured mode of the locked battle (Play → Battle Setup → Fight). Only when <see cref="BattleHandoff.Pending"/>
    /// holds a lineup at boot; otherwise every method here is a no-op and BattleLocked plays its standalone demo.
    /// The lineup's heroes and villains spawn on their own locked rigs with their three-attack rotations and hit
    /// reactions, lane by lane (lanes as wide as their widest body, 2x dragons get wide lanes). Each landed hit deals
    /// damage (<see cref="BattleCombat"/>); a fighter at 0 HP shrinks away, and whoever it fought retargets the nearest
    /// standing opponent. When one side is down (or at the timeout, or on Retreat) the winners rest, a VICTORY / DEFEAT
    /// banner shows, and the result goes back through <see cref="BattleHandoff.Complete"/>.
    /// </summary>
    public sealed partial class LockedBattleDemo
    {
        private const float DefeatHoldSeconds = 0.25f;
        private const float DefeatShrinkSeconds = 0.6f;
        private const float EndBannerSeconds = 2.2f;

        private static readonly Color HeroFill = new Color(0.36f, 0.86f, 0.42f, 1f);
        private static readonly Color VillainFill = new Color(0.92f, 0.30f, 0.32f, 1f);

        private BattleLineup? _config;
        private BattleCombat? _combat;
        private RectTransform? _hud;
        private Text? _clock;
        private Text? _score;
        private Text? _banner;
        private bool _ended;
        private float _endAge;
        private readonly List<DamagePop> _pops = new List<DamagePop>();

        /// <summary>True while this battle runs a handed-off lineup.</summary>
        public bool Configured => _config != null;

        private sealed class DamagePop
        {
            public Text Text = null!;
            public Vector3 World;
            public float Age;
        }

        private sealed partial class Fighter
        {
            public CombatUnit? Unit { get; set; }
            public bool Down { get; set; }
            public float DownAge { get; set; }
            public RectTransform? Bar { get; set; }
            public Image? BarFill { get; set; }
            public float ShownFraction { get; set; } = 1f;

            /// <summary>Lane width this fighter stands in (the bar stays inside it).</summary>
            public float LaneWidth { get; set; } = BattleLaneLayout.BaseLaneMetres;

            /// <summary>Widest point of the body sideways (metres, battle scale, wings included).</summary>
            public float BarHalfWidth { get; set; }

            public float BarMeasureAge { get; set; }
        }

        private bool BootConfigured()
        {
            var lineup = BattleHandoff.Pending;
            if (lineup == null)
            {
                return false;
            }

            _config = lineup;
            _combat = new BattleCombat(lineup);

            // Lane widths from the widest body in each lane.
            var widths = new float[Mathf.Max(1, lineup.LaneCount)];
            var depth = new float[widths.Length];
            for (var i = 0; i < widths.Length; i++)
            {
                var scale = 1f;
                foreach (var lf in lineup.All)
                {
                    if (lf.Lane == i && TryRoster(lf.Fighter, out var d))
                    {
                        scale = Mathf.Max(scale, LockedBattleRoster.BattleScaleOf(d));
                    }
                }

                widths[i] = BattleLaneLayout.LaneWidth(scale);
                depth[i] = LockedBattleRoster.StartHalfGapMetres * (scale > 1.01f ? 1.4f : 1f);
            }

            var centres = BattleLaneLayout.Centres(widths);
            foreach (var unit in _combat.Units)
            {
                if (!TryRoster(unit.Fighter.Fighter, out var data))
                {
                    Debug.LogWarning("Locked battle (configured): no roster fighter named " + unit.Fighter.Fighter + "; skipped.");
                    continue;
                }

                var f = Spawn(data);
                f.Unit = unit;
                var lane = Mathf.Clamp(unit.Fighter.Lane, 0, centres.Length - 1);
                var hero = unit.Side == CampaignSide.Hero;
                f.StartPosition = new Vector3(centres[lane], 0f, hero ? -depth[lane] : depth[lane]);
                f.StartRotation = Quaternion.Euler(0f, hero ? 0f : 180f, 0f);
                f.Slot.SetPositionAndRotation(f.StartPosition, f.StartRotation);
                f.LaneWidth = widths[lane];
                f.BarHalfWidth = BattleHpBarLayout.HalfWidthOf(data.Name, data.DesignHeightMetres, f.BattleScale);
                _fighters.Add(f);
            }

            foreach (var f in _fighters)
            {
                f.Target = NearestFoe(f);
            }

            BuildLights();
            BuildGround();
            _camera = gameObject.GetComponent<LockedBattleCamera>() ?? gameObject.AddComponent<LockedBattleCamera>();
            _camera.SetField(FieldBounds());
            BuildConfiguredHud();
            Restart();
            Debug.Log("Locked battle (configured): " + lineup.Stage.Name + ", " + lineup.Heroes.Count + " hero(es) vs " + lineup.Enemies.Count + " villain(s) in " + lineup.LaneCount + " lane(s).");
            return true;
        }

        private static bool TryRoster(string name, out LockedBattleFighter fighter)
        {
            foreach (var f in LockedBattleRoster.All)
            {
                if (string.Equals(f.Name, name, System.StringComparison.Ordinal))
                {
                    fighter = f;
                    return true;
                }
            }

            fighter = default;
            return false;
        }

        private Fighter? NearestFoe(Fighter from)
        {
            if (_combat == null || from.Unit == null)
            {
                return null;
            }

            var positions = new Dictionary<int, (float, float)>();
            foreach (var f in _fighters)
            {
                if (f.Unit != null)
                {
                    var p = f.Built ? VisualPosition(f) : f.StartPosition;
                    positions[f.Unit.Uid] = (p.x, p.z);
                }
            }

            var unit = _combat.NearestFoe(from.Unit, u => positions.TryGetValue(u.Uid, out var p) ? p : (0f, 0f));
            if (unit == null)
            {
                return null;
            }

            foreach (var f in _fighters)
            {
                if (f.Unit == unit && f.Built && !f.Down)
                {
                    return f;
                }
            }

            return null;
        }

        /// <summary>Damage for a landed hit, the floating number, and a defeat when the HP runs out.</summary>
        private void OnConfiguredHit(Fighter attacker, Fighter target, int attackIndex)
        {
            if (_combat == null || attacker.Unit == null || target.Unit == null)
            {
                return;
            }

            var hits = LockedBattleRotation.HitCount(attacker.Data, attacker.Profile, attackIndex);
            var heavy = target.Reaction.Kind == BattleReactionKind.Stagger;
            var dmg = _combat.Hit(attacker.Unit, target.Unit, hits, heavy);
            if (dmg <= 0)
            {
                return;
            }

            PopDamage(target, dmg, heavy);
            if (!target.Unit.Alive)
            {
                Defeat(target);
            }
        }

        private void Defeat(Fighter f)
        {
            f.Down = true;
            f.DownAge = 0f;
            f.Phase = Phase.Hold;
            f.AttackClipOn = false;
            f.Play(f.Data.RestPoseName);
            foreach (var other in _fighters)
            {
                if (other.Target == f && !other.Down)
                {
                    Retarget(other);
                }
            }
        }

        private void Retarget(Fighter f)
        {
            var next = NearestFoe(f);
            f.Target = next;
            if (next == null)
            {
                Rest(f);
                return;
            }

            if (f.Phase == Phase.Attack && !f.Data.AttackCyclesItself && f.AttackClipOn)
            {
                // Let the swing in progress finish; the rest-gap check walks it over (ConfiguredRewalk).
                return;
            }

            if (Distance(f, next) > f.Data.AttackRangeMetres + 0.3f)
            {
                f.RestForward = StrideForwardOf(f);
                BeginWalk(f);
            }
        }

        private static float StrideForwardOf(Fighter f) => f.StrideBone == null ? 0f : f.Slot.InverseTransformPoint(f.StrideBone.position).z;

        private static float Distance(Fighter a, Fighter b)
        {
            var d = VisualPosition(b) - VisualPosition(a);
            d.y = 0f;
            return d.magnitude;
        }

        private void Rest(Fighter f)
        {
            f.Phase = Phase.Hold;
            f.PhaseTime = 0f;
            f.AttackClipOn = false;
            f.AttackOffset = Vector3.zero;
            f.Play(f.Data.RestPoseName);
        }

        /// <summary>
        /// Configured only: between swings (in the rest gap), a fighter whose target walked out of reach after a
        /// retarget walks over again. Returns true when it switched to the walk.
        /// </summary>
        private bool ConfiguredRewalk(Fighter f)
        {
            if (_config == null || f.Target == null)
            {
                return false;
            }

            LockedBattleRotation.Locate(f.Data, f.PhaseTime, out var index, out var into);
            var attack = LockedBattleRotation.AttackAt(f.Data, index);
            if (into < attack.Seconds || Distance(f, f.Target) <= f.Data.AttackRangeMetres + 0.6f)
            {
                return false;
            }

            f.AttackClipOn = false;
            f.AttackOffset = Vector3.zero;
            f.Play(f.Data.RestPoseName);
            f.RestForward = StrideForwardOf(f);
            BeginWalk(f);
            return true;
        }

        private void TickConfigured(float dt)
        {
            if (_combat == null)
            {
                return;
            }

            if (_time >= LockedBattleRoster.OpeningHoldSeconds)
            {
                _combat.Tick(dt);
            }

            foreach (var f in _fighters)
            {
                if (f.Down)
                {
                    f.DownAge += dt;
                }
            }

            for (var i = _pops.Count - 1; i >= 0; i--)
            {
                _pops[i].Age += dt;
                if (_pops[i].Age > 0.9f)
                {
                    Destroy(_pops[i].Text.gameObject);
                    _pops.RemoveAt(i);
                }
            }

            if (_clock != null)
            {
                var left = Mathf.Max(0f, (float)(_config!.TimeoutSeconds - _combat.Elapsed));
                _clock.text = ((int)left / 60) + ":" + ((int)left % 60).ToString("00");
            }

            if (_score != null)
            {
                _score.text = "HEROES " + _combat.Standing(CampaignSide.Hero) + "/" + _config!.Heroes.Count + "   \u00b7   FOES " + _combat.Standing(CampaignSide.Villain) + "/" + _config.Enemies.Count;
            }

            if (_combat.Outcome == BattleOutcome.Ongoing)
            {
                return;
            }

            if (!_ended)
            {
                _ended = true;
                _endAge = 0f;
                foreach (var f in _fighters)
                {
                    if (!f.Down)
                    {
                        f.Target = null;
                        Rest(f);
                    }
                }

                if (_banner != null)
                {
                    var won = _combat.Outcome == BattleOutcome.Won;
                    _banner.text = won ? "VICTORY!" : _combat.Retreated ? "RETREAT" : "DEFEAT";
                    _banner.color = won ? SurvivalVisuals.Reward : new Color(1f, 0.55f, 0.5f, 1f);
                    _banner.gameObject.SetActive(true);
                }
            }

            _endAge += dt;
            if (_banner != null)
            {
                var k = _endAge < 0.25f ? Mathf.Lerp(0.3f, 1.15f, _endAge / 0.25f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((_endAge - 0.25f) / 0.2f));
                _banner.rectTransform.localScale = new Vector3(k, k, 1f);
            }

            if (_endAge >= (_combat.Retreated ? 0.6f : EndBannerSeconds) && _config != null)
            {
                var result = _combat.Result();
                _config = null;
                BattleHandoff.Complete(result);
            }
        }

        private void LateConfigured()
        {
            if (_combat == null)
            {
                return;
            }

            var cam = Camera.main;
            foreach (var f in _fighters)
            {
                if (f.Down)
                {
                    // Defeat: a short beat, then the body shrinks away into the ground (no death clip in the locked set).
                    var u = Mathf.Clamp01((f.DownAge - DefeatHoldSeconds) / DefeatShrinkSeconds);
                    var k = 1f - (u * u);
                    f.Host.localScale = Vector3.one * (f.Scale * Mathf.Max(0.001f, k));
                    if (u >= 1f && f.Slot.gameObject.activeSelf)
                    {
                        f.Slot.gameObject.SetActive(false);
                    }
                }

                if (f.Bar == null || f.Unit == null)
                {
                    continue;
                }

                var show = !f.Down && cam != null && _hud != null;
                if (show)
                {
                    show = PlaceSideBar(f, cam!);
                }

                f.Bar.gameObject.SetActive(show);
                f.ShownFraction = Mathf.MoveTowards(f.ShownFraction, f.Unit.Fraction, Time.unscaledDeltaTime * 1.5f);
                if (f.BarFill != null)
                {
                    f.BarFill.rectTransform.anchorMax = new Vector2(1f, Mathf.Max(0.001f, f.ShownFraction));
                }
            }

            foreach (var pop in _pops)
            {
                if (cam == null || _hud == null)
                {
                    break;
                }

                var screen = cam.WorldToScreenPoint(pop.World);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_hud, screen, null, out var at))
                {
                    pop.Text.rectTransform.anchoredPosition = at + new Vector2(0f, 90f * pop.Age);
                }

                var c = pop.Text.color;
                pop.Text.color = new Color(c.r, c.g, c.b, pop.Age < 0.55f ? 1f : Mathf.Lerp(1f, 0f, (pop.Age - 0.55f) / 0.35f));
                var s = pop.Age < 0.12f ? Mathf.Lerp(0.5f, 1.25f, pop.Age / 0.12f) : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((pop.Age - 0.12f) / 0.15f));
                pop.Text.rectTransform.localScale = new Vector3(s, s, 1f);
            }
        }

        /// <summary>
        /// Vertical HP bar beside the fighter on its outer / back side (heroes their left, foes their right), clear of
        /// the body and wings and out of the lane between the sides. Placed in the fighter's start frame so it does not
        /// swing when the body turns to strike; drawn in screen space so it always faces the camera, upright, and kept
        /// at a readable height at any free-camera zoom.
        /// </summary>
        private bool PlaceSideBar(Fighter f, Camera cam)
        {
            MeasureBody(f);
            var side = f.Unit!.Side;
            var (right, forward) = BattleHpBarLayout.Offset(side, f.BarHalfWidth, f.BattleScale, f.LaneWidth);
            var (bottom, top) = BattleHpBarLayout.Span(f.Data.DesignHeightMetres, f.BattleScale);
            var foot = f.Slot.position + (f.StartRotation * new Vector3(right, 0f, forward));
            foot.y = f.StartPosition.y;
            var low = cam.WorldToScreenPoint(foot + (Vector3.up * bottom));
            var high = cam.WorldToScreenPoint(foot + (Vector3.up * top));
            if (low.z <= 0f || high.z <= 0f
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_hud!, low, null, out var a)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_hud!, high, null, out var b))
            {
                return false;
            }

            var height = BattleHpBarLayout.HeightPx(Mathf.Abs(b.y - a.y));
            f.Bar!.anchoredPosition = new Vector2((a.x + b.x) * 0.5f, ((a.y + b.y) * 0.5f) - (height * 0.5f));
            f.Bar.sizeDelta = new Vector2(BattleHpBarLayout.WidthPx, height);
            return true;
        }

        /// <summary>
        /// Widest sideways extent of the rendered body (start frame), sampled over its first second so the bones have
        /// posed. Never narrower than the roster estimate (the dragons' known wing spans).
        /// </summary>
        private static void MeasureBody(Fighter f)
        {
            if (f.BarMeasureAge > 1f)
            {
                return;
            }

            f.BarMeasureAge += Time.unscaledDeltaTime;
            var axis = f.StartRotation * Vector3.right;
            var origin = f.Slot.position;
            var widest = 0f;
            foreach (var r in f.Host.GetComponentsInChildren<Renderer>(false))
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer || !r.enabled)
                {
                    continue;
                }

                var bb = r.bounds;
                for (var i = 0; i < 8; i++)
                {
                    var corner = bb.center + Vector3.Scale(bb.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    widest = Mathf.Max(widest, Mathf.Abs(Vector3.Dot(corner - origin, axis)));
                }
            }

            // An axis-aligned box overstates a turned body; cap the measurement at the lane so a bad bound can't push
            // the bar into the next lane (Offset moves any excess backwards instead).
            f.BarHalfWidth = Mathf.Max(f.BarHalfWidth, Mathf.Min(widest, f.LaneWidth));
        }

        private void PopDamage(Fighter target, int dmg, bool heavy)
        {
            if (_hud == null)
            {
                return;
            }

            var text = SurvivalVisuals.Text(_hud, "Damage", "-" + dmg, heavy ? 44 : 34, TextAnchor.MiddleCenter, heavy ? new Color(1f, 0.82f, 0.3f, 1f) : Color.white);
            text.fontStyle = FontStyle.Bold;
            HookSkin.Outlined(text, 2.5f);
            var rt = text.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(220f, 70f);
            var jitter = new Vector3(Random.Range(-0.25f, 0.25f), 0f, 0f);
            _pops.Add(new DamagePop
            {
                Text = text,
                World = target.Slot.position + jitter + (Vector3.up * (target.Data.DesignHeightMetres * target.BattleScale * 0.8f)),
                Age = 0f
            });
        }

        private void BuildConfiguredHud()
        {
            var canvas = SurvivalVisuals.Canvas(transform, "LockedBattleHud", 80);
            _hud = canvas;
            var top = SurvivalVisuals.Image(canvas, "TopPlate", new Color(0.06f, 0.08f, 0.16f, 0.82f));
            HookSkin.TrySkin(top, HookArtKeys.Header, 0.42f);
            Anchor(top.rectTransform, 0.10f, 0.925f, 0.90f, 0.990f);
            var title = HookSkin.Outlined(SurvivalVisuals.Text(top.transform, "Caption", _config!.Stage.Name.ToUpperInvariant(), 32, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 2f);
            title.fontStyle = FontStyle.Bold;
            Anchor(title.rectTransform, 0.05f, 0.30f, 0.95f, 0.98f);
            _score = HookSkin.Outlined(SurvivalVisuals.Text(top.transform, "Score", "", 20, TextAnchor.MiddleCenter, SurvivalVisuals.Gold), 1.5f);
            Anchor(_score.rectTransform, 0.05f, 0.02f, 0.95f, 0.36f);

            _clock = HookSkin.Outlined(SurvivalVisuals.Text(canvas, "Clock", "", 30, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 2f);
            Anchor(_clock.rectTransform, 0.02f, 0.86f, 0.22f, 0.915f);

            var retreat = HookSkin.Button(canvas, "LockedBattleRetreat", HookArtKeys.ButtonRed, new Color(0.45f, 0.12f, 0.12f, 0.94f), 0.5f);
            Anchor(retreat.GetComponent<RectTransform>(), 0.74f, 0.862f, 0.97f, 0.914f);
            var label = HookSkin.Outlined(SurvivalVisuals.Text(retreat.transform, "Label", "RETREAT", 24, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
            label.fontStyle = FontStyle.Bold;
            SurvivalVisuals.Stretch(label.rectTransform);
            retreat.onClick.AddListener(() => _combat?.Retreat());

            _banner = HookSkin.Outlined(SurvivalVisuals.Text(canvas, "Banner", "", 96, TextAnchor.MiddleCenter, SurvivalVisuals.Reward), 4f);
            _banner.fontStyle = FontStyle.Bold;
            Anchor(_banner.rectTransform, 0f, 0.55f, 1f, 0.68f);
            _banner.gameObject.SetActive(false);

            var heroFrame = Rgb(CampaignArtKeys.HeroFrameRgb, 1f, Color.blue);
            var villainFrame = Rgb(CampaignArtKeys.VillainFrameRgb, 1f, Color.magenta);
            foreach (var f in _fighters)
            {
                if (f.Unit == null)
                {
                    continue;
                }

                var hero = f.Unit.Side == CampaignSide.Hero;
                var bar = SurvivalVisuals.Image(canvas, f.Data.FileKey + "Hp", hero ? heroFrame : villainFrame).rectTransform;
                bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0.5f);
                bar.pivot = new Vector2(0.5f, 0f);
                bar.sizeDelta = new Vector2(BattleHpBarLayout.WidthPx, BattleHpBarLayout.MinHeightPx);
                var back = SurvivalVisuals.Image(bar, "Back", new Color(0.05f, 0.05f, 0.08f, 0.9f));
                Anchor(back.rectTransform, 0f, 0f, 1f, 1f);
                back.rectTransform.offsetMin = new Vector2(3f, 3f);
                back.rectTransform.offsetMax = new Vector2(-3f, -3f);
                var fill = SurvivalVisuals.Image(back.transform, "Fill", hero ? HeroFill : VillainFill);
                Anchor(fill.rectTransform, 0f, 0f, 1f, 1f);
                // Level tag on top of the vertical bar (the name would cross the lane).
                var name = HookSkin.Outlined(SurvivalVisuals.Text(bar, "Name", "Lv " + f.Unit.Fighter.Level, 18, TextAnchor.LowerCenter, SurvivalVisuals.Cream), 1.5f);
                var nrt = name.rectTransform;
                nrt.anchorMin = nrt.anchorMax = new Vector2(0.5f, 1f);
                nrt.pivot = new Vector2(0.5f, 0f);
                nrt.sizeDelta = new Vector2(90f, 26f);
                nrt.anchoredPosition = new Vector2(0f, 4f);
                f.Bar = bar;
                f.BarFill = fill;
            }
        }
    }
}
