# Play HUD Layout v1 — Front Garden (Design FAIL fix)

**Status:** P0 — blocks Design PASS / Derek invite  
**Owner:** Design · **Integrate:** Development  
**Trigger:** Derek FAIL on e7f89cb — orders overlap board; no clear first action; board/HUD/crate unfinished vs Maya/backdrop

## Hard rules
1. **Orders never cover the board.** Tray lives in a bottom dock (safe zone), not over cells.
2. **Board is the hero** — center 55–65% of portrait height is playfield only.
3. **First action is visible without reading a novel** — teach: Tap crate → Drag merge → Deliver to order.
4. Polish: HUD/crate/pieces must feel in the same family as Maya + backdrop (soft rounded chrome, Grove Green, no debug grey).

## Portrait layout (1080×1920 ref)

| Zone | Y% | Contents |
|------|-----|----------|
| Top bar | 0–10% | Energy pill (L), Goal pill “Restore the Front Garden” (C), Coins (R). Maya portrait small top-right under coins optional. |
| Board | 12–68% | Backdrop + BoardSurface + cells + pieces + **Garden Crate** left/center on board. **No order cards here.** |
| Teach layer | over board | Spotlight rings / ghost hand — FTUE only; dismiss after first deliver. |
| Bottom dock | 70–100% | Cream dock panel: **Order tray ×3** + inventory bar ×5. Thumb-friendly. |

## Teach beats (on-screen, short)
1. Ring + label on **Garden Crate**: “Tap to grow”
2. After spit: ghost drag between two matching WF T1: “Stack matches”
3. Pulse order slot needing item: “Deliver here”
Copy max 3–4 words. Project Grove title only in boot splash, not cluttering play HUD.

## New / updated stubs
- `UI_OrderDock_Panel` — bottom dock chrome
- `UI_GoalPill` — top goal chip
- `UI_Teach_Ring` — soft gold/teal highlight ring
- `UI_Teach_Hand` — ghost hand affordance
- Refresh: `UI_OrderTray_Card`, `HUD_EnergyPill`, `HUD_Wallet_Coin`, `ENV_FG_GardenCrate_Idle` (higher polish)

## Dev AC
- [ ] Orders rendered only inside bottom dock; zero overlap with cell rects
- [ ] Goal pill always visible on play
- [ ] Teach sequence fires once (persist `ftue_play_teach_done`)
- [ ] No green grid / letter tokens
