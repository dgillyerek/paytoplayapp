# LYRA fitted (tighten): Blender humanoid rig pack (Design, 2026-10-08)

HOLD: staging only. Nothing committed, pushed or PR'd. Derek must check it in Game-view before any merge.

## Source
- Meshy GLB: `heroes/anim/lyra/meshy/tighten_20261008/Lyra_tighten_20261008_texture.glb`, MD5 `60ed0a89cda4284cdf433b5996d5382b`. 2.0M faces; the staff was fused to the right hand.
- Look SoT: `heroes/anim/lyra/LOOK_SOT/tighten_20261007/02_human_mage_lyra_FULLBODY_tighten.png`, MD5 `0887414eb31f8a0882e33a42bc2f7454`.

## Contents
- `LYRA_tighten_blenderig.blend`: rig SoT. Objects `LYRA_tighten_rig` (22 mixamorig bones) and `LYRA_tighten_body` (200,000 faces / 99,521 verts). Walk action `LYRA_tighten_walk` is stored with a fake user.
- `LYRA_tighten_blenderig.fbx`: rest pose. `LYRA_tighten_blenderig_walk.fbx`: walk, f0–f30 @30fps, f30 = f0. Both use the stage_export settings.
- `textures/`: Meshy atlas as 2048² PNG files (basecolor, metal_rough, normal).
- `props/LYRA_staff.fbx` + `LYRA_staff_props.blend` + `LYRA_staff_meta.json`: separate staff prop (14,048 faces).
  - Origin is at the grip, +Z runs from the foot to the crystal.
  - It is parented to `mixamorig:RightHand` with the offset in the meta file.
- `LYRA_tighten_rest_compare_lookSoT.png`, `LYRA_tighten_walk_sheet.jpg`, `LYRA_tighten_walk_preview.mp4`: QC.
- `work/`: joints.json, bone_overlay.png, batch.log, verify_rest_walk.json, scripts/, qc_frames/.

## What was done
- **Staff split**: same method as Rowan (region growth from staff seeds, geodesic contact resolution).
  - The two fist holes were patched with flat single-texel caps; no blur fill.
  - A generated cylinder (r 2.4 cm, one staff texel) bridges the 6.5 cm gap inside the fist. It is hidden by the fist.
- **Recentring**: the body was recentred on x = 0 (it was ~18 cm off) because the pipeline's weight cleanup assumes the midline is x = 0.
- **Joints**: the shared analysis put face_forward at +1, collapsed the arm chains and placed the neck too high. The wrapper `work/hum_analyze.py` overrides arms / neck / head and face_forward = −1. See bone_overlay.png.
- **Walk**: 10-05 `author_humanoid_walk()` from identity rest, loaded verbatim.

## Reimport QC (`work/verify_rest_walk.json`)
- Rest and walk: 22 bones each, same names and order. Frames 1–31.
- **Hips at the first frame vs rest FBX: 0.0° and 0.0 m. Not flipped.**
- Loop 0.0° and 0.0 m. Foot-Y correlation −0.998. Toes point −Y.

## Known leftovers
- Flat caps where the staff left the fist (staff-facing palm and thumb side). They are mostly covered while the staff is held.
- Walk: the 10-05 arm swing (±28°) is applied to the staff arm, so the 1.9 m staff swings like a pendulum (about ±30° at the foot). If this reads badly in Game-view, the fix is to reduce RightArm swing for the staff hand. That would be a deviation from the 10-05 method, so it was not done without approval.

## One blend (2026-10-08)
`LYRA_tighten_blenderig.blend` is now the only Lyra fitted blend. It holds the rig, body, staff and bolt, plus four actions:
- `LYRA_tighten_walk`: the armature's active action (edit this one).
- `LYRA_tighten_rest`: identity bind pose at f0, kept on a muted NLA track named `rest`.
- `LYRA_tighten_attack`: kept on a muted NLA track named `attack`. To preview it, mute the walk or set it as the active action.
- `LYRA_arcane_bolt_attack`: on `LYRA_arcane_bolt_vfx`. It appears at f12 and travels −Y at 9 m/s. The `LYRA_attack_vfx` collection is hidden by default.

Pose bones are XYZ euler (the walk's curves need that). The attack was converted from quaternion keys at f0/8/12/19/30 to per-frame linear euler keys (f0–f30). Its per-frame pose matches the old attack work file (max 4.5e-7). `LYRA_staff` is bone-parented to `mixamorig:RightHand` with the Design offset in all clips. Textures are packed.

The old attack work and props blends, `props/LYRA_staff_props.blend` and `work/dec.blend` were removed. `dec.blend` was scratch: the decimated, unrigged 200k-face body, with vertices identical to `LYRA_tighten_body`. No FBX was re-exported.

## Derek's rig edit + re-export (2026-10-08, e92a644 → this commit)
Derek edited the combined blend in Blender 5.2. He saved it in Edit Mode; it has been re-saved in Object Mode. His change was to the bone rest positions only (Edit Mode). No keys, mesh, weights or hierarchy changed.
- **Right arm:** elbow (RightArm tail / RightForeArm head) moved 7.1 cm, from (-31.5, -1.0, 123.5) to (-28.8, 4.3, 127.4) cm. Wrist (RightForeArm tail / RightHand head) moved 4.1 cm, from (-45.0, -7.0, 126.5) to (-49.1, -7.4, 126.5) cm. The hand tip and shoulder did not move.
- **Feet:** both ball-of-foot joints (Foot tail / ToeBase head) moved up about 8 cm and back:
  - L (27.9, -13.7, 3.8) → (29.0, -6.9, 12.4) cm.
  - R (-23.8, -1.7, 3.8) → (-18.5, 3.5, 11.1) cm.
  - Toe tips moved: L 6.4 cm, R 3.8 cm. Ankles did not move.
- Rolls changed only as a side effect of those moves.
- Rest, walk and attack were re-exported from his blend in Blender 5.2.2 with the same settings. A parity check of 5.2 against 4.3.2 on the pre-edit blend matched the shipped FBXs (bone transforms 0 / 0 / 1.6e-6).
  - `LYRA_tighten_blenderig.fbx`: c8d41802 → 59768294.
  - `LYRA_tighten_blenderig_walk.fbx`: e546a82b → ff4786e2.
  - `../attack_20261008/LYRA_tighten_blenderig_attack.fbx`: 90385eda → 64d8b8d4.
  - Staff FBXs unchanged (mesh identical).
- **Staff:** it still sits at Design's `rest_world` in the fist. Its hand offset was re-derived for the new RightHand: `props/LYRA_staff_meta.json` and `attack_meta.json`.
- **Bolt:** the crystal at f12 moved, so the bolt was re-keyed. The spawn is now (-0.168, -0.654, 1.910); it was (-0.342, -0.646, 1.862). Unity's `LyraAttack.cs` staff and bolt frames and its RightHand calibration anchor were updated to match.
