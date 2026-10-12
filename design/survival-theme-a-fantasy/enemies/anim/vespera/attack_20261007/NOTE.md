# VESPERA — attack add-on (Design, 2026-10-07)

**Attack:** Shadow Bolt
**Weapon / VFX:** Shadow bolt projectile (violet void orb, black smoke puffs, purple wisps, rune rings) — one-hand cast

## Built on (rig untouched — no rebind / reskin / weight edits)
- Rig blend: `enemies/anim/vespera/blender_rig/VESPERA_blenderig.blend` MD5 `b51721af26efbb02985a0a13536e15a8` (opened read-only in headless Blender 4.3.2; never saved)
- Rest FBX: `enemies/anim/vespera/blender_rig/VESPERA_blenderig.fbx` MD5 `969964e9fc0d5818b8877548d83bdecc`
- Walk/loco FBX: `enemies/anim/vespera/blender_rig/VESPERA_blenderig_walk.fbx` MD5 `25fff5842afb46fe50d0703236754cfb`
- Before/after MD5 identical for rig blend + rest + walk: **YES**; whole rig dir unchanged: **YES**

## Attack clip
- `VESPERA_blenderig_attack.fbx` MD5 `082b741eae52d9144a612566380766bc` — action `VESPERA_attack`, frames 0–30 @30fps (31 keys, starts/ends at rest so it blends from idle/walk).
- Keys: anticipation f9 · strike f13 · release/impact **f13** · follow-through f20 · recover f30.
- Export: same settings as the existing clip FBX for this rig (`rebake` profile: binary FBX, axis_forward -Z / up Y, add_leaf_bones off, embed textures, bake_anim all bones step 1, simplify 0, force start/end keys). Blender 4.3.2 (matches the walk FBX's Blender version).
- Reimport QC: same bone names+order as rest FBX: **True** ([22, 22]), rest-head delta 0.0 m, faces [199997, 199997], same vertex groups True, frames [1.0, 31.0], end-pose deviation from rest (deg) {'1': 0, '31': 0}, max bone travel 0.675 m.
- Direction: strike/projectile goes character-forward (Blender −Y, same facing as rest/walk) = **toward the TOP of the screen** in the rear gameplay camera; never toward camera.

## Props / VFX (separate assets — never baked into the body mesh)
- `VESPERA_shadowbolt_vfx.fbx` MD5 `b3563693c102ba55a65e051131aacf2a` (3076 faces) — role `bolt`
- All props in one blend: `VESPERA_attack_props.blend` MD5 `23ae831c5c5cffb869b299970749c78a`
- Attach / spawn (bone-local offsets + spawn transforms in `work/attack_meta.json`):
  - `VESPERA_shadowbolt_vfx` — proj, release f13, speed 9.0 m/s — shadow bolt spawns 12cm ahead of RightHand tail, travels character-forward
- Prop axes: held weapons have origin at the grip (staff/bow long axis +Z; arrows/spear tip −Y, origin at nock/centre); projectiles/VFX travel along local −Y with the trail on +Y.

## QC
- Look sheet: `VESPERA_attack_looksheet.png` (SoT fullbody beside the prop/VFX renders; flat background, no blur fill / mirror pad / edge smear)
- Contact sheet: `VESPERA_attack_contact.jpg` (rear gameplay cam + side, every 3rd frame)
- Preview: `VESPERA_attack_preview_rear.mp4` (rear gameplay cam, 2 loops)
- Stills: `stills/qc_*` (anticipation / strike / release / inflight / follow; rear + side, plus q34front at strike + inflight), `stills/look_*` (prop renders, transparent).
- Work copy with action + props: `VESPERA_attack_work.blend` (compressed copy for review; **not** the rig SoT).

## Caveats
- No weapon in the SoT, so the attack is an empty-hand cast; the shadow bolt is VFX only. Rig upper-arm bone is very short (auto joints) so the whole arm swings from RightArm — soft sleeve deformation on the raise.

HOLD: staging add-on only — not committed / pushed; Derek Game-view before any merge.
