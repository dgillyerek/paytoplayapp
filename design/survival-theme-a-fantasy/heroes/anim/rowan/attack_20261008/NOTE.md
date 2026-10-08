# ROWAN no-cape: attack add-on (Design, 2026-10-08): bow draw + release

HOLD: staging only. Nothing committed, pushed or PR'd. Derek must check it in Game-view before any merge.

Built on `heroes/anim/rowan/blender_rig_nocape_20261008/ROWAN_nocape_blenderig.blend`. The rig blend was opened and never saved; the work copy here was saved with copy=True.

## Clip
- `ROWAN_nocape_blenderig_attack.fbx`: f0–f30 @30fps. Keys authored from identity rest (pose matrices → quaternion keys, BEZIER auto-clamped). f0 = f30 = rest, so it blends from idle/walk.
- Export uses the `hum` profile (same as the rest/walk FBX: axis_forward −Z / up Y, no leaf bones, embedded textures, all bones baked, step 1, simplify 0).
- Timeline:
  - f3–f4: bow handed from the right hand to the left hand in front of the belly.
  - f8: arrow nocked, bow raised.
  - f13: full draw (string pulled 0.47 m, right hand at the jaw).
  - **f14: release.** The arrow leaves at 14 m/s along exactly −Y.
  - f20: follow-through.
  - f22–f23: bow handed back to the right hand.
  - f30: rest.
- Torso turns up to 45° so the left (bow) shoulder leads; the head counter-rotates to stay on the target.
- Direction: the arrow flies along Blender −Y, which is **toward the top of the screen** in the rear battle cam.

## Props / VFX (separate assets, never baked into the body)
- `ROWAN_bow_attack.fbx`: bow mesh + `ROWAN_bow_rig`. `bow_grip` carries the **baked per-frame character-space transform**, so Dev can drop it under the character root and play it in sync with no parent switching. `bow_nock` local +Y is the string draw in metres.
- Parent windows, if Dev prefers hand-parenting (offsets in `work/attack_meta.json`):
  - RightHand: f0–f3 and f23–f30.
  - **LeftHand: f4–f22.** The bow is LH-parented for the whole shot.
  - During f4–f7 and f23–f26 the receiving hand slides 10 cm along the limb onto the grip.
- `ROWAN_arrow_blue_fletch.fbx` (67 faces, **BLUE fletching**, gold nock bands; tip −Y, origin at nock):
  - f5–f7: held in RightHand (scale-in).
  - f8–f14: nocked on the string.
  - From f14: in flight (14 m/s, spawn matrix in meta).
- `ROWAN_arrow_trail_vfx.fbx`: pale-blue trail. Same spawn and speed as the arrow; it trails on +Y.
- `ROWAN_nocape_attack_props.blend`: all props in one file.

## Reimport QC (`work/verify_attack.json`)
- 22 bones, same names and order as the rest FBX. Faces 199,999 (same as rest). Frames 1–31.
- **Hips at the first frame vs rest: 0.0°. Hips max deviation over all frames: 0°.** Not flipped.
- End-pose deviation from rest at f1 and f31: 0.0°.
- Bow FBX: armature `ROWAN_bow_rig` (bow_grip, bow_nock), 16,284 faces, frames 1–31.

## QC media
- `ROWAN_nocape_attack_sheet.jpg`: rear battle cam / side / 3-4 front rows at the key frames.
- `ROWAN_nocape_walk_attack_preview.mp4`: walk ×2 then attack ×2.

## Caveats
- The arrow is not drawn from the quiver. It scales in inside the right hand during f5–f7.
- The right hand keeps the Meshy fist shape. The string is held at the fist centre; there is no finger hook or open-hand release (no finger bones).
- The hand-over (f3–f4 and f22–f23) is quick (about 0.1 s). The receiving hand overlaps the bow limb for 2–3 frames.
- Fist caps and the grip bridge are the same as noted in the rig pack.
