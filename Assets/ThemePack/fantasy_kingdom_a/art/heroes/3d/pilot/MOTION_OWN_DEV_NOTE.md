# Sir Aldric — Scene-marker attack

**HOLD** until Derek Game-view PASS.

## Status
Walk = Mixamo Standard Walk. Strike = four Scene empties under `SirAldricStrikePath`. Derek rejected another Mixamo DIAG tweak (`1821c4a` UPWARDS). The `7958283` held-pose robot clip is discarded.

Story: sword starts at LEFT hip and is drawn → rise above the head on the RIGHT → strike FORWARD → DOWN toward the foot.

**No** Mixamo DIAG playback. **No** leftover `AimArmAlong` Mixamo fight. **Not** Mixamo Mirror / DIAG_REV / DIAG_MIRR / MILD.

## Clips
| Role | File | Notes |
|---|---|---|
| Walk | `SirAldric_body_holefixed_walk.fbx` | md5 `799d851d…` unchanged |
| Slash (playing) | Scene markers `1_Draw_LeftHipPocket` … `4_Strike_DownToFoot` | Drag in Scene; next Play follows |
| Slash (leftover) | DIAG FBX + `SirAldric_DIAG_InwardSlash.anim` | unused at Play; clip.name = file |

## Drive
1. PlayableGraph mixer: walk FBX only (0.20s blend into plant).
2. After `Evaluate()`: `ReachRightArmToward` the Catmull through the four markers. Sword on `mixamorig:RightHand`.
