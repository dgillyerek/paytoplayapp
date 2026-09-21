# Aldric 3D art upgrade

## Gate 3 Path 2 bind iterate (LOOK PASS fd9d6f8 / BIND FAIL)

Design **LOOK PASS** on `fd9d6f8` Game-view (Meshy/`e5b132f` knight). **BIND FAIL:** MP4 arm sheets mid-swing + horizontal mid/leg tear bands. Scabbard one.

This tip keeps the same GLB UVs / Image_0+lion atlas (do not touch the LOOK path). Bind iterate: isolated `Arm_*` solidify + prune; hem no longer Y-cuts leg tubes (full UpLeg/Leg/Foot); hairline armpit slivers only. `Evaluate()` `5916447` reused.

Proof: `sir_aldric_path2_walk_toward_top.mp4` + `gate3_path2_vs_fd9d6f8.png`. **Bind NOT claimed. Walk-with-look NOT claimed.** Hub PNG locked.

## Gate 3 Path 2 (Meshy-look e5b132f hang-skin — Derek STOP remesh/capsule)

Design **PASS** look on `e5b132f`. Derek STOP: voxel-remesh / capsule-arm Game-view (`61e023d` / `c840b73`) threw away that paint PASS.

- **Source look:** `e5b132f` Path 2 Meshy GLB (`aldric_meshy_retopo.glb`) + paint iterate (Image_0 UV bleed + SoT lion cards).
- **UV / albedo:** original GLB UVs kept. Actor atlas = Image_0 + lion-card top strip (`combine_atlas_and_remap`, same method as e5b132f). Game-view EEVEE renders original GLB materials **before** atlas remap.
- **Retopo / clean:** no voxel remesh; no capsule arms in Game-view. GEO-delete extra sheath islands + 1mm weld (11283 → 12 islands). Small same-side hang-arm faces get 5cm bmesh solidify; huge leftover faces deleted. Exclusive hang-volume weights; cloth/hem locked before `Arm_*`.
- Motion: `Evaluate()` keys from `5916447` **reused**.
- Proof: `Docs/Survival/previews/gate3/sir_aldric_path2_walk_toward_top.mp4` + `gate3_path2_walk_phases.png` + `gate3_meshy_hang_vs_e5b132f.png`.
- Play hub PNG **locked**.

**Look PASS claimed** (Design, paint stills `e5b132f`). **Bind NOT claimed.** **Walk-with-look NOT claimed.**

## Gate 3 Path 2 Meshy (LOOK stills — paint iterate) — archived

Design Meshy Flagship Image-to-3D from locked rear SoT, Decimate ~50k (original 1.77M faces / 57MB exceeded upload). **Not** the scripted loft.

8babba7 World-cam FAIL: UV island cracks (shoulders/neck/joints) + smeared gold lion. Paint iterate `e5b132f`:

- UV occupancy bleed + dark-border inpaint + split-normals cleared
- Dedicated planar SoT lion cards (`01_rear_LOCKED` / `01_FRONT`) on upper-torso cloth
- Hang **not** applied (A-pose).

- GLB: `ThemePack/.../3d/sir_aldric_meshy_retopo.glb` + `design/.../AI_MESH_PATH2/out/`
- Unity-oriented `.blend` for Play-cam stills (Y-up, +Z face, scabbard +X)
- Still proof: `Docs/Survival/previews/gate3/` front / rear / side R / ¾ + vs-SoT sheet + `gate3_uv_lion_delta.png`

## Gate 3 FAIL iterate (segmented plate / helm / heraldry)

Derek FAIL: portrait-level detail missing; first Gate 3 stills read as flat white tubes + cone helm + plaque lion. Surcoat volume kept. This iterate:

- Segmented pauldrons / arms / legs with chunky gold rims (readable at 1080×1920 Play cam)
- Closed armet + gold comb / visor band (not a white cone)
- Lion embroidered on royal-blue cloth (cylindrical UV, no navy plaque); Greek-key hem
- Sabaton lames + gold; gauntlet cuffs / fingers (no sphere hands)
- ~13192 tris. Scabbard character-RIGHT. Same Play World-cam angle. Motion HOLD. Play hub locked.

**Look gate is not claimed.** Filigree still short of the portrait.

## Gate 3 HOLD (game mesh + World-cam stills)

