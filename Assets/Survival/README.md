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
3. Game view **1080×1920 portrait** (also check a wider/taller size — splash must fill edges, no letterbox bars). Click **Play**.
4. Splash keep fills the screen for **~5 seconds** with animated `Loading.` / `Loading..` / `Loading...`, then Play crusade world (no tap to skip).
5. Tap **GATHER · Quarry** → Stone Quarry inspect → **Mine** → march for the listed time (e.g. 2m 12s, live countdown) → **+840 Stone** → short return HOME.
6. Tap **FIGHT · Dark Keep** → Dark Keep inspect (Threat · Darkness · March + battle · **Attack**) → march → purple pulse / **+1,200 Gold** → return HOME.
7. Tap **EXPLORE · Ruins** → Ruins inspect (Relics · March + scout · **Explore**) → march → **+600 Gold** → return HOME.

Direct Play shell (skips splash): `Assets/Survival/Scenes/Play.unity`.

Sir Aldric **3D Animator** (high-angle rear, walk+attack toward TOP): open **`Assets/Survival/Scenes/SirAldric.unity`**, Game view **1080×1920**, Play. Skinned blockout proxy (`SirAldric3DActor`). Painted mid-poly upgrade: `Docs/Survival/previews/ART_UPGRADE.md`. Play hub still uses locked rear PNG `SIR_ALDRIC_REAR_MASTER_LOCKED.png` until Design PASS. HUD portrait stays `theme_a_hero_knight_01`.

Menu **Survival → Use Theme A Flavor (fantasy_kingdom_a)** puts Splash then Play first in Editor Build Settings (Grove `Board` stays listed).

Visual / playable: Design phone Game-view re-gate before merge. Do not self-merge.

## IDs

Logic uses SURV-P0 stable IDs only (`sys.*`, `building.*`, `world.node_type.*`, `world.node.gather_01` / `fight_01` / `explore_01`, `world.action.gather` / `scout` / `march`, `battle.action.start`, `energy.action.gather` / `battle` / `march`). Stone/Gold are Theme A HUD chips (`res_stone` / `res_soft`), not new `res.*` ids. Theme A copy/art is `theme_a.*` inside the pack. Do not add `grove.*` / merge-board IDs.

Headless: `dotnet test tests/Survival.Domain.Tests/Survival.Domain.Tests.csproj`
