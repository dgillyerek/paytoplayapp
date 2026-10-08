using System;

namespace Survival.Domain.Roster
{
    /// <summary>How one attack prop or effect is driven during the attack clip.</summary>
    public enum AttackTrackKind
    {
        /// <summary>Rides a bone with a fixed bone-local offset (staff, bow, nocked arrow, conjured spear).</summary>
        Held = 0,

        /// <summary>Spawns at the release frame and flies character-forward at the Design speed.</summary>
        Projectile = 1,

        /// <summary>Appears at a fixed point for a few frames (breath flare, claw slash).</summary>
        Fixed = 2
    }

    /// <summary>
    /// Unity-side look for a track. Design VFX meshes are look reference only, so effects are
    /// procedural URP particles, trails, and lines in the looksheet colours.
    /// </summary>
    public enum AttackVfxShape
    {
        None = 0,
        Orb = 1,
        Trail = 2,
        Flare = 3,
        Lightning = 4,
        Slash = 5
    }

    /// <summary>
    /// One baked frame of a track, already converted from Blender world to Unity convention
    /// (Unity = (-x, z, -y) of Blender, metres, character forward = +Z).
    /// </summary>
    public readonly struct AttackFrame
    {
        public AttackFrame(float x, float y, float z, float qx, float qy, float qz, float qw, float scale, bool visible)
        {
            X = x;
            Y = y;
            Z = z;
            Qx = qx;
            Qy = qy;
            Qz = qz;
            Qw = qw;
            Scale = scale;
            Visible = visible;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float Qx { get; }
        public float Qy { get; }
        public float Qz { get; }
        public float Qw { get; }
        public float Scale { get; }
        public bool Visible { get; }
    }

    /// <summary>One prop or effect on an attack clip, with frames 0..30 baked from Design's attack_meta drivers.</summary>
    public sealed class AttackTrack
    {
        public AttackTrack(
            string objectName,
            AttackTrackKind kind,
            string propFileName,
            string bone,
            int refFrame,
            int releaseFrame,
            float speed,
            AttackVfxShape shape,
            float sizeMetres,
            float lengthMetres,
            float[] coreRgb,
            float[] glowRgb,
            float[] edgeRgb,
            float[] propExtent,
            AttackFrame[] frames,
            string note)
        {
            ObjectName = objectName;
            Kind = kind;
            PropFileName = propFileName ?? string.Empty;
            Bone = bone ?? string.Empty;
            RefFrame = refFrame;
            ReleaseFrame = releaseFrame;
            Speed = speed;
            Shape = shape;
            SizeMetres = sizeMetres;
            LengthMetres = lengthMetres;
            CoreRgb = coreRgb;
            GlowRgb = glowRgb;
            EdgeRgb = edgeRgb;
            PropExtent = propExtent;
            Frames = frames;
            Note = note ?? string.Empty;
        }

        public string ObjectName { get; }
        public AttackTrackKind Kind { get; }

        /// <summary>Prop FBX next to the character FBXs. Empty for particle-only effects.</summary>
        public string PropFileName { get; }

        /// <summary>Attach bone for held props. Empty for projectiles and fixed effects.</summary>
        public string Bone { get; }

        public int RefFrame { get; }
        public int ReleaseFrame { get; }

        /// <summary>Design speed in metres per second (projectiles only).</summary>
        public float Speed { get; }

        public AttackVfxShape Shape { get; }
        public float SizeMetres { get; }
        public float LengthMetres { get; }
        public float[] CoreRgb { get; }
        public float[] GlowRgb { get; }
        public float[] EdgeRgb { get; }

        /// <summary>Prop mesh extent at identity in Unity convention (x, y, z metres). Used to size the imported prop.</summary>
        public float[] PropExtent { get; }

        public AttackFrame[] Frames { get; }
        public string Note { get; }

        public bool HasMesh => PropFileName.Length > 0;

        public int FirstVisibleFrame
        {
            get
            {
                for (var i = 0; i < Frames.Length; i++)
                {
                    if (Frames[i].Visible)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        public int LastVisibleFrame
        {
            get
            {
                for (var i = Frames.Length - 1; i >= 0; i--)
                {
                    if (Frames[i].Visible)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        /// <summary>Visible on the integer frame at or below <paramref name="frame"/>, like the Design renders.</summary>
        public bool VisibleAt(float frame)
        {
            if (Frames.Length == 0 || frame < 0f)
            {
                return false;
            }

            var i = (int)Math.Floor(frame);
            if (i >= Frames.Length)
            {
                i = Frames.Length - 1;
            }

            return Frames[i].Visible;
        }

        /// <summary>Linear position and scale, normalised-lerp rotation between baked frames.</summary>
        public AttackFrame Sample(float frame)
        {
            if (Frames.Length == 0)
            {
                return new AttackFrame(0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, false);
            }

            if (frame <= 0f)
            {
                return Frames[0];
            }

            var last = Frames.Length - 1;
            if (frame >= last)
            {
                return Frames[last];
            }

            var i = (int)Math.Floor(frame);
            var t = frame - i;
            var a = Frames[i];
            var b = Frames[i + 1];
            var dot = (a.Qx * b.Qx) + (a.Qy * b.Qy) + (a.Qz * b.Qz) + (a.Qw * b.Qw);
            var sign = dot < 0f ? -1f : 1f;
            var qx = a.Qx + (((sign * b.Qx) - a.Qx) * t);
            var qy = a.Qy + (((sign * b.Qy) - a.Qy) * t);
            var qz = a.Qz + (((sign * b.Qz) - a.Qz) * t);
            var qw = a.Qw + (((sign * b.Qw) - a.Qw) * t);
            var len = (float)Math.Sqrt((qx * qx) + (qy * qy) + (qz * qz) + (qw * qw));
            if (len > 1e-6f)
            {
                qx /= len;
                qy /= len;
                qz /= len;
                qw /= len;
            }

            return new AttackFrame(
                a.X + ((b.X - a.X) * t),
                a.Y + ((b.Y - a.Y) * t),
                a.Z + ((b.Z - a.Z) * t),
                qx,
                qy,
                qz,
                qw,
                a.Scale + ((b.Scale - a.Scale) * t),
                a.Visible);
        }
    }

    /// <summary>
    /// Design Theme A attack add-on (2026-10-07): a 0–30 frame Scene take at 30 fps on the existing
    /// blenderig (same bones, no rebind), starting and ending at rest. The demo plays it once,
    /// holds rest for <see cref="RestGapSeconds"/>, then repeats, so the projectile reads every cycle.
    /// Strikes and projectiles travel character-forward (+Z, top of the screen in the rear camera).
    /// </summary>
    public sealed class BlenderRigAttackSpec
    {
        public const string PoseName = "attack";
        public const string TakeName = "Scene";
        public const int FrameRate = 30;
        public const int FirstFrame = 0;
        public const int LastFrame = 30;
        public const float RestGapSeconds = 0.5f;
        public const string DesignFolder = "attack_20261007";

        public BlenderRigAttackSpec(
            string character,
            string fileName,
            string fileMd5,
            string designDir,
            int releaseFrame,
            bool inPlace,
            string[] calibrationBones,
            float[] calibrationRest,
            AttackTrack[] tracks)
        {
            Character = character;
            FileName = fileName;
            FileMd5 = fileMd5;
            DesignDir = designDir;
            ReleaseFrame = releaseFrame;
            InPlace = inPlace;
            CalibrationBones = calibrationBones;
            CalibrationRest = calibrationRest;
            Tracks = tracks;
        }

        public string Character { get; }
        public string FileName { get; }
        public string FileMd5 { get; }

        /// <summary>Design SoT folder under the repo root, e.g. design/survival-theme-a-fantasy/heroes/anim/lyra/attack_20261007.</summary>
        public string DesignDir { get; }

        public int ReleaseFrame { get; }

        /// <summary>True when the clip slides the root and must play in place (root motion off).</summary>
        public bool InPlace { get; }

        /// <summary>Bones whose rest heads map Design (Blender) space onto the imported Unity rig.</summary>
        public string[] CalibrationBones { get; }

        /// <summary>Rest heads of <see cref="CalibrationBones"/> in Unity convention, xyz per bone.</summary>
        public float[] CalibrationRest { get; }

        public AttackTrack[] Tracks { get; }

        public static float ClipSeconds => (LastFrame - FirstFrame) / (float)FrameRate;

        public static float CycleSeconds => ClipSeconds + RestGapSeconds;

        /// <summary>
        /// Attack frame for a time since the attack was picked. Plays 0–30 once, then holds the
        /// last frame (rest) through the gap, then repeats.
        /// </summary>
        public static float CycleFrame(float seconds, out bool inClip, out int cycle)
        {
            if (seconds < 0f)
            {
                seconds = 0f;
            }

            cycle = (int)Math.Floor(seconds / CycleSeconds);
            var t = seconds - (cycle * CycleSeconds);
            if (t <= ClipSeconds)
            {
                inClip = true;
                return FirstFrame + (t * FrameRate);
            }

            inClip = false;
            return LastFrame;
        }

        public AttackTrack? Find(string objectName)
        {
            foreach (var track in Tracks)
            {
                if (string.Equals(track.ObjectName, objectName, StringComparison.Ordinal))
                {
                    return track;
                }
            }

            return null;
        }
    }
}
