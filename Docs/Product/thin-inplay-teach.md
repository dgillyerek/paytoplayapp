# Project Grove — Thin In-Play Teach + Play HUD Layout (post–Derek FAIL)

**Status:** Active · unblocks Design visual PASS / QA path  
**Trigger:** Derek FAIL — order cards overlap board; no first-action guidance; play UI polish ≠ splash  
**Scope:** Limited slice only — **not** full FTUE 0–12  

---

## Player outcome

A new player can complete **crate → merge matching → deliver Order 1** without getting lost, on a play HUD that matches splash polish and **does not cover the board**.

---

## Layout AC (Design + Dev) — FAIL if unmet

| Rule | Spec |
|------|------|
| Board sanctity | Order tray / Maya / teach chrome **never overlap** playable cells |
| Order tray | Docked **below or beside** board in a reserved HUD band; max 3 cards; readable thumbs |
| Safe margins | ≥16dp gap between board edge and any HUD chrome |
| Visual parity | Play HUD materials/corners/type match splash identity (Grove Green / Sun Cream / Ink Soft) — no debug chrome |
| Energy + coins | Top bar; never collide with order cards |
| Maya | Portrait + one-line bubble in HUD band only (2D); not on board |

---

## Thin teach (3 beats only)

Trigger once per new save until Order 1 complete. Skippable after beat 1 if player already acted correctly. No multi-screen campaign.

| Beat | When | Player sees | Must do |
|------|------|-------------|---------|
| **T1 Crate** | Area start after Front Garden splash | Highlight Garden Crate + line: “Tap the crate to grow supplies.” | Tap crate ≥1 |
| **T2 Merge** | After first spit / or if 2× WF T1 present | Highlight two matching pieces + “Drag two matches together.” | Complete one merge |
| **T3 Deliver** | When Order 1 item ready (WF T2) | Highlight order card + item + “Deliver to Maya.” | Complete Order 1 |

**Fail-safes (keep thin):** magnet/ghost on T2 if idle 4s; auto-highlight correct order item on T3.  
**Out:** Beats 0–12 FTUE script, inventory lesson, dig lesson, herb intro teach — those stay post-slice.

---

## Tickets

| ID | Owner | Work |
|----|-------|------|
| **DES-003** | Design | Play HUD layout mock: board + reserved order band + top resource bar + Maya slot; fix overlap; match splash chrome |
| **DES-004** | Design | Teach affordances: crate/merge/deliver highlight rings + 3 one-liners (EN) |
| **DEV-019** | Dev | Implement HUD layout per DES-003 (no board overlap) |
| **DEV-020** | Dev | Thin teach T1–T3 state machine; completes before/with Order 1 |

---

## Exit addendum (prototype)

Previous six criteria **plus**:
7. Order chrome never overlaps board  
8. Thin teach T1–T3 completable; new player can finish Order 1 guided  
9. Play HUD visually consistent with splash (Design sign-off)

---

**— Product**
