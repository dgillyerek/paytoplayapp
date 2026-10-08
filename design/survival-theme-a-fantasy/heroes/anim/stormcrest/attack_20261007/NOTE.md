# STORMCREST — attack clip (Design concept 2026-10-07, retargeted onto Derek's rig)

**Attack:** Wing Buffet: Storm Bolt
**Weapon / VFX:** Storm bolt VFX (blue-white forked lightning, royal-blue glow, gold sparks) cast from the beak

## Rig (Derek's hand-built skeleton is the source of truth)
- Rig blend: `heroes/anim/stormcrest/blender_rig/STORMCREST_blenderig.blend` MD5 `afea32f84fb33d1e13a339abea29f774`
- His 44 bones were kept in place (heads, tails, and rolls unchanged). They were default `Bone` / `Bone.00N` names and are now clean `.L` / `.R` names. A non-deforming `root` sits at the world origin and parents the chain roots without moving them. 45 bones total.
- The blend he committed had an Armature modifier with no armature assigned, and the mesh was still weighted to the old 22 mixamorig vertex groups. Nothing was weighted to his bones. Automatic weights were applied onto his bones. Two zero-length pins (`wing_pin.L`, `wing_pin_b.L`) do not deform; their weight went to the parent. Unweighted verts: 0.
- Rest FBX: `heroes/anim/stormcrest/blender_rig/STORMCREST_blenderig.fbx` MD5 `35dc5e9c396281225ca4734646bdd964`
- Walk FBX: `heroes/anim/stormcrest/blender_rig/STORMCREST_blenderig_walk.fbx` MD5 `f5efc9d99a8eef90755511748ed8c0a2`
- Wing flap FBX: `heroes/anim/stormcrest/blender_rig/STORMCREST_blenderig_wingflap.fbx` MD5 `8abcc49e79a36eb73cbd1c7dd8d9c5a0`
- ThemePack copies of those three FBXs match these MD5s. No cloth.

## Attack clip
- `STORMCREST_blenderig_attack.fbx` MD5 `9bae7b662f48a1c1eec33fafc0fed6d2` — action `STORMCREST_attack`, frames 0–30 @30fps (31 keys). Starts and ends at the bind pose.
- Same concept as the 2026-10-07 Wing Buffet: anticipation f9 (wings up, head pulled back) · strike/release **f14** (wings sweep forward and down, beak thrust, bolt leaves the beak) · follow f21 · recover f30 = rest.
- Bolt: projectile, visible frames 14–19, speed 4.0 m/s character-forward, length 2.76 m. Spawn is the head-bone tail at f14, Blender `(0.17318, -0.9018, 0.64927)`.
- Export: Blender 5.2.2, `FBX_SCALE_UNITS`, UnitScaleFactor 100, armature scale 1 (not 100), axis_forward −Z / up Y, add_leaf_bones off, embed textures, bake all bones, simplify 0. Same convention on rest, walk, wing flap, and attack. Generic in Unity (`animationType` 2, `useFileScale` 0, `bakeAxisConversion` 0).
- Reimport QC (leaf bones kept): 45 bones, armature scale `(1,1,1)` every frame, loop error 0 m on every clip. Attack wing-tip height 0.23–1.18 m. Feet stay planted.

## Props / VFX (separate assets — never baked into the body mesh)
- `STORMCREST_stormbolt_vfx.fbx` MD5 `2ab650e8aaf130d1e86e76a5dc0fc5b4` (1561 faces) — role `bolt`. Look reference only; Unity draws the lightning.
- All props in one blend: `STORMCREST_attack_props.blend` MD5 `8abe14989e039fa3e48615e4cdf3bcbc`
- `STORMCREST_stormbolt_vfx` — proj, release f14, speed 4.0 m/s, visible frames 14–19.

## QC images
- Look sheet, contact sheet, preview, and `stills/qc_*` / `stills/look_*` are the 2026-10-07 concept renders (the Mixamo-skeleton pass). They were not re-rendered. The new clip keeps that timing on Derek's skeleton.
- Work copy: `STORMCREST_attack_work.blend` is the old concept work file, not the rig source of truth.

## Caveats
- Griffin, not a humanoid. Wings, four legs, and a tail are body bones. Walk steps the legs and holds the wings near the body, opposite left/right. Wing flap is a large eased sweep, both wings together, tips from about knee height up to level with the raised wing. No cloth.

HOLD: do not merge until Derek Game-view PASS.
