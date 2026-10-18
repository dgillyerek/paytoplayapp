namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Ironhowl Mixamo body. Bone root mixamorig:Hips. Generic import.
    /// Rest, walk, and attack are separate skinned FBX files on this skeleton.
    /// Rest hangs the arms at the sides with a slight elbow bend. It is not the T-pose body.
    /// Rest curve paths match walk: mixamorig:Hips is the root. No Armature wrapper.
    /// Take is mixamo.com at 30 fps (FBX TimeMode Frames30). Keys sit on every frame.
    /// Rest LocalStop 1539538600 ticks is frame 1. Walk LocalStop 47725696600 is frame 31.
    /// Attack LocalStop 40028003600 is frame 26. No mirror. No keyframe reduction.
    /// The look JPEG and the atlas are QC stills and are not bound.
    /// </summary>
    public static class IronhowlMotion
    {
        public const string ThemePackDir = "ThemePack/fantasy_kingdom_a/art/enemies/3d/ironhowl";
        public const string DesignMixamoDir = "design/survival-theme-a-fantasy/enemies/anim/ironhowl/mixamo";

        public const string BodyFileName = "IRONHOWL_rig.fbx";
        public const string RestFileName = "IRONHOWL_rig_rest.fbx";
        public const string WalkFileName = "IRONHOWL_rig_walk.fbx";
        public const string AttackFileName = "IRONHOWL_rig_attack.fbx";
        public const string AtlasFileName = "IRONHOWL_rig_atlas.png";
        public const string LookReferenceFileName = "Ironhowl_meshy_look.jpg";

        public const string BodyThemePackRel = ThemePackDir + "/" + BodyFileName;
        public const string RestThemePackRel = ThemePackDir + "/" + RestFileName;
        public const string WalkThemePackRel = ThemePackDir + "/" + WalkFileName;
        public const string AttackThemePackRel = ThemePackDir + "/" + AttackFileName;

        public const string BaseColorFileName = "ironhowl_basecolor.png";
        public const string NormalFileName = "ironhowl_normal.png";
        public const string MetallicFileName = "ironhowl_metallic.png";
        public const string RoughnessFileName = "ironhowl_roughness.png";

        public const string RestPoseName = "rest";
        public const string WalkPoseName = "walk";
        public const string AttackPoseName = "attack";

        /// <summary>
        /// Crossfade between poses (rest, walk, attack and back), Derek on #65: the rest to attack
        /// switch popped. The outgoing clip freezes on its current frame while the incoming one
        /// plays from frame 0 and takes over along <see cref="CrossfadeWeight"/>.
        /// </summary>
        public const float CrossfadeSeconds = 0.25f;

        /// <summary>
        /// Weight of the incoming pose, 0 at the switch and 1 after <see cref="CrossfadeSeconds"/>.
        /// Smootherstep (6t^5 - 15t^4 + 10t^3): zero slope and zero acceleration at both ends, so
        /// neither the start nor the end of the blend adds a velocity jump on any bone.
        /// </summary>
        public static float CrossfadeWeight(float secondsSinceSwitch)
        {
            if (secondsSinceSwitch <= 0f)
            {
                return 0f;
            }

            if (secondsSinceSwitch >= CrossfadeSeconds)
            {
                return 1f;
            }

            var t = secondsSinceSwitch / CrossfadeSeconds;
            return t * t * t * ((t * ((t * 6f) - 15f)) + 10f);
        }

        /// <summary>Verified AnimationStack on every Ironhowl Mixamo FBX. Not a Blender Scene take.</summary>
        public const string TakeName = "mixamo.com";

        public const float FrameRate = 30f;
        public const int RestLastFrame = 1;
        public const float RestSeconds = 1f / 30f;
        public const int WalkLastFrame = 31;
        public const float WalkSeconds = 31f / 30f;
        public const int AttackLastFrame = 26;
        public const float AttackSeconds = 26f / 30f;
        public const bool Mirror = false;

        public const string BodyMd5 = "2076255b828787ba8db7ea24d075f231";
        public const string RestMd5 = "3c04265d7dc6d25693c373f8cb4e5e27";
        public const string WalkMd5 = "8c49b0f95c70418c2e7938036080040d";
        public const string AttackMd5 = "21cd216d46c3363ad9af8993a92daba3";
        public const string AtlasMd5 = "7c4b6b14ef204acf51139789b76d498d";

        public const string BoneRoot = "mixamorig:Hips";
        public const string SceneName = "Ironhowl";

        public static readonly string[] PoseNames = { RestPoseName, WalkPoseName, AttackPoseName };

        // Attack rotation (2026-10-17, Derek: humanoids get 3 premade Mixamo attacks). Downloaded on
        // the IRONHOWL_UPLOAD Mixamo character: FBX for Unity, without skin, 30 fps, no keyframe
        // reduction, no mirror. Same 33-bone mixamorig skeleton as IRONHOWL_rig, so they play on the
        // body as-is (generic, no retarget). Mixamo offered no In Place for either, so the battle
        // holds the jump's hips travel on the spot (LockedBattleAttack.InPlace); the files are kept
        // byte-identical to the downloads.
        public const string DesignMixamoAttacksDir = "design/survival-theme-a-fantasy/enemies/anim/ironhowl/mixamo_attacks";

        /// <summary>Attack 2, ground slam: Mixamo "Mutant Jump Attack". Leaps 1.79 m forward (hips) and slams both claws.</summary>
        public const string Attack2PoseName = "attack2";
        public const string Attack2FileName = "IRONHOWL_mixamo_mutant_jump_attack.fbx";
        public const string Attack2ThemePackRel = ThemePackDir + "/" + Attack2FileName;
        public const string Attack2Md5 = "2ce6f751c01e4cf9f1112fe87a24ffdf";
        public const int Attack2LastFrame = 111;

        /// <summary>Claws hit the ground (0.01-0.04 m up, 0.7-0.9 m ahead of the in-place hips) on frame 54.</summary>
        public const int Attack2ContactFrame = 54;

        /// <summary>Attack 3, roar: Mixamo "Mutant Roaring". Leans 0.3 m in and comes back to its start pose.</summary>
        public const string Attack3PoseName = "attack3";
        public const string Attack3FileName = "IRONHOWL_mixamo_mutant_roaring.fbx";
        public const string Attack3ThemePackRel = ThemePackDir + "/" + Attack3FileName;
        public const string Attack3Md5 = "74863a8be8a6b080bb3be28d75d38cd0";
        public const int Attack3LastFrame = 162;

        /// <summary>Roar peak (hips leaned furthest in, arms flung up) on frame 42.</summary>
        public const int Attack3ContactFrame = 42;

        public static readonly string[] AttackPoseNames = { AttackPoseName, Attack2PoseName, Attack3PoseName };
    }
}
