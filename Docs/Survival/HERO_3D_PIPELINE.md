# Theme A hero motion — 3D + Unity pipeline (LOCKED)

**Derek LOCK 2026-09-18:** Heroes = rigged **3D model + Unity Animator**. High-angle rear camera.  
**Stopped:** AI multi-frame sprites, paper-doll cutouts, mesh-warp of painted stills.

Pilot hero: **Sir Aldric**. Same pipeline for later heroes once Aldric walk+attack PASSes.

---

## Ownership

| Role | Delivers |
|------|----------|
| **Design** | Look lock (concept / fullbody / squared rear SoT), turnaround orthos, material notes, PASS/FAIL on Game-view motion |
| **Dev** | Model (or source), rig, Animator clips, scene camera, Game-view proof clip for Derek |

Runtime anim asset is **3D**, not the PNG SoT. PNGs are **visual targets / placeholders** only.

---

## Design look targets (Aldric)

Path: `heroes/anim/sir_aldric/UNITY_3D_HANDOFF/look_targets/`

- `00_fullbody_LOCKED.png` — roster fullbody
- `01_rear_LOCKED.png` — squared high-angle rear (Derek PASS still)
- `02_aldric_turnaround_orthos.png` — front / left / right / rear orthos

### Must match
- Silver plate + gold trim (helm crest, pauldrons, gauntlets, greaves, boots)
- Royal-blue short surcoat/cape: **gold lion rampant** + **gold Greek-key hem**
- Sword sheathed on **character-right** hip while idle/walk (brown scabbard, gold fittings)
- Boots silver+gold (never brown leather)
- Proportions: readable at phone Game-view (~1080×1920), hero fills similar scale to current Play mock

Turnaround rear panel was mirrored vs `01_rear_LOCKED` (scabbard). **SoT = character-right hip** as in the locked rear still (viewer-right from behind).

---

## Dev build (Aldric pilot)

1. **Model** — stylized mid-poly OK for mobile; one texture set matching look targets.
2. **Rig** — humanoid (Unity Humanoid or custom); cape can be skinned or simple secondary bones.
3. **Camera** — high-angle rear; character faces / moves toward **TOP of screen** (enemy = top). Same framing intent as locked rear SoT.
4. **Clips (minimum for first re-gate)**
   - `Walk` — readable stride toward TOP
   - `Attack` — draw → strike toward TOP → recover / re-sheath (no snap)
   - Loop: walk → attack → walk
5. **Proof** — Editor/Game-view 1080×1920 clip (gif/mp4) + still. Draft PR OK; **do not merge** until Design/Derek PASS.
6. **Do not** ship hard-cut 2D limb puppets or soft-warp of the PNG as the hero solution.

Placeholder: keep showing locked rear PNG in Play until 3D clip PASSes.

---

## This PR (orientation + look)

Runtime: `SirAldric3DActor` binds **Path 2 Meshy** (`sir_aldric_meshy.mesh.txt` + `sir_aldric_meshy_atlas.png`) and plays `Aldric_WalkAttackLoop` through **Animator + PlayableGraph**. Loft mid-poly is archive. `Evaluate()` keys from `5916447` are unchanged.

