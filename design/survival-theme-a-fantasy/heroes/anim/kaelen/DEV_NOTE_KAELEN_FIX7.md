# Kaelen FIX7 — swords never touch: cross thrust re-key + idle + dualslash fixes

Derek on FIX6 (main e4385005): "the cross thrust should raise both hands straight up into the air then strike down
across in front of the body. the sword should not hit each other or any other part of his body".

## Changes (LOCAL branch kaelen-fix7; not pushed until Derek approves)
- attack2_crossthrust re-keyed (53f -> 48f, 1.60 s, keeps the ~1.5x pace):
  - f1-4 guard: blades separated in height (L hand 7 cm lower, R 6.5 cm higher), 22 cm sword gap.
  - f4-18 wind-up: both hands rise straight up over the shoulders, wrists 1.98 / 1.95 m at the top (f18; shoulders 1.47, head top ~1.85).
  - f18-28 strike: in-plane chop forward and down; blades end pointing down and inward, crossing in an X in front of the torso.
    The R arm trails the L by 3 frames and the hands are offset in depth/height (L forward/lower, R back/higher), so the blades pass on separate planes.
  - f28-31 impact hold, f31-48 recovery back to guard with the blades forward.
- Wrists as FIX5/FIX6: hand twist 0, constant 12 deg flex; the forearm takes the twist (<=7 deg/frame, 0 flips). Grips unchanged.
- Feet pinned.
- Skeleton hash unchanged: 6212627f289a1abb. Clip names unchanged. No .cs edits; HitAt 0.5 = f24.5, blades come down f25-28.

## Verify (FBX re-import, every frame)
- Sword-sword min 3.05 cm at f34 (mesh-to-mesh, 0 overlap frames). Impact f28 ~4 cm.
- Sword-body 5.33/4.66 cm (f1 guard, as FIX6); sword-torso 19.0/18.9; sword-head/shoulder 23.9/20.7; sword-other arm 13.0/22.3.
- Hand-torso 21.7/19.2; hand-head/shoulder 22.1/19.4; forearm-torso 8.2/2.5; arm-arm >= 21.9 cm. No penetration.
- Cape fresh sim (2 loops): 0 penetration, min 0.05 cm. Foot slip 0.05 cm.
- Sheet: design/survival-theme-a-fantasy/new_2026-11-03/kaelen/sheets/fix7/KAELEN_FIX7_SHEET.png (+ clips/ front/side MP4)

## Idle (commit 2)
- Problem: the two swords interpenetrated on every frame (0.0 cm, blades through each other in an X in front of the hips).
- Fix: L elbow +16 deg flexion about its own hinge axis, constant on all 299 frames (L hand ~4 cm forward/up), so the L blade sits above/in front of the R blade. R arm, hands, legs untouched; still a relaxed low guard.
- Verify (FBX re-import): sword-sword min 5.09 cm at f108, 0 overlap frames. Sword-body 5.33/3.37 cm (R side 3.37 = unchanged from FIX6), sword-torso 7.8/3.4, hand-torso 6.0/3.7, forearm-torso 3.2/3.7, sword-head/shoulder 38.7/45.2. Hand twist 0, flex 12 const, forearm twist maxD 0.51/0.61 deg/f, palm 2.42/2.00, gap 0.15. Foot slip 0.448 cm = identical to FIX6 idle (legs not touched).

## Dualslash (commit 2)
- Problem: swords touched at f31 (0.0 cm, one overlap frame).
- Fix: the R (stationary) arm only — shoulder rotation bump [-6,-3,3] deg rotvec (6.7 deg peak at f32, gaussian sigma 6 f), R hand moves at most ~4 cm. The L slash arc and the 58f / 1.55x timing are untouched.
- Verify (FBX re-import): sword-sword min 3.17 cm at f31, 0 overlap frames (clr7 swsw 3.45 at f36). Sword-body 5.33/4.66, sword-torso 10.2/12.8, sword-other arm 6.8/10.4, hand-torso 18.5/17.2, forearm-torso 3.3/3.8. Hand twist 0, flex 12 const, forearm twist maxD 7.5/7.46 deg/f, palm 2.42/2.00, gap 0.15. Foot slip 0.0.
- Crossthrust re-checked after this commit: 3.05 cm at f34 (unchanged; its FBX is the commit-1 file).
- Sheet 2: sheets/fix7/KAELEN_FIX7_SHEET2_idle_dualslash.png; MP4s sheets/fix7/clips/KAELEN_FIX7_{idle,attack_dualslash}_{front,side}.mp4.

## Cape sim settings used (kf3/sim.sh, capeanim_f3.py ... kaelen OUT attack)
CAPEBLEND=kf3/KAELEN_cape_cs2.blend CAPEVER=derekskel RESTCLAMP=1 ARMR=0.10 FARMR=0.08 STIFF=1.0 (STIFF01=STIFF) DRAG=0.4 GRAV=0.4 hit=0.05 LEGHIT=0.10 COLIT=1 TORSOSCALE=1.0 ARMLV=0 NORENDER=1, NF = 2 loops (crossthrust 96, dualslash 116, idle 299).
- crossthrust: 0 pen, min 0.054 cm. dualslash: 1 vert 0.013 cm at f13 (identical on FIX6's own f6ds blend -> settings difference vs FIX6's 0). idle: 1 vert <=0.04 cm at f106-110 (identical on the unmodified FIX6 idle). Cape left alone.

## Flags (pre-existing, not changed)
- Cape sim tiny 1-vertex contacts on dualslash f13 / idle f106-110 with the settings above (reproduce on unmodified FIX6 clips).
- The crossthrust guard pose differs from idle, so blending idle -> attack has a small pose change.
