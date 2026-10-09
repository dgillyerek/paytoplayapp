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

## Staff clear of the body, natural wrist (2026-10-09)
Derek, Game view on desktop: "her staff pushes through the body on attack and her wrist bends in an unnatural way."

- **Penetration before (e1c7d36):** f9–f23 the lower shaft went through the right thigh (up to 8.5 cm deep at f18) and f20–f23 through the right knee/shin (up to 6.8 cm at f21); f11–f18 it also cut 1–2 cm into the right forearm. The crystal and head never touched the head or hair (closest about 11 cm).
- **Wrist before:** the staff wrist bent up to 119° away from Derek's rest grip (f15). That was about 99° of bend around the shaft, about 40° sideways (84° at f10) and 64° of twist. In the wind-up at f6–f8 it was 52–76°, mostly sideways.
- **Fix:** only RightArm, RightForeArm and RightHand keys changed, solved per frame against the real posed mesh. The staff angle now comes from the shoulder and elbow. During the cast the butt leans out past her right hip, and the crystal sits about 9 cm further to her right. Timing, f12 release, crystal height and forward reach, hips, legs, spine, head and the left arm are unchanged. f0 and f30 are identical to rest.
- **After:** no staff penetration on any frame; minimum clearance 3.7 cm. Staff wrist at most 25° total (at most 15° around the shaft, 16° sideways, 12° twist). The elbow stays a hinge (at most 6° off-plane), forearm twist at most 56°, upper-arm twist at most 50°. Left wrist unchanged (0° from rest).
- **Bolt:** re-keyed at the f12 crystal: (-0.246, -0.659, 1.902), was (-0.154, -0.660, 1.912). Unity `LyraAttack` staff and bolt frames regenerated; calibration unchanged because the bones are unchanged.
- **FBX:** attack 53fd21bb → 835b0502. Rest, walk and both staff FBXs are byte-identical.
- **Scripts:** `work/scripts/staff_clear_*_20261009.py`. QC: `work/staff_clear_before_e1c7d36.json`, `work/staff_clear_after.json`.
