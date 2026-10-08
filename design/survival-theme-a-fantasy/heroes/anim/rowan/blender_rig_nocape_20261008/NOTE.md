# ROWAN no-cape: Blender humanoid rig pack (Design, 2026-10-08)

HOLD: staging only. Nothing committed, pushed or PR'd. Derek must check it in Game-view before any merge.

## Source
- Meshy GLB: `heroes/anim/rowan/meshy/nocape_20261008/Rowan_nocape_20261008_texture.glb`, MD5 `240e072a80a583e334c018c72e392e8f`. 2.12M faces; the bow was fused to the right hand.
- Look SoT: `heroes/anim/rowan/LOOK_SOT/nocape_20261007/03_human_ranger_rowan_FULLBODY_nocape.png`, MD5 `97cc52a25cebe133e4c9da355bccdc02`.

## Contents
- `ROWAN_nocape_blenderig.blend`: rig SoT. Objects `ROWAN_nocape_rig` (22 mixamorig bones) and `ROWAN_nocape_body` (199,999 faces / 99,710 verts). Walk action `ROWAN_nocape_walk` is stored with a fake user; the blend is saved at rest.
- `ROWAN_nocape_blenderig.fbx`: rest pose. `ROWAN_nocape_blenderig_walk.fbx`: walk, f0–f30 @30fps, f30 = f0. Both use the hum_pipeline stage_export settings.
- `textures/`: the Meshy atlas as 2048² PNG files (basecolor, metal_rough, normal). The same images are packed in the blend and embedded in the FBX files.
- `props/ROWAN_bow.fbx` + `ROWAN_bow_props.blend` + `ROWAN_bow_meta.json`: separate bow prop (16,284 faces).
  - The prop has its own 2-bone armature: `bow_grip` (root) and `bow_nock`. Translating `bow_nock` along local +Y draws the string.
  - Origin is at the grip, +Z runs along the bow, and local −Y is the arrow direction.
  - It is parented to `mixamorig:RightHand` at rest and in the walk. The offset matrix is in the meta file.
- `ROWAN_nocape_rest_compare_lookSoT.png`: look SoT beside rig rest front / side / back / side.
- `ROWAN_nocape_walk_sheet.jpg` and `ROWAN_nocape_walk_preview.mp4`: walk QC.
- `work/`: joints.json, bone_overlay.png, batch.log, verify_rest_walk.json, scripts/, qc_frames/.

## What was done
1. **Weapon split** (welded mesh, graph region growth from bow seeds):
   - Blocked zones at the fist, string and boot contacts; ambiguous contacts resolved by geodesic Voronoi assignment.
   - The Meshy string was deleted and replaced by a generated string.
   - Holes in the body were patched with a fan fill, using one rim texel per cap. This gives a flat solid colour: no blur fill, smear or mirror pad.
2. **Quiver**: stays in the body mesh. It is weighted rigidly to `mixamorig:Spine2` (6,654 verts fully rigid, 2,764 blended in a 2 cm falloff, ≤4 influences, normalized).
3. **hum_pipeline** prep + build: ~200k faces, 22 bones, heat weights, 0 unweighted verts.
   - The shared joint analysis mis-detected this mesh: face_forward was +1 (toes pointing backwards), the left arm chain went up into the quiver, the neck bone was 36 cm long, and the leg roots were asymmetric.
   - Fix: `work/hum_analyze.py` (per-character wrapper) overrides face_forward = −1 and supplies measured arm / neck / head joints. Legs are taken per side at symmetric knee and ankle heights. See bone_overlay.png.
4. **Walk**: 10-05 `author_humanoid_walk()` from identity rest (local XYZ euler), loaded verbatim from `handoffs/anim_previews_20261005/_rebake_clips.py`. The pose-stage walk was NOT used; it has the parent-relative quat bug that flipped Hips 180°.

## Reimport QC (`work/verify_rest_walk.json`)
- Rest FBX: 22 bones, 199,999 faces, no animation.
- Walk FBX: same 22 bone names and order. Frames 1–31.
- **Hips at the first frame vs rest FBX: 0.0° and 0.0 m. Not flipped.**
- Loop first vs last frame: 0.0° and 0.0 m. Left/right foot-Y correlation −0.996 (feet alternate). Toes point −Y.

## Known leftovers
- Flat single-texel caps on the top and bottom of the right fist where the bow was cut out, plus a few small jagged flaps under the fist.
- A generated leather grip cylinder (r 2.4 cm) bridges the gap inside the fist. Part of it shows briefly below the fist.
- Walk hips "bob" is part of the 10-05 method. On this rig it moves along Y by about 2.5 cm, not Z. This is inherited from the method and was not changed.
