# MALZARETH.blend

Built 2026-11-03 ~23:56 America/New_York, Blender 4.2.3 headless, from final/*.fbx (per-clip work blends not used). Geometry, weights and keys are the FBX data verbatim; cape spring sim NOT baked.

Base: final/MALZARETH_rig_rest.fbx (MD5 cc9addcffea22eca825ae7521f316c43); cape: final/MALZARETH_cape.fbx (MD5 fb523988d9ca1a50a74661528d500d97).

No action assigned = rest pose. Each clip = Action with fake user + muted NLA track.

## Objects
| object | type | parent | constraints |
|---|---|---|---|
| MALZARETH_rig | ARMATURE | None |  |
| MALZARETH_greatsword | MESH | MALZARETH_rig |  |
| MALZARETH_body | MESH | MALZARETH_rig |  |
| MALZARETH_cape_rig | ARMATURE | None | follow_Spine2 |
| MALZARETH_cape | MESH | MALZARETH_cape_rig |  |

Cape rig is a separate armature (as in final/) with an object Child Of constraint `follow_Spine2` (inverse = Spine2 rest), so it follows the body in Blender and is at identity at rest; Unity attach per final/CAPE_ATTACH.md.

Variant meshes (hidden; `__<action>` suffix) exist where a clip FBX ships different prop geometry; swap them in when exporting that action.

## Actions (30 fps)
| action | source | MD5 | frames | variant meshes | re-export check |
|---|---|---|---|---|---|
| attack_greatsword | final/MALZARETH_attack_greatsword.fbx | e50c5085568c4910d5dd8f2a7aaabab4 | 1-56 |  | PASS: bones 33 equal=True, frames [1, 56], max pose diff 1.73e-05, max vert diff 3.6e-07, max weight diff 0 |
| idle | final/MALZARETH_idle.fbx | dc3659e3d3e6242bed4ab78a58f41ff4 | 1-299 |  | FAIL: bones 33 equal=True, frames [1, 299], max pose diff 1.18e-04, max vert diff 3.6e-07, max weight diff 0 |
| walk | final/MALZARETH_walk.fbx | 20cff1af533e45a810fcb66331d9982e | 1-36 |  | PASS: bones 33 equal=True, frames [1, 36], max pose diff 1.70e-05, max vert diff 3.6e-07, max weight diff 0 |

Cape re-export vs final/MALZARETH_cape.fbx: PASS (10 bones equal=True, rest diff 5.4e-07, vert diff 1.8e-07, weight diff 0).

## Import settings
```
{
 "global_scale": 1.0,
 "use_anim": true,
 "anim_offset": 0.0,
 "automatic_bone_orientation": false,
 "ignore_leaf_bones": false,
 "use_custom_normals": true,
 "use_image_search": true,
 "primary_bone_axis": "Y",
 "secondary_bone_axis": "X",
 "axis_forward": "-Z",
 "axis_up": "Y"
}
```
## Export settings (select armature + its non-variant mesh children, assign one action, scene range = action range, bake_anim=True; cape: bake_anim=False)
```
{
 "use_selection": true,
 "object_types": [
  "ARMATURE",
  "MESH"
 ],
 "global_scale": 1.0,
 "apply_unit_scale": true,
 "apply_scale_options": "FBX_SCALE_NONE",
 "axis_forward": "-Z",
 "axis_up": "Y",
 "primary_bone_axis": "Y",
 "secondary_bone_axis": "X",
 "add_leaf_bones": false,
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