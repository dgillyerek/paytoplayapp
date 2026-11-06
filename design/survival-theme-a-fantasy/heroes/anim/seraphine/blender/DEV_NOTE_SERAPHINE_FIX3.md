# Seraphine FIX3 — staff never through body (continued)

Branch: `seraphine-fix3`. Derek's 24-bone split-thigh skeleton **unchanged** (hash `d5611ac70629687b`).

## This pass (wrist / grip / wing / cape)

### Attack_thrust — PASS grip + wrist
- Staff mesh seated toward palm: palm→axis **1.51 cm**, inner gap **0.40 cm** (≤2.5 / ≤0.5).
- Wrist twist maxΔ **8.0 °/f**, 0 flips (IK absorb into forearm/upper-arm; hand world locked).
- 0 body crossings. Body-center nearest is always R_forearm grip-zone (min 0.24 cm @f9) — acceptable contact.
- Wing min 22.5 cm.

### Attack_throw — wing clear; twist best-with-0-cross
- Wing near-misses fixed: wing min **2.54 cm** (was 0.02); 0 wing_overlap.
- Wrist: **8 °/f reintroduces body crossings**. Best with 0 crossings: **40 °/f** maxΔ, 0 flips (forearm-follow / IK absorb). Raw was ~118–156 °/f.
- Palm 2.78 / gap 1.57 unchanged (main spear seat caused R_forearm ray crossings — reverted).
- 0 body crossings. Min 0.46 cm @f15 R_forearm.

### Idle / walk
- Twist OK (4.82 / 6.47 °/f). Palm 2.78 / gap 1.57 (seat skipped — caused forearm cross).
- 0 body crossings. Mins 1.68 / 2.22 cm R_forearm.

### Cape (fresh spring sim TAG=_fix3)
- Walk: **0 body / 0 spear / 0 wing** penetration; staff↔cape min 54.8 cm.
- Idle / thrust: staff↔cape **0 cross**; residual cape↔body collar pens in spring sim (pre-existing pattern).
- Throw: after 2× capefix, **1 frame** staff↔cape residual (f10); body/wing pens in raw spring remain.
- Cape FBX asset not re-baked this pass (dumps under design/.../work/capedump_*_v5_fix3*).

### Foot slip walk
- L 4.85 cm / R 4.93 cm (unchanged ballpark).

## Artifacts
- Sheet + MP4s: `design/.../seraphine/sheets/fix3/`
- Work blend: `/workspace/rigjob/sera_fix3/work/SERAPHINE_FIX3.blend`
