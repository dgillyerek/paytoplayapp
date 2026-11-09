# SERAPHINE.blend

Rebuilt on 2026-11-04 (America/New_York) in Blender 5.2.2 (file format 5.2) on Derek's 24-bone skeleton, branch seraphine-derek-edit @0af9324. Derek's bones are unchanged and nothing was added back.

Bones (24, all `mixamorig:`): Hips, Spine, Spine1, Spine2, Neck, Head, HeadTop_End, Left/Right Shoulder, Arm, ForeArm, Hand, LeftUpLeg + LeftUpLeg.001/.002/.003, LeftToe_End (parented to LeftUpLeg; it carries no weights), RightUpLeg + RightUpLeg.001/.002/.003.

Opens in OBJECT mode at rest pose, with no active action. Each clip is an action with a fake user and a muted NLA track.

## Weights
- Body legs: the old leg weights on each side were redistributed along UpLeg→.001→.002→.003 by the closest segment. Joints are smoothstepped (knee ±5 cm, ankle ±3 cm, ball ±1.2 cm). Verts more than 1 cm below the ankle are limited to .002 and .003.
- Finger groups were merged into the Hand bones. No vertex group points to a missing bone.
- The spear and its variant are weighted 100% to RightHand. The wings are weighted to Spine2. The cape is a separate rig, `SERAPHINE_cape_rig`, with a Child Of `follow_Spine2` constraint, unchanged.

## Actions (30 fps)
| action | frames | variant mesh |
|---|---|---|
| idle | 1-299 | |
| walk | 1-36 | |
| attack_thrust | 1-72 | SERAPHINE_spear__attack_thrust (swap it in as SERAPHINE_spear on export) |
| attack_throw | 1-67 | |

Retarget: the upper body uses the rest-relative world transform from the old 33-bone clips. Legs use analytic 2-bone IK: the old ankle path drives the new ankle, and the old knee is the pole. Foot and toe rotations come from the old Foot and ToeBase. A floor pass lifts the ankle so no foot vert goes below z=0.
The spear arm keys on thrust and throw were corrected so the weapon has no crossings (full-res check, weapfix5).

## Exports (repo `Assets/ThemePack/fantasy_kingdom_a/art/preview_new_chars/seraphine/`)
- SERAPHINE_rig_rest.fbx: b7f82e5d0a5da49f45a4b3f7e2782173
- SERAPHINE_idle.fbx: 456f46997ce91e91e2f9770a20bc444a
- SERAPHINE_walk.fbx: a5c327b19dbbc8fe3582296e4fc5d0c2
- SERAPHINE_attack_thrust.fbx: c082d9b0bd8658f4932b244b066dc599
- SERAPHINE_attack_throw.fbx: f4f9e6177b8e986ce2728a4da1d2238e
- SERAPHINE_cape.fbx: 0f0c54f5fa0b4aebc24232907e68e684

The cape FBX was not re-exported.

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