- Scene: `Assets/Survival/Scenes/SirAldric.unity` — Game view **1080×1920** → Play
- Camera is **fixed** high-angle rear `(0, 2.80, −5.40)` look-at `(0, 0.90, 0.50)`; RootZ marches toward **TOP** (+Z). Root X = 0. Spine Z counters hip roll so the torso stays in one vertical plane (Derek CLOSE on ff81201: no left↔right weave). Small hip drop + shoulder–hip counter-rotate stay. **Passing** foot tucks under the pelvis then steps **+Z / TOP** (14d9c17 pass thigh +X read as a back-kick toward camera). Contact step matches march 0.40 m. **5916447 motion HOLD.**
- **Look:** Path 2 Meshy GLB / `e5b132f` paint PASS (Image_0 + lion cards). UVs kept; no remesh/capsule Game-view. Scabbard character-right. **Look PASS claimed** (Design, paint stills). **Walk-with-look NOT claimed.**
- Walk is **4 Game-view keys** in `SirAldric3DMotion` solved against `refs/WALK_GAIT_BAR_skeleton_sample.mp4` rear phases. BVH eulers reached the Actor but did **not** transfer the gait; do not re-claim a BVH eye-match. Sword / right hand stay on the far / TOP side as a rest; **walk** uses a loose contralateral pendulum (trailing arm may swing +X toward camera — that is the sample, not a FAIL).
- Motion SoT: `SirAldric3DMotion` (`Evaluate` is what `BuildLoopClip` samples)
- Play hub still uses `SIR_ALDRIC_REAR_MASTER_LOCKED.png` until Design PASS
- 2D warp (`SirAldricView` / `SirAldricWarp`) is **quarantined / unused**
- Look targets on disk: `UNITY_3D_HANDOFF/look_targets/00_fullbody_LOCKED.png`, `01_rear_LOCKED.png`, `02_aldric_turnaround_orthos.png`
- Rebuild Gate 3 game mesh: `blender --background --python scripts/blender/build_sir_aldric_gate3.py`
- World-cam stills: `python3 scripts/blender/render_gate3_playcam.py`

See `ART_UPGRADE.md` for the remaining painted-unwrap gap. **Do not claim Design eye PASS** unless Derek can scrub sample and Aldric and recognize the same gait.

---

## Gate 2 BLOCKOUT

Look law Gate 1 is **LOCKED** (`TURNAROUND_GATE1/LOCKED/` 01–05 + sheet; `look_targets/00_portrait_COMPLETE_LOCKED.png` = face law).  
Grey-clay blockout: `sir_aldric_blockout.blend` + stills vs turnaround. Volume law = `5de0e16`. CEO Gate 2 was a **soft-PASS** on silhouette (Derek moved on without FAIL).

---

## Gate 3 GAME MESH (this pass)

**Path 2 Meshy** look PASSed on `e5b132f`. Remesh/capsule Game-view (`61e023d` / `c840b73`) STOP'd — threw away paint PASS.  
Runtime: Meshy-look hang-skin FMT v4 `sir_aldric_meshy.mesh.txt` + DCC `sir_aldric_path2_clean.fbx` (Meshy mesh, not capsules). Actor `Bone4`.  
World-cam clip: `Docs/Survival/previews/gate3/sir_aldric_path2_walk_toward_top.mp4`. vs paint PASS: `gate3_meshy_hang_vs_e5b132f.png`.  
Hard lock: scabbard character-RIGHT. Motion `5916447` holds.  
**Paused:** Play hub swap. **Bind NOT claimed. Walk-with-look NOT claimed.**

---

## PASS / FAIL (Design gate)

**PASS when all true:**
1. Same look as locked targets every frame (no redesign mid-clip)
2. Silhouette stays connected (no detached legs/arms)
3. Walk tracks `refs/WALK_GAIT_BAR_skeleton_sample.mp4`: pass knee ~65°, hip drop, shoulder–hip counter-rotate, heel lift, contralateral arms (not stiff-leg, not 90° cartoon)
4. Attack strikes toward TOP; draw + recover visible
5. Orientation: squared rear / toward TOP (not sideways 3/4 drift)
6. Arms and held sword sit on the **far / TOP** side of the body (toward the enemy), not hanging toward the camera
7. **One** brown scabbard on the **character-right hip** only — no back-mounted sheath / second sword tube

**FAIL if:** limb pop, rubber melt, look drift, attack toward camera, or still/placeholder labeled as motion.

---

## Stop doing

- Regenerating AI walk frame packs
- Paper-doll or mesh-warp of `01_rear_LOCKED.png` as production motion
- Merging PR sprite-anim experiments as the hero system
