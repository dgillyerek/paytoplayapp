# PASS ASHWYRM

bones=30 faces=199999 unw=0 clip_max=0.924
- Kind: winged dragon custom skeleton
- Weighting: ARMATURE_AUTO; L/R suppress; 4-infl; **wing membrane rebind 2026-10-05**
- Caveat: auto wing placement; soft fold leftovers OK on extreme poses

## FIX 2026-10-05 — wingflap looked like membrane stretch / idle breath

**Root cause: B (weights wrong).** Not a missing-bone rig, not a missing-key clip.
- Rig has proper wing chains (14 bones): `wing_root → wing_arm → wing_forearm → wing_f1..f4` × L/R.
- Rebaked clip already keyed real flap: `wing_root`/`wing_arm` euler X ±~45° (quat amp ~0.76/0.65 in FBX). Tip bone travel ~0.86m.
- Heat weights had put outer wing mesh mostly on front **arm** bones (`forearm`/`hand`/`upperarm`) because auto arm bones over-span into wing space. Outer mean wing influence was ~0.17 (Emberfang ~1.0). Bones flapped; mesh stayed locked → preview looked like chest/tail breath only.

**Fix:** Geometric re-bind of outer wing verts onto `wing_*` bones; suppress arm/leg stealers. Soft leftovers OK.

**QC after:**
- Outer `|x|>0.35` mean wing ~0.94, arm ~0.06, frac wing-dom ~0.98 (was wing~0.17 / arm~0.60)
- Tip-vert mean travel ~0.76m (was ~0.11m); wing_z_mean range across cycle ~0.40m
- Preview: `handoffs/anim_previews_20261005/rebake/ASHWYRM_wingflap_preview.mp4`

**Dev tip-swap:** replace **both** `ASHWYRM_blenderig.fbx` (rest+weights) and `ASHWYRM_blenderig_wingflap.fbx` (clip+weights). Clip-only tip-swap is insufficient if Unity uses rest pack mesh. Backups: `broken_clips_backup/*_pre_wingweight.fbx`.

## DEREK EDIT 2026-10-05 — Derek's hand-placed wing bones adopted as source of truth

Input: `derek_edit/ASHWYRM_blenderig_derek_20261005.blend`. It was saved in **Blender 5.2**, so Blender 4.3 can't open it. Bones were read with portable 5.2.2 (`/workspace/tools/blender-5.2.2-linux-x64`), then rebuilt exactly (head/tail/roll/parent/connect, <1e-5 m) in 4.3 on the previous blend, which keeps the main `.blend` openable by the pipeline. Mesh, UVs, materials and transforms are byte-identical to before (vert max diff 0.0).

**What Derek changed:** deleted all 46 auto bones and drew a new 25-bone skeleton, all named `Bone`…`Bone.024`. Each wing is a root → arm bone up to the wrist, a fan of 4 fingers along the membrane spars, and 1 inner bone down the trailing membrane. Each leg is 2 bones. One chain runs chest → hips → 3 tail bones. The wings now follow the mesh's raised wings; the old auto bones lay flat at z≈0.85. He removed the old "arm" bones that were stealing wing weights. He left no head, neck, jaw or feet bones. 5 chains were unparented and there was no root bone. Bone.011 and Bone.016 had zero length and Bone.014 was a 3.7 cm accidental stub. He did **not** re-skin: the mesh still carried 45 vertex groups for the deleted bones, so his rig moved nothing.

**Cleanup (no bones moved):** renamed `wing_root/wing_arm/wing_f1..f4/wing_inner.{L,R}` (fingers numbered top to bottom, so f1 is the leading edge), `thigh/shin.{L,R}`, `spine`, `tail_01..03`. Added non-deform `root` (same spot as before). Parented spine→root and wings and thighs→spine. Deleted the 3 degenerate bones. All bones deform except root. Result: 23 bones (22 deform).

