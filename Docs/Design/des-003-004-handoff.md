# DES-003 / DES-004 Handoff (Dev-ready)

**Aligned to:** `/workspace/product/thin-inplay-teach.md`  
**Drop:** `/workspace/design/unity-drop/Area1/` (+ grove mirror)  
**Layout detail:** `play-hud-layout-v1.md`

## DES-003 — Play HUD (anchors)

Portrait 1080×1920 logical:

| Element | Anchor | Notes |
|---------|--------|-------|
| Energy + coins | Top bar, Y 0–8% | Never collide with orders |
| Goal pill | Top center under bar | “Restore the Front Garden” |
| Maya portrait + 1-line bubble | Top-right HUD band only | Not on board |
| Board (backdrop + surface + cells) | Center Y ~10–66% | **Sanctity** — no order/teach permanent chrome over cells |
| Garden Crate | On board left/center | Teach ring overlays temporarily |
| Order dock | Bottom Y 70–100% | `UI_OrderDock_Panel` + 3× `UI_OrderTray_Card`; **≥16dp gap** above board edge |
| Inventory | Inside dock under orders | 5 slots |

**FAIL if:** any order card rect intersects any playable cell rect.

## DES-004 — Teach (T1–T3 only)

| Beat | Assets | EN line (exact) |
|------|--------|-----------------|
| T1 Crate | `UI_Teach_Ring` on crate | Tap the crate to grow supplies. |
| T2 Merge | Ring on 2 matches + `UI_Teach_Hand` drag; magnet @4s idle | Drag two matches together. |
| T3 Deliver | Ring on Order 1 card + needed item | Deliver to Maya. |

Once per new save until Order 1 done. Skippable after beat 1 if player already acted correctly.

## PNG stubs ready now
- `UI/UI_OrderDock_Panel.png`
- `UI/UI_GoalPill.png`
- `UI/UI_Teach_Ring.png`
- `UI/UI_Teach_Hand.png`
- Existing: `UI_OrderTray_Card`, HUD energy/coin, Maya Neutral/Happy, crate, board polish

Re-pull unity-drop and implement DEV-019/020.
