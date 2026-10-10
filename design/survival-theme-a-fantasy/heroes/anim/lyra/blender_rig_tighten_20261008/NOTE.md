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

## Walk v2 (2026-10-08, Derek: "walking is very stiff, arms wave out to the side")
`LYRA_tighten_walk` was re-authored on Derek's rig with Blender 5.2.2. His bones are unchanged; the script is `work/scripts/walk_v2_author.py`. It is an in-place loop, f0–f30 @30fps with f30 = f0. Foot timing is unchanged: right contact f8, left contact f23.
- **Legs:** two-bone IK to planted feet. Heel strike at −12°, then flat, then the heel rises to 24° at toe-off. The pivot sits on the mesh sole at the heel and toe points.
  - The toes stay rigid with the foot: no keys bend around the raised ball joints.
  - Knee bend comes from the hip drop.
  - The sole sits at +3 mm in stance, fixing the old dips of −3.2 cm (left) and −1.9 cm (right).
- **Body:**
  - Pelvis yaw ±5° (that hip goes forward with its leg).
  - Pelvis list ±2.5° (the swing-side hip drops).
  - 1.6 cm shift over the stance leg.
  - Bob ±1.1 cm, low at contact and high at passing.
  - Chest counter-rotation ±4.5°, forward lean up to 3°.
  - The head is held at its rest orientation.
- **Left arm:**
  - Swings in the sagittal plane: 22° forward, 16° back.
  - Abduction is fixed at 11° (12° in the rest pose); the hand stays outside the hip mesh.
  - Elbow bend goes from 12° at the back of the swing to 32° at the front.
- **Right arm (staff):**
  - IK to a grip that keeps the staff near vertical, swinging ±4°, with the staff foot at least 3 cm off the ground.
  - Upper arm abduction is 18.5–21.8°; the rest pose is 28°.
  - Forward/back upper-arm swing is only −1.5° to +2.8°.
- **Upper-arm abduction (degrees out of the sagittal plane):**

  | Arm | Before | After |
  |---|---|---|
  | Left | −5.6 to 38.5 | 11.0 to 11.0 |
  | Right | 2.8 to 50.7 | 18.5 to 21.8 |

- Only `LYRA_tighten_blenderig_walk.fbx` was re-exported: ff4786e2 → f66a08b9. Rest and attack are untouched.

## Derek's second rig edit + rebuild (2026-10-08 22:45, e8b050d → this commit)
Derek edited the combined blend on the laptop in Blender 5.2 and saved it in Edit Mode; it has been re-saved in Object Mode. His bones were kept exactly: every head, tail, roll and parent is byte-for-byte his (checked after the rebuild).

**Note:** his saved file was built on the 8:11 PM version (e92a644), not on a995489. So it carried the old 10-05 walk, the old staff offset and the old bolt keys. Those were not hand edits, so they were replaced by the current versions below. Rest and attack keys, mesh, weights, materials and textures in his file were unchanged.

**What he changed (bone rest positions, Edit Mode):**
- **Both toe bones (`LeftToeBase`, `RightToeBase`) deleted.** The rig is now 20 bones and each Foot bone runs from the ankle to the tip of the toe on the ground.
- **Knees** raised about 10 cm. L: z 0.49 → 0.60 m. R: z 0.49 → 0.59 m.
- **Ankles** lowered about 8 cm. L: z 0.21 → 0.13 m. R: z 0.21 → 0.12 m.
- **Hips** joint moved 4.8 cm back and 2 cm to her right, and 1.3 cm down. The right hip socket moved 1.4 cm.
- **Head** bone shortened: its tip came down 6.4 cm. The neck/head joint did not move.
- **Left arm:** elbow up 5.2 cm and back 2.9 cm; wrist 3.2 cm lower and 1.6 cm further out. The forearm is now 27 cm (was 18 cm).
- **Right hand:** wrist moved 0.6 cm. The hand bone tip moved 1.7 cm, which re-rolled the hand.

**What was rebuilt (`work/scripts/rebuild_e8b050d.py`, Blender 5.2.2):**
- **Toe weights:** the 744 (L) and 1,060 (R) vertices weighted to the deleted toe bones were added onto their Foot bone, and the two empty toe groups were removed. Without this, 51 toe vertices had no bone at all and would stay behind when the foot moved. The 36 leftover toe curves in the rest and attack actions were removed.
- **Walk:** walk v2 (`work/scripts/walk_v2_author.py`, now also works without toe bones) was re-authored on his bones. The timing, arm swing and heel-toe roll are the same. The soles sit at +3 mm in stance with no dips. Arm abduction is L 11°, R 17.8–21.0°.
- **Attack:** retargeted from a995489's attack using world-space rotation from rest for every bone. Hips and feet stay still as before.
  - Compared with a995489, joint paths are within 0.9 cm for the head, shoulders, staff arm and wrist (1.6 cm).
  - The left hand travels up to 12.5 cm differently because Derek's left forearm is 9 cm longer. Re-using the old keys as-is would have been worse everywhere else.
