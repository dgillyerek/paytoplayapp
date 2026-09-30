# Sir Aldric — Design-owned motion package (READY)

**Date:** 2026-09-30 ~14:14 ET  
**SoT video:** https://www.youtube.com/watch?v=iQ1s3nN1330  
**Storyboard:** `/workspace/aldric_youtube_sot_iQ1s3nN1330/STORYBOARD.md`  
**Package:** `separate_portrait_20260928/motion_own_20260929/`

## Status
**READY for Dev wire.** Design owns bones. No tip-led arm/clavicle math, Path A, weight-paint, Unity mirror/scale hack, or runtime time-reverse.

**Authoritative slash:** DIAG_REV — time-reversed DIAG after tip `43b33fb` clarification (raise above head first, then strike down-left). **Not** Mixamo Mirror / L-R flip. DIAG_MIRR stays retired.

## Clips
| Role | Mixamo name | File | MD5 |
|---|---|---|---|
| Walk | Standard Walk | `SirAldric_body_holefixed_walk.fbx` | `799d851db5ca4ecfbea28a9478fc039e` |
| Slash (authoritative) | Stable Sword Inward Slash (DIAG time-reversed) | `SirAldric_body_holefixed_slash_Stable_Sword_Inward_Slash_DIAG_REV.fbx` | `0beb3c77a1f53c1604dd3c725b1ad789` |

Source DIAG bake (pre-reverse): Emphasis **50**, Angle **80**, Overdrive **30**, Arm-Space **50**, Mirror **off**. Reverse: Blender fcurve time-reverse of DIAG FBX `72412be4…`. Export record: `EXPORT_DIAG_REV.md`.

Do **not** wire as authority:
- `…_Slash_DIAG.fbx` (md5 `72412be4…`) — pre-reverse; superseded by DIAG_REV
- `…_Slash_DIAG_MIRR.fbx` (md5 `70fd9483…`) — retired (wrong call: L-R Mirror)
- `…_Slash_MILD.fbx` (md5 `8d5b78b0…`) — retired (chest sweep)
- baseline `…_Slash.fbx` (md5 `4a143441…`) — old hot TOP

## Import notes
- Import With-Skin FBX; Mixamo mid280k as ONLY visible body (not AccuRIG).
- Discard embedded duplicate mesh if Unity doubles.
- Parent sword prop `../../mixamo_20260928/props/SirAldric_PILOT_sword.{fbx,glb}` to `mixamorig:RightHand`.
- Soft: Mixamo has no finger bones; empty LH-hip scabbard OK. Blender re-export packaging may differ from raw Mixamo binary — watch import scale/orientation once. Do **not** add a Unity time-scale or mirror hack.
- AccuRIG superseded for this pilot path unless Derek reverts.

## PASS criteria (Derek Game-view vs video / his ask)
1. Walk plantable; sword in RH grip (prop).
2. Slash **time order:** raise hand above head **first**, then strike **down and to the left** (end-of-prior-loop becomes initial movement). Not a left-right flip.
3. TOP: **no** pinching shoulders / torn pauldrons; not a flat chest sweep.
4. Soft leftovers OK otherwise.

## HOLD
Merge HOLD until Derek Game-view PASS once after Dev wires play/blend/cam only.
