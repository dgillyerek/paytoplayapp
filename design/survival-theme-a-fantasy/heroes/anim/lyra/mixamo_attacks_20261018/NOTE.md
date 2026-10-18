# Lyra attacks 2 and 3 (Mixamo retarget, 2026-10-18). HOLD: Derek Game-view check before any merge.

Sources (raw downloads kept here, md5s in CHECKSUMS.md5). Mixamo auto-rigged LYRA.fbx (no marker/LOD options); no In Place offered.
- `LYRA_mixamo_standing_melee_attack_horizontal.fbx`: "Standing Melee Attack Horizontal", 73 frames.
- `LYRA_mixamo_standing_2h_magic_area_attack_01.fbx`: "Standing 2H Magic Area Attack 01", 90 frames.

Both use the same 20 mixamorig names as LYRA_tighten_rig.

## Bakes
- `LYRA_blenderig_attack2_staff_sweep.fbx`: 95 frames (0–94), contact f40 (the crystal's fastest pass across her front).
- `LYRA_blenderig_attack3_arcane_nova.fbx`: 112 frames (0–111), contact f51 (both hands at their lowest on the drive-down).
- Built from `heroes/anim/lyra/blender_rig_tighten_20261008/LYRA_tighten_blenderig.blend`, which was opened and never saved.
- Exported like the attack: take "Scene", 30 fps, armature and body.

## Method
`work/scripts/lyra_mixamo_retarget_20261018.py` with the `params_*.json` files.
- Retargeted by bone name, using the world-space rotation delta from rest. Hips height is scaled 0.991. Body bones only: the rig has no finger bones, so the hands keep the Meshy fist from her existing grip.
- Root travel: the hips' ground travel was linearly de-drifted. The net travel was already 0 in both clips. The in-clip weight shift is kept, up to 13–15 cm, because the feet are planted and removing it would make them slide.
- Blending: 10 frames in from rest into the Mixamo stance, then 12 frames back out. Each foot steps on a 7 cm lifted arc during the blend. The left foot steps first, and the stance puts the left foot forward with the body side-on.
- Two-handed staff: per frame the script searches the staff pose. The right hand keeps Derek's staff offset, and the left hand grips the shaft 35–61 cm lower. The staff is held in front of the chest, with the grip midpoint following the Mixamo hands' sway. Both arms use 2-bone IK with elbow-swivel search. Forearm twist takes up to 60° of each hand's twist.
- Search costs: torso, leg and head clearance (body mesh BVH), ground clearance, wrist bend, continuity (at most 50°/frame on the sweep, 35° on the nova), and a style term. The sweep keeps the staff near horizontal; the nova keeps it crystal-up.

## QC (work/qc_*.json)
- **Nova:** left grip within 0.6 cm on every frame. No staff/body overlap on any checked frame. Wrist bend at most 53° (left) and 73° (right); over 60° only on f45–f47.
- **Sweep:**
  - The left hand slides up to 10 cm off the shaft on f39–f47, the fastest part of the swing.
  - Wrist bend reaches 85° (left) and 78° (right) on the follow-through, f60–f64.
  - On f64 and f88 the staff is within 1 cm of her body (slight overlap).
- Contact sheets (side and 3/4): `/workspace/three_attacks/LYRA_attack2_staff_sweep_contact_sheet.jpg` and `/workspace/three_attacks/LYRA_attack3_arcane_nova_contact_sheet.jpg`.
