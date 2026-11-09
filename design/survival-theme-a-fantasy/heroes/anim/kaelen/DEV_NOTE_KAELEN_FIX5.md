# Kaelen FIX5 — natural wrists + seated grip

Derek FAIL on Unity main 233d4a15 / PR #181: "kaelens writs bend unaturally"
(screenshot: kinked wrists + left hand off hilt).

## Changes (LOCAL branch kaelen-fix5; not pushed until Derek approves)
- Rest grip: radial tighten + seat swords toward palm (palm→axis L 2.42 / R 2.00 cm; inner gap 0.15 cm).
- Animation: lock hand local twist=0, flex=12°; rate-limit forearm twist ≤7.5°/f (absorbs prior wrist wind).
- Applied to attack_dualslash, attack2_crossthrust, idle, walk.
- Skeleton hash unchanged: 6212627f289a1abb. Clip names unchanged. No .cs edits.

## Verify
- Sword–body min (dualslash): L 5.22 / R 4.60 cm. Cape sim: 0 pen.
- Sheet: design/.../kaelen/sheets/fix5/KAELEN_FIX5_SHEET.png
