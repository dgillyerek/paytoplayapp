# HOLD — Sir Aldric PILOT Mixamo separate-portrait (2026-09-28)

**HOLD merge on PR #27 until Derek Game-view PASS vs the slash video.** Path A / ClipSword / AimChain / Dev weight-paint are not SoT. Slash SoT is https://www.youtube.com/watch?v=iQ1s3nN1330 (Judith Hamma “Sword Swing - Animation”): sword in the **right hand**, high character-right → sweep down to low character-left. Prior +Z mid-hold / horizontal-thrust arc guesses are overridden. Walk SoT remains Derek `0fc0930` / `98dabbb` RH grip. Orbit 1/2/3 stays. Rematch optional.

## Wire

| Role | Asset |
| --- | --- |
| Playable Mixamo body | `Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot/SirAldric_body_holefixed_walk.fbx` |
| Unrigged look mesh (on disk) | `SirAldric_body_holefixed_mid280k.fbx` (no armature — not the Play instance) |
| Walk clip | same walk FBX · Unity takeName **`mixamo.com`** 1–36 (Mixamo Standard Walk) |
| Slash clip | `SirAldric_body_holefixed_slash.fbx` · takeName **`mixamo.com`** **8–40** (Stable Sword Inward Slash) |
| Sword prop | `SirAldric_PILOT_sword.fbx` walk + strike **`mixamorig:RightHand`**. Leftover **`HipSheathSocket`** on **`mixamorig:Hips`** is unused. |
| Cape prop | `SirAldric_PILOT_cape.fbx` on disk (optional soft — not auto-parented; scale/bind leftover) |
| Paint | `pilot/mixamo_tex/Meshy_AI_Lionheart_Sentinel_0929004215_texture*.png` |

Slash With-Skin mesh is **not instantiated**. `StripEmbeddedClipMeshes` disables Ico / withSkin leftovers. **Generic** CreateFromThisModel. Import **`useFileScale: 0`**. Sword localScale = `1 / Abs(hand.lossyScale)` then normalize world blade to **1.01 m**. Walk→slash is `AnimationMixerPlayable` **0.20 s**. `updateWhenOffscreen`. No Path 1 / Path A / ClipSword / AimChain. **Not a whole-body X-flip.**

**Sword hand:** walk/idle = `mixamorig:RightHand` along-arm grip (Derek `0fc0930` / `98dabbb` Game-view). Hip-sheath socket code is leftover only. Strike = stay on `mixamorig:RightHand`. FaceWorldTop yaws 180 and **Abs** scale (never `scale.x = -1`). Derek FAIL `212c6da` whole-body X-flip put the sword on the left hand — discarded. Local Generic Euler extras (`5f5e375`) swung the opposite way on Play-cam — discarded.

Light **attack-only** extras after `Evaluate()`, re-applied on **camera render**. Walk: `BindSwordTo(mixamorig:RightHand)` + along-arm snap. `HipSheathSocket` / `HipSheathGripLocal` / `HipSheathBladeLocal` remain on disk (leftover). Unity bake blade = **local +Y**. Do **not** aim with world `Vector3.left/right/back` or Mixamo `hips.right` — that locked the blade **world-left across the neck** (Derek FAIL `cdfbea5`). Arm-lock only when `attackU>0`. Attack: `BindSwordTo(mixamorig:RightHand)`, YouTube iQ1s3nN1330 diagonal (high-right → low-left, **no +Z hold**), forearm–hand snap. Not AimChain. Not a body X-flip. **Rematch is optional and not Game-view proof.**

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

**Derek FAIL `cdfbea5`:** Game-view (laptop) — sword **constantly facing left**, floating horizontal behind neck/upper back, hip sheath empty.

**Derek FAIL `879a6f3`:** orbit keys/drag did **not** rotate Play-cam (legacy `Input.GetKey` is dead while Input System owns Play). Blade still **left from the hand**, started on the **left** side. Blind yaw 180 when the FBX already faces +Z puts `mixamorig:RightHand` on world −X. Fix: yaw only if RH is left of LH; sheath on RH/+X; Input System + OnGUI orbit.

**Derek console `8098f64`:** `QuaternionToEuler: Input quaternion was not normalized` from `GUIUtility:ProcessEvent`. Play-cam `LookAt` wrote a denormalized rotation; IMGUI then converted it. Fix: `LookRotation` + `Quaternion.Normalize` (no `.eulerAngles` on a raw LookAt).

**Derek compile FAIL `8098f64`:** `CS1061` `'Event' does not contain a definition for 'repeat'`. OnGUI now keys off `EventType.KeyDown` + `keyCode != None` (no `Event.repeat`). Orbit + RH hip sheath unchanged.

**Derek FAIL `7187204`:** Game-view still **rear-only** (1/2/3 Q/E RMB dead) and sword **R→L across the torso**. Rematch stills are the **visual target**; Game-view must match them (hip sheath tip out/down the right hip; orbit rear / 3/4 / front).

**Derek FAIL `52aba6b`:** worse. Walk sheath **jumped** (RightUpLeg follow). Attack blade **R→L in front** (arm-axis snap). Camera still **no rotation**.

**Derek FAIL `0fc0930` Game-view (walk SoT shots):** walk sword **floating**, not in the RH. Strike **ends high on the character’s left** instead of **low LL**. Hip sheath superseded. Walk = **RH grip**, along-arm (`98dabbb`). Strike path later superseded by the YouTube clip.

**Derek slash SoT (video, overrides prior arc guesses):** https://www.youtube.com/watch?v=iQ1s3nN1330 — RH grip; continuous high character-right → sweep down to low character-left. The `FrontHoldU` +Z plateau / horizontal thrust (`b0a9509` / `0fc0930` mid) was a guess. `FrontHoldU = FrontU = 0.45`. HOLD until Derek Game-view PASS vs this clip.

## Cam (1080×1920 Scale 1×)

Rear `(0, 1.92, −3.08)` LookAt `(0, 1.12, 0.15)` FOV **42**. Mesh 1×.

## Proofs here

Play-cam rematch: walk f18 (older hip-sheath dump `sir_aldric_pilot_dump_walk_f18.json`) / slash start f8 / mid f20 / finish f32. Numeric dumps `sir_aldric_pilot_dump_slash_f8.json` / `f20` / `f32`. **Not Meshy website stills.** **Rematch is optional.** **Not Unity Game-view.** Game-view must match https://www.youtube.com/watch?v=iQ1s3nN1330, not rematch.

## Unity Play

1. Play **SirAldric** (click the Game view). Default cam = **rear**. **1 / 2 / 3** orbit. HUD `cam <yaw>` must change.
2. **Walk (Derek 0fc0930 SoT):** sword **in the right hand**, along-arm — not floating at the hip.
3. **Strike (YouTube iQ1s3nN1330):** start **high on the character’s right**, sweep **down to low on the character’s left**. Must not +Z-thrust mid. Must not finish high-left.
4. HOLD until Derek Game-view PASS vs that video.
