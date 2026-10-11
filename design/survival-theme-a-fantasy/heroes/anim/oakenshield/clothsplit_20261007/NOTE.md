# OAKENSHIELD: cloth split (2026-10-07)

## Overview

New sibling pack. The current packs (`blender_rig/`, `mixamo/`, `attack_20261007/`) were **not** modified; their MD5s were verified before and after.

| | |
|---|---|
| Rig source | `/workspace/design/survival-theme-a-fantasy/heroes/anim/oakenshield/blender_rig/OAKENSHIELD_blenderig.blend` (22 mixamorig body bones, unchanged) |
| Attack source | `/workspace/design/survival-theme-a-fantasy/heroes/anim/oakenshield/attack_20261007/OAKENSHIELD_attack_work.blend` |

**Meshes**
- `OAKENSHIELD_body`: 182615 faces. Cloth faces were removed and the openings closed with real triangles; boundary edges after fill: 62 (pre-existing non-manifold edges from the source remesh).
- `OAKENSHIELD_cloth`: 18391 faces, the loose cloth below z = 0.816 m.
- Selection: geometric. The legs are found by horizontal section loops, and everything below the seam that is not inside a leg/foot/hand tube becomes cloth.
- Same material and UVs as the source.

**Body weights**
- Leg-bone weight was moved to Hips on 17680 torso/non-leg verts (above the crotch, z 0.906, plus the non-leg band at the seam).
- 5360 seam verts were blended toward Spine.
- Torso verts with leg weight after the fix: 0.
- Leg tubes keep their original weights.

## Cloth chains

All chains are parented to `mixamorig:Spine` (the cloth starts at hip height). Only bone `_01` of each chain shares weight with Spine (a ring of the top 35%); the rest is chain-only, with at most 4 influences.

Sector letters: F = front (−Y), L = character-left (+X), B = back.

Suggested spring settings are a starting point for a Unity SpringBone / DynamicBone / MagicaCloth BoneSpring-style setup, all 0–1.

| chain | sector | bones | parent | length m | verts | stiffness | damping | drag | gravity | radius m |
|---|---|---|---|---|---|---|---|---|---|---|
| vine_FL | FL | vine_FL_01, vine_FL_02, vine_FL_03 | mixamorig:Spine | 0.36 | 360 | 0.5 | 0.45 | 0.5 | 0.25 | 0.04 |
| vine_L | L | vine_L_01, vine_L_02, vine_L_03 | mixamorig:Spine | 0.46 | 3470 | 0.35 | 0.4 | 0.45 | 0.4 | 0.05 |
| leaf_BL | BL | leaf_BL_01, leaf_BL_02, leaf_BL_03 | mixamorig:Spine | 0.45 | 1717 | 0.35 | 0.4 | 0.45 | 0.4 | 0.05 |
| leaf_B | B | leaf_B_01, leaf_B_02 | mixamorig:Spine | 0.25 | 1530 | 0.5 | 0.45 | 0.5 | 0.25 | 0.04 |
| leaf_BR | BR | leaf_BR_01, leaf_BR_02, leaf_BR_03 | mixamorig:Spine | 0.53 | 2111 | 0.35 | 0.4 | 0.45 | 0.4 | 0.05 |

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
| rest | 36 | True / True | 0 | 0 | 0/0 | - | - / - | - / - |
| walk | 36 | True / True | 0 | 0 | 0/0 | [1, 31] | 9.392756392659083e-07 / 0.0 | 3.577757825354486e-07 / 0.0 |
| attack | 36 | True / True | 0 | 0 | 0/0 | [1, 31] | 7.945617175229972e-07 / 0.0 | 3.998401124775034e-07 / 0.0 |

- Bone order: the 22 body bones keep their relative order, but they are **not** the first 22 indices. Blender writes bones by hierarchy, so the cloth chains (children of Spine) are interleaved after Spine's subtree. Use name-based mapping (Unity Humanoid / Avatar does this).
- Cloth mesh vertex groups: Spine plus cloth bones only; no leg groups.

## Caveats
- **What counts as cloth here.** Oakenshield's loose "cloth" is the hanging vine/leaf skirt below the bark belt. The split line is at 0.432H (z 0.816 m).
- **Spikes and claws stay on the body.** Small leaf spikes and root claws that don't touch the seam stay on the body.
- **Right vine strip.** The right-side vine strip falls in the same sector as the back-right leaves, so it's driven by `leaf_BR`; there's no separate `vine_R` chain.
- **Fixed cloth parents.** All chains are parented to Spine.
- **Leg pass-through without simulation.** With the cloth at rest, the legs pass through the hanging vines during the walk. Use the spring setup with leg colliders in-engine.


## Files

- `OAKENSHIELD_clothsplit.blend`, `OAKENSHIELD_clothsplit.fbx` (rest), `OAKENSHIELD_clothsplit_walk.fbx`, `OAKENSHIELD_clothsplit_attack.fbx`
- `stills/`: rest stills with the cloth hidden and shown (front/q34/side/rear) plus `OAKENSHIELD_clothsplit_rest_compare.jpg`
- `OAKENSHIELD_clothsplit_walk_contact.jpg` (rear + 3/4, every 3rd frame)
- `OAKENSHIELD_clothsplit_walk_preview_rear.mp4`
- `work/`: build_meta.json, qc.json, scripts
