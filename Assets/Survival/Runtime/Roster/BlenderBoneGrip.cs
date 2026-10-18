using System;

namespace Survival.Domain.Roster
{
    /// <summary>
    /// A prop held at a fixed offset in a bone's own space, taken straight from Blender (pose-bone space: y along the bone).
    /// Unity's FBX import keeps each bone's local axes but flips handedness by negating local X, so a Blender bone-space
    /// point (x, y, z) is (-x, y, z) in the imported bone and a rotation quaternion (x, y, z, w) becomes (x, -y, -z, w).
    /// Two child bone heads recorded in the same Blender bone space let the runtime confirm that mapping and recover the
    /// import scale from the real imported skeleton instead of a world-space fit.
    /// </summary>
    public sealed class BlenderBoneGrip
    {
        public BlenderBoneGrip(float[] gripPos, float[] gripRot, string childA, float[] childAHead, string childB, float[] childBHead, float[] handCentre,
            float rollRestDeg = 0f, float rollDrawDeg = 0f, float[]? rollKeys = null,
            float[]? arrowRest = null, float[]? stringTop = null, float[]? stringBottom = null, string stringMaterial = "",
            string arrowTrack = "", float nockFrame = -1f, float releaseFrame = -1f)
        {
            ArrowRest = arrowRest;
            StringTop = stringTop;
            StringBottom = stringBottom;
            StringMaterial = stringMaterial ?? string.Empty;
            ArrowTrack = arrowTrack ?? string.Empty;
            NockFrame = nockFrame;
            ReleaseFrame = releaseFrame;
            RollRestDeg = rollRestDeg;
            RollDrawDeg = rollDrawDeg;
            RollKeys = rollKeys ?? new[] { 0f, 0f, 0f, 0f };
            GripPos = gripPos ?? throw new ArgumentNullException(nameof(gripPos));
            GripRot = gripRot ?? throw new ArgumentNullException(nameof(gripRot));
            ChildA = childA;
            ChildAHead = childAHead;
            ChildB = childB;
            ChildBHead = childBHead;
            HandCentre = handCentre;
        }

        /// <summary>Grip centre (prop pivot) in Blender bone space, metres.</summary>
        public float[] GripPos { get; }

        /// <summary>Prop rotation in Blender bone space, quaternion x, y, z, w.</summary>
        public float[] GripRot { get; }

        public string ChildA { get; }
        public float[] ChildAHead { get; }
        public string ChildB { get; }
        public float[] ChildBHead { get; }

        /// <summary>Centre of the closed hand around the grip axis, Blender bone space (worst sample over rest, walk and attack).</summary>
        public float[] HandCentre { get; }

        /// <summary>Roll about the prop's long axis (Blender prop-local z, after <see cref="GripRot"/>) in rest and walk.</summary>
        public float RollRestDeg { get; }

        /// <summary>Roll held in the attack from <see cref="RollKeys"/>[1] to [2] (nock to release).</summary>
        public float RollDrawDeg { get; }

        /// <summary>Attack frames: start easing to the draw roll, reach it, leave it, back to the rest roll (smoothstep eases).</summary>
        public float[] RollKeys { get; }

        /// <summary>Arrow rest in Blender prop-local space (metres): beside the riser on the archer's left, just above the fist.
        /// From <see cref="NockFrame"/> to <see cref="ReleaseFrame"/> the nocked arrow is aimed from its nock (the draw-hand bone
        /// binding) through this point, exactly as Blender bakes it, so it lies on the correct side of the bow and flies straight.</summary>
        public float[]? ArrowRest { get; }

        /// <summary>Bowstring tips in Blender prop-local space (metres). Unity draws the string as tip, nock, tip while nocked.</summary>
        public float[]? StringTop { get; }

        public float[]? StringBottom { get; }

        /// <summary>Prop material slot holding the static (braced) string; hidden in Unity, the drawn line replaces it.</summary>
        public string StringMaterial { get; }

        /// <summary>Attack track name of the nocked arrow.</summary>
        public string ArrowTrack { get; }

        public float NockFrame { get; }

        public float ReleaseFrame { get; }

        public bool HasDrawnString => StringTop != null && StringBottom != null && ArrowTrack.Length > 0 && NockFrame >= 0f;

        /// <summary>True while the arrow sits on the string (nock to release): the string is drawn to the nock.</summary>
        public bool Nocked(bool attack, float frame) => attack && HasDrawnString && frame >= NockFrame && frame < ReleaseFrame;

        /// <summary>Blender prop-local point to the imported prop root's local space (mirror X).</summary>
        public static float[] PropLocalToUnity(float[] p) => ToUnityPoint(p);

