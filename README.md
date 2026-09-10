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
- The **full 7×5 is in the mid board-safe band** (~26–86% up the screen) with **≥16dp** gap to HUD. Tray/Maya/crate never cover tiles.
- Goal banner at the top: **Restore the Front Garden**.
- HUD top: energy bolt + count (left), **coin** count (right). Gems stay hidden.
- **Bottom dock:** Maya 2D portrait + one-line bubble + **3 order cards**. Wooden **garden crate** under the dock. **STARTER** bottom-left.

### Expected Play layout (Design sign-off)

Normalized Y = 0 at the bottom of the Game view:

| Band | Y (bottom–top) | Contents |
| --- | --- | --- |
| Top chrome | 0.87–0.98 | Goal, energy, coins |
| **Board safe** | **0.26–0.86** | **Entire 7×5, tappable** |
| Bottom dock | 0.15–0.25 | Maya + one-line bubble + 3 order cards |
| Producers | 0.01–0.14 | Crate + STARTER |

If the order tray, Maya, or crate overlaps any cell, that is a FAIL. Splash art stays full-screen and unchanged.

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
