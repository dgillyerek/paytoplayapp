# Kingdom Defense (Survival core, Theme A)

White-label F2P survival game. Shared systems live under `Assets/Survival/`; Theme A
(`fantasy_kingdom_a`) is the first baked store flavor. Full guide: [Assets/Survival/README.md](Assets/Survival/README.md).

**Engine:** Unity **6.3 LTS** (`6000.3.6f1`), URP.

## Open and play

1. Unity Hub → **Open** this repo folder.
2. Open `Assets/Survival/Scenes/Splash.unity` (or `Play.unity` to skip the splash).
3. Game view **1080×1920 portrait**, then **Play**.

Other scenes: `BattleLocked.unity` (locked heroes-vs-villains battle), `Assets/Survival/Runner/RunnerLevel.unity`
(runner), `NewCharsPreview.unity` (new-character preview).

## Layout

| Path | What |
| --- | --- |
| `Assets/Survival/Runtime/` | Engine-free domain code (`Survival.Domain`), tested headless |
| `Assets/Survival/Unity/` | MonoBehaviours, views, actors (`Survival.Unity`) |
| `Assets/Survival/Editor/` | Editor menus, importers, build scripts |
| `Assets/ThemePack/`, `Assets/AppFlavor/` | Theme A art, strings, store data, flavor |
| `Backend/alliance/` | Alliance / world / IAP / cloud-save API (Neon Function, Node) |
| `tests/Survival.Domain.Tests/` | xUnit tests for the domain |
| `design/`, `ArtSource/` | Art sources (Blender, FBX) and handoffs |
| `Docs/` | Tech notes, IAP, cloud save, Android builds |
| `ci/github-workflows/` | Android build workflow, staged until it moves to `.github/workflows/` |

## Tests

```bash
scripts/test.sh
```

Runs the domain tests (`dotnet test tests/Survival.Domain.Tests`) and the backend tests (`npm test` in `Backend/alliance`).
