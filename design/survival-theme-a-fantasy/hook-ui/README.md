# Theme A hook UI kit

PNG kit for the world + quest hook slice (Castle / Farm / Lumber Mill cards, quest scroll, Gem Shop, Speed Up, celebration).

- `gen_hook_ui.py` writes every kit PNG into `Assets/ThemePack/fantasy_kingdom_a/art/hook/{ui,icons,fx}` (RGBA, supersampled; navy `#1b3060` + gold trim to match the hero art). Helpers: `hookart_base.py`, `hookart_ui.py`, `hookart_icons.py`.
- Painted set (primary): `painted/src/*.jpg` are the generated paintings (flat white or black backgrounds, castle used as the style reference). `painted/cutout_all.py` keys them out (border flood-fill on low-saturation near-white, soft drop shadow kept, edge unmix from white; rays keyed from black), then `painted/finalize.py` trims, resizes and writes `Assets/ThemePack/fantasy_kingdom_a/art/hook/painted/{buildings,icons,ui,fx}`. Run both from `painted/`. The header crown is erased so the banner stretches cleanly.
- Buildings: `theme_a.hook.plot.*` and `theme_a.building.hq.keep` point at `art/hook/painted/buildings/*` (castle, farm, lumber_mill, quarry).
- Fallback: the code-drawn kit from `gen_hook_ui.py` stays at `art/hook/{ui,icons,fx}` and is bound in `pack.json` `art_fallback` / `art_fallback_slices`; it only loads when a painted file is missing.
- `mock_hook_screens.py` composes 1080x1920 layout mocks (main view, Speed Up sheet, Gem Shop, celebration) with the same anchors as `SurvivalPlayView` / `SurvivalHookView`; `mocks/hook_visual_contact_sheet.jpg` is the latest (painted set). It is a layout/art check, not a Unity capture.
- To swap art, drop a PNG at the same path (or rebind the key in `pack.json`); 9-slice art also needs its `art_slices` border (painted: panel 40, buttons 40/38, ribbons 84/22, scroll 50/40, header 110/44).

## World + Village screens (Survival Play)

The Play scene now has two scrollable screens and a World/Village toggle (`hud_layout.json` `view_toggle`; the bottom bar no longer has Base / World tabs):

- **Village** (`SurvivalVillageView` + the plot cards in `SurvivalHookView`): `map/village_layout.json` is 3600x4800 (was 2160x2880; zoom 0.5-1.6, start 0.72 just south of the Castle) and places the Castle, two Farms, two Lumber Mills and the Forge in the middle of a tiled lawn with decor (mountains, forests, ponds, oaks, rocks, wheat fields, bushes) and 17 free `build_spots`. No roads or paths: grass, decor and buildings only. Every building opens a sheet with Upgrade / Speed Up / Collect on the existing timer + gem economy (`base_economy.json`; Castle Lv 1-20, the others keep Lv 1-6). The Castle has 20 painted stages, one per level (`castle_tiers` -> `theme_a.village.castle.tier_1..20` -> `art/world/village/castle_lv01..20.png`), footprint growing evenly x0.60 -> x1.30, with a burst on every upgrade. Lv 1-4 are wood (palisade + watchtower, taller tower, wooden gate, stone base); Lv 5 / 10 / 15 / 20 are the original four paintings (anchors) and every stage in between adds one feature (stone wall, gateway, turrets, blue roofs + banners, taller keep, wider walls, inner wall, gold trim, outer wall ring, gold trim everywhere, grand gatehouse, inner towers + statues). The other buildings keep their painting and show the `theme_a.ui.level_badge` sign with their level.
- **World** (`SurvivalWorldView`): `map/world_layout.json` is a 3240x4320 kingdom map: the painted crusade map (with the six catalog world nodes) in the middle, 10 villages (yours highlighted), 7 enemy camps (goblin / bandit / undead / dragon, Lv 3-20), 7 resource fields and live mock marches (`WorldSim`). Tap anything for an info card; Attack / Gather send a timed march from your village.
- Camera (`MapCamera` + `SurvivalMapRig`): drag / fling, pinch, mouse wheel, Shift+drag zoom, WASD / arrows to pan, Q/E or +/- to zoom; clamped to the map with a cover-zoom floor.
- Art: `world/src/*.jpg` are the generated paintings (each castle stage was generated from the previous stage plus the nearest anchor as references, so angle, light and footprint stay consistent); run `python3 cut.py` (or `python3 cut.py castle_lv` for just the castle) then `python3 finalize.py` from `world/` to key them out and write `Assets/ThemePack/fantasy_kingdom_a/art/world/{map,village,ui}` (+ `art/hook/painted/buildings/forge.png`); tiles are made seamless and colour-matched to the crusade map.
- `mock_world_village.py` renders 1080x1920 mocks of both screens (Village at Castle Lv 1 / 5 / 12 / 20) plus all 20 castle stages in order from the real pack art and layout JSON; `mocks/world_village_contact_sheet.jpg` is the latest. Layout/art check, not a Unity capture.

### Build (village)

- The painted **Build** button (`hud_layout.json` `build_button`, hammer icon) opens the build menu: Farm, Lumber Mill, Quarry (new: produces stone) and Forge (`base_economy.json` `buildables`), each with its cost, build time and `Built n/max`.
- How many you may build is gated by the Castle: `unlock_at_castle` lists the Castle level for each extra copy (Farm 2/4/7/11, Lumber Mill 2/5/8/12, Quarry 3/6/10/15, Forge 5/12). Base costs: Farm 600 Gold + 400 Wood (1:30), Lumber Mill 600 Gold + 400 Food (1:30), Quarry 900 Gold + 600 Wood (2:30), Forge 1,500 Gold + 800 Wood + 1,000 Stone (5:00); every further copy of the same type costs x1.6 and takes x1.5 longer.
- Pick a type, then tap a glowing free spot; if you don't, it auto-places on the next free spot (nearest the Castle) when the 10 s countdown runs out (looking around restarts it), or tap Auto-place.
- Construction uses the shared build queue (`base.timer.<plot>`), so Finish FREE (< 5 min), gem rush and speed-ups work as for upgrades. It shows the faded building + hammer + timer, opens at Lv 1 with a level badge, and upgrades to Lv 6 (never past the Castle level).
- Save: `VillageSave` keeps plot levels, built buildings (type, spot, level) and running timers in PlayerPrefs `survival.village.save.v1`; Debug **R** clears it (restart Play).

### Bottom bar

- Heroes / Alliance / Bag, evenly spaced with room for more (`BottomNav`). Painted kit: `theme_a.ui.nav_bar` (carved wood/stone, gold trim), `theme_a.ui.nav_button` (gold bevel), `theme_a.ui.icon_heroes|alliance|bag|battle|build`; label under the icon; squash while pressed. Missing art falls back to the blue kit button / code-drawn frame.
- Hooks: `SurvivalPlayView.OnHeroesTapped()` raises `HeroesTapped` (also `AllianceTapped`, `BagTapped`); `AddNavButton(BottomNav.Battle, BottomNav.IconBattle, onTap, index)` adds Battle and re-spaces the bar.
- The red debug buttons are small corner chips now (G gift, T +5 min; keys unchanged).
- Art sources: `world/src/nav_*.jpg`, `icon_*.jpg`, `prop_rocks|field|tree.jpg` -> `world/cut.py` + `world/finalize.py`.
- Mock: `python3 mock_village_build.py` -> `/workspace/world_quest/village_build_and_bar.png`.
