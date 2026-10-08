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

## DEREK EDIT 2026-10-06b — small placement tweaks + walk

Input: `derek_edit/ASHWYRM_blenderig_derek_small_20261006.blend` (Blender 5.2 SoT, MD5 `934e9cd0ff57d6b5112c852784e9aa7a`, 32835718 B, mtime 2026-10-06 11:34:55 ET). Copied from derekdesktop `D:\Development\paytoplayapp\design\...\ASHWYRM_blenderig.blend`. Read with portable 5.2.2; rebuilt exactly (head/tail/roll/parent/connect, rebuild delta **0 m**) in 4.2/4.3 on the prior cleaned mesh so the main `.blend` stays pipeline-openable. Backups: `broken_clips_backup/*_pre_small_20261006.*`.

**What Derek changed vs armsrebuild (`d84e2817…`):** bone placement tweaks only (same 29-bone naming / mesh size). 12 bones moved (max tip ~0.11 m on `wing_f3.L` / `wing_f4.L`; wing wrist/arm joints ~3–4 cm; `tail_03.003` tip ~7.8 cm). Arms/legs/spine/tail_01..05 placements unchanged.

**Cleanup (same renames as armsrebuild):** `wing_root.{L,R}.001/.002` → `upperarm/forearm.{L,R}`; `tail_03.001/.002/.003` → `tail_04/05/06`; add non-deform `root`; spine→root. Result: **30 bones** (29 deform + root). Mesh/UVs/materials from prior cleaned base (vert-identical).

**Re-skin:** cleared groups → ARMATURE_AUTO heat → same cleanup as armsrebuild (wing fence, outer anti-stealer + anti-arm-stealer, anti-thigh-stealer for front arms, L/R suppress, top-4, normalize). Scripts: `/workspace/ashwyrm_small_20261006/`.

**Clips (identity rest base):**
- `ASHWYRM_clip` wingflap 0..30 @30fps linear+cyclic — same wing amps as armsrebuild; **NO** root/spine/thigh/shin/arm motion; light tail ±5° wave only.
- `ASHWYRM_walk` (**NEW**) 0..30 @30fps linear+cyclic — procedural biped/dragon: thigh ±22° opposing X, shin ~half follow, upperarm ±10° counter, forearm ±5° follow, spine pitch/yaw ±2.5/±2°, tail_01..06 lateral ±4°, **root locked**, all `wing_*` identity.

**QC:**
- FBX: 30 bones, 199,999 faces, 0 unweighted, 31 frames, loop err 0.0 (rest + wingflap + walk)
- Outer `|x|>0.35` mean wing **0.996** / arm **0.000**, frac wing-dom 0.996; tip wing 1.00
- Front-limb mean arm **0.959** / leg 0.001 / wing 0.007 / spine 0.030
- Wingflap tip-vert mean travel **0.93 m** (tip bone ~1.06 m); wing_z_mean range 0.61 m; front 0.0006 m
- Walk tip-vert mean travel **0.057 m** (wings near-rest); front-limb ~0.042 m
- Previews: `handoffs/anim_previews_20261005/rebake/ASHWYRM_wingflap_preview_small.mp4`, `.../ASHWYRM_walk_preview.mp4`
- Stills: `stills/derek_small_bones.jpg` / `stills/arms_vs_wings_labeled_small.jpg`

**Dev tip-swap:** replace **BOTH** rest+wingflap **and NEW** `ASHWYRM_blenderig_walk.fbx`. Skeleton still **30 bones** with same names as armsrebuild — Unity BoneNames likely unchanged unless names drifted. **HOLD merge** until Derek Game-view PASS. Context: PR #32 demo + PR #33 SoT.

## POLISH 2026-10-08 — wing-flap body follow + walk tail / hip sway

