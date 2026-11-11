# Seraphine Armada on the preview, 2026-11-10

Slot 1 in New Characters Preview is `attack_armada`. The clip's bone curves are unchanged. During Armada the preview lifts the spear off the right hand and seats it on a mount under `mixamorig:Spine2`. Idle, walk, and thrust slash keep the original hand grip. `SERAPHINE_rig_rest.fbx`, the cape, and every clip file are byte-identical to the previous commit on this branch.

Unity is not available headless here. Plates are Blender. `NewCharsPreview` instances `SERAPHINE_rig_rest.fbx` and samples the clip. These renders do the same thing, then pin horizontal hips the way `HoldHips` does. The words "BLENDER — not Unity" are composited on the frames. Workbench did not draw a text object.

## How close the clip is to the Mixamo file

Source: `design/survival-theme-a-fantasy/heroes/anim/seraphine/attacks_20261109/mixamo_armada.fbx`.

Motion-only FBX, no skin, FBX SDK 2020.2, written 2026-11-10 02:22:37. Action name `Armature|mixamo.com|Layer0`. The file text does not contain "Armada". 50 frames at 30 fps (source frames 0–49). Standard mixamorig, including index fingers and toes. The note next to the download says: without skin, FBX for Unity, 30 fps, keyframe reduction none, and In Place was not in the dialog. No character name and no Arm-Space setting are stored in the file. A second download is not required. This file is the source the clip was built from.

Compared after one FBX import, source frame 0 against clip frame 1, for the 20 bones the retarget maps (shin `UpLeg.001`, foot `UpLeg.002`). Bind matrices of those bones match the rest rig (max absolute difference 0). World rotation relative to each rig's own rest:

- Worst bone: `RightUpLeg`, 0.158° at frame 26.
- Mean of the per-bone means: 0.010°.
- `RightArm` stays under 0.001°.
- `RightHand` max 0.040°.

Vertical hip motion matches within 7 mm. The source hips travel about 1.35 m in X and −1.35 m in Y and do not return. The clip removes that straight drift and keeps the sway (about 0.21 m in X and 0.48 m in Y). The preview then pins horizontal hips anyway.

The preview FBX and the design-folder copy differ by the FBX timestamp only (3 bytes).

## Staff stow (Unity)

`NewCharsPreview.AttachSeraphineStaff` bakes the spear with `SkinnedMeshRenderer.BakeMesh` (the FBX mesh is not readable) into RightHand space, hides the skinned renderer, and shows a rigid copy. `SERAPHINE_staff_hand` is a child of `mixamorig:RightHand` with an identity local pose, so the copy matches the skinned spear (measured error 0.000 mm on idle and walk, 0.001 mm on thrust slash frame 1). `SERAPHINE_staff_back` is a child of `mixamorig:Spine2`:

- local position `(-0.185664, -0.488309, -0.501672)` m
- local rotation quaternion xyzw `(-0.014996, 0.008718, 0.500424, 0.865607)`

`PlaceSeraphineStaff`, called after the pose crossfade in `Tick`, moves a linear weight to 1 over 0.20 s while `attack_armada` plays and, after a 0.14 s wait, back to 0 over 0.20 s on any other clip. The shown weight is `w*w*(3-2*w)`. The rigid spear's world pose is the lerp/slerp of the two sockets, plus `sin(pi * w) * 0.60` m along the spine bone's local -Z, so the shaft swings behind her instead of through the wings. At 0 and at 1 that extra offset is zero and the spear is parented to the socket, so on the mount it does not move relative to Spine2.

The diagonal sits behind the cape, between the wing roots, ornament up over her left shoulder and butt down past the right hip. Closer to the cape, the ornament meets the wings.

## Why a hand grip cannot clear Armada

`NewCharsPreview` does not special-case the spear. On `SERAPHINE_rig_rest` it is a separate mesh, parented to the armature at identity, and all 15,900 vertices are weight 1 on `mixamorig:RightHand`. The open hand has no finger bones and lives in that same rest file, which stays unchanged.

The shaft is 2.10 m long, about 2.0 cm in radius, with an ornament about 15 cm across near one end. The nearest hand vertex is 2.15 cm from the shaft axis, so the shaft surface touches the open hand. The hand-vertex centroid is 7.5 cm off the axis, because the open hand extends away from the shaft. That distance is constant in the hand, so it does not drift during the clip.

Holding the spear in the Armada hand cannot clear the body outline. On frame 1 that grip point is 12.9 cm inside the convex hull of torso, head, and legs. The stow takes the spear off the hand instead of editing the kick.

## Outline clearance with the stow

Signed distance between convex hulls. Positive is a gap. The right hand is not part of the body or the right arm. The sequence is the preview timeline: idle, the 0.20 s stow while the pose crossfades into Armada, all 50 Armada frames with the spear on the mount, then the return to idle. Cape is the bound cape, parented to Spine2 the way the preview attaches it (the spring is not re-simulated).

| Pair | Minimum | Where |
|---|---:|---|
| staff–body | 16.5 cm | stow t = 0.77 s (spear on the mount, during the kick) |
| staff–wings | 5.6 cm | return t = 0.30 s |
| staff–right arm | 0.08 cm | return t = 0.33 s (spear back in the idle hand) |
| staff–left arm | 39.7 cm | return t = 0.13 s |
| staff–cape | 19.2 cm | stow t = 0.40 s |

The 0.08 cm right-arm gap is the original open-hand grip against the forearm, after the spear has arrived home. While the spear is on the mount the right-arm gap stays above 40 cm. No pair goes negative.

In-hand check, rigid copy versus the skinned spear: idle 0.000 mm, walk 0.000 mm, thrust slash frame 1 0.001 mm.

## Visual verdict

Clips are `clips/SERAPHINE_stow_front.mp4`, `SERAPHINE_stow_side.mp4`, `SERAPHINE_stow_preview.mp4`, each also at half speed. The combined preview film is `clips/SERAPHINE_armada_stow_preview.mp4` (2.67 s, preview camera, idle, stow, the kick, and the return).

- **attack_armada with the stow — PASS.** The kick is still the spin. Once the blend finishes, the spear stays diagonal on the back and moves with the torso, not with the arm. It sits behind the cape, ornament up past the left shoulder, butt down past the right hip, clear of both wings. The stow and the return are smooth travels. The return swings out behind her and comes within 5.6 cm of the wing outline; the side plate at that moment shows the shaft in front of the body, not inside the wing.
- **idle, walk, thrust slash — unchanged.** The rigid spear matches the skinned one. Thrust slash still carries the spear in the hand, which is the previous clip, not this mount.

## Residual

- The mount is about 19 cm behind the cape. Tighter to the cloth, the ornament meets the wings.
- The 0.60 m back-swing is visible. It is what keeps the shaft out of the wings on the way.
- The cape gap is against the bound cape. The preview spring can still drift; 19 cm is the margin it has.
- Desktop Unity was not run. The Blender playback uses these same numbers.
