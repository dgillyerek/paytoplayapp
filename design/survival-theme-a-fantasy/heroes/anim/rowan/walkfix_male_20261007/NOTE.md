# ROWAN_male walk — rebake fix (2026-10-07)

**New clip:** `ROWAN_male_blenderig_walk.fbx` MD5 `3bd5f6d392ff61c8f4ae1ac23a39fae3`
**Replaces (for Game-view review only, nothing overwritten):** `../blender_rig_male_20261007/ROWAN_male_blenderig_walk.fbx` MD5 `ab148b67d09b4f39f2923f4db8549d66`.

## Bug
`hum_pipeline.make_walk_clip` keyed `parent.matrix⁻¹ @ bone.matrix` (parent-relative armature-space quaternions) into `rotation_quaternion`, which is relative to the bone's REST orientation. On reimport the Hips sit 180.104° off rest at frame 1 and the body plays folded, z 0.563–1.55 m (rest is 0.00–1.90 m). The feet barely move (L/R foot Y range is 0.053 m).

## Fix (same method as handoffs/anim_previews_20261005/_rebake_clips.py)
- Opened `../blender_rig_male_20261007/ROWAN_male_blenderig.blend` (MD5 `9255d17cd7be53fcb5d6b911500a9d64`) read-only. It was never saved, and the armature, mesh, weights and bind are unchanged.
- Re-authored the walk with the 10-05 `author_humanoid_walk()`, loaded verbatim from that script. Keys are local XYZ Euler from identity rest. Thigh ±28°, knee 7–27°, foot ±8°, arms ±28° counter-phase, hips bob 2.5 cm and yaw. Frames 0–30 @30fps, with frame 30 = frame 0.
- Action/take name: `ROWAN_male_walk`, the same as the original.
- Export uses the exact `hum_pipeline.stage_export` walk call: axis -Z/Y, FBX_SCALE_ALL, unit scale, global 1.0, no leaf bones, embedded textures (COPY), use_armature_deform_only False, FACE smoothing, use_mesh_modifiers False. Bake settings: all bones, no NLA, current action only, forced start/end keys, step 1, simplify 0. Blender 4.3.2.

## Verify (reimported FBX, frames 1–31 in Blender's importer)
| check | new | old (bug) |
|---|---|---|
| frame 1 mesh bbox z | -0.021 – 1.9 m | 0.563 – 1.55 m |
| Hips frame 1 vs rest FBX | 0.0 m / 0.0° | 180.104° |
| L/R foot Y correlation | -0.994 | 0.599 |
| foot fore-aft range | -0.259 → 0.482 m | 0.532 → 0.584 m |
| loop f1 vs f31 | 0.0 m / 0.0° | |
| bones / faces / vgroups vs rest FBX | same / 200000 / same | |

Lead foot by frame (1→31, `=` = passing): `=RRRRRRRRRRRRRR=LLLLLLLLLLLLLL=`
Hips z 0.88–0.882 m. Rest bbox z 0.003–1.9 m.

## Caveats
- The 10-05 recipe holds the knees at about 17° even at passing. The lowest mesh point is -0.021 m (rest is 0.003 m), from the cape/boot hem deforming about 2 cm.
- The arm swing follows the 10-05 local axes. On this rig it reads as a swing out to the side: the bow swings out to x ≈ -1.09 m at the extreme, and the quiver bundle (the left-arm bones) sways. This matches the existing left-arm/quiver weighting caveat.
- It is an in-place cycle with no root motion.

## Files
- `ROWAN_male_walk_contact.jpg`: rear + 3/4 front, f00–f27 every 3 frames.
- `ROWAN_male_walk_preview_rear.mp4`: rear gameplay cam, 3 loops, 90 frames @30fps, 480×640, EEVEE. Rendered from the reimported new FBX.
- `work/`: verify_walk.json, bake_meta.json, the scripts, and ROWAN_male_walkfix_work.blend (a compressed copy; not the rig SoT).
- The protected dirs `blender_rig_male_20261007/` and `LOOK_SOT/remesh_male_20261007/` were checked against MD5s taken before the work and are byte-identical.
