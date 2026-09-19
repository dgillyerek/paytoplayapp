# Aldric 3D art upgrade

## This pass (LOOK — Derek CLOSE on motion 5916447)

- **Motion HOLD:** Walk/Attack `Evaluate()` keys from 5916447 stay. Pass thigh −X, pass-foot world ΔZ +, no L↔R weave, ~80° pass knee, tuck, ~0.40 m step, arm pendulum, TOP march. Play hub stays locked rear PNG until Derek PASS on **look**.
- **Look now:** capsule + projective-albedo proxy replaced with a **mid-poly plate/surcoat** mesh and a **look_targets atlas** (`ThemePack/.../art/heroes/3d/`).
  - Silver plate + gold trim (helm crest, pauldrons, gauntlets, greaves, sabatons)
  - Royal-blue short surcoat with **gold lion rampant** from `01_rear_LOCKED` (tilted back badge so it reads at high-angle rear)
  - Gold Greek-key hem (locked hem crop + procedural key)
  - Brown scabbard + gold fittings on **character-right** hip only (01 SoT; ignore 02 if mirrored)
  - Boots silver+gold (not brown leather)
- **Bind:** `SirAldric3DActor` loads `sir_aldric_midpoly.mesh.txt` + `sir_aldric_atlas.png` onto the existing rig / PlayableGraph. Same bones. Do not regress motion.
- **PIXEL_PROOF** has motion ΔZ gates **and** look pixel counts (blue/gold vs grey). Pixel Δ does **not** override the eye test.
- **Look gate is not claimed.** Derek must scrub Game-view and recognize the locked painted knight.

## Remaining gap (DCC)

1. A painted mid-poly FBX from `look_targets/` (`00_fullbody_LOCKED`, `01_rear_LOCKED`, `02` ref only) will still beat this procedural mesh (faceted plates, no unique unwrap, lion is a camera-facing badge).
2. Keep scabbard **character-right**. Ignore turnaround if it mirrors.
3. If a DCC clip replaces `Evaluate()`, dump Hip/Knee/Shoulder eulers at PASS/CONTACT L/R; pass knee <~50° or near-zero arm span means the remap is wrong.

## Drop into Unity

1. Assets live under `Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/`.
2. Actor maps bones to `Hips`, `UpLeg_L/R`, `Arm_R`, `Sword`, `Scabbard`, …
3. Camera stays high-angle rear, **fixed**, 1080×1920, character forward = world +Z = TOP.

## Play placeholder

Do not swap Play hub off `SIR_ALDRIC_REAR_MASTER_LOCKED.png` until Derek PASS on the 3D Game-view clip.
