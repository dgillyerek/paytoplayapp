# DES-006 — a9e4bb6 phone Play FAIL fix (Design)

**SoT FAIL (CEO/Derek frame):** clipped Maya line; wrapped STARTER/Wildflower; overlapping order icons; cream cell plates; empty dead space.

## Design drops
- `Env/ENV_FG_CellEmpty.png` — re-export, magenta-keyed alpha (no outer cream plate)
- Keep production `UI_OrderTray_Card` / Deliver / Starter — Dev must **inset item icons** inside card safe pad (≥12px @1080) so icons don’t overlap neighbors

## Dev layout (must)
1. Maya bubble fully inside top-right HUD band — no clip
2. STARTER + item names: **single line**, truncate with ellipsis if needed; never wrap
3. Order cards: horizontal gutters ≥12dp; icon scale ≤70% of card inner
4. Reduce mid empty: board band bottom closer to dock (Design BoardBottom ~0.68–0.70; DockTop ~0.72) while keeping ≥16dp gap
5. No cream plate behind pieces or cells in Play

## Re-PASS
Fresh phone Play only after this tip — do not re-ask Derek’s old frame.
