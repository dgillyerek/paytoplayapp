# HOLD — Sir Aldric PILOT Mixamo separate-portrait (2026-09-28)

**HOLD merge on PR #27 until Derek Game-view PASS.** Path A / ClipSword / AimChain / Dev weight-paint are not SoT. No Design / Derek PASS. AccuRIG clips-only is **superseded**. **Rematch stills are not proof.** Derek’s laptop Game-view is the walk-sheath SoT.

## Wire

| Role | Asset |
| --- | --- |
| Playable Mixamo body | `Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot/SirAldric_body_holefixed_walk.fbx` |
| Unrigged look mesh (on disk) | `SirAldric_body_holefixed_mid280k.fbx` (no armature — not the Play instance) |
| Walk clip | same walk FBX · Unity takeName **`mixamo.com`** 1–36 (Mixamo Standard Walk) |
| Slash clip | `SirAldric_body_holefixed_slash.fbx` · takeName **`mixamo.com`** **8–40** (Stable Sword Inward Slash) |
| Sword prop | `SirAldric_PILOT_sword.fbx` walk **`HipSheathSocket`** on **`mixamorig:Hips`** (character-right after yaw 180); strike **`mixamorig:RightHand`** |
| Cape prop | `SirAldric_PILOT_cape.fbx` on disk (optional soft — not auto-parented; scale/bind leftover) |
| Paint | `pilot/mixamo_tex/Meshy_AI_Lionheart_Sentinel_0929004215_texture*.png` |

Slash With-Skin mesh is **not instantiated**. `StripEmbeddedClipMeshes` disables Ico / withSkin leftovers. **Generic** CreateFromThisModel. Import **`useFileScale: 0`**. Sword localScale = `1 / Abs(hand.lossyScale)` then normalize world blade to **1.01 m**. Walk→slash is `AnimationMixerPlayable` **0.20 s**. `updateWhenOffscreen`. No Path 1 / Path A / ClipSword / AimChain. **Not a whole-body X-flip.**

**Sword hand:** walk/idle = `mixamorig:Hips` hip sheath on the **outside of the right hip**, tip down beside the right leg — **not** in `mixamorig:RightHand`, **not** through the mesh. Strike only = transfer to `mixamorig:RightHand`. FaceWorldTop yaws 180 and **Abs** scale (never `scale.x = -1`). Derek FAIL `212c6da` whole-body X-flip put the sword on the left hand — discarded. Local Generic Euler extras (`5f5e375`) swung the opposite way on Play-cam — discarded. Prefer RH parent + world-space tip arc **for strike only**.

Light **attack-only** extras after `Evaluate()`, re-applied on **camera render**. Walk: **`HipSheathSocket`** child of `mixamorig:Hips`, socket rotation = **instance FaceWorldTop** (yaw 180). Grip / blade in **character space** (`HipSheathGripLocal` / `HipSheathBladeLocal`): +X character-right, +Y up, +Z forward. Unity bake blade = **local +Y**. Do **not** aim with world `Vector3.left/right/back` or Mixamo `hips.right` — that locked the blade **world-left across the neck** (Derek FAIL `cdfbea5`). Arm-lock only when `attackU>0`. Attack: `BindSwordTo(mixamorig:RightHand)`, UR → horizontal front +Z → LL, forearm–hand snap. Not AimChain. Not a body X-flip. **Rematch is not Game-view proof.**

Play-cam **defaults to rear** `(0, 1.92, −3.08)`. Orbit: **1** rear, **2** 3/4, **3** front, **Q/E** or **RMB** drag. Do not change the default rear frame.

**Design lean FAIL `a9f8aff`:** rematch walk still showed the sword in the **right hand**. “Sheath” only changed RH aim while the prop stayed a RightHand child. Strike arm-lock rematch was OK; Design cannot lean PASS without a walk still that clearly shows **true hip sheath** (not RH grip). This tip reparents walk to **Hips**.

**Derek Game-view `a9f8aff`:** walk sheath a little better but the blade still **horizontal behind the neck/shoulders** (across the torso), not **out/down along the outside of the right hip**. Mixer walk→slash blend ran arm-lock while `attackU=0`; RH child + back-aim laid the 1.01 m blade across the back. **Derek’s shot is the walk sheath SoT.** Rematch ≠ Game-view.

### takeName note

