using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Bonequill gritty rebuild (2026-10-13, replaces the fresh_20261013 build in #62): Derek-approved gritty purple look
    /// (weathered bone, battered iron armour, tattered purple hooded cloak), Meshy body decimated to 200k faces plus a modelled
    /// quiver with bone arrows rigid on Spine2, same mixamorig rig method (20 body bones + 5 left-hand grip bones: finger chain x3, thumb x2, Meshy hand mesh unsculpted) (design .../bonequill/gritty_20261013). Skull and face 100% on Head, loincloth/belt/pelvis 100% on Hips
    /// (never the legs), bracers and boots rigid on their bones, Hips to UpLeg ramp 6 cm. Rest = arms relaxed down by the sides.
    /// Walk = 1 s in-place Scene take at 30 fps, ankles about 30 cm apart, head forward with a 0-1.8 deg bob. Cape is a separate
    /// FBX on runtime springs (<see cref="BonequillCape"/>). Imported Generic like the locked rigs. Replaces #47 / #34.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class BonequillMotion
    {
        public const string ThemePackDir = "ThemePack/fantasy_kingdom_a/art/enemies/3d/bonequill";
        public const string DesignDir = "design/survival-theme-a-fantasy/enemies/anim/bonequill/gritty_20261013";
        public const string RestFileName = "BONEQUILL_blenderig.fbx";
        public const string WalkFileName = "BONEQUILL_blenderig_walk.fbx";
        public const string RestThemePackRel = ThemePackDir + "/" + RestFileName;
        public const string WalkThemePackRel = ThemePackDir + "/" + WalkFileName;
        public const string AttackPoseName = BlenderRigAttackSpec.PoseName;
        public const string AttackFileName = BonequillAttack.FileName;
        public const string RejectedMixamoFileName = "BONEQUILL_rig.fbx";
        public const string RestMd5 = "8059d49ac54bd1fa0ea6030d23c3f934";
        public const string WalkMd5 = "f88e1cddcf0c72ec93d155939c69a093";
        public const string AttackMd5 = BonequillAttack.FileMd5;

        public const string RestPoseName = BlenderRigSpec.RestPoseName;
        public const string WalkPoseName = "walk";
        public const string WalkTakeName = "Scene";
        public const float WalkSeconds = 1f;
        public const float WalkFrameRate = 30f;
        public const int WalkLastFrame = 30;
        public const int BoneCount = 25;   // 20 Mixamo body bones + 5 left-hand grip bones (finger chain x3, thumb x2)

        public const string TextureFolder = "BONEQUILL_blenderig.fbm";
        public const string BaseColorFile = "BONEQUILL_fresh_basecolor_0.jpg";
        public const string NormalFile = "BONEQUILL_fresh_normal_2.jpg";
        public const string MetallicRoughnessFile = "BONEQUILL_fresh_metal_rough_1.png";

        public const string BoneRoot = MixamoHumanoidBones.Root;

        // Attacks 2 and 3 (2026-10-18): Mixamo Pro Longbow clips (auto-rig of BONEQUILL.fbx keeps his 25 mixamorig names) retargeted
        // onto BONEQUILL_rig (design .../bonequill/mixamo_attacks, same pipeline as Rowan's): T-pose Mixamo bind matched to the rig bind,
        // body bones only (left finger grip from the attack rest), hips drift removed, left-foot step into the side-on stance. The
        // right hand reaches back over the right shoulder to his quiver and the arrow appears in it there; it is nocked on the
        // archer's left side of the riser and the drawn string meets its nock. The release is Mixamo "Standing Aim Recoil" from Rowan's
        // download (no Bonequill recoil was downloaded); the bow stays in the locked BowGrip and only rolls in it (10.5 -> 30 deg).
        public const string DesignMixamoAttacksDir = "design/survival-theme-a-fantasy/enemies/anim/bonequill/mixamo_attacks";
        public const string Attack2PoseName = "attack2";
        public const string Attack2FileName = "BONEQUILL_blenderig_attack2_rapid_quiver_shot.fbx";
        public const string Attack2Md5 = "e9e2dc2fa00b62316e59c8e5019abf04";
        public const int Attack2LastFrame = 67;
        public const string Attack3PoseName = "attack3";
        public const string Attack3FileName = "BONEQUILL_blenderig_attack3_bone_volley.fbx";
        public const string Attack3Md5 = "1cc81ee7d22f0b982d0e5394939e8a34";
        public const int Attack3LastFrame = 115;
        public const string ExtraTakeName = "Scene";

        /// <summary>Arrow flight time for the extra clips' shots: 4.2 m archer range at the locked 14 m/s, 9 frames.</summary>
        public const int ShotFlightFrames = 9;

        /// <summary>Rapid quiver shot: Draw Arrow played 1.3x faster, Aim Recoil (loose f36).</summary>
        public const int Attack2ReleaseFrame = 36;

        /// <summary>Bone volley: Draw Arrow, Aim Overdraw (40 frames, spine lifted 35 deg), Aim Recoil (loose f84).</summary>
        public const int Attack3ReleaseFrame = 84;

        public static readonly BlenderBowShots Attack2BowShots = new BlenderBowShots(
            "BONEQUILL_bonearrow_purple_fletch",
            new[] { 0.033f, 0f, 0.0574f },
            new[] { new BlenderBowShot(19, 24, 36, 19, 30, 39, 49) });

        public static readonly BlenderBowShots Attack3BowShots = new BlenderBowShots(
            "BONEQUILL_bonearrow_purple_fletch",
            new[] { 0.033f, 0f, 0.0574f },
            new[] { new BlenderBowShot(21, 28, 84, 21, 36, 87, 97) });

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

        /// <summary>Bow-hand grip bones (not Mixamo): one combined chain for the four fingers and a two-segment thumb, parented to LeftHand.</summary>
        public static readonly string[] LeftGripBoneNames =
        {
            "mixamorig:LeftHandFingers1",
            "mixamorig:LeftHandFingers2",
            "mixamorig:LeftHandFingers3",
            "mixamorig:LeftHandThumb1",
            "mixamorig:LeftHandThumb2"
        };

        public static readonly BlenderRigSpec Spec = new BlenderRigSpec(
            "Bonequill",
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