**Re-skin:** removed the stale groups and ran ARMATURE_AUTO heat. Outer wing was already 1.00 wing, but heat bled wing bones into the body (head 0.14, chest 0.27, torso 0.31, legs 0.17 wing). Fixes: wing bones fenced out of the centre/legs (smoothstep |x| 0.12→0.26, z 0.48→0.58) with that weight moved to the nearest body bone; the PASS outer-membrane anti-stealer (|x|>0.35 wing-dominant → wing bones only); L/R midline suppress; 4 influences; normalize. Script: `work/derek_edit_qc/`.

**Clip:** `ASHWYRM_clip`, 0..30 @30fps, linear + cyclic, keyed from identity rest. Per side (mirrored): wing_root −15+30cos (+15° up … −45° down), wing_arm −8+16cos(φ−0.5), wing_f1..4 −4+10cos(φ−1.0), wing_inner 6cos(φ−0.5). Also root bob 2.5 cm, spine pitch ±3°, tail yaw ±5° wave, thighs ±4°.

**QC (re-imported FBX):** 23 bones, 199,999 faces, 0 unweighted, 2 embedded 2K textures, 31 frames, loop error 0.0.
- Outer `|x|>0.35` mean wing 1.00 / non-wing 0.00, frac wing-dom 1.00; tip `|x|>0.7` wing 0.99
- Wing share: head 0.00, neck/chest 0.00, torso 0.005, legs 0.001
- Tip-vert mean travel **0.72 m** (per-vert 0.88 m; earlier good ~0.76); wing_z_mean range 0.36 m; head 0.05 m, feet 0.09 m
- Preview: `handoffs/anim_previews_20261005/rebake/ASHWYRM_wingflap_preview_derekedit.mp4`; bones still: `stills/derek_edit_bones.jpg`

**Dev tip-swap:** replace **both** FBXs. The skeleton changed from 46 to 23 bones with new names, so Unity avatars, prefabs and any clips bound to old bone paths (`hips`, `chest`, `wing_forearm`, `upperarm`…) must be re-bound or re-imported. Backups: `broken_clips_backup/*_pre_derekedit.*`.
**Known limits:** head, neck and jaw ride the `spine` bone rigidly (no head bones). Feet follow `shin`.

## FIX 2026-10-06 — soft arm bounce on wingflap (Derek tip 24fbc94 / PR #32 HOLD)

**Root cause:** Ashwyrm has separate bipedal front-limb mesh (arms+claws) distinct from wings, but Derek's 23-bone rig has **no arm bones**. Heat weights put those verts mainly on `thigh`/`shin` (hands/forearms) and `spine` (shoulders). The derek-edit clip keyed thighs ±4°, spine ±3°, and root bob 2.5 cm, so the arms rode the body motion and looked like a soft bounce next to the flap.

**Fix (Path A — clip only; weights unchanged):** Rebaked `ASHWYRM_clip` with **ZERO** motion on `root` / `spine` / `thigh.*` / `shin.*`. Wings kept identical amps (`wing_root` −15+30cos, `wing_arm` −8+16cos(φ−0.5), fingers −4+10cos(φ−1.0), `wing_inner` 6cos(φ−0.5)). Light tail ±5° wave kept. Identity rest. Did **not** restore old auto arm bones.

**QC (blend eval):**
- Front-limb mean per-vert travel **0.056 m → 0.0035 m** (centroid 0.053 → 0.0008); head 0.050 → 0.0
- Wing tip mean travel **0.875 → 0.917 m** (still clear flap); outer-wing centroid ~0.60 m
- Path B (rebind) not needed — residual front travel ≪ 3 cm after zeroing thigh/spine keys

**Artifacts:**
- Preview: `handoffs/anim_previews_20261005/rebake/ASHWYRM_wingflap_preview_noarmbounce.mp4`
- Labeled still: `stills/arms_labeled_rest.jpg` (red=front limbs, blue=wing tips)
- Backups: `broken_clips_backup/*_pre_noarmbounce.*`
- QC json: `work/derek_edit_qc/noarmbounce_qc.json`

**Dev tip-swap:** **clip-only** — replace `ASHWYRM_blenderig_wingflap.fbx`. Rest FBX weights/skeleton unchanged (same 23 bones); optional rest FBX refresh is harmless but not required. HOLD merge until Derek re-checks.

