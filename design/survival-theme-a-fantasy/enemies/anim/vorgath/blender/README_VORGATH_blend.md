# VORGATH.blend

Built 2026-11-03 ~23:56 America/New_York, Blender 4.2.3 headless, from final/*.fbx (per-clip work blends not used). Geometry, weights and keys are the FBX data verbatim; cape spring sim NOT baked.

Base: final/VORGATH_rig_rest.fbx (MD5 03e07b80b4c9a5335954e75e4f497556); no cape asset for this character.

No action assigned = rest pose. Each clip = Action with fake user + muted NLA track.

## Objects
| object | type | parent | constraints |
|---|---|---|---|
| VORGATH_rig | ARMATURE | None |  |
| VORGATH_body | MESH | VORGATH_rig |  |


Variant meshes (hidden; `__<action>` suffix) exist where a clip FBX ships different prop geometry; swap them in when exporting that action.

## Actions (30 fps)
| action | source | MD5 | frames | variant meshes | re-export check |
|---|---|---|---|---|---|
| attack_slam | final/VORGATH_attack_slam.fbx | 3fd49c90fd30f716e6877ad61762059e | 1-112 |  | PASS: bones 33 equal=True, frames [1, 112], max pose diff 1.01e-05, max vert diff 2.8e-07, max weight diff 0 |
| attack_spit | final/VORGATH_attack_spit.fbx | 5de841d97fe00f261f0880a2d2a97e39 | 1-163 |  | PASS: bones 33 equal=True, frames [1, 163], max pose diff 9.02e-06, max vert diff 2.8e-07, max weight diff 0 |
| idle | final/VORGATH_idle.fbx | c789efe2a62acd3f587a6ec4d28cf805 | 1-160 |  | PASS: bones 33 equal=True, frames [1, 160], max pose diff 9.36e-06, max vert diff 2.8e-07, max weight diff 0 |
| walk | final/VORGATH_walk.fbx | a64fcf4fc15415dd68c31a9f2e856949 | 1-44 |  | PASS: bones 33 equal=True, frames [1, 44], max pose diff 9.16e-06, max vert diff 2.8e-07, max weight diff 0 |

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