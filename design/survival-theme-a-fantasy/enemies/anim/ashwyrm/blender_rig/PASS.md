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

## Derek Game-view on 5059fad — open fan, swept for lift

Derek: the flap wings look squished next to the walk, and the whole wing should rotate back so the tips sit further toward the tail, like the wing is getting lift.

The finger fan is the walk spread again, stored once against the arm and replayed with it, so the membrane stays open for the whole loop. `wing_f1` to `wing_f4` is **86.75° min, 89.13° mean** (walk **87.67° min, 88.16° mean**). Membrane area, the three triangles from the wrist through the finger tips, averages **0.2810** against the walk's **0.2775**, ratio **1.013**. Outer fingers still curl 3° and 5° on the downstroke only. The shoulder tail peaks at frame 7, the arm at frame 8, the leading tip at frame 9 (**lag 2**), and the trailing tip at frame 11 (**lag 4**).

Each wing is yawed an extra **20°** back from the shoulder. Left arm heading goes from 48° to **68°**. Arm elevation stays **−21.96° to −8.04°**. The rearmost finger (`wing_f2`) moves from Y **+0.668..+0.763** to **+0.787..+0.819**; at frame 0 that tip is **+0.100 m** further back (0.717 → 0.817), and the new range sits entirely behind the old one. Leading tip frame 0 is **+0.037 m** in Y (0.595 → 0.632) and **+0.421 m to +0.584 m** above the hip back line (was +0.002..+0.237), Z **0.944..1.104**. The outer trailing tip (`wing_f4`) opens outboard, so its frame-0 Y is **0.084 m** less than the collapsed spear (0.645 → 0.560) while its height versus the back line is **−0.122 m to +0.149 m** (was −0.220..+0.059). Frame 0 and frame 30 match (loop error 0). Non-wing FBX curves match 5059fad exactly (max abs 0). Arm and inner blend keys are unchanged; the FBX float32 rewrite of those channels is 3e-5. `FBX_SCALE_NONE`, `UnitScaleFactor` 1, rig scale 100. Mesh still 99649 verts / 199427 faces.

**Unchanged.** Walk `490e574a13f505202af9c468a9f38a0e`. Rest `f1f9f937f2961ce185eb2d942d91d595`. Attack `9dc8b9a2d2083290b6a369129a6469b3`. Unity was not run.

MD5: blend `f5be0757bdb9b8a4fcc27700ea26e165`, flap `e163c1a87712c56cfde4976d08d25e83`. **HOLD merge.**

## Derek Game-view on df54a63 — rigid wing at the shoulder

Derek: part of the wing sits back, but the flap still does not look like the standing pose. Rotate the whole wing at the shoulder.

Every wing bone past `wing_root` (`wing_arm`, `wing_inner`, `wing_f1`..`wing_f4`, both sides) holds its standing local rotation on every frame. The largest difference from the rest pose is **0.000°**. The membrane triangles match the rest pose exactly (area ratio **1.000**). Left fan stays **87.71°**, right fan stays **92.59°**. There is no finger lag and no curl.

Only `wing_root` moves. It yaws the standing wing **50°** back about vertical, then beats **±7°** (14° peak to peak) on the same sine as before. The stroke off that swept pose is **0.00° to 6.96°**, peaking at frame 7. Angle from the rest pose is **50.00° to 50.45°**. Leading tip Z is **1.227..1.393** (peak frame 6 once the spine pose is included). Left tip Y through the cycle:

| Bone | Standing Y | Flap Y |
|---|---|---|
| wing_f1 | −0.005 | +0.418..+0.504 |
| wing_f2 | +0.016 | +0.563..+0.582 |
| wing_f3 | +0.143 | +0.507..+0.543 |
| wing_f4 | +0.161 | +0.316..+0.370 |

Frame 0 and frame 30 match (loop error 0). Non-wing FBX curves match df54a63 exactly (max abs 0). `FBX_SCALE_NONE`, `UnitScaleFactor` 1, rig scale 100. Mesh still 99649 verts / 199427 faces.

