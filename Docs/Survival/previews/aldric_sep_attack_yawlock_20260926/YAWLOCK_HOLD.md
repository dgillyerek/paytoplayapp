# HOLD — Sir Aldric SEP yawlock attack clip (2026-09-26)

**HOLD merge on PR #27 until Derek Play + Design re-gate.** Path A CANCELLED.

Design motion **PASS** is on the yawlock clip itself (`max|Δ hips yaw Z|=0.00°`; rear strike away-from-camera). This wire does **not** claim Unity Play PASS. Humanoid retarget onto the walk Mixamo Avatar still needs Derek eyes.

## SoT

| Item | Status |
| --- | --- |
| Walk motion | `SirAldric_SEP_meshy_animate_walk.fbx` Walking take on Mixamo Humanoid. Unchanged from `c88ea55` / `1ac345a` Derek Play PASS. |
| Look | Painted midpoly atlas + `LookSwordScabbard` on character-RIGHT `RightUpperLeg`, Euler `(0, −90, −90)`, scale `0.28`, offset right`*0.06` + up`*−0.12`. Walk RH clearance from tip `1ac345a` **unchanged**. Soft leftover: body-only empty RH — hip prop stays sheathed, not drawn to hand. |
| Attack | **`SirAldric_SEP_meshy_animate_attack_yawlock.fbx` only.** Take `target_character\|Scene`, frames 3–92 @ 30 FPS. Hips world Z yaw locked at 1.859°. Slash toward TOP / +Z (away from rear camera). |

Actor instantiates the walk FBX, yaws **180°** (Mixamo face −Z → world +Z / TOP), binds painted maps, parents the hip scabbard, then Humanoid-retargets the yawlock Attack clip onto that same Avatar. No second mesh. No ClipSword. No AimChain.

## Discarded as SoT

- `SirAldric_SEP_meshy_animate_attack.fbx` — leftover spin ~180°. **not SoT.**
- `SirAldric_SEP_meshy_animate_attack_nospin.fbx` — leftover re-author still spun ~180°. **not SoT.** nospin is not SoT.
- ClipSword / AimChain / Path A weight-paint
- Treating Unity Game-view as already captured on this VM

## Proofs in this folder

Play-cam remap (Unity Y-up SoT: eye `(0, 2.80, −5.40)` LookAt `(0, 0.90, 0.50)` FOV 30, 1080×1920). Blender Z-up remap `(x, −z, y)` so this MeshyRig FBX's cape-back matches Unity rear after RearYaw 180. Character faces −Y; rear cam at +Y; slash toward −Y = TOP / away from rear camera. **Not Meshy website stills. Not Unity Game-view** (no Editor on this VM).

- `sir_aldric_sep_yawlock_rear_attack_playcam.png` — strike f25, rear / cape-back
- `sir_aldric_sep_yawlock_front_attack_playcam.png` — strike f25, front
- `sir_aldric_sep_yawlock_34_attack_playcam.png` — strike f25, 3/4
- `sir_aldric_sep_yawlock_rear_draw_playcam.png` — draw f8, rear
- `sir_aldric_sep_yawlock_attack_juice_playcam.mp4` — full yawlock clip, rear, 15 fps

Unity Game-view capturer writes `*_gameview.*` into this folder when Derek Plays (`Survival → Capture Sir Aldric SEP yawlock attack` or `-aldric-capture`).

## Unity Play repro

1. Unity 6.3 LTS. Play **SirAldric**. Rear cam as above. Enemy cube = TOP / +Z.
2. Walk: painted look + RIGHT-hip scabbard + RH clearance from `1ac345a`. Motion unchanged.
3. Attack: yawlock clip only. Must **not** spin ~180°. Slash toward TOP / away from rear camera.
4. HOLD until Derek Play PASS. Design re-gates after.
