# HOLD — Sir Aldric PILOT clips-only AccuRIG wire (2026-09-28)

**HOLD merge on PR #27 until Derek Play.** Path A / ClipSword / AimChain are not SoT. No Design / Derek PASS.

## Wire

| Role | Asset |
| --- | --- |
| Visible body (ONLY) | `Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot/SirAldric_PILOT_accurig_humanoid.fbx` |
| Walk clip | `SirAldric_PILOT_walk_accurig.fbx` take `Armature\|Armature\|Armature\|Walking` 1–25 |
| Attack clip | `SirAldric_PILOT_attack_library.fbx` take `target_character\|target_character\|target_character\|Right_Hand_Sword_Slash` 1–38 |
| Paint | `pilot_albedo.png` + AccuRIG metallic/roughness |

Meshy AI Attack_Forward (`SirAldric_PILOT_attack_accurig.fbx`) is **discarded**. Library/mocap Right-hand Sword Slash only. Clip FBX With-Skin meshes are **not instantiated**. `StripEmbeddedClipMeshes` disables Ico / withSkin leftovers. No Path 1 retarget. No `addHumanoidExtraRoot` / twist fields.

## Cam (1080×1920 Scale 1×)

Rear `(0, 1.75, −2.90)` LookAt `(0, 0.85, 0.15)` FOV 34. Mesh 1×.

## Proofs here

Play-cam rematch: AccuRIG `SirAldric_PILOT_look_mid280k` + clip actions (clip With-Skin mesh discarded). **Not Meshy website stills. Not Unity Game-view.**

Walk still = Walking frame 9/1–25. Library slash still = `Right_Hand_Sword_Slash` frame 15/1–38 (planted one-hand slash, no unsheathe).

Walk Unity takeName is the FBX stack **`Armature|Armature|Armature|Walking`** (not 4×). Attack Unity takeName is the FBX stack **`target_character|target_character|target_character|Right_Hand_Sword_Slash`**. Ignore rest take `…Slash.001` (1–3).

Library clip bones are Mixamo-prefixed (`mixamorig:Hips`). Unity Humanoid `CreateFromThisModel` retargets onto AccuRIG. Blender rematch copies Mixamo→AccuRIG by bone map for stills only.

## Unity Play

1. Play **SirAldric**. Log: `visible=AccuRIG` and `walkLen`/`attackLen` > 0.
2. AccuRIG painted look. Walk then library slash. Sword fused. No unsheathe.
3. HOLD until Derek PASS.
