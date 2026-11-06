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
