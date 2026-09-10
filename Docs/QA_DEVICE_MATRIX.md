# QA device matrix (DEV-001 / P0)

Minimum store targets: **Android 8.0 (API 26)** and **iOS 15**.

| Class | Example | OS | Why |
| --- | --- | --- | --- |
| Android low | Pixel 3a / A-series | 10–13 | Thermal, 720p UI scale, 3→1 drag hitboxes |
| Android mid | Pixel 6 / Galaxy A54 | 13–15 | Daily driver |
| Android new | Pixel 8 / S24 | 15–16 | Gesture nav, 120 Hz |
| iOS low | iPhone 8 / SE 2 | iOS 15–16 | Min GPU / RAM |
| iOS mid | iPhone 12 / 13 | iOS 16–18 | Notch safe area |
| iOS new | iPhone 15 / 16 | iOS 18+ | Dynamic Island |
| Tablet (later) | iPad mini / Tab A | — | Not P0; board is 7×5 portrait-first |

## Must-pass on every P0 device

1. Board is 7 columns × 5 rows and fully tappable.
2. Three matching pieces merge to one next-tier piece.
3. Illegal drop **snaps back**; no duplicate / missing pieces.
4. App recovers from backgrounding mid-drag (piece returns to origin).
5. FakeStore purchase stub does not block the board.

Record GPU, thermal throttling, and drag latency. No paid device farm required for DEV-001 — use office devices + emulator/simulator.
