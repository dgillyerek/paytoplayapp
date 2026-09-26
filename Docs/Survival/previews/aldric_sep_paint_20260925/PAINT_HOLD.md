# HOLD — Sir Aldric SEP painted look rewire (2026-09-26)

**HOLD merge on PR #27 until Derek Play + Design re-gate.** No Design PASS on this look wire. Path A CANCELLED. Attack is **not** claimed fixed in this folder. Attack clip iterate 2026-09-26: yawlock SoT lives in `../aldric_sep_attack_yawlock_20260926/`. Look density iterate 2026-09-26: mid450k / mid80k SoT lives in `../aldric_sep_paint_mid450k_20260926/`.

## SoT

| Item | Status |
| --- | --- |
| Walk motion | `SirAldric_SEP_meshy_animate_walk.fbx` Walking take on Mixamo Humanoid. Unchanged from `c88ea55` Derek Play PASS. |
| Look | Painted midpoly atlas (`sep_paint/sep_body_*`) UV-stamped onto that AccuRIG. `LookSwordScabbard` parented at character-RIGHT `RightUpperLeg` (thin-lateral hang). Walk RH empty; measured min gap **> 0.10 m** on Walking 1–26. No hip-grip glue. |
| Attack | `SirAldric_SEP_meshy_animate_attack.fbx` left loaded. **not SoT.** Native clip still spins toward camera. Do not AimChain / ClipSword. |

Actor (`SirAldricMeshyAnimateActor`) instantiates the walk FBX, yaws **180°** (Mixamo face −Z → world +Z / TOP), binds painted maps, parents the painted sword+scabbard at **RightUpperLeg** (not RightHand, not a hip-grip). Design FAIL on tip `2876356` was RH melt into the wide product-shot pair; clearance iterate rotated the pair-width along forward and dropped the prop onto the thigh. Attack playable stays leftover.

## Imported look drop (alongside walk FBX)

- `SirAldric_SEP_body_nosword_PAINTED_mid200k.fbx` + `.glb` — static A-pose, 200k faces, no rig. UV source only.
- `SirAldric_SEP_sword_scabbard_PAINTED_mid.fbx` + `.glb` — look prop.
- `sep_paint/sep_body_*.jpg` + `sword_*.jpg` — JPG stand-ins of Design PNGs.
- `sep_paint/sir_aldric_sep_walk_look_uv.bin` — per-vertex UV sidecar (99946 verts) for Unity if the walk mesh is readable.

Walk FBX is **not** overwritten. Painted 145k-vert body is a different mesh — not swapped onto walk weights (that would be Path A / retopo-bind).

## Discarded as SoT

- Path A weight-paint / hang-skin / retopo-bind
- ClipSword / AimChain
- Old fused walk atlas bind
- Treating the spinning attack clip as fixed

## Proofs in this folder

Play-cam remap (Unity Y-up SoT: eye `(0, 2.80, −5.40)` LookAt `(0, 0.90, 0.50)` FOV 30, 1080×1920). Blender Z-up remap `(x, −z, y)` so this Mixamo FBX's cape-back matches Unity rear after RearYaw 180. **Not Meshy website stills. Not Unity Game-view** (no Editor on this VM).

- `sir_aldric_sep_paint_rear_walk_playcam.png`
- `sir_aldric_sep_paint_front_walk_playcam.png`
- `sir_aldric_sep_paint_34_walk_playcam.png`
- `sir_aldric_sep_paint_walk_juice_playcam.mp4`

Unity Game-view capturer writes `*_gameview.*` into this folder when Derek Plays (`Survival → Capture Sir Aldric SEP painted walk` or `-aldric-capture`).

Attack juice is **not** re-proofed here. Clay native-spin FAIL remains in `../aldric_sep_20260925/`.

## Unity Play repro

1. Unity 6.3 LTS. Play **SirAldric**. Rear cam as above. Enemy cube = TOP / +Z.
2. Walk: painted blue / gold / silver (not clay). Scabbard on character-RIGHT hip. **Open air between RH and scabbard** for the full cycle (no melt / no grip).
3. Attack: leftover SEP clip, still expected to spin. Not a look-rewire bug.
4. HOLD until Derek Play PASS. Design re-gates after.
