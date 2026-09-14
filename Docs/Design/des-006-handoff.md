# DES-006 handoff — CellEmpty true-alpha + Play layout notes

**Status:** Dev wired layout + fallback wells. **Art recut was not on this VM.**  
**Drop (when shared box is mounted):** `/workspace/design/unity-drop/`  
**Play SoT:** portrait **1080×1920** (Derek FAIL screenshot).

## This VM could not read the drop

`/workspace/design/` is absent here. Play still ships with:

- Layout notes from DES-006 / FAIL screenshot (gutters, no mid-word wrap, Maya unclipped, less dead space)
- Generated maple wells while pack `ENV_FG_CellEmpty.png` is the cream plate
- Runtime overlay: if `design/unity-drop/**/ENV_FG_CellEmpty.png` (or `CellEmpty` / `Cell_Empty`) appears, Play uses it **when corners are true-alpha**

## Dev: copy CellEmpty into the art pack

When the shared box is available:

```bash
# typical Design path — glob if the filename differs
cp design/unity-drop/Area1/Env/ENV_FG_CellEmpty.png \
   Assets/Grove/Art/Area1/Env/ENV_FG_CellEmpty.png
```

PASS when that PNG is **RGBA (color type 6)**, transparent corners, rim/object only (not a filled cream tile). The pack file today is **RGB color type 2** (no alpha) — that is the cream plate. After the copy, `CellEmpty_true_alpha_well_when_des006_recut_is_dropped` requires a true-alpha well.

Optional: same drop may include `ENV_FG_CellHighlight.png` — overlay already maps `CellHighlight`.

Do **not** keep the cream-plate CellEmpty on Play. `GroveArt.PlayCellSprite()` prefers the true-alpha recut; otherwise it draws generated wood pockets so Derek does not see dinner-plate tiles.

## Layout already on this branch (no drop required)

| Note | What shipped |
|------|----------------|
| Order gutters | **16dp** between cards (was 12) |
| No mid-word wrap | Icons in a top cluster; body full-width; `Wildflower` fits @ TypeOrder 30 |
| Maya unclipped | Spoken line in top-bar bubble left of portrait, inside screen safe, word wrap |
| Less dead space | Dock raised (`DesignDockTop` 0.675); board safe rect taller/wider; toast no longer steals a strip between goal and board |
| STARTER | Landscape badge + one-line label |
| Energy / Moved | `100/100` one line; no Moved toast |

## Play check after the PNG copy

`Board.unity` · Game view **1080×1920**. Cells read as wood wells (Design recut or generated fallback), not cream plates. Maya line fully on-screen. STARTER one line. Order 4 `Wildflower` intact.
