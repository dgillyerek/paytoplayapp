# Kaelen FIX9 — overhead arc, no shoulder revolution

Derek rejected merged FIX8 (`02b3660e`) on two motions:

1. Dual slash did not raise the arms and then cut clearly down. The blades were flicked by a forearm twist clamped at ±75° while the hands stayed near shoulder height.
2. Cross thrust's right arm rolled a full turn around the shoulder during the overhead. Wrist-bend numbers and a continuous world-quaternion track did not catch it.

FIX8's premise was false. A quaternion track can be continuous and still be a 360° roll. Wrist bend can stay small while the upper arm spins. The measurement that shows the roll is the upper-arm swing/twist about the bone axis, plus the elbow's azimuth around the shoulder-to-hand line.

## What the numbers were on FIX8

Cross thrust, bone `mixamorig:RightArm` (upper arm), local twist about Y, unwrapped:

- frame 3: −39°
- frame 14: +198° (largest step, +77° in one frame)
- frame 22: **+389°** (the peak; this is the close-up frame)
- travel from the minimum to that peak: **428°**
- elbow azimuth around the shoulder-to-hand axis swept about 275°

The pose at frame 22 is the same orientation as a +29° twist, because a full turn is the same rotation. The defect is the path that gets there. A still of only frame 22 can look acceptable. The spin is the frames between 12 and 22.

Dual slash hand height peaked at 1.63 m (left) and 1.58 m (right). Head-mesh top is 1.85 m. The right sword tip never went below 1.20 m. The arm was not doing the arc; the forearm twist was.

## What FIX9 changes

The hand arc is solved with two-bone IK. The elbow is kept on a lateral pole, so the upper arm does not orbit the shoulder. Upper-arm roll is then held within ±55° of that clip's guard roll, and it cannot step more than 16° in one frame. Forearm pronation is a separate joint: it only aims the sword that is already seated in the hand, inside ±100° of the guard pronation, at most 20° per frame. The hand bone's local rotation (the grip) is not edited. No Euler curve is clamped or offset.

Edit bones, parents, head, tail, and bone length are unchanged (compared before and after in `k9_fix.py`). Body mesh and weights are not edited. Idle, walk, rest, and cape FBX are not re-exported.

| | FIX8 | FIX9 |
|---|---|---|
| Cross thrust right upper-arm twist span | 428° | 55° |
| Cross thrust right elbow-azimuth sweep | ~275° | ~97°, no whip through the front |
| Cross thrust hand height | 1.95 / 1.98 m | 1.95 / 1.98 m (overhead kept) |
| Cross thrust sword-tip low | 0.61 / 0.52 m | 0.71 / 0.48 m |
| Cross thrust sword–sword min | 6.31 cm | 3.98 cm, 0 triangle overlaps |
| Cross thrust sword–body min (grip faces excluded) | 4.66 cm | 5.02 / 6.05 cm, 0 overlaps |
| Dual slash hand height | 1.63 / 1.58 m | 1.95 / 1.95 m (head top is 1.85 m) |
| Dual slash sword-tip high / low | right tip never under 1.20 m | 2.65 m high, 0.28 / 0.62 m low |
| Dual slash upper-arm twist span | ~0° (the roll was in the forearm clamp) | 55° / 67° |
| Dual slash sword–sword min | 6.44 cm | 19.5 cm, 0 overlaps |
| Dual slash sword–body min | 4.66 cm | 4.89 / 5.32 cm, 0 overlaps |

FBX re-import in Blender 5.2.3 with the same axis settings as the exporter (`-Z` forward, `Y` up, bone primary `Y`, secondary `X`, `automatic_bone_orientation` off) reproduces the shoulder-to-hand vector within 0.01 mm. Bone-length error is under 0.001 mm. Unity's preview importer sets `bakeAxisConversion = false` and `globalScale = 1`, so it consumes that same FBX. **Unity itself was not launched. These images are Blender, not a Unity play-mode pass.**

## Tradeoffs

- Forearm pronation is allowed ±100° from the guard so the blade can point down while the shoulder stays still. That is wider than a relaxed ±80° forearm. It is rate-limited and it is not a shoulder revolution. The down-cut in the sheets is that pronation plus the arm arc.
- Cross-thrust sword clearance is 3.98 cm, tighter than FIX8's 6.31 cm, still with no triangle overlap.
- The cape was not re-simulated. The blue mesh in the sheets is the existing cape on its own rig. A fresh spring sim like FIX8's 619-frame pass was not repeated; the sim scripts were never in the repo.
- Idle and walk were not re-keyed.

