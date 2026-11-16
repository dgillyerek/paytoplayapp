# Tech notes

## Stack

- Unity **6.3 LTS** (`ProjectSettings/ProjectVersion.txt` → `6000.3.6f1`)
- **URP 17.3.0** (`Packages/manifest.json`), pipeline asset `Assets/Settings/URP/KingdomURP.asset`
- Color space: Linear (inherited from the 6.3 URP baseline)
- Scripting: IL2CPP on iOS; Android min API **26** (8.0); iOS **15.0**
- Input: **Both** Input Manager + Input System (`activeInputHandler: 2`) so Editor Play mouse-drag works

## Architecture

```
Survival.Unity (MonoBehaviours, views, actors)  →  Survival.Domain (no engine refs)
Survival.Editor (menus, importers, builds)
```

Keep game rules (economy, campaign, runner, battle math) in `Assets/Survival/Runtime` (`Survival.Domain`). Unity code
draws, animates and forwards input.

## Tests

`dotnet test tests/Survival.Domain.Tests` compiles `Assets/Survival/Runtime/**/*.cs` directly. Do not put `UnityEngine`
types in Runtime.

## URP

GraphicsSettings and QualitySettings both reference `KingdomURP.asset` (guid `7b7fd9122c28c4d15b667c7040e3b3fd`). Global URP settings: `UniversalRenderPipelineGlobalSettings.asset`. First editor open may regenerate package lock and shader cache.
