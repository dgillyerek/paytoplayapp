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
- **First-run coach marks** (3 steps, tap “Got it” or do the action): crate → merge → deliver. Not full FTUE.
- Board: garden **backdrop** covering the camera, wood **BoardSurface** filling the 7×5 tray, overlapping cream **cells** — **not** a green/grey grid, pink letter tokens, or camera clear leaking around the board.
- The **full 7×5 is in the mid band** (about 24–70% up the screen). HUD never covers tiles.
- Goal banner at the top: **Restore the Front Garden**.
- HUD under the goal: energy bolt + count (left), **coin** count (right). Gems stay hidden.
- Maya 2D portrait (top-left) + **3 order cards in a horizontal row** (Maya lines live on the cards as item/progress; Deliver on the active card).
- Wooden **garden crate** at the bottom center (tap to produce). **STARTER** bottom-left.

### Expected Play layout (Design sign-off)

Normalized Y = 0 at the bottom of the Game view:

| Band | Y (bottom–top) | Contents |
| --- | --- | --- |
| Top chrome | 0.85–0.95 | Goal, energy, coins |
| Order row | 0.72–0.84 | Maya + 3 order cards across |
| **Playfield** | **0.24–0.70** | **Entire 7×5 grid, tappable** |
| Bottom chrome | 0.05–0.22 | Crate + charges + STARTER |

If the order tray or Maya overlaps any cell, that is a FAIL. Splash art stays full-screen and unchanged.

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
