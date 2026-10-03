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
        /// DIAG Inward Slash take window (EXPORT_DIAG.md) — last Mixamo clip
        /// that actually played in Game-view (tips 95b5a4a / 43b33fb, md5 72412be4).
        /// PlayableGraph plays this Mixamo take (human arm/shoulder/torso).
        /// Leftover AttackRaiseThenCutReach unused after Derek FAIL 95aba89. DIAG_REV
        /// 0beb3c77 time-reverse bake (e90b654) showed no attack. Not Mixamo
        /// Mirror (DIAG_MIRR 70fd9483, Derek reject 19e6efe). Not MILD 8d5b78b0
        /// / baseline 4a143441. Not a Unity scale.x flip. Not the 7958283 held-pose clip.
        /// </summary>
        public const int MixamoSlashFirstFrame = 0;
        public const int MixamoSlashLastFrame = 85;
        /// <summary>
        /// Design Sword And Shield Slash (md5 cc4f97a3). 74 frames @ 30 FPS.
        /// Not the 53-frame Sword And Shield Attack. Not leftover DIAG 0–85.
        /// </summary>
        public const int MixamoSwordShieldSlashFirstFrame = 0;
        public const int MixamoSwordShieldSlashLastFrame = 73;
        public const float MixamoSwordShieldSlashFps = 30f;
        public const string MixamoSwordShieldSlashMd5 = "cc4f97a3ec981d0ece2780aaec49e1a4";

        /// <summary>
        /// Lite Sword And Shield Pack (17). Same Mixamo auto-rig as Pro
        /// Sword And Shield Slash (md5 cc4f97a3). Play on the slash avatar,
        /// not the walk avatar. Lookup key = exact Mixamo catalog name
        /// (parentheses and capitalization). Do not rename the clips.
        /// </summary>
        public const string LiteSwordShieldThemePackDir =
            "ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot/lite_sword_shield";
        public const int LiteSwordShieldClipCount = 17;

        public readonly struct LiteSwordShieldClip
        {
            public LiteSwordShieldClip(string exactName, string fileName, string md5, bool loop)
            {
                ExactName = exactName;
                FileName = fileName;
                Md5 = md5;
                Loop = loop;
            }

            public string ExactName { get; }
            public string FileName { get; }
            public string Md5 { get; }
            public bool Loop { get; }
            public string ThemePackRel => LiteSwordShieldThemePackDir + "/" + FileName;
        }

        public static readonly LiteSwordShieldClip[] LiteSwordShieldClips =
        {
            new("Draw Sword 1", "draw_sword_1.fbx", "8e72ecbdd39e5a2ef30476687f380f27", false),
            new("Sheath Sword 1", "sheath_sword_1.fbx", "1efefb69b6461a0d48cf5c8912f7b2d1", false),
            new("Sword And Shield attack", "sword_and_shield_attack.fbx", "a07b23ac21d3f9821069b518a43cfe43", false),
            new("Sword And Shield attack (2)", "sword_and_shield_attack_(2).fbx", "ccf7bf077754c28b5476ab619f502864", false),
            new("Sword And Shield attack (3)", "sword_and_shield_attack_(3).fbx", "fbc9c03fafa4762a2122f8704d7b768d", false),
            new("Sword And Shield attack (4)", "sword_and_shield_attack_(4).fbx", "2812b491741d1104942e2597334a6cce", false),
            new("Sword And Shield block", "sword_and_shield_block.fbx", "6ac198628aa941677126071bbc45a207", false),
            new("Sword And Shield block (2)", "sword_and_shield_block_(2).fbx", "d1ef63c7a51144128cdb02657ea5fbca", false),
            new("Sword And Shield block idle", "sword_and_shield_block_idle.fbx", "a32eb3a98442b0ef722838af7dedc50a", true),
            new("Sword And Shield death", "sword_and_shield_death.fbx", "6fdab61142196017047f56547bcdbd3c", false),
            new("Sword And Shield idle", "sword_and_shield_idle.fbx", "3d4658649d3c0a7723553df64ddc0469", true),
            new("Sword And Shield run", "sword_and_shield_run.fbx", "5b042626753fec74b1e4d5b5b9a571eb", true),
            new("Sword And Shield run (2)", "sword_and_shield_run_(2).fbx", "4f2dccb0f8c7e26e4f2dcddace48553c", true),
            new("Sword And Shield strafe", "sword_and_shield_strafe.fbx", "78e0cfcfa06cd7eaea2b1b0122c17531", true),
            new("Sword And Shield strafe (2)", "sword_and_shield_strafe_(2).fbx", "b000466170975b7a388b9def9df680fd", true),
            new("Sword And Shield turn", "sword_and_shield_turn.fbx", "9a44ad26fc7458be8b2100bba9814f4e", false),
            new("Sword And Shield turn (2)", "sword_and_shield_turn_(2).fbx", "c531ea9ecae863e04819275ed199f373", false),
        };

        public static bool TryLiteSwordShield(string exactName, out LiteSwordShieldClip clip)
        {
            foreach (var candidate in LiteSwordShieldClips)
            {
                if (string.Equals(candidate.ExactName, exactName, StringComparison.Ordinal))
                {
                    clip = candidate;
                    return true;
                }
            }

            clip = default;
            return false;
        }

        public static string? LiteSwordShieldExactNameForFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            var leaf = fileName;
            var slash = leaf.LastIndexOf('/');
            if (slash >= 0)
            {
                leaf = leaf[(slash + 1)..];
            }

            var bslash = leaf.LastIndexOf('\\');
            if (bslash >= 0)
            {
                leaf = leaf[(bslash + 1)..];
            }

            foreach (var candidate in LiteSwordShieldClips)
            {
                if (string.Equals(candidate.FileName, leaf, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate.ExactName;
                }
            }

            return null;
        }
        /// <summary>DIAG ready ~f9/85. Sword on LEFT hip until this u, then RH.</summary>
        public const float MixamoDiagDrawEndU = 0.18f;
        /// <summary>
        /// EXPORT_DIAG.md TOP f59/85 — start of the descending cut. Linear
        /// clip time before this is the long chamber/rise (Derek 1821c4a:
        /// strike read as UPWARDS).
        /// </summary>
        public const float MixamoDiagApexClipU = 59f / 85f;
        /// <summary>
        /// Attack play-head when raise finishes. DIAG rise is ~69% of the
        /// take; compress it so the strike is the descent (f59–85), not the
        /// chamber. Not a Mixamo Mirror. Not DIAG_REV.
        /// </summary>
        public const float MixamoDiagRaisePlayEndU = 0.38f;

        /// <summary>
        /// Map attack play-head onto Mixamo DIAG clip time. Draw stays 1:1,
        /// raise is compressed onto clip 0.18–apex, strike is stretched onto
        /// the descending frames (apex–1). Monotonic. Never reverses the take.
        /// </summary>
        public static float MixamoDiagPlaybackU(float attackNormalized01)
        {
            var u = Clamp01(attackNormalized01);
            if (u <= MixamoDiagDrawEndU)
            {
                return u;
            }

            if (u <= MixamoDiagRaisePlayEndU)
            {
                var t = (u - MixamoDiagDrawEndU) / (MixamoDiagRaisePlayEndU - MixamoDiagDrawEndU);
                return Lerp(MixamoDiagDrawEndU, MixamoDiagApexClipU, t);
            }

            var s = (u - MixamoDiagRaisePlayEndU) / (1f - MixamoDiagRaisePlayEndU);
            return Lerp(MixamoDiagApexClipU, 1f, s);
        }

        /// <summary>
        /// Character-local blade during the strike: start FORWARD (+Z), finish
        /// DOWN toward the standing foot (−Y). Never +Y (1821c4a FAIL).
        /// </summary>
        public static void MixamoDiagStrikeBladeLocal(float attackNormalized01, out float x, out float y, out float z)
        {
            var u = Clamp01(attackNormalized01);
            var k = 0f;
            if (u > MixamoDiagRaisePlayEndU)
            {
                k = Smooth01((u - MixamoDiagRaisePlayEndU) / (1f - MixamoDiagRaisePlayEndU));
            }

            var down = Lerp(0.28f, 0.88f, k);
            var fwd = Lerp(0.90f, 0.35f, k);
            x = 0f;
            y = -down;
            z = fwd;
            Normalize(ref x, ref y, ref z);
        }

        /// <summary>
        /// Scene-marker strike path (Derek rejected another Mixamo DIAG tweak).
        /// Four empties in SirAldric.unity — drag in Scene, Play reads transforms.
        /// No Mixamo DIAG playback. No Mixamo Mirror. No frozen holds.
        /// </summary>
        public const int StrikeMarkerCount = 4;
        public const string StrikePathRootName = "SirAldricStrikePath";
        public const string StrikeMarkerDrawName = "1_Draw_LeftHipPocket";
        public const string StrikeMarkerRaiseName = "2_Raise_AboveHeadRight";
        public const string StrikeMarkerForwardName = "3_Strike_Forward";
        public const string StrikeMarkerFootName = "4_Strike_DownToFoot";
        public const float StrikePathSeconds = 2.40f;
        public const float StrikePathHipY = 0.96f;
        public const float StrikeMarkerDrawX = -0.22f;
        public const float StrikeMarkerDrawY = 0.96f;
        public const float StrikeMarkerDrawZ = 0.05f;
        public const float StrikeMarkerRaiseX = 0.26f;
        public const float StrikeMarkerRaiseY = 2.08f;
        public const float StrikeMarkerRaiseZ = 0.12f;
        public const float StrikeMarkerForwardX = 0.08f;
        public const float StrikeMarkerForwardY = 1.30f;
        public const float StrikeMarkerForwardZ = 0.84f;
        public const float StrikeMarkerFootX = 0.14f;
        public const float StrikeMarkerFootY = 0.16f;
        public const float StrikeMarkerFootZ = 0.32f;

        public static string StrikeMarkerName(int index)
        {
            return index switch
            {
                0 => StrikeMarkerDrawName,
                1 => StrikeMarkerRaiseName,
                2 => StrikeMarkerForwardName,
                _ => StrikeMarkerFootName,
            };
        }

        public static float StrikeMarkerKnotU(int index)
        {
            return index switch
            {
                0 => 0f,
                1 => 0.32f,
                2 => 0.62f,
                _ => 1f,
            };
        }

        public static void StrikeMarkerDefaultWorld(int index, out float x, out float y, out float z)
        {
            switch (index)
            {
                case 0:
                    x = StrikeMarkerDrawX;
                    y = StrikeMarkerDrawY;
                    z = StrikeMarkerDrawZ;
                    break;
                case 1:
                    x = StrikeMarkerRaiseX;
                    y = StrikeMarkerRaiseY;
                    z = StrikeMarkerRaiseZ;
                    break;
                case 2:
                    x = StrikeMarkerForwardX;
                    y = StrikeMarkerForwardY;
                    z = StrikeMarkerForwardZ;
                    break;
                default:
                    x = StrikeMarkerFootX;
                    y = StrikeMarkerFootY;
                    z = StrikeMarkerFootZ;
                    break;
            }
        }

        /// <summary>
        /// Catmull-Rom through the four Scene markers. Linear in u — no hold
        /// at a knot (Smooth01 would freeze). Shoulder/elbow IK follows this.
        /// </summary>
        public static void SampleStrikePath(
            float attackNormalized01,
            float x0,
            float y0,
            float z0,
            float x1,
            float y1,
            float z1,
            float x2,
            float y2,
            float z2,
            float x3,
            float y3,
            float z3,
            out float x,
            out float y,
            out float z)
        {
            var u = Clamp01(attackNormalized01);
            var seg = 0;
            if (u >= StrikeMarkerKnotU(2))
            {
                seg = 2;
            }
            else if (u >= StrikeMarkerKnotU(1))
            {
                seg = 1;
            }

            var u0 = StrikeMarkerKnotU(seg);
            var u1 = StrikeMarkerKnotU(seg + 1);
            var t = u1 - u0 > 1e-5f ? (u - u0) / (u1 - u0) : 1f;
            StrikePathPoint(seg - 1, x0, y0, z0, x1, y1, z1, x2, y2, z2, x3, y3, z3, out var ax, out var ay, out var az);
            StrikePathPoint(seg, x0, y0, z0, x1, y1, z1, x2, y2, z2, x3, y3, z3, out var bx, out var by, out var bz);
            StrikePathPoint(seg + 1, x0, y0, z0, x1, y1, z1, x2, y2, z2, x3, y3, z3, out var cx, out var cy, out var cz);
            StrikePathPoint(seg + 2, x0, y0, z0, x1, y1, z1, x2, y2, z2, x3, y3, z3, out var dx, out var dy, out var dz);
            x = CatmullScalar(t, ax, bx, cx, dx);
            y = CatmullScalar(t, ay, by, cy, dy);
            z = CatmullScalar(t, az, bz, cz, dz);
        }

        private static void StrikePathPoint(
            int index,
            float x0,
            float y0,
            float z0,
            float x1,
            float y1,
            float z1,
            float x2,
            float y2,
            float z2,
            float x3,
            float y3,
            float z3,
            out float x,
            out float y,
            out float z)
        {
            if (index <= 0)
            {
                if (index == 0)
                {
                    x = x0;
                    y = y0;
                    z = z0;
                    return;
                }

                x = (2f * x0) - x1;
                y = (2f * y0) - y1;
                z = (2f * z0) - z1;
                return;
            }

            if (index == 1)
            {
                x = x1;
                y = y1;
                z = z1;
                return;
            }

            if (index == 2)
            {
                x = x2;
                y = y2;
                z = z2;
                return;
            }

            if (index == 3)
            {
                x = x3;
                y = y3;
                z = z3;
                return;
            }

            x = (2f * x3) - x2;
            y = (2f * y3) - y2;
            z = (2f * z3) - z2;
        }

        private static float CatmullScalar(float t, float p0, float p1, float p2, float p3)
        {
            var t2 = t * t;
            var t3 = t2 * t;
            return 0.5f * ((2f * p1) + ((-p0 + p2) * t) + (((2f * p0) - (5f * p1) + (4f * p2) - p3) * t2) + ((-p0 + (3f * p1) - (3f * p2) + p3) * t3));
        }

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
        /// <summary>
        /// Rebuilt attack clip (not a tweak of the ddda018 smear). 3.20s.
        /// Hip-relative meters, held beats. Rear cam / Derek POV.
        /// Draw 0.32–0.90s · raise 1.28–1.79s · forward 2.18–2.56s · foot 2.88–3.20s.
        /// </summary>
        public const float AttackClipSeconds = 3.20f;
        public const float AttackDrawArriveU = 0.10f;
        public const float AttackDrawHoldEndU = 0.28f;
        public const float AttackRaiseArriveU = 0.40f;
        public const float AttackRaiseHoldEndU = 0.56f;
        public const float AttackFwdArriveU = 0.68f;
        public const float AttackFwdHoldEndU = 0.80f;
        public const float AttackFootArriveU = 0.90f;
        public const float AttackDrawLeftHipU = 0.20f;
        public const float AttackSwordInHandU = 0.28f;
        public const float AttackRaiseOverheadRightU = 0.48f;
        public const float AttackStrikeForwardU = 0.74f;
        public const float AttackStrikeFootU = 0.95f;
        public const float AttackHangRight = 0.16f;
        public const float AttackHangUp = -0.06f;
        public const float AttackHangFront = 0.06f;
        public const float AttackPocketRight = -0.24f;
        public const float AttackPocketUp = 0.00f;
        public const float AttackPocketFront = 0.02f;
        public const float AttackOverheadRight = 0.22f;
        public const float AttackOverheadUp = 0.90f;
        public const float AttackOverheadFront = 0.06f;
        public const float AttackForwardRight = 0.10f;
        public const float AttackForwardUp = 0.40f;
        public const float AttackForwardFront = 0.78f;
        public const float AttackFootRight = 0.10f;
        public const float AttackFootUp = -0.48f;
        public const float AttackFootFront = 0.24f;

        /// <summary>
        /// Leftover raise-then-cut keys (Derek FAIL 95aba89 “much worse”).
        /// Unused — mixer plays the left-hip-draw clip. Not Mixamo L/R Mirror.
        /// Milder overhead than leftover AttackSlashUrUp 0.82 (ee3f7bd pinch).
        /// </summary>
        public const float AttackRaiseRight = 0.22f;
        public const float AttackRaiseUp = 0.92f;
        public const float AttackRaiseFront = -0.12f;
        public const float AttackPeakRight = 0.16f;
        public const float AttackPeakUp = 0.96f;
        public const float AttackPeakFront = 0.08f;
        public const float AttackPeakU = 0.28f;
        public const float AttackCutRight = 0.06f;
        public const float AttackCutUp = 0.18f;
        public const float AttackCutFront = 0.68f;
        public const float AttackCutU = 0.52f;
        public const float AttackAcrossRight = -0.48f;
        public const float AttackAcrossUp = -0.22f;
        public const float AttackAcrossFront = 0.42f;
        public const float AttackAcrossU = 0.74f;
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
        /// Hip-relative RH meters for the rebuilt clip. Held poses, not a smear:
        /// draw hold, overhead-right hold, forward hold, foot hold.
        /// +X right, +Y up, +Z forward. Not normalized (bake needs meters).
        /// </summary>
        public static void AttackLeftHipDrawReach(float attackNormalized01, out float x, out float y, out float z)
        {
            var u = Clamp01(attackNormalized01);
            if (u <= AttackDrawHoldEndU)
            {
                if (u < AttackDrawArriveU)
                {
                    SampleSlash(
                        u, 0f, AttackDrawArriveU,
                        AttackHangRight, AttackHangUp, AttackHangFront,
                        AttackPocketRight, AttackPocketUp, AttackPocketFront,
                        out x, out y, out z);
                }
                else
                {
                    x = AttackPocketRight;
                    y = AttackPocketUp;
                    z = AttackPocketFront;
                }
            }
            else if (u <= AttackRaiseHoldEndU)
            {
                if (u < AttackRaiseArriveU)
                {
                    SampleSlash(
                        u, AttackDrawHoldEndU, AttackRaiseArriveU,
                        AttackPocketRight, AttackPocketUp, AttackPocketFront,
                        AttackOverheadRight, AttackOverheadUp, AttackOverheadFront,
                        out x, out y, out z);
                }
                else
                {
                    x = AttackOverheadRight;
                    y = AttackOverheadUp;
                    z = AttackOverheadFront;
                }
            }
            else if (u <= AttackFwdHoldEndU)
            {
                if (u < AttackFwdArriveU)
                {
                    SampleSlash(
                        u, AttackRaiseHoldEndU, AttackFwdArriveU,
                        AttackOverheadRight, AttackOverheadUp, AttackOverheadFront,
                        AttackForwardRight, AttackForwardUp, AttackForwardFront,
                        out x, out y, out z);
                }
                else
                {
                    x = AttackForwardRight;
                    y = AttackForwardUp;
                    z = AttackForwardFront;
                }
            }
            else if (u < AttackFootArriveU)
            {
                SampleSlash(
                    u, AttackFwdHoldEndU, AttackFootArriveU,
                    AttackForwardRight, AttackForwardUp, AttackForwardFront,
                    AttackFootRight, AttackFootUp, AttackFootFront,
                    out x, out y, out z);
            }
            else
            {
                x = AttackFootRight;
                y = AttackFootUp;
                z = AttackFootFront;
            }
        }

        /// <summary>
        /// Leftover RH reach after 95aba89 FAIL: u=0 overhead, u=1 low character-left.
        /// Rear-cam down-left = −X. Unused — mixer plays AttackLeftHipDrawReach.
        /// Leftover AttackSlashReach stays ready→backswing→LL.
        /// </summary>
        public static void AttackRaiseThenCutReach(float attackNormalized01, out float x, out float y, out float z)
        {
            var u = Clamp01(attackNormalized01);
            if (u <= AttackPeakU)
            {
                SampleSlash(
                    u, 0f, AttackPeakU,
                    AttackRaiseRight, AttackRaiseUp, AttackRaiseFront,
                    AttackPeakRight, AttackPeakUp, AttackPeakFront,
                    out x, out y, out z);
            }
            else if (u <= AttackCutU)
            {
                SampleSlash(
                    u, AttackPeakU, AttackCutU,
                    AttackPeakRight, AttackPeakUp, AttackPeakFront,
                    AttackCutRight, AttackCutUp, AttackCutFront,
                    out x, out y, out z);
            }
            else if (u <= AttackAcrossU)
            {
                SampleSlash(
                    u, AttackCutU, AttackAcrossU,
                    AttackCutRight, AttackCutUp, AttackCutFront,
                    AttackAcrossRight, AttackAcrossUp, AttackAcrossFront,
                    out x, out y, out z);
            }
            else
            {
                SampleSlash(
                    u, AttackAcrossU, 1f,
                    AttackAcrossRight, AttackAcrossUp, AttackAcrossFront,
                    AttackSlashLlRight, AttackSlashLlUp, AttackSlashLlFront,
                    out x, out y, out z);
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
