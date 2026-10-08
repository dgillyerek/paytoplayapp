# EMBERFANG — attack add-on (Design, 2026-10-07)

**Attack:** Fire Breath: Fireball
**Weapon / VFX:** Fireball projectile (white-gold core, amber flame shell, royal-blue flame rim + trail) + breath flare at the mouth

## Built on (rig untouched — no rebind / reskin / weight edits)
- Rig blend: `heroes/anim/emberfang/blender_rig/EMBERFANG_dragonrig.blend` MD5 `9a0a8385997963b7af71d11789539e4f` (opened read-only in headless Blender 4.2.3 LTS; never saved)
- Rest FBX: `heroes/anim/emberfang/blender_rig/EMBERFANG_dragonrig.fbx` MD5 `91336af931c3a67dcf8754c8901e5fd3`
- Walk/loco FBX: `heroes/anim/emberfang/blender_rig/EMBERFANG_dragonrig_walk.fbx` MD5 `ac678a652c7d2158769eb35ec8fddace`
- Other clip: `heroes/anim/emberfang/blender_rig/EMBERFANG_dragonrig_wingflap.fbx` MD5 `405acb84ea35fbfd0358419483e05e54`
- Before/after MD5 identical for rig blend + rest + walk + wingflap: **YES**; whole rig dir unchanged: **YES**

## Attack clip
- `EMBERFANG_blenderig_attack.fbx` MD5 `e832baf41cd41e2c54a50de4af1dbb89` — action `EMBERFANG_attack`, frames 0–30 @30fps (31 keys, starts/ends at rest so it blends from idle/walk).
- Keys: anticipation f9 · strike f14 · release/impact **f14** · follow-through f21 · recover f30.
- Export: same settings as the existing clip FBX for this rig (`dragon` profile: binary FBX, axis_forward -Z / up Y, add_leaf_bones off, embed textures, bake_anim all bones step 1, simplify 0, force start/end keys). Blender 4.2.3 LTS (matches the walk FBX's Blender version).
- Reimport QC: same bone names+order as rest FBX: **True** ([55, 55]), rest-head delta 0.0 m, faces [200000, 200000], same vertex groups True, frames [1.0, 31.0], end-pose deviation from rest (deg) {'1': 0, '31': 0}, max bone travel 0.242 m.
- Direction: strike/projectile goes character-forward (Blender −Y, same facing as rest/walk) = **toward the TOP of the screen** in the rear gameplay camera; never toward camera.

## Props / VFX (separate assets — never baked into the body mesh)
- `EMBERFANG_fireball_vfx.fbx` MD5 `45740b4d9a4e12f73f32608eb138d954` (2260 faces) — role `fireball`
- `EMBERFANG_breathflare_vfx.fbx` MD5 `6842fa5c8ca7fc5ca7a1cccc484c3b08` (736 faces) — role `flare`
- All props in one blend: `EMBERFANG_attack_props.blend` MD5 `98ab69cba22f575a1569e24761c57e34`
- Attach / spawn (bone-local offsets + spawn transforms in `work/attack_meta.json`):
  - `EMBERFANG_breathflare_vfx` — fixed — breath flare at mouth (head/jaw tips) around release
  - `EMBERFANG_fireball_vfx` — proj, release f14, speed 9.0 m/s — fireball spawns 8cm ahead of mouth, travels character-forward
- Prop axes: held weapons have origin at the grip (staff/bow long axis +Z; arrows/spear tip −Y, origin at nock/centre); projectiles/VFX travel along local −Y with the trail on +Y.

## QC
- Look sheet: `EMBERFANG_attack_looksheet.png` (SoT fullbody beside the prop/VFX renders; flat background, no blur fill / mirror pad / edge smear)
- Contact sheet: `EMBERFANG_attack_contact.jpg` (rear gameplay cam + side, every 3rd frame)
- Preview: `EMBERFANG_attack_preview_rear.mp4` (rear gameplay cam, 2 loops)
- Stills: `stills/qc_*` (anticipation / strike / release / inflight / follow; rear + side, plus q34front at strike + inflight), `stills/look_*` (prop renders, transparent).
- Work copy with action + props: `EMBERFANG_attack_work.blend` (compressed copy for review; **not** the rig SoT).

## Caveats
- ADD-ON ONLY: PR #41 tip files (EMBERFANG_dragonrig.blend/.fbx/_walk/_wingflap) are untouched; this clip uses the same 55-bone skeleton.
- Fire breath uses neck_01..03/head/jaw + chest; legs stay planted (no root motion). Fireball spawns at the head/jaw tip midpoint.

HOLD: staging add-on only — not committed / pushed; Derek Game-view before any merge.