Derek Game-view: the wing flap itself is fine and the body is too still; the walk tail should swing side to side with follow-through, and the body needs a subtle step-tied yaw and roll without a hop. Rebaked from `ASHWYRM_blenderig.blend` in Blender 4.2.3 LTS. **Rest pose, weights, materials, and bone count (30) unchanged.** Rest FBX not re-exported. Attack FBX, props, and VFX not opened.

**Wing flap** (`ASHWYRM_clip`, 0..30 @30fps, linear + cyclic). Wing curves and the existing tail wave were not rewritten.
- Legs: opposing pedal, thigh ±2.6° and shin ±2.2° on world X (sign flexes the hock).
- Arms: both sides rise with the wings. Upperarm world Y, ±3.2° × cos(φ) (L sign −1, R sign +1). Forearm world X, ±3.6° × cos(φ−0.45), sign −1.
- Head/neck: spine pitch ±2.2° × cos(φ), symmetric, no upward bias. Root stays locked. There is no neck bone, so this is a small snout follow, not a chest lunge.

**Walk** (`ASHWYRM_walk`). Thigh, shin, and forearm curves were not rewritten (stride kept). Wings stay identity.
- Tail yaw around world up, lagging the step. Degrees `[7, 14, 4, 3.5, 3, 2.5]` on `tail_01..06`, each bone 1.40 rad (~6.7 frames) after the previous. `tail_01` leads; `tail_02` carries the curl.
- Spine yaw ±4.2° and roll ±2.2° on the existing pitch, phase-locked to the stride (`sin(φ+0.31)`).
- Shoulders counter that: both upperarms yaw ∓3.0° and roll ∓1.8°. No root translation.

**QC** (re-imported FBX, same measure as the pre-polish clips). Bind matches the rest FBX: 30 bones, same head/tail, 199999 faces, vertex-coordinate sum identical, both 2K textures embedded. Loop error 0.

| | before | after |
|---|---|---|
| Flap leg mean travel | 0.00046 m | 0.0562 m |
| Flap arm mean travel | 0.00180 m | 0.0295 m |
| Flap head mean travel | 0.00038 m | 0.0223 m |
| Flap head vertical | 0.00014 m | 0.0019 m |
| Flap wing-tip mean travel | 0.847 m | 0.861 m |
| Walk tail-tip lateral (X peak-to-peak) | 0.070 m | 0.226 m |
| Walk tail zero-cross, `tail_01` → `tail_06` | (small wave) | +1.9 frames |
| Walk spine yaw peak-to-peak | 0.89° | 4.20° |
| Walk spine roll peak-to-peak | 3.97° | 10.54° |
| Walk spine pitch peak-to-peak | 4.95° | 4.98° |
| Walk spine-tail vertical | 0.0022 m | 0.0029 m |
| Walk thigh.L forward stride | 0.184 m | 0.208 m |

MD5: flap `a2aef92b8d0c0e8d8287693e1583b39e`, walk `8e29ae51ff9898157c57ea9716631af7`, blend `d26062494346447bebf68fd1bab28d4c`. Rest FBX still `a432672643153588f2e50cb4e24927b8`. Attack FBX still `b0d240cc17cf79af0af3ed22be5f07ef`.

**Attack is Derek Game-view PASS (2026-10-07).** Wing flap and walk await Game-view. **HOLD merge.** Menu: **Survival/Ashwyrm Demo (rest + wing flap + attack, Game view 1080x1920)**. Dropdown already lists **rest, wing flap, walk, attack**.

## FIX 2026-10-08 — Game-view saw no motion (ebe6533)

Derek: wing flap and walk were completely still. Import had no errors. Files matched `a2aef92b…` / `8e29ae51…`. Blender reimport of those files still showed the polish, so the curves existed.

**Diff vs f4e4ead (the export Unity did play), both clips:**

