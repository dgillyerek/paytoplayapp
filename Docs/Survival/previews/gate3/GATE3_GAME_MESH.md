# Gate 3 — Path 2 Meshy look stills (paint iterate)

Design dropped a Meshy Flagship Image-to-3D remesh (locked rear SoT). Original ~57MB / 1.77M faces exceeded upload; this is the Blender Decimate handoff (~50k faces) **plus a paint iterate** on UV padding and heraldry.

This pass is **LOOK stills only**. It does **not** claim look PASS. Walk / Animator / Play hub are **HOLD**.

## In

- Source: `design/.../AI_MESH_PATH2/out/aldric_meshy_retopo.glb` (8babba7 drop + this iterate)
- ThemePack: `sir_aldric_meshy_retopo.glb` + `sir_aldric_meshy.blend` (Unity Y-up, +Z face, scabbard +X)
- World-cam stills at **Play march angle** `(0, 2.80, −5.40)` look-at `(0, 0.90, 0.50)` FOV 30, 1080×1920, TOP = +Z = away:
  - `world_rear.png` / `world_rear_34.png` / `world_front.png` / `world_side_r.png` / `world_34_front.png`
  - `gate3_worldcam_vs_sot.png` vs `01_rear_LOCKED` + turnaround 01–04
  - `gate3_uv_lion_delta.png` vs 8babba7 FAIL stills (lion + neck/shoulder crops)
- Hard lock: brown scabbard **character-RIGHT** (viewer-right from World-cam rear). Sheathed.

Actor motion bind is **unchanged** (`sir_aldric_midpoly.mesh.txt` + `Evaluate()`). Path 2 is not skinned to the walk clip this pass.

## What changed vs 8babba7 (paint FAIL)

1. **UV island cracks** — atlas occupancy bleed (~16px NN) + near-black island-border inpaint + custom split-normals cleared. Shoulder/arm crack lines are reduced; a neck groove remains (geometry + leftover bake).
2. **Gold lion smear** — dedicated planar lion cards from locked `01_rear_LOCKED` / `01_FRONT` on upper-torso cloth faces (separate materials, not a stretched atlas stamp). Rear rampant is now readable vs the 8babba7 smear. Leftover Meshy gold specks at panel edges remain.
3. **Hang** — not applied. A vertex A-pose→hang explode the scabbard/arms; stills stay A-pose. Honest pose gap vs Play hang.

Rebuild: `scripts/blender/repair_path2_paint.py` then `import_sir_aldric_meshy.py` + `compose_gate3_worldcam.py`.

## Out (paused until look PASS)

- Walk / Animator clip swap
- Play hub PNG swap (`SIR_ALDRIC_REAR_MASTER_LOCKED.png` stays)

Motion from 5916447 **holds**.

## Honest gaps (look NOT claimed)

- Path 2 Meshy-based mid-poly, not a painted unique unwrap. Bleed is padding, not a full re-unwrap of plate.
- Neck seam + some joint lines still read. Lion cards sit on a reconstructed panel, not embroidered cloth grain.
- A-pose vs Play hang.
- World-cam is high Play angle — not a beauty portrait cam.

**Look gate is not claimed.** Design can re-eye UV + lion vs the locked turnaround.
