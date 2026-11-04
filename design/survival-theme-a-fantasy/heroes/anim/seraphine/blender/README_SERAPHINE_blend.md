# SERAPHINE.blend

Built 2026-11-03 ~23:56 America/New_York, Blender 4.2.3 headless, from final/*.fbx (per-clip work blends not used). Geometry, weights and keys are the FBX data verbatim; cape spring sim NOT baked.

Base: final/SERAPHINE_rig_rest.fbx (MD5 313e22821981d2dc2f276e8641221e2a); cape: final/SERAPHINE_cape.fbx (MD5 0f0c54f5fa0b4aebc24232907e68e684).

No action assigned = rest pose. Each clip = Action with fake user + muted NLA track.

## Objects
| object | type | parent | constraints |
|---|---|---|---|
| SERAPHINE_rig | ARMATURE | None |  |
| SERAPHINE_spear | MESH | SERAPHINE_rig |  |
| SERAPHINE_wing_B | MESH | SERAPHINE_rig |  |
| SERAPHINE_body | MESH | SERAPHINE_rig |  |
| SERAPHINE_wing_A | MESH | SERAPHINE_rig |  |
| SERAPHINE_spear__attack_thrust | MESH | SERAPHINE_rig |  |
| SERAPHINE_cape_rig | ARMATURE | None | follow_Spine2 |
| SERAPHINE_cape | MESH | SERAPHINE_cape_rig |  |

Cape rig is a separate armature (as in final/) with an object Child Of constraint `follow_Spine2` (inverse = Spine2 rest), so it follows the body in Blender and is at identity at rest; Unity attach per final/CAPE_ATTACH.md.

Variant meshes (hidden; `__<action>` suffix) exist where a clip FBX ships different prop geometry; swap them in when exporting that action.

## Actions (30 fps)
| action | source | MD5 | frames | variant meshes | re-export check |
|---|---|---|---|---|---|
| attack_throw | final/SERAPHINE_attack_throw.fbx | a2119e9cf535e6d6182ca74c36181306 | 1-67 |  | PASS: bones 33 equal=True, frames [1, 67], max pose diff 1.73e-05, max vert diff 3.0e-07, max weight diff 0 |
| attack_thrust | final/SERAPHINE_attack_thrust.fbx | 4ad38d740a0ab1682f49ca7643a21843 | 1-72 | SERAPHINE_spear__attack_thrust | PASS: bones 33 equal=True, frames [1, 72], max pose diff 1.93e-05, max vert diff 4.8e-07, max weight diff 0 |
| idle | final/SERAPHINE_idle.fbx | 3803a68c5d582aa484e391c5dca72030 | 1-299 |  | PASS: bones 33 equal=True, frames [1, 299], max pose diff 1.69e-05, max vert diff 3.0e-07, max weight diff 0 |
| walk | final/SERAPHINE_walk.fbx | cf3a49a65d4c28c7d8f8fa183a784fd5 | 1-36 |  | PASS: bones 33 equal=True, frames [1, 36], max pose diff 2.06e-05, max vert diff 3.0e-07, max weight diff 0 |

Cape re-export vs final/SERAPHINE_cape.fbx: PASS (10 bones equal=True, rest diff 2.4e-07, vert diff 1.8e-07, weight diff 0).

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