**Unchanged.** Walk `490e574a13f505202af9c468a9f38a0e`. Rest `f1f9f937f2961ce185eb2d942d91d595`. Attack `9dc8b9a2d2083290b6a369129a6469b3`. Unity was not run.

MD5: blend `2e3e338d5b8ec67c5cc876b9da297dc8`, flap `ca88e019bcb1744f73907001f80d12d1`. **HOLD merge.**

## Derek Game-view on 1c64ab9 — level wings, larger beat

Derek: the wings look full again. Keep that standing shape. Take off the backward yaw so the wings face forward like the standing pose, pitch them about 20° forward/down so they sit nearer level, and make the beat much bigger.

Bones past `wing_root` still hold the standing local rotation (max difference **0.000°**). Membrane area ratio flap/rest is **1.000**. Left fan **87.71°**, right fan **92.59°**.

`wing_root` has no rearward yaw. Its local pitch at the level pose (frame 0 and frame 30) is **−20.00°** on the left and **+20.00°** on the right, the mirror. That drops the left wing-centroid elevation from **+2.25°** at standing to **−17.1°**. Around that pose the shoulder beats **±28°** (sampled frames **−47.85° to +7.85°** versus standing, **55.7°** peak to peak). The sine crests at frame 7. Leading tip Z is **0.724..1.396**. Left centroid azimuth stays **−0.2° to +17.0°**, with the standing heading, instead of the old 50° sweep. Frame 0 and frame 30 match (loop error 0). Non-wing FBX curves match 1c64ab9 exactly (max abs 0). `FBX_SCALE_NONE`, `UnitScaleFactor` 1, rig scale 100. Mesh still 99649 verts / 199427 faces.

**Unchanged.** Walk `490e574a13f505202af9c468a9f38a0e`. Rest `f1f9f937f2961ce185eb2d942d91d595`. Attack `9dc8b9a2d2083290b6a369129a6469b3`. Unity was not run.

MD5: blend `7d147011df8ae4f08a8203b4deb0aee2`, flap `b7ef0b307f3be6326d54afd98cb34fdc`. **HOLD merge.**

## Derek Game-view on bbfe31b — shoulder pitch flipped to 40°

Derek: the wing looks better, but the shoulder rotated the wrong way. Go about 40° in the opposite direction.

Same axis as bbfe31b. The level pose (frame 0 and frame 30) is now **+40.00°** on the left and **−40.00°** on the right. The beat around that center is still the **±28°** sine (sampled **+12.15° to +67.85°** on the left, **−67.85° to −12.15°** on the right). Crest stays at frame 7, bottom at frame 22. Loop error 0.

Wing-centroid elevation (standing was left **+2.25°**, right **+2.17°**):

| | Center (f0) | Top (f7) | Bottom (f22) |
|---|---|---|---|
| Left | +41.91° | +66.96° | +13.98° |
| Right | +38.63° | +61.39° | +13.12° |

Bones past `wing_root` stay at the standing local rotation (max difference **0.000°**). Membrane area ratio **1.000**. Non-wing FBX curves match bbfe31b exactly (max abs 0). `FBX_SCALE_NONE`, `UnitScaleFactor` 1, rig scale 100. Mesh still 99649 verts / 199427 faces.

**Unchanged.** Walk `490e574a13f505202af9c468a9f38a0e`. Rest `f1f9f937f2961ce185eb2d942d91d595`. Attack `9dc8b9a2d2083290b6a369129a6469b3`. Unity was not run.

MD5: blend `102480a286929f6cec8b7564a15dabe2`, flap `561778dfe4a3678929779c7a80be70a7`. **HOLD merge.**

## Derek Game-view on 0ab347a — flap lowered below the head

Derek: better, but on the upstroke the wings come up too high into the head. Make them go down further and not as high.

