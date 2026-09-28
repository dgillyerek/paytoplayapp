# HOLD — Sir Aldric PILOT AccuRIG + Walk + Attack (2026-09-27)

**HOLD merge on PR #27 until Derek Play.** Path A / ClipSword / AimChain are **not SoT** and stay HOLD forever for this ship. No Design / Derek PASS claimed.

## Root cause (Derek Play FAIL `3a306ea`)

`Editor.log`: `PILOT walk FBX has no Walking clip after Humanoid import.` → `Build()` returned before `_graphReady` → T-pose.

1. **takeName mismatch (PR #25 class).** Walk FBX AnimationStack is `Armature|Armature|Armature|Walking`. The hand-written `.meta` used a 4× Armature guess (`Armature|Armature|Armature|Armature|Walking`). Attack used Blender's concatenated `target_character|rigify_clip|BaseLayer`; FBX stacks are `rigify_clip` and `Armature|clip0|baselayer`. Unmatched `clipAnimations.takeName` → Unity imports the mesh and **zero** `AnimationClip`s. `internalID: 0` is fine (working Meshy walk uses it).
2. **CopyFromOther AccuRIG avatar** (`avatarSetup: 2`) on walk/attack. Walk has 34 bones vs AccuRIG 28 extra-End bones. Remap can drop clips even after takeName is fixed. SoT importer is **CreateFromThisModel** (`avatarSetup: 1`), same as PR #25 Meshy walk.
3. Play-time `RepairWalkingTake` is not enough: Fresh open must materialize clips. `SirAldricPilotFbxImport` `OnPreprocessModel` copies `defaultClipAnimations` takeNames onto `clipAnimations`.

Albedo: AccuRIG embeds no PNG. Sibling `pilot_albedo.png` is `texture_0` from the walk FBX.

## SoT

| Item | Status |
| --- | --- |
| Look | AccuRIG Humanoid mid280k + `pilot_albedo.png` + metallic/roughness PNGs. |
| Walk | `Armature\|Armature\|Armature\|Walking` 1–25. Humanoid CreateFromThisModel. |
| Attack | `rigify_clip` 3–92. Humanoid CreateFromThisModel. |
| Sword | **Fused.** No Path A / ClipSword / empty-scabbard. |
| Facing | Rear yaw 180 so +Z = TOP. |

## Proofs in this folder

Play-cam rematch (Unity Y-up remapped). **Not Meshy website stills. Not Unity Game-view.** `SirAldricGameViewCapture` is the Play-machine `Camera.Render` path.

## Unity Play

1. Fresh open. Play **SirAldric**. 1080×1920 Scale 1×.
2. Walk clip plays on the **walk FBX instance** (not retargeted onto AccuRIG). Attack on its own instance. Cam rear `(0, 1.75, −2.90)` LookAt `(0, 0.85, 0.15)` FOV 34, reapplied every LateUpdate (scene default was ortho 5.5 at z=−10).
3. HOLD until Derek PASS.
