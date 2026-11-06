# Seraphine FIX4 — LOCAL REVIEW CANDIDATE

Started from fetched `origin/main` `259608d691eafa96b571fd6abf1089798b642de4` on branch `seraphine-fix4`.
**Not a merge approval. Desktop Unity has not been run by this lane.**

## Actual root causes

`Assets/Survival/Unity/NewCharsPreview.cs:137–157` instantiates the `rig_rest` FBX once, then loads animation clips from the other FBXs. The thrust-specific staff geometry in FIX3 therefore did not correct the prop mesh that the preview actually renders. FIX4 uses the same hand-space staff geometry in rest, idle, walk and both attacks. The reimport validation explicitly drives the imported ready-rest model with all four imported clips.

FIX3's validator excluded all RightHand-dominant geometry and used spear-edge rays. A strict full-triangle test found gripping-hand and forearm intersections that it missed. FIX4 uses all 43,266 body vertices and all 15,900 staff vertices, including the thick head and end ornaments. No hand or forearm intersection exemption is applied.

## Motion

Controlled two-link right-arm poses replace the accumulated correction/spin keys. The staff is aligned continuously to the hand. It returns upright while the hand is still forward, then the hand withdraws; reversing that order swept the tail through a wing. The free arm balances in front, instead of passing behind through the cape. The original body mesh, fingers, leg/spine animation and bind skeleton are retained. The attacks alone are retimed uniformly to 2× duration.

| Clip | Frames @ 30 fps | FBX duration | Staff→body min incl fingers | Staff→non-hand min | Staff→wing min |
|---|---:|---:|---:|---:|---:|
| idle | 299 | 9.933 s | 0.109 cm | 5.131 cm | 31.100 cm |
| walk | 36 | 1.167 s | 0.109 cm | 5.132 cm | 31.840 cm |
| attack_thrust | 143 | 4.733 s | 0.109 cm | 5.140 cm | 1.012 cm |
| attack_throw | 133 | 4.400 s | 0.106 cm | 3.752 cm | 3.288 cm |

All 611 animated frames plus the ready-rest FBX pass staff/body and staff/wing full-triangle checks. This includes palm, fingers, forearms, mouth/head, shoulders, torso, hips and legs. Reimported shared-rest-mesh reports are under the `validation` folder. Maximum hand world-orientation step is 6.007°/frame thrust and 7.378°/frame throw; wrist twist is effectively constant (floating-point noise below 0.00003°/frame). Full relative-wrist quaternion metrics are included separately, not inferred from Euler angles.

The 24 body bones, parent hierarchy and every bind matrix are identical to the original main asset (maximum matrix difference exactly 0). The original approved fingerprint was `d5611ac70629687b`. The ready-rest FBX is saved in a nonanimated idle pose; its actual bind matrices also compare exactly to the prior rest FBX. The body/finger mesh coordinates compare exactly to main (maximum vertex difference 0).

## Cape — significant review tradeoff

Fresh v5 spring simulations use the final motion and actual cape mesh, not FIX3 caches. The original cape had hair intersections even after the free arm was corrected. Offset experiments either worsened hair contact or crossed the wings and were rejected. A broad collar opening was also rejected in favor of a conservative mask based only on faces/vertices implicated by fresh hair/body contacts, plus one adjacent face ring.

**3,763 of 11,773 cape faces (31.96%) are removed, primarily in the hair-covered upper collar. This is a substantial geometry change and needs visual approval.** The cape's original rig, weights for retained vertices, Spine2-equal root, attachment, texture/UVs and collider settings are retained. It is not silently described as an unchanged cape.

Fresh every-frame triangle checks: 0 cape/body, 0 cape/staff and 0 cape/wing intersections for all four clips. The conservative back-ray check also reports 0 body penetrations. Its minimum body margins are idle 0.625 cm, walk 0.877 cm, thrust 0.244 cm, throw 5.632 cm. These are Blender spring results, not a guarantee about Unity's separate runtime solver.

## Remaining limitations / honest review gate

- The retained finger geometry is an **open supporting grip, not a closed fist**. The staff is tangent to the actual fingers/palm (~1 mm gap), but the fingers do not wrap around it. Finger-curl mesh experiments looked worse and were rejected. No finger bones were added to Derek's skeleton.
- Review the substantial cape mask in the rear sheet. Reject this candidate if that cape shape is not acceptable; do not hide the tradeoff behind zero-intersection metrics.
- Desktop Unity playback, imported clip ranges, transitions/crossfades and the runtime cloth solver have not been exercised here. Reimport all six Seraphine FBXs on desktop and inspect the full new 143/133-frame attacks before approval.
- No Unity code/scenes or other characters were changed.

## Proof assets

`design/survival-theme-a-fantasy/new_2026-11-03/seraphine/sheets/fix4/` contains four front/side MP4s, overview and rear sheets, every-frame front/side contact pages, and full per-frame validation JSON. Videos include one displayed endpoint frame; the table gives FBX timeline durations, `(frames-1)/30`.

Reproduction scripts and raw fresh cape dumps remain under `/workspace/rigjob/sera_fix4/`; progress is `/workspace/rigjob/PROGRESS_sera_fix4.md`.
