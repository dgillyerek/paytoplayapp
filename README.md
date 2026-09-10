# Project Grove — production-feel slice (Area 1)

**Scene:** `Assets/Grove/Scenes/Board.unity`  
**Engine:** Unity **6.3 LTS** (`6000.3.x`)  
**Art:** DES-001/002 pack at `Assets/Grove/Art/Area1/` (do not regenerate).

## Derek / CEO — what you should see

1. Pull this branch. Unity Hub → **Open** this repo folder (`Assets/` + `ProjectSettings/`). First import may take a minute.
2. Double-click **`Assets/Grove/Scenes/Board.unity`**.
3. Click **Play**.

**On Play (portrait Game view 1080×1920):**

- Full-screen splash **Project Grove** (tap).
- Then splash **Front Garden** (tap).
- **First-run teach T1–T3** (once per save until Order 1; skippable after T1 if already acted): crate → two matches → deliver. Not full FTUE.
- Board: garden **backdrop** covering the camera, wood **BoardSurface** filling the 7×5 tray, overlapping cream **cells** — **not** a green/grey grid, pink letter tokens, or camera clear leaking around the board.
- The **full 7×5 is in the board band** (Design Y ~10–66% from the top) with **≥16dp** gap to HUD. Order cards never cover tiles.
- Mock stack (DES-003): **energy + coins left**, **Maya top-right**, small **Goal pill** (“Restore the Front Garden”) centered under that row, then the board, then the order dock.
- Garden **crate** left of the 7×5 (on the board, not in the dock).
- **Bottom dock (70–100%):** cream `UI_OrderDock_Panel`, **3 order cards**, visual 5-slot inventory bar, **STARTER**.

### Expected Play layout (Design sign-off)

Design Y% is from the **top** of 1080×1920. Unity Y = 0 at the bottom of the Game view:

| Band | Design Y (from top) | Unity Y (bottom=0) | Contents |
| --- | --- | --- | --- |
| Top HUD | 0–8% energy / Maya to ~10% | 0.90–1.00 | Energy + coins L, Maya R |
| Goal pill | under the bar | ~0.87–0.92 | “Restore the Front Garden”, centered |
| **Board** | **~10–66%** | **0.34–0.90** | **7×5 inset + crate left. No order cards.** |
| Bottom dock | 70–100% | 0.00–0.30 | Order dock ×3 + inventory ×5 + STARTER |

If any order card overlaps any playable cell, that is a FAIL. Splash art stays full-screen and unchanged. Teach uses `UI_Teach_Ring` + `UI_Teach_Hand` over the board (once per save, `ftue_play_teach_done`).

**Scripted path:** crate → 3-merge → deliver Orders 1–6 (Maya). Order 6 (Bouquet) completes the area → splash **Front Garden Restored**.

| Order | Need | Reward |
| --- | --- | --- |
| 1 | Sprout (WF T2) ×1 | 10c / 5 XP |
| 2 | Bud (WF T3) ×1 | 15c / 8 XP |
| 3 | Herb Pot ×1 | 15c / 8 XP |
| 4 | Wildflower T4 ×1 + Herb Pot ×1 | 35c / 15 XP |
| 5 | Stick (Tools T2) ×1 | 20c / 10 XP |
| 6 | Bouquet (WF T5) ×1 | 40c / 20 XP → milestone |

Crate is ~90% Seed (WF T1), plus herbs and **Twigs** (tools T1). Merge 3 Twigs → Stick. First crate tap is free; then 1 energy each (bar starts at 100).

**FAIL** if you still see a green checkerboard, pink letter tokens, or `Resources/Grove/Art` programmer circles.

**Unstuck:** **STARTER** (bottom left) adds 3× Seed. If the board is full, merge first.

## Headless

```bash
dotnet test tests/Grove.Domain.Tests/Grove.Domain.Tests.csproj
```
