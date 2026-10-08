using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Rowan Theme A attack add-on (Design 2026-10-07). Aimed Shot — blue-fletch arrow (new prop) nocked on the mesh bow f5–13, flies at f14 with a pale-blue trail.
    /// New 0–30 frame Scene take at 30 fps on the existing blenderig (same bones, no rebind; rest and walk unchanged).
    /// Frames below are baked from Design attack_meta drivers in Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class RowanAttack
    {
        public const string FileName = "ROWAN_male_blenderig_attack.fbx";
        public const string FileMd5 = "3ad624f3e53db018e3cb18a035e13121";
        public const string DesignDir = "design/survival-theme-a-fantasy/heroes/anim/rowan/attack_20261007";
        public const int ReleaseFrame = 14;
        public const bool InPlace = false;

        /// <summary>Prop FBXs copied next to the character FBXs, with Design MD5s.</summary>
        public static readonly string[] PropFileNames =
        {
            "ROWAN_arrow_blue_fletch.fbx"
        };

        public static readonly string[] PropFileMd5s =
        {
            "7940bf35a79de4fcdca798df34ca6143"
        };

        public static readonly BlenderRigAttackSpec Spec = new BlenderRigAttackSpec(
            "Rowan",
            FileName,
            FileMd5,
            DesignDir,
            ReleaseFrame,
            InPlace,
            new[] { "mixamorig:Hips", "mixamorig:Head", "mixamorig:LeftHand", "mixamorig:RightHand" },
            new[] { 0.00138f, 0.87992f, 0.05395f, 0f, 1.83552f, 0.08044f, -0.28074f, 1.64058f, -0.11402f, 0.36546f, 1.55698f, 0.07042f },
            new[]
            {
                new AttackTrack(
                    "ROWAN_arrow_blue_fletch",
                    AttackTrackKind.Held,
                    "ROWAN_arrow_blue_fletch.fbx",
                    "mixamorig:RightForeArm",
                    14,
                    0,
                    0f,
                    AttackVfxShape.None,
                    0f,
                    0f,
                    new float[0],
                    new float[0],
                    new float[0],
                    new[] { 0.03886f, 0.0343f, 0.8025f },
                    new[]
                    {
                        new AttackFrame(0.15463f, 1.02883f, -0.46126f, 0.0073f, 0.18866f, -0.06499f, 0.97986f, 0.05f, false),
                        new AttackFrame(0.15944f, 1.02845f, -0.45577f, 0.00647f, 0.1851f, -0.06451f, 0.98058f, 0.05f, false),
                        new AttackFrame(0.17266f, 1.02774f, -0.4404f, 0.00428f, 0.17529f, -0.06312f, 0.98248f, 0.05f, false),
                        new AttackFrame(0.19208f, 1.02748f, -0.41662f, 0.00117f, 0.1605f, -0.0609f, 0.98515f, 0.05f, false),
                        new AttackFrame(0.2153f, 1.02849f, -0.38592f, -0.00249f, 0.14206f, -0.05799f, 0.98816f, 0.05f, false),
                        new AttackFrame(0.24009f, 1.03149f, -0.34992f, -0.00632f, 0.1213f, -0.05447f, 0.9911f, 0.05f, true),
                        new AttackFrame(0.26471f, 1.03706f, -0.31052f, -0.00998f, 0.09963f, -0.05034f, 0.9937f, 0.25f, true),
                        new AttackFrame(0.28809f, 1.04556f, -0.26984f, -0.01316f, 0.07849f, -0.04545f, 0.99579f, 0.5f, true),
                        new AttackFrame(0.30987f, 1.0572f, -0.23009f, -0.01558f, 0.05936f, -0.03954f, 0.99733f, 0.75f, true),
                        new AttackFrame(0.3304f, 1.07201f, -0.19348f, -0.01699f, 0.04374f, -0.03219f, 0.99838f, 1f, true),
                        new AttackFrame(0.35068f, 1.0901f, -0.16006f, -0.01578f, 0.03065f, -0.02364f, 0.99913f, 1f, true),
                        new AttackFrame(0.3697f, 1.10943f, -0.12977f, -0.01167f, 0.01876f, -0.01508f, 0.99964f, 1f, true),
                        new AttackFrame(0.3855f, 1.12689f, -0.10491f, -0.00646f, 0.00903f, -0.00753f, 0.99991f, 1f, true),
                        new AttackFrame(0.39636f, 1.13947f, -0.08802f, -0.00194f, 0.00244f, -0.0021f, 0.99999f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.39979f, 1.1435f, -0.08161f, -0.00075f, 0.00005f, -0.00046f, 1f, 1f, false),
                        new AttackFrame(0.3978f, 1.141f, -0.08196f, -0.00268f, 0.00045f, -0.00178f, 0.99999f, 1f, false),
                        new AttackFrame(0.39449f, 1.13664f, -0.08416f, -0.00527f, 0.00156f, -0.00388f, 0.99998f, 1f, false),
                        new AttackFrame(0.38981f, 1.13027f, -0.08951f, -0.00805f, 0.00374f, -0.00666f, 0.99994f, 1f, false),
                        new AttackFrame(0.38355f, 1.12172f, -0.09935f, -0.01054f, 0.00738f, -0.01011f, 0.99987f, 1f, false),
                        new AttackFrame(0.37528f, 1.11088f, -0.1151f, -0.0123f, 0.01289f, -0.01424f, 0.99974f, 1f, false),
                        new AttackFrame(0.36485f, 1.09812f, -0.14083f, -0.01254f, 0.02302f, -0.0186f, 0.99948f, 1f, false),
                        new AttackFrame(0.35141f, 1.08447f, -0.17763f, -0.0111f, 0.03922f, -0.02302f, 0.9989f, 1f, false),
                        new AttackFrame(0.33327f, 1.07101f, -0.22229f, -0.00847f, 0.05989f, -0.02793f, 0.99778f, 1f, false),
                        new AttackFrame(0.30944f, 1.05872f, -0.27122f, -0.00518f, 0.08344f, -0.03362f, 0.99593f, 1f, false),
                        new AttackFrame(0.28013f, 1.04834f, -0.32051f, -0.00173f, 0.10819f, -0.04006f, 0.99332f, 1f, false),
                        new AttackFrame(0.24707f, 1.04031f, -0.36634f, 0.00142f, 0.13242f, -0.04694f, 0.99008f, 1f, false),
                        new AttackFrame(0.21348f, 1.03468f, -0.40545f, 0.00398f, 0.15437f, -0.05366f, 0.98655f, 1f, false),
                        new AttackFrame(0.18371f, 1.03118f, -0.43541f, 0.00581f, 0.17225f, -0.05945f, 0.98324f, 1f, false),
                        new AttackFrame(0.16256f, 1.02937f, -0.4545f, 0.00692f, 0.18427f, -0.06349f, 0.9808f, 1f, false),
                        new AttackFrame(0.15463f, 1.02883f, -0.46126f, 0.0073f, 0.18866f, -0.06499f, 0.97986f, 1f, false)
                    },
                    "blue-fletched arrow nocked on the mesh bow (grip vertex) f5-13; no draw hand: male rig left hand is skinned to LeftUpLeg"),
                new AttackTrack(
                    "ROWAN_arrow_blue_fletch_inflight",
                    AttackTrackKind.Projectile,
                    "ROWAN_arrow_blue_fletch.fbx",
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
                    new[] { 0.03886f, 0.0343f, 0.8025f },
                    new[]
                    {
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 0.38488f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 0.85155f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 1.31822f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 1.78488f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 2.25155f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 2.71822f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 3.18488f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 3.65155f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 4.11822f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 4.58488f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 5.05155f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 5.51822f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 5.98488f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 6.45155f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 6.91822f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 7.38488f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "arrow flies character-forward from nock pose"),
                new AttackTrack(
                    "ROWAN_arrow_trail_vfx",
                    AttackTrackKind.Projectile,
                    "",
                    "",
                    0,
                    14,
                    14f,
                    AttackVfxShape.Trail,
                    0.05f,
                    0.9f,
                    new[] { 0.73536f, 0.85431f, 1f },
                    new[] { 0.22092f, 0.39588f, 0.85431f },
                    new[] { 0.73536f, 0.85431f, 1f },
                    new[] { 0.026f, 0.02473f, 0.9f },
                    new[]
                    {
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.40046f, 1.1443f, -0.08178f, 0f, 0f, 0f, 1f, 0.35f, true),
                        new AttackFrame(0.40046f, 1.1443f, 0.38488f, 0f, 0f, 0f, 1f, 0.56667f, true),
                        new AttackFrame(0.40046f, 1.1443f, 0.85155f, 0f, 0f, 0f, 1f, 0.78333f, true),
                        new AttackFrame(0.40046f, 1.1443f, 1.31822f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 1.78488f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 2.25155f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 2.71822f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 3.18488f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 3.65155f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 4.11822f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 4.58488f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 5.05155f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 5.51822f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 5.98488f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 6.45155f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 6.91822f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.40046f, 1.1443f, 7.38488f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "pale-blue trail behind arrow")
            });
    }
}
