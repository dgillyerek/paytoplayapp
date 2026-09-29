# HOLD — Sir Aldric PILOT Mixamo separate-portrait (2026-09-28)

**HOLD merge on PR #27 until Derek Play.** Path A / ClipSword / AimChain / Dev weight-paint are not SoT. No Design / Derek PASS. AccuRIG clips-only is **superseded**.

## Wire

| Role | Asset |
| --- | --- |
| Playable Mixamo body | `Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot/SirAldric_body_holefixed_walk.fbx` |
| Unrigged look mesh (on disk) | `SirAldric_body_holefixed_mid280k.fbx` (no armature — not the Play instance) |
| Walk clip | same walk FBX · Unity takeName **`mixamo.com`** 1–36 (Mixamo Standard Walk) |
| Slash clip | `SirAldric_body_holefixed_slash.fbx` · takeName **`mixamo.com`** 1–67 (Stable Sword Inward Slash) |
| Sword prop | `SirAldric_PILOT_sword.fbx` parented to `mixamorig:RightHand` |
| Cape prop | `SirAldric_PILOT_cape.fbx` on disk (optional soft — not auto-parented; scale/bind leftover) |
| Paint | `pilot/mixamo_tex/Meshy_AI_Lionheart_Sentinel_0929004215_texture*.png` |

Slash With-Skin mesh is **not instantiated**. `StripEmbeddedClipMeshes` disables Ico / withSkin leftovers. No Path 1 retarget. No `addHumanoidExtraRoot` / twist fields.

### takeName note

Both Mixamo clip FBXs use the FBX AnimationStack **`mixamo.com`** (not AccuRIG `Armature|…|Walking` / `rigify_clip`). Blender shows `Armature|mixamo.com|Layer0`. Unity clip names after import: `Walking` / `Attack`.

### Caveat

The unrigged body FBX has **no skeleton**. Play instantiates the Mixamo-skinned holefixed mesh from the walk export (same 139k verts / mid280k, no cape, empty RH). That is the Mixamo body — not AccuRIG.

## Cam (1080×1920 Scale 1×)

Rear `(0, 1.75, −2.90)` LookAt `(0, 0.85, 0.15)` FOV 34. Mesh 1×.

## Proofs here

Play-cam rematch: Mixamo holefixed + walk frame 18 / slash peak frame 32 + RH sword. Rear = back (no extra blender yaw). Cape not parented (UnitScale leftover). **Not Meshy website stills. Not Unity Game-view.**

## Unity Play

1. Play **SirAldric**. Log: `visible=Mixamo` and `walkLen`/`attackLen` > 0.
2. Mixamo painted look. Walk then inward slash. Sword in RH. Soft cape OK.
3. HOLD until Derek PASS.
