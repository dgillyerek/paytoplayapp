# Project Grove — production-feel slice (Area 1)

**Scene:** `Assets/Grove/Scenes/Board.unity`  
**Engine:** Unity **6.3 LTS** (`6000.3.x`)  
**Art:** DES-001/002 pack at `Assets/Grove/Art/Area1/` (do not regenerate).

## Derek / CEO — what you should see

1. Pull this branch. Unity Hub → **Open** this repo folder (`Assets/` + `ProjectSettings/`). First import may take a minute.
2. Double-click **`Assets/Grove/Scenes/Board.unity`**.
3. Click **Play**.

**On Play:**

- Full-screen splash **Project Grove** (tap).
- Then splash **Front Garden** (tap).
- Board: garden backdrop, wooden tray, cream cell tiles, illustrated pieces (Seed / Sprout / …) — **not** a green grid or pink letter tokens.
- Goal banner: **Restore the Front Garden**.
- HUD: energy bolt + count, **coin** count. Gems stay hidden.
- Maya 2D portrait + up to **3** order cards (Maya lines, item art, coin/XP).
- Wooden **garden crate** at the bottom (tap to produce).

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
