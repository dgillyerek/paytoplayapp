# HOLD — Sir Aldric SEP 2H draw-parent (2026-09-26)

**HOLD merge on PR #27 until Design re-gate + Derek Play.** Path A CANCELLED. No Design PASS claimed.

Design FAIL lean on tip `b715c88`: `LookSwordScabbard` stayed on LeftUpperLeg the whole clip; hands empty at overhead + strike. Look mid450k PASS lean — look density / walk AccuRIG / Walking clip unchanged.

## SoT

| Item | Status |
| --- | --- |
| Walk motion | `SirAldric_SEP_meshy_animate_walk.fbx` Walking. Unchanged AccuRIG Humanoid. |
| Look | mid450k body + mid80k sword GLB, same `sep_paint` atlas UV. Unchanged. |
| Sheath | Empty `LookScabbard` stays character-**LEFT** `LeftUpperLeg`, Euler `(0, 90, −90)`, scale `0.28`, offset −right`*0.06` + up`*−0.12`. |
| Sword | `LookSword` (blade+hilt split from mid80k). Sheathed on LEFT hip until draw `u≥0.05`. Then parented to **RightHand**. Overhead→strike follows 2H grip (RH-biased midpoint). |
| Attack | **`SirAldric_SEP_meshy_animate_attack_2h_downstrike_yawlock.fbx` only.** Take `target_character\|Scene` frames 4–92. Ground-yaw Δ=0°. |

## Discarded as SoT

- Combined `LookSwordScabbard` glued for the whole clip
- Raw `*_attack_2h_downstrike` (~177° spin)
- 1H `*_attack_yawlock` / nospin / old SEP spin
- ClipSword / AimChain / Path A

## Proofs in this folder

Play-cam remap (Unity Y-up SoT remapped `(x, −z, y)`). **Not Meshy website stills. Not Unity Game-view.**

Attack: `sir_aldric_sep_2h_dp_{rear,34}_{draw,overhead,strike}_playcam.png` + `sir_aldric_sep_2h_dp_attack_juice_playcam.mp4`

Juice plays the same Scene take as the stills (draw f6 → overhead f30 → downstrike f39 → recover). Blade is in hands from draw through strike. Empty scabbard stays LEFT hip.

Walk proofs unchanged: `../aldric_sep_2h_yawlock_20260926/` + mid450k look folder.

Design verify (clip log): ground-yaw Δ=0°; RH travel ~1.15 m; overhead→down ~1.09 m; rear away-dot ~0.98.

## Unity Play

1. Play **SirAldric**. Rear cam `(0, 2.80, −5.40)` LookAt `(0, 0.90, 0.50)`. Enemy = TOP / +Z.
2. Walk: mid450k paint, LEFT-hip empty+sheathed pair, RH empty.
3. Attack: 2H yawlock only. Blade leaves LEFT sheath on draw and stays in hands through overhead + downstrike. Must not spin ~180°.
4. HOLD until Design re-gate + Derek Play. Do not claim Design PASS.