Same shoulder axis and direction as f82f633, same standing wing shape. Only `wing_root.L` / `wing_root.R` keys changed (left positive, right the mirror). The shoulder now beats **−22° ± 18°** from standing instead of **+40° ± 28°**: top of the stroke **−4.1°** (was **+67.85°**), bottom **−39.9°** (was **+12.15°**). Crest still frame 7, bottom frame 22, 30 frames at 30 fps, frame 0 equals frame 30 (loop error 5e-7 m).

The cap comes from the head: the standing wing already reaches head height (highest wing vertex 1.347 m against a head/horn top of 1.352 m), so any shoulder lift above standing puts the tips over the head. The bottom comes from the body: below about −40° the right outer finger starts entering the right forearm.

Wing-centroid elevation from the shoulder (static spine, standing was left **+2.8°**):

| | Top (f7) | Center (f0) | Bottom (f22) |
|---|---|---|---|
| Left | −1.2° | −18.7° | −36.0° |
| Right | −1.1° | −17.1° | −32.5° |

**Reimport** (Blender 4.2.3, frames 1–31): highest wing vertex **1.313 m** at the crest against head top **1.348 m** in that frame (was **1.702 m**, over the head). Highest finger tip **1.21 m**. Wing vertices inside the head outline from the front camera **0** on every frame (was up to **5575**). Lowest wing vertex **0.21 m** (was **0.557 m**), so the downstroke hangs to the hips. Wing faces inside the body: only the inner membrane edge against the upper arm (present in the standing pose too) and, near the bottom, the left inner edge brushing the tail root; no finger or tip enters the legs. Non-wing FBX curves match f82f633 within float noise (max 5e-5°). `FBX_SCALE_NONE`, `UnitScaleFactor` 1, rig scale 100. Mesh still 99649 verts / 199427 faces. Blend: bones, walk action, and every other channel unchanged.

**Unchanged.** Walk `490e574a13f505202af9c468a9f38a0e`. Rest `f1f9f937f2961ce185eb2d942d91d595`. Attack `9dc8b9a2d2083290b6a369129a6469b3`. Unity was not run.

MD5: blend `7e0da670017cee72a0ba0ac8e0025481`, flap `e9b1dc479dde71c9c44d8fe6e22c5557`. **HOLD merge.**

## Derek Game-view on fcec802 — wings swept back at the shoulder

Derek: the wing flap still has the wings placed vertically. Rotate at the shoulders so the wing tips are further back. Do not distort the wings at all.

Only `wing_root.L` / `wing_root.R` keys changed. Each shoulder key is now a yaw about the body's vertical axis (taken in the shoulder's rest frame, positive = tips toward the tail, right side mirrored) applied on top of the fcec802 beat (−22° ± 18°, top −4.1° at frame 7, bottom −39.9° at frame 22). The sweep is **32° ± 13°**, trailing the beat by 2 frames: **45°** back near the top of the upstroke (frame 9.5), **32°** at mid-stroke, **19°** near the bottom (frame 24.5). Same 30 frames at 30 fps, frame 0 equals frame 30 (loop error 4e-7 m).

Why the sweep eases off on the downstroke: the tail curls to the left behind the hip, and with a fixed 30° or more the left trailing finger (`wing_f4.L`) cuts through `tail_02` on the downstroke (fixed 30°: frames 23–29; fixed 35°/40°: most of the lower half). A fixed 25° is clean but sits lower than the 35–45° brief. With the sweep easing to 19° at the bottom, no finger crosses the tail, legs, or forearms on any frame (triangle intersection test, frames 1–31). The fcec802 right-finger/forearm touch on the downstroke is gone too.

Furthest-back wing point (Y, + is toward the tail): frame 7 **0.575 / 0.540 m** left/right (fcec802 0.151 / 0.089), frame 15 **0.447 / 0.463 m** (0.091 / 0.070), frame 22 **0.177 / 0.276 m** (0.076 / 0.052). Side and top-down renders confirm the tips go back toward the tail, not forward.

