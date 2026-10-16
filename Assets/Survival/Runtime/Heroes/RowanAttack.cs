using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Rowan A-pose rebuild bow attack (2026-10-14 v2, Derek: an arm came through the chest on the raise; bow through the hand):
    /// both hands follow keyed waypoints in the turning torso frame so the 70 deg chest turn carries the arms. Draw arm: f3 elbow leads
    /// out to the side, f6 up outside the shoulder line, f8 hand in at head height, f10 full draw at the right jaw with the wrist flexed
    /// so the fingers point along the arrow (elbow out to the side, just above the shoulder). Bow arm: out to the side first, then
    /// straight from the shoulder (its roll solved so the bow stands within 13 deg of vertical). f14 loose (arrow flies nock to grip at
    /// 14 m/s with a hero-blue trail), f17 follow-through, out the same way, f30 rest. Arm vs chest/coat penetration measured on every
    /// frame and half-frame (design .../work/apose_20261014/pen.json): 0. Arm roll fixed: after the aim the full Arm/ForeArm basis maps the rest elbow hinge onto the IK hinge (explicit pole
    /// roll) and RightHand/LeftHand are keyed every frame; every attack frame checked for forearm flip (forearm twist 0).
    /// One bow only, its grip closed in the left hand on real finger bones (LeftHandFingers1-3 + LeftHandThumb1-2 curled 15/35/20 and 60/20 deg, fingers4.py; original Meshy hand, no sculpt) in every clip; nothing on his back. Feet planted, Head faces forward every frame.
    /// Frames baked in Unity convention (character forward = +Z) by design .../fresh_20261013/work/apose_20261014/sheet.py.
    /// 2026-10-15 (Derek): the left foot steps toward the target as he fires: lifts f2, plants about 29 cm forward at f8 (before the f14
    /// release) with the hips/weight shifting onto it (f4-f11), holds through the release, returns to rest by f26; both feet are 2-bone
    /// IK on fixed world targets (no sliding), f0 and f30 are the rest pose.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class RowanAttack
    {
        public const string FileName = "ROWAN_blenderig_attack.fbx";
        public const string FileMd5 = "f50ab0b2ed1560515c8242fca9f87cb6";
        public const string DesignDir = RowanMotion.DesignDir;
        public const int ReleaseFrame = 14;
        public const bool InPlace = false;

        /// <summary>Prop FBXs modelled in Blender for the fresh rebuild (design .../fresh_20261013/props).</summary>
        public static readonly string[] PropFileNames =
        {
            "ROWAN_bow.fbx",
            "ROWAN_arrow.fbx"
        };

        public static readonly string[] PropFileMd5s =
        {
            "a147b7d3ab59893e9b563decc1241dac",
            "079abe232bead0a8fee5d019441fe4d5"
        };

        /// <summary>Bow grip in mixamorig:LeftHand bone space from Blender (design work/apose_20261014/grip_unity.json; same fix as Bonequill #63):
        /// pivot at the grip centre on the hand centreline, bow axis along the knuckle line. Unity parents the bow to the hand bone at this offset
        /// (mirror-X bone-space conversion) instead of fitting a world pose. Rolled about the long axis per frame (work/apose_20261014/bowroll.py):
        /// rest/walk carry (Derek on 29ecada, v5e): the arm, forearm and hand hang in the natural relaxed pose (palm to the thigh, no carry
        /// twist); ONLY the bow is rolled in the closed grip (8 deg; v5g grip seated deeper in the palm) so the string lies along the inside (body side) of the forearm, ~0.8 cm
        /// off the skin, string side up, limb outside. Attack: eases 8 -> 50 (string toward him) over f1-f11, held through the f14 release
        /// to f17, back by f27 (this short way turns the string away from the forearm, never through the wrist).
        /// Checks: work/apose_20261014/carry_check.json, string_clearance.json.</summary>
        public static readonly BlenderBoneGrip BowGrip = new BlenderBoneGrip(
            new[] { 0.02683f, 0.12618f, 0.00832f },   // v5g: seated 5.5 mm deeper in the palm
            new[] { -0.05760f, -0.02022f, 0.99813f, 0f },   // grip axis flip (180 deg about bone z) + 7 deg palm-seat tilt about the pinky end; the roll is per frame below
            "mixamorig:LeftHandFingers1",
            new[] { -0.02264f, 0.13500f, 0.00866f },
            "mixamorig:LeftHandThumb1",
            new[] { 0.02908f, 0.06549f, 0.05844f },
            new[] { 0.02442f, 0.12649f, 0.00805f },
            8f,     // rest/walk carry: natural arm, string along the inside of the forearm ~0.9 cm off the skin (grip corners clear of the fingers)
            50f,    // nock to release: string toward him
            new[] { 1f, 11f, 17f, 27f });

        /// <summary>The single bow, a child of LeftHand at <see cref="BowGrip"/> in rest, walk and attack. Stands in for the attack's ROWAN_bow track.</summary>
        public static readonly BlenderRigIdlePropSpec[] IdleProps =
        {
            new BlenderRigIdlePropSpec(
                "ROWAN_bow_rest",
                "ROWAN_bow.fbx",
                "a147b7d3ab59893e9b563decc1241dac",
                "mixamorig:LeftHand",
                new[] { -0.35101f, 0.85809f, 0.09459f, 0.54675f, 0.00535f, -0.03011f, 0.83674f },
                new[] { 0.048f, 1.3323f, 0.1643f },
                "Single wooden bow, grip closed in the left-hand finger-bone grip, in rest, walk and attack (rest_world from sheet.json). Stands in for the attack's ROWAN_bow track so one bow serves every clip.",
                "ROWAN_bow",
                BowGrip)
        };

        public static readonly BlenderRigAttackSpec Spec = new BlenderRigAttackSpec(
            "Rowan",
            FileName,
            FileMd5,
            DesignDir,
            ReleaseFrame,
            InPlace,
            new[] { "mixamorig:Hips", "mixamorig:Head", "mixamorig:LeftHand", "mixamorig:RightHand" },
            new[] { 0f, 0.96f, -0.02f, 0f, 1.555f, -0.01f, -0.452f, 1.005f, 0.025f, 0.452f, 1.005f, 0.025f },
            new[]
            {
                new AttackTrack(
                    "ROWAN_bow",
                    AttackTrackKind.Held,
                    "ROWAN_bow.fbx",
                    "mixamorig:LeftHand",
                    10,
                    0,
                    0f,
                    AttackVfxShape.None,
                    0f,
                    0f,
                    new float[0],
                    new float[0],
                    new float[0],
                    new[] { 0.048f, 1.3323f, 0.1643f },
                    new[]
                    {
                        new AttackFrame(-0.35615f, 0.85985f, 0.09376f, 0.52911f, -0.08463f, -0.01870f, 0.84412f, 1f, true),
                        new AttackFrame(-0.41094f, 0.88011f, 0.11376f, 0.51805f, -0.11380f, -0.0524f, 0.84583f, 1f, true),
                        new AttackFrame(-0.51217f, 0.95023f, 0.19502f, 0.45972f, -0.18284f, -0.13747f, 0.85810f, 1f, true),
                        new AttackFrame(-0.53892f, 1.06485f, 0.3444f, 0.3083f, -0.19729f, -0.21901f, 0.90505f, 1f, true),
                        new AttackFrame(-0.45971f, 1.12317f, 0.48832f, 0.20558f, -0.1513f, -0.26851f, 0.92778f, 1f, true),
                        new AttackFrame(-0.31346f, 1.1696f, 0.62945f, 0.10822f, -0.11389f, -0.27088f, 0.94970f, 1f, true),
                        new AttackFrame(-0.14764f, 1.28274f, 0.75207f, -0.01905f, -0.11690f, -0.17669f, 0.97711f, 1f, true),
                        new AttackFrame(-0.0363f, 1.40676f, 0.80125f, -0.11472f, -0.12734f, -0.03547f, 0.98456f, 1f, true),
                        new AttackFrame(-0.00987f, 1.4602f, 0.82101f, -0.15118f, -0.15697f, 0.03470f, 0.97535f, 1f, true),
                        new AttackFrame(-0.01221f, 1.45434f, 0.84445f, -0.14963f, -0.19686f, 0.04088f, 0.96808f, 1f, true),
                        new AttackFrame(-0.01393f, 1.45006f, 0.8616f, -0.14843f, -0.22375f, 0.04503f, 0.96222f, 1f, true),
                        new AttackFrame(-0.01459f, 1.4484f, 0.86824f, -0.14796f, -0.23361f, 0.04655f, 0.95988f, 1f, true),
                        new AttackFrame(-0.01459f, 1.4484f, 0.86824f, -0.14796f, -0.23361f, 0.04655f, 0.95988f, 1f, true),
                        new AttackFrame(-0.01459f, 1.4484f, 0.86824f, -0.14796f, -0.23361f, 0.04655f, 0.95988f, 1f, true),
                        new AttackFrame(-0.01459f, 1.4484f, 0.86824f, -0.14796f, -0.23361f, 0.04655f, 0.95988f, 1f, true),
                        new AttackFrame(-0.01459f, 1.4484f, 0.86824f, -0.14796f, -0.23361f, 0.04655f, 0.95988f, 1f, true),
                        new AttackFrame(-0.01459f, 1.4484f, 0.86824f, -0.14796f, -0.23361f, 0.04655f, 0.95988f, 1f, true),
                        new AttackFrame(-0.01459f, 1.4484f, 0.86824f, -0.14796f, -0.23361f, 0.04655f, 0.95988f, 1f, true),
                        new AttackFrame(-0.0441f, 1.42022f, 0.8714f, -0.13456f, -0.24362f, 0.01252f, 0.96041f, 1f, true),
                        new AttackFrame(-0.12314f, 1.34853f, 0.8674f, -0.09178f, -0.26278f, -0.07346f, 0.95767f, 1f, true),
                        new AttackFrame(-0.22888f, 1.25997f, 0.82543f, -0.02880f, -0.2625f, -0.17537f, 0.94793f, 1f, true),
                        new AttackFrame(-0.33451f, 1.17975f, 0.73717f, 0.05331f, -0.24514f, -0.25304f, 0.93436f, 1f, true),
                        new AttackFrame(-0.42418f, 1.12495f, 0.62491f, 0.14155f, -0.22877f, -0.28338f, 0.92050f, 1f, true),
                        new AttackFrame(-0.4952f, 1.10817f, 0.52f, 0.20017f, -0.23100f, -0.27257f, 0.91229f, 1f, true),
                        new AttackFrame(-0.54354f, 1.09255f, 0.42133f, 0.25260f, -0.23526f, -0.24402f, 0.90626f, 1f, true),
                        new AttackFrame(-0.55663f, 1.04523f, 0.31802f, 0.32993f, -0.22012f, -0.20394f, 0.89504f, 1f, true),
                        new AttackFrame(-0.54227f, 0.97747f, 0.22068f, 0.43467f, -0.21000f, -0.15195f, 0.86248f, 1f, true),
                        new AttackFrame(-0.48814f, 0.92484f, 0.15499f, 0.48474f, -0.16493f, -0.10598f, 0.85240f, 1f, true),
                        new AttackFrame(-0.42626f, 0.88681f, 0.11697f, 0.51387f, -0.12554f, -0.06446f, 0.84618f, 1f, true),
                        new AttackFrame(-0.37627f, 0.86619f, 0.09871f, 0.52622f, -0.09603f, -0.03181f, 0.84431f, 1f, true),
                        new AttackFrame(-0.35615f, 0.85985f, 0.09376f, 0.52911f, -0.08463f, -0.01870f, 0.84412f, 1f, true)
                    },
                    "single wooden bow, grip closed in the finger-bone grip (grip.json) in every frame; the RowanMotion idle bow stands in for it"),
                new AttackTrack(
                    "ROWAN_arrow",
                    AttackTrackKind.Held,
                    "ROWAN_arrow.fbx",
                    "mixamorig:RightHand",
                    10,
                    0,
                    0f,
                    AttackVfxShape.None,
                    0f,
                    0f,
                    new float[0],
                    new float[0],
                    new float[0],
                    new[] { 0.033f, 0.0381f, 0.795f },
                    new[]
                    {
                        new AttackFrame(0.38157f, 0.92744f, 0.05665f, 0.03499f, 0.43106f, -0.70613f, 0.56066f, 1f, false),
                        new AttackFrame(0.46168f, 0.96445f, 0.02314f, -0.0011f, 0.44402f, -0.65343f, 0.61309f, 1f, false),
                        new AttackFrame(0.548f, 1.05105f, -0.0811f, -0.09378f, 0.44277f, -0.59807f, 0.66141f, 1f, false),
                        new AttackFrame(0.54853f, 1.12583f, -0.21241f, -0.21457f, 0.44065f, -0.56444f, 0.66423f, 1f, false),
                        new AttackFrame(0.47625f, 1.21861f, -0.31055f, -0.26201f, 0.36537f, -0.53437f, 0.71576f, 1f, false),
                        new AttackFrame(0.37085f, 1.35587f, -0.31388f, -0.22805f, 0.18012f, -0.45685f, 0.84074f, 1f, false),
                        new AttackFrame(0.29953f, 1.43647f, -0.28752f, -0.20505f, 0.05249f, -0.3305f, 0.91977f, 1f, false),
                        new AttackFrame(0.26707f, 1.51099f, -0.18161f, -0.04857f, -0.12184f, -0.20459f, 0.97002f, 1f, false),
                        new AttackFrame(0.25669f, 1.53845f, -0.06369f, 0.10115f, -0.17402f, -0.137f, 0.96991f, 1f, true),
                        new AttackFrame(0.22059f, 1.52803f, 0.03826f, 0.10013f, -0.22238f, -0.04263f, 0.96887f, 1f, true),
                        new AttackFrame(0.18605f, 1.5028f, 0.12815f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.20829f, 1.50794f, 0.12426f, -0.0206f, -0.11805f, -0.05237f, 0.99141f, 1f, false),
                        new AttackFrame(0.2397f, 1.51772f, 0.10944f, -0.09875f, -0.10626f, -0.11217f, 0.98304f, 1f, false),
                        new AttackFrame(0.25585f, 1.52258f, 0.08664f, -0.13704f, -0.10578f, -0.12939f, 0.97637f, 1f, false),
                        new AttackFrame(0.25438f, 1.52147f, 0.04902f, -0.06274f, -0.20286f, -0.1159f, 0.9703f, 1f, false),
                        new AttackFrame(0.26015f, 1.51539f, -0.01011f, 0.03864f, -0.24479f, -0.14621f, 0.95771f, 1f, false),
                        new AttackFrame(0.28101f, 1.50161f, -0.03688f, 0.05373f, -0.22201f, -0.19479f, 0.95388f, 1f, false),
                        new AttackFrame(0.30766f, 1.47834f, -0.05669f, 0.00053f, -0.23337f, -0.24559f, 0.94086f, 1f, false),
                        new AttackFrame(0.35249f, 1.42416f, -0.09639f, -0.08744f, -0.15874f, -0.36212f, 0.91434f, 1f, false),
                        new AttackFrame(0.40283f, 1.36781f, -0.11690f, -0.08192f, -0.04493f, -0.4756f, 0.87468f, 1f, false),
                        new AttackFrame(0.4644f, 1.30119f, -0.119f, -0.08157f, 0.08086f, -0.54734f, 0.82899f, 1f, false),
                        new AttackFrame(0.54592f, 1.19835f, -0.1259f, -0.15906f, 0.27096f, -0.57311f, 0.75685f, 1f, false),
                        new AttackFrame(0.5757f, 1.12454f, -0.09154f, -0.15568f, 0.37036f, -0.58344f, 0.70583f, 1f, false),
                        new AttackFrame(0.56378f, 1.07657f, -0.03692f, -0.08633f, 0.38763f, -0.59803f, 0.69616f, 1f, false),
                        new AttackFrame(0.50734f, 1.01049f, 0.01608f, -0.0208f, 0.39349f, -0.63872f, 0.66089f, 1f, false),
                        new AttackFrame(0.43456f, 0.95005f, 0.04703f, 0.01869f, 0.43522f, -0.66994f, 0.60118f, 1f, false),
                        new AttackFrame(0.38157f, 0.92744f, 0.05665f, 0.03499f, 0.43106f, -0.70613f, 0.56066f, 1f, false)
                    },
                    "arrow on the string from f8, drawn to the jaw by RightHand at f10, held to f13"),
                new AttackTrack(
                    "ROWAN_arrow_inflight",
                    AttackTrackKind.Projectile,
                    "ROWAN_arrow.fbx",
                    "",
                    0,
                    14,
                    14f,
                    AttackVfxShape.None,
                    0f,
                    0f,
                    new float[0],
                    new float[0],
                    new float[0],
                    new[] { 0.033f, 0.0381f, 0.795f },
                    new[]
                    {
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.06292f, 1.46884f, 0.58395f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-0.05954f, 1.43654f, 1.0331f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-0.18201f, 1.40424f, 1.48225f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-0.30447f, 1.37194f, 1.9314f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-0.42694f, 1.33964f, 2.38055f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-0.5494f, 1.30734f, 2.82971f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-0.67186f, 1.27504f, 3.27886f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-0.79433f, 1.24274f, 3.72801f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-0.91679f, 1.21044f, 4.17716f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-1.03926f, 1.17814f, 4.62631f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-1.16172f, 1.14584f, 5.07546f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-1.28419f, 1.11354f, 5.52461f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-1.40665f, 1.08124f, 5.97377f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-1.52912f, 1.04894f, 6.42292f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-1.65158f, 1.01664f, 6.87207f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-1.77405f, 0.98434f, 7.32122f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-1.89651f, 0.95204f, 7.77037f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true)
                    },
                    "loosed at f14 along nock to grip (character-forward), 14 m/s"),
                new AttackTrack(
                    "ROWAN_arrow_trail_vfx",
                    AttackTrackKind.Projectile,
                    "",
                    "",
                    0,
                    14,
                    14f,
                    AttackVfxShape.Trail,
                    0.04f,
                    0.8f,
                    new[] { 0.62f, 0.78f, 1f },
                    new[] { 0.25f, 0.45f, 0.85f },
                    new[] { 0.62f, 0.78f, 1f },
                    new[] { 0.03f, 0.03f, 0.8f },
                    new[]
                    {
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, false),
                        new AttackFrame(0.06292f, 1.46884f, 0.58395f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-0.05954f, 1.43654f, 1.0331f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-0.18201f, 1.40424f, 1.48225f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-0.30447f, 1.37194f, 1.9314f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-0.42694f, 1.33964f, 2.38055f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-0.5494f, 1.30734f, 2.82971f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-0.67186f, 1.27504f, 3.27886f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-0.79433f, 1.24274f, 3.72801f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-0.91679f, 1.21044f, 4.17716f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-1.03926f, 1.17814f, 4.62631f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-1.16172f, 1.14584f, 5.07546f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-1.28419f, 1.11354f, 5.52461f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-1.40665f, 1.08124f, 5.97377f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-1.52912f, 1.04894f, 6.42292f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-1.65158f, 1.01664f, 6.87207f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-1.77405f, 0.98434f, 7.32122f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true),
                        new AttackFrame(-1.89651f, 0.95204f, 7.77037f, 0.03557f, -0.13229f, -0.00475f, 0.99056f, 1f, true)
                    },
                    "hero-blue trail behind the arrow")
            });
    }
}
