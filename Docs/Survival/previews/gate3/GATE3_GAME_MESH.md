# Gate 3 — Sir Aldric game mesh (FAIL iterate)

Derek FAIL: "The cape isn't terrible but everywhere else lacks the detail of the portrait."
This pass keeps surcoat volume and **adds readable plate / helm / heraldry / sabaton detail** at Play World-cam (1080×1920). It does **not** claim look PASS.

## In

- `sir_aldric.blend` / `.fbx` / `sir_aldric_midpoly.mesh.txt` — segmented mid-poly (~13192 tris, ~6926 verts) grown from clay `5de0e16`. Gold rims on pauldrons / arms / legs. Closed armet + crest (not a cone). Gauntlet fingers (not spheres). Sabaton lames + gold. No voxel remesh.
- Atlas `sir_aldric_atlas.png` — darker silver + gold lame grooves; lion keyed onto exact surcoat blue (no navy plaque); chunky Greek-key.
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

- Scripted loft mid-poly, not a painted unique unwrap. Filigree density is still short of the portrait / `01_rear_LOCKED` (gold rims + tiled plate, not unique scrollwork).
- Lion is embroidered on the cloth (cylindrical UV, no floating plaque) but smaller / simpler than the locked rampant.
- Bind pose is hang (Play / `01_rear`); Gate 1 turnaround is A-pose. Arm pose will not overlay the turnaround sheet.
- World-cam is high rear — **face law is not visible**. Helm BACK is judged vs `01_rear_LOCKED` / `03_BACK`. Portrait attachments were not on disk this pass; do not invent a face.

**Look gate is not claimed.** Derek must scrub these World-cam stills (not a beauty portrait cam) against the locked rear / portrait.
