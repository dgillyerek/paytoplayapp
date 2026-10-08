# NIGHTFANG — attack add-on (Design, 2026-10-07), rebaked 2026-10-08 on Derek's rig

**Attack:** Lunge Bite + Claw Rake
**Weapon / VFX:** Natural weapons (jaw/claws on the body) — lunge bite with violet triple claw-rake slash VFX

## Built on Derek's hand-edited rig (bone placement kept)
- Rig blend (SoT, not overwritten): `enemies/anim/nightfang/blender_rig/NIGHTFANG_blenderig.blend` MD5 `ebb0c6f9b0d5ef55df95dd785938d11f` (Blender 5.2). His head/tail/roll were not moved.
- Rest FBX: `enemies/anim/nightfang/blender_rig/NIGHTFANG_blenderig.fbx` MD5 `09bd9ceb6fdfa9c39a6e09673b585f4b`
- Trot FBX: `enemies/anim/nightfang/blender_rig/NIGHTFANG_blenderig_trot.fbx` MD5 `438a490c45576fa928f9eaa7a8d65630`
- What he changed vs the old 33-bone auto rig (`dce842f9…`): deleted that skeleton and drew 29 deform bones (`Bone`…`Bone.028`), four unparented chains. Forelegs are 5-bone chains from the withers down to the paws (the old right foreleg was splayed out through the torso). Spine runs withers → hips (2 bones). Hind legs are 4 bones. Tail is 5 bones. Neck/head/jaw/snout is 4 bones. No root in his file.
- Bind fix only: renamed those bones (shoulder/upperarm/forearm/hand/toe, chest/hips, thigh/shin/foot/toe_h, tail_01..05, neck/head/jaw/snout), added a `root` with no vertex group (it parents the rig and does not skin the mesh), and parented the loose foreleg and neck chains to the chest without moving heads, tails, or rolls (reimport: every joint matches his blend, roll delta 0). The mesh still wore the old vertex groups (`hips`, `upperarm.R`, …), and `upperarm.R` had been dominating ~25k verts because of the splay, so the body was not bound to his bones. Cleared those groups and rebound with automatic heat weights, L/R suppress, 4 influences. Unweighted 0. Each bone's weight centroid sits on the bone (worst 8.6 cm, `shoulder.L`).

## Attack clip
- `NIGHTFANG_blenderig_attack.fbx` MD5 `3e1d6cfa12e353f207a4e610804f9a3b` — Scene take, frames 0–30 @30fps (31 keys, starts and ends at rest).
- Keys: anticipation f9 · strike f14 · release/impact **f14** · follow-through f20 · recover f30.
- Same design as the previous pass: in-place lunge (root forward 28 cm along Blender −Y, back to 0 by f30), jaw bite, and now both forelegs reach and rake. Claw-slash VFX stays visible on frames 13–18; its world point moved with the new snout (Unity 0, 0.381, 1.070).
- Export: binary FBX, axis_forward −Z / up Y, `FBX_SCALE_ALL` + apply unit scale so UnitScaleFactor stays 100 and the armature scale curves stay 1 (not the collapsed 0.01 skeleton). add_leaf_bones off, embed textures, bake all bones step 1, simplify 0, force start/end keys. Blender 5.2.2. Take name `Scene`. Generic.
- Reimport QC: 30 bones, armature scale 1, faces 200000, unweighted 0, frames 1–31, attack end pose matches the start (0 m). Foreleg toe travel about 0.83–0.88 m. Trot loop error 0 m.

## Trot clip
- Diagonal pairs (fore L with hind R, then the other pair), one stride per second, in place.
- Each paw steps about 32 cm forward and back. Foreleg toes travel about 0.37 m. Swing paws lift (left fore up to about 8 cm; the more upright right fore lifts less). Spine and head bob, tail follows.
- Reimport: armature scale curves 1.0, root travel 3.6 cm, head travel 7.8 cm, tail tip 76 cm, loop error 0 m.

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
- Natural weapons only (no prop, no cloth). Body bones only. The lunge is a non-deform root translate (28 cm forward, back to 0 by f30). Claw-slash VFX is still a fixed effect on frames 13–18, now in front of the new snout. Unity keeps `applyRootMotion` off.
- Stills, contact sheet, look sheet, preview mp4, and `NIGHTFANG_attack_work.blend` are the 2026-10-07 pass on the old auto rig. The FBXs above are the rebake.

HOLD: do not merge until Derek Game-view PASS.