## DEREK EDIT 2026-10-06 — arm bones + tail tip (armsrebuild; NOT clip-only)

Input: `derek_edit/ASHWYRM_blenderig_derek_arms_20261006.blend` (Blender 5.2 SoT, ~32.8 MB, ~11:07 ET). Read with portable 5.2.2; rebuilt exactly (head/tail/roll/parent/connect, delta **0 m**) in 4.3 on the prior cleaned mesh so the main `.blend` stays pipeline-openable (4.2/4.3). Backups: `broken_clips_backup/*_pre_armsrebuild.*`.

**What Derek added (on top of cleaned 23-bone names):**
- Front limbs as `wing_root.{L,R}.001/.002` (2-bone chains parented to `spine`) — renamed `upperarm.{L,R}` / `forearm.{L,R}` (no hand bone).
- Tail tip extension `tail_03.001/.002/.003` → `tail_04`/`tail_05`/`tail_06`.
- Repositioned thigh heads slightly (kept HIS placements). Wings/spine/tail_01..03 unchanged from prior Derek edit.
- No zero-length accidents. No root in his file (we re-added non-deform `root`). Stale vertex groups were still the prior 22 (no arm groups) — he did not re-skin.

**Cleanup:** 30 bones total (29 deform + root). Parenting: spine→root; wings/arms/thighs→spine (Derek); forearm→upperarm; shin→thigh; tail chain connected. No bones moved.

**Re-skin:** cleared groups → ARMATURE_AUTO heat → cleanup:
1. Fence wing bones out of head/torso/legs (smoothstep |x| 0.12→0.26, z 0.48→0.58).
2. Outer-membrane anti-stealer + **anti-arm-stealer for wings** (`|x|>0.35` wing-dom → wing only; strip arm from outer/mid-wing).
3. **Anti-thigh-stealer for arms** (verts near arm bones, not outer wing → upperarm/forearm; strip leg/spine steal).
4. L/R midline suppress; max 4 influences; normalize.
Scripts: `work/derek_edit_qc/armsrebuild_*.py`.

**Clip:** `ASHWYRM_clip` 0..30 @30fps, linear+cyclic, identity rest. Wings same amps as derek-edit (`wing_root` −15+30cos, `wing_arm` −8+16cos(φ−0.5), f1..4 −4+10cos(φ−1.0), inner 6cos(φ−0.5)). Subtle arm idle: upperarm ±2.5° / forearm ±3° sin. Spine ±1° only (no root bob / thigh keys — arms have own bones). Tail ±5° wave through tail_01..06.

**QC:**
- FBX: 30 bones, 199,999 faces, 0 unweighted, 31 frames, loop err 0.0
- Outer `|x|>0.35` mean wing **0.996** / arm **0.000**, frac wing-dom 0.996; tip wing 1.00
- Front-limb mean arm **0.959** / leg 0.001 / wing 0.008 / spine 0.031
- Tip-vert mean travel **0.92 m** (centroid 0.84; tip bone ~1.02–1.06 m); outer centroid ~0.60 m; wing_z_mean range 0.61 m
- Front-limb mean travel **0.016 m** (subtle idle, not bounce); head 0.005 m
- Preview: `handoffs/anim_previews_20261005/rebake/ASHWYRM_wingflap_preview_arms.mp4`
- Stills: `stills/derek_arms_bones.jpg` / `stills/arms_vs_wings_labeled.jpg` (orange=wing, red=arm, green=leg, cyan=spine/tail)

**Dev tip-swap:** replace **BOTH** `ASHWYRM_blenderig.fbx` (rest+weights) **and** `ASHWYRM_blenderig_wingflap.fbx` (clip). Skeleton changed 23→30 bones (new upperarm/forearm + tail_04..06); Unity avatars/prefabs/clips bound to old paths must re-bind. **HOLD merge** for Derek Game-view check.
**Known limits:** no hand/neck/head/jaw/feet bones — head rides spine; claws follow forearm; feet follow shin.
