# HOLD — Sir Aldric PILOT AccuRIG + Walk + Attack (2026-09-27)

**HOLD merge on PR #27 until Derek Play.** Path A / ClipSword / AimChain are **not SoT** and stay HOLD forever for this ship. No Design / Derek PASS claimed.

## SoT

| Item | Status |
| --- | --- |
| Look | `SirAldric_PILOT_accurig_humanoid.fbx` AccuRIG Humanoid mid280k. Paint maps: AccuRIG metallic/roughness PNGs + albedo from the walk/AccuRIG pack. Design paint stills `look_{front,34,rear}_paint.png`. |
| Walk | `SirAldric_PILOT_walk.fbx` take `Armature|Armature|Armature|Armature|Walking` frames 1–25. Retarget onto AccuRIG Humanoid. |
| Attack | `SirAldric_PILOT_attack.fbx` take `target_character\|rigify_clip\|BaseLayer` frames 3–92. Retarget onto AccuRIG Humanoid. |
| Sword | **Fused** in the body mesh. No empty-scabbard draw. No Path A / ClipSword / weight-paint. |
| Facing | Rear yaw 180 so +Z = TOP / enemy. |

## Discarded as SoT

- SEP mid450k / sep_paint UV sidecar / draw-parent palm seat
- Old fused Meshy walk as motion SoT (`sir_aldric_meshy_animate_walk.fbx` leftover on disk)
- ClipSword / AimChain / Path A

## Proofs in this folder

Play-cam rematch (Unity Y-up SoT remapped `(x, −z, y)`). **Not Meshy website stills. Not Unity Game-view** — this VM has no Unity Editor. `SirAldricGameViewCapture` is the Play-machine `Camera.Render` path.

- `sir_aldric_pilot_{rear,front,34}_walk_playcam.png`
- `sir_aldric_pilot_{rear,34}_strike_playcam.png`

Design lean stills (`animate_*.png`, `look_*_paint.png`) are reference only — not the hard gate.

## Unity Play

1. Play **SirAldric**. Game view 1080×1920 Scale 1×. Rear cam `(0, 2.20, −3.40)` LookAt `(0, 1.00, 0.30)` FOV 34.
2. Painted AccuRIG look. Walk toward TOP, then attack. Sword stays fused.
3. HOLD until Derek Play. Do not claim Design PASS.
