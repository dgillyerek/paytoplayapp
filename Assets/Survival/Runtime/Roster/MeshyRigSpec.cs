using System;

namespace Survival.Domain.Roster
{
    /// <summary>
    /// One Meshy animation export (each Meshy FBX carries the full skinned character plus one take).
    /// Frames are in the FBX's own frame rate; Unity keys the clip by seconds.
    /// </summary>
    public readonly struct MeshyRigClip
    {
        public MeshyRigClip(
            string poseName,
            string fileName,
            string fileMd5,
            string meshyAnimation,
            string takeName,
            float frameRate,
            int lastFrame)
        {
            PoseName = poseName;
            FileName = fileName;
            FileMd5 = fileMd5;
            MeshyAnimation = meshyAnimation;
            TakeName = takeName;
            FrameRate = frameRate;
            LastFrame = lastFrame;
        }

        public string PoseName { get; }
        public string FileName { get; }
        public string FileMd5 { get; }

        /// <summary>Meshy library animation name, e.g. Idle_02, Walking, Archery_Shot.</summary>
        public string MeshyAnimation { get; }

        /// <summary>FBX AnimationStack name (Unity take name).</summary>
        public string TakeName { get; }

        public float FrameRate { get; }
        public int LastFrame { get; }
        public float Seconds => LastFrame / FrameRate;
    }

    /// <summary>
    /// A static prop held on a bone. <see cref="CharacterPose"/> is the prop root pose in character space
    /// (Unity convention: x, y, z, qx, qy, qz, qw) at <see cref="BindPose"/> frame <see cref="BindFrame"/>,
    /// derived from Design's hand offset. The player samples that frame, converts it to bone-local, and
    /// parents the prop to the bone, so it follows the hand with no per-frame code.
    /// </summary>
    public readonly struct MeshyHeldProp
    {
        public MeshyHeldProp(
            string name,
            string fileName,
            string fileMd5,
            string bone,
            string bindPose,
            int bindFrame,
            float[] characterPose)
        {
            Name = name;
            FileName = fileName;
            FileMd5 = fileMd5;
            Bone = bone;
            BindPose = bindPose;
            BindFrame = bindFrame;
            CharacterPose = characterPose;
        }

        public string Name { get; }
        public string FileName { get; }
        public string FileMd5 { get; }
        public string Bone { get; }
        public string BindPose { get; }
        public int BindFrame { get; }
        public float[] CharacterPose { get; }
    }

    /// <summary>
    /// A prop whose attack motion is baked per frame in character space (its own FBX take). Dropped
    /// under the character root unparented from any bone and sampled in sync with the attack clip.
    /// </summary>
    public readonly struct MeshyBakedProp
    {
        public MeshyBakedProp(string name, string fileName, string fileMd5, string takeName, int lastFrame)
        {
            Name = name;
            FileName = fileName;
            FileMd5 = fileMd5;
            TakeName = takeName;
            LastFrame = lastFrame;
        }

        public string Name { get; }
        public string FileName { get; }
        public string FileMd5 { get; }
        public string TakeName { get; }
        public int LastFrame { get; }
    }

    /// <summary>
    /// A Meshy auto-rigged character (Mixamo skeleton template) played as-is in a portrait Game view:
    /// rest = Meshy idle loop, walk loop, and an attack that plays once, holds its last frame
    /// (rest) for <see cref="RestGapSeconds"/>, then repeats (house attack pattern).
    /// Generic import, clips bind by bone name (every export has the same skeleton). Root motion off.
    /// <see cref="UseFileScale"/> follows the export units (raw Meshy cm exports need it; Design's
    /// metre re-exports do not). Stray meshes listed in <see cref="HiddenMeshNames"/> are hidden by name.
    /// Separate metallic and roughness maps pack into URP metallic (R) + smoothness (A).
    /// Rest/walk show <see cref="IdleProps"/> on their bones; the attack hides them and plays
    /// <see cref="AttackBakedProps"/> in sync (or <see cref="AttackFallbackProps"/> if a baked file fails).
    /// </summary>
    public sealed class MeshyRigSpec
    {
        public const string RestPoseName = BlenderRigSpec.RestPoseName;
        public const string WalkPoseName = "walk";
        public const string AttackPoseName = BlenderRigAttackSpec.PoseName;
        public const float RestGapSeconds = BlenderRigAttackSpec.RestGapSeconds;

