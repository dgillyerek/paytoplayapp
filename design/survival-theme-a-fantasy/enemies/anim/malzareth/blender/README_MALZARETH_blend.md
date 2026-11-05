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

## Polish pass (2026-11-04)
- Foot plant (Vorgath method, m_plant.py): contacts locked + 2-bone IK re-solve. Per-contact real-mesh slip: walk L 2.44->0.00 cm, R 2.40->0.04; idle 10.4/10.6->0.12/0.03; attack 52.9/49.1->0.41/0.00 (attack feet held planted whole clip; large leg rotation deltas, RightLeg up to 43 deg). Floor min z 0.05-0.06 cm.
- Walk arms eased forward/outward (m_armfix.py, <=9.4 deg L / 11.1 deg R): cape-into-body/arm 7.37 cm -> 0.60 cm.
- Sword clearance: attack body 1.79 cm, walk 7.22 / cape 8.03 cm, idle 6.61 / 17.9 cm. Attack left hand to hilt unchanged at 2.58 cm (optional fix skipped).
- Cape FBX unchanged.

## FIX2 (2026-11-04, Blender 5.2.2)
Skeleton unchanged: same 20 bones, names, parents, heads, tails and rolls. The bone-table hash matches the pre-FIX2 blend.
Four fake-user actions sit on muted NLA tracks with no active action, so the file opens in rest pose.

- **Cape (snugger):**
  - Rest refit with capefit.py: shoulder hotspots, a 2.4 cm minimum rest gap, and a per-vertex push on the 22 vertices that came within 1.3 cm of the body in any clip's sim.
  - Upper-cape local-min gap p5/50/95 is now 2.4/2.5/5.2 cm (was 2.18/5.7/7.53).
  - Spring roots moved about 3-5 cm toward the body.
  - Cape sim in the preview: torso hit radius 0, COLCLAMP off.
- **attack_greatsword (knee):**
  - Right-knee twist is spread over at least 3 frames.
  - Max change per frame: RightLeg 43.1 -> 27.1 deg/f, RightUpLeg 30.0 -> 20.1 deg/f.
  - Right-foot contact slip 0.80 -> 0.48 cm/f.
  - The right foot steps out and plants on f16-34 by design.
- **attack2_overhead_cleave (new, 1-69, one-handed):**
  - Same overhead wind-up as before.
  - The blade then comes down and out past the right hip and thigh. At f30-36 the tip is about 1.0 m to his right, 1.5 m in front and 0.25-0.3 m off the floor. It never crosses the torso.
  - Returns to the ready stance by f69.
  - Built by keying the hand path and solving the elbow analytically (restrike2.py), with the spine bend toward the stance scaled to 0.55. The left arm is eased out of the cape on f13-31.
  - Feet stay planted (0 slip, 0 drift). Max leg change is 10.9 deg/f.
- **Verified on the re-imported FBXs, every frame, full-res body:** see the table in CAPE_ATTACH.md (FIX2 section).
- Scripts are in /workspace/rigjob/mf2/: knee2, capefit, f_capeanim, restrike2, nudge, merge2, export2, vfy.
