using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Lyra Theme A fitted/tighten pack + attack (Design 2026-10-08). Staff Cast: Arcane Bolt — staff stays in RightHand, bolt released at f12 at 9 m/s toward top of screen.
    /// Rest, walk and attack FBXs replaced; 20 mixamorig bones (no ToeBase); bones bound by name.
    /// Frames baked from Design attack_meta / work blend in Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class LyraAttack
    {
        public const string FileName = "LYRA_blenderig_attack.fbx";
        public const string FileMd5 = "53fd21bbd3b1a479493b5c34898c7495";
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
            new[] { 0.02337f, 1.04778f, 0.00552f, 0f, 1.665f, 0f, -0.28053f, 1.01305f, 0.09289f, 0.49385f, 1.25844f, 0.07523f },
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
                        new AttackFrame(0.54847f, 1.29859f, 0.1199f, -0.00121f, 0.00841f, -0.03666f, 0.99929f, 1f, true),
                        new AttackFrame(0.5534f, 1.33942f, 0.08834f, -0.0452f, 0.02748f, -0.01944f, 0.99841f, 1f, true),
                        new AttackFrame(0.54781f, 1.39678f, 0.04537f, -0.10393f, 0.04196f, 0.00549f, 0.99368f, 1f, true),
                        new AttackFrame(0.52414f, 1.45808f, 0.00135f, -0.16218f, 0.04063f, 0.03032f, 0.98546f, 1f, true),
                        new AttackFrame(0.48489f, 1.50972f, -0.03326f, -0.20472f, 0.02216f, 0.04239f, 0.97765f, 1f, true),
                        new AttackFrame(0.44156f, 1.54229f, -0.05153f, -0.22127f, -0.00385f, 0.02848f, 0.97479f, 1f, true),
                        new AttackFrame(0.40739f, 1.55249f, -0.05107f, -0.20699f, -0.0221f, -0.02049f, 0.97788f, 1f, true),
                        new AttackFrame(0.39089f, 1.53938f, -0.03176f, -0.15653f, -0.0173f, -0.10765f, 0.98164f, 1f, true),
                        new AttackFrame(0.38469f, 1.5107f, 0.05786f, -0.0438f, -0.01812f, -0.18464f, 0.98166f, 1f, true),
                        new AttackFrame(0.3372f, 1.48653f, 0.21035f, 0.1172f, -0.04282f, -0.16184f, 0.9789f, 1f, true),
                        new AttackFrame(0.24303f, 1.4766f, 0.33233f, 0.23111f, -0.07015f, -0.05775f, 0.96867f, 1f, true),
                        new AttackFrame(0.18454f, 1.4664f, 0.38331f, 0.28267f, -0.07854f, 0.00516f, 0.95598f, 1f, true),
                        new AttackFrame(0.17895f, 1.44885f, 0.39703f, 0.31751f, -0.06807f, 0.00655f, 0.94579f, 1f, true),
                        new AttackFrame(0.17468f, 1.43558f, 0.4056f, 0.34022f, -0.06166f, 0.00705f, 0.9383f, 1f, true),
                        new AttackFrame(0.17167f, 1.42647f, 0.41088f, 0.35159f, -0.05905f, 0.00573f, 0.93427f, 1f, true),
                        new AttackFrame(0.1698f, 1.42084f, 0.41432f, 0.35261f, -0.0599f, 0.00239f, 0.93385f, 1f, true),
                        new AttackFrame(0.16897f, 1.41778f, 0.41699f, 0.34408f, -0.06383f, -0.00272f, 0.93676f, 1f, true),
                        new AttackFrame(0.16906f, 1.41629f, 0.41972f, 0.32654f, -0.07043f, -0.00902f, 0.94251f, 1f, true),
                        new AttackFrame(0.16994f, 1.41535f, 0.42317f, 0.30014f, -0.07927f, -0.01579f, 0.95046f, 1f, true),
                        new AttackFrame(0.1798f, 1.41241f, 0.42763f, 0.27121f, -0.0879f, -0.02703f, 0.95812f, 1f, true),
                        new AttackFrame(0.2058f, 1.40534f, 0.43128f, 0.2432f, -0.09417f, -0.04439f, 0.96437f, 1f, true),
                        new AttackFrame(0.24641f, 1.39386f, 0.43001f, 0.21204f, -0.09775f, -0.06368f, 0.97027f, 1f, true),
                        new AttackFrame(0.29885f, 1.37816f, 0.4186f, 0.17555f, -0.09751f, -0.0803f, 0.97633f, 1f, true),
                        new AttackFrame(0.35796f, 1.35929f, 0.39247f, 0.13505f, -0.09181f, -0.08995f, 0.98247f, 1f, true),
                        new AttackFrame(0.41645f, 1.33913f, 0.35023f, 0.09525f, -0.07957f, -0.09027f, 0.98815f, 1f, true),
                        new AttackFrame(0.4669f, 1.32002f, 0.29553f, 0.06177f, -0.06151f, -0.08218f, 0.9928f, 1f, true),
                        new AttackFrame(0.50451f, 1.30406f, 0.23653f, 0.03805f, -0.0405f, -0.06933f, 0.99605f, 1f, true),
                        new AttackFrame(0.52843f, 1.29249f, 0.18344f, 0.02411f, -0.02063f, -0.0562f, 0.99792f, 1f, true),
                        new AttackFrame(0.54087f, 1.28561f, 0.14594f, 0.0176f, -0.00615f, -0.04654f, 0.99874f, 1f, true),
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
                        new AttackFrame(0.15444f, 1.91172f, 0.65988f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.15444f, 1.91172f, 0.65988f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.15444f, 1.91172f, 0.65988f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.15444f, 1.91172f, 0.65988f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.15444f, 1.91172f, 0.65988f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.15444f, 1.91172f, 0.65988f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.15444f, 1.91172f, 0.65988f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.15444f, 1.91172f, 0.65988f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.15444f, 1.91172f, 0.65988f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.15444f, 1.91172f, 0.65988f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.15444f, 1.91172f, 0.65988f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.15444f, 1.91172f, 0.65988f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(0.15444f, 1.91172f, 0.65988f, 0f, 0f, 0f, 1f, 0.35f, true),
                        new AttackFrame(0.15444f, 1.91172f, 0.95988f, 0f, 0f, 0f, 1f, 0.56667f, true),
                        new AttackFrame(0.15444f, 1.91172f, 1.25988f, 0f, 0f, 0f, 1f, 0.78333f, true),
                        new AttackFrame(0.15444f, 1.91172f, 1.55988f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.15444f, 1.91172f, 1.85988f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.15444f, 1.91172f, 2.15988f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.15444f, 1.91172f, 2.45988f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.15444f, 1.91172f, 2.75988f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.15444f, 1.91172f, 3.05988f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.15444f, 1.91172f, 3.35988f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.15444f, 1.91172f, 3.65988f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.15444f, 1.91172f, 3.95988f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.15444f, 1.91172f, 4.25988f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.15444f, 1.91172f, 4.55988f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.15444f, 1.91172f, 4.85988f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.15444f, 1.91172f, 5.15988f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.15444f, 1.91172f, 5.45988f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.15444f, 1.91172f, 5.75988f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.15444f, 1.91172f, 6.05988f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "arcane bolt spawns at staff crystal, travels character-forward (-Y Blender)")
            });
    }
}
