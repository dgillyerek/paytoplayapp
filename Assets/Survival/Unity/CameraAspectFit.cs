#nullable enable
using Survival.Domain.View;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Holds a perspective camera's horizontal view at what it is on the authored 9:16 Game view, so heroes and enemies
    /// are the same share of the screen's width on any portrait phone; a taller screen shows more world instead of
    /// bigger units (<see cref="AspectFit"/>). At 9:16 and wider it is the plain authored vertical field of view.
    /// <see cref="ExtraBelow"/> other than one half draws an off-centre frustum (the extra height under the frame).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraAspectFit : MonoBehaviour
    {
        /// <summary>The vertical field of view (degrees) authored on 9:16.</summary>
        public float ReferenceVerticalFov = 60f;

        /// <summary>0..1: share of the extra height added under the authored frame (0.5 = centred).</summary>
        public float ExtraBelow = 0.5f;

        private Camera? _cam;
        private int _w, _h;
        private float _near, _far, _fov, _below;

        public static CameraAspectFit Attach(Camera cam, float referenceVerticalFov, float extraBelow)
        {
            var fit = cam.GetComponent<CameraAspectFit>() ?? cam.gameObject.AddComponent<CameraAspectFit>();
            fit.ReferenceVerticalFov = referenceVerticalFov;
            fit.ExtraBelow = extraBelow;
            fit.Apply(true);
            return fit;
        }

        private void OnEnable() => Apply(true);

        private void OnDisable()
        {
            if (_cam != null)
            {
                _cam.ResetProjectionMatrix();
                _cam.fieldOfView = ReferenceVerticalFov;
            }
        }

        // Before anything reads the projection this frame (drag rays, HUD markers placed in LateUpdate).
        private void Update() => Apply(false);

        private void LateUpdate() => Apply(false);

        private void Apply(bool force)
        {
            _cam ??= GetComponent<Camera>();
            if (_cam == null || _cam.orthographic)
            {
                return;
            }

            var w = _cam.pixelWidth;
            var h = _cam.pixelHeight;
            if (!force && w == _w && h == _h && _near == _cam.nearClipPlane && _far == _cam.farClipPlane
                && _fov == ReferenceVerticalFov && _below == ExtraBelow)
            {
                return;
            }

            _w = w;
            _h = h;
            _near = _cam.nearClipPlane;
            _far = _cam.farClipPlane;
            _fov = ReferenceVerticalFov;
            _below = ExtraBelow;
            var aspect = h > 0 ? (float)w / h : AspectFit.ReferenceAspect;
            var f = AspectFit.Fit(ReferenceVerticalFov, aspect, ExtraBelow);
            _cam.ResetAspect();
            _cam.fieldOfView = (float)f.VerticalDegrees;
            if (f.Symmetric)
            {
                _cam.ResetProjectionMatrix();
                return;
            }

            var n = _near;
            _cam.projectionMatrix = Matrix4x4.Frustum(
                (float)(f.Left * n), (float)(f.Right * n), (float)(f.Bottom * n), (float)(f.Top * n), n, _far);
        }
    }
}
