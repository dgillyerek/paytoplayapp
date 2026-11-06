# Seraphine FIX5 revision — same PR, not a pass

Branch `cursor/seraphine-fix5-attacks-d044`. Revises the d579 candidate. Not merged. Unity was not run.

## Why the left hand floated, and why it went through the breastplate

The staff mesh is weighted only to `RightHand`. The left hand is a separate bone, so a palm on the shaft is a pose, not a skin bind. On d579 the left-hand target was either held off the shaft or sent to the far side of the chest. From the left shoulder that line passes through the breastplate, which is what the mesh audit found (left forearm/breastplate f14–27, arm/braid f16–21). The way back to idle was a blend that swept the hand down through the right leg at f27. Pulling the arm out to the side avoids the armor and also leaves the palm off the weapon. That is the float.

This revision turns the chest so the right shoulder comes forward after the hips, and sets the shaft as the line from a station in front of the sternum to the right hand 30 cm further toward the tip. The left palm aims at that station. When that reach would still put the forearm inside the chest (f17–20), the hand stays in a forward-left guard instead of being buried in the armor. On the strike frames the forearm is about 5 cm off the torso, with no intersection samples, and the staff stays about 22–30 cm off the hair. The butt of the full staff mesh stays about 40 cm off the ground. The earlier staff-to-body gap (no intersection, at least 3.3 cm) and the wing gaps are not spent to buy the grip.

## What the motion is

Hips X/Y stay on the idle mark. The preview pins hip X and Z, so the steps are the feet under that pelvis, not a root slide.

- Thrust, 45 frames, **1.50 s**. Load with a small hip turn while the feet are still together (the f8–10 thigh cross was the big yaw on a narrow stance). Left foot then lifts 16 cm, knee folds, and steps. The right heel rises by orbiting a fixed toe while the hips unwind, then the arms push. The lead foot lifts again, travels back in the air, and plants before the weight settles. Ends on the idle pose.
- Throw, 46 frames, **1.53 s**. Left foot steps out. The staff cocks up and back on the right shoulder, not flat across the crown. The left arm stays wide on her left, below the braid, and comes back down that side instead of onto the thigh. Right wrist keeps the idle hand-to-forearm angle, **26.4°**, instead of the 71° bend.

## Still not a visual pass

The thrust line stays steep, because a flat thrust puts the trailing end of this staff back through the wings. The left palm is not on the shaft for f17–20. The cape in the sheets is the original mesh parented to Spine2, not a new spring sim. Fingers have no bones. No Unity playback.

Proof, front three-quarter and strict side, camera locked:

- `design/.../seraphine/sheets/fix5/SERAPHINE_FIX5_thrust_SHEET.png`
- `design/.../seraphine/sheets/fix5/SERAPHINE_FIX5_throw_SHEET.png`
- `SERAPHINE_FIX5_thrust_front_side.mp4` (1.50 s)
- `SERAPHINE_FIX5_throw_front_side.mp4` (1.53 s)
- `SERAPHINE_FIX5_{thrust,throw}_hands_quarter.mp4` wrist close-ups
