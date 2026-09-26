# HOLD — Sir Aldric SEP mid450k look density swap (2026-09-26)

**HOLD merge on PR #27 until Design chunky re-gate + Derek Play.** Path A CANCELLED. Density-swap archive. 2H + LEFT-hip SoT lives in `../aldric_sep_2h_yawlock_20260926/`. No Design PASS on this look drop.

## SoT

| Item | Status |
| --- | --- |
| Walk motion | `SirAldric_SEP_meshy_animate_walk.fbx` Walking on Mixamo Humanoid. Unchanged from `c88ea55` / `948adbc`. |
| Look | **`SirAldric_SEP_body_nosword_PAINTED_mid450k.glb`** + **`SirAldric_SEP_sword_scabbard_PAINTED_mid80k.glb`**. Same `sep_paint` atlas UV. Sheath iterate: character-LEFT hip — see `../aldric_sep_2h_yawlock_20260926/TWO_HAND_HOLD.md`. |
| Attack | 2H yawlock SoT — see `../aldric_sep_2h_yawlock_20260926/`. This folder is the density-swap archive. |

mid200k body + mid50k sword stay on disk as leftover. Not active look.

## Discarded as SoT

- Path A weight-paint / AccuRIG overwrite
- Treating mid200k / mid50k as look SoT

## Proofs in this folder

Play-cam remap (Unity Y-up SoT: eye `(0, 2.80, −5.40)` LookAt `(0, 0.90, 0.50)` FOV 30, 1080×1920). Blender Z-up remap `(x, −z, y)`. **Not Meshy website stills. Not Unity Game-view** (no Editor on this VM).

- `sir_aldric_sep_paint_mid450k_rear_walk_playcam.png`
- `sir_aldric_sep_paint_mid450k_front_walk_playcam.png`
- `sir_aldric_sep_paint_mid450k_34_walk_playcam.png`
- `sir_aldric_sep_paint_mid450k_walk_juice_playcam.mp4`

## Unity Play repro

1. Unity 6.3 LTS. Play **SirAldric**. Rear cam as above. Enemy = TOP / +Z.
2. Walk: mid450k look SoT + mid80k hip scabbard, RH clearance from `1ac345a`.
3. Attack: yawlock leftover for this tip. 2H still pending Design.
4. HOLD until Design chunky re-gate + Derek Play.
