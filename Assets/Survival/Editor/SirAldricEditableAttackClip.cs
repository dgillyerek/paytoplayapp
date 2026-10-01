#if UNITY_EDITOR
using System.Reflection;
using Survival.Unity;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Survival.Editor
{
    /// <summary>
    /// Duplicate the DIAG Mixamo take into SirAldric_DIAG_InwardSlash.anim,
    /// wire an AnimatorController so Animation-window Preview can sample it
    /// onto SirAldricAttackEditKnight, and spawn that knight. Does not
    /// overwrite a keyed clip unless Extract is used.
    /// </summary>
    [InitializeOnLoad]
    public static class SirAldricEditableAttackClip
    {
        public const string EditKnightName = "SirAldricAttackEditKnight";
        public const string AttackControllerAsset =
            "Assets/" + SirAldricMeshyAnimateActor.ThemePackAttackController;

        static SirAldricEditableAttackClip()
        {
            EditorApplication.delayCall += () =>
            {
                SirAldricMeshyAnimateActor.EnsureEditableAttackClip();
                EnsureAttackController();
            };
        }

        [MenuItem("Survival/Sir Aldric/Extract editable DIAG attack clip")]
        public static void ExtractMenu()
        {
            SirAldricMeshyAnimateActor.EnsureEditableAttackClip(forceReextract: true);
            EnsureAttackController();
            var knight = GameObject.Find(EditKnightName);
            if (knight != null)
            {
                WireEditKnight(knight);
                FocusEditKnight(knight);
                return;
            }

            var animRel = "Assets/" + SirAldricMeshyAnimateActor.ThemePackAttackAnim;
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
            EnsureAttackController();
            var existing = GameObject.Find(EditKnightName);
            if (existing != null)
            {
                WireEditKnight(existing);
                FocusEditKnight(existing);
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
            if (PrefabUtility.IsPartOfPrefabInstance(go))
            {
                PrefabUtility.UnpackPrefabInstance(
                    go,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
            }

            WireEditKnight(go);
            Undo.RegisterCreatedObjectUndo(go, "Spawn SirAldric attack-edit knight");
            FocusEditKnight(go);
        }

        /// <summary>
        /// AnimatorController with default/Attack = editable DIAG .anim.
        /// Created next to the .anim if missing.
        /// </summary>
        public static AnimatorController? EnsureAttackController()
        {
            var animRel = "Assets/" + SirAldricMeshyAnimateActor.ThemePackAttackAnim;
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animRel);
            if (clip == null)
            {
                Debug.LogWarning("PILOT attack .anim missing; cannot wire AnimatorController. " + animRel);
                return null;
            }

            var folder = "Assets/Survival/Unity/Anims";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Survival/Unity", "Anims");
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AttackControllerAsset);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(
                    AttackControllerAsset,
                    clip);
                controller.name = "SirAldric_DIAG_InwardSlash";
            }

            if (controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            var sm = controller.layers[0].stateMachine;
            AnimatorState? attackState = null;
            foreach (var child in sm.states)
            {
                if (child.state != null
                    && (child.state.name == "Attack"
                        || child.state.motion == clip
                        || child.state.name == clip.name))
                {
                    attackState = child.state;
                    break;
                }
            }

            if (attackState == null)
            {
                attackState = sm.AddState("Attack");
            }

            attackState.name = "Attack";
            attackState.motion = clip;
            sm.defaultState = attackState;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        public static void WireEditKnight(GameObject go)
        {
            foreach (var extra in go.GetComponentsInChildren<Animator>(true))
            {
                if (extra.gameObject != go)
                {
                    Object.DestroyImmediate(extra);
                }
            }

            var animator = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;

            var avatar = LoadWalkFbxAvatar();
            if (avatar != null)
            {
                animator.avatar = avatar;
            }
            else
            {
                Debug.LogWarning(
                    "PILOT walk FBX has no Avatar sub-asset. Animation-window Preview may not sample.");
            }

            var controller = EnsureAttackController();
            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
            }

            EditorUtility.SetDirty(animator);
            Debug.Log(
                "PILOT Animation-window knight wired controller=" + AttackControllerAsset +
                " avatar=" + (avatar != null ? avatar.name + (avatar.isHuman ? " humanoid" : " generic") : "null") +
                " applyRootMotion=false");
        }

        /// <summary>
        /// Walk FBX model avatar (Humanoid if Unity mapped one, else Generic
        /// CreateFromThisModel). LoadAllAssetsAtPath — LoadAssetAtPath misses
        /// the FBX sub-asset and left Scene scrub frozen.
        /// </summary>
        public static Avatar? LoadWalkFbxAvatar()
        {
            var walkRel = "Assets/" + SirAldricMeshyAnimateActor.ThemePackWalkFbx;
            Avatar? human = null;
            Avatar? generic = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(walkRel))
            {
                if (obj is not Avatar avatar)
                {
                    continue;
                }

                if (avatar.isHuman)
                {
                    human = avatar;
                }
                else
                {
                    generic = avatar;
                }
            }

            return human ?? generic;
        }

        public static void FocusEditKnight(GameObject go)
        {
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            EditorApplication.ExecuteMenuItem("Window/Animation/Animation");
            EditorApplication.delayCall += () =>
            {
                if (go == null)
                {
                    return;
                }

                Selection.activeGameObject = go;
                TryLockAndPreviewAnimationWindow();
            };
        }

        /// <summary>
        /// Lock Animation + start Preview so scrubbing samples the knight.
        /// Selecting Hierarchy without lock greys the window (Unity default).
        /// </summary>
        public static void TryLockAndPreviewAnimationWindow()
        {
            var winType = typeof(EditorWindow).Assembly.GetType("UnityEditor.AnimationWindow");
            if (winType == null)
            {
                return;
            }

            Object[] windows = Resources.FindObjectsOfTypeAll(winType);
            if (windows == null || windows.Length == 0)
            {
                var opened = EditorWindow.GetWindow(winType, false, "Animation", false);
                windows = opened != null ? new Object[] { opened } : System.Array.Empty<Object>();
            }

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var window in windows)
            {
                if (window == null)
                {
                    continue;
                }

                var locked = winType.GetProperty("locked", flags);
                if (locked != null && locked.CanWrite)
                {
                    locked.SetValue(window, true);
                }

                var stateProp = winType.GetProperty("state", flags);
                var state = stateProp?.GetValue(window);
                if (state == null)
                {
                    continue;
                }

                var stateType = state.GetType();
                var startPreview = stateType.GetMethod("StartPreview", flags, null, System.Type.EmptyTypes, null);
                startPreview?.Invoke(state, null);
                var previewing = stateType.GetProperty("previewing", flags);
                if (previewing != null && previewing.CanWrite)
                {
                    previewing.SetValue(state, true);
                }
            }
        }
    }
}
#endif
