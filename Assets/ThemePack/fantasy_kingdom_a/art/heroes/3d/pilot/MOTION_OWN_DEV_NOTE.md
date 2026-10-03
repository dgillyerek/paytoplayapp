# Sir Aldric — Dev-owned left-hip-draw attack clip

**Date:** 2026-10-03  
**HOLD** until Derek Game-view PASS.

## Status
**Dev owns the strike.** Derek is done hand-keying. Playing clip name **must** be `SirAldric_DIAG_InwardSlash` (file + clip.name). `Attack` as clip.name does not match and Unity will not compile.

Rebuilt 3.20s clip with held beats (not the ddda018 smear). Rear cam / Derek POV:
- left-hip draw **0.32–0.90s**
- raise overhead right **1.28–1.79s**
- strike forward **2.18–2.56s**
- down to the foot **2.88–3.20s**

Hand at hip-relative meters in the mixer clip. **No** post-Evaluate `AimArmAlong` (95aba89 FAIL). **Not** Mixamo Mirror / DIAG_REV / DIAG_MIRR / MILD.

## Clips
| Role | File | Notes |
|---|---|---|
| Walk | `SirAldric_body_holefixed_walk.fbx` | md5 `799d851d…` unchanged |
| Slash (playing) | `Assets/Survival/Unity/Anims/SirAldric_DIAG_InwardSlash.anim` | Dev-authored; name = file |
| Slash (leftover source) | `…_Slash_DIAG.fbx` | md5 `72412be4…` not the playing take |

## Drive
1. PlayableGraph mixer: walk FBX → authored `.anim` (0.20s blend).
2. After `Evaluate()`: sword parent only — LEFT hip during draw, `mixamorig:RightHand` for strike / walk after draw.
3. See `Assets/Survival/Unity/SirAldric_AttackEdit.md`.
