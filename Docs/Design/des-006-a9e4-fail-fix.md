# DES-006 — a9e4bb6 phone Play FAIL fix (Design)

**SoT FAIL (CEO/Derek frame):** clipped Maya line; wrapped STARTER/Wildflower; overlapping order icons; cream cell plates; empty dead space.

## Design drops
- `Env/ENV_FG_CellEmpty.png` — re-export, magenta-keyed alpha (no outer cream plate)
- Keep production `UI_OrderTray_Card` / Deliver / Starter — Dev must **inset item icons** inside card safe pad (≥12px @1080) so icons don’t overlap neighbors

## Dev layout (must)
1. Maya bubble fully inside top-right HUD — no clip
2. STARTER + item names: single line, ellipsis; never wrap
3. Order cards: gutters ≥12dp; icon ≤70% card inner; inset ≥12px @1080
4. BoardBottom ~0.68–0.70, DockTop ~0.72, ≥16dp gap
5. CellEmpty from cursor/des-006-cellempty @ abcf378 — no cream plates
+ ban Moved toast; no energy/Charges wrap

## Re-PASS
Fresh phone Play only after this tip — do not re-ask Derek’s old frame.
