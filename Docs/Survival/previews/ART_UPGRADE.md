# Aldric 3D art upgrade

## This pass (orientation + look)

- **Motion:** capsule-sculpted skinned mesh, Animator PlayableGraph, high-angle rear camera **fixed** so RootZ travel reads as march toward **TOP** of 1080×1920 Game view. Thigh −X = toward world +Z (not toward camera). Arm/forearm −X = hands and blade in front toward TOP / enemy (never +X toward the camera). Chevrons + enemy keep mark TOP.
- **Look:** not cubes. Capsules/spheres with **01_rear_LOCKED** projective albedo on the body (same bytes as Play placeholder). Gold/silver/brown solids for trim, scabbard, blade.
- **Still not a DCC hero:** no painted mid-poly FBX, no unique unwrap, cape is a simple skinned capsule, turnaround `02` is reference only (rear panel may mirror — scabbard SoT is `01` character-right).

## Remaining gap (DCC)

1. Model in DCC from `look_targets/` (`00_fullbody_LOCKED`, `01_rear_LOCKED`, `02_aldric_turnaround_orthos`).
2. Mid-poly mobile mesh, one texture set: silver plate, gold trim, royal-blue surcoat, gold lion, Greek-key hem, brown scabbard + gold fittings, silver/gold boots, cape.
3. **Scabbard on character-right hip** (locked rear SoT). Ignore turnaround if it mirrors that.
4. Export FBX (Unity Humanoid) or keep the current custom bone names and retarget.

## Drop into Unity

1. Import under `Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/`.
2. Avatar: Humanoid or map bones to `SirAldric3DActor` names (`Hips`, `UpLeg_L/R`, `Arm_R`, `Sword`, `Scabbard`, …).
3. Keep clips `Walk` / `Attack` (or the concatenated `Aldric_WalkAttackLoop`) on the existing Animator PlayableGraph.
4. Camera stays high-angle rear, **fixed**, 1080×1920, character forward = world +Z = TOP of screen.

## Play placeholder

Do not swap Play hub off `SIR_ALDRIC_REAR_MASTER_LOCKED.png` until Derek PASS on the 3D Game-view clip.
