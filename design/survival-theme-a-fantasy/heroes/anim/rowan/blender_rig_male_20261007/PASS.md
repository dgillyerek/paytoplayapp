# ROWAN_male — PASS (Design remesh 2026-10-07)

- **Look SoT:** male full-body (locked MD5 `8142ea1f2316cf032badbae8fcc67b80`) — stubble/heavier build matches portrait; **blue fletching** (not purple).
- **Source GLB:** `meshy/male_20261007/Rowan_male_fullbody_texture.glb` MD5 `a4a6a85715d4772e8e0d61daf372adc9` (Meshy Azure Ranger, ~76MB).
- **Faces:** clean 2,457,160 → decimate **200,000** (verts 99,775); `prep.json` DZ 0.951958.
- **Bones:** 22 Mixamo-compatible (`mixamorig:*`), no fingers; ARMATURE_AUTO heat OK; unweighted 0/99775; maxinfl=4; normalize OK.
- **Caveat:** source is tight A-pose; extreme arm abduction webs the armpit (heat captures torso↔sleeve). Test poses use twist/walk/wave instead of T-pose. Legs/spine/walk clean.
- **Outputs:** `ROWAN_male_blenderig.blend`, `.fbx` (rest), `_walk.fbx`, `_sheet.jpg` (1600×1770), `stills/` (28 frames).
- **Old female remesh:** left untouched at `anim/rowan/blender_rig/` until Derek Game-view PASS on this male pack — do **not** overwrite.
- **LOOK_SOT mirror:** `LOOK_SOT/remesh_male_20261007/`
