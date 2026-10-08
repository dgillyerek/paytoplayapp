# BONEQUILL — attack add-on (Design, 2026-10-07)

**Attack:** Cursed Bone Arrow
**Weapon / VFX:** Bone-wood longbow with purple wraps/runes (NEW separate prop; the SoT bow is missing from the Meshy body mesh) + bone arrow w/ PURPLE fletching + violet trail

## Built on (rig untouched — no rebind / reskin / weight edits)
- Rig blend: `enemies/anim/bonequill/blender_rig/BONEQUILL_blenderig.blend` MD5 `c70327f7081996d05ee0cee25d456849` (opened read-only in headless Blender 4.3.2; never saved)
- Rest FBX: `enemies/anim/bonequill/blender_rig/BONEQUILL_blenderig.fbx` MD5 `301d285e17d06fe17a6bf76184131a4b`
- Walk/loco FBX: `enemies/anim/bonequill/blender_rig/BONEQUILL_blenderig_walk.fbx` MD5 `f9f8bb27a40bcf6faf8ef0615a6cd645`
- Before/after MD5 identical for rig blend + rest + walk: **YES**; whole rig dir unchanged: **YES**

## Attack clip
- `BONEQUILL_blenderig_attack.fbx` MD5 `16cfb8a07ffe13e6ac1f48b31791523c` — action `BONEQUILL_attack`, frames 0–30 @30fps (31 keys, starts/ends at rest so it blends from idle/walk).
- Keys: anticipation f9 · strike f14 · release/impact **f14** · follow-through f20 · recover f30.
- Export: same settings as the existing clip FBX for this rig (`rebake` profile: binary FBX, axis_forward -Z / up Y, add_leaf_bones off, embed textures, bake_anim all bones step 1, simplify 0, force start/end keys). Blender 4.3.2 (matches the walk FBX's Blender version).
- Reimport QC: same bone names+order as rest FBX: **True** ([22, 22]), rest-head delta 0.0 m, faces [199998, 199998], same vertex groups True, frames [1.0, 31.0], end-pose deviation from rest (deg) {'1': 0, '31': 0}, max bone travel 0.368 m.
- Direction: strike/projectile goes character-forward (Blender −Y, same facing as rest/walk) = **toward the TOP of the screen** in the rear gameplay camera; never toward camera.

## Props / VFX (separate assets — never baked into the body mesh)
- `BONEQUILL_bow.fbx` MD5 `3b25e1c4d1f4f418fb00540e4e29345f` (350 faces) — role `bow`
- `BONEQUILL_bonearrow_purple_fletch.fbx` MD5 `9d8abdd529785dae22fcc01149e50dd3` (67 faces) — role `arrow`
- `BONEQUILL_arrow_trail_vfx.fbx` MD5 `90e4a6a1fa73616b72b300faf4066879` (100 faces) — role `trail`
- All props in one blend: `BONEQUILL_attack_props.blend` MD5 `1109e21e16a7a424a1887822fca5133c`
- Attach / spawn (bone-local offsets + spawn transforms in `work/attack_meta.json`):
  - `BONEQUILL_bow` — held, bone `mixamorig:RightHand` (offset captured at f14) — bow grip in RightHand; vertical at strike frame
  - `BONEQUILL_bonearrow_purple_fletch` — held, bone `mixamorig:RightHand` (offset captured at f14) — cursed bone arrow materialises nocked on the bow f5-9 (no draw hand: left hand is skinned to LeftUpLeg on this rig)
  - `BONEQUILL_bonearrow_purple_fletch_inflight` — proj, release f14, speed 14.0 m/s — bone arrow flies character-forward from nock pose
  - `BONEQUILL_arrow_trail_vfx` — proj, release f14, speed 14.0 m/s — purple trail follows arrow nock
- Prop axes: held weapons have origin at the grip (staff/bow long axis +Z; arrows/spear tip −Y, origin at nock/centre); projectiles/VFX travel along local −Y with the trail on +Y.

## QC
- Look sheet: `BONEQUILL_attack_looksheet.png` (SoT fullbody beside the prop/VFX renders; flat background, no blur fill / mirror pad / edge smear)
- Contact sheet: `BONEQUILL_attack_contact.jpg` (rear gameplay cam + side, every 3rd frame)
- Preview: `BONEQUILL_attack_preview_rear.mp4` (rear gameplay cam, 2 loops)
- Stills: `stills/qc_*` (anticipation / strike / release / inflight / follow; rear + side, plus q34front at strike + inflight), `stills/look_*` (prop renders, transparent).
- Work copy with action + props: `BONEQUILL_attack_work.blend` (compressed copy for review; **not** the rig SoT).

## Caveats
- The SoT bow is missing from the Meshy body mesh, so the bow is a NEW separate prop (BONEQUILL_bow.fbx) gripped by RightHand.
- Real left hand is weighted to LeftUpLeg (LeftForeArm/LeftHand bones run up to the quiver), so there is no draw-hand pull; the cursed bone arrow materialises nocked on the bow (f5–9) and looses at f14. Proper two-hand draw needs a left-arm reskin (out of scope).

HOLD: staging add-on only — not committed / pushed; Derek Game-view before any merge.
