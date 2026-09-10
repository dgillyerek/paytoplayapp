# DES-005 — Mobile HUD + Alpha Icon PASS Criteria (HARD FAIL #2)

**Status:** Blocks Design PASS / Derek invite  
**Tip context:** Play must look store-ready on **portrait mid-range phone**, not desktop Game view

## Type floors (Product SoT — FAIL below these @1080p)
| Role | Min px | Design target |
|------|--------|---------------|
| Labels | ≥18 | 22–28 |
| HUD numbers | ≥28 | 32–36 |
| Teach / DELIVER | ≥24 | 28–34 |

## Must fix
1. **True alpha icons** — piece + order item sprites are RGBA; **no opaque white/cream plates**. Unity: Texture Type Sprite, Alpha Is Transparency = ON, generate mip maps off for UI.
2. **Readable type @1080×1920 portrait**
   | Role | Min size | Weight | Color |
   |------|----------|--------|-------|
   | HUD numbers (energy/coins) | 32–36px | SemiBold | Ink Soft `#2A2E2C` on cream OR Sun Cream on Grove Green |
   | Goal / teach banner | 28–32px | SemiBold | Ink Soft on cream |
   | Order labels / DELIVER | 30–34px | Bold | Sun Cream on Grove Green button |
   | Tiny chrome | never < 22px | Medium | — |
   Contrast: body text vs fill ≥ WCAG AA where practical. No thin grey-on-grey.
3. **Phone layout** — use DES-003 bands; safe area top/bottom; dock cards with **gutters ≥12dp**, no card clip; board never under dock.
4. **Production chrome only** — use `UI_Btn_Deliver`, `UI_Teach_Banner`, `UI_OrderTray_Card`, `UI_OrderDock_Panel`, `UI_Badge_Starter`, `UI_GoalPill`. **Ban** black debug boxes, yellow debug rings, checkerboard leaf plates, grey placeholder panels.

## Re-sign-off checklist (Design PASS)
- [ ] No white blob behind any piece on board or in order slots
- [ ] Teach banner + DELIVER readable at arm’s length on phone aspect
- [ ] Order cards fully inside dock, separated, not overlapping board
- [ ] No debug/programmer chrome in Play
- [ ] Feels within one family of Maya + garden backdrop

## Drop
`/workspace/design/unity-drop/Area1/` — pieces re-exported alpha; new HUD chrome in `UI/`
