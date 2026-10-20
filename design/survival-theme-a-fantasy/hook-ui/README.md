# Theme A hook UI kit

PNG kit for the world + quest hook slice (Castle / Farm / Lumber Mill cards, quest scroll, Gem Shop, Speed Up, celebration).

- `gen_hook_ui.py` writes every kit PNG into `Assets/ThemePack/fantasy_kingdom_a/art/hook/{ui,icons,fx}` (RGBA, supersampled; navy `#1b3060` + gold trim to match the hero art). Helpers: `hookart_base.py`, `hookart_ui.py`, `hookart_icons.py`.
- Painted set (primary): `painted/src/*.jpg` are the generated paintings (flat white or black backgrounds, castle used as the style reference). `painted/cutout_all.py` keys them out (border flood-fill on low-saturation near-white, soft drop shadow kept, edge unmix from white; rays keyed from black), then `painted/finalize.py` trims, resizes and writes `Assets/ThemePack/fantasy_kingdom_a/art/hook/painted/{buildings,icons,ui,fx}`. Run both from `painted/`. The header crown is erased so the banner stretches cleanly.
- Buildings: `theme_a.hook.plot.*` and `theme_a.building.hq.keep` point at `art/hook/painted/buildings/*` (castle, farm, lumber_mill, quarry).
- Fallback: the code-drawn kit from `gen_hook_ui.py` stays at `art/hook/{ui,icons,fx}` and is bound in `pack.json` `art_fallback` / `art_fallback_slices`; it only loads when a painted file is missing.
- `mock_hook_screens.py` composes 1080x1920 layout mocks (main view, Speed Up sheet, Gem Shop, celebration) with the same anchors as `SurvivalPlayView` / `SurvivalHookView`; `mocks/hook_visual_contact_sheet.jpg` is the latest (painted set). It is a layout/art check, not a Unity capture.
- To swap art, drop a PNG at the same path (or rebind the key in `pack.json`); 9-slice art also needs its `art_slices` border (painted: panel 40, buttons 40/38, ribbons 84/22, scroll 50/40, header 110/44).

## World + Village screens (Survival Play)

The Play scene now has two scrollable screens and a World/Village toggle (`hud_layout.json` `view_toggle`; the nav Base / World tabs switch too):

- **Village** (`SurvivalVillageView` + the plot cards in `SurvivalHookView`): `map/village_layout.json` places the Castle, two Farms, two Lumber Mills and the Forge on a tiled lawn with roads, props and 4 empty plots (unlock at Castle Lv 3-6). Every building opens a sheet with Upgrade / Speed Up / Collect on the existing timer + gem economy (`base_economy.json`). The Castle swaps art by `castle_tiers` (Lv 1 / 3 / 5 / 7 -> `theme_a.village.castle.tier_1..4`, footprint x0.78 -> x1.26) with a burst when it grows; the other buildings keep their painting and show the `theme_a.ui.level_badge` sign with their level.
- **World** (`SurvivalWorldView`): `map/world_layout.json` is a 3240x4320 kingdom map: the painted crusade map (with the six catalog world nodes) in the middle, 10 villages (yours highlighted), 7 enemy camps (goblin / bandit / undead / dragon, Lv 3-20), 7 resource fields and live mock marches (`WorldSim`). Tap anything for an info card; Attack / Gather send a timed march from your village.
- Camera (`MapCamera` + `SurvivalMapRig`): drag / fling, pinch, mouse wheel, Shift+drag zoom, WASD / arrows to pan, Q/E or +/- to zoom; clamped to the map with a cover-zoom floor.
- Art: `world/src/*.jpg` are the generated paintings; run `python3 cut.py` then `python3 finalize.py` from `world/` to key them out and write `Assets/ThemePack/fantasy_kingdom_a/art/world/{map,village,ui}` (+ `art/hook/painted/buildings/forge.png`); tiles are made seamless and colour-matched to the crusade map.
- `mock_world_village.py` renders 1080x1920 mocks of both screens (Village at each castle tier) plus a castle-tier strip from the real pack art and layout JSON; `mocks/world_village_contact_sheet.jpg` is the latest. Layout/art check, not a Unity capture.
