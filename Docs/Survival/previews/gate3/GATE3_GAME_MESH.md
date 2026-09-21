# Gate 3 — Path 2 Meshy look stills

Design dropped a Meshy Flagship Image-to-3D remesh (locked rear SoT). Original ~57MB / 1.77M faces exceeded upload; this is the Blender Decimate handoff (~11MB, ~50k faces, UVMap, textures embedded).

This pass is **LOOK stills only**. It does **not** claim look PASS. Walk / Animator / Play hub are **HOLD**.

## In

- Source: `design/.../AI_MESH_PATH2/out/aldric_meshy_retopo.glb`
- ThemePack: `sir_aldric_meshy_retopo.glb` (drop bytes) + `sir_aldric_meshy.blend` (Unity Y-up, +Z face, scabbard +X)
- World-cam stills at **Play march angle** `(0, 2.80, −5.40)` look-at `(0, 0.90, 0.50)` FOV 30, 1080×1920, TOP = +Z = away:
  - `world_rear.png` / `world_rear_34.png` / `world_front.png` / `world_side_r.png` / `world_34_front.png`
  - `gate3_worldcam_vs_sot.png` vs `01_rear_LOCKED` + turnaround 01–04
- Hard lock: brown scabbard **character-RIGHT** (viewer-right from World-cam rear). Sheathed.

Actor motion bind is **unchanged** (`sir_aldric_midpoly.mesh.txt` + `Evaluate()`). Path 2 is not skinned to the walk clip this pass.

## Out (paused until look PASS)

- Walk / Animator clip swap
- Play hub PNG swap (`SIR_ALDRIC_REAR_MASTER_LOCKED.png` stays)

Motion from 5916447 **holds**.

## Honest gaps (look NOT claimed)

- **Path 2 Meshy-based mid-poly, not a scripted loft.** Decimate from Flagship still has UV island cracks, gold-lion smear, and A-pose (turnaround) vs Play hang.
- Not a painted unique unwrap; Meshy bake + 50k remesh.
- World-cam is high Play angle — not a beauty portrait cam.

**Look gate is not claimed.** Design can eye these World-cam stills vs the locked turnaround for re-gate.
