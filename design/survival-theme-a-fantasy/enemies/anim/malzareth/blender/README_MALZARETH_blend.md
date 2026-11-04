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
## 2026-11-04 rebuild on Derek's 20-bone skeleton (Blender 5.2.2)
Derek's edited skeleton is kept exactly as he made it (no bones added, renamed or moved): mixamorig Hips, Spine, Spine1, Spine2, Neck, Head, Left/RightShoulder, Left/RightArm, Left/RightForeArm, LeftHand, RightForeArm.001, Left/RightUpLeg, Left/RightLeg, Left/RightFoot.
Deleted vs the old 33-bone rig: RightHand (its job is now RightForeArm.001), all HandIndex finger bones, Left/RightToeBase, Toe_End, HeadTop_End.

- Weights: arms and legs reprojected along each chain. Left fingers go to LeftHand, the right hand and fingers to RightForeArm.001, toes to Foot. The greatsword is 100% on RightForeArm.001. Body weight sums are 1.0. No vertex group names a missing bone.
- Motion: idle 1-299, walk 1-36, attack_greatsword 1-56 re-keyed from the old clips by world-space bone deltas, with two-bone IK to the old wrist and ankle targets. The feet are fit to the old foot and toe verts (Kabsch), then floor-clamped to z >= 0.05 cm. The greatsword matches the old world pose exactly.
- Clearance fixes (eased over at least +-3 frames, rotations spread across the chain): the attack's left support hand is moved off the blade. Its fingers have no bones now, so they can't curl round the grip. The greatsword is also kept clear of the right forearm (attack f32-33) and of the cape and right thigh (walk f19-29).
- The cape is unchanged and still follows Spine2 (`follow_Spine2`). The cape spring sim is still NOT baked.
- Layout: three fake-user actions on muted NLA tracks, no active action. The file opens in rest pose in OBJECT mode.
- Scripts: /workspace/rigjob/msk/ (m_build, m_lhfix, m_weapfix, m_merge, m_export).
