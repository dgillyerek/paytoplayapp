# PASS EMBERFANG

bones=55 faces=200000 unw=0
- Kind: winged dragon custom skeleton (hero)
- Weighting: ARMATURE_AUTO + wing membrane anti-stealer; L/R suppress; 4-infl

## DEREK EDIT 2026-10-06 — tip bones + walk

Input: Derek hand-edit `EMBERFANG_dragonrig_derek_20261006.blend` (Blender **5.2**, MD5 `722e98ed04ea7e794c3f09a44909e794`). Read with portable 5.2.2; rebuilt exactly (head/tail/roll/parent/connect, assert <1e-5 m) in **Blender 4.2** on the prior mesh/mats host so the SoT `.blend` stays pipeline-openable (4.2/4.3). Backups: `broken_clips_backup/*_pre_derek_20261006.*`.

**What Derek changed (15 bones vs prior):**
- Wing tip segments `wing_f*_b.{L,R}` heads moved ~0.05–0.20 m (largest: `wing_f3_b.L` 0.20 m); corresponding `wing_f*_a` tails updated.
- `head` tip moved ~0.057 m + roll change (~0.79 rad).
- Removed non-deform `root` (prior had it; Unity BoneNames requires 55 including root).

**Cleanup:** Re-created non-deform `root` from prior placement; parented `hips` → `root`. Kept Derek names for all 54 bones. Result: **BoneCount=55**, BoneNames order matches Unity (`root, hips, spine_01…toe.R`).

**Re-skin (needed — tip placement change):** Cleared vertex groups → ARMATURE_AUTO heat → wing membrane anti-stealer (outer |x| wing-dominant → wing_* only; fence wing out of head/torso/legs) + L/R midline suppress + max 4 influences + normalize. Unweighted verts: **0**.
- Outer wing mean **1.0**, arm **0.0**, frac wing-dom **1.0**
- Body wing leak: head 0.0, chest 0.0026, legs 0.0194

**Clips** (procedural 0..30 @30fps, LINEAR + CYCLES, identity rest):
- `EMBERFANG_wingflap`: animlib-style flap — wing_root/arm/forearm primary + a-segment lag + **b-tip lag ~6° cos(p−1.4)**. Arms locked (no soft bounce). Light tail wave + tiny neck/head. Tip-vert mean travel **0.89729 m**; tip bone L travel **1.01621 m**; wing_z range **0.39598 m**.
- `EMBERFANG_walk` **NEW**: biped walk thigh ±22°, shin follow, arm counter-swing, spine tiny pitch/yaw, tail gentle wave; **ALL wing_* identity**. Root locked. Thigh L travel **0.05474 m**; wing tip pervert **0.06048 m** (near-zero vs flap); wing_z range **0.00159 m**.

**QC (re-imported FBXs):** bones=55, faces=200000, unw=0, root restored, BoneNames set match. Clips 31 keys, loop_err 0.0.

**Artifacts / MD5s (uncompressed):**
- blend `9a0a8385997963b7af71d11789539e4f` → `heroes/anim/emberfang/blender_rig/EMBERFANG_dragonrig.blend`
- rest FBX `91336af931c3a67dcf8754c8901e5fd3` → `EMBERFANG_dragonrig.fbx`
- wingflap FBX `405acb84ea35fbfd0358419483e05e54` → `EMBERFANG_dragonrig_wingflap.fbx`
- walk FBX `ac678a652c7d2158769eb35ec8fddace` → `EMBERFANG_dragonrig_walk.fbx` (**NEW**)
- Previews: `handoffs/anim_previews_20261005/rebake/EMBERFANG_wingflap_preview_derek20261006.mp4`, `EMBERFANG_walk_preview_derek20261006.mp4`
- Still: `stills/derek_tip_bones_20261006.jpg`
- Work: `/workspace/emberfang_derek_20261006/` (scripts adapted from ashwyrm_small)

**Dev tip-swap:** replace blend + rest + wingflap FBXs; **add** walk FBX. HOLD merge messaging for parent/Derek re-check.

## FIX 2026-10-06 — Derek Game-view FAIL on tip 8db30e1

Wing-tip motion on the flap was not enough. The body stayed still, and the walk left the wings at identity.

**Verified on the 4.2 SoT before the edit:** `EMBERFANG_wingflap` keyed wings, a tiny neck/head, and the tail. `hips`, `spine_01..03`, and `chest` were flat. Arms were already locked. `EMBERFANG_walk` keyed thighs, shins, arms, a small spine, and the tail. Every `wing_*` channel was flat.

**Edit (same blend, no reskin, rest pose untouched):**
- Wing flap: hips / spine_01..03 / chest pitch with the wing phase (about 4.2° at the hips down to 1.4° at spine_03, chest 1.8°) plus a ±1.8 cm hip bob. Wing tip keys kept. Arm and thigh local channels stay identity.
- Walk: wing_root ±10°, wing_arm ±5°, forearm ±3°, finger a ±4°, finger b ±6°, sine with the gait and opposite the left thigh. Not a second flap. Leg and arm keys kept.

