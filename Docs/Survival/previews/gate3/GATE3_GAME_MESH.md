# Gate 3 — Path 2 Meshy-look hang-skin (e5b132f)

Design **FAIL** walk-with-look on `89481e5`, `71f0c4a`, `e56aeb1` (shatter weights) then remesh/capsule Game-view (`61e023d` / `c840b73`). Derek STOP: remesh/capsule threw away paint PASS `e5b132f`. Look PASS on paint stills still stands. **Walk-with-look is NOT claimed.** Play hub PNG locked.

## Census (why weights-on-shatter is dead)

Raw import, no weld (`census_path2_bind.py`):

- **11283** connected islands (Meshy/decimate shatter). Top island = 127 verts.
- **6** sheath-like islands; **2** overlapping duplicate groups.
- Mesh arm PCA is already hang (~8° from −Y). **Not A-pose.**

Three binds on that shell all shredded under `Evaluate()`:

| tip | premise | result |
|---|---|---|
| `89481e5` | Bone1 A-pose boxes | arm slabs, stacked sheath, tear bands |
| `71f0c4a` | Bone1 hang corridor / one-arm volume | same three |
| `e56aeb1` | hang-heat premise B on shatter | same three |

## Old premise (abandoned)

**Weights (Bone1 or heat) on Meshy shattered islands.** Design STOP: do not iterate that again.

## New premise (this tip)

**Hero look = Meshy GLB / `e5b132f`. Clean for skin without destroying albedo. Then hang-skin + Evaluate().**

- Source: `aldric_meshy_retopo.glb` + paint-PASS Image_0 / SoT lion cards.
- UV / albedo: original GLB UVs kept. Atlas = Image_0 + lion-card strip (`combine_atlas_and_remap`). Game-view renders original mats before remap.
- Clean: GEO-delete extra sheath islands + 1mm weld (11283 → 12 islands). No voxel remesh. No capsule arms in Game-view.
- Arm volume: small same-side hang-arm faces solidified ~5cm (UVs kept on the Meshy shell); huge leftover faces deleted.
- Exclusive hang-volume weights; cloth/hem locked before `Arm_*`. One remaining sheath → Scabbard.
- `Evaluate()` keys from `5916447` **reused**. No gait rewrite.

Rebuild: `blender --background --python scripts/blender/skin_sir_aldric_meshylook.py` then `python3 scripts/blender/compose_path2_walk_proof.py` and `python3 scripts/blender/compose_vs_e5b132f.py`

## KEEP

TOP march, lion readable, **one** brown scabbard character-RIGHT, face +Z, Unity Y-up, sheathed. Hub PNG locked.

## Proof

Play World-cam `(0, 2.80, −5.40)` look-at `(0, 0.90, 0.50)` FOV 30, 1080×1920:

- `sir_aldric_path2_walk_toward_top.mp4`
- `gate3_path2_walk_phases.png`
- `gate3_meshy_hang_vs_e5b132f.png` (hang rest vs e5b132f `world_rear`)
- `walk/world_walk_{pass,contact}_{l,r}.png` + `walk/world_walk_mid_swing.png`

DCC: `sir_aldric_path2_clean.fbx` (Meshy, not capsules) + `sir_aldric_meshy_skinned.blend`. Runtime `sir_aldric_meshy.mesh.txt` (FMT v4).

## Honest ART

Source tip **`e5b132f`**. UVs/albedo preserved (GLB UVs + Image_0/lion atlas). Retopo was weld + extra-sheath GEO-delete + limited arm-face solidify — **not** voxel remesh, **not** capsule arms in the hero Game-view.

`fd9d6f8` LOOK PASS / BIND FAIL: mid-swing arm sheets + mid/leg tear bands (scabbard one). `cc77d8a` isolated `Arm_*` solidify+prune; full hang-leg tubes so hem cannot Y-cut the shin. LOOK path unchanged. Design FAIL bind again — MP4 mid-swing still sheets.

**Mesh-vs-capture (mandatory):** same mid-swing t=0.625. A = Actor LBS still (Unity skin path, no encoder). B = offline Blender EEVEE. C = MP4 n=10.

- `cc77d8a` FAIL: A+B+C all tear → **MESH**. Sheet `gate3_mesh_vs_capture_mid_swing.png`.
- After hang-arm wrap: A+B+C closed volumes in the stills and in the re-dropped MP4. Wrap sheet `gate3_mesh_vs_capture_wrap_mid_swing.png`. Gold lame albedo on the right still reads as bands. **Do not claim bind PASS. Do not claim walk PASS.** Hub PNG locked.

vs-fd9: `gate3_path2_vs_fd9d6f8.png` + frozen `fail_fd9d6f8/`.
