# STORMCREST — attack add-on (Design, 2026-10-07)

**Attack:** Wing Buffet: Storm Bolt
**Weapon / VFX:** Storm bolt VFX (blue-white forked lightning, royal-blue glow, gold sparks + charge orb/ring) cast from the beak

## Built on (rig untouched — no rebind / reskin / weight edits)
- Rig blend: `heroes/anim/stormcrest/blender_rig/STORMCREST_blenderig.blend` MD5 `54e082c1da274aadbcfb1691607450ce` (opened read-only in headless Blender 4.3.2; never saved)
- Rest FBX: `heroes/anim/stormcrest/blender_rig/STORMCREST_blenderig.fbx` MD5 `f883e552eb1fdfd69dae953bb506e94b`
- Walk/loco FBX: `heroes/anim/stormcrest/blender_rig/STORMCREST_blenderig_walk.fbx` MD5 `bab9c62ff5d60edb9e9b109c0edffa1f`
- Before/after MD5 identical for rig blend + rest + walk: **YES**; whole rig dir unchanged: **YES**

## Attack clip
- `STORMCREST_blenderig_attack.fbx` MD5 `48acf6540852f78175b2cda1a89b4ceb` — action `STORMCREST_attack`, frames 0–30 @30fps (31 keys, starts/ends at rest so it blends from idle/walk).
- Keys: anticipation f9 · strike f14 · release/impact **f14** · follow-through f21 · recover f30.
- Export: same settings as the existing clip FBX for this rig (`rebake` profile: binary FBX, axis_forward -Z / up Y, add_leaf_bones off, embed textures, bake_anim all bones step 1, simplify 0, force start/end keys). Blender 4.3.2 (matches the walk FBX's Blender version).
- Reimport QC: same bone names+order as rest FBX: **True** ([22, 22]), rest-head delta 0.0 m, faces [200000, 200000], same vertex groups True, frames [1.0, 31.0], end-pose deviation from rest (deg) {'1': 0, '31': 0}, max bone travel 0.342 m.
- Direction: strike/projectile goes character-forward (Blender −Y, same facing as rest/walk) = **toward the TOP of the screen** in the rear gameplay camera; never toward camera.

## Props / VFX (separate assets — never baked into the body mesh)
- `STORMCREST_stormbolt_vfx.fbx` MD5 `2ab650e8aaf130d1e86e76a5dc0fc5b4` (1561 faces) — role `bolt`
- All props in one blend: `STORMCREST_attack_props.blend` MD5 `8abe14989e039fa3e48615e4cdf3bcbc`
- Attach / spawn (bone-local offsets + spawn transforms in `work/attack_meta.json`):
  - `STORMCREST_stormbolt_vfx` — proj, release f14, speed 4.0 m/s — storm bolt anchored at beak vertex #35320; lightning strikes forward (2.6m reach), visible ~6 frames
- Prop axes: held weapons have origin at the grip (staff/bow long axis +Z; arrows/spear tip −Y, origin at nock/centre); projectiles/VFX travel along local −Y with the trail on +Y.

## QC
- Look sheet: `STORMCREST_attack_looksheet.png` (SoT fullbody beside the prop/VFX renders; flat background, no blur fill / mirror pad / edge smear)
- Contact sheet: `STORMCREST_attack_contact.jpg` (rear gameplay cam + side, every 3rd frame)
- Preview: `STORMCREST_attack_preview_rear.mp4` (rear gameplay cam, 2 loops)
- Stills: `stills/qc_*` (anticipation / strike / release / inflight / follow; rear + side, plus q34front at strike + inflight), `stills/look_*` (prop renders, transparent).
- Work copy with action + props: `STORMCREST_attack_work.blend` (compressed copy for review; **not** the rig SoT).

## Caveats
- Griffin uses the 22-bone Mixamo humanoid skeleton (LeftArm/RightArm drive the wings, legs/feet bones run along the body). Attack keeps legs still: wing raise + forward buffet, neck/head beak thrust, storm bolt from the beak vertex. Wing feathers may soften at the forward sweep.

HOLD: staging add-on only — not committed / pushed; Derek Game-view before any merge.
