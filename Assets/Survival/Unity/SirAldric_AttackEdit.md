# Sir Aldric — attack clip (`SirAldric_DIAG_InwardSlash`)

**HOLD.** Game-view is SoT. PlayableGraph plays `Assets/Survival/Unity/Anims/SirAldric_DIAG_InwardSlash.anim`.

**Compile:** the AnimationClip’s **name must be `SirAldric_DIAG_InwardSlash`**, same as the file. Naming it `Attack` does not match `SirAldric_DIAG_InwardSlash` and Unity will not compile the asset.

Derek is done hand-keying. Dev owns the motion. Sequence (rear cam / Derek POV):

1. Walk straight (Walk FBX).
2. Reach down into the **LEFT hip pocket** and pull the sword out.
3. Raise the sword **directly above the head, RIGHT side**.
4. Strike **FORWARD**, then **DOWN toward his foot**.

Baked into the mixer clip at Play (`AttackLeftHipDrawReach`). No post-Evaluate AimArmAlong. No DIAG_REV / DIAG_MIRR / MILD / Mixamo Mirror.

Sword: sheath/draw at **LEFT hip**; strike and walk-after-draw on `mixamorig:RightHand`. Orbit 1/2/3 unchanged.

## Animation window (optional inspect)

1. Open `Assets/Survival/Scenes/SirAldric.unity`.
2. **Survival → Sir Aldric → Spawn knight for Animation-window edit**.
3. **Window → Animation → Animation**. Select root **`SirAldricAttackEditKnight`** → **Lock** Animation → **Preview** on → scrub `mixamorig:RightArm` / `mixamorig:RightHand`.

Do not rename the clip to `Attack`.
