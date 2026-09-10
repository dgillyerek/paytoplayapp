# Art budget

**Maya is 2D UI / sprite source only.** Nothing 3D ships in the player for P0.

| Bucket | P0 | Notes |
| --- | --- | --- |
| Board pieces | 5 silhouettes (pebble→grove) | Flat shapes + palette; 256² max |
| Board chrome | Frame, cell, grab highlight | 9-slice |
| Snap-back | Ease / squash | Code tween, not a cinematic |
| VFX | One merge pop | Sprite flipbook or UI scale |
| Audio | Merge + error + drop | Tiny SFX pack |
| Font | One UI font | TMP |
| 3D / Maya runtime | **None** | Maya only if a contractor prefers it to author 2D renders |

If an asset needs a custom shader beyond URP 2D / UI, it is over budget. Prefer uGUI or UI Toolkit; no HDRP, no filmic volumes on device.