| | f4e4ead (moved) | ebe6533 (still in Unity) | this re-export |
|---|---|---|---|
| Take | Scene | Scene | Scene |
| Keys / bone channel | 31 | 31 | 31 |
| Curve targets | 30 LimbNodes + `ASHWYRM_rig`, `Lcl Rotation` / `Translation` / `Scaling` | same | same |
| Bone paths | `ASHWYRM_rig/root/…` (30 bones) | same | same |
| `UnitScaleFactor` | 1 | 100 | 1 |
| `ASHWYRM_rig` scale curve | 100, 100, 100 | 1, 1, 1 | 100, 100, 100 |

The demo instantiates the **rest** FBX (armature scale 100, `useFileScale` 0) and plays the clip on that hierarchy. ebe6533 was exported with `FBX_SCALE_ALL`, so the clip's scale curves set `ASHWYRM_rig` to 1 and collapsed the skeleton. f4e4ead used the bake that leaves UnitScaleFactor at 1 and the armature scale at 100 (`FBX_SCALE_NONE`). Bone paths and the Scene take were already the same.

Re-exported both clips from the polished blend with `FBX_SCALE_NONE`, same axis, leaf-bone, and bake settings as the working clips. Actions were not rebaked. Rest FBX and attack FBX were not rewritten.

**QC** (Blender 4.2.3 reimport, frames 1–31, armature-local bone-head travel). Pose copied onto the rest armature (`ASHWYRM_rig|Scene` → rest pose bones) matches these numbers, so the clip drives the rest hierarchy.

Wing flap, bone-head peak-to-peak: wing tips dZ 0.342 / 0.396 m (f4e4ead was 0.333 / 0.386). shin.L dY 0.045 m (was 0). forearm.L path 0.067 m (was 0). Spine pivot stays put; spine euler X now spans 4.35°, which is the snout follow. Root still locked.

Walk, bone-head peak-to-peak: tail_06 dX 0.210 m (f4e4ead 0.066). thigh/shin stride kept (shin.L dY 0.208 m, was 0.184). Spine euler spans X 4.30° / Y 5.07° / Z 10.16° (f4e4ead 4.92° / 0.78° / 4.00°). Root still locked. Wing bones stay at identity; their small head travel is the spine carrying them.

MD5: flap `139c1d3e11622aa5eeaf6436c23ce660`, walk `52b6a4b53ace591cafd19ad8a7c70c50`. Blend unchanged `d26062494346447bebf68fd1bab28d4c`. Rest still `a432672643153588f2e50cb4e24927b8`. Attack still `b0d240cc17cf79af0af3ed22be5f07ef`. **HOLD merge.**

## Derek Game-view on b391b49 (2026-10-08) — looser upper body, flatter flap, tail tip

Derek: the walk upper body (spine, wings, head) rotated as one block. Reduce that a little and offset head, spine, and wings so they follow each other. Wings may sway a little instead of staying at identity. Arms, legs, and tail stay as passed. Wing flap should read more horizontal through the downstroke, keeping the slight leg, arm, and head motion. A stray tail-tip spike that juts back and up comes off the mesh. Attack motion stays the passed clip. Export `FBX_SCALE_NONE`.

There is no neck bone. The snout rides `spine`. Overlap is spine yaw leading, roll two frames later, pitch (the snout) four frames later, each at 0.86 of the b391b49 amplitude. Wing roots lag that spine by six frames at half strength (about 6° local). The arm and the fingers take a smaller, later share of that drag so the membrane is not one rigid panel. Arm, leg, and tail world aim is restored onto the new spine; their bends stay within 0.03° of b391b49. The hip and the tail socket still shift with the spine (max 1.44 cm at the thigh, 1.25 cm along the tail) because those bones are parented to it.

**Mesh.** 272 verts past `tail_06`, continuing back and up from the tip, deleted. The 32-edge hole is one cap face with a borrowed boundary UV. Body is 99649 verts / 199427 faces (was 99921 / 199999). Packed images are byte-identical (`ASHWYRM_basecolor_0` `541ffa273bde961e96d87f0123fe84f9`, metal-rough `ef784e788ac96566aa84e14a6bd0a1c7`, normal `ddf337b3dda03151b34feff3b7a96db4`).

