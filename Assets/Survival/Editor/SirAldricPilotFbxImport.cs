#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Survival.Editor
{
    /// <summary>
    /// Fresh-open Humanoid clip materialization for PILOT walk/attack.
    /// Hand-written clipAnimations with a guessed takeName (or CopyFromOther AccuRIG)
    /// produce a mesh and zero AnimationClips — Derek Play T-pose on 3a306ea.
    /// takeName must be the FBX AnimationStack (PR #25). Avatar is CreateFromThisModel.
    /// </summary>
    public sealed class SirAldricPilotFbxImport : AssetPostprocessor
    {
        private const string PilotWalk = "SirAldric_body_holefixed_walk.fbx";
        private const string PilotAttack = "SirAldric_body_holefixed_slash.fbx";

        private void OnPreprocessModel()
        {
            if (assetImporter is not ModelImporter importer)
            {
                return;
            }

            var path = assetPath.Replace('\\', '/');
            if (path.IndexOf("/heroes/3d/pilot/", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            var walk = path.EndsWith(PilotWalk, StringComparison.OrdinalIgnoreCase);
            var attack = path.EndsWith(PilotAttack, StringComparison.OrdinalIgnoreCase);
            if (!walk && !attack)
            {
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            var defaults = importer.defaultClipAnimations;
            if (defaults == null || defaults.Length == 0)
            {
                Debug.LogWarning("PILOT import " + path + " has no defaultClipAnimations yet.");
                return;
            }

            var best = PickTake(defaults, walk);
            if (best == null)
            {
                Debug.LogError("PILOT import " + path + " could not pick a take. defaults=" + DumpTakes(defaults));
                return;
            }

            best.name = walk ? "Walking" : "Attack";
            best.loopTime = walk;
            best.loop = walk;
            best.keepOriginalOrientation = true;
            best.keepOriginalPositionY = true;
            best.keepOriginalPositionXZ = true;
            importer.clipAnimations = new[] { best };
            Debug.Log(
                "PILOT import " + path + " takeName=" + best.takeName +
                " frames=" + best.firstFrame + "-" + best.lastFrame +
                " defaults=" + DumpTakes(defaults));
        }

        internal static ModelImporterClipAnimation? PickTake(ModelImporterClipAnimation[] defaults, bool walk)
        {
            ModelImporterClipAnimation? best = null;
            var bestSpan = -1f;
            foreach (var candidate in defaults)
            {
                var label = (candidate.takeName ?? "") + "\n" + (candidate.name ?? "");
                var span = candidate.lastFrame - candidate.firstFrame;
                if (walk)
                {
                    if (label.IndexOf("Walk", StringComparison.OrdinalIgnoreCase) < 0
                        && label.IndexOf("mixamo", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                }
                else
                {
                    var named = label.IndexOf("rigify", StringComparison.OrdinalIgnoreCase) >= 0
                                || label.IndexOf("BaseLayer", StringComparison.OrdinalIgnoreCase) >= 0
                                || label.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0
                                || label.IndexOf("Slash", StringComparison.OrdinalIgnoreCase) >= 0
                                || label.IndexOf("Sword", StringComparison.OrdinalIgnoreCase) >= 0
                                || label.IndexOf("mixamo", StringComparison.OrdinalIgnoreCase) >= 0
                                || label.IndexOf("clip0", StringComparison.OrdinalIgnoreCase) >= 0
                                || label.IndexOf("Scene", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!named && span < 8f)
                    {
                        continue;
                    }
                }

                if (best == null || span > bestSpan)
                {
                    best = candidate;
                    bestSpan = span;
                }
            }

            if (best != null)
            {
                return best;
            }

            foreach (var candidate in defaults)
            {
                var span = candidate.lastFrame - candidate.firstFrame;
                if (best == null || span > bestSpan)
                {
                    best = candidate;
                    bestSpan = span;
                }
            }

            return best;
        }

        internal static string DumpTakes(ModelImporterClipAnimation[] defaults)
        {
            var parts = new string[defaults.Length];
            for (var i = 0; i < defaults.Length; i++)
            {
                var c = defaults[i];
                parts[i] = (c.takeName ?? "?") + "[" + c.firstFrame + "-" + c.lastFrame + "]";
            }

            return string.Join(" | ", parts);
        }
    }
}
#endif
