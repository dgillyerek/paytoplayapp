# Seraphine Armada on the preview, 2026-11-10

Slot 1 in New Characters Preview is `attack_armada`. The clip file that was already on disk is reused. Slot 2 is still `attack_thrust_slash`, byte-identical to main. `SERAPHINE_rig_rest.fbx`, idle, walk, and the cape are byte-identical to main. The spear mesh was not moved.

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

## Why the spear was not moved

`NewCharsPreview` does not special-case the spear. On `SERAPHINE_rig_rest` it is a separate mesh, parented to the armature at identity, and all 15,900 vertices are weight 1 on `mixamorig:RightHand`. The open hand has no finger bones and lives in that same rest file, which stays unchanged.

The shaft is 2.10 m long, about 2.0 cm in radius, with an ornament about 15 cm across near one end. The nearest hand vertex is 2.15 cm from the shaft axis, so the shaft surface touches the open hand. The hand-vertex centroid is 7.5 cm off the axis, because the open hand extends away from the shaft. That distance is constant in the hand, so it does not drift during the clip.

A different constant aim does not clear the body outline. On frame 1 the grip point is 12.9 cm inside the convex hull of torso, head, and legs, and every one of the 509 right-hand vertices is inside that hull (the outermost is still 10.0 cm inside). The best spear direction through that grip is still −18.3 cm on frame 1. Frames 1–22 are the same kind of miss. Moving the spear in the rest file would also move it in idle, walk, and thrust slash, and would not put frame 1 outside the hull.

Pushing the hand out of the hull and re-aiming the spear would be a new arm animation. That was not done. The kick stays the Mixamo retarget.

## Outline clearance

Signed distance between convex hulls. Positive is a gap. The right hand is not part of the body or the right arm. Unit cubes at 2.00 m, touching, overlapping by 0.20 m, and separated on a diagonal matched to 0.001 mm. Full table is `measurements.json`.

| Clip | Pair | Minimum | Frame | Frames under −0.5 cm |
|---|---|---:|---:|---:|
| armada | staff–body | −42.0 cm | 49 | 47 / 50 |
| armada | staff–wings | −43.1 cm | 33 | 6 / 50 |
| armada | staff–right arm | −8.2 cm | 30 | 30 / 50 |
| armada | staff–left arm | −14.5 cm | 42 | 9 / 50 |
| thrust slash (unchanged) | staff–body | −41.3 cm | 78 | 61 / 91 |
| thrust slash | staff–wings | −44.9 cm | 13 | 63 / 91 |
| thrust slash | staff–right arm | +0.44 cm | 40 | 0 / 91 |
| thrust slash | staff–left arm | −10.0 cm | 43 | 7 / 91 |

The right hand overlaps the spear by about 4 cm. That is the open hand against the shaft, and it is not in the table above.

Triangle pairs, Armada only: the spear intersects body polygons on 36 of 50 frames (578 pairs at frame 1). Wing polygon intersections: 0. Right-arm polygons: frames 30–32. Left-arm polygons: frames 41–44.

## Visual verdicts

Playback is in `clips/`. Grip stills are in `grips/`.

- **attack_armada — FAIL.** The body motion is a spinning kick: she turns and throws one leg. Front, side, and the preview camera all show the shaft through the hips and thighs on the windup and again as the leg comes down, and through the raised leg at the kick. The right hand stays open. The shaft touches that hand and continues through the pelvis. A small foot slide shows as the kick settles. Wing polygon intersections were 0. The wing outline still overlaps, and the blade reads as crossing the wing in the playback.
- **attack_thrust_slash — FAIL.** Unchanged from main. At frame 1 the spear is horizontal through the hip, with the head sticking out of the right hip on the side view and the shaft crossing the wing on the front view. The hand stays open. The sweep then carries the spear through the torso. This clip was not edited.

PR #213's reauthored attacks were not used. The outline pairs (whole spear, whole body, both wings, each arm, gripping hand left out) are the same check that PR asked for. The measurement was rebuilt for this clip.
