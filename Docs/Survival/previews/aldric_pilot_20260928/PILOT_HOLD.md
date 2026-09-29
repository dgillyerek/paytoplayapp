# HOLD — Sir Aldric PILOT Mixamo separate-portrait (2026-09-28)

**HOLD merge on PR #27 until Derek Play.** Path A / ClipSword / AimChain / Dev weight-paint are not SoT. No Design / Derek PASS. AccuRIG clips-only is **superseded**.

## Wire

| Role | Asset |
| --- | --- |
| Playable Mixamo body | `Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot/SirAldric_body_holefixed_walk.fbx` |
| Unrigged look mesh (on disk) | `SirAldric_body_holefixed_mid280k.fbx` (no armature — not the Play instance) |
| Walk clip | same walk FBX · Unity takeName **`mixamo.com`** 1–36 (Mixamo Standard Walk) |
| Slash clip | `SirAldric_body_holefixed_slash.fbx` · takeName **`mixamo.com`** **8–40** (Stable Sword Inward Slash; skip f1 high-left rest so rear Play reads high-right → down-left) |
| Sword prop | `SirAldric_PILOT_sword.fbx` parented to `mixamorig:RightHand` |
| Cape prop | `SirAldric_PILOT_cape.fbx` on disk (optional soft — not auto-parented; scale/bind leftover) |
| Paint | `pilot/mixamo_tex/Meshy_AI_Lionheart_Sentinel_0929004215_texture*.png` |

Slash With-Skin mesh is **not instantiated**. `StripEmbeddedClipMeshes` disables Ico / withSkin leftovers. **Generic** CreateFromThisModel. Import **`useFileScale: 0`** so FileScale 0.01 does not shrink meter-sized Mixamo verts to ~2 cm (Derek `9075270` then did body ×88). Runtime still flattens leftover Armature **0.01** and FAIL-corrects height to 0.5–5 m. Sword localScale = `1 / Abs(hand.lossyScale)` then normalize world blade to **1.01 m** (grip at palm; blade +Z). Walk→slash is `AnimationMixerPlayable` crossfade **0.20 s**, not a hard `SetSourcePlayable` swap. `updateWhenOffscreen`. No Path 1 / Path A / ClipSword / AimChain. No `addHumanoidExtraRoot` / twist fields.

**Rear L↔R:** Mixamo instantiate faces −Z. `FaceWorldTop` yaws **180** so the back faces the Play-cam and walk goes toward TOP. That yaw also swaps world X, so the Mixamo RH inward slash read **high-LEFT → down-RIGHT** on Derek’s screen (`5f5e375` FAIL). **`MixamoRearMirrorX = −1`** restores **high-RIGHT → down-LEFT** from Derek’s Play-cam without changing clip frames or AimChain.

Light **attack-only** offset after `Evaluate()` (`ApplyAttackWindupLift`): RightShoulder X−28 / RightArm X−80 / RightHand X−40 at slash start, easing to 0 by the down-left finish; sword local Euler `(90, 0, −35)` at wind-up back to `(90, 0, 0)`. Raises the RH **above the helmet** so the **tip leads** the high-right wind-up. Not AimChain.

### takeName note

Both Mixamo clip FBXs use the FBX AnimationStack **`mixamo.com`** (not AccuRIG `Armature|…|Walking` / `rigify_clip`). Blender shows `Armature|mixamo.com|Layer0`. Unity clip names after import: `Walking` / `Attack`.

### Caveat

The unrigged body FBX has **no skeleton**. Play instantiates the Mixamo-skinned holefixed mesh from the walk export (same 139k verts / mid280k, no cape, empty RH). That is the Mixamo body — not AccuRIG.

**Derek FAIL `bf5a5fa`:** FileScale 0.01 × Mixamo mesh → ~2 cm body; Humanoid drove bones so only the sword waved.

**Derek FAIL `9075270`:** `cm-root none (already 1×)` then body scale correct `height=0.020 ×87.9`. Walk silhouette OK. Sword parented with `localScale=1` inherited **lossyScale ~88** → ~88 m blade floating (grip 0.08 m × 88). Hard `SetSourcePlayable` cut walk→slash. Fix: `useFileScale: 0` + sword world-scale compensate + mixer 0.20 s blend.

**Derek Play `063d928`:** direction claimed high-right → down-left, but wind-up not overhead / not tip-led, and the arc clipped at FOV 34.

**Derek FAIL `5f5e375`:** overhead + FOV 42 OK, but Unity yaw 180 left the slash **laterally opposite** on Play-cam (high-left → down-right from Derek’s view). Fix: `MixamoRearMirrorX`.

## Cam (1080×1920 Scale 1×)

Rear `(0, 1.92, −3.08)` LookAt `(0, 1.12, 0.15)` FOV **42**. Mesh 1×. Pull back from `(0, 1.75, −2.90)` FOV 34 so the overhead tip and down-left finish stay in frame; knight stays large (not the tiny −3.50 / FOV 40 overshoot).

## Proofs here

Play-cam rematch: Mixamo holefixed + walk frame 18 / slash **start frame 8** (hand above helmet, tip up-right) + **finish frame 32** (down-left). RH sword ~1.01 m. Rear = back. Cape not parented. Walk→slash 0.20 s mixer blend is Unity Play only — rematch stills are still poses. **Not Meshy website stills. Not Unity Game-view.**

## Unity Play

1. Play **SirAldric**. Walk/slash reimport **Generic** + `useFileScale: 0`. Log: `visible=Mixamo`, `walkLen`/`attackLen` > 0, `PILOT skin` height 0.5–5 m, `PILOT sword` world blade ≈ 1.01 m (hand.lossyScale logged).
2. Knight-readable walk. Sword in RH, ~1 m, held at palm. Walk **crossfades** ~0.20 s into inward slash: from Derek’s screen, hand **high to the RIGHT** (above the head), tip **up-right**, then **down to the LEFT**. Full knight + sword arc in Scale 1× phone Game-view.
3. HOLD until Derek PASS.
