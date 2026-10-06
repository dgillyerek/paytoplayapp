using Survival.Domain.Roster;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Portrait Game view shared by Design Blender rigs.
    /// Opens on the rest bind pose. Dropdown plays rest and each action clip on the spec.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public abstract class BlenderRigDemo : MonoBehaviour
    {
        private bool _booted;
        private MonoBehaviour? _actorBehaviour;
        private IBlenderRigPlayback? _playback;
        private Dropdown? _dropdown;
        private Text? _phase;

        protected abstract BlenderRigSpec Spec { get; }

        protected abstract MonoBehaviour CreateActor(GameObject host);

        private void Awake() => Boot();

        private void OnEnable() => Boot();

        private void Start() => Boot();

        private void Update()
        {
            if (!_booted)
            {
                Boot();
            }

            if (_phase == null || _playback == null)
            {
                return;
            }

            var length = _playback.ClipLength > 0.05f ? _playback.ClipLength.ToString("0.00") + "s" : "bind";
            _phase.text = _playback.Pose + "   ·   " + length;
        }

        public bool PlayNamedPose(string poseName)
        {
            var playback = _playback;
            if (playback == null && _actorBehaviour != null)
            {
                playback = _actorBehaviour as IBlenderRigPlayback;
            }

            if (playback == null)
            {
                return false;
            }

            var played = playback.PlayPose(poseName);
            if (_dropdown != null)
            {
                var poses = Spec.PoseNames;
                for (var i = 0; i < poses.Length; i++)
                {
                    if (string.Equals(poses[i], poseName, System.StringComparison.Ordinal))
                    {
                        _dropdown.SetValueWithoutNotify(i);
                        _dropdown.RefreshShownValue();
                        break;
                    }
                }
            }

            return played;
        }

        private void Boot()
        {
            if (_booted || !Application.isPlaying)
            {
                return;
            }

            var spec = Spec;
            SurvivalVisuals.EnsureEventSystem();
            var stage = GetComponent<PortraitStage>() ?? gameObject.AddComponent<PortraitStage>();
            var actorGo = new GameObject(spec.Name + "Actor");
            actorGo.transform.SetParent(transform, false);
            _actorBehaviour = CreateActor(actorGo);
            _playback = _actorBehaviour as IBlenderRigPlayback;
            if (_playback != null && _playback.Built)
            {
                var bounds = _playback.VisibleBounds;
                stage.SetBounds(bounds);
                var span = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                stage.BuildGround(bounds.min.y, span * 2f);
            }
            else
            {
                stage.BuildGround(0f, 8f);
            }

            var canvas = SurvivalVisuals.Canvas(transform, spec.Name + "Hud", 80);
            var title = SurvivalVisuals.Text(canvas, "Caption", spec.Title, 30, TextAnchor.MiddleCenter, SurvivalVisuals.Cream);
            var tr = title.rectTransform;
            tr.anchorMin = new Vector2(0.06f, 0.952f);
            tr.anchorMax = new Vector2(0.94f, 0.992f);
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;

            var dropdown = SurvivalVisuals.Dropdown(canvas, spec.DropdownObjectName, new Color(0.10f, 0.12f, 0.16f, 0.96f));
            var ddRt = dropdown.GetComponent<RectTransform>();
            ddRt.anchorMin = new Vector2(0.04f, 0.858f);
            ddRt.anchorMax = new Vector2(0.96f, 0.946f);
            ddRt.offsetMin = Vector2.zero;
            ddRt.offsetMax = Vector2.zero;
            if (dropdown.captionText != null)
            {
                dropdown.captionText.fontSize = 32;
                dropdown.captionText.alignment = TextAnchor.MiddleLeft;
            }

            if (dropdown.itemText != null)
            {
                dropdown.itemText.fontSize = 28;
            }

            dropdown.ClearOptions();
            var options = new System.Collections.Generic.List<Dropdown.OptionData>();
            var poses = spec.PoseNames;
            for (var i = 0; i < poses.Length; i++)
            {
                options.Add(new Dropdown.OptionData(poses[i]));
            }

            dropdown.AddOptions(options);
            dropdown.SetValueWithoutNotify(0);
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(OnPosePicked);
            _dropdown = dropdown;

            _phase = SurvivalVisuals.Text(canvas, "Phase", BlenderRigSpec.RestPoseName, 22, TextAnchor.MiddleCenter, SurvivalVisuals.Gold);
            var pr = _phase.rectTransform;
            pr.anchorMin = new Vector2(0.06f, 0.012f);
            pr.anchorMax = new Vector2(0.94f, 0.048f);
            pr.offsetMin = Vector2.zero;
            pr.offsetMax = Vector2.zero;

            _booted = true;
            if (_playback != null && _playback.Built)
            {
                _playback.PlayPose(BlenderRigSpec.RestPoseName);
            }
        }

        private void OnPosePicked(int index)
        {
            var poses = Spec.PoseNames;
            if (index < 0 || index >= poses.Length)
            {
                return;
            }

            PlayNamedPose(poses[index]);
        }
    }

    public interface IBlenderRigPlayback
    {
        bool Built { get; }
        string Pose { get; }
        float ClipLength { get; }
        Bounds VisibleBounds { get; }
        bool PlayPose(string poseName);
    }
}
