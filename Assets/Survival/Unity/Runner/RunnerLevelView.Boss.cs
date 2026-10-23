#nullable enable
using Survival.Domain.Runner;
using Survival.Domain.Theme;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// The end boss's slam, shown (Update 7). When the run emits <see cref="RunnerEventKind.BossWindup"/> he raises his arm
    /// (the rig's attack plays off the <see cref="RunnerEventKind.Fired"/> that comes with it) and the road he is about to
    /// hit lights up red in front of him, a darker fill growing from his feet outward over the windup so the squad can see
    /// how long it has; <see cref="RunnerEventKind.BossSlam"/> lands it. Two pooled quads (built once under the loading
    /// screen) on two cached materials: no allocation, no extra draw calls to speak of.
    /// </summary>
    public sealed partial class RunnerLevelView
    {
        private static readonly Color SlamZone = new Color(1f, 0.18f, 0.1f, 0.3f);
        private static readonly Color SlamCharge = new Color(1f, 0.28f, 0.1f, 0.5f);

        private Transform? _slamRoot;
        private Transform? _slamZone;
        private Transform? _slamCharge;
        private float _slamWidth;
        private float _slamStart;
        private float _slamLength;
        private float _slamDepth;

        /// <summary>Builds the two marker quads (hidden). Safe to call again.</summary>
        private void BuildSlamMark()
        {
            if (_slamRoot != null)
            {
                return;
            }

            var parent = _spare != null ? _spare : _actors;
            if (parent == null)
            {
                return;
            }

            _slamRoot = new GameObject("BossSlamMark").transform;
            _slamRoot.SetParent(parent, false);
            _slamZone = Part(_slamRoot, PrimitiveType.Quad, "Zone", Vector3.zero, Vector3.one, Unlit(SlamZone), Quaternion.Euler(90f, 0f, 0f)).transform;
            _slamCharge = Part(_slamRoot, PrimitiveType.Quad, "Charge", new Vector3(0f, 0.004f, 0f), Vector3.one, Unlit(SlamCharge), Quaternion.Euler(90f, 0f, 0f)).transform;
            _slamRoot.gameObject.SetActive(false);
        }

        /// <summary>He starts the slam: mark the road it will hit.</summary>
        private void BossWindup(RunnerEvent e)
        {
            var boss = _run?.Foe(e.Id);
            if (boss == null)
            {
                return;
            }

            BuildSlamMark();
            if (_slamRoot == null)
            {
                return;
            }

            var area = RunnerRun.SlamArea(boss);
            var width = area.MaxX - area.MinX;
            _slamDepth = area.MaxZ - area.MinZ;
            _slamStart = Time.time;
            _slamLength = Mathf.Max(0.05f, e.Amount);
            _slamRoot.position = new Vector3((area.MinX + area.MaxX) * 0.5f, 0.03f, (area.MinZ + area.MaxZ) * 0.5f);
            _slamWidth = width;
            if (_slamZone != null)
            {
                _slamZone.localScale = new Vector3(width, _slamDepth, 1f);
            }

            _slamRoot.gameObject.SetActive(true);
            AnimateSlamMark();
        }

        /// <summary>The slam lands: drop the marker, shake, dust and the call-out.</summary>
        private void BossSlammed(RunnerEvent e)
        {
            if (_slamRoot != null)
            {
                _slamRoot.gameObject.SetActive(false);
            }

            var at = new Vector3(e.X, 0.2f, e.Z);
            AddFx(HookArtKeys.FxBurstRing, at, 3.4f, 0.5f, new Color(1f, 0.5f, 0.25f, 0.9f), Vector3.zero, 2.2f, 0f);
            AddFx(HookArtKeys.FxGlow, at + (Vector3.up * 0.3f), 3f, 0.4f, new Color(1f, 0.8f, 0.5f, 0.8f), Vector3.zero, 1f, 0f);
            Sparks(at, new Color(1f, 0.6f, 0.3f, 1f), 12, 2f);
            Shake(0.8f);
            Floater(at + (Vector3.up * 2.4f), S("runner.boss_slam", "SLAM!"), new Color(1f, 0.45f, 0.3f, 1f), 52);
        }

        /// <summary>The charge fill grows from his feet to the far end of the marked road over the windup.</summary>
        private void AnimateSlamMark()
        {
            if (_slamRoot == null || _slamCharge == null || !_slamRoot.gameObject.activeSelf)
            {
                return;
            }

            var p = Mathf.Clamp01((Time.time - _slamStart) / _slamLength);
            // The root is unrotated: z runs along the road, the boss's front is at +depth/2 and the fill grows back from it.
            _slamCharge.localScale = new Vector3(_slamWidth, Mathf.Max(0.001f, _slamDepth * p), 1f);
            _slamCharge.localPosition = new Vector3(0f, 0.004f, _slamDepth * 0.5f * (1f - p));
            if (Time.time - _slamStart > _slamLength + 0.5f)
            {
                _slamRoot.gameObject.SetActive(false);
            }
        }
    }
}
