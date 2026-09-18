# Walk gait bar (Derek 2026-09-18)

**Source:** `WALK_GAIT_BAR_skeleton_sample.mp4` (Derek-provided skeleton walk — camera orbits; gait is treadmill / in-place)

**Goal:** Rematch Sir Aldric **Walk** to this sample’s gait feel, under knight look. Not a new cartoon march.

## What the sample does (locked notes)
- Steady moderate-paced human walk; contralateral arm swing (relaxed pendulum, slight elbow bend)
- Knee: soft flex on contact (weight absorb), **deep bend on pass** to clear the foot, near-straight on push-off
- Pelvis: rotates with advancing leg; **hip drop** on unweighted/passing side (COG shift)
- Spine: counter-twist vs hips (overlapping action — not a rigid torso)
- Feet: heel strike → flat → heel lift / toe-off (readable plant)
- Sample walks **in place**; when adding root motion for TOP march, stride length must match speed (no foot slide)

## High-angle rear retarget (knight marching TOP)
Most visible from behind / looking down:
1. **Shoulder–hip counter-rotation** — fluid torso twist so armor doesn’t look locked
2. **Pelvic tilt / hip drop** on passing leg — avoid floating / rigid waddle
3. **Arm swing** — keep sample timing; flare elbows slightly if needed for pauldron clearance; arms/sword stay on **far / TOP** side (not toward camera)
4. **Heel lift / toe-off** — soles readable from high rear; snappy plant before release
5. **Root motion** — +Z = TOP; stride length ↔ travel speed (no slide)

## Keep game constraints
- High-angle rear 1080×1920; march toward TOP; enemy = TOP
- Single brown scabbard **character-right** only
- 3D Animator only (no PNG warp)
- Look: knight from `look_targets/` (`01_rear_LOCKED` SoT)

## Walk clip landed (this pass)

Not another sine march. Runtime Walk keys are retargeted from `walk_cycle_mixamo_style.bvh` (CreativeInquiry BVH-Examples `walk-cycle.bvh`, Mixamo-style humanoid — https://github.com/CreativeInquiry/BVH-Examples), skinned as the knight, 1.75s → 1.00s to match this bar’s 120 spm. Side-by-side phases live in `gait_bar_phases/`.

## Deliverable
Side-by-side sample-vs-Aldric sheet + walk GIF + walk→attack mp4 for Design/Derek re-gate.
