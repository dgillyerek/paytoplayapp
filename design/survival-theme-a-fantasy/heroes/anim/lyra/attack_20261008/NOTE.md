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
