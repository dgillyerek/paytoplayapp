# Gate 3 — Sir Aldric game mesh (look stills)

CEO Gate 2 was a **soft-PASS** (Derek moved on without FAIL). Volume law = clay `5de0e16` / compare `6ef0919`. This pass **grows** a mid-poly game mesh from those volumes. It does **not** redesign proportions.

## In

- `sir_aldric.blend` / `.fbx` / `sir_aldric_midpoly.mesh.txt` — lofted mid-poly (~6352 tris, ~3287 verts) with extra rings at shoulders / elbows / hips / knees. No voxel remesh.
- Atlas `sir_aldric_atlas.png` — silver / gold / royal-blue / brown + lion + Greek-key from `01_rear_LOCKED`.
- World-cam stills at **Play march angle** `(0, 2.80, −5.40)` look-at `(0, 0.90, 0.50)` FOV 30, 1080×1920, TOP = +Z = away:
  - `world_rear.png` — high rear (front-of-march)
  - `world_rear_34.png` — high rear ¾ (+X)
  - `gate3_worldcam_vs_sot.png` — side-by-side vs `01_rear_LOCKED` and turnaround `03_BACK`
- Hard lock: brown scabbard **character-RIGHT** only (viewer-right from World-cam rear). Sheathed only — no drawn second sword. No cape mesh.

## Out (paused until look PASS)

- Walk / Animator clip swap
- Play hub PNG swap (`SIR_ALDRIC_REAR_MASTER_LOCKED.png` stays)

Motion from 5916447 **holds**. `Evaluate()` untouched.

## Honest gaps (look NOT claimed)

- Scripted loft mid-poly, not a painted unique unwrap. Faceting / plate thickness still short of the locked PNGs.
- Bind pose is hang (Play / `01_rear`); Gate 1 turnaround is A-pose. Arm pose will not overlay the turnaround sheet.
- World-cam is high rear — **face law is not visible**. Helm BACK is judged vs `01_rear_LOCKED` / `03_BACK`. `05_PORTRAIT_COMPLETE_fullbody.png` / `00_portrait_COMPLETE_LOCKED.png` were not on the VM this pass; do not invent a face.
- Scabbard chape can pick up a small atlas-bleed band; lion is a back plaque from the locked crop, not embroidered cloth.

**Look gate is not claimed.** Derek must scrub these World-cam stills (not a beauty portrait cam) against the locked rear / turnaround.
