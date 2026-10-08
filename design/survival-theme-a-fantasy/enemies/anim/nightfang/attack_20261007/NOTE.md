# NIGHTFANG — attack add-on (Design, 2026-10-07)

**Attack:** Lunge Bite + Claw Rake
**Weapon / VFX:** Natural weapons (jaw/claws on the body) — lunge bite with violet triple claw-rake slash VFX

## Built on (rig untouched — no rebind / reskin / weight edits)
- Rig blend: `enemies/anim/nightfang/blender_rig/NIGHTFANG_blenderig.blend` MD5 `8d0d7a876ec69f65e0db3a58ebba1f9b` (opened read-only in headless Blender 4.3.2; never saved)
- Rest FBX: `enemies/anim/nightfang/blender_rig/NIGHTFANG_blenderig.fbx` MD5 `dce842f9cd7605dae24808075c33db0a`
- Walk/loco FBX: `enemies/anim/nightfang/blender_rig/NIGHTFANG_blenderig_trot.fbx` MD5 `227458a450746f3ed10316dd5d7d4165`
- Before/after MD5 identical for rig blend + rest + walk: **YES**; whole rig dir unchanged: **YES**

## Attack clip
- `NIGHTFANG_blenderig_attack.fbx` MD5 `389229e0dd405ebf18a8c2aa38ec4577` — action `NIGHTFANG_attack`, frames 0–30 @30fps (31 keys, starts/ends at rest so it blends from idle/walk).
- Keys: anticipation f9 · strike f14 · release/impact **f14** · follow-through f20 · recover f30.
- Export: same settings as the existing clip FBX for this rig (`rebake` profile: binary FBX, axis_forward -Z / up Y, add_leaf_bones off, embed textures, bake_anim all bones step 1, simplify 0, force start/end keys). Blender 4.3.2 (matches the walk FBX's Blender version).
- Reimport QC: same bone names+order as rest FBX: **True** ([33, 33]), rest-head delta 0.0 m, faces [200000, 200000], same vertex groups True, frames [1.0, 31.0], end-pose deviation from rest (deg) {'1': 0, '31': 0}, max bone travel 0.468 m.
- Direction: strike/projectile goes character-forward (Blender −Y, same facing as rest/walk) = **toward the TOP of the screen** in the rear gameplay camera; never toward camera.

## Props / VFX (separate assets — never baked into the body mesh)
- `NIGHTFANG_clawslash_vfx.fbx` MD5 `539dda574663d1bda0dba276dbb05e94` (1092 faces) — role `slash`
- All props in one blend: `NIGHTFANG_attack_props.blend` MD5 `b1db416a2e94ae866545a3f8fd4d8142`
- Attach / spawn (bone-local offsets + spawn transforms in `work/attack_meta.json`):
  - `NIGHTFANG_clawslash_vfx` — fixed — claw-rake slash arcs in front of the jaw at bite/strike frame
- Prop axes: held weapons have origin at the grip (staff/bow long axis +Z; arrows/spear tip −Y, origin at nock/centre); projectiles/VFX travel along local −Y with the trail on +Y.

## QC
- Look sheet: `NIGHTFANG_attack_looksheet.png` (SoT fullbody beside the prop/VFX renders; flat background, no blur fill / mirror pad / edge smear)
- Contact sheet: `NIGHTFANG_attack_contact.jpg` (rear gameplay cam + side, every 3rd frame)
- Preview: `NIGHTFANG_attack_preview_rear.mp4` (rear gameplay cam, 2 loops)
- Stills: `stills/qc_*` (anticipation / strike / release / inflight / follow; rear + side, plus q34front at strike + inflight), `stills/look_*` (prop renders, transparent).
- Work copy with action + props: `NIGHTFANG_attack_work.blend` (compressed copy for review; **not** the rig SoT).

## Caveats
- Natural weapons only (no prop). Foreleg chains on this auto-placed quadruped rig are splayed, so legs are not animated; the lunge is a root translate (≤32 cm forward, returns to 0 by f30) + chest/neck/head/jaw. The claw-rake is a VFX mesh placed in front of the jaw at f13–18. Root motion is in-clip (non-deform root bone) — disable root motion in Unity or keep as in-place lunge.

HOLD: staging add-on only — not committed / pushed; Derek Game-view before any merge.
