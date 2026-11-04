# KAELEN.blend

## 2026-11-04 rebuild on Derek's 20-bone skeleton (Blender 5.2.2)
This rebuild uses Derek's skeleton from commit 19ebaba exactly as he made it, with no bones added back.
- **Bones:** Hips, Spine, Spine1, Spine2, Neck, Head, Left/RightShoulder, Left/RightArm, Arm.001, Arm.002, Left/RightUpLeg, Left/RightLeg and Left/RightFoot, all with the `mixamorig:` prefix.
- **Body weights:** re-projected onto the new chains. ForeArm became Arm.001, Hand and the fingers became Arm.002, and ToeBase/Toe_End became Foot. Smoothstep blends are 5 cm at the elbow and knee and 3 cm at the wrist and ankle. No vertex group points to a missing bone.
- **Swords:** KAELEN_sword_L is 100% on LeftArm.002 and KAELEN_sword_R is 100% on RightArm.002.
- **Clips:** re-keyed from the old 33-bone poses.
  - Arms use 2-bone IK to the old wrist, and Arm.002 takes the old Hand rotation.
  - Legs use 2-bone IK to the old ankle. The rigid Foot pitches about the ankle to stay above the floor, and gets 1 cm toe clearance in mid-swing on walk.
  - Small Arm.002 quaternion keys were added on some walk and dual-slash frames to keep both swords off the body and the cape.
- **Cape:** the cape rig is unchanged and still uses Child Of `follow_Spine2`.
- **FBX MD5s:** KAELEN_rig_rest.fbx: 953166e6172f3962ca6da36906dd055b; KAELEN_idle.fbx: bf55016d29f800ee188934a70ddba430; KAELEN_walk.fbx: b74f1458503880be57cc3df60b68f3ee; KAELEN_attack_dualslash.fbx: aadcbec22fe8be115159a151a6d70db7. KAELEN_cape.fbx was not re-exported.
- Sections below this one describe the 2026-11-03 build. Where they differ, the notes above win: the actions are now 20-bone, and the frame ranges are unchanged (attack_dualslash 1-110, idle 1-299, walk 1-36).

Built 2026-11-03 ~23:56 America/New_York, Blender 4.2.3 headless, from final/*.fbx (per-clip work blends not used). Geometry, weights and keys are the FBX data verbatim; cape spring sim NOT baked.

Base: final/KAELEN_rig_rest.fbx (MD5 24c10dc0150dfa7a12c9efea9014df79); cape: final/KAELEN_cape.fbx (MD5 a58a559ab972a1588aaf58f87b5bcbf1).

No action assigned = rest pose. Each clip = Action with fake user + muted NLA track.

## Objects
| object | type | parent | constraints |
|---|---|---|---|
| KAELEN_rig | ARMATURE | None |  |
| KAELEN_sword_R | MESH | KAELEN_rig |  |
| KAELEN_body | MESH | KAELEN_rig |  |
| KAELEN_sword_L | MESH | KAELEN_rig |  |
| KAELEN_cape_rig | ARMATURE | None | follow_Spine2 |
| KAELEN_cape | MESH | KAELEN_cape_rig |  |

Cape rig is a separate armature (as in final/) with an object Child Of constraint `follow_Spine2` (inverse = Spine2 rest), so it follows the body in Blender and is at identity at rest; Unity attach per final/CAPE_ATTACH.md.

Variant meshes (hidden; `__<action>` suffix) exist where a clip FBX ships different prop geometry; swap them in when exporting that action.

## Actions (30 fps)
| action | source | MD5 | frames | variant meshes | re-export check |
|---|---|---|---|---|---|
| attack_dualslash | final/KAELEN_attack_dualslash.fbx | de5ff76d96ac3bcf332fc947a706a3e1 | 1-110 |  | PASS: bones 33 equal=True, frames [1, 110], max pose diff 1.04e-05, max vert diff 2.3e-07, max weight diff 0 |
| idle | final/KAELEN_idle.fbx | a4f86d7c9351efdee1b8fb289b4ebe0d | 1-299 |  | PASS: bones 33 equal=True, frames [1, 299], max pose diff 8.23e-06, max vert diff 2.3e-07, max weight diff 0 |
| walk | final/KAELEN_walk.fbx | d8dd5ea0c15ce3d76bf44463d317474b | 1-36 |  | PASS: bones 33 equal=True, frames [1, 36], max pose diff 9.00e-06, max vert diff 2.3e-07, max weight diff 0 |

Cape re-export vs final/KAELEN_cape.fbx: PASS (10 bones equal=True, rest diff 7.7e-07, vert diff 1.6e-07, weight diff 0).

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