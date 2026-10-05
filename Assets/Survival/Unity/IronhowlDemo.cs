using Survival.Domain.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Ironhowl Game view 1080×1920. Standing Mixamo T-pose. No clips.
    /// The look JPEG stays a Design reference and is not bound.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed class IronhowlDemo : MonoBehaviour
    {
        private bool _booted;
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

            if (_phase != null)
            {
                _phase.text = "T-pose · no clips";
            }
        }

        private void Boot()
        {
            if (_booted || !Application.isPlaying)
            {
                return;
            }

            SurvivalVisuals.EnsureEventSystem();
            var stage = GetComponent<PortraitStage>() ?? gameObject.AddComponent<PortraitStage>();
            var actorGo = new GameObject("IronhowlActor");
            actorGo.transform.SetParent(transform, false);
            var actor = actorGo.AddComponent<IronhowlActor>();
            actor.Build();
            if (actor.Built)
            {
                var bounds = actor.VisibleBounds;
                stage.SetBounds(bounds);
                var span = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                stage.BuildGround(bounds.min.y, span * 2f);
            }
            else
            {
                stage.BuildGround(0f, 8f);
            }

            var canvas = SurvivalVisuals.Canvas(transform, "IronhowlHud", 80);
            var title = SurvivalVisuals.Text(canvas, "Caption", "IRONHOWL", 30, TextAnchor.MiddleCenter, SurvivalVisuals.Cream);
            var tr = title.rectTransform;
            tr.anchorMin = new Vector2(0.06f, 0.952f);
            tr.anchorMax = new Vector2(0.94f, 0.992f);
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;

            _phase = SurvivalVisuals.Text(canvas, "Phase", "T-pose · no clips", 22, TextAnchor.MiddleCenter, SurvivalVisuals.Gold);
            var pr = _phase.rectTransform;
            pr.anchorMin = new Vector2(0.06f, 0.012f);
            pr.anchorMax = new Vector2(0.94f, 0.048f);
            pr.offsetMin = Vector2.zero;
            pr.offsetMax = Vector2.zero;
            _booted = true;
        }
    }
}
