# Project Grove — prototype QA

**Scene:** `Assets/Grove/Scenes/Board.unity`  
**Engine:** Unity **6.3 LTS** (`6000.3.x`)  
**Placeholders are expected.** Do not fail on temp circles / solid UI.

## Pass / fail

| | |
| --- | --- |
| **PASS** | Produce from **CRATE** → merge to **Wildflower T3** (`WF T3`) → **DELIVER** Order 1, then stay in Play **≥ 2 minutes** with no crash and no stuck board (you can still crate/merge). |
| **FAIL** | Cannot produce, cannot 3-merge, cannot deliver Order 1, crash, or hard-stuck (no empty cell and no legal merge) within 2 minutes. |

You need **9× WF T1** → three T2 merges → **one T3**. Crate is ~90% WF T1. First crate tap is free; then 1 energy each (bar starts at 100).

## Editor play steps

1. Unity Hub → **Open** → this repo folder (contains `Assets/` + `ProjectSettings/`). First import may take a minute (`Library/` is gitignored).
2. Double-click **`Assets/Grove/Scenes/Board.unity`**.
3. Click **Play**. You should see a **7×5** green grid, one pink **WF T1**, **Energy**, **Order 1**, and **CRATE**.
4. Click **CRATE** (bottom center). A new token lands on an empty cell.
5. **Drag** a WF onto another WF. Two become `WF T1 ×2`. Drag a third WF onto that stack → **WF T2** (piece punches). Wrong-type drops **snap back**.
6. Repeat crate + merge until a token reads **WF T3**.
7. Click **DELIVER** (right). Toast: Order 1 complete.
8. Keep clicking CRATE / merging for **two more minutes**. Pass if nothing crashes or dead-ends.

**Unstuck:** **STARTER PACK** (bottom left) adds 3× WF T1. If the board is full, merge first.

## Not this test

Orders 2–3, real IAP, inventory, map, vines, FTUE, analytics. Headless check (optional):

```bash
dotnet test tests/Grove.Domain.Tests/Grove.Domain.Tests.csproj
```
