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

Runtime: `SirAldric3DActor` builds a **capsule-sculpted skinned mesh** (not cubes) with `01_rear_LOCKED` projective albedo and plays `Aldric_WalkAttackLoop` through **Animator + PlayableGraph**.

- Scene: `Assets/Survival/Scenes/SirAldric.unity` — Game view **1080×1920** → Play
- Camera is **fixed** high-angle rear; RootZ marches toward **TOP** (+Z). Thigh −X = toward TOP (rejects moonwalk). Arm/forearm −X = hands and blade on the far / TOP side (rejects hang-toward-camera).
- Motion SoT: `SirAldric3DMotion`
- Play hub still uses `SIR_ALDRIC_REAR_MASTER_LOCKED.png` until Design PASS
- 2D warp (`SirAldricView` / `SirAldricWarp`) is **quarantined / unused**
- Look targets on disk: `UNITY_3D_HANDOFF/look_targets/00_fullbody_LOCKED.png`, `01_rear_LOCKED.png`, `02_aldric_turnaround_orthos.png`

See `ART_UPGRADE.md` for the remaining DCC painted-mesh gap.

---

## PASS / FAIL (Design gate)

**PASS when all true:**
1. Same look as locked targets every frame (no redesign mid-clip)
2. Silhouette stays connected (no detached legs/arms)
3. Walk reads at phone size (clear stride, **bent passing knee**, not stiff-leg pivots)
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