Both Mixamo clip FBXs use the FBX AnimationStack **`mixamo.com`**. Blender shows `Armature|mixamo.com|Layer0`. Unity clip names: `Walking` / `Attack`.

### Caveat

The unrigged body FBX has **no skeleton**. Play instantiates the Mixamo-skinned holefixed mesh from the walk export. Mixamo body — not AccuRIG.

**Derek FAIL `bf5a5fa`:** FileScale 0.01 × Mixamo mesh → ~2 cm body; Humanoid sword-only.

**Derek FAIL `9075270`:** giant sword from inherited lossyScale ~88 + hard clip cut. Fix: `useFileScale: 0` + world blade 1.01 m + mixer 0.20 s.

**Derek FAIL `5f5e375`:** local bone Euler extras swung the arc the wrong way on Play-cam.

**Derek FAIL `212c6da`:** whole-body X-flip put the sword on the **left** hand.

**Derek FAIL `b0a9509`:** mid Game-view blade straight **up** (world +Y / HUD TOP). `FrontUp 0.48` plus `FromToRotation(Vector3.forward, desired)` aimed the wrong mesh axis after Unity FBX bake (Design +Z → local +Y). Rematch stills that looked like a forward thrust **are not Unity Game-view** — camera-up ≈ world +Y, so extra +Y made rematch screen-up look like “forward.” Trust Game-view. Fix: `FrontUp 0.06`, `FrontHoldU` plateau, rest-blade `FromToRotation`. Mid is a **horizontal thrust** toward the enemy along the yellow path.

**Derek FAIL `b2ce8f7`:** sword **dragged / trailed** the Mixamo arm. Independent `FromToRotation(restBlade, AttackSlashReach)` + `Slerp` aimed the prop at a slower key path than the hand.

**Derek FAIL `19b16aa`:** Game-view **walk** blade through right thigh/hip; **strike** hand at hip while blade sat upper-right. LateUpdate arm-lock ran in walk (pierce) and strike extras were overwritten before render (hand vs blade desync). Rematch stills did **not** match Game-view. Fix: sheath-only on walk; forearm snap only on attack; re-apply pose on `beginCameraRendering`.

**Design lean FAIL `a9f8aff`:** walk rematch still **RH-held**. Hip sheath must be a Hips (or sheath-socket) parent, not an aimed RightHand child.

**Derek Game-view `a9f8aff`:** blade **across the back / behind the neck**, not out/down the right hip.

**Derek FAIL `cdfbea5`:** Game-view (laptop) — sword **constantly facing left**, floating horizontal behind neck/upper back, hip sheath empty. Hands at sides. HUD hip sheath walk. Rematch stills did **not** match. Cause: sheath used world axes / Mixamo hip-bone axes and a measured blade local, so after FaceWorldTop yaw 180 the visible blade stayed **world-left**. Fix: character-space socket + local +Y → character-right+down.

## Cam (1080×1920 Scale 1×)

Rear `(0, 1.92, −3.08)` LookAt `(0, 1.12, 0.15)` FOV **42**. Mesh 1×.

## Proofs here

Play-cam rematch: walk f18 (**hip sheath** on `mixamorig:Hips`, not RH grip; dump `sir_aldric_pilot_dump_walk_f18.json`) / slash start f8 (RH max-reach UR) / mid f20 (horizontal front +Z; rear is **foreshortened** toward the enemy cube, not a sky stick) / finish f32 (max-reach LL). Numeric dumps `sir_aldric_pilot_dump_slash_f8.json` / `f20` / `f32`. **Not Meshy website stills. Not Unity Game-view.** Rematch camera `to_track_quat` can disagree with Unity LookAt on screen-right — Unity Play is the gate.

## Unity Play

1. Play **SirAldric**. Generic + `useFileScale: 0`. Default cam = **rear Play-cam**. **1 / 2 / 3** = rear / 3/4 / front. **Q/E** or **RMB** orbit. Walk sword = `HipSheathSocket` on Hips. Strike = RightHand. Blade ≈ 1.01 m.
2. **Walk Game-view (SoT):** blade on the **outside of the right hip**, tip **out/down beside the right leg** — not world-left across the neck, not in the RH, not through the mesh. Strike: blade on the arm; UR → front → LL.
3. HOLD until Derek Game-view PASS. Rematch is not proof.
