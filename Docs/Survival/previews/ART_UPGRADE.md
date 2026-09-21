# Aldric 3D art upgrade

## Gate 2 HOLD (blockout only)

Look law Gate 1 is LOCKED (helmeted turnaround + complete portrait as face law). Compare sheet SoT columns are full-figure on light gray (no black-void alpha). Clay stills unchanged from `5de0e16`. Texture polish, walk clips, and Play hub swap are **paused** until Derek PASSes the grey-clay silhouette vs `TURNAROUND_GATE1/LOCKED/`. Clay must read as the knight (helm / surcoat / plate / scabbard-RIGHT), not a mannequin or box stack. See `Docs/Survival/previews/blockout/GATE2_BLOCKOUT.md`. **Gate 2 not claimed.**

## This pass (PATH B — Blender / FBX)

- **Motion HOLD:** Walk/Attack `Evaluate()` keys from 5916447 / 1cd3320 stay. Pass thigh −X, pass-foot world ΔZ +, no L↔R weave, ~80° pass knee, tuck, ~0.40 m step, arm pendulum, TOP march. Play hub stays locked rear PNG until Derek PASS on **look**.
- **Look now:** box-atlas / procedural mid-poly iteration is **stopped**. Sir Aldric is modeled in **Blender** from `look_targets` (`01_rear_LOCKED` = primary Game-view SoT; `02` ref only — scabbard = character-RIGHT).
  - `sir_aldric.fbx` + `sir_aldric.blend` under `ThemePack/.../art/heroes/3d/`
  - Runtime bind (Editor-less): `sir_aldric_midpoly.mesh.txt` + `sir_aldric_atlas.png` on the existing Actor bones / PlayableGraph
  - Humanoid slot map: `sir_aldric_humanoid.json` (rest = Actor hang, not T-pose)
  - Silver plate + gold trim (helm crest, pauldrons, gauntlets, greaves, sabatons)
  - Royal-blue short surcoat with **gold lion rampant** + **gold Greek-key hem** from `01_rear_LOCKED`
  - Brown scabbard + gold fittings on **character-right** hip only
  - Boots silver+gold (not brown leather)
- **PIXEL_PROOF** has motion ΔZ gates **and** look pixel counts (blue/gold vs grey). Pixel Δ does **not** override the eye test.
- **Look gate is not claimed.** Derek must scrub Game-view and recognize the locked painted knight as a real Blender/FBX hero.

## Remaining gap

1. This is a first-milestone Blender mid-poly (scripted sculpt from SoT stills), not a painted unique unwrap from Design. Faceting / lion projection will still read short of the locked PNG.
2. Keep scabbard **character-right**. Ignore turnaround if it mirrors.
3. If a DCC clip replaces `Evaluate()`, dump Hip/Knee/Shoulder eulers at PASS/CONTACT L/R; pass knee <~50° or near-zero arm span means the remap is wrong.

## Drop into Unity

1. Assets live under `Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/` (`sir_aldric.fbx` for Editor import).
2. Actor maps bones to `Hips`, `UpLeg_L/R`, `Arm_R`, `Sword`, `Scabbard`, …
3. Camera stays high-angle rear, **fixed**, 1080×1920, character forward = world +Z = TOP.

## Play placeholder

Do not swap Play hub off `SIR_ALDRIC_REAR_MASTER_LOCKED.png` until Derek PASS on the 3D Game-view clip.
