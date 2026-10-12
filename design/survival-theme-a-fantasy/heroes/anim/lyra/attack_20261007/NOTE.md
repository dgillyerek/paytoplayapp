# LYRA — attack add-on (Design, 2026-10-07)

**Attack:** Staff Cast: Arcane Bolt
**Weapon / VFX:** Gold crystal staff (NEW separate prop; the staff in the SoT is missing from the Meshy body mesh) + Arcane Bolt projectile (royal-blue crystal-core orb, gold rings/sparks, blue flame trail)

## Built on (rig untouched — no rebind / reskin / weight edits)
- Rig blend: `heroes/anim/lyra/blender_rig/LYRA_blenderig.blend` MD5 `e8405b0c6c8852ed046facccc09f0906` (opened read-only in headless Blender 4.3.2; never saved)
- Rest FBX: `heroes/anim/lyra/blender_rig/LYRA_blenderig.fbx` MD5 `6acba12a6b8bd710d2ef9910896f9c4a`
- Walk/loco FBX: `heroes/anim/lyra/blender_rig/LYRA_blenderig_walk.fbx` MD5 `063a211075ab280af8359003f9a5d49a`
- Before/after MD5 identical for rig blend + rest + walk: **YES**; whole rig dir unchanged: **YES**

## Attack clip
- `LYRA_blenderig_attack.fbx` MD5 `efd19534168581a65e3e7b4b21c10813` — action `LYRA_attack`, frames 0–30 @30fps (31 keys, starts/ends at rest so it blends from idle/walk).
- Keys: anticipation f8 · strike f12 · release/impact **f12** · follow-through f19 · recover f30.
- Export: same settings as the existing clip FBX for this rig (`rebake` profile: binary FBX, axis_forward -Z / up Y, add_leaf_bones off, embed textures, bake_anim all bones step 1, simplify 0, force start/end keys). Blender 4.3.2 (matches the walk FBX's Blender version).
- Reimport QC: same bone names+order as rest FBX: **True** ([22, 22]), rest-head delta 0.0 m, faces [207330, 207330], same vertex groups True, frames [1.0, 31.0], end-pose deviation from rest (deg) {'1': 0, '31': 0}, max bone travel 0.3 m.
- Direction: strike/projectile goes character-forward (Blender −Y, same facing as rest/walk) = **toward the TOP of the screen** in the rear gameplay camera; never toward camera.

## Props / VFX (separate assets — never baked into the body mesh)
- `LYRA_staff_goldcrystal.fbx` MD5 `cc8470fb650d01f27fda54913b4038a1` (1188 faces) — role `staff`
- `LYRA_arcane_bolt_vfx.fbx` MD5 `3900a0d079dddff05e33300b20b84e52` (2040 faces) — role `bolt`
- All props in one blend: `LYRA_attack_props.blend` MD5 `17e2f8382531f5737d6e8e0b875cb55b`
- Attach / spawn (bone-local offsets + spawn transforms in `work/attack_meta.json`):
  - `LYRA_staff_goldcrystal` — held, bone `mixamorig:RightHand` (offset captured at f0) — staff grip in RightHand
  - `LYRA_arcane_bolt_vfx` — proj, release f12, speed 9.0 m/s — arcane bolt spawns at staff crystal, travels character-forward (-Y Blender)
- Prop axes: held weapons have origin at the grip (staff/bow long axis +Z; arrows/spear tip −Y, origin at nock/centre); projectiles/VFX travel along local −Y with the trail on +Y.

## QC
- Look sheet: `LYRA_attack_looksheet.png` (SoT fullbody beside the prop/VFX renders; flat background, no blur fill / mirror pad / edge smear)
- Contact sheet: `LYRA_attack_contact.jpg` (rear gameplay cam + side, every 3rd frame)
- Preview: `LYRA_attack_preview_rear.mp4` (rear gameplay cam, 2 loops)
- Stills: `stills/qc_*` (anticipation / strike / release / inflight / follow; rear + side, plus q34front at strike + inflight), `stills/look_*` (prop renders, transparent).
- Work copy with action + props: `LYRA_attack_work.blend` (compressed copy for review; **not** the rig SoT).

## Caveats
- The SoT staff is missing from the Meshy body mesh (only a stub in the right fist), so the staff is a NEW separate prop gripped by RightHand.
- Gown/cape A-pose heat weights soften on the big arm raise (known PASS caveat); torso twist kept moderate.

HOLD: staging add-on only — not committed / pushed; Derek Game-view before any merge.