## Reproduce

Blender 5.2.3. Start from the FIX8 blend at main `02b3660e`, not from the blend this branch commits (that one is already solved).

```bash
git show 02b3660e:design/survival-theme-a-fantasy/heroes/anim/kaelen/blender/KAELEN.blend > /tmp/KAELEN_fix8.blend
# that object is a Git LFS pointer unless LFS is smudged; prefer:
GIT_LFS_SKIP_SMUDGE=0 git checkout 02b3660e -- design/survival-theme-a-fantasy/heroes/anim/kaelen/blender/KAELEN.blend
export KAELEN_FIX9_OUT=/tmp/kaelen-fix9
export KAELEN_FIX8_BLEND=design/survival-theme-a-fantasy/heroes/anim/kaelen/blender/KAELEN.blend
blender --background --factory-startup "$KAELEN_FIX8_BLEND" \
  --python design/survival-theme-a-fantasy/new_2026-11-03/kaelen/fix9/k9_fix.py
export KAELEN_FIX9_BLEND="$KAELEN_FIX9_OUT/KAELEN_fix9.blend"
blender --background --factory-startup \
  --python design/survival-theme-a-fantasy/new_2026-11-03/kaelen/fix9/k9_present.py
```

`k9_fix.py` writes `$KAELEN_FIX9_OUT/solve_report.json` and the solved blend. `k9_present.py` exports the two attack FBX files, re-imports them, and renders the sheets. Diagnosis-only (no writes to the pose) is `k9_diag.py`.


## Dual slash reauthor (not a pass)

The first FIX9 dual slash was rejected on the clips: at the start the right arm was already high while the left stayed at the hip (a tight diagonal), and the quarter-speed close-up showed the left palm open with the blade crossing it.

This pass rewrites only `attack_dualslash` (still 58 frames). Both hands rise together (frame 16: right/left hands 1.94 m and 1.93 m, tips 2.66 m and 2.45 m; head top is 1.85 m), then both come down (frame 37: hands 0.94 m and 0.91 m, tips 0.54 m and 0.74 m). Sword–sword minimum 9.1 cm, sword–body 3.3 cm and 6.1 cm, no triangle overlaps. Forearm twist stays inside ±22° of the guard (span 44° right, 14° left).

The palm socket is the hand bone, not a vertex edit. `KAELEN_sword_L` and `KAELEN_sword_R` have no object constraint. All 17059 vertices of each are weight 1 on `Arm.002`, which is the same bone as the fingers. The solver stores the sword axis in that bone at the guard pose and aims `hand_matrix * bind_axis` after that. It does not read deformed vertices to aim, and it does not write vertices. A second Child Of on the sword would move it twice, so none is added. Body and sword fingerprints match the source blend and the reimported clip (`KAELEN_body` `ff76c909c2e8e141`, sword L `39a435ece28975e9`, sword R `f49919f8acb6069b`).

Unity's preview instantiates `KAELEN_rig_rest.fbx` and samples the clip onto it (`NewCharsPreview.SampleAnimation`). The socket that preview can see is the hand pose on that rest mesh. `KAELEN_rig_rest.fbx` was not replaced. Its sha256 is still `7ac5ea510f50ac0f96e28f5b6c000fa652cebe74935a49d9e3b916dbbd6946eb`. Re-import of the dual-slash FBX in Blender puts the hilt back on that bind point within 0.001 mm at frames 1, 16, 37, and 58. The hand-bone world rotation matches the blend within 0.06°.

The pommel still sticks out past the heel of the hand, and at the low cut the crossguard sits against the wrist. That is the bind. Moving the sword mesh would change the rest asset the preview actually draws, so it was not done. These pictures are not a pass.

`attack2_crossthrust.fbx` sha256 is still `a06eafb9d24df1a7da4382a09b5b037e335e0c221acb8bf0a2841015d538044e` in both the design final folder and the preview folder. Idle, walk, rest, and cape were not rewritten. Unity was not launched. The images below are Blender re-imports of the dual-slash FBX.

