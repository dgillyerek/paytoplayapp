# Sir Aldric — Design-owned motion package (READY)

**Date:** 2026-09-30 ~12:36 ET  
**SoT video:** https://www.youtube.com/watch?v=iQ1s3nN1330  
**Storyboard:** `/workspace/aldric_youtube_sot_iQ1s3nN1330/STORYBOARD.md`  
**Package:** `separate_portrait_20260928/motion_own_20260929/`

## Status
**READY for Dev wire.** Design owns bones. No tip-led arm/clavicle math, Path A, or weight-paint.

**Authoritative slash:** DIAG re-bake (Angle **80**) after Derek Play FAIL on tip `c5c8469` / MILD (`8d5b78b0…`) — flat chest sweep. Retire MILD for Game-view.

## Clips
| Role | Mixamo name | File | MD5 |
|---|---|---|---|
| Walk | Standard Walk | `SirAldric_body_holefixed_walk.fbx` | `799d851db5ca4ecfbea28a9478fc039e` |
| Slash (authoritative) | Stable Sword Inward Slash (DIAG bake) | `SirAldric_body_holefixed_slash_Stable_Sword_Inward_Slash_DIAG.fbx` | `72412be4405bea372184dd5f5e17e186` |

DIAG bake sliders: Emphasis **50**, Angle **80**, Overdrive **30**, Arm-Space **50**, Mirror **off**, Trim 0–100. TOP scrub @ frame **59/85**. Export record: `EXPORT_DIAG.md`.

Do **not** wire as authority:
- `…_Slash_MILD.fbx` (md5 `8d5b78b0…`) — retired after Play FAIL (chest sweep)
- baseline `…_Slash.fbx` (md5 `4a143441…`) — old hot TOP

## Import notes
- Import With-Skin FBX; Mixamo mid280k as ONLY visible body (not AccuRIG).
- Discard embedded duplicate mesh if Unity doubles.
- Parent sword prop `../../mixamo_20260928/props/SirAldric_PILOT_sword.{fbx,glb}` to `mixamorig:RightHand`.
- Soft: Mixamo has no finger bones; empty LH-hip scabbard OK.
- AccuRIG superseded for this pilot path unless Derek reverts.

## PASS criteria (Derek Game-view vs video)
1. Walk plantable; sword in RH grip (prop).
2. Slash path: low RH hip → backswing behind → **high-right diagonal chamber** → descending diagonal cut → low-left finish (character-relative). **FAIL** if mid reads as flat horizontal chest sweep / sword pointing too far left on a flat plane.
3. TOP: **no** pinching shoulders / torn pauldrons.
4. Soft leftovers OK otherwise.

## HOLD
Merge HOLD until Derek Game-view PASS once vs video after Dev wires play/blend/cam only.
