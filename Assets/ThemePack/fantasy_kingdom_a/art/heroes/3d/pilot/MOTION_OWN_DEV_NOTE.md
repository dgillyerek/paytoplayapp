# Sir Aldric — walk only

**HOLD** until Derek Game-view PASS.

## Status
Walk = Mixamo Standard Walk. **No attack at Play.** Derek reverted ALL attacks (DIAG, rebuilt clip, four-marker path, blade pitch, sword swing). `1821c4a` UPWARDS leftover. The `7958283` held-pose robot clip is discarded.

Leftover story (unused): sword starts at LEFT hip → rise above the head on the RIGHT → strike FORWARD → DOWN toward the foot.

**No** Mixamo DIAG playback. **No** leftover `AimArmAlong`. **Not** Mixamo Mirror / DIAG_REV / DIAG_MIRR / MILD.

## Clips
| Role | File | Notes |
|---|---|---|
| Walk | `SirAldric_body_holefixed_walk.fbx` | md5 `799d851d…` unchanged — this is Play |
| Slash (leftover) | Scene markers `1_Draw_LeftHipPocket` … `4_Strike_DownToFoot` | inactive; do not drive |
| Slash (leftover) | DIAG FBX + `SirAldric_DIAG_InwardSlash.anim` | unused at Play; clip.name = file |

## Drive
1. PlayableGraph: walk FBX only, looping.
2. After `Evaluate()`: sword on `mixamorig:RightHand` walk grip. Leftover `ReachRightArmToward` unused.
