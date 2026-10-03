using System;
using System.IO;
using Survival.Domain.Heroes;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Unity Game-view proof: Camera.Render 1080×1920 from the PILOT Play cam.
    /// Not a Blender stand-in. This VM may not have a Unity Editor.
    /// </summary>
    public static class SirAldricGameViewCapture
    {
        public const int Width = 1080;
        public const int Height = 1920;
        public const string RelDir = "Docs/Survival/previews/aldric_pilot_20260927";
        public const string RearWalkName = "sir_aldric_pilot_rear_walk_gameview.png";
        public const string FrontWalkName = "sir_aldric_pilot_front_walk_gameview.png";
        public const string ThreeQuarterWalkName = "sir_aldric_pilot_34_walk_gameview.png";
        public const string RearAttackName = "sir_aldric_pilot_rear_strike_gameview.png";
        public const string WalkMp4Name = "sir_aldric_pilot_walk_juice_gameview.mp4";
        public const string AttackMp4Name = "sir_aldric_pilot_attack_juice_gameview.mp4";

        public static string ResolveDir()
        {
            var cwd = Directory.GetCurrentDirectory();
            var data = Application.dataPath ?? Path.Combine(cwd, "Assets");
            var repo = Directory.GetParent(data)?.FullName ?? cwd;
            var dir = Path.Combine(repo, RelDir.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static bool ShouldRunFromCommandLine()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("ALDRIC_CAPTURE"), "1", StringComparison.Ordinal))
            {
                return true;
            }

            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (string.Equals(arg, "-aldric-capture", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static string CapturePilot(SirAldricMeshyAnimateActor actor, Camera cam)
        {
            if (actor == null || !actor.Built || cam == null)
            {
                throw new InvalidOperationException("PILOT Game-view capture needs a built actor and Play camera.");
            }

            var dir = ResolveDir();
            var walkLen = actor.WalkLength > 0.05f ? actor.WalkLength : 1f;
            var attackLen = actor.AttackLength > 0.05f ? actor.AttackLength : 1f;
            var walkBlock = walkLen * SirAldric3DMotion.WalkCyclesBeforeAttack;

            actor.SampleAt(walkLen * 0.35f);
            PlacePlayCam(cam, rear: true, threeQ: false);
            WriteFrame(cam, Path.Combine(dir, RearWalkName));
            PlacePlayCam(cam, rear: false, threeQ: false);
            WriteFrame(cam, Path.Combine(dir, FrontWalkName));
            PlacePlayCam(cam, rear: true, threeQ: true);
            WriteFrame(cam, Path.Combine(dir, ThreeQuarterWalkName));

            actor.SampleAt(walkBlock + attackLen * 0.55f);
            PlacePlayCam(cam, rear: true, threeQ: false);
            WriteFrame(cam, Path.Combine(dir, RearAttackName));
            return dir;
        }

        private static void PlacePlayCam(Camera cam, bool rear, bool threeQ)
        {
            cam.fieldOfView = SirAldric3DMotion.PlayCamFovDegrees;
            var look = new Vector3(
                SirAldric3DMotion.PlayCamLookX,
                SirAldric3DMotion.PlayCamLookY,
                SirAldric3DMotion.PlayCamLookZ);
            if (threeQ)
            {
                cam.transform.position = new Vector3(
                    SirAldric3DMotion.PlayCamThreeQuarterX,
                    SirAldric3DMotion.PlayCamThreeQuarterY,
                    SirAldric3DMotion.PlayCamThreeQuarterZ);
            }
            else if (rear)
            {
                cam.transform.position = new Vector3(
                    SirAldric3DMotion.PlayCamRearX,
                    SirAldric3DMotion.PlayCamRearY,
                    SirAldric3DMotion.PlayCamRearZ);
            }
            else
            {
                cam.transform.position = new Vector3(
                    SirAldric3DMotion.PlayCamFrontX,
                    SirAldric3DMotion.PlayCamFrontY,
                    SirAldric3DMotion.PlayCamFrontZ);
            }

            var forward = look - cam.transform.position;
            if (forward.sqrMagnitude < 1e-8f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            var up = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.999f)
            {
                up = Vector3.back;
            }

            cam.transform.rotation = Quaternion.Normalize(Quaternion.LookRotation(forward, up));
        }

        public static void WriteFrame(Camera cam, string pngPath)
        {
            var prev = cam.targetTexture;
            var rt = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            UnityEngine.Object.Destroy(tex);
            RenderTexture.active = prevActive;
            cam.targetTexture = prev;
            RenderTexture.ReleaseTemporary(rt);
        }
    }
}
