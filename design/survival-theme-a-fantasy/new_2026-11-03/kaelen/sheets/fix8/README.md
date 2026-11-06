# Kaelen FIX8 — native left wrist recovery

Review-only. Do not merge until Derek approves the contact sheets and motion.

## What changed

- Cross thrust retains its original 48-frame / 30-fps overhead lift, downward across-body strike, torso and foot timing. The front/side review videos are 1.60 seconds.
- The left hand eases from the previous guard bend into its native, nearly straight alignment by frame 27. Actual forearm-to-hand bend is **8.28° throughout frames 34–48**, versus FIX7's 20.24°. The glove is not flattened or reweighted.
- The left elbow reaches up to 6 cm farther forward during the strike/recovery, maintaining hand/weapon grip while separating the blade paths. Natural forearm pronation is retained rather than hiding twist in a wrist: cross-thrust left roll stays within approximately 6–70°, and late recovery f34–48 spans only about 44°.
- Idle is unchanged, including every animation key.

## Required all-clip cleanups / tradeoffs

The stronger scan found existing defects outside the reported wrist pose. These are intentionally visible for review:

1. **Walk:** the previous free arm swing put both blades through the moving thighs and let the swords touch. Arms now carry a relaxed forward guard with 2° stride sway. The original hips, torso, legs, feet, gait and 36-frame duration are unchanged.
2. **Dual slash:** a between-frame blade crossing remained in FIX7 even though integer frames passed. The right-arm strike is delayed by up to **6 frames / 0.20 seconds** within the same 58-frame clip, giving the two attacks a cleaner left-then-right rhythm. Wrists, grips, torso and feet are unchanged.
3. Cross thrust's duration, overhead timing and right-arm timing are unchanged. Preserving anatomical forearm roll proved safer than a forced 30° roll cap, which turned the left blade into the other blade's sweep.

## Delivered-asset verification

All six FBXs were exported and imported again. Body skeleton fingerprint **6212627f289a1abb**, 20 bones, is preserved in rest and all four animated FBXs. Cape skeleton **41e5849c3e331ac1**, 10 bones, is unchanged. Native Blender bone matrices, parents and connection flags are bit-for-bit unchanged too.

All body/sword/cape rest vertices, polygons and skin weights hash-identical to main. All non-edited torso/gait/foot channels hash-identical to main; see the integrity JSONs. No contact-vertex flattening, mesh deformation edits, new bones or grip sliding.

| Clip | Frames | Sword–sword min cm | Sword–body min cm* | Limb/head/torso/other-arm min cm | Fresh cape/body min mm |
|---|---:|---:|---:|---:|---:|
| idle | 299 | 5.09 | 3.37 | 3.16 | 2.37 |
| walk | 36 | 10.59 | 4.66 | 3.16 | 4.95 |
| attack_dualslash | 58 | 6.44 | 4.66 | 3.30 | 2.92 |
| attack2_crossthrust | 48 | 6.31 | 4.66 | 2.47 | 4.77 |

\* The actual gripping hand is excluded from sword/body clearance; grip is checked separately. Inner hand/handle gap remains 0.15 cm on both sides. Palm/axis landmark distances remain 2.42 / 2.00 cm, with no per-frame drift. Physical wrist bend across all four clips stays at or below 23.36°; cross-thrust hand twist is approximately zero, with forearm roll range under 120° on each side. Cross-thrust contact-foot drift is 0.049 cm (0.49 mm), unchanged in the core animation.

### Check scope

- **441 body poses:** every integer frame of all four re-imported clips, using nearest-surface and mesh-edge/triangle crossing checks for swords, hands, forearms, head/shoulders, torso and opposing arms. No detected intersections.
- **3,000 sword-pair poses:** exact triangle BVH overlap plus bidirectional sampled nearest surfaces. Fast attacks sampled every 1/16 frame (**480 Hz**); idle/walk every 1/4 frame (120 Hz). No overlapping samples; all measured gaps exceed the 3 cm gate.
- **619 fresh cape simulation poses:** current cape geometry and current corrected actions, including warm/settled loops. Zero detected body or sword intersections on every simulated frame. The cape's attached fabric naturally sits closer than the weapon/body clearance target; its actual millimetre-scale gap is reported rather than hidden.
- Quaternion hemispheres are continuous on edited channels, with linear interpolation. The prior zero hand-twist metric alone was not treated as proof of a natural wrist: the actual native hand/forearm axes were measured and the late glove viewed from the requested front-right three-quarter angle.

These are measured finite-sample checks, not a mathematical proof of every instant between samples or an in-engine runtime test. Runtime animation blending, weapon attachments and spring settings can still differ; owner visual review remains required.

## Review files

- `KAELEN_FIX8_SHEET.png`: front/side motion plus new left-wrist closeups through f34–48.
- `KAELEN_FIX8_WRIST_F34_48.png`: front-right three-quarter, front and side wrist details.
- `KAELEN_FIX8_WALK_DUAL_SHEET.png`: the required guard-carry and phase tradeoffs.
- `clips/`: six normal-speed front/side MP4s for cross thrust, dual slash and walk, plus a quarter-speed f34–48 left-wrist closeup.
- `metrics/`: full per-frame body/wrist/grip reports, sword sweep samples, fresh cape results, native skeleton/core channel hashes and video probes. `SUMMARY.json` is the rollup.

The final metadata pass also repairs inherited nested image paths: the authoring file uses `//../final/` paths with packed PNGs, and FBXs use portable texture basenames. All six final FBXs were re-imported with image search disabled; textures resolve from the shipped final folder, and full mesh/skeleton/animation fingerprints match the measured exports exactly. Packed PNG bytes match the unchanged shipped PNGs. See `portable_fbx_equivalence.json` and `packed_texture_integrity.json`.

The previews use geometry-review colors (red swords and blue fresh-sim cape); original texture assets are unchanged. Cameras fit the complete deformed blade paths, not just the body.

## Authoring / exports

- `heroes/anim/kaelen/blender/KAELEN.blend`: combined editable source, existing action names.
- `heroes/anim/kaelen/final/KAELEN_{rig_rest,idle,walk,attack_dualslash,attack2_crossthrust,cape}.fbx`: all six exports.
- Base: main `259608d691eafa96b571fd6abf1089798b642de4` (includes FIX7).
- Local reproducibility scripts and rejected candidates remain outside the repository in `/workspace/rigjob/kf8`; no scripts or application code are included in this art change. Progress log: `/workspace/rigjob/PROGRESS_kaelen_fix8.md`.