CEO Gate 2 was a **soft-PASS** (Derek moved on without FAIL). Volume law = clay `5de0e16` / compare `6ef0919` — this mesh is **grown from that clay**, not a new proportion.

- **Game mesh:** segmented mid-poly (~13192 tris) with joint loops + gold plate rims. No voxel remesh. `sir_aldric.fbx` + `sir_aldric_midpoly.mesh.txt` + atlas.
- **Look:** silver/gold plate, royal-blue surcoat, gold lion rampant + Greek-key hem, brown scabbard **character-RIGHT** only, silver/gold boots. Helm BACK vs `01_rear_LOCKED` / turnaround (World-cam is rear; face law not visible).
- **Proof:** Play march-angle World-cam stills `(0, 2.80, −5.40)` look-at `(0, 0.90, 0.50)` FOV 30, 1080×1920, TOP = +Z = away — **not** a beauty portrait cam. See `Docs/Survival/previews/gate3/`.
- **Walk / Animator** now have a Path 2 skin clip for Design re-gate. Play hub stays paused. Motion HOLD 5916447 / 1cd3320. `Evaluate()` untouched.

**Look PASS claimed** (Design, paint stills). **Walk-with-look NOT claimed.**

## Gate 2 HOLD (blockout only)

Look law Gate 1 is LOCKED (helmeted turnaround + complete portrait as face law). Compare sheet SoT columns are the Design re-lock full-figure light-gray panels (bottom-band meanL > 200). Clay stills unchanged from `5de0e16`. See `Docs/Survival/previews/blockout/GATE2_BLOCKOUT.md`. **Gate 2 not claimed** (CEO soft-PASS on volume; still not a look PASS).

## This pass (PATH B — Blender / FBX)

- **Motion HOLD:** Walk/Attack `Evaluate()` keys from 5916447 / 1cd3320 stay. Pass thigh −X, pass-foot world ΔZ +, no L↔R weave, ~80° pass knee, tuck, ~0.40 m step, arm pendulum, TOP march. Play hub stays locked rear PNG until Design PASS on the **walk-with-look clip**.
- **Look now:** Path 2 Meshy (Design look PASS `e5b132f`) is the Actor bind. Loft mid-poly is archive.
  - Runtime bind (Editor-less): `sir_aldric_meshy.mesh.txt` + `sir_aldric_meshy_atlas.png` on the existing Actor bones / PlayableGraph
  - Humanoid slot map: `sir_aldric_humanoid.json` (rest = Actor hang, not T-pose; mesh is A-pose)
  - Silver plate + gold trim (helm crest, pauldrons, gauntlets, greaves, sabatons)
  - Royal-blue short surcoat with **gold lion rampant** + **gold Greek-key hem** from `01_rear_LOCKED`
  - Brown scabbard + gold fittings on **character-right** hip only
  - Boots silver+gold (not brown leather)
- **PIXEL_PROOF** has motion ΔZ gates **and** Gate 3 World-cam look pixel counts (blue/gold vs grey). Pixel Δ does **not** override the eye test.
- **Look PASS claimed** (Design, paint stills). **Walk-with-look NOT claimed.** Derek must recognize the locked painted knight **walking** in the Game-view clip.

## Remaining gap

1. Scripted loft mid-poly, not a painted unique unwrap from Design. Faceting / lion plaque will still read short of the locked PNG.
2. Keep scabbard **character-right**. Ignore turnaround if it mirrors.
3. Face law (`05_PORTRAIT`) is not visible from World-cam; do not invent a face if those bytes are missing.
4. If a DCC clip replaces `Evaluate()`, dump Hip/Knee/Shoulder eulers at PASS/CONTACT L/R; pass knee <~50° or near-zero arm span means the remap is wrong.

## Drop into Unity

1. Assets live under `Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/` (`sir_aldric.fbx` for Editor import).
2. Actor maps bones to `Hips`, `UpLeg_L/R`, `Arm_R`, `Sword`, `Scabbard`, …
3. Camera stays high-angle rear, **fixed**, 1080×1920, character forward = world +Z = TOP.

## Play placeholder

Do not swap Play hub off `SIR_ALDRIC_REAR_MASTER_LOCKED.png` until Design PASS on this 3D Game-view walk clip.
