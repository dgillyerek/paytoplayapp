using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Bonequill gritty rebuild bow attack (2026-10-14, quiver draw + archer stance), 31 frames at 30 fps: f0 rest, f2 draw hand lifts out
    /// to the side, f4 grabs an arrow at the quiver mouth over the right shoulder (arrow prop appears in the fingers), f6 pulls it up
    /// out of the quiver, f8 hand up over the right shoulder, f10 nock, f14 full draw with the torso yawed ~55 deg (bow shoulder at the
    /// target, LeftArm straight out, RightHand at the jaw, elbow out), f17 loose (arrow flies straight at the target at 14 m/s with a
    /// purple trail), f20 string-hand recoil, f30 rest. Elbow stays high and out, so the arm never crosses the hood, cloak or ribcage
    /// (checked at every half frame); the forearm has no twist relative to the upper arm, so it cannot roll-flip.
    /// In rest and walk the bow is carried lower, tipped 45 deg out (top clear of the shoulder guard), and swings up to the full-draw
    /// grip between f0 and f10; it is back to the carry by f30. Bow-vs-body/cloak clearance is checked at every half frame of every clip
    /// (work/bow_clearance.json, BonequillBowClearanceTests).
    /// The bow hand closes on real finger bones (LeftHandFingers1-3, LeftHandThumb1-2, keyed in every clip): grip seated in the palm,
    /// fingers curled around it, thumb over the top; the hand is rolled so the grip runs across the palm.
    /// Attack stance (Derek 10-15 23:29): from f8 the left foot steps toward the target and the hips turn 30 deg so he stands side-on
    /// (bow arm at the target) by f12; back to square by f30. Rest and walk are unchanged.
    /// The nocked arrow lies on the archer's left (outer) side of the riser and runs nock -> arrow rest straight at the target
    /// (within 0.5 deg at release, work/draw_check.json); in Unity it is re-aimed every frame from nock to release and the bowstring
    /// is drawn as a line from tip to the arrow nock to tip (BowGrip.ArrowRest / StringTop / StringBottom).
    /// Head forward. Frames baked in Unity convention (character forward = +Z) by design .../gritty_20261013/work/sheet.py.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class BonequillAttack
    {
        public const string FileName = "BONEQUILL_blenderig_attack.fbx";
        public const string FileMd5 = "b3fd5c3208bc3f2992a1293fcd77ef0c";
        public const string DesignDir = BonequillMotion.DesignDir;
        public const int ReleaseFrame = 17;
        public const bool InPlace = false;

        /// <summary>Prop FBXs from #47 (attack_20261007), unchanged.</summary>
        public static readonly string[] PropFileNames =
        {
            "BONEQUILL_bonearrow_purple_fletch.fbx",
            "BONEQUILL_bow.fbx"
        };

        public static readonly string[] PropFileMd5s =
        {
            "9d8abdd529785dae22fcc01149e50dd3",
            "3b25e1c4d1f4f418fb00540e4e29345f"
        };

        /// <summary>Bow grip in mixamorig:LeftHand bone space from Blender (design work/grip_unity.json): pivot at the grip
        /// centre on the hand centreline, bow axis along the knuckle line, limb in the palm. Rolled about the long axis per frame
        /// (work/bowroll.py): carried with the string resting on the inside of the forearm and the bow limb out, turned toward him only from nock to release
        /// (work/string_clearance.json). The carry arm pose is anim.py CARRY. Unity parents the bow to the
        /// hand bone at this offset (mirror-X bone-space conversion) instead of fitting a world pose.</summary>
        public static readonly BlenderBoneGrip BowGrip = new BlenderBoneGrip(
            new[] { 0.04200f, 0.09400f, -0.08380f },
            new[] { 0f, 0f, 1f, 0f },   // grip axis flip (180 deg about bone z); the roll about the bow's long axis is per frame below
            "mixamorig:LeftHandFingers1",
            new[] { -0.01620f, 0.08500f, -0.08380f },
            "mixamorig:LeftHandThumb1",
            new[] { 0.04439f, 0.03838f, -0.05479f },
            new[] { 0.04243f, 0.09263f, -0.08380f },
            10.5f,  // rest/walk carry: bow front to back beside the leg, natural hand; string 0.7 cm off the inside of the forearm (anim.py CARRY)
            30f,    // nock to release: string toward him; eases +19.5 from 10.5, string stays off the forearm
            new[] { 2f, 10f, 17f, 26f },
            new[] { 0.033f, 0f, 0.0574f },   // arrow rest: archer's left (outer) side of the riser, above the fist (bowroll.py ARROW_REST_BOW)
            new[] { 0f, 0.19f, 0.62f },      // string tips (bow mesh)
            new[] { 0f, 0.19f, -0.62f },
            "BONEQUILL_bow_string",
            "BONEQUILL_bonearrow_purple_fletch",
            10f,                              // nock: string drawn to the arrow nock from here
            17f);                             // loose: string snaps back straight

        public static readonly BlenderRigIdlePropSpec[] IdleProps =
        {
            new BlenderRigIdlePropSpec(
                "BONEQUILL_bow_rest",
                "BONEQUILL_bow.fbx",
                "3b25e1c4d1f4f418fb00540e4e29345f",
                "mixamorig:LeftHand",
                new[] { -0.40784f, 0.91081f, -0.02139f, 0.62506f, -0.16161f, -0.15247f, 0.74829f },
                new[] { 0.048f, 1.41f, 0.22335f },
                "Bone bow on LeftHand in rest, walk and attack (rest_world from sheet.json). Stands in for the attack's BONEQUILL_bow track so one bow serves every clip.",
                "BONEQUILL_bow",
                BowGrip)
        };

        public static readonly BlenderRigAttackSpec Spec = new BlenderRigAttackSpec(
            "Bonequill",
            FileName,
            FileMd5,
            DesignDir,
            ReleaseFrame,
            InPlace,
            new[] { "mixamorig:Hips", "mixamorig:Head", "mixamorig:LeftHand", "mixamorig:RightHand" },
            new[] { 0f, 1f, -0.03f, 0f, 1.575f, -0.035f, -0.335f, 0.97f, -0.03f, 0.335f, 0.97f, -0.03f },
            new[]
            {
                new AttackTrack(
                    "BONEQUILL_bow",
                    AttackTrackKind.Held,
                    "BONEQUILL_bow.fbx",
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
                    new[] { 0.048f, 1.41f, 0.22335f },
                    new[]
                    {
                        new AttackFrame(-0.41444f, 0.91197f, -0.02439f, 0.62656f, -0.1483f, -0.1807f, 0.74349f, 1f, true),
                        new AttackFrame(-0.41404f, 0.90995f, -0.00657f, 0.60292f, -0.14158f, -0.17866f, 0.76454f, 1f, true),
                        new AttackFrame(-0.41107f, 0.90823f, 0.04396f, 0.53727f, -0.12311f, -0.17194f, 0.81647f, 1f, true),
                        new AttackFrame(-0.4014f, 0.91639f, 0.12238f, 0.43763f, -0.1021f, -0.16195f, 0.87854f, 1f, true),
                        new AttackFrame(-0.38061f, 0.94428f, 0.21885f, 0.31789f, -0.08772f, -0.14569f, 0.93275f, 1f, true),
                        new AttackFrame(-0.34729f, 0.99563f, 0.31612f, 0.1979f, -0.08096f, -0.11789f, 0.96973f, 1f, true),
                        new AttackFrame(-0.30614f, 1.06272f, 0.39546f, 0.09563f, -0.08047f, -0.08006f, 0.98892f, 1f, true),
                        new AttackFrame(-0.26612f, 1.12992f, 0.44804f, 0.02059f, -0.0838f, -0.03952f, 0.99549f, 1f, true),
                        new AttackFrame(-0.23444f, 1.18409f, 0.47728f, -0.02714f, -0.08869f, -0.00453f, 0.99568f, 1f, true),
                        new AttackFrame(-0.2083f, 1.21862f, 0.55092f, -0.05328f, -0.09297f, 0.01541f, 0.99412f, 1f, true),
                        new AttackFrame(-0.17741f, 1.22867f, 0.61021f, -0.06063f, -0.09505f, 0.02767f, 0.99324f, 1f, true),
                        new AttackFrame(-0.1459f, 1.25269f, 0.63098f, -0.06063f, -0.09505f, 0.02767f, 0.99324f, 1f, true),
                        new AttackFrame(-0.07012f, 1.31449f, 0.66303f, -0.06063f, -0.09505f, 0.02767f, 0.99324f, 1f, true),
                        new AttackFrame(0.0084f, 1.38559f, 0.67411f, -0.06063f, -0.09505f, 0.02767f, 0.99324f, 1f, true),
                        new AttackFrame(0.04296f, 1.416f, 0.6728f, -0.06063f, -0.09505f, 0.02767f, 0.99324f, 1f, true),
                        new AttackFrame(0.04296f, 1.416f, 0.6728f, -0.06063f, -0.09505f, 0.02767f, 0.99324f, 1f, true),
                        new AttackFrame(0.04296f, 1.416f, 0.6728f, -0.06063f, -0.09505f, 0.02767f, 0.99324f, 1f, true),
                        new AttackFrame(0.04296f, 1.416f, 0.6728f, -0.06063f, -0.09505f, 0.02767f, 0.99324f, 1f, true),
                        new AttackFrame(0.04296f, 1.416f, 0.6728f, -0.06079f, -0.08925f, 0.02732f, 0.99378f, 1f, true),
                        new AttackFrame(0.04296f, 1.416f, 0.6728f, -0.06121f, -0.07369f, 0.02636f, 0.99505f, 1f, true),
                        new AttackFrame(0.04296f, 1.416f, 0.6728f, -0.06179f, -0.05115f, 0.02497f, 0.99647f, 1f, true),
                        new AttackFrame(0.02969f, 1.40372f, 0.67315f, -0.06293f, -0.02788f, 0.00822f, 0.99759f, 1f, true),
                        new AttackFrame(-0.00842f, 1.36597f, 0.67139f, -0.05843f, -0.00851f, -0.03392f, 0.99768f, 1f, true),
                        new AttackFrame(-0.07129f, 1.29682f, 0.65817f, -0.03394f, 0.00668f, -0.09218f, 0.99514f, 1f, true),
                        new AttackFrame(-0.15962f, 1.19072f, 0.61272f, 0.027f, 0.01576f, -0.15348f, 0.98766f, 1f, true),
                        new AttackFrame(-0.26203f, 1.06463f, 0.50612f, 0.13331f, 0.01258f, -0.20023f, 0.97056f, 1f, true),
                        new AttackFrame(-0.34573f, 0.96751f, 0.34672f, 0.27231f, -0.01218f, -0.22039f, 0.93655f, 1f, true),
                        new AttackFrame(-0.39234f, 0.9215f, 0.19012f, 0.4133f, -0.05479f, -0.21779f, 0.88247f, 1f, true),
                        new AttackFrame(-0.41062f, 0.9092f, 0.07189f, 0.52804f, -0.09997f, -0.20286f, 0.81855f, 1f, true),
                        new AttackFrame(-0.41451f, 0.91017f, 0.00016f, 0.60108f, -0.1348f, -0.18736f, 0.76514f, 1f, true),
                        new AttackFrame(-0.41444f, 0.91197f, -0.02439f, 0.62656f, -0.1483f, -0.1807f, 0.74349f, 1f, true)
                    },
                    "bone bow gripped 6 cm along LeftHand, world-upright at full draw; the LYRA-style idle prop stands in for it"),
                new AttackTrack(
                    "BONEQUILL_bonearrow_purple_fletch",
                    AttackTrackKind.Held,
                    "BONEQUILL_bonearrow_purple_fletch.fbx",
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
                    new[] { 0.03886f, 0.0343f, 0.7925f },
                    new[]
                    {
                        new AttackFrame(0.26429f, 1.0125f, -0.06777f, 0.69197f, -0.1455f, -0.1455f, 0.69197f, 1f, false),
                        new AttackFrame(0.3822f, 1.24647f, 0.07109f, 0.69197f, -0.1455f, -0.1455f, 0.69197f, 1f, false),
                        new AttackFrame(0.34328f, 1.52369f, -0.02065f, 0.69197f, -0.1455f, -0.1455f, 0.69197f, 1f, false),
                        new AttackFrame(0.26049f, 1.62898f, -0.16028f, 0.69197f, -0.1455f, -0.1455f, 0.69197f, 1f, false),
                        new AttackFrame(0.17328f, 1.64369f, -0.29065f, 0.69197f, -0.1455f, -0.1455f, 0.69197f, 1f, true),
                        new AttackFrame(0.21875f, 1.75248f, -0.28626f, 0.69197f, -0.1455f, -0.1455f, 0.69197f, 1f, true),
                        new AttackFrame(0.26328f, 1.82369f, -0.26065f, 0.69197f, -0.1455f, -0.1455f, 0.69197f, 1f, true),
                        new AttackFrame(0.42506f, 1.83801f, -0.12217f, 0.77137f, -0.5389f, -0.08669f, 0.32722f, 1f, true),
                        new AttackFrame(0.57727f, 1.71216f, 0.12466f, -0.39424f, 0.77148f, 0.34258f, 0.36335f, 1f, true),
                        new AttackFrame(0.40299f, 1.54788f, 0.36486f, -0.04068f, 0.18717f, 0.29392f, 0.93644f, 1f, true),
                        new AttackFrame(0.05597f, 1.36949f, 0.36915f, 0.1644f, -0.3983f, -0.0728f, 0.89946f, 1f, true),
                        new AttackFrame(0.04897f, 1.38218f, 0.35439f, 0.12964f, -0.33883f, -0.0472f, 0.93068f, 1f, true),
                        new AttackFrame(0.03298f, 1.41431f, 0.32076f, 0.0668f, -0.19517f, -0.01333f, 0.9784f, 1f, true),
                        new AttackFrame(0.01558f, 1.4549f, 0.28605f, 0.01946f, -0.05576f, -0.00109f, 0.99825f, 1f, true),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.03605f, 1.47509f, 0.27003f, 0.00558f, -0.03586f, -0.0002f, 0.99934f, 1f, false),
                        new AttackFrame(0.08864f, 1.47909f, 0.26396f, 0.01031f, -0.10019f, -0.00104f, 0.99491f, 1f, false),
                        new AttackFrame(0.11541f, 1.48126f, 0.25796f, 0.01253f, -0.12977f, -0.00164f, 0.99146f, 1f, false),
                        new AttackFrame(0.12386f, 1.46643f, 0.26196f, 0.0079f, -0.15328f, -0.00123f, 0.98815f, 1f, false),
                        new AttackFrame(0.1469f, 1.42039f, 0.2713f, -0.00578f, -0.2176f, 0.00129f, 0.97602f, 1f, false),
                        new AttackFrame(0.18184f, 1.33578f, 0.27786f, -0.02949f, -0.30963f, 0.00961f, 0.95035f, 1f, false),
                        new AttackFrame(0.22855f, 1.20838f, 0.2641f, -0.06025f, -0.4098f, 0.02714f, 0.90978f, 1f, false),
                        new AttackFrame(0.28698f, 1.0617f, 0.20887f, -0.08997f, -0.49739f, 0.05196f, 0.86128f, 1f, false),
                        new AttackFrame(0.34235f, 0.94939f, 0.12122f, -0.11658f, -0.55948f, 0.07987f, 0.81671f, 1f, false),
                        new AttackFrame(0.37642f, 0.89229f, 0.04186f, -0.1466f, -0.59838f, 0.11252f, 0.77961f, 1f, false),
                        new AttackFrame(0.39153f, 0.87009f, -0.00937f, -0.19154f, -0.61835f, 0.15889f, 0.74546f, 1f, false),
                        new AttackFrame(0.39686f, 0.86299f, -0.03582f, -0.25006f, -0.6186f, 0.2171f, 0.71251f, 1f, false),
                        new AttackFrame(0.39807f, 0.86148f, -0.04399f, -0.28188f, -0.61262f, 0.24833f, 0.69539f, 1f, false)
                    },
                    "arrow nocked f5, drawn to the chin by RightHand at f10, held to f13"),
                new AttackTrack(
                    "BONEQUILL_bonearrow_purple_fletch_inflight",
                    AttackTrackKind.Projectile,
                    "BONEQUILL_bonearrow_purple_fletch.fbx",
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
                    new[] { 0.03886f, 0.0343f, 0.7925f },
                    new[]
                    {
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00834f, 1.47022f, 0.7369f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.00971f, 1.46733f, 1.20356f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.01108f, 1.46444f, 1.67021f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.01245f, 1.46155f, 2.13687f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.01382f, 1.45867f, 2.60352f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.01519f, 1.45578f, 3.07018f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.01655f, 1.45289f, 3.53684f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.01792f, 1.45f, 4.00349f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.01929f, 1.44711f, 4.47015f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.02066f, 1.44423f, 4.9368f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.02203f, 1.44134f, 5.40346f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.0234f, 1.43845f, 5.87011f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.02477f, 1.43556f, 6.33677f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.02614f, 1.43268f, 6.80343f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true)
                    },
                    "loosed at f14 along nock to grip (character-forward), 14 m/s"),
                new AttackTrack(
                    "BONEQUILL_arrow_trail_vfx",
                    AttackTrackKind.Projectile,
                    "",
                    "",
                    0,
                    14,
                    14f,
                    AttackVfxShape.Trail,
                    0.05f,
                    0.9f,
                    new[] { 0.82666f, 0.48453f, 1f },
                    new[] { 0.70141f, 0.2717f, 0.85431f },
                    new[] { 0.82666f, 0.48453f, 1f },
                    new[] { 0.03f, 0.02853f, 0.9f },
                    new[]
                    {
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00697f, 1.4731f, 0.27025f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, false),
                        new AttackFrame(0.00834f, 1.47022f, 0.7369f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.00971f, 1.46733f, 1.20356f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.01108f, 1.46444f, 1.67021f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.01245f, 1.46155f, 2.13687f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.01382f, 1.45867f, 2.60352f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.01519f, 1.45578f, 3.07018f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.01655f, 1.45289f, 3.53684f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.01792f, 1.45f, 4.00349f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.01929f, 1.44711f, 4.47015f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.02066f, 1.44423f, 4.9368f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.02203f, 1.44134f, 5.40346f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.0234f, 1.43845f, 5.87011f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.02477f, 1.43556f, 6.33677f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true),
                        new AttackFrame(0.02614f, 1.43268f, 6.80343f, 0.00309f, 0.00147f, 0f, 0.99999f, 1f, true)
                    },
                    "purple trail behind the arrow (colours from #47)")
            });
    }
}
