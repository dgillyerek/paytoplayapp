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

Slash With-Skin mesh is **not instantiated**. `StripEmbeddedClipMeshes` disables Ico / withSkin leftovers. **Generic** CreateFromThisModel. Import **`useFileScale: 0`** so FileScale 0.01 does not shrink meter-sized Mixamo verts to ~2 cm (Derek `9075270` then did body ×88). Runtime still flattens leftover Armature **0.01** and FAIL-corrects height to 0.5–5 m. Sword localScale = `1 / hand.lossyScale` then normalize world blade to **1.01 m** (grip at palm; blade +Z). Walk→slash is `AnimationMixerPlayable` crossfade **0.20 s**, not a hard `SetSourcePlayable` swap. `updateWhenOffscreen`. No Path 1 / Path A / ClipSword / AimChain. No `addHumanoidExtraRoot` / twist fields.

### takeName note

Both Mixamo clip FBXs use the FBX AnimationStack **`mixamo.com`** (not AccuRIG `Armature|…|Walking` / `rigify_clip`). Blender shows `Armature|mixamo.com|Layer0`. Unity clip names after import: `Walking` / `Attack`.

### Caveat

The unrigged body FBX has **no skeleton**. Play instantiates the Mixamo-skinned holefixed mesh from the walk export (same 139k verts / mid280k, no cape, empty RH). That is the Mixamo body — not AccuRIG.

**Derek FAIL `bf5a5fa`:** FileScale 0.01 × Mixamo mesh → ~2 cm body; Humanoid drove bones so only the sword waved.

**Derek FAIL `9075270`:** `cm-root none (already 1×)` then body scale correct `height=0.020 ×87.9`. Walk silhouette OK. Sword parented with `localScale=1` inherited **lossyScale ~88** → ~88 m blade floating (grip 0.08 m × 88). Hard `SetSourcePlayable` cut walk→slash. Fix: `useFileScale: 0` + sword world-scale compensate + mixer 0.20 s blend.

## Cam (1080×1920 Scale 1×)

Rear `(0, 1.75, −2.90)` LookAt `(0, 0.85, 0.15)` FOV 34. Mesh 1×.

## Proofs here

Play-cam rematch: Mixamo holefixed + walk frame 18 / slash peak frame 32 + RH sword ~1.01 m. Rear = back. Cape not parented. Walk→slash 0.20 s mixer blend is Unity Play only — rematch stills are still poses. **Not Meshy website stills. Not Unity Game-view.**

## Unity Play

1. Play **SirAldric**. Walk/slash reimport **Generic** + `useFileScale: 0`. Log: `visible=Mixamo`, `walkLen`/`attackLen` > 0, `PILOT skin` height 0.5–5 m, `PILOT sword` world blade ≈ 1.01 m (hand.lossyScale logged).
2. Knight-readable walk. Sword in RH, ~1 m, held at palm — not an 88 m floater. Walk **crossfades** ~0.20 s into inward slash.
3. HOLD until Derek PASS.
