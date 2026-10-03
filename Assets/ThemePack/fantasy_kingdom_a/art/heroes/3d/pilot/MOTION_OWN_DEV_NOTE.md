# Sir Aldric — Mixamo DIAG human attack

**HOLD** until Derek Game-view PASS.

## Status
Mixer plays Mixamo **DIAG** `Stable Sword Inward Slash` (md5 `72412be4`). Human arm/shoulder/torso. The `7958283` held-pose robot clip is discarded.

Story: sword starts at LEFT hip and is drawn → rise above the head on the RIGHT → strike FORWARD → DOWN toward the foot.

Derek `1821c4a`: linear DIAG strike went UPWARDS. `MixamoDiagPlaybackU` plays the EXPORT descent (f59–85) as the strike; `MixamoDiagStrikeBladeLocal` pitches the prop forward then down. Body stays DIAG.

**No** post-Evaluate `AimArmAlong`. **Not** Mixamo Mirror / DIAG_REV / DIAG_MIRR / MILD.

## Clips
| Role | File | Notes |
|---|---|---|
| Walk | `SirAldric_body_holefixed_walk.fbx` | md5 `799d851d…` unchanged |
| Slash (playing) | DIAG FBX + `SirAldric_DIAG_InwardSlash.anim` | Mixamo human take; clip.name = file |

## Drive
1. PlayableGraph mixer: walk FBX → DIAG take (0.20s blend).
2. After `Evaluate()`: sword parent LEFT hip until `MixamoDiagDrawEndU` 0.18, then `mixamorig:RightHand`. Strike play-head = `MixamoDiagPlaybackU`. Strike blade = `MixamoDiagStrikeBladeLocal` (forward then down).
