# Theme A hook UI kit

PNG kit for the world + quest hook slice (Castle / Farm / Lumber Mill cards, quest scroll, Gem Shop, Speed Up, celebration).

- `gen_hook_ui.py` writes every kit PNG into `Assets/ThemePack/fantasy_kingdom_a/art/hook/{ui,icons,fx}` (RGBA, supersampled; navy `#1b3060` + gold trim to match the hero art). Helpers: `hookart_base.py`, `hookart_ui.py`, `hookart_icons.py`.
- Painted set (primary): `painted/src/*.jpg` are the generated paintings (flat white or black backgrounds, castle used as the style reference). `painted/cutout_all.py` keys them out (border flood-fill on low-saturation near-white, soft drop shadow kept, edge unmix from white; rays keyed from black), then `painted/finalize.py` trims, resizes and writes `Assets/ThemePack/fantasy_kingdom_a/art/hook/painted/{buildings,icons,ui,fx}`. Run both from `painted/`. The header crown is erased so the banner stretches cleanly.
- Buildings: `theme_a.hook.plot.*` and `theme_a.building.hq.keep` point at `art/hook/painted/buildings/*` (castle, farm, lumber_mill, quarry).
- Fallback: the code-drawn kit from `gen_hook_ui.py` stays at `art/hook/{ui,icons,fx}` and is bound in `pack.json` `art_fallback` / `art_fallback_slices`; it only loads when a painted file is missing.
- `mock_hook_screens.py` composes 1080x1920 layout mocks (main view, Speed Up sheet, Gem Shop, celebration) with the same anchors as `SurvivalPlayView` / `SurvivalHookView`; `mocks/hook_visual_contact_sheet.jpg` is the latest (painted set). It is a layout/art check, not a Unity capture.
- To swap art, drop a PNG at the same path (or rebind the key in `pack.json`); 9-slice art also needs its `art_slices` border (painted: panel 40, buttons 40/38, ribbons 84/22, scroll 50/40, header 110/44).
