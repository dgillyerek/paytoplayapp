# Gate 3 — Path 2 hang-heat after two Bone1 FAILs

Design **FAIL** walk-with-look on `89481e5` and again on `71f0c4a` — same three hard FAILs (arm slabs / stacked scabbard / tear bands). Look PASS on paint stills `e5b132f` still stands. **Walk-with-look is NOT claimed.** Play hub PNG locked.

## Census (rerun: `blender --background --python scripts/blender/census_path2_bind.py`)

Raw import, no weld:

- **11283** connected islands (Meshy/decimate shatter). Top island = 127 verts.
- **6** sheath-like islands; **2** overlapping duplicate groups `(66+30)` and `(43+42+39)`.
- Actor hang `Arm_R` chain = world **−Y**. Bone gizmos are +Y stubs.
- Mesh arm PCA is already hang: Arm_R **8.1°** from −Y, Arm_L **7.9°**. **Not A-pose.**
- The twice-failed premise was **Bone1 corridor weights**, not a 90° rest mismatch.

Dump: `Docs/Survival/previews/gate3/CENSUS_BIND.txt` + `sir_aldric_meshy_census.json`.

## Old premise (failed twice)

`rigid Bone1 / corridor weights on a Meshy shell driven by hang Evaluate() keys.`

That shreds a thin plate into slabs, splits a shattered sheath across bones, and Y-cuts the hem.

## New premise (this tip)

**B — delete duplicate sheath islands at GEO, then Blender heat / multi-bone weights on hang limb spans (elbow + hand influence).**

- Mesh already hang (~8°). No A-pose vertex explode.
- FMT v4: object-space verts + up to 4 named weights. Actor `SkinQuality.Bone4`.
- `Evaluate()` keys from `5916447` **reused**.

## KEEP

TOP march, lion readable, scabbard character-RIGHT, face +Z, Unity Y-up, sheathed. Hub PNG locked.

## Proof

Play World-cam `(0, 2.80, −5.40)` look-at `(0, 0.90, 0.50)` FOV 30, 1080×1920:

- `sir_aldric_path2_walk_toward_top.mp4`
- `gate3_path2_walk_phases.png`
- `walk/world_walk_{pass,contact}_{l,r}.png`

Rebuild: `blender --background --python scripts/blender/skin_sir_aldric_hangheat.py` then `python3 scripts/blender/compose_path2_walk_proof.py`

## Honest ART

Clip is **visibly different** from `71f0c4a` / `89481e5`: hang arm volumes, elbow influence, contralateral swing, one thin sheath, hem stays on the hips.

Remaining: heat can tug the hip/tabard a little; rigid plate is still Meshy mid-poly; neck groove / gold specks remain (look-PASS polish). **Do not claim walk PASS.**
