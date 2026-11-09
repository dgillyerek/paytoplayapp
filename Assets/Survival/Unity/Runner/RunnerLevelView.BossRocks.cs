#nullable enable
using Survival.Domain.Runner;
using Survival.Domain.Theme;
using Survival.Domain.Enemies;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Ironhowl's boulder opener, shown. Review (Derek 2026-11-03): a much more detailed rock, really picked up in his hands
    /// (his hands were rising before the stone, so it floated), no landing ring. On <see cref="RunnerEventKind.BossRockThrown"/>
    /// he plays his ground pound: when his fists reach the road (<see cref="RunnerRules.BossRockGrab"/>) the boulder rips
    /// out of the ground with dust and pebbles and from then sits between his real hand bones (followed in LateUpdate,
    /// after the animation). At <see cref="RunnerRules.BossRockWindupAt"/> he goes into his overhead smash with it, and on
    /// <see cref="RunnerEventKind.BossRockReleased"/> (the smash's throw frame) it leaves his hands and arcs to the landing.
    /// <see cref="RunnerEventKind.BossRockLanded"/> lands it with dust, sparks and a small shake. One pooled boulder.
    /// </summary>
    public sealed partial class RunnerLevelView
    {
        private const int RockPebbles = 6;
        private const float RockSize = 0.55f;

        private Transform? _rockStone;
        private readonly Transform?[] _pebbles = new Transform?[RockPebbles];
        private readonly Vector3[] _pebbleVel = new Vector3[RockPebbles];
        private RunnerRig? _rockRig;
        private Vector3 _rockFrom;
        private Vector3 _rockTo;
        private Vector3 _rockSpin;
        private float _rockStart = -1f;
        private float _rockFlightStart = -1f;
        private float _rockFlight;
        private float _rockScale = 1f;
        private bool _rockGrabbed;
        private bool _rockWound;

        private void BuildRockMark()
        {
            if (_rockStone != null)
            {
                return;
            }

            var parent = _spare != null ? _spare : _actors;
            if (parent == null)
            {
                return;
            }

            var mat = BossRockMesh.Material(LitMat(new Color(0.5f, 0.47f, 0.43f, 1f)));
            _rockStone = RockPiece(parent, "BossRock", BossRockMesh.Build(7, RockSize), mat);
            for (var i = 0; i < RockPebbles; i++)
            {
                _pebbles[i] = RockPiece(parent, "BossRockPebble" + i, BossRockMesh.Build(100 + i, 0.09f + (0.03f * (i % 3)), false), mat);
            }
        }

        private static Transform RockPiece(Transform parent, string name, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.SetActive(false);
            return go.transform;
        }

        /// <summary>He goes for a boulder: his ground pound bends him to the road; the rock appears when his fists get there.</summary>
        private void BossRockThrown(RunnerEvent e)
        {
            BuildRockMark();
            if (_rockStone == null)
            {
                return;
            }

            EndRock();
            _rockRig = _foes.TryGetValue(e.Id, out var rig) ? rig : null;
            _rockScale = _run?.Foe(e.Id)?.Unit.Scale ?? 1f;
            _rockStart = Time.time;
            _rockGrabbed = false;
            _rockWound = false;
            if (_rockRig == null)
            {
                return;
            }

            if (_rockRig.RotatesAttacks)
            {
                _rockRig.Swing(IronhowlMotion.Attack4PoseName, IronhowlMotion.Attack4LastFrame / IronhowlMotion.FrameRate);
            }
            else
            {
                _rockRig.Attack();
            }
        }

        /// <summary>The throw frame: the boulder leaves his hands for the landing spot the sim just locked.</summary>
        private void BossRockReleased(RunnerEvent e)
        {
            if (_rockStone == null || !_rockStone.gameObject.activeSelf)
            {
                return;
            }

            _rockFrom = _rockStone.position;
            _rockTo = new Vector3(e.X, RockSize * _rockScale * 0.6f, e.Z);
            _rockFlight = Mathf.Max(0.1f, e.Amount);
            _rockFlightStart = Time.time;
            _rockSpin = new Vector3(260f, 70f, 30f);
        }

        /// <summary>It lands: drop the boulder, then dust, sparks and a small shake.</summary>
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
            _rockFlightStart = -1f;
            _rockRig = null;
            if (_rockStone != null)
            {
                _rockStone.gameObject.SetActive(false);
            }
        }

        /// <summary>Between his palms, a little in front so the stone sits in them, not through them.</summary>
        private Vector3 RockHands(RunnerRig rig)
        {
            var hands = rig.HandPoint();
            var fwd = rig.Slot.forward;
            return hands is { } h ? h + (fwd * (RockSize * _rockScale * 0.35f)) : rig.Slot.position + (Vector3.up * 1.2f * _rockScale) + (fwd * 0.6f);
        }

        /// <summary>Update: the pickup and wind-up timeline, the flight arc, and the pebbles falling.</summary>
        private void AnimateRockMark()
        {
            TickPebbles();
            if (_rockStart < 0f || _rockStone == null)
            {
                return;
            }

            var t = Time.time - _rockStart;
            if (t > RunnerRules.BossRockRelease + RunnerRules.BossRockFlight + 0.6f || (_rockRig != null && _rockRig.Dead && _rockFlightStart < 0f))
            {
                // He died mid-pickup, or the run ended mid-throw.
                EndRock();
                return;
            }

            if (_rockFlightStart >= 0f)
            {
                var f = Mathf.Clamp01((Time.time - _rockFlightStart) / _rockFlight);
                var pos = Vector3.Lerp(_rockFrom, _rockTo, f);
                pos.y += 4f * f * (1f - f) * 1.2f;
                _rockStone.position = pos;
                _rockStone.Rotate(_rockSpin * Time.deltaTime, Space.Self);
                return;
            }

            if (_rockRig == null)
            {
                return;
            }

            if (!_rockGrabbed && t >= RunnerRules.BossRockGrab)
            {
                // His fists hit the road: the boulder tears out of it, in his hands from this frame.
                _rockGrabbed = true;
                _rockStone.localScale = Vector3.one * _rockScale;
                _rockStone.rotation = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
                _rockStone.position = RockHands(_rockRig);
                _rockStone.gameObject.SetActive(true);
                var ground = new Vector3(_rockStone.position.x, 0.15f, _rockStone.position.z);
                AddFx(HookArtKeys.FxBurstRing, ground, 1.6f * _rockScale, 0.5f, new Color(0.62f, 0.55f, 0.45f, 0.8f), Vector3.zero, 1.4f, 0f);
                SpawnPebbles(ground + (Vector3.up * 0.4f * _rockScale));
            }

            if (!_rockWound && t >= RunnerRules.BossRockWindupAt)
            {
                // Rock in hand, up into the overhead smash; its throw frame is the release.
                _rockWound = true;
                if (_rockRig.RotatesAttacks)
                {
                    _rockRig.Swing(IronhowlMotion.Attack2PoseName, IronhowlMotion.Attack2LastFrame / IronhowlMotion.FrameRate);
                }
            }
        }

        /// <summary>LateUpdate, after his bones have animated: a held boulder sits between his hands.</summary>
        private void FollowRockHands()
        {
            if (_rockStone == null || _rockRig == null || !_rockGrabbed || _rockFlightStart >= 0f || !_rockStone.gameObject.activeSelf)
            {
                return;
            }

            _rockStone.position = RockHands(_rockRig);
        }

        private void SpawnPebbles(Vector3 at)
        {
            for (var i = 0; i < RockPebbles; i++)
            {
                var p = _pebbles[i];
                if (p == null)
                {
                    continue;
                }

                var dir = Quaternion.Euler(0f, (360f / RockPebbles * i) + Random.Range(-20f, 20f), 0f) * Vector3.forward;
                p.position = at + (dir * 0.3f);
                p.localScale = Vector3.one * _rockScale;
                p.rotation = Random.rotation;
                _pebbleVel[i] = (dir * Random.Range(1.2f, 2.4f)) + (Vector3.up * Random.Range(2.5f, 4f));
                p.gameObject.SetActive(true);
            }
        }

        private void TickPebbles()
        {
            var dt = Time.deltaTime;
            for (var i = 0; i < RockPebbles; i++)
            {
                var p = _pebbles[i];
                if (p == null || !p.gameObject.activeSelf)
                {
                    continue;
                }

                _pebbleVel[i] += Vector3.down * 14f * dt;
                p.position += _pebbleVel[i] * dt;
                p.Rotate(400f * dt, 0f, 200f * dt, Space.Self);
                if (p.position.y < -0.2f)
                {
                    p.gameObject.SetActive(false);
                }
            }
        }
    }
}