- **Staff:** the one shared staff stays at Design's `rest_world` in the fist. Its hand offset was re-derived for the re-rolled RightHand (`props/LYRA_staff_meta.json`, `attack_meta.json`). The staff mesh is unchanged, so both staff FBXs are unchanged.
- **Bolt:** re-keyed at the f12 crystal. Spawn is (-0.154, -0.660, 1.912); it was (-0.168, -0.654, 1.910).
- **Unity:**
  - `LyraAttack.cs`: staff and bolt frames, and the Hips, LeftHand and RightHand calibration anchors, updated.
  - `LyraMotion.BoneNames` is now 20 bones; the toes are gone.
- **FBX MD5s:**
  - Rest 59768294 → e4898881.
  - Walk f66a08b9 → 94d6cc78.
  - Attack 64d8b8d4 → 53fd21bb.

**Known leftover:** the skin weights still bend at the old knee and ankle heights (z ≈ 0.50 m and 0.20 m), while the joints now pivot at 0.60 m and 0.13 m. The renders show no visible creasing in the walk, but watch the knees and boot tops.

## 2026-10-09: walk v3, feet and knees forward, narrow stance, head motion (walk only)

Derek, Game view on desktop: "the walk looks like her legs are facing to the side. can you rotate the left foot inward and move the hips and legs closer together. also her head should move slightly while walking."

- **Script:** `work/scripts/walk_v3_author.py` (v2 plus the fixes below), run by `work/scripts/walk_feet_20261009.py`, which re-authors only `LYRA_tighten_walk`, checks that bones, rest/attack/bolt keys and the staff are unchanged, saves in Object Mode with the walk active, and exports only the walk FBX.
- **Feet:** the mesh's right boot points about 42° outward and the left about 4°. v2 kept those angles and also aimed each knee along its foot, so the right knee pointed about 33° outward and the left about 10–15° outward. Both feet are now turned in to a near-straight 2–3° toe-out (right turned in about 40°, left about 1.5°), and both knees point forward (within about 2°) through the whole cycle.
- **Stance:** the sole centres now land 16 cm apart, about 8 cm either side of the centre line; in v2 they were about 57 cm apart. The narrowest gap is about 4 cm between the boots and 2.5 cm between the thighs at passing, so there is no crossing or knee contact. Hip side-shift was cut from 1.6 to 1.1 cm and the hip drop from 2.5° to 2.0°.
- **Head:** the head was locked to its rest angle. It now turns about ±2.5° with the chest's counter-rotation, nods about ±1.4° just after each heel strike, and tilts about ±0.9° with the weight shift, so the eyes stay level. The vertical bob (about 2 cm) comes from the body as before.
- **Unchanged:** timing, heel-to-toe roll, arm swing, the staff grip, and soles at +3 mm in stance with no dips. Derek's bones, the rest and attack FBXs, both staff FBXs, and the rest/attack/bolt actions are all byte-identical.
- **FBX MD5:** walk 94d6cc78 → 7c5b1577.
- **Watch in Unity:** the right ankle and boot top twist by about 40° to straighten that foot.

## 2026-10-09: attack staff clear of body, natural wrist (attack only)
Derek: the staff pushed through her body in the attack and the wrist bent unnaturally. Only the attack's RightArm, RightForeArm and RightHand keys changed, plus the bolt location keys. The staff no longer enters the body: it was up to 8.5 cm into the right thigh and 6.8 cm into the shin, and now has at least 3.7 cm clearance. The staff wrist went from 119° to at most 25° from the rest grip. Bones, the rest and walk actions, the staff offset, the rest and walk FBXs and both staff FBXs are unchanged. Saved in Object Mode with the walk active. Attack FBX 53fd21bb → 835b0502. Details are in `../attack_20261008/NOTE.md`.

## 2026-10-09: attack re-authored as a two-handed side-on cast (attack only)
Derek: the shoulder pulled out too much. The attack is now a two-handed cast: 52° hip turn on planted balls of the feet, left hand on the shaft f8–f20, body-driven thrust at f12, recovering to rest at f30. Right armpit p99 stretch went from 2.66 to 1.50, and the wrists are at most 23°. No staff penetration. Attack and bolt actions changed. Bones, the rest and walk actions, the staff offset, the rest and walk FBXs and both staff FBXs are unchanged. Saved in Object Mode with the walk active. Attack FBX 835b0502 → 2d6f799e. Details are in `../attack_20261008/NOTE.md`.
