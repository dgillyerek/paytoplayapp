# Art budget

**Maya is 2D UI / sprite source only.** Nothing 3D ships in the player for P0.

Area 1 Play Mode binds **DES-001/002** from `Assets/Grove/Art/Area1/` (`manifest.json` stub → PNG). Programmer `Resources/Grove/Art` circles are removed.

| Bucket | P0 | Notes |
| --- | --- | --- |
| Board pieces | WF T1–6, Herb T1–3, Tools T1–3 | Sprite 2D+UI, PPU 100, center pivot |
| Board chrome | Backdrop, tray, cream cells, crate states | Hero backdrop ≤2048 |
| HUD | Energy pill, coins (gems hidden), order tray, primary button | Maya Neutral + Happy |
| Splashes | Boot / area start / milestone (DEV-018) | Plus order-complete / tier-up / sparkle |
| Snap-back | Ease / squash | Code tween |
| Font | Built-in UI font | LegacyRuntime / Arial |
| 3D / Maya runtime | **None** | 2D portrait only |

If an asset needs a custom shader beyond URP 2D / UI, it is over budget.
