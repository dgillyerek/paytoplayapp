#if UNITY_EDITOR
using System;
using System.IO;
using Survival.Domain.Heroes;
using UnityEditor;
using UnityEngine;

namespace Survival.Editor
{
    /// <summary>
    /// Fresh-open Generic clip materialization for PILOT Mixamo walk/attack.
    /// Humanoid Playable retarget collapses the Mixamo holefixed skin (Derek FAIL bf5a5fa:
    /// sword-only). takeName must be the FBX AnimationStack mixamo.com. Avatar is
    /// CreateFromThisModel Generic — never Humanoid, never AccuRIG.
    /// </summary>
    public sealed class SirAldricPilotFbxImport : AssetPostprocessor
    {
        private const string PilotWalk = "SirAldric_body_holefixed_walk.fbx";
        private const string PilotAttack = "SirAldric_body_holefixed_slash.fbx";
        private const string PilotAttackDiag = "SirAldric_body_holefixed_slash_Stable_Sword_Inward_Slash_DIAG.fbx";
        private const string PilotAttackDiagRevRetired = "SirAldric_body_holefixed_slash_Stable_Sword_Inward_Slash_DIAG_REV.fbx";
        private const string PilotAttackDiagMirrRetired = "SirAldric_body_holefixed_slash_Stable_Sword_Inward_Slash_DIAG_MIRR.fbx";
        private const string PilotAttackMildRetired = "SirAldric_body_holefixed_slash_Stable_Sword_Inward_Slash_MILD.fbx";
        private const string PilotAttackSwordShield = "SirAldric_body_holefixed_slash_SWORD_SHIELD_ATTACK.fbx";

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

            var lite = path.IndexOf("/lite_sword_shield/", StringComparison.OrdinalIgnoreCase) >= 0;
            if (lite)
            {
                ApplyLiteSwordShield(importer, path);
                return;
            }

            var walk = path.EndsWith(PilotWalk, StringComparison.OrdinalIgnoreCase);
            var swordShield = path.EndsWith(PilotAttackSwordShield, StringComparison.OrdinalIgnoreCase);
            var attack = path.EndsWith(PilotAttack, StringComparison.OrdinalIgnoreCase)
                         || path.EndsWith(PilotAttackDiag, StringComparison.OrdinalIgnoreCase)
                         || path.EndsWith(PilotAttackDiagRevRetired, StringComparison.OrdinalIgnoreCase)
                         || path.EndsWith(PilotAttackDiagMirrRetired, StringComparison.OrdinalIgnoreCase)
                         || path.EndsWith(PilotAttackMildRetired, StringComparison.OrdinalIgnoreCase);
            if (!walk && !attack && !swordShield)
            {
                return;
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.useFileScale = false;
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
            if (swordShield)
            {
                best.firstFrame = SirAldric3DMotion.MixamoSwordShieldSlashFirstFrame;
                best.lastFrame = SirAldric3DMotion.MixamoSwordShieldSlashLastFrame;
                best.mirror = false;
            }
            else if (!walk)
            {
                best.firstFrame = SirAldric3DMotion.MixamoSlashFirstFrame;
                best.lastFrame = SirAldric3DMotion.MixamoSlashLastFrame;
            }

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

        /// <summary>
        /// Lite Sword And Shield Pack: keep the exact Mixamo catalog name.
        /// Do not rename to Attack. Do not force 0–73 or 0–85.
        /// </summary>
        private static void ApplyLiteSwordShield(ModelImporter importer, string path)
        {
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.useFileScale = false;
            var exact = SirAldric3DMotion.LiteSwordShieldExactNameForFile(Path.GetFileName(path));
            if (string.IsNullOrEmpty(exact))
            {
                Debug.LogError("PILOT lite import " + path + " is not in the exact Mixamo name table.");
                return;
            }

            var defaults = importer.defaultClipAnimations;
            if (defaults == null || defaults.Length == 0)
            {
                var existing = importer.clipAnimations;
                if (existing != null && existing.Length > 0)
                {
                    existing[0].name = exact;
                    existing[0].takeName = SirAldric3DMotion.LiteSwordShieldTakeName;
                    existing[0].mirror = false;
                    importer.clipAnimations = existing;
                }

                Debug.LogWarning("PILOT lite import " + path + " has no defaultClipAnimations yet. takeName=Scene.");
                return;
            }

            var best = PickTake(defaults, walk: false);
            if (best == null)
            {
                Debug.LogError("PILOT lite import " + path + " could not pick a take. defaults=" + DumpTakes(defaults));
                return;
            }

            best.name = exact;
            if (string.IsNullOrEmpty(best.takeName)
                || string.Equals(best.takeName, "mixamo.com", StringComparison.OrdinalIgnoreCase))
            {
                best.takeName = SirAldric3DMotion.LiteSwordShieldTakeName;
            }

            best.mirror = false;
            var loops = SirAldric3DMotion.TryLiteSwordShield(exact, out var entry) && entry.Loop;
            best.loopTime = loops;
            best.loop = loops;
            best.keepOriginalOrientation = true;
            best.keepOriginalPositionY = true;
            best.keepOriginalPositionXZ = true;
            importer.clipAnimations = new[] { best };
            Debug.Log(
                "PILOT lite import " + path + " exactName=" + exact +
                " takeName=" + best.takeName +
                " frames=" + best.firstFrame + "-" + best.lastFrame);
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
