# NIGHTFANG — attack add-on (Design, 2026-10-07), rebaked 2026-10-08 on Derek's rig

**Attack:** Lunge Bite + Claw Rake
**Weapon / VFX:** Natural weapons (jaw/claws on the body) — lunge bite with violet triple claw-rake slash VFX

## Built on Derek's hand-edited rig (bone placement kept)
- Rig blend (SoT, not overwritten): `enemies/anim/nightfang/blender_rig/NIGHTFANG_blenderig.blend` MD5 `ebb0c6f9b0d5ef55df95dd785938d11f` (Blender 5.2). His head/tail/roll were not moved.
- Rest FBX: `enemies/anim/nightfang/blender_rig/NIGHTFANG_blenderig.fbx` MD5 `9bad5fd2559b9a7efb27cc206767ed54`
- Trot FBX: `enemies/anim/nightfang/blender_rig/NIGHTFANG_blenderig_trot.fbx` MD5 `379bb90b630c33723bf4fbc0032467c0`
- What he changed vs the old 33-bone auto rig (`dce842f9…`): deleted that skeleton and drew 29 deform bones (`Bone`…`Bone.028`), four unparented chains. Forelegs are 5-bone chains from the withers down to the paws (the old right foreleg was splayed out through the torso). Spine runs withers → hips (2 bones). Hind legs are 4 bones. Tail is 5 bones. Neck/head/jaw/snout is 4 bones. No root in his file.
- Bind fix only: renamed those bones (shoulder/upperarm/forearm/hand/toe, chest/hips, thigh/shin/foot/toe_h, tail_01..05, neck/head/jaw/snout), added a `root` with no vertex group (it parents the rig and does not skin the mesh), and parented the loose foreleg and neck chains to the chest without moving heads, tails, or rolls (reimport: every joint matches his blend, roll delta 0). The mesh still wore the old vertex groups, so it was rebound with automatic weights. Those weights were then cleaned: at most 3 influences per vertex, anything under 0.05 removed, normalized, then smoothed so neighbouring verts around the joints agree. Unweighted 0. Root-only verts 0. His blend file was not saved over.

## Attack clip
- `NIGHTFANG_blenderig_attack.fbx` MD5 `f87f9b0b05cd8a8f229491b88a2fb5b4` — Scene take, frames 0–30 @30fps. The lunge is still 28 cm forward and back to the start. The rear is straightened in the pose layer (see below).
- Keys: anticipation f9 · strike f14 · release/impact **f14** · follow-through f20 · recover f30.
- Same design as the previous pass: in-place lunge (root forward 28 cm along Blender −Y, back to 0 by f30), jaw bite, and now both forelegs reach and rake. Claw-slash VFX stays visible on frames 13–18; its world point moved with the new snout (Unity 0, 0.381, 1.070).
- Export: binary FBX, axis_forward −Z / up Y, `FBX_SCALE_ALL` + apply unit scale so UnitScaleFactor stays 100 and the armature scale curves stay 1 (not the collapsed 0.01 skeleton). add_leaf_bones off, embed textures, bake all bones step 1, simplify 0, force start/end keys. Blender 5.2.2. Take name `Scene`. Generic.
- Reimport QC: 30 bones, armature scale 1, faces 200000, unweighted 0, frames 1–31, attack end pose matches the start (0 m). Foreleg toe travel about 0.83–0.88 m. Trot loop error 0 m.

## Trot clip
- Diagonal pairs (fore L with hind R, then the other pair), one stride per second, in place.
- Each paw steps about 32 cm forward and back. Foreleg toes travel about 0.37 m. Swing paws lift (left fore up to about 8 cm; the more upright right fore lifts less). Spine and head bob, tail follows.
- Reimport: armature scale stays 1,1,1 (no scale keys). Loop error 0. Fore toes still travel about 31 cm and 29 cm.
- Trot shake fix (2026-10-08): the left upper arm was reversing about 36° in a single frame, and 30,618 weights were under 0.05, so the skin swelled between bones. After the weight clean and a looping smooth of the trot curves, the max per-frame vertex jitter (second difference of each vertex) dropped from 29.3 cm to 2.3 cm. Mean of each vertex's worst jitter dropped from 3.4 cm to 0.8 cm. Max influences per vertex: 4 before, 3 after. Unity clip import keeps animation compression Off.
- Rear straightening (pose only, 2026-10-08): in the rest pose his spine already sits sideways. Chest joint x = −0.247 m, hips joint x = −0.125 m (12.2 cm to the side), tail root x = +0.052 m (29.9 cm). The hips bone yaws 22° off straight back and the tail root 42°. Pose channels on those bones are zero, so this is his edit-bone placement, not an animation lean. Edit bones were not moved, and the rest FBX was not rewritten. On the trot and the attack, the pose layer shifts the rear onto the chest's centerline and yaws the pelvis and tail root onto the travel axis (Blender +Y, straight back). The trot keeps the small sway, now centered on zero: pelvis yaw mean 0° (was −22°), range about ±2.4°; lateral offset mean 0 cm (was +12.2 cm), range about ±1 cm. Attack pelvis yaw and lateral offset are 0. Trot vertex jitter stays 2.3 cm. The blend file still has MD5 `ebb0c6f9b0d5ef55df95dd785938d11f`; its leftover `NIGHTFANG_clip` action keys the old bone names and does not move his skeleton.

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