**Wing flap.** `wing_f1.L` direction Z was +0.65 to −0.89 (steep downstroke). It is now +0.38 to −0.28, with X staying 0.92–0.99, so the spread stays outward and the downstroke stays near level. Non-wing keys are unchanged (max delta 0). Reimport bone-head travel of thighs, shins, arms, spine, and tail matches b391b49 within 0.03°. Wing-tip head dZ is 0.150 / 0.173 m (was 0.342 / 0.396).

**Walk** (Blender 4.2.3 reimport, frames 1–31, bone-head travel vs b391b49):

| | b391b49 | this export |
|---|---|---|
| shin.L forward span / peak | 0.208 m / f8 | 0.192 m / f9 |
| shin.L aim vs b391b49 | — | 0.03° |
| tail_06 lateral / forward span | 0.210 / 0.330 m, peaks f23 / f12 | 0.208 / 0.335 m, same peaks |
| tail_06 aim vs b391b49 | — | 0.03° |
| forearm.L path | 0.030 m forward | 0.030 m, aim 0.03° |
| snout X / Z peak | f18 / f8 | f16 / f12 |
| wing_root.L local | identity | 6.0° |
| wing_f1.L direction vs rigid pose | — | 9.1° |

**Attack.** Same mesh. 93 rotation channels, 2883 samples, max abs error vs `b0d240cc17cf79af0af3ed22be5f07ef` is 4.6e-5 on `wing_inner.R` Lcl Rotation Z. Translation max 2.4e-7, scale max 4.6e-6. Same 31 keys, take Scene.

**Scale.** `FBX_SCALE_NONE`, `apply_unit_scale` on, scene in meters. All four FBXs: `UnitScaleFactor` 1, `ASHWYRM_rig` and `ASHWYRM_body` Lcl Scaling 100, 100, 100. Rest has no animation curves. Flap, walk, and attack keep a scale curve of 100 on the rig. Unity was not run; this is the same headless reimport as the scale fix, not a Game-view pass.

MD5: blend `01bef02707494e480f3c04e09ba111bc`, rest `f1f9f937f2961ce185eb2d942d91d595`, flap `d0def08faa525437f8d27bcb52d139e3`, walk `b7137b2d687efb4ad7408cf7d94b6492`, attack `9dc8b9a2d2083290b6a369129a6469b3`. Theme Pack matches the design FBXs. **HOLD merge.**

## Derek Game-view on 44b9044 — opposite walk wings, level flap

Derek: walk wings should swing opposite each other, like the arms, with the stride. Keep that bake's lag and a modest amplitude, and leave the rest of the walk. Wing flap should sit about 75° from vertical (15° off horizontal) through the stroke. Leg, arm, spine, and tail keys stay. Mesh, rest FBX, and attack FBX stay.

**Walk.** A shared yaw on `wing_root.L` and `wing_root.R` (24° peak, world Z, cosine locked to the left-leg stride) sends the wings opposite ways: about 1.1 cm of tip travel per degree, and no vertical change. Outer-wing keys are the previous lag/overlap, untouched. Non-wing FBX curves match 44b9044 exactly (max abs 0). Reimport, frames 1–30: left wing tip most forward at f11, right at f24, 13 frames apart (156°). Forearms are f10 and f25, so the wings step with the arms. Tip forward span 0.232 m / 0.202 m (was 0.136 / 0.167). Shin, forearm, and tail forward frames are unchanged.

**Wing flap.** Each wing bone's elevation is recentered on +15° from horizontal, keeping 22% of its old up/down stroke and the same outward heading. `wing_f1` is 11.4°–19.9° above horizontal (mean 15.0°), which is 70.1°–78.6° from vertical (mean 75.0°) on both sides. Non-wing FBX curves match 44b9044 exactly.

