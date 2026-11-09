# IRONHOWL.blend

Built 2026-11-03 (America/New_York) with Blender 4.2.3 headless from dgillyerek/paytoplayapp origin/main @ 394775d1 (read-only; nothing committed).

Base skinned file: `Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/ironhowl/IRONHOWL_rig.fbx`. Armature `IRONHOWL_Armature` (33 bones, object scale [0.009999999776482582, 0.009999999776482582, 0.009999999776482582]), mesh `IRONHOWL_Mesh` (177233 verts, scale [100.0, 100.0, 100.0]). Geometry, weights and keys untouched (no cleanup/decimation/keyframe reduction).

Default state: no action assigned = rest/bind pose. Every clip is its own Action with fake user + a muted NLA track of the same name.

## Source files

| repo path | MD5 |
|---|---|
| `Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/ironhowl/IRONHOWL_mixamo_standing_melee_attack_downward.fbx` | b9424842fd939da1c349993ffe0b84d8 |
| `Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/ironhowl/IRONHOWL_mixamo_standing_melee_combo_attack_v2.fbx` | 53449519ca7b87c34699dbab97c291b0 |
| `Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/ironhowl/IRONHOWL_rig.fbx` | 2076255b828787ba8db7ea24d075f231 |
| `Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/ironhowl/IRONHOWL_rig_atlas.png` | 7c4b6b14ef204acf51139789b76d498d |
| `Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/ironhowl/IRONHOWL_rig_attack.fbx` | 21cd216d46c3363ad9af8993a92daba3 |
| `Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/ironhowl/IRONHOWL_rig_backhand_sweep.fbx` | 5d933a147d5cf6ef5d1358603cb62164 |
| `Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/ironhowl/IRONHOWL_rig_ground_pound.fbx` | b2e7b6f1b12bdeb230deaab063204bd1 |
| `Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/ironhowl/IRONHOWL_rig_rest.fbx` | 3c04265d7dc6d25693c373f8cb4e5e27 |
| `Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/ironhowl/IRONHOWL_rig_shoulder_charge.fbx` | 5b5eb4b9fd4b19cb9e5e0583c54c86c0 |
| `Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/ironhowl/IRONHOWL_rig_walk.fbx` | 8c49b0f95c70418c2e7938036080040d |
| `Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/ironhowl/Ironhowl_meshy_look.jpg` | 1df4a62b540f60c0bcd894c6b6a9eadc |
| `Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/ironhowl/ironhowl_basecolor.png` | a07ddfb3f0a3adc7537e2ea53f31ab77 |
| `Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/ironhowl/ironhowl_metallic.png` | 666cd7ec271b3be5515f4d7e529d776e |
| `Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/ironhowl/ironhowl_normal.png` | 3dd8309a9ddffeefe37d116135e33bf4 |
| `Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/ironhowl/ironhowl_roughness.png` | 9ded8e2f9a36f067fe135a52d320455d |

## Actions (fps 30, frames as stored in Blender; import anim_offset=0 so frame 0 = FBX time 0)

