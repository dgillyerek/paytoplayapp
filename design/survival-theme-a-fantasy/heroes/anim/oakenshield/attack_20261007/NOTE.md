# OAKENSHIELD — attack add-on (Design, 2026-10-07)

**Attack:** Thorn Spear Throw
**Weapon / VFX:** Conjured thorn spear (pale twisted wood, side thorns, royal-blue leaf fletching, glowing gold spiral) — held prop, then thrown projectile

## Built on (rig untouched — no rebind / reskin / weight edits)
- Rig blend: `heroes/anim/oakenshield/blender_rig/OAKENSHIELD_blenderig.blend` MD5 `7dafbbf622442a4fe0af345d04a38577` (opened read-only in headless Blender 4.3.2; never saved)
- Rest FBX: `heroes/anim/oakenshield/blender_rig/OAKENSHIELD_blenderig.fbx` MD5 `9f0e892f6b56eba78c2de4078931bd63`
- Walk/loco FBX: `heroes/anim/oakenshield/blender_rig/OAKENSHIELD_blenderig_walk.fbx` MD5 `03b747fc974e86dbb9afb52b209036ba`
- Before/after MD5 identical for rig blend + rest + walk: **YES**; whole rig dir unchanged: **YES**

## Attack clip
- `OAKENSHIELD_blenderig_attack.fbx` MD5 `3fe681bc07658c519dbc1e903614c766` — action `OAKENSHIELD_attack`, frames 0–30 @30fps (31 keys, starts/ends at rest so it blends from idle/walk).
- Keys: anticipation f10 · strike f14 · release/impact **f14** · follow-through f20 · recover f30.
- Export: same settings as the existing clip FBX for this rig (`rebake` profile: binary FBX, axis_forward -Z / up Y, add_leaf_bones off, embed textures, bake_anim all bones step 1, simplify 0, force start/end keys). Blender 4.3.2 (matches the walk FBX's Blender version).
- Reimport QC: same bone names+order as rest FBX: **True** ([22, 22]), rest-head delta 0.0 m, faces [199893, 199893], same vertex groups True, frames [1.0, 31.0], end-pose deviation from rest (deg) {'1': 0, '31': 0}, max bone travel 0.663 m.
- Direction: strike/projectile goes character-forward (Blender −Y, same facing as rest/walk) = **toward the TOP of the screen** in the rear gameplay camera; never toward camera.

## Props / VFX (separate assets — never baked into the body mesh)
- `OAKENSHIELD_thornspear.fbx` MD5 `0b7ebca1eb1946a850320fc44e65c1f5` (553 faces) — role `spear`
- All props in one blend: `OAKENSHIELD_attack_props.blend` MD5 `f7b447c342d89a290560a4dba2750895`
- Attach / spawn (bone-local offsets + spawn transforms in `work/attack_meta.json`):
  - `OAKENSHIELD_thornspear` — held, bone `mixamorig:RightHand` (offset captured at f10) — thorn spear conjured f3-8 in RightHand, thrown at release
  - `OAKENSHIELD_thornspear_inflight` — proj, release f14, speed 11.0 m/s — thrown thorn spear, travels character-forward
- Prop axes: held weapons have origin at the grip (staff/bow long axis +Z; arrows/spear tip −Y, origin at nock/centre); projectiles/VFX travel along local −Y with the trail on +Y.

## QC
- Look sheet: `OAKENSHIELD_attack_looksheet.png` (SoT fullbody beside the prop/VFX renders; flat background, no blur fill / mirror pad / edge smear)
- Contact sheet: `OAKENSHIELD_attack_contact.jpg` (rear gameplay cam + side, every 3rd frame)
- Preview: `OAKENSHIELD_attack_preview_rear.mp4` (rear gameplay cam, 2 loops)
- Stills: `stills/qc_*` (anticipation / strike / release / inflight / follow; rear + side, plus q34front at strike + inflight), `stills/look_*` (prop renders, transparent).
- Work copy with action + props: `OAKENSHIELD_attack_work.blend` (compressed copy for review; **not** the rig SoT).

## Caveats
- No held weapon in the SoT; the thorn spear is conjured (scales in f3–8) in RightHand and thrown at f14. Rig arm bones sit inside the torso (auto joints), so the overhead wind-up reads softer than a true javelin pose.

HOLD: staging add-on only — not committed / pushed; Derek Game-view before any merge.
