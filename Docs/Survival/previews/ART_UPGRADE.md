# Aldric 3D art upgrade

## This pass (gait — Design HARD FAIL on f1e4770)

- **Root cause (verified):** BVH keys **did** reach `SirAldric3DMotion.Evaluate` and `SirAldric3DActor.BuildLoopClip` (`Leg_L.X=65.9` at pass L). This was **not** a missing-clip / PlayableGraph miss. High-rear + a **forward (−X) pass thigh** put that 66° flex along the ground (~11 cm lift), so the eye still read a toy-soldier. Arms also read pinned (small sagittal + wrong-sign Z tucked into the ribs).
- **Motion now:** 4 Game-view keys (`pass L / contact L / pass R / contact R`) solved against `WALK_GAIT_BAR` rear phases. Gait keeps tucked pass, two-foot contact, 0.40 m step, arm pendulum, knee flex. **ff81201 CLOSE:** Derek saw the body weave left↔right. There was no root-X curve — `Hips.Z ±8` with only 28% spine counter tipped the torso ~11 cm each step. Hip drop is now ±5.5° and spine Z fully counters the roll so chest/head stay on world +Z. Yaw damped. Root X stays 0. Camera `(0, 2.80, −5.40)` look-at `(0, 0.90, 0.50)`. Attack unchanged. March +Z = TOP, 0.80 m/s.
- **PIXEL_PROOF** includes the bone-drive dump table (euler + world knee flex + foot Y + hand Z). Pixel Δ does **not** override the eye test. **Design eye gate is not claimed** until Derek can scrub sample and Aldric and recognize the same gait.
- **Look:** not cubes. Capsules/spheres with **01_rear_LOCKED** projective albedo on the body (painted right-hip sheath stripped so it does not double the 3D scabbard). Gold/silver/brown solids for trim, **one** character-right `Scabbard`, blade.
- **Still not a DCC hero:** no painted mid-poly FBX, no unique unwrap, no back-mounted sheath mesh (short surcoat is the body albedo, not a diagonal cape tube). Turnaround `02` is reference only (rear panel may mirror — scabbard SoT is `01` character-right).

## Remaining gap (DCC)

1. Model in DCC from `look_targets/` (`00_fullbody_LOCKED`, `01_rear_LOCKED`, `02_aldric_turnaround_orthos`).
2. Mid-poly mobile mesh, one texture set: silver plate, gold trim, royal-blue surcoat, gold lion, Greek-key hem, brown scabbard + gold fittings, silver/gold boots, cape.
3. **Scabbard on character-right hip** (locked rear SoT). Ignore turnaround if it mirrors that.
4. Export FBX (Unity Humanoid) or keep the current custom bone names and retarget.

## Drop into Unity

1. Import under `Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/`.
2. Avatar: Humanoid or map bones to `SirAldric3DActor` names (`Hips`, `UpLeg_L/R`, `Arm_R`, `Sword`, `Scabbard`, …).
3. Keep clips `Walk` / `Attack` (or the concatenated `Aldric_WalkAttackLoop`) on the existing Animator PlayableGraph. `BuildLoopClip` samples `Evaluate()` — if a DCC clip replaces it, dump Hip/Knee/Shoulder eulers at PASS/CONTACT L/R; pass knee <~50° or near-zero arm span means the remap is wrong.
4. Camera stays high-angle rear, **fixed**, 1080×1920, character forward = world +Z = TOP of screen.

## Play placeholder

Do not swap Play hub off `SIR_ALDRIC_REAR_MASTER_LOCKED.png` until Derek PASS on the 3D Game-view clip.
