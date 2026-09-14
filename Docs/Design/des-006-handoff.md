# DES-006 handoff — CellEmpty true-alpha + Play layout

**SoT:** `Docs/Design/des-006-a9e4-fail-fix.md` (Design) + portrait 1080×1920.

## Art

`Assets/Grove/Art/Area1/Env/ENV_FG_CellEmpty.png` is the DES-006 recut (`cursor/des-006-cellempty` @ `abcf378`): RGBA, transparent outside, sage frame + wood well. Magenta fringe is punched at load. `GroveArt.PlayCellSprite()` uses this well when corners are true-alpha (no generated cream plate).

## Layout (Play)

| Rule | Implementation |
|------|----------------|
| Maya unclipped | Top-bar bubble left of portrait, inside `ScreenSafe`, word wrap |
| STARTER / item names one line | Overflow + `PlayCopy.Ellipsize` — never STA/RTER or Wildflo/wer wrap |
| Order gutters | 16dp (≥12dp) |
| Icon scale | ≤70% of card inner; two-req icons do not overlap |
| Mid empty | `DesignBoardBottom` 0.70, `DesignDockTop` 0.72, ≥16dp gap |
| Crate | Left of 7×5 on the board band, 16dp from cells |
| Energy / Moved | `100/100` one line; no Moved toast |
