using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Rowan A-pose rebuild (2026-10-14, #64): Derek-approved A-pose Meshy model (1.78M faces) decimated to 200k with its texture,
    /// 25-bone mixamorig rig (20 body bones, no ToeBase, + 5 left-hand finger bones) in Blender 5.2.2 (design .../rowan/fresh_20261013/work/apose_20261014). Face 100% on Head;
    /// right hand 100% on Hand; left hand = original Meshy shape with a 3-segment finger chain + 2-segment thumb (palm on LeftHand) keyed in a closed grip round the bow, lower forearm 100% on ForeArm, no arm vertex
    /// weighted to hips or torso (underarm sleeve shares with the arm-chain Shoulder bone so it never squashes into the ribs); belt 100% on Hips.
    /// Coat tails below the belt are a separate spring mesh (RowanCoat), never on the legs. Rest = arms down by the sides from the
    /// A-pose. Walk = 1 s in place at 30 fps, body bones only, head forward. Bow attack in RowanAttack. Imported Generic like the
    /// locked rigs. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class RowanMotion
    {
        public const string ThemePackDir = "ThemePack/fantasy_kingdom_a/art/heroes/3d/rowan";
        public const string DesignDir = "design/survival-theme-a-fantasy/heroes/anim/rowan/fresh_20261013";
        public const string RestFileName = "ROWAN_blenderig.fbx";
        public const string WalkFileName = "ROWAN_blenderig_walk.fbx";
        public const string RestThemePackRel = ThemePackDir + "/" + RestFileName;
        public const string WalkThemePackRel = ThemePackDir + "/" + WalkFileName;
        public const string AttackPoseName = BlenderRigAttackSpec.PoseName;
        public const string AttackFileName = RowanAttack.FileName;
        public const string RejectedMixamoFileName = "ROWAN_rig.fbx";
        public const string RestMd5 = "83d9eb753fe91ec6e1348fa3cab3fb35";
        public const string WalkMd5 = "29d9722d57d744539dccb6843eb2efdd";
        public const string AttackMd5 = RowanAttack.FileMd5;

        public const string RestPoseName = BlenderRigSpec.RestPoseName;
        public const string WalkPoseName = "walk";
        public const string WalkTakeName = "Scene";
        public const float WalkSeconds = 1f;
        public const float WalkFrameRate = 30f;
        public const int WalkLastFrame = 30;
        public const int BoneCount = 25;

        public const string TextureFolder = "ROWAN_blenderig.fbm";
        public const string BaseColorFile = "ROWAN_fresh_basecolor_0.jpg";
        public const string NormalFile = "ROWAN_fresh_normal_2.jpg";
        public const string MetallicRoughnessFile = "ROWAN_fresh_metal_rough_1.png";

        public const string BoneRoot = MixamoHumanoidBones.Root;

        // Attacks 2 and 3 (2026-10-18): Mixamo Pro Longbow clips (auto-rig of ROWAN.fbx keeps his 25 mixamorig names) retargeted
        // onto ROWAN_rig in Blender (design .../rowan/mixamo_attacks, work/scripts/archer_mixamo_retarget_20261018.py): the T-pose
        // Mixamo bind is matched bone by bone to the A-pose bind, body bones only (left finger grip from rest), hips drift removed
        // (net travel 0), 10-frame blend in with the left foot stepping forward into the side-on stance, 12-frame blend out. The draw
        // hand is held so the 0.795 m arrow always reaches past the riser (draw length capped at 0.74 m). The bow never takes the
        // Mixamo hand: it stays in the locked BowGrip and only rolls in it (8 -> 50 deg per shot, BlenderBowShots).
        public const string DesignMixamoAttacksDir = "design/survival-theme-a-fantasy/heroes/anim/rowan/mixamo_attacks";
        public const string Attack2PoseName = "attack2";
        public const string Attack2FileName = "ROWAN_blenderig_attack2_double_shot.fbx";
        public const string Attack2Md5 = "4f0d150a350d8dbb6e0f686dd4641e35";
        public const int Attack2LastFrame = 111;
        public const string Attack3PoseName = "attack3";
        public const string Attack3FileName = "ROWAN_blenderig_attack3_volley.fbx";
        public const string Attack3Md5 = "04418578f664edd228f159a13cae80fe";
        public const int Attack3LastFrame = 115;
        public const string ExtraTakeName = "Scene";

        /// <summary>Arrow flight time for the extra clips' shots: 4.2 m archer range at the locked 14 m/s, 9 frames.</summary>
        public const int ShotFlightFrames = 9;

        /// <summary>Double shot: Draw Arrow, Aim Recoil (loose f44), Draw Arrow from the reach, Aim Recoil (loose f80).</summary>
        public static readonly int[] Attack2ReleaseFrames = { 44, 80 };

        /// <summary>Volley: Draw Arrow, Aim Overdraw (held 40 frames, spine lifted 35 deg so the shot goes up), Aim Recoil (loose f84).</summary>
        public const int Attack3ReleaseFrame = 84;

        /// <summary>Arrow rest for the extra clips, Blender prop-local metres: archer's left (outer) side of the riser just above the
        /// fist (+X is his left at full draw on every shot frame, work/qc_*.json), like Bonequill's.</summary>
        public static readonly float[] ExtraArrowRest = { 0.03f, 0f, 0.06f };

        public static readonly BlenderBowShots Attack2BowShots = new BlenderBowShots(
            "ROWAN_arrow",
            ExtraArrowRest,
            new[]
            {
                new BlenderBowShot(22, 28, 44, 22, 36, 47, 53),
                new BlenderBowShot(60, 64, 80, 60, 72, 83, 93)
            });

        public static readonly BlenderBowShots Attack3BowShots = new BlenderBowShots(
            "ROWAN_arrow",
            ExtraArrowRest,
            new[] { new BlenderBowShot(22, 28, 84, 22, 36, 87, 97) });

        public static readonly string[] AttackPoseNames = { AttackPoseName, Attack2PoseName, Attack3PoseName };

        public static readonly string[] PoseNames = { RestPoseName, WalkPoseName, AttackPoseName, Attack2PoseName, Attack3PoseName };
        public static readonly string[] BoneNames =
        {
            "mixamorig:Hips",
            "mixamorig:Spine",
            "mixamorig:Spine1",
            "mixamorig:Spine2",
            "mixamorig:Neck",
            "mixamorig:Head",
            "mixamorig:LeftShoulder",
            "mixamorig:LeftArm",
            "mixamorig:LeftForeArm",
            "mixamorig:LeftHand",
            "mixamorig:RightShoulder",
            "mixamorig:RightArm",
            "mixamorig:RightForeArm",
            "mixamorig:RightHand",
            "mixamorig:LeftUpLeg",
            "mixamorig:LeftLeg",
            "mixamorig:LeftFoot",
            "mixamorig:RightUpLeg",
            "mixamorig:RightLeg",
            "mixamorig:RightFoot",
            "mixamorig:LeftHandFingers1",
            "mixamorig:LeftHandFingers2",
            "mixamorig:LeftHandFingers3",
            "mixamorig:LeftHandThumb1",
            "mixamorig:LeftHandThumb2"
        };

        /// <summary>Left-hand finger bones (v3): one 3-segment chain for the four fingers + a 2-segment thumb, all parented to LeftHand.</summary>
        public static readonly string[] LeftFingerBoneNames =
        {
            "mixamorig:LeftHandFingers1",
            "mixamorig:LeftHandFingers2",
            "mixamorig:LeftHandFingers3",
            "mixamorig:LeftHandThumb1",
            "mixamorig:LeftHandThumb2"
        };

        /// <summary>Single bow on LeftHand (see RowanAttack.IdleProps).</summary>
        public static BlenderRigIdlePropSpec[] IdleProps => RowanAttack.IdleProps;

        public static readonly BlenderRigSpec Spec = new BlenderRigSpec(
            "Rowan",
            ThemePackDir,
            RestFileName,
            RejectedMixamoFileName,
            WalkPoseName,
            WalkFileName,
            WalkTakeName,
            WalkSeconds,
            WalkFrameRate,
            WalkLastFrame,
            BlenderRigAvatar.CustomGeneric,
            BoneCount,
            BoneNames,
            BoneRoot,
            TextureFolder,
            BaseColorFile,
            NormalFile,
            MetallicRoughnessFile,
            new[]
            {
                new BlenderRigClip(
                    AttackPoseName,
                    AttackFileName,
                    BlenderRigAttackSpec.TakeName,
                    BlenderRigAttackSpec.ClipSeconds,
                    BlenderRigAttackSpec.FrameRate,
                    BlenderRigAttackSpec.LastFrame),
                new BlenderRigClip(Attack2PoseName, Attack2FileName, ExtraTakeName, Attack2LastFrame / WalkFrameRate, WalkFrameRate, Attack2LastFrame, Attack2BowShots),
                new BlenderRigClip(Attack3PoseName, Attack3FileName, ExtraTakeName, Attack3LastFrame / WalkFrameRate, WalkFrameRate, Attack3LastFrame, Attack3BowShots)
            });
    }
}
