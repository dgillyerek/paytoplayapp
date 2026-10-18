# Sir Aldric battle attacks 2 and 3 (2026-10-18, PR #70, HOLD merge)

Rotation: 1 locked "Sword And Shield attack (4)" thrust · 2 diagonal slash · 3 shield bash.

Both bakes target the Lite Sword And Shield Pack avatar (`sword_and_shield_idle.fbx` rig: `Armature` root, 41
`mixamorig:` bones, take `Scene`, cm units) so they play through `PlayNamedClip` like the 17 pack clips. They live
only in `Assets/ThemePack/.../pilot/lite_sword_shield/` (14 MB each, mesh embedded like the pack clips); md5s in
CHECKSUMS.md5. Body bones only, fingers from idle f2, blend in 8 / out 12 frames from the lite idle pose.

## Attack 2 — diagonal slash (`Aldric Diagonal Slash`, frames 0-75, contact f24)
Source: repo `SirAldric_body_holefixed_slash_Stable_Sword_Inward_Slash_DIAG.fbx` (Mixamo "Stable Sword Inward
Slash"), source frames 31-86, yaw 0. Sword arm chambers high at f20, cuts through by f24, arms open wide to f32,
recovers by f75. Impact SwordSteel on mixamorig:RightHand, reach slide 0.2 m.

## Attack 3 — shield bash (`Aldric Shield Bash`, frames 0-59, contact f38)
Source: `SIRALDRIC_mixamo_sword_and_shield_attack_40f.fbx` (Mixamo "Sword And Shield Attack", 40 f, uploaded on
SirAldric_PILOT_accurig_humanoid). The performance is a low 360° spin charge with 2.8 m of hip travel; travel is
cancelled (hips xy held at the idle hips, hips z solved so the lowest ankle stays grounded) and the battle slides him
0.35 m (frames 8-38) instead. Yaw 22.3° from the travel direction so he finishes side-on toward the target. Shield
(left) hand furthest forward at f38. Impact HeroShieldShock (blue rings) on mixamorig:LeftHand.

## Eyeball
- Bash: the spin is in the clip; feet run in place during the cancelled charge; deep crouch (hips ~0.55 m).
- Slash: after the cut the sword arm swings out behind him (f26-57).
- Lite files have no shield/sword meshes; sheets are untextured grey Blender renders.

work/scripts: `aldric_retarget.py` + `p_bash.json`/`p_diag.json` (blender -b --python aldric_retarget.py -- params).
