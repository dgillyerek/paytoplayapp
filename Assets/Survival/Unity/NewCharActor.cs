using System.Collections.Generic;
using Survival.Domain.Roster;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Plays one of the preview_new_chars characters (Seraphine, Kaelen, Malzareth; see <see cref="NewCharMotion"/>) in
    /// the battle and the runner. It builds the body exactly as the New Characters Preview does (same atlas, cape on
    /// Spine2 with runtime spring chains, Seraphine's spear, Kaelen's sword props and inward-slash grip) through
    /// <see cref="NewCharsPreview.BuildUnit"/>, so a file Design swaps in place shows up in both. Nothing is re-rigged,
    /// re-weighted or re-baked: every pose is Design's own clip, sampled in place with the preview's crossfade.
    /// Same surface as <see cref="VorgathActor"/>: Build, PlayPose, Pose, Built, VisibleBounds, AttackSpeed.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class NewCharActor : MonoBehaviour
    {
        private NewCharSpec? _spec;
        private NewCharsPreview.Unit? _unit;
        private string _pose = NewCharMotion.RestPoseName;
        private bool _loop = true;
        private Bounds _bounds;

        /// <summary>Attack take rate. 1 in the battle; the runner sets it so the contact lands on its release.</summary>
        public float AttackSpeed { get; set; } = 1f;

        /// <summary>Walk loop playback rate (runner foes, <see cref="Survival.Domain.Runner.RunnerWalkCadence"/>); 1 in the battle.</summary>
        public float WalkRate { get; set; } = 1f;

        public bool Built { get; private set; }
        public string Pose => _pose;
        public Bounds VisibleBounds => _bounds;

        /// <summary>Build <paramref name="rosterName"/> (a <see cref="FighterNames"/> value with a <see cref="NewCharMotion"/> spec).</summary>
        public void Build(string rosterName)
        {
            if (!NewCharMotion.TryFind(rosterName, out var spec))
            {
                Debug.LogError("NewCharActor: no NewCharMotion spec for " + rosterName);
                return;
            }

            Build(spec);
        }

        public void Build(NewCharSpec spec)
        {
            _spec = spec;
            var clips = new List<string> { NewCharMotion.IdlePoseName, NewCharMotion.WalkPoseName };
            var impacts = new List<BattleImpactKind>();
            foreach (var a in spec.Attacks)
            {
                clips.Add(a.Pose);
                impacts.Add(a.Impact);
            }

            var def = new NewCharsPreview.Def(
                spec.Folder,
                spec.Prefix,
                spec.Side == BattleSide.Hero,
                0f,
                0f,
                clips.ToArray(),
                impacts.ToArray(),
                Color.white,
                Color.white);
            _unit = NewCharsPreview.BuildUnit(def, transform);
            if (_unit == null)
            {
                return;
            }

            var missing = new List<string>();
            for (var i = 0; i < _unit.Clips.Count; i++)
            {
                if (_unit.Clips[i] == null)
                {
                    missing.Add(clips[i]);
                }
            }

            _unit.Clip = 0;
            _unit.Time = 0f;
            Sample(0f);
            _unit.Cloth?.ResetToRest();
            _bounds = MeasureLocal();
            Built = missing.Count == 0;
            if (!Built)
            {
                Debug.LogError(spec.Name + " clip missing: " + string.Join(", ", missing));
            }
        }

        public bool PlayPose(string poseName)
        {
            if (_unit == null || _spec == null)
            {
                return false;
            }

            var index = IndexOf(poseName);
            if (index < 0)
            {
                Debug.LogWarning(_spec.Name + " unknown pose. " + poseName);
                return false;
            }

            var loop = index <= 1;
            if (_pose == poseName && _loop == loop)
            {
                return true;
            }

            NewCharsPreview.Snapshot(_unit);
            _pose = poseName;
            _loop = loop;
            _unit.Clip = index;
            _unit.Time = 0f;
            return true;
        }

        private int IndexOf(string poseName)
        {
            if (_unit == null)
            {
                return -1;
            }

            if (poseName == NewCharMotion.RestPoseName)
            {
                return 0;
            }

            var names = _unit.Def.Clips;
            for (var i = 0; i < names.Length; i++)
            {
                if (names[i] == poseName)
                {
                    return i;
                }
            }

            return -1;
        }

        private void Update()
        {
            var u = _unit;
            if (u == null)
            {
                return;
            }

            var dt = Time.deltaTime;
            var walkLoop = _loop && string.Equals(_pose, NewCharMotion.WalkPoseName, System.StringComparison.Ordinal);
            u.Time += dt * (_loop ? (walkLoop ? Mathf.Max(0.05f, WalkRate) : 1f) : Mathf.Max(0.05f, AttackSpeed));
            var clip = u.Clips[u.Clip];
            var length = clip != null ? Mathf.Max(0.05f, clip.length) : 1f;
            Sample(_loop ? Mathf.Repeat(u.Time, length) : Mathf.Min(u.Time, length));
            if (u.BlendLeft > 0f)
            {
                u.BlendLeft = Mathf.Max(0f, u.BlendLeft - dt);
                var w = 1f - (u.BlendLeft / NewCharsPreview.Blend);
                w = w * w * (3f - (2f * w));
                for (var i = 0; i < u.Bones.Length; i++)
                {
                    var bone = u.Bones[i];
                    if (bone == null)
                    {
                        continue;
                    }

                    bone.localPosition = Vector3.Lerp(u.FromPos[i], bone.localPosition, w);
                    bone.localRotation = Quaternion.Slerp(u.FromRot[i], bone.localRotation, w);
                }
            }

            NewCharsPreview.HoldGrips(u);
        }

        /// <summary>One clip sample, then the preview's per-frame holds (scale pins, spear grip, in-place hips, props).</summary>
        private void Sample(float time)
        {
            var u = _unit;
            if (u == null)
            {
                return;
            }

            var clip = u.Clips[u.Clip];
            if (clip != null)
            {
                BlendShapeReset.Zero(u.Shaped);
                clip.SampleAnimation(u.Model, time);
                NewCharsPreview.RestoreScales(u);
                NewCharsPreview.ApplyStaffGrip(u);
            }

            NewCharsPreview.HoldHips(u);
            NewCharsPreview.HoldGrips(u);
        }

        /// <summary>Visible renderers in this host's frame (the battle and runner measure with the host at the origin).</summary>
        private Bounds MeasureLocal()
        {
            var seeded = false;
            var bounds = new Bounds(Vector3.up, Vector3.one);
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (!(r is SkinnedMeshRenderer) && !(r is MeshRenderer))
                {
                    continue;
                }

                var b = r.bounds;
                var min = transform.InverseTransformPoint(b.min);
                var max = transform.InverseTransformPoint(b.max);
                var local = new Bounds((min + max) * 0.5f, Vector3.zero);
                local.Encapsulate(min);
                local.Encapsulate(max);
                if (!seeded)
                {
                    bounds = local;
                    seeded = true;
                }
                else
                {
                    bounds.Encapsulate(local);
                }
            }

            return bounds;
        }
    }
}
