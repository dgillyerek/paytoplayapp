using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Lyra Theme A fitted/tighten pack + attack (Design 2026-10-08). Staff Cast: Arcane Bolt — staff stays in RightHand, bolt released at f12 at 9 m/s toward top of screen.
    /// Rest, walk and attack FBXs replaced; 22 mixamorig bones; bones bound by name.
    /// Frames baked from Design attack_meta / work blend in Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class LyraAttack
    {
        public const string FileName = "LYRA_blenderig_attack.fbx";
        public const string FileMd5 = "64d8b8d405044e28945a309f36aecef8";
        public const string DesignDir = "design/survival-theme-a-fantasy/heroes/anim/lyra/attack_20261008";
        public const int ReleaseFrame = 12;
        public const bool InPlace = false;

        /// <summary>Prop FBXs copied next to the character FBXs, with Design MD5s.</summary>
        public static readonly string[] PropFileNames =
        {
            "LYRA_staff.fbx"
        };

        public static readonly string[] PropFileMd5s =
        {
            "43aef847341f2e86f88a3020b32bae72"
        };

        public static readonly BlenderRigAttackSpec Spec = new BlenderRigAttackSpec(
            "Lyra",
            FileName,
            FileMd5,
            DesignDir,
            ReleaseFrame,
            InPlace,
            new[] { "mixamorig:Hips", "mixamorig:Head", "mixamorig:LeftHand", "mixamorig:RightHand" },
            new[] { 0.00357f, 1.06126f, 0.05368f, 0f, 1.665f, 0f, -0.265f, 1.045f, 0.09f, 0.4906f, 1.26479f, 0.07435f },
            new[]
            {
                new AttackTrack(
                    "LYRA_staff",
                    AttackTrackKind.Held,
                    "LYRA_staff.fbx",
                    "mixamorig:RightHand",
                    0,
                    0,
                    0f,
                    AttackVfxShape.None,
                    0f,
                    0f,
                    new float[0],
                    new float[0],
                    new float[0],
                    new[] { 0.27652f, 1.88927f, 0.11727f },
                    new[]
                    {
                        new AttackFrame(0.54468f, 1.2833f, 0.13197f, 0.01582f, -0.00068f, -0.04293f, 0.99895f, 1f, true),
                        new AttackFrame(0.54791f, 1.29837f, 0.11988f, -0.00121f, 0.00841f, -0.03666f, 0.99929f, 1f, true),
                        new AttackFrame(0.5514f, 1.33857f, 0.08832f, -0.0452f, 0.02748f, -0.01944f, 0.99841f, 1f, true),
                        new AttackFrame(0.54385f, 1.3949f, 0.04542f, -0.10393f, 0.04196f, 0.00549f, 0.99368f, 1f, true),
                        new AttackFrame(0.51804f, 1.45483f, 0.00153f, -0.16218f, 0.04063f, 0.03032f, 0.98546f, 1f, true),
                        new AttackFrame(0.47678f, 1.50482f, -0.033f, -0.20472f, 0.02216f, 0.04239f, 0.97765f, 1f, true),
                        new AttackFrame(0.43169f, 1.53564f, -0.05147f, -0.22127f, -0.00385f, 0.02848f, 0.97479f, 1f, true),
                        new AttackFrame(0.39608f, 1.5442f, -0.05168f, -0.20699f, -0.0221f, -0.02049f, 0.97788f, 1f, true),
                        new AttackFrame(0.37845f, 1.52989f, -0.03372f, -0.15653f, -0.0173f, -0.10765f, 0.98164f, 1f, true),
                        new AttackFrame(0.37481f, 1.50166f, 0.05333f, -0.0438f, -0.01812f, -0.18464f, 0.98166f, 1f, true),
                        new AttackFrame(0.3359f, 1.47989f, 0.20338f, 0.1172f, -0.04282f, -0.16183f, 0.9789f, 1f, true),
                        new AttackFrame(0.25185f, 1.47316f, 0.3257f, 0.23111f, -0.07015f, -0.05775f, 0.96867f, 1f, true),
                        new AttackFrame(0.19812f, 1.46513f, 0.37759f, 0.28267f, -0.07854f, 0.00516f, 0.95598f, 1f, true),
                        new AttackFrame(0.19269f, 1.44851f, 0.39095f, 0.31751f, -0.06807f, 0.00655f, 0.94579f, 1f, true),
                        new AttackFrame(0.18851f, 1.4359f, 0.39933f, 0.34022f, -0.06166f, 0.00705f, 0.9383f, 1f, true),
                        new AttackFrame(0.18551f, 1.42719f, 0.40455f, 0.35159f, -0.05905f, 0.00573f, 0.93427f, 1f, true),
                        new AttackFrame(0.18357f, 1.42176f, 0.40801f, 0.35261f, -0.0599f, 0.00239f, 0.93385f, 1f, true),
                        new AttackFrame(0.18257f, 1.41874f, 0.41079f, 0.34408f, -0.06383f, -0.00272f, 0.93676f, 1f, true),
                        new AttackFrame(0.18241f, 1.4172f, 0.41371f, 0.32654f, -0.07043f, -0.00902f, 0.94251f, 1f, true),
                        new AttackFrame(0.18294f, 1.41621f, 0.41744f, 0.30014f, -0.07927f, -0.01579f, 0.95046f, 1f, true),
                        new AttackFrame(0.19204f, 1.41318f, 0.42213f, 0.27121f, -0.0879f, -0.02703f, 0.95812f, 1f, true),
                        new AttackFrame(0.21666f, 1.40597f, 0.42589f, 0.2432f, -0.09417f, -0.04439f, 0.96437f, 1f, true),
                        new AttackFrame(0.25544f, 1.39435f, 0.42481f, 0.21204f, -0.09775f, -0.06368f, 0.97027f, 1f, true),
                        new AttackFrame(0.30583f, 1.37855f, 0.41382f, 0.17555f, -0.09751f, -0.0803f, 0.97633f, 1f, true),
                        new AttackFrame(0.36292f, 1.35961f, 0.3884f, 0.13505f, -0.09181f, -0.08995f, 0.98247f, 1f, true),
                        new AttackFrame(0.41965f, 1.33942f, 0.34711f, 0.09525f, -0.07957f, -0.09027f, 0.98815f, 1f, true),
                        new AttackFrame(0.46876f, 1.32029f, 0.29342f, 0.06177f, -0.06151f, -0.08218f, 0.9928f, 1f, true),
                        new AttackFrame(0.50545f, 1.30427f, 0.23531f, 0.03805f, -0.0405f, -0.06933f, 0.99605f, 1f, true),
                        new AttackFrame(0.52881f, 1.29261f, 0.18289f, 0.02411f, -0.02063f, -0.0562f, 0.99792f, 1f, true),
                        new AttackFrame(0.54097f, 1.28565f, 0.1458f, 0.0176f, -0.00615f, -0.04654f, 0.99874f, 1f, true),
                        new AttackFrame(0.54468f, 1.2833f, 0.13197f, 0.01582f, -0.00068f, -0.04293f, 0.99895f, 1f, true)
                    },
                    "staff grip in RightHand (constant offset)"),
                new AttackTrack(
                    "LYRA_arcane_bolt_vfx",
                    AttackTrackKind.Projectile,
                    "",
                    "",
                    0,
                    12,
                    9f,
                    AttackVfxShape.Orb,
                    0.338f,
                    1.03f,
                    new[] { 0.93092f, 0.96398f, 1f },
                    new[] { 0.48453f, 0.70141f, 1f },
                    new[] { 1f, 0.86504f, 0.58383f },
                    new[] { 0.338f, 0.338f, 1.03096f },
                    new[]
                    {
                        new AttackFrame(0.16802f, 1.91045f, 0.65416f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16802f, 1.91045f, 0.65416f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16802f, 1.91045f, 0.65416f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16802f, 1.91045f, 0.65416f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16802f, 1.91045f, 0.65416f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16802f, 1.91045f, 0.65416f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16802f, 1.91045f, 0.65416f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16802f, 1.91045f, 0.65416f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16802f, 1.91045f, 0.65416f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16802f, 1.91045f, 0.65416f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16802f, 1.91045f, 0.65416f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16802f, 1.91045f, 0.65416f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.16802f, 1.91045f, 0.65416f, 0f, 0f, 0f, 1f, 0.35f, true),
                        new AttackFrame(0.16802f, 1.91045f, 0.95416f, 0f, 0f, 0f, 1f, 0.56667f, true),
                        new AttackFrame(0.16802f, 1.91045f, 1.25416f, 0f, 0f, 0f, 1f, 0.78333f, true),
                        new AttackFrame(0.16802f, 1.91045f, 1.55416f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16802f, 1.91045f, 1.85416f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16802f, 1.91045f, 2.15416f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16802f, 1.91045f, 2.45416f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16802f, 1.91045f, 2.75416f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16802f, 1.91045f, 3.05416f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16802f, 1.91045f, 3.35416f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16802f, 1.91045f, 3.65416f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16802f, 1.91045f, 3.95416f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16802f, 1.91045f, 4.25416f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16802f, 1.91045f, 4.55416f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16802f, 1.91045f, 4.85416f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16802f, 1.91045f, 5.15416f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16802f, 1.91045f, 5.45416f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16802f, 1.91045f, 5.75416f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.16802f, 1.91045f, 6.05416f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "arcane bolt spawns at staff crystal, travels character-forward (-Y Blender)")
            });
    }
}