| action | source | frames | bind pose matches skin? |
|---|---|---|---|
| rig | ironhowl/IRONHOWL_rig.fbx | 0-1 | yes |
| mixamo_standing_melee_attack_downward | ironhowl/IRONHOWL_mixamo_standing_melee_attack_downward.fbx | 0-68 | NO (bind differs, max 102.99 armature units) -> raw keys verbatim; use `mixamo_standing_melee_attack_downward_onskin` |
| mixamo_standing_melee_attack_downward_onskin | baked from ironhowl/IRONHOWL_mixamo_standing_melee_attack_downward.fbx onto skin bind (armature-space match, every frame) | 0-68 | yes |
| mixamo_standing_melee_combo_attack_v2 | ironhowl/IRONHOWL_mixamo_standing_melee_combo_attack_v2.fbx | 0-126 | NO (bind differs, max 102.99 armature units) -> raw keys verbatim; use `mixamo_standing_melee_combo_attack_v2_onskin` |
| mixamo_standing_melee_combo_attack_v2_onskin | baked from ironhowl/IRONHOWL_mixamo_standing_melee_combo_attack_v2.fbx onto skin bind (armature-space match, every frame) | 0-126 | yes |
| rig_attack | ironhowl/IRONHOWL_rig_attack.fbx | 0-26 | yes |
| rig_backhand_sweep | ironhowl/IRONHOWL_rig_backhand_sweep.fbx | 0-40 | NO (bind differs, max 102.99 armature units) -> raw keys verbatim; use `rig_backhand_sweep_onskin` |
| rig_backhand_sweep_onskin | baked from ironhowl/IRONHOWL_rig_backhand_sweep.fbx onto skin bind (armature-space match, every frame) | 0-40 | yes |
| rig_ground_pound | ironhowl/IRONHOWL_rig_ground_pound.fbx | 0-52 | NO (bind differs, max 102.99 armature units) -> raw keys verbatim; use `rig_ground_pound_onskin` |
| rig_ground_pound_onskin | baked from ironhowl/IRONHOWL_rig_ground_pound.fbx onto skin bind (armature-space match, every frame) | 0-52 | yes |
| rig_rest | ironhowl/IRONHOWL_rig_rest.fbx | 0-1 | yes |
| rig_shoulder_charge | ironhowl/IRONHOWL_rig_shoulder_charge.fbx | 0-46 | NO (bind differs, max 102.99 armature units) -> raw keys verbatim; use `rig_shoulder_charge_onskin` |
| rig_shoulder_charge_onskin | baked from ironhowl/IRONHOWL_rig_shoulder_charge.fbx onto skin bind (armature-space match, every frame) | 0-46 | yes |
| rig_walk | ironhowl/IRONHOWL_rig_walk.fbx | 0-31 | yes |

Bone names: every clip has the same bone set as the skin (no missing/extra).

## Import settings (bpy.ops.import_scene.fbx)

```
{
 "global_scale": 1.0,
 "use_anim": true,
 "automatic_bone_orientation": false,
 "ignore_leaf_bones": false,
 "use_custom_normals": true,
 "use_image_search": true,
 "anim_offset": 0.0,
 "primary_bone_axis": "Y",
 "secondary_bone_axis": "X",
 "axis_forward": "-Z",
 "axis_up": "Y"
}
```
## Export settings for Unity (bpy.ops.export_scene.fbx; select armature+mesh, assign one action)

```
{
 "use_selection": true,
 "object_types": [
  "MESH",
  "ARMATURE"
 ],
 "global_scale": 1.0,
 "apply_unit_scale": true,
 "apply_scale_options": "FBX_SCALE_NONE",
 "axis_forward": "-Z",
 "axis_up": "Y",
 "primary_bone_axis": "Y",
 "secondary_bone_axis": "X",
 "add_leaf_bones": false,
 "bake_anim": true,
 "bake_anim_use_all_actions": false,
 "bake_anim_use_nla_strips": false,
 "bake_anim_force_startend_keying": true,
 "bake_anim_simplify_factor": 0.0,
 "bake_anim_step": 1.0,
 "mesh_smooth_type": "FACE",
 "use_armature_deform_only": false,
 "path_mode": "AUTO"
}
```
## Round-trip check

Exported action `rig_ground_pound_onskin` and re-imported, compared with source `IRONHOWL_rig_ground_pound.fbx`: bone names equal=True ([33, 33]), armature scale [[0.01, 0.01, 0.01], [0.01, 0.01, 0.01]], mesh scale [[], [[100.0, 100.0, 100.0]]], verts [[], [177233]], frame range [[1, 53], [1, 53]], fps [30, 30], max world bone position diff 2.93e-06, max rotation diff 0.0 rad. PASS.

Also checked `rig_ground_pound_onskin` export vs source IRONHOWL_rig_ground_pound.fbx: 33/33 bones equal, frames 1-53 both, max world bone position diff 2.9e-06. PASS.

Extra packed images (fake user, not wired): IRONHOWL_rig_atlas.png, Ironhowl_meshy_look.jpg. Material IRONHOWL_MAT wired to ironhowl_basecolor/metallic/roughness/normal.png.
