# LYRA fitted: attack add-on (Design, 2026-10-08): staff spell cast

HOLD: staging only. Nothing committed, pushed or PR'd. Derek must check it in Game-view before any merge.

Built on `heroes/anim/lyra/blender_rig_tighten_20261008/LYRA_tighten_blenderig.blend`. The rig blend was opened and never saved.

## Clip
- `LYRA_tighten_blenderig_attack.fbx`: f0–f30 @30fps. Authored from identity rest; f0 = f30 = rest. Exported with the `hum` profile.
- Timeline: same timing family as lyra attack_20261007 (efd19534).
  - f8 anticipation: staff pulled back and up, torso turned right, left hand forward.
  - **f12 strike / release:** staff thrust forward, tilted 27°.
  - f19 follow-through.
  - f30 rest.
- **Staff stays parented to `mixamorig:RightHand` for the whole clip**, with a constant offset (meta).
- `LYRA_arcane_bolt_vfx` spawns at the staff crystal at f12. It grows from 40% to 100% over 3 frames and travels along exactly −Y at 9 m/s, **toward the top of the screen** in the rear battle cam.

## Props / VFX
- `LYRA_staff.fbx` (14,048 faces): the Meshy staff split from the body. Origin is at the grip, +Z runs toward the crystal.
- `LYRA_arcane_bolt_vfx.fbx`: emissive blue bolt with a gold ring.
- `LYRA_tighten_attack_props.blend`: both props.

## Reimport QC (`work/verify_attack.json`)
- 22 bones, same names and order as rest. Faces 200,000. Frames 1–31.
- **Hips at the first frame vs rest: 0.0°. Max over all frames: 0°.** End-pose deviation 0.0°.

## QC media
- `LYRA_tighten_attack_sheet.jpg`
- `LYRA_tighten_walk_attack_preview.mp4`

## Caveats
- The hand keeps the Meshy fist. The strike reads as a staff thrust and bolt, not an open-palm cast.
- Fist caps are the same as noted in the rig pack.

## Re-export after Derek's rig edit (2026-10-08)
`LYRA_tighten_blenderig_attack.fbx` was re-exported from Derek's edited combined blend: 90385eda → 64d8b8d4. The attack keys are unchanged. Because the right elbow and wrist joints moved, the same keys now put the staff crystal at f12 17 cm closer to the body midline and 5 cm higher. The bolt spawn in `work/attack_meta.json` was updated. See `../blender_rig_tighten_20261008/NOTE.md`.

## Attack staff in Unity (2026-10-08)
Unity no longer loads `LYRA_staff.fbx` for the attack. The rest/walk staff (`LYRA_staff_rest.fbx`) stands in for the attack's `LYRA_staff` track: it has the same mesh and the same grip on RightHand. One textured staff now serves all three clips. The old attack copy rendered white because its imported material never got the rig's texture atlas. The file is kept, unchanged.

## Rebuild on Derek's second rig edit (2026-10-08 22:45, e8b050d)
`LYRA_tighten_blenderig_attack.fbx` 64d8b8d4 → 53fd21bb. Derek moved the hips, knees, ankles, left elbow/wrist and right hand, and removed both toe bones. The attack was retargeted from a995489 by world-space rotation from rest, so the cast reads the same. The staff wrist path is within 1.6 cm and the staff crystal path within 1.5 cm. The bolt spawn was re-measured at f12: (-0.154, -0.660, 1.912). See `../blender_rig_tighten_20261008/NOTE.md`.
