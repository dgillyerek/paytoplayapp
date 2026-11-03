#nullable enable
using Survival.Domain.Runner;
using Survival.Domain.Theme;
using Survival.Domain.Enemies;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Ironhowl's boulder opener, shown (Derek 2026-11-03). On <see cref="RunnerEventKind.BossRockThrown"/> he plays his ground
    /// pound (fists clasp high over his head = the lift, then the throw), a stone sits over his fists until
    /// <see cref="RunnerRules.BossRockRelease"/> and then arcs to the locked landing spot, where a red-violet ring with a
    /// growing fill marks the road it will hit. <see cref="RunnerEventKind.BossRockLanded"/> lands it with his slam's sparks
    /// and dust. One pooled stone and one pooled ring (built with the slam mark under the loading screen).
    /// </summary>
    public sealed partial class RunnerLevelView
    {
        private static readonly Color RockZone = new Color(0.85f, 0.15f, 0.45f, 0.32f);
        private static readonly Color RockCharge = new Color(1f, 0.25f, 0.35f, 0.55f);
        private static readonly Color RockStone = new Color(0.42f, 0.39f, 0.36f, 1f);

        private Transform? _rockRoot;
        private Transform? _rockZone;
        private Transform? _rockCharge;
        private Transform? _rockStone;
        private RunnerRig? _rockRig;
        private Vector3 _rockTo;
        private float _rockStart = -1f;
        private float _rockLength;
        private float _rockHand;

        private void BuildRockMark()
        {
            if (_rockRoot != null)
            {
                return;
            }

            var parent = _spare != null ? _spare : _actors;
            if (parent == null)
            {
                return;
            }

            var d = RunnerRules.BossRockRadius * 2f;
            _rockRoot = new GameObject("BossRockMark").transform;
            _rockRoot.SetParent(parent, false);
            _rockZone = Part(_rockRoot, PrimitiveType.Cylinder, "Zone", Vector3.zero, new Vector3(d, 0.002f, d), Unlit(RockZone), Quaternion.identity).transform;
            _rockCharge = Part(_rockRoot, PrimitiveType.Cylinder, "Charge", new Vector3(0f, 0.004f, 0f), new Vector3(0.01f, 0.002f, 0.01f), Unlit(RockCharge), Quaternion.identity).transform;
            // A lumpy stone: a squashed, tilted sphere (no new art).
            _rockStone = Part(parent, PrimitiveType.Sphere, "BossRock", Vector3.zero, new Vector3(1.1f, 0.8f, 0.95f), Unlit(RockStone), Quaternion.Euler(18f, 35f, 12f)).transform;
            _rockStone.gameObject.SetActive(false);
            _rockRoot.gameObject.SetActive(false);
        }

        /// <summary>He lifts a boulder: ring the landing spot and start his lift-and-throw take.</summary>
        private void BossRockThrown(RunnerEvent e)
        {
            BuildRockMark();
            if (_rockRoot == null)
            {
                return;
            }

            var boss = _run?.Foe(e.Id);
            _rockRig = _foes.TryGetValue(e.Id, out var rig) ? rig : null;
            _rockHand = 2.2f * (boss?.Unit.Scale ?? 1f);
            _rockTo = new Vector3(e.X, 0f, e.Z);
            _rockStart = Time.time;
            _rockLength = Mathf.Max(0.1f, e.Amount);
            _rockRoot.position = new Vector3(e.X, 0.03f, e.Z);
            _rockRoot.gameObject.SetActive(true);
            if (_rockStone != null)
            {
                _rockStone.gameObject.SetActive(true);
            }

            if (_rockRig != null)
            {
                if (_rockRig.RotatesAttacks)
                {
                    _rockRig.Swing(IronhowlMotion.Attack4PoseName, IronhowlMotion.Attack4LastFrame / IronhowlMotion.FrameRate);
                }
                else
                {
                    _rockRig.Attack();
                }
            }

            AnimateRockMark();
        }

        /// <summary>It lands: drop the ring and stone, then his slam's dust, sparks and a small shake.</summary>
        private void BossRockLanded(RunnerEvent e)
        {
            EndRock();
            var at = new Vector3(e.X, 0.2f, e.Z);
            AddFx(HookArtKeys.FxBurstRing, at, RunnerRules.BossRockRadius * 2.2f, 0.45f, new Color(0.8f, 0.7f, 0.6f, 0.9f), Vector3.zero, 1.8f, 0f);
            Sparks(at, new Color(1f, 0.6f, 0.3f, 1f), 10, 1.6f);
            Shake(0.5f);
        }

        private void EndRock()
        {
            _rockStart = -1f;
            _rockRig = null;
            if (_rockRoot != null)
            {
                _rockRoot.gameObject.SetActive(false);
            }

            if (_rockStone != null)
            {
                _rockStone.gameObject.SetActive(false);
            }
        }

        /// <summary>Frame update: the ring's fill grows to the impact; the stone rides his fists, then arcs down onto the ring.</summary>
        private void AnimateRockMark()
        {
            if (_rockStart < 0f || _rockRoot == null)
            {
                return;
            }

            var t = Time.time - _rockStart;
            if (t > _rockLength + 0.5f || (_rockRig != null && _rockRig.Dead))
            {
                // He died or the run ended mid-throw.
                EndRock();
                return;
            }

            var p = Mathf.Clamp01(t / _rockLength);
            var d = RunnerRules.BossRockRadius * 2f * Mathf.Max(0.02f, p);
            if (_rockCharge != null)
            {
                _rockCharge.localScale = new Vector3(d, 0.002f, d);
            }

            if (_rockStone == null || _rockRig == null)
            {
                return;
            }

            var hand = _rockRig.Slot.position + (Vector3.up * _rockHand) + (_rockRig.Slot.forward * 0.4f);
            var release = Mathf.Min(RunnerRules.BossRockRelease, _rockLength * 0.8f);
            if (t < release)
            {
                // Torn up from the ground beside him and raised overhead with the clasped fists.
                var lift = Mathf.SmoothStep(0f, 1f, t / release);
                _rockStone.position = Vector3.Lerp(_rockRig.Slot.position + (_rockRig.Slot.forward * 0.8f), hand, lift);
                return;
            }

            var f = Mathf.Clamp01((t - release) / Mathf.Max(0.05f, _rockLength - release));
            var pos = Vector3.Lerp(hand, _rockTo + (Vector3.up * 0.5f), f);
            pos.y += 3f * f * (1f - f) * 4f * 0.5f;
            _rockStone.position = pos;
            _rockStone.Rotate(240f * Time.deltaTime, 90f * Time.deltaTime, 0f, Space.Self);
        }
    }
}
