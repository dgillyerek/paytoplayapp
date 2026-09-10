# Project Grove

**QA pass / fail (prototype):** produce from the crate → merge to **Wildflower T3** → **Deliver Order 1**. Stay in Play ≥2 minutes with no softlock or crash. Placeholders are shipping art until Design swaps them.

## Play in Unity (2 minutes)

1. Install **Unity 6.3 LTS** (`6000.3.6f1` or any 6000.3.x).
2. Hub → **Open** → this repo folder (the one with `Assets/` and `ProjectSettings/`).
3. Wait for first import (`Library/` is gitignored).
4. Open `Assets/Grove/Scenes/Board.unity` → **Play**.
5. Click **CRATE** (bottom). A token appears on the **7×5** board.
6. **Drag** matching tokens together. 2 stack (`×2`); the 3rd **merges to the next tier**. Illegal drops **snap back**.
7. Repeat until you have **WF T3**. Click **DELIVER**. Toast: Order 1 complete.
8. Optional: **STARTER PACK** adds 3× WF T1.

Temp sprites/UI are generated in Play Mode (tinted circles + solid buttons). Final art can replace `Resources/Grove/Art/` later — do not block on Design.

| | |
| --- | --- |
| Engine | Unity **6.3 LTS** + **URP 17.3** |
| Loop | Crate → 3-merge wildflowers → Order 1 (T3) |
| Energy | 100 cap, HUD bar, 1/tap after first free tap |
| Orders | 1 = WF T3, 2 = T4, 3 = T5 |

**Not in this slice:** vines, inventory, map, real IAP, FTUE, analytics, dailies, settings.

```bash
dotnet test tests/Grove.Domain.Tests/Grove.Domain.Tests.csproj
```
