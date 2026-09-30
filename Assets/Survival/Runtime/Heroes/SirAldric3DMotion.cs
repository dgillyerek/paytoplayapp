using System;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Animator-ready 3D bone eulers for Sir Aldric.
    /// Unity Y-up. Character forward / march = world +Z = TOP of the high-angle rear Game view
    /// (camera sits at −Z, looking toward +Z; enemy is up-screen).
    /// Thigh −X swings the foot toward +Z (TOP); +X swings toward the camera (down-screen).
    /// Arm/forearm −X swings the hand/blade in front of the body toward +Z (TOP / enemy);
    /// +X hangs them toward the camera (FAIL). Character-right = +X = viewer-right from behind.
    /// </summary>
    public static class SirAldric3DMotion
    {
        public const float WalkPeriodSeconds = 1.00f;
        public const int WalkCyclesBeforeAttack = 2;
        public const float AttackSeconds = 1.40f;
        public const float MarchMetersPerSecond = 0.80f;

        /// <summary>
        /// AccuRIG / Mixamo instantiate face-to-camera (−Z). Yaw so forward = +Z (TOP).
        /// Do not scale-X mirror: that put the sword on the left hand (Derek 212c6da FAIL).
        /// RH grip stays mixamorig:RightHand. Play-cam arc is attack-only lift + sword aim.
        /// </summary>
        public const float MixamoImportRearYawDegrees = 180f;

        /// <summary>
        /// Design-owned DIAG_MIRR Inward Slash take window (EXPORT_DIAG_MIRR.md).
        /// Full authored clip 0–85 @ 30fps. Mixamo Mirror ON in the FBX — not a
        /// Unity scale.x flip. Not DIAG 72412be4 / MILD 8d5b78b0 / baseline 4a143441.
        /// </summary>
        public const int MixamoSlashFirstFrame = 0;
        public const int MixamoSlashLastFrame = 85;

        /// <summary>
        /// Derek slash SoT: https://www.youtube.com/watch?v=iQ1s3nN1330
        /// (Judith Hamma “Sword Swing - Animation”, Maya, 20s orbit).
        /// Storyboard silhouette, not a prior UR→LL guess:
        /// ready = RH low at the right hip, slightly back (maxres / t≈0);
        /// backswing = sword far behind the body (t≈5s side profile);
        /// high = lift over the right shoulder; contact = in front;
        /// finish = low character-left. Mixamo Inward Slash cannot do this
        /// wide backswing — strike is tip-led onto these keys.
        /// </summary>
        public const float AttackSlashReadyRight = 0.42f;
        public const float AttackSlashReadyUp = -0.48f;
        public const float AttackSlashReadyFront = -0.22f;
        public const float AttackSlashBackRight = 0.55f;
        public const float AttackSlashBackUp = 0.05f;
        public const float AttackSlashBackFront = -0.78f;
        public const float AttackSlashBackU = 0.22f;
        public const float AttackSlashUrRight = 0.68f;
        public const float AttackSlashUrUp = 0.82f;
        public const float AttackSlashUrFront = -0.12f;
        public const float AttackSlashFrontRight = 0.08f;
        public const float AttackSlashFrontUp = 0.06f;
        public const float AttackSlashFrontZ = 0.72f;
        public const float AttackSlashLlRight = -0.76f;
        public const float AttackSlashLlUp = -0.80f;
        public const float AttackSlashLlFront = 0.16f;
        public const float AttackSlashFrontU = 0.40f;
        public const float AttackSlashFrontHoldU = 0.58f;
        public const float AttackSlashLlU = 0.84f;
        public const float AttackSlashGuardRight = 0.12f;
        public const float AttackSlashGuardUp = 0.22f;
        public const float AttackSlashGuardFront = 0.90f;
        public const float AttackSlashSpineYawReady = 22f;
        public const float AttackSlashSpineYawFinish = -16f;
        public const float HeldSwordRestEulerX = 90f;
        /// <summary>
        /// Walk hip-sheath in character space after FaceWorldTop (yaw 180):
        /// +X = character-right, +Y = up, +Z = forward / enemy. Never raw world
        /// ±X — that locked the blade world-left across the neck (Derek FAIL
        /// cdfbea5 Game-view) while rematch hid it. Grip on the outside of the
        /// right hip; tip out/down beside the right leg. Strike = RightHand only.
        /// </summary>
        public const float HipSheathOutboard = 0.32f;
        public const float HipSheathUp = 0.02f;
        public const float HipSheathBack = 0.06f;
        public const float HeldSwordSheathRight = 0.40f;
        public const float HeldSwordSheathDown = 1.00f;
        public const float HeldSwordSheathBack = 0.00f;
        public const float PlayCamOrbitYawSpeed = 90f;
        public const float PlayCamOrbitPitchMin = -25f;
        public const float PlayCamOrbitPitchMax = 35f;

        /// <summary>
        /// Character-space grip offset (right, up, forward) after FaceWorldTop.
        /// </summary>
        public static void HipSheathGripLocal(out float right, out float up, out float forward)
        {
            right = HipSheathOutboard;
            up = HipSheathUp;
            forward = -HipSheathBack;
        }

        /// <summary>
        /// Character-space blade aim (right, up, forward). Dominant down + outboard.
        /// No world-left. No world-back. Unity bake blade is local +Y.
        /// </summary>
        public static void HipSheathBladeLocal(out float right, out float up, out float forward)
        {
            right = HeldSwordSheathRight;
            up = -HeldSwordSheathDown;
            forward = -HeldSwordSheathBack;
            var mag = MathF.Sqrt((right * right) + (up * up) + (forward * forward));
            if (mag < 1e-5f)
            {
                right = 0.35f;
                up = -1f;
                forward = 0f;
                return;
            }

            right /= mag;
            up /= mag;
            forward /= mag;
        }

        /// <summary>
        /// Yaw from the default rear Play-cam to the authored 3/4 eye, around LookAt.
        /// </summary>
        public static float PlayCamThreeQuarterYawDegrees()
        {
            var rearX = PlayCamRearX - PlayCamLookX;
            var rearZ = PlayCamRearZ - PlayCamLookZ;
            var tqX = PlayCamThreeQuarterX - PlayCamLookX;
            var tqZ = PlayCamThreeQuarterZ - PlayCamLookZ;
            var deg = (MathF.Atan2(tqX, tqZ) - MathF.Atan2(rearX, rearZ)) * (180f / MathF.PI);
            if (deg > 180f)
            {
                deg -= 360f;
            }

            if (deg < -180f)
            {
                deg += 360f;
            }

            return deg;
        }

        /// <summary>
        /// Orbit the default rear Play-cam around LookAt. yaw/pitch 0 = rear SoT.
        /// </summary>
        public static void PlayCamOrbitEye(float yawDegrees, float pitchDegrees, out float x, out float y, out float z)
        {
            var lookX = PlayCamLookX;
            var lookY = PlayCamLookY;
            var lookZ = PlayCamLookZ;
            var ox = PlayCamRearX - lookX;
            var oy = PlayCamRearY - lookY;
            var oz = PlayCamRearZ - lookZ;
            var yaw = yawDegrees * (MathF.PI / 180f);
            var cos = MathF.Cos(yaw);
            var sin = MathF.Sin(yaw);
            var rx = (ox * cos) + (oz * sin);
            var rz = (-ox * sin) + (oz * cos);
            var pitch = pitchDegrees * (MathF.PI / 180f);
            var pc = MathF.Cos(pitch);
            var ps = MathF.Sin(pitch);
            var horiz = MathF.Sqrt((rx * rx) + (rz * rz));
            var ny = (oy * pc) - (horiz * ps);
            var nh = (oy * ps) + (horiz * pc);
            if (horiz > 1e-5f)
            {
                rx *= nh / horiz;
                rz *= nh / horiz;
            }

            x = lookX + rx;
            y = lookY + ny;
            z = lookZ + rz;
        }

        /// <summary>
        /// Play-cam SoT for SirAldricDemo / 1080×1920 Game-view Scale 1×. View +Z = TOP.
        /// Derek 063d928: FOV 34 at (0, 1.75, −2.90) filled the knight but clipped the
        /// overhead tip and finish. FOV 42 + slight pullback keeps the 1.8 m knight large
        /// and the full sword arc in frame.
        /// </summary>
        public const float PlayCamFovDegrees = 42f;
        public const float PlayCamRearX = 0f;
        public const float PlayCamRearY = 1.92f;
        public const float PlayCamRearZ = -3.08f;
        public const float PlayCamLookX = 0f;
        public const float PlayCamLookY = 1.12f;
        public const float PlayCamLookZ = 0.15f;
        public const float PlayCamFrontX = 0f;
        public const float PlayCamFrontY = 1.92f;
        public const float PlayCamFrontZ = 3.38f;
        public const float PlayCamThreeQuarterX = 1.82f;
        public const float PlayCamThreeQuarterY = 1.92f;
        public const float PlayCamThreeQuarterZ = -2.38f;

        public static float WalkBlockSeconds => WalkPeriodSeconds * WalkCyclesBeforeAttack;

        public static float LoopSeconds => WalkBlockSeconds + AttackSeconds;

        public readonly struct Euler
        {
            public Euler(float x, float y, float z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public float X { get; }
            public float Y { get; }
            public float Z { get; }
        }

        public readonly struct Pose
        {
            public Pose(
                bool attacking,
                bool swordDrawn,
                float rootZ,
                float rootY,
                Euler hips,
                Euler spine,
                Euler chest,
                Euler head,
                Euler upLegL,
                Euler legL,
                Euler footL,
                Euler upLegR,
                Euler legR,
                Euler footR,
                Euler armL,
                Euler foreL,
                Euler armR,
                Euler foreR,
                Euler handR,
                Euler sword)
            {
                Attacking = attacking;
                SwordDrawn = swordDrawn;
                RootZ = rootZ;
                RootY = rootY;
                Hips = hips;
                Spine = spine;
                Chest = chest;
                Head = head;
                UpLegL = upLegL;
                LegL = legL;
                FootL = footL;
                UpLegR = upLegR;
                LegR = legR;
                FootR = footR;
                ArmL = armL;
                ForeL = foreL;
                ArmR = armR;
                ForeR = foreR;
                HandR = handR;
                Sword = sword;
            }

            public bool Attacking { get; }
            public bool SwordDrawn { get; }
            public float RootZ { get; }
            public float RootY { get; }
            public Euler Hips { get; }
            public Euler Spine { get; }
            public Euler Chest { get; }
            public Euler Head { get; }
            public Euler UpLegL { get; }
            public Euler LegL { get; }
            public Euler FootL { get; }
            public Euler UpLegR { get; }
            public Euler LegR { get; }
            public Euler FootR { get; }
            public Euler ArmL { get; }
            public Euler ForeL { get; }
            public Euler ArmR { get; }
            public Euler ForeR { get; }
            public Euler HandR { get; }
            public Euler Sword { get; }

            /// <summary>Hips stay squared to world +Z (screen TOP), not yawed to a side. |Z| allows a readable hip drop.</summary>
            public bool FacesTop => Math.Abs(Hips.Y) < 18f && Math.Abs(Hips.Z) < 18f;

            /// <summary>Walk/sheath: right hand stays near the hip (pendulum may go slightly +X), scabbard on +X.</summary>
            public bool SheathedOnCharacterRight => !SwordDrawn && ArmR.X > -50f && ArmR.X < 28f;

            /// <summary>
            /// Sword / right arm stay on the far / TOP side as a rest — not a dual hang toward camera.
            /// Walk allows the trailing arm to swing +X (contralateral pendulum); attack keeps both −X.
            /// </summary>
            public bool ArmsTowardTop =>
                SwordDrawn
                    ? ArmL.X < 0f && ArmR.X < 0f && ForeL.X <= 0f && ForeR.X < 0f
                    : ArmR.X < 28f && ForeR.X < 5f;

            /// <summary>Loose contralateral pendulum: one arm back (+X / camera), the other forward (−X / TOP).</summary>
            public bool WalkArmPendulum =>
                !Attacking
                && ((ArmL.X > 8f && ArmR.X < -4f) || (ArmL.X < -6f && ArmR.X > 4f));

            /// <summary>Right arm / blade swinging toward world +Z (TOP of rear camera).</summary>
            public bool StrikeTowardTop => SwordDrawn && ArmR.X <= -80f && ArmR.X >= -175f;

            /// <summary>Thigh −X = foot toward +Z = TOP. Used to reject moonwalk / down-screen stride.</summary>
            public bool LeadLegTowardTop => UpLegL.X < -8f || UpLegR.X < -8f;

            /// <summary>Passing knee reads from high-rear (euler 52–82). Steeper camera needs the top of this band.</summary>
            public bool PassingKneeBent =>
                (LegL.X >= 52f && LegL.X <= 82f) || (LegR.X >= 52f && LegR.X <= 82f);

            /// <summary>Spine yaws opposite the pelvis (shoulder–hip counter-rotation).</summary>
            public bool ShoulderHipCounter =>
                Math.Abs(Hips.Y) < 2f || Math.Sign(Spine.Y) == -Math.Sign(Hips.Y);

            /// <summary>Pelvis rolls so the unweighted / passing hip drops.</summary>
            public bool HipDropOnPass => Math.Abs(Hips.Z) >= 5f;

            /// <summary>No whole-body left↔right weave. Small hip drop / yaw is OK; torso stays on +Z.</summary>
            public bool StraightTrack => Math.Abs(Hips.Y) <= 8f && Math.Abs(Hips.Z) <= 6f;
        }

        public static Pose Evaluate(float timeSeconds)
        {
            var loopT = Repeat(timeSeconds, LoopSeconds);
            var attacking = loopT >= WalkBlockSeconds;
            var rootZ = loopT * MarchMetersPerSecond;
            return attacking
                ? AttackPose(loopT - WalkBlockSeconds, rootZ)
                : WalkPose(loopT, rootZ);
        }

        public static bool IsAttacking(float timeSeconds) =>
            Repeat(timeSeconds, LoopSeconds) >= WalkBlockSeconds;

        // Four Game-view keys solved against WALK_GAIT_BAR rear poses on this hang −Y rig.
        // 14d9c17 kicked the calves toward the camera: pass thigh +22 stacked on knee +X
        // so the swing foot's world Z went negative (back-kick / −Z). Pass thigh is now
        // −X (toward TOP) so the tucked foot travels +Z; trail stays a small +X toe-off.
        // a6d4703 still kicked the pass foot out: stance abduct + Hips.Z=14 leaned the
        // silhouette into a side-kick. Pass tucks under the pelvis (Up*.Z ~ 0).
        // Contact lead/trail ±12 keeps step ≈ 0.40 m so both plants stay visible.
        // ff81201 weaved left↔right: Hips.Z ±8 plus only 28% spine counter tipped the
        // torso ~11 cm each side. Hip drop stays small (±5.5); spine Z fully counters
        // the roll so COM stays on world +Z. Yaw damped. Root X = 0.
        // u=0 pass L, 0.25 contact L, 0.50 pass R, 0.75 contact R.
        private static readonly float[] HipsY = { -3f, 5f, 3f, -5f };
        private static readonly float[] HipsZ = { 5.5f, 1.5f, -5.5f, 1.5f };
        private static readonly float[] SpineY = { 8f, -8f, -8f, 8f };
        private static readonly float[] UpLX = { -18f, -12f, 8f, 12f };
        private static readonly float[] UpLZ = { 0f, 0f, 8f, 0f };
        private static readonly float[] LegL = { 80f, 12f, 14f, 18f };
        private static readonly float[] FootL = { 16f, -12f, -6f, 14f };
        private static readonly float[] UpRX = { 8f, 12f, -18f, -12f };
        private static readonly float[] UpRZ = { -8f, 0f, 0f, 0f };
        private static readonly float[] LegR = { 14f, 16f, 80f, 12f };
        private static readonly float[] FootR = { -6f, 14f, 16f, -12f };
        private static readonly float[] ArmLX = { 36f, 32f, -32f, -34f };
        private static readonly float[] ArmRX = { -32f, -34f, 24f, 22f };

        private static Pose WalkPose(float loopT, float rootZ)
        {
            var u = Repeat(loopT / WalkPeriodSeconds, 1f);
            var hipsY = Sample(HipsY, u);
            var hipsZ = Sample(HipsZ, u);
            var spineY = Sample(SpineY, u);
            var armLX = Sample(ArmLX, u);
            var armRX = Sample(ArmRX, u);
            var bob = 0.012f + 0.018f * Math.Abs(MathF.Cos(u * (float)Math.PI * 2f));
            return new Pose(
                attacking: false,
                swordDrawn: false,
                rootZ,
                bob,
                hips: new Euler(0f, hipsY, hipsZ),
                spine: new Euler(5f, spineY, -hipsZ),
                chest: new Euler(2f, spineY * 0.5f, 0f),
                head: new Euler(6f, spineY * 0.25f, 0f),
                upLegL: new Euler(Sample(UpLX, u), 0f, Sample(UpLZ, u)),
                legL: new Euler(Sample(LegL, u), 0f, 0f),
                footL: new Euler(Sample(FootL, u), 0f, 0f),
                upLegR: new Euler(Sample(UpRX, u), 0f, Sample(UpRZ, u)),
                legR: new Euler(Sample(LegR, u), 0f, 0f),
                footR: new Euler(Sample(FootR, u), 0f, 0f),
                armL: new Euler(armLX, 0f, -22f),
                foreL: new Euler(-18f, 0f, 0f),
                armR: new Euler(armRX, 4f, 22f),
                foreR: new Euler(-22f, 0f, 0f),
                handR: new Euler(0f, 0f, 0f),
                sword: new Euler(-6f, 0f, 8f));
        }

        private static float Sample(float[] keys, float u)
        {
            var n = keys.Length;
            var x = Repeat(u, 1f) * n;
            var i0 = (int)MathF.Floor(x) % n;
            if (i0 < 0)
            {
                i0 += n;
            }

            var i1 = (i0 + 1) % n;
            var t = x - MathF.Floor(x);
            return Lerp(keys[i0], keys[i1], t);
        }

        private static Pose AttackPose(float attackT, float rootZ)
        {
            var u = Clamp01(attackT / AttackSeconds);
            float armX;
            float armY;
            float foreX;
            float lunge;
            float spineX;
            float drawnK;
            if (u < 0.18f)
            {
                var k = Smooth01(u / 0.18f);
                armX = Lerp(-16f, -85f, k);
                armY = Lerp(6f, 2f, k);
                foreX = Lerp(-22f, -8f, k);
                lunge = 0f;
                spineX = Lerp(4f, 10f, k);
                drawnK = k;
            }
            else if (u < 0.40f)
            {
                var k = Smooth01((u - 0.18f) / 0.22f);
                armX = Lerp(-85f, -155f, k);
                armY = Lerp(2f, 0f, k);
                foreX = Lerp(-8f, -28f, k);
                lunge = Lerp(0f, 0.04f, k);
                spineX = Lerp(10f, 14f, k);
                drawnK = 1f;
            }
            else if (u < 0.56f)
            {
                var k = Smooth01((u - 0.40f) / 0.16f);
                armX = Lerp(-155f, -118f, k);
                armY = Lerp(0f, 0f, k);
                foreX = Lerp(-28f, -6f, k);
                lunge = Lerp(0.04f, 0.10f, k);
                spineX = Lerp(14f, 6f, k);
                drawnK = 1f;
            }
            else if (u < 0.78f)
            {
                var k = Smooth01((u - 0.56f) / 0.22f);
                armX = Lerp(-118f, -48f, k);
                armY = Lerp(0f, 4f, k);
                foreX = Lerp(-6f, -16f, k);
                lunge = Lerp(0.10f, 0.02f, k);
                spineX = Lerp(6f, 2f, k);
                drawnK = 1f - k * 0.35f;
            }
            else
            {
                var k = Smooth01((u - 0.78f) / 0.22f);
                armX = Lerp(-48f, -16f, k);
                armY = Lerp(4f, 6f, k);
                foreX = Lerp(-16f, -22f, k);
                lunge = Lerp(0.02f, 0f, k);
                spineX = Lerp(2f, 4f, k);
                drawnK = 1f - k;
            }

            var drawn = drawnK > 0.22f;
            return new Pose(
                attacking: true,
                swordDrawn: drawn,
                rootZ,
                lunge,
                hips: new Euler(lunge * 20f, 0f, 0f),
                spine: new Euler(spineX, 0f, 0f),
                chest: new Euler(spineX * 0.4f, 0f, 0f),
                head: new Euler(8f, 0f, 0f),
                upLegL: new Euler(8f, 0f, 0f),
                legL: new Euler(12f, 0f, 0f),
                footL: new Euler(-6f, 0f, 0f),
                upLegR: new Euler(-6f, 0f, 0f),
                legR: new Euler(16f, 0f, 0f),
                footR: new Euler(-4f, 0f, 0f),
                armL: new Euler(-22f, 0f, 10f),
                foreL: new Euler(-18f, 0f, 0f),
                armR: new Euler(armX, armY, -8f),
                foreR: new Euler(foreX, 0f, 0f),
                handR: new Euler(drawn ? -12f : 0f, 0f, 0f),
                sword: new Euler(drawn ? -10f : -6f, 0f, drawn ? 0f : 8f));
        }

        public static float Repeat(float t, float length)
        {
            if (length <= 0f)
            {
                return 0f;
            }

            var r = t % length;
            return r < 0f ? r + length : r;
        }

        /// <summary>
        /// 1 for the whole Mixamo slash. Walk↔slash is the mixer attackWeight only.
        /// </summary>
        public static float AttackWindupWeight(float attackNormalized01)
        {
            _ = attackNormalized01;
            return 1f;
        }

        /// <summary>
        /// Unit RH reach for YouTube iQ1s3nN1330 (storyboard keys).
        /// u=0 ready low-right-back, u=BackU far behind, u=FrontU high-right,
        /// u=FrontHoldU contact in front, u≥LlU low-left.
        /// </summary>
        public static void AttackSlashReach(float attackNormalized01, out float x, out float y, out float z)
        {
            var u = Clamp01(attackNormalized01);
            if (u <= AttackSlashBackU)
            {
                SampleSlash(
                    u, 0f, AttackSlashBackU,
                    AttackSlashReadyRight, AttackSlashReadyUp, AttackSlashReadyFront,
                    AttackSlashBackRight, AttackSlashBackUp, AttackSlashBackFront,
                    out x, out y, out z);
            }
            else if (u <= AttackSlashFrontU)
            {
                SampleSlash(
                    u, AttackSlashBackU, AttackSlashFrontU,
                    AttackSlashBackRight, AttackSlashBackUp, AttackSlashBackFront,
                    AttackSlashUrRight, AttackSlashUrUp, AttackSlashUrFront,
                    out x, out y, out z);
            }
            else if (u <= AttackSlashFrontHoldU)
            {
                SampleSlash(
                    u, AttackSlashFrontU, AttackSlashFrontHoldU,
                    AttackSlashUrRight, AttackSlashUrUp, AttackSlashUrFront,
                    AttackSlashFrontRight, AttackSlashFrontUp, AttackSlashFrontZ,
                    out x, out y, out z);
            }
            else if (u <= AttackSlashLlU)
            {
                SampleSlash(
                    u, AttackSlashFrontHoldU, AttackSlashLlU,
                    AttackSlashFrontRight, AttackSlashFrontUp, AttackSlashFrontZ,
                    AttackSlashLlRight, AttackSlashLlUp, AttackSlashLlFront,
                    out x, out y, out z);
            }
            else
            {
                x = AttackSlashLlRight;
                y = AttackSlashLlUp;
                z = AttackSlashLlFront;
            }

            Normalize(ref x, ref y, ref z);
        }

        /// <summary>
        /// Left-hand point from the video ready / 3/4 maxres (toward the enemy).
        /// </summary>
        public static void AttackSlashGuardReach(out float x, out float y, out float z)
        {
            x = AttackSlashGuardRight;
            y = AttackSlashGuardUp;
            z = AttackSlashGuardFront;
            Normalize(ref x, ref y, ref z);
        }

        public static float AttackSlashSpineYawDegrees(float attackNormalized01)
        {
            return Lerp(AttackSlashSpineYawReady, AttackSlashSpineYawFinish, Clamp01(attackNormalized01));
        }

        private static void SampleSlash(
            float u,
            float u0,
            float u1,
            float ax,
            float ay,
            float az,
            float bx,
            float by,
            float bz,
            out float x,
            out float y,
            out float z)
        {
            var span = u1 - u0;
            var t = span > 1e-5f ? Smooth01((u - u0) / span) : 1f;
            x = Lerp(ax, bx, t);
            y = Lerp(ay, by, t);
            z = Lerp(az, bz, t);
        }

        private static void Normalize(ref float x, ref float y, ref float z)
        {
            var mag = MathF.Sqrt((x * x) + (y * y) + (z * z));
            if (mag < 1e-5f)
            {
                x = 0f;
                y = 0f;
                z = 1f;
                return;
            }

            x /= mag;
            y /= mag;
            z /= mag;
        }

        public static float Clamp01(float x)
        {
            if (x < 0f)
            {
                return 0f;
            }

            return x > 1f ? 1f : x;
        }

        public static float Lerp(float a, float b, float t) => a + (b - a) * t;

        public static float Smooth01(float x)
        {
            x = Clamp01(x);
            return x * x * (3f - 2f * x);
        }
    }
}
