using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Oakenshield thorn spear attack on the fresh rig (2026-10-12). Beats carried over from #48: feet planted, spear conjured in
    /// RightHand f3-8 (scales up), cocked beside the right shoulder at f10 with the chest turned 26 deg to her right, RightHand held at
    /// its f10 rotation f8-14 so the spear keeps pointing forward while the arm whips over, thrown at f14 (11 m/s, character-forward),
    /// follow-through f20, back to rest by f30. Head stays forward (Head bone kept at its rest orientation every frame).
    /// Held frames = RightHand grip 7 cm along the hand, bound at f10. Frames baked in Unity convention (character forward = +Z).
    /// 2026-10-15 (Derek on #65: "turn sideways and step forward with her LEFT foot before throwing"): side-step take
    /// (design work/sidestep.py on OAKENSHIELD_fresh.blend). Hips turn side-on to her right (-62 deg at f10, left shoulder to the
    /// target) while the LEFT foot lifts f2 and plants 32 cm forward at f8 (toe turned 25 deg in), weight shifts onto it as the
    /// hips drop 7-8 cm and slide 15 cm forward; the hips whip back square (-8 deg) for the f14 release, the left foot steps back
    /// f21-28. The right (back) foot pivots on its ball (contact point fixed). Legs solved by two-bone IK (planted foot drift
    /// under 4 mm). Head bone kept at its rest orientation every frame, so the face (100% Head) stays locked and facing the
    /// target; leaf and spike pieces stay rigid on their single bones (weights unchanged). Frame 0 and 30 match the old take
    /// (and rest) to 0.0001 deg. Spear grip and release re-baked from the new FBX: released at f14 from (0.243, 1.494, 0.568).
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class OakenshieldAttack
    {
        public const string FileName = "OAKENSHIELD_blenderig_attack.fbx";
        public const string FileMd5 = "3907605b95f42cf77d1a786b42a8e56e";
        public const string DesignDir = "design/survival-theme-a-fantasy/heroes/anim/oakenshield/fresh_20261012";
        public const int ReleaseFrame = 14;
        public const bool InPlace = false;

        /// <summary>Prop FBXs copied next to the character FBXs (thorn spear from #48 attack_20261007, unchanged).</summary>
        public static readonly string[] PropFileNames =
        {
            "OAKENSHIELD_thornspear.fbx"
        };

        public static readonly string[] PropFileMd5s =
        {
            "0b7ebca1eb1946a850320fc44e65c1f5"
        };

        public static readonly BlenderRigAttackSpec Spec = new BlenderRigAttackSpec(
            "Oakenshield",
            FileName,
            FileMd5,
            DesignDir,
            ReleaseFrame,
            InPlace,
            new[] { "mixamorig:Hips", "mixamorig:Head", "mixamorig:LeftHand", "mixamorig:RightHand" },
            new[] { 0f, 0.95f, 0f, 0f, 1.455f, 0f, -0.48f, 0.945f, 0.02f, 0.48f, 0.945f, 0.02f },
            new[]
            {
                new AttackTrack(
                    "OAKENSHIELD_thornspear",
                    AttackTrackKind.Held,
                    "OAKENSHIELD_thornspear.fbx",
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
                    new[] { 0.12781f, 0.13377f, 1.40074f },
                    new[]
                    {
                        new AttackFrame(0.36195f, 0.79451f, 0.04801f, -0.08661f, -0.16955f, -0.95129f, 0.24248f, 0.05f, false),
                        new AttackFrame(0.37689f, 0.80365f, 0.07404f, -0.10776f, -0.22837f, -0.93427f, 0.25172f, 0.05f, false),
                        new AttackFrame(0.40377f, 0.86143f, 0.15829f, -0.13745f, -0.41918f, -0.85887f, 0.26025f, 0.05f, false),
                        new AttackFrame(0.39241f, 1.00787f, 0.24185f, -0.12847f, -0.67676f, -0.6889f, 0.22563f, 0.05f, true),
                        new AttackFrame(0.35341f, 1.12216f, 0.2493f, -0.11266f, -0.81151f, -0.54832f, 0.16764f, 0.24f, true),
                        new AttackFrame(0.33589f, 1.15732f, 0.24347f, -0.1101f, -0.84539f, -0.50345f, 0.14045f, 0.43f, true),
                        new AttackFrame(0.37931f, 1.19773f, 0.199f, -0.08592f, -0.75017f, -0.39626f, 0.52234f, 0.62f, true),
                        new AttackFrame(0.41347f, 1.3436f, 0.03316f, -0.01965f, -0.2494f, -0.09033f, 0.96398f, 0.81f, true),
                        new AttackFrame(0.34256f, 1.57267f, -0.21263f, -0.05968f, 0f, 0f, 0.99822f, 1f, true),
                        new AttackFrame(0.21407f, 1.65602f, -0.32266f, -0.05968f, 0f, 0f, 0.99822f, 1f, true),
                        new AttackFrame(0.17035f, 1.66483f, -0.34754f, -0.05968f, 0f, 0f, 0.99822f, 1f, true),
                        new AttackFrame(0.22325f, 1.69216f, -0.23759f, -0.05968f, 0f, 0f, 0.99822f, 1f, true),
                        new AttackFrame(0.29598f, 1.68731f, 0.11907f, -0.05968f, 0f, 0f, 0.99822f, 1f, true),
                        new AttackFrame(0.27383f, 1.55763f, 0.45885f, -0.05968f, 0f, 0f, 0.99822f, 1f, true),
                        new AttackFrame(0.24322f, 1.49437f, 0.56812f, -0.05968f, 0f, 0f, 0.99822f, 1f, false),
                        new AttackFrame(0.25431f, 1.46895f, 0.59731f, 0.02801f, -0.11385f, -0.08081f, 0.98981f, 1f, false),
                        new AttackFrame(0.26227f, 1.36425f, 0.67112f, 0.20071f, -0.39038f, -0.30944f, 0.84354f, 1f, false),
                        new AttackFrame(0.21459f, 1.17008f, 0.71395f, 0.27606f, -0.61947f, -0.59085f, 0.43697f, 1f, false),
                        new AttackFrame(0.14615f, 1.00446f, 0.66652f, 0.21842f, -0.62806f, -0.73994f, 0.1016f, 1f, false),
                        new AttackFrame(0.11415f, 0.91877f, 0.6068f, 0.16076f, -0.5764f, -0.80076f, 0.0264f, 1f, false),
                        new AttackFrame(0.10312f, 0.89289f, 0.58186f, 0.14188f, -0.55373f, -0.82051f, 0.00326f, 1f, false),
                        new AttackFrame(0.10999f, 0.88665f, 0.57123f, 0.13544f, -0.54481f, -0.82752f, 0.00682f, 1f, false),
                        new AttackFrame(0.1294f, 0.86971f, 0.54068f, 0.11741f, -0.51935f, -0.84627f, 0.01784f, 1f, false),
                        new AttackFrame(0.15976f, 0.8455f, 0.49094f, 0.08968f, -0.47861f, -0.87262f, 0.03774f, 1f, false),
                        new AttackFrame(0.19862f, 0.81921f, 0.42302f, 0.05497f, -0.42481f, -0.90108f, 0.06755f, 1f, false),
                        new AttackFrame(0.24155f, 0.79738f, 0.34103f, 0.0175f, -0.36294f, -0.92563f, 0.10572f, 1f, false),
                        new AttackFrame(0.28266f, 0.78504f, 0.2537f, -0.0178f, -0.30078f, -0.94209f, 0.14724f, 1f, false),
                        new AttackFrame(0.31689f, 0.78273f, 0.17227f, -0.04712f, -0.24628f, -0.95008f, 0.18569f, 1f, false),
                        new AttackFrame(0.34175f, 0.78677f, 0.10661f, -0.06867f, -0.20476f, -0.9522f, 0.21606f, 1f, false),
                        new AttackFrame(0.35678f, 0.7921f, 0.06352f, -0.08197f, -0.17869f, -0.95177f, 0.23556f, 1f, false),
                        new AttackFrame(0.36195f, 0.79451f, 0.04801f, -0.08661f, -0.16955f, -0.95129f, 0.24248f, 1f, false)
                    },
                    "thorn spear conjured f3-8 in RightHand, thrown at release"),
                new AttackTrack(
                    "OAKENSHIELD_thornspear_inflight",
                    AttackTrackKind.Projectile,
                    "OAKENSHIELD_thornspear.fbx",
                    "",
                    0,
                    14,
                    11f,
                    AttackVfxShape.Trail,
                    0.05f,
                    1.4f,
                    new[] { 1f, 0.86504f, 0.58383f },
                    new[] { 0.2478f, 0.43663f, 0.88083f },
                    new[] { 1f, 0.86504f, 0.58383f },
                    new[] { 0.12781f, 0.13377f, 1.40074f },
                    new[]
                    {
                        new AttackFrame(0.24322f, 1.49437f, 0.56812f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.24322f, 1.49437f, 0.56812f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.24322f, 1.49437f, 0.56812f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.24322f, 1.49437f, 0.56812f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.24322f, 1.49437f, 0.56812f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.24322f, 1.49437f, 0.56812f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.24322f, 1.49437f, 0.56812f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.24322f, 1.49437f, 0.56812f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.24322f, 1.49437f, 0.56812f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.24322f, 1.49437f, 0.56812f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.24322f, 1.49437f, 0.56812f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.24322f, 1.49437f, 0.56812f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.24322f, 1.49437f, 0.56812f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.24322f, 1.49437f, 0.56812f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(0.24322f, 1.49437f, 0.56812f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.24322f, 1.49437f, 0.93479f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.24322f, 1.49437f, 1.30145f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.24322f, 1.49437f, 1.66812f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.24322f, 1.49437f, 2.03479f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.24322f, 1.49437f, 2.40145f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.24322f, 1.49437f, 2.76812f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.24322f, 1.49437f, 3.13479f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.24322f, 1.49437f, 3.50145f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.24322f, 1.49437f, 3.86812f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.24322f, 1.49437f, 4.23479f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.24322f, 1.49437f, 4.60145f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.24322f, 1.49437f, 4.96812f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.24322f, 1.49437f, 5.33479f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.24322f, 1.49437f, 5.70145f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.24322f, 1.49437f, 6.06812f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.24322f, 1.49437f, 6.43479f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "thrown thorn spear, travels character-forward")
            });
    }
}
