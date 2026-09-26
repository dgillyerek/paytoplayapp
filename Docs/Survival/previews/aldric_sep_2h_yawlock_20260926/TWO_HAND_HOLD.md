# HOLD — Sir Aldric SEP 2H yawlock + LEFT hip + mid450k look (2026-09-26)

**HOLD merge on PR #27 until Design re-gate + Derek Play.** Path A CANCELLED. No Design PASS claimed.

## SoT

| Item | Status |
| --- | --- |
| Walk motion | `SirAldric_SEP_meshy_animate_walk.fbx` Walking. Unchanged AccuRIG Humanoid. |
| Look | mid450k body + mid80k sword GLB, same `sep_paint` atlas UV. |
| Sheath | `LookSwordScabbard` on character-**LEFT** `LeftUpperLeg`, Euler `(0, 90, −90)`, scale `0.28`, offset −right`*0.06` + up`*−0.12`. Derek: RH draws from LEFT sheath. |
| Attack | **`SirAldric_SEP_meshy_animate_attack_2h_downstrike_yawlock.fbx` only.** Take `target_character\|Scene` frames 3–92. Ground-yaw Δ=0°. LEFT-hip RH draw → overhead → two-hand downstrike toward TOP. |

## Discarded as SoT

- Raw `*_attack_2h_downstrike` (~177° spin)
- 1H `*_attack_yawlock` / nospin / old SEP spin
- ClipSword / AimChain / Path A
- RIGHT-hip sheath (superseded for this 2H SoT)

## Proofs in this folder

Play-cam remap (Unity Y-up SoT remapped `(x, −z, y)`). **Not Meshy website stills. Not Unity Game-view.**

Walk: `sir_aldric_sep_2h_{rear,front,34}_walk_playcam.png` + `sir_aldric_sep_2h_walk_juice_playcam.mp4`

Attack: `sir_aldric_sep_2h_rear_{draw,overhead,strike}_playcam.png`, `sir_aldric_sep_2h_34_{draw,overhead,strike}_playcam.png` + `sir_aldric_sep_2h_attack_juice_playcam.mp4`

Design verify (clip log): ground-yaw Δ=0°; RH travel ~1.15 m; overhead→down ~1.09 m; rear away-dot ~0.98.

## Unity Play

1. Play **SirAldric**. Rear cam `(0, 2.80, −5.40)` LookAt `(0, 0.90, 0.50)`. Enemy = TOP / +Z.
2. Walk: mid450k paint, LEFT-hip scabbard, RH empty.
3. Attack: 2H yawlock only. Must not spin ~180°. Draw LEFT → overhead → downstrike toward TOP.
4. HOLD until Design re-gate + Derek Play.
