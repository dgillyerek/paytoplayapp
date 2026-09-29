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

Slash With-Skin mesh is **not instantiated**. `StripEmbeddedClipMeshes` disables Ico / withSkin leftovers. **Generic** CreateFromThisModel (not Humanoid — Playable retarget collapsed the holefixed skin on `bf5a5fa`). Runtime flattens Mixamo Armature **0.01** (cm) so body is ~1.8 m, not ~2 cm. `updateWhenOffscreen`. No Path 1 retarget. No `addHumanoidExtraRoot` / twist fields.

### takeName note

Both Mixamo clip FBXs use the FBX AnimationStack **`mixamo.com`** (not AccuRIG `Armature|…|Walking` / `rigify_clip`). Blender shows `Armature|mixamo.com|Layer0`. Unity clip names after import: `Walking` / `Attack`.

### Caveat

The unrigged body FBX has **no skeleton**. Play instantiates the Mixamo-skinned holefixed mesh from the walk export (same 139k verts / mid280k, no cape, empty RH). That is the Mixamo body — not AccuRIG.

**Derek FAIL `bf5a5fa` root cause:** Mixamo With-Skin Armature Lcl Scaling is **0.01**. Unity FileScale also applies 0.01 (UnitScaleFactor 1 = cm). Stacked → body world height **~2 cm**. Humanoid Playable then drives `mixamorig:*` at 1.8 m, so Game-view showed only the ~1 m RH sword waving. BindPaintedLook still reported `renderers=1`. Fix: Generic clips + flatten 0.01 → 1× + bounds FAIL if height not in 0.5–5 m.

## Cam (1080×1920 Scale 1×)

Rear `(0, 1.75, −2.90)` LookAt `(0, 0.85, 0.15)` FOV 34. Mesh 1×.

## Proofs here

Play-cam rematch: Mixamo holefixed + walk frame 18 / slash peak frame 32 + RH sword. Rear = back (no extra blender yaw). Cape not parented (UnitScale leftover). **Not Meshy website stills. Not Unity Game-view.**

## Unity Play

1. Play **SirAldric**. Walk/slash FBXs must reimport as **Generic** (`animationType: 2`). Log: `visible=Mixamo`, `walkLen`/`attackLen` > 0, `PILOT Mixamo cm-root … 0.01 → 1`, and `PILOT skin` bounds height 0.5–5 m (FAIL loud otherwise).
2. Knight-readable Mixamo painted look filling Play-cam. Walk then inward slash. Sword in RH (~1 m blade). Soft cape OK.
3. HOLD until Derek PASS.
