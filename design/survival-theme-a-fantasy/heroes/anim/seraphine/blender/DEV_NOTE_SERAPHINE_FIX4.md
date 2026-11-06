# Seraphine FIX4 — review candidate r2 (full cape, no mask)

Branch `seraphine-fix4`, base main `259608d691eafa96b571fd6abf1089798b642de4`. **This is not a merge approval. Desktop Unity has not been run by this lane.**

r2 replaces the first FIX4 candidate's cape mask, which removed 3,763 of 11,773 faces (31.96%). **The cape is now the original main asset, byte-identical, with 0 faces removed.**

## Staff fix (unchanged from r1)
- `NewCharsPreview.cs` instantiates `rig_rest` once and samples clips from the other FBXs. FIX3's per-clip staff mesh therefore never reached the visible prop. FIX4 uses one hand-space staff binding across rest and all four clips. Validation drives the imported ready-rest model with every imported clip, matching what Unity does.
- Strict full-triangle staff checks use all 43,266 body vertices, including fingers and forearms, with no hand exemption.
- Re-authored two-link right-arm arcs. The attacks are 2× duration: thrust 4.733 s, throw 4.400 s. Idle 9.933 s and walk 1.167 s are unchanged.

## Cape — real cause and fix (r2)
- An inspection of the r1 mask showed that every masked cape/body contact was with her **rigid braid**: hair weighted Head 0.2 / HeadTop_End 0.8, hanging to z≈0.98 m inside the cape's upper channel. No contacts were with the torso. In the attacks the neck/head turned up to **66° relative to the chest** (idle nodded up to 12.7°). That swung the braid up to 26 cm sideways through the Spine2-rigid (cape_root) upper cape. The same swing **also pushed the braid through both wings (thrust) and her own body (throw)**, a separate body-through-body violation that the cape mask had hidden.
- No cape bone or collider setting can solve this. The contacts are in cape_root-rigid geometry, and the preview's collider table cannot express a head capsule without new code.
- r2 fix: for each frame, the Neck+Head rotation relative to Spine2 is scaled to the largest fraction at which the braid clears the rigid cape by ≥1 cm and has zero overlap with wings and body. The scale is smoothed with a guaranteed-conservative filter (erosion, then box average, ×0.97) so there is no snapping. The resulting maximum head turn relative to the chest is about 9.8° in thrust (was 66.4°), 12.0° in throw (was 56.7°), 9.0° in idle (was 12.7°) and 9.0° in walk (was 9.3°). **The visible tradeoff is that she now looks along her chest line in the attacks instead of turning her head far toward the target.** See `SERAPHINE_FIX4_HEAD_BRAID_BEFORE_AFTER.jpg`.
- The lower head turn would have put her crown in the throw wind-up's staff path (f22–35). A smooth two-link elbow extension fixes that. The hand moves up to 4.5 cm away from the head in f14–40, the elbow goes 84°→155°→143° with no lock-out, and hand world orientation is unchanged. Staff→head/neck/hair is ≥3.0 cm on every frame of every clip, and staff→non-hand minimum in throw is 3.71 cm.
- The cape FBX was restored byte-identical from main. r1's cape export had also baked a posed Spine2 Child-Of offset, about 4 cm, into the cape asset. That is gone with the restore.

## Measured gates (all on final assets; JSON under `sheets/fix4/validation/`)
| Clip | Frames | Cape∩body (incl. hair) | Cape∩staff | Cape∩wings | Braid∩wings/body | Staff∩body incl. fingers | Staff→non-hand min | Staff→wing min | Cape-body min |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| idle | 299 | 0 | 0 | 0 | 0/0 | 0 | 5.13 cm | 31.10 cm | 0.72 cm |
| walk | 36 | 0 | 0 | 0 | 0/0 | 0 | 5.13 cm | 31.84 cm | 0.88 cm |
| attack_thrust | 143 | 0 | 0 | 0 | 0/0 | 0 | 5.14 cm | 1.01 cm | 0.86 cm |
| attack_throw | 133 | 0 | 0 | 0 | 0/0 | 0 | 3.71 cm | 3.29 cm | 1.00 cm |

- The Unity-pattern check (imported ready-rest mesh driven by each imported clip) reports 0 staff/body and 0 staff/wing on all 611 frames. The rest FBX also passes.
- Wrists: max hand world step is 6.01°/frame in thrust and 7.38°/frame in throw. Max relative wrist change is 1.04°/frame in thrust and 3.54°/frame in throw. The staff sits about 1 mm off the palm/fingers (open supporting grip, unchanged).
- Skeleton: 24 bones, parents and every bind matrix match main exactly (max diff 0). The body mesh vertices and weights also match main exactly.

## Remaining limitations
- The head-turn reduction is a deliberate motion change. Review it in the before/after sheet and the MP4s.
- The fingers are an open supporting grip, not a closed fist, as in r1.
- Desktop Unity playback, crossfades and the runtime cloth solver were not exercised. These are Blender spring results.

Reproduction scripts live on the build box under `/workspace/rigjob/sera_fix5/`: `headsearch.py`, `headapply.py`, `armik.py`, `capesim5.py`, `runall.sh`, `export5.py` and `render5.py`.
