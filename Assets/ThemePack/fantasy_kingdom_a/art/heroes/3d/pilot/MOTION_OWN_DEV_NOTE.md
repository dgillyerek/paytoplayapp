# Sir Aldric — Design-owned motion package (READY)

**Date:** 2026-09-29 ~23:09 ET  
**SoT video:** https://www.youtube.com/watch?v=iQ1s3nN1330  
**Storyboard:** `/workspace/aldric_youtube_sot_iQ1s3nN1330/STORYBOARD.md`  
**Package:** `separate_portrait_20260928/motion_own_20260929/`

## Status
**READY for Dev wire.** Design owns bones. No tip-led arm/clavicle math, Path A, or weight-paint.

## Clips
| Role | Mixamo name | File | MD5 |
|---|---|---|---|
| Walk | Standard Walk | `SirAldric_body_holefixed_walk.fbx` | `799d851db5ca4ecfbea28a9478fc039e` |
| Slash (authoritative) | Stable Sword Inward Slash (mild bake) | `SirAldric_body_holefixed_slash_Stable_Sword_Inward_Slash_MILD.fbx` | `8d5b78b0d3608ef0d0fb022ae9af16f1` |

Mild bake sliders: Emphasis **54**, Overdrive **28**, Angle 50, Arm-Space 50. TOP @ frame **60/85**. Do **not** use the baseline `…_Slash.fbx` (md5 `4a143441…`) — that is the old hot TOP.

## Import notes
- Import With-Skin FBX; Mixamo mid280k as ONLY visible body (not AccuRIG).
- Discard embedded duplicate mesh if Unity doubles.
- Parent sword prop `../../mixamo_20260928/props/SirAldric_PILOT_sword.{fbx,glb}` to `mixamorig:RightHand`.
- Soft: Mixamo has no finger bones; empty LH-hip scabbard OK.
- AccuRIG superseded for this pilot path unless Derek reverts.

## PASS criteria (Derek Game-view vs video)
1. Walk plantable; sword in RH grip (prop).
2. Slash path: low RH hip → backswing behind → high-right diagonal → cut low-left (character-relative; not mirrored opposite).
3. TOP: **no** pinching shoulders / torn pauldrons.
4. Soft leftovers OK otherwise.

## HOLD
Merge HOLD until Derek Game-view PASS once vs video after Dev wires play/blend/cam only.
