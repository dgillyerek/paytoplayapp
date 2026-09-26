# CHUNKY LOOK HANDOFF — Sir Aldric SEP painted

**Generated:** 2026-09-26 ~08:30 EDT  
**Package:** `AUTO_RIG_PATH/out/MESHY_GEN_SEPARATE_SWORD/`  
**Trigger:** Derek Play FAIL at tip `948adbc` — "still looks all chunky" on painted midpoly + atlas wire.  
**Ownership:** Design owns look re-gate. Soft leftovers OK only if silhouette + paint + walk; **chunky = FAIL**.  
**Not done:** Path A, weight-paint, overwrite of mid200k / AccuRIG / Animate FBXs. No user/Dev ping.

---

## 1. Evidence compared

| Source | What was read | Face / map notes |
|---|---|---|
| Full painted | `stills/body_painted_viewport.png` (Meshy) | **2,137,452** faces — smooth plate curves, crisp gold trim, soft cape silhouette |
| mid200k Design still | `stills/body_painted_midpoly_viewport.png` | **200,000** faces — visible faceting on helmet dome, pauldron outer curves, cape hem stair-steps; specular breaks on flat tris |
| Unity Play-cam | `cloud-agent-artifacts/bc-b81d1a4a-…/sir_aldric_sep_paint_{front,34,rear}_walk_playcam.png` | Same mid200k under play lighting — blocky specular on plates + faceted cape folds = Derek "chunky" |
| Atlas / PBR | `assets/textures/sep_body_{basecolor,normal,metallic,roughness}.png` | **2048²** each; lion embroidery and plate paint still readable in playcam |

**Silhouette / plate-edge read:** Full painted is soft. mid200k + Unity playcam both show polygonal stepping on curved plates and cape — geometry density, not missing paint.

---

## 2. Hypotheses ranked

| Rank | Hypothesis | Verdict |
|---:|---|---|
| **1 (primary)** | **(a) Decimate too aggressive / faceted plates** — 2.1M → 200k (~9.4% retain) | **LIKELY.** Faceting on helmet crown, pauldrons, greaves; cape hem stair-steps; specular highlights break on flat tris in playcam. Matches FAIL. |
| 2 | **(b) Atlas resolution / Unity compression** | Unlikely primary. Maps are 2048²; embroidery/lion crest remain sharp in playcam. Compression may soften micro-detail but does not create plate faceting. |
| 3 | **(c) Missing normal/roughness punch** | Secondary only. Normals present; they cannot hide large-scale silhouette/specular faceting from under-density. Punch alone will not clear chunky gate. |
| 4 | **(d) AccuRIG bind mesh is clay topology receiving stamp poorly** | Out of scope for this Design package (would be Path A / rebind). Paint is bound onto midpoly atlas wire; chunky reads on the painted mid mesh itself in Design stills *before* Unity bind nuances. |
| 5 | **(e) Lighting** | Contributing amplifier (studio key reveals flat faces) but not root cause — same mesh looks faceted in Design Eevee still. |

---

## 3. Design fix produced (density path)

Method: Blender 4.2.3 **Decimate → COLLAPSE** (edge collapse; **no** voxel / remesh / melt). UVs kept. Pose/scale of painted source preserved (same class as mid200k paint pass). Script: `prep/make_painted_mid450k.py`. Stats: `paint_mid450k_stats.json`.

| Asset | Output | Faces before → after | UV / mats |
|---|---|---:|---|
| Painted body | `assets/SirAldric_SEP_body_nosword_PAINTED_mid450k.{glb,fbx}` | 2,137,452 → **449,999** (~21% retain) | 1 UV layer, 1 material; GLB embeds PBR |
| Painted sword + scabbard | `assets/SirAldric_SEP_sword_scabbard_PAINTED_mid80k.{glb,fbx}` | 247,420 → **80,000** (~32% retain) | 1 UV layer, 1 material; GLB embeds PBR |

**Stills**
- `stills/body_painted_mid450k_viewport.png`
- `stills/sword_painted_mid80k_{front,34,rear}.png`

**Protected (verified unchanged)**
- `assets/SirAldric_SEP_body_nosword_PAINTED_mid200k.{glb,fbx}`
- `assets/SirAldric_SEP_sword_scabbard_PAINTED_mid.*` (50k)
- AccuRIG FBX, `meshy_animate_*.fbx`

Residual soft faceting may still appear at extreme close-up on helmet dome; that is acceptable soft leftover if silhouette + paint + walk pass. mid450k roughly **2.25×** body density vs mid200k specifically to clear plate-curve / cape-fold specular chunk.

---

## 4. Exact next ask

**For parent → Dev (recommended):** Drop **mid450k body + mid80k sword** into the Unity paint wire in place of mid200k / mid50k. Re-run Play-cam walk proofs (`front` / `34` / `rear`). Design re-gates on silhouette softness + plate edge readability; soft leftovers OK, chunky FAIL.

**Do not** treat as punch-only or Path A. If mid450k still reads chunky after Dev drop, escalate density again (~700k) or revisit bind-mesh stamp — not atlas punch alone.

**Dev notes (non-blocking):** Prefer GLB when full metallic/roughness set needed; FBX carries mesh + embedded images. Same atlas paths under `assets/textures/sep_body_*` and `sword_*`. Parent scabbard at character-RIGHT hip unchanged.