        public float RollDeg(bool attack, float frame)
        {
            float a = RollKeys[0], b = RollKeys[1], c = RollKeys[2], e = RollKeys[3];
            if (!attack || frame <= a || frame >= e)
            {
                return RollRestDeg;
            }

            if (frame < b)
            {
                return RollRestDeg + (RollDrawDeg - RollRestDeg) * Smooth((frame - a) / (b - a));
            }

            if (frame <= c)
            {
                return RollDrawDeg;
            }

            return RollDrawDeg + (RollRestDeg - RollDrawDeg) * Smooth((frame - c) / (e - c));
        }

        /// <summary>Roll for an extra bow clip (attacks 2 and 3): rest roll, eased to the draw roll and back once per shot.</summary>
        public float RollDeg(BlenderBowShots shots, float frame)
        {
            var roll = RollRestDeg;
            foreach (var s in shots.Shots)
            {
                if (frame > s.RollA && frame < s.RollB)
                {
                    roll = RollRestDeg + (RollDrawDeg - RollRestDeg) * Smooth((frame - s.RollA) / (s.RollB - s.RollA));
                }
                else if (frame >= s.RollB && frame <= s.RollC)
                {
                    roll = RollDrawDeg;
                }
                else if (frame > s.RollC && frame < s.RollE)
                {
                    roll = RollDrawDeg + (RollRestDeg - RollDrawDeg) * Smooth((frame - s.RollC) / (s.RollE - s.RollC));
                }
            }

            return roll;
        }

        private static float Smooth(float x)
        {
            x = Math.Max(0f, Math.Min(1f, x));
            return x * x * (3f - 2f * x);
        }

        /// <summary>Blender bone-space prop rotation for a roll: GripRot * Rz(roll), quaternion x, y, z, w.</summary>
        public float[] BlenderRotation(float rollDeg)
        {
            var h = rollDeg * Math.PI / 360.0;
            float rz = (float)Math.Sin(h), rw = (float)Math.Cos(h);
            float x = GripRot[0], y = GripRot[1], z = GripRot[2], w = GripRot[3];
            // (x,y,z,w) * (0,0,rz,rw)
            return new[] { x * rw + y * rz, y * rw - x * rz, z * rw + w * rz, w * rw - z * rz };
        }

        /// <summary>Unity bone-space prop rotation for a roll (mirror-X of <see cref="BlenderRotation"/>).</summary>
        public float[] UnityRotation(float rollDeg) => ToUnityRotation(BlenderRotation(rollDeg));

        public static float[] ToUnityPoint(float[] p) => new[] { -p[0], p[1], p[2] };

        public static float[] ToUnityRotation(float[] q) => new[] { q[0], -q[1], -q[2], q[3] };

        /// <summary>Imported-bone-space grip position, in the imported bone's units.</summary>
        public float[] UnityGripPos(float unitsPerMetre)
        {
            var p = ToUnityPoint(GripPos);
            return new[] { p[0] * unitsPerMetre, p[1] * unitsPerMetre, p[2] * unitsPerMetre };
        }

        public float[] UnityGripRot() => ToUnityRotation(GripRot);

        /// <summary>Distance (metres) between the grip centre and the closed-hand centre after the Unity axis conversion.</summary>
        public float GripToHandCentreUnity()
        {
            var a = ToUnityPoint(GripPos);
            var b = ToUnityPoint(HandCentre);
            var dx = a[0] - b[0];
            var dy = a[1] - b[1];
            var dz = a[2] - b[2];
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }

    /// <summary>One shot inside an extra bow clip, in clip frames (30 fps).</summary>
    public readonly struct BlenderBowShot
    {
        public BlenderBowShot(int arrowInHand, int nock, int release, int rollA, int rollB, int rollC, int rollE)
        {
            ArrowInHand = arrowInHand;
            Nock = nock;
            Release = release;
            RollA = rollA;
            RollB = rollB;
            RollC = rollC;
            RollE = rollE;
        }

        /// <summary>The arrow appears in the draw hand (from the quiver, or nocking).</summary>
        public int ArrowInHand { get; }

        /// <summary>On the string from here: the arrow is aimed from its nock through the arrow rest beside the riser.</summary>
        public int Nock { get; }

        /// <summary>Loose: the held arrow goes, the battle's flight streak takes over.</summary>
        public int Release { get; }

        public int RollA { get; }
        public int RollB { get; }
        public int RollC { get; }
        public int RollE { get; }
    }

