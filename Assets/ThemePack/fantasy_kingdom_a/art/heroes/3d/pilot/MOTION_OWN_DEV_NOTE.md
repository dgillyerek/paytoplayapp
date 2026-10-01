# Sir Aldric — Design-owned motion package + editable DIAG .anim

**Date:** 2026-09-30  
**SoT video:** https://www.youtube.com/watch?v=iQ1s3nN1330  
**Package:** `separate_portrait_20260928/motion_own_20260929/`

## Status
**Derek GO — Dev owns the strike in Unity.** No more Design FBX re-bakes for this tip loop.

Playing Mixamo clip: editable DIAG `.anim` (duplicate of md5 `72412be4…`) — last clip that animated in Game-view. DIAG_REV (`0beb3c77…`, `e90b654`) showed **no attack**. Derek FAIL `95aba89` raise-then-cut RH drive — **no** post-Evaluate `AttackRaiseThenCutReach` / `AimArmAlong`. Derek keys raise / down-left in the Animation window. **Not** Mixamo Mirror / L-R flip.

## Clips
| Role | Mixamo name | File | MD5 |
|---|---|---|---|
| Walk | Standard Walk | `SirAldric_body_holefixed_walk.fbx` | `799d851db5ca4ecfbea28a9478fc039e` |
| Slash (source take) | Stable Sword Inward Slash (DIAG bake) | `SirAldric_body_holefixed_slash_Stable_Sword_Inward_Slash_DIAG.fbx` | `72412be4405bea372184dd5f5e17e186` |
| Slash (playing clip) | Attack (editable) | `Assets/Survival/Unity/Anims/SirAldric_DIAG_InwardSlash.anim` | extract of 72412be4 |

Do **not** wire as the playing clip:
- `…_Slash_DIAG_REV.fbx` (md5 `0beb3c77…`) — retired; Game-view no attack
- `…_Slash_DIAG_MIRR.fbx` (md5 `70fd9483…`) — retired (L-R Mirror)
- `…_Slash_MILD.fbx` (md5 `8d5b78b0…`) — retired (chest sweep)
- baseline `…_Slash.fbx` (md5 `4a143441…`) — old hot TOP

## Drive
1. PlayableGraph mixer: walk FBX → editable DIAG `.anim` (0.20s blend), sword on `mixamorig:RightHand`.
2. After `Evaluate()`: sword parent only. **No** `AttackRaiseThenCutReach` / `AimArmAlong`.
3. Derek keys `mixamorig:RightArm` / `RightHand` in Window → Animation → Animation. Spawn assigns `SirAldric_DIAG_InwardSlash.controller` so Scene Preview can scrub. Select knight → lock Animation → Preview on. See `Assets/Survival/Unity/SirAldric_AttackEdit.md`.

## HOLD
Merge HOLD until Derek Game-view PASS once vs video / raise-then-down-left ask.
