# Kaelen FIX7 — cross thrust: hands straight up, strike down + across, blades never touch

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
- Feet pinned. Only the attack2_crossthrust action changed in KAELEN.blend; dualslash, idle and walk keys are byte-identical (FBX for them not re-exported).
- Skeleton hash unchanged: 6212627f289a1abb. Clip names unchanged. No .cs edits; HitAt 0.5 = f24.5, blades come down f25-28.

## Verify (FBX re-import, every frame)
- Sword-sword min 3.05 cm at f34 (mesh-to-mesh, 0 overlap frames). Impact f28 ~4 cm.
- Sword-body 5.33/4.66 cm (f1 guard, as FIX6); sword-torso 19.0/18.9; sword-head/shoulder 23.9/20.7; sword-other arm 13.0/22.3.
- Hand-torso 21.7/19.2; hand-head/shoulder 22.1/19.4; forearm-torso 8.2/2.5; arm-arm >= 21.9 cm. No penetration.
- Cape fresh sim (2 loops): 0 penetration, min 0.05 cm. Foot slip 0.05 cm.
- Sheet: design/survival-theme-a-fantasy/new_2026-11-03/kaelen/sheets/fix7/KAELEN_FIX7_SHEET.png (+ clips/ front/side MP4)

## Flags (pre-existing, not changed)
- idle: the two swords overlap on every frame.
- attack_dualslash: swords touch at f31 (0.0 cm), and a fresh cape sim now shows 1 vertex 0.1 mm at f13 (FIX6 reported 0).
- The crossthrust guard pose differs from idle, so blending idle -> attack has a small pose change.
