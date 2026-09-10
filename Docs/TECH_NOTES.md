# Tech notes

## Stack

- Unity **6.3 LTS** (`ProjectSettings/ProjectVersion.txt` → `6000.3.6f1`)
- **URP 17.3.0** (`Packages/manifest.json`), pipeline asset `Assets/Settings/URP/GroveURP.asset`
- Color space: Linear (inherited from the 6.3 URP baseline)
- Scripting: IL2CPP on iOS; Android min API **26** (8.0); iOS **15.0**
- Input: **Both** Input Manager + Input System (`activeInputHandler: 2`) so Editor Play mouse-drag works

## Architecture

```
Unity MonoBehaviours  →  Grove.Domain (no engine refs)
BoardView / DragMergeController / PrototypeHud
MergeSession / BoardGrid / CatalogLoader / GardenCrate / EnergyWallet / OrderBoard
```

Keep **all merge / crate / energy / order math** in `Grove.Domain`. Unity code animates, draws greybox quads, and calls `TryDrag` / `TryTap` / `TryDeliver`.

Play Mode builds the HUD at runtime (no extra scene wiring). Input Manager + Input System are both enabled (`activeInputHandler: 2`) so Editor mouse drag works.

## Tests

`dotnet test tests/Grove.Domain.Tests` compiles `Assets/Grove/Runtime/**/*.cs` directly. Do not put `UnityEngine` types in Runtime.

## URP

GraphicsSettings and QualitySettings both reference `GroveURP.asset` (guid `7b7fd9122c28c4d15b667c7040e3b3fd`). Global URP settings: `UniversalRenderPipelineGlobalSettings.asset`. First editor open may regenerate package lock and shader cache.
