#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Survival.Editor
{
    /// <summary>
    /// Preview-only import for the four Design characters of 2026-11-03 (art/preview_new_chars/&lt;name&gt;/): Generic rig
    /// (Humanoid drops the turns, as with Lyra), one clip per FBX, idle and walk looping. Not in the roster.
    /// Also the menu Survival → New Characters Preview, which opens a fresh scene with the preview battle and plays it.
    /// </summary>
    public sealed class NewCharsPreviewImport : AssetPostprocessor
    {
        private static bool Ours(string path) =>
            path.Replace('\\', '/').IndexOf("/art/" + Survival.Unity.NewCharsPreview.FolderName + "/", StringComparison.OrdinalIgnoreCase) >= 0;

        private void OnPreprocessModel()
        {
            if (assetImporter is not ModelImporter importer || !Ours(assetPath))
            {
                return;
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.useFileUnits = true;
            importer.globalScale = 1f;
            importer.bakeAxisConversion = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.resampleCurves = true;
            importer.importCameras = false;
            importer.importLights = false;
        }

        private void OnPreprocessAnimation()
        {
            if (assetImporter is not ModelImporter importer || !Ours(assetPath))
            {
                return;
            }

            var file = System.IO.Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            var loop = file.EndsWith("_idle", StringComparison.Ordinal) || file.EndsWith("_walk", StringComparison.Ordinal);
            var clips = importer.defaultClipAnimations;
            if (clips.Length == 0)
            {
                return;
            }

            var keep = clips[0];
            var under = file.IndexOf('_');
            keep.name = under >= 0 ? file.Substring(under + 1) : file;
            keep.loopTime = loop;
            keep.loop = loop;
            keep.keepOriginalOrientation = true;
            keep.keepOriginalPositionY = true;
            keep.keepOriginalPositionXZ = true;
            importer.clipAnimations = new[] { keep };
        }

        [MenuItem("Survival/New Characters Preview")]
        private static void OpenPreview()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var path = "Assets/Survival/Scenes/NewCharsPreview.unity";
            if (System.IO.File.Exists(path))
            {
                EditorSceneManager.OpenScene(path);
            }
            else
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("NewCharsPreview").AddComponent<Survival.Unity.NewCharsPreview>();
                EditorSceneManager.SaveScene(scene, path);
            }

            EditorApplication.isPlaying = true;
        }
    }
}
#endif
