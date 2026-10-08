# LYRA fitted (tighten): Blender humanoid rig pack (Design, 2026-10-08)

HOLD: staging only. Nothing committed, pushed or PR'd. Derek must check it in Game-view before any merge.

## Source
- Meshy GLB: `heroes/anim/lyra/meshy/tighten_20261008/Lyra_tighten_20261008_texture.glb`, MD5 `60ed0a89cda4284cdf433b5996d5382b`. 2.0M faces; the staff was fused to the right hand.
- Look SoT: `heroes/anim/lyra/LOOK_SOT/tighten_20261007/02_human_mage_lyra_FULLBODY_tighten.png`, MD5 `0887414eb31f8a0882e33a42bc2f7454`.

## Contents
- `LYRA_tighten_blenderig.blend`: rig SoT. Objects `LYRA_tighten_rig` (22 mixamorig bones) and `LYRA_tighten_body` (200,000 faces / 99,521 verts). Walk action `LYRA_tighten_walk` is stored with a fake user.
- `LYRA_tighten_blenderig.fbx`: rest pose. `LYRA_tighten_blenderig_walk.fbx`: walk, f0–f30 @30fps, f30 = f0. Both use the stage_export settings.
- `textures/`: Meshy atlas as 2048² PNG files (basecolor, metal_rough, normal).
- `props/LYRA_staff.fbx` + `LYRA_staff_props.blend` + `LYRA_staff_meta.json`: separate staff prop (14,048 faces).
  - Origin is at the grip, +Z runs from the foot to the crystal.
  - It is parented to `mixamorig:RightHand` with the offset in the meta file.
- `LYRA_tighten_rest_compare_lookSoT.png`, `LYRA_tighten_walk_sheet.jpg`, `LYRA_tighten_walk_preview.mp4`: QC.
- `work/`: joints.json, bone_overlay.png, batch.log, verify_rest_walk.json, scripts/, qc_frames/.

## What was done
- **Staff split**: same method as Rowan (region growth from staff seeds, geodesic contact resolution).
  - The two fist holes were patched with flat single-texel caps; no blur fill.
  - A generated cylinder (r 2.4 cm, one staff texel) bridges the 6.5 cm gap inside the fist. It is hidden by the fist.
- **Recentring**: the body was recentred on x = 0 (it was ~18 cm off) because the pipeline's weight cleanup assumes the midline is x = 0.
- **Joints**: the shared analysis put face_forward at +1, collapsed the arm chains and placed the neck too high. The wrapper `work/hum_analyze.py` overrides arms / neck / head and face_forward = −1. See bone_overlay.png.
- **Walk**: 10-05 `author_humanoid_walk()` from identity rest, loaded verbatim.

## Reimport QC (`work/verify_rest_walk.json`)
- Rest and walk: 22 bones each, same names and order. Frames 1–31.
- **Hips at the first frame vs rest FBX: 0.0° and 0.0 m. Not flipped.**
- Loop 0.0° and 0.0 m. Foot-Y correlation −0.998. Toes point −Y.

## Known leftovers
- Flat caps where the staff left the fist (staff-facing palm and thumb side). They are mostly covered while the staff is held.
- Walk: the 10-05 arm swing (±28°) is applied to the staff arm, so the 1.9 m staff swings like a pendulum (about ±30° at the foot). If this reads badly in Game-view, the fix is to reduce RightArm swing for the staff hand. That would be a deviation from the 10-05 method, so it was not done without approval.
