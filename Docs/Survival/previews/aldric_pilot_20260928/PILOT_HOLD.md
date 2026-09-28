# HOLD — Sir Aldric PILOT clips-only AccuRIG wire (2026-09-28)

**HOLD merge on PR #27 until Derek Play.** Path A / ClipSword / AimChain are not SoT. No Design / Derek PASS.

## Wire

| Role | Asset |
| --- | --- |
| Visible body (ONLY) | `Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot/SirAldric_PILOT_accurig_humanoid.fbx` |
| Walk clip | `SirAldric_PILOT_walk_accurig.fbx` take `Armature\|Armature\|Armature\|Walking` 1–25 |
| Attack clip | `SirAldric_PILOT_attack_accurig.fbx` take `rigify_clip` 3–92 |
| Paint | `pilot_albedo.png` + AccuRIG metallic/roughness |

Clip FBX With-Skin meshes are **not instantiated**. `StripEmbeddedClipMeshes` disables Ico / withSkin leftovers. No AvatarBuilder. No `addHumanoidExtraRoot` / `legTwist`.

## Cam (1080×1920 Scale 1×)

Rear `(0, 1.75, −2.90)` LookAt `(0, 0.85, 0.15)` FOV 34. Mesh 1×.

## Proofs here

Play-cam rematch: AccuRIG `SirAldric_PILOT_look_mid280k` + clip actions (clip With-Skin mesh discarded). **Not Meshy website stills. Not Unity Game-view.**

Walk still = Walking frame 9/1–25. Attack still = `rigify_clip` frame 27 (RightArm windup; later frames yaw ~140°).

Blender names the walk action `Armature|Armature|Armature|Armature|Walking` (object prefix). Unity takeName is the FBX stack **`Armature|Armature|Armature|Walking`**. Attack Unity takeName **`rigify_clip`** (Blender `target_character|rigify_clip|BaseLayer`). Do not use the 1-frame `Walking.001` rest take or `clip0` 2–3.

## Unity Play

1. Play **SirAldric**. Log: `visible=AccuRIG` and `walkLen`/`attackLen` > 0.
2. AccuRIG painted look. Walk then attack. Sword fused.
3. HOLD until Derek PASS.
