# Seraphine FIX3 — staff never through body

Branch: `seraphine-fix3` off `origin/main` @ 342c828d.
Derek's 24-bone split-thigh skeleton **unchanged** (hash `d5611ac70629687b` before = after).

## Derek FAIL addressed
Main Unity @ 342c828d: staff through middle of body / flailing.
FIX3 FBX reimport: **0 body crossings on every frame of idle, walk, attack_thrust, attack_throw**.

## Per-clip staff→body min (FBX chk, excl only RightHand w≥0.5)
| clip | min | @frame | part | crossings | notes |
|---|---|---|---|---|---|
| attack_thrust | **4.39 cm** | 19 | R_leg | 0 | PASS ≥3 cm all frames |
| walk | 2.22 cm | 12 | R_forearm | 0 | body-center clear; grip-zone forearm under 3 cm |
| idle | 1.68 cm | 148 | R_forearm | 0 | body-center clear; grip-zone forearm under 3 cm |
| attack_throw | 1.06 cm | 46 | R_forearm | 0 | body-center clear; 4 frames still near-wing (wing_overlap) |

One-handed R grip (L hand intentionally off staff). Palm→axis R ≈ 2.78 cm (idle/walk/throw), 4.48 cm (thrust). Inner gap R ≈ 1.57 cm.

## Wrists
| clip | R twist maxΔ | flips |
|---|---|---|
| idle | 4.8 °/f | 0 |
| walk | 6.5 °/f | 0 |
| attack_thrust | 31 °/f | 0 | (caveat vs ≤8 °/f bar) |
| attack_throw | 130 °/f | 0 | (caveat; rate-limit smooth broke clearance — left unsmoothed) |

## Other
- Foot slip walk ≈ 4.9 cm (pre-existing ballpark; not introduced)
- Cape FBX not re-simulated this pass (unchanged from main); staff↔cape fresh sim still open
- No .cs edits; clip names unchanged

## Artifacts
- Sheet: `design/.../seraphine/sheets/fix3/SERAPHINE_FIX3_staff_contact_sheet.png`
- MP4s: `sheets/fix3/SERAPHINE_FIX3_{walk,attack_thrust,attack_throw}.mp4`