        public MeshyRigSpec(
            string name,
            string title,
            string themePackDir,
            string designDir,
            MeshyRigClip rest,
            MeshyRigClip walk,
            MeshyRigClip attack,
            string armatureName,
            string boneRoot,
            string[] boneNames,
            string bodyMeshName,
            string[] hiddenMeshNames,
            string textureFolder,
            string baseColorFile,
            string normalFile,
            string metallicFile,
            string roughnessFile,
            bool useFileScale,
            MeshyHeldProp[] idleProps,
            MeshyBakedProp[] attackBakedProps,
            MeshyHeldProp[] attackFallbackProps,
            int attackReleaseFrame,
            float cameraYawDegrees,
            float cameraPitchDegrees)
        {
            Name = name;
            Title = title;
            ThemePackDir = themePackDir;
            DesignDir = designDir;
            Rest = rest;
            Walk = walk;
            Attack = attack;
            ArmatureName = armatureName;
            BoneRoot = boneRoot;
            BoneNames = boneNames;
            BodyMeshName = bodyMeshName;
            HiddenMeshNames = hiddenMeshNames;
            TextureFolder = textureFolder;
            BaseColorFile = baseColorFile;
            NormalFile = normalFile;
            MetallicFile = metallicFile;
            RoughnessFile = roughnessFile;
            UseFileScale = useFileScale;
            IdleProps = idleProps;
            AttackBakedProps = attackBakedProps;
            AttackFallbackProps = attackFallbackProps;
            AttackReleaseFrame = attackReleaseFrame;
            CameraYawDegrees = cameraYawDegrees;
            CameraPitchDegrees = cameraPitchDegrees;
        }

        public string Name { get; }
        public string Title { get; }
        public string ThemePackDir { get; }
        public string DesignDir { get; }
        public MeshyRigClip Rest { get; }
        public MeshyRigClip Walk { get; }
        public MeshyRigClip Attack { get; }
        public string ArmatureName { get; }
        public string BoneRoot { get; }
        public string[] BoneNames { get; }
        public string BodyMeshName { get; }
        public string[] HiddenMeshNames { get; }
        public string TextureFolder { get; }
        public string BaseColorFile { get; }
        public string NormalFile { get; }
        public string MetallicFile { get; }
        public string RoughnessFile { get; }

        /// <summary>True when the FBX is in cm with a 100x node scale (raw Meshy). False for metre exports.</summary>
        public bool UseFileScale { get; }

        /// <summary>Props shown in rest and walk (hidden while the attack plays).</summary>
        public MeshyHeldProp[] IdleProps { get; }

        /// <summary>Props baked per frame for the attack, sampled in sync with the body clip.</summary>
        public MeshyBakedProp[] AttackBakedProps { get; }

        /// <summary>Bone-held stand-ins used only if a baked attack prop cannot be loaded.</summary>
        public MeshyHeldProp[] AttackFallbackProps { get; }

        /// <summary>Arrow release frame in the attack clip (informational; the flight is baked).</summary>
        public int AttackReleaseFrame { get; }

        public float CameraYawDegrees { get; }
        public float CameraPitchDegrees { get; }

        public string[] PoseNames => new[] { RestPoseName, WalkPoseName, AttackPoseName };

        public MeshyRigClip[] Clips => new[] { Rest, Walk, Attack };

        public string DropdownObjectName => Name + "ClipDropdown";

        public string SceneFileName => Name + ".unity";

        public string TexturePath(string fileName) => ThemePackDir + "/" + TextureFolder + "/" + fileName;

        public float AttackCycleSeconds => Attack.Seconds + RestGapSeconds;

        public bool IsHiddenMesh(string objectName)
        {
            foreach (var hidden in HiddenMeshNames)
            {
                if (string.Equals(hidden, objectName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public MeshyRigClip? FindClip(string poseName)
        {
            foreach (var clip in Clips)
            {
                if (string.Equals(clip.PoseName, poseName, StringComparison.Ordinal))
                {
                    return clip;
                }
            }

            return null;
        }

        /// <summary>
        /// Attack clip time (seconds) for a time since the attack was picked: plays the whole take
        /// once, holds the last frame (rest) for <see cref="RestGapSeconds"/>, then repeats.
        /// </summary>
        public float AttackClipSeconds(float seconds, out bool inClip, out int cycle)
        {
            if (seconds < 0f)
            {
                seconds = 0f;
            }

            var length = Attack.Seconds;
            cycle = (int)Math.Floor(seconds / AttackCycleSeconds);
            var t = seconds - (cycle * AttackCycleSeconds);
            if (t <= length)
            {
                inClip = true;
                return t;
            }

            inClip = false;
            return length;
        }

        /// <summary>Spec the shared portrait demo uses for its title-less name, dropdown, and pose list.</summary>
        public BlenderRigSpec ToDemoSpec()
        {
            return new BlenderRigSpec(
                Name,
                ThemePackDir,
                Rest.FileName,
                Name + "_rig.fbx",
                Walk.PoseName,
                Walk.FileName,
                Walk.TakeName,
                Walk.Seconds,
                Walk.FrameRate,
                Walk.LastFrame,
                BlenderRigAvatar.CustomGeneric,
                BoneNames.Length,
                BoneNames,
                BoneRoot,
                TextureFolder,
                BaseColorFile,
                NormalFile,
                string.Empty,
                new[]
                {
                    new BlenderRigClip(
                        Attack.PoseName,
                        Attack.FileName,
                        Attack.TakeName,
                        Attack.Seconds,
                        Attack.FrameRate,
                        Attack.LastFrame)
                });
        }
    }
}
