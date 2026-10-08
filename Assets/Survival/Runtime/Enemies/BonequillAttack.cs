using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Bonequill Theme A attack add-on (Design 2026-10-07). Cursed Bone Arrow — bow (new held prop, RightHand), bone arrow materialises nocked f5–9, looses at f14 with a purple trail.
    /// New 0–30 frame Scene take at 30 fps on the existing blenderig (same bones, no rebind; rest and walk unchanged).
    /// Frames below are baked from Design attack_meta drivers in Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class BonequillAttack
    {
        public const string FileName = "BONEQUILL_blenderig_attack.fbx";
        /// <summary>ThemePack attack FBX: Design clothsplit_20261007 BONEQUILL_clothsplit_attack.fbx (body bones only, cloth at rest).</summary>
        public const string FileMd5 = "bbdf2f41a9a6bfe48a4bed2c422472a9";
        /// <summary>Original attack FBX in BonequillAttack.DesignDir (attack_20261007), unchanged.</summary>
        public const string DesignFileMd5 = "16cfb8a07ffe13e6ac1f48b31791523c";
        public const string DesignDir = "design/survival-theme-a-fantasy/enemies/anim/bonequill/attack_20261007";
        public const int ReleaseFrame = 14;
        public const bool InPlace = false;

        /// <summary>Prop FBXs copied next to the character FBXs, with Design MD5s.</summary>
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

        public static readonly BlenderRigAttackSpec Spec = new BlenderRigAttackSpec(
            "Bonequill",
            FileName,
            FileMd5,
            DesignDir,
            ReleaseFrame,
            InPlace,
            new[] { "mixamorig:Hips", "mixamorig:Head", "mixamorig:LeftHand", "mixamorig:RightHand" },
            new[] { -0.0498f, 1.00747f, 0.04605f, 0f, 1.83586f, 0.15077f, -0.51052f, 1.72199f, 0.01374f, 0.53025f, 1.04468f, 0.06706f },
            new[]
            {
                new AttackTrack(
                    "BONEQUILL_bow",
                    AttackTrackKind.Held,
                    "BONEQUILL_bow.fbx",
                    "mixamorig:RightHand",
                    14,
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
                        new AttackFrame(0.58704f, 1.058f, 0.12608f, 0.1423f, -0.2692f, 0.03283f, 0.95195f, 1f, true),
                        new AttackFrame(0.59031f, 1.06113f, 0.12634f, 0.13979f, -0.2619f, 0.03579f, 0.95425f, 1f, true),
                        new AttackFrame(0.59831f, 1.06931f, 0.12696f, 0.13243f, -0.24184f, 0.04334f, 0.96026f, 1f, true),
                        new AttackFrame(0.60772f, 1.08041f, 0.12749f, 0.12026f, -0.21165f, 0.05319f, 0.96846f, 1f, true),
                        new AttackFrame(0.61511f, 1.09193f, 0.12766f, 0.10355f, -0.17431f, 0.06285f, 0.97721f, 1f, true),
                        new AttackFrame(0.61766f, 1.10128f, 0.12786f, 0.08315f, -0.13339f, 0.07003f, 0.98508f, 1f, true),
                        new AttackFrame(0.61376f, 1.10638f, 0.12922f, 0.06062f, -0.09292f, 0.07303f, 0.99114f, 1f, true),
                        new AttackFrame(0.60327f, 1.10617f, 0.1332f, 0.03797f, -0.05693f, 0.07103f, 0.99512f, 1f, true),
                        new AttackFrame(0.58721f, 1.10067f, 0.14113f, 0.01729f, -0.02896f, 0.06413f, 0.99737f, 1f, true),
                        new AttackFrame(0.56727f, 1.09063f, 0.15392f, 0.00035f, -0.01181f, 0.05294f, 0.99853f, 1f, true),
                        new AttackFrame(0.54608f, 1.07689f, 0.16894f, -0.00769f, -0.00382f, 0.03949f, 0.99918f, 1f, true),
                        new AttackFrame(0.52619f, 1.06125f, 0.18251f, -0.00629f, -0.00023f, 0.02648f, 0.99963f, 1f, true),
                        new AttackFrame(0.50907f, 1.04594f, 0.19426f, -0.00096f, 0.00078f, 0.01508f, 0.99989f, 1f, true),
                        new AttackFrame(0.49599f, 1.03326f, 0.20451f, 0.0029f, 0.00051f, 0.00602f, 0.99998f, 1f, true),
                        new AttackFrame(0.48815f, 1.02587f, 0.21403f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.48448f, 1.02349f, 0.22324f, -0.00869f, -0.00049f, -0.00323f, 0.99996f, 1f, true),
                        new AttackFrame(0.48342f, 1.02372f, 0.23176f, -0.01823f, -0.00119f, -0.00439f, 0.99982f, 1f, true),
                        new AttackFrame(0.4854f, 1.02632f, 0.23917f, -0.02787f, -0.00181f, -0.00333f, 0.9996f, 1f, true),
                        new AttackFrame(0.49084f, 1.03106f, 0.24497f, -0.03682f, -0.00209f, 0.00011f, 0.99932f, 1f, true),
                        new AttackFrame(0.50018f, 1.03767f, 0.24863f, -0.04424f, -0.00176f, 0.00613f, 0.999f, 1f, true),
                        new AttackFrame(0.51377f, 1.04596f, 0.24954f, -0.04922f, -0.00059f, 0.01489f, 0.99868f, 1f, true),
                        new AttackFrame(0.53226f, 1.05584f, 0.24647f, -0.04716f, -0.00633f, 0.02599f, 0.99853f, 1f, true),
                        new AttackFrame(0.55386f, 1.06643f, 0.23911f, -0.03498f, -0.02531f, 0.03789f, 0.99835f, 1f, true),
                        new AttackFrame(0.57488f, 1.07584f, 0.22814f, -0.0141f, -0.05513f, 0.04861f, 0.9972f, 1f, true),
                        new AttackFrame(0.59176f, 1.08199f, 0.21413f, 0.01335f, -0.09285f, 0.05601f, 0.99401f, 1f, true),
                        new AttackFrame(0.60208f, 1.08348f, 0.19758f, 0.04432f, -0.13468f, 0.05861f, 0.98816f, 1f, true),
                        new AttackFrame(0.60512f, 1.08026f, 0.17916f, 0.0752f, -0.17628f, 0.05615f, 0.97986f, 1f, true),
                        new AttackFrame(0.60213f, 1.07375f, 0.16026f, 0.10256f, -0.21352f, 0.04984f, 0.97026f, 1f, true),
                        new AttackFrame(0.59581f, 1.0663f, 0.14317f, 0.12384f, -0.24302f, 0.04202f, 0.96117f, 1f, true),
                        new AttackFrame(0.58967f, 1.06035f, 0.13079f, 0.13747f, -0.26228f, 0.03549f, 0.95449f, 1f, true),
                        new AttackFrame(0.58704f, 1.058f, 0.12608f, 0.1423f, -0.2692f, 0.03283f, 0.95195f, 1f, true)
                    },
                    "bow grip in RightHand; vertical at strike frame"),
                new AttackTrack(
                    "BONEQUILL_bonearrow_purple_fletch",
                    AttackTrackKind.Held,
                    "BONEQUILL_bonearrow_purple_fletch.fbx",
                    "mixamorig:RightHand",
                    14,
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
                        new AttackFrame(0.88196f, 1.23722f, -0.38938f, 0.1423f, -0.2692f, 0.03283f, 0.95195f, 0.05f, false),
                        new AttackFrame(0.87681f, 1.23827f, -0.39457f, 0.13979f, -0.2619f, 0.03579f, 0.95425f, 0.05f, false),
                        new AttackFrame(0.86157f, 1.23961f, -0.40829f, 0.13243f, -0.24184f, 0.04334f, 0.96026f, 0.05f, false),
                        new AttackFrame(0.83587f, 1.23774f, -0.42748f, 0.12026f, -0.21165f, 0.05319f, 0.96846f, 0.05f, false),
                        new AttackFrame(0.79964f, 1.22925f, -0.44844f, 0.10355f, -0.17431f, 0.06285f, 0.97721f, 0.05f, false),
                        new AttackFrame(0.75429f, 1.21212f, -0.46699f, 0.08315f, -0.13339f, 0.07003f, 0.98508f, 0.05f, true),
                        new AttackFrame(0.70303f, 1.18663f, -0.47938f, 0.06062f, -0.09292f, 0.07303f, 0.99114f, 0.25f, true),
                        new AttackFrame(0.65051f, 1.1553f, -0.48337f, 0.03797f, -0.05693f, 0.07103f, 0.99512f, 0.5f, true),
                        new AttackFrame(0.60185f, 1.12182f, -0.47866f, 0.01729f, -0.02896f, 0.06413f, 0.99737f, 0.75f, true),
                        new AttackFrame(0.562f, 1.08973f, -0.46638f, 0.00035f, -0.01181f, 0.05294f, 0.99853f, 1f, true),
                        new AttackFrame(0.53125f, 1.06597f, -0.45111f, -0.00769f, -0.00382f, 0.03949f, 0.99918f, 1f, true),
                        new AttackFrame(0.50671f, 1.0524f, -0.43744f, -0.00629f, -0.00023f, 0.02648f, 0.99963f, 1f, true),
                        new AttackFrame(0.48813f, 1.04414f, -0.42571f, -0.00096f, 0.00078f, 0.01508f, 0.99989f, 1f, true),
                        new AttackFrame(0.47533f, 1.03662f, -0.41546f, 0.0029f, 0.00051f, 0.00602f, 0.99998f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.46506f, 1.01284f, -0.39669f, -0.00869f, -0.00049f, -0.00323f, 0.99996f, 1f, false),
                        new AttackFrame(0.46479f, 1.00129f, -0.38787f, -0.01823f, -0.00119f, -0.00439f, 0.99982f, 1f, false),
                        new AttackFrame(0.46753f, 0.9919f, -0.37994f, -0.02787f, -0.00181f, -0.00333f, 0.9996f, 1f, false),
                        new AttackFrame(0.47344f, 0.98542f, -0.37343f, -0.03682f, -0.00209f, 0.00011f, 0.99932f, 1f, false),
                        new AttackFrame(0.4827f, 0.98263f, -0.369f, -0.04424f, -0.00176f, 0.00613f, 0.999f, 1f, false),
                        new AttackFrame(0.49542f, 0.98443f, -0.36745f, -0.04922f, -0.00059f, 0.01489f, 0.99868f, 1f, false),
                        new AttackFrame(0.52164f, 0.9966f, -0.37093f, -0.04716f, -0.00633f, 0.02599f, 0.99853f, 1f, false),
                        new AttackFrame(0.56692f, 1.02276f, -0.37954f, -0.03498f, -0.02531f, 0.03789f, 0.99835f, 1f, false),
                        new AttackFrame(0.62412f, 1.05975f, -0.39002f, -0.0141f, -0.05513f, 0.04861f, 0.9972f, 1f, false),
                        new AttackFrame(0.68575f, 1.10272f, -0.39868f, 0.01335f, -0.09285f, 0.05601f, 0.99401f, 1f, false),
                        new AttackFrame(0.74474f, 1.1455f, -0.40292f, 0.04432f, -0.13468f, 0.05861f, 0.98816f, 1f, false),
                        new AttackFrame(0.79545f, 1.18223f, -0.40238f, 0.0752f, -0.17628f, 0.05615f, 0.97986f, 1f, false),
                        new AttackFrame(0.8346f, 1.20928f, -0.39866f, 0.10256f, -0.21352f, 0.04984f, 0.97026f, 1f, false),
                        new AttackFrame(0.86143f, 1.22615f, -0.39413f, 0.12384f, -0.24302f, 0.04202f, 0.96117f, 1f, false),
                        new AttackFrame(0.87685f, 1.23468f, -0.39069f, 0.13747f, -0.26228f, 0.03549f, 0.95449f, 1f, false),
                        new AttackFrame(0.88196f, 1.23722f, -0.38938f, 0.1423f, -0.2692f, 0.03283f, 0.95195f, 1f, false)
                    },
                    "cursed bone arrow materialises nocked on the bow f5-9 (no draw hand: left hand is skinned to LeftUpLeg on this rig)"),
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
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 0.06069f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 0.52736f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 0.99403f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 1.46069f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 1.92736f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 2.39403f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 2.86069f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 3.32736f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 3.79403f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 4.26069f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 4.72736f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 5.19403f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 5.66069f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 6.12736f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 6.59403f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 7.06069f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "bone arrow flies character-forward from nock pose"),
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
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.46815f, 1.02587f, -0.40597f, 0f, 0f, 0f, 1f, 0.35f, true),
                        new AttackFrame(0.46815f, 1.02587f, 0.06069f, 0f, 0f, 0f, 1f, 0.56667f, true),
                        new AttackFrame(0.46815f, 1.02587f, 0.52736f, 0f, 0f, 0f, 1f, 0.78333f, true),
                        new AttackFrame(0.46815f, 1.02587f, 0.99403f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 1.46069f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 1.92736f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 2.39403f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 2.86069f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 3.32736f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 3.79403f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 4.26069f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 4.72736f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 5.19403f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 5.66069f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 6.12736f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 6.59403f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.46815f, 1.02587f, 7.06069f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "purple trail follows arrow nock")
            });
    }
}
