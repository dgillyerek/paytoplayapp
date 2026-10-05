using Survival.Domain.Heroes;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Emberfang Game view 1080×1920. Opens on the rest bind pose.
    /// Dropdown plays rest and the 1s wing flap. HOLD merge until Derek Game-view PASS.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed class EmberfangDemo : MonoBehaviour
    {
        public const string DropdownObjectName = "EmberfangClipDropdown";

        private bool _booted;
        private EmberfangActor? _actor;
        private Dropdown? _dropdown;
        private Text? _phase;

        private void Awake() => Boot();

        private void OnEnable() => Boot();

        private void Start() => Boot();

        private void Update()
        {
            if (!_booted)
            {
                Boot();
            }

            if (_phase == null || _actor == null)
            {
                return;
            }

            var length = _actor.ClipLength > 0.05f ? _actor.ClipLength.ToString("0.00") + "s" : "bind";
            _phase.text = _actor.Pose + "   ·   " + length;
        }

        public bool PlayNamedPose(string poseName)
        {
            var actor = _actor != null ? _actor : FindFirstObjectByType<EmberfangActor>();
            if (actor == null)
            {
                return false;
            }

            var played = actor.PlayPose(poseName);
            if (_dropdown != null)
            {
                var poses = EmberfangMotion.PoseNames;
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

            SurvivalVisuals.EnsureEventSystem();
            var stage = GetComponent<PortraitStage>() ?? gameObject.AddComponent<PortraitStage>();
            var actorGo = new GameObject("EmberfangActor");
            actorGo.transform.SetParent(transform, false);
            _actor = actorGo.AddComponent<EmberfangActor>();
            _actor.Build();
            if (_actor.Built)
            {
                var bounds = _actor.VisibleBounds;
                stage.SetBounds(bounds);
                var span = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                stage.BuildGround(bounds.min.y, span * 2f);
            }
            else
            {
                stage.BuildGround(0f, 8f);
            }

            var canvas = SurvivalVisuals.Canvas(transform, "EmberfangHud", 80);
            var title = SurvivalVisuals.Text(canvas, "Caption", "EMBERFANG", 30, TextAnchor.MiddleCenter, SurvivalVisuals.Cream);
            var tr = title.rectTransform;
            tr.anchorMin = new Vector2(0.06f, 0.952f);
            tr.anchorMax = new Vector2(0.94f, 0.992f);
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;

            var dropdown = SurvivalVisuals.Dropdown(canvas, DropdownObjectName, new Color(0.10f, 0.12f, 0.16f, 0.96f));
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
            var poses = EmberfangMotion.PoseNames;
            for (var i = 0; i < poses.Length; i++)
            {
                options.Add(new Dropdown.OptionData(poses[i]));
            }

            dropdown.AddOptions(options);
            dropdown.SetValueWithoutNotify(0);
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(OnPosePicked);
            _dropdown = dropdown;

            _phase = SurvivalVisuals.Text(canvas, "Phase", EmberfangMotion.RestPoseName, 22, TextAnchor.MiddleCenter, SurvivalVisuals.Gold);
            var pr = _phase.rectTransform;
            pr.anchorMin = new Vector2(0.06f, 0.012f);
            pr.anchorMax = new Vector2(0.94f, 0.048f);
            pr.offsetMin = Vector2.zero;
            pr.offsetMax = Vector2.zero;

            _booted = true;
            if (_actor.Built)
            {
                _actor.PlayPose(EmberfangMotion.RestPoseName);
            }
        }

        private void OnPosePicked(int index)
        {
            var poses = EmberfangMotion.PoseNames;
            if (index < 0 || index >= poses.Length)
            {
                return;
            }

            PlayNamedPose(poses[index]);
        }
    }
}
