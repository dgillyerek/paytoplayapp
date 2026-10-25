# Ironhowl Mixamo attack picks: REJECTED (2026-10-16)

Derek: Ironhowl's attacks must look different from Blightroot's mutant moves; Blightroot keeps his.
`IRONHOWL_mixamo_mutant_jump_attack.fbx` and `IRONHOWL_mixamo_mutant_roaring.fbx` are out of his battle rotation
and out of the ThemePack. Only these raw design downloads (and CHECKSUMS.md5) stay here, for reference.
Ironhowl plays his single locked claw swipe until the replacement downloads ('Standing Melee Attack Downward'
and a 'Standing Melee Combo Attack') are wired.

## Replacements wired (2026-10-18)
- attack2 overhead smash: `IRONHOWL_mixamo_standing_melee_attack_downward.fbx` (Mixamo "Standing Melee Attack Downward",
  md5 b9424842fd939da1c349993ffe0b84d8, frames 0-68, contact 28, purple GroundSlam crack ring + 0.09 m shake).
- attack3 claw combo: `IRONHOWL_mixamo_standing_melee_combo_attack_v2.fbx` (Mixamo "Standing Melee Combo Attack Ver. 2",
  md5 53449519ca7b87c34699dbab97c291b0, frames 0-126, hits 29 R / 46 L / 59 L / 82 R, purple claw sparks per hit).
- Both: same 33-bone skeleton, no retarget, byte-identical to the downloads; no In Place offered, so the battle holds
  the hips' ground travel on the spot (downward 0.17 m max, combo 0.47 m max; both return to start).

## Authored attacks added (2026-10-24)
attack4 ground pound, attack5 backhand sweep, attack6 shoulder charge, keyposed on his own skeleton. See `../authored_attacks/NOTE.md`.
