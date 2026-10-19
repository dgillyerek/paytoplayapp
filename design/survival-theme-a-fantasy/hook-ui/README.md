# Theme A hook UI kit

PNG kit for the world + quest hook slice (Castle / Farm / Lumber Mill cards, quest scroll, Gem Shop, Speed Up, celebration).

- `gen_hook_ui.py` writes every kit PNG into `Assets/ThemePack/fantasy_kingdom_a/art/hook/{ui,icons,fx}` (RGBA, supersampled; navy `#1b3060` + gold trim to match the hero art). Helpers: `hookart_base.py`, `hookart_ui.py`, `hookart_icons.py`.
- Buildings reuse the painted Theme A world-node art (`art/world-nodes/*`), bound per plot in `pack.json` (`theme_a.hook.plot.*`).
- `mock_hook_screens.py` composes 1080x1920 layout mocks (main view, Speed Up sheet, Gem Shop, celebration) with the same anchors as `SurvivalPlayView` / `SurvivalHookView`; `mocks/hook_visual_contact_sheet.jpg` is the latest. It is a layout/art check, not a Unity capture.
- To swap in painted replacements, drop a PNG at the same path (or rebind the key in `pack.json`); 9-slice art also needs its `art_slices` border.