    /// <summary>
    /// Bow behaviour for an extra (Mixamo) archer clip: the bow stays in the locked LeftHand grip and only rolls in it
    /// (<see cref="BlenderBoneGrip.RollDeg(BlenderBowShots, float)"/>); the attack's held arrow track rides the draw hand and
    /// is aimed through <see cref="ArrowRest"/> (Blender prop-local metres) while nocked.
    /// </summary>
    public sealed class BlenderBowShots
    {
        public BlenderBowShots(string arrowTrack, float[] arrowRest, BlenderBowShot[] shots)
        {
            ArrowTrack = arrowTrack ?? string.Empty;
            ArrowRest = arrowRest ?? throw new ArgumentNullException(nameof(arrowRest));
            Shots = shots ?? Array.Empty<BlenderBowShot>();
        }

        public string ArrowTrack { get; }
        public float[] ArrowRest { get; }
        public BlenderBowShot[] Shots { get; }

        public bool ArrowHeld(float frame)
        {
            foreach (var s in Shots)
            {
                if (frame >= s.ArrowInHand && frame < s.Release)
                {
                    return true;
                }
            }

            return false;
        }

        public bool Nocked(float frame)
        {
            foreach (var s in Shots)
            {
                if (frame >= s.Nock && frame < s.Release)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Extra clip that carries an attack's held prop and turns it in the grip (2026-10-18, Oakenshield attack 2 spear thrust).
    /// The prop rides its bone at the attack binding (hand and arm untouched); from <see cref="Keys"/>[0] it turns about the grip
    /// point (minimal rotation, smoothstep weight) from that orientation to the two-hand line, point leading (prop root +Z in
    /// Unity, as at the attack's release) toward the front palm (<see cref="FrontBone"/> plus <see cref="PalmFraction"/> of the
    /// forearm beyond it), full at Keys[1], held to Keys[2], back by Keys[3]. With the turn it slides <see cref="SlideMetres"/>
    /// forward through the fist so the butt clears the rear forearm. <see cref="ShowKeys"/> conjure it in and out (scale).
    /// </summary>
    public sealed class BlenderHeldRealign
    {
        public const float HiddenScale = 0.05f;

        public BlenderHeldRealign(
            string trackName,
            string frontBone,
            string frontParentBone,
            float palmFraction,
            float[] keys,
            float slideMetres,
            float forearmMetres,
            float[] showKeys)
        {
            TrackName = trackName ?? throw new ArgumentNullException(nameof(trackName));
            FrontBone = frontBone ?? throw new ArgumentNullException(nameof(frontBone));
            FrontParentBone = frontParentBone ?? throw new ArgumentNullException(nameof(frontParentBone));
            PalmFraction = palmFraction;
            Keys = keys is { Length: 4 } ? keys : throw new ArgumentException("four keys", nameof(keys));
            SlideMetres = slideMetres;
            ForearmMetres = forearmMetres;
            ShowKeys = showKeys is { Length: 4 } ? showKeys : throw new ArgumentException("four show keys", nameof(showKeys));
        }

        public string TrackName { get; }
        public string FrontBone { get; }
        public string FrontParentBone { get; }
        public float PalmFraction { get; }
        public float[] Keys { get; }
        public float SlideMetres { get; }
        public float ForearmMetres { get; }
        public float[] ShowKeys { get; }

        /// <summary>0 = the attack grip orientation, 1 = the two-hand line.</summary>
        public float Weight(float frame)
        {
            float a = Keys[0], b = Keys[1], c = Keys[2], e = Keys[3];
            if (frame <= a || frame >= e)
            {
                return 0f;
            }

            if (frame < b)
            {
                return Smooth((frame - a) / (b - a));
            }

            return frame <= c ? 1f : 1f - Smooth((frame - c) / (e - c));
        }

        /// <summary>Prop scale: conjured in over ShowKeys[0..1], gone over ShowKeys[2..3].</summary>
        public float Scale(float frame)
        {
            float a = ShowKeys[0], b = ShowKeys[1], c = ShowKeys[2], e = ShowKeys[3];
            float u;
            if (frame <= a || frame >= e)
            {
                u = 0f;
            }
            else if (frame < b)
            {
                u = Smooth((frame - a) / (b - a));
            }
            else
            {
                u = frame <= c ? 1f : 1f - Smooth((frame - c) / (e - c));
            }

            return HiddenScale + ((1f - HiddenScale) * u);
        }

        public bool Visible(float frame) => frame > ShowKeys[0] && frame < ShowKeys[3];

        private static float Smooth(float t)
        {
            t = t < 0f ? 0f : t > 1f ? 1f : t;
            return t * t * (3f - (2f * t));
        }
    }
}
