# Bonequill Meshy Animate walk wire (2026-09-25)

**HOLD merge.** No Design PASS. No hub unlock. Path A weight-paint CANCELLED.

Design AccuRIG→Animate walk, Dev stills + walk-only (same playbook as Sir Aldric Meshy Animate / #26 atlas-before-punch).

## Package

| | |
| --- | --- |
| FBX | `Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/bonequill_meshy_animate_walk.fbx` |
| Atlas | `…/bonequill_meshy_animate_walk_atlas.png` |
| Normal | `…/bonequill_meshy_animate_walk_normal.png` (FBX `texture_0_normal`; tar sibling was a duplicate albedo) |
| Design drop | `design/survival-theme-a-fantasy/enemies/anim/bonequill/AUTO_RIG_PATH/out/` |
| Mesh | `Bonequill_AccuRIG_BodyOnly` |
| Rig | AccuRIG Mixamo `mixamorig:*` (32), Humanoid |
| Take | `target_character\|target_character\|target_character\|Walking` (1–26). Stub `.001` ignored. |

## Soft leftover (does not block wire)

Body-only: bow (character-RIGHT) and quiver (character-LEFT) culled so AccuRIG succeeded. Design gate later.

## Actor

`BonequillMeshyAnimateActor` + `BonequillDemo` scene. RearYaw 180 (Mixamo face −Z → back to Play cam, march +Z / TOP). Walking clip AS-IS. Atlas `_BaseMap` + normal `_BumpMap`. No attack. No Path A.

Play cam SoT: `(0, 2.80, −5.40)` LookAt `(0, 0.90, 0.50)` FOV 30.

## Proofs

| File | What |
| --- | --- |
| `bonequill_world_front.png` | Front still |
| `bonequill_world_34_front.png` | ¾ front |
| `bonequill_world_rear.png` | Rear / back-to-camera |
| `bonequill_world_rear_34.png` | ¾ rear |
| `bonequill_walk_toward_top_rear.mp4` | Walk toward TOP, rear Play cam |

Do not treat these as Unity Game-view. This VM has no Unity Editor; stills/MP4 are Blender Play-cam remaps of the same FBX+atlas (yaw 180). Design re-gates PASS/FAIL.