**Unchanged files.** Rest `f1f9f937f2961ce185eb2d942d91d595`. Attack `9dc8b9a2d2083290b6a369129a6469b3`. Mesh still 99649 verts / 199427 faces. Export `FBX_SCALE_NONE`, `UnitScaleFactor` 1, rig scale 100. Unity was not run.

MD5: blend `d7d38f857c4317fe98529e07e264f743`, flap `29380f058b6c70b228a8d15b438937ca`, walk `490e574a13f505202af9c468a9f38a0e`. **HOLD merge.**

## Derek Game-view on 9a860fb — flap plane at 45°

Derek: the flap is too parallel to the ground. Set the wing plane to about 45° (mean `wing_f1` ~45° above horizontal) and give it a bigger eased stroke, raised at the top and below the shoulders at the bottom, without crossing over the head. Walk, rest, attack, and the mesh stay.

The stroke is one eased beat. It crests at 67.4° (wingtips stay outboard of the skull, about 0.55 m above the shoulder) and eases down to −8.6° (tips about 9 cm below the shoulder in the reimport), then eases back up. Both sides match.

**Reimport** (Blender 4.2.3, frames 1–30): `wing_f1` **−8.63° to 67.38° above horizontal, mean 45.00°**, left and right. Tip vertical travel 0.646 m (left) and 0.744 m (right). Leg, arm, spine, and tail curves match 9a860fb exactly (max abs 0). `FBX_SCALE_NONE`, `UnitScaleFactor` 1, rig scale 100.

**Unchanged.** Walk `490e574a13f505202af9c468a9f38a0e`. Rest `f1f9f937f2961ce185eb2d942d91d595`. Attack `9dc8b9a2d2083290b6a369129a6469b3`. Unity was not run.

MD5: blend `bd32eeb99fbde2aad3faa71f8b1c40f1`, flap `a63f63b4ca464bbe8a110e58eb2671b1`. **HOLD merge.**

## Derek Game-view on db79128 — swept-back flap

Derek: the wings go too high and move too robotically. They should come up and down the length of the body, very smoothly, tips straight back with a slight bend as they come down.

The wings are no longer spread to the side. Each bone is aimed back along the body (tip direction Y 0.86–0.97, only about 0.2 of X) and the stroke is one sine over the existing 30 frames. `wing_root` leads, `wing_arm` lags 2 frames, the fingers lag 3–5, so the trailing tip peaks 4 frames after the shoulder. On the lower half of the sine the fingers curl down a few extra degrees and straighten again on the way up. Keys in the blend are bezier with the same slope at frame 0 and frame 30. Blender's FBX exporter still stamps the shared linear key flag it uses for every clip; the values themselves are one sine sample per frame, so the speed follows a cosine and there is no held crest.

**Reimport** (Blender 4.2.3, take Scene, frames 1–31): `wing_f1` **−27.99° to −8.04°** above horizontal on both sides (was −8.6° to +67.4°). Leading tip height versus the hip back line **+0.002 m to +0.237 m**. Trailing tip (`wing_f4`) **−0.220 m to +0.059 m**, so the downstroke hangs along the flank. The skinned posterior tip matches that: **−0.219 m to +0.115 m**, and the FBX reimport puts that same vert at 0.622 m / 0.289 m. Shoulder tail peaks at frame 7, leading tip at frame 9 (**lag 2**), trailing tip at frame 11 (**lag 4**). Frame 0 and frame 30 match (loop error 0). Non-wing curves match db79128 exactly (max abs 0). `FBX_SCALE_NONE`, `UnitScaleFactor` 1, rig scale 100. Mesh still 99649 verts / 199427 faces.

**Unchanged.** Walk `490e574a13f505202af9c468a9f38a0e`. Rest `f1f9f937f2961ce185eb2d942d91d595`. Attack `9dc8b9a2d2083290b6a369129a6469b3`. Unity was not run.

MD5: blend `403ce89136072133e0d7144a45f4c195`, flap `d7e8afee7141418457bf9d0deb7fa4fd`. **HOLD merge.**
