# Kaelen FIX6 — faster attacks + higher cross thrust

Derek on FIX5 (main 342c828d): "kaelens motion looks a lot better but it needs to be sped up.
for the crossthrust make hands and arms extend up higher".

## Changes (LOCAL branch kaelen-fix6; not pushed until Derek approves)
- attack_dualslash: 90f (3.00 s) -> 58f (1.93 s), 1.55x. Smooth time warp (1.45x through the attack, 1.8x on the recovery) so anticipation / follow-through keep their shape.
- attack2_crossthrust: 80f (2.67 s) -> 53f (1.77 s), 1.51x, same warp.
- Cross thrust raised: both jabs lift from chest (hand z ~1.19 m) to chin/mouth height (~1.52 m); the final cross thrust reaches forward-up with arms extended, hands ~1.60-1.63 m (was ~0.93-1.07 m; shoulder 1.40, head top 1.85). Done with a clavicle + shoulder lift and slight elbow extension; the hand bone is untouched.
- FIX5 wrists kept: hand twist 0, constant 12 deg flex; forearm twist re-limited to <=7.5 deg/frame after the retime. Grips unchanged (palm->axis 2.42/2.00 cm, inner gap 0.15 cm).
- Feet pinned (foot slip 0). idle and walk unchanged (same speed).
- Skeleton hash unchanged: 6212627f289a1abb. Clip names unchanged. No .cs edits; NewCharsPreview uses clip.length and HitAt as a fraction, so the shorter clips need no code change.

## Verify (FBX re-import, every frame)
- Sword-torso min: dualslash 10.24/12.78 cm, crossthrust 19.26/18.95 cm. Sword vs other arm >= 7.55 cm.
- Raised cross-thrust frames: hand-head/shoulder >= 29.4 cm, sword-head/shoulder >= 27.6 cm.
- Cape fresh sim: 0 penetration on both clips.
- Sheet: design/survival-theme-a-fantasy/new_2026-11-03/kaelen/sheets/fix6/KAELEN_FIX6_SHEET.png
