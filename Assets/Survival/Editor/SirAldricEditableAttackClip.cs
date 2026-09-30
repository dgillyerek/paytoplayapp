#if UNITY_EDITOR
using Survival.Unity;
using UnityEditor;
using UnityEngine;

namespace Survival.Editor
{
    /// <summary>
    /// Duplicate the DIAG Mixamo take into SirAldric_DIAG_InwardSlash.anim and
    /// spawn a knight for Animation-window keys. Does not overwrite a keyed clip.
    /// </summary>
    [InitializeOnLoad]
    public static class SirAldricEditableAttackClip
    {
        public const string EditKnightName = "SirAldricAttackEditKnight";

        static SirAldricEditableAttackClip()
        {
            EditorApplication.delayCall += () => SirAldricMeshyAnimateActor.EnsureEditableAttackClip();
        }

        [MenuItem("Survival/Sir Aldric/Extract editable DIAG attack clip")]
        public static void ExtractMenu()
        {
            var animRel = "Assets/" + SirAldricMeshyAnimateActor.ThemePackAttackAnim;
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(animRel) != null)
            {
                AssetDatabase.DeleteAsset(animRel);
            }

            SirAldricMeshyAnimateActor.EnsureEditableAttackClip();
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animRel);
            if (clip != null)
            {
                Selection.activeObject = clip;
                EditorGUIUtility.PingObject(clip);
            }
        }

        [MenuItem("Survival/Sir Aldric/Spawn knight for Animation-window edit")]
        public static void SpawnEditKnight()
        {
            SirAldricMeshyAnimateActor.EnsureEditableAttackClip();
            var existing = GameObject.Find(EditKnightName);
            if (existing != null)
            {
                Selection.activeGameObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            var walkRel = "Assets/" + SirAldricMeshyAnimateActor.ThemePackWalkFbx;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(walkRel);
            if (prefab == null)
            {
                Debug.LogError("PILOT walk FBX not imported: " + walkRel);
                return;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (go == null)
            {
                go = Object.Instantiate(prefab);
            }

            go.name = EditKnightName;
            var animator = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(walkRel);
            if (avatar != null && !avatar.isHuman)
            {
                animator.avatar = avatar;
            }

            Undo.RegisterCreatedObjectUndo(go, "Spawn SirAldric attack-edit knight");
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }
    }
}
#endif
