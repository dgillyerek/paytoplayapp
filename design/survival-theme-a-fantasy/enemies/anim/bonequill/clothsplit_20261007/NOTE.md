# BONEQUILL: cloth split (2026-10-07)

## Overview

New sibling pack. The current packs (`blender_rig/`, `mixamo/`, `attack_20261007/`) were **not** modified; their MD5s were verified before and after.

| | |
|---|---|
| Rig source | `/workspace/design/survival-theme-a-fantasy/enemies/anim/bonequill/blender_rig/BONEQUILL_blenderig.blend` (22 mixamorig body bones, unchanged) |
| Attack source | `/workspace/design/survival-theme-a-fantasy/enemies/anim/bonequill/attack_20261007/BONEQUILL_attack_work.blend` |

**Meshes**
- `BONEQUILL_body`: 170182 faces. Cloth faces were removed and the openings closed with real triangles; boundary edges after fill: 49 (pre-existing non-manifold edges from the source remesh).
- `BONEQUILL_cloth`: 31310 faces, the loose cloth below z = 0.906 m.
- Selection: geometric. The legs are found by horizontal section loops, and everything below the seam that is not inside a leg/foot/hand tube becomes cloth.
- Same material and UVs as the source.

**Body weights**
- Leg-bone weight was moved to Hips on 18591 torso/non-leg verts (above the crotch, z 1.066, plus the non-leg band at the seam).
- 9094 seam verts were blended toward Spine.
- Torso verts with leg weight after the fix: 0.
- Leg tubes keep their original weights.

## Cloth chains

All chains are parented to `mixamorig:Spine` (the cloth starts at hip height). Only bone `_01` of each chain shares weight with Spine (a ring of the top 35%); the rest is chain-only, with at most 4 influences.

Sector letters: F = front (−Y), L = character-left (+X), B = back.

Suggested spring settings are a starting point for a Unity SpringBone / DynamicBone / MagicaCloth BoneSpring-style setup, all 0–1.

| chain | sector | bones | parent | length m | verts | stiffness | damping | drag | gravity | radius m |
|---|---|---|---|---|---|---|---|---|---|---|
| skirt_F | F | skirt_F_01, skirt_F_02, skirt_F_03 | mixamorig:Spine | 0.57 | 1741 | 0.35 | 0.4 | 0.45 | 0.4 | 0.05 |
| skirt_FL | FL | skirt_FL_01, skirt_FL_02, skirt_FL_03 | mixamorig:Spine | 0.44 | 953 | 0.35 | 0.4 | 0.45 | 0.4 | 0.05 |
| skirt_L | L | skirt_L_01, skirt_L_02, skirt_L_03, skirt_L_04 | mixamorig:Spine | 0.77 | 1938 | 0.25 | 0.35 | 0.4 | 0.6 | 0.06 |
| robe_BL | BL | robe_BL_01, robe_BL_02, robe_BL_03, robe_BL_04 | mixamorig:Spine | 0.80 | 1545 | 0.25 | 0.35 | 0.4 | 0.6 | 0.06 |
| robe_B | B | robe_B_01, robe_B_02, robe_B_03, robe_B_04 | mixamorig:Spine | 0.85 | 1582 | 0.25 | 0.35 | 0.4 | 0.6 | 0.06 |
| robe_BR | BR | robe_BR_01, robe_BR_02, robe_BR_03 | mixamorig:Spine | 0.75 | 4272 | 0.25 | 0.35 | 0.4 | 0.6 | 0.06 |
| skirt_R | R | skirt_R_01, skirt_R_02, skirt_R_03 | mixamorig:Spine | 0.59 | 2111 | 0.35 | 0.4 | 0.45 | 0.4 | 0.05 |
| skirt_FR | FR | skirt_FR_01, skirt_FR_02, skirt_FR_03 | mixamorig:Spine | 0.56 | 1502 | 0.35 | 0.4 | 0.45 | 0.4 | 0.05 |

- **Colliders:** capsules on mixamorig:LeftUpLeg/LeftLeg/RightUpLeg/RightLeg (r ≈ 0.07 thigh, 0.055 shin) and a sphere on Hips (r ≈ 0.15).
- **Limits:** cap the angle at about 60° per joint for the front strips and about 75° for the back/cape strips.
- **Inertia:** lower the gravity on front strips if they clip the thighs during the walk; raise the root stiffness if the attack swing flares too much.

## Animation

- Walk and attack were baked with the **body bones only**; fcurves for non-body bones were removed.
- Cloth bones sit exactly at rest on every frame (keyed constant by the full-bone bake).
- Export settings match the existing packs:
  - rest: hum profile, no animation.
  - walk/attack: rebake profile (bake all bones, force start/end keys, step 1, simplify 0, axis −Z/Y, embedded textures).

## QC (reimport of each FBX)

| fbx | bones | body22 present / rel. order | cloth verts w/ leg weight | torso verts w/ leg weight | unweighted body/cloth | frames | cloth bone max dev m/deg | body motion vs original FBX m/deg |
|---|---|---|---|---|---|---|---|---|
| rest | 49 | True / True | 0 | 0 | 0/0 | - | - / - | - / - |
| walk | 49 | True / True | 0 | 0 | 0/0 | [1, 31] | 6.260785817561437e-07 / 0.0 | 4.300734308261219e-07 / 0.0 |
| attack | 49 | True / True | 0 | 0 | 0/0 | [1, 31] | 7.79574296446343e-07 / 0.0 | 3.6378370570342845e-07 / 0.0 |

- Bone order: the 22 body bones keep their relative order, but they are **not** the first 22 indices. Blender writes bones by hierarchy, so the cloth chains (children of Spine) are interleaved after Spine's subtree. Use name-based mapping (Unity Humanoid / Avatar does this).
- Cloth mesh vertex groups: Spine plus cloth bones only; no leg groups.

## Caveats
- **Upper robe band stays on the body.** The split line is at 0.476H (z 0.906 m). The upper skirt/robe band from 0.476H to 0.55H (hip belt area) stays on the body mesh, and only the hanging part below is cloth.
- **Left hand now rigid on Hips.** The source rig had the left hand/quiver-side fingers weighted to LeftUpLeg; the left-arm bones sit misplaced in the quiver. That weight was moved to Hips with the rest of the torso. The hand is rigid to Hips, as it effectively was before, but now no longer follows the thigh.
- **Fixed cloth parents.** All chains start at hip height and are parented to Spine.
- **Leg pass-through without simulation.** With the cloth at rest, the legs pass through the front robe panels during the walk. Use the spring setup with leg colliders in-engine.


## Files

- `BONEQUILL_clothsplit.blend`, `BONEQUILL_clothsplit.fbx` (rest), `BONEQUILL_clothsplit_walk.fbx`, `BONEQUILL_clothsplit_attack.fbx`
- `stills/`: rest stills with the cloth hidden and shown (front/q34/side/rear) plus `BONEQUILL_clothsplit_rest_compare.jpg`
- `BONEQUILL_clothsplit_walk_contact.jpg` (rear + 3/4, every 3rd frame)
- `BONEQUILL_clothsplit_walk_preview_rear.mp4`
- `work/`: build_meta.json, qc.json, scripts
