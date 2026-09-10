# Tech notes

## Stack

- Unity **6.3 LTS** (`ProjectSettings/ProjectVersion.txt` → `6000.3.6f1`)
- **URP 17.3.0** (`Packages/manifest.json`), pipeline asset `Assets/Settings/URP/GroveURP.asset`
- Color space: Linear (inherited from the 6.3 URP baseline)
- Scripting: IL2CPP on iOS; Android min API **26** (8.0); iOS **15.0**
- Input: Input System package present; old Input Manager kept for editor convenience (`activeInputHandler` can be switched to Both)

## Architecture

```
Unity MonoBehaviours  →  Grove.Domain (no engine refs)
BoardView / DragMergeController     MergeSession / BoardGrid / MergeCatalog
GroveBootstrap                      FakeStore : IPurchaseService
```

Keep **all merge math** in `Grove.Domain`. Unity code may animate, spawn views, and call `TryDrag`. If a rule needs a `#if UNITY_EDITOR`, it is in the wrong assembly.

## Tests

`dotnet test tests/Grove.Domain.Tests` compiles `Assets/Grove/Runtime/**/*.cs` directly. Do not put `UnityEngine` types in Runtime.

## URP

GraphicsSettings and QualitySettings both reference `GroveURP.asset` (guid `7b7fd9122c28c4d15b667c7040e3b3fd`). Global URP settings: `UniversalRenderPipelineGlobalSettings.asset`. First editor open may regenerate package lock and shader cache.
