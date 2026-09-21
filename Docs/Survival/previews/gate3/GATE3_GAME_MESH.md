# Gate 3 — Path 2 rebind after walk-with-look FAIL (`89481e5`)

Design **FAIL** walk-with-look on tip `89481e5`. Look PASS on paint stills `e5b132f` still stands. This pass rebinds only. `Evaluate()` gait keys are **reused**. Play hub PNG stays locked. **Walk-with-look is NOT claimed.**

## Hard FAILs addressed

1. **Arm slabs / layered smears** — `89481e5` used A-pose XYZ boxes that stole the tabard into Hand_L/R (~4k verts each). Bone1 then split that thin plate across Arm/Fore/Hand, so Evaluate() shredded it into stacked sheets. Rebind: weld 1 mm stacked Meshy shells, lock cloth to torso, one hang-arm corridor per side on **Arm_L / Arm_R only** (no elbow split). Fore keys still run; they have no mesh.
2. **Stacked scabbard** — thin sheath-axis left the shell on Hips and the core on Scabbard (rest 18/22), so two sheaths. Rebind: one island, all on `Scabbard`.
3. **Horizontal tear bands mid + legs** — hard Y-cuts through the tabard/hem assigned left/right skirt to opposite limbs. Rebind: hem stays Hips. Weld removes the duplicate shells that slid apart.

## KEEP (unchanged)

- TOP march away, lion readable
- `Evaluate()` gait / weave / forward-swing keys from `5916447` — not rewritten
- Scabbard character-RIGHT, face +Z, Unity Y-up, sheathed
- Play hub `SIR_ALDRIC_REAR_MASTER_LOCKED.png`

## Proof

Play World-cam `(0, 2.80, −5.40)` look-at `(0, 0.90, 0.50)` FOV 30, 1080×1920:

- `sir_aldric_path2_walk_toward_top.mp4`
- `gate3_path2_walk_phases.png`
- `walk/world_walk_{pass,contact}_{l,r}.png`

Rebuild: `blender --background --python scripts/blender/skin_sir_aldric_meshy.py` then `python3 scripts/blender/compose_path2_walk_proof.py`

## Honest ART

- Arms are **one rigid Bone1 volume per side** swinging from the shoulder. Elbow/forearm keys do not bend the mesh. At extreme pass the thin plate can still read flat from this rear cam — that is the bind, not a gait change.
- Tabard is a hip plate (no cloth).
- Neck groove / gold edge specks remain (look-PASS polish, not this gate).
- **Do not claim walk PASS.** Design re-eyes this clip.
