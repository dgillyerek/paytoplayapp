# Project Grove

F2P merge + progression game for **Android 8+** and **iOS 15+**.

Canonical Origin repo: [`derek-gilbert/paytoplayapp`](https://origin.cursor.com/derek-gilbert/paytoplayapp).

| | |
| --- | --- |
| Engine | **Unity 6.3 LTS** (`6000.3.6f1`) |
| Render pipeline | **URP 17.3.0** |
| Board | **7×5** greybox |
| Prototype loop | **Crate → 3-merge wildflowers → Order 1 (WF T3)** |
| Art | Solid-color placeholders (2D UI / quads only) |

## Play in Unity (Derek)

Exit criteria: **produce → merge to Wildflower T3 → complete Order 1**, no crash for a couple of minutes.

1. Install **Unity 6.3 LTS** (`6000.3.6f1` or any 6000.3.x).
2. Unity Hub → **Open** → select **this repo folder** (the folder that contains `Assets/` and `ProjectSettings/`). Do not nest another project.
3. Wait for the first import (`Library/` is gitignored; URP shaders may compile for a minute).
4. Double-click `Assets/Grove/Scenes/Board.unity`.
5. Set the **Game** view to a portrait-ish size if you can (free aspect is fine). Click **Play** (Ctrl/Cmd+P).
6. You should see a **7×5 green grid**, one pink **WF T1**, an **Energy** bar, **Order 1**, and a big **CRATE** button.
7. Click **CRATE** (bottom). A new piece appears on the board (usually Wildflower T1).
8. **Drag** a piece onto another of the **same type**. Two stack (`×2`). A third matching drop **merges to the next tier** with a scale punch. Illegal drops **snap back**.
9. Keep producing and merging until you have **Wildflower T3**. Click **DELIVER** (Order 1). Toast should say Order 1 complete.
10. Optional: **STARTER PACK** (FakeStore) dumps 3× WF T1 onto empty cells.

If the Game view is empty: select **Main Camera**, confirm Orthographic, and click Play again. Graphics settings should already point at `Assets/Settings/URP/GroveURP.asset`.

## Prototype scope (IN)

1. **DEV-001** — 7×5 board, 3→1 drag-merge, visible snap-back / merge punch.
2. **DEV-002** — Wildflower T1–5 (JSON; T6–8 also in data). Herb/Tools are stubs.
3. **DEV-003** — Garden Crate (30 charges, 2s recharge, ~70% WF T1).
4. **DEV-004** — Energy 100 cap, 1 per crate tap, HUD bar.
5. **DEV-005** — Orders 1–3 only (T3 / T4 / T5).

**OUT:** vines/debris, inventory, map, real IAP, FTUE, analytics, daily, settings, all P1.

## Tests (no Unity license / no paid CI)

```bash
dotnet test tests/Grove.Domain.Tests/Grove.Domain.Tests.csproj
```

Headless coverage includes the prototype loop: crate spit → merge to WF T3 → Order 1.

## Layout

```
Assets/Grove/Resources/Grove/  # items, recipes, crate, energy, orders JSON
Assets/Grove/Runtime/          # board, merge, crate, energy, orders (pure C#)
Assets/Grove/Unity/            # Play Mode visuals + HUD
Assets/Grove/Scenes/Board.unity
Docs/
tests/Grove.Domain.Tests/
```

QA can change tiers/recipes/weights in the JSON files without editing merge logic.

## Docs

- [DEV-001](Docs/DEV-001.md) merge loop
- [DEV-002](Docs/DEV-002.md) item/recipe data
- [DEV-003](Docs/DEV-003.md) garden crate
- [DEV-004](Docs/DEV-004.md) energy
- [DEV-005](Docs/DEV-005.md) orders 1–3
- [Tech notes](Docs/TECH_NOTES.md)
- [P0 build order](Docs/P0_BUILD_ORDER.md)