The new pictures and clips are renders of the re-imported dual-slash FBX, not the previous before/after sheet:

- `sheets/KAELEN_FIX9_DUALSLASH_FBX.png` — frames 15–17 (high) and 36–38 (low), front, side, and three-quarter, plus frames 1, 8, and 58.
- `sheets/KAELEN_FIX9_DUALSLASH_GRIPS.png` — left and right hands, three consecutive frames at the high and at the low.
- `clips/KAELEN_FIX9_dualslash_front.mp4`, `_side.mp4`, `_qright.mp4`, `_qright_quarter.mp4`.

The old dual-slash front, close-up, and quarter-speed files were removed so they are not reused.

```bash
# On the blend that already contains the reviewed cross thrust. Rewrites dual slash only.
export KAELEN_FIX9_OUT=/tmp/kaelen-dual
blender --background --factory-startup \
  design/survival-theme-a-fantasy/heroes/anim/kaelen/blender/KAELEN.blend \
  --python design/survival-theme-a-fantasy/new_2026-11-03/kaelen/fix9/k9_dual_only.py
export KAELEN_FIX9_BLEND="$KAELEN_FIX9_OUT/KAELEN_fix9b.blend"
# Socket check only. KAELEN_FIX9_RENDER=1 also writes the frame pngs.
KAELEN_FIX9_RENDER=0 blender --background --factory-startup \
  --python design/survival-theme-a-fantasy/new_2026-11-03/kaelen/fix9/k9_dual_render.py
```

`k9_dual_render.py` exits if a sword or body vertex hash differs from the blend, or if the reimported hilt leaves the hand bone by more than 0.2 mm. It does not write `KAELEN_rig_rest.fbx`.

This is not a FIX9 pass. Unity was not run.

## Cross thrust left grip (inspected, not rebound)

The cross-thrust file was not changed. Preview sha256 is still `a06eafb9d24df1a7da4382a09b5b037e335e0c221acb8bf0a2841015d538044e`. `KAELEN_rig_rest.fbx` is still `7ac5ea510f50ac0f96e28f5b6c000fa652cebe74935a49d9e3b916dbbd6946eb`, the same bytes as main `02b3660e`.

`NewCharsPreview` instantiates `KAELEN_rig_rest.fbx` and calls `AnimationClip.SampleAnimation` on that instance. `LoadClip` keeps the clip and drops the mesh inside the attack FBX. There is no separate sword prefab. On the imported rest file each sword is parented to the armature as an object, has an Armature modifier, has no constraint, and all 17059 vertices are weight 1 on `Arm.002`. That is one bind. A Child Of was not added.

On that rest mesh plus the current clip, the left hilt stays at 2.19 cm, 7.84 cm, −1.56 cm in `LeftArm.002` at frames 1, 8, 14, 18, 20, 22, 28, 36, and 48. The same point is on the base `02b3660e` cross clip, on dual slash, on idle, and on walk. The nearest sword vertex is 0.16 cm from a hand vertex (Arm.002 weight ≥ 0.5). The hand bone's location channel is 0. The earlier 4–6 cm sword–body figure excluded those hand faces, so it is not the grip distance.

At frame 20 the left hand quaternion matches the base clip. From frame 28 the current clip eases that hand to the rest bend (8.28° between forearm and hand); the base clip stays at 20.24°. The sword does not leave the hand either way, because the fingers and the sword are the same bone. The shoulder fix is the upper-arm path already in `a06e…`. Those arm matrices were not edited again.

I looked at the imported clip, not Unity: front, left quarter, and a left-hand view with the right sword hidden, including frames 20 and 28. The fingers stay on the handle. The pommel still passes the heel. That is the shared bind, so the sword mesh was not moved and body weights were not changed. Existing cross-thrust and dual-slash clips were left in place.

- `clips/KAELEN_FIX9_crossthrust_inspect_front.mp4`
- `clips/KAELEN_FIX9_crossthrust_inspect_lquarter.mp4`
- `clips/KAELEN_FIX9_crossthrust_inspect_lhand.mp4`
- `sheets/cross_left/`

Unity was not run. A Blender camera at yaw 135°, pitch 30°, and 54° fov (the locked-battle numbers) shows the same overhead from behind the body. It is not a Unity capture.
