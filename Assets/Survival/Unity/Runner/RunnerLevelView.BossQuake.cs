#nullable enable
using Survival.Domain.Runner;
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// The end boss's big attack, the quake (Update 8). He rears up for <see cref="RunnerRules.BossQuakeWindup"/> seconds
    /// (<see cref="RunnerEventKind.BossQuakeWindup"/>): the whole road in front of him goes red, a darker fill sweeps out
    /// from his feet over the warning, a green band shows the safe lane (on the side away from the lead, reachable from
    /// anywhere on the road in well under the warning), and the HUD shouts DODGE! with an arrow to it, a red pulse round
    /// the screen edges. <see cref="RunnerEventKind.BossQuake"/> lands it: a flash, shake, dust ring and the swing.
    /// All of it is built once under the loading screen (five quads on three cached materials, five HUD images and two
    /// texts, strings cached), so a quake allocates nothing. (There is no audio layer in the runner yet, so the cue is
    /// visual only.)
    /// </summary>
    public sealed partial class RunnerLevelView
    {
        private static readonly Color QuakeZone = new Color(1f, 0.1f, 0.06f, 0.34f);
        private static readonly Color QuakeCharge = new Color(0.9f, 0.08f, 0.05f, 0.62f);
        private static readonly Color QuakeSafe = new Color(0.25f, 1f, 0.45f, 0.38f);
        private static readonly Color QuakeEdge = new Color(1f, 0.08f, 0.05f, 0f);

        private Transform? _quakeRoot;
        private Transform? _quakeZoneA;
        private Transform? _quakeZoneB;
        private Transform? _quakeChargeA;
        private Transform? _quakeChargeB;
        private Transform? _quakeSafe;
        private float _quakeStart = -99f;
        private float _quakeLength = 1f;
        private float _quakeMinZ;
        private float _quakeDepth;
        private float _quakeAMinX;
        private float _quakeAMaxX;
        private float _quakeBMinX;
        private float _quakeBMaxX;
        private bool _quakeLive;
        private bool _quakeRight;
        private RunnerRig? _quakeRig;

        private RectTransform? _quakeHud;
        private readonly Image?[] _quakeEdges = new Image?[4];
        private Image? _quakeFlash;
        private Text? _dodgeText;
        private Text? _dodgeArrow;
        private string _dodgeLeft = "";
        private string _dodgeRight = "";
        private float _quakeFlashAge = 9f;

        /// <summary>Builds the marker quads (hidden). Safe to call again.</summary>
        private void BuildQuakeMark()
        {
            if (_quakeRoot != null)
            {
                return;
            }

            var parent = _spare != null ? _spare : _actors;
            if (parent == null)
            {
                return;
            }

            _quakeRoot = new GameObject("BossQuakeMark").transform;
            _quakeRoot.SetParent(parent, false);
            var flat = Quaternion.Euler(90f, 0f, 0f);
            _quakeZoneA = Part(_quakeRoot, PrimitiveType.Quad, "ZoneA", Vector3.zero, Vector3.one, Unlit(QuakeZone), flat).transform;
            _quakeZoneB = Part(_quakeRoot, PrimitiveType.Quad, "ZoneB", Vector3.zero, Vector3.one, Unlit(QuakeZone), flat).transform;
            _quakeChargeA = Part(_quakeRoot, PrimitiveType.Quad, "ChargeA", Vector3.zero, Vector3.one, Unlit(QuakeCharge), flat).transform;
            _quakeChargeB = Part(_quakeRoot, PrimitiveType.Quad, "ChargeB", Vector3.zero, Vector3.one, Unlit(QuakeCharge), flat).transform;
            _quakeSafe = Part(_quakeRoot, PrimitiveType.Quad, "Safe", Vector3.zero, Vector3.one, Unlit(QuakeSafe), flat).transform;
            _quakeRoot.gameObject.SetActive(false);
            _dodgeLeft = "⟵  " + S("runner.quake_left", "GET LEFT");
            _dodgeRight = S("runner.quake_right", "GET RIGHT") + "  ⟶";
        }

        /// <summary>The warning's HUD: four red edge strips, a white flash, DODGE! and an arrow to the safe lane (hidden).</summary>
        private void BuildQuakeHud()
        {
            if (_hud == null)
            {
                return;
            }

            _quakeHud = new GameObject("QuakeWarning", typeof(RectTransform)).GetComponent<RectTransform>();
            _quakeHud.SetParent(_hud, false);
            SurvivalVisuals.Stretch(_quakeHud);
            // x0,y0,x1,y1 of the strips: bottom, top, left, right.
            var spans = new[] { new Vector4(0f, 0f, 1f, 0.07f), new Vector4(0f, 0.93f, 1f, 1f), new Vector4(0f, 0f, 0.05f, 1f), new Vector4(0.95f, 0f, 1f, 1f) };
            for (var i = 0; i < 4; i++)
            {
                var img = SurvivalVisuals.Image(_quakeHud, "Edge" + i, QuakeEdge);
                img.raycastTarget = false;
                Anchor(img.rectTransform, spans[i].x, spans[i].y, spans[i].z, spans[i].w);
                _quakeEdges[i] = img;
            }

            _quakeFlash = SurvivalVisuals.Image(_quakeHud, "Flash", new Color(1f, 1f, 1f, 0f));
            _quakeFlash.raycastTarget = false;
            SurvivalVisuals.Stretch(_quakeFlash.rectTransform);
            _dodgeText = HookSkin.Outlined(SurvivalVisuals.Text(_quakeHud, "Dodge", S("runner.quake_dodge", "DODGE!"), 96, TextAnchor.MiddleCenter, new Color(1f, 0.25f, 0.18f, 1f)));
            _dodgeText.raycastTarget = false;
            Anchor(_dodgeText.rectTransform, 0f, 0.56f, 1f, 0.68f);
            _dodgeArrow = HookSkin.Outlined(SurvivalVisuals.Text(_quakeHud, "DodgeArrow", "", 54, TextAnchor.MiddleCenter, new Color(0.55f, 1f, 0.6f, 1f)));
            _dodgeArrow.raycastTarget = false;
            Anchor(_dodgeArrow.rectTransform, 0f, 0.48f, 1f, 0.57f);
            _quakeHud.gameObject.SetActive(false);
            _quakeFlashAge = 9f;
        }

        /// <summary>He rears up: mark the road, show the safe lane, shout DODGE!.</summary>
        private void BossQuakeWindup(RunnerEvent e)
        {
            var boss = _run?.Foe(e.Id);
            if (boss == null || _run == null)
            {
                return;
            }

            BuildQuakeMark();
            if (_quakeRoot == null)
            {
                return;
            }

            var area = RunnerRun.QuakeArea(boss);
            var safe = RunnerRun.QuakeSafe(boss);
            var half = _run.Level.HalfWidth;
            _quakeDepth = area.MaxZ - area.MinZ;
            _quakeMinZ = area.MinZ;
            _quakeLength = Mathf.Max(0.05f, e.Amount);
            _quakeStart = Time.time;
            _quakeLive = true;
            _quakeRight = boss.QuakeSafeX > 0f;

            // The two red strips either side of the safe band, clipped to the road.
            _quakeAMinX = -half;
            _quakeAMaxX = Mathf.Min(half, safe.MinX);
            _quakeBMinX = Mathf.Max(-half, safe.MaxX);
            _quakeBMaxX = half;
            var midZ = (area.MinZ + area.MaxZ) * 0.5f;
            _quakeRoot.position = new Vector3(0f, 0.035f, midZ);
            Strip(_quakeZoneA, _quakeAMinX, _quakeAMaxX, _quakeDepth, 0f, 0f);
            Strip(_quakeZoneB, _quakeBMinX, _quakeBMaxX, _quakeDepth, 0f, 0f);
            var safeMin = Mathf.Max(-half, safe.MinX);
            var safeMax = Mathf.Min(half, safe.MaxX);
            Strip(_quakeSafe, safeMin, safeMax, _quakeDepth, 0.012f, 0f);
            _quakeRoot.gameObject.SetActive(true);

            if (_dodgeArrow != null)
            {
                _dodgeArrow.text = _quakeRight ? _dodgeRight : _dodgeLeft;
            }

            if (_quakeHud != null)
            {
                _quakeHud.gameObject.SetActive(true);
            }

            _quakeRig = _foes.TryGetValue(boss.Id, out var rig) ? rig : null;
            Banner(S("runner.quake_banner", "Ironhowl is winding up a quake!"), new Color(1f, 0.3f, 0.2f, 1f));
            Shake(0.25f);
            AnimateQuakeMark();
        }

        /// <summary>It lands: flash, shake, dust, the swing, and whatever it caught flies.</summary>
        private void BossQuaked(RunnerEvent e)
        {
            _quakeLive = false;
            if (_quakeRoot != null)
            {
                _quakeRoot.gameObject.SetActive(false);
            }

            if (_quakeRig != null)
            {
                _quakeRig.Slot.localScale = Vector3.one;
                _quakeRig.Attack();
                _quakeRig = null;
            }

            _quakeFlashAge = 0f;
            Shake(1.2f);
            var at = new Vector3(0f, 0.2f, e.Z);
            for (var i = -1; i <= 1; i++)
            {
                var p = at + new Vector3(i * 2.6f, 0f, 0f);
                AddFx(HookArtKeys.FxBurstRing, p, 4.2f, 0.6f, new Color(1f, 0.4f, 0.2f, 0.9f), Vector3.zero, 2.4f, 0f);
                Sparks(p, new Color(1f, 0.55f, 0.25f, 1f), 10, 2.2f);
            }

            AddFx(HookArtKeys.FxGlow, at + (Vector3.up * 0.4f), 6f, 0.45f, new Color(1f, 0.8f, 0.5f, 0.8f), Vector3.zero, 1f, 0f);
            Floater(at + (Vector3.up * 2.8f), S("runner.boss_quake", "QUAKE!"), new Color(1f, 0.35f, 0.25f, 1f), 64);
        }

        /// <summary>Frame update: the fill sweep, the rear-up, the HUD pulse and the landing flash. No allocation.</summary>
        private void AnimateQuakeMark()
        {
            var dt = Time.deltaTime;
            if (_quakeLive && _quakeRoot != null)
            {
                var t = Time.time - _quakeStart;
                var p = Mathf.Clamp01(t / _quakeLength);
                // The fill grows from his feet (the +z end of the strips) out to the far end.
                var fill = Mathf.Max(0.001f, _quakeDepth * p);
                var shift = _quakeDepth * 0.5f * (1f - p);
                Strip(_quakeChargeA, _quakeAMinX, _quakeAMaxX, fill, 0.006f, shift);
                Strip(_quakeChargeB, _quakeBMinX, _quakeBMaxX, fill, 0.006f, shift);

                if (_quakeRig != null)
                {
                    // Rears up and shudders, a bigger and bigger stretch as the fill nears the end.
                    var up = 1f + (0.28f * p * p);
                    var wobble = 1f + (0.05f * p * Mathf.Sin(t * 38f));
                    _quakeRig.Slot.localScale = new Vector3(wobble, up, wobble);
                }

                if (t > _quakeLength + 0.6f)
                {
                    // He died or the run ended mid-warning: tidy up.
                    EndQuake();
                }
                else if (_quakeHud != null)
                {
                    // Pulse faster as it nears: 3 Hz at first, 9 Hz at the end.
                    var pulse = 0.5f + (0.5f * Mathf.Sin(t * Mathf.Lerp(18f, 56f, p)));
                    var edge = new Color(1f, 0.08f, 0.05f, Mathf.Lerp(0.25f, 0.6f, pulse) * Mathf.Lerp(0.7f, 1f, p));
                    for (var i = 0; i < _quakeEdges.Length; i++)
                    {
                        var img = _quakeEdges[i];
                        if (img != null)
                        {
                            img.color = edge;
                        }
                    }

                    if (_dodgeText != null)
                    {
                        _dodgeText.rectTransform.localScale = Vector3.one * (1f + (0.12f * pulse));
                    }
                }
            }
            else if (_quakeHud != null && _quakeHud.gameObject.activeSelf && _quakeFlashAge >= 0.5f)
            {
                _quakeHud.gameObject.SetActive(false);
            }

            if (_quakeFlashAge < 0.5f)
            {
                _quakeFlashAge += dt;
                if (_quakeFlash != null)
                {
                    var a = Mathf.Clamp01(1f - (_quakeFlashAge / 0.45f)) * 0.55f;
                    _quakeFlash.color = new Color(1f, 0.95f, 0.85f, a);
                }

                if (_quakeHud != null && !_quakeLive)
                {
                    for (var i = 0; i < _quakeEdges.Length; i++)
                    {
                        var img = _quakeEdges[i];
                        if (img != null)
                        {
                            img.color = QuakeEdge;
                        }
                    }

                    if (_dodgeText != null)
                    {
                        _dodgeText.rectTransform.localScale = Vector3.zero;
                    }

                    if (_dodgeArrow != null)
                    {
                        _dodgeArrow.text = "";
                    }
                }
            }
        }

        private void EndQuake()
        {
            _quakeLive = false;
            if (_quakeRoot != null)
            {
                _quakeRoot.gameObject.SetActive(false);
            }

            if (_quakeRig != null)
            {
                _quakeRig.Slot.localScale = Vector3.one;
                _quakeRig = null;
            }

            if (_quakeHud != null)
            {
                _quakeHud.gameObject.SetActive(false);
            }
        }

        /// <summary>Lays a flat quad over x in [<paramref name="x0"/>, <paramref name="x1"/>] and <paramref name="length"/> deep (hidden when empty).</summary>
        private static void Strip(Transform? quad, float x0, float x1, float length, float lift, float shiftZ)
        {
            if (quad == null)
            {
                return;
            }

            var w = x1 - x0;
            if (w <= 0.01f)
            {
                quad.localScale = Vector3.zero;
                return;
            }

            quad.localScale = new Vector3(w, length, 1f);
            quad.localPosition = new Vector3((x0 + x1) * 0.5f, lift, shiftZ);
        }
    }
}
