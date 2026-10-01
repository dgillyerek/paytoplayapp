# Sir Aldric — edit the DIAG attack in the Animation window

**HOLD.** Game-view is SoT. PlayableGraph plays this clip: `Assets/Survival/Unity/Anims/SirAldric_DIAG_InwardSlash.anim`  
Source take: DIAG FBX `SirAldric_body_holefixed_slash_Stable_Sword_Inward_Slash_DIAG.fbx` (md5 `72412be4`, Mixamo **Inward Slash**, take **`mixamo.com`**, frames **0–85**).  
Walk stays the Walk FBX. Sword stays on `mixamorig:RightHand`. No Mixamo Mirror. No DIAG_REV / DIAG_MIRR / MILD. No raise-then-cut / AimArm.

The `.anim` in git starts as a stub. First Unity open / Play / Spawn **duplicates the imported DIAG take** into that file (GUID stays put). After you key it, Unity will **not** overwrite your curves.

## One-shot (do this)

1. Open `Assets/Survival/Scenes/SirAldric.unity`.
2. Menu **Survival → Sir Aldric → Spawn knight for Animation-window edit**.  
   This extracts the DIAG take if needed, creates/wires `SirAldric_DIAG_InwardSlash.controller` (default **Attack** state = the `.anim`), spawns **`SirAldricAttackEditKnight`**, assigns the **walk FBX Avatar** + that controller, `applyRootMotion = false`, **selects the knight**, opens **Window → Animation → Animation**, and tries to **lock** + start **Preview**.
3. Confirm Inspector on the knight: **Animator → Controller** is `SirAldric_DIAG_InwardSlash`, **Avatar** is the Mixamo walk FBX avatar (not None).
4. In the Animation window clip dropdown, pick **Attack**. If it still says read-only, you are on the FBX take — switch to `Assets/Survival/Unity/Anims/SirAldric_DIAG_InwardSlash.anim`.

## Lock / Preview gotchas (Scene frozen)

Classic “clip loaded, Scene frozen, Play greyed”:

- **Select the root `SirAldricAttackEditKnight`** (the object with the Animator). Clicking a `mixamorig:*` child in Hierarchy **greys** Animation — that child has no Animator.
- **Lock** the Animation window (lock icon, top-right of the window) **before** clicking anything else in Hierarchy. Unlocking + selecting Hierarchy is what greys Play.
- Turn **Preview** **on** (Animation window). Scrub only samples Scene while Preview is on.
- If Preview / Play stay grey: re-run **Spawn knight** (it re-wires an existing knight). Do **not** drag the clip onto an empty Scene with no AnimatorController.

Optional extract-only: **Survival → Sir Aldric → Extract editable DIAG attack clip** refreshes the `.anim` in place (same GUID) and re-selects the knight if it exists.

## Keys Derek asked for (rear Play-cam / Derek POV)

Scrub the 0–85 / ~2.83s clip. Scene should move. Add keys on **`mixamorig:RightArm`** and **`mixamorig:RightHand`** (and forearm if needed):

1. **Raise** — RH / sword **above the head first**.
2. **Cut** — strike **down and to the left** (viewer-left from the rear cam).

Do **not** X-flip the body. Do **not** re-import DIAG_REV / DIAG_MIRR.

**Ctrl+S** (or **File → Save**) so the `.anim` asset writes.

## Verify

1. Play **SirAldric** (click Game view). Default cam is rear. **1 / 2 / 3** orbit.
2. Walk: sword in the **right hand**.
3. Strike should play **your keyed `.anim`** (mixer blend 0.20s). No runtime arm overwrite. The AnimatorController is **not** used at Play — only the PlayableGraph + `.anim`.

If extract did not run, Play still falls back to the DIAG FBX until the `.anim` has curves.
