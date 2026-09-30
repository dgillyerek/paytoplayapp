# Sir Aldric — Design-owned motion package (READY)

**Date:** 2026-09-30 ~13:33 ET  
**SoT video:** https://www.youtube.com/watch?v=iQ1s3nN1330  
**Storyboard:** `/workspace/aldric_youtube_sot_iQ1s3nN1330/STORYBOARD.md`  
**Package:** `separate_portrait_20260928/motion_own_20260929/`

## Status
**READY for Dev wire.** Design owns bones. No tip-led arm/clavicle math, Path A, weight-paint, or Unity mirror/scale hack.

**Authoritative slash:** DIAG_MIRR (Angle **80**, Mixamo **Mirror ON**) after Derek Play FAIL on tip `95b5a4a` / DIAG (`72412be4…`) — cut was the opposite direction; whole-clip reverse baked in Mixamo.

## Clips
| Role | Mixamo name | File | MD5 |
|---|---|---|---|
| Walk | Standard Walk | `SirAldric_body_holefixed_walk.fbx` | `799d851db5ca4ecfbea28a9478fc039e` |
| Slash (authoritative) | Stable Sword Inward Slash (DIAG_MIRR) | `SirAldric_body_holefixed_slash_Stable_Sword_Inward_Slash_DIAG_MIRR.fbx` | `70fd94832eb1b09e9b8a7b8dda9d2f45` |

DIAG_MIRR bake: Emphasis **50**, Angle **80**, Overdrive **30**, Arm-Space **50**, **Mirror ON**, Trim 0–100. Export record: `EXPORT_DIAG_MIRR.md`.

Do **not** wire as authority:
- `…_Slash_DIAG.fbx` (md5 `72412be4…`) — retired after Play FAIL (cut opposite)
- `…_Slash_MILD.fbx` (md5 `8d5b78b0…`) — retired (chest sweep)
- baseline `…_Slash.fbx` (md5 `4a143441…`) — old hot TOP

## Import notes
- Import With-Skin FBX; Mixamo mid280k as ONLY visible body (not AccuRIG).
- Discard embedded duplicate mesh if Unity doubles.
- Parent sword prop `../../mixamo_20260928/props/SirAldric_PILOT_sword.{fbx,glb}` to `mixamorig:RightHand`.
- Soft: Mixamo has no finger bones; empty LH-hip scabbard OK. Mixamo 3D preview blanked during MIRR QC captures — Game-view is hard gate.
- AccuRIG superseded for this pilot path unless Derek reverts.

## PASS criteria (Derek Game-view vs video)
1. Walk plantable; sword in RH grip (prop).
2. Slash path matches SoT direction: low RH hip → backswing → high diagonal chamber → descending diagonal cut → low finish (character-relative). **FAIL** if cut reads opposite of the video / prior DIAG fail direction.
3. TOP: **no** pinching shoulders / torn pauldrons; not a flat chest sweep.
4. Soft leftovers OK otherwise.

## HOLD
Merge HOLD until Derek Game-view PASS once vs video after Dev wires play/blend/cam only.
