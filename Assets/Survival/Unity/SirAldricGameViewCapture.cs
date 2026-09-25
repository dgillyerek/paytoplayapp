using System;
using System.Diagnostics;
using System.IO;
using Survival.Domain.Heroes;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Unity Game-view proof: Camera.Render 1080×1920 from the SirAldricDemo Play cam.
    /// Writes rear/TOP draw→strike still + MP4. Not a Blender stand-in.
    /// </summary>
    public static class SirAldricGameViewCapture
    {
        public const int Width = 1080;
        public const int Height = 1920;
        public const string RelDir = "Docs/Survival/previews/facing_20260925";
        public const string AttackMp4Name = "sir_aldric_humanoid_draw_strike_gameview.mp4";
        public const string MidStrikeName = "sir_aldric_humanoid_mid_strike_gameview.png";
        public const string DrawName = "sir_aldric_humanoid_draw_gameview.png";

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
            if (actor == null || !actor.Built || cam == null)
            {
                throw new InvalidOperationException("SirAldric Game-view capture needs a built Meshy Animate actor and Play camera.");
            }

            var dir = ResolveDir();
            var frames = Path.Combine(dir, "_humanoid_attack_frames");
            if (Directory.Exists(frames))
            {
                Directory.Delete(frames, true);
            }

            Directory.CreateDirectory(frames);

            var walkLen = actor.WalkLength > 0.05f ? actor.WalkLength : 1f;
            var walkBlock = walkLen * SirAldric3DMotion.WalkCyclesBeforeAttack;
            var fps = 30;
            var n = Mathf.Max(8, Mathf.RoundToInt(SirAldricHumanoidAttack.Seconds * fps));
            string? midPath = null;
            var peak = walkBlock + SirAldricHumanoidAttack.StrikePeakSeconds;
            var peakDist = float.MaxValue;

            for (var i = 0; i < n; i++)
            {
                var u = i / (float)Mathf.Max(n - 1, 1);
                var t = walkBlock + u * SirAldricHumanoidAttack.Seconds;
                actor.SampleAt(t);
                var png = Path.Combine(frames, "f_" + i.ToString("D3") + ".png");
                WriteFrame(cam, png);
                var dist = Mathf.Abs(t - peak);
                if (dist < peakDist)
                {
                    peakDist = dist;
                    midPath = png;
                }

                if (i == 2)
                {
                    File.Copy(png, Path.Combine(dir, DrawName), overwrite: true);
                }
            }

            var midOut = Path.Combine(dir, MidStrikeName);
            if (midPath != null && File.Exists(midPath))
            {
                File.Copy(midPath, midOut, overwrite: true);
            }

            var mp4 = Path.Combine(dir, AttackMp4Name);
            TryFfmpeg(frames, mp4);
            UnityEngine.Debug.Log("SirAldric Game-view attack wrote " + dir + " (" + AttackAuthoredReasonNote() + ")");
            return dir;
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

        private static string AttackAuthoredReasonNote() => SirAldricHumanoidAttack.Authorship;

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