**No wing distortion.** In the blend every other wing bone (`wing_arm`, `wing_inner`, `wing_f1`–`wing_f4`) and every other channel is identical to fcec802. In the FBX, non-`wing_root` curves match fcec802 within float noise (max 6e-5°). Membrane check on the reimport: 74,384 edges whose vertices are weighted only to one side's wing bones keep their rest length on every frame within **4.2e-6 m** (same rounding floor as fcec802, 4.1e-6 m).

**Head and body.** Highest wing vertex 1.314 m, at least 3.4 cm under the head and horn top on every frame. 0 wing vertices inside the head outline from the front camera. Lowest wing vertex 0.21 m. The inner membrane next to the body (`wing_inner`) still overlaps the upper arm and tail root along its body-side edge, as it did in fcec802; the sweep makes that overlap larger (mostly on the left, hidden behind the arm and the tail root in the renders).

`FBX_SCALE_NONE`, `UnitScaleFactor` 1, rig scale 100. Mesh still 99649 verts / 199427 faces. Bones unchanged.

**Unchanged.** Walk `490e574a13f505202af9c468a9f38a0e`. Rest `f1f9f937f2961ce185eb2d942d91d595`. Attack `9dc8b9a2d2083290b6a369129a6469b3`. Unity was not run.

MD5: blend `6fd955905099cf49e0e7aebeddb1b9ff`, flap `6f2b3924678d0fa6e9191cad096726c3`. **HOLD merge.**

## Derek Game-view on be76d61 — wings laid flat for lift

Derek: the flap motion looks good, but rotate the entire wings from the shoulder so the tips point up toward the sky and the wings look like they would generate lift. Keep the motion the same.

Only `wing_root.L` / `wing_root.R` keys changed. The be76d61 yaw sweep is removed. Each whole wing is first turned **70°** about the body's side-to-side axis (world X through the shoulder; the same rotation on both sides, which is its own mirror). That lays the fan flat: the old top finger becomes the leading edge in front, the lower fingers trail back toward the tail, and the membrane faces the ground. The beat then runs on the same shoulder axis and timing as before (±18°, crest frame 7, bottom frame 22, 30 frames at 30 fps, first frame equals last), centred **+10°** up instead of −22°, so the wings hold a dihedral V. That gives **−8° to +28°**. Left positive, right mirrored.

Membrane plane (fit to the finger vertices), angle of its normal from vertical, left / right: frame 0 **14° / 8°**, top (f7) **31° / 24°**, mid (f15) **14° / 9°**, bottom (f22) **7° / 15°** (be76d61 stood upright, about 71° / 76°). Finger tips above the shoulder joint, mean left / right: mid **0.26 / 0.21 m**, top **0.41 / 0.35 m**, bottom **0.08 / 0.06 m**.

**Collisions.** Triangle intersection between the wing and the head, body, arms, tail, and legs: **none on any frame** (frames 1–31). From the front camera no wing vertex is inside the head outline. At the top of the stroke the outer wing reaches 1.39 m, about 4 cm above the horn top, but well out to the side of the head.

**No distortion.** In the blend every other wing bone and channel is identical to be76d61. In the FBX, non-`wing_root` curves match be76d61 within float noise (max 6e-5°). The 74,384 single-side wing edges keep their rest length on every frame within 4.2e-6 m. Shoulder Euler curves are continuous (largest step 6.2° per frame).

`FBX_SCALE_NONE`, `UnitScaleFactor` 1, rig scale 100. Mesh 99649 verts / 199427 faces. Bones unchanged.

**Unchanged.** Walk `490e574a13f505202af9c468a9f38a0e`. Rest `f1f9f937f2961ce185eb2d942d91d595`. Attack `9dc8b9a2d2083290b6a369129a6469b3`. Unity was not run.

MD5: blend `ab154426820cb622fd5c55b6a47c8f24`, flap `4321658f1ff3dd3c33227db80115d6ac`. **HOLD merge.**
