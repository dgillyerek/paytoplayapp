# Survival core (white-label F2P) — Theme A first flavor

**Not Grove.** Shared systems live under `Assets/Survival/` (`survival_core_v1`). Theme A is a baked store flavor, not an in-app skin.

| Piece | Path |
| --- | --- |
| Systems | `Assets/Survival/Runtime/` + `Assets/Survival/Resources/Survival/*.json` |
| Theme pack | `Assets/ThemePack/fantasy_kingdom_a/` |
| App flavor | `Assets/AppFlavor/fantasy_kingdom_a/flavor.json` |
| Splash scene | `Assets/Survival/Scenes/Splash.unity` |
| Play scene | `Assets/Survival/Scenes/Play.unity` |
| Design stills | `design/survival-theme-a-fantasy/play-mock/` |
| Gather juice SoT | `design/survival-theme-a-fantasy/juice/quarry-loop-stills/` |

## Open in Unity 6.3 LTS (`6000.3.6f1`)

1. Open this repo folder.
2. Double-click **`Assets/Survival/Scenes/Splash.unity`**.
3. Game view **1080×1920 portrait**. Click **Play**.
4. Tap the keep splash (PASS — do not regress) → Play crusade world.
5. Tap **GATHER · Quarry** (map-size quarry under the pin) → Stone Quarry inspect.
6. Tap **Mine** → march from HOME, arrive, **+840 Stone**, then return toward HOME.

Direct Play shell (skips splash): `Assets/Survival/Scenes/Play.unity`.

Menu **Survival → Use Theme A Flavor (fantasy_kingdom_a)** puts Splash then Play first in Editor Build Settings (Grove `Board` stays listed).

Visual / playable: Design phone Game-view re-gate before merge. Do not self-merge.

## IDs

Logic uses SURV-P0 stable IDs only (`sys.*`, `building.*`, `world.node_type.*`, `world.node.gather_01`, `world.action.gather` / `world.action.march`, `energy.action.gather`). Stone is a Theme A HUD chip (`res_stone` layout slot), not a new `res.*` id. Theme A copy/art is `theme_a.*` inside the pack. Do not add `grove.*` / merge-board IDs.

Headless: `dotnet test tests/Survival.Domain.Tests/Survival.Domain.Tests.csproj`
