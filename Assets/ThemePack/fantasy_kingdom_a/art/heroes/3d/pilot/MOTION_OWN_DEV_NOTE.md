# Sir Aldric — Sword And Shield Slash

**HOLD** until Derek Game-view PASS.

## Status
Walk = Mixamo Standard Walk (`SirAldric_body_holefixed_walk.fbx`, md5 `799d851d`). After walk, Play Design Sword And Shield Slash (`SirAldric_body_holefixed_slash_SWORD_SHIELD_ATTACK.fbx`, md5 `cc4f97a3`, 74 frames / 30 FPS / Mirror off) on **that FBX's own Mixamo auto-rig**. Do not remap onto the walk skeleton. Not the 53-frame Sword And Shield Attack.

Leftover 130cec4 walk only. Leftover DIAG / rebuilt clip / four-marker path / blade pitch unused.

Leftover story (unused): sword starts at LEFT hip → rise above the head on the RIGHT → strike FORWARD → DOWN toward the foot.

**No** Mixamo DIAG playback. **No** leftover `AimArmAlong`. **Not** Mixamo Mirror / DIAG_REV / DIAG_MIRR / MILD.

## Clips
| Role | File | Notes |
|---|---|---|
| Walk | `SirAldric_body_holefixed_walk.fbx` | md5 `799d851d…` unchanged — walk avatar |
| Slash (playing) | `SirAldric_body_holefixed_slash_SWORD_SHIELD_ATTACK.fbx` | own Mixamo auto-rig; clip as authored |
| Slash (leftover) | Scene markers `1_Draw_LeftHipPocket` … `4_Strike_DownToFoot` | inactive; do not drive |
| Slash (leftover) | DIAG FBX + `SirAldric_DIAG_InwardSlash.anim` | unused at Play; clip.name = file |

## Drive
1. Walk PlayableGraph on the walk FBX avatar.
2. After two walk cycles, hide the walk avatar and Evaluate this slash FBX on its own Animator / PlayableGraph. Sword prop identity-parented to that RightHand (no pitch). No shield prop.
