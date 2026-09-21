# Gate 3 — Path 2 look skinned to held walk (clip for Design re-gate)

Design **PASS** look on paint tip `e5b132f` and unlocked skin-to-walk. This pass binds that Path 2 Meshy retopo to the held Actor / `Evaluate()` pipeline from motion tip `5916447`.

**Look PASS** is Design's (paint stills). **Walk-with-look is NOT claimed.** Play hub PNG stays locked. PR #21 **HOLD merge** until Design eyes the walk clip.

## In

- Look mesh: `design/.../AI_MESH_PATH2/out/aldric_meshy_retopo.glb` (e5b132f paint iterate)
- Runtime bind: `sir_aldric_meshy.mesh.txt` + `sir_aldric_meshy_atlas.png` on `SirAldric3DActor` bones
- Motion: `SirAldric3DMotion.Evaluate()` keys **reused** — gait / weave / forward-swing **not edited**
- World-cam clip at Play march angle `(0, 2.80, −5.40)` look-at `(0, 0.90, 0.50)` FOV 30, 1080×1920, TOP = +Z = away:
  - `sir_aldric_path2_walk_toward_top.mp4`
  - `gate3_path2_walk_phases.png` (PASS L / CONTACT L / PASS R / CONTACT R)
  - captioned stills in `walk/world_walk_{pass,contact}_{l,r}.png`
- Hard locks: brown scabbard **character-RIGHT** (+X; viewer-right from World-cam rear). Face +Z / TOP away. Unity Y-up. Sheathed.

Loft archive `sir_aldric_midpoly.mesh.txt` remains on disk. Actor prefers Meshy.

## What bind did (honest)

1. **A-pose mesh on hang bones.** Actor rest is hang (arms local −Y). Meshy is A-pose. Bind is rigid `SkinQuality.Bone1` with hang bindposes. Rest display stays A-pose. `Evaluate()` rotations were authored for hang, so walk arms read as **stiff A-pose slabs / wings**. That is the bind, not a gait change.
2. **Assign order.** A-pose arms first (`y>0.58`, `|x|>0.175`, `z>-0.03`), then a thin sheath-axis scabbard `(0.22,1.00,−0.04)→(0.45,0.28,0.06)` r=0.042 (~761 verts / 519 tris). Earlier `x>0.26` heuristics stole the A-pose right arm (~4k verts) and exploded PASS L into a floating slab.
3. **Atlas combine.** `Image_0` + LionBack / LionFront into `sir_aldric_meshy_atlas.png` (2048×2688, lions in the top strip) with UVs remapped. Lion stays readable on CONTACT L.
4. **Hang vertex-rotate skipped.** A-pose→Play hang explode the scabbard/arms (paint-era finding). Polish later — not applied.
5. **Tabard** rides Hips as a rigid plate. No cloth.

Rebuild: `blender --background --python scripts/blender/skin_sir_aldric_meshy.py`

## Out (paused)

- Walk PASS / walk-with-look claim
- Play hub PNG swap (`SIR_ALDRIC_REAR_MASTER_LOCKED.png` stays)
- Polish (not blockers): neck groove, A-pose→Play hang, leftover gold edge specks

Motion from `5916447` **holds**.

## Honest gaps (walk NOT claimed)

- A-pose arm slabs on a hang clip — contralateral pendulum is in the keys, but the mesh does not hang, so arms do not read as the accepted motion silhouette.
- Tabard is a hip plate; it does not swing.
- Path 2 Meshy mid-poly, not a painted unique unwrap. Neck groove + gold edge specks remain from the look PASS stills.
- World-cam is high Play angle — not a beauty portrait cam.

**Walk-with-look gate is next.** Design re-eyes this clip. Do not merge on this tip.
