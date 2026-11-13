# Seraphine's staff stays in her hand, 2026-11-12

Slot 1 in New Characters Preview is `attack_armada`. Slot 2 is `attack_thrust_slash`. The spear is a separate prop, `SERAPHINE_staff.fbx`, parented to `mixamorig:RightHand` for idle, walk, and both attacks. It is not skinned into the body, and it is not stowed on the back.

The right arm holds one pose relative to the chest: the hand sits about 26 cm out to her right, 18 cm forward, and 20 cm above the shoulder, and the staff rises at 74° with a 30° turn toward her right. The ornament is the far end. The shaft lies against the palm. The rest of the skeleton is the previous clip. Hips, head, left hand, and left leg match the previous files with 0.0 cm of drift on every measured frame.

Unity was not run for the plates. They are Blender, labeled "BLENDER — not Unity".

## Why she vanished in Game view

The rest file and these clips now share the same model roots: `SERAPHINE_rig`, `SERAPHINE_body`, `SERAPHINE_wing_A`, `SERAPHINE_wing_B`. No spear. Kaelen's clips match his rest the same way, so Unity stores the curves as `KAELEN_rig/mixamorig:Hips` and the buttons move him. Seraphine's clips had been armature-only, one root, so the curves were `mixamorig:Hips` and `SampleAnimation` never reached the bones under `SERAPHINE_rig`. She stayed in the standing rest. The tracks were already there: walk's left leg swings 51°, Armada's hips spin 378°. Re-exporting with the body and wings kept those spans, the 100 scale track, and the −90° root rotation. `RestoreScales` still puts the imported rig scale and rotation back after each sample. Bone rotations are not pinned. The preview still starts frozen until a clip button.

The earlier armature-only clips also made Unity compensate the root track (scale 100 to 1, rotation X −90° to 0), which shrank her and then laid her on her back. The contact shadow is parented to the host, not the mesh. Each one has a constant root track on `SERAPHINE_rig`: rotation X −90° and scale 100. That −90° is the Blender Z-up conversion; the rest file's rig node is the same rotation and has no root animation. Unity compensates an armature-only root track, so `SampleAnimation` was writing scale 1 and rotation 0 over the imported scale 100 and rotation −90°. Full size, she lay on her back in the idle pose. `RestoreScales` puts both the imported scale and the imported rotation back after every sample. Bone rotations still come from the clip, so idle, walk, Armada, and thrust slash still play. The preview starts frozen on idle until a clip button, same as the other three. `SERAPHINE_rig_rest.fbx` is the previous file with the spear nodes removed. The skinned body is 1.79 m tall.

## Unity change

File: `Assets/Survival/Unity/NewCharsPreview.cs`

`AttachSeraphineStaff`, called from `Spawn` after the cape:

- Loads `SERAPHINE_staff.fbx`.
- Instantiates it as a child of `mixamorig:RightHand`.
- Destroys any Animator on the prop and applies the body atlas.
- `ApplyStaffGrip` sets the local pose after every sample. Local scale stays 1.

Idle, walk, and Armada are unchanged: the staff stays on the right hand at mid-shaft. Thrust slash does not sample a staff clip. After `HoldHips` and the pose blend, `PlaceThrustStaff` lays the same mesh through the two wrists with the middle of the shaft on the midpoint of the hands, tip toward the enemy. She stays side-on with her feet planted. The arms push the staff forward 0.38 m on one cosine and draw it back. Films: `clips/SERAPHINE_lunge_front.mp4`, `SERAPHINE_lunge_side.mp4`, `SERAPHINE_lunge_top.mp4`, labeled BLENDER — not Unity.

`SERAPHINE_rig_rest.fbx` no longer contains the skinned spear. The 24-bone skeleton, the cape, the clip names, and the existing `.meta` GUIDs are unchanged. `SERAPHINE_staff.fbx.meta` is new. `bakeAxisConversion` is 0, the same as the rest rig, so the prop's hand-space vertices are not given a second axis bake.

## Clearance

Signed distance between convex hulls, full vertex support, 480 directions. Positive is a gap. The right hand is reported on its own row because the shaft is meant to sit against the palm. The right-arm row is the shoulder, upper arm, and forearm. The cape is the bound cape parented to Spine2.

| Clip | Body | Wings | Cape | Left arm | Right arm | Right hand |
|---|---:|---:|---:|---:|---:|---:|
| idle | 34.8 cm | 35.7 cm | 57.4 cm | 58.4 cm | 4.9 cm | 0.7 cm |
| walk | 36.0 cm | 34.1 cm | 53.6 cm | 56.9 cm | 4.9 cm | 0.8 cm |
| attack_armada | 26.1 cm | 29.6 cm | 49.9 cm | 38.0 cm | 4.9 cm | 0.7 cm |
| attack_thrust_slash | 25.6 cm | 31.0 cm | 53.9 cm | 45.1 cm | 4.9 cm | 0.7 cm |

Nothing goes negative. The tightest body gap is thrust slash, 25.6 cm. The tightest wing gap is Armada, 29.6 cm.

## Films

Front, side, and the preview camera (yaw 135°, pitch 30°, 54° vertical FOV). Idle is the first 45 frames. Walk, Armada, and thrust slash are the whole clip, in that order, all at mid-shaft. The camera follows the hips so the spin stays in frame. Plates are Blender, labeled "BLENDER — not Unity".

`design/survival-theme-a-fantasy/heroes/anim/seraphine/blender/armada_2026-11-10/clips/SERAPHINE_mid_preview.mp4`

Thrust slash, side-on, both hands at mid-shaft. The feet stay planted and the arms push the staff forward and back. Labeled BLENDER — not Unity.

`design/survival-theme-a-fantasy/heroes/anim/seraphine/blender/armada_2026-11-10/clips/SERAPHINE_lunge_top.mp4`

The clearance table above is the end-grip measurement from before the mid-shaft slide. It is not the lunge.

## Residual

- The hand has no finger bones, so the fingers stay open. The shaft clears the hand outline by 0.7 cm. It reads as resting against the palm, not as a closed fist.
- Armada keeps the raised right-arm hold. Thrust slash keeps a planted side-on stance. Both wrists sit 21 cm either side of the middle of the shaft, and the arms carry it forward 0.38 m and back once. The staff mesh stays 3.5 cm clear of the torso, 25 cm clear of the wings, and 31 cm clear of the head. The hand has no finger bones, so the grip is the wrist on the shaft.
- Desktop Unity was not run. This is not a Game-view pass.
