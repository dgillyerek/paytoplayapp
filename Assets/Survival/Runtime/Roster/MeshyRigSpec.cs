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
    /// A Meshy auto-rigged character (Mixamo skeleton template) played as-is in a portrait Game view:
    /// rest = Meshy idle loop, walk loop, and an attack that plays once, holds its last frame
    /// (rest) for <see cref="RestGapSeconds"/>, then repeats (house attack pattern).
    /// Generic import, clips bind by bone name (every export has the same skeleton). Root motion off.
    /// Meshy FBX units are centimetres with a 100x node scale, so the FBX file scale is honoured.
    /// Stray meshes Meshy leaves in the export (e.g. an Icosphere) are hidden by name.
    /// Separate metallic and roughness maps pack into URP metallic (R) + smoothness (A).
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
            float attackYawDegrees,
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
            AttackYawDegrees = attackYawDegrees;
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

        /// <summary>
        /// Yaw applied to the whole character only while the attack plays, so the shot travels
        /// +Z (top of the screen in the rear camera). Rest and walk keep 0 (character faces +Z).
        /// </summary>
        public float AttackYawDegrees { get; }

        /// <summary>Approximate arrow release frame in the attack clip (informational).</summary>
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
