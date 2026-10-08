# VESPERA: cloth split (2026-10-07)

## Overview

New sibling pack. The current packs (`blender_rig/`, `mixamo/`, `attack_20261007/`) were **not** modified; their MD5s were verified before and after.

| | |
|---|---|
| Rig source | `/workspace/design/survival-theme-a-fantasy/enemies/anim/vespera/blender_rig/VESPERA_blenderig.blend` (22 mixamorig body bones, unchanged) |
| Attack source | `/workspace/design/survival-theme-a-fantasy/enemies/anim/vespera/attack_20261007/VESPERA_attack_work.blend` |

**Meshes**
- `VESPERA_body`: 144569 faces. Cloth faces were removed and the openings closed with real triangles; boundary edges after fill: 40 (pre-existing non-manifold edges from the source remesh).
- `VESPERA_cloth`: 56483 faces, the loose cloth below z = 0.906 m.
- Selection: geometric. The legs are found by horizontal section loops, and everything below the seam that is not inside a leg/foot/hand tube becomes cloth.
- Same material and UVs as the source.

**Body weights**
- Leg-bone weight was moved to Hips on 6008 torso/non-leg verts (above the crotch, z 1.046, plus the non-leg band at the seam).
- 4311 seam verts were blended toward Spine.
- Torso verts with leg weight after the fix: 0.
- Leg tubes keep their original weights.

## Cloth chains

All chains are parented to `mixamorig:Spine` (the cloth starts at hip height). Only bone `_01` of each chain shares weight with Spine (a ring of the top 35%); the rest is chain-only, with at most 4 influences.

Sector letters: F = front (−Y), L = character-left (+X), B = back.

Suggested spring settings are a starting point for a Unity SpringBone / DynamicBone / MagicaCloth BoneSpring-style setup, all 0–1.

| chain | sector | bones | parent | length m | verts | stiffness | damping | drag | gravity | radius m |
|---|---|---|---|---|---|---|---|---|---|---|
| skirt_F | F | skirt_F_01, skirt_F_02, skirt_F_03 | mixamorig:Spine | 0.48 | 780 | 0.35 | 0.4 | 0.45 | 0.4 | 0.05 |
| skirt_FL | FL | skirt_FL_01, skirt_FL_02, skirt_FL_03 | mixamorig:Spine | 0.61 | 1163 | 0.35 | 0.4 | 0.45 | 0.4 | 0.05 |
| skirt_L | L | skirt_L_01, skirt_L_02, skirt_L_03, skirt_L_04 | mixamorig:Spine | 0.90 | 7166 | 0.25 | 0.35 | 0.4 | 0.6 | 0.06 |
| cape_L | BL | cape_L_01, cape_L_02, cape_L_03, cape_L_04 | mixamorig:Spine | 0.89 | 5516 | 0.25 | 0.35 | 0.4 | 0.6 | 0.06 |
| cape_C | B | cape_C_01, cape_C_02, cape_C_03, cape_C_04 | mixamorig:Spine | 0.84 | 5338 | 0.25 | 0.35 | 0.4 | 0.6 | 0.06 |
| cape_R | BR | cape_R_01, cape_R_02, cape_R_03, cape_R_04 | mixamorig:Spine | 0.88 | 5252 | 0.25 | 0.35 | 0.4 | 0.6 | 0.06 |
| skirt_R | R | skirt_R_01, skirt_R_02, skirt_R_03 | mixamorig:Spine | 0.76 | 1838 | 0.25 | 0.35 | 0.4 | 0.6 | 0.06 |
| skirt_FR | FR | skirt_FR_01, skirt_FR_02, skirt_FR_03 | mixamorig:Spine | 0.61 | 1097 | 0.35 | 0.4 | 0.45 | 0.4 | 0.05 |

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
| rest | 50 | True / True | 0 | 0 | 0/0 | - | - / - | - / - |
| walk | 50 | True / True | 0 | 0 | 0/0 | [1, 31] | 5.993087904027822e-07 / 0.0 | 2.764757769596498e-07 / 0.0 |
| attack | 50 | True / True | 0 | 0 | 0/0 | [1, 31] | 6.712555508479016e-07 / 0.0 | 4.933162140025749e-07 / 0.0 |

- Bone order: the 22 body bones keep their relative order, but they are **not** the first 22 indices. Blender writes bones by hierarchy, so the cloth chains (children of Spine) are interleaved after Spine's subtree. Use name-based mapping (Unity Humanoid / Avatar does this).
- Cloth mesh vertex groups: Spine plus cloth bones only; no leg groups.

## Caveats
- **Upper cloth stays on the body.** The split line is at 0.477H (z 0.906 m), so the upper cape and skirt above the hips (shoulders to waist) stay part of the body mesh and follow Spine/shoulders. Only the hanging part below the hips is simulated. In the body-only stills the upper cape reads as a short capelet.
- **Fixed cloth parents.** All cape/skirt chains start at hip height and are parented to Spine, not to the shoulders.
- **Leg pass-through without simulation.** With the cloth at rest, the legs show through the front skirt strips during the walk (3/4 view). Use the spring setup with leg colliders in-engine.
- **Torso weights changed.** Body weights on the torso and seam band were changed (leg influence moved to Hips/Spine). The leg tubes are unchanged.


## Files

- `VESPERA_clothsplit.blend`, `VESPERA_clothsplit.fbx` (rest), `VESPERA_clothsplit_walk.fbx`, `VESPERA_clothsplit_attack.fbx`
- `stills/`: rest stills with the cloth hidden and shown (front/q34/side/rear) plus `VESPERA_clothsplit_rest_compare.jpg`
- `VESPERA_clothsplit_walk_contact.jpg` (rear + 3/4, every 3rd frame)
- `VESPERA_clothsplit_walk_preview_rear.mp4`
- `work/`: build_meta.json, qc.json, scripts
