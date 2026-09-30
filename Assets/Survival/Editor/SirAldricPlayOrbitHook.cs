#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Survival.Editor
{
    /// <summary>
    /// Editor play-mode keys reach this hook even when Game view Input System is deaf
    /// (Derek 52aba6b). 1/2/3 Q/E arrows drive SirAldricDemo orbit.
    /// </summary>
    [InitializeOnLoad]
    public static class SirAldricPlayOrbitHook
    {
        static SirAldricPlayOrbitHook()
        {
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var demo = Survival.Unity.SirAldricDemo.Live;
            if (demo == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                demo.ForcePreset(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                demo.ForcePreset(2);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                demo.ForcePreset(3);
            }

            var yaw = 0f;
            if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                yaw -= 1f;
            }

            if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                yaw += 1f;
            }

            if (Mathf.Abs(yaw) > 0.01f)
            {
                demo.AddOrbit(yaw * 90f * Time.unscaledDeltaTime, 0f);
            }
        }
    }
}
#endif
