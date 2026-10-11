using Survival.Domain.View;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Survival.Unity
{
    /// <summary>
    /// Portrait 1080×1920 battle camera for the locked battle demo. Same lens as PortraitStage
    /// (54° FOV, near 0.25, far 4000, dark green clear). Fits the whole battlefield box from a
    /// raised angle: rear (behind the heroes, villains toward the top), three-quarter,
    /// front (behind the villains), or side. Keys 1–4 match the dropdown.
    /// </summary>
    [DefaultExecutionOrder(900)]
    public sealed class LockedBattleCamera : MonoBehaviour
    {
        public const int Rear = 0;
        public const int ThreeQuarter = 1;
        public const int Front = 2;
        public const int Side = 3;

        public static readonly string[] ModeNames =
        {
            "Rear battle cam (behind heroes)",
            "Three-quarter",
            "Front (behind villains)",
            "Side"
        };

        private Bounds _field = new Bounds(new Vector3(0f, 1f, 0f), new Vector3(6f, 2f, 8f));
        private int _mode = Rear;
        private Vector3 _eye;
        private Quaternion _rotation = Quaternion.identity;
        private Vector3 _shakeOffset;
        private float _shakeMetres;
        private float _shakeAge = 1f;

        /// <summary>How long one hit shake lasts.</summary>
        public const float ShakeSeconds = 0.18f;

        public int Mode => _mode;

        public System.Action<int>? ModeChanged { get; set; }

        public void SetField(Bounds field)
        {
            _field = field;
            Solve();
        }

        public void SetMode(int mode)
        {
            if (mode < 0 || mode >= ModeNames.Length)
            {
                return;
            }

            _mode = mode;
            Solve();
            ModeChanged?.Invoke(mode);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null)
            {
                return;
            }

            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
            {
                SetMode(Rear);
            }
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
            {
                SetMode(ThreeQuarter);
            }
            else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
            {
                SetMode(Front);
            }
            else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame)
            {
                SetMode(Side);
            }
        }

        /// <summary>Small screen-plane shake for a big hit. Overlapping hits keep the stronger one.</summary>
        public void Shake(float metres)
        {
            if (metres <= 0f)
            {
                return;
            }

            var left = _shakeAge < ShakeSeconds ? _shakeMetres * (1f - (_shakeAge / ShakeSeconds)) : 0f;
            _shakeMetres = Mathf.Max(metres, left);
            _shakeAge = 0f;
        }

        private void LateUpdate()
        {
            _shakeAge += Time.unscaledDeltaTime;
            if (_shakeAge < ShakeSeconds && _shakeMetres > 0f)
            {
                var k = 1f - (_shakeAge / ShakeSeconds);
                var t = Time.unscaledTime * 55f;
                var x = (Mathf.PerlinNoise(t, 0.37f) * 2f) - 1f;
                var y = (Mathf.PerlinNoise(0.71f, t) * 2f) - 1f;
                _shakeOffset = ((_rotation * Vector3.right * x) + (_rotation * Vector3.up * y)) * (_shakeMetres * k * k);
            }
            else
            {
                _shakeOffset = Vector3.zero;
            }

            Apply();
        }

        private void Solve()
        {
            float yaw;
            float pitch;
            switch (_mode)
            {
                case ThreeQuarter:
                    yaw = 135f;
                    pitch = 30f;
                    break;
                case Front:
                    yaw = 0f;
                    pitch = 30f;
                    break;
                case Side:
                    yaw = 90f;
                    pitch = 20f;
                    break;
                default:
                    yaw = 180f;
                    pitch = 32f;
                    break;
            }

            var yr = yaw * Mathf.Deg2Rad;
            var pr = pitch * Mathf.Deg2Rad;
            var toEye = new Vector3(Mathf.Cos(pr) * Mathf.Sin(yr), Mathf.Sin(pr), Mathf.Cos(pr) * Mathf.Cos(yr));
            var forward = -toEye;
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var up = Vector3.Cross(forward, right).normalized;
            var tanV = Mathf.Tan(PortraitGameView.FovDegrees * 0.5f * Mathf.Deg2Rad);
            var tanH = tanV * (PortraitGameView.Width / PortraitGameView.Height);
            var look = _field.center;
            var need = 2f;
            var min = _field.min;
            var max = _field.max;
            for (var i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y,
                    (i & 4) == 0 ? min.z : max.z);
                var rel = corner - look;
                var r = Mathf.Abs(Vector3.Dot(rel, right));
                var u = Mathf.Abs(Vector3.Dot(rel, up));
                var w = Vector3.Dot(rel, forward);
                need = Mathf.Max(need, w + (r / tanH));
                need = Mathf.Max(need, w + (u / tanV));
            }

            var distance = need * 1.06f;
            _eye = look + (toEye * distance);
            _rotation = Quaternion.LookRotation(forward, Vector3.up);
            Apply();
        }

        private void Apply()
        {
            var n = Camera.allCamerasCount;
            if (n <= 0)
            {
                Place(Camera.main);
                return;
            }

            var cams = new Camera[n];
            Camera.GetAllCameras(cams);
            for (var i = 0; i < n; i++)
            {
                Place(cams[i]);
            }
        }

        private void Place(Camera? cam)
        {
            if (cam == null)
            {
                return;
            }

            cam.orthographic = false;
            cam.usePhysicalProperties = false;
            cam.fieldOfView = PortraitGameView.FovDegrees;
            cam.nearClipPlane = PortraitGameView.Near;
            cam.farClipPlane = PortraitGameView.Far;
            cam.aspect = PortraitGameView.Width / PortraitGameView.Height;
            cam.backgroundColor = new Color(0.07f, 0.09f, 0.06f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.SetPositionAndRotation(_eye + _shakeOffset, _rotation);
        }
    }
}
