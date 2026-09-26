using System;
using System.Diagnostics;
using System.IO;
using Survival.Domain.Heroes;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Unity Game-view proof: Camera.Render 1080×1920 from the SirAldricDemo Play cam
    /// plus front / 3-4 stills. Writes painted look + yawlock attack juice.
    /// Not a Blender stand-in.
    /// </summary>
    public static class SirAldricGameViewCapture
    {
        public const int Width = 1080;
        public const int Height = 1920;
        public const string RelDir = "Docs/Survival/previews/aldric_sep_attack_yawlock_20260926";
        public const string WalkMp4Name = "sir_aldric_sep_yawlock_walk_juice_gameview.mp4";
        public const string AttackMp4Name = "sir_aldric_sep_yawlock_attack_juice_gameview.mp4";
        public const string RearWalkName = "sir_aldric_sep_yawlock_rear_walk_gameview.png";
        public const string FrontWalkName = "sir_aldric_sep_yawlock_front_walk_gameview.png";
        public const string ThreeQuarterWalkName = "sir_aldric_sep_yawlock_34_walk_gameview.png";
        public const string RearAttackName = "sir_aldric_sep_yawlock_rear_attack_gameview.png";
        public const string FrontAttackName = "sir_aldric_sep_yawlock_front_attack_gameview.png";
        public const string ThreeQuarterAttackName = "sir_aldric_sep_yawlock_34_attack_gameview.png";

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

            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "-aldric-capture", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static string CaptureAttack(SirAldricMeshyAnimateActor actor, Camera cam)
        {
            return CaptureSep(actor, cam);
        }

        public static string CaptureSep(SirAldricMeshyAnimateActor actor, Camera cam)
        {
            if (actor == null || !actor.Built || cam == null)
            {
                throw new InvalidOperationException("SirAldric Game-view capture needs a built SEP Meshy actor and Play camera.");
            }

            var dir = ResolveDir();
            var savedPos = cam.transform.position;
            var savedRot = cam.transform.rotation;

            var walkLen = actor.WalkLength > 0.05f ? actor.WalkLength : 1f;
            var attackLen = actor.AttackLength > 0.05f ? actor.AttackLength : 1f;
            var walkBlock = walkLen * SirAldric3DMotion.WalkCyclesBeforeAttack;
            var walkStill = walkLen * 0.35f;
            var attackStill = walkBlock + attackLen * 0.35f;

            actor.SampleAt(walkStill);
            PlacePlayCam(cam, Angle.Rear);
            WriteFrame(cam, Path.Combine(dir, RearWalkName));
            PlacePlayCam(cam, Angle.Front);
            WriteFrame(cam, Path.Combine(dir, FrontWalkName));
            PlacePlayCam(cam, Angle.ThreeQuarter);
            WriteFrame(cam, Path.Combine(dir, ThreeQuarterWalkName));

            actor.SampleAt(attackStill);
            PlacePlayCam(cam, Angle.Rear);
            WriteFrame(cam, Path.Combine(dir, RearAttackName));
            PlacePlayCam(cam, Angle.Front);
            WriteFrame(cam, Path.Combine(dir, FrontAttackName));
            PlacePlayCam(cam, Angle.ThreeQuarter);
            WriteFrame(cam, Path.Combine(dir, ThreeQuarterAttackName));

            PlacePlayCam(cam, Angle.Rear);
            WriteJuice(actor, cam, Path.Combine(dir, "_sep_walk_frames"), 0f, walkLen, WalkMp4Name, dir);
            WriteJuice(actor, cam, Path.Combine(dir, "_sep_attack_frames"), walkBlock, attackLen, AttackMp4Name, dir);

            cam.transform.position = savedPos;
            cam.transform.rotation = savedRot;
            UnityEngine.Debug.Log("SirAldric Game-view SEP wrote " + dir + " (" + SirAldricMeshyAnimateActor.AttackAuthoredReason + ")");
            return dir;
        }

        private enum Angle
        {
            Rear,
            Front,
            ThreeQuarter
        }

        private static void PlacePlayCam(Camera cam, Angle angle)
        {
            cam.fieldOfView = 30f;
            switch (angle)
            {
                case Angle.Front:
                    cam.transform.position = new Vector3(0f, 2.80f, 5.40f);
                    cam.transform.LookAt(new Vector3(0f, 0.90f, 0.50f));
                    break;
                case Angle.ThreeQuarter:
                    cam.transform.position = new Vector3(3.20f, 2.80f, -4.40f);
                    cam.transform.LookAt(new Vector3(0f, 0.90f, 0.50f));
                    break;
                default:
                    cam.transform.position = new Vector3(0f, 2.80f, -5.40f);
                    cam.transform.LookAt(new Vector3(0f, 0.90f, 0.50f));
                    break;
            }
        }

        private static void WriteJuice(
            SirAldricMeshyAnimateActor actor,
            Camera cam,
            string framesDir,
            float start,
            float span,
            string mp4Name,
            string dir)
        {
            if (Directory.Exists(framesDir))
            {
                Directory.Delete(framesDir, true);
            }

            Directory.CreateDirectory(framesDir);
            var fps = 30;
            var n = Mathf.Max(8, Mathf.RoundToInt(span * fps));
            for (var i = 0; i < n; i++)
            {
                var u = i / (float)Mathf.Max(n - 1, 1);
                actor.SampleAt(start + u * span);
                WriteFrame(cam, Path.Combine(framesDir, "f_" + i.ToString("D3") + ".png"));
            }

            TryFfmpeg(framesDir, Path.Combine(dir, mp4Name));
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

        private static void TryFfmpeg(string framesDir, string mp4Path)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-y -framerate 30 -i \"" + Path.Combine(framesDir, "f_%03d.png") +
                                "\" -c:v libx264 -pix_fmt yuv420p -crf 18 \"" + mp4Path + "\"",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(120_000);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("ffmpeg Game-view MP4 skipped: " + ex.Message);
            }
        }
    }
}
