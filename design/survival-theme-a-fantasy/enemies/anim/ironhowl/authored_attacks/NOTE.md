# Ironhowl authored attacks (2026-10-24)

Derek: "iron howl needs more attack motions. he just punches." Three new moves, keyposed by hand on Ironhowl's own
skeleton. They are not Mixamo downloads and not Blightroot's mutant moves.

| pose | file | frames | contact | what it looks like |
|---|---|---|---|---|
| attack4 | `IRONHOWL_rig_ground_pound.fbx` | 0-52 | 22 (both fists hit the ground, 0.62 m ahead) | squares up, clasps both fists high over his head (13-17), folds forward and hammers them into the ground (22), holds the crouch (to 32), rises back to the stance |
| attack5 | `IRONHOWL_rig_backhand_sweep.fbx` | 0-40 | 15 (left hand, arm out flat at his side) | winds the torso to his right with the lead (left) arm coiled across his chest (9-12), whips it out flat to his left (15), follows through behind him (19-24), back to the stance |
| attack6 | `IRONHOWL_rig_shoulder_charge.fbx` | 0-46 | 20 (left shoulder, hips 0.62 m ahead) | sinks back (10), drives two steps forward with both arms tucked and the lead shoulder low in front (16-24), backs up to the stance |

Battle: ground pound = StompQuake (crack ring, dust ring, debris) with a 0.08 m shake; backhand and charge = IronClaw
(claw sparks, dust, debris). The feet are pinned in the clips, so the battle plays their hips travel as authored (not in place).

## How they were made (body, rig and weights untouched)
- Start and end pose: frame 0 of `IRONHOWL_mixamo_standing_melee_attack_downward.fbx`, so they meet the other attacks.
- `tools/specs.py` holds the keyposes (world-axis rotations on top of the stance, arm two-bone IK, foot IK targets).
  `tools/author.py` poses `IRONHOWL_rig.fbx` in Blender 4.2.3, keys every frame and exports it.
- Blender's export adds an `Armature` root node, which stops a clip binding (see IronhowlRestBindingTests).
  `tools/retarget.py` writes the exported motion into a copy of the overhead smash FBX instead: same PreRotations,
  `mixamo.com` take, `mixamorig:Hips` root, same animated curves. Local transforms match the export to 1e-5.
- Previews (`tools/render_new.py` with `UP=1`) drive the body the way Unity does (each node's T * Pre * R from the file),
  because Blender's own import of a Mixamo clip onto another armature does not match Unity.
- `tools/fbxlib` (not committed) is Blender 4.2.3's `io_scene_fbx` parse_fbx / encode_bin / data_types, copied.
- Not yet checked in Unity.
