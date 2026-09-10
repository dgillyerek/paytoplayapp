# Project Grove

F2P merge + progression game for **Android 8+** and **iOS 15+**.

Canonical Origin repo: [`derek-gilbert/paytoplayapp`](https://origin.cursor.com/derek-gilbert/paytoplayapp).

| | |
| --- | --- |
| Engine | **Unity 6.3 LTS** (`6000.3.6f1`) |
| Render pipeline | **URP 17.3.0** |
| Board | **7×5** |
| Core loop (DEV-001) | Data-driven **3→1** drag merge with snap-back |
| IAP | `IPurchaseService` + `FakeStore` stub |
| Art | 2D UI / sprites only (no 3D Maya runtime) |

## Open in Unity

1. Install **Unity 6.3 LTS** (6000.3.x).
2. Open this folder as a Unity project (URP is already in `Packages/manifest.json` and assigned in Graphics / Quality).
3. Play `Assets/Grove/Scenes/Board.unity`.

First open will generate `Library/` (gitignored).

## DEV-001 merge loop

Domain lives in `Assets/Grove/Runtime` with **no UnityEngine references** (`Grove.Domain.asmdef`, `noEngineReferences: true`).

- Drag a stack onto an empty cell → move.
- Drag onto the **same piece id** → stack.
- Stack count reaching the recipe input (**3**) → **1** next-tier piece at the drop cell.
- Illegal drop → `DragResult.SnapBack` and the board is unchanged (view snaps the piece back).

Default chain (data-driven, not hardcoded in the loop):

`pebble ×3 → sprout ×3 → sapling ×3 → tree ×3 → grove`

## Tests (no Unity license / no paid CI)

```bash
dotnet test tests/Grove.Domain.Tests/Grove.Domain.Tests.csproj
```

The test project compiles the same Runtime `.cs` files. Keep merge rules here so CI can stay free.

## Layout

```
Assets/Grove/Runtime/     # board, merge, FakeStore (pure C#)
Assets/Grove/Unity/       # MonoBehaviours: bootstrap, board view, drag controller
Assets/Grove/Scenes/      # Board.unity
Assets/Settings/URP/      # URP asset, renderer, global settings
Docs/                     # tech notes, QA matrix, P0 order, art budget, migrate
ProjectSettings/          # Unity 6.3 LTS player (Android 26 / iOS 15)
Packages/                 # URP 17.3.0 + UGUI + Input System
tests/Grove.Domain.Tests/ # xUnit
```

## Docs

- [DEV-001](Docs/DEV-001.md)
- [Tech notes](Docs/TECH_NOTES.md)
- [QA device matrix](Docs/QA_DEVICE_MATRIX.md)
- [P0 build order](Docs/P0_BUILD_ORDER.md)
- [Art budget](Docs/ART_BUDGET.md)
- [Migrate / open notes](Docs/MIGRATE.md)
