using System;
using System.Collections.Generic;
using Survival.Domain.Heroes;

namespace Survival.Domain.Roster
{
    /// <summary>
    /// How Unity should import a Design Blender rig.
    /// Mixamo humanoid is used only when the avatar validates. Custom creatures stay Generic.
    /// </summary>
    public enum BlenderRigAvatar
    {
        MixamoHumanoid = 0,
        CustomGeneric = 1
    }

    /// <summary>
    /// One extra action clip beyond the primary clip on a Design Blender rig.
    /// </summary>
    public readonly struct BlenderRigClip
    {
        public BlenderRigClip(
            string poseName,
            string fileName,
            string takeName,
            float seconds,
            float frameRate,
            int lastFrame)
        {
            PoseName = poseName;
            FileName = fileName;
            TakeName = takeName;
            Seconds = seconds;
            FrameRate = frameRate;
            LastFrame = lastFrame;
        }

        public string PoseName { get; }
        public string FileName { get; }
        public string TakeName { get; }
        public float Seconds { get; }
        public float FrameRate { get; }
        public int LastFrame { get; }
    }

    /// <summary>
    /// One Design Blender-rigged character: rest FBX plus one or more action clips.
    /// The first clip stays on the original fields. Further clips are ExtraClips.
    /// Metal/roughness is optional and stays empty unless the FBX embedded it.
    /// </summary>
    public readonly struct BlenderRigSpec
    {
        public BlenderRigSpec(
            string name,
            string themePackDir,
            string restFileName,
            string rejectedMixamoFileName,
            string clipPoseName,
            string clipFileName,
            string clipTakeName,
            float clipSeconds,
            float clipFrameRate,
            int clipLastFrame,
            BlenderRigAvatar avatar,
            int boneCount,
            string[] boneNames,
            string boneRoot,
            string textureFolder,
            string baseColorFile,
            string normalFile,
            string metallicRoughnessFile,
            BlenderRigClip[]? extraClips = null)
        {
            Name = name;
            ThemePackDir = themePackDir;
            RestFileName = restFileName;
            RejectedMixamoFileName = rejectedMixamoFileName;
            ClipPoseName = clipPoseName;
            ClipFileName = clipFileName;
            ClipTakeName = clipTakeName;
            ClipSeconds = clipSeconds;
            ClipFrameRate = clipFrameRate;
            ClipLastFrame = clipLastFrame;
            Avatar = avatar;
            BoneCount = boneCount;
            BoneNames = boneNames;
            BoneRoot = boneRoot;
            TextureFolder = textureFolder;
            BaseColorFile = baseColorFile;
            NormalFile = normalFile;
            MetallicRoughnessFile = metallicRoughnessFile;
            ExtraClips = extraClips ?? Array.Empty<BlenderRigClip>();
        }

        public string Name { get; }
        public string ThemePackDir { get; }
        public string RestFileName { get; }
        public string RejectedMixamoFileName { get; }
        public string ClipPoseName { get; }
        public string ClipFileName { get; }
        public string ClipTakeName { get; }
        public float ClipSeconds { get; }
        public float ClipFrameRate { get; }
        public int ClipLastFrame { get; }
        public BlenderRigAvatar Avatar { get; }
        public int BoneCount { get; }
        public string[] BoneNames { get; }
        public string BoneRoot { get; }
        public string TextureFolder { get; }
        public string BaseColorFile { get; }
        public string NormalFile { get; }
        public string MetallicRoughnessFile { get; }
        public BlenderRigClip[] ExtraClips { get; }

        public const string RestPoseName = "rest";

        public bool PreferHumanoid => Avatar == BlenderRigAvatar.MixamoHumanoid;

        public string Title => Name.ToUpperInvariant();

        public string DropdownObjectName => Name + "ClipDropdown";

        public string SceneFileName => Name + ".unity";

        public string RestThemePackRel => ThemePackDir + "/" + RestFileName;

        public string ClipThemePackRel => ThemePackDir + "/" + ClipFileName;

        public string BaseColorThemePackRel =>
            ThemePackDir + "/" + TextureFolder + "/" + BaseColorFile;

        public string NormalThemePackRel =>
            ThemePackDir + "/" + TextureFolder + "/" + NormalFile;

        public string[] PoseNames
        {
            get
            {
                var names = new string[2 + ExtraClips.Length];
                names[0] = RestPoseName;
                names[1] = ClipPoseName;
                for (var i = 0; i < ExtraClips.Length; i++)
                {
                    names[i + 2] = ExtraClips[i].PoseName;
                }

                return names;
            }
        }

        /// <summary>Mixamo export named *_rig.fbx. Design replacements are *_blenderig*.fbx.</summary>
        public static bool IsRejectedMixamoRigFile(string fileName)
        {
            var leaf = fileName;
            var slash = leaf.LastIndexOf('/');
            if (slash >= 0 && slash + 1 < leaf.Length)
            {
                leaf = leaf.Substring(slash + 1);
            }

            var back = leaf.LastIndexOf('\\');
            if (back >= 0 && back + 1 < leaf.Length)
            {
                leaf = leaf.Substring(back + 1);
            }

            if (leaf.IndexOf("blenderig", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            return leaf.EndsWith("_rig.fbx", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>The 22 Mixamo humanoid bones on the Design Blender mixamorig exports.</summary>
    public static class MixamoHumanoidBones
    {
        public const int Count = 22;
        public const string Root = "mixamorig:Hips";

        public static readonly string[] Names =
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
            "mixamorig:LeftToeBase",
            "mixamorig:RightUpLeg",
            "mixamorig:RightLeg",
            "mixamorig:RightFoot",
            "mixamorig:RightToeBase"
        };
    }

    /// <summary>
    /// Lyra Design Blender rig. This branch registers only Lyra.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class BlenderRigRoster
    {
        public static IReadOnlyList<BlenderRigSpec> All { get; } = new[]
        {
            LyraMotion.Spec
        };
    }
}
