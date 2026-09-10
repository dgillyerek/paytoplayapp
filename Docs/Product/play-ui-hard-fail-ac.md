# Play UI Hard-FAIL AC (Derek — raised bar)

**Status:** Active · blocks Design PASS / QA PASS / Derek invite  
**Trigger:** Derek HARD FAIL — debug overlay on board, dock clipping orders, placeholder chrome  

---

## Player / business outcome

Front Garden Play must look like a **shipped casual F2P screen** — not a debug layout. If anything on Play reads as programmer/debug UI, it is **FAIL**.

---

## Raised acceptance (ALL required)

| # | Rule | FAIL examples |
|---|------|----------------|
| 1 | **Production-only chrome** | Cream/Grove Green HUD from Design pack only |
| 2 | **Zero debug overlays** | Green/grey cell debug, BOARD SAFE labels, layout wireframes, FPS/stats, gizmo text, pink/letter tokens, editor guides left on |
| 3 | **Zero UI overlap** | Order cards, dock, Maya, teach, goal pill intersecting any playable cell **or** clipping each other |
| 4 | **Dock integrity** | Orders fully inside bottom dock; no clip/crop of card art or text |
| 5 | **Splash parity** | Play HUD/crate/pieces feel same family as boot/milestone splashes |
| 6 | **Thin teach only** | T1–T3 exact lines; temporary rings/hand only — no permanent debug teach chrome |
| 7 | **No missing textures** | Pink/magenta voids, broken sprites, white squares, unloaded atlases |

| 8 | **True-alpha icons** | No white/opaque plates behind pieces or HUD glyphs; transparent PNG/atlas alpha only |
| 9 | **Mobile-readable type** | HUD/order/teach copy legible at phone scale (≤1080p ref); no hairline type |
| 10 | **Phone-scaled HUD** | Dock, pills, cards sized for thumbs (44–48dp min taps); not desktop-wireframe scale |
| 11 | **No greybox/blob UI** | Flat grey blobs, untextured panels, placeholder shapes = FAIL |

Prior nine prototype criteria still apply. This doc **tightens** visual FAIL — Design visual gate is binding.

---

## Ownership

| Lane | Owns |
|------|------|
| **Design** | Visual PASS/FAIL; replace any placeholder chrome; verify no debug in Play frames |
| **Dev** | Ship build with debug overlays **off** in Play; layout rects honor dock; no wireframe/mock sprites in player path |
| **QA** | NOT PASS without Play evidence proving rules 1–6 |
| **Product** | AC owner; hold invite until Design + QA PASS |

---

## Explicit bans (do not ship)

- DES003 layout mock / “BOARD SAFE” art used as runtime chrome  
- Debug cell grids or letter-tier piece stand-ins  
- Order dock that crops cards  
- Any `OnGUI` / debug draw / layout gizmo visible in Play  
- White plates / opaque cards behind piece icons  
- Greybox or blob placeholder UI panels  
- Desktop-scale HUD that fails phone readability  

---

**— Product**


---

## Evidence + type floors (QA-aligned)

| Rule | Spec |
|------|------|
| Evidence | **Portrait phone Game view** (or device) screenshot/video required for PASS — not desktop-only free aspect |
| Type size floor | HUD labels ≥ **18px @ 1080p**; HUD numbers ≥ **28px**; teach/order body ≥ **24px**; no weight thinner than Regular for outdoor glare |
| Contrast | Ink Soft on Sun Cream / light fills; light text on dark chips — aim WCAG AA where practical |
| Icons | True alpha only — FAIL opaque white plates |



---

## Design companion

**DES-005** (`/workspace/design/docs/des-005-mobile-hud-pass.md`) is Design’s implementation checklist — meets or exceeds Product floors (their type mins are higher). Product SoT for PASS/FAIL remains this file + DES-005 checklist; Design visual PASS required.
