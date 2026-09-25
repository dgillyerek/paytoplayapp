# Bonequill Meshy Animate walk **v2** wire (2026-09-25)

**HOLD merge.** No Design PASS. No hub unlock. Path A weight-paint CANCELLED.

Replaces shredded `body_only` package (PR #28 v1 FAIL). Continuous watertight AccuRIG body (props kept) → AccuRIG → Meshy Animate walk. Same import+actor path as v1 / Aldric Meshy Animate: Humanoid Avatar, painted atlas, walk clip only.

## Package

| | |
| --- | --- |
| FBX | `Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/bonequill_meshy_animate_walk_v2.fbx` (~30.1 MB) |
| Atlas | `…/bonequill_meshy_animate_walk_v2_atlas.png` |
| Normal | `…/bonequill_meshy_animate_walk_v2_normal.png` (real normal, not a duplicate albedo) |
| Design drop | `design/survival-theme-a-fantasy/enemies/anim/bonequill/AUTO_RIG_PATH/out/` |
| Mesh | `Bonequill_AccuRIG_Continuous_V2` |
| Rig | AccuRIG `Hips` / `Spine02` / `LeftFoot` (no `mixamorig`). Humanoid, native avatar. |
| Take | `Armature\|Armature\|Armature\|Walking` (1–25). Stub `.001` ignored. |

## Soft leftover (does not block wire)

Paint bake may be softer than remesh UVs; silhouette is continuous. Side law: bow character-RIGHT, quiver character-LEFT (fused on mesh).

## Actor

`BonequillMeshyAnimateActor` + `BonequillDemo` scene. RearYaw 180 (AccuRIG import face −Z → back to Play cam, march +Z / TOP). Walking clip AS-IS on this FBX’s own Humanoid avatar. Atlas `_BaseMap` + normal `_BumpMap`. No attack. No Path A. Do not retarget onto a Mixamo walk avatar.

Play cam SoT: `(0, 2.80, −5.40)` LookAt `(0, 0.90, 0.50)` FOV 30.

## Proofs

| File | What |
| --- | --- |
| `bonequill_v2_world_front.png` | Front still |
| `bonequill_v2_world_34_front.png` | ¾ front |
| `bonequill_v2_world_rear.png` | Rear / back-to-camera |
| `bonequill_v2_world_rear_34.png` | ¾ rear |
| `bonequill_v2_walk_toward_top_rear.mp4` | Walk toward TOP, rear Play cam |
| `bonequill_accurig_continuous_v2_{front,34,rear}.png` | Design Body SoT stills (copied) |

Do not treat World stills/MP4 as Unity Game-view. This VM has no Unity Editor; stills/MP4 are Blender Play-cam remaps of the same FBX+atlas (Unity Y-up, X−90, −X laterality so bow is character-RIGHT). Design re-gates PASS/FAIL.
