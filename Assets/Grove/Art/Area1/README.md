# Project Grove — Unity Drop (Area 1)

**Working title:** Project Grove only  
**Gate:** DES-001 atlas + DES-002 splashes  
**Repo target:** `Assets/Grove/Art/Area1/`

## Import
- PNG → Sprite (2D and UI)
- PPU 100, Pivot Center (0.5, 0.5)
- Piece atlas ≤1024; hero backdrop ≤2048; ASTC-ready
- Bind via `manifest.json` stub → filename → category

## DES-001 covered
WF T1–6, Herb T1–3, Tools T1–3, crate states, board cells, backdrop, HUD, order tray, Maya Neutral (+ Happy)

## DES-002 covered
- Splash_Boot_ProjectGrove
- Splash_AreaStart_FrontGarden
- Splash_Milestone_FrontGardenRestored
(+ OrderComplete, TierUp, MergeSparkle)

## DES-003 / DES-004 covered
- UI_OrderDock_Panel, UI_GoalPill, UI_Teach_Ring, UI_Teach_Hand

## DES-005 covered
- True-alpha piece re-exports (WF/HB/TL)
- UI_Btn_Deliver, UI_Teach_Banner, UI_Badge_Starter, refreshed UI_OrderTray_Card
- Sprite import: Texture Type Sprite, Alpha Is Transparency ON, mip maps off

Asset count: 45
