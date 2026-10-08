# ROWAN_male — attack add-on (Design, 2026-10-07)

**Attack:** Aimed Shot
**Weapon / VFX:** Arrow with BLUE fletching + gold nock bands (NEW separate prop) fired from the bow already in the body mesh; pale-blue arrow trail VFX

## Built on (rig untouched — no rebind / reskin / weight edits)
- Rig blend: `heroes/anim/rowan/blender_rig_male_20261007/ROWAN_male_blenderig.blend` MD5 `9255d17cd7be53fcb5d6b911500a9d64` (opened read-only in headless Blender 4.3.2; never saved)
- Rest FBX: `heroes/anim/rowan/blender_rig_male_20261007/ROWAN_male_blenderig.fbx` MD5 `486d9cf097f6d0fc0dd8ae6713b5f726`
- Walk/loco FBX: `heroes/anim/rowan/blender_rig_male_20261007/ROWAN_male_blenderig_walk.fbx` MD5 `ab148b67d09b4f39f2923f4db8549d66`
- Before/after MD5 identical for rig blend + rest + walk: **YES**; whole rig dir unchanged: **YES**

## Attack clip
- `ROWAN_male_blenderig_attack.fbx` MD5 `3ad624f3e53db018e3cb18a035e13121` — action `ROWAN_male_attack`, frames 0–30 @30fps (31 keys, starts/ends at rest so it blends from idle/walk).
- Keys: anticipation f9 · strike f14 · release/impact **f14** · follow-through f20 · recover f30.
- Export: same settings as the existing clip FBX for this rig (`hum` profile: binary FBX, axis_forward -Z / up Y, add_leaf_bones off, embed textures, bake_anim all bones step 1, simplify 0, force start/end keys). Blender 4.3.2 (matches the walk FBX's Blender version).
- Reimport QC: same bone names+order as rest FBX: **True** ([22, 22]), rest-head delta 0.0 m, faces [200000, 200000], same vertex groups True, frames [1.0, 31.0], end-pose deviation from rest (deg) {'1': 0, '31': 0}, max bone travel 0.427 m.
- Direction: strike/projectile goes character-forward (Blender −Y, same facing as rest/walk) = **toward the TOP of the screen** in the rear gameplay camera; never toward camera.

## Props / VFX (separate assets — never baked into the body mesh)
- `ROWAN_arrow_blue_fletch.fbx` MD5 `7940bf35a79de4fcdca798df34ca6143` (67 faces) — role `arrow`
- `ROWAN_arrow_trail_vfx.fbx` MD5 `b7f52e4f0f6c4a6023678067886b815d` (100 faces) — role `trail`
- All props in one blend: `ROWAN_male_attack_props.blend` MD5 `583362d396d2e227e57c28eca58b3872`
- Attach / spawn (bone-local offsets + spawn transforms in `work/attack_meta.json`):
  - `ROWAN_arrow_blue_fletch` — held, bone `mixamorig:RightForeArm` (offset captured at f14) — blue-fletched arrow nocked on the mesh bow (grip vertex) f5-13; no draw hand: male rig left hand is skinned to LeftUpLeg
  - `ROWAN_arrow_blue_fletch_inflight` — proj, release f14, speed 14.0 m/s — arrow flies character-forward from nock pose
  - `ROWAN_arrow_trail_vfx` — proj, release f14, speed 14.0 m/s — pale-blue trail behind arrow
- Prop axes: held weapons have origin at the grip (staff/bow long axis +Z; arrows/spear tip −Y, origin at nock/centre); projectiles/VFX travel along local −Y with the trail on +Y.

## QC
- Look sheet: `ROWAN_male_attack_looksheet.png` (SoT fullbody beside the prop/VFX renders; flat background, no blur fill / mirror pad / edge smear)
- Contact sheet: `ROWAN_male_attack_contact.jpg` (rear gameplay cam + side, every 3rd frame)
- Preview: `ROWAN_male_attack_preview_rear.mp4` (rear gameplay cam, 2 loops)
- Stills: `stills/qc_*` (anticipation / strike / release / inflight / follow; rear + side, plus q34front at strike + inflight), `stills/look_*` (prop renders, transparent).
- Work copy with action + props: `ROWAN_male_attack_work.blend` (compressed copy for review; **not** the rig SoT).

## Caveats
- Male rig skinning: the real left hand is weighted to LeftUpLeg (LeftForeArm/LeftHand bones run up to the quiver), so a two-hand draw is impossible without a reskin (out of scope: no weight changes). The shot is staged as a bow-arm aim with the arrow nocked on the mesh bow; no draw-hand pull.
- Bow is part of the body mesh (RightArm/RightForeArm/RightHand + some leg weights); bow-arm raise is kept moderate (~34°) to avoid stretching the lower bow limb / cape.
- GAP FOUND (not touched — #35 files are off-limits): ROWAN_male_blenderig_walk.fbx (MD5 ab148b67…) plays folded in Blender reimport — frame-0 mesh bbox z 0.57–1.55 m / y −0.14–1.15 m vs rest z 0–1.90 m. Root cause: action ROWAN_male_walk was keyed by hum_pipeline make_walk_clip (parent-relative matrices written as pose quats; Hips ≈180° off identity at f0) — same class of bug the 2026-10-05 rebake fixed for the other humanoids. This attack clip is authored from identity rest and does NOT inherit it.

HOLD: staging add-on only — not committed / pushed; Derek Game-view before any merge.
