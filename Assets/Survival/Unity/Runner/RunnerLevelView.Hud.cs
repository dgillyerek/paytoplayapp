#nullable enable
using System.Collections.Generic;
using System.Globalization;
using Survival.Domain.Campaign;
using Survival.Domain.Ids;
using Survival.Domain.Roster;
using Survival.Domain.Runner;
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Runner HUD in the painted kit (pack.json keys, code-drawn fallback): title plate and distance bar with the boss
    /// at its end, kills, upgrade chips, the squad badge over the hero, an HP bar with its number over every enemy and
    /// every box (the same widget for both), rising hit numbers, call-out banners and the result panel.
    /// </summary>
    public sealed partial class RunnerLevelView
    {
        private static readonly Color Ink = new Color(0.12f, 0.07f, 0.02f, 1f);
        private static readonly Color FoeFill = new Color(1f, 0.42f, 0.32f, 1f);
        private static readonly Color HeroFill = new Color(0.36f, 0.86f, 0.42f, 1f);
        private static readonly Color BattleVillainFill = new Color(0.92f, 0.30f, 0.32f, 1f);

        private RectTransform? _hud;
        private RectTransform? _worldUi;
        private Image? _progressFill;
        private Text? _progressText;
        private Text? _killsText;
        private Text? _dmgChip;
        private Text? _spdChip;
        private RectTransform? _badge;
        private Text? _badgeText;
        private Image? _badgeIcon;
        private Text? _hint;
        private RectTransform? _result;
        private RectTransform? _levelList;
        private RectTransform? _banner;
        private Text? _bannerText;
        private float _bannerAge = 9f;
        private int _lastSquad = -1;
        private float _badgePunch;
        private readonly Dictionary<int, WorldBar> _bars = new Dictionary<int, WorldBar>();
        private readonly List<Floating> _floaters = new List<Floating>();

        private sealed class WorldBar
        {
            public RectTransform Root = null!;
            public Image Fill = null!;
            public Text Number = null!;
            public bool OnBox;
            public bool Boss;
            public float Height;
            public RectTransform? Reward;

            /// <summary>A fighter's vertical side bar (the battle's layout) rather than a box's horizontal bar.</summary>
            public bool Side;
            public bool Hero;
            public RunnerRig? Rig;
            public float HalfWidth;
            public float MeasureAge;

            /// <summary>Fades the bar itself (a box's reward tag stays visible).</summary>
            public CanvasGroup Group = null!;
            public int LastHp = -1;
            public float SinceDamage = -1f;
        }

        private sealed class Floating
        {
            public RectTransform Rt = null!;
            public Text Text = null!;
            public Vector3 World;
            public float Age;
            public float Life;
            public Color Color;
            public float Pop;
        }

        private void BuildHud()
        {
            _hud = SurvivalVisuals.Canvas(transform, "RunnerHud", 60);
            _worldUi = new GameObject("WorldUi", typeof(RectTransform)).GetComponent<RectTransform>();
            _worldUi.SetParent(_hud, false);
            SurvivalVisuals.Stretch(_worldUi);

            // Drag anywhere to steer (under every button).
            var pad = SurvivalVisuals.Image(_hud, "DragPad", new Color(1f, 1f, 1f, 0f));
            pad.raycastTarget = true;
            SurvivalVisuals.Stretch(pad.rectTransform);
            pad.transform.SetAsFirstSibling();
            pad.gameObject.AddComponent<RunnerDragPad>().View = this;

            // Title plate + distance bar.
            var plate = HookSkin.Panel(_hud, "TitlePlate", HookArtKeys.Header, SurvivalVisuals.Panel, 0.6f);
            Anchor(plate.rectTransform, 0.16f, 0.925f, 0.84f, 0.985f);
            var title = HookSkin.Outlined(SurvivalVisuals.Text(plate.transform, "Title", LevelTitle(_level), 44, TextAnchor.MiddleCenter, SurvivalVisuals.Cream));
            SurvivalVisuals.Stretch(title.rectTransform);
            var bar = HookSkin.Bar(_hud, "Progress", out var fill);
            Anchor(bar.rectTransform, 0.14f, 0.885f, 0.80f, 0.915f);
            _progressFill = fill;
            _progressText = HookSkin.Outlined(SurvivalVisuals.Text(bar.transform, "Metres", "", 24, TextAnchor.MiddleCenter, Color.white));
            SurvivalVisuals.Stretch(_progressText.rectTransform);
            if (_level != null && _level.HasBoss)
            {
                RunnerRowDef? boss = null;
                foreach (var r in _level.Rows)
                {
                    if (r.Kind == RunnerRowKind.Boss)
                    {
                        boss = r;
                    }
                }

                // The boss is the 3D Ironhowl on the road; the bar's end only marks where he stands.
                var face = HookSkin.Art(_hud, "BossMark", "theme_a.ui.icon_battle", FoeFill);
                face.color = new Color(1f, 0.55f, 0.5f, 1f);
                Anchor(face.rectTransform, 0.80f, 0.872f, 0.88f, 0.928f);
                var tag = HookSkin.Outlined(SurvivalVisuals.Text(face.transform, "Boss", S("runner.boss", "BOSS"), 20, TextAnchor.UpperCenter, FoeFill));
                tag.rectTransform.anchorMin = new Vector2(0f, -0.45f);
                tag.rectTransform.anchorMax = new Vector2(1f, 0f);
                tag.rectTransform.offsetMin = tag.rectTransform.offsetMax = Vector2.zero;
            }

            // Kills (crossed swords) and upgrade chips.
            var kills = HookSkin.Panel(_hud, "Kills", HookArtKeys.Chip, SurvivalVisuals.PanelSoft, 0.5f);
            Anchor(kills.rectTransform, 0.70f, 0.835f, 0.97f, 0.875f);
            var swords = HookSkin.Art(kills.transform, "Icon", "theme_a.ui.icon_battle", SurvivalVisuals.PinFight);
            Anchor(swords.rectTransform, 0.02f, -0.15f, 0.30f, 1.15f);
            _killsText = HookSkin.Outlined(SurvivalVisuals.Text(kills.transform, "Count", "0", 30, TextAnchor.MiddleCenter, Color.white));
            Anchor(_killsText.rectTransform, 0.30f, 0f, 0.98f, 1f);
            _dmgChip = Chip("DmgChip", 0.03f, "theme_a.runner.crate_weapon");
            _spdChip = Chip("SpdChip", 0.25f, "theme_a.runner.crate_speed");

            // Leave (back to the village, or a fresh run on its own).
            var leave = HookSkin.Button(_hud, "Leave", HookArtKeys.ButtonRed, new Color(0.65f, 0.18f, 0.15f, 1f));
            Anchor(leave.GetComponent<RectTransform>(), 0.015f, 0.93f, 0.14f, 0.98f);
            leave.onClick.AddListener(Leave);
            var leaveText = HookSkin.Outlined(SurvivalVisuals.Text(leave.transform, "Label", S("runner.leave", "Leave"), 24, TextAnchor.MiddleCenter, Color.white));
            SurvivalVisuals.Stretch(leaveText.rectTransform);

            // Squad badge over the hero (count of everyone walking).
            _badge = HookSkin.Panel(_worldUi, "SquadBadge", HookArtKeys.Chip, new Color(0.10f, 0.22f, 0.45f, 0.96f), 0.45f).rectTransform;
            _badge.sizeDelta = new Vector2(150f, 62f);
            _badgeIcon = HookSkin.Art(_badge, "Icon", "theme_a.ui.icon_battle", SurvivalVisuals.PinBuild);
            Anchor(_badgeIcon.rectTransform, -0.05f, -0.15f, 0.40f, 1.15f);
            _badgeText = HookSkin.Outlined(SurvivalVisuals.Text(_badge, "Count", "1", 36, TextAnchor.MiddleCenter, Color.white));
            Anchor(_badgeText.rectTransform, 0.40f, 0f, 1f, 1f);

            _hint = HookSkin.Outlined(SurvivalVisuals.Text(_hud, "Hint", "⟵  " + S("runner.hint", "Drag or A / D to steer") + "  ⟶", 34, TextAnchor.MiddleCenter, SurvivalVisuals.Cream));
            Anchor(_hint.rectTransform, 0f, 0.10f, 1f, 0.15f);

            var banner = HookSkin.Panel(_hud, "Banner", HookArtKeys.RibbonBlue, new Color(0.12f, 0.24f, 0.5f, 0.95f), 0.6f);
            _banner = banner.rectTransform;
            Anchor(_banner, 0.08f, 0.70f, 0.92f, 0.765f);
            _bannerText = HookSkin.Outlined(SurvivalVisuals.Text(_banner, "Text", "", 40, TextAnchor.MiddleCenter, Color.white));
            SurvivalVisuals.Stretch(_bannerText.rectTransform);
            _banner.gameObject.SetActive(false);
        }

        private Text Chip(string name, float x, string iconKey)
        {
            var chip = HookSkin.Panel(_hud!, name, HookArtKeys.Chip, SurvivalVisuals.PanelSoft, 0.5f);
            Anchor(chip.rectTransform, x, 0.835f, x + 0.2f, 0.875f);
            var icon = HookSkin.Art(chip.transform, "Icon", iconKey, SurvivalVisuals.Gold);
            Anchor(icon.rectTransform, 0.0f, -0.25f, 0.36f, 1.25f);
            var t = HookSkin.Outlined(SurvivalVisuals.Text(chip.transform, "Value", "", 24, TextAnchor.MiddleCenter, Color.white));
            Anchor(t.rectTransform, 0.34f, 0f, 1f, 1f);
            chip.gameObject.SetActive(false);
            return t;
        }

        private static void Anchor(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void ShowHint(bool on)
        {
            if (_hint != null)
            {
                _hint.gameObject.SetActive(on && !_steered);
            }
        }

        private void TickHud(float dt)
        {
            if (_run == null)
            {
                return;
            }

            if (_progressFill != null)
            {
                HookSkin.SetBar(_progressFill, _run.Progress, HookArtKeys.BarFillGold, SurvivalVisuals.Gold);
            }

            if (_progressText != null)
            {
                _progressText.text = Mathf.FloorToInt(_run.Distance).ToString(CultureInfo.InvariantCulture) + " / " + Mathf.RoundToInt(_run.Level.Length) + " m";
            }

            if (_killsText != null)
            {
                _killsText.text = _run.Kills.ToString(CultureInfo.InvariantCulture);
            }

            SetChip(_dmgChip, _run.DamageMul, "runner.box.weapon", "+{0}% DMG");
            SetChip(_spdChip, _run.RateMul, "runner.box.rate", "+{0}% SPD");
            var squad = _run.Squad.Count;
            if (squad != _lastSquad)
            {
                _badgePunch = _lastSquad >= 0 ? 1f : 0f;
                _lastSquad = squad;
                if (_badgeText != null)
                {
                    _badgeText.text = "×" + squad.ToString(CultureInfo.InvariantCulture);
                }
            }

            _badgePunch = Mathf.MoveTowards(_badgePunch, 0f, dt * 3.5f);
            if (_badge != null)
            {
                _badge.localScale = Vector3.one * (1f + (0.3f * Mathf.Sin(_badgePunch * Mathf.PI)));
                _badge.gameObject.SetActive(!_run.Over || _run.Outcome == RunnerOutcome.Won);
            }

            if (_banner != null)
            {
                _bannerAge += dt;
                var on = _bannerAge < 1.8f;
                _banner.gameObject.SetActive(on);
                if (on)
                {
                    var t = Mathf.Clamp01(_bannerAge / 0.18f);
                    _banner.localScale = Vector3.one * Mathf.LerpUnclamped(1.4f, 1f, t);
                }
            }

            TickFloaters(dt);
        }

        private void SetChip(Text? chip, float mul, string key, string fallback)
        {
            if (chip == null)
            {
                return;
            }

            var on = mul > 1.0001f;
            chip.transform.parent.gameObject.SetActive(on);
            if (on)
            {
                chip.text = Fmt(key, fallback, Mathf.RoundToInt((mul - 1f) * 100f));
            }
        }

        private void Banner(string text, Color color)
        {
            if (_banner == null || _bannerText == null)
            {
                return;
            }

            _bannerText.text = text;
            _bannerText.color = color;
            _bannerAge = 0f;
            _banner.gameObject.SetActive(true);
        }

        // ---------- world-anchored UI ----------

        private void AddBar(int id, bool onBox, float height, bool boss)
        {
            if (_worldUi == null || _bars.ContainsKey(id))
            {
                return;
            }

            // A thin, semi-transparent strip with a small HP number, shown only while the box is being shot (see
            // RunnerSideBar.Alpha); the reward tag above it stays, so you always know what is inside.
            var root = new GameObject(onBox ? "BoxBar" : "FoeBar", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(_worldUi, false);
            root.sizeDelta = new Vector2(110f, 9f);
            var body = new GameObject("Body", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
            body.SetParent(root, false);
            SurvivalVisuals.Stretch(body);
            var group = body.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            var back = SurvivalVisuals.Image(body, "Back", new Color(0.05f, 0.05f, 0.08f, 0.8f));
            SurvivalVisuals.Stretch(back.rectTransform);
            var fill = SurvivalVisuals.Image(back.transform, "Fill", SurvivalVisuals.Gold);
            Anchor(fill.rectTransform, 0f, 0f, 1f, 1f);
            fill.rectTransform.offsetMin = new Vector2(1.5f, 1.5f);
            fill.rectTransform.offsetMax = new Vector2(-1.5f, -1.5f);
            var number = HookSkin.Outlined(SurvivalVisuals.Text(body, "Hp", "", 18, TextAnchor.LowerCenter, Color.white), 1.2f);
            number.rectTransform.anchorMin = new Vector2(0f, 1f);
            number.rectTransform.anchorMax = new Vector2(1f, 3.4f);
            number.rectTransform.offsetMin = number.rectTransform.offsetMax = Vector2.zero;
            var bar = new WorldBar { Root = root, Fill = fill, Number = number, OnBox = onBox, Boss = boss, Height = height, Group = group };
            if (onBox && _boxDefs.TryGetValue(id, out var def))
            {
                bar.Reward = RewardTag(root, def);
            }

            _bars[id] = bar;
        }

        /// <summary>
        /// A fighter's HP bar as the configured battle draws it (#76): vertical, beside the body on its own outer side
        /// (the hero's left, a foe's right), filling bottom to top, with the HP number on top.
        /// </summary>
        private void AddSideBar(int id, bool hero, RunnerRig rig, bool boss)
        {
            if (_worldUi == null || _bars.ContainsKey(id))
            {
                return;
            }

            // The configured battle's frame and fill colours (navy hero frame / purple villain frame, green / red fill).
            var rgb = hero ? CampaignArtKeys.HeroFrameRgb : CampaignArtKeys.VillainFrameRgb;
            // Slimmed to a 6 px sliver with a 1 px frame, and faded in only while the unit is taking damage. Only the
            // boss carries a (small) HP number; fodder and the hero show the sliver alone.
            var root = SurvivalVisuals.Image(_worldUi, hero ? "HeroHp" : "FoeHp", new Color(rgb[0], rgb[1], rgb[2], 1f)).rectTransform;
            root.anchorMin = root.anchorMax = Vector2.zero;
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(RunnerSideBar.WidthPx, RunnerSideBar.MinHeightPx);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            var back = SurvivalVisuals.Image(root, "Back", new Color(0.05f, 0.05f, 0.08f, 0.85f));
            Anchor(back.rectTransform, 0f, 0f, 1f, 1f);
            back.rectTransform.offsetMin = new Vector2(1f, 1f);
            back.rectTransform.offsetMax = new Vector2(-1f, -1f);
            var fill = SurvivalVisuals.Image(back.transform, "Fill", hero ? HeroFill : BattleVillainFill);
            Anchor(fill.rectTransform, 0f, 0f, 1f, 1f);
            var number = HookSkin.Outlined(SurvivalVisuals.Text(root, "Hp", "", 18, TextAnchor.LowerCenter, Color.white), 1.2f);
            var nrt = number.rectTransform;
            nrt.anchorMin = nrt.anchorMax = new Vector2(0.5f, 1f);
            nrt.pivot = new Vector2(0.5f, 0f);
            nrt.sizeDelta = new Vector2(110f, 24f);
            nrt.anchoredPosition = new Vector2(0f, 3f);
            number.gameObject.SetActive(boss);
            _bars[id] = new WorldBar
            {
                Root = root, Fill = fill, Number = number, Boss = boss, Height = rig.Height, Side = true, Hero = hero, Rig = rig,
                HalfWidth = RunnerSideBar.HalfWidth(rig.Height), Group = group,
            };
        }

        /// <summary>Stand a side bar beside its fighter: foot from the battle's layout, height from the projected body.</summary>
        private bool PlaceSideBar(WorldBar bar, Vector3 origin, float fraction)
        {
            var scale = bar.Rig != null ? LockedBattleRoster.BattleScaleOf(bar.Rig.Data) * (bar.Boss ? BossScale : 1f) : 1f;
            var (dx, dz) = RunnerSideBar.WorldOffset(bar.Hero, bar.HalfWidth, scale);
            var (bottom, top) = RunnerSideBar.Span(bar.Height);
            var foot = new Vector3(origin.x + dx, 0f, origin.z + dz);
            var low = _cam!.WorldToScreenPoint(foot + (Vector3.up * bottom));
            var high = _cam.WorldToScreenPoint(foot + (Vector3.up * top));
            if (low.z <= 0.1f || high.z <= 0.1f)
            {
                return false;
            }

            var canvas = _hud!.GetComponent<Canvas>();
            var k = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            var height = RunnerSideBar.HeightPx(Mathf.Abs(high.y - low.y) / k);
            bar.Root.anchorMin = bar.Root.anchorMax = Vector2.zero;
            bar.Root.anchoredPosition = new Vector2((low.x + high.x) * 0.5f / k, (((low.y + high.y) * 0.5f) / k) - (height * 0.5f));
            bar.Root.sizeDelta = new Vector2(RunnerSideBar.WidthPx, height);
            bar.Fill.rectTransform.anchorMax = new Vector2(1f, Mathf.Max(0.001f, fraction));
            return low.x > -200f && low.x < Screen.width + 200f && low.y > -200f && low.y < Screen.height + 200f;
        }

        /// <summary>Widest sideways reach of the rendered body, measured over its first second (the battle does the same).</summary>
        private static void MeasureBar(WorldBar bar)
        {
            if (bar.Rig == null || bar.Rig.Host == null || bar.MeasureAge > 1f)
            {
                return;
            }

            bar.MeasureAge += Time.deltaTime;
            var origin = bar.Rig.Slot.position;
            var widest = 0f;
            foreach (var r in bar.Rig.Host.GetComponentsInChildren<Renderer>(false))
            {
                if (!r.enabled || (!(r is SkinnedMeshRenderer) && !(r is MeshRenderer)))
                {
                    continue;
                }

                var bb = r.bounds;
                widest = Mathf.Max(widest, Mathf.Abs(bb.min.x - origin.x), Mathf.Abs(bb.max.x - origin.x));
            }

            bar.HalfWidth = Mathf.Max(bar.HalfWidth, Mathf.Min(widest, 2.5f));
        }

        /// <summary>What a box pays, drawn over its bar: hero portrait + "+3", or the upgrade %.</summary>
        private RectTransform RewardTag(RectTransform bar, RunnerBoxDef def)
        {
            var tag = HookSkin.Panel(bar, "Reward", HookArtKeys.Plate, new Color(0.12f, 0.2f, 0.36f, 0.95f), 0.4f).rectTransform;
            // Fixed size above the (thin, often hidden) bar, clear of the HP number.
            tag.anchorMin = tag.anchorMax = new Vector2(0.5f, 1f);
            tag.pivot = new Vector2(0.5f, 0f);
            tag.sizeDelta = new Vector2(150f, 40f);
            tag.anchoredPosition = new Vector2(0f, 26f);
            string label;
            string icon;
            switch (def.Kind)
            {
                case RunnerBoxKind.Weapon:
                    label = Fmt("runner.box.weapon", "+{0}% DMG", Mathf.RoundToInt(def.Amount * 100f));
                    icon = "theme_a.runner.crate_weapon";
                    break;
                case RunnerBoxKind.Rate:
                    label = Fmt("runner.box.rate", "+{0}% SPD", Mathf.RoundToInt(def.Amount * 100f));
                    icon = "theme_a.runner.crate_speed";
                    break;
                default:
                {
                    // The unit itself stands caged on the box in 3D (see BuildBox); the tag only says how many.
                    label = def.Kind == RunnerBoxKind.Hero ? HeroName(def.Unit) : Fmt("runner.box.followers", "+{0}", def.Count) + " " + HeroName(def.Unit);
                    icon = string.Empty;
                    break;
                }
            }

            var textLeft = 0.04f;
            if (icon.Length > 0)
            {
                var art = HookSkin.Art(tag, "Icon", icon, SurvivalVisuals.Gold);
                Anchor(art.rectTransform, -0.05f, -0.15f, 0.38f, 1.15f);
                textLeft = 0.36f;
            }

            var text = HookSkin.Outlined(SurvivalVisuals.Text(tag, "Text", label, 24, TextAnchor.MiddleCenter, def.Kind == RunnerBoxKind.Rate ? new Color(0.6f, 0.92f, 1f, 1f) : SurvivalVisuals.Reward));
            Anchor(text.rectTransform, textLeft, 0f, 0.96f, 1f);
            return tag;
        }

        private void RemoveBar(int id)
        {
            if (_bars.TryGetValue(id, out var bar))
            {
                _bars.Remove(id);
                if (bar.Root != null)
                {
                    Destroy(bar.Root.gameObject);
                }
            }
        }

        private void PlaceWorldUi()
        {
            if (_cam == null || _run == null || _hud == null)
            {
                return;
            }

            foreach (var pair in _bars)
            {
                var bar = pair.Value;
                Vector3 at;
                float fraction;
                int hp;
                if (bar.OnBox)
                {
                    var b = _run.Box(pair.Key);
                    if (b == null)
                    {
                        continue;
                    }

                    at = new Vector3(b.X, bar.Height + 0.25f, b.Z);
                    fraction = b.HpFraction;
                    hp = b.Hp;
                    FadeBar(bar, hp, b.MaxHp);
                }
                else if (bar.Side)
                {
                    Vector3 origin;
                    if (bar.Hero)
                    {
                        var m = _run.Member(pair.Key);
                        if (m == null)
                        {
                            continue;
                        }

                        origin = new Vector3(m.X, 0f, m.Z);
                        fraction = Mathf.Clamp01(m.Hp / (float)Mathf.Max(1, m.Unit.Hp));
                        hp = m.Hp;
                        FadeBar(bar, hp, m.Unit.Hp);
                    }
                    else
                    {
                        var f = _run.Foe(pair.Key);
                        if (f == null)
                        {
                            continue;
                        }

                        origin = new Vector3(f.X, 0f, f.Z);
                        fraction = f.HpFraction;
                        hp = f.Hp;
                        FadeBar(bar, hp, f.MaxHp);
                    }

                    MeasureBar(bar);
                    var shown = bar.Group.alpha > 0.001f && (bar.Rig == null || !bar.Rig.Dead) && PlaceSideBar(bar, origin, fraction);
                    bar.Root.gameObject.SetActive(shown);
                    bar.Number.text = hp.ToString(CultureInfo.InvariantCulture);
                    continue;
                }
                else
                {
                    var f = _run.Foe(pair.Key);
                    if (f == null)
                    {
                        continue;
                    }

                    at = new Vector3(f.X, bar.Height + 0.3f, f.Z);
                    fraction = f.HpFraction;
                    hp = f.Hp;
                }

                var visible = Place(bar.Root, at);
                bar.Root.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                bar.Fill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.001f, fraction), 1f);
                bar.Number.text = hp.ToString(CultureInfo.InvariantCulture);
            }

            if (_badge != null && _members.TryGetValue(_run.Hero.Id, out var hero))
            {
                Place(_badge, hero.Slot.position + (Vector3.up * (hero.Height + 0.55f)));
            }

            foreach (var f in _floaters)
            {
                Place(f.Rt, f.World + (Vector3.up * (f.Age * 1.4f)));
            }
        }

        /// <summary>Hidden at full HP; in on a hit, held while hits land, faded out two seconds after the last one.</summary>
        private static void FadeBar(WorldBar bar, int hp, int maxHp)
        {
            if (bar.LastHp >= 0 && hp < bar.LastHp)
            {
                bar.SinceDamage = 0f;
            }
            else if (bar.SinceDamage >= 0f)
            {
                bar.SinceDamage += Time.deltaTime;
            }

            bar.LastHp = hp;
            bar.Group.alpha = RunnerSideBar.Alpha(hp, maxHp, bar.SinceDamage);
        }

        /// <summary>Put a HUD rect over a world point; false when it is behind the camera or off screen.</summary>
        private bool Place(RectTransform rt, Vector3 world)
        {
            var sp = _cam!.WorldToScreenPoint(world);
            if (sp.z <= 0.1f)
            {
                return false;
            }

            var canvas = _hud!.GetComponent<Canvas>();
            var scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.anchoredPosition = new Vector2(sp.x / scale, sp.y / scale);
            return sp.x > -200f && sp.x < Screen.width + 200f && sp.y > -100f && sp.y < Screen.height + 100f;
        }

        private void HitNumber(Vector3 world, int amount, bool big, bool onBox, bool onHero = false)
        {
            var color = onHero ? new Color(1f, 0.35f, 0.3f, 1f) : big ? new Color(1f, 0.85f, 0.25f, 1f) : onBox ? new Color(1f, 0.93f, 0.7f, 1f) : Color.white;
            Floater(world, (onHero ? "-" : "") + amount.ToString(CultureInfo.InvariantCulture), color, big ? 50 : 36);
        }

        private void Floater(Vector3 world, string text, Color color, int size)
        {
            if (_worldUi == null || _floaters.Count > 60)
            {
                return;
            }

            var t = HookSkin.Outlined(SurvivalVisuals.Text(_worldUi, "Hit", text, size, TextAnchor.MiddleCenter, color));
            t.rectTransform.sizeDelta = new Vector2(220f, 70f);
            var jitter = new Vector3(Random.Range(-0.35f, 0.35f), 0f, 0f);
            _floaters.Add(new Floating { Rt = t.rectTransform, Text = t, World = world + jitter, Life = 0.85f, Color = color, Pop = 1f });
        }

        private void TickFloaters(float dt)
        {
            for (var i = _floaters.Count - 1; i >= 0; i--)
            {
                var f = _floaters[i];
                f.Age += dt;
                if (f.Rt == null || f.Age >= f.Life)
                {
                    if (f.Rt != null)
                    {
                        Destroy(f.Rt.gameObject);
                    }

                    _floaters.RemoveAt(i);
                    continue;
                }

                var u = f.Age / f.Life;
                f.Rt.localScale = Vector3.one * (1f + (0.5f * Mathf.Max(0f, 1f - (f.Age / 0.12f))));
                var c = f.Color;
                c.a = 1f - (u * u * u);
                f.Text.color = c;
            }
        }

        private void ClearFloaters()
        {
            foreach (var f in _floaters)
            {
                if (f.Rt != null)
                {
                    Destroy(f.Rt.gameObject);
                }
            }

            _floaters.Clear();
            foreach (var b in _bars.Values)
            {
                if (b.Root != null)
                {
                    Destroy(b.Root.gameObject);
                }
            }

            _bars.Clear();
            _lastSquad = -1;
        }

        // ---------- result ----------

        private void HideResult()
        {
            if (_result != null)
            {
                Destroy(_result.gameObject);
                _result = null;
            }
        }

        private void ShowResult(RunnerResult result, RewardBundle bundle, bool saved)
        {
            HideResult();
            if (_hud == null)
            {
                return;
            }

            var dim = SurvivalVisuals.Image(_hud, "RunnerResult", new Color(0.02f, 0.03f, 0.05f, 0.78f));
            dim.raycastTarget = true;
            SurvivalVisuals.Stretch(dim.rectTransform);
            _result = dim.rectTransform;
            if (result.Won)
            {
                var rays = HookSkin.Art(_result, "Rays", HookArtKeys.FxRays, new Color(1f, 0.85f, 0.4f, 0.3f));
                Anchor(rays.rectTransform, -0.2f, 0.42f, 1.2f, 1.02f);
                rays.color = new Color(1f, 0.92f, 0.6f, 0.75f);
                rays.gameObject.AddComponent<RunnerSpin>();
            }

            var panel = HookSkin.Panel(_result, "Panel", HookArtKeys.Panel, SurvivalVisuals.InspectFill, 1f);
            Anchor(panel.rectTransform, 0.07f, 0.22f, 0.93f, 0.76f);
            var ribbon = HookSkin.Panel(panel.transform, "Ribbon", result.Won ? HookArtKeys.RibbonBlue : HookArtKeys.RibbonRed, result.Won ? new Color(0.15f, 0.3f, 0.6f, 1f) : new Color(0.55f, 0.15f, 0.12f, 1f), 0.7f);
            Anchor(ribbon.rectTransform, -0.06f, 0.86f, 1.06f, 1.04f);
            var title = HookSkin.Outlined(SurvivalVisuals.Text(ribbon.transform, "Title", result.Won ? S("runner.victory", "VICTORY!") : S("runner.defeat", "DEFEAT"), 64, TextAnchor.MiddleCenter, result.Won ? new Color(1f, 0.9f, 0.45f, 1f) : Color.white));
            SurvivalVisuals.Stretch(title.rectTransform);

            var rows = new[]
            {
                (S("runner.result.kills", "Enemies defeated"), result.Kills.ToString(CultureInfo.InvariantCulture)),
                (S("runner.result.boxes", "Boxes broken"), result.BoxesBroken + " / " + (result.BoxesBroken + result.BoxesMissed + (_run?.Boxes.Count ?? 0))),
                (S("runner.result.squad", "Biggest squad"), "×" + (result.FollowersPeak + 1)),
                (S("runner.result.distance", "Distance"), Mathf.FloorToInt(result.Distance) + " m")
            };
            for (var i = 0; i < rows.Length; i++)
            {
                var y = 0.76f - (i * 0.075f);
                var k = SurvivalVisuals.Text(panel.transform, "Stat" + i, rows[i].Item1, 32, TextAnchor.MiddleLeft, HookSkin.ParchmentInk);
                Anchor(k.rectTransform, 0.1f, y - 0.035f, 0.7f, y + 0.035f);
                var v = SurvivalVisuals.Text(panel.transform, "Value" + i, rows[i].Item2, 34, TextAnchor.MiddleRight, HookSkin.ParchmentTitle);
                Anchor(v.rectTransform, 0.55f, y - 0.035f, 0.9f, y + 0.035f);
            }

            // Rewards: gold always (kills + boxes), win gold and hero shards on a win.
            var rewards = bundle.AsRows();
            var x = 0.5f - ((rewards.Count - 1) * 0.16f);
            foreach (var pair in rewards)
            {
                var slot = HookSkin.Panel(panel.transform, "Reward", HookArtKeys.Plate, SurvivalVisuals.RowFill, 0.5f);
                Anchor(slot.rectTransform, x - 0.14f, 0.27f, x + 0.14f, 0.45f);
                var icon = HookSkin.ResIcon(slot.transform, "Icon", pair.Key, SurvivalVisuals.Gold, pair.Key == SurvIds.ResSoft ? "G" : "S");
                Anchor(icon.rectTransform, 0.2f, 0.38f, 0.8f, 0.98f);
                var amount = HookSkin.Outlined(SurvivalVisuals.Text(slot.transform, "Amount", "+" + pair.Value.ToString("N0", CultureInfo.InvariantCulture), 32, TextAnchor.MiddleCenter, Color.white));
                Anchor(amount.rectTransform, 0f, 0.02f, 1f, 0.4f);
                x += 0.32f;
            }

            if (!RunnerHandoff.FromPlay)
            {
                var note = SurvivalVisuals.Text(panel.transform, "Saved", saved ? S("runner.result.saved", "Rewards saved") : "Open from the village to keep rewards", 24, TextAnchor.MiddleCenter, HookSkin.ParchmentInk);
                Anchor(note.rectTransform, 0.05f, 0.2f, 0.95f, 0.26f);
            }

            // Levels (the list) / Retry / Next level (a win opens it) / Continue (back to the village, from Play).
            var buttons = new List<(string Name, string Label, string Key, UnityEngine.Events.UnityAction Act)>
            {
                ("Levels", S("runner.levels", "Levels"), HookArtKeys.ButtonBlue, ShowLevelList),
                ("Retry", S("runner.retry", "Retry"), HookArtKeys.ButtonBlue, Retry)
            };
            var next = OpenNext;
            if (result.Won && next != null)
            {
                var n = next.Number;
                buttons.Add(("Next", S("runner.next", "Next level"), HookArtKeys.ButtonGreen, () => PlayLevel(n)));
            }

            if (RunnerHandoff.FromPlay)
            {
                buttons.Add(("Continue", S("runner.continue", "Continue"), HookArtKeys.ButtonGreen, Leave));
            }

            var w = 0.88f / buttons.Count;
            for (var i = 0; i < buttons.Count; i++)
            {
                var b = PanelButton(panel.transform, buttons[i].Name, buttons[i].Label, buttons[i].Key, 0.06f + (i * w) + 0.01f, 0.06f + ((i + 1) * w) - 0.01f);
                b.onClick.AddListener(buttons[i].Act);
            }

            if (result.Won && next != null && next.Number > result.Number)
            {
                var unlocked = HookSkin.Outlined(SurvivalVisuals.Text(panel.transform, "Unlocked", Fmt("runner.unlocked", "Level {0} unlocked: {1}", next.Number, LevelName(next)), 26, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.45f, 1f)));
                Anchor(unlocked.rectTransform, 0.05f, 0.175f, 0.95f, 0.215f);
            }

            _result.localScale = Vector3.one;
        }

        private string LevelName(RunnerLevelDef level) => S(level.Title, level.DisplayName.Length > 0 ? level.DisplayName : "Runner");

        /// <summary>"3 · Mirefen Causeway".</summary>
        private string LevelTitle(RunnerLevelDef? level) =>
            level == null ? S("runner.title", "Runner") : level.Number.ToString(CultureInfo.InvariantCulture) + " · " + LevelName(level);

        private void HideLevelList()
        {
            if (_levelList != null)
            {
                Destroy(_levelList.gameObject);
                _levelList = null;
            }
        }

        /// <summary>A simple level list over the result screen: open levels play, locked ones show a lock.</summary>
        private void ShowLevelList()
        {
            HideLevelList();
            if (_hud == null || _catalog == null)
            {
                return;
            }

            var dim = SurvivalVisuals.Image(_hud, "RunnerLevels", new Color(0.02f, 0.03f, 0.05f, 0.86f));
            dim.raycastTarget = true;
            SurvivalVisuals.Stretch(dim.rectTransform);
            _levelList = dim.rectTransform;
            var panel = HookSkin.Panel(_levelList, "Panel", HookArtKeys.Panel, SurvivalVisuals.InspectFill, 1f);
            Anchor(panel.rectTransform, 0.05f, 0.14f, 0.95f, 0.86f);
            var ribbon = HookSkin.Panel(panel.transform, "Ribbon", HookArtKeys.RibbonBlue, new Color(0.15f, 0.3f, 0.6f, 1f), 0.7f);
            Anchor(ribbon.rectTransform, -0.04f, 0.9f, 1.04f, 1.03f);
            var title = HookSkin.Outlined(SurvivalVisuals.Text(ribbon.transform, "Title", S("runner.levels", "Levels"), 52, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.45f, 1f)));
            SurvivalVisuals.Stretch(title.rectTransform);

            const int cols = 3;
            var levels = _catalog.Levels;
            var rows = (levels.Count + cols - 1) / cols;
            var cellH = 0.74f / Mathf.Max(1, rows);
            for (var i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                var open = RunnerProgress.IsOpen(_unlocked, level.Number);
                var current = _level != null && level.Number == _level.Number;
                var c = i % cols;
                var r = i / cols;
                var x0 = 0.05f + (c * 0.3f);
                var y1 = 0.87f - (r * cellH);
                var key = open ? (current ? HookArtKeys.ButtonGreen : HookArtKeys.ButtonBlue) : HookArtKeys.ButtonRed;
                var btn = HookSkin.Button(panel.transform, "Level" + level.Number, key, open ? SurvivalVisuals.PinBuild : new Color(0.3f, 0.3f, 0.3f, 1f));
                Anchor(btn.GetComponent<RectTransform>(), x0 + 0.01f, y1 - cellH + 0.012f, x0 + 0.29f, y1 - 0.012f);
                var label = open
                    ? level.Number.ToString(CultureInfo.InvariantCulture) + "\n" + LevelName(level)
                    : level.Number.ToString(CultureInfo.InvariantCulture) + "\n" + S("runner.locked", "Locked");
                var t = HookSkin.Outlined(SurvivalVisuals.Text(btn.transform, "Label", label, 24, TextAnchor.MiddleCenter, open ? HookSkin.InkFor(key) : new Color(0.75f, 0.75f, 0.75f, 1f)));
                SurvivalVisuals.Stretch(t.rectTransform);
                btn.interactable = open;
                if (open)
                {
                    var n = level.Number;
                    btn.onClick.AddListener(() => PlayLevel(n));
                    btn.gameObject.AddComponent<PressSquash>();
                }
            }

            var close = HookSkin.Button(panel.transform, "Close", HookArtKeys.ButtonRed, new Color(0.65f, 0.18f, 0.15f, 1f));
            Anchor(close.GetComponent<RectTransform>(), 0.3f, 0.02f, 0.7f, 0.1f);
            var closeText = HookSkin.Outlined(SurvivalVisuals.Text(close.transform, "Label", S("runner.close", "Close"), 32, TextAnchor.MiddleCenter, Color.white));
            SurvivalVisuals.Stretch(closeText.rectTransform);
            close.onClick.AddListener(HideLevelList);
        }

        private static Button PanelButton(Transform parent, string name, string label, string key, float x0, float x1)
        {
            var btn = HookSkin.Button(parent, name, key, key == HookArtKeys.ButtonGreen ? new Color(0.2f, 0.6f, 0.25f, 1f) : SurvivalVisuals.PinBuild);
            Anchor(btn.GetComponent<RectTransform>(), x0, 0.05f, x1, 0.17f);
            var t = HookSkin.Outlined(SurvivalVisuals.Text(btn.transform, "Label", label, 40, TextAnchor.MiddleCenter, HookSkin.InkFor(key)));
            SurvivalVisuals.Stretch(t.rectTransform);
            btn.gameObject.AddComponent<PressSquash>();
            return btn;
        }
    }

    /// <summary>Slow spin for the victory rays.</summary>
    public sealed class RunnerSpin : MonoBehaviour
    {
        private void Update() => transform.Rotate(0f, 0f, -18f * Time.deltaTime);
    }
}
