# Sir Aldric — edit the DIAG attack in the Animation window

**HOLD.** Game-view is SoT. PlayableGraph plays this clip: `Assets/Survival/Unity/Anims/SirAldric_DIAG_InwardSlash.anim`  
Source take: DIAG FBX `SirAldric_body_holefixed_slash_Stable_Sword_Inward_Slash_DIAG.fbx` (md5 `72412be4`, Mixamo **Inward Slash**, take **`mixamo.com`**, frames **0–85**).  
Walk stays the Walk FBX. Sword stays on `mixamorig:RightHand`. No Mixamo Mirror. No DIAG_REV / DIAG_MIRR / MILD.

The `.anim` in git starts as a stub. First Unity open / Play / the menu below **duplicates the imported DIAG take** into that file. After you key it, Unity will **not** overwrite your curves.

## One-time extract

1. Open `Assets/Survival/Scenes/SirAldric.unity`.
2. Menu **Survival → Sir Aldric → Extract editable DIAG attack clip**.
3. Project window should ping `SirAldric_DIAG_InwardSlash` (clip name **Attack**).

## Open the Animation window on the knight

1. Menu **Survival → Sir Aldric → Spawn knight for Animation-window edit**  
   (creates `SirAldricAttackEditKnight` from the Mixamo walk FBX — same `mixamorig:*` bones Play uses).
2. In **Hierarchy**, select **`SirAldricAttackEditKnight`**.
3. **Window → Animation → Animation** (or `Ctrl+6`).
4. In the Animation window clip dropdown, pick **Attack**, **or** drag  
   `Assets/Survival/Unity/Anims/SirAldric_DIAG_InwardSlash.anim` from Project onto the Animation window.

If the window says the clip is read-only, you are still on the FBX take. Switch to the `.anim` in `Assets/Survival/Unity/Anims/`.

## Keys Derek asked for (rear Play-cam / Derek POV)

Scrub the 0–85 / ~2.83s clip. Add keys on **`mixamorig:RightArm`** and **`mixamorig:RightHand`** (and forearm if needed):

1. **Raise** — RH / sword **above the head first**.
2. **Cut** — strike **down and to the left** (viewer-left from the rear cam).

Do **not** X-flip the body. Do **not** re-import DIAG_REV / DIAG_MIRR.

**Ctrl+S** (or **File → Save**) so the `.anim` asset writes.

## Verify

1. Play **SirAldric** (click Game view). Default cam is rear. **1 / 2 / 3** orbit.
2. Walk: sword in the **right hand**.
3. Strike should play **your keyed `.anim`** (mixer blend 0.20s). No runtime arm overwrite.

If extract did not run, Play still falls back to the DIAG FBX until the `.anim` has curves.
