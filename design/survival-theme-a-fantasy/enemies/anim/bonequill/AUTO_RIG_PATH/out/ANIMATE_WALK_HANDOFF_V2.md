# Bonequill Meshy Animate walk v2 — handoff (2026-09-25)

Replaces shredded body_only package (PR #28 FAIL). Continuous AccuRIG body v2 → Meshy AccuRIG → Walking.

## Package
| File | Notes |
| --- | --- |
| `bonequill_meshy_animate_walk_v2.fbx` | ~30.1 MB skinned Walking |
| `bonequill_meshy_animate_walk_v2_atlas.png` | ~7.2 MB |
| `bonequill_meshy_animate_walk_v2_normal.png` | ~5.8 MB |
| Body SoT stills | `bonequill_accurig_continuous_v2_{front,34,rear}.png` |

## Why v2
v1 `body_only` island-cull shredded look → World FAIL. v2 = inflate + voxel remesh (1 island, watertight, props kept) → AccuRIG success.

## Soft
Paint bake may be softer than remesh look UVs; silhouette continuous. Side law: bow character-RIGHT, quiver character-LEFT (fused on mesh).

## Dev
Wire stills + walk-only (no Path A). HOLD merge until Design PASS lean. Re-ping Design for gate.
