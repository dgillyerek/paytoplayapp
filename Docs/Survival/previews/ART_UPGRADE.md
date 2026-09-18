# Aldric 3D art upgrade

## This pass (gait — Derek FAIL on 14d9c17)

- **Root cause (verified):** not a missing clip or flipped root-motion sign. `Evaluate` is the clip; `RootZ` still marches +Z. Pass thigh **+22** stacked on knee **+X** (shin folds toward camera on this hang −Y rig), so the swing foot's world Z went **negative** through mid-pass — calves read as kicking toward the camera / BOTTOM. Axis hypothesis is only partly true: knee +X is the normal back-fold; the bug was the pass thigh sitting *behind* the trail.
- **Motion now:** same 4 Game-view keys, pass thigh **−X** (toward TOP) so the tucked foot travels world **+Z**. Small +X trail = toe-off, not a rear kick. Weave lock held (root X = 0, chest/head |X| ≲ 0.04 m). Tucked pass, two-foot contact, ~0.40 m step, arm pendulum, ~80° pass knee. Camera `(0, 2.80, −5.40)` look-at `(0, 0.90, 0.50)`. Attack unchanged. March +Z = TOP, 0.80 m/s.
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
