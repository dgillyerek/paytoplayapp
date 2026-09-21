# Gate 3 — Path 2 retopo + hang-skin FBX (shatter weights abandoned)

Design **FAIL** walk-with-look on `89481e5`, `71f0c4a`, and `e56aeb1` — same three hard FAILs (arm slabs / stacked scabbard / tear bands). Look PASS on paint stills `e5b132f` still stands. **Walk-with-look is NOT claimed.** Play hub PNG locked.

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

**Retopo / remesh Path 2 look into a clean mid-poly, then proper Blender hang skin → FBX + FMT v4.**

- Voxel remesh after deleting sheath shards; arms pushed out so hang tubes stay solid volumes (not tabard sheets).
- **One** solid scabbard capsule on character-RIGHT (not stacked Meshy hem/sheath shells).
- Look albedo transferred from the look-PASS Path 2 source (`e5b132f` Image_0 + lion cards).
- Hang armature: upper + fore + hand, up-leg + shin + foot. Actor `Bone4`.
- `Evaluate()` keys from `5916447` **reused**. No gait rewrite.

Rebuild: `blender --background --python scripts/blender/skin_sir_aldric_retopo.py` then `python3 scripts/blender/compose_path2_walk_proof.py` and `python3 scripts/blender/compose_vs_e56_fail.py`

## KEEP

TOP march, lion readable, **one** brown scabbard character-RIGHT, face +Z, Unity Y-up, sheathed. Hub PNG locked.

## Proof

Play World-cam `(0, 2.80, −5.40)` look-at `(0, 0.90, 0.50)` FOV 30, 1080×1920:

- `sir_aldric_path2_walk_toward_top.mp4`
- `gate3_path2_walk_phases.png`
- `gate3_path2_vs_e56aeb1.png` (Design-asked side-by-side vs e56 FAIL)
- `walk/world_walk_{pass,contact}_{l,r}.png`
- `fail_e56aeb1/` — frozen e56 stills

DCC: `sir_aldric_path2_clean.fbx` + `sir_aldric_meshy_skinned.blend`. Runtime still `sir_aldric_meshy.mesh.txt` (FMT v4).

## Honest ART

`61e023d` remesh stills looked cleaner than the **Game-view MP4**. Design retracted bind PASS: MP4 still FAILed arm slab/sheets + mid/leg tear bands (scabbard = one, progress).

This iterate: **detach hang-arm islands** from the body, inflate them to solid tubes, exclusive 1-bone weights (no heat smear, no Fore/Hand split). Hem/tabard locked to Hips/Spine/Chest. Look transfer unchanged — bind first. **Do not claim bind PASS. Do not claim walk PASS.** Hub PNG locked.