**Posed QC (Blender 4.2.23):**
- Flap chest travel 0.187 m, head 0.322 m, tip bone 1.066 m. Body mesh mean 0.196 m. Tip mesh mean 0.872 m. Thigh local amp 0. Upper arm local amp 0.
- Walk tip bone 0.322 m, tip mesh mean 0.253 m (was ~0.06 m). Thigh local amp still 0.37. Hips local amp 0.

**MD5s after this fix:**
- blend `0c0c7e631edbdc9af09e515d2030d24a` (still Blender 4.2)
- rest `91336af931c3a67dcf8754c8901e5fd3` (unchanged bind)
- wingflap `e8dba1177daa684cee71a0fa965efefe`
- walk `35c6a70d6941a7a6d11838eed6622a65`

HOLD merge until Derek Game-view PASS.

## POLISH 2026-10-07 — Derek Game-view notes on tip ca85a86

Same 4.2 SoT blend. No reskin, no new mesh, rest bind untouched. Arms stay locked on the flap. BoneCount stays 55.

**Walk**
- Neck and head are no longer identity. Pitch lags the chest dip (neck_01 ~4° peaking ~3 frames after the spine, then neck_02 / neck_03), and the head yaw leads the spine yaw by ~3 frames. Local peaks: neck_01 4.1°, head 3.5°.
- Wings keep the small gait sway (wing_root ±10°). Outer feathers add a downstroke-only droop (forearm 4°, finger a 6°, finger b 9°). Finger bend is ~25° while the wing is coming down and ~16° at the top of the sway.

**Wing flap**
- Hips, spine_01..03, and chest local channels are back to identity (no pitch, no hip bob). Chest world Z travel is 0. The front stays planted. Wing tip Z travel is still 0.70 m.
- Legs pedal. Thigh ±6°, knee folds the other way (shin ±12°, knee angle span 24°), ankle ±12° (span 24°), toe ±5°. Left and right are opposite. Toe travel is 9.3 cm, not a second walk.
- Same downstroke feather droop as the walk, a little stronger (forearm 6°, finger a 10°, finger b 14°). Finger bend peaks at ~28° mid-downstroke versus ~19° at the up pose.

**Rest:** `EMBERFANG_dragonrig.fbx` MD5 unchanged.

**MD5s after this polish (uncompressed):**
- blend `24e11c02d65178f95f919ecfacabcaf1` (Blender 4.2.23)
- rest `91336af931c3a67dcf8754c8901e5fd3` (unchanged bind)
- wingflap `c437cc09e8807d46418616e00ba1eca7`
- walk `1e1b02ba8dc5d9d333f7f879b37e9114`

HOLD merge until Derek Game-view PASS. Unity Game view was not run in this bake.

## POLISH 2026-10-08 — Derek Game-view FAIL on tip 70d8c90

Same 4.2 SoT blend. No reskin, no new mesh, rest bind untouched. BoneCount stays 55. Wing curves were not rewritten. The planted flap front stays planted.

**Wing flap**
- Rear pedal cut from 9.3 cm of toe travel to 3.2 cm. Knee angle span stays 24°. Ankle span is 12.0° (was 23.9°), still a hinge rather than a stiff stick. Thigh ±3.5°, shin ±12°, foot ±6°, toe ±2°, left/right opposite.
- Front legs (upperarm / forearm / hand) now pedal. Hand travel 3.0 cm (was 0). Elbow span 15.6°, wrist span 11.8°. This replaces the earlier arm-lock on the flap.
- Downstroke wing droop and the identity hips/spine/chest are unchanged.

**Walk**
- Neck and head rotations scaled to 0.40 of tip 70d8c90. Head amplitude 3.45° → 1.38°. Neck_01 4.06° → 1.63°. Head-tip vertical travel 16.2 cm → 4.1 cm.
- Spine pitch that hopped the chest is removed (spine_02 identity, spine_01 keeps only its small yaw). Chest vertical travel 3.69 cm → 0.08 cm. Hip vertical bob 0.00 cm → 0.05 cm.
- Hips ride the stride: 4.0 cm forward at each step extreme, ±2.4 cm lateral weight shift, ±4° roll onto the forward leg. The rear stride and the wing droop are unchanged.

**Before / after**

| measure | tip 70d8c90 | this bake |
|---|---:|---:|
| flap rear toe travel | 9.27 cm | 3.20 cm |
| flap front hand travel | 0.00 cm | 3.04 cm |
| walk head rotation amplitude | 3.45° | 1.38° |
| walk hip vertical bob | 0.00 cm | 0.05 cm |
| walk chest vertical (the hop) | 3.69 cm | 0.08 cm |

**Rest:** `EMBERFANG_dragonrig.fbx` MD5 still `91336af931c3a67dcf8754c8901e5fd3`.

**MD5s after this bake (uncompressed):**
- blend `0286f1670ae2cb2a3bd5431e361605ba` (Blender 4.2.23)
- rest `91336af931c3a67dcf8754c8901e5fd3` (unchanged bind)
- wingflap `65f4a039ac82839fdb42bf7c23b120e5`
- walk `49ad72ed7a6bf4ecfb6af2b7cf9052cd`

HOLD merge until Derek Game-view PASS. Unity Game view was not run in this bake